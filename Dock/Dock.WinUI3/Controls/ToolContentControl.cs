using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = ContentPresenterName, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = DockableControlName, Type = typeof(DockableControl))]
    public sealed class ToolContentControl : ContentControl
    {
        public const string ContentPresenterName = "PART_ContentPresenter";
        public const string DockableControlName = "PART_DockableControl";
        public ToolContentControl()
        {
            this.DefaultStyleKey = typeof(ToolContentControl);

            Loaded += ToolContentControl_Loaded;
            Unloaded += ToolContentControl_Unloaded;
        }

        private void ToolContentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_toolContentToken != 0 && DataContext is Tool tool)
            {
                tool.UnregisterPropertyChangedCallback(Tool.ContentProperty, _toolContentToken);
                _toolContentToken = 0;
            }

            DataContextChanged -= ToolContentControl_DataContextChanged;
            LayoutUpdated -= ToolContentControl_LayoutUpdated;

            // The content element is model-owned and shared across hosts. Detach
            // it while this tree is still alive: when this control unloads
            // because its window is CLOSING (float → redock), an element left
            // attached to the closed window's tree poisons the next host —
            // measuring it there throws E_INVALIDARG "Value does not fall
            // within the expected range".
            if (_contentPresenter is not null)
            {
                _contentPresenter.Content = null;
            }
        }

        private void ToolContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += ToolContentControl_DataContextChanged;

            // Closed-loop guard: event-driven attachment (DataContextChanged /
            // ActiveDockable / Loaded) is one-shot and loses races against the
            // dock/float/flyout migrations — a single missed event leaves the
            // pane blank forever. LayoutUpdated re-validates every layout pass,
            // so a detached state can survive at most one frame regardless of
            // what caused it.
            LayoutUpdated -= ToolContentControl_LayoutUpdated;
            LayoutUpdated += ToolContentControl_LayoutUpdated;

            // Restore content after an Unloaded detach (pin/flyout/tab round trips).
            BindData();
        }

        private void ToolContentControl_LayoutUpdated(object sender, object e)
        {
            EnsureContentAttached();
        }

        private void ToolContentControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // New content assignment: give the guard a fresh budget.
            _repairAttempts = 0;
            _repairSuspended = false;
            _healthyStreak = 0;

            // A departing DataContext must release the shared element too — a
            // presenter still holding it blocks the tool's next host.
            if (args.NewValue is not Tool && _contentPresenter is not null)
            {
                _contentPresenter.Content = null;
            }

            BindData();
        }

        /// <summary>
        /// Permanently stands this host down for window teardown. The dying
        /// window still counts as visible until its deferred close is pumped, so
        /// without this the watchdog steals the element back from its new host.
        /// </summary>
        internal void StandDownForTeardown()
        {
            _repairSuspended = true;
            LayoutUpdated -= ToolContentControl_LayoutUpdated;

            if (_contentPresenter is not null)
            {
                _contentPresenter.Content = null;
            }
        }

        /// <summary>
        /// Verifies that the model-owned content element is really hosted by
        /// this presenter and repairs it when it is not. Costs two reference
        /// comparisons on the healthy path.
        /// </summary>
        private void EnsureContentAttached()
        {
            if (_repairSuspended || _contentPresenter is null || DataContext is not Tool tool)
            {
                return;
            }

            if (tool.Content is not UIElement expected)
            {
                return;
            }

            if (ReferenceEquals(_contentPresenter.Content, expected)
                && ReferenceEquals(VisualTreeHelper.GetParent(expected), _contentPresenter))
            {
                // Forgive the repair budget only after a SUSTAINED healthy run:
                // in a two-host fight each host wins every other frame, so a
                // reset-on-every-healthy-frame counter never trips the limit.
                if (++_healthyStreak >= HealthyStreakToForgive)
                {
                    _repairAttempts = 0;
                }

                return;
            }

            _healthyStreak = 0;

            // Only a host that is actually on screen may claim the shared
            // element — otherwise a collapsed flyout and the docked pane would
            // steal it back and forth every frame.
            if (!IsEffectivelyVisible(this))
            {
                return;
            }

            if (++_repairAttempts > RepairAttemptLimit)
            {
                _repairSuspended = true;
                Internal.DockDiag.Log(
                    $"ToolContentControl watchdog SUSPENDED on {Internal.DockDiag.Describe(this)} after {RepairAttemptLimit} consecutive repairs (hosts fighting over the element?)");
                return;
            }

            Internal.DockDiag.Log(
                $"ToolContentControl watchdog REATTACH {Internal.DockDiag.Describe(expected)} -> {Internal.DockDiag.Describe(this)} (was in {Internal.DockDiag.Describe(VisualTreeHelper.GetParent(expected))})");

            DetachFromCurrentHost(expected, _contentPresenter);
            _contentPresenter.Content = null;
            _contentPresenter.Content = expected;
            _dockableControl?.RecordSize();
        }

        /// <summary>
        /// True when the element is loaded and no ancestor is collapsed — i.e.
        /// this host is really presenting, not parked off screen.
        /// </summary>
        internal static bool IsEffectivelyVisible(FrameworkElement element)
        {
            if (element is null || !element.IsLoaded)
            {
                return false;
            }

            DependencyObject node = element;
            for (int hops = 0; node is not null && hops < 32; hops++)
            {
                if (node is UIElement ui && ui.Visibility != Visibility.Visible)
                {
                    return false;
                }

                node = VisualTreeHelper.GetParent(node);
            }

            return true;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _contentPresenter = GetTemplateChild(ContentPresenterName) as ContentPresenter;
            _dockableControl = GetTemplateChild(DockableControlName) as DockableControl;

            BindData();
        }

        private void BindData()
        {
            if (DataContext is Tool tool)
            {
                if (_toolContentToken != 0)
                    tool.UnregisterPropertyChangedCallback(Tool.ContentProperty, _toolContentToken);

                _toolContentToken = tool.RegisterPropertyChangedCallback(Tool.ContentProperty, ToolContentChangedCallback);
                UpdateContent();
            }
        }

        private void UpdateContent()
        {
            if (_repairSuspended || _contentPresenter is null || DataContext is not Tool tool)
            {
                return;
            }

            var content = tool.Content;

            if (content is UIElement element)
            {
                var visualParent = VisualTreeHelper.GetParent(element);

                if (ReferenceEquals(_contentPresenter.Content, content))
                {
                    // Presenter still REFERENCES this element, but the element
                    // may have been visually stolen by another host in between
                    // (float/flyout round trips). A same-value assignment is a
                    // no-op, so the presenter never re-hooks it — the silent
                    // branch behind "pane docks back empty". Force a re-hook.
                    if (!ReferenceEquals(visualParent, _contentPresenter))
                    {
                        Internal.DockDiag.Log(
                            $"ToolContentControl.UpdateContent REHOOK host={Internal.DockDiag.Describe(this)} content={Internal.DockDiag.Describe(content)} visualParent={Internal.DockDiag.Describe(visualParent)}");
                        DetachFromCurrentHost(element, _contentPresenter);
                        _contentPresenter.Content = null;
                        _contentPresenter.Content = content;
                    }

                    _dockableControl?.RecordSize();
                    return;
                }

                // The same element may still sit in another live presenter —
                // release it there first or measuring the double-parented
                // element throws E_INVALIDARG. NOTE: FrameworkElement.Parent
                // does not surface a ContentPresenter host; use the VISUAL
                // parent. A previous host in an already-closed window throws
                // on access; that detach is best-effort.
                DetachFromCurrentHost(element, _contentPresenter);
            }

            _contentPresenter.Content = content;
            _dockableControl?.RecordSize();
        }

        internal static void DetachFromCurrentHost(UIElement element, ContentPresenter newHost)
        {
            try
            {
                var visualParent = VisualTreeHelper.GetParent(element);
                if (visualParent is null || ReferenceEquals(visualParent, newHost))
                {
                    return;
                }

                Internal.DockDiag.Log($"DetachFromCurrentHost: releasing {Internal.DockDiag.Describe(element)} from {Internal.DockDiag.Describe(visualParent)}");

                switch (visualParent)
                {
                    case ContentPresenter presenter:
                        presenter.Content = null;
                        break;
                    case ContentControl contentControl:
                        contentControl.Content = null;
                        break;
                    case Border border:
                        border.Child = null;
                        break;
                    case Panel panel:
                        panel.Children.Remove(element);
                        break;
                }
            }
            catch
            {
            }
        }

        private void ToolContentChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if (dp == Tool.ContentProperty)
            {
                UpdateContent();
            }
        }

        private const int RepairAttemptLimit = 10;
        private const int HealthyStreakToForgive = 10;

        private long _toolContentToken = 0;
        private int _repairAttempts;
        private int _healthyStreak;
        private bool _repairSuspended;
        ContentPresenter _contentPresenter;
        private DockableControl _dockableControl;
    }
}

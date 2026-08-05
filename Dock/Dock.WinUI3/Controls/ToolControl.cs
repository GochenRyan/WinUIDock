using CommunityToolkit.WinUI;
using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = TabStripName, Type = typeof(ToolTabStrip))]
    [TemplatePart(Name = ToolContentControlName, Type = typeof(ContentControl))]
    public sealed class ToolControl : ContentControl
    {
        public const string TabStripName = "PART_TabStrip";
        public const string DockableControlName = "PART_DockableControl";
        public const string ToolContentControlName = "PART_ToolContentControl";

        public ToolControl()
        {
            this.DefaultStyleKey = typeof(ToolControl);
            Loaded += ToolControl_Loaded;
            Unloaded += ToolControl_Unloaded;
        }

        private void ToolControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ToolDock toolDock)
            {
                if (_activeDockableContentToken != 0)
                    toolDock.UnregisterPropertyChangedCallback(ToolDock.ActiveDockableProperty, _activeDockableContentToken);
            }
        }

        private void ToolControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += ToolControl_DataContextChanged;

            // Closed-loop guard, outer half: makes sure the inner
            // ToolContentControl really materialized from the ContentTemplate.
            // An expansion aborted mid-flight (cross-window redock) is not
            // retried by any event, which is what leaves a pane hollow.
            LayoutUpdated -= ToolControl_LayoutUpdated;
            LayoutUpdated += ToolControl_LayoutUpdated;
        }

        private void ToolControl_LayoutUpdated(object sender, object e)
        {
            EnsureContentHostBound();
        }

        private void EnsureContentHostBound()
        {
            if (_hostSuspended || _toolContentControl is null || DataContext is not ToolDock toolDock)
            {
                return;
            }

            var active = toolDock.ActiveDockable;
            if (active is null)
            {
                // The null transition IS the release order: merely returning here
                // keeps the stale DataContext, and this host then fights the
                // tool's new host for the shared element, one steal per frame.
                if (_toolContentControl.Content is not null || _toolContentControl.DataContext is not null)
                {
                    Internal.DockDiag.Log(
                        $"ToolControl releasing content host on {Internal.DockDiag.Describe(this)} (ActiveDockable is null)");
                    _toolContentControl.Content = null;
                    _toolContentControl.DataContext = null;
                    _innerContent = null;
                    _hostAttempts = 0;
                }

                return;
            }

            var bound = ReferenceEquals(_toolContentControl.DataContext, active)
                        && ReferenceEquals(_toolContentControl.Content, active);

            if (bound && _innerContent is { IsLoaded: true })
            {
                _hostAttempts = 0;
                return;
            }

            _innerContent = _toolContentControl.FindDescendant<ToolContentControl>();
            if (bound && _innerContent is not null)
            {
                _hostAttempts = 0;
                return;
            }

            if (!ToolContentControl.IsEffectivelyVisible(this))
            {
                return;
            }

            if (++_hostAttempts > HostAttemptLimit)
            {
                _hostSuspended = true;
                Internal.DockDiag.Log(
                    $"ToolControl watchdog SUSPENDED on {Internal.DockDiag.Describe(this)} after {HostAttemptLimit} consecutive rebuilds");
                return;
            }

            Internal.DockDiag.Log(
                $"ToolControl watchdog REBUILD content host on {Internal.DockDiag.Describe(this)} (bound={bound}, inner={_innerContent is not null}) active='{active.Title}'");

            // Re-assigning Content forces the ContentTemplate to instantiate a
            // fresh inner chain.
            _toolContentControl.Content = null;
            _toolContentControl.DataContext = active;
            _toolContentControl.Content = active;
            _innerContent = null;
        }

        private void ToolControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            _hostAttempts = 0;
            _hostSuspended = false;
            _innerContent = null;
            BindData();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _toolTabStrip = GetTemplateChild(TabStripName) as ToolTabStrip;
            _toolContentControl = GetTemplateChild(ToolContentControlName) as ContentControl;

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext is ToolDock toolDock)
            {
                _toolTabStrip.ClearValue(ToolTabStrip.DataContextProperty);
                _toolTabStrip.SetBinding(ToolTabStrip.DataContextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath(""),
                    Mode = BindingMode.OneWay
                });

                // If you use SetBinding, there will be a conversion error. I don't know why...
                UpdateSelectedItem();
                UpdateToolContentControl();
                if (_activeDockableContentToken != 0)
                    toolDock.UnregisterPropertyChangedCallback(ToolDock.ActiveDockableProperty, _activeDockableContentToken);
                _activeDockableContentToken = toolDock.RegisterPropertyChangedCallback(ToolDock.ActiveDockableProperty, ActiveDockableChangedCallback);
            }
        }

        private void ActiveDockableChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if (dp == ToolDock.ActiveDockableProperty)
            {
                UpdateSelectedItem();
                UpdateToolContentControl();
            }
        }

        private void UpdateSelectedItem()
        {
            if (DataContext is ToolDock toolDock)
            {
                _toolTabStrip.SelectedItem = toolDock.ActiveDockable;
            }
        }

        private void UpdateToolContentControl()
        {
            if (DataContext is ToolDock toolDock)
            {
                // To reuse child controls
                if (toolDock.ActiveDockable is Tool tool)
                {
                    var contentElem = tool.Content as UIElement;
                    if (contentElem != null)
                    {
                        var parent = VisualTreeHelper.GetParent(contentElem) as UIElement;
                        if (parent is ContentPresenter presenter)
                        {
                            presenter.Content = null;
                        }
                    }
                }

                // Setting Content (not just DataContext) forces the
                // ContentTemplate to re-instantiate a fresh inner
                // ToolContentControl chain. The old always-null-Content "reuse"
                // scheme had no rebuild path: when the transient cross-window
                // layout race aborted the one-shot template expansion, the pane
                // stayed hollow forever (no later activation could repair it).
                _toolContentControl.Content = null;
                _toolContentControl.DataContext = toolDock.ActiveDockable;
                _toolContentControl.Content = toolDock.ActiveDockable;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = base.MeasureOverride(availableSize);
            return size;
        }

        private const int HostAttemptLimit = 10;

        private ToolTabStrip _toolTabStrip;
        private ContentControl _toolContentControl;
        private ToolContentControl _innerContent;
        private int _hostAttempts;
        private bool _hostSuspended;
        private long _activeDockableContentToken = 0;
    }
}

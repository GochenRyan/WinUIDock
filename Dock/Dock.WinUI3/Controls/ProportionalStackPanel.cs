using Dock.Model.WinUI3.Controls;
using Dock.Model.WinUI3.Core;
using Dock.WinUI3.Converters;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public sealed class ProportionalStackPanel : Panel
    {
        public ProportionalStackPanel() : base()
        {
            DataContextChanged += ProportionalStackPanel_DataContextChanged;
        }

        private void ProportionalStackPanel_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext is ProportionalDock)
            {
                ClearValue(OrientationProperty);
                SetBinding(OrientationProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("Orientation"),
                    Converter = new OrientationConverter(),
                    Mode = BindingMode.OneWay
                });

                ClearValue(IsCollapsableProperty);
                SetBinding(IsCollapsableProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("IsCollapsable"),
                    Mode = BindingMode.OneWay
                });

                ClearValue(IsEmptyProperty);
                SetBinding(IsEmptyProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("IsEmpty"),
                    Mode = BindingMode.OneWay
                });
            }
        }

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(ProportionalStackPanel),
            new PropertyMetadata(Orientation.Vertical, OnOrientationChanged));

        public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

        public static readonly DependencyProperty IsCollapsableProperty = DependencyProperty.Register(
            nameof(IsCollapsable),
            typeof(bool),
            typeof(ProportionalStackPanel),
            new PropertyMetadata(false, OnIsCollapsableChanged));

        public bool IsCollapsable
        {
            get => (bool)GetValue(IsCollapsableProperty);
            set => SetValue(IsCollapsableProperty, value);
        }
        private static void OnIsCollapsableChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            var control = ob as ProportionalStackPanel;

            control.Visibility = (control.IsCollapsable && control.IsEmpty) ? Visibility.Collapsed : Visibility.Visible;
        }

        public static readonly DependencyProperty IsEmptyProperty = DependencyProperty.Register(
            nameof(IsEmpty),
            typeof(bool),
            typeof(ProportionalStackPanel),
            new PropertyMetadata(false, OnIsEmptyChanged));

        public bool IsEmpty
        {
            get => (bool)GetValue(IsEmptyProperty);
            set => SetValue(IsEmptyProperty, value);
        }
        private static void OnIsEmptyChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            var control = ob as ProportionalStackPanel;

            control.Visibility = (control.IsCollapsable && control.IsEmpty) ? Visibility.Collapsed : Visibility.Visible;
        }


        public static double GetProportion(UIElement obj)
        {
            if (obj is ContentPresenter presenter)
            {
                if (presenter.Content != null && presenter.Content is DockBase dock)
                {
                    return dock.Proportion;
                }
            }
            else if (obj is Control control)
            {
                if (control.DataContext != null && control.DataContext is DockBase dock)
                {
                    return dock.Proportion;
                }
            }

            return double.NaN;
        }

        public static void SetProportion(UIElement obj, double value)
        {
            if (obj is ContentPresenter presenter)
            {
                if (presenter.Content != null && presenter.Content is DockBase dock)
                {
                    dock.Proportion = value;
                }
            }
            else if (obj is Control control)
            {
                if (control.DataContext != null && control.DataContext is DockBase dock)
                {
                    dock.Proportion = value;
                }
            }
        }

        public static bool GetIsCollapsed(UIElement obj)
        {
            if (obj is ContentPresenter presenter)
            {
                if (presenter.Content != null && presenter.Content is DockBase dock)
                {
                    if (dock.IsCollapsable && dock.IsEmpty)
                    {
                        return true;
                    }
                }
            }
            else if (obj is Control control)
            {
                if (control.DataContext != null && control.DataContext is DockBase dock)
                {
                    if (dock.IsCollapsable && dock.IsEmpty)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void SetIsCollapsed(UIElement obj, bool value)
        {
            if (obj is ContentPresenter presenter)
            {
                if (presenter.Content != null && presenter.Content is DockBase dock)
                {
                    dock.IsCollapsable = value;
                }
            }
            else if (obj is Control control)
            {
                if (control.DataContext != null && control.DataContext is DockBase dock)
                {
                    dock.IsCollapsable = value;
                }
            }
        }

        public static double GetThickness(UIElement obj)
        {
            if (obj is ContentPresenter presenter)
            {
                if (presenter.Content != null && presenter.Content is ProportionalDockSplitter proportionalDockSplitter)
                {
                    return ResolveSplitterThickness(proportionalDockSplitter.Thickness);
                }
            }
            else if (obj is Control control)
            {
                if (control.DataContext != null && control.DataContext is ProportionalDockSplitter proportionalDockSplitter)
                {
                    return ResolveSplitterThickness(proportionalDockSplitter.Thickness);
                }
            }

            return double.NaN;
        }

        // Model thickness 0 means "not explicitly set" — fall back to the
        // DockSplitterThickness theme metric (host-overridable).
        internal static double ResolveSplitterThickness(double modelValue)
        {
            return modelValue > 0 ? modelValue : DockMetrics.GetDouble("DockSplitterThickness", 4.0);
        }

        private static void OnOrientationChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            if (ob is ProportionalStackPanel panel)
                panel.InvalidateMeasure();
        }

        private void AssignProportions(UIElementCollection children)
        {
            var assignedProportion = 0.0;
            var unassignedProportions = 0;

            for (var i = 0; i < children.Count; i++)
            {
                var element = children[i];
                var isCollapsed = GetIsCollapsed(element);
                var isSplitter = ProportionalStackPanelSplitter.IsSplitter(element);

                if (!isSplitter && !isCollapsed)
                {
                    var proportion = GetProportion(element);

                    if (double.IsNaN(proportion))
                    {
                        unassignedProportions++;
                    }
                    else
                    {
                        assignedProportion += proportion;
                    }
                }
            }

            if (unassignedProportions > 0)
            {
                // First-time assignment: give each not-yet-proportioned child an
                // equal share of whatever is left. This is the ONLY place a
                // proportion is written back to the model.
                var toAssign = Math.Max(0.0, 1.0 - assignedProportion) / unassignedProportions;
                foreach (var element in children.Where(c =>
                {
                    var isCollapsed = GetIsCollapsed(c);
                    return !isCollapsed && double.IsNaN(GetProportion(c));
                }))
                {
                    if (!ProportionalStackPanelSplitter.IsSplitter(element))
                    {
                        SetProportion(element, toAssign);
                    }
                }
            }

            // Deliberately NO rebalancing writes when the total is not 1 (e.g. a
            // pane is collapsed by auto-hide, or was floated out): stored
            // proportions stay untouched and Measure/Arrange normalize at use
            // time. The previous persistent rebalancing permanently shrank a pane
            // on every hide/restore round-trip.
        }

        // Sum of the stored proportions currently participating in layout; used to
        // normalize shares at measure/arrange time without mutating the model.
        private double GetTotalProportion(UIElementCollection children)
        {
            var total = 0.0;
            for (var i = 0; i < children.Count; i++)
            {
                var c = children[i];
                if (!ProportionalStackPanelSplitter.IsSplitter(c) && !GetIsCollapsed(c))
                {
                    var proportion = GetProportion(c);
                    if (!double.IsNaN(proportion))
                    {
                        total += proportion;
                    }
                }
            }

            return total;
        }

        private static double Normalize(double proportion, double totalProportion)
        {
            return totalProportion > 0 ? proportion / totalProportion : 0.0;
        }

        private double GetTotalSplitterThickness(UIElementCollection children)
        {
            var previousisCollapsed = false;
            var totalThickness = 0.0;

            for (var i = 0; i < children.Count; i++)
            {
                var c = children[i];
                var isSplitter = ProportionalStackPanelSplitter.IsSplitter(c);

                if (isSplitter)
                {
                    if (previousisCollapsed)
                    {
                        previousisCollapsed = false;
                        continue;
                    }

                    if (i + 1 < Children.Count)
                    {
                        var nextControl = Children[i + 1];
                        var nextisCollapsed = GetIsCollapsed(nextControl);
                        if (nextisCollapsed)
                        {
                            continue;
                        }
                    }

                    var thickness = GetThickness(c);
                    totalThickness += thickness;
                }
                else
                {
                    previousisCollapsed = GetIsCollapsed(c);
                }
            }

            return double.IsNaN(totalThickness) ? 0 : totalThickness;
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            try
            {
                return MeasureCore(availableSize);
            }
            catch (Exception ex) when (Internal.DockDiag.IsTransientLayoutError(ex))
            {
                // Transient close-vs-layout race during cross-window redock —
                // self-heal (see ToolChromeControl.MeasureOverride).
                Internal.DockDiag.Log($"ProportionalStackPanel.MeasureOverride transient failure: {ex.Message} — retrying inline");
                try
                {
                    return MeasureCore(availableSize);
                }
                catch (Exception retryEx) when (Internal.DockDiag.IsTransientLayoutError(retryEx))
                {
                    Internal.DockDiag.Log("ProportionalStackPanel.MeasureOverride retry also failed — deferring");
                    DispatcherQueue?.TryEnqueue(InvalidateMeasure);
                    return DesiredSize;
                }
            }
        }

        private Size MeasureCore(Size availableSize)
        {
            var horizontal = Orientation == Orientation.Horizontal;
            if ((horizontal && double.IsInfinity(availableSize.Width))
                || (!horizontal && double.IsInfinity(availableSize.Height)))
            {
                throw new Exception("Proportional StackPanel cannot be inside a control that offers infinite space.");
            }

            //GeneratedAllControls(Children);

            var usedWidth = 0.0;
            var usedHeight = 0.0;
            var maximumWidth = 0.0;
            var maximumHeight = 0.0;
            var splitterThickness = GetTotalSplitterThickness(Children);

            AssignProportions(Children);
            var totalProportion = GetTotalProportion(Children);

            var needsNextSplitter = false;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                var isSplitter = ProportionalStackPanelSplitter.IsSplitter(child);

                var proportion = GetProportion(child);

                var isCollapsed = !isSplitter && GetIsCollapsed(child);
                if (isCollapsed)
                {
                    var size = new Size();
                    child.Measure(size);
                    continue;
                }

                if (!isSplitter)
                {
                    Debug.Assert(!double.IsNaN(proportion));

                    switch (Orientation)
                    {
                        case Orientation.Horizontal:
                            {
                                var width = Math.Max(0, (availableSize.Width - splitterThickness) * Normalize(proportion, totalProportion));
                                var size = new Size(width, availableSize.Height);
                                child.Measure(size);
                                break;
                            }
                        case Orientation.Vertical:
                            {
                                var height = Math.Max(0, (availableSize.Height - splitterThickness) * Normalize(proportion, totalProportion));
                                var size = new Size(availableSize.Width, height);
                                child.Measure(size);
                                break;
                            }
                    }

                    needsNextSplitter = true;
                }
                else
                {
                    if (!needsNextSplitter)
                    {
                        var size = new Size();
                        child.Measure(size);
                        continue;
                    }

                    switch (Orientation)
                    {
                        case Orientation.Horizontal:
                            {
                                var size = new Size(GetThickness(child), availableSize.Height);
                                child.Measure(size);
                                break;
                            }
                        case Orientation.Vertical:
                            {
                                var size = new Size(availableSize.Width, GetThickness(child));
                                child.Measure(size);
                                break;
                            }
                    }
                    needsNextSplitter = false;
                }

                var desiredSize = child.DesiredSize;

                // Decrease the remaining space for the rest of the children
                switch (Orientation)
                {
                    case Orientation.Horizontal:
                        {
                            maximumHeight = Math.Max(maximumHeight, usedHeight + desiredSize.Height);

                            if (isSplitter)
                            {
                                usedWidth += desiredSize.Width;
                            }
                            else
                            {
                                usedWidth += Math.Max(0, (availableSize.Width - splitterThickness) * Normalize(proportion, totalProportion));
                            }

                            break;
                        }
                    case Orientation.Vertical:
                        {
                            maximumWidth = Math.Max(maximumWidth, usedWidth + desiredSize.Width);

                            if (isSplitter)
                            {
                                usedHeight += desiredSize.Height;
                            }
                            else
                            {
                                usedHeight += Math.Max(0, (availableSize.Height - splitterThickness) * Normalize(proportion, totalProportion));
                            }

                            break;
                        }
                }
            }

            maximumWidth = Math.Max(maximumWidth, usedWidth);
            maximumHeight = Math.Max(maximumHeight, usedHeight);

            return new Size(maximumWidth, maximumHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            try
            {
                return ArrangeCore(finalSize);
            }
            catch (Exception ex) when (Internal.DockDiag.IsTransientLayoutError(ex))
            {
                Internal.DockDiag.Log($"ProportionalStackPanel.ArrangeOverride transient failure: {ex.Message} — retrying inline");
                try
                {
                    return ArrangeCore(finalSize);
                }
                catch (Exception retryEx) when (Internal.DockDiag.IsTransientLayoutError(retryEx))
                {
                    Internal.DockDiag.Log("ProportionalStackPanel.ArrangeOverride retry also failed — deferring");
                    DispatcherQueue?.TryEnqueue(InvalidateArrange);
                    return finalSize;
                }
            }
        }

        private Size ArrangeCore(Size finalSize)
        {
            var left = 0.0;
            var top = 0.0;
            var right = 0.0;
            var bottom = 0.0;

            // Arrange each of the Children
            var splitterThickness = GetTotalSplitterThickness(Children);
            var index = 0;

            AssignProportions(Children);
            var totalProportion = GetTotalProportion(Children);

            var needsNextSplitter = false;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];

                var isSplitter = ProportionalStackPanelSplitter.IsSplitter(child);

                var isCollapsed = !isSplitter && GetIsCollapsed(child);
                if (isCollapsed)
                {
                    var rect = new Rect();
                    child.Arrange(rect);
                    index++;
                    continue;
                }

                if (!isSplitter)
                    needsNextSplitter = true;
                else if (isSplitter && !needsNextSplitter)
                {
                    var rect = new Rect();
                    child.Arrange(rect);
                    index++;
                    needsNextSplitter = false;
                    continue;
                }

                // Determine the remaining space left to arrange the element
                var remainingRect = new Rect(
                    left,
                    top,
                    Math.Max(0.0, finalSize.Width - left - right),
                    Math.Max(0.0, finalSize.Height - top - bottom));

                // Trim the remaining Rect to the docked size of the element
                // (unless the element should fill the remaining space because
                // of LastChildFill)
                if (index < Children.Count)
                {
                    var desiredSize = child.DesiredSize;
                    var proportion = GetProportion(child);

                    switch (Orientation)
                    {
                        case Orientation.Horizontal:
                            {
                                if (isSplitter)
                                {
                                    left += desiredSize.Width;
                                    remainingRect = new Rect(remainingRect.X, remainingRect.Y, desiredSize.Width, remainingRect.Height);
                                }
                                else
                                {
                                    Debug.Assert(!double.IsNaN(proportion));
                                    var width = Math.Max(0, (finalSize.Width - splitterThickness) * Normalize(proportion, totalProportion));
                                    remainingRect = new Rect(remainingRect.X, remainingRect.Y, width, remainingRect.Height);
                                    left += width;
                                }

                                break;
                            }
                        case Orientation.Vertical:
                            {
                                if (isSplitter)
                                {
                                    top += desiredSize.Height;
                                    remainingRect = new Rect(remainingRect.X, remainingRect.Y, remainingRect.Width, desiredSize.Height);
                                }
                                else
                                {
                                    Debug.Assert(!double.IsNaN(proportion));
                                    var height = Math.Max(0, (finalSize.Height - splitterThickness) * Normalize(proportion, totalProportion));
                                    remainingRect = new Rect(remainingRect.X, remainingRect.Y, remainingRect.Width, height);
                                    top += height;
                                }

                                break;
                            }
                    }
                }

                child.Arrange(remainingRect);
                index++;
            }

            return finalSize;
        }
    }
}

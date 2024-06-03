using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public sealed class ProportionalStackPanel : Panel
    {
        public static DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(ProportionalStackPanel),
            new PropertyMetadata(Orientation.Vertical, OnOrientationChanged));

        public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

        public static readonly DependencyProperty ProportionProperty =
            DependencyProperty.RegisterAttached("Proportion", typeof(double), typeof(ProportionalStackPanel), new PropertyMetadata(double.NaN));

        public static double GetProportion(UIElement obj)
        {
            return (double)obj.GetValue(ProportionProperty);
        }

        public static void SetProportion(UIElement obj, double value)
        {
            obj.SetValue(ProportionProperty, value);
        }

        public static readonly DependencyProperty IsCollapsedProperty =
            DependencyProperty.RegisterAttached("IsCollapsed", typeof(bool), typeof(ProportionalStackPanel), new PropertyMetadata(false));

        public static bool GetIsCollapsed(UIElement obj)
        {
            return (bool)obj.GetValue(IsCollapsedProperty);
        }

        public static void SetIsCollapsed(UIElement obj, bool value)
        {
            obj.SetValue(IsCollapsedProperty, value);
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
                var isSplitter = ProportionalStackPanelSplitter.IsSplitter(element, out _);

                if (!isSplitter)
                {
                    var proportion = GetProportion(element);

                    if (isCollapsed)
                    {
                        proportion = 0.0;
                    }

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
                var toAssign = assignedProportion;
                foreach (var element in children.Where(c =>
                {
                    var isCollapsed = GetIsCollapsed(c);
                    return !isCollapsed && double.IsNaN(GetProportion(c));
                }))
                {
                    if (!ProportionalStackPanelSplitter.IsSplitter(element, out _))
                    {
                        var proportion = (1.0 - toAssign) / unassignedProportions;
                        SetProportion(element, proportion);
                        assignedProportion += (1.0 - toAssign) / unassignedProportions;
                    }
                }
            }

            if (assignedProportion < 1)
            {
                var numChildren = (double)children.Count(c => !ProportionalStackPanelSplitter.IsSplitter(c, out _));

                var toAdd = (1.0 - assignedProportion) / numChildren;

                foreach (var child in children.Where(c =>
                {
                    var isCollapsed = GetIsCollapsed(c);
                    return !isCollapsed && !ProportionalStackPanelSplitter.IsSplitter(c, out _);
                }))
                {
                    var proportion = GetProportion(child) + toAdd;
                    SetProportion(child, proportion);
                }
            }
            else if (assignedProportion > 1)
            {
                var numChildren = (double)children.Count(c => !ProportionalStackPanelSplitter.IsSplitter(c, out _));

                var toRemove = (assignedProportion - 1.0) / numChildren;

                foreach (var child in children.Where(c =>
                {
                    var isCollapsed = GetIsCollapsed(c);
                    return !isCollapsed && !ProportionalStackPanelSplitter.IsSplitter(c, out _);
                }))
                {
                    var proportion = GetProportion(child) - toRemove;
                    SetProportion(child, proportion);
                }
            }
        }

        private double GetTotalSplitterThickness(UIElementCollection children)
        {
            var previousisCollapsed = false;
            var totalThickness = 0.0;

            for (var i = 0; i < children.Count; i++)
            {
                var c = children[i];
                var isSplitter = ProportionalStackPanelSplitter.IsSplitter(c, out var proportionalStackPanelSplitter);

                if (isSplitter && proportionalStackPanelSplitter is not null)
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

                    var thickness = proportionalStackPanelSplitter.Thickness;
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
            var horizontal = Orientation == Orientation.Horizontal;
            if ((horizontal && double.IsInfinity(availableSize.Width))
                || (!horizontal && double.IsInfinity(availableSize.Height)))
            {
                throw new Exception("Proportional StackPanel cannot be inside a control that offers infinite space.");
            }

            bool bLoaded = true;
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (VisualTreeHelper.GetChildrenCount(child) == 0)
                {
                    child.Measure(new Size(0.0, 0.0));
                    bLoaded = false;
                }
            }

            if (!bLoaded)
            {
                return new Size(0.0, 0.0);
            }

            var usedWidth = 0.0;
            var usedHeight = 0.0;
            var maximumWidth = 0.0;
            var maximumHeight = 0.0;
            var splitterThickness = GetTotalSplitterThickness(Children);

            AssignProportions(Children);

            var needsNextSplitter = false;




            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                var isSplitter = ProportionalStackPanelSplitter.IsSplitter(child, out _);

                // Get the child's desired size
                var remainingSize = new Size(
                    Math.Max(0.0, availableSize.Width - usedWidth - splitterThickness),
                    Math.Max(0.0, availableSize.Height - usedHeight - splitterThickness));

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
                                var width = Math.Max(0, (availableSize.Width - splitterThickness) * proportion);
                                var size = new Size(width, availableSize.Height);
                                child.Measure(size);
                                break;
                            }
                        case Orientation.Vertical:
                            {
                                var height = Math.Max(0, (availableSize.Height - splitterThickness) * proportion);
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

                    child.Measure(remainingSize);
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
                                usedWidth += Math.Max(0, (availableSize.Width - splitterThickness) * proportion);
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
                                usedHeight += Math.Max(0, (availableSize.Height - splitterThickness) * proportion);
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
            var left = 0.0;
            var top = 0.0;
            var right = 0.0;
            var bottom = 0.0;

            // Arrange each of the Children
            var splitterThickness = GetTotalSplitterThickness(Children);
            var index = 0;

            AssignProportions(Children);

            var needsNextSplitter = false;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];

                var isSplitter = ProportionalStackPanelSplitter.IsSplitter(child, out _);

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
                                    left += finalSize.Width;
                                    remainingRect = new Rect(remainingRect.X, remainingRect.Y, desiredSize.Width, remainingRect.Height);
                                }
                                else
                                {
                                    Debug.Assert(!double.IsNaN(proportion));
                                    var width = Math.Max(0, (finalSize.Width - splitterThickness) * proportion);
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
                                    var height = Math.Max(0, (finalSize.Height - splitterThickness) * proportion);
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

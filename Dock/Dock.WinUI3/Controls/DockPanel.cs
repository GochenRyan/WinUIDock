using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    /// <summary>
    /// Defines an area where you can arrange child elements either horizontally or vertically, relative to each other.
    /// </summary>
    public partial class DockPanel : Panel
    {
        private static void DockChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var senderElement = sender as FrameworkElement;
            var dockPanel = senderElement?.FindParent<DockPanel>();

            dockPanel?.InvalidateArrange();
        }

        private static void LastChildFillChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var dockPanel = (DockPanel)sender;
            dockPanel.InvalidateArrange();
        }

        private static void OnPaddingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var dockPanel = (DockPanel)sender;
            dockPanel.InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count == 0)
            {
                return finalSize;
            }

            var currentBounds = new Rect(Padding.Left, Padding.Top, finalSize.Width - Padding.Right, finalSize.Height - Padding.Bottom);
            var childrenCount = LastChildFill ? Children.Count - 1 : Children.Count;

            for (var index = 0; index < childrenCount; index++)
            {
                var child = Children[index];
                var dock = (Dock)child.GetValue(DockProperty);
                double width, height;
                switch (dock)
                {
                    case Dock.Left:

                        width = Math.Min(child.DesiredSize.Width, GetPositiveOrZero(currentBounds.Width - currentBounds.X));
                        child.Arrange(new Rect(currentBounds.X, currentBounds.Y, width, GetPositiveOrZero(currentBounds.Height - currentBounds.Y)));
                        currentBounds.X += width;

                        break;
                    case Dock.Top:

                        height = Math.Min(child.DesiredSize.Height, GetPositiveOrZero(currentBounds.Height - currentBounds.Y));
                        child.Arrange(new Rect(currentBounds.X, currentBounds.Y, GetPositiveOrZero(currentBounds.Width - currentBounds.X), height));
                        currentBounds.Y += height;

                        break;
                    case Dock.Right:

                        width = Math.Min(child.DesiredSize.Width, GetPositiveOrZero(currentBounds.Width - currentBounds.X));
                        child.Arrange(new Rect(GetPositiveOrZero(currentBounds.Width - width), currentBounds.Y, width, GetPositiveOrZero(currentBounds.Height - currentBounds.Y)));
                        currentBounds.Width -= (currentBounds.Width - width) > 0 ? width : 0;

                        break;
                    case Dock.Bottom:

                        height = Math.Min(child.DesiredSize.Height, GetPositiveOrZero(currentBounds.Height - currentBounds.Y));
                        child.Arrange(new Rect(currentBounds.X, GetPositiveOrZero(currentBounds.Height - height), GetPositiveOrZero(currentBounds.Width - currentBounds.X), height));
                        currentBounds.Height -= (currentBounds.Height - height) > 0 ? height : 0;

                        break;
                }
            }

            if (LastChildFill)
            {
                var width = GetPositiveOrZero(currentBounds.Width - currentBounds.X);
                var height = GetPositiveOrZero(currentBounds.Height - currentBounds.Y);
                var child = Children[Children.Count - 1];
                child.Arrange(
                    new Rect(currentBounds.X, currentBounds.Y, width, height));
            }

            return finalSize;
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            var parentWidth = 0.0;
            var parentHeight = 0.0;
            var accumulatedWidth = Padding.Left + Padding.Right;
            var accumulatedHeight = Padding.Top + Padding.Bottom;

            foreach (var child in Children)
            {
                var childConstraint = new Size(
                    GetPositiveOrZero(availableSize.Width - accumulatedWidth),
                    GetPositiveOrZero(availableSize.Height - accumulatedHeight));

                child.Measure(childConstraint);
                var childDesiredSize = child.DesiredSize;

                switch ((Dock)child.GetValue(DockProperty))
                {
                    case Dock.Left:
                    case Dock.Right:
                        parentHeight = Math.Max(parentHeight, accumulatedHeight + childDesiredSize.Height);
                        accumulatedWidth += childDesiredSize.Width;
                        break;

                    case Dock.Top:
                    case Dock.Bottom:
                        parentWidth = Math.Max(parentWidth, accumulatedWidth + childDesiredSize.Width);
                        accumulatedHeight += childDesiredSize.Height;
                        break;
                }
            }

            parentWidth = Math.Max(parentWidth, accumulatedWidth);
            parentHeight = Math.Max(parentHeight, accumulatedHeight);
            return new Size(parentWidth, parentHeight);
        }

        private static double GetPositiveOrZero(double value)
        {
            return Math.Max(value, 0);
        }
    }
}

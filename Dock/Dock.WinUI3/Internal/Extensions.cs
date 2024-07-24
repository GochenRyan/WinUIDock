using Dock.Model.Core;
using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace Dock.WinUI3.Internal
{
    internal static class Extensions
    {
        public static IEnumerable<DockControl> GetZOrderedDockControls(this IList<IDockControl> dockControls)
        {
            return dockControls
                .OfType<DockControl>()
                .Reverse();
        }

        public static Point TransformPoint(UIElement from, Point point, UIElement to)
        {
            if (from.XamlRoot != to.XamlRoot)
            {
                // Get the window containing the 'from' element
                var fromWindow = HostWindow.GetWindowForElement(from);
                if (fromWindow == null)
                {
                    throw new InvalidOperationException("Cannot find window for 'from' element.");
                }

                // Calculate the position relative to the window content
                GeneralTransform t1 = from.TransformToVisual(fromWindow.Content);
                Point fromWindowPoint = t1.TransformPoint(point);

                // Adjust for rasterization scale
                double fromScaleAdjustment = from.XamlRoot.RasterizationScale;
                var fromLeft = fromWindowPoint.X * fromScaleAdjustment + fromWindow.AppWindow.Position.X;
                var fromTop = fromWindowPoint.Y * fromScaleAdjustment + fromWindow.AppWindow.Position.Y;

                // Adjust Y coordinate if the window extends content into title bar
                if (fromWindow.ExtendsContentIntoTitleBar)
                {
                    fromTop -= GetTitleBarHeight(fromWindow);
                }

                // Get the window containing the 'to' element
                var toWindow = HostWindow.GetWindowForElement(to);
                if (toWindow == null)
                {
                    throw new InvalidOperationException("Cannot find window for 'to' element.");
                }

                double toScaleAdjustment = to.XamlRoot.RasterizationScale;
                Point toWindowPoint = new()
                {
                    X = (fromLeft - toWindow.AppWindow.Position.X) / toScaleAdjustment,
                    Y = (fromTop - toWindow.AppWindow.Position.Y) / toScaleAdjustment
                };

                // Adjust Y coordinate if the target window extends content into title bar
                if (toWindow.ExtendsContentIntoTitleBar)
                {
                    toWindowPoint.Y += GetTitleBarHeight(toWindow);
                }

                // Calculate the position relative to the 'to' element
                GeneralTransform t2 = toWindow.Content.TransformToVisual(to);
                Point toPoint = t2.TransformPoint(toWindowPoint);

                return toPoint;
            }

            // For elements within the same XamlRoot
            GeneralTransform transform = from.TransformToVisual(to);
            var relativePoint = transform.TransformPoint(point);
            return relativePoint;
        }

        private static double GetTitleBarHeight(Window window)
        {
            // Get the height of the title bar; adjust as necessary
            var appTitleBar = window.AppWindow.TitleBar;
            return appTitleBar.Height;
        }

        public static Point GetScreenPoint(UIElement element, Point point)
        {
            var fromWindow = HostWindow.GetWindowForElement(element);
            GeneralTransform t1 = element.TransformToVisual(fromWindow.Content);
            Point fromWindowPoint = t1.TransformPoint(point);
            double fromScaleAdjustment = element.XamlRoot.RasterizationScale;
            var fromLeft = fromWindowPoint.X * fromScaleAdjustment + fromWindow.AppWindow.Position.X;
            var fromTop = fromWindowPoint.Y * fromScaleAdjustment + fromWindow.AppWindow.Position.Y;

            return new Point(fromLeft, fromTop);
        }

        public static Size GetScreenSize(UIElement element, Size size)
        {
            double scaleAdjustment = element.XamlRoot.RasterizationScale;

            var window = HostWindow.GetWindowForElement(element);
            GeneralTransform transform = element.TransformToVisual(window.Content);
            Rect bounds = transform.TransformBounds(new Rect(0, 0, size.Width, size.Height));

            return new Size(bounds.Width * scaleAdjustment, bounds.Height * scaleAdjustment);
        }

        public static Rect GetScreenBounds(UIElement element, double x, double y, double width, double height)
        {
            double scaleAdjustment = element.XamlRoot.RasterizationScale;
            var window = HostWindow.GetWindowForElement(element);
            GeneralTransform transform = element.TransformToVisual(window.Content);
            Rect bounds = transform.TransformBounds(new Rect(x, y, width, height));
            Rect screenBounds = new(bounds.X * scaleAdjustment,
                bounds.Y * scaleAdjustment,
                bounds.Width * scaleAdjustment,
                bounds.Height * scaleAdjustment);

            return screenBounds;
        }

        public static Rect GetScreenBounds(UIElement element, Rect rect)
        {
            double scaleAdjustment = element.XamlRoot.RasterizationScale;
            var window = HostWindow.GetWindowForElement(element);
            GeneralTransform transform = element.TransformToVisual(window.Content);
            Rect bounds = transform.TransformBounds(rect);
            Rect screenBounds = new(bounds.X * scaleAdjustment,
                bounds.Y * scaleAdjustment,
                bounds.Width * scaleAdjustment,
                bounds.Height * scaleAdjustment);

            return screenBounds;
        }

        public static Size GetInfinitySize(UIElement element, Size availableSize)
        {
            var width = availableSize.Width;
            var height = availableSize.Height;

            if (double.IsInfinity(width))
            {
                var parent = VisualTreeHelper.GetParent(element) as UIElement;
                while (parent != null && parent != element.XamlRoot.Content)
                {
                    if (!double.IsInfinity(parent.DesiredSize.Width))
                    {
                        width = parent.DesiredSize.Width;
                        break;
                    }
                }

                if (double.IsInfinity(width))
                {
                    if (parent == element.XamlRoot.Content)
                    {
                        if (element.XamlRoot != null)
                        {
                            width = element.XamlRoot.Size.Width;
                        }
                        else
                        {
                            width = 0;
                        }
                    }
                    else
                    {
                        width = 0;
                    }
                }
            }

            if (double.IsInfinity(height))
            {
                var parent = VisualTreeHelper.GetParent(element) as UIElement;
                while (parent != null && parent != element.XamlRoot.Content)
                {
                    if (!double.IsInfinity(parent.DesiredSize.Height))
                    {
                        height = parent.DesiredSize.Height;
                        break;
                    }
                }

                if (double.IsInfinity(height))
                {
                    if (parent == element.XamlRoot.Content)
                    {
                        if (element.XamlRoot != null)
                        {
                            height = element.XamlRoot.Size.Height;
                        }
                        else
                        {
                            height = 0;
                        }
                    }
                    else
                    {
                        height = 0;
                    }
                }
            }

            var finalSize = new Size(width, height);

            return finalSize;
        }


    }
}

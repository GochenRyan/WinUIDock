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

        // TODO: test multiple monitors, different scaling
        public static Point TransformPoint(UIElement from, Point point, UIElement to)
        {
            if (from.XamlRoot != to.XamlRoot)
            {
                var fromWindow = HostWindow.GetWindowForElement(from);
                GeneralTransform t1 = from.TransformToVisual(null);
                Point fromWindowPoint = t1.TransformPoint(point);
                double fromScaleAdjustment = from.XamlRoot.RasterizationScale;
                var fromLeft = fromWindowPoint.X * fromScaleAdjustment + fromWindow.AppWindow.Position.X;
                var fromTop = fromWindowPoint.Y * fromScaleAdjustment + fromWindow.AppWindow.Position.Y;

                var toWindow = HostWindow.GetWindowForElement(to);
                double toScaleAdjustment = to.XamlRoot.RasterizationScale;
                Point toWindowPoint = new();
                toWindowPoint.X = (fromLeft - toWindow.AppWindow.Position.X) / toScaleAdjustment;
                toWindowPoint.Y = (fromTop - toWindow.AppWindow.Position.Y) / toScaleAdjustment;

                GeneralTransform t2 = toWindow.Content.TransformToVisual(to);
                Point toPoint = t2.TransformPoint(toWindowPoint);

                return toPoint;
            }

            GeneralTransform transform = from.TransformToVisual(to);
            var relativePoint = transform.TransformPoint(point);
            return relativePoint;
        }

        public static Point GetScreenPoint(UIElement element, Point point)
        {
            var fromWindow = HostWindow.GetWindowForElement(element);
            GeneralTransform t1 = element.TransformToVisual(null);
            Point fromWindowPoint = t1.TransformPoint(point);
            double fromScaleAdjustment = element.XamlRoot.RasterizationScale;
            var fromLeft = fromWindowPoint.X * fromScaleAdjustment + fromWindow.AppWindow.Position.X;
            var fromTop = fromWindowPoint.Y * fromScaleAdjustment + fromWindow.AppWindow.Position.Y;

            return new Point(fromLeft, fromTop);
        }

        public static Size GetScreenSize(UIElement element, Size size)
        {
            double scaleAdjustment = element.XamlRoot.RasterizationScale;

            GeneralTransform transform = element.TransformToVisual(null);
            Rect bounds = transform.TransformBounds(new Rect(0, 0, size.Width, size.Height));

            return new Size(bounds.Width * scaleAdjustment, bounds.Height * scaleAdjustment);
        }

        public static Rect GetScreenBounds(UIElement element, double x, double y, double width, double height)
        {
            double scaleAdjustment = element.XamlRoot.RasterizationScale;

            GeneralTransform transform = element.TransformToVisual(null);
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

            GeneralTransform transform = element.TransformToVisual(null);
            Rect bounds = transform.TransformBounds(rect);
            Rect screenBounds = new(bounds.X * scaleAdjustment,
                bounds.Y * scaleAdjustment,
                bounds.Width * scaleAdjustment,
                bounds.Height * scaleAdjustment);

            return screenBounds;
        }
    }
}

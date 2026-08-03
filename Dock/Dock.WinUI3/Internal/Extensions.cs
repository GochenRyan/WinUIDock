using Dock.Model.Core;
using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace Dock.WinUI3.Internal
{
    internal static class Extensions
    {
        /// <summary>
        /// Orders the dock controls so the one actually on top at
        /// <paramref name="screenPoint"/> is probed first.
        ///
        /// Registration order is NOT z-order — clicking a window raises it without
        /// touching the DockControls list — so the real answer has to come from the OS.
        /// </summary>
        public static IEnumerable<DockControl> GetZOrderedDockControls(this IList<IDockControl> dockControls, IntPtr topWindow)
        {
            var controls = dockControls.OfType<DockControl>().ToList();
            if (controls.Count <= 1)
            {
                return controls;
            }

            // Newest-first as the baseline: it is still the best guess for the
            // windows the pointer is NOT over, and the only thing available when the
            // point falls outside our process entirely.
            controls.Reverse();

            if (topWindow == IntPtr.Zero)
            {
                return controls;
            }

            var index = controls.FindIndex(control => GetWindowHandle(control) == topWindow);
            if (index <= 0)
            {
                return controls;
            }

            var top = controls[index];
            controls.RemoveAt(index);
            controls.Insert(0, top);
            return controls;
        }

        /// <summary>
        /// The top-level window of OURS that sits topmost at <paramref name="screenPoint"/>,
        /// or <see cref="IntPtr.Zero"/> when the point is over another process (or
        /// nothing at all).
        /// </summary>
        public static IntPtr GetOwnWindowAt(this IList<IDockControl> dockControls, Point screenPoint)
        {
            var topWindow = GetRootWindowFromPoint(screenPoint);
            if (topWindow == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            foreach (var control in dockControls.OfType<DockControl>())
            {
                if (GetWindowHandle(control) == topWindow)
                {
                    return topWindow;
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Raises a window in z-order WITHOUT activating it.
        ///
        /// Needed during a drag because the dock guides are drawn inside the window
        /// they belong to: while that window is occluded, most of its guides are
        /// simply invisible and the screen area they cover belongs to whatever is on
        /// top — the window is unreachable as a drop target no matter what the hit
        /// testing says.
        ///
        /// SWP_NOACTIVATE is the load-bearing flag. Activating would move focus off
        /// the window that captured the pointer and end the drag mid-gesture.
        /// </summary>
        public static void RaiseWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            try
            {
                SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            catch
            {
                // Window went away mid-drag — the next move re-resolves it.
            }
        }

        private static IntPtr GetRootWindowFromPoint(Point screenPoint)
        {
            if (double.IsNaN(screenPoint.X) || double.IsNaN(screenPoint.Y))
            {
                return IntPtr.Zero;
            }

            try
            {
                var hwnd = WindowFromPoint(new NativePoint
                {
                    X = (int)Math.Round(screenPoint.X),
                    Y = (int)Math.Round(screenPoint.Y)
                });

                // WindowFromPoint lands on the deepest child (the XAML island host),
                // so walk up to the top-level window the DockControl belongs to.
                return hwnd == IntPtr.Zero ? IntPtr.Zero : GetAncestor(hwnd, GA_ROOT);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static IntPtr GetWindowHandle(UIElement element)
        {
            try
            {
                var window = HostWindow.GetWindowForElement(element);
                return window is null ? IntPtr.Zero : WinRT.Interop.WindowNative.GetWindowHandle(window);
            }
            catch
            {
                // Closed window that has not been unregistered yet — every member of
                // one throws, and it cannot be the top window anyway.
                return IntPtr.Zero;
            }
        }

        private const uint GA_ROOT = 2;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private static readonly IntPtr HWND_TOP = IntPtr.Zero;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(NativePoint point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClientToScreen(IntPtr hWnd, ref NativePoint point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        public static Point TransformPoint(UIElement from, Point point, UIElement to)
        {
            if (from.XamlRoot != to.XamlRoot)
            {
                var fromWindow = HostWindow.GetWindowForElement(from);
                if (fromWindow == null)
                {
                    throw new InvalidOperationException("Cannot find window for 'from' element.");
                }

                var toWindow = HostWindow.GetWindowForElement(to);
                if (toWindow == null)
                {
                    throw new InvalidOperationException("Cannot find window for 'to' element.");
                }

                // Content-relative -> screen -> content-relative on the target.
                GeneralTransform t1 = from.TransformToVisual(fromWindow.Content);
                Point fromWindowPoint = t1.TransformPoint(point);

                var screenPoint = ToScreen(fromWindow, fromWindowPoint, from.XamlRoot.RasterizationScale);

                var toOrigin = GetClientOriginOnScreen(toWindow);
                var toScale = to.XamlRoot.RasterizationScale;
                Point toWindowPoint = new()
                {
                    X = (screenPoint.X - toOrigin.X) / toScale,
                    Y = (screenPoint.Y - toOrigin.Y) / toScale
                };

                GeneralTransform t2 = toWindow.Content.TransformToVisual(to);
                Point toPoint = t2.TransformPoint(toWindowPoint);

                return toPoint;
            }

            // For elements within the same XamlRoot
            GeneralTransform transform = from.TransformToVisual(to);
            var relativePoint = transform.TransformPoint(point);
            return relativePoint;
        }

        public static Point GetScreenPoint(UIElement element, Point point)
        {
            var fromWindow = HostWindow.GetWindowForElement(element);
            GeneralTransform t1 = element.TransformToVisual(fromWindow.Content);
            Point fromWindowPoint = t1.TransformPoint(point);

            return ToScreen(fromWindow, fromWindowPoint, element.XamlRoot.RasterizationScale);
        }

        private static Point ToScreen(Window window, Point contentPoint, double scale)
        {
            var origin = GetClientOriginOnScreen(window);
            return new Point(contentPoint.X * scale + origin.X, contentPoint.Y * scale + origin.Y);
        }

        /// <summary>
        /// Screen position (physical pixels) of the window's CLIENT origin, which is
        /// where Window.Content starts.
        ///
        /// Do NOT derive this from AppWindow.Position: that is the OUTER window
        /// position, so it needs a caption correction that differs per window and that
        /// AppWindow reports in physical pixels while the callers work in DIPs. Two
        /// windows of the same kind cancel each other's error, so the mistake only
        /// shows when dragging between a normal window and one with extended content.
        /// ClientToScreen needs no special-casing.
        /// </summary>
        private static Point GetClientOriginOnScreen(Window window)
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var origin = default(NativePoint);

                if (hwnd != IntPtr.Zero && ClientToScreen(hwnd, ref origin))
                {
                    return new Point(origin.X, origin.Y);
                }
            }
            catch
            {
                // Closed window — fall through to the outer position.
            }

            var position = window.AppWindow.Position;
            return new Point(position.X, position.Y);
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

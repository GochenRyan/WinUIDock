using Dock.Model.Core;
using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Dock.WinUI3.Internal
{
    public class AdornerHelper
    {
        public DockTarget Adorner { get; set; }
        private Popup _popup;
        private UIElement _currentElement;
        private Grid _host;

        /// <param name="element">The control being adorned (the drop target).</param>
        /// <param name="dockControl">
        /// The DockControl the drop belongs to. Supplies the DOCKABLE rectangle: the
        /// edge guides are laid out inside it, not against the window border, because
        /// that is the region an edge drop actually carves up. Anchored to the window
        /// instead, the top guide fell inside the float window's caption band and was
        /// visible but not hittable.
        /// </param>
        public void AddAdorner(UIElement element, FrameworkElement dockControl = null)
        {
            if (element == null) return;

            var window = HostWindow.GetWindowForElement(element);
            if (window?.Content is not FrameworkElement root)
            {
                return;
            }

            if (_popup is { } && Adorner is { } && ReferenceEquals(_currentElement, element))
            {
                UpdateAdornerLayout(element, root, dockControl);
                return;
            }

            if (_popup is { })
            {
                RemoveAdorner(_currentElement ?? element);
            }

            var host = new Grid
            {
                Width = root.ActualWidth,
                Height = root.ActualHeight,
                // Popups are not part of the themed visual root — inherit the
                // target window's effective theme explicitly so the guides and
                // preview use the right palette.
                RequestedTheme = root.ActualTheme,
            };

            Adorner = new DockTarget();
            host.Children.Add(Adorner);
            _host = host;

            UpdateAdornerLayout(element, root, dockControl);

            _popup = new Popup
            {
                XamlRoot = root.XamlRoot,
                Child = host,
                HorizontalOffset = 0,
                VerticalOffset = 0,
                IsOpen = true
            };

            _currentElement = element;
        }

        public void RemoveAdorner(UIElement element)
        {
            if (element == null) return;

            if (_popup is { })
            {
                _popup.IsOpen = false;
                Adorner = null;
                _popup = null;
                _currentElement = null;
                _host = null;
            }
        }

        private void UpdateAdornerLayout(UIElement element, FrameworkElement root, FrameworkElement dockControl)
        {
            var t = element.TransformToVisual(root);
            var p = t.TransformPoint(new Point(0, 0));

            if (_host is { })
            {
                _host.Width = root.ActualWidth;
                _host.Height = root.ActualHeight;
            }

            if (Adorner is { })
            {
                Adorner.LocalX = p.X;
                Adorner.LocalY = p.Y;
                Adorner.LocalWidth = element.ActualSize.X;
                Adorner.LocalHeight = element.ActualSize.Y;

                // Fall back to the whole window when the caller has no DockControl.
                if (dockControl is { ActualWidth: > 0, ActualHeight: > 0 })
                {
                    var rootPoint = dockControl.TransformToVisual(root).TransformPoint(new Point(0, 0));
                    Adorner.RootX = rootPoint.X;
                    Adorner.RootY = rootPoint.Y;
                    Adorner.RootWidth = dockControl.ActualWidth;
                    Adorner.RootHeight = dockControl.ActualHeight;
                }
                else
                {
                    Adorner.RootX = 0;
                    Adorner.RootY = 0;
                    Adorner.RootWidth = root.ActualWidth;
                    Adorner.RootHeight = root.ActualHeight;
                }

            }
        }
    }
}

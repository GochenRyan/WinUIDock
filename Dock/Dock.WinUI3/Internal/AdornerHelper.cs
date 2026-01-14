using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Dock.WinUI3.Internal
{
    public class AdornerHelper
    {
        public DockTarget Adorner { get; set; }
        private Popup _popup;
        private UIElement _currentElement;
        private Grid _host;

        public void AddAdorner(UIElement element)
        {
            if (element == null) return;

            var window = HostWindow.GetWindowForElement(element);
            var root = (FrameworkElement)window.Content;

            if (_popup is { } && Adorner is { } && ReferenceEquals(_currentElement, element))
            {
                UpdateAdornerLayout(element, root);
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
            };

            Adorner = new DockTarget();
            host.Children.Add(Adorner);
            _host = host;

            UpdateAdornerLayout(element, root);

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

        private void UpdateAdornerLayout(UIElement element, FrameworkElement root)
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
            }
        }
    }
}

using Dock.WinUI3.Controls;
using Microsoft.UI;
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

        public void AddAdorner(UIElement element)
        {
            if (element == null) return;

            var grid = new Grid()
            {
                Width = element.ActualSize.X,
                Height = element.ActualSize.Y,
                Background = new SolidColorBrush(Colors.Brown)
            };
            Adorner = new DockTarget();
            grid.Children.Add(Adorner);

            var currentWindow = HostWindow.GetWindowForElement(element);
            var t = element.TransformToVisual(currentWindow.Content);
            var windowPoint = t.TransformPoint(new Point());

            _popup = new Popup();
            _popup.XamlRoot = element.XamlRoot;
            _popup.Child = grid;
            _popup.HorizontalOffset = windowPoint.X;
            _popup.VerticalOffset = windowPoint.Y;
            _popup.IsOpen = true;
        }

        public void RemoveAdorner(UIElement element)
        {
            if (element == null) return;

            if (_popup is { })
            {
                Adorner = null;
                _popup = null;
            }
        }
    }
}

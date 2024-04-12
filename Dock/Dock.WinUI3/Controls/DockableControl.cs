using Dock.Model.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Dock.WinUI3.Controls
{
    public sealed class DockableControl : Panel
    {
        private IDockable _currentDockable;

        public DockableControl()
        {
            Loading += DockableControl_Loading;
            SizeChanged += DockableControl_SizeChanged;
            DataContextChanged += DockableControl_DataContextChanged;
        }

        private void DockableControl_Loading(FrameworkElement sender, object args)
        {
            AddHandler(PointerPressedEvent, new PointerEventHandler(PressedHandler), true);
            AddHandler(PointerMovedEvent, new PointerEventHandler(MovedHandler), true);
        }

        private void DockableControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void DockableControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
        }

        private void PressedHandler(object sender, PointerRoutedEventArgs e)
        {
        }

        private void MovedHandler(object sender, PointerRoutedEventArgs e)
        {
        }

        public static readonly DependencyProperty TrackingModeProperty =
        DependencyProperty.Register(
            nameof(TrackingMode),
            typeof(TrackingMode),
            typeof(DockableControl),
            new PropertyMetadata(null, new PropertyChangedCallback(OnTrackingModeChanged)));

        public TrackingMode TrackingMode
        {
            get { return (TrackingMode)GetValue(TrackingModeProperty); }
            set { SetValue(TrackingModeProperty, value); }
        }

        private static void OnTrackingModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
    }
}

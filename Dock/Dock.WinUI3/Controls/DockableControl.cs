using Dock.Model.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Dock.WinUI3.Controls
{
    public class DockableControl : Panel, IDockableControl
    {
        private IDockable _currentDockable;

        public DockableControl()
        {
            PressedHandler = new PointerEventHandler(OnPressed);
            MovedHandler = new PointerEventHandler(OnMoved);

            Loaded += DockableControl_Loaded;
            Unloaded += DockableControl_Unloaded;
            SizeChanged += DockableControl_SizeChanged;
            DataContextChanged += DockableControl_DataContextChanged;
        }

        private void DockableControl_Unloaded(object sender, RoutedEventArgs e)
        {
            RemoveHandler(PointerPressedEvent, PressedHandler);
            RemoveHandler(PointerMovedEvent, MovedHandler);
        }

        private void DockableControl_Loaded(object sender, RoutedEventArgs e)
        {
            AddHandler(PointerPressedEvent, PressedHandler, true);
            AddHandler(PointerMovedEvent, MovedHandler, true);
        }

        private void DockableControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void DockableControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_currentDockable is not null)
            {
                UnRegister(_currentDockable);
                _currentDockable = null;
            }

            if (args.NewValue is IDockable dockableChanged)
            {
                _currentDockable = dockableChanged;
                Register(dockableChanged);
            }
        }

        private void Register(IDockable dockable)
        {
            switch (TrackingMode)
            {
                case TrackingMode.Visible:
                    if (dockable.Factory is not null)
                    {
                        dockable.Factory.VisibleDockableControls[dockable] = this;
                    }
                    break;
                case TrackingMode.Pinned:
                    if (dockable.Factory is not null)
                    {
                        dockable.Factory.PinnedDockableControls[dockable] = this;
                    }
                    break;
                case TrackingMode.Tab:
                    if (dockable.Factory is not null)
                    {
                        dockable.Factory.TabDockableControls[dockable] = this;
                    }
                    break;
            }
        }

        private void UnRegister(IDockable dockable)
        {
            switch (TrackingMode)
            {
                case TrackingMode.Visible:
                    dockable.Factory?.VisibleDockableControls.Remove(dockable);
                    break;
                case TrackingMode.Pinned:
                    dockable.Factory?.PinnedDockableControls.Remove(dockable);
                    break;
                case TrackingMode.Tab:
                    dockable.Factory?.TabDockableControls.Remove(dockable);
                    break;
            }
        }

        private void OnPressed(object sender, PointerRoutedEventArgs e)
        {
            SetPointerTracking(e);
        }

        private void OnMoved(object sender, PointerRoutedEventArgs e)
        {
            SetPointerTracking(e);
        }

        private void SetPointerTracking(PointerRoutedEventArgs e)
        {
            if (DataContext is not IDockable dockable)
            {
                return;
            }

            var position = e.GetCurrentPoint(this).Position;

            //var screenPoint = DockHelpers.ToDockPoint(this.PointToScreen(position).ToPoint(1.0));

            dockable.SetPointerPosition(position.X, position.Y);
            //dockable.SetPointerScreenPosition(screenPoint.X, screenPoint.Y);
        }

        public static readonly DependencyProperty TrackingModeProperty =
        DependencyProperty.Register(
            nameof(TrackingMode),
            typeof(TrackingMode),
            typeof(DockableControl),
            new PropertyMetadata(default));

        public TrackingMode TrackingMode
        {
            get { return (TrackingMode)GetValue(TrackingModeProperty); }
            set { SetValue(TrackingModeProperty, value); }
        }

        private static void OnTrackingModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        private PointerEventHandler PressedHandler { get; set; }
        private PointerEventHandler MovedHandler { get; set; }
    }
}

using Dock.Model.Core;
using Dock.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Dock.WinUI3.Internal
{
    internal class DockDragState
    {
        public Control DragControl { get; set; }
        public Control DropControl { get; set; }
        public Point DragStartPoint { get; set; }
        public bool PointerPressed { get; set; }
        public bool DoDragDrop { get; set; }
        public Point TargetPoint { get; set; }
        public UIElement TargetDockControl { get; set; }

        public void Start(Control dragControl, Point point)
        {
            DragControl = dragControl;
            DropControl = null;
            DragStartPoint = point;
            PointerPressed = true;
            DoDragDrop = false;
            TargetPoint = default;
            TargetDockControl = null;
        }

        public void End()
        {
            DragControl = null;
            DropControl = null;
            DragStartPoint = default;
            PointerPressed = false;
            DoDragDrop = false;
            TargetPoint = default;
            TargetDockControl = null;
        }
    }
    internal class DockControlState : IDockControlState
    {
        private readonly AdornerHelper _adornerHelper = new();
        private readonly DockDragState _state = new();

        public IDockManager DockManager { get; set; }

        public DockControlState(IDockManager dockManager)
        {
            DockManager = dockManager;
        }

        private void Enter(Point point, DragAction dragAction, UIElement relativeTo)
        {
            var isValid = Validate(point, DockOperation.Fill, dragAction, relativeTo);
            if (isValid && _state.DropControl is { } control && control.GetValue(DockProperties.IsDockTargetProperty) != null)
            {
                _adornerHelper.AddAdorner(control);
            }
        }

        private void Over(Point point, DragAction dragAction, UIElement relativeTo)
        {
            var operation = DockOperation.Fill;

            if (_adornerHelper.Adorner is DockTarget target)
            {
                operation = target.GetDockOperation(point, relativeTo, dragAction, Validate);
            }

            Validate(point, operation, dragAction, relativeTo);
        }

        private void Drop(Point point, DragAction dragAction, UIElement relativeTo)
        {

        }

        private void Leave()
        {

        }

        private bool Validate(Point point, DockOperation operation, DragAction dragAction, UIElement relativeTo)
        {
            if (_state.DragControl is null || _state.DropControl is null)
            {
                return false;
            }

            if (_state.DragControl.DataContext is IDockable sourceDockable && _state.DropControl.DataContext is IDockable targetDockable)
            {
                DockManager.Position = DockHelpers.ToDockPoint(point);

                if (relativeTo.XamlRoot is null)
                {
                    return false;
                }

                GeneralTransform transform = relativeTo.TransformToVisual(Window.Current.Content);
                var screenPoint = transform.TransformPoint(point);
                DockManager.ScreenPosition = DockHelpers.ToDockPoint(screenPoint);

                return DockManager.ValidateDockable(sourceDockable, targetDockable, dragAction, operation, bExecute: false);
            }

            return false;
        }
    }

}

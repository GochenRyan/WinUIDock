using Dock.Model.Core;
using Dock.Settings;
using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Dock.WinUI3.Internal
{
    internal class WindowDragState
    {
        public Point DragStartPoint { get; set; }
        public bool PointerPressed { get; set; }
        public bool DoDragDrop { get; set; }
        public DockControl TargetDockControl { get; set; }
        public Point TargetPoint { get; set; }
        public Control TargetDropControl { get; set; }
        public DragAction DragAction { get; set; }

        public void Start(Point point)
        {
            DragStartPoint = point;
            PointerPressed = true;
            DoDragDrop = false;
            TargetDockControl = null;
            TargetPoint = default;
            TargetDropControl = null;
            DragAction = DragAction.Move;
        }

        public void End()
        {
            DragStartPoint = default;
            PointerPressed = false;
            DoDragDrop = false;
            TargetDockControl = null;
            TargetPoint = default;
            TargetDropControl = null;
            DragAction = DragAction.Move;
        }
    }

    internal class HostWindowState : IHostWindowState
    {
        private readonly AdornerHelper _adornerHelper = new AdornerHelper();
        private readonly HostWindowControl _hostWindow;
        private readonly WindowDragState _state = new();

        /// <inheritdoc/>
        public IDockManager DockManager { get; set; }

        public HostWindowState(IDockManager dockManager, HostWindowControl hostWindow)
        {
            DockManager = dockManager;
            _hostWindow = hostWindow;
        }

        private void Enter(Point point, DragAction dragAction, FrameworkElement relativeTo)
        {
            var isValid = Validate(point, DockOperation.Fill, dragAction, relativeTo);

            if (isValid && _state.TargetDropControl is { } control && (bool)control.GetValue(DockProperties.IsDockTargetProperty))
            {
                _adornerHelper.AddAdorner(control);
            }
        }

        private void Over(Point point, DragAction dragAction, FrameworkElement relativeTo)
        {
            var operation = DockOperation.Fill;

            if (_adornerHelper.Adorner is DockTarget target)
            {
                operation = target.GetDockOperation(point, relativeTo, dragAction, Validate);
            }

            if (operation != DockOperation.Window)
            {
                Validate(point, operation, dragAction, relativeTo);
            }
        }

        private void Drop(Point point, DragAction dragAction, FrameworkElement relativeTo)
        {
            var operation = DockOperation.Window;

            if (_adornerHelper.Adorner is DockTarget target)
            {
                operation = target.GetDockOperation(point, relativeTo, dragAction, Validate);
            }

            if (_state.TargetDropControl is { } control && (bool)control.GetValue(DockProperties.IsDockTargetProperty))
            {
                _adornerHelper.RemoveAdorner(control);
            }

            if (operation != DockOperation.Window)
            {
                Execute(point, operation, dragAction, relativeTo);
            }
        }

        private void Leave()
        {
            if (_state.TargetDropControl is { } control && (bool)control.GetValue(DockProperties.IsDockTargetProperty))
            {
                _adornerHelper.RemoveAdorner(control);
            }
        }

        private bool Validate(Point point, DockOperation operation, DragAction dragAction, FrameworkElement relativeTo)
        {
            if (_state.TargetDropControl is null)
            {
                return false;
            }

            var layout = _hostWindow.Window?.Layout;

            if (layout?.FocusedDockable is { } sourceDockable && _state.TargetDropControl.DataContext is IDockable targetDockable)
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

        private void Execute(Point point, DockOperation operation, DragAction dragAction, FrameworkElement relativeTo)
        {
            if (_state.TargetDropControl is null)
            {
                return;
            }

            var layout = _hostWindow.Window?.Layout;

            if (layout?.ActiveDockable is { } sourceDockable && _state.TargetDropControl.DataContext is IDockable targetDockable)
            {
                DockManager.Position = DockHelpers.ToDockPoint(point);

                if (relativeTo.XamlRoot is null)
                {
                    return;
                }
                GeneralTransform transform = relativeTo.TransformToVisual(Window.Current.Content);
                var screenPoint = transform.TransformPoint(point);
                DockManager.ScreenPosition = DockHelpers.ToDockPoint(screenPoint);

                DockManager.ValidateDockable(sourceDockable, targetDockable, dragAction, operation, bExecute: true);
            }
        }

        private bool IsMinimumDragDistance(Point diff)
        {
            return (Math.Abs(diff.X) > DockSettings.MinimumHorizontalDragDistance
                    || Math.Abs(diff.Y) > DockSettings.MinimumVerticalDragDistance);
        }

        /// <summary>
        /// Process pointer event.
        /// </summary>
        /// <param name="point">The pointer position.</param>
        /// <param name="eventType">The pointer event type.</param>
        public void Process(Point point, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Pressed:
                    {
                        var isDragEnabled = (bool)_hostWindow.GetValue(DockProperties.IsDragEnabledProperty);
                        if (isDragEnabled != true)
                        {
                            break;
                        }
                        _state.Start(point);
                        break;
                    }
                case EventType.Released:
                    {
                        if (_state.DoDragDrop)
                        {
                            if (_state.TargetDockControl is { } && _state.TargetDropControl is { })
                            {
                                var isDropEnabled = true;

                                if (_state.TargetDockControl is Control targetControl)
                                {
                                    isDropEnabled = (bool)targetControl.GetValue(DockProperties.IsDropEnabledProperty);
                                }

                                if (isDropEnabled)
                                {
                                    Drop(_state.TargetPoint, _state.DragAction, _state.TargetDockControl);
                                }
                            }
                        }
                        Leave();
                        _state.End();
                        break;
                    }
                case EventType.Moved:
                    {
                        if (_state.PointerPressed == false)
                        {
                            break;
                        }

                        if (_state.DoDragDrop == false)
                        {
                            var diff = new Point(_state.DragStartPoint.X - point.X, _state.DragStartPoint.Y - point.Y);
                            var haveMinimumDragDistance = IsMinimumDragDistance(diff);
                            if (haveMinimumDragDistance)
                            {
                                _state.DoDragDrop = true;
                            }
                        }

                        if (!_state.DoDragDrop || _hostWindow.Window?.Layout?.Factory is not { } factory)
                        {
                            break;
                        }

                        foreach (var dockControl in factory.DockControls.GetZOrderedDockControls())
                        {
                            if (dockControl.Layout == _hostWindow.Window?.Layout)
                            {
                                continue;
                            }

                            var position = new Point(point.X + _state.DragStartPoint.X, point.Y + _state.DragStartPoint.Y);
                            var screenPoint = new Point((int)position.X, (int)position.Y);
                            if (dockControl.XamlRoot is null)
                            {
                                continue;
                            }

                            GeneralTransform t = Window.Current.Content.TransformToVisual(dockControl);
                            var dockControlPoint = t.TransformPoint(screenPoint);
                            var dropControl = DockHelpers.GetControl(dockControl, dockControlPoint, DockProperties.IsDropAreaProperty);
                            if (dropControl is { })
                            {
                                var isDropEnabled = (bool)dockControl.GetValue(DockProperties.IsDropEnabledProperty);
                                if (!isDropEnabled)
                                {
                                    Leave();
                                    _state.TargetDockControl = null;
                                    _state.TargetPoint = default;
                                    _state.TargetDropControl = null;
                                }
                                else
                                {
                                    if (_state.TargetDropControl == dropControl)
                                    {
                                        _state.TargetDockControl = dockControl;
                                        _state.TargetPoint = dockControlPoint;
                                        _state.TargetDropControl = dropControl;
                                        _state.DragAction = DragAction.Move;
                                        Over(_state.TargetPoint, _state.DragAction, _state.TargetDockControl);
                                        break;
                                    }

                                    if (_state.TargetDropControl is { })
                                    {
                                        Leave();
                                        _state.TargetDropControl = null;
                                    }

                                    _state.TargetDockControl = dockControl;
                                    _state.TargetPoint = dockControlPoint;
                                    _state.TargetDropControl = dropControl;
                                    _state.DragAction = DragAction.Move;
                                    Enter(_state.TargetPoint, _state.DragAction, _state.TargetDockControl);
                                    break;
                                }
                            }
                        }

                        break;
                    }
                case EventType.Enter:
                    {
                        break;
                    }
                case EventType.Leave:
                    {
                        break;
                    }
                case EventType.CaptureLost:
                    {
                        _state.End();
                        break;
                    }
                case EventType.WheelChanged:
                    {
                        break;
                    }
            }
        }
    }

}

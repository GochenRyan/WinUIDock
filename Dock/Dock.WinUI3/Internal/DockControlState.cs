using Dock.Model.Core;
using Dock.Settings;
using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
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
        public FrameworkElement TargetDockControl { get; set; }

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

        private void Enter(Point point, DragAction dragAction, FrameworkElement relativeTo)
        {
            var valid = Validate(point, DockOperation.Fill, dragAction, relativeTo);
            if (_state.DropControl is { } control && DockProperties.GetIsDockTarget(control))
            {
                EnsureAdorner(control);
            }
        }

        private void Over(Point point, DragAction dragAction, FrameworkElement relativeTo)
        {
            var operation = DockOperation.Fill;

            if (_adornerHelper.Adorner is DockTarget target)
            {
                var sourceDockable = _state.DragControl?.DataContext as IDockable;
                var targetDockable = _state.DropControl?.DataContext as IDockable;
                operation = target.GetDockOperation(point, relativeTo, dragAction, sourceDockable, targetDockable, Validate);
            }

            Validate(point, operation, dragAction, relativeTo);
        }

        private void Drop(Point point, DragAction dragAction, FrameworkElement relativeTo)
        {
            var operation = DockOperation.Window;

            if (_adornerHelper.Adorner is DockTarget target)
            {
                var sourceDockable = _state.DragControl?.DataContext as IDockable;
                var targetDockable = _state.DropControl?.DataContext as IDockable;
                operation = target.GetDockOperation(point, relativeTo, dragAction, sourceDockable, targetDockable, Validate);
            }

            if (_state.DropControl is { } control && DockProperties.GetIsDockTarget(control))
            {
                _adornerHelper.RemoveAdorner(control);
            }

            Execute(point, operation, dragAction, relativeTo);
        }

        private void Leave()
        {
            if (_adornerHelper.Adorner is { } adorner)
            {
                _adornerHelper.RemoveAdorner(adorner);
            }
        }

        private static bool IsPointInBounds(Point point, FrameworkElement element)
        {
            return point.X >= 0
                   && point.Y >= 0
                   && point.X <= element.ActualWidth
                   && point.Y <= element.ActualHeight;
        }

        private void EnsureAdorner(UIElement element)
        {
            _adornerHelper.AddAdorner(element);
        }

        private void ShowRootAdorner(Point point, FrameworkElement relativeTo)
        {
            if (relativeTo is null)
            {
                return;
            }

            var window = HostWindow.GetWindowForElement(relativeTo);
            if (window?.Content is not FrameworkElement root)
            {
                return;
            }

            var rootPoint = Extensions.TransformPoint(relativeTo, point, root);
            if (!IsPointInBounds(rootPoint, root))
            {
                if (_adornerHelper.Adorner is { })
                {
                    _adornerHelper.RemoveAdorner(_adornerHelper.Adorner);
                }
                return;
            }

            EnsureAdorner(root);
            if (_adornerHelper.Adorner is DockTarget target)
            {
                var sourceDockable = _state.DragControl?.DataContext as IDockable;
                target.UpdateRootVisibility(sourceDockable);
            }
        }

        private bool Validate(Point point, DockOperation operation, DragAction dragAction, FrameworkElement relativeTo)
        {
            if (_state.DragControl is null || _state.DropControl is null)
            {
                return false;
            }

            if (_state.DragControl.DataContext is IDockable sourceDockable && _state.DropControl.DataContext is IDockable targetDockable)
            {
                var ownerWindow = HostWindow.GetWindowForElement(relativeTo);
                GeneralTransform transform = ownerWindow.Content.TransformToVisual(relativeTo);
                var relativePoint = transform.TransformPoint(point);
                DockManager.Position = DockHelpers.ToDockPoint(relativePoint);

                if (relativeTo.XamlRoot is null)
                {
                    return false;
                }

                var screenPoint = Extensions.GetScreenPoint(ownerWindow.Content, point);
                DockManager.ScreenPosition = DockHelpers.ToDockPoint(screenPoint);

                return DockManager.ValidateDockable(sourceDockable, targetDockable, dragAction, operation, bExecute: false);
            }

            return false;
        }

        private void Execute(Point point, DockOperation operation, DragAction dragAction, FrameworkElement relativeTo)
        {
            if (_state.DragControl is null || _state.DropControl is null)
            {
                return;
            }

            if (_state.DragControl.DataContext is IDockable sourceDockable && _state.DropControl.DataContext is IDockable targetDockable)
            {
                if (sourceDockable is IDock dock)
                {
                    sourceDockable = dock.ActiveDockable;
                }

                if (sourceDockable == null)
                {
                    return;
                }

                var ownerWindow = HostWindow.GetWindowForElement(relativeTo);
                GeneralTransform t = ownerWindow.Content.TransformToVisual(relativeTo);
                Point relativePoint = t.TransformPoint(point);
                DockManager.Position = DockHelpers.ToDockPoint(relativePoint);

                if (relativeTo.XamlRoot is null)
                {
                    return;
                }

                var screenPoint = Extensions.GetScreenPoint(ownerWindow.Content, point);
                DockManager.ScreenPosition = DockHelpers.ToDockPoint(screenPoint);
                DockManager.ValidateDockable(sourceDockable, targetDockable, dragAction, operation, true);
            }
        }

        private static bool IsMinimumDragDistance(Vector diff)
        {
            return (Math.Abs(diff.X) > DockSettings.MinimumHorizontalDragDistance
                    || Math.Abs(diff.Y) > DockSettings.MinimumVerticalDragDistance);
        }

        /// <summary>
        /// Process pointer event.
        /// </summary>
        /// <param name="point">The pointer position.</param>
        /// <param name="delta">The mouse wheel delta.</param>
        /// <param name="eventType">The pointer event type.</param>
        /// <param name="dragAction">The input drag action.</param>
        /// <param name="activeDockControl">The active dock control.</param>
        /// <param name="dockControls">The dock controls.</param>
        public void Process(Point point, Vector delta, EventType eventType, DragAction dragAction, DockControl activeDockControl, IList<IDockControl> dockControls)
        {
            if (activeDockControl is not { } inputActiveDockControl)
            {
                return;
            }

            switch (eventType)
            {
                case EventType.Pressed:
                    {
                        var dragControl = DockHelpers.GetControl(inputActiveDockControl, point, DockProperties.IsDragAreaProperty);
                        if (dragControl is { })
                        {
                            bool isDragEnabled = DockProperties.GetIsDragEnabled(dragControl);
                            if (!isDragEnabled)
                            {
                                break;
                            }
                            _state.Start(dragControl, point);
                            activeDockControl.IsDraggingDock = true;
                        }
                        break;
                    }
                case EventType.Released:
                    {
                        if (_state.DoDragDrop)
                        {
                            if (_state.DropControl is { } && _state.TargetDockControl is { })
                            {
                                var isDropEnabled = true;

                                if (_state.TargetDockControl is Control targetControl)
                                {
                                    // The DROP target is gated by IsDropEnabled;
                                    // IsDragEnabled belongs to the drag SOURCE
                                    // (see the Pressed branch). Reading the wrong
                                    // one here let a drop execute on a target that
                                    // the Moved branch had already refused to show
                                    // guides for.
                                    isDropEnabled = DockProperties.GetIsDropEnabled(targetControl);
                                }

                                if (isDropEnabled)
                                {
                                    Drop(_state.TargetPoint, dragAction, _state.TargetDockControl);
                                }
                            }
                            else
                            {
                                // Drag out of the window
                                _state.DropControl = activeDockControl;
                                _state.TargetDockControl = activeDockControl;
                                _state.TargetPoint = point;
                                Drop(_state.TargetPoint, dragAction, activeDockControl);
                                _state.DropControl = null;
                                _state.TargetPoint = default;
                                _state.TargetDockControl = null;
                            }
                        }
                        Leave();
                        _state.End();
                        activeDockControl.IsDraggingDock = false;
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
                            Vector diff = new Vector(_state.DragStartPoint.X - point.X, _state.DragStartPoint.Y - point.Y);
                            var haveMinimumDragDistance = IsMinimumDragDistance(diff);
                            if (haveMinimumDragDistance)
                            {
                                if (_state.DragControl?.DataContext is IDockable targetDockable)
                                {
                                    DockHelpers.ShowWindows(targetDockable);
                                }
                                _state.DoDragDrop = true;
                            }
                        }

                        if (_state.DoDragDrop)
                        {
                            Point targetPoint = default;
                            FrameworkElement targetDockControl = null;
                            Control dropControl = null;
                            bool isOverDockControl = false;

                            foreach (var inputDockControl in dockControls.GetZOrderedDockControls())
                            {
                                if (inputActiveDockControl.XamlRoot is null)
                                {
                                    continue;
                                }

                                if (inputDockControl.XamlRoot is null)
                                {
                                    continue;
                                }

                                if (inputActiveDockControl.XamlRoot != inputDockControl.XamlRoot)
                                {
                                    var fromWindow = HostWindow.GetWindowForElement(inputActiveDockControl);
                                    var toWindow = HostWindow.GetWindowForElement(inputDockControl);
                                    if (!DockSettings.DockBetweenFloatWindows && toWindow != HostWindow.MainWindow)
                                        continue;

                                    var toPoint = Extensions.TransformPoint(fromWindow.Content, point, toWindow.Content);
                                    dropControl = DockHelpers.GetControl(inputDockControl, toPoint, DockProperties.IsDropAreaProperty);
                                    if (dropControl is { })
                                        targetPoint = toPoint;

                                    if (IsPointInBounds(toPoint, inputDockControl))
                                    {
                                        isOverDockControl = true;
                                    }
                                }
                                else
                                {
                                    dropControl = DockHelpers.GetControl(inputDockControl, point, DockProperties.IsDropAreaProperty);
                                    if (dropControl is { })
                                        targetPoint = point;

                                    if (IsPointInBounds(point, inputDockControl))
                                    {
                                        isOverDockControl = true;
                                    }
                                }

                                if (dropControl is { })
                                {
                                    targetDockControl = inputDockControl;
                                    break;
                                }
                            }

                            if (dropControl is null)
                            {
                                dropControl = DockHelpers.GetControl(inputActiveDockControl, point, DockProperties.IsDropAreaProperty);
                                if (dropControl is { })
                                {
                                    targetPoint = point;
                                    targetDockControl = inputActiveDockControl;
                                }
                            }

                            if (IsPointInBounds(point, inputActiveDockControl))
                            {
                                isOverDockControl = true;
                            }

                            if (dropControl is { } && targetDockControl is { })
                            {
                                var isDropEnabled = true;

                                if (targetDockControl is Control targetControl)
                                {
                                    isDropEnabled = DockProperties.GetIsDropEnabled(targetControl);
                                }

                                if (isDropEnabled)
                                {
                                    if (_state.DropControl == dropControl)
                                    {
                                        _state.TargetPoint = targetPoint;
                                        _state.TargetDockControl = targetDockControl;
                                        Over(targetPoint, dragAction, targetDockControl);
                                    }
                                    else
                                    {
                                        if (_state.DropControl is { })
                                        {
                                            Leave();
                                            _state.DropControl = null;
                                        }

                                        _state.DropControl = dropControl;
                                        _state.TargetPoint = targetPoint;
                                        _state.TargetDockControl = targetDockControl;
                                        Enter(targetPoint, dragAction, targetDockControl);
                                    }
                                }
                                else
                                {
                                    if (_state.DropControl is { })
                                    {
                                        Leave();
                                        _state.DropControl = null;
                                        _state.TargetPoint = default;
                                        _state.TargetDockControl = null;
                                    }
                                }
                            }
                            else
                            {
                                if (_state.DropControl is { })
                                {
                                    Leave();
                                    _state.DropControl = null;
                                }

                                if (!isOverDockControl)
                                {
                                    _state.TargetPoint = point;
                                    _state.TargetDockControl = inputActiveDockControl;
                                    ShowRootAdorner(point, inputActiveDockControl);
                                }
                                else
                                {
                                    Leave();
                                    _state.DropControl = null;
                                    _state.TargetPoint = default;
                                    _state.TargetDockControl = null;
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
                        Leave();
                        _state.End();
                        activeDockControl.IsDraggingDock = false;
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

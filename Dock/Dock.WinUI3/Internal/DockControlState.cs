using Dock.Model.Controls;
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

        /// <summary>Window last raised by the drag; only tracked to avoid re-raising it.</summary>
        private IntPtr _raisedWindow;

        /// <summary>Last line written by <see cref="LogDragState"/>, for de-duplication.</summary>
        private string _lastDiagState;

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
                // relativeTo IS the target DockControl — it carries the dockable
                // rectangle the edge guides are laid out in.
                EnsureAdorner(control, relativeTo);
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

            LogDragState($"over op={operation} drop={DockDiag.Describe(_state.DropControl)}");
        }

        /// <summary>
        /// The release point expressed in the TARGET window's coordinates — the same
        /// space <see cref="DockDragState.TargetPoint"/> uses, so it is a drop-in
        /// replacement for it. Falls back to the recorded target point if the
        /// cross-window transform is unavailable (a window closing mid-drag).
        /// </summary>
        private Point ResolveDropPoint(Point point, FrameworkElement activeDockControl)
        {
            if (_state.TargetDockControl is not { } targetDockControl)
            {
                return _state.TargetPoint;
            }

            if (ReferenceEquals(targetDockControl.XamlRoot, activeDockControl.XamlRoot))
            {
                return point;
            }

            try
            {
                var fromWindow = HostWindow.GetWindowForElement(activeDockControl);
                var toWindow = HostWindow.GetWindowForElement(targetDockControl);

                if (fromWindow?.Content is { } fromContent && toWindow?.Content is { } toContent)
                {
                    return Extensions.TransformPoint(fromContent, point, toContent);
                }
            }
            catch
            {
                // Window gone — the recorded point is the best that remains.
            }

            return _state.TargetPoint;
        }

        /// <summary>
        /// Per-move diagnostics, de-duplicated: a drag produces hundreds of moves but
        /// only a handful of distinct resolutions, so this stays within DockDiag's
        /// "no per-frame chatter" rule while still showing every transition.
        /// </summary>
        private void LogDragState(string state)
        {
            if (!DockDiag.IsEnabled || state == _lastDiagState)
            {
                return;
            }

            _lastDiagState = state;
            DockDiag.Log($"drag: {state}");
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

            // The operation is re-resolved here from _state.TargetPoint (the LAST
            // MOVE's point, not the release point) — worth seeing when the executed
            // operation disagrees with the guide that was highlighted.
            DockDiag.Log($"drop: op={operation} at {point.X:F0},{point.Y:F0} "
                         + $"(last move was {_state.TargetPoint.X:F0},{_state.TargetPoint.Y:F0}) "
                         + $"drop={DockDiag.Describe(_state.DropControl)}");

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

        /// <summary>
        /// A drag that ends in a float window leaves it BEHIND the source window:
        /// the drop shows the new window first, but the cleanup that follows
        /// (adorner teardown, pointer-capture release) re-activates the source on
        /// top of it. Activating the dockable's final host window one tick later
        /// wins that race; a dockable that landed in a plain dock resolves to a
        /// root without a window and is a no-op.
        /// </summary>
        private static void ActivateFinalHostWindow(IDockable dockable)
        {
            var current = dockable?.Owner;
            while (current is { } && current is not IRootDock)
            {
                current = current.Owner;
            }

            if (current is not IRootDock root
                || root.Window?.Host is not HostWindowControl host
                || host.OwnerWindow is not { } window)
            {
                return;
            }

            window.DispatcherQueue?.TryEnqueue(() =>
            {
                try
                {
                    window.Activate();
                }
                catch
                {
                    // The window closed between the drop and this tick.
                }
            });
        }

        private static bool IsPointInBounds(Point point, FrameworkElement element)
        {
            return point.X >= 0
                   && point.Y >= 0
                   && point.X <= element.ActualWidth
                   && point.Y <= element.ActualHeight;
        }

        private void EnsureAdorner(UIElement element, FrameworkElement dockControl = null)
        {
            _adornerHelper.AddAdorner(element, dockControl);
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

            // `point` is ALREADY relative to the window content — the DockControl
            // pointer handlers report GetCurrentPoint(ownerWindow.Content). Running it
            // through relativeTo -> root added the DockControl's own offset a second
            // time, so near the bottom of a window the bounds check saw an outside
            // point and tore the adorner down.
            var rootPoint = point;
            if (!IsPointInBounds(rootPoint, root))
            {
                if (_adornerHelper.Adorner is { })
                {
                    _adornerHelper.RemoveAdorner(_adornerHelper.Adorner);
                }
                return;
            }

            EnsureAdorner(root, relativeTo);
            if (_adornerHelper.Adorner is DockTarget target)
            {
                var sourceDockable = _state.DragControl?.DataContext as IDockable;

                // Layout of the window the adorner is on — it decides whether the
                // source's "already at this edge" suppression applies here at all.
                var targetDockable = (relativeTo as DockControl)?.Layout;
                target.UpdateRootVisibility(sourceDockable, targetDockable);
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
                // D18: deliberately NOT reduced to dock.ActiveDockable. Dragging a
                // tool TAB gives a tool; dragging the chrome caption gives the whole
                // IToolDock, and the whole dock is what should then move.
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

        /// <summary>
        /// Screen point for the pointer, or an off-screen sentinel when it cannot be
        /// derived.
        ///
        /// The transform must start at the WINDOW CONTENT: the DockControl pointer
        /// handlers already report <c>GetCurrentPoint(ownerWindow.Content)</c>, so
        /// handing <see cref="Extensions.GetScreenPoint"/> the DockControl instead
        /// made it re-apply that control's offset inside the window — 32px for a
        /// float window's title bar, about 40 for the sample's menu bar — and probed
        /// the OS that far BELOW the real cursor. Near the bottom edge of a window the
        /// probe then landed outside it entirely, so the window underneath was
        /// reported as topmost and promptly raised over the one being dragged onto.
        ///
        /// Wrapped because it reads XamlRoot and the window handle, both of which
        /// throw on a window that has just closed — and this runs on every pointer
        /// move, exactly when float windows are being torn down.
        /// </summary>
        private static Point TryGetScreenPoint(UIElement element, Point point)
        {
            try
            {
                if (HostWindow.GetWindowForElement(element)?.Content is not UIElement content)
                {
                    return new Point(double.NaN, double.NaN);
                }

                return Extensions.GetScreenPoint(content, point);
            }
            catch
            {
                return new Point(double.NaN, double.NaN);
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
                            // Drop anything a previous gesture left behind. A drag
                            // whose release never came back here (the pointer ended up
                            // over another window) leaves the adorner open and the
                            // drag state half-set.
                            Leave();

                            _state.Start(dragControl, point);
                            _raisedWindow = IntPtr.Zero;
                            activeDockControl.IsDraggingDock = true;
                        }
                        break;
                    }
                case EventType.Released:
                    {
                        IDockable draggedDockable = null;
                        if (_state.DoDragDrop)
                        {
                            draggedDockable = _state.DragControl?.DataContext as IDockable;
                            if (_state.DropControl is { } && _state.TargetDockControl is { })
                            {
                                var isDropEnabled = true;

                                if (_state.TargetDockControl is Control targetControl)
                                {
                                    // IsDropEnabled, not IsDragEnabled — the latter
                                    // belongs to the drag SOURCE (see Pressed).
                                    isDropEnabled = DockProperties.GetIsDropEnabled(targetControl);
                                }

                                if (isDropEnabled)
                                {
                                    // Resolve at the RELEASE point, not at the last
                                    // move's. Pointer moves are sampled and coalesced,
                                    // so a fast gesture leaves TargetPoint a sample
                                    // behind the cursor — and the guide is resolved
                                    // from wherever that stale sample landed, which is
                                    // often no guide at all (=> Window => floats).
                                    Drop(ResolveDropPoint(point, inputActiveDockControl), dragAction, _state.TargetDockControl);
                                }
                            }
                            else
                            {
                                // Drag out of the window
                                DockDiag.Log($"drop: NO TARGET (drop={DockDiag.Describe(_state.DropControl)}, "
                                             + $"targetDockControl={DockDiag.Describe(_state.TargetDockControl)}) -> float");
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
                        _raisedWindow = IntPtr.Zero;
                        activeDockControl.IsDraggingDock = false;

                        ActivateFinalHostWindow(draggedDockable);
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

                            // Screen coordinates, so the OS can be asked which window
                            // is really on top under the cursor. Overlapping float
                            // windows are otherwise resolved by creation order.
                            var pointerScreenPoint = TryGetScreenPoint(inputActiveDockControl, point);
                            var topWindow = dockControls.GetOwnWindowAt(pointerScreenPoint);

                            // Bring it forward. A window sitting behind another cannot
                            // be dropped into at all — its guides live in its own
                            // window, so while it is occluded they are invisible and
                            // the area they cover belongs to whatever is on top.
                            // Guarded on "changed" so a stationary pointer does not
                            // re-issue SetWindowPos on every move event.
                            if (topWindow != IntPtr.Zero && topWindow != _raisedWindow)
                            {
                                LogDragState($"raise hwnd={topWindow.ToInt64():x} "
                                             + $"(pointer {point.X:F0},{point.Y:F0} -> screen "
                                             + $"{pointerScreenPoint.X:F0},{pointerScreenPoint.Y:F0})");
                                Extensions.RaiseWindow(topWindow);
                                _raisedWindow = topWindow;
                            }

                            foreach (var inputDockControl in dockControls.GetZOrderedDockControls(topWindow))
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
                                    if (fromWindow is null || toWindow is null)
                                        continue;

                                    // What the setting forbids is dropping INTO a float
                                    // window, and HostWindow is exactly that type. Asking
                                    // "is it the main window" instead also rejects every
                                    // other dock-hosting window an application may open.
                                    if (!DockSettings.DockBetweenFloatWindows && toWindow is HostWindow)
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
                                // Suspect path: a single move with no drop control tears
                                // the adorner down and clears TargetDockControl, and a
                                // release right after lands in the "drag out" branch.
                                LogDragState($"NO drop control at {point.X:F0},{point.Y:F0} "
                                             + $"overDockControl={isOverDockControl} (adorner torn down)");

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
                        _raisedWindow = IntPtr.Zero;
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

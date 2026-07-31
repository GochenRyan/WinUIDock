using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = TopIndicatorPartName, Type = typeof(Grid))]
    [TemplatePart(Name = BottomIndicatorPartName, Type = typeof(Grid))]
    [TemplatePart(Name = LeftIndicatorPartName, Type = typeof(Grid))]
    [TemplatePart(Name = RightIndicatorPartName, Type = typeof(Grid))]
    [TemplatePart(Name = CenterIndicatorPartName, Type = typeof(Grid))]
    [TemplatePart(Name = TopSelectorPartName, Type = typeof(DockTargetGuide))]
    [TemplatePart(Name = BottomSelectorPartName, Type = typeof(DockTargetGuide))]
    [TemplatePart(Name = LeftSelectorPartName, Type = typeof(DockTargetGuide))]
    [TemplatePart(Name = RightSelectorPartName, Type = typeof(DockTargetGuide))]
    [TemplatePart(Name = CenterSelectorPartName, Type = typeof(DockTargetGuide))]
    [TemplatePart(Name = RootTopSelectorPartName, Type = typeof(DockTargetGuide))]
    [TemplatePart(Name = RootBottomSelectorPartName, Type = typeof(DockTargetGuide))]
    [TemplatePart(Name = RootLeftSelectorPartName, Type = typeof(DockTargetGuide))]
    [TemplatePart(Name = RootRightSelectorPartName, Type = typeof(DockTargetGuide))]
    [TemplatePart(Name = ClusterPlatePartName, Type = typeof(Border))]
    public sealed class DockTarget : Control
    {
        public const string ClusterPlatePartName = "PART_ClusterPlate";
        public const string TopIndicatorPartName = "PART_TopIndicator";
        public const string BottomIndicatorPartName = "PART_BottomIndicator";
        public const string LeftIndicatorPartName = "PART_LeftIndicator";
        public const string RightIndicatorPartName = "PART_RightIndicator";
        public const string CenterIndicatorPartName = "PART_CenterIndicator";
        public const string TopSelectorPartName = "PART_TopSelector";
        public const string BottomSelectorPartName = "PART_BottomSelector";
        public const string LeftSelectorPartName = "PART_LeftSelector";
        public const string RightSelectorPartName = "PART_RightSelector";
        public const string CenterSelectorPartName = "PART_CenterSelector";
        public const string RootTopSelectorPartName = "PART_RootTopSelector";
        public const string RootBottomSelectorPartName = "PART_RootBottomSelector";
        public const string RootLeftSelectorPartName = "PART_RootLeftSelector";
        public const string RootRightSelectorPartName = "PART_RootRightSelector";

        public double LocalX
        {
            get => (double)GetValue(LocalXProperty);
            set => SetValue(LocalXProperty, value);
        }
        public static readonly DependencyProperty LocalXProperty =
            DependencyProperty.Register(nameof(LocalX), typeof(double), typeof(DockTarget), new PropertyMetadata(0d));

        public double LocalY
        {
            get => (double)GetValue(LocalYProperty);
            set => SetValue(LocalYProperty, value);
        }
        public static readonly DependencyProperty LocalYProperty =
            DependencyProperty.Register(nameof(LocalY), typeof(double), typeof(DockTarget), new PropertyMetadata(0d));

        public double LocalWidth
        {
            get => (double)GetValue(LocalWidthProperty);
            set => SetValue(LocalWidthProperty, value);
        }
        public static readonly DependencyProperty LocalWidthProperty =
            DependencyProperty.Register(nameof(LocalWidth), typeof(double), typeof(DockTarget), new PropertyMetadata(0d));

        public double LocalHeight
        {
            get => (double)GetValue(LocalHeightProperty);
            set => SetValue(LocalHeightProperty, value);
        }
        public static readonly DependencyProperty LocalHeightProperty =
            DependencyProperty.Register(nameof(LocalHeight), typeof(double), typeof(DockTarget), new PropertyMetadata(0d));

        public DockTarget()
        {
            this.DefaultStyleKey = typeof(DockTarget);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _topIndicator = GetTemplateChild(TopIndicatorPartName) as Grid;
            _bottomIndicator = GetTemplateChild(BottomIndicatorPartName) as Grid;
            _leftIndicator = GetTemplateChild(LeftIndicatorPartName) as Grid;
            _rightIndicator = GetTemplateChild(RightIndicatorPartName) as Grid;
            _centerIndicator = GetTemplateChild(CenterIndicatorPartName) as Grid;

            _topSelector = GetTemplateChild(TopSelectorPartName) as FrameworkElement;
            _bottomSelector = GetTemplateChild(BottomSelectorPartName) as FrameworkElement;
            _leftSelector = GetTemplateChild(LeftSelectorPartName) as FrameworkElement;
            _rightSelector = GetTemplateChild(RightSelectorPartName) as FrameworkElement;
            _centerSelector = GetTemplateChild(CenterSelectorPartName) as FrameworkElement;

            _rootTopSelector = GetTemplateChild(RootTopSelectorPartName) as FrameworkElement;
            _rootBottomSelector = GetTemplateChild(RootBottomSelectorPartName) as FrameworkElement;
            _rootLeftSelector = GetTemplateChild(RootLeftSelectorPartName) as FrameworkElement;
            _rootRightSelector = GetTemplateChild(RootRightSelectorPartName) as FrameworkElement;

            _clusterPlate = GetTemplateChild(ClusterPlatePartName) as Border;

            // Root guides sit against the window edges; the inset is a metric key
            // (kept in code so the overlap math below reads the same value).
            var edgeInset = DockMetrics.GetDouble("DockGuideEdgeInset", 16.0);
            if (_rootTopSelector is not null)
            {
                _rootTopSelector.Margin = new Thickness(0, edgeInset, 0, 0);
            }

            if (_rootBottomSelector is not null)
            {
                _rootBottomSelector.Margin = new Thickness(0, 0, 0, edgeInset);
            }

            if (_rootLeftSelector is not null)
            {
                _rootLeftSelector.Margin = new Thickness(edgeInset, 0, 0, 0);
            }

            if (_rootRightSelector is not null)
            {
                _rootRightSelector.Margin = new Thickness(0, 0, edgeInset, 0);
            }

            // Drop previews fade in/out (alpha lives in the fill brush).
            foreach (var indicator in new[] { _topIndicator, _bottomIndicator, _leftIndicator, _rightIndicator, _centerIndicator })
            {
                if (indicator != null)
                {
                    indicator.OpacityTransition = new ScalarTransition { Duration = System.TimeSpan.FromMilliseconds(120) };
                }
            }
        }

        internal DockOperation GetDockOperation(Point point, FrameworkElement relativeTo, DragAction dragAction, IDockable? sourceDockable, IDockable? targetDockable, Func<Point, DockOperation, DragAction, FrameworkElement, bool> validate)
        {
            var result = DockOperation.Window;
            var baseValid = validate(point, DockOperation.Fill, dragAction, relativeTo);
            var sourceRootOperation = sourceDockable is null ? null : GetSourceRootOperation(sourceDockable);

            UpdateSelectorVisibility(sourceDockable, targetDockable, baseValid, sourceRootOperation);

            if (InvalidateIndicator(_rootLeftSelector, _leftIndicator, point, relativeTo, DockOperation.RootLeft, dragAction, sourceDockable, sourceRootOperation, validate))
            {
                result = DockOperation.RootLeft;
            }

            if (InvalidateIndicator(_rootRightSelector, _rightIndicator, point, relativeTo, DockOperation.RootRight, dragAction, sourceDockable, sourceRootOperation, validate))
            {
                result = DockOperation.RootRight;
            }

            if (InvalidateIndicator(_rootTopSelector, _topIndicator, point, relativeTo, DockOperation.RootTop, dragAction, sourceDockable, sourceRootOperation, validate))
            {
                result = DockOperation.RootTop;
            }

            if (InvalidateIndicator(_rootBottomSelector, _bottomIndicator, point, relativeTo, DockOperation.RootBottom, dragAction, sourceDockable, sourceRootOperation, validate))
            {
                result = DockOperation.RootBottom;
            }

            if (InvalidateIndicator(_leftSelector, _leftIndicator, point, relativeTo, DockOperation.Left, dragAction, sourceDockable, sourceRootOperation, validate))
            {
                result = DockOperation.Left;
            }

            if (InvalidateIndicator(_rightSelector, _rightIndicator, point, relativeTo, DockOperation.Right, dragAction, sourceDockable, sourceRootOperation, validate))
            {
                result = DockOperation.Right;
            }

            if (InvalidateIndicator(_topSelector, _topIndicator, point, relativeTo, DockOperation.Top, dragAction, sourceDockable, sourceRootOperation, validate))
            {
                result = DockOperation.Top;
            }

            if (InvalidateIndicator(_bottomSelector, _bottomIndicator, point, relativeTo, DockOperation.Bottom, dragAction, sourceDockable, sourceRootOperation, validate))
            {
                result = DockOperation.Bottom;
            }

            if (InvalidateIndicator(_centerSelector, _centerIndicator, point, relativeTo, DockOperation.Fill, dragAction, sourceDockable, sourceRootOperation, validate))
            {
                result = DockOperation.Fill;
            }

            return result;
        }

        internal void UpdateRootVisibility(IDockable? sourceDockable)
        {
            var sourceRootOperation = sourceDockable is null ? null : GetSourceRootOperation(sourceDockable);
            var rootDock = GetRootDock(sourceDockable);

            if (sourceDockable is Document)
            {
                SetRootSelectorVisibility(Visibility.Collapsed, sourceRootOperation, rootDock);
            }
            else
            {
                SetRootSelectorVisibility(Visibility.Visible, sourceRootOperation, rootDock);
            }

            SetLocalSelectorVisibility(Visibility.Collapsed);
        }

        private bool InvalidateIndicator(FrameworkElement selector, FrameworkElement indicator, Point point, FrameworkElement relativeTo, DockOperation operation, DragAction dragAction, IDockable? sourceDockable, DockOperation? sourceRootOperation, Func<Point, DockOperation, DragAction, FrameworkElement, bool> validate)
        {
            if (selector is null || indicator is null)
            {
                return false;
            }

            var show = false;

            // Selectors are composite (a root shape with vector children), so accept a hit
            // on the selector itself or on any of its descendants.
            if (VisualTreeHelper.FindElementsInHostCoordinates(point, this, true) is { } inputElements
                && inputElements.Any(el => IsSelfOrDescendantOf(el, selector)))
            {
                var isValid = validate(point, operation, dragAction, relativeTo);
                show = ShouldShowIndicator(isValid, sourceDockable, operation, sourceRootOperation);
            }

            indicator.Opacity = show ? 1 : 0;
            if (selector is DockTargetGuide guide)
            {
                guide.IsHighlighted = show;
            }

            return show;
        }

        private static bool IsSelfOrDescendantOf(DependencyObject element, FrameworkElement selector)
        {
            var current = element;
            while (current != null)
            {
                if (ReferenceEquals(current, selector))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static DockOperation? GetSourceRootOperation(IDockable sourceDockable)
        {
            var factory = sourceDockable.Factory ?? sourceDockable.Owner?.Factory;
            if (factory is null)
            {
                return null;
            }

            var rootDock = factory.FindRoot(sourceDockable, _ => true);
            if (rootDock is null)
            {
                return null;
            }

            if (rootDock.RootLeftDock is { } rootLeftDock && factory.FindDockable(rootLeftDock, dockable => ReferenceEquals(dockable, sourceDockable)) is not null)
            {
                return DockOperation.RootLeft;
            }

            if (rootDock.RootRightDock is { } rootRightDock && factory.FindDockable(rootRightDock, dockable => ReferenceEquals(dockable, sourceDockable)) is not null)
            {
                return DockOperation.RootRight;
            }

            if (rootDock.RootTopDock is { } rootTopDock && factory.FindDockable(rootTopDock, dockable => ReferenceEquals(dockable, sourceDockable)) is not null)
            {
                return DockOperation.RootTop;
            }

            if (rootDock.RootBottomDock is { } rootBottomDock && factory.FindDockable(rootBottomDock, dockable => ReferenceEquals(dockable, sourceDockable)) is not null)
            {
                return DockOperation.RootBottom;
            }

            return null;
        }

        private static bool IsRootOperation(DockOperation operation)
        {
            return operation is DockOperation.RootLeft or DockOperation.RootRight or DockOperation.RootTop or DockOperation.RootBottom;
        }

        private static bool ShouldShowIndicator(bool isValid, IDockable? sourceDockable, DockOperation operation, DockOperation? sourceRootOperation)
        {
            if (sourceDockable is Document)
            {
                return !IsRootOperation(operation) && isValid;
            }

            if (sourceDockable is IDock)
            {
                if (IsRootOperation(operation))
                {
                    return sourceRootOperation != operation;
                }

                return isValid;
            }

            return isValid;
        }

        private void UpdateSelectorVisibility(IDockable? sourceDockable, IDockable? targetDockable, bool isValid, DockOperation? sourceRootOperation)
        {
            var rootDock = GetRootDock(targetDockable);
            var rootVisibility = isValid ? Visibility.Visible : Visibility.Collapsed;
            if (sourceDockable is Document)
            {
                SetRootSelectorVisibility(Visibility.Collapsed, sourceRootOperation, rootDock);
                SetLocalSelectorVisibility(isValid ? Visibility.Visible : Visibility.Collapsed);
                return;
            }

            if (sourceDockable is IDock)
            {
                SetRootSelectorVisibility(rootVisibility, sourceRootOperation, rootDock);
                SetLocalSelectorVisibility(isValid ? Visibility.Visible : Visibility.Collapsed);
                return;
            }

            SetRootSelectorVisibility(rootVisibility, null, rootDock);
            SetLocalSelectorVisibility(isValid ? Visibility.Visible : Visibility.Collapsed);
        }

        private static IRootDock? GetRootDock(IDockable? sourceDockable)
        {
            if (sourceDockable is null)
            {
                return null;
            }

            var factory = sourceDockable.Factory ?? sourceDockable.Owner?.Factory;
            if (factory is null)
            {
                return null;
            }

            return factory.FindRoot(sourceDockable, _ => true);
        }

        private static bool IsRootDockAvailable(IDock? dock)
        {
            return dock is not null && Math.Abs(dock.Proportion) > 0.001;
        }

        private void SetRootSelectorVisibility(Visibility visibility, DockOperation? sourceRootOperation, IRootDock? rootDock)
        {
            var rootLeftAvailable = IsRootDockAvailable(rootDock?.RootLeftDock);
            var rootRightAvailable = IsRootDockAvailable(rootDock?.RootRightDock);
            var rootTopAvailable = IsRootDockAvailable(rootDock?.RootTopDock);
            var rootBottomAvailable = IsRootDockAvailable(rootDock?.RootBottomDock);

            SetSelectorVisibility(_rootLeftSelector, visibility, sourceRootOperation == DockOperation.RootLeft || !rootLeftAvailable);
            SetSelectorVisibility(_rootRightSelector, visibility, sourceRootOperation == DockOperation.RootRight || !rootRightAvailable);
            SetSelectorVisibility(_rootTopSelector, visibility, sourceRootOperation == DockOperation.RootTop || !rootTopAvailable);
            SetSelectorVisibility(_rootBottomSelector, visibility, sourceRootOperation == DockOperation.RootBottom || !rootBottomAvailable);
        }

        private void SetLocalSelectorVisibility(Visibility visibility)
        {
            SetSelectorVisibility(_leftSelector, visibility, false);
            SetSelectorVisibility(_rightSelector, visibility, false);
            SetSelectorVisibility(_topSelector, visibility, false);
            SetSelectorVisibility(_bottomSelector, visibility, false);
            SetSelectorVisibility(_centerSelector, visibility, false);

            if (_clusterPlate != null)
            {
                _clusterPlate.Visibility = visibility;

                if (visibility == Visibility.Visible)
                {
                    UpdateClusterOffset();
                }
            }
        }

        /// <summary>
        /// Nudges the centered guide cluster clear of the window-edge root
        /// guides. A pane sitting against a window edge (e.g. the Output pane at
        /// the bottom) puts its cluster right on top of that edge's root guide.
        /// Pure geometry — derived from the metric keys and the pane rectangle,
        /// so it needs no layout pass.
        /// </summary>
        private void UpdateClusterOffset()
        {
            if (_clusterPlate is null)
            {
                return;
            }

            var guideSize = DockMetrics.GetDouble("DockGuideSize", 40.0);
            var rootSize = DockMetrics.GetDouble("DockRootGuideSize", 36.0);
            var inset = DockMetrics.GetDouble("DockGuideEdgeInset", 16.0);
            const double gap = 10.0;

            // 3x3 grid of guides with the 2px gutters declared in the template.
            var extent = guideSize * 3 + 4;
            var centerX = LocalX + LocalWidth / 2;
            var centerY = LocalY + LocalHeight / 2;
            var cluster = new Rect(centerX - extent / 2, centerY - extent / 2, extent, extent);

            var hostWidth = ActualWidth;
            var hostHeight = ActualHeight;
            double dx = 0;
            double dy = 0;

            if (IsSelectorVisible(_rootTopSelector))
            {
                var r = new Rect((hostWidth - rootSize) / 2, inset, rootSize, rootSize);
                if (Overlaps(cluster, r, gap))
                {
                    dy = Push(dy, r.Bottom + gap - cluster.Top);
                }
            }

            if (IsSelectorVisible(_rootBottomSelector))
            {
                var r = new Rect((hostWidth - rootSize) / 2, hostHeight - inset - rootSize, rootSize, rootSize);
                if (Overlaps(cluster, r, gap))
                {
                    dy = Push(dy, r.Top - gap - cluster.Bottom);
                }
            }

            if (IsSelectorVisible(_rootLeftSelector))
            {
                var r = new Rect(inset, (hostHeight - rootSize) / 2, rootSize, rootSize);
                if (Overlaps(cluster, r, gap))
                {
                    dx = Push(dx, r.Right + gap - cluster.Left);
                }
            }

            if (IsSelectorVisible(_rootRightSelector))
            {
                var r = new Rect(hostWidth - inset - rootSize, (hostHeight - rootSize) / 2, rootSize, rootSize);
                if (Overlaps(cluster, r, gap))
                {
                    dx = Push(dx, r.Left - gap - cluster.Right);
                }
            }

            // Never push the cluster outside the pane it belongs to.
            var limitX = Math.Max(0, (LocalWidth - extent) / 2);
            var limitY = Math.Max(0, (LocalHeight - extent) / 2);
            dx = Math.Clamp(dx, -limitX, limitX);
            dy = Math.Clamp(dy, -limitY, limitY);

            if (dx == 0 && dy == 0)
            {
                _clusterPlate.RenderTransform = null;
                return;
            }

            _clusterPlate.RenderTransform = new TranslateTransform { X = dx, Y = dy };
        }

        private static double Push(double current, double candidate)
        {
            return Math.Abs(candidate) > Math.Abs(current) ? candidate : current;
        }

        private static bool Overlaps(Rect a, Rect b, double gap)
        {
            b = new Rect(b.X - gap, b.Y - gap, b.Width + gap * 2, b.Height + gap * 2);
            return a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;
        }

        private static bool IsSelectorVisible(FrameworkElement selector)
        {
            return selector is { Visibility: Visibility.Visible };
        }

        private static void SetSelectorVisibility(FrameworkElement selector, Visibility visibility, bool hide)
        {
            if (selector is null)
            {
                return;
            }

            selector.Visibility = hide ? Visibility.Collapsed : visibility;
        }

        private Grid _topIndicator;
        private Grid _bottomIndicator;
        private Grid _leftIndicator;
        private Grid _rightIndicator;
        private Grid _centerIndicator;

        private FrameworkElement _topSelector;
        private FrameworkElement _bottomSelector;
        private FrameworkElement _leftSelector;
        private FrameworkElement _rightSelector;
        private FrameworkElement _centerSelector;

        private FrameworkElement _rootTopSelector;
        private FrameworkElement _rootBottomSelector;
        private FrameworkElement _rootLeftSelector;
        private FrameworkElement _rootRightSelector;

        private Border _clusterPlate;
    }
}

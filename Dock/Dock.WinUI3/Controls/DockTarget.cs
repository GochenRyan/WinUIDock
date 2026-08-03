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
    [TemplatePart(Name = RootTopIndicatorPartName, Type = typeof(Grid))]
    [TemplatePart(Name = RootBottomIndicatorPartName, Type = typeof(Grid))]
    [TemplatePart(Name = RootLeftIndicatorPartName, Type = typeof(Grid))]
    [TemplatePart(Name = RootRightIndicatorPartName, Type = typeof(Grid))]
    [TemplatePart(Name = ClusterPlatePartName, Type = typeof(Border))]
    public sealed class DockTarget : Control
    {
        /// <summary>
        /// Share of the window a freshly created edge region takes. Must match
        /// FactoryBase.RootEdgeProportion, otherwise the preview lies about where
        /// the drop will land.
        /// </summary>
        private const double RootEdgeProportion = 0.2;

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
        public const string RootTopIndicatorPartName = "PART_RootTopIndicator";
        public const string RootBottomIndicatorPartName = "PART_RootBottomIndicator";
        public const string RootLeftIndicatorPartName = "PART_RootLeftIndicator";
        public const string RootRightIndicatorPartName = "PART_RootRightIndicator";

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

        /// <summary>
        /// Rectangle of the DOCKABLE area (the DockControl) inside the adorner, which
        /// spans the whole window. The four edge guides and their previews live here
        /// rather than at the window border, because that is the region an edge drop
        /// actually carves up — the menu bar and the float window's caption are not
        /// part of it.
        /// </summary>
        public double RootX
        {
            get => (double)GetValue(RootXProperty);
            set => SetValue(RootXProperty, value);
        }
        public static readonly DependencyProperty RootXProperty =
            DependencyProperty.Register(nameof(RootX), typeof(double), typeof(DockTarget), new PropertyMetadata(0d));

        public double RootY
        {
            get => (double)GetValue(RootYProperty);
            set => SetValue(RootYProperty, value);
        }
        public static readonly DependencyProperty RootYProperty =
            DependencyProperty.Register(nameof(RootY), typeof(double), typeof(DockTarget), new PropertyMetadata(0d));

        public double RootWidth
        {
            get => (double)GetValue(RootWidthProperty);
            set => SetValue(RootWidthProperty, value);
        }
        public static readonly DependencyProperty RootWidthProperty =
            DependencyProperty.Register(nameof(RootWidth), typeof(double), typeof(DockTarget), new PropertyMetadata(0d));

        public double RootHeight
        {
            get => (double)GetValue(RootHeightProperty);
            set => SetValue(RootHeightProperty, value);
        }
        public static readonly DependencyProperty RootHeightProperty =
            DependencyProperty.Register(nameof(RootHeight), typeof(double), typeof(DockTarget), new PropertyMetadata(0d));

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



            _rootTopIndicator = GetTemplateChild(RootTopIndicatorPartName) as Grid;
            _rootBottomIndicator = GetTemplateChild(RootBottomIndicatorPartName) as Grid;
            _rootLeftIndicator = GetTemplateChild(RootLeftIndicatorPartName) as Grid;
            _rootRightIndicator = GetTemplateChild(RootRightIndicatorPartName) as Grid;

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
            foreach (var indicator in new[]
                     {
                         _topIndicator, _bottomIndicator, _leftIndicator, _rightIndicator, _centerIndicator,
                         _rootTopIndicator, _rootBottomIndicator, _rootLeftIndicator, _rootRightIndicator,
                     })
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
            var sourceRootOperation = sourceDockable is null ? null : GetSourceRootOperation(sourceDockable, targetDockable);

            // Every guide is validated for ITS OWN operation — gating them all on the
            // single Fill result would show guides for drops that get refused.
            bool IsEnabled(DockOperation operation)
                => ShouldShowIndicator(validate(point, operation, dragAction, relativeTo), sourceDockable, operation, sourceRootOperation);

            UpdateSelectorVisibility(sourceDockable, baseValid, IsEnabled);
            UpdateRootIndicatorBounds();

            // Root guides get their OWN previews. Reusing the local ones would draw
            // the preview inside the pane under the cursor, while the drop actually
            // inserts a region spanning the whole window edge.
            if (InvalidateIndicator(_rootLeftSelector, _rootLeftIndicator, point, relativeTo, DockOperation.RootLeft, dragAction, sourceDockable, sourceRootOperation, validate))
            {
                result = DockOperation.RootLeft;
            }

            if (InvalidateIndicator(_rootRightSelector, _rootRightIndicator, point, relativeTo, DockOperation.RootRight, dragAction, sourceDockable, sourceRootOperation, validate))
            {
                result = DockOperation.RootRight;
            }

            if (InvalidateIndicator(_rootTopSelector, _rootTopIndicator, point, relativeTo, DockOperation.RootTop, dragAction, sourceDockable, sourceRootOperation, validate))
            {
                result = DockOperation.RootTop;
            }

            if (InvalidateIndicator(_rootBottomSelector, _rootBottomIndicator, point, relativeTo, DockOperation.RootBottom, dragAction, sourceDockable, sourceRootOperation, validate))
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

        internal void UpdateRootVisibility(IDockable? sourceDockable, IDockable? targetDockable)
        {
            var sourceRootOperation = sourceDockable is null ? null : GetSourceRootOperation(sourceDockable, targetDockable);

            // This path runs when the pointer is over window chrome rather than a
            // dock target, so there is no validate callback to consult — the only
            // pruning available is "the source already occupies that edge".
            SetRootSelectorVisibility(
                sourceDockable is Document ? Visibility.Collapsed : Visibility.Visible,
                operation => sourceRootOperation != operation);

            // No cluster on this path, so nothing for the edge guides to dodge —
            // drop any displacement left over from the last hover.
            ResetRootGuideOffsets();

            SetLocalSelectorVisibility(Visibility.Collapsed, _ => true);
        }

        /// <summary>
        /// Sizes the four window-level previews to the slice the drop will actually
        /// carve out. Pure geometry off <see cref="Control.ActualWidth"/> /
        /// <see cref="Control.ActualHeight"/>, so it needs no layout pass — and it
        /// has to run per pointer move because the window can be resized mid-drag.
        /// </summary>
        private void UpdateRootIndicatorBounds()
        {
            SetEdgeThickness(_rootTopIndicator, _rootBottomIndicator, _rootLeftIndicator, _rootRightIndicator,
                RootWidth, RootHeight);
        }

        private static void SetEdgeThickness(Grid top, Grid bottom, Grid left, Grid right, double width, double height)
        {
            var thicknessY = height * RootEdgeProportion;
            var thicknessX = width * RootEdgeProportion;

            if (top is not null)
            {
                top.Height = thicknessY;
            }

            if (bottom is not null)
            {
                bottom.Height = thicknessY;
            }

            if (left is not null)
            {
                left.Width = thicknessX;
            }

            if (right is not null)
            {
                right.Width = thicknessX;
            }
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

        /// <summary>
        /// Which window edge the source ALREADY is, used to hide the guide that would
        /// put it back exactly where it stands. An edge drop tears the source dock
        /// down and builds a new one, losing its Id, so a no-op drop is worth refusing.
        ///
        /// Derived from the layout itself: an edge region (see
        /// <c>IFactory.SplitToRootEdge</c>) is a DIRECT child of the root layout, at
        /// the first or last position, and the layout's orientation says which pair of
        /// edges those positions mean. Nothing else counts — a pane nested one level
        /// down does not span the window, so moving it out to the edge really does
        /// change the layout.
        /// </summary>
        private static DockOperation? GetSourceRootOperation(IDockable sourceDockable, IDockable? targetDockable)
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

            // "Already at this edge" only means anything within ONE root: two windows'
            // right edges are different places.
            if (targetDockable is not null)
            {
                var targetFactory = targetDockable.Factory ?? targetDockable.Owner?.Factory;
                var targetRoot = targetFactory?.FindRoot(targetDockable, _ => true);
                if (targetRoot is not null && !ReferenceEquals(targetRoot, rootDock))
                {
                    return null;
                }
            }

            if (GetRootLayout(rootDock) is not IProportionalDock layout
                || layout.VisibleDockables is not { Count: > 0 } children)
            {
                return null;
            }

            IDockable first = null;
            IDockable last = null;

            foreach (var child in children)
            {
                if (child is IProportionalDockSplitter)
                {
                    continue;
                }

                first ??= child;
                last = child;
            }

            // Fully qualified: Microsoft.UI.Xaml.Controls has an Orientation too.
            var horizontal = layout.Orientation == global::Dock.Model.Core.Orientation.Horizontal;

            if (ReferenceEquals(first, sourceDockable))
            {
                return horizontal ? DockOperation.RootLeft : DockOperation.RootTop;
            }

            if (ReferenceEquals(last, sourceDockable))
            {
                return horizontal ? DockOperation.RootRight : DockOperation.RootBottom;
            }

            return null;
        }

        /// <summary>
        /// The single layout node a root dock hosts — mirrors FactoryBase.GetRootLayout.
        /// </summary>
        private static IDock GetRootLayout(IRootDock rootDock)
        {
            if (rootDock.VisibleDockables is not { } dockables)
            {
                return null;
            }

            if (rootDock.ActiveDockable is IDock active && dockables.Contains(active))
            {
                return active;
            }

            foreach (var dockable in dockables)
            {
                if (dockable is IDock dock)
                {
                    return dock;
                }
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
                    return sourceRootOperation != operation && isValid;
                }

                return isValid;
            }

            return isValid;
        }

        private void UpdateSelectorVisibility(IDockable? sourceDockable, bool isValid, Func<DockOperation, bool> isEnabled)
        {
            // A Document never gets the edge guides — documents belong in the
            // document area, not pinned along a window border.
            var rootVisibility = isValid && sourceDockable is not Document
                ? Visibility.Visible
                : Visibility.Collapsed;

            SetRootSelectorVisibility(rootVisibility, isEnabled);
            SetLocalSelectorVisibility(isValid ? Visibility.Visible : Visibility.Collapsed, isEnabled);
        }


        /// <summary>
        /// An edge guide no longer needs a pre-declared <c>RootXxxDock</c> to be
        /// usable: dropping there now inserts a brand new region at the root level.
        /// What decides whether it appears is simply whether that operation
        /// validates.
        ///
        /// The old availability gate (<c>RootXxxDock is null || Proportion == 0</c>)
        /// is why the top guide never appeared in DockServiceSample — TopPane has
        /// Proportion="0" — and why LevelEditor showed no edge guides at all: its
        /// layout declares none of the four properties.
        /// </summary>
        private void SetRootSelectorVisibility(Visibility visibility, Func<DockOperation, bool> isEnabled)
        {
            SetSelectorVisibility(_rootLeftSelector, visibility, !isEnabled(DockOperation.RootLeft));
            SetSelectorVisibility(_rootRightSelector, visibility, !isEnabled(DockOperation.RootRight));
            SetSelectorVisibility(_rootTopSelector, visibility, !isEnabled(DockOperation.RootTop));
            SetSelectorVisibility(_rootBottomSelector, visibility, !isEnabled(DockOperation.RootBottom));
        }

        private void SetLocalSelectorVisibility(Visibility visibility, Func<DockOperation, bool> isEnabled)
        {
            SetSelectorVisibility(_leftSelector, visibility, !isEnabled(DockOperation.Left));
            SetSelectorVisibility(_rightSelector, visibility, !isEnabled(DockOperation.Right));
            SetSelectorVisibility(_topSelector, visibility, !isEnabled(DockOperation.Top));
            SetSelectorVisibility(_bottomSelector, visibility, !isEnabled(DockOperation.Bottom));
            SetSelectorVisibility(_centerSelector, visibility, !isEnabled(DockOperation.Fill));

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
        /// Centres the guide cluster on its pane, then slides any window-edge guide
        /// it covers out of the way. Pure geometry — derived from the metric keys and
        /// the pane rectangle, so it needs no layout pass.
        ///
        /// The cluster must NOT move to dodge an edge guide. On a short pane that
        /// carries it onto the NEIGHBOURING pane, and reaching for a guide there
        /// leaves the pane the guides belong to — which changes DropControl and
        /// rebuilds the whole adorner out from under the pointer, so the guides vanish
        /// mid-reach. The edge guides move instead; they have a whole window border to
        /// slide along.
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
            var fullExtent = guideSize * 3 + 4;
            var scale = ResolveClusterScale(fullExtent, out var extent);
            var centerX = LocalX + LocalWidth / 2;
            var centerY = LocalY + LocalHeight / 2;

            // The plate is centred on the whole adorner by the template, so the
            // translate is measured from there; the clamp uses the DOCKABLE rect,
            // since a guide over the menu bar or the caption is not reachable.
            var dx = centerX - ActualWidth / 2;
            var dy = centerY - ActualHeight / 2;

            // Last resort only: an unreachable guide is worse than an off-centre one.
            var left = (ActualWidth - extent) / 2 + dx;
            var top = (ActualHeight - extent) / 2 + dy;

            if (left < RootX)
            {
                dx += RootX - left;
            }
            else if (left + extent > RootX + RootWidth)
            {
                dx -= left + extent - (RootX + RootWidth);
            }

            if (top < RootY)
            {
                dy += RootY - top;
            }
            else if (top + extent > RootY + RootHeight)
            {
                dy -= top + extent - (RootY + RootHeight);
            }

            var cluster = new Rect(
                (ActualWidth - extent) / 2 + dx,
                (ActualHeight - extent) / 2 + dy,
                extent,
                extent);

            OffsetRootGuidesAroundCluster(cluster, rootSize, inset, gap);

            if (scale >= 1.0 && dx == 0 && dy == 0)
            {
                _clusterPlate.RenderTransform = null;
                return;
            }

            // Scale first, translate second: TransformGroup multiplies in order, so
            // the offsets stay in window pixels instead of being scaled too.
            var transform = new TransformGroup();

            if (scale < 1.0)
            {
                transform.Children.Add(new ScaleTransform { ScaleX = scale, ScaleY = scale });
            }

            if (dx != 0 || dy != 0)
            {
                transform.Children.Add(new TranslateTransform { X = dx, Y = dy });
            }

            _clusterPlate.RenderTransform = transform;
        }

        /// <summary>
        /// Slides any window-edge guide the cluster covers along its own edge. The
        /// four rotate as a set — bottom to the right, right upwards, top to the
        /// left, left downwards — so the displacement reads as one deliberate motion
        /// rather than four independent jumps.
        ///
        /// Applied as a RenderTransform, which hit testing accounts for
        /// (<see cref="VisualTreeHelper.FindElementsInHostCoordinates"/>), so a moved
        /// guide is still aimable at its new spot.
        /// </summary>
        private void OffsetRootGuidesAroundCluster(Rect cluster, double rootSize, double inset, double gap)
        {
            // Guide rectangles in ADORNER coordinates: the template lays them out
            // inside the dockable rect, so every one carries the RootX/RootY offset.
            // The cluster rect is already in adorner coordinates.
            var minX = RootX + inset;
            var maxX = RootX + RootWidth - inset;
            var minY = RootY + inset;
            var maxY = RootY + RootHeight - inset;
            var midX = RootX + (RootWidth - rootSize) / 2;
            var midY = RootY + (RootHeight - rootSize) / 2;

            // Top guide slides LEFT.
            var top = new Rect(midX, minY, rootSize, rootSize);
            var topDx = Overlaps(top, cluster, gap) ? cluster.Left - gap - top.Right : 0;
            SetSelectorOffset(_rootTopSelector, Math.Min(0, Math.Max(topDx, minX - top.Left)), 0);

            // Bottom guide slides RIGHT.
            var bottom = new Rect(midX, maxY - rootSize, rootSize, rootSize);
            var bottomDx = Overlaps(bottom, cluster, gap) ? cluster.Right + gap - bottom.Left : 0;
            SetSelectorOffset(_rootBottomSelector, Math.Max(0, Math.Min(bottomDx, maxX - bottom.Right)), 0);

            // Left guide slides DOWN.
            var leftGuide = new Rect(minX, midY, rootSize, rootSize);
            var leftDy = Overlaps(leftGuide, cluster, gap) ? cluster.Bottom + gap - leftGuide.Top : 0;
            SetSelectorOffset(_rootLeftSelector, 0, Math.Max(0, Math.Min(leftDy, maxY - leftGuide.Bottom)));

            // Right guide slides UP.
            var right = new Rect(maxX - rootSize, midY, rootSize, rootSize);
            var rightDy = Overlaps(right, cluster, gap) ? cluster.Top - gap - right.Bottom : 0;
            SetSelectorOffset(_rootRightSelector, 0, Math.Min(0, Math.Max(rightDy, minY - right.Top)));
        }

        private void ResetRootGuideOffsets()
        {
            SetSelectorOffset(_rootTopSelector, 0, 0);
            SetSelectorOffset(_rootBottomSelector, 0, 0);
            SetSelectorOffset(_rootLeftSelector, 0, 0);
            SetSelectorOffset(_rootRightSelector, 0, 0);
        }

        private static void SetSelectorOffset(FrameworkElement selector, double dx, double dy)
        {
            if (selector is null)
            {
                return;
            }

            selector.RenderTransform = dx == 0 && dy == 0
                ? null
                : new TranslateTransform { X = dx, Y = dy };
        }

        /// <summary>
        /// Shrinks the cluster to fit its pane, down to a floor where a guide is
        /// still big enough to aim at. Below that floor it simply overflows — the
        /// caller clamps it into the window so every guide stays reachable.
        ///
        /// Never hides guides to make room: hiding a target the user can legitimately
        /// drop on is worse than an overlapping one, and the drop preview already says
        /// which pane wins.
        /// </summary>
        private double ResolveClusterScale(double fullExtent, out double extent)
        {
            const double margin = 8.0;      // breathing room against the pane edges
            const double minScale = 0.55;   // below this a 40px guide is under 22px

            var available = Math.Min(LocalWidth, LocalHeight) - margin * 2;
            if (available <= 0 || fullExtent <= 0)
            {
                extent = fullExtent;
                return 1.0;
            }

            var scale = Math.Clamp(available / fullExtent, minScale, 1.0);
            extent = fullExtent * scale;
            return scale;
        }

        private static bool Overlaps(Rect a, Rect b, double gap)
        {
            b = new Rect(b.X - gap, b.Y - gap, b.Width + gap * 2, b.Height + gap * 2);
            return a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;
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



        private Grid _rootTopIndicator;
        private Grid _rootBottomIndicator;
        private Grid _rootLeftIndicator;
        private Grid _rootRightIndicator;

        private Border _clusterPlate;
    }
}

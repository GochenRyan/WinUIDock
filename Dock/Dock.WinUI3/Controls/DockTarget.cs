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
    [TemplatePart(Name = TopSelectorPartName, Type = typeof(Image))]
    [TemplatePart(Name = BottomSelectorPartName, Type = typeof(Image))]
    [TemplatePart(Name = LeftSelectorPartName, Type = typeof(Image))]
    [TemplatePart(Name = RightSelectorPartName, Type = typeof(Image))]
    [TemplatePart(Name = CenterSelectorPartName, Type = typeof(Image))]
    [TemplatePart(Name = RootTopSelectorPartName, Type = typeof(Image))]
    [TemplatePart(Name = RootBottomSelectorPartName, Type = typeof(Image))]
    [TemplatePart(Name = RootLeftSelectorPartName, Type = typeof(Image))]
    [TemplatePart(Name = RootRightSelectorPartName, Type = typeof(Image))]
    public sealed class DockTarget : Control
    {
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

            _topSelector = GetTemplateChild(TopSelectorPartName) as Image;
            _bottomSelector = GetTemplateChild(BottomSelectorPartName) as Image;
            _leftSelector = GetTemplateChild(LeftSelectorPartName) as Image;
            _rightSelector = GetTemplateChild(RightSelectorPartName) as Image;
            _centerSelector = GetTemplateChild(CenterSelectorPartName) as Image;

            _rootTopSelector = GetTemplateChild(RootTopSelectorPartName) as Image;
            _rootBottomSelector = GetTemplateChild(RootBottomSelectorPartName) as Image;
            _rootLeftSelector = GetTemplateChild(RootLeftSelectorPartName) as Image;
            _rootRightSelector = GetTemplateChild(RootRightSelectorPartName) as Image;
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


            if (VisualTreeHelper.FindElementsInHostCoordinates(point, this, true) is { } inputElements && inputElements.Contains(selector))
            {
                var isValid = validate(point, operation, dragAction, relativeTo);
                if (ShouldShowIndicator(isValid, sourceDockable, operation, sourceRootOperation))
                {
                    indicator.Opacity = 0.5;
                    return true;
                }
            }

            indicator.Opacity = 0;
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

        private Image _topSelector;
        private Image _bottomSelector;
        private Image _leftSelector;
        private Image _rightSelector;
        private Image _centerSelector;

        private Image _rootTopSelector;
        private Image _rootBottomSelector;
        private Image _rootLeftSelector;
        private Image _rootRightSelector;
    }
}

using Dock.Model.Core;
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
    [TemplatePart(Name = TopIndicatorPartName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = BottomIndicatorPartName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = LeftIndicatorPartName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = RightIndicatorPartName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = CenterIndicatorPartName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = TopSelectorPartName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = BottomSelectorPartName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = LeftSelectorPartName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = RightSelectorPartName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = CenterSelectorPartName, Type = typeof(FrameworkElement))]
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

        public DockTarget()
        {
            this.DefaultStyleKey = typeof(DockTarget);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _topIndicator = GetTemplateChild(TopIndicatorPartName) as FrameworkElement;
            _bottomIndicator = GetTemplateChild(BottomIndicatorPartName) as FrameworkElement;
            _leftIndicator = GetTemplateChild(LeftIndicatorPartName) as FrameworkElement;
            _rightIndicator = GetTemplateChild(RightIndicatorPartName) as FrameworkElement;
            _centerIndicator = GetTemplateChild(CenterIndicatorPartName) as FrameworkElement;

            _topSelector = GetTemplateChild(TopSelectorPartName) as FrameworkElement;
            _bottomSelector = GetTemplateChild(BottomSelectorPartName) as FrameworkElement;
            _leftSelector = GetTemplateChild(LeftSelectorPartName) as FrameworkElement;
            _rightSelector = GetTemplateChild(RightSelectorPartName) as FrameworkElement;
            _centerSelector = GetTemplateChild(CenterSelectorPartName) as FrameworkElement;
        }

        internal DockOperation GetDockOperation(Point point, FrameworkElement relativeTo, DragAction dragAction, Func<Point, DockOperation, DragAction, FrameworkElement, bool> validate)
        {
            var result = DockOperation.Window;

            if (InvalidateIndicator(_leftSelector, _leftIndicator, point, relativeTo, DockOperation.Left, dragAction, validate))
            {
                result = DockOperation.Left;
            }

            if (InvalidateIndicator(_rightSelector, _rightIndicator, point, relativeTo, DockOperation.Right, dragAction, validate))
            {
                result = DockOperation.Right;
            }

            if (InvalidateIndicator(_topSelector, _topIndicator, point, relativeTo, DockOperation.Top, dragAction, validate))
            {
                result = DockOperation.Top;
            }

            if (InvalidateIndicator(_bottomSelector, _bottomIndicator, point, relativeTo, DockOperation.Bottom, dragAction, validate))
            {
                result = DockOperation.Bottom;
            }

            if (InvalidateIndicator(_centerSelector, _centerIndicator, point, relativeTo, DockOperation.Fill, dragAction, validate))
            {
                result = DockOperation.Fill;
            }

            return result;
        }

        private bool InvalidateIndicator(FrameworkElement selector, FrameworkElement indicator, Point point, FrameworkElement relativeTo, DockOperation operation, DragAction dragAction, Func<Point, DockOperation, DragAction, FrameworkElement, bool> validate)
        {
            if (selector is null || indicator is null)
            {
                return false;
            }

            GeneralTransform transform = relativeTo.TransformToVisual(selector);
            Point selectorPoint = transform.TransformPoint(point);

            if (selectorPoint is { })
            {
                if (VisualTreeHelper.FindElementsInHostCoordinates(selectorPoint, selector) is { } inputElements && inputElements.Contains(selector))
                {
                    if (validate(point, operation, dragAction, relativeTo))
                    {
                        indicator.Opacity = 0.5;
                        return true;
                    }
                }
            }

            indicator.Opacity = 0;
            return false;
        }

        private FrameworkElement _topIndicator;
        private FrameworkElement _bottomIndicator;
        private FrameworkElement _leftIndicator;
        private FrameworkElement _rightIndicator;
        private FrameworkElement _centerIndicator;

        private FrameworkElement _topSelector;
        private FrameworkElement _bottomSelector;
        private FrameworkElement _leftSelector;
        private FrameworkElement _rightSelector;
        private FrameworkElement _centerSelector;
    }
}

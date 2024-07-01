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


            if (VisualTreeHelper.FindElementsInHostCoordinates(point, this) is { } inputElements && inputElements.Contains(selector))
            {
                if (validate(point, operation, dragAction, relativeTo))
                {
                    indicator.Opacity = 0.5;
                    return true;
                }
            }

            indicator.Opacity = 0;
            return false;
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
    }
}

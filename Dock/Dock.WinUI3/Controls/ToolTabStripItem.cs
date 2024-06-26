using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = DragToolName, Type = typeof(StackPanel))]
    [TemplatePart(Name = TitleItemName, Type = typeof(FrameworkElement))]
    public sealed class ToolTabStripItem : ContentControl
    {
        public const string DragToolName = "PART_DragTool";
        public const string TitleItemName = "PART_TitleItem";

        public ToolTabStripItem()
        {
            this.DefaultStyleKey = typeof(ToolTabStripItem);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _dragTool = GetTemplateChild(DragToolName) as StackPanel;
            _titleItem = GetTemplateChild(TitleItemName) as FrameworkElement;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size finalSize = base.MeasureOverride(availableSize);

            _dragTool.Width = _titleItem.DesiredSize.Width + _dragTool.Spacing;

            return finalSize;
        }

        private StackPanel _dragTool;
        private FrameworkElement _titleItem;
    }
}

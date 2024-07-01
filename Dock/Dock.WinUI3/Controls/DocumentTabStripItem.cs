using Dock.Model.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = BorderName, Type = typeof(Border))]
    [TemplatePart(Name = DragToolName, Type = typeof(StackPanel))]
    [TemplatePart(Name = TitleItemName, Type = typeof(TextBlock))]
    [TemplatePart(Name = CloseButtonName, Type = typeof(Button))]
    public sealed class DocumentTabStripItem : Control
    {
        public const string BorderName = "PART_Border";
        public const string DragToolName = "PART_DragTool";
        public const string TitleItemName = "PART_TitleItem";
        public const string CloseButtonName = "PART_CloseButton";

        public DocumentTabStripItem()
        {
            this.DefaultStyleKey = typeof(DocumentTabStripItem);
            Unloaded += DocumentTabStripItem_Unloaded;
        }

        private void DocumentTabStripItem_Unloaded(object sender, RoutedEventArgs e)
        {
            _titleItem.PointerPressed -= _titleItem_PointerPressed;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _border = GetTemplateChild(BorderName) as Border;
            _dragTool = GetTemplateChild(DragToolName) as StackPanel;
            _titleItem = GetTemplateChild(TitleItemName) as TextBlock;
            _closeButton = GetTemplateChild(CloseButtonName) as Button;

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            _titleItem.PointerPressed += _titleItem_PointerPressed;

            _titleItem.SetBinding(TextBlock.TextProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("Title"),
                Mode = BindingMode.OneWay
            });

            _closeButton.SetBinding(Button.CommandProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("Owner.Factory.CloseDockableCmd"),
                Mode = BindingMode.OneWay
            });

            _closeButton.SetBinding(Button.CommandParameterProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath(""),
                Mode = BindingMode.OneWay
            });

            _closeButton.SetBinding(Button.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("CanClose"),
                Mode = BindingMode.OneWay
            });
        }

        private void _titleItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (DataContext != null)
            {
                IDockable dockable = (IDockable)DataContext;
                dockable?.Owner?.Factory?.SetActiveDockable(dockable);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size finalSize = base.MeasureOverride(availableSize);

            _dragTool.Width = _titleItem.DesiredSize.Width + _closeButton.DesiredSize.Width + _dragTool.Spacing * 2;
            _border.Width = _dragTool.Width;

            return finalSize;
        }

        private Border _border;
        private StackPanel _dragTool;
        private TextBlock _titleItem;
        private Button _closeButton;
    }
}

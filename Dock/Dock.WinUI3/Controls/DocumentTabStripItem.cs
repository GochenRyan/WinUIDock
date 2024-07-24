using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.WinUI3.Internal;
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

        public const string FloatItemName = "PART_FloatItem";
        public const string CloseSelfItemName = "PART_CloseSelfItem";
        public const string CloseOtherItemName = "PART_CloseOtherItem";
        public const string CloseAllItemName = "PART_CloseAllItem";
        public const string CloseLeftItemName = "PART_CloseLeftItem";
        public const string CloseRightItemName = "PART_CloseRightItem";

        public DocumentTabStripItem()
        {
            this.DefaultStyleKey = typeof(DocumentTabStripItem);
            Loaded += DocumentTabStripItem_Loaded;
            Unloaded += DocumentTabStripItem_Unloaded;
        }

        private void DocumentTabStripItem_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += DocumentTabStripItem_DataContextChanged;
        }

        private void DocumentTabStripItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
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
            if (DataContext is IDocument)
            {
                _titleItem.PointerPressed -= _titleItem_PointerPressed;
                _titleItem.PointerPressed += _titleItem_PointerPressed;

                _titleItem.ClearValue(TextBlock.TextProperty);
                _titleItem.SetBinding(TextBlock.TextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("Title"),
                    Mode = BindingMode.OneWay
                });

                _closeButton.ClearValue(Button.CommandProperty);
                _closeButton.SetBinding(Button.CommandProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("Owner.Factory.CloseDockableCmd"),
                    Mode = BindingMode.OneWay
                });

                _closeButton.ClearValue(Button.CommandParameterProperty);
                _closeButton.SetBinding(Button.CommandParameterProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath(""),
                    Mode = BindingMode.OneWay
                });

                _closeButton.ClearValue(Button.VisibilityProperty);
                _closeButton.SetBinding(Button.VisibilityProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("CanClose"),
                    Converter = DockConverters.DockBoolToVisibilityConverter,
                    Mode = BindingMode.OneWay
                });

                var menuFlyout = new MenuFlyout();
                menuFlyout.XamlRoot = this.XamlRoot;
                var floatItem = CreateMenuFlyoutItem(FloatItemName, "Float", "Owner.Factory.FloatDockableCmd", "CanFloat");
                var closeSelfItem = CreateMenuFlyoutItem(CloseSelfItemName, "Close", "Owner.Factory.CloseDockableCmd", "CanClose");
                var closeOtherItem = CreateMenuFlyoutItem(CloseOtherItemName, "Close other tabs", "Owner.Factory.CloseOtherDockablesCmd", "CanClose");
                var closeAllItem = CreateMenuFlyoutItem(CloseAllItemName, "Close all tabs", "Owner.Factory.CloseAllDockablesCmd", "CanClose");
                var closeLeftItem = CreateMenuFlyoutItem(CloseLeftItemName, "Close tabs to the left", "Owner.Factory.CloseLeftDockablesCmd", "CanClose");
                var closeRightItem = CreateMenuFlyoutItem(CloseRightItemName, "Close tabs to the right", "Owner.Factory.CloseRightDockablesCmd", "CanClose");

                menuFlyout.Items.Add(floatItem);
                menuFlyout.Items.Add(closeSelfItem);
                menuFlyout.Items.Add(closeOtherItem);
                menuFlyout.Items.Add(closeAllItem);
                menuFlyout.Items.Add(closeLeftItem);
                menuFlyout.Items.Add(closeRightItem);

                _border.ContextFlyout = menuFlyout;
            }
        }

        private MenuFlyoutItem CreateMenuFlyoutItem(string name, string text, string cmdPath, string visibilityPath)
        {
            var item = new MenuFlyoutItem
            {
                Name = name,
                Text = text
            };
            item.SetBinding(MenuFlyoutItem.CommandProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath(cmdPath),
                Mode = BindingMode.OneWay
            });
            item.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath(""),
                Mode = BindingMode.OneWay
            });
            item.SetBinding(MenuFlyoutItem.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath(visibilityPath),
                Converter = DockConverters.DockBoolToVisibilityConverter,
                Mode = BindingMode.OneWay,
                FallbackValue = Visibility.Collapsed
            });

            return item;
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
            _border.Width = _dragTool.Width + _border.Padding.Left + _border.Padding.Right;

            return finalSize;
        }

        private Border _border;
        private StackPanel _dragTool;
        private TextBlock _titleItem;
        private Button _closeButton;
    }
}

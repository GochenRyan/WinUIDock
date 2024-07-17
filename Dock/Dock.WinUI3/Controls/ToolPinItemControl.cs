using CommunityToolkit.WinUI.Controls;
using Dock.Model.WinUI3.Controls;
using Dock.WinUI3.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = PreviewPinnedBtnName, Type = typeof(Button))]
    [TemplatePart(Name = PreviewPinnedTextName, Type = typeof(TextBlock))]
    [TemplatePart(Name = LayoutTransformControlName, Type = typeof(LayoutTransformControl))]
    public sealed class ToolPinItemControl : Control
    {

        public const string PreviewPinnedBtnName = "PART_PreviewPinnedBtn";
        public const string PreviewPinnedTextName = "PART_PreviewPinnedText";
        public const string LayoutTransformControlName = "PART_LayoutTransformControl";

        public const string FloatItemName = "PART_FloatItem";
        public const string ShowItemName = "PART_ShowItem";
        public const string CloseItemName = "PART_CloseItem";

        public ToolPinItemControl()
        {
            this.DefaultStyleKey = typeof(ToolPinItemControl);
        }

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(ToolPinItemControl),
            new PropertyMetadata(Orientation.Vertical, OnOrientationChanged));

        public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

        private static void OnOrientationChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            var control = ob as ToolPinItemControl;
            Orientation orientation = (Orientation)args.NewValue;
            control.ChangeOrientation(orientation);
        }

        private void ChangeOrientation(Orientation orientation)
        {
            switch (orientation)
            {
                case Orientation.Horizontal:
                    _layoutTransformControl.Transform = new RotateTransform { Angle = 0 };
                    break;
                case Orientation.Vertical:
                    _layoutTransformControl.Transform = new RotateTransform { Angle = 270 };
                    break;
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _previewPinnedBtn = GetTemplateChild(PreviewPinnedBtnName) as Button;
            _previewPinnedText = GetTemplateChild(PreviewPinnedTextName) as TextBlock;
            _layoutTransformControl = GetTemplateChild(LayoutTransformControlName) as LayoutTransformControl;

            var parent = VisualTreeHelper.GetParent(this) as UIElement;
            while (parent != null)
            {
                if (parent is ToolPinnedControl control)
                {
                    Orientation = control.Orientation;
                    break;
                }
                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }

            ChangeOrientation(Orientation);

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext is Tool)
            {
                _previewPinnedBtn.ClearValue(Button.CommandProperty);
                _previewPinnedBtn.SetBinding(Button.CommandProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("Owner.Factory.PreviewPinnedDockableCmd"),
                    Mode = BindingMode.OneWay
                });

                _previewPinnedBtn.ClearValue(Button.CommandParameterProperty);
                _previewPinnedBtn.SetBinding(Button.CommandParameterProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath(""),
                    Mode = BindingMode.OneWay
                });

                _previewPinnedBtn.ClearValue(Button.VisibilityProperty);
                _previewPinnedBtn.SetBinding(Button.VisibilityProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("CanPin"),
                    Converter = DockConverters.DockBoolToVisibilityConverter,
                    Mode = BindingMode.OneWay
                });

                _previewPinnedText.ClearValue(TextBlock.TextProperty);
                _previewPinnedText.SetBinding(TextBlock.TextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("Title"),
                    Mode = BindingMode.OneWay
                });

                AddFlyout();
            }
        }

        private void AddFlyout()
        {
            var menuFlyout = new MenuFlyout();
            menuFlyout.XamlRoot = this.XamlRoot;

            var floatItem = new MenuFlyoutItem
            {
                Name = FloatItemName,
                Text = "Float"
            };
            floatItem.SetBinding(MenuFlyoutItem.CommandProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("Owner.Factory.FloatDockableCmd"),
                Mode = BindingMode.OneWay
            });
            floatItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath(""),
                Mode = BindingMode.OneWay
            });
            floatItem.SetBinding(MenuFlyoutItem.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("CanFloat"),
                Converter = DockConverters.DockBoolToVisibilityConverter,
                Mode = BindingMode.OneWay,
                FallbackValue = Visibility.Collapsed
            });
            menuFlyout.Items.Add(floatItem);

            var showItem = new MenuFlyoutItem
            {
                Name = ShowItemName,
                Text = "Show"
            };
            showItem.SetBinding(MenuFlyoutItem.CommandProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("Owner.Factory.PreviewPinnedDockableCmd"),
                Mode = BindingMode.OneWay
            });
            showItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath(""),
                Mode = BindingMode.OneWay
            });
            showItem.SetBinding(MenuFlyoutItem.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("CanPin"),
                Mode = BindingMode.OneWay,
                Converter = DockConverters.DockBoolToVisibilityConverter,
                FallbackValue = Visibility.Collapsed
            });
            menuFlyout.Items.Add(showItem);

            var closeItem = new MenuFlyoutItem
            {
                Name = CloseItemName,
                Text = "Close"
            };
            closeItem.SetBinding(MenuFlyoutItem.CommandProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("Owner.Factory.CloseDockableCmd"),
                Mode = BindingMode.OneWay
            });
            closeItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath(""),
                Mode = BindingMode.OneWay
            });
            closeItem.SetBinding(MenuFlyoutItem.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("CanClose"),
                Mode = BindingMode.OneWay,
                Converter = DockConverters.DockBoolToVisibilityConverter,
                FallbackValue = Visibility.Collapsed
            });
            menuFlyout.Items.Add(closeItem);

            _previewPinnedText.ContextFlyout = menuFlyout;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = availableSize;
            switch (Orientation)
            {
                case Orientation.Horizontal:
                    size.Height = 30;
                    _previewPinnedBtn.Measure(availableSize);
                    size.Width = _previewPinnedBtn.DesiredSize.Width;
                    break;
                case Orientation.Vertical:
                    size.Width = 30;
                    _previewPinnedBtn.Measure(availableSize);
                    // Rotate
                    size.Height = _previewPinnedBtn.DesiredSize.Width;
                    break;
            }

            Size finalSize = base.MeasureOverride(size);
            return finalSize;
        }

        private Button _previewPinnedBtn;
        private TextBlock _previewPinnedText;
        private LayoutTransformControl _layoutTransformControl;
    }
}

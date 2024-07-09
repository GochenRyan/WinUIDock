using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = BorderName, Type = typeof(Border))]
    [TemplatePart(Name = MenuButtonName, Type = typeof(Button))]
    [TemplatePart(Name = GripPartName, Type = typeof(Grid))]
    [TemplatePart(Name = CloseButtonPartName, Type = typeof(Button))]
    [TemplatePart(Name = TitlePartName, Type = typeof(TextBlock))]
    [TemplatePart(Name = PinButtonPartName, Type = typeof(Button))]
    [TemplatePart(Name = MaximizeRestoreButtonPartName, Type = typeof(Button))]
    public sealed class ToolChromeControl : ContentControl
    {
        public const string BorderName = "PART_Border";
        public const string MenuButtonName = "PART_MenuButton";
        public const string GripPartName = "PART_Grip";
        public const string CloseButtonPartName = "PART_CloseButton";
        public const string TitlePartName = "PART_Title";
        public const string PinButtonPartName = "PART_PinButton";
        public const string MaximizeRestoreButtonPartName = "PART_MaximizeRestoreButton";

        public const string FloatItemName = "PART_FloatItem";
        public const string DockItemName = "PART_DockItem";
        public const string AutoHideItemName = "PART_AutoHideItem";
        public const string CloseItemName = "PART_CloseItem";

        public ToolChromeControl()
        {
            this.DefaultStyleKey = typeof(ToolChromeControl);
            Loaded += ToolChromeControl_Loaded;
            Unloaded += ToolChromeControl_Unloaded;
        }

        private void ToolChromeControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_attachedWindow != null)
            {
                HostWindowControl hostWindowControl = _attachedWindow.WindowContent as HostWindowControl;
                hostWindowControl.DetachGrip(this);
                _attachedWindow = null;
            }
        }

        private void ToolChromeControl_Loaded(object sender, RoutedEventArgs e)
        {
            AttachToWindow();
        }

        public static DependencyProperty TitleProprty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(ToolChromeControl),
            new PropertyMetadata(string.Empty));

        public static DependencyProperty IsActiveProperty = DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(ToolChromeControl),
            new PropertyMetadata(false));

        public static DependencyProperty IsPinnedProperty = DependencyProperty.Register(
            nameof(IsPinned),
            typeof(bool),
            typeof(ToolChromeControl),
            new PropertyMetadata(false));

        public static DependencyProperty IsFloatingProperty = DependencyProperty.Register(
            nameof(IsFloating),
            typeof(bool),
            typeof(ToolChromeControl),
            new PropertyMetadata(false));

        public static DependencyProperty IsMaximizedProperty = DependencyProperty.Register(
            nameof(IsMaximized),
            typeof(bool),
            typeof(ToolChromeControl),
            new PropertyMetadata(false));

        public string Title
        {
            get => (string)GetValue(TitleProprty);
            set => SetValue(TitleProprty, value);
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public bool IsPinned
        {
            get => (bool)GetValue(IsPinnedProperty);
            set => SetValue(IsPinnedProperty, value);
        }

        public bool IsFloating
        {
            get => (bool)GetValue(IsFloatingProperty);
            set => SetValue(IsFloatingProperty, value);
        }

        public bool IsMaximized
        {
            get => (bool)GetValue(IsMaximizedProperty);
            set => SetValue(IsMaximizedProperty, value);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _border = GetTemplateChild(BorderName) as Border;
            _menuButton = GetTemplateChild(MenuButtonName) as Button;
            Grip = GetTemplateChild(GripPartName) as Grid;
            CloseButton = GetTemplateChild(CloseButtonPartName) as Button;

            _title = GetTemplateChild(TitlePartName) as TextBlock;
            _pinButton = GetTemplateChild(PinButtonPartName) as Button;
            _maximizeRestoreButton = GetTemplateChild(MaximizeRestoreButtonPartName) as Button;

            BindData();

            AttachToWindow();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            _title.SetBinding(TextBlock.TextProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable.Title"),
                Mode = BindingMode.OneWay,
                FallbackValue = "TITLE"
            });

            _pinButton.SetBinding(Button.CommandProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("Owner.Factory.PinDockableCmd"),
                Mode = BindingMode.OneWay
            });
            _pinButton.SetBinding(Button.CommandParameterProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable"),
                Mode = BindingMode.OneWay
            });

            _maximizeRestoreButton.SetBinding(Button.VisibilityProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath("IsFloating"),
                Mode = BindingMode.OneWay
            });

            CloseButton.SetBinding(Button.CommandProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("Owner.Factory.CloseDockableCmd"),
                Mode = BindingMode.OneWay
            });

            CloseButton.SetBinding(Button.CommandParameterProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable"),
                Mode = BindingMode.OneWay
            });

            CloseButton.SetBinding(Button.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable.CanClose"),
                Mode = BindingMode.OneWay,
                FallbackValue = Visibility.Collapsed
            });


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
                Path = new PropertyPath("ActiveDockable"),
                Mode = BindingMode.OneWay
            });
            floatItem.SetBinding(MenuFlyoutItem.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable.CanFloat"),
                Mode = BindingMode.OneWay,
                FallbackValue = Visibility.Collapsed
            });
            menuFlyout.Items.Add(floatItem);

            var dockItem = new MenuFlyoutItem
            {
                Name = DockItemName,
                Text = "Dock"
            };
            dockItem.SetBinding(MenuFlyoutItem.CommandProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("Owner.Factory.PinDockableCmd"),
                Mode = BindingMode.OneWay
            });
            dockItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable"),
                Mode = BindingMode.OneWay
            });
            dockItem.SetBinding(MenuFlyoutItem.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable.CanPin"),
                Mode = BindingMode.OneWay,
                FallbackValue = Visibility.Collapsed
            });
            dockItem.SetBinding(MenuFlyoutItem.IsEnabledProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable.OriginalOwner"),
                Mode = BindingMode.OneWay,
                FallbackValue = false
            });
            menuFlyout.Items.Add(dockItem);

            var autoHideItem = new MenuFlyoutItem
            {
                Name = AutoHideItemName,
                Text = "Auto Hide"
            };
            autoHideItem.SetBinding(MenuFlyoutItem.CommandProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("Owner.Factory.PinDockableCmd"),
                Mode = BindingMode.OneWay
            });
            autoHideItem.SetBinding(MenuFlyoutItem.CommandParameterProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable"),
                Mode = BindingMode.OneWay
            });
            autoHideItem.SetBinding(MenuFlyoutItem.IsEnabledProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable.OriginalOwner"),
                Mode = BindingMode.OneWay,
                FallbackValue = false
            });
            menuFlyout.Items.Add(autoHideItem);

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
                Path = new PropertyPath("ActiveDockable"),
                Mode = BindingMode.OneWay
            });
            closeItem.SetBinding(MenuFlyoutItem.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable.CanClose"),
                Mode = BindingMode.OneWay,
                FallbackValue = Visibility.Collapsed
            });
            menuFlyout.Items.Add(closeItem);

            _border.ContextFlyout = menuFlyout;
            _menuButton.ContextFlyout = menuFlyout;

            //TODO: Set 'Visibility' of menuFlyout
        }

        private void AttachToWindow()
        {
            if (Grip == null)
                return;

            if (HostWindow.GetWindowForElement(this) is HostWindow window)
            {
                HostWindowControl hostWindowControl = window.WindowContent as HostWindowControl;
                hostWindowControl.AttachGrip(this);
                _attachedWindow = window;

                IsFloating = true;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        public Grid Grip { get; private set; }

        public Button CloseButton { get; private set; }

        private HostWindow _attachedWindow;
        private TextBlock _title;
        private Button _pinButton;
        private Button _maximizeRestoreButton;
        private Border _border;
        private Button _menuButton;
    }
}

using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Dock.WinUI3.Converters;
using Dock.WinUI3.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using System.Collections.ObjectModel;
using Windows.Foundation;
using WinUIEx;

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

            if (DataContext is ToolDock toolDock)
            {
                toolDock.VisibleDockables.CollectionChanged -= VisibleDockables_CollectionChanged;
            }
        }

        private void ToolChromeControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += ToolChromeControl_DataContextChanged;
            AttachToWindow();
        }

        private void ToolChromeControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        public static readonly DependencyProperty TitleProprty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(ToolChromeControl),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(ToolChromeControl),
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register(
            nameof(IsPinned),
            typeof(bool),
            typeof(ToolChromeControl),
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsFloatingProperty = DependencyProperty.Register(
            nameof(IsFloating),
            typeof(bool),
            typeof(ToolChromeControl),
            new PropertyMetadata(false, OnIsFloatingChanged));

        public static readonly DependencyProperty IsMaximizedProperty = DependencyProperty.Register(
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
            if (DataContext is ToolDock toolDock)
            {
                toolDock.VisibleDockables.CollectionChanged -= VisibleDockables_CollectionChanged;
                toolDock.VisibleDockables.CollectionChanged += VisibleDockables_CollectionChanged;

                _title.ClearValue(TextBlock.TextProperty);
                _title.SetBinding(TextBlock.TextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("ActiveDockable.Title"),
                    Mode = BindingMode.OneWay,
                    FallbackValue = "TITLE"
                });

                _pinButton.ClearValue(Button.CommandProperty);
                _pinButton.SetBinding(Button.CommandProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("Owner.Factory.PinDockableCmd"),
                    Mode = BindingMode.OneWay
                });

                _pinButton.ClearValue(Button.CommandParameterProperty);
                _pinButton.SetBinding(Button.CommandParameterProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("ActiveDockable"),
                    Mode = BindingMode.OneWay
                });

                _maximizeRestoreButton.ClearValue(Button.VisibilityProperty);
                _maximizeRestoreButton.SetBinding(Button.VisibilityProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath("IsFloating"),
                    Converter = DockConverters.DockBoolToVisibilityConverter,
                    Mode = BindingMode.OneWay
                });

                CloseButton.ClearValue(Button.CommandProperty);
                CloseButton.SetBinding(Button.CommandProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("Owner.Factory.CloseDockableCmd"),
                    Mode = BindingMode.OneWay
                });

                CloseButton.ClearValue(Button.CommandParameterProperty);
                CloseButton.SetBinding(Button.CommandParameterProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("ActiveDockable"),
                    Mode = BindingMode.OneWay
                });

                CloseButton.ClearValue(Button.VisibilityProperty);
                CloseButton.SetBinding(Button.VisibilityProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("ActiveDockable.CanClose"),
                    Converter = DockConverters.DockBoolToVisibilityConverter,
                    Mode = BindingMode.OneWay,
                    FallbackValue = Visibility.Collapsed
                });

                AddFlyout();
                _menuButton.Click += _menuButton_Click;
                _maximizeRestoreButton.Click += _maximizeRestoreButton_Click;
            }
        }

        private void VisibleDockables_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (sender is ObservableCollection<IDockable> visibleDockables)
            {
                if (visibleDockables.Count > 0)
                {
                    Visibility = Visibility.Visible;
                    return;
                }
            }

            Visibility = Visibility.Collapsed;
        }

        private void _menuButton_Click(object sender, RoutedEventArgs e)
        {
            if (_menuButton.ContextFlyout != null)
            {
                _menuButton.ContextFlyout.Placement = FlyoutPlacementMode.Bottom;
                _menuButton.ContextFlyout.ShowAt(_menuButton);
            }
        }

        private void _maximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (HostWindow.GetWindowForElement(this) is WindowEx window)
            {
                if (window.WindowState == WindowState.Maximized)
                {
                    window.WindowState = WindowState.Normal;
                }
                else
                {
                    window.WindowState = WindowState.Maximized;
                }
            }
        }

        private static void OnIsFloatingChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            var control = ob as ToolChromeControl;

            control.RefreshAutoHideItem();
            control.RefreshPinBtn();
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
                Path = new PropertyPath("ActiveDockable"),
                Mode = BindingMode.OneWay
            });

            floatItem.SetBinding(MenuFlyoutItem.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable.CanFloat"),
                Converter = DockConverters.DockBoolToVisibilityConverter,
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
                Converter = DockConverters.DockBoolToVisibilityConverter,
                Mode = BindingMode.OneWay,
                FallbackValue = Visibility.Collapsed
            });

            dockItem.SetBinding(MenuFlyoutItem.IsEnabledProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable.OriginalOwner"),
                Mode = BindingMode.OneWay,
                Converter = _objectToBoolConverter,
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
                Converter = _objectToBoolConverter,
                ConverterParameter = true,
                FallbackValue = false
            });
            _autoHideItem = autoHideItem;
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
                Converter = DockConverters.DockBoolToVisibilityConverter,
                Mode = BindingMode.OneWay,
                FallbackValue = Visibility.Collapsed
            });
            menuFlyout.Items.Add(closeItem);

            _border.ContextFlyout = menuFlyout;
            _menuButton.ContextFlyout = menuFlyout;

            RefreshAutoHideItem();
            RefreshPinBtn();
        }

        public void RefreshAutoHideItem()
        {
            if (_autoHideItem == null)
                return;

            if (IsFloating || (DataContext is ToolDock dock && (dock.ActiveDockable == null || !dock.ActiveDockable.CanPin)))
            {
                _autoHideItem.Visibility = Visibility.Collapsed;
            }
            else
            {
                _autoHideItem.Visibility = Visibility.Visible;
            }
        }

        public void RefreshPinBtn()
        {
            if (_pinButton == null)
                return;

            if (DataContext is ToolDock dock && dock.ActiveDockable != null && dock.ActiveDockable.CanPin && !IsFloating)
            {
                _pinButton.Visibility = Visibility.Visible;
            }
            else
            {
                _pinButton.Visibility = Visibility.Collapsed;
            }
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
            var size = base.MeasureOverride(availableSize);
            return size;
        }

        public Grid Grip { get; private set; }

        public Button CloseButton { get; private set; }

        private HostWindow _attachedWindow;
        private TextBlock _title;
        private Button _pinButton;
        private Button _maximizeRestoreButton;
        private Border _border;
        private Button _menuButton;

        private static ObjectToBoolConverter _objectToBoolConverter = new();
        private MenuFlyoutItem _autoHideItem;
    }
}

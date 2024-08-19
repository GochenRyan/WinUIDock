using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Dock.WinUI3.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System.Reflection.Metadata;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = DragToolName, Type = typeof(StackPanel))]
    [TemplatePart(Name = TitleItemName, Type = typeof(Button))]
    [TemplatePart(Name = BorderItemName, Type = typeof(Border))]
    [TemplateVisualState(Name = NormalState, GroupName = BorderStates)]
    [TemplateVisualState(Name = ActiveState, GroupName = BorderStates)]
    [TemplateVisualState(Name = HoverState, GroupName = BorderStates)]
    [TemplateVisualState(Name = SelectedUnfocusedState, GroupName = BorderStates)]
    public sealed class ToolTabStripItem : ContentControl
    {
        public const string DragToolName = "PART_DragTool";
        public const string TitleItemName = "PART_TitleItem";
        public const string BorderItemName = "PART_Border";

        public const string FloatItemName = "PART_FloatItem";
        public const string DockItemName = "PART_DockItem";
        public const string AutoHideItemName = "PART_AutoHideItem";
        public const string CloseItemName = "PART_CloseItem";

        public const string BorderStates = "BorderStates";
        public const string NormalState = "Normal";
        public const string ActiveState = "Active";
        public const string HoverState = "Hover";
        public const string SelectedUnfocusedState = "SelectedUnfocused";

        public ToolTabStripItem()
        {
            this.DefaultStyleKey = typeof(ToolTabStripItem);
            Loaded += ToolTabStripItem_Loaded;
            Unloaded += ToolTabStripItem_Unloaded;
        }

        private void ToolTabStripItem_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += ToolTabStripItem_DataContextChanged;
            UpdateIdleState();
        }

        private void ToolTabStripItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        private void ToolTabStripItem_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_canPinToken != 0 && DataContext is Tool tool)
                tool.UnregisterPropertyChangedCallback(Tool.CanPinProperty, _canPinToken);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _dragTool = GetTemplateChild(DragToolName) as StackPanel;
            _titleItem = GetTemplateChild(TitleItemName) as TextBlock;
            _border = GetTemplateChild(BorderItemName) as Border;

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext is Tool tool)
            {
                _titleItem.PointerPressed -= _titleItem_PointerPressed;
                _titleItem.PointerPressed += _titleItem_PointerPressed;

                _titleItem.PointerEntered -= _titleItem_PointerEntered;
                _titleItem.PointerEntered += _titleItem_PointerEntered;

                _titleItem.PointerExited -= _titleItem_PointerExited;
                _titleItem.PointerExited += _titleItem_PointerExited;

                if (_canPinToken != 0)
                    tool.UnregisterPropertyChangedCallback(Tool.CanPinProperty, _canPinToken);
                tool.RegisterPropertyChangedCallback(Tool.CanPinProperty, CanPinChangedCallback);
                RefreshAutoHideItem();

                _border.SetBinding(Border.WidthProperty, new Binding
                {
                    ElementName = DragToolName,
                    Path = new PropertyPath("Width"),
                    Mode = BindingMode.OneWay
                });

                _titleItem.SetBinding(TextBlock.TextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("Title"),
                    Mode = BindingMode.OneWay
                });
                AddFlyout();

                if (tool.Owner is ToolDock dock)
                {
                    dock.Factory.ActiveDockableChanged -= Factory_ActiveDockableChanged;
                    dock.Factory.ActiveDockableChanged += Factory_ActiveDockableChanged;
                }
            }
        }

        private void _titleItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (DataContext is IDockable dockable)
            {
                if (dockable.Owner is IDock dock && dock.ActiveDockable != dockable)
                {
                    VisualStateManager.GoToState(this, NormalState, true);
                }
            }
        }

        private void _titleItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (DataContext is IDockable dockable)
            {
                if (dockable.Owner is IDock dock && dock.ActiveDockable != dockable)
                {
                    VisualStateManager.GoToState(this, HoverState, true);
                }
            }
        }

        private void Factory_ActiveDockableChanged(object sender, Model.Core.Events.ActiveDockableChangedEventArgs e)
        {
            if (e.Dockable == null)
                return;

            if (DataContext is IDockable dockable)
            {
                if (e.Dockable.Owner == dockable.Owner)
                {
                    if (e.Dockable == dockable)
                    {
                        VisualStateManager.GoToState(this, ActiveState, true);
                    }
                    else
                    {
                        VisualStateManager.GoToState(this, NormalState, true);
                    }
                }
                else
                {
                    UpdateIdleState();
                }
            }
        }

        private void UpdateIdleState()
        {
            if (DataContext is not ITool dockable)
                return;

            if (dockable.Owner is IDock dock && dock.ActiveDockable == dockable)
            {
                VisualStateManager.GoToState(this, SelectedUnfocusedState, true);
            }
            else
            {
                VisualStateManager.GoToState(this, NormalState, true);
            }
        }

        private void _titleItem_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (DataContext != null)
            {
                IDockable dockable = (IDockable)DataContext;
                dockable?.Owner?.Factory?.SetActiveDockable(dockable);
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
                Mode = BindingMode.OneWay,
                Converter = DockConverters.DockBoolToVisibilityConverter,
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
                Path = new PropertyPath(""),
                Mode = BindingMode.OneWay
            });
            dockItem.SetBinding(MenuFlyoutItem.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("CanPin"),
                Mode = BindingMode.OneWay,
                Converter = DockConverters.DockBoolToVisibilityConverter,
                FallbackValue = Visibility.Collapsed
            });
            dockItem.SetBinding(MenuFlyoutItem.IsEnabledProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("OriginalOwner"),
                Mode = BindingMode.OneWay,
                Converter = DockConverters.DockObjectToBoolConverter,
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
                Path = new PropertyPath(""),
                Mode = BindingMode.OneWay
            });
            autoHideItem.SetBinding(MenuFlyoutItem.IsEnabledProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("OriginalOwner"),
                Mode = BindingMode.OneWay,
                Converter = DockConverters.DockObjectToBoolConverter,
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

            _border.ContextFlyout = menuFlyout;
            RefreshAutoHideItem();
        }

        public void RefreshAutoHideItem()
        {
            if (_autoHideItem == null)
                return;

            if ((DataContext is Tool tool && !tool.CanPin) || HostWindow.GetWindowForElement(this) is HostWindow)
            {
                _autoHideItem.Visibility = Visibility.Collapsed;
            }
            else
            {
                _autoHideItem.Visibility = Visibility.Visible;
            }
        }

        private void CanPinChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if (dp == Tool.CanPinProperty)
            {
                RefreshAutoHideItem();
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            Size finalSize = base.MeasureOverride(availableSize);

            _dragTool.Width = _titleItem.DesiredSize.Width + _dragTool.Spacing * 2;
            _border.Width = _dragTool.Width + _border.Padding.Left + _border.Padding.Right;

            return finalSize;
        }

        private StackPanel _dragTool;
        private TextBlock _titleItem;
        private Border _border;
        private MenuFlyoutItem _autoHideItem;

        private long _canPinToken = 0;
    }
}

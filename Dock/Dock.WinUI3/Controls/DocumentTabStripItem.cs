using Dock.Model.Controls;
using System;
using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Dock.Model.WinUI3.Core;
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
    [TemplateVisualState(Name = NormalState, GroupName = BorderStates)]
    [TemplateVisualState(Name = ActiveState, GroupName = BorderStates)]
    [TemplateVisualState(Name = HoverState, GroupName = BorderStates)]
    [TemplateVisualState(Name = SelectedUnfocusedState, GroupName = BorderStates)]
    public sealed class DocumentTabStripItem : Control
    {
        public const string BorderName = "PART_Border";
        public const string DragToolName = "PART_DragTool";
        public const string TitleItemName = "PART_TitleItem";
        public const string CloseButtonName = "PART_CloseButton";
        public const string IconItemName = "PART_IconItem";

        public const string FloatItemName = "PART_FloatItem";
        public const string CloseSelfItemName = "PART_CloseSelfItem";
        public const string CloseOtherItemName = "PART_CloseOtherItem";
        public const string CloseAllItemName = "PART_CloseAllItem";
        public const string CloseLeftItemName = "PART_CloseLeftItem";
        public const string CloseRightItemName = "PART_CloseRightItem";

        public const string BorderStates = "BorderStates";
        public const string NormalState = "Normal";
        public const string ActiveState = "Active";
        public const string HoverState = "Hover";
        public const string SelectedUnfocusedState = "SelectedUnfocused";

        public DocumentTabStripItem()
        {
            this.DefaultStyleKey = typeof(DocumentTabStripItem);
            Loaded += DocumentTabStripItem_Loaded;
            Unloaded += DocumentTabStripItem_Unloaded;
        }

        private void DocumentTabStripItem_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += DocumentTabStripItem_DataContextChanged;
            UpdateIdleState();
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
            _iconItem = GetTemplateChild(IconItemName) as IconSourceElement;

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext is IDocument document)
            {
                _titleItem.PointerPressed -= _titleItem_PointerPressed;
                _titleItem.PointerPressed += _titleItem_PointerPressed;

                _titleItem.PointerEntered -= _titleItem_PointerEntered;
                _titleItem.PointerEntered += _titleItem_PointerEntered;

                _titleItem.PointerExited -= _titleItem_PointerExited;
                _titleItem.PointerExited += _titleItem_PointerExited;

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

                UpdateIcon();
                if (document is DockableBase dockableBase)
                {
                    if (_iconToken != 0)
                        dockableBase.UnregisterPropertyChangedCallback(DockableBase.IconProperty, _iconToken);
                    _iconToken = dockableBase.RegisterPropertyChangedCallback(DockableBase.IconProperty, (_, _) => UpdateIcon());
                }

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

                if (document.Owner is DocumentDock dock)
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
            if (DataContext is not IDocument dockable)
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

        /// <summary>Icon slot: shown only when the dockable carries an
        /// IconSource; collapsed otherwise so it costs no width or spacing.</summary>
        private void UpdateIcon()
        {
            if (_iconItem is null)
            {
                return;
            }

            var iconSource = (DataContext as DockableBase)?.Icon as IconSource;
            _iconItem.IconSource = iconSource;
            _iconItem.Visibility = iconSource is null ? Visibility.Collapsed : Visibility.Visible;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size finalSize;

            try
            {
                finalSize = base.MeasureOverride(availableSize);
            }
            catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException
                                           or ArgumentException
                                           or UnauthorizedAccessException)
            {
                // Teardown-time measure of a half-dead template.
                Internal.DockDiag.Log($"DocumentTabStripItem.MeasureOverride teardown-time failure: {ex.Message}");
                return DesiredSize;
            }

            if (_dragTool is null || _titleItem is null || _closeButton is null || _border is null)
            {
                return finalSize;
            }

            var iconWidth = _iconItem?.DesiredSize.Width ?? 0;
            _dragTool.Width = _titleItem.DesiredSize.Width + _closeButton.DesiredSize.Width + iconWidth + _dragTool.Spacing * 2;
            _border.Width = _dragTool.Width + _border.Padding.Left + _border.Padding.Right;

            return finalSize;
        }

        private Border _border;
        private StackPanel _dragTool;
        private TextBlock _titleItem;
        private Button _closeButton;
        private IconSourceElement _iconItem;
        private long _iconToken;
    }
}

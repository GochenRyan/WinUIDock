using CommunityToolkit.WinUI;
using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Dock.WinUI3.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = ItemsPresenterName, Type = typeof(ItemsPresenter))]
    [TemplatePart(Name = TitleBarDragAreaName, Type = typeof(Border))]
    public sealed class ToolTabStrip : ItemsControl, IFloatTitleBarStrip
    {
        public const string ItemsPresenterName = "PART_ItemsPresenter";
        public const string TitleBarDragAreaName = "PART_TitleBarDragArea";

        public ToolTabStrip()
        {
            this.DefaultStyleKey = typeof(ToolTabStrip);
            _titleSupport = new FloatTitleStripSupport(this, "DockToolTabStripHeight", 26.0);
            Loaded += ToolTabStrip_Loaded;
            Unloaded += ToolTabStrip_Unloaded;
            PointerWheelChanged += ToolTabStrip_PointerWheelChanged;
        }

        private void ToolTabStrip_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ToolDock dock)
            {
                dock.VisibleDockables.CollectionChanged -= VisibleDockables_CollectionChanged;
            }

            _titleSupport.OnUnloaded();
        }

        private void ToolTabStrip_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ToolDock dock)
            {
                ItemsSource = dock.VisibleDockables;
            }
            DataContextChanged += ToolTabStrip_DataContextChanged;

            _titleSupport.OnLoaded();
        }

        private void ToolTabStrip_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is ToolDock dock)
            {
                ItemsSource = dock.VisibleDockables;
            }
        }

        private TabOverflowPanel TabPanel => ItemsPanelRoot as TabOverflowPanel;

        private void ToolTabStrip_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (TabPanel is not { } panel || panel.ExtentWidth <= panel.ViewportWidth)
            {
                return;
            }

            var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
            panel.Offset -= delta;
            e.Handled = true;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _titleBarDragArea = GetTemplateChild(TitleBarDragAreaName) as FrameworkElement;
            _titleSupport.OnApplyTemplate(GetTemplateChild(ItemsPresenterName) as ItemsPresenter);
        }

        private void VisibleDockables_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var visibleDockables = sender as ObservableCollection<IDockable>;
            ItemsSource = visibleDockables;
            Visibility = visibleDockables.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(IDockable),
            typeof(ToolTabStrip),
            new PropertyMetadata(null, OnSelectedItemChanged));

        public IDockable SelectedItem
        {
            get => (IDockable)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            var control = ob as ToolTabStrip;
            IDockable item = (IDockable)args.NewValue;
        }

        // ----- Float single-row chrome: the strip as the window title row -----

        /// <summary>
        /// True while this strip IS its float window's title row. Set only by
        /// HostWindowControl's title-role arbitration; never true inside the
        /// main window (strips there don't register as candidates).
        /// </summary>
        public static readonly DependencyProperty IsWindowTitleBarProperty = DependencyProperty.Register(
            nameof(IsWindowTitleBar),
            typeof(bool),
            typeof(ToolTabStrip),
            new PropertyMetadata(false, OnIsWindowTitleBarChanged));

        public bool IsWindowTitleBar
        {
            get => (bool)GetValue(IsWindowTitleBarProperty);
            set => SetValue(IsWindowTitleBarProperty, value);
        }

        private static void OnIsWindowTitleBarChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            (ob as ToolTabStrip)?.ApplyWindowTitleBar((bool)args.NewValue);
        }

        /// <summary>
        /// Promotion flips the whole PANE into title-row shape, demotion flips
        /// it back: strip metrics (via the shared support), strip placement
        /// (bottom tabs -> top title tabs) and the ToolChromeControl caption
        /// (fully redundant in the single-row chrome: title -> tabs, max/close
        /// -> system buttons, menu -> tab context flyout).
        /// </summary>
        private void ApplyWindowTitleBar(bool enabled)
        {
            _titleSupport.ApplyTitleRole(enabled);

            this.FindAscendant<ToolControl>()?.SetTabStripPlacementTop(enabled);
            this.FindAscendant<ToolChromeControl>()?.SetCaptionSuppressed(enabled);
        }

        Control IFloatTitleBarStrip.Strip => this;

        FrameworkElement IFloatTitleBarStrip.TitleBarDragArea => _titleBarDragArea;

        // Rank by the OWNING PANE's position: until promoted the strip itself
        // sits at the BOTTOM of its pane, which would lose the "top-most strip
        // wins" comparison it should win.
        FrameworkElement IFloatTitleBarStrip.RankAnchor =>
            this.FindAscendant<ToolChromeControl>()
            ?? (FrameworkElement)this.FindAscendant<ToolControl>()
            ?? this;

        void IFloatTitleBarStrip.SetTitleBarRightInset(double dips) => _titleSupport.SetRightInset(dips);

        private readonly FloatTitleStripSupport _titleSupport;
        private FrameworkElement _titleBarDragArea;
    }
}

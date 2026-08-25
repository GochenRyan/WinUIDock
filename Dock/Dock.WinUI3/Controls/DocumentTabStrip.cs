using CommunityToolkit.WinUI;
using Dock.Model.Core;
using System;
using Dock.Model.WinUI3.Controls;
using Dock.WinUI3.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = CreateButtonPartName, Type = typeof(Button))]
    [TemplatePart(Name = ItemsPresenterName, Type = typeof(ItemsPresenter))]
    [TemplatePart(Name = TitleBarDragAreaName, Type = typeof(Border))]
    public sealed class DocumentTabStrip : ItemsControl, IFloatTitleBarStrip
    {
        public const string CreateButtonPartName = "PART_CreateButton";
        public const string ItemsPresenterName = "PART_ItemsPresenter";
        public const string TitleBarDragAreaName = "PART_TitleBarDragArea";

        public DocumentTabStrip()
        {
            this.DefaultStyleKey = typeof(DocumentTabStrip);
            _titleSupport = new FloatTitleStripSupport(this, "DockDocumentTabStripHeight", 28.0);
            Loaded += DocumentTabStrip_Loaded;
            Unloaded += DocumentTabStrip_Unloaded;
            PointerWheelChanged += DocumentTabStrip_PointerWheelChanged;
        }


        private void DocumentTabStrip_Loaded(object sender, RoutedEventArgs e)
        {
            BindDock();
            BindCreateButton();
            DataContextChanged += DocumentTabStrip_DataContextChanged;
            _titleSupport.OnLoaded();
        }

        // Why a custom panel and not a ScrollViewer: see TabOverflowPanel.
        private TabOverflowPanel TabPanel => ItemsPanelRoot as TabOverflowPanel;

        private void DocumentTabStrip_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged -= DocumentTabStrip_DataContextChanged;
            UnhookActiveDockable();
            _titleSupport.OnUnloaded();
        }

        private void DocumentTabStrip_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindDock();
            BindCreateButton();
        }

        private void BindDock()
        {
            UnhookActiveDockable();

            if (DataContext is DocumentDock dock)
            {
                ItemsSource = dock.VisibleDockables;

                // A tab activated from code (new document, restore) can sit
                // outside the viewport — scroll it in.
                _boundDock = dock;
                _activeDockableToken = dock.RegisterPropertyChangedCallback(
                    DocumentDock.ActiveDockableProperty, (_, _) => BringActiveTabIntoView());
                BringActiveTabIntoView();
            }
        }

        private void UnhookActiveDockable()
        {
            try
            {
                if (_boundDock is not null && _activeDockableToken != 0)
                {
                    _boundDock.UnregisterPropertyChangedCallback(DocumentDock.ActiveDockableProperty, _activeDockableToken);
                }
            }
            catch
            {
                // Dock already torn down.
            }

            _boundDock = null;
            _activeDockableToken = 0;
        }

        private void DocumentTabStrip_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (TabPanel is not { } panel || panel.ExtentWidth <= panel.ViewportWidth)
            {
                return;
            }

            var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
            panel.Offset -= delta;
            e.Handled = true;
        }

        private void BringActiveTabIntoView()
        {
            // Deferred: right after activation the container may not exist yet.
            // Everything inside the guard: when this runs the window may be
            // mid-teardown, where even reading a DP throws.
            DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try
                {
                    if (TabPanel is not { } panel
                        || !IsLoaded
                        || XamlRoot is null
                        || _boundDock?.ActiveDockable is not { } active
                        || ContainerFromItem(active) is not UIElement container)
                    {
                        return;
                    }

                    panel.EnsureVisible(container);
                }
                catch
                {
                }
            });
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _createButton = GetTemplateChild(CreateButtonPartName) as Button;
            _titleBarDragArea = GetTemplateChild(TitleBarDragAreaName) as FrameworkElement;
            _titleSupport.OnApplyTemplate(GetTemplateChild(ItemsPresenterName) as ItemsPresenter);
            BindCreateButton();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        private void BindCreateButton()
        {
            if (_createButton is null)
            {
                return;
            }

            _createButton.ClearValue(VisibilityProperty);
            _createButton.SetBinding(VisibilityProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(CanCreateItem)),
                Converter = DockConverters.DockBoolToVisibilityConverter,
                Mode = BindingMode.OneWay
            });

            if (DataContext is DocumentDock dock)
            {
                _createButton.ClearValue(Button.CommandProperty);
                _createButton.SetBinding(Button.CommandProperty, new Binding
                {
                    Source = dock,
                    Path = new PropertyPath("CreateDocument"),
                    Mode = BindingMode.OneWay
                });
            }
        }

        private Button _createButton;
        private DocumentDock _boundDock;
        private long _activeDockableToken;
        private readonly FloatTitleStripSupport _titleSupport;
        private FrameworkElement _titleBarDragArea;

        // ----- Float single-row chrome: the strip as the window title row -----

        /// <summary>
        /// True while this strip IS its float window's title row. Set only by
        /// HostWindowControl's title-role arbitration; strips inside any other
        /// window never register as candidates and stay false.
        /// </summary>
        public static readonly DependencyProperty IsWindowTitleBarProperty = DependencyProperty.Register(
            nameof(IsWindowTitleBar),
            typeof(bool),
            typeof(DocumentTabStrip),
            new PropertyMetadata(false, OnIsWindowTitleBarChanged));

        public bool IsWindowTitleBar
        {
            get => (bool)GetValue(IsWindowTitleBarProperty);
            set => SetValue(IsWindowTitleBarProperty, value);
        }

        private static void OnIsWindowTitleBarChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            (ob as DocumentTabStrip)?._titleSupport.ApplyTitleRole((bool)args.NewValue);
        }

        Control IFloatTitleBarStrip.Strip => this;

        FrameworkElement IFloatTitleBarStrip.TitleBarDragArea => _titleBarDragArea;

        // Rank by the owning pane, symmetric with ToolTabStrip (this strip is
        // already at the top of its DocumentControl, but the pane origin is the
        // stable comparison anchor).
        FrameworkElement IFloatTitleBarStrip.RankAnchor =>
            this.FindAscendant<DocumentControl>() ?? (FrameworkElement)this;

        void IFloatTitleBarStrip.SetTitleBarRightInset(double dips) => _titleSupport.SetRightInset(dips);

        protected override void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);
        }

        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(DocumentTabStrip),
            new PropertyMetadata(false));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public static readonly DependencyProperty CanCreateItemProperty = DependencyProperty.Register(
            nameof(CanCreateItem),
            typeof(bool),
            typeof(DocumentTabStrip),
            new PropertyMetadata(false));


        public bool CanCreateItem
        {
            get => (bool)GetValue(CanCreateItemProperty);
            set => SetValue(CanCreateItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(IDockable),
            typeof(DocumentTabStrip),
            new PropertyMetadata(null, OnSelectedItemChanged));

        public IDockable SelectedItem
        {
            get => (IDockable)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            var control = ob as DocumentTabStrip;
            IDockable item = (IDockable)args.NewValue;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            try
            {
                return base.MeasureOverride(availableSize);
            }
            catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException
                                           or ArgumentException
                                           or UnauthorizedAccessException)
            {
                // Teardown-time measure of a half-dead template.
                Internal.DockDiag.Log($"DocumentTabStrip.MeasureOverride teardown-time failure: {ex.Message}");
                return DesiredSize;
            }
        }
    }
}

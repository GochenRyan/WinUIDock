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
    public sealed class DocumentTabStrip : ItemsControl
    {
        public const string CreateButtonPartName = "PART_CreateButton";

        public DocumentTabStrip()
        {
            this.DefaultStyleKey = typeof(DocumentTabStrip);
            Loaded += DocumentTabStrip_Loaded;
            Unloaded += DocumentTabStrip_Unloaded;
            PointerWheelChanged += DocumentTabStrip_PointerWheelChanged;
        }


        private void DocumentTabStrip_Loaded(object sender, RoutedEventArgs e)
        {
            BindDock();
            BindCreateButton();
            DataContextChanged += DocumentTabStrip_DataContextChanged;
        }

        // Why a custom panel and not a ScrollViewer: see TabOverflowPanel.
        private TabOverflowPanel TabPanel => ItemsPanelRoot as TabOverflowPanel;

        private void DocumentTabStrip_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged -= DocumentTabStrip_DataContextChanged;
            UnhookActiveDockable();
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

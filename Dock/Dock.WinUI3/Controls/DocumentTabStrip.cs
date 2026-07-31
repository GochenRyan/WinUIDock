using Dock.Model.Core;
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
        }


        private void DocumentTabStrip_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DocumentDock dock)
            {
                ItemsSource = dock.VisibleDockables;
            }
            BindCreateButton();
            DataContextChanged += DocumentTabStrip_DataContextChanged;
        }

        private void DocumentTabStrip_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is DocumentDock dock)
            {
                ItemsSource = dock.VisibleDockables;
            }
            BindCreateButton();
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
            return base.MeasureOverride(availableSize);
        }
    }
}

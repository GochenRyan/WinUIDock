using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public sealed class DocumentTabStrip : ItemsControl
    {
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
            DataContextChanged += DocumentTabStrip_DataContextChanged;
        }

        private void DocumentTabStrip_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is DocumentDock dock)
            {
                ItemsSource = dock.VisibleDockables;
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

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

        public IDockable SelectedItem { get => (IDockable)GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

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

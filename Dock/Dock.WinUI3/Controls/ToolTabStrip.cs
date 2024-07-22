using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public sealed class ToolTabStrip : ItemsControl
    {
        public ToolTabStrip()
        {
            this.DefaultStyleKey = typeof(ToolTabStrip);
            Loaded += ToolTabStrip_Loaded;
            Unloaded += ToolTabStrip_Unloaded;
        }

        private void ToolTabStrip_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ToolDock dock)
            {
                dock.VisibleDockables.CollectionChanged -= VisibleDockables_CollectionChanged;
            }
        }

        private void ToolTabStrip_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ToolDock dock)
            {
                ItemsSource = dock.VisibleDockables;
            }
            DataContextChanged += ToolTabStrip_DataContextChanged;
        }

        private void ToolTabStrip_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is ToolDock dock)
            {
                ItemsSource = dock.VisibleDockables;
            }
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext is ToolDock dock)
            {
                //dock.VisibleDockables.CollectionChanged -= VisibleDockables_CollectionChanged;
                //dock.VisibleDockables.CollectionChanged += VisibleDockables_CollectionChanged;
            }
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
    }
}

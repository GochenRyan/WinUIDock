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
    public sealed class ProportionalDockControl : ItemsControl
    {
        public ProportionalDockControl()
        {
            this.DefaultStyleKey = typeof(ProportionalDockControl);
            Loaded += ProportionalDockControl_Loaded;
            Unloaded += ProportionalDockControl_Unloaded;
        }

        private void ProportionalDockControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProportionalDock dock)
            {
                ItemsSource = dock.VisibleDockables;
            }
            DataContextChanged += ProportionalDockControl_DataContextChanged;
        }

        private void ProportionalDockControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProportionalDock dock)
            {
                dock.VisibleDockables.CollectionChanged -= VisibleDockables_CollectionChanged;
            }
        }

        private void ProportionalDockControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is ProportionalDock dock)
            {
                ItemsSource = dock.VisibleDockables;
            }
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
            if (DataContext is ProportionalDock dock)
            {
                dock.VisibleDockables.CollectionChanged -= VisibleDockables_CollectionChanged;
                dock.VisibleDockables.CollectionChanged += VisibleDockables_CollectionChanged;
            }
        }

        private void VisibleDockables_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var visibleDockables = sender as ObservableCollection<IDockable>;
            ItemsSource = visibleDockables;
            Visibility = visibleDockables.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            InvalidateMeasure();
        }


        protected override Size MeasureOverride(Size availableSize)
        {
            var size = base.MeasureOverride(availableSize);
            return size;
        }
    }
}

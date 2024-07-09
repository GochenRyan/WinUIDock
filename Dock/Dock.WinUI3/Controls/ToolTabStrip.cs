using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

        public static DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(IDockable),
            typeof(ToolTabStrip),
            new PropertyMetadata(null, OnSelectedItemChanged));

        public IDockable SelectedItem { get => (IDockable)GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

        private static void OnSelectedItemChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            var control = ob as ToolTabStrip;
            IDockable item = (IDockable)args.NewValue;
        }
    }
}

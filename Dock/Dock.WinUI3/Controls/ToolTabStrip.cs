using Dock.Model.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public sealed class ToolTabStrip : ItemsControl
    {
        public ToolTabStrip()
        {
            this.DefaultStyleKey = typeof(ToolTabStrip);
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
        }

        protected override void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);
        }

        //protected override Size MeasureOverride(Size availableSize)
        //{
        //    Size itemSize = new(100.0, 20.0);
        //    foreach (var item in Items)
        //    {
        //        ToolTabStripItem toolTabStripItem = item as ToolTabStripItem;
        //        toolTabStripItem?.Measure(itemSize);
        //    }
        //    return availableSize;
        //}

        //protected override Size ArrangeOverride(Size finalSize)
        //{
        //    if (Items == null || Items.Count == 0)
        //    {
        //        return base.ArrangeOverride(finalSize);
        //    }

        //    var left = 0.0;
        //    var top = 0.0;

        //    var childHeight = 20.0;
        //    var childWidth = 100.0;

        //    for (var i = 0; i < Items.Count; i++)
        //    {
        //        var child = Items[i] as ToolTabStripItem;
        //        var rect = new Rect(left, top, childWidth, childHeight);
        //        child?.Arrange(rect);
        //        left += childWidth;
        //    }

        //    return finalSize;
        //}

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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using Windows.Foundation;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public sealed class ToolPinnedControl : ItemsControl
    {
        public ToolPinnedControl()
        {
            this.DefaultStyleKey = typeof(ToolPinnedControl);
        }

        protected override void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);
        }

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(ToolPinnedControl),
            new PropertyMetadata(Orientation.Vertical, OnOrientationChanged));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        private static void OnOrientationChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            var control = ob as ToolPinnedControl;
            Orientation orientation = (Orientation)args.NewValue;

            Queue<UIElement> elements = new();
            elements.Enqueue(control);
            while (elements.Count > 0)
            {
                var element = elements.Dequeue();
                var cnt = VisualTreeHelper.GetChildrenCount(element);
                for (int i = 0; i < cnt; i++)
                {
                    var child = VisualTreeHelper.GetChild(element, i);
                    if (child is ToolPinItemControl item)
                    {
                        item.Orientation = orientation;
                    }
                    else if (child is UIElement childElem)
                    {
                        elements.Enqueue(childElem);
                    }
                }
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (ItemsPanelRoot != null)
            {
                var panel = ItemsPanelRoot as WrapPanel;
                panel.Orientation = Orientation;
            }

            Size size = base.MeasureOverride(availableSize);
            return size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size size = base.ArrangeOverride(finalSize);
            return size;
        }
    }
}

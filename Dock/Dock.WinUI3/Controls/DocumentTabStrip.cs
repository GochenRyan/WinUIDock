using Dock.Model.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = WrapPanelName, Type = typeof(WrapPanel))]
    public sealed class DocumentTabStrip : ItemsControl
    {
        public const string WrapPanelName = "HorizontalWrapPanel";

        public DocumentTabStrip()
        {
            this.DefaultStyleKey = typeof(DocumentTabStrip);
            DataContextChanged += DocumentTabStrip_DataContextChanged;
        }

        private void DocumentTabStrip_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is IDock dock)
            {
                ItemsSource = dock.VisibleDockables;
            }
        }

        protected override void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);
        }

        public static DependencyProperty IsActiveProperty = DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(DocumentTabStrip),
            new PropertyMetadata(false));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public static DependencyProperty CanCreateItemProperty = DependencyProperty.Register(
            nameof(CanCreateItem),
            typeof(bool),
            typeof(DocumentTabStrip),
            new PropertyMetadata(false));


        public bool CanCreateItem
        {
            get => (bool)GetValue(CanCreateItemProperty);
            set => SetValue(CanCreateItemProperty, value);
        }

        public static DependencyProperty SelectedItemProperty = DependencyProperty.Register(
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

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }
    }
}

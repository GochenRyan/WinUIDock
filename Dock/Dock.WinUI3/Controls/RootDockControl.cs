using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public sealed class RootDockControl : ContentControl
    {
        public RootDockControl()
        {
            this.DefaultStyleKey = typeof(RootDockControl);
            Loaded += RootDockControl_Loaded;
            Loading += RootDockControl_Loading;
            LayoutUpdated += RootDockControl_LayoutUpdated;
        }

        private void RootDockControl_LayoutUpdated(object sender, object e)
        {
            //var control = (RootDockControl)sender;
            //var templateControl = control.ContentTemplateRoot;
        }

        private void RootDockControl_Loading(FrameworkElement sender, object args)
        {
            var control = (RootDockControl)sender;
            var templateControl = control.ContentTemplateRoot;
        }

        private void RootDockControl_Loaded(object sender, RoutedEventArgs e)
        {
            var control = (RootDockControl)sender;
            var templateControl = control.ContentTemplateRoot;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        protected override void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
        {
            base.OnContentTemplateChanged(oldContentTemplate, newContentTemplate);
        }

        protected override void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)
        {
            base.OnContentTemplateSelectorChanged(oldContentTemplateSelector, newContentTemplateSelector);
        }
    }
}

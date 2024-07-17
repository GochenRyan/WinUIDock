using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = DocumentControlName, Type = typeof(DocumentControl))]
    public sealed class DocumentDockControl : Control
    {
        public const string DocumentControlName = "PART_DocumentControl";

        public DocumentDockControl()
        {
            this.DefaultStyleKey = typeof(DocumentDockControl);
            Loaded += DocumentDockControl_Loaded;
        }

        private void DocumentDockControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += DocumentDockControl_DataContextChanged;
        }

        private void DocumentDockControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _documentControl = GetTemplateChild(DocumentControlName) as DocumentControl;

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            //_documentControl.SetBinding(DocumentControl.ContentProperty, new Binding
            //{
            //    Source = DataContext,
            //    Path = new PropertyPath(""),
            //    Mode = BindingMode.OneWay
            //});
        }

        private DocumentControl _documentControl;
    }
}

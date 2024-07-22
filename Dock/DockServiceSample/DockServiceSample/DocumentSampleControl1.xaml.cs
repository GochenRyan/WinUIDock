using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DockServiceSample
{
    public sealed partial class DocumentSampleControl1 : UserControl
    {
        public DocumentSampleControl1()
        {
            this.InitializeComponent();

            Name = "DocumentSampleControl1";
        }

        public string DocumentText { get; set; }
    }
}

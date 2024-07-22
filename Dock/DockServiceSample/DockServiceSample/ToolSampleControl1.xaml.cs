using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DockServiceSample
{
    public sealed partial class ToolSampleControl1 : UserControl
    {
        public ToolSampleControl1()
        {
            this.InitializeComponent();
            Name = "ToolSampleControl1";
        }

        public string ToolText { get; set; }
    }
}

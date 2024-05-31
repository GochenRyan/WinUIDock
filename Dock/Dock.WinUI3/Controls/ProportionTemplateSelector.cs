using Dock.Model.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Dock.WinUI3.Controls
{
    public class ProportionTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SplitterTemplate { get; set; }
        public DataTemplate ProportionalDockTemplate { get; set; }
        public DataTemplate DocumentDockTemplate { get; set; }

        public DataTemplate ToolDockTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is IProportionalDockSplitter)
            {
                return SplitterTemplate;
            }
            else if (item is IProportionalDock)
            {
                return ProportionalDockTemplate;
            }
            else if (item is IDocumentDock)
            {
                return DocumentDockTemplate;
            }
            else if (item is IToolDock)
            {
                return ToolDockTemplate;
            }
            else
            {
                return null;
            }
        }
    }
}

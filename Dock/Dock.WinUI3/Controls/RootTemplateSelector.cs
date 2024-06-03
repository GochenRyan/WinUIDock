using Dock.Model.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Dock.WinUI3.Controls
{
    public class RootTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ProportionalDockTemplate 
        { 
            get; 
            set; 
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is IProportionalDock)
            {
                return ProportionalDockTemplate;
            }
            else
            {
                return null;
            }
        }
    }
}

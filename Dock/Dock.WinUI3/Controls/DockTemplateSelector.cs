using Dock.Model.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Dock.WinUI3.Controls
{
    public class DockTemplateSelector : DataTemplateSelector
    {
        public DataTemplate RootDockTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is IRootDock)
            {
                return RootDockTemplate;
            }
            else
            {
                return null;
            }
        }
    }
}

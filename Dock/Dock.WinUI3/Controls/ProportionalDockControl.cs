using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public sealed class ProportionalDockControl : ItemsControl
    {
        public ProportionalDockControl()
        {
            this.DefaultStyleKey = typeof(ProportionalDockControl);
            DataContextChanged += ProportionalDockControl_DataContextChanged;
        }

        private void ProportionalDockControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is ProportionalDock dock)
            {
                ItemsSource = dock.VisibleDockables;
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var panel = ItemsPanel;
        }
    }
}

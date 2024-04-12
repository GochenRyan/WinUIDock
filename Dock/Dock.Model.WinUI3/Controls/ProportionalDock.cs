using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Core;
using Microsoft.UI.Xaml;

namespace Dock.Model.WinUI3.Controls
{
    public class ProportionalDock : DockBase, IProportionalDock
    {
        DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(ProportionalDock),
            new PropertyMetadata(default));

        public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

        public ProportionalDock()
        {
        }
    }
}

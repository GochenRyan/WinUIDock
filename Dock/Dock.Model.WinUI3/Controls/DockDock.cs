using Dock.Model.Controls;
using Dock.Model.WinUI3.Core;
using Microsoft.UI.Xaml;

namespace Dock.Model.WinUI3.Controls
{
    public class DockDock : DockBase, IDockDock
    {
        public bool LastChildFill { get => (bool)GetValue(LastChildFillProperty); set => SetValue(LastChildFillProperty, value); }

        DependencyProperty LastChildFillProperty = DependencyProperty.Register(
            nameof(LastChildFill),
            typeof(bool),
            typeof(DockDock),
            new PropertyMetadata(true));
    }
}

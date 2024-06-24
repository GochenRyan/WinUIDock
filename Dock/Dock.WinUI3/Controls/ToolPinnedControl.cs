using Dock.Model.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public sealed class ToolPinnedControl : Control
    {
        public ToolPinnedControl()
        {
            this.DefaultStyleKey = typeof(ToolPinnedControl);
        }

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(ToolPinnedControl),
            new PropertyMetadata(Orientation.Vertical));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public DependencyProperty ItemsProperty = DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<IDockable>),
            typeof(ToolPinnedControl),
            new PropertyMetadata(new List<IDockable>()));

        public ObservableCollection<IDockable> Items
        {
            get => (ObservableCollection<IDockable>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }
    }
}

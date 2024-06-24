using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dock.Model.WinUI3.Controls
{
    [ContentProperty(Name = "VisibleDockables")]
    public class DockDock : DockBase, IDockDock
    {
        public DependencyProperty VisibleDockablesProperty = DependencyProperty.Register(
            nameof(VisibleDockables),
            typeof(ObservableCollection<IDockable>),
            typeof(DockDock),
            new PropertyMetadata(new ObservableCollection<IDockable>()));

        public static DependencyProperty LastChildFillProperty = DependencyProperty.Register(
            nameof(LastChildFill),
            typeof(bool),
            typeof(DockDock),
            new PropertyMetadata(true));

        public bool LastChildFill { get => (bool)GetValue(LastChildFillProperty); set => SetValue(LastChildFillProperty, value); }
        public override ObservableCollection<IDockable> VisibleDockables
        {
            get => (ObservableCollection<IDockable>)GetValue(VisibleDockablesProperty);
            set => SetValue(VisibleDockablesProperty, value);
        }
    }
}

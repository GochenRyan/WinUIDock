using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System.Collections.Generic;

namespace Dock.Model.WinUI3.Controls
{
    [ContentProperty(Name = "VisibleDockables")]
    public class DockDock : DockBase, IDockDock
    {
        public DependencyProperty VisibleDockablesProperty = DependencyProperty.Register(
            nameof(VisibleDockables),
            typeof(IList<IDockable>),
            typeof(DocumentDock),
            new PropertyMetadata(new List<IDockable>()));

        public static DependencyProperty LastChildFillProperty = DependencyProperty.Register(
            nameof(LastChildFill),
            typeof(bool),
            typeof(DockDock),
            new PropertyMetadata(true));

        public bool LastChildFill { get => (bool)GetValue(LastChildFillProperty); set => SetValue(LastChildFillProperty, value); }
        public override IList<IDockable> VisibleDockables
        {
            get => (IList<IDockable>)GetValue(VisibleDockablesProperty);
            set
            {
                SetValue(VisibleDockablesProperty, value);
                _visibleDockables = value;
            }
        }
    }
}

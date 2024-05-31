using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System.Collections.Generic;

namespace Dock.Model.WinUI3.Controls
{
    [ContentProperty(Name = "VisibleDockables")]
    public class ProportionalDockSplitter : DockBase, IProportionalDockSplitter
    {
        public DependencyProperty VisibleDockablesProperty = DependencyProperty.Register(
            nameof(VisibleDockables),
            typeof(IList<IDockable>),
            typeof(ProportionalDockSplitter),
            new PropertyMetadata(new List<IDockable>()));

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

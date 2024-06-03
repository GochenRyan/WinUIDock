using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System.Collections.Generic;

namespace Dock.Model.WinUI3.Controls
{
    [ContentProperty(Name = "VisibleDockables")]
    public class ToolDock : DockBase, IToolDock
    {
        public DependencyProperty VisibleDockablesProperty = DependencyProperty.Register(
            nameof(VisibleDockables),
            typeof(IList<IDockable>),
            typeof(ToolDock),
            new PropertyMetadata(new List<IDockable>()));

        public static DependencyProperty AlignmentProperty = DependencyProperty.Register(
            nameof(Alignment),
            typeof(Alignment),
            typeof(ToolDock),
            new PropertyMetadata(default));

        public static DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(ToolDock),
            new PropertyMetadata(default));

        public static DependencyProperty AutoHideProperty = DependencyProperty.Register(
            nameof(AutoHide),
            typeof(bool),
            typeof(ToolDock),
            new PropertyMetadata(default));

        public static DependencyProperty GripModeProperty = DependencyProperty.Register(
            nameof(GripMode),
            typeof(GripMode),
            typeof(ToolDock),
            new PropertyMetadata(default));

        public override IList<IDockable> VisibleDockables
        {
            get => (IList<IDockable>)GetValue(VisibleDockablesProperty);
            set => SetValue(VisibleDockablesProperty, value);
        }

        public Alignment Alignment { get => (Alignment)GetValue(AlignmentProperty); set => SetValue(AlignmentProperty, value); }
        public bool IsExpanded { get => (bool)GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
        public bool AutoHide { get => (bool)GetValue(AutoHideProperty); set => SetValue(AutoHideProperty, value); }
        public GripMode GripMode { get => (GripMode)GetValue(GripModeProperty); set => SetValue(GripModeProperty, value); }
    }
}

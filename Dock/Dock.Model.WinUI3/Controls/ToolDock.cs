using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Dock.Model.WinUI3.Controls
{
    [DataContract(IsReference = true)]
    [ContentProperty(Name = "VisibleDockables")]
    public class ToolDock : DockBase, IToolDock
    {
        public ToolDock() : base()
        {
            VisibleDockables = new ObservableCollection<IDockable>();
        }

        public static readonly DependencyProperty VisibleDockablesProperty = DependencyProperty.Register(
            nameof(VisibleDockables),
            typeof(ObservableCollection<IDockable>),
            typeof(ToolDock),
            new PropertyMetadata(null));

        public static readonly DependencyProperty AlignmentProperty = DependencyProperty.Register(
            nameof(Alignment),
            typeof(Alignment),
            typeof(ToolDock),
            new PropertyMetadata(Alignment.Unset));

        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(ToolDock),
            new PropertyMetadata(true));

        public static readonly DependencyProperty AutoHideProperty = DependencyProperty.Register(
            nameof(AutoHide),
            typeof(bool),
            typeof(ToolDock),
            new PropertyMetadata(true));

        public static readonly DependencyProperty GripModeProperty = DependencyProperty.Register(
            nameof(GripMode),
            typeof(GripMode),
            typeof(ToolDock),
            new PropertyMetadata(GripMode.Visible));

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("VisibleDockables")]
        public override ObservableCollection<IDockable> VisibleDockables
        {
            get => (ObservableCollection<IDockable>)GetValue(VisibleDockablesProperty);
            set => SetValue(VisibleDockablesProperty, value);
        }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Alignment")]
        public Alignment Alignment { get => (Alignment)GetValue(AlignmentProperty); set => SetValue(AlignmentProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("IsExpanded")]
        public bool IsExpanded { get => (bool)GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("AutoHide")]
        public bool AutoHide { get => (bool)GetValue(AutoHideProperty); set => SetValue(AutoHideProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("GripMode")]
        public GripMode GripMode { get => (GripMode)GetValue(GripModeProperty); set => SetValue(GripModeProperty, value); }
    }
}

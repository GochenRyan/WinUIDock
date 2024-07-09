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
    public class ProportionalDock : DockBase, IProportionalDock
    {
        public ProportionalDock() : base()
        {
            VisibleDockables = new ObservableCollection<IDockable>();
        }

        public static readonly DependencyProperty VisibleDockablesProperty = DependencyProperty.Register(
            nameof(VisibleDockables),
            typeof(ObservableCollection<IDockable>),
            typeof(ProportionalDock),
            new PropertyMetadata(null));

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(ProportionalDock),
            new PropertyMetadata(Orientation.Horizontal));

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Orientation")]
        public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("VisibleDockables")]
        public override ObservableCollection<IDockable> VisibleDockables
        {
            get => (ObservableCollection<IDockable>)GetValue(VisibleDockablesProperty);
            set => SetValue(VisibleDockablesProperty, value);
        }
    }
}

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
    public class ProportionalDockSplitter : DockBase, IProportionalDockSplitter
    {
        public ProportionalDockSplitter() : base()
        {
            Kind = nameof(IProportionalDockSplitter);
            VisibleDockables = new ObservableCollection<IDockable>();
        }

        public static readonly DependencyProperty VisibleDockablesProperty = DependencyProperty.Register(
            nameof(VisibleDockables),
            typeof(ObservableCollection<IDockable>),
            typeof(ProportionalDockSplitter),
            new PropertyMetadata(new ObservableCollection<IDockable>()));

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("VisibleDockables")]
        public override ObservableCollection<IDockable> VisibleDockables
        {
            get => (ObservableCollection<IDockable>)GetValue(VisibleDockablesProperty);
            set => SetValue(VisibleDockablesProperty, value);
        }

        // 0 = "not explicitly set": the view layer resolves the DockSplitterThickness
        // theme metric instead. Layouts that serialized an explicit thickness keep it.
        public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
            nameof(Thickness),
            typeof(double),
            typeof(ProportionalDockSplitter),
            new PropertyMetadata(0.0));

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Thickness")]
        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }
    }
}

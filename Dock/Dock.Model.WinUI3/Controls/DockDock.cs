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
    public class DockDock : DockBase, IDockDock
    {
        public DockDock() : base()
        {
            VisibleDockables = new ObservableCollection<IDockable>();
        }

        public static readonly DependencyProperty VisibleDockablesProperty = DependencyProperty.Register(
            nameof(VisibleDockables),
            typeof(ObservableCollection<IDockable>),
            typeof(DockDock),
            new PropertyMetadata(null));

        public static readonly DependencyProperty LastChildFillProperty = DependencyProperty.Register(
            nameof(LastChildFill),
            typeof(bool),
            typeof(DockDock),
            new PropertyMetadata(true));

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("LastChildFill")]
        public bool LastChildFill { get => (bool)GetValue(LastChildFillProperty); set => SetValue(LastChildFillProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("VisibleDockables")]
        public override ObservableCollection<IDockable> VisibleDockables
        {
            get => (ObservableCollection<IDockable>)GetValue(VisibleDockablesProperty);
            set => SetValue(VisibleDockablesProperty, value);
        }
    }
}

using Dock.Model.Adapters;
using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Dock.Model.WinUI3.Internal;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Windows.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.Model.WinUI3.Core
{
    [DataContract(IsReference = true)]
    [JsonPolymorphic]
    [JsonDerivedType(typeof(DockDock), typeDiscriminator: "DockDock")]
    [JsonDerivedType(typeof(DocumentDock), typeDiscriminator: "DocumentDock")]
    [JsonDerivedType(typeof(ProportionalDock), typeDiscriminator: "ProportionalDock")]
    [JsonDerivedType(typeof(ProportionalDockSplitter), typeDiscriminator: "ProportionalDockSplitter")]
    [JsonDerivedType(typeof(RootDock), typeDiscriminator: "RootDock")]
    [JsonDerivedType(typeof(ToolDock), typeDiscriminator: "ToolDock")]
    public abstract class DockBase : DockableBase, IDock
    {
        public DockBase()
        {
            _navigateAdapter = new NavigateAdapter(this);
            GoBack = Command.Create(() => _navigateAdapter.GoBack());
            GoForward = Command.Create(() => _navigateAdapter.GoForward());
            Navigate = Command.Create(NavigateTo);
            Close = Command.Create(() => _navigateAdapter.Close());
        }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("VisibleDockables")]
        public virtual ObservableCollection<IDockable> VisibleDockables { get; set; }


        public void NavigateTo(object root)
        {
            _navigateAdapter.Navigate(root, true);
        }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("ActiveDockable")]
        public IDockable ActiveDockable
        {
            get => (IDockable)GetValue(ActiveDockableProperty);
            set
            {
                SetValue(ActiveDockableProperty, value);
                Factory?.InitActiveDockable(value, this);
            }
        }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("DefaultDockable")]
        public IDockable DefaultDockable { get => (IDockable)GetValue(DefaultDockableProperty); set => SetValue(DefaultDockableProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("FocusedDockable")]
        public IDockable FocusedDockable { get => (IDockable)GetValue(FocusedDockableProperty); set => SetValue(FocusedDockableProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Proportion")]
        public double Proportion { get => (double)GetValue(ProportionProperty); set => SetValue(ProportionProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Dock")]
        public DockMode Dock { get => (DockMode)GetValue(DockProperty); set => SetValue(DockProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("IsActive")]
        public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("IsEmpty")]
        public bool IsEmpty { get => (bool)GetValue(IsEmptyProperty); set => SetValue(IsEmptyProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("IsCollapsable")]
        public bool IsCollapsable { get => (bool)GetValue(IsCollapsableProperty); set => SetValue(IsCollapsableProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonIgnore]
        public int OpenedDockablesCount { get => (int)GetValue(OpenedDockablesCountProperty); set => SetValue(OpenedDockablesCountProperty, value); }

        [IgnoreDataMember]
        [JsonIgnore]
        public bool CanGoBack => (bool)GetValue(CanGoBackProperty);

        [IgnoreDataMember]
        [JsonIgnore]
        public bool CanGoForward => (bool)GetValue(CanGoForwardProperty);

        [IgnoreDataMember]
        [JsonIgnore]
        public ICommand GoBack { get; }

        [IgnoreDataMember]
        [JsonIgnore]
        public ICommand GoForward { get; }

        [IgnoreDataMember]
        [JsonIgnore]
        public ICommand Navigate { get; }

        [IgnoreDataMember]
        [JsonIgnore]
        public ICommand Close { get; }

        public static DependencyProperty ActiveDockableProperty = DependencyProperty.Register(
            nameof(ActiveDockable),
            typeof(IDockable),
            typeof(DockBase),
            new PropertyMetadata(default(IDockable)));

        public static DependencyProperty DefaultDockableProperty = DependencyProperty.Register(
            nameof(DefaultDockable),
            typeof(IDockable),
            typeof(DockBase),
            new PropertyMetadata(default(IDockable)));

        public static DependencyProperty FocusedDockableProperty = DependencyProperty.Register(
            nameof(FocusedDockable),
            typeof(IDockable),
            typeof(DockBase),
            new PropertyMetadata(default(IDockable)));

        public static DependencyProperty ProportionProperty = DependencyProperty.Register(
            nameof(Proportion),
            typeof(double),
            typeof(DockBase),
            new PropertyMetadata(double.NaN));

        public static DependencyProperty DockProperty = DependencyProperty.Register(
            nameof(Dock),
            typeof(DockMode),
            typeof(DockBase),
            new PropertyMetadata(DockMode.Center));

        public static DependencyProperty IsActiveProperty = DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(DockBase),
            new PropertyMetadata(default(bool)));

        public static DependencyProperty IsEmptyProperty = DependencyProperty.Register(
            nameof(IsEmpty),
            typeof(bool),
            typeof(DockBase),
            new PropertyMetadata(default(bool)));

        public static DependencyProperty IsCollapsableProperty = DependencyProperty.Register(
            nameof(IsCollapsable),
            typeof(bool),
            typeof(DockBase),
            new PropertyMetadata(true));

        public static DependencyProperty OpenedDockablesCountProperty = DependencyProperty.Register(
            nameof(OpenedDockablesCount),
            typeof(int),
            typeof(DockBase),
            new PropertyMetadata(default(int)));

        public static DependencyProperty CanGoBackProperty = DependencyProperty.Register(
            nameof(CanGoBack),
            typeof(bool),
            typeof(DockBase),
            new PropertyMetadata(default(bool)));

        public static DependencyProperty CanGoForwardProperty = DependencyProperty.Register(
            nameof(CanGoForward),
            typeof(bool),
            typeof(DockBase),
            new PropertyMetadata(default(bool)));

        internal INavigateAdapter _navigateAdapter;
    }
}

using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Core;
using Dock.Model.WinUI3.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace Dock.Model.WinUI3.Controls
{
    [DataContract(IsReference = true)]
    [ContentProperty(Name = "VisibleDockables")]
    public class RootDock : DockBase, IRootDock
    {
        public RootDock() : base()
        {
            VisibleDockables = new ObservableCollection<IDockable>();
            HiddenDockables = new ObservableCollection<IDockable>();
            LeftPinnedDockables = new ObservableCollection<IDockable>();
            RightPinnedDockables = new ObservableCollection<IDockable>();
            TopPinnedDockables = new ObservableCollection<IDockable>();
            BottomPinnedDockables = new ObservableCollection<IDockable>();
            Windows = new ObservableCollection<IDockWindow>();

            ShowWindows = Command.Create(() => _navigateAdapter.ShowWindows());
            ExitWindows = Command.Create(() => _navigateAdapter.ExitWindows());
        }

        public static readonly DependencyProperty VisibleDockablesProperty = DependencyProperty.Register(
            nameof(VisibleDockables),
            typeof(ObservableCollection<IDockable>),
            typeof(RootDock),
            new PropertyMetadata(null));

        public static readonly DependencyProperty IsFocusableRootProperty = DependencyProperty.Register(
            nameof(IsFocusableRoot),
            typeof(bool),
            typeof(RootDock),
            new PropertyMetadata(true));

        public static readonly DependencyProperty HiddenDockablesProperty = DependencyProperty.Register(
            nameof(HiddenDockables),
            typeof(ObservableCollection<IDockable>),
            typeof(RootDock),
            new PropertyMetadata(null));

        public static readonly DependencyProperty LeftPinnedDockablesProperty = DependencyProperty.Register(
            nameof(LeftPinnedDockables),
            typeof(ObservableCollection<IDockable>),
            typeof(RootDock),
            new PropertyMetadata(null));

        public static readonly DependencyProperty RightPinnedDockablesProperty = DependencyProperty.Register(
            nameof(RightPinnedDockables),
            typeof(ObservableCollection<IDockable>),
            typeof(RootDock),
            new PropertyMetadata(null));

        public static readonly DependencyProperty TopPinnedDockablesProperty = DependencyProperty.Register(
            nameof(TopPinnedDockables),
            typeof(ObservableCollection<IDockable>),
            typeof(RootDock),
            new PropertyMetadata(null));

        public static readonly DependencyProperty BottomPinnedDockablesProperty = DependencyProperty.Register(
            nameof(BottomPinnedDockables),
            typeof(ObservableCollection<IDockable>),
            typeof(RootDock),
            new PropertyMetadata(null));

        public static readonly DependencyProperty PinnedDockProperty = DependencyProperty.Register(
            nameof(PinnedDock),
            typeof(IToolDock),
            typeof(RootDock),
            new PropertyMetadata(null));

        public static readonly DependencyProperty WindowProperty = DependencyProperty.Register(
            nameof(Window),
            typeof(IDockWindow),
            typeof(RootDock),
            new PropertyMetadata(null));

        public static readonly DependencyProperty WindowsProperty = DependencyProperty.Register(
            nameof(Windows),
            typeof(ObservableCollection<IDockWindow>),
            typeof(RootDock),
            new PropertyMetadata(null));

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("VisibleDockables")]
        public override ObservableCollection<IDockable> VisibleDockables
        {
            get => (ObservableCollection<IDockable>)GetValue(VisibleDockablesProperty);
            set => SetValue(VisibleDockablesProperty, value);
        }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("IsFocusableRoot")]
        public bool IsFocusableRoot { get => (bool)GetValue(IsFocusableRootProperty); set => SetValue(IsFocusableRootProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("HiddenDockables")]
        public ObservableCollection<IDockable> HiddenDockables { get => (ObservableCollection<IDockable>)GetValue(HiddenDockablesProperty); set => SetValue(HiddenDockablesProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("LeftPinnedDockables")]
        public ObservableCollection<IDockable> LeftPinnedDockables { get => (ObservableCollection<IDockable>)GetValue(LeftPinnedDockablesProperty); set => SetValue(LeftPinnedDockablesProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("RightPinnedDockables")]
        public ObservableCollection<IDockable> RightPinnedDockables { get => (ObservableCollection<IDockable>)GetValue(RightPinnedDockablesProperty); set => SetValue(RightPinnedDockablesProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("TopPinnedDockables")]
        public ObservableCollection<IDockable> TopPinnedDockables { get => (ObservableCollection<IDockable>)GetValue(TopPinnedDockablesProperty); set => SetValue(TopPinnedDockablesProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("BottomPinnedDockables")]
        public ObservableCollection<IDockable> BottomPinnedDockables { get => (ObservableCollection<IDockable>)GetValue(BottomPinnedDockablesProperty); set => SetValue(BottomPinnedDockablesProperty, value); }

        [JsonIgnore]
        public IToolDock PinnedDock { get => (IToolDock)GetValue(PinnedDockProperty); set => SetValue(PinnedDockProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Window")]
        public IDockWindow Window { get => (IDockWindow)GetValue(WindowProperty); set => SetValue(WindowProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Windows")]
        public ObservableCollection<IDockWindow> Windows { get => (ObservableCollection<IDockWindow>)GetValue(WindowsProperty); set => SetValue(WindowsProperty, value); }

        [IgnoreDataMember]
        [JsonIgnore]
        public ICommand ShowWindows { get; }

        [IgnoreDataMember]
        [JsonIgnore]
        public ICommand ExitWindows { get; }
    }
}

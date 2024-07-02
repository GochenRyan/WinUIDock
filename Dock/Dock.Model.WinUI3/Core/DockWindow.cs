using Dock.Model.Adapters;
using Dock.Model.Controls;
using Dock.Model.Core;
using Microsoft.UI.Xaml;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.Model.WinUI3.Core
{
    [DataContract(IsReference = true)]
    public class DockWindow : DependencyObject, IDockWindow
    {
        public DockWindow()
        {
            Id = nameof(IDockWindow);
            Title = nameof(IDockWindow);
            _hostAdapter = new HostAdapter(this);
        }

        private readonly IHostAdapter _hostAdapter;

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Id")]
        public string Id { get => (string)GetValue(IDProperty); set => SetValue(IDProperty, value); }

        [DataMember(IsRequired = true, EmitDefaultValue = true)]
        [JsonPropertyName("X")]
        public double X { get => (double)GetValue(XProperty); set => SetValue(XProperty, value); }

        [DataMember(IsRequired = true, EmitDefaultValue = true)]
        [JsonPropertyName("Y")]
        public double Y { get => (double)GetValue(YProperty); set => SetValue(YProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Topmost")]
        public bool Topmost { get => (bool)GetValue(TopmostProperty); set => SetValue(TopmostProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Title")]
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        [IgnoreDataMember]
        [JsonIgnore]
        public IDockable Owner { get => (IDockable)GetValue(OwnerProperty); set => SetValue(OwnerProperty, value); }

        [IgnoreDataMember]
        [JsonIgnore]
        public IFactory Factory { get => (IFactory)GetValue(FactoryProperty); set => SetValue(FactoryProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Layout")]
        public IRootDock Layout { get => (IRootDock)GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

        [IgnoreDataMember]
        [JsonIgnore]
        public IHostWindow Host { get => (IHostWindow)GetValue(HostProperty); set => SetValue(HostProperty, value); }

        [DataMember(IsRequired = true, EmitDefaultValue = true)]
        [JsonPropertyName("WindowWidth")]
        public double WindowWidth { get => (double)GetValue(WindowWidthProperty); set => SetValue(WindowWidthProperty, value); }

        [DataMember(IsRequired = true, EmitDefaultValue = true)]
        [JsonPropertyName("WindowHeight")]
        public double WindowHeight { get => (double)GetValue(WindowHeightProperty); set => SetValue(WindowHeightProperty, value); }

        DependencyProperty IDProperty = DependencyProperty.Register(
            nameof(Id),
            typeof(string),
            typeof(DockWindow),
            new PropertyMetadata(default(string)));

        DependencyProperty XProperty = DependencyProperty.Register(
            nameof(X),
            typeof(double),
            typeof(DockWindow),
            new PropertyMetadata(default(double)));

        DependencyProperty YProperty = DependencyProperty.Register(
            nameof(Y),
            typeof(double),
            typeof(DockWindow),
            new PropertyMetadata(default(double)));

        DependencyProperty TopmostProperty = DependencyProperty.Register(
            nameof(Topmost),
            typeof(bool),
            typeof(DockWindow),
            new PropertyMetadata(default(bool)));

        DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(DockWindow),
            new PropertyMetadata(default(string)));

        DependencyProperty OwnerProperty = DependencyProperty.Register(
            nameof(Owner),
            typeof(IDockable),
            typeof(DockWindow),
            new PropertyMetadata(default(IDockable)));

        DependencyProperty FactoryProperty = DependencyProperty.Register(
            nameof(Factory),
            typeof(IFactory),
            typeof(DockWindow),
            new PropertyMetadata(default(IFactory)));

        DependencyProperty LayoutProperty = DependencyProperty.Register(
            nameof(Layout),
            typeof(IRootDock),
            typeof(DockWindow),
            new PropertyMetadata(default(IRootDock)));

        DependencyProperty HostProperty = DependencyProperty.Register(
            nameof(Host),
            typeof(IHostWindow),
            typeof(DockWindow),
            new PropertyMetadata(default(IHostWindow)));

        DependencyProperty WindowWidthProperty = DependencyProperty.Register(
            nameof(WindowWidth),
            typeof(double),
            typeof(DockWindow),
            new PropertyMetadata(default(double)));

        DependencyProperty WindowHeightProperty = DependencyProperty.Register(
            nameof(WindowHeight),
            typeof(double),
            typeof(DockWindow),
            new PropertyMetadata(default(double)));

        public void Exit()
        {
            _hostAdapter.Exit();
        }

        public virtual bool OnClose()
        {
            return true;
        }

        public virtual void OnMoveDrag()
        {
        }

        public virtual void OnMoveDragEnd()
        {
        }

        public virtual bool OnMoveDragBegin()
        {
            return true;
        }

        public void Present(bool isDialog)
        {
            _hostAdapter.Present(isDialog);
        }

        public void Save()
        {
            _hostAdapter.Save();
        }
    }
}

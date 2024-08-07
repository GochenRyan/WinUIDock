using Dock.Model.Adapters;
using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.Model.WinUI3.Core
{
    [DataContract(IsReference = true)]
    [JsonPolymorphic]
    [JsonDerivedType(typeof(DockDock), typeDiscriminator: "DockDock")]
    [JsonDerivedType(typeof(Document), typeDiscriminator: "Document")]
    [JsonDerivedType(typeof(DocumentDock), typeDiscriminator: "DocumentDock")]
    [JsonDerivedType(typeof(ProportionalDock), typeDiscriminator: "ProportionalDock")]
    [JsonDerivedType(typeof(ProportionalDockSplitter), typeDiscriminator: "ProportionalDockSplitter")]
    [JsonDerivedType(typeof(RootDock), typeDiscriminator: "RootDock")]
    [JsonDerivedType(typeof(Tool), typeDiscriminator: "Tool")]
    [JsonDerivedType(typeof(ToolDock), typeDiscriminator: "ToolDock")]
    [JsonDerivedType(typeof(DockBase), typeDiscriminator: "DockBase")]
    public abstract class DockableBase : DependencyObject, IDockable
    {
        public DockableBase()
        {
            _trackingAdapter = new TrackingAdapter();
        }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Id")]
        public string Id { get => (string)GetValue(IDProperty); set => SetValue(IDProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Title")]
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Context")]
        public object Context { get => GetValue(ContextProperty); set => SetValue(ContextProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("Owner")]
        public IDockable Owner { get => (IDockable)GetValue(OwnerProperty); set => SetValue(OwnerProperty, value); }

        [IgnoreDataMember]
        [JsonIgnore]
        public IDockable OriginalOwner { get => (IDockable)GetValue(OriginalOwnerProperty); set => SetValue(OriginalOwnerProperty, value); }

        [IgnoreDataMember]
        [JsonIgnore]
        public IFactory Factory { get => (IFactory)GetValue(FactoryProperty); set => SetValue(FactoryProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("CanClose")]
        public bool CanClose { get => (bool)GetValue(CanCloseProperty); set => SetValue(CanCloseProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("CanPin")]
        public bool CanPin { get => (bool)GetValue(CanPinProperty); set => SetValue(CanPinProperty, value); }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("CanFloat")]
        public bool CanFloat { get => (bool)GetValue(CanFloatProperty); set => SetValue(CanFloatProperty, value); }

        public static readonly DependencyProperty IDProperty = DependencyProperty.Register(
            nameof(Id),
            typeof(string),
            typeof(DockableBase),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(DockableBase),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ContextProperty = DependencyProperty.Register(
            nameof(Context),
            typeof(object),
            typeof(DockableBase),
            new PropertyMetadata(null));

        public static readonly DependencyProperty OwnerProperty = DependencyProperty.Register(
            nameof(Owner),
            typeof(IDockable),
            typeof(DockableBase),
            new PropertyMetadata(null));

        public static readonly DependencyProperty OriginalOwnerProperty = DependencyProperty.Register(
            nameof(OriginalOwner),
            typeof(IDockable),
            typeof(DockableBase),
            new PropertyMetadata(null));

        public static readonly DependencyProperty FactoryProperty = DependencyProperty.Register(
            nameof(Factory),
            typeof(IFactory),
            typeof(DockableBase),
            new PropertyMetadata(null));

        public static readonly DependencyProperty CanCloseProperty = DependencyProperty.Register(
            nameof(CanClose),
            typeof(bool),
            typeof(DockableBase),
            new PropertyMetadata(true));

        public static readonly DependencyProperty CanPinProperty = DependencyProperty.Register(
            nameof(CanPin),
            typeof(bool),
            typeof(DockableBase),
            new PropertyMetadata(true));

        public static readonly DependencyProperty CanFloatProperty = DependencyProperty.Register(
            nameof(CanFloat),
            typeof(bool),
            typeof(DockableBase),
            new PropertyMetadata(true));


        public void GetPinnedBounds(out double x, out double y, out double width, out double height)
        {
            _trackingAdapter.GetPinnedBounds(out x, out y, out width, out height);
        }

        public void GetPointerPosition(out double x, out double y)
        {
            _trackingAdapter.GetPointerPosition(out x, out y);
        }

        public void GetPointerScreenPosition(out double x, out double y)
        {
            _trackingAdapter.GetPointerScreenPosition(out x, out y);
        }

        public void GetTabBounds(out double x, out double y, out double width, out double height)
        {
            _trackingAdapter.GetTabBounds(out x, out y, out width, out height);
        }

        public void GetVisibleBounds(out double x, out double y, out double width, out double height)
        {
            _trackingAdapter.GetVisibleBounds(out x, out y, out width, out height);
        }

        public virtual bool OnClose()
        {
            return true;
        }

        public virtual void OnPinnedBoundsChanged(double x, double y, double width, double height)
        {
        }

        public virtual void OnPointerPositionChanged(double x, double y)
        {
        }

        public virtual void OnPointerScreenPositionChanged(double x, double y)
        {
        }

        public virtual void OnSelected()
        {
        }

        public virtual void OnTabBoundsChanged(double x, double y, double width, double height)
        {
        }

        public virtual void OnVisibleBoundsChanged(double x, double y, double width, double height)
        {
        }

        public void SetPinnedBounds(double x, double y, double width, double height)
        {
            _trackingAdapter.SetPinnedBounds(x, y, width, height);
            OnPinnedBoundsChanged(x, y, width, height);
        }

        public void SetPointerPosition(double x, double y)
        {
            _trackingAdapter.SetPointerPosition(x, y);
            OnPointerPositionChanged(x, y);
        }

        public void SetPointerScreenPosition(double x, double y)
        {
            _trackingAdapter.SetPointerScreenPosition(x, y);
            OnPointerScreenPositionChanged(x, y);
        }

        public void SetTabBounds(double x, double y, double width, double height)
        {
            _trackingAdapter.SetTabBounds(x, y, width, height);
            OnTabBoundsChanged(x, y, width, height);
        }

        public void SetVisibleBounds(double x, double y, double width, double height)
        {
            _trackingAdapter.SetVisibleBounds(x, y, width, height);
            OnVisibleBoundsChanged(x, y, width, height);
        }

        private readonly TrackingAdapter _trackingAdapter;
    }
}

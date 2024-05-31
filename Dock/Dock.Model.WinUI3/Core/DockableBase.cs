using Dock.Model.Adapters;
using Dock.Model.Core;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.Model.WinUI3.Core
{
    public abstract class DockableBase : DependencyObject, IDockable
    {
        public DockableBase()
        {
            _trackingAdapter = new TrackingAdapter();
        }

        public string Id { get => (string)GetValue(IDProperty); set => SetValue(IDProperty, value); }
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
        public object Context { get => GetValue(ContextProperty); set => SetValue(ContextProperty, value); }
        public IDockable Owner { get => (IDockable)GetValue(OwnerProperty); set => SetValue(OwnerProperty, value); }
        public IDockable OriginalOwner { get => (IDockable)GetValue(OriginalOwnerProperty); set => SetValue(OriginalOwnerProperty, value); }
        public IFactory Factory { get => (IFactory)GetValue(FactoryProperty); set => SetValue(FactoryProperty, value); }
        public bool CanClose { get => (bool)GetValue(CanCloseProperty); set => SetValue(CanCloseProperty, value); }
        public bool CanPin { get => (bool)GetValue(CanPinProperty); set => SetValue(CanPinProperty, value); }
        public bool CanFloat { get => (bool)GetValue(CanFloatProperty); set => SetValue(CanFloatProperty, value); }

        public static DependencyProperty IDProperty = DependencyProperty.Register(
            nameof(Id),
            typeof(string),
            typeof(DockableBase),
            new PropertyMetadata(string.Empty));

        public static DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(DockableBase),
            new PropertyMetadata(string.Empty));

        public static DependencyProperty ContextProperty = DependencyProperty.Register(
            nameof(Context),
            typeof(object),
            typeof(DockableBase),
            new PropertyMetadata(default));

        public static DependencyProperty OwnerProperty = DependencyProperty.Register(
            nameof(Owner),
            typeof(IDockable),
            typeof(DockableBase),
            new PropertyMetadata(default(IDockable)));

        public static DependencyProperty OriginalOwnerProperty = DependencyProperty.Register(
            nameof(OriginalOwner),
            typeof(IDockable),
            typeof(DockableBase),
            new PropertyMetadata(default(IDockable)));

        public static DependencyProperty FactoryProperty = DependencyProperty.Register(
            nameof(Factory),
            typeof(IFactory),
            typeof(DockableBase),
            new PropertyMetadata(default(IFactory)));

        public static DependencyProperty CanCloseProperty = DependencyProperty.Register(
            nameof(CanClose),
            typeof(bool),
            typeof(DockableBase),
            new PropertyMetadata(true));

        public static DependencyProperty CanPinProperty = DependencyProperty.Register(
            nameof(CanPin),
            typeof(bool),
            typeof(DockableBase),
            new PropertyMetadata(true));

        public static DependencyProperty CanFloatProperty = DependencyProperty.Register(
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

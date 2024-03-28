using Dock.Model.Core;
using Microsoft.UI.Xaml.Controls;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.Model.WinUI3.Core
{
    public abstract class DockableBase : Control, IDockable
    {
        public DockableBase()
        {
            this.DefaultStyleKey = typeof(DockableBase);
        }

        public string Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Title { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public object Context { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IDockable Owner { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IDockable OriginalOwner { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFactory Factory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CanClose { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CanPin { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CanFloat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void GetPinnedBounds(out double x, out double y, out double width, out double height)
        {
            throw new NotImplementedException();
        }

        public void GetPointerPosition(out double x, out double y)
        {
            throw new NotImplementedException();
        }

        public void GetPointerScreenPosition(out double x, out double y)
        {
            throw new NotImplementedException();
        }

        public void GetTabBounds(out double x, out double y, out double width, out double height)
        {
            throw new NotImplementedException();
        }

        public void GetVisibleBounds(out double x, out double y, out double width, out double height)
        {
            throw new NotImplementedException();
        }

        public bool OnClose()
        {
            throw new NotImplementedException();
        }

        public void OnPinnedBoundsChanged(double x, double y, double width, double height)
        {
            throw new NotImplementedException();
        }

        public void OnPointerPositionChanged(double x, double y)
        {
            throw new NotImplementedException();
        }

        public void OnPointerScreenPositionChanged(double x, double y)
        {
            throw new NotImplementedException();
        }

        public void OnSelected()
        {
            throw new NotImplementedException();
        }

        public void OnTabBoundsChanged(double x, double y, double width, double height)
        {
            throw new NotImplementedException();
        }

        public void OnVisibleBoundsChanged(double x, double y, double width, double height)
        {
            throw new NotImplementedException();
        }

        public void SetPinnedBounds(double x, double y, double width, double height)
        {
            throw new NotImplementedException();
        }

        public void SetPointerPosition(double x, double y)
        {
            throw new NotImplementedException();
        }

        public void SetPointerScreenPosition(double x, double y)
        {
            throw new NotImplementedException();
        }

        public void SetTabBounds(double x, double y, double width, double height)
        {
            throw new NotImplementedException();
        }

        public void SetVisibleBounds(double x, double y, double width, double height)
        {
            throw new NotImplementedException();
        }
    }
}

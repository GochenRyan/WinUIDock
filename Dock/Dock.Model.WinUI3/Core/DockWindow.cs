using Dock.Model.Controls;
using Dock.Model.Core;
using Microsoft.UI.Xaml.Controls;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.Model.WinUI3.Core
{
    public abstract class DockWindow : Control, IDockWindow
    {
        public DockWindow()
        {
            this.DefaultStyleKey = typeof(DockWindow);
        }

        public string Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double X { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double Y { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Topmost { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Title { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IDockable Owner { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFactory Factory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IRootDock Layout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IHostWindow Host { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Exit()
        {
            throw new NotImplementedException();
        }

        public bool OnClose()
        {
            throw new NotImplementedException();
        }

        public void OnMoveDrag()
        {
            throw new NotImplementedException();
        }

        public bool OnMoveDragBegin()
        {
            throw new NotImplementedException();
        }

        public void OnMoveDragEnd()
        {
            throw new NotImplementedException();
        }

        public void Present(bool isDialog)
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }
    }
}

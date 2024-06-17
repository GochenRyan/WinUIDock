using Dock.Model;
using Dock.Model.Core;
using Dock.WinUI3.Internal;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public sealed class HostWindowControl : Control, IHostWindow
    {
        public HostWindowControl()
        {
            this.DefaultStyleKey = typeof(HostWindowControl);
        }

        private readonly DockManager _dockManager;
        private readonly HostWindowState _hostWindowState;
        private List<Control> _chromeGrips = new();
        private HostWindowTitleBar _hostWindowTitleBar;
        private bool _mouseDown, _draggingWindow;

        public IDockManager DockManager => _dockManager;

        public IHostWindowState HostWindowState => _hostWindowState;

        public bool IsTracked { get; set; }
        public IDockWindow Window { get; set; }

        public void Present(bool isDialog)
        {
            if (isDialog)
            {
                if (Visibility == Visibility.Collapsed)
                {
                    if (Window is { })
                    {
                        Window.Factory?.OnWindowOpened(Window);
                    }

                    _ownerWindow.Show(); // FIXME: Set correct parent window.
                }
            }
            else
            {
                if (Visibility == Visibility.Collapsed)
                {
                    if (Window is { })
                    {
                        Window.Factory?.OnWindowOpened(Window);
                    }

                    var ownerDockControl = Window?.Layout?.Factory?.DockControls.FirstOrDefault();

                    if (ownerDockControl is Control control && Extensions.GetAppWindow(control) is AppWindow parentWindow)
                    {
                        //Title = parentWindow.Title;
                        //Icon = parentWindow.Icon;
                        //Show(parentWindow);
                        _ownerWindow.Show();
                    }
                    else
                    {
                        _ownerWindow.Show();
                    }
                }
            }
        }

        public void Exit()
        {
            throw new NotImplementedException();
        }

        public void SetPosition(double x, double y)
        {
            throw new NotImplementedException();
        }

        public void GetPosition(out double x, out double y)
        {
            throw new NotImplementedException();
        }

        public void SetSize(double width, double height)
        {
            throw new NotImplementedException();
        }

        public void GetSize(out double width, out double height)
        {
            throw new NotImplementedException();
        }

        public void SetTitle(string title)
        {
            throw new NotImplementedException();
        }

        public void SetLayout(IDock layout)
        {
            throw new NotImplementedException();
        }

        WindowEx _ownerWindow;
    }
}

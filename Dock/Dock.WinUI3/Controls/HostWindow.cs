using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public class HostWindow : WindowEx
    {
        public HostWindow() : base()
        {
            RegisterWindow(AppWindow, this);
        }

        public static Dictionary<AppWindow, Window> windowMap = new Dictionary<AppWindow, Window>();

        private static void RegisterWindow(AppWindow appWindow, Window window)
        {
            windowMap[appWindow] = window;
            if (_mainWindow != null)
            {
                _mainWindow.Closed += (_, _) =>
                {
                    window.Close();
                };
            }
            window.Closed += Window_Closed;
        }

        private static void Window_Closed(object sender, WindowEventArgs args)
        {
            Window window = sender as Window;
            if (window != null && window.AppWindow != null)
            {
                windowMap.Remove(window.AppWindow);
            }
        }

        public static Window GetWindow(AppWindow appWindow)
        {
            windowMap.TryGetValue(appWindow, out Window window);
            return window;
        }

        public static Window GetWindowForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in windowMap.Values)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                    {
                        return window;
                    }
                }
            }

            if (_mainWindow != null && element.XamlRoot == _mainWindow.Content.XamlRoot)
            {
                return _mainWindow;
            }

            return null;
        }

        private static Window _mainWindow;

        public static Window MainWindow
        {
            get { return _mainWindow; }
            set { _mainWindow = value; }
        }
    }
}

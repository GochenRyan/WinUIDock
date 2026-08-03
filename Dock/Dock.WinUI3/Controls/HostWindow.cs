using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Foundation;
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
            if (appWindow is null)
            {
                return;
            }

            windowMap[appWindow] = window;

            // Float windows do not outlive the main window. The subscription must be
            // removed again when this window closes on its own — otherwise every
            // float window ever created leaves a handler behind that still captures
            // it, and closing the main window then calls Close() on a long-dead
            // window: "The WinUI Desktop Window object has already been closed."
            TypedEventHandler<object, WindowEventArgs> closeWithMain = null;

            if (_mainWindow != null)
            {
                closeWithMain = (_, _) =>
                {
                    try
                    {
                        window.Close();
                    }
                    catch
                    {
                        // Already gone — nothing to do.
                    }
                };

                _mainWindow.Closed += closeWithMain;
            }

            // Remove by the CAPTURED key: Window.AppWindow is not reliably
            // accessible once the window has closed, so re-fetching it inside a
            // Closed handler could leave a zombie entry behind. Touching members
            // of such a closed window later (e.g. Content in GetWindowForElement)
            // throws E_INVALIDARG ("Value does not fall within the expected
            // range") on whatever unrelated code path calls in next.
            window.Closed += (_, _) =>
            {
                windowMap.Remove(appWindow);

                if (closeWithMain is not null && _mainWindow is not null)
                {
                    _mainWindow.Closed -= closeWithMain;
                }
            };
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
                List<AppWindow> broken = null;

                foreach (var entry in windowMap)
                {
                    UIElement content;
                    try
                    {
                        content = entry.Value.Content;
                    }
                    catch
                    {
                        // Closed window that escaped unregistration — prune it
                        // instead of letting the failure surface here.
                        (broken ??= new List<AppWindow>()).Add(entry.Key);
                        continue;
                    }

                    if (content?.XamlRoot == element.XamlRoot)
                    {
                        return entry.Value;
                    }
                }

                if (broken != null)
                {
                    foreach (var key in broken)
                    {
                        windowMap.Remove(key);
                    }
                }
            }

            if (_mainWindow != null && element.XamlRoot == _mainWindow.Content?.XamlRoot)
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

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    /// <summary>
    /// The window a torn-off dockable floats in, and — through its statics — the
    /// registry of every top-level window that hosts a dock control.
    ///
    /// Being in the registry is what lets coordinate transforms resolve which
    /// window an element lives in, so a window that carries its own DockControl
    /// (an asset-editor window, say) must register itself through
    /// <see cref="Register"/>. Float windows do it from their constructor.
    /// </summary>
    public class HostWindow : WindowEx
    {
        public HostWindow() : this(null)
        {
        }

        /// <param name="owner">
        /// The window this one belongs to: it closes when the owner closes. Null
        /// falls back to <see cref="MainWindow"/>, which is right for a panel torn
        /// off the main window and wrong for one torn off anything else.
        /// </param>
        public HostWindow(Window owner) : base()
        {
            Register(this, owner ?? MainWindow);
        }

        public static Dictionary<AppWindow, Window> windowMap = new Dictionary<AppWindow, Window>();

        private static readonly Dictionary<AppWindow, Window> _owners = new Dictionary<AppWindow, Window>();
        private static readonly List<Window> _empty = new List<Window>();

        /// <summary>
        /// Adds a dock-hosting window to the registry and ties its lifetime to
        /// <paramref name="owner"/>. Registering twice is a no-op.
        /// </summary>
        public static void Register(Window window, Window owner)
        {
            if (window is null)
            {
                return;
            }

            var appWindow = window.AppWindow;
            if (appWindow is null || windowMap.ContainsKey(appWindow))
            {
                return;
            }

            windowMap[appWindow] = window;

            if (owner is { } && !ReferenceEquals(owner, window))
            {
                _owners[appWindow] = owner;
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
                _owners.Remove(appWindow);
                CloseOwned(window);
            };
        }

        /// <summary>
        /// Closes everything registered under <paramref name="owner"/>, deepest
        /// first.
        ///
        /// Walking the chain here rather than subscribing each window to its
        /// owner's Closed is what makes the cascade reach grandchildren: during
        /// shutdown the queued close of an intermediate window may never be
        /// pumped, so its own Closed handler would never run to pass the message
        /// along.
        /// </summary>
        private static void CloseOwned(Window owner)
        {
            foreach (var owned in OwnedBy(owner))
            {
                CloseOwned(owned);

                try
                {
                    owned.Close();
                }
                catch
                {
                    // Already gone — nothing to do.
                }
            }
        }

        private static List<Window> OwnedBy(Window owner)
        {
            List<Window> owned = null;

            // Materialized before anything closes: Close() can raise Closed
            // synchronously, and that mutates both dictionaries.
            foreach (var entry in _owners)
            {
                if (ReferenceEquals(entry.Value, owner) && windowMap.TryGetValue(entry.Key, out var window))
                {
                    (owned ??= new List<Window>()).Add(window);
                }
            }

            return owned ?? _empty;
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

        /// <summary>
        /// The application's root window. Setting it registers it as an owner, so
        /// everything else in the registry closes with it.
        /// </summary>
        public static Window MainWindow
        {
            get { return _mainWindow; }
            set
            {
                _mainWindow = value;
                Register(value, null);
            }
        }
    }
}

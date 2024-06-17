using Dock.Model.Core;
using Dock.WinUI3.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dock.WinUI3.Internal
{
    internal static class Extensions
    {
        public static IEnumerable<DockControl> GetZOrderedDockControls(this IList<IDockControl> dockControls)
        {
            return dockControls
                .OfType<DockControl>()
                .Reverse();
        }

        public static AppWindow GetAppWindow(UIElement element)
        {
            XamlRoot root = element.XamlRoot;
            var id = root.ContentIslandEnvironment.AppWindowId;
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
            return appWindow;
        }
    }
}

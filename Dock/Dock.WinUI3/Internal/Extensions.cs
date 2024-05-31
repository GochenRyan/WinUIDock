using Dock.Model.Core;
using Dock.WinUI3.Controls;
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
    }
}

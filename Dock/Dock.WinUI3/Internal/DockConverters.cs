using CommunityToolkit.WinUI.UI.Converters;
using Dock.WinUI3.Converters;

namespace Dock.WinUI3.Internal
{
    static class DockConverters
    {
        public static BoolToVisibilityConverter DockBoolToVisibilityConverter = new();
        //public static DockableConverter DockableConverter = new();
        public static ObjectToBoolConverter DockObjectToBoolConverter = new();
    }
}

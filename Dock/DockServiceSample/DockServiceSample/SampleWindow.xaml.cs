using Dock.WinUI3;
using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace DockServiceSample
{
    /// <summary>
    /// A separate top-level window with a dock context of its own — what "window"
    /// actually means, as opposed to the float panel the View menu toggles.
    ///
    /// Two lines carry the demonstration: RegisterFactory="False" in the XAML
    /// keeps the WinUIDockManager facade pointed at the main dock, and the
    /// HostWindow.Register call below puts this window into the dock registry so
    /// drag coordinates resolve — and so it closes when the main window does.
    /// </summary>
    public sealed partial class SampleWindow : WindowEx
    {
        public SampleWindow(Window owner)
        {
            InitializeComponent();

            // Placement persists across runs (size/position/maximized).
            PersistenceId = "SampleWindow";

            HostWindow.Register(this, owner);
            DockThemeManager.RegisterWindow(this);
        }

        public DockControl DockControl => Dock;
    }
}

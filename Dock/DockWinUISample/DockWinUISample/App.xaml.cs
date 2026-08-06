using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DockWinUISample
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        /// <summary>Appends to the same file the crash handler uses, so a headless
        /// run still leaves evidence of what the self-checks concluded.</summary>
        internal static void Log(string message)
        {
            try
            {
                var path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "crash.log");
                System.IO.File.AppendAllText(path, $"[{System.DateTimeOffset.Now:O}] {message}\r\n");
            }
            catch
            {
            }
        }

        public App()
        {
            this.InitializeComponent();

            Log($"BOOT: DockWinUISample marker=082-save-truncate-flush");

            // Window-placement persistence (PersistenceId windows). Must be set
            // before the first window initializes: WinUIEx reads the stored
            // placement during window setup, not on demand. File-backed because
            // this app is unpackaged (no ApplicationData).
            WinUIEx.WindowManager.PersistenceStorage = new Dock.WinUI3.FilePersistenceStorage(
                System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "winuidock-sample", "window-placement.json"));

            // Interop failures surface at the WinUI boundary with the managed stack
            // stripped; first-chance fires AT the throw. Opt-in — noisy by nature.
            if (Environment.GetEnvironmentVariable("DOCKSAMPLE_FIRSTCHANCE") == "1")
            {
                AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
                {
                    // E_NOINTERFACE, E_INVALIDARG and their managed wrappers.
                    if (e.Exception is System.InvalidCastException or System.Runtime.InteropServices.COMException
                            or System.ArgumentException or System.UnauthorizedAccessException
                        || e.Exception.HResult == unchecked((int)0x80070057))
                    {
                        Log($"FirstChance: {e.Exception.GetType().Name} hr=0x{e.Exception.HResult:X8}: {e.Exception.Message}\r\n{e.Exception.StackTrace}\r\n  capture:\r\n{Environment.StackTrace}");
                    }
                };
            }

            // Crash forensics: dump the full exception chain to a file next to
            // the exe so field repros keep their evidence even without a
            // debugger attached.
            UnhandledException += (_, e) =>
            {
                try
                {
                    var path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "crash.log");
                    System.IO.File.AppendAllText(path,
                        $"[{System.DateTimeOffset.Now:O}] Unhandled: {e.Message}\r\n{e.Exception}\r\nStack: {e.Exception?.StackTrace}\r\n\r\n");
                }
                catch
                {
                }
            };
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
            HostWindow.MainWindow = m_window;
        }

        private Window m_window;
    }
}

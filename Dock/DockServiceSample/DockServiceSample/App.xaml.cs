using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DockServiceSample
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
        public App()
        {
            this.InitializeComponent();

            // Stamp which build is actually running. A packaged (MSIX) launch loads
            // whatever was last deployed, which is easy to mistake for a rebuild —
            // if this line is missing from crash.log, the binaries are stale.
            Log("BOOT", null,
                $"DockServiceSample build {typeof(App).Assembly.GetName().Version} " +
                $"| Dock.WinUI3 {typeof(Dock.WinUI3.Controls.HostWindowControl).Assembly.GetName().Version} " +
                $"| marker=076-doc-tab-overflow-scroll");

            // Crash forensics: dump to a file next to the exe so field repros keep
            // their evidence even without a debugger.
            UnhandledException += (_, e) => Log("Unhandled", e.Exception, e.Message);

            // Anything this sample swallows in a try/catch also lands here — a GUI
            // process has no console, so Console.WriteLine alone loses the evidence.
            AppDomain.CurrentDomain.UnhandledException += (_, e) => Log("Domain", e.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (_, e) => Log("Task", e.Exception);

            // Interop failures surface at the WinUI boundary with their managed stack
            // already stripped — by the time UnhandledException runs there is nothing
            // left to point at. First-chance fires AT the throw, so it is the only
            // place the origin is still visible. Noisy by nature, hence opt-in:
            // set DOCKSAMPLE_FIRSTCHANCE=1 before launching.
            if (Environment.GetEnvironmentVariable("DOCKSAMPLE_FIRSTCHANCE") == "1")
            {
                AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
                {
                    // Only the E_INVALIDARG family — logging every first-chance
                    // exception would bury the one that matters.
                    if (e.Exception.HResult == unchecked((int)0x80070057))
                    {
                        Log("FirstChance", e.Exception);
                    }
                };
            }
        }

        /// <summary>Appends to crash.log next to the exe. Never throws.</summary>
        internal static void Log(string kind, Exception? exception, string? message = null)
        {
            try
            {
                var text =
                    $"[{DateTimeOffset.Now:O}] {kind}: {message ?? exception?.Message}\r\n" +
                    $"  type={exception?.GetType().FullName} hresult=0x{exception?.HResult:X8}\r\n" +
                    $"  exception stack:\r\n{exception?.StackTrace ?? "    <none>"}\r\n" +
                    $"  capture stack:\r\n{Environment.StackTrace}\r\n";

                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "crash.log"), text + "\r\n");
            }
            catch
            {
            }
        }

        /// <summary>Overload for the swallowing catch blocks.</summary>
        internal static void Log(string message) => Log("Swallowed", null, message);

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

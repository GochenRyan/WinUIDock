using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;

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
        public App()
        {
            this.InitializeComponent();

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

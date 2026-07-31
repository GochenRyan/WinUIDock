using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace Dock.WinUI3.Internal
{
    /// <summary>
    /// Opt-in diagnostics for the hard-to-reproduce docking paths (cross-window
    /// redock, content migration, transient layout failures).
    ///
    /// Disabled by default — zero cost. Enable by setting the environment
    /// variable <c>WINUIDOCK_DIAG=1</c> before launching the app; output goes to
    /// <c>%TEMP%\winuidock-diag.log</c>. Only rare, meaningful events are logged
    /// (content watchdog repairs, transient layout recoveries, float window
    /// teardown), never per-frame chatter.
    /// </summary>
    internal static class DockDiag
    {
        private static readonly bool _enabled =
            Environment.GetEnvironmentVariable("WINUIDOCK_DIAG") == "1";

        private static readonly string LogPath =
            Path.Combine(Path.GetTempPath(), "winuidock-diag.log");

        public static bool IsEnabled => _enabled;

        public static void Log(string message)
        {
            if (!_enabled)
            {
                return;
            }

            try
            {
                File.AppendAllText(LogPath, $"[{DateTimeOffset.Now:O}] {message}\r\n");
            }
            catch
            {
            }
        }

        public static string Describe(object o)
        {
            if (o is null)
            {
                return "<null>";
            }

            var name = (o as FrameworkElement)?.Name;
            return $"{o.GetType().Name}#{o.GetHashCode():x8}{(string.IsNullOrEmpty(name) ? "" : $"({name})")}";
        }

        /// <summary>
        /// The transient cross-window layout race surfaces as E_INVALIDARG
        /// ("Value does not fall within the expected range") from native
        /// measure/arrange. Only that signature is eligible for self-healing.
        /// </summary>
        public static bool IsTransientLayoutError(Exception ex)
        {
            const int E_INVALIDARG = unchecked((int)0x80070057);
            return ex is ArgumentException or System.Runtime.InteropServices.COMException
                && ex.HResult == E_INVALIDARG;
        }
    }
}

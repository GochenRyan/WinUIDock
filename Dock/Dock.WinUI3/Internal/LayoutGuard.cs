using System;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace Dock.WinUI3.Internal
{
    /// <summary>
    /// Wraps a layout pass so the transient E_INVALIDARG the native layer throws
    /// while a dock tree is being torn down and rebuilt does not reach the app as an
    /// unhandled exception.
    ///
    /// Two things were learned the hard way in ChangeLog 017 and are encoded here:
    ///
    /// * The condition clears immediately, so the first response is to retry the pass
    ///   INLINE. Skipping the pass instead leaves templates unmaterialized and shows
    ///   up as blank panes — worse than the crash it replaces.
    /// * Only if the retry also fails is a re-measure deferred to the next tick.
    ///
    /// Guards placed nearest the source recover most precisely (ToolChromeControl,
    /// ToolDockControl, DockPanel and ProportionalStackPanel carry their own). This
    /// helper additionally backs the controls at the ROOT of each tree, where a pass
    /// covers the whole subtree: layout recurses top-down, so a catch in the host
    /// control also catches whatever its descendants throw. That matters because most
    /// dock controls override measure/arrange without any guard of their own.
    /// </summary>
    internal static class LayoutGuard
    {
        /// <param name="element">The control running the pass; used to schedule the retry.</param>
        /// <param name="pass">The actual measure/arrange work.</param>
        /// <param name="fallback">What to report when the pass could not be completed
        /// (DesiredSize when measuring, finalSize when arranging).</param>
        /// <param name="what">Label for the diagnostic log.</param>
        public static Size Run(FrameworkElement element, Func<Size> pass, Size fallback, string what)
        {
            try
            {
                return pass();
            }
            catch (Exception ex) when (DockDiag.IsTransientLayoutError(ex))
            {
                DockDiag.Log($"{what}: transient layout failure — retrying inline");

                try
                {
                    return pass();
                }
                catch (Exception retryEx) when (DockDiag.IsTransientLayoutError(retryEx))
                {
                    DockDiag.Log($"{what}: transient layout failure persisted — deferring a re-measure");
                    element.DispatcherQueue?.TryEnqueue(element.InvalidateMeasure);
                    return fallback;
                }
            }
        }
    }
}

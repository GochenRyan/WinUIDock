using CommunityToolkit.WinUI;
using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;
using Microsoft.UI.Xaml;
using System;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = DockControlName, Type = typeof(DockControl))]
    [TemplatePart(Name = TitleBarName, Type = typeof(HostWindowTitleBar))]
    public class HostWindowControl : ContentControl, IHostWindow
    {
        public const string DockControlName = "PART_DockControl";
        public const string TitleBarName = "PART_TitleBar";

        public HostWindowControl(HostWindow hostWindow)
        {
            this.DefaultStyleKey = typeof(HostWindowControl);
            _ownerWindow = hostWindow;
            _ownerWindow.WindowContent = this;
            // ExtendsContentIntoTitleBar is deliberately NOT set here — see Present().

            // Float windows are created dynamically and must follow the dock theme
            // (content root + OS caption buttons; applied immediately and kept in
            // sync with later SetTheme calls; weak refs, drop on close).
            DockThemeManager.RegisterWindow(hostWindow);

            _ownerWindow.PositionChanged += _ownerWindow_PositionChanged;
            _ownerWindow.SizeChanged += _ownerWindow_SizeChanged;

            // The one reliable "is this window still usable" signal. Every member of
            // a closed WinUI window throws, so it cannot be probed after the fact —
            // it has to be recorded when it happens. Covers closes we do not
            // initiate too: the user's X button, and the main window taking its
            // float windows down with it.
            _ownerWindow.Closed += OnOwnerWindowClosed;

            LayoutUpdated += HostWindowControl_LayoutUpdated;

            _dockManager = new DockManager();

            DataContextChanged += HostWindowControl_DataContextChanged;
        }

        private void OnOwnerWindowClosed(object sender, WindowEventArgs args)
        {
            _ownerWindowClosed = true;

            if (sender is Window window)
            {
                window.Closed -= OnOwnerWindowClosed;
            }
        }

        private void _ownerWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            _ownerWindowWidth = args.Size.Width;
            _ownerWindowHeight = args.Size.Height;
        }

        private void _ownerWindow_PositionChanged(object sender, Windows.Graphics.PointInt32 e)
        {
            _ownerWindowX = e.X;
            _ownerWindowY = e.Y;
        }

        private void HostWindowControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            UpdateTemplateChilren();
        }

        private void UpdateTemplateChilren()
        {
            IDock dock = DataContext as IDock;
            if (_dockControl != null)
            {
                _dockControl.Layout = dock;
            }

            if (_titleBar != null && dock != null)
            {
                _titleBar.TitleText = ResolveTitle(dock);
            }
        }

        // Float windows show the floated content's own title (walk down the
        // active chain to the leaf tool/document) instead of the parent window's.
        private static string ResolveTitle(IDock dock)
        {
            IDockable current = dock;
            while (current is IDock d && d.ActiveDockable is not null)
            {
                current = d.ActiveDockable;
            }

            return current?.Title ?? string.Empty;
        }

        /// <summary>
        /// Records the window's geometry every frame.
        ///
        /// LayoutUpdated is raised by the framework, so anything thrown here escapes
        /// as an unhandled exception with no managed frame of ours on the stack. And
        /// there IS something to throw: reading <c>AppWindow</c> — or any other member
        /// of a closed WinUI window — fails with E_INVALIDARG ("The parameter is
        /// incorrect"), which a float window closing under a live subscription hits on
        /// the very next frame.
        ///
        /// Unsubscribing on close is the fix; this guard is the belt to that braces,
        /// since WinUI does not reliably raise Unloaded for a closing window's tree.
        /// </summary>
        private void HostWindowControl_LayoutUpdated(object sender, object e)
        {
            // A close we did not initiate (the user's X button, or the main window
            // taking its float windows down) never runs CloseOwnerWindow, so this
            // subscription survives it. Checking the recorded flag costs nothing and
            // keeps the AppWindow read below off a dead window.
            if (_ownerWindowClosed)
            {
                LayoutUpdated -= HostWindowControl_LayoutUpdated;
                return;
            }

            try
            {
                if (Window is { } && _ownerWindow.AppWindow != null && IsTracked)
                {
                    Window.Save();
                }
            }
            catch (Exception ex)
            {
                Internal.DockDiag.Log(
                    $"HostWindowControl.LayoutUpdated on a dead window — unsubscribing: {ex.Message}");
                LayoutUpdated -= HostWindowControl_LayoutUpdated;
            }
        }

        public static readonly DependencyProperty IsToolWindowProperty = DependencyProperty.Register(
            nameof(IsToolWindow),
            typeof(bool),
            typeof(HostWindowControl),
            new PropertyMetadata(false));

        public bool IsToolWindow
        {
            get => (bool)GetValue(IsToolWindowProperty);
            set => SetValue(IsToolWindowProperty, value);
        }

        public static readonly DependencyProperty ToolChromeControlsWholeWindowProperty = DependencyProperty.Register(
            nameof(ToolChromeControlsWholeWindow),
            typeof(bool),
            typeof(HostWindowControl),
            new PropertyMetadata(false));

        public bool ToolChromeControlsWholeWindow
        {
            get => (bool)GetValue(ToolChromeControlsWholeWindowProperty);
            set => SetValue(ToolChromeControlsWholeWindowProperty, value);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _dockControl = GetTemplateChild(DockControlName) as DockControl;
            _dockControl.Layout = DataContext as IDock;

            _titleBar = GetTemplateChild(TitleBarName) as HostWindowTitleBar;
            _titleBar.Height = DockMetrics.GetDouble("DockFloatTitleBarHeight", 32.0);
            _ownerWindow.SetTitleBar(_titleBar);
            UpdateTemplateChilren();
        }

        private readonly DockManager _dockManager;
        private List<Grid> _chromeGrips = new();

        public IDockManager DockManager => _dockManager;

        public IHostWindowState HostWindowState => null;

        public bool IsTracked { get; set; }
        public IDockWindow Window { get; set; }

        public void Present(bool isDialog)
        {
            if (isDialog)
            {
                if (Window is { })
                {
                    Window.Factory?.OnWindowOpened(Window);
                }

                _ownerWindow.Show();
                ExtendContentIntoTitleBar();
            }
            else
            {
                if (Window is { })
                {
                    Window.Factory?.OnWindowOpened(Window);
                }

                // OS-level title (taskbar/alt-tab): use the floated content's own
                // title when available, falling back to the parent window's.
                if (DataContext is IDock dock && ResolveTitle(dock) is { Length: > 0 } title)
                {
                    _ownerWindow.Title = title;
                }
                else
                {
                    var ownerDockControl = Window?.Layout?.Factory?.DockControls.FirstOrDefault();
                    if (ownerDockControl is Control control && HostWindow.GetWindowForElement(control) is Window parentWindow)
                    {
                        _ownerWindow.Title = parentWindow.Title;
                    }
                }

                _ownerWindow.Show();
                ExtendContentIntoTitleBar();
            }
        }

        /// <summary>
        /// Hands the caption area over to our own title bar — but only once the
        /// window is actually on screen.
        ///
        /// Setting this makes WinUI reconfigure the non-client area, which resolves
        /// the window's HWND through its WindowId. Before the window is shown that
        /// association does not exist yet and Microsoft.UI.Input fails with
        /// "There is no HWND associated with the provided WindowId" — visible in the
        /// debug output on every float window ever created. Harmless on its own, but
        /// the same call turns into a process-killing fail-fast (0xC0000602) when it
        /// runs while another window is being torn down, which is what made
        /// reloading a layout crash.
        ///
        /// Measured order (WINUIDOCK_DIAG): Present -> Show -> here -> OnApplyTemplate,
        /// so the title bar element is hooked up (SetTitleBar) strictly after this.
        /// </summary>
        private void ExtendContentIntoTitleBar()
        {
            try
            {
                _ownerWindow.ExtendsContentIntoTitleBar = true;
            }
            catch (Exception ex)
            {
                Internal.DockDiag.Log($"HostWindowControl.ExtendContentIntoTitleBar failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Attaches grip to chrome.
        /// </summary>
        /// <param name="chromeControl">The chrome control.</param>
        public void AttachGrip(ToolChromeControl chromeControl)
        {
            if (_chromeGrips.Contains(chromeControl.Grip))
                return;

            if (chromeControl.CloseButton is not null)
            {
                chromeControl.CloseButton.Click += ChromeCloseClick;
            }

            if (chromeControl.Grip is { } grip)
            {
                _chromeGrips.Add(grip);
            }

            IsToolWindow = true;
        }

        /// <summary>
        /// Detaches grip to chrome.
        /// </summary>
        /// <param name="chromeControl">The chrome control.</param>
        public void DetachGrip(ToolChromeControl chromeControl)
        {
            if (chromeControl.Grip is { } grip)
            {
                _chromeGrips.Remove(grip);
            }

            if (chromeControl.CloseButton is not null)
            {
                chromeControl.CloseButton.Click -= ChromeCloseClick;
            }
        }

        private void ChromeCloseClick(object sender, RoutedEventArgs e)
        {
            if (CountVisibleToolsAndDocuments(DataContext as IRootDock) <= 1)
                Exit();
        }

        private int CountVisibleToolsAndDocuments(IDockable dockable)
        {
            switch (dockable)
            {
                case ITool: return 1;
                case IDocument: return 1;
                case IDock dock:
                    return dock.VisibleDockables?.Sum(CountVisibleToolsAndDocuments) ?? 0;
                default: return 0;
            }
        }

        /// <summary>
        /// Synchronously releases the model-owned content elements so a layout
        /// swap can re-parent them in the SAME tick. Exit() closes deferred
        /// (load-bearing for the drag paths), and until that close is pumped this
        /// window still counts as visible — so the hosts must both let go AND
        /// stand their watchdogs down, or they steal the elements back from the
        /// new host (a cross-island re-parent that dies in native code).
        /// </summary>
        public void SeverContentForTeardown()
        {
            try
            {
                foreach (var element in this.FindDescendants())
                {
                    switch (element)
                    {
                        case ToolContentControl toolContent:
                            toolContent.StandDownForTeardown();
                            break;
                        case DocumentContentControl documentContent:
                            documentContent.StandDownForTeardown();
                            break;
                    }
                }
            }
            catch
            {
                // Tree mid-teardown — the deferred close finishes the job.
            }

            if (_dockControl is not null)
            {
                _dockControl.Layout = null;
            }
        }

        public void Exit()
        {
            // Deferred close: Exit is reached synchronously from pointer-event
            // handlers of elements living in THIS window (grip drag-dock drop:
            // DockControlState executes the dock, the emptied float window exits,
            // then the pointer chain keeps touching this window's objects —
            // closing immediately yields E_ACCESSDENIED "The caller is not
            // allowed to perform this operation on this object"). Let the event
            // chain unwind first.
            Internal.DockDiag.Log($"HostWindowControl.Exit requested for {Internal.DockDiag.Describe(this)}");

            // Cheap early out for the repeat-Exit case, so it does not even cost a
            // dispatcher hop. CloseOwnerWindow checks again — by the time a deferred
            // callback runs, the window may have closed in between.
            if (_ownerWindowClosed)
            {
                return;
            }

            var window = Window;
            var ownerWindow = _ownerWindow;
            // Low priority: let the post-drop layout pass of the target window
            // finish before this window tears down — closing mid-pass is the
            // trigger of the transient E_INVALIDARG measure/arrange race.
            var enqueued = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try
                {
                    if (window is { })
                    {
                        if (window.OnClose())
                        {
                            CloseOwnerWindow(ownerWindow);
                        }
                    }
                    else
                    {
                        CloseOwnerWindow(ownerWindow);
                    }
                }
                catch
                {
                    // Already closed/closing — nothing left to do.
                }
            });

            if (!enqueued)
            {
                CloseOwnerWindow(ownerWindow);
            }
        }

        private void CloseOwnerWindow(WindowEx ownerWindow)
        {
            // Exit() can be reached more than once for the same window — the reload
            // path calls it explicitly (035) and the layout swap's
            // DeInitialize -> ExitWindows calls it again — and BOTH hops defer, so
            // the second callback lands on a window that is already gone. Every
            // member of a closed WinUI window then throws
            // "The WinUI Desktop Window object has already been closed": the
            // try/catch blocks below do swallow it, but it still breaks the debugger
            // on every reload, and none of the work below means anything once the
            // HWND is gone.
            if (_ownerWindowClosed)
            {
                Internal.DockDiag.Log($"HostWindowControl.CloseOwnerWindow skipped, already closed: {Internal.DockDiag.Describe(this)}");
                return;
            }

            Internal.DockDiag.Log($"HostWindowControl.CloseOwnerWindow executing for {Internal.DockDiag.Describe(this)}");

            // Everything below runs BEFORE the window is closed, and all of it is
            // about cutting links that would otherwise outlive the HWND. This has to
            // be prevention rather than a try/catch: the failure mode here is a
            // fail-fast (0xC0000602 — "exception handlers will not be called, the
            // process terminates immediately"), which no catch block can intercept.

            // 1. The per-frame geometry recorder reads AppWindow, which throws
            //    E_INVALIDARG once the window is closed, from the framework's
            //    LayoutUpdated dispatch.
            LayoutUpdated -= HostWindowControl_LayoutUpdated;

            // 2. Our own subscriptions to the window's own events.
            try
            {
                ownerWindow.PositionChanged -= _ownerWindow_PositionChanged;
                ownerWindow.SizeChanged -= _ownerWindow_SizeChanged;
            }
            catch
            {
            }

            // 3. Detach the custom title bar. WinUIEx keeps tracking that element to
            //    maintain the non-client drag region through InputNonClientPointerSource;
            //    once the HWND is gone that lookup fails with
            //    "There is no HWND associated with the provided WindowId" (E_INVALIDARG)
            //    deep inside Microsoft.UI.Input, and takes the process down.
            try
            {
                ownerWindow.SetTitleBar(null);
            }
            catch
            {
            }

            // Closing a WinUI 3 window does NOT reliably raise Unloaded for its
            // tree, so the model-owned shared content elements can die attached
            // to this window and poison their next host (E_INVALIDARG at its
            // native measure). Force-detach every content presenter while the
            // tree is still alive, then close.
            try
            {
                foreach (var presenter in this.FindDescendants().OfType<ContentPresenter>())
                {
                    if (presenter.Name == ToolContentControl.ContentPresenterName)
                    {
                        presenter.Content = null;
                    }
                }
            }
            catch
            {
            }

            // Set BEFORE closing: Close() raises Closed synchronously, and anything
            // re-entering through it must already see this window as gone.
            _ownerWindowClosed = true;
            ownerWindow.Close();
        }

        // Window geometry restored from a layout file is untrusted input: it may be
        // stale, may have been written before the window was ever sized, and passes
        // through a DPI conversion on the way to the OS. Values the OS rejects come
        // back as "The parameter is incorrect" — an E_INVALIDARG that no layout guard
        // can catch, because it is not thrown from measure/arrange. Validate here.

        public void SetPosition(double x, double y)
        {
            // Infinity and out-of-int-range matter as much as NaN: the double->int
            // cast below has already overflowed into garbage by the time the OS sees
            // them.
            if (!IsFinite(x) || !IsFinite(y)
                || x < int.MinValue || x > int.MaxValue
                || y < int.MinValue || y > int.MaxValue)
            {
                Internal.DockDiag.Log($"HostWindowControl.SetPosition ignoring invalid position ({x}, {y})");
                return;
            }

            try
            {
                _ownerWindow.Move((int)x, (int)y);
            }
            catch (Exception ex)
            {
                // Losing a restored position is cosmetic; taking down the app is not.
                Internal.DockDiag.Log($"HostWindowControl.SetPosition failed for ({x}, {y}): {ex.Message}");
            }
        }

        public void GetPosition(out double x, out double y)
        {
            x = _ownerWindowX;
            y = _ownerWindowY;
        }

        // WindowWidth/Height carry the DOCK-CONTENT area (DIPs). SetSize converts
        // to the OUTER bounds Window.Width wants; treating content as outer makes
        // every float/save-load round shrink the window by one chrome.

        public void SetSize(double width, double height)
        {
            var (chromeWidth, chromeHeight) = GetChromeOverheadDips();
            TrySetExtent(v => _ownerWindow.Width = v, width + chromeWidth, nameof(width));
            TrySetExtent(v => _ownerWindow.Height = v, height + chromeHeight, nameof(height));
        }

        /// <summary>
        /// Outer-minus-content overhead in DIPs: the measured OS frame plus the
        /// caption strip, which lives INSIDE the client area
        /// (ExtendsContentIntoTitleBar) and so is not part of the frame delta.
        /// </summary>
        private (double Width, double Height) GetChromeOverheadDips()
        {
            var frameWidth = 0.0;
            var frameHeight = 0.0;

            try
            {
                if (_ownerWindow.AppWindow is { } appWindow)
                {
                    var outer = appWindow.Size;
                    var client = appWindow.ClientSize;
                    var scale = GetDpiScale();

                    if (outer.Width >= client.Width && outer.Height >= client.Height && scale > 0)
                    {
                        frameWidth = (outer.Width - client.Width) / scale;
                        frameHeight = (outer.Height - client.Height) / scale;
                    }
                }
            }
            catch
            {
                // Window mid-teardown — fall back to the caption strip alone.
            }

            var captionHeight = DockMetrics.GetDouble("DockFloatTitleBarHeight", 32.0);
            return (frameWidth, frameHeight + captionHeight);
        }

        private double GetDpiScale()
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_ownerWindow);
                if (hwnd != IntPtr.Zero)
                {
                    var dpi = GetDpiForWindow(hwnd);
                    if (dpi > 0)
                    {
                        return dpi / 96.0;
                    }
                }
            }
            catch
            {
            }

            return 1.0;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        /// <summary>
        /// Applies one window extent, skipping values the OS would refuse. A zero or
        /// negative extent is the dangerous one: it reaches here whenever a layout was
        /// saved before the window had ever been sized (the tracked extents start at
        /// zero), and the resize call then fails with "The parameter is incorrect".
        /// </summary>
        private static void TrySetExtent(Action<double> apply, double value, string what)
        {
            if (!IsFinite(value) || value <= 0.0)
            {
                Internal.DockDiag.Log($"HostWindowControl.SetSize ignoring invalid {what} ({value})");
                return;
            }

            try
            {
                apply(value);
            }
            catch (Exception ex)
            {
                Internal.DockDiag.Log($"HostWindowControl.SetSize failed for {what}={value}: {ex.Message}");
            }
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        public void GetSize(out double width, out double height)
        {
            // Symmetric with SetSize: report the DOCK-CONTENT area. The tracked
            // extents are the CLIENT size (Window.SizeChanged reports it in DIPs),
            // which still contains the in-content caption strip.
            width = _ownerWindowWidth;
            height = _ownerWindowHeight;

            if (height > 0)
            {
                height = Math.Max(0, height - DockMetrics.GetDouble("DockFloatTitleBarHeight", 32.0));
            }
        }

        public void SetTitle(string title)
        {
            _ownerWindow.Title = title;
        }

        public void SetLayout(IDock layout)
        {
            DataContext = layout;
            Content = layout;
            ToolChromeControlsWholeWindow = layout.OpenedDockablesCount < 2;
        }

        // Root of a float window's tree — counterpart to the guard on DockControl.
        // Float windows are created and torn down exactly when these transient
        // failures happen, and their trees do not hang below the main DockControl,
        // so they need a top-level guard of their own.
        protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
            => Internal.LayoutGuard.Run(
                this, () => base.MeasureOverride(availableSize), DesiredSize, nameof(HostWindowControl) + ".Measure");

        protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
            => Internal.LayoutGuard.Run(
                this, () => base.ArrangeOverride(finalSize), finalSize, nameof(HostWindowControl) + ".Arrange");

        private WindowEx _ownerWindow;
        private DockControl _dockControl;
        private HostWindowTitleBar _titleBar;

        private double _ownerWindowX;
        private double _ownerWindowY;
        private double _ownerWindowWidth;
        private double _ownerWindowHeight;
        private bool _ownerWindowClosed;
    }
}

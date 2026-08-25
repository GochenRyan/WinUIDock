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
    public class HostWindowControl : ContentControl, IHostWindow
    {
        public const string DockControlName = "PART_DockControl";

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

            // Caption-button inset is reported in raw pixels and can change with
            // the DPI the window sits on — refresh the title strip's reservation.
            PushTitleStripInset();
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
        }

        // The floated content's own title (walk down the active chain to the
        // leaf tool/document) — used for the OS-level window title only
        // (taskbar / Alt-Tab); the in-window title row is the tab strip itself.
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
                return;
            }

            // Closed-loop guard for the single-row chrome: the title role must
            // sit on the TOP-most strip, and panes can move inside this window
            // without any load/unload event firing. Cheap (a couple of
            // transforms over a tiny candidate list) and follows the repo's
            // per-frame-verification discipline for cross-host state.
            VerifyTitleStrip();

            // Closed-loop half of SetSize. Frame-driven ON PURPOSE: the OS
            // applies resizes asynchronously and in two steps, so event-driven
            // fixups act on stale intermediates and stall or oscillate; frames
            // keep coming until the measured content host matches the request.
            ConvergeContentSize();
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

            // No SetTitleBar here: the caption drag region is the blank fill of
            // whichever tab strip wins the title role — see UpdateTitleStrip().
            UpdateTemplateChilren();
        }

        private readonly DockManager _dockManager;
        private List<Grid> _chromeGrips = new();

        public IDockManager DockManager => _dockManager;

        public IHostWindowState HostWindowState => null;

        public bool IsTracked { get; set; }
        public IDockWindow Window { get; set; }

        /// <summary>The WinUI window this control fills — for callers inside the
        /// library that need to raise or activate it (drag drop-target focus).</summary>
        internal WinUIEx.WindowEx OwnerWindow => _ownerWindow;

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

        // STORED-SIZE INVARIANT: IDockWindow.WindowWidth/Height carry the floated
        // dock's CONTENT area in DIPs — the rect of the content host inside the
        // pane (what DockableControl size tracking records while docked, see
        // IsSizeTracked). Every producer/consumer agrees on that one meaning:
        //   - DockableControl (content host) records it while docked,
        //   - FloatDockable/SplitToWindow pass it through unchanged,
        //   - SetSize resizes the CLIENT to content + in-client float chrome,
        //   - GetSize subtracts the in-client chrome back out for Save().
        // Sizing the client to the raw content rect would make every float come
        // out one title row + borders SMALLER than the pane.

        public void SetSize(double width, double height)
        {
            var (chromeWidth, chromeHeight) = GetContentChromeOverheadDips();
            var clientWidth = width + chromeWidth;
            var clientHeight = height + chromeHeight;

            if (!IsFinite(clientWidth) || !IsFinite(clientHeight) || width <= 0.0 || height <= 0.0)
            {
                // Zero/garbage extents reach here whenever a layout was saved
                // before the window was ever sized; the OS would refuse them
                // with "The parameter is incorrect".
                Internal.DockDiag.Log($"HostWindowControl.SetSize ignoring invalid content size ({width}x{height})");
                return;
            }

            // No open-loop geometry math: the OS frame delta, ResizeClient AND
            // even the in-client chrome all wobble by a physical pixel or two
            // (non-client reconfiguration, fractional-DPI border rounding that
            // depends on sub-pixel origins), and every float paid those errors
            // as lost content. Instead: apply a best-effort guess now, then
            // CONVERGE the measured CONTENT HOST onto the requested content size
            // (closed loop, same discipline as the content watchdogs).
            _targetContentWidth = width;
            _targetContentHeight = height;
            _clientSizeFixups = 0;
            _convergeWaitFrames = 0;
            _lastConvergeWidth = -1;
            _lastConvergeHeight = -1;

            Internal.DockDiag.Log($"HostWindowControl.SetSize content={width:F1}x{height:F1} "
                + $"chrome={chromeWidth:F1}x{chromeHeight:F1} -> initial client guess={clientWidth:F1}x{clientHeight:F1} DIPs");

            try
            {
                var (frameWidth, frameHeight) = GetFrameOverheadEstimateDips();
                _ownerWindow.Width = clientWidth + frameWidth;
                _ownerWindow.Height = clientHeight + frameHeight;
            }
            catch (Exception ex)
            {
                // Losing a restored size is cosmetic; taking down the app is not.
                Internal.DockDiag.Log($"HostWindowControl.SetSize failed for {width}x{height}: {ex.Message}");
            }
        }

        /// <summary>
        /// Second half of SetSize: measure the CONTENT HOST the window actually
        /// laid out and apply the remaining delta as a relative outer resize.
        /// Driving the loop off the content host (not the client) folds every
        /// geometry wobble — OS frame, non-client reconfiguration, fractional-DPI
        /// border rounding — into one measured correction. The stability gate
        /// avoids acting on sizes the OS is still applying; the fixup cap keeps
        /// a fight with a concurrent user resize impossible.
        /// </summary>
        private void ConvergeContentSize()
        {
            if (_targetContentWidth <= 0 || _targetContentHeight <= 0 || _ownerWindowClosed)
            {
                return;
            }

            // The single-row chrome promotion (caption collapses, strip moves to
            // the top at title height) lands a beat AFTER the window shows and
            // shifts the content by the chrome delta — converging before it wins
            // the race and bakes the pre-promotion chrome into the size. Wait
            // for the title role (or a frame budget, for stripless layouts).
            if (_titleStrip is null && _convergeWaitFrames < 30)
            {
                _convergeWaitFrames++;
                return;
            }

            var (host, multiple) = FindContentHosts();
            if (host is null)
            {
                // Content still materializing — later frames retry.
                return;
            }

            if (multiple)
            {
                // Several panes: "the content" is ambiguous; the initial guess
                // (content + standard chrome) is the best available meaning.
                _targetContentWidth = 0;
                _targetContentHeight = 0;
                return;
            }

            var actualWidth = host.ActualWidth;
            var actualHeight = host.ActualHeight;
            var scale = GetDpiScale();

            // Converged when within ONE physical pixel: at fractional scales the
            // exact DIP target may sit between representable pixel sizes.
            var tolerance = Math.Max(0.6, scale > 0 ? 1.05 / scale : 0.6);
            var deltaWidth = _targetContentWidth - actualWidth;
            var deltaHeight = _targetContentHeight - actualHeight;

            if (Math.Abs(deltaWidth) <= tolerance && Math.Abs(deltaHeight) <= tolerance)
            {
                Internal.DockDiag.Log($"HostWindowControl.ConvergeContentSize converged at "
                    + $"{actualWidth:F1}x{actualHeight:F1} (target {_targetContentWidth:F1}x{_targetContentHeight:F1}, fixups={_clientSizeFixups})");
                _targetContentWidth = 0;
                _targetContentHeight = 0;
                return;
            }

            // The OS applies our resize asynchronously — acting on a value that
            // is still moving stacks corrections and oscillates. Only act once
            // the same actual size has been observed on two consecutive frames.
            if (Math.Abs(actualWidth - _lastConvergeWidth) > 0.01 || Math.Abs(actualHeight - _lastConvergeHeight) > 0.01)
            {
                _lastConvergeWidth = actualWidth;
                _lastConvergeHeight = actualHeight;
                return;
            }

            if (++_clientSizeFixups > 4)
            {
                Internal.DockDiag.Log($"HostWindowControl.ConvergeContentSize giving up after 4 fixups "
                    + $"(target={_targetContentWidth:F1}x{_targetContentHeight:F1}, actual={actualWidth:F1}x{actualHeight:F1})");
                _targetContentWidth = 0;
                _targetContentHeight = 0;
                return;
            }

            try
            {
                if (_ownerWindow.WindowState != WindowState.Normal)
                {
                    // Maximized/minimized geometry is not ours to correct.
                    _targetContentWidth = 0;
                    _targetContentHeight = 0;
                    return;
                }

                if (_ownerWindow.AppWindow is not { } appWindow || scale <= 0)
                {
                    return;
                }

                var outerWidth = appWindow.Size.Width / scale;
                var outerHeight = appWindow.Size.Height / scale;

                Internal.DockDiag.Log($"HostWindowControl.ConvergeContentSize fixup #{_clientSizeFixups}: "
                    + $"content {actualWidth:F1}x{actualHeight:F1} -> {_targetContentWidth:F1}x{_targetContentHeight:F1} "
                    + $"(outer {outerWidth:F1}x{outerHeight:F1} {deltaWidth:+0.0;-0.0}x{deltaHeight:+0.0;-0.0})");

                _ownerWindow.Width = outerWidth + deltaWidth;
                _ownerWindow.Height = outerHeight + deltaHeight;

                // Force a fresh stability observation before the next action.
                _lastConvergeWidth = -1;
                _lastConvergeHeight = -1;
            }
            catch (Exception ex)
            {
                Internal.DockDiag.Log($"HostWindowControl.ConvergeContentSize failed: {ex.Message}");
                _targetContentWidth = 0;
                _targetContentHeight = 0;
            }
        }

        /// <summary>
        /// The window's dock CONTENT host(s): the size-tracking DockableControl
        /// inside a Tool/Document pane (root wrapper and zero-size branches like
        /// the auto-hide flyout host excluded). Returns the first plus whether
        /// more than one exists — with several panes "the content size" is
        /// ambiguous and callers fall back to client-minus-standard-chrome.
        /// </summary>
        private (DockableControl Host, bool Multiple) FindContentHosts()
        {
            DockableControl first = null;

            try
            {
                foreach (var descendant in this.FindDescendants().OfType<DockableControl>())
                {
                    if (descendant.TrackingMode != TrackingMode.Visible || !descendant.IsSizeTracked)
                    {
                        continue;
                    }

                    if (descendant.DataContext is not IDock || descendant.DataContext is Model.Controls.IRootDock)
                    {
                        continue;
                    }

                    if (descendant.ActualWidth <= 0 || descendant.ActualHeight <= 0)
                    {
                        continue;
                    }

                    if (first is null)
                    {
                        first = descendant;
                    }
                    else
                    {
                        return (first, true);
                    }
                }
            }
            catch
            {
                // Tree mid-teardown.
            }

            return (first, false);
        }

        /// <summary>
        /// Outer-minus-client ESTIMATE in DIPs for the initial guess (the
        /// convergence step absorbs its error).
        /// </summary>
        private (double Width, double Height) GetFrameOverheadEstimateDips()
        {
            try
            {
                if (_ownerWindow.AppWindow is { } appWindow)
                {
                    var outer = appWindow.Size;
                    var client = appWindow.ClientSize;
                    var scale = GetDpiScale();

                    if (outer.Width >= client.Width && outer.Height >= client.Height && scale > 0)
                    {
                        return ((outer.Width - client.Width) / scale, (outer.Height - client.Height) / scale);
                    }
                }
            }
            catch
            {
                // Window mid-teardown — fall through to the zero estimate.
            }

            return (0.0, 0.0);
        }

        /// <summary>
        /// Client-minus-content overhead in DIPs: the single-row title strip plus
        /// the borders between the client edge and the content host (RootDockControl's
        /// 1px frame and the pane's 1px content border — both hardcoded in their
        /// templates). Present identically for tool and document floats.
        /// Border sides are snapped to PHYSICAL pixels the way XAML layout
        /// rounding does (a 1px border at 150% DPI arranges as 2 physical px =
        /// 1.333 DIPs); without the snap every float lost ~1.3 DIPs of content
        /// per border pair at fractional scales.
        /// </summary>
        private (double Width, double Height) GetContentChromeOverheadDips()
        {
            var scale = GetDpiScale();
            double Snap(double dips) => scale > 0
                ? Math.Round(dips * scale, MidpointRounding.AwayFromZero) / scale
                : dips;

            // Root frame + pane content border: 1px per side, two sides each.
            var borders = 4 * Snap(1.0);
            var titleRow = Snap(DockMetrics.GetDouble("DockFloatTitleBarHeight", 32.0));
            return (borders, titleRow + borders);
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

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        public void GetSize(out double width, out double height)
        {
            // Symmetric with SetSize: report the CONTENT area (stored-size
            // invariant). Preferred source is the MEASURED content host — the
            // same layout quantity the docked-side recorder writes — so
            // save/refloat round trips are exact. With several panes (or before
            // the tree is up) fall back to client minus the standard chrome,
            // mirroring what SetSize builds the window from.
            var (host, multiple) = FindContentHosts();
            if (host is not null && !multiple)
            {
                width = host.ActualWidth;
                height = host.ActualHeight;
                return;
            }

            width = ActualWidth > 0 ? ActualWidth : _ownerWindowWidth;
            height = ActualHeight > 0 ? ActualHeight : _ownerWindowHeight;

            var (chromeWidth, chromeHeight) = GetContentChromeOverheadDips();
            if (width > 0)
            {
                width = Math.Max(0, width - chromeWidth);
            }

            if (height > 0)
            {
                height = Math.Max(0, height - chromeHeight);
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

        // ----- Float single-row chrome: title-role arbitration -----
        //
        // Every tab strip living in this float window registers as a candidate;
        // the strip whose PANE sits closest to the window origin becomes the
        // title row: it stretches, gets the caption-button inset, and its blank
        // trailing fill is handed to Window.SetTitleBar (native move / snap /
        // double-click-maximize). Tabs stay client-area elements, so tab drag
        // keeps feeding the existing dock pipeline untouched.

        internal void RegisterTitleStrip(Internal.IFloatTitleBarStrip strip)
        {
            if (strip is null || _titleStripCandidates.Contains(strip))
            {
                return;
            }

            _titleStripCandidates.Add(strip);
            RequestTitleStripUpdate();
        }

        internal void UnregisterTitleStrip(Internal.IFloatTitleBarStrip strip)
        {
            if (!_titleStripCandidates.Remove(strip))
            {
                return;
            }

            if (ReferenceEquals(strip, _titleStrip))
            {
                // Demote NOW: the strip is unloading and a deferred demotion
                // would poke a dead template.
                TrySetTitleRole(strip, false);
                _titleStrip = null;
                TrySetWindowTitleBar(null);
            }

            RequestTitleStripUpdate();
        }

        /// <summary>Deferred + coalesced (dispatcher hop, NOT a timer): strips
        /// register from Loaded, before their first layout pass has produced
        /// meaningful positions.</summary>
        private void RequestTitleStripUpdate()
        {
            if (_titleStripUpdateQueued || _ownerWindowClosed)
            {
                return;
            }

            _titleStripUpdateQueued = true;
            var enqueued = DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                _titleStripUpdateQueued = false;
                UpdateTitleStrip();
            }) ?? false;

            if (!enqueued)
            {
                _titleStripUpdateQueued = false;
            }
        }

        /// <summary>Per-frame drift check (see LayoutUpdated): re-arbitrates only
        /// when the computed winner changed, so a settled window costs a couple
        /// of transform reads per frame at most.</summary>
        private void VerifyTitleStrip()
        {
            if (_titleStripUpdateQueued || _titleStripCandidates.Count == 0)
            {
                return;
            }

            if (!ReferenceEquals(PickTitleStrip(), _titleStrip))
            {
                RequestTitleStripUpdate();
            }
        }

        private void UpdateTitleStrip()
        {
            if (_ownerWindowClosed)
            {
                return;
            }

            var winner = PickTitleStrip();
            if (ReferenceEquals(winner, _titleStrip))
            {
                // Same owner; keep its inset fresh (RightInset can settle late).
                PushTitleStripInset();
                return;
            }

            if (_titleStrip is { } previous)
            {
                TrySetTitleRole(previous, false);
            }

            _titleStrip = winner;

            if (winner is null)
            {
                // No strip (transient, or a stripless layout): fall back to the
                // system's default top drag band rather than an immovable window.
                TrySetWindowTitleBar(null);
                return;
            }

            PushTitleStripInset();
            TrySetTitleRole(winner, true);
            TrySetWindowTitleBar(winner.TitleBarDragArea);

            Internal.DockDiag.Log($"HostWindowControl title role -> {Internal.DockDiag.Describe(winner.Strip)}");
        }

        /// <summary>The candidate whose pane origin is closest to the window's
        /// top-left ((y, x) lexicographic), i.e. the strip the single-row chrome
        /// belongs to. Candidates that cannot be transformed (mid-teardown) are
        /// skipped.</summary>
        private Internal.IFloatTitleBarStrip PickTitleStrip()
        {
            Internal.IFloatTitleBarStrip best = null;
            var bestX = double.MaxValue;
            var bestY = double.MaxValue;

            foreach (var candidate in _titleStripCandidates)
            {
                try
                {
                    var anchor = candidate.RankAnchor;
                    if (anchor is null || candidate.Strip.XamlRoot is null)
                    {
                        continue;
                    }

                    // A pane that has never been arranged (or is collapsed away)
                    // is not a real title-row candidate; its (0,0) origin would
                    // beat every rendered pane.
                    if (anchor.ActualWidth <= 0 || anchor.ActualHeight <= 0)
                    {
                        continue;
                    }

                    var origin = anchor.TransformToVisual(this).TransformPoint(new Windows.Foundation.Point(0, 0));
                    // Half-DIP tolerance: panes on the same row should tie on Y
                    // and fall through to the X comparison.
                    if (best is null
                        || origin.Y < bestY - 0.5
                        || (Math.Abs(origin.Y - bestY) <= 0.5 && origin.X < bestX))
                    {
                        best = candidate;
                        bestX = origin.X;
                        bestY = origin.Y;
                    }
                }
                catch
                {
                    // Candidate mid-teardown — not a valid title row.
                }
            }

            return best;
        }

        private void TrySetTitleRole(Internal.IFloatTitleBarStrip strip, bool enabled)
        {
            try
            {
                switch (strip)
                {
                    case ToolTabStrip tool:
                        tool.IsWindowTitleBar = enabled;
                        break;
                    case DocumentTabStrip document:
                        document.IsWindowTitleBar = enabled;
                        break;
                }
            }
            catch (Exception ex)
            {
                Internal.DockDiag.Log($"HostWindowControl title role toggle failed: {ex.Message}");
            }
        }

        private void TrySetWindowTitleBar(UIElement element)
        {
            try
            {
                _ownerWindow.SetTitleBar(element);
            }
            catch (Exception ex)
            {
                // Window mid-teardown, or not shown yet — the close path and the
                // next arbitration re-run cover both.
                Internal.DockDiag.Log($"HostWindowControl.SetTitleBar failed: {ex.Message}");
            }
        }

        /// <summary>Space the title strip must keep clear of the system caption
        /// buttons: AppWindow reports it in RAW pixels, the strip lays out in
        /// DIPs. Falls back to three standard buttons' worth when the window is
        /// not far enough along to report it.</summary>
        private void PushTitleStripInset()
        {
            if (_titleStrip is null || _ownerWindowClosed)
            {
                return;
            }

            var inset = 138.0; // 3 caption buttons x 46 DIPs
            try
            {
                var titleBar = _ownerWindow.AppWindow?.TitleBar;
                var scale = GetDpiScale();
                if (titleBar is { RightInset: > 0 } && scale > 0)
                {
                    inset = titleBar.RightInset / scale;
                }
            }
            catch
            {
                // Keep the fallback.
            }

            try
            {
                _titleStrip.SetTitleBarRightInset(inset);
            }
            catch
            {
                // Strip mid-teardown; unregistration follows.
            }
        }

        private readonly List<Internal.IFloatTitleBarStrip> _titleStripCandidates = new();
        private Internal.IFloatTitleBarStrip _titleStrip;
        private bool _titleStripUpdateQueued;

        private WindowEx _ownerWindow;
        private DockControl _dockControl;

        private double _ownerWindowX;
        private double _ownerWindowY;
        private double _ownerWindowWidth;
        private double _ownerWindowHeight;
        private bool _ownerWindowClosed;

        // Pending SetSize target (content DIPs) for the per-frame convergence
        // step; zero means no request in flight.
        private double _targetContentWidth;
        private double _targetContentHeight;
        private int _clientSizeFixups;
        private int _convergeWaitFrames;
        private double _lastConvergeWidth = -1;
        private double _lastConvergeHeight = -1;
    }
}

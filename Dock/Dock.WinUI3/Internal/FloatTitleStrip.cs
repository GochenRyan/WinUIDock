using CommunityToolkit.WinUI;
using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Dock.WinUI3.Internal
{
    /// <summary>
    /// A tab strip that can be promoted to a float window's single-row title
    /// bar (tabs left, blank caption-drag area, system buttons right).
    /// Implemented by <see cref="ToolTabStrip"/> and <see cref="DocumentTabStrip"/>;
    /// the promotion decision is made by <see cref="HostWindowControl"/>.
    /// </summary>
    internal interface IFloatTitleBarStrip
    {
        /// <summary>The strip control itself.</summary>
        Control Strip { get; }

        /// <summary>The blank fill element handed to Window.SetTitleBar — the
        /// native move/snap/double-click-maximize region.</summary>
        FrameworkElement TitleBarDragArea { get; }

        /// <summary>Element whose window position ranks this strip for the
        /// title role (the OWNING PANE, not the strip: a tool strip sits at the
        /// bottom of its pane until promotion flips it to the top).</summary>
        FrameworkElement RankAnchor { get; }

        /// <summary>Title-role switch. Setting it applies/restores the strip's
        /// chrome (height, caption-button inset, tab-width clamp) and any
        /// pane-side changes (tool strip placement, chrome caption).</summary>
        bool IsWindowTitleBar { get; set; }

        /// <summary>Space (DIPs) to keep clear of the system caption buttons at
        /// the right end. Pushed by the host window; only consumed while the
        /// strip holds the title role.</summary>
        void SetTitleBarRightInset(double dips);
    }

    /// <summary>
    /// The metric/registration mechanics shared by both strip types, so the
    /// two implementations cannot drift apart. Owns no policy: the strip's
    /// IsWindowTitleBar DP drives it.
    /// </summary>
    internal sealed class FloatTitleStripSupport
    {
        private readonly IFloatTitleBarStrip _owner;
        private readonly string _dockedStripHeightKey;
        private readonly double _dockedStripHeightFallback;

        private HostWindowControl _host;
        private ItemsPresenter _itemsPresenter;
        private double _rightInsetDips;
        private bool _applied;

        public FloatTitleStripSupport(IFloatTitleBarStrip owner, string dockedStripHeightKey, double dockedStripHeightFallback)
        {
            _owner = owner;
            _dockedStripHeightKey = dockedStripHeightKey;
            _dockedStripHeightFallback = dockedStripHeightFallback;

            owner.Strip.SizeChanged += (_, _) => UpdateTabAreaClamp();
        }

        /// <summary>The host window this strip registered with, if any.</summary>
        public HostWindowControl Host => _host;

        public void OnApplyTemplate(ItemsPresenter itemsPresenter)
        {
            _itemsPresenter = itemsPresenter;
            if (_applied)
            {
                UpdateTabAreaClamp();
            }
        }

        /// <summary>Registers with the float window's HostWindowControl as a
        /// title-role candidate. Strips inside any other window (the main
        /// window, an app's own dock-hosting window) never register, so their
        /// behavior is untouched.</summary>
        public void OnLoaded()
        {
            if (_host is not null)
            {
                return;
            }

            // The auto-hide flyout host: PinnedDockControl's template carries a
            // permanent (usually zero-size) ToolDockControl whose strip must
            // never own the window title row — unfiltered, it registers first
            // and its (0,0) origin wins the arbitration.
            if (_owner.Strip.FindAscendant<PinnedDockControl>() is not null)
            {
                return;
            }

            if (HostWindow.GetWindowForElement(_owner.Strip) is HostWindow hostWindow
                && hostWindow.WindowContent is HostWindowControl host)
            {
                _host = host;
                host.RegisterTitleStrip(_owner);
            }
        }

        public void OnUnloaded()
        {
            if (_host is { } host)
            {
                _host = null;
                host.UnregisterTitleStrip(_owner);
            }
        }

        public void SetRightInset(double dips)
        {
            _rightInsetDips = double.IsNaN(dips) || dips < 0 ? 0 : dips;
            if (_applied)
            {
                ApplyPadding();
                UpdateTabAreaClamp();
            }
        }

        /// <summary>Applies (or restores) the title-row metrics. Restore is
        /// EXPLICIT: promotion wrote local values over the pane template's
        /// ThemeResource ones, and ClearValue does not bring template-inflated
        /// values back.</summary>
        public void ApplyTitleRole(bool enabled)
        {
            if (_applied == enabled)
            {
                return;
            }

            _applied = enabled;
            var strip = _owner.Strip;

            if (enabled)
            {
                var height = DockMetrics.GetDouble("DockFloatTitleBarHeight", 32.0);
                strip.Height = height;
                strip.MaxHeight = height;
                ApplyPadding();
            }
            else
            {
                strip.ClearValue(FrameworkElement.HeightProperty);
                strip.MaxHeight = DockMetrics.GetDouble(_dockedStripHeightKey, _dockedStripHeightFallback);
                strip.ClearValue(Control.PaddingProperty);
            }

            UpdateTabAreaClamp();
        }

        private void ApplyPadding()
        {
            _owner.Strip.Padding = new Thickness(0, 0, _rightInsetDips, 0);
        }

        /// <summary>In title-role mode, caps the tab area so the blank
        /// caption-drag region never collapses to zero and tabs never slide
        /// under the system caption buttons.</summary>
        private void UpdateTabAreaClamp()
        {
            if (_itemsPresenter is null)
            {
                return;
            }

            if (!_applied)
            {
                _itemsPresenter.ClearValue(FrameworkElement.MaxWidthProperty);
                return;
            }

            var stripWidth = _owner.Strip.ActualWidth;
            if (double.IsNaN(stripWidth) || stripWidth <= 0)
            {
                return;
            }

            var minDrag = DockMetrics.GetDouble("DockFloatTitleBarMinDragWidth", 48.0);
            var max = stripWidth - _rightInsetDips - minDrag;
            _itemsPresenter.MaxWidth = max > 0 ? max : 0;
        }
    }
}

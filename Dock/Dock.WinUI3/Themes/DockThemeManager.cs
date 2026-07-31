using System;
using System.Collections.Generic;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Dock.WinUI3
{
    /// <summary>
    /// Window backdrop material for dock windows (Spec 05). Mica variants are
    /// cheap (static wallpaper sampling); Acrylic is live blur and additionally
    /// gated by <see cref="DockThemeManager.SetAcrylicEnabled"/>.
    /// </summary>
    public enum DockBackdrop
    {
        None,
        Mica,
        MicaAlt,
        Acrylic,
    }

    /// <summary>
    /// Runtime theme switching for Dock.WinUI3.
    ///
    /// The library ships two themes in Themes/DockDefaults.xaml: "Default" (the
    /// black theme, primary) and "Light". Application-level RequestedTheme cannot
    /// change after startup in WinUI 3, so switching is done the supported way:
    /// setting RequestedTheme on each window's root element, which makes every
    /// {ThemeResource} reference re-resolve. Register each window's root via
    /// <see cref="RegisterRoot"/> and call <see cref="SetTheme"/> to switch.
    ///
    /// Note: the OS title bar does not follow element themes on WindowsAppSDK 1.5
    /// (AppWindowTitleBar.PreferredTheme requires 1.7) — hosts should re-apply
    /// their title bar colors after switching (resolve them via
    /// <see cref="TryGetResource(string, out object)"/>).
    /// </summary>
    public static class DockThemeManager
    {
        private static readonly List<WeakReference<FrameworkElement>> _roots = new();
        private static readonly List<WeakReference<Window>> _windows = new();
        private static readonly List<(WeakReference<Window> Window, DockBackdrop Backdrop)> _backdrops = new();
        private static ElementTheme _currentTheme = ElementTheme.Default;
        private static bool _acrylicEnabled;

        // The fixed set of AcrylicBrush keys shipped in DockDefaults.xaml; the
        // manual acrylic switch flips AlwaysUseFallback on these instances.
        private static readonly string[] _acrylicBrushKeys =
        {
            "DockAcrylicFlyoutBrush",
            "DockAcrylicGuidePlateBrush",
            "DockAcrylicChromeBrush",
        };

        /// <summary>The theme last applied through <see cref="SetTheme"/>.</summary>
        public static ElementTheme CurrentTheme => _currentTheme;

        /// <summary>
        /// Whether acrylic materials are enabled. Defaults to false: the
        /// DockAcrylic* brushes ship with AlwaysUseFallback="True" and render
        /// their solid fallback colors, so an opted-out app looks exactly like
        /// the plain black/light theme at zero material cost.
        /// </summary>
        public static bool IsAcrylicEnabled => _acrylicEnabled;

        /// <summary>
        /// Backdrop applied to windows registered after this is set (float
        /// windows are created by the library, so hosts cannot reach them with
        /// <see cref="SetBackdrop"/> directly). Does not retro-apply.
        /// </summary>
        public static DockBackdrop DefaultBackdrop { get; set; } = DockBackdrop.None;

        /// <summary>
        /// Sets a window's backdrop material. Mica variants apply immediately;
        /// Acrylic is additionally gated by <see cref="SetAcrylicEnabled"/> —
        /// while acrylic is disabled the window keeps no backdrop and gets the
        /// DesktopAcrylicBackdrop once acrylic is enabled.
        /// </summary>
        public static void SetBackdrop(Window window, DockBackdrop backdrop)
        {
            if (window is null)
            {
                return;
            }

            for (int i = _backdrops.Count - 1; i >= 0; i--)
            {
                if (!_backdrops[i].Window.TryGetTarget(out var existing))
                {
                    _backdrops.RemoveAt(i);
                }
                else if (ReferenceEquals(existing, window))
                {
                    _backdrops.RemoveAt(i);
                }
            }

            _backdrops.Add((new WeakReference<Window>(window), backdrop));
            ApplyBackdrop(window, backdrop);
        }

        /// <summary>
        /// The manual acrylic switch (Spec 05): flips AlwaysUseFallback on the
        /// library's AcrylicBrush instances (both theme dictionaries), so every
        /// consuming surface swaps between live acrylic and its solid fallback
        /// immediately — no template change, no theme re-evaluation. Acrylic
        /// window backdrops are demoted to none while disabled; Mica backdrops
        /// are unaffected (near-zero cost).
        /// </summary>
        public static void SetAcrylicEnabled(bool enabled)
        {
            _acrylicEnabled = enabled;

            foreach (var key in _acrylicBrushKeys)
            {
                foreach (var theme in new[] { ElementTheme.Dark, ElementTheme.Light })
                {
                    if (TryGetResource(key, theme, out var value) && value is AcrylicBrush acrylic)
                    {
                        acrylic.AlwaysUseFallback = !enabled;
                    }
                }
            }

            for (int i = _backdrops.Count - 1; i >= 0; i--)
            {
                if (!_backdrops[i].Window.TryGetTarget(out var window))
                {
                    _backdrops.RemoveAt(i);
                    continue;
                }

                if (_backdrops[i].Backdrop == DockBackdrop.Acrylic)
                {
                    ApplyBackdrop(window, DockBackdrop.Acrylic);
                }
            }
        }

        private static void ApplyBackdrop(Window window, DockBackdrop backdrop)
        {
            try
            {
                window.SystemBackdrop = backdrop switch
                {
                    DockBackdrop.Mica => new MicaBackdrop(),
                    DockBackdrop.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
                    DockBackdrop.Acrylic when _acrylicEnabled => new DesktopAcrylicBackdrop(),
                    _ => null,
                };
            }
            catch
            {
                // Closed window — the weak reference drops it from the registry.
            }
        }

        /// <summary>
        /// Registers a window: its content root follows SetTheme AND its OS title
        /// bar (caption + buttons) is recolored from the dock theme keys. Prefer
        /// this over RegisterRoot for top-level windows.
        /// </summary>
        public static void RegisterWindow(Window window)
        {
            if (window is null)
            {
                return;
            }

            foreach (var reference in _windows)
            {
                if (reference.TryGetTarget(out var existing) && ReferenceEquals(existing, window))
                {
                    return;
                }
            }

            _windows.Add(new WeakReference<Window>(window));

            if (window.Content is FrameworkElement root)
            {
                RegisterRoot(root);
            }

            TryApplyTitleBar(window);

            if (DefaultBackdrop != DockBackdrop.None)
            {
                SetBackdrop(window, DefaultBackdrop);
            }

            // Title bar colors set before the window is first activated can be
            // silently dropped by the shell — re-apply once it is really up.
            void OnFirstActivated(object sender, WindowActivatedEventArgs args)
            {
                window.Activated -= OnFirstActivated;
                TryApplyTitleBar(window);
            }

            window.Activated += OnFirstActivated;
        }

        /// <summary>
        /// Registers a visual root (typically Window.Content) that SetTheme will
        /// re-theme. Roots are held weakly; closed windows drop out automatically.
        /// If a theme was already applied, the new root is themed immediately.
        /// </summary>
        public static void RegisterRoot(FrameworkElement root)
        {
            if (root is null)
            {
                return;
            }

            foreach (var reference in _roots)
            {
                if (reference.TryGetTarget(out var existing) && ReferenceEquals(existing, root))
                {
                    return;
                }
            }

            _roots.Add(new WeakReference<FrameworkElement>(root));

            if (_currentTheme != ElementTheme.Default)
            {
                try
                {
                    root.RequestedTheme = _currentTheme;
                }
                catch
                {
                    _roots.RemoveAt(_roots.Count - 1);
                }
            }
        }

        /// <summary>
        /// Switches every registered root to the given theme. ElementTheme.Default
        /// follows the OS theme; Dark selects the black theme, Light the light one.
        /// </summary>
        public static void SetTheme(ElementTheme theme)
        {
            _currentTheme = theme;

            for (int i = _roots.Count - 1; i >= 0; i--)
            {
                if (!_roots[i].TryGetTarget(out var root))
                {
                    _roots.RemoveAt(i);
                    continue;
                }

                try
                {
                    root.RequestedTheme = theme;
                }
                catch
                {
                    // Element belongs to an already-closed window ("The WinUI
                    // Desktop Window object has already been closed") that the GC
                    // has not collected yet — prune instead of surfacing.
                    _roots.RemoveAt(i);
                }
            }

            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                if (!_windows[i].TryGetTarget(out var window))
                {
                    _windows.RemoveAt(i);
                    continue;
                }

                // Do NOT prune on a failed application: failures can be transient
                // (window not fully up yet); actually-dead windows drop out via
                // the weak reference.
                TryApplyTitleBar(window);
            }
        }

        /// <summary>
        /// Recolors a window's OS title bar (caption + min/max/close buttons) from
        /// the current theme's dock keys. No-ops where title bar customization is
        /// unsupported; returns false when the window is already closed.
        /// </summary>
        public static bool TryApplyTitleBar(Window window)
        {
            try
            {
                // Standard (non-extended) title bars: the DWM immersive dark flag
                // is the reliable switch for the caption color; the color APIs
                // below refine it (and cover extended title bars' caption buttons).
                var isDark = _currentTheme == ElementTheme.Dark
                    || (_currentTheme == ElementTheme.Default
                        && Application.Current?.RequestedTheme == ApplicationTheme.Dark);
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var darkMode = isDark ? 1 : 0;
                _ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref darkMode, sizeof(int));

                if (!AppWindowTitleBar.IsCustomizationSupported())
                {
                    return true;
                }

                var titleBar = window?.AppWindow?.TitleBar;
                if (titleBar is null)
                {
                    return true;
                }

                var background = GetColor("DockBackgroundBrush", Color.FromArgb(255, 26, 26, 26));
                var foreground = GetColor("DockCaptionForegroundBrush", Color.FromArgb(255, 197, 197, 197));
                var hover = GetColor("DockCaptionButtonHoverBrush", Color.FromArgb(51, 255, 255, 255));
                var pressed = GetColor("DockCaptionButtonPressedBrush", Color.FromArgb(31, 255, 255, 255));
                var activeForeground = GetColor("DockCaptionActiveForegroundBrush", Color.FromArgb(255, 255, 255, 255));

                titleBar.BackgroundColor = background;
                titleBar.InactiveBackgroundColor = background;
                titleBar.ForegroundColor = foreground;
                titleBar.InactiveForegroundColor = foreground;
                titleBar.ButtonBackgroundColor = background;
                titleBar.ButtonInactiveBackgroundColor = background;
                titleBar.ButtonForegroundColor = foreground;
                titleBar.ButtonInactiveForegroundColor = foreground;
                titleBar.ButtonHoverBackgroundColor = hover;
                titleBar.ButtonHoverForegroundColor = activeForeground;
                titleBar.ButtonPressedBackgroundColor = pressed;
                titleBar.ButtonPressedForegroundColor = activeForeground;

                return true;
            }
            catch
            {
                // Closed window — caller prunes.
                return false;
            }
        }

        private static Color GetColor(string key, Color fallback)
        {
            return TryGetResource(key, out var value) ? value switch
            {
                SolidColorBrush solid => solid.Color,
                // Hosts may override a color key with an AcrylicBrush; the OS
                // title bar APIs need a plain color, so use its fallback.
                AcrylicBrush acrylic => acrylic.FallbackColor,
                _ => fallback,
            } : fallback;
        }

        private const int DwmwaUseImmersiveDarkMode = 20;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        /// <summary>
        /// Resolves a resource key for the currently applied theme without throwing
        /// when the host registered no ThemeDictionaries (the library defaults from
        /// Themes/DockDefaults.xaml apply). Use this instead of indexing
        /// Application.Current.Resources.ThemeDictionaries directly.
        /// </summary>
        public static bool TryGetResource(string key, out object value)
        {
            var theme = _currentTheme;
            if (theme == ElementTheme.Default)
            {
                theme = Application.Current?.RequestedTheme == ApplicationTheme.Light
                    ? ElementTheme.Light
                    : ElementTheme.Dark;
            }

            return TryGetResource(key, theme, out value);
        }

        /// <summary>Resolves a resource key for an explicit theme.</summary>
        public static bool TryGetResource(string key, ElementTheme theme, out object value)
        {
            value = null;
            var app = Application.Current;
            if (app is null)
            {
                return false;
            }

            var themeName = theme == ElementTheme.Light ? "Light" : "Dark";
            return TryGetFromDictionary(app.Resources, key, themeName, out value);
        }

        private static bool TryGetFromDictionary(ResourceDictionary dictionary, string key, string themeName, out object value)
        {
            value = null;
            if (dictionary is null)
            {
                return false;
            }

            // Explicit theme dictionaries FIRST: a bare ResourceDictionary lookup
            // resolves ThemeDictionaries against the APP-level theme, which is
            // fixed at startup and can disagree with the dock theme being queried
            // (e.g. host declares RequestedTheme="Dark" while the dock theme is
            // Light) — it would return the wrong theme's values.
            if (dictionary.ThemeDictionaries.TryGetValue(themeName, out var themeObj)
                && themeObj is ResourceDictionary themeDict
                && themeDict.TryGetValue(key, out value)
                && value is not null)
            {
                return true;
            }

            if (dictionary.ThemeDictionaries.TryGetValue("Default", out var defaultObj)
                && defaultObj is ResourceDictionary defaultDict
                && defaultDict.TryGetValue(key, out value)
                && value is not null)
            {
                return true;
            }

            for (int i = dictionary.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                if (TryGetFromDictionary(dictionary.MergedDictionaries[i], key, themeName, out value))
                {
                    return true;
                }
            }

            // Bare lookup LAST — covers theme-invariant keys and host overrides,
            // accepting the app-theme resolution quirk described above.
            return dictionary.TryGetValue(key, out value) && value is not null;
        }
    }
}

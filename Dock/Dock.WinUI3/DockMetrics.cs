namespace Dock.WinUI3
{
    /// <summary>
    /// Safe accessors for the metric resources defined in Themes/DockDefaults.xaml,
    /// for the C#-side consumers that cannot use {ThemeResource} (measure passes,
    /// grid sizing). Hosts override a metric by redefining its key in
    /// Application.Resources; the fallback matches the library default.
    /// </summary>
    public static class DockMetrics
    {
        public static double GetDouble(string key, double fallback)
        {
            return DockThemeManager.TryGetResource(key, out var value) && value is double d
                ? d
                : fallback;
        }
    }
}

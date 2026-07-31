using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Dock.WinUI3.Controls
{
    /// <summary>
    /// Attached helper that assigns vector icon geometry to a Path from a string
    /// resource (e.g. {StaticResource DockIconCloseData}).
    ///
    /// Why not a shared Style with a Data setter: Geometry instances in WinUI are
    /// single-association objects — a style-provided geometry shared by several
    /// Paths silently fails to render. This attached property converts the path
    /// data string into a FRESH Geometry per element.
    ///
    /// Consume with {StaticResource}: {ThemeResource} references to custom
    /// attached properties are silently ignored by the WinUI 3 runtime (verified
    /// empirically), and icon geometry is theme-invariant anyway.
    ///
    /// Icon data strings use a 16x16 design box (see Themes/DockDefaults.xaml);
    /// hosts override an icon by redefining its DockIcon*Data string key.
    /// </summary>
    // NOTE: must be a non-static DependencyObject subclass — the WinUI 3 XAML
    // compiler silently ignores attached properties hosted on static classes.
    public class DockIcon : DependencyObject
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.RegisterAttached(
                "Data",
                typeof(string),
                typeof(DockIcon),
                new PropertyMetadata(null, OnDataChanged));

        public static void SetData(DependencyObject element, string value) => element.SetValue(DataProperty, value);

        public static string GetData(DependencyObject element) => (string)element.GetValue(DataProperty);

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Path path)
            {
                return;
            }

            var data = e.NewValue as string;
            if (string.IsNullOrEmpty(data))
            {
                path.Data = null;
                return;
            }

            path.Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), data);
        }
    }
}

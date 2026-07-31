using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Dock.WinUI3.Controls
{
    /// <summary>
    /// A single vector dock guide (rosette segment) shown by DockTarget during a
    /// drag: a themed rounded plate with a vector glyph. The glyph geometry comes
    /// from the GlyphData string (see the DockGuideGlyph*Data keys in
    /// Themes/DockDefaults.xaml, 40x40 design box) and is converted to a fresh
    /// Geometry per element (Geometry instances cannot be shared in WinUI).
    /// IsHighlighted is driven positionally by DockTarget while the pointer is
    /// over the guide.
    /// </summary>
    [TemplatePart(Name = PlatePartName, Type = typeof(Border))]
    [TemplatePart(Name = GlyphPartName, Type = typeof(Path))]
    public sealed class DockTargetGuide : Control
    {
        public const string PlatePartName = "PART_Plate";
        public const string GlyphPartName = "PART_Glyph";

        public DockTargetGuide()
        {
            this.DefaultStyleKey = typeof(DockTargetGuide);
        }

        public static readonly DependencyProperty GlyphDataProperty = DependencyProperty.Register(
            nameof(GlyphData),
            typeof(string),
            typeof(DockTargetGuide),
            new PropertyMetadata(null, (d, _) => ((DockTargetGuide)d).ApplyGlyph()));

        public string GlyphData
        {
            get => (string)GetValue(GlyphDataProperty);
            set => SetValue(GlyphDataProperty, value);
        }

        public static readonly DependencyProperty IsHighlightedProperty = DependencyProperty.Register(
            nameof(IsHighlighted),
            typeof(bool),
            typeof(DockTargetGuide),
            new PropertyMetadata(false, (d, _) => ((DockTargetGuide)d).ApplyHighlight()));

        public bool IsHighlighted
        {
            get => (bool)GetValue(IsHighlightedProperty);
            set => SetValue(IsHighlightedProperty, value);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _plate = GetTemplateChild(PlatePartName) as Border;
            _glyph = GetTemplateChild(GlyphPartName) as Path;

            ApplyGlyph();
            ApplyHighlight();
        }

        private void ApplyGlyph()
        {
            var data = GlyphData;
            if (_glyph is null)
            {
                return;
            }

            _glyph.Data = string.IsNullOrEmpty(data)
                ? null
                : (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), data);
        }

        // Theme-aware lookup at toggle time so host overrides and runtime theme
        // switches are honored.
        private void ApplyHighlight()
        {
            if (_plate is null || _glyph is null)
            {
                return;
            }

            SetBrush(brush => _plate.Background = brush,
                IsHighlighted ? "DockGuideHoverBackgroundBrush" : "DockGuideBackgroundBrush");
            SetBrush(brush => _glyph.Fill = brush,
                IsHighlighted ? "DockGuideGlyphAccentBrush" : "DockGuideGlyphBrush");
        }

        private static void SetBrush(System.Action<Brush> apply, string key)
        {
            if (DockThemeManager.TryGetResource(key, out var value) && value is Brush brush)
            {
                apply(brush);
            }
        }

        private Border _plate;
        private Path _glyph;
    }
}

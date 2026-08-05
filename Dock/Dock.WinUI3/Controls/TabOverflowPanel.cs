using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Dock.WinUI3.Controls
{
    /// <summary>
    /// A single-row tab host that clips overflow and exposes a scroll offset.
    /// A plain Panel, NOT a ScrollViewer: a ScrollViewer around a templated tab
    /// strip dies natively on window close (unhandled E_ACCESSDENIED, no
    /// managed stack).
    /// </summary>
    public sealed class TabOverflowPanel : Panel
    {
        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
            nameof(Offset),
            typeof(double),
            typeof(TabOverflowPanel),
            new PropertyMetadata(0.0, OnOffsetChanged));

        /// <summary>How far the row is shifted left, in DIPs. Clamped to the
        /// overflow during arrange, so callers can add deltas blindly.</summary>
        public double Offset
        {
            get => (double)GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        private static void OnOffsetChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            (ob as TabOverflowPanel)?.InvalidateArrange();
        }

        /// <summary>Total width of all children, in DIPs (last measure).</summary>
        public double ExtentWidth => _extentWidth;

        /// <summary>Width actually available for the row (last arrange).</summary>
        public double ViewportWidth => _viewportWidth;

        /// <summary>Adjusts <see cref="Offset"/> so <paramref name="child"/> is
        /// fully inside the viewport (left-aligned when wider than it).</summary>
        public void EnsureVisible(UIElement child)
        {
            var left = 0.0;

            foreach (var candidate in Children)
            {
                if (ReferenceEquals(candidate, child))
                {
                    var width = child.DesiredSize.Width;

                    if (left < Offset)
                    {
                        Offset = left;
                    }
                    else if (left + width > Offset + _viewportWidth && _viewportWidth > 0)
                    {
                        Offset = Math.Min(left, left + width - _viewportWidth);
                    }

                    return;
                }

                left += candidate.DesiredSize.Width;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var height = 0.0;
            _extentWidth = 0.0;

            foreach (var child in Children)
            {
                try
                {
                    // Finite constraint on purpose: the tab templates throw
                    // native E_FAIL on every pass when measured with infinity.
                    child.Measure(availableSize);
                }
                catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException
                                               or ArgumentException
                                               or UnauthorizedAccessException)
                {
                    // Teardown-time refusal, thrown at the native boundary — it
                    // never reaches the child's own MeasureOverride, so the
                    // guard must sit here. Keep the last desired size.
                    Internal.DockDiag.Log($"TabOverflowPanel: child measure refused: {ex.Message}");
                }

                _extentWidth += child.DesiredSize.Width;
                height = Math.Max(height, child.DesiredSize.Height);
            }

            var width = double.IsInfinity(availableSize.Width)
                ? _extentWidth
                : Math.Min(availableSize.Width, _extentWidth);

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _viewportWidth = finalSize.Width;

            var maxOffset = Math.Max(0, _extentWidth - finalSize.Width);
            var offset = Math.Clamp(Offset, 0, maxOffset);

            if (Math.Abs(offset - Offset) > 0.5)
            {
                Offset = offset;
            }

            var x = -offset;
            foreach (var child in Children)
            {
                try
                {
                    child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
                }
                catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException
                                               or ArgumentException
                                               or UnauthorizedAccessException)
                {
                    Internal.DockDiag.Log($"TabOverflowPanel: child arrange refused: {ex.Message}");
                }

                x += child.DesiredSize.Width;
            }

            Clip = new RectangleGeometry { Rect = new Rect(0, 0, finalSize.Width, finalSize.Height) };
            return finalSize;
        }

        private double _extentWidth;
        private double _viewportWidth;
    }
}

using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Dock.WinUI3.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = PinnedDockGridName, Type = typeof(Grid))]
    [TemplatePart(Name = PinnedToolDockName, Type = typeof(ToolDockControl))]
    [TemplatePart(Name = SplitterName, Type = typeof(GridSplitter))]
    public sealed class PinnedDockControl : Control
    {
        public const string PinnedDockGridName = "PART_PinnedDockGrid";
        public const string PinnedToolDockName = "PART_ToolDockControl";
        public const string SplitterName = "PART_Splitter";

        public PinnedDockControl()
        {
            this.DefaultStyleKey = typeof(PinnedDockControl);
            Loaded += PinnedDockControl_Loaded;

            // The flyout is a transient surface (Spec 05): its outer dockable
            // gets the acrylic flyout brush. The brush is resolved per theme in
            // code, so re-resolve when the element theme changes.
            ActualThemeChanged += (_, _) => ApplyFlyoutSurface();
        }

        private void PinnedDockControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += PinnedDockControl_DataContextChanged;
        }

        private void PinnedDockControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        public static readonly DependencyProperty PinnedDockAlignmentProperty = DependencyProperty.Register(
            nameof(PinnedDockAlignment),
            typeof(Alignment),
            typeof(PinnedDockControl),
            new PropertyMetadata(Alignment.Unset, OnAlignmentChanged));

        public Alignment PinnedDockAlignment
        {
            get => (Alignment)GetValue(PinnedDockAlignmentProperty);
            set => SetValue(PinnedDockAlignmentProperty, value);
        }
        private static void OnAlignmentChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            var control = ob as PinnedDockControl;
            control.UpdateGrid();
        }

        private void UpdateGrid()
        {
            if (_pinnedToolDock == null || _pinnedDockGrid == null)
                return;

            _pinnedDockGrid.RowDefinitions.Clear();
            _pinnedDockGrid.ColumnDefinitions.Clear();

            var window = HostWindow.GetWindowForElement(this);

            double maxHeight = 960.0;
            double maxWidth = 1080.0;

            if (window != null)
            {
                maxHeight = window.Bounds.Height;
                maxWidth = window.Bounds.Width;
            }

            switch (PinnedDockAlignment)
            {
                case Alignment.Unset:
                case Alignment.Left:
                    _pinnedDockGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        MinWidth = 0,
                        MaxWidth = maxWidth,
                        Width = new GridLength(DockMetrics.GetDouble("DockPinnedFlyoutSize", 300.0))
                    });
                    _pinnedDockGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        Width = GridLength.Auto
                    });
                    _pinnedDockGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    _splitter.ResizeDirection = GridSplitter.GridResizeDirection.Columns;

                    Grid.SetColumn(_pinnedToolDock, 0);
                    Grid.SetRow(_pinnedToolDock, 0);
                    Grid.SetColumn(_splitter, 1);
                    Grid.SetRow(_splitter, 0);
                    break;
                case Alignment.Bottom:
                    _pinnedDockGrid.RowDefinitions.Add(new RowDefinition());
                    _pinnedDockGrid.RowDefinitions.Add(new RowDefinition()
                    {
                        Height = GridLength.Auto
                    });
                    _pinnedDockGrid.RowDefinitions.Add(new RowDefinition()
                    {
                        MinHeight = 0,
                        MaxHeight = maxHeight,
                        Height = new GridLength(DockMetrics.GetDouble("DockPinnedFlyoutSize", 300.0))
                    });
                    _splitter.ResizeDirection = GridSplitter.GridResizeDirection.Rows;

                    Grid.SetColumn(_pinnedToolDock, 0);
                    Grid.SetRow(_pinnedToolDock, 2);
                    Grid.SetColumn(_splitter, 0);
                    Grid.SetRow(_splitter, 1);
                    break;
                case Alignment.Right:
                    _pinnedDockGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    _pinnedDockGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        Width = GridLength.Auto
                    });
                    _pinnedDockGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        MinWidth = 0,
                        MaxWidth = maxWidth,
                        Width = new GridLength(DockMetrics.GetDouble("DockPinnedFlyoutSize", 300.0))
                    });
                    _splitter.ResizeDirection = GridSplitter.GridResizeDirection.Columns;

                    Grid.SetColumn(_pinnedToolDock, 2);
                    Grid.SetRow(_pinnedToolDock, 0);
                    Grid.SetColumn(_splitter, 1);
                    Grid.SetRow(_splitter, 0);
                    break;
                case Alignment.Top:
                    _pinnedDockGrid.RowDefinitions.Add(new RowDefinition()
                    {
                        MinHeight = 0,
                        MaxHeight = maxHeight,
                        Height = new GridLength(DockMetrics.GetDouble("DockPinnedFlyoutSize", 300.0))
                    });
                    _pinnedDockGrid.RowDefinitions.Add(new RowDefinition()
                    {
                        Height = GridLength.Auto
                    });
                    _pinnedDockGrid.RowDefinitions.Add(new RowDefinition()
                    {
                    });
                    _splitter.ResizeDirection = GridSplitter.GridResizeDirection.Rows;

                    Grid.SetColumn(_pinnedToolDock, 1);
                    Grid.SetRow(_pinnedToolDock, 0);
                    Grid.SetColumn(_splitter, 1);
                    Grid.SetRow(_splitter, 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _pinnedDockGrid = GetTemplateChild(PinnedDockGridName) as Grid;
            _pinnedToolDock = GetTemplateChild(PinnedToolDockName) as ToolDockControl;
            _splitter = GetTemplateChild(SplitterName) as GridSplitter;

            BindData();
            UpdateGrid();

            // Inner templates are not applied yet at this point; retry on
            // layout until the flyout's surface elements exist.
            LayoutUpdated += TryApplyFlyoutMaterial;
        }

        /// <summary>
        /// Locates the flyout's paint surfaces once the inner templates exist:
        /// the outer DockableControl (solid DockBackgroundBrush by template)
        /// becomes the acrylic flyout surface, and the tool pane border becomes
        /// transparent so the surface shows through the content area. With
        /// acrylic disabled (the default) the surface renders the brush's solid
        /// FallbackColor — visually the plain theme.
        /// </summary>
        private void TryApplyFlyoutMaterial(object sender, object e)
        {
            if (_pinnedToolDock?.FindDescendant<DockableControl>() is not DockableControl surface)
            {
                return;
            }

            var paneBorder = _pinnedToolDock.FindDescendant<Border>(b => b.Name == "PART_Border");
            if (paneBorder is null)
            {
                return;
            }

            LayoutUpdated -= TryApplyFlyoutMaterial;
            _flyoutSurface = surface;
            paneBorder.Background = null;
            ApplyFlyoutSurface();
        }

        private void ApplyFlyoutSurface()
        {
            if (_flyoutSurface is null)
            {
                return;
            }

            var theme = ActualTheme == ElementTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
            if (DockThemeManager.TryGetResource("DockAcrylicFlyoutBrush", theme, out var value)
                && value is Brush brush)
            {
                _flyoutSurface.Background = brush;
            }
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext == null)
                Visibility = Visibility.Collapsed;

            if (DataContext is ToolDock)
            {
                Visibility = Visibility.Visible;

                _pinnedDockGrid.ClearValue(Grid.VisibilityProperty);
                _pinnedDockGrid.SetBinding(Grid.VisibilityProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("IsEmpty"),
                    Mode = BindingMode.TwoWay,
                    Converter = DockConverters.DockBoolToVisibilityConverter,
                    ConverterParameter = true,
                    FallbackValue = Visibility.Collapsed
                });

                _pinnedToolDock.ClearValue(ToolDockControl.DataContextProperty);
                _pinnedToolDock.SetBinding(ToolDockControl.DataContextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath(""),
                    Mode = BindingMode.OneWay
                });

                ClearValue(PinnedDockAlignmentProperty);
                SetBinding(PinnedDockAlignmentProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("Alignment"),
                    Mode = BindingMode.OneWay
                });
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = base.MeasureOverride(availableSize);
            _pinnedToolDock.InvalidateMeasure();
            return size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        private Grid _pinnedDockGrid;
        private ToolDockControl _pinnedToolDock;
        private GridSplitter _splitter;
        private DockableControl _flyoutSurface;
    }
}

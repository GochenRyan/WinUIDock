using CommunityToolkit.WinUI.Controls;
using Dock.Model.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = PinnedDockGridName, Type = typeof(Grid))]
    [TemplatePart(Name = PinnedDockName, Type = typeof(ContentControl))]
    [TemplatePart(Name = SplitterName, Type = typeof(GridSplitter))]
    public sealed class PinnedDockControl : Control
    {
        public const string PinnedDockGridName = "PART_PinnedDockGrid";
        public const string PinnedDockName = "PART_PinnedDock";
        public const string SplitterName = "PART_Splitter";

        public PinnedDockControl()
        {
            this.DefaultStyleKey = typeof(PinnedDockControl);
        }

        public static DependencyProperty PinnedDockAlignmentProperty = DependencyProperty.Register(
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
            if (_pinnedDock == null || _pinnedDockGrid == null)
                return;

            _pinnedDockGrid.RowDefinitions.Clear();
            _pinnedDockGrid.ColumnDefinitions.Clear();

            switch (PinnedDockAlignment)
            {
                case Alignment.Unset:
                case Alignment.Left:
                    _pinnedDockGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        MinWidth = 50,
                        Width = GridLength.Auto
                    });
                    _pinnedDockGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        Width = GridLength.Auto
                    });
                    _pinnedDockGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        MinWidth = 50,
                        Width = new GridLength(1, GridUnitType.Star)
                    });

                    Grid.SetColumn(_pinnedDock, 0);
                    Grid.SetRow(_pinnedDock, 0);
                    Grid.SetColumn(_splitter, 1);
                    Grid.SetRow(_splitter, 0);
                    break;
                case Alignment.Bottom:
                    _pinnedDockGrid.RowDefinitions.Add(new RowDefinition()
                    {
                        MinHeight = 50,
                        Height = new GridLength(1, GridUnitType.Star)
                    });
                    _pinnedDockGrid.RowDefinitions.Add(new RowDefinition()
                    {
                        Height = GridLength.Auto
                    });
                    _pinnedDockGrid.RowDefinitions.Add(new RowDefinition()
                    {
                        MinHeight = 50,
                        Height = GridLength.Auto
                    });

                    Grid.SetColumn(_pinnedDock, 0);
                    Grid.SetRow(_pinnedDock, 2);
                    Grid.SetColumn(_splitter, 0);
                    Grid.SetRow(_splitter, 1);
                    break;
                case Alignment.Right:
                    _pinnedDockGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        MinWidth = 50,
                        Width = new GridLength(1, GridUnitType.Star)
                    });
                    _pinnedDockGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        Width = GridLength.Auto
                    });
                    _pinnedDockGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        MinWidth = 50,
                        Width = GridLength.Auto
                    });

                    Grid.SetColumn(_pinnedDock, 2);
                    Grid.SetRow(_pinnedDock, 0);
                    Grid.SetColumn(_splitter, 1);
                    Grid.SetRow(_splitter, 0);
                    break;
                case Alignment.Top:
                    _pinnedDockGrid.RowDefinitions.Add(new RowDefinition()
                    {
                        MinHeight = 50,
                        Height = GridLength.Auto
                    });
                    _pinnedDockGrid.RowDefinitions.Add(new RowDefinition()
                    {
                        Height = GridLength.Auto
                    });
                    _pinnedDockGrid.RowDefinitions.Add(new RowDefinition()
                    {
                        MinHeight = 50,
                        Height = new GridLength(1, GridUnitType.Star)
                    });

                    Grid.SetColumn(_pinnedDock, 1);
                    Grid.SetRow(_pinnedDock, 0);
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
            _pinnedDock = GetTemplateChild(PinnedDockName) as ContentControl;
            _splitter = GetTemplateChild(SplitterName) as GridSplitter;
            UpdateGrid();
        }

        private Grid _pinnedDockGrid;
        private ContentControl _pinnedDock;
        private GridSplitter _splitter;
    }
}

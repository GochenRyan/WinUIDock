using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = LeftPinnedControlName, Type = typeof(ToolPinnedControl))]
    [TemplatePart(Name = RightPinnedControlName, Type = typeof(ToolPinnedControl))]
    [TemplatePart(Name = TopPinnedControlName, Type = typeof(ToolPinnedControl))]
    [TemplatePart(Name = BottomPinnedControlName, Type = typeof(ToolPinnedControl))]
    [TemplatePart(Name = PinnedControlName, Type = typeof(PinnedDockControl))]
    [TemplatePart(Name = ProportionalDockControlName, Type = typeof(ProportionalDockControl))]
    public sealed class RootDockControl : Control
    {
        public const string LeftPinnedControlName = "PART_LeftPinnedControl";
        public const string RightPinnedControlName = "PART_RightPinnedControl";
        public const string TopPinnedControlName = "PART_TopPinnedControl";
        public const string BottomPinnedControlName = "PART_BottomPinnedControl";
        public const string PinnedControlName = "PART_PinnedDockControl";
        public const string ProportionalDockControlName = "PART_ProportionalDockControl";

        public RootDockControl()
        {
            this.DefaultStyleKey = typeof(RootDockControl);
            Loaded += RootDockControl_Loaded;
        }

        private void RootDockControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RootDock rootDock)
            {
                _leftPinnedControl.Loaded += _leftPinnedControl_Loaded;
                _rightPinnedControl.Loaded += _rightPinnedControl_Loaded;
                _topPinnedControl.Loaded += _topPinnedControl_Loaded;
                _bottomPinnedControl.Loaded += _bottomPinnedControl_Loaded;
            }

            DataContextChanged += RootDockControl_DataContextChanged;
        }

        private void _bottomPinnedControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RootDock rootDock)
            {
                _bottomPinnedControl.ItemsSource = rootDock.BottomPinnedDockables;
            }
            _bottomPinnedControl.DataContextChanged += _bottomPinnedControl_DataContextChanged;
        }

        private void _bottomPinnedControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is RootDock rootDock)
            {
                _bottomPinnedControl.ItemsSource = rootDock.BottomPinnedDockables;
            }
        }

        private void _topPinnedControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RootDock rootDock)
            {
                _topPinnedControl.ItemsSource = rootDock.TopPinnedDockables;
            }
            _topPinnedControl.DataContextChanged += _topPinnedControl_DataContextChanged;
        }

        private void _topPinnedControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is RootDock rootDock)
            {
                _topPinnedControl.ItemsSource = rootDock.TopPinnedDockables;
            }
        }

        private void _rightPinnedControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RootDock rootDock)
            {
                _rightPinnedControl.ItemsSource = rootDock.RightPinnedDockables;
            }
            _rightPinnedControl.DataContextChanged += _rightPinnedControl_DataContextChanged;
        }

        private void _rightPinnedControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is RootDock rootDock)
            {
                _rightPinnedControl.ItemsSource = rootDock.RightPinnedDockables;
            }
        }

        private void _leftPinnedControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RootDock rootDock)
            {
                _leftPinnedControl.ItemsSource = rootDock.LeftPinnedDockables;
            }
            _leftPinnedControl.DataContextChanged += _leftPinnedControl_DataContextChanged;
            //_leftPinnedControl.SizeChanged += PinnedControl_SizeChanged;
        }

        //private void PinnedControl_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    // Force refresh the layout
        //    InvalidateMeasure();
        //}

        private void _leftPinnedControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is RootDock rootDock)
            {
                _leftPinnedControl.ItemsSource = rootDock.LeftPinnedDockables;
            }
        }

        private void RootDockControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _leftPinnedControl = GetTemplateChild(LeftPinnedControlName) as ToolPinnedControl;
            _rightPinnedControl = GetTemplateChild(RightPinnedControlName) as ToolPinnedControl;
            _topPinnedControl = GetTemplateChild(TopPinnedControlName) as ToolPinnedControl;
            _bottomPinnedControl = GetTemplateChild(BottomPinnedControlName) as ToolPinnedControl;
            _pinnedControl = GetTemplateChild(PinnedControlName) as PinnedDockControl;
            _proportionalDockControl = GetTemplateChild(ProportionalDockControlName) as ProportionalDockControl;

            _proportionalDockControl.PointerPressed += _proportionalDockControl_PointerPressed;

            BindData();
        }

        private void _proportionalDockControl_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (DataContext is RootDock rootDock)
            {
                rootDock.Factory?.HidePreviewingDockables(rootDock);
            }
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext is RootDock rootDock)
            {
                _pinnedControl.ClearValue(PinnedDockControl.DataContextProperty);
                _pinnedControl.SetBinding(PinnedDockControl.DataContextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("PinnedDock"),
                    Mode = BindingMode.OneWay
                });

                _proportionalDockControl.ClearValue(ProportionalDockControl.DataContextProperty);
                _proportionalDockControl.SetBinding(ProportionalDockControl.DataContextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("DefaultDockable"),
                    Mode = BindingMode.OneWay
                });

                //rootDock.LeftPinnedDockables.CollectionChanged += PinnedDockables_CollectionChanged;
                //rootDock.RightPinnedDockables.CollectionChanged += PinnedDockables_CollectionChanged;
                //rootDock.TopPinnedDockables.CollectionChanged += PinnedDockables_CollectionChanged;
                //rootDock.BottomPinnedDockables.CollectionChanged += PinnedDockables_CollectionChanged;
            }
        }

        //private void PinnedDockables_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        //{
        //    // Force refresh the layout
        //    InvalidateMeasure();
        //}

        //protected override Size MeasureOverride(Size availableSize)
        //{
        //    var cnt = VisualTreeHelper.GetChildrenCount(this);
        //    for (int i = 0; i < cnt; ++i)
        //    {
        //        var child = VisualTreeHelper.GetChild(this, i) as FrameworkElement;
        //        if (child != null)
        //        {
        //            child.Measure(availableSize);
        //        }
        //    }
        //    return availableSize;
        //}

        private ToolPinnedControl _leftPinnedControl;
        private ToolPinnedControl _rightPinnedControl;
        private ToolPinnedControl _topPinnedControl;
        private ToolPinnedControl _bottomPinnedControl;
        private PinnedDockControl _pinnedControl;
        private ProportionalDockControl _proportionalDockControl;
    }
}

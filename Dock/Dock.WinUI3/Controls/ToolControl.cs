using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = TabStripName, Type = typeof(ToolTabStrip))]
    [TemplatePart(Name = ToolContentControlName, Type = typeof(ContentControl))]
    public sealed class ToolControl : ContentControl
    {
        public const string TabStripName = "PART_TabStrip";
        public const string DockableControlName = "PART_DockableControl";
        public const string ToolContentControlName = "PART_ToolContentControl";

        public ToolControl()
        {
            this.DefaultStyleKey = typeof(ToolControl);
            Loaded += ToolControl_Loaded;
            Unloaded += ToolControl_Unloaded;
        }

        private void ToolControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ToolDock toolDock)
            {
                if (_activeDockableContentToken != 0)
                    toolDock.UnregisterPropertyChangedCallback(ToolDock.ActiveDockableProperty, _activeDockableContentToken);
            }
        }

        private void ToolControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += ToolControl_DataContextChanged;
        }

        private void ToolControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _toolTabStrip = GetTemplateChild(TabStripName) as ToolTabStrip;
            _toolContentControl = GetTemplateChild(ToolContentControlName) as ContentControl;

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext is ToolDock toolDock)
            {
                _toolTabStrip.ClearValue(ToolTabStrip.DataContextProperty);
                _toolTabStrip.SetBinding(ToolTabStrip.DataContextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath(""),
                    Mode = BindingMode.OneWay
                });

                // If you use SetBinding, there will be a conversion error. I don't know why...
                UpdateSelectedItem();
                UpdateToolContentControl();
                if (_activeDockableContentToken != 0)
                    toolDock.UnregisterPropertyChangedCallback(ToolDock.ActiveDockableProperty, _activeDockableContentToken);
                _activeDockableContentToken = toolDock.RegisterPropertyChangedCallback(ToolDock.ActiveDockableProperty, ActiveDockableChangedCallback);
            }
        }

        private void ActiveDockableChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if (dp == ToolDock.ActiveDockableProperty)
            {
                UpdateSelectedItem();
                UpdateToolContentControl();
            }
        }

        private void UpdateSelectedItem()
        {
            if (DataContext is ToolDock toolDock)
            {
                _toolTabStrip.SelectedItem = toolDock.ActiveDockable;
            }
        }

        private void UpdateToolContentControl()
        {
            if (DataContext is ToolDock toolDock)
            {
                // To reuse child controls
                if (toolDock.ActiveDockable is Tool tool)
                {
                    var contentElem = tool.Content as UIElement;
                    if (contentElem != null)
                    {
                        var parent = VisualTreeHelper.GetParent(contentElem) as UIElement;
                        if (parent is ContentPresenter presenter)
                        {
                            presenter.Content = null;
                        }
                    }
                }

                _toolContentControl.Content = null;
                _toolContentControl.DataContext = toolDock.ActiveDockable;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = base.MeasureOverride(availableSize);
            return size;
        }

        private ToolTabStrip _toolTabStrip;
        //private ToolContentControl _toolContentControl;
        private ContentControl _toolContentControl;
        private long _activeDockableContentToken = 0;
    }
}

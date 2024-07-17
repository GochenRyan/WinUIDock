using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = ToolControlPartName, Type = typeof(ToolControl))]
    [TemplatePart(Name = ToolChromeControlPartName, Type = typeof(ToolChromeControl))]
    public sealed class ToolDockControl : Control
    {
        public const string ToolControlPartName = "PART_ToolControl";
        public const string ToolChromeControlPartName = "PART_ToolChromeControl";
        public ToolDockControl()
        {
            this.DefaultStyleKey = typeof(ToolDockControl);
            Loaded += ToolDockControl_Loaded;
        }


        private void ToolDockControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += ToolDockControl_DataContextChanged;
        }

        private void ToolDockControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _toolChromeControl = GetTemplateChild(ToolChromeControlPartName) as ToolChromeControl;
            _toolControl = GetTemplateChild(ToolControlPartName) as ToolControl;
            _toolChromeControl.RegisterPropertyChangedCallback(ToolChromeControl.VisibilityProperty, OnChildVisibilityChanged);

            BindData();
        }

        private void OnChildVisibilityChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (dp == ToolChromeControl.VisibilityProperty)
            {
                var visibility = (Visibility)sender.GetValue(dp);
                Visibility = visibility;
            }
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext is ToolDock)
            {
                _toolChromeControl.ClearValue(ToolChromeControl.IsActiveProperty);
                _toolChromeControl.SetBinding(ToolChromeControl.IsActiveProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("IsActive"),
                    Mode = BindingMode.OneWay
                });

                _toolControl.ClearValue(ToolControl.DataContextProperty);
                _toolControl.SetBinding(ToolControl.DataContextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath(""),
                    Mode = BindingMode.OneWay
                });

            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        private ToolChromeControl _toolChromeControl;
        private ToolControl _toolControl;
    }
}

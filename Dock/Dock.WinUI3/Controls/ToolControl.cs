using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = TabStripName, Type = typeof(ToolTabStrip))]
    [TemplatePart(Name = DockableControlName, Type = typeof(DockableControl))]
    [TemplatePart(Name = ContentPresenterName, Type = typeof(ContentPresenter))]
    public sealed class ToolControl : ContentControl
    {
        public const string TabStripName = "PART_TabStrip";
        public const string DockableControlName = "PART_DockableControl";
        public const string ContentPresenterName = "PART_ContentPresenter";

        public ToolControl()
        {
            this.DefaultStyleKey = typeof(ToolControl);
            DataContextChanged += ToolControl_DataContextChanged;
        }

        private void ToolControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is ToolDock dock)
            {
                Content = dock;
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _toolTabStrip = GetTemplateChild(TabStripName) as ToolTabStrip;
            _dockableControl = GetTemplateChild(DockableControlName) as DockableControl;
            _contentPresenter = GetTemplateChild(ContentPresenterName) as ContentPresenter;

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            _toolTabStrip.SetBinding(ToolTabStrip.SelectedItemProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable"),
                Mode = BindingMode.TwoWay
            });

            _dockableControl.SetBinding(DockableControl.DataContextProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable"),
                Mode = BindingMode.OneWay
            });

            _contentPresenter.SetBinding(ContentPresenter.ContentProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable.Content"),
                Mode = BindingMode.OneWay
            });
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        private ToolTabStrip _toolTabStrip;
        private DockableControl _dockableControl;
        private ContentPresenter _contentPresenter;
    }
}

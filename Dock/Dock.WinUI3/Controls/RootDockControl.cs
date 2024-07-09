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
    [TemplatePart(Name = ProportionalDockControlName, Type = typeof(ProportionalDockControl))]
    public sealed class RootDockControl : Control
    {
        public const string LeftPinnedControlName = "PART_LeftPinnedControl";
        public const string RightPinnedControlName = "PART_RightPinnedControl";
        public const string TopPinnedControlName = "PART_TopPinnedControl";
        public const string BottomPinnedControlName = "PART_BottomPinnedControl";
        public const string ProportionalDockControlName = "PART_ProportionalDockControl";

        public RootDockControl()
        {
            this.DefaultStyleKey = typeof(RootDockControl);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _leftPinnedControl = GetTemplateChild(LeftPinnedControlName) as ToolPinnedControl;
            _rightPinnedControl = GetTemplateChild(RightPinnedControlName) as ToolPinnedControl;
            _topPinnedControl = GetTemplateChild(TopPinnedControlName) as ToolPinnedControl;
            _bottomPinnedControl = GetTemplateChild(BottomPinnedControlName) as ToolPinnedControl;
            _proportionalDockControl = GetTemplateChild(ProportionalDockControlName) as ProportionalDockControl;

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            _leftPinnedControl.SetBinding(ToolPinnedControl.ItemsProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("LeftPinnedDockables"),
                Mode = BindingMode.OneWay
            });

            _leftPinnedControl.SetBinding(ToolPinnedControl.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("LeftPinnedDockables.Count"),
                Mode = BindingMode.OneWay
            });

            _rightPinnedControl.SetBinding(ToolPinnedControl.ItemsProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("RightPinnedDockables"),
                Mode = BindingMode.OneWay
            });

            _rightPinnedControl.SetBinding(ToolPinnedControl.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("RightPinnedDockables.Count"),
                Mode = BindingMode.OneWay
            });

            _topPinnedControl.SetBinding(ToolPinnedControl.ItemsProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("TopPinnedDockables"),
                Mode = BindingMode.OneWay
            });

            _topPinnedControl.SetBinding(ToolPinnedControl.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("TopPinnedDockables.Count"),
                Mode = BindingMode.OneWay
            });

            _bottomPinnedControl.SetBinding(ToolPinnedControl.ItemsProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("BottomPinnedDockables"),
                Mode = BindingMode.OneWay
            });

            _bottomPinnedControl.SetBinding(ToolPinnedControl.VisibilityProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("BottomPinnedDockables.Count"),
                Mode = BindingMode.OneWay
            });

            _proportionalDockControl.SetBinding(ProportionalDockControl.DataContextProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("DefaultDockable"),
                Mode = BindingMode.OneWay
            });
        }

        private ToolPinnedControl _leftPinnedControl;
        private ToolPinnedControl _rightPinnedControl;
        private ToolPinnedControl _topPinnedControl;
        private ToolPinnedControl _bottomPinnedControl;
        private ProportionalDockControl _proportionalDockControl;
    }
}

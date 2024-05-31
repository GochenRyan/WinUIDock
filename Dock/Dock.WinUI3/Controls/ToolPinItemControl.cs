using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplateVisualState(Name = VerticalState, GroupName = OrientationStates)]
    [TemplateVisualState(Name = HorizontalState, GroupName = OrientationStates)]
    public sealed class ToolPinItemControl : ContentControl
    {
        public const string OrientationStates = "OrientationStates";
        public const string VerticalState = "Vertical";
        public const string HorizontalState = "Horizontal";

        public ToolPinItemControl()
        {
            this.DefaultStyleKey = typeof(ToolPinItemControl);
        }

        public static DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(ToolPinItemControl),
            new PropertyMetadata(Orientation.Vertical, OnOrientationChanged));

        public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

        private static void OnOrientationChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        {
            var control = ob as ToolPinItemControl;
            Orientation orientation = (Orientation)args.NewValue;
            switch (orientation)
            {
                case Orientation.Horizontal:
                    VisualStateManager.GoToState(control, HorizontalState, true);
                    break;
                case Orientation.Vertical:
                    VisualStateManager.GoToState(control, VerticalState, true);
                    break;
            }
        }
    }
}

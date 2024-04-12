using Dock.Model.Core;
using Microsoft.UI.Xaml;

namespace Dock.Settings
{
    public class DockProperties : DependencyObject
    {
        public static readonly DependencyProperty ControlRecyclingProperty =
            DependencyProperty.RegisterAttached("ControlRecycling", typeof(IControlRecycling), typeof(DockProperties), new PropertyMetadata(default));

        public static readonly DependencyProperty IsDockTargetProperty =
            DependencyProperty.RegisterAttached("IsDockTarget", typeof(bool), typeof(DockProperties), new PropertyMetadata(default));

        public static readonly DependencyProperty IsDragAreaProperty =
            DependencyProperty.RegisterAttached("IsDragArea", typeof(bool), typeof(DockProperties), new PropertyMetadata(default));

        public static readonly DependencyProperty IsDropAreaProperty =
            DependencyProperty.RegisterAttached("IsDropArea", typeof(bool), typeof(DockProperties), new PropertyMetadata(default));

        public static readonly DependencyProperty IsDragEnabledProperty =
            DependencyProperty.RegisterAttached("IsDragEnabled", typeof(bool), typeof(DockProperties), new PropertyMetadata(default));

        public static readonly DependencyProperty IsDropEnabledProperty =
            DependencyProperty.RegisterAttached("IsDropEnabled", typeof(bool), typeof(DockProperties), new PropertyMetadata(default));

        public static IControlRecycling GetControlRecycling(DependencyObject control)
        {
            return (IControlRecycling)control.GetValue(ControlRecyclingProperty);
        }

        public static void SetControlRecycling(DependencyObject control, IControlRecycling value)
        {
            control.SetValue(ControlRecyclingProperty, value);
        }

        public static bool GetIsDockTarget(DependencyObject control)
        {
            return (bool)control.GetValue(IsDockTargetProperty);
        }

        public static void SetIsDockTarget(DependencyObject control, bool value)
        {
            control.SetValue(IsDockTargetProperty, value);
        }

        public static bool GetIsDragArea(DependencyObject control)
        {
            return (bool)control.GetValue(IsDragAreaProperty);
        }

        public static void SetIsDragArea(DependencyObject control, bool value)
        {
            control.SetValue(IsDragAreaProperty, value);
        }

        public static bool GetIsDropArea(DependencyObject control)
        {
            return (bool)control.GetValue(IsDropAreaProperty);
        }

        public static void SetIsDropArea(DependencyObject control, bool value)
        {
            control.SetValue(IsDropAreaProperty, value);
        }

        public static bool GetIsDragEnabled(DependencyObject control)
        {
            return (bool)control.GetValue(IsDragEnabledProperty);
        }

        public static void SetIsDragEnabled(DependencyObject control, bool value)
        {
            control.SetValue(IsDragEnabledProperty, value);
        }

        public static bool GetIsDropEnabled(DependencyObject control)
        {
            return (bool)control.GetValue(IsDropEnabledProperty);
        }

        public static void SetIsDropEnabled(DependencyObject control, bool value)
        {
            control.SetValue(IsDropEnabledProperty, value);
        }
    }
}

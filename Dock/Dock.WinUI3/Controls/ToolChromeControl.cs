using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public sealed class ToolChromeControl : ContentControl
    {
        public ToolChromeControl()
        {
            this.DefaultStyleKey = typeof(ToolChromeControl);
        }

        public static DependencyProperty TitleProprty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(ToolChromeControl),
            new PropertyMetadata(string.Empty));

        public static DependencyProperty IsActiveProperty = DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(ToolChromeControl),
            new PropertyMetadata(false));

        public static DependencyProperty IsPinnedProperty = DependencyProperty.Register(
            nameof(IsPinned),
            typeof(bool),
            typeof(ToolChromeControl),
            new PropertyMetadata(false));

        public static DependencyProperty IsFloatingProperty = DependencyProperty.Register(
            nameof(IsFloating),
            typeof(bool),
            typeof(ToolChromeControl),
            new PropertyMetadata(false));

        public static DependencyProperty IsMaximizedProperty = DependencyProperty.Register(
            nameof(IsMaximized),
            typeof(bool),
            typeof(ToolChromeControl),
            new PropertyMetadata(false));

        public string Title
        {
            get => (string)GetValue(TitleProprty);
            set => SetValue(TitleProprty, value);
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public bool IsPinned
        {
            get => (bool)GetValue(IsPinnedProperty);
            set => SetValue(IsPinnedProperty, value);
        }

        public bool IsFloating
        {
            get => (bool)GetValue(IsFloatingProperty);
            set => SetValue(IsFloatingProperty, value);
        }

        public bool IsMaximized
        {
            get => (bool)GetValue(IsMaximizedProperty);
            set => SetValue(IsMaximizedProperty, value);
        }
    }
}

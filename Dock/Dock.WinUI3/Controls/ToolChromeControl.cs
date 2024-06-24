using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = GripPartName, Type = typeof(Grid))]
    [TemplatePart(Name = CloseButtonPartName, Type = typeof(Button))]
    public sealed class ToolChromeControl : ContentControl
    {
        public const string GripPartName = "PART_Grip";
        public const string CloseButtonPartName = "PART_CloseButton";

        public ToolChromeControl()
        {
            this.DefaultStyleKey = typeof(ToolChromeControl);
            Loaded += ToolChromeControl_Loaded;
            Unloaded += ToolChromeControl_Unloaded;
        }

        private void ToolChromeControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_attachedWindow != null)
            {
                HostWindowControl hostWindowControl = _attachedWindow.WindowContent as HostWindowControl;
                hostWindowControl.DetachGrip(this);
                _attachedWindow = null;
            }
        }

        private void ToolChromeControl_Loaded(object sender, RoutedEventArgs e)
        {
            AttachToWindow();
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

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Grip = GetTemplateChild(GripPartName) as Grid;
            CloseButton = GetTemplateChild(CloseButtonPartName) as Button;

            AttachToWindow();
        }

        private void AttachToWindow()
        {
            if (Grip == null)
                return;

            if (HostWindow.GetWindowForElement(this) is HostWindow window)
            {
                HostWindowControl hostWindowControl = window.WindowContent as HostWindowControl;
                hostWindowControl.AttachGrip(this);
                _attachedWindow = window;

                IsFloating = true;
            }
        }

        public Grid Grip { get; private set; }

        public Button CloseButton { get; private set; }

        private HostWindow _attachedWindow;
    }
}

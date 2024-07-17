using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = DocumentTabStripName, Type = typeof(DocumentControl))]
    [TemplatePart(Name = DockableControlName, Type = typeof(DockableControl))]
    [TemplatePart(Name = DocumentContentControlName, Type = typeof(DocumentContentControl))]
    public sealed class DocumentControl : ContentControl
    {
        public const string DocumentTabStripName = "PART_TabStrip";
        public const string DockableControlName = "PART_DockableControl";
        public const string DocumentContentControlName = "PART_DocumentContentControl";

        public DocumentControl()
        {
            this.DefaultStyleKey = typeof(DocumentControl);
            Loaded += DocumentControl_Loaded;
            Unloaded += DocumentControl_Unloaded;
        }

        private void DocumentControl_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void DocumentControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += DocumentControl_DataContextChanged;
        }

        private void DocumentControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(DocumentControl),
            new PropertyMetadata(false));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _documentTabStrip = GetTemplateChild(DocumentTabStripName) as DocumentTabStrip;
            _dockableControl = GetTemplateChild(DockableControlName) as DockableControl;
            _documentContentControl = GetTemplateChild(DocumentContentControlName) as DocumentContentControl;

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext is DocumentDock)
            {
                _documentTabStrip.ClearValue(DocumentTabStrip.SelectedItemProperty);
                _documentTabStrip.SetBinding(DocumentTabStrip.SelectedItemProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("ActiveDockable"),
                    Mode = BindingMode.TwoWay
                });

                _documentTabStrip.ClearValue(DocumentTabStrip.CanCreateItemProperty);
                _documentTabStrip.SetBinding(DocumentTabStrip.CanCreateItemProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("CanCreateDocument"),
                    Mode = BindingMode.OneWay
                });

                _documentTabStrip.ClearValue(DocumentTabStrip.IsActiveProperty);
                _documentTabStrip.SetBinding(DocumentTabStrip.IsActiveProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("IsActive"),
                    Mode = BindingMode.OneWay
                });

                _dockableControl.ClearValue(DockableControl.DataContextProperty);
                _dockableControl.SetBinding(DockableControl.DataContextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("ActiveDockable"),
                    Mode = BindingMode.OneWay
                });

                _documentContentControl.ClearValue(DocumentContentControl.DataContextProperty);
                _documentContentControl.SetBinding(DocumentContentControl.DataContextProperty, new Binding
                {
                    Source = DataContext,
                    Path = new PropertyPath("ActiveDockable"),
                    Mode = BindingMode.OneWay
                });
            }
        }

        private DocumentTabStrip _documentTabStrip;
        private DockableControl _dockableControl;
        private DocumentContentControl _documentContentControl;
    }
}

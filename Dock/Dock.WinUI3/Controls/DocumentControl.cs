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
    [TemplatePart(Name = ContentPresenterName, Type = typeof(ContentPresenter))]
    public sealed class DocumentControl : ContentControl
    {
        public const string DocumentTabStripName = "PART_TabStrip";
        public const string DockableControlName = "PART_DockableControl";
        public const string ContentPresenterName = "PART_ContentPresenter";

        public DocumentControl()
        {
            this.DefaultStyleKey = typeof(DocumentControl);
            DataContextChanged += DocumentControl_DataContextChanged;
        }

        private void DocumentControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is DocumentDock dock)
            {
                Content = dock;
            }
        }

        public static DependencyProperty IsActiveProperty = DependencyProperty.Register(
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
            _contentPresenter = GetTemplateChild(ContentPresenterName) as ContentPresenter;

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            _documentTabStrip.SetBinding(DocumentTabStrip.SelectedItemProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("ActiveDockable"),
                Mode = BindingMode.TwoWay
            });

            _documentTabStrip.SetBinding(DocumentTabStrip.CanCreateItemProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("CanCreateDocument"),
                Mode = BindingMode.OneWay
            });

            _documentTabStrip.SetBinding(DocumentTabStrip.IsActiveProperty, new Binding
            {
                Source = DataContext,
                Path = new PropertyPath("IsActive"),
                Mode = BindingMode.OneWay
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

        private DocumentTabStrip _documentTabStrip;
        private DockableControl _dockableControl;
        private ContentPresenter _contentPresenter;
    }
}

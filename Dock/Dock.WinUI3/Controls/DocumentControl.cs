using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

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
            if (DataContext is DocumentDock documentDock)
            {
                if (_activeDockableContentToken != 0)
                    documentDock.UnregisterPropertyChangedCallback(DocumentDock.ActiveDockableProperty, _activeDockableContentToken);
            }
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
            if (DataContext is DocumentDock documentDock)
            {
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

                // If you use SetBinding, there will be a conversion error. I don't know why...
                UpdateActiveDockable();
                if (_activeDockableContentToken != 0)
                    documentDock.UnregisterPropertyChangedCallback(DocumentDock.ActiveDockableProperty, _activeDockableContentToken);
                _activeDockableContentToken = documentDock.RegisterPropertyChangedCallback(DocumentDock.ActiveDockableProperty, ActiveDockableChangedCallback);
            }
        }

        private void ActiveDockableChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if (dp == DocumentDock.ActiveDockableProperty)
            {
                UpdateActiveDockable();
            }
        }

        private void UpdateActiveDockable()
        {
            if (DataContext is DocumentDock documentDock)
            {
                _documentTabStrip.SelectedItem = documentDock.ActiveDockable;
                _dockableControl.DataContext = documentDock.ActiveDockable;

                // To reuse child controls
                if (documentDock.ActiveDockable is Tool tool)
                {
                    var contentElem = tool.Content as UIElement;
                    var parent = VisualTreeHelper.GetParent(contentElem) as UIElement;
                    if (parent is ContentPresenter presenter)
                    {
                        presenter.Content = null;
                    }
                }
                else if (documentDock.ActiveDockable is Document document)
                {
                    var contentElem = document.Content as UIElement;
                    var parent = VisualTreeHelper.GetParent(contentElem) as UIElement;
                    if (parent is ContentPresenter presenter)
                    {
                        presenter.Content = null;
                    }
                }

                _documentContentControl.Content = null;
                _documentContentControl.DataContext = documentDock.ActiveDockable;
            }
        }

        private DocumentTabStrip _documentTabStrip;
        private DockableControl _dockableControl;
        private DocumentContentControl _documentContentControl;
        private long _activeDockableContentToken = 0;
    }
}

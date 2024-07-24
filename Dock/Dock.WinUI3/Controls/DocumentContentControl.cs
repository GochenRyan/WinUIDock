using Dock.Model.Controls;
using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = ContentPresenterName, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = DockableControlName, Type = typeof(DockableControl))]
    public sealed class DocumentContentControl : ContentControl
    {
        public const string ContentPresenterName = "PART_ContentPresenter";
        public const string DockableControlName = "PART_DockableControl";
        public DocumentContentControl()
        {
            this.DefaultStyleKey = typeof(DocumentContentControl);
            Loaded += DocumentContentControl_Loaded;
            Unloaded += DocumentContentControl_Unloaded;
        }

        private void DocumentContentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_contentToken != 0)
            {
                if (DataContext is Tool tool)
                {
                    tool.UnregisterPropertyChangedCallback(Tool.ContentProperty, _contentToken);
                }
                else if (DataContext is Document document)
                {
                    document.UnregisterPropertyChangedCallback(Document.ContentProperty, _contentToken);
                }
            }
        }

        private void DocumentContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += DocumentContentControl_DataContextChanged;
        }

        private void DocumentContentControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        private void ContentChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if (dp == Tool.ContentProperty || dp == Document.ContentProperty)
            {
                UpdateContent();
            }
        }

        private void UpdateContent()
        {
            if (DataContext is IDocument document)
            {
                if (document is IDocumentContent documentContent && _contentPresenter.Content != documentContent.Content)
                {
                    _contentPresenter.Content = documentContent.Content;
                    _contentPresenter.InvalidateMeasure();
                    _dockableControl.RecordSize();
                }
                else if (document is IToolContent toolContent && _contentPresenter.Content != toolContent.Content)
                {
                    _contentPresenter.Content = toolContent.Content;
                    _contentPresenter.InvalidateMeasure();
                    _dockableControl.RecordSize();
                }
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _contentPresenter = GetTemplateChild(ContentPresenterName) as ContentPresenter;
            _dockableControl = GetTemplateChild(DockableControlName) as DockableControl;

            BindData();
        }

        private void BindData()
        {
            if (DataContext is IDocument document)
            {
                if (_lastDocument != null && _contentToken != 0)
                {
                    if (_lastDocument is Document lastDoc)
                    {
                        lastDoc.UnregisterPropertyChangedCallback(Document.ContentProperty, _contentToken);
                    }
                    else if (_lastDocument is Tool lastTool)
                    {
                        lastTool.UnregisterPropertyChangedCallback(Tool.ContentProperty, _contentToken);
                    }
                }

                if (document is Tool tool)
                {
                    _contentToken = tool.RegisterPropertyChangedCallback(Tool.ContentProperty, ContentChangedCallback);
                }
                else if (document is Document doc)
                {
                    _contentToken = doc.RegisterPropertyChangedCallback(Tool.ContentProperty, ContentChangedCallback);
                }
                _lastDocument = document;

            }

            UpdateContent();
        }

        private long _contentToken = 0;
        private IDocument _lastDocument = null;
        private ContentPresenter _contentPresenter;
        private DockableControl _dockableControl;
    }
}

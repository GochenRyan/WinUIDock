using Dock.Model.Controls;
using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = ContentPresenterName, Type = typeof(ContentPresenter))]
    public sealed class DocumentContentControl : ContentControl
    {
        public const string ContentPresenterName = "PART_ContentPresenter";
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
            if (DataContext is IDocument doc)
            {
                if (_contentToken != 0)
                {
                    if (doc is Tool tool)
                    {
                        tool.UnregisterPropertyChangedCallback(Tool.ContentProperty, _contentToken);
                        _contentToken = tool.RegisterPropertyChangedCallback(Tool.ContentProperty, ContentChangedCallback);
                    }
                    else if (doc is Document document)
                    {
                        document.UnregisterPropertyChangedCallback(Document.ContentProperty, _contentToken);
                        _contentToken = document.RegisterPropertyChangedCallback(Tool.ContentProperty, ContentChangedCallback);
                    }
                }

                UpdateContent();
            }
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
                if (document is IDocumentContent documentContent)
                {
                    _contentPresenter.Content = documentContent.Content;
                }
                else if (document is IToolContent toolContent)
                {
                    _contentPresenter.Content = toolContent.Content;
                }
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _contentPresenter = GetTemplateChild(ContentPresenterName) as ContentPresenter;
            UpdateContent();
        }

        private long _contentToken = 0;
        private ContentPresenter _contentPresenter;
    }
}

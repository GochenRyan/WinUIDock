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
                    _contentToken = 0;
                }
                else if (DataContext is Document document)
                {
                    document.UnregisterPropertyChangedCallback(Document.ContentProperty, _contentToken);
                    _contentToken = 0;
                }
            }

            DataContextChanged -= DocumentContentControl_DataContextChanged;

            // Detach the model-owned content element while this tree is still
            // alive — see ToolContentControl_Unloaded for the closed-window
            // poisoning this prevents (E_INVALIDARG on the next host's measure).
            if (_contentPresenter is not null)
            {
                _contentPresenter.Content = null;
            }
        }

        private void DocumentContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += DocumentContentControl_DataContextChanged;

            // Restore content after an Unloaded detach (tab/float round trips).
            BindData();
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

        /// <summary>
        /// Permanently stands this host down — see
        /// <see cref="ToolContentControl.StandDownForTeardown"/> for the race
        /// this closes (its window is dying while the content moves elsewhere).
        /// </summary>
        internal void StandDownForTeardown()
        {
            _stoodDown = true;

            if (_contentPresenter is not null)
            {
                _contentPresenter.Content = null;
            }
        }

        private void UpdateContent()
        {
            if (_stoodDown || _contentPresenter is null || DataContext is not IDocument document)
            {
                return;
            }

            object content = document is IDocumentContent documentContent
                ? documentContent.Content
                : document is IToolContent toolContent ? toolContent.Content : null;

            if (content is null)
            {
                return;
            }

            if (ReferenceEquals(_contentPresenter.Content, content))
            {
                // Same stale-reference hole as ToolContentControl.UpdateContent:
                // the presenter still references the element while another host
                // stole it visually — a same-value assignment won't re-hook it.
                if (content is UIElement el
                    && !ReferenceEquals(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(el), _contentPresenter))
                {
                    Internal.DockDiag.Log(
                        $"DocumentContentControl.UpdateContent REHOOK host={Internal.DockDiag.Describe(this)} content={Internal.DockDiag.Describe(content)}");
                    ToolContentControl.DetachFromCurrentHost(el, _contentPresenter);
                    _contentPresenter.Content = null;
                    _contentPresenter.Content = content;
                    _contentPresenter.InvalidateMeasure();
                    _dockableControl?.RecordSize();
                }

                return;
            }

            // Release the element from its current visual host first — see
            // ToolContentControl.UpdateContent (shared model-owned element;
            // FrameworkElement.Parent does not surface presenter hosts).
            if (content is UIElement element)
            {
                ToolContentControl.DetachFromCurrentHost(element, _contentPresenter);
            }

            _contentPresenter.Content = content;
            _contentPresenter.InvalidateMeasure();
            _dockableControl?.RecordSize();
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
        private bool _stoodDown;
        private IDocument _lastDocument = null;
        private ContentPresenter _contentPresenter;
        private DockableControl _dockableControl;
    }
}

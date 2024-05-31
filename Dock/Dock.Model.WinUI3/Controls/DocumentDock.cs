using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Core;
using Dock.Model.WinUI3.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System.Collections.Generic;
using System.Windows.Input;

namespace Dock.Model.WinUI3.Controls
{
    [ContentProperty(Name = "VisibleDockables")]
    public class DocumentDock : DockBase, IDocumentDock, IDocumentDockContent
    {
        public DependencyProperty VisibleDockablesProperty = DependencyProperty.Register(
            nameof(VisibleDockables),
            typeof(IList<IDockable>),
            typeof(DocumentDock),
            new PropertyMetadata(new List<IDockable>()));

        public static DependencyProperty CanCreateDocumentProperty = DependencyProperty.Register(
            nameof(CanCreateDocument),
            typeof(bool),
            typeof(DocumentDock),
            new PropertyMetadata(default));

        public static DependencyProperty DocumentTemplateProperty = DependencyProperty.Register(
            nameof(DocumentTemplate),
            typeof(IDocumentTemplate),
            typeof(DocumentDock),
            new PropertyMetadata(default));

        public override IList<IDockable> VisibleDockables
        {
            get => (IList<IDockable>)GetValue(VisibleDockablesProperty);
            set
            {
                SetValue(VisibleDockablesProperty, value);
                _visibleDockables = value;
            }
        }

        public bool CanCreateDocument { get => (bool)GetValue(CanCreateDocumentProperty); set => SetValue(CanCreateDocumentProperty, value); }
        public ICommand CreateDocument { get; set; }
        public IDocumentTemplate DocumentTemplate { get => (IDocumentTemplate)GetValue(DocumentTemplateProperty); set => SetValue(DocumentTemplateProperty, value); }

        public DocumentDock()
        {
            CreateDocument = new Command(() => CreateDocumentFromTemplate());
        }

        public object CreateDocumentFromTemplate()
        {
            if (DocumentTemplate is null || !CanCreateDocument)
            {
                return null;
            }

            var document = new Document
            {
                Title = $"Document{VisibleDockables?.Count ?? 0}",
                Content = DocumentTemplate.Content
            };

            Factory?.AddDockable(this, document);
            Factory?.SetActiveDockable(document);
            Factory?.SetFocusedDockable(this, document);

            return document;
        }
    }
}

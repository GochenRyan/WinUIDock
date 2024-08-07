﻿using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Core;
using Dock.Model.WinUI3.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace Dock.Model.WinUI3.Controls
{
    [DataContract(IsReference = true)]
    [ContentProperty(Name = "VisibleDockables")]
    public class DocumentDock : DockBase, IDocumentDock, IDocumentDockContent
    {
        public DocumentDock() : base()
        {
            VisibleDockables = new ObservableCollection<IDockable>();
            CreateDocument = new Command(() => CreateDocumentFromTemplate());
        }

        public static readonly DependencyProperty VisibleDockablesProperty = DependencyProperty.Register(
            nameof(VisibleDockables),
            typeof(ObservableCollection<IDockable>),
            typeof(DocumentDock),
            new PropertyMetadata(null));

        public static readonly DependencyProperty CanCreateDocumentProperty = DependencyProperty.Register(
            nameof(CanCreateDocument),
            typeof(bool),
            typeof(DocumentDock),
            new PropertyMetadata(false));

        public static readonly DependencyProperty DocumentTemplateProperty = DependencyProperty.Register(
            nameof(DocumentTemplate),
            typeof(IDocumentTemplate),
            typeof(DocumentDock),
            new PropertyMetadata(null));

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("VisibleDockables")]
        public override ObservableCollection<IDockable> VisibleDockables
        {
            get => (ObservableCollection<IDockable>)GetValue(VisibleDockablesProperty);
            set => SetValue(VisibleDockablesProperty, value);
        }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        [JsonPropertyName("CanCreateDocument")]
        public bool CanCreateDocument { get => (bool)GetValue(CanCreateDocumentProperty); set => SetValue(CanCreateDocumentProperty, value); }

        [IgnoreDataMember]
        [JsonIgnore]
        public ICommand CreateDocument { get; set; }

        [IgnoreDataMember]
        [JsonIgnore]
        public IDocumentTemplate DocumentTemplate { get => (IDocumentTemplate)GetValue(DocumentTemplateProperty); set => SetValue(DocumentTemplateProperty, value); }

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

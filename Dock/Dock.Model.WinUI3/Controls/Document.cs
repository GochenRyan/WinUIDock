using Dock.Model.Controls;
using Dock.Model.WinUI3.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Dock.Model.WinUI3.Controls
{
    [DataContract(IsReference = true)]
    [ContentProperty(Name = "Content")]
    public class Document : DockableBase, IDocument, IDocumentContent
    {
        [IgnoreDataMember]
        [JsonIgnore]
        public object Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

        public static DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(Document),
            new PropertyMetadata(default));

        [IgnoreDataMember]
        [JsonIgnore]
        public Type DataType { get; set; }

        public bool Match(object data)
        {
            if (DataType == null)
            {
                return true;
            }

            return DataType.IsInstanceOfType(data);
        }
    }
}

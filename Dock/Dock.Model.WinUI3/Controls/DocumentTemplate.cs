using Dock.Model.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Dock.Model.WinUI3.Controls
{
    [DataContract(IsReference = true)]
    [ContentProperty(Name = "Content")]
    public class DocumentTemplate : DependencyObject, IDocumentTemplate
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(DocumentTemplate),
            new PropertyMetadata(null));

        [IgnoreDataMember]
        [JsonIgnore]
        public object Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

        public DocumentTemplate()
        {
        }

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

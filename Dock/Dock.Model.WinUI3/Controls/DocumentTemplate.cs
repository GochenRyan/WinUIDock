using Dock.Model.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System;

namespace Dock.Model.WinUI3.Controls
{
    [ContentProperty(Name = "Content")]
    public class DocumentTemplate : DependencyObject, IDocumentTemplate
    {
        DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(DocumentTemplate),
            new PropertyMetadata(default));

        public object Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

        public DocumentTemplate()
        {
        }

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

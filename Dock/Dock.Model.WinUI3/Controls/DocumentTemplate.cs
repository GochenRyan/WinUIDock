using Dock.Model.Controls;
using Microsoft.UI.Xaml.Markup;
using System;

namespace Dock.Model.WinUI3.Controls
{
    [ContentProperty(Name = "Content")]
    public class DocumentTemplate : IDocumentTemplate
    {
        public object Content { get; set; }

        //DependencyProperty ContentProperty = DependencyProperty.Register(
        //    nameof(Content),
        //    typeof(object),
        //    typeof(Document),
        //    new PropertyMetadata(default));

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

using Dock.Model.Controls;
using Dock.Model.WinUI3.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System;

namespace Dock.Model.WinUI3.Controls
{
    [ContentProperty(Name = "Content")]
    public class Tool : DockableBase, ITool, IDocument, IToolContent
    {
        public object Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

        DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(Tool),
            new PropertyMetadata(default));

        public Tool()
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

﻿using Dock.Model.Controls;
using Dock.Model.WinUI3.Core;
using Microsoft.UI.Xaml;
using System;

namespace Dock.Model.WinUI3.Controls
{
    public class Document : DockableBase, IDocument, IDocumentContent
    {
        public object Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

        DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(Document),
            new PropertyMetadata(default));

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

using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = DocumentControlName, Type = typeof(DocumentControl))]
    public sealed class DocumentDockControl : Control
    {
        public const string DocumentControlName = "PART_DocumentControl";

        public DocumentDockControl()
        {
            this.DefaultStyleKey = typeof(DocumentDockControl);
            Loaded += DocumentDockControl_Loaded;
            Unloaded += DocumentDockControl_Unloaded;
        }

        private void DocumentDockControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DocumentDock documentDock)
            {
                documentDock.VisibleDockables.CollectionChanged -= VisibleDockables_CollectionChanged;
            }
        }

        private void DocumentDockControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += DocumentDockControl_DataContextChanged;
        }

        private void DocumentDockControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _documentControl = GetTemplateChild(DocumentControlName) as DocumentControl;

            BindData();
        }

        // The Windows Runtime doesn't support a Binding usage for Setter.Value.
        // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter?view=winrt-26100
        private void BindData()
        {
            if (DataContext is DocumentDock documentDock)
            {
                documentDock.VisibleDockables.CollectionChanged -= VisibleDockables_CollectionChanged;
                documentDock.VisibleDockables.CollectionChanged += VisibleDockables_CollectionChanged;
            }
        }

        private void VisibleDockables_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                sender is ObservableCollection<IDockable> visibleDockables &&
                visibleDockables.Count == 1)
            {
                var parent = VisualTreeHelper.GetParent(this);
                while (parent != null)
                {
                    if (parent is ProportionalStackPanel proportionalStackPanel)
                    {
                        proportionalStackPanel.InvalidateMeasure();
                        break;
                    }
                    else
                    {
                        parent = VisualTreeHelper.GetParent(parent);
                    }
                }
            }
        }

        private DocumentControl _documentControl;
    }
}

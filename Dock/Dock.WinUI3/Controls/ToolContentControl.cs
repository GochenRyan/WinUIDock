using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = ContentPresenterName, Type = typeof(ContentPresenter))]
    public sealed class ToolContentControl : ContentControl
    {
        public const string ContentPresenterName = "PART_ContentPresenter";
        public ToolContentControl()
        {
            this.DefaultStyleKey = typeof(ToolContentControl);

            Loaded += ToolContentControl_Loaded;
            Unloaded += ToolContentControl_Unloaded;
        }

        private void ToolContentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_toolContentToken != 0 && DataContext is Tool tool)
                tool.UnregisterPropertyChangedCallback(Tool.ContentProperty, _toolContentToken);
        }

        private void ToolContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += ToolContentControl_DataContextChanged;
        }

        private void ToolContentControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            BindData();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _contentPresenter = GetTemplateChild(ContentPresenterName) as ContentPresenter;

            BindData();
        }

        private void BindData()
        {
            if (DataContext is Tool tool)
            {
                if (_toolContentToken != 0)
                    tool.UnregisterPropertyChangedCallback(Tool.ContentProperty, _toolContentToken);

                _toolContentToken = tool.RegisterPropertyChangedCallback(Tool.ContentProperty, ToolContentChangedCallback);
                UpdateContent();
            }
        }

        private void UpdateContent()
        {
            Tool tool = (Tool)DataContext;
            _contentPresenter.Content = tool.Content;
        }

        private void ToolContentChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if (dp == Tool.ContentProperty)
            {
                UpdateContent();
            }
        }

        private long _toolContentToken = 0;
        ContentPresenter _contentPresenter;
    }
}

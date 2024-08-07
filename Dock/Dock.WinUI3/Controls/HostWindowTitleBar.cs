using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = BorderName, Type = typeof(DocumentControl))]
    [TemplatePart(Name = AppTitleName, Type = typeof(TextBlock))]
    public sealed class HostWindowTitleBar : Control
    {
        public const string BorderName = "PART_Border";
        public const string AppTitleName = "PART_AppTitle";

        public HostWindowTitleBar()
        {
            this.DefaultStyleKey = typeof(HostWindowTitleBar);
            IsHitTestVisible = true;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _border = GetTemplateChild(BorderName) as Border;
            _title = GetTemplateChild(AppTitleName) as TextBlock;

            _title.Text = TitleText;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        public TextBlock GetTitle()
        {
            return _title;
        }

        public string TitleText
        {
            set
            {
                _text = value;
                if (_title != null)
                {
                    _title.Text = _text;
                }
            }
            get
            {
                return _text;
            }
        }

        private Border _border;
        private TextBlock _title;
        private string _text;
    }
}

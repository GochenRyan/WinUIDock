using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = DockControlName, Type = typeof(DockControl))]
    [TemplatePart(Name = TitleBarName, Type = typeof(HostWindowTitleBar))]
    public class HostWindowControl : ContentControl, IHostWindow
    {
        public const string DockControlName = "PART_DockControl";
        public const string TitleBarName = "PART_TitleBar";

        public HostWindowControl(HostWindow hostWindow)
        {
            this.DefaultStyleKey = typeof(HostWindowControl);
            _ownerWindow = hostWindow;
            _ownerWindow.WindowContent = this;
            _ownerWindow.ExtendsContentIntoTitleBar = true;

            _ownerWindow.PositionChanged += _ownerWindow_PositionChanged;
            _ownerWindow.SizeChanged += _ownerWindow_SizeChanged;

            LayoutUpdated += HostWindowControl_LayoutUpdated;

            _dockManager = new DockManager();

            DataContextChanged += HostWindowControl_DataContextChanged;
        }

        private void _ownerWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            _ownerWindowWidth = args.Size.Width;
            _ownerWindowHeight = args.Size.Height;
        }

        private void _ownerWindow_PositionChanged(object sender, Windows.Graphics.PointInt32 e)
        {
            _ownerWindowX = e.X;
            _ownerWindowY = e.Y;
        }

        private void HostWindowControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            UpdateTemplateChilren();
        }

        private void UpdateTemplateChilren()
        {
            IDock dock = DataContext as IDock;
            if (_dockControl != null)
            {
                _dockControl.Layout = dock;
            }

            //if (_titleBar != null)
            //{
            //    _titleBar.TitleText = (dock != null ? dock.Title : "");
            //}
        }

        private void HostWindowControl_LayoutUpdated(object sender, object e)
        {
            if (Window is { } && _ownerWindow.AppWindow != null && IsTracked)
            {
                Window.Save();
            }
        }

        public static readonly DependencyProperty IsToolWindowProperty = DependencyProperty.Register(
            nameof(IsToolWindow),
            typeof(bool),
            typeof(HostWindowControl),
            new PropertyMetadata(false));

        public bool IsToolWindow
        {
            get => (bool)GetValue(IsToolWindowProperty);
            set => SetValue(IsToolWindowProperty, value);
        }

        public static readonly DependencyProperty ToolChromeControlsWholeWindowProperty = DependencyProperty.Register(
            nameof(ToolChromeControlsWholeWindow),
            typeof(bool),
            typeof(HostWindowControl),
            new PropertyMetadata(false));

        public bool ToolChromeControlsWholeWindow
        {
            get => (bool)GetValue(ToolChromeControlsWholeWindowProperty);
            set => SetValue(ToolChromeControlsWholeWindowProperty, value);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _dockControl = GetTemplateChild(DockControlName) as DockControl;
            _dockControl.Layout = DataContext as IDock;

            _titleBar = GetTemplateChild(TitleBarName) as HostWindowTitleBar;
            _ownerWindow.SetTitleBar(_titleBar);
            UpdateTemplateChilren();
        }

        private readonly DockManager _dockManager;
        private List<Grid> _chromeGrips = new();

        public IDockManager DockManager => _dockManager;

        public IHostWindowState HostWindowState => null;

        public bool IsTracked { get; set; }
        public IDockWindow Window { get; set; }

        public void Present(bool isDialog)
        {
            if (isDialog)
            {
                if (Window is { })
                {
                    Window.Factory?.OnWindowOpened(Window);
                }

                _ownerWindow.Show();

            }
            else
            {
                if (Window is { })
                {
                    Window.Factory?.OnWindowOpened(Window);
                }

                var ownerDockControl = Window?.Layout?.Factory?.DockControls.FirstOrDefault();

                if (ownerDockControl is Control control && HostWindow.GetWindowForElement(control) is Window parentWindow)
                {
                    _ownerWindow.Title = parentWindow.Title;
                    _ownerWindow.Show();
                }
                else
                {
                    _ownerWindow.Show();
                }
            }
        }

        /// <summary>
        /// Attaches grip to chrome.
        /// </summary>
        /// <param name="chromeControl">The chrome control.</param>
        public void AttachGrip(ToolChromeControl chromeControl)
        {
            if (_chromeGrips.Contains(chromeControl.Grip))
                return;

            if (chromeControl.CloseButton is not null)
            {
                chromeControl.CloseButton.Click += ChromeCloseClick;
            }

            if (chromeControl.Grip is { } grip)
            {
                _chromeGrips.Add(grip);
            }

            IsToolWindow = true;
        }

        /// <summary>
        /// Detaches grip to chrome.
        /// </summary>
        /// <param name="chromeControl">The chrome control.</param>
        public void DetachGrip(ToolChromeControl chromeControl)
        {
            if (chromeControl.Grip is { } grip)
            {
                _chromeGrips.Remove(grip);
            }

            if (chromeControl.CloseButton is not null)
            {
                chromeControl.CloseButton.Click -= ChromeCloseClick;
            }
        }

        private void ChromeCloseClick(object sender, RoutedEventArgs e)
        {
            if (CountVisibleToolsAndDocuments(DataContext as IRootDock) <= 1)
                Exit();
        }

        private int CountVisibleToolsAndDocuments(IDockable dockable)
        {
            switch (dockable)
            {
                case ITool: return 1;
                case IDocument: return 1;
                case IDock dock:
                    return dock.VisibleDockables?.Sum(CountVisibleToolsAndDocuments) ?? 0;
                default: return 0;
            }
        }

        public void Exit()
        {
            if (Window is { })
            {
                if (Window.OnClose())
                {
                    _ownerWindow.Close();
                }
            }
            else
            {
                _ownerWindow.Close();
            }
        }

        public void SetPosition(double x, double y)
        {
            if (!double.IsNaN(x) && !double.IsNaN(y))
            {
                _ownerWindow.Move((int)x, (int)y);
            }
        }

        public void GetPosition(out double x, out double y)
        {
            x = _ownerWindowX;
            y = _ownerWindowY;
        }

        public void SetSize(double width, double height)
        {
            if (!double.IsNaN(width))
            {
                _ownerWindow.Width = width;
            }

            if (!double.IsNaN(height))
            {
                _ownerWindow.Height = height;
            }
        }

        public void GetSize(out double width, out double height)
        {
            width = _ownerWindowWidth;
            height = _ownerWindowHeight;
        }

        public void SetTitle(string title)
        {
            _ownerWindow.Title = title;
        }

        public void SetLayout(IDock layout)
        {
            DataContext = layout;
            Content = layout;
            ToolChromeControlsWholeWindow = layout.OpenedDockablesCount < 2;
        }

        private WindowEx _ownerWindow;
        private DockControl _dockControl;
        private HostWindowTitleBar _titleBar;

        private double _ownerWindowX;
        private double _ownerWindowY;
        private double _ownerWindowWidth;
        private double _ownerWindowHeight;
    }
}

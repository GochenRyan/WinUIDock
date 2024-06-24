using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.WinUI3.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [TemplatePart(Name = DockControlName, Type = typeof(DockControl))]
    public class HostWindowControl : ContentControl, IHostWindow
    {
        public const string DockControlName = "PART_DockControl";

        public HostWindowControl(HostWindow hostWindow)
        {
            this.DefaultStyleKey = typeof(HostWindowControl);
            _ownerWindow = hostWindow;
            _ownerWindow.WindowContent = this;

            LayoutUpdated += HostWindowControl_LayoutUpdated;

            _dockManager = new DockManager();
            _hostWindowState = new HostWindowState(_dockManager, this);

            DataContextChanged += HostWindowControl_DataContextChanged;
        }

        private void HostWindowControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_dockControl != null)
            {
                _dockControl.Layout = DataContext as IDock;
            }
        }

        private void HostWindowControl_LayoutUpdated(object sender, object e)
        {
            if (Window is { } && _ownerWindow.AppWindow != null && IsTracked)
            {
                Window.Save();
            }
        }

        public static DependencyProperty IsToolWindowProperty = DependencyProperty.Register(
            nameof(IsToolWindow),
            typeof(bool),
            typeof(HostWindowControl),
            new PropertyMetadata(false));

        public bool IsToolWindow
        {
            get => (bool)GetValue(IsToolWindowProperty);
            set => SetValue(IsToolWindowProperty, value);
        }

        public static DependencyProperty ToolChromeControlsWholeWindowProperty = DependencyProperty.Register(
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

            _hostWindowTitleBar = new HostWindowTitleBar();
            _hostWindowTitleBar.ApplyTemplate();

            _hostWindowTitleBar.PointerPressed += _hostWindowTitleBar_PointerPressed;
            _ownerWindow.SetTitleBar(_hostWindowTitleBar);

            _dockControl = GetTemplateChild(DockControlName) as DockControl;
            _dockControl.Layout = DataContext as IDock;
        }

        private void _hostWindowTitleBar_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MoveDrag(e);
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (_chromeGrips.Any(grip => grip.CapturePointer(e.Pointer)))
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    MoveDrag(e);
                }
            }
        }

        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        {
            base.OnPointerMoved(e);

            if (Window is { } && IsTracked)
            {
                Window.Save();

                if (_mouseDown)
                {
                    Window.Factory?.OnWindowMoveDrag(Window);
                    _hostWindowState.Process(e.GetCurrentPoint(this).Position, EventType.Moved);
                }
            }
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_draggingWindow)
            {
                EndDrag(e);
            }
        }

        private void EndDrag(PointerRoutedEventArgs e)
        {
            Window?.Factory?.OnWindowMoveDragEnd(Window);
            _hostWindowState.Process(ClientPointToScreenRelativeToWindow(e.GetCurrentPoint(this).Position), EventType.Released);
            _mouseDown = false;
            _draggingWindow = false;
        }

        private void MoveDrag(PointerRoutedEventArgs e)
        {
            if (!ToolChromeControlsWholeWindow)
                return;

            if (Window?.Factory?.OnWindowMoveDragBegin(Window) != true)
            {
                return;
            }

            _mouseDown = true;
            _hostWindowState.Process(ClientPointToScreenRelativeToWindow(e.GetCurrentPoint(this).Position), EventType.Pressed);
            _draggingWindow = true;
        }

        private Point ClientPointToScreenRelativeToWindow(Point clientPoint)
        {
            GeneralTransform t = TransformToVisual(HostWindow.MainWindow.Content);

            var absScreenPoint = t.TransformPoint(clientPoint);
            var absScreenWindowPoint = t.TransformPoint(new Point(0, 0));
            var relativeScreenDiff = new Point(absScreenPoint.X - absScreenWindowPoint.X, absScreenPoint.Y - absScreenWindowPoint.Y);
            return relativeScreenDiff;
        }

        private readonly DockManager _dockManager;
        private readonly HostWindowState _hostWindowState;
        private List<Grid> _chromeGrips = new();
        private HostWindowTitleBar _hostWindowTitleBar;
        private bool _mouseDown, _draggingWindow;

        public IDockManager DockManager => _dockManager;

        public IHostWindowState HostWindowState => _hostWindowState;

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
            x = _ownerWindow.AppWindow.Position.X;
            y = _ownerWindow.AppWindow.Position.Y;
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
            width = _ownerWindow.Width;
            height = _ownerWindow.Height;
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

        WindowEx _ownerWindow;
        private DockControl _dockControl;
    }
}

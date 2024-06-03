using Dock.Model;
using Dock.Model.Core;
using Dock.WinUI3.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.System;
using Vector = Dock.WinUI3.Internal.Vector;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    [ContentProperty(Name = "Layout")]
    public sealed class DockControl : ContentControl, IDockControl
    {
        public DockControl()
        {
            this.DefaultStyleKey = typeof(DockControl);
        }

        //public static readonly DependencyProperty LayoutProperty = DependencyProperty.Register(
        //    nameof(Layout),
        //    typeof(IDock),
        //    typeof(DockControl),
        //    new PropertyMetadata(null, OnLayoutChanged));

        public static readonly DependencyProperty DefaultContextProperty = DependencyProperty.Register(
            nameof(DefaultContext),
            typeof(object),
            typeof(DockControl),
            new PropertyMetadata(null));

        public static readonly DependencyProperty InitializeLayoutProperty = DependencyProperty.Register(
            nameof(InitializeLayout),
            typeof(bool),
            typeof(DockControl),
            new PropertyMetadata(false));

        public static readonly DependencyProperty InitializeFactoryProperty = DependencyProperty.Register(
            nameof(InitializeFactory),
            typeof(bool),
            typeof(DockControl),
            new PropertyMetadata(false));

        public static readonly DependencyProperty FactoryProperty = DependencyProperty.Register(
            nameof(Factory),
            typeof(IFactory),
            typeof(DockControl),
            new PropertyMetadata(null));

        public static readonly DependencyProperty IsDraggingDockProperty = DependencyProperty.Register(
            nameof(IsDraggingDock),
            typeof(bool),
            typeof(DockControl),
            new PropertyMetadata(null));

        public IDock Layout
        {
            get { return (IDock)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public object DefaultContext
        {
            get { return GetValue(DefaultContextProperty); }
            set { SetValue(DefaultContextProperty, value); }
        }

        public bool InitializeLayout
        {
            get { return (bool)GetValue(InitializeLayoutProperty); }
            set { SetValue(InitializeLayoutProperty, value); }
        }

        public bool InitializeFactory
        {
            get { return (bool)GetValue(InitializeFactoryProperty); }
            set { SetValue(InitializeFactoryProperty, value); }
        }

        public IFactory Factory
        {
            get { return (IFactory)GetValue(FactoryProperty); }
            set { SetValue(FactoryProperty, value); }
        }

        public bool IsDraggingDock
        {
            get { return (bool)GetValue(IsDraggingDockProperty); }
            set { SetValue(IsDraggingDockProperty, value); }
        }

        public IDockManager DockManager => _dockManager;

        public IDockControlState DockControlState => _dockControlState;

        //private static void OnLayoutChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
        //{
        //    var control = ob as DockControl;
        //    if (control._isInitialized)
        //    {
        //        control.DeInitialize((IDock)args.OldValue);
        //    }

        //    control.Initialize((IDock)args.NewValue);
        //}

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_isInitialized)
            {
                DeInitialize(Layout);
            }

            Initialize(Layout);
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            if (_isInitialized)
            {
                DeInitialize((IDock)oldContent);
            }

            Initialize((IDock)newContent);
        }

        private void DeInitialize(IDock layout)
        {
            if (layout?.Factory is null)
            {
                return;
            }

            layout.Factory.DockControls.Remove(this);

            if (InitializeLayout)
            {
                if (layout.Close.CanExecute(null))
                {
                    layout.Close.Execute(null);
                }
            }

            _isInitialized = false;
        }

        private void Initialize(IDock layout)
        {
            if (layout is null)
            {
                return;
            }

            if (layout.Factory is null)
            {
                if (Factory is { })
                {
                    layout.Factory = Factory;
                }
                else
                {
                    return;
                }
            }

            layout.Factory.DockControls.Add(this);

            if (InitializeFactory)
            {
                layout.Factory.ContextLocator = new Dictionary<string, Func<object?>>();
                //layout.Factory.HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
                //{
                //    [nameof(IDockWindow)] = () => new HostWindow()
                //};
                layout.Factory.DockableLocator = new Dictionary<string, Func<IDockable?>>();
                layout.Factory.DefaultContextLocator = GetContext;
                //layout.Factory.DefaultHostWindowLocator = GetHostWindow;

                //IHostWindow GetHostWindow() => new HostWindow();

                object GetContext() => DefaultContext;
            }

            if (InitializeLayout)
            {
                layout.Factory.InitLayout(layout);
            }

            _isInitialized = true;
        }

        protected override void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
        {
            base.OnContentChanged(oldContentTemplate, newContentTemplate);
        }

        protected override void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)
        {
            base.OnContentTemplateSelectorChanged(oldContentTemplateSelector, newContentTemplateSelector);
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (Layout?.Factory?.DockControls is { })
            {
                var position = e.GetCurrentPoint(this).Position;
                var delta = new Vector();
                var action = ToDragAction(e);
                _dockControlState.Process(position, delta, EventType.Pressed, action, this, Layout.Factory.DockControls);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (Layout?.Factory?.DockControls is { })
            {
                var position = e.GetCurrentPoint(this).Position;
                var delta = new Vector();
                var action = ToDragAction(e);
                _dockControlState.Process(position, delta, EventType.Released, action, this, Layout.Factory.DockControls);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        {
            base.OnPointerMoved(e);
            if (Layout?.Factory?.DockControls is { })
            {
                var position = e.GetCurrentPoint(this).Position;
                var delta = new Vector();
                var action = ToDragAction(e);
                _dockControlState.Process(position, delta, EventType.Moved, action, this, Layout.Factory.DockControls);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
            if (Layout?.Factory?.DockControls is { })
            {
                var position = e.GetCurrentPoint(this).Position;
                var delta = new Vector();
                var action = ToDragAction(e);
                _dockControlState.Process(position, delta, EventType.Enter, action, this, Layout.Factory.DockControls);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            if (Layout?.Factory?.DockControls is { })
            {
                var position = e.GetCurrentPoint(this).Position;
                var delta = new Vector();
                var action = ToDragAction(e);
                _dockControlState.Process(position, delta, EventType.Leave, action, this, Layout.Factory.DockControls);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            if (Layout?.Factory?.DockControls is { })
            {
                var position = new Point();
                var delta = new Vector();
                var action = DragAction.None;
                _dockControlState.Process(position, delta, EventType.CaptureLost, action, this, Layout.Factory.DockControls);
            }
        }

        protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            if (Layout?.Factory?.DockControls is { })
            {
                var position = e.GetCurrentPoint(this).Position;
                var delta = new Vector(0, e.GetCurrentPoint(this).Properties.MouseWheelDelta);
                var action = ToDragAction(e);
                _dockControlState.Process(position, delta, EventType.WheelChanged, action, this, Layout.Factory.DockControls);
            }
        }

        private static DragAction ToDragAction(PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu))
            {
                return DragAction.Link;
            }

            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
            {
                return DragAction.Move;
            }

            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
            {
                return DragAction.Copy;
            }

            return DragAction.Move;
        }

        private readonly DockManager _dockManager;
        private readonly DockControlState _dockControlState;
        private bool _isInitialized;
    }
}

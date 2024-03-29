using Dock.Model.Adapters;
using Dock.Model.Core;
using Dock.Model.WinUI3.Internal;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Windows.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.Model.WinUI3.Core
{
    public abstract class DockBase : DockableBase, IDock
    {
        public DockBase()
        {
            this.DefaultStyleKey = typeof(DockBase);
            _navigateAdapter = new NavigateAdapter(this);
            _visibleDockables = new List<IDockable>();
            GoBack = Command.Create(() => _navigateAdapter.GoBack());
            GoForward = Command.Create(() => _navigateAdapter.GoForward());
            Navigate = Command.Create<object>(root => _navigateAdapter.Navigate(root, true));
            Close = Command.Create(() => _navigateAdapter.Close());
        }

        public IList<IDockable> VisibleDockables { get => (IList<IDockable>)GetValue(VisibleDockablesProperty); set => SetValue(VisibleDockablesProperty, value); }
        public IDockable ActiveDockable { get => (IDockable)GetValue(ActiveDockableProperty); set => SetValue(ActiveDockableProperty, value); }
        public IDockable DefaultDockable { get => (IDockable)GetValue(DefaultDockableProperty); set => SetValue(DefaultDockableProperty, value); }
        public IDockable FocusedDockable { get => (IDockable)GetValue(FocusedDockableProperty); set => SetValue(FocusedDockableProperty, value); }
        public double Proportion { get => (double)GetValue(ProportionProperty); set => SetValue(ProportionProperty, value); }
        public DockMode Dock { get => (DockMode)GetValue(DockProperty); set => SetValue(DockProperty, value); }
        public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }
        public bool IsEmpty { get => (bool)GetValue(IsEmptyProperty); set => SetValue(IsEmptyProperty, value); }
        public bool IsCollapsable { get => (bool)GetValue(IsCollapsableProperty); set => SetValue(IsCollapsableProperty, value); }
        public int OpenedDockablesCount { get => (int)GetValue(OpenedDockablesCountProperty); set => SetValue(OpenedDockablesCountProperty, value); }

        public bool CanGoBack => (bool)GetValue(CanGoBackProperty);

        public bool CanGoForward => (bool)GetValue(CanGoForwardProperty);

        public ICommand GoBack { get; }

        public ICommand GoForward { get; }

        public ICommand Navigate { get; }

        public ICommand Close { get; }


        DependencyProperty VisibleDockablesProperty = DependencyProperty.Register(
            nameof(VisibleDockables),
            typeof(IList<IDockable>),
            typeof(DockBase),
            new PropertyMetadata(default(IList<IDockable>)));

        DependencyProperty ActiveDockableProperty = DependencyProperty.Register(
            nameof(ActiveDockable),
            typeof(IDockable),
            typeof(DockBase),
            new PropertyMetadata(default(IDockable)));

        DependencyProperty DefaultDockableProperty = DependencyProperty.Register(
            nameof(DefaultDockable),
            typeof(IDockable),
            typeof(DockBase),
            new PropertyMetadata(default(IDockable)));

        DependencyProperty FocusedDockableProperty = DependencyProperty.Register(
            nameof(FocusedDockable),
            typeof(IDockable),
            typeof(DockBase),
            new PropertyMetadata(default(IDockable)));

        DependencyProperty ProportionProperty = DependencyProperty.Register(
            nameof(Proportion),
            typeof(double),
            typeof(DockBase),
            new PropertyMetadata(double.NaN));

        DependencyProperty DockProperty = DependencyProperty.Register(
            nameof(Dock),
            typeof(DockMode),
            typeof(DockBase),
            new PropertyMetadata(DockMode.Center));

        DependencyProperty IsActiveProperty = DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(DockBase),
            new PropertyMetadata(default(bool)));

        DependencyProperty IsEmptyProperty = DependencyProperty.Register(
            nameof(IsEmpty),
            typeof(bool),
            typeof(DockBase),
            new PropertyMetadata(default(bool)));

        DependencyProperty IsCollapsableProperty = DependencyProperty.Register(
            nameof(IsCollapsable),
            typeof(bool),
            typeof(DockBase),
            new PropertyMetadata(true));

        DependencyProperty OpenedDockablesCountProperty = DependencyProperty.Register(
            nameof(OpenedDockablesCount),
            typeof(int),
            typeof(DockBase),
            new PropertyMetadata(default(int)));

        DependencyProperty CanGoBackProperty = DependencyProperty.Register(
            nameof(CanGoBack),
            typeof(bool),
            typeof(DockBase),
            new PropertyMetadata(default(bool)));

        DependencyProperty CanGoForwardProperty = DependencyProperty.Register(
            nameof(CanGoForward),
            typeof(bool),
            typeof(DockBase),
            new PropertyMetadata(default(bool)));

        internal INavigateAdapter _navigateAdapter;
        private IList<IDockable> _visibleDockables;
    }
}

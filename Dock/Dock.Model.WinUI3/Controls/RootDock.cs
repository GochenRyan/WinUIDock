using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Core;
using Dock.Model.WinUI3.Internal;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Windows.Input;

namespace Dock.Model.WinUI3.Controls
{
    public class RootDock : DockBase, IRootDock
    {
        DependencyProperty IsFocusableRootProperty = DependencyProperty.Register(
            nameof(IsFocusableRoot),
            typeof(bool),
            typeof(RootDock),
            new PropertyMetadata(default));

        DependencyProperty HiddenDockablesProperty = DependencyProperty.Register(
            nameof(HiddenDockables),
            typeof(IList<IDockable>),
            typeof(RootDock),
            new PropertyMetadata(default));

        DependencyProperty LeftPinnedDockablesProperty = DependencyProperty.Register(
            nameof(LeftPinnedDockables),
            typeof(IList<IDockable>),
            typeof(RootDock),
            new PropertyMetadata(default));

        DependencyProperty RightPinnedDockablesProperty = DependencyProperty.Register(
            nameof(RightPinnedDockables),
            typeof(IList<IDockable>),
            typeof(RootDock),
            new PropertyMetadata(default));

        DependencyProperty TopPinnedDockablesProperty = DependencyProperty.Register(
            nameof(TopPinnedDockables),
            typeof(IList<IDockable>),
            typeof(RootDock),
            new PropertyMetadata(default));

        DependencyProperty BottomPinnedDockablesProperty = DependencyProperty.Register(
            nameof(BottomPinnedDockables),
            typeof(IList<IDockable>),
            typeof(RootDock),
            new PropertyMetadata(default));

        DependencyProperty PinnedDockProperty = DependencyProperty.Register(
            nameof(PinnedDock),
            typeof(IToolDock),
            typeof(RootDock),
            new PropertyMetadata(default));

        DependencyProperty WindowProperty = DependencyProperty.Register(
            nameof(Window),
            typeof(IDockWindow),
            typeof(RootDock),
            new PropertyMetadata(default));

        DependencyProperty WindowsProperty = DependencyProperty.Register(
            nameof(Windows),
            typeof(IList<IDockWindow>),
            typeof(RootDock),
            new PropertyMetadata(default));


        public bool IsFocusableRoot { get => (bool)GetValue(IsFocusableRootProperty); set => SetValue(IsFocusableRootProperty, value); }
        public IList<IDockable> HiddenDockables { get => (IList<IDockable>)GetValue(HiddenDockablesProperty); set => SetValue(HiddenDockablesProperty, value); }
        public IList<IDockable> LeftPinnedDockables { get => (IList<IDockable>)GetValue(LeftPinnedDockablesProperty); set => SetValue(LeftPinnedDockablesProperty, value); }
        public IList<IDockable> RightPinnedDockables { get => (IList<IDockable>)GetValue(RightPinnedDockablesProperty); set => SetValue(RightPinnedDockablesProperty, value); }
        public IList<IDockable> TopPinnedDockables { get => (IList<IDockable>)GetValue(TopPinnedDockablesProperty); set => SetValue(TopPinnedDockablesProperty, value); }
        public IList<IDockable> BottomPinnedDockables { get => (IList<IDockable>)GetValue(BottomPinnedDockablesProperty); set => SetValue(BottomPinnedDockablesProperty, value); }
        public IToolDock PinnedDock { get => (IToolDock)GetValue(PinnedDockProperty); set => SetValue(PinnedDockProperty, value); }
        public IDockWindow Window { get => (IDockWindow)GetValue(WindowProperty); set => SetValue(WindowProperty, value); }
        public IList<IDockWindow> Windows { get => (IList<IDockWindow>)GetValue(WindowsProperty); set => SetValue(WindowsProperty, value); }

        public ICommand ShowWindows { get; }

        public ICommand ExitWindows { get; }

        public RootDock()
        {
            ShowWindows = Command.Create(() => _navigateAdapter.ShowWindows());
            ExitWindows = Command.Create(() => _navigateAdapter.ExitWindows());
        }
    }
}

using Dock.Model.Core;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Dock.Model.Controls;

/// <summary>
/// Root dock contract.
/// </summary>
public interface IRootDock : IDock
{
    /// <summary>
    /// Gets or sets if root dock is focusable.
    /// </summary>
    bool IsFocusableRoot { get; set; }

    /// <summary>
    /// Gets or sets hidden dockables.
    /// </summary>
    ObservableCollection<IDockable>? HiddenDockables { get; set; }

    /// <summary>
    /// Gets or sets left pinned dockables.
    /// </summary>
    ObservableCollection<IDockable>? LeftPinnedDockables { get; set; }

    /// <summary>
    /// Gets or sets right pinned dockables.
    /// </summary>
    ObservableCollection<IDockable>? RightPinnedDockables { get; set; }

    /// <summary>
    /// Gets or sets top pinned dockables.
    /// </summary>
    ObservableCollection<IDockable>? TopPinnedDockables { get; set; }

    /// <summary>
    /// Gets or sets bottom pinned dockables.
    /// </summary>
    ObservableCollection<IDockable>? BottomPinnedDockables { get; set; }

    /// <summary>
    /// Gets or sets pinned tool dock.
    /// </summary>
    IToolDock? PinnedDock { get; set; }

    /// <summary>
    /// Gets or sets root left dock.
    /// </summary>
    IDock? RootLeftDock { get; set; }

    /// <summary>
    /// Gets or sets root right dock.
    /// </summary>
    IDock? RootRightDock { get; set; }

    /// <summary>
    /// Gets or sets root top dock.
    /// </summary>
    IDock? RootTopDock { get; set; }

    /// <summary>
    /// Gets or sets root bottom dock.
    /// </summary>
    IDock? RootBottomDock { get; set; }

    /// <summary>
    /// Gets or sets owner window.
    /// </summary>
    IDockWindow? Window { get; set; }

    /// <summary>
    /// Gets or sets windows.
    /// </summary>
    ObservableCollection<IDockWindow>? Windows { get; set; }

    /// <summary>
    /// Show windows.
    /// </summary>
    ICommand ShowWindows { get; }

    /// <summary>
    /// Exit windows.
    /// </summary>
    ICommand ExitWindows { get; }
}

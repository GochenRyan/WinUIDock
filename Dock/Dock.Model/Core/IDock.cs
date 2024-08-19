﻿using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Dock.Model.Core;

/// <summary>
/// Dock contract.
/// </summary>
public interface IDock : IDockable
{
    /// <summary>
    /// Gets or sets active dockable.
    /// </summary>
    IDockable? ActiveDockable { get; set; }

    /// <summary>
    /// Gets or sets default dockable.
    /// </summary>
    IDockable? DefaultDockable { get; set; }

    /// <summary>
    /// Gets or sets the focused dockable.
    /// Different windows can have their own focused dockable at the same time, 
    /// but there can only be one active dockable.
    /// </summary>
    IDockable? FocusedDockable { get; set; }

    /// <summary> 
    /// Gets or sets splitter proportion. 
    /// </summary> 
    double Proportion { get; set; }

    /// <summary> 
    /// Gets or sets docking mode. 
    /// </summary> 
    DockMode Dock { get; set; }

    /// <summary>
    /// Gets or sets if the dockable is the currently active.
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// Gets if the dockable is empty.
    /// </summary>
    bool IsEmpty { get; set; }

    /// <summary>
    /// Gets or sets if the Dock collapses when all its children are removed.
    /// </summary>
    bool IsCollapsable { get; set; }

    /// <summary>
    /// Gets the number of currently opened and visible dockables
    /// </summary>
    int OpenedDockablesCount { get; set; }

    /// <summary>
    /// Gets a value that indicates whether there is at least one entry in back navigation history.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets a value that indicates whether there is at least one entry in forward navigation history.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Navigates to the most recent entry in back navigation history, if there is one.
    /// </summary>
    ICommand GoBack { get; }

    /// <summary>
    /// Navigate to the most recent entry in forward navigation history, if there is one.
    /// </summary>
    ICommand GoForward { get; }

    /// <summary>
    /// Navigate to content that is contained by an object.
    /// </summary>
    ICommand Navigate { get; }

    /// <summary>
    /// Close layout.
    /// </summary>
    ICommand Close { get; }

    /// <summary>
    /// Gets or Sets visible dockables.
    /// </summary>
    ObservableCollection<IDockable> VisibleDockables { get; set; }
}

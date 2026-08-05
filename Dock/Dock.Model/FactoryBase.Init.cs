using Dock.Model.Controls;
using Dock.Model.Core;

namespace Dock.Model;

/// <summary>
/// Factory base class.
/// </summary>
public abstract partial class FactoryBase
{
    /// <inheritdoc/>
    public virtual void InitLayout(IDockable layout)
    {
        InitDockable(layout, null);

        if (layout is IDock dock)
        {
            // RootDockControl renders DefaultDockable: a root arriving without
            // one (deserialized layouts, typically) shows an EMPTY window however
            // complete its tree is.
            if (dock is IRootDock rootLayout && rootLayout.DefaultDockable is null)
            {
                rootLayout.DefaultDockable = FindRenderableChild(rootLayout);
            }

            if (dock.DefaultDockable is not null)
            {
                dock.ActiveDockable = dock.DefaultDockable;
            }
        }

        if (layout is IRootDock rootDock)
        {
            if (rootDock.ShowWindows.CanExecute(null))
            {
                rootDock.ShowWindows.Execute(null);
            }
        }
    }

    /// <summary>The first child a root dock could sensibly render: splitters and
    /// leaf dockables are skipped, so an empty root still yields null.</summary>
    private static IDockable? FindRenderableChild(IRootDock rootDock)
    {
        if (rootDock.VisibleDockables is not { } dockables)
        {
            return null;
        }

        foreach (var dockable in dockables)
        {
            if (dockable is IDock and not IProportionalDockSplitter)
            {
                return dockable;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public virtual void InitDockable(IDockable dockable, IDockable? owner)
    {
        if (dockable.Context is null)
        {
            if (GetContext(dockable.Kind) is { } context)
            {
                dockable.Context = context;
            }
        }

        dockable.Owner = owner;

        if (dockable is IDock dock)
        {
            dock.Factory = this;

            if (dock.VisibleDockables is not null)
            {
                foreach (var child in dock.VisibleDockables)
                {
                    InitDockable(child, dockable);
                }

                UpdateIsEmpty(dock);
            }

            // Tabbed docks render their ActiveDockable: tabs present but none
            // active draws an empty chrome. Pane-level sibling of the root's
            // DefaultDockable heal above.
            if (dock is IToolDock or IDocumentDock
                && dock.ActiveDockable is null
                && dock.VisibleDockables?.Count > 0)
            {
                dock.ActiveDockable = dock.VisibleDockables[0];
            }
        }

        if (dockable is IRootDock rootDock)
        {
            if (rootDock.Windows is not null)
            {
                foreach (var child in rootDock.Windows)
                {
                    InitDockWindow(child, dockable);
                }
            }
        }

        OnDockableInit(dockable);
    }

    /// <inheritdoc/>
    public virtual void InitDockWindow(IDockWindow window, IDockable? owner)
    {
        window.Host = GetHostWindow(window.Kind);
        if (window.Host is not null)
        {
            window.Host.Window = window;
        }

        window.Owner = owner;
        window.Factory = this;

        if (window.Layout is not null)
        {
            InitDockable(window.Layout, window.Layout.Owner);
        }
    }

    /// <inheritdoc/>
    public virtual void InitActiveDockable(IDockable? dockable, IDock owner)
    {
        OnActiveDockableChanged(dockable);

        if (dockable is { })
        {
            InitDockable(dockable, owner);
            dockable.OnSelected();
        }

        if (dockable is { })
        {
            SetFocusedDockable(owner, dockable);
        }
    }

    /// <inheritdoc/>
    public virtual void SetActiveDockable(IDockable dockable)
    {
        if (dockable.Owner is IDock dock)
        {
            dock.ActiveDockable = dockable;
        }
    }

    private void SetIsActive(IDockable dockable, bool active)
    {
        if (dockable is IDock dock)
        {
            dock.IsActive = active;
        }
    }

    /// <inheritdoc />
    public virtual void SetFocusedDockable(IDock dock, IDockable? dockable)
    {
        if (dock.ActiveDockable is not null && FindRoot(dock.ActiveDockable, x => x.IsFocusableRoot) is { } root)
        {
            if (dockable is not null)
            {
                var results = Find(x => x is IRootDock);

                foreach (var result in results)
                {
                    if (result is IRootDock rootDock
                        && rootDock.IsFocusableRoot
                        && rootDock != root)
                    {
                        if (rootDock.FocusedDockable?.Owner is not null)
                        {
                            SetIsActive(rootDock.FocusedDockable.Owner, false);
                        }
                    }
                }
            }

            if (root.FocusedDockable?.Owner is not null)
            {
                SetIsActive(root.FocusedDockable.Owner, false);
            }

            if (dockable is not null)
            {
                if (root.FocusedDockable != dockable)
                {
                    root.FocusedDockable = dockable;
                    OnFocusedDockableChanged(dockable);
                }
            }

            if (root.FocusedDockable?.Owner is not null)
            {
                SetIsActive(root.FocusedDockable.Owner, true);
            }
        }
    }
}

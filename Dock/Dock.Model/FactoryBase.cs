using Dock.Model.Controls;
using Dock.Model.Core;
using System.Collections.ObjectModel;

namespace Dock.Model;

/// <summary>
/// Factory base class.
/// </summary>
public abstract partial class FactoryBase : IFactory
{
    private bool IsDockPinned(ObservableCollection<IDockable>? pinnedDockables, IDock dock)
    {
        if (pinnedDockables is not null && pinnedDockables.Count != 0)
        {
            foreach (var pinnedDockable in pinnedDockables)
            {
                if (pinnedDockable.Owner == dock)
                {
                    return true;
                }
            }
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public virtual void CollapseDock(IDock dock)
    {
        if (!dock.IsCollapsable || dock.VisibleDockables is null || dock.VisibleDockables.Count != 0 || !dock.CanClose)
        {
            return;
        }

        var rootDock = FindRoot(dock, _ => true);
        if (rootDock is { })
        {
            if (dock is IToolDock toolDock)
            {
                if (toolDock.Alignment == Alignment.Left
                    && IsDockPinned(rootDock.LeftPinnedDockables, dock))
                {
                    return;
                }

                if (toolDock.Alignment == Alignment.Right
                    && IsDockPinned(rootDock.RightPinnedDockables, dock))
                {
                    return;
                }

                if (toolDock.Alignment == Alignment.Top
                    && IsDockPinned(rootDock.TopPinnedDockables, dock))
                {
                    return;
                }

                if (toolDock.Alignment == Alignment.Bottom
                    && IsDockPinned(rootDock.BottomPinnedDockables, dock))
                {
                    return;
                }
            }
        }

        if (dock.Owner is IDock ownerDock && ownerDock.VisibleDockables is { })
        {
            var toRemove = new List<IDockable>();
            var dockIndex = ownerDock.VisibleDockables.IndexOf(dock);

            if (dockIndex >= 0)
            {
                var indexSplitterPrevious = dockIndex - 1;
                if (dockIndex > 0 && indexSplitterPrevious >= 0)
                {
                    var previousVisible = ownerDock.VisibleDockables[indexSplitterPrevious];
                    if (previousVisible is IProportionalDockSplitter splitterPrevious)
                    {
                        toRemove.Add(splitterPrevious);
                    }
                }

                var indexSplitterNext = dockIndex + 1;
                if (dockIndex < ownerDock.VisibleDockables.Count - 1 && indexSplitterNext >= 0)
                {
                    var nextVisible = ownerDock.VisibleDockables[indexSplitterNext];
                    if (nextVisible is IProportionalDockSplitter splitterNext)
                    {
                        toRemove.Add(splitterNext);
                    }
                }

                foreach (var removeVisible in toRemove)
                {
                    RemoveDockable(removeVisible, true);
                }
            }
        }

        if (dock is IRootDock rootDockDock && rootDockDock.Window is { })
        {
            RemoveWindow(rootDockDock.Window);
        }
        else
        {
            // Note where it sat BEFORE removing it — afterwards the owner link and
            // the index are gone.
            var collapseOwner = dock.Owner as IDock;
            var collapseIndex = collapseOwner?.VisibleDockables?.IndexOf(dock) ?? -1;

            RemoveDockable(dock, true);

            // Park it in HiddenDockables rather than dropping it, so reopening one
            // of its tools can bring the whole dock back to this spot. Without this
            // the position is simply lost and the tool has nowhere to return to.
            if (collapseOwner is not null && collapseIndex >= 0)
            {
                dock.RestoreOwner = collapseOwner;
                dock.RestoreIndex = collapseIndex;

                if (FindRoot(collapseOwner, _ => true) is { } collapseRoot)
                {
                    collapseRoot.HiddenDockables ??= new ObservableCollection<IDockable>(CreateList<IDockable>());
                    if (!collapseRoot.HiddenDockables.Contains(dock))
                    {
                        collapseRoot.HiddenDockables.Add(dock);
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public virtual IDock CreateSplitLayout(IDock dock, IDockable dockable, DockOperation operation)
    {
        IDock? split;

        if (dockable is IDock dockableDock)
        {
            split = dockableDock;
        }
        else
        {
            split = CreateProportionalDock();
            split.Title = nameof(IProportionalDock);
            split.VisibleDockables = new ObservableCollection<IDockable>(CreateList<IDockable>());
            if (split.VisibleDockables is not null)
            {
                AddVisibleDockable(split, dockable);
                OnDockableAdded(dockable);
                split.ActiveDockable = dockable;
            }
        }

        var containerProportion = dock.Proportion;
        dock.Proportion = double.NaN;

        var layout = CreateProportionalDock();
        layout.Title = nameof(IProportionalDock);
        layout.VisibleDockables = new ObservableCollection<IDockable>(CreateList<IDockable>());
        layout.Proportion = containerProportion;

        var splitter = CreateProportionalDockSplitter();
        splitter.Title = nameof(IProportionalDockSplitter);

        switch (operation)
        {
            case DockOperation.Left:
            case DockOperation.Right:
                {
                    layout.Orientation = Orientation.Horizontal;
                    break;
                }
            case DockOperation.Top:
            case DockOperation.Bottom:
                {
                    layout.Orientation = Orientation.Vertical;
                    break;
                }
        }

        switch (operation)
        {
            case DockOperation.Left:
            case DockOperation.Top:
                {
                    if (layout.VisibleDockables is not null)
                    {
                        AddVisibleDockable(layout, split);
                        OnDockableAdded(split);
                        layout.ActiveDockable = split;
                    }

                    break;
                }
            case DockOperation.Right:
            case DockOperation.Bottom:
                {
                    if (layout.VisibleDockables is not null)
                    {
                        AddVisibleDockable(layout, dock);
                        OnDockableAdded(dock);
                        layout.ActiveDockable = dock;
                    }

                    break;
                }
        }

        AddVisibleDockable(layout, splitter);
        OnDockableAdded(splitter);

        switch (operation)
        {
            case DockOperation.Left:
            case DockOperation.Top:
                {
                    if (layout.VisibleDockables is not null)
                    {
                        AddVisibleDockable(layout, dock);
                        OnDockableAdded(dock);
                        layout.ActiveDockable = dock;
                    }

                    break;
                }
            case DockOperation.Right:
            case DockOperation.Bottom:
                {
                    if (layout.VisibleDockables is not null)
                    {
                        AddVisibleDockable(layout, split);
                        OnDockableAdded(split);
                        layout.ActiveDockable = split;
                    }

                    break;
                }
        }

        return layout;
    }

    /// <inheritdoc/>
    public virtual void SplitToDock(IDock dock, IDockable dockable, DockOperation operation)
    {
        switch (operation)
        {
            case DockOperation.Left:
            case DockOperation.Right:
            case DockOperation.Top:
            case DockOperation.Bottom:
                {
                    if (dock.Owner is IDock ownerDock && ownerDock.VisibleDockables is { })
                    {
                        var index = ownerDock.VisibleDockables.IndexOf(dock);
                        if (index >= 0)
                        {
                            var layout = CreateSplitLayout(dock, dockable, operation);
                            RemoveVisibleDockableAt(ownerDock, index);
                            OnDockableRemoved(dockable);
                            InsertVisibleDockable(ownerDock, index, layout);
                            OnDockableAdded(dockable);
                            InitDockable(layout, ownerDock);
                            ownerDock.ActiveDockable = layout;
                        }
                    }
                    break;
                }
            default:
                throw new NotSupportedException($"Not supported split operation: {operation}.");
        }
    }

    /// <summary>
    /// Share of the window handed to a freshly created edge region. Public because
    /// the WinUI3 drop preview draws its edge band from the same number — a second
    /// copy there is how the preview starts lying about where the drop will land.
    /// </summary>
    public const double RootEdgeProportion = 0.2;

    /// <inheritdoc/>
    public virtual bool SplitToRootEdge(IRootDock rootDock, IDock dock, DockOperation operation)
    {
        var orientation = operation switch
        {
            DockOperation.RootLeft or DockOperation.RootRight => Orientation.Horizontal,
            DockOperation.RootTop or DockOperation.RootBottom => Orientation.Vertical,
            _ => throw new NotSupportedException($"Not supported root edge operation: {operation}.")
        };

        // Resolve the layout LAST — moving the source out of its old owner can
        // collapse an emptied dock and reshape the tree in the meantime.
        if (GetRootLayout(rootDock) is not { } layout)
        {
            return false;
        }

        var index = rootDock.VisibleDockables?.IndexOf(layout) ?? -1;

        if (!InsertAtContainerEdge(layout, dock, operation, orientation))
        {
            return false;
        }

        // RootDockControl renders DefaultDockable, NOT VisibleDockables. If the
        // layout was wrapped, leaving it pointing at the node we just wrapped would
        // put the new edge region in the tree but keep it off screen.
        if (index >= 0 && rootDock.VisibleDockables is { } rootDockables && index < rootDockables.Count)
        {
            rootDock.DefaultDockable = rootDockables[index];
        }

        return true;
    }

    /// <summary>
    /// Puts <paramref name="dock"/> at one end of <paramref name="container"/> so it
    /// spans that container.
    /// </summary>
    private bool InsertAtContainerEdge(IDock container, IDock dock, DockOperation operation, Orientation orientation)
    {
        var atStart = operation is DockOperation.RootLeft or DockOperation.RootTop;

        // Unconditional: a proportion only means anything relative to the siblings
        // it was measured against. Carrying one across containers is what let a dock
        // arriving from a float window take the whole edge — ProportionalStackPanel
        // writes 1.0 back into the model for the lone child of a container, which is
        // exactly the shape a float window has.
        dock.Proportion = RootEdgeProportion;

        // Container already runs along the required axis: insert in place. Going
        // through CreateSplitLayout here would wrap it again, so three drops on the
        // same edge would nest three levels deep for nothing.
        if (container is IProportionalDock proportional
            && proportional.Orientation == orientation
            && proportional.VisibleDockables is { })
        {
            var splitter = CreateProportionalDockSplitter();
            splitter.Title = nameof(IProportionalDockSplitter);

            if (atStart)
            {
                InsertVisibleDockable(proportional, 0, splitter);
                OnDockableAdded(splitter);
                InsertVisibleDockable(proportional, 0, dock);
                OnDockableAdded(dock);
            }
            else
            {
                AddVisibleDockable(proportional, splitter);
                OnDockableAdded(splitter);
                AddVisibleDockable(proportional, dock);
                OnDockableAdded(dock);
            }

            InitDockable(splitter, proportional);
            InitDockable(dock, proportional);
            proportional.ActiveDockable = dock;
            return true;
        }

        // Otherwise wrap the container in one of the required orientation.
        SplitToDock(container, dock, ToLocalOperation(operation));
        return true;
    }

    private static DockOperation ToLocalOperation(DockOperation operation)
    {
        return operation switch
        {
            DockOperation.RootLeft => DockOperation.Left,
            DockOperation.RootRight => DockOperation.Right,
            DockOperation.RootTop => DockOperation.Top,
            DockOperation.RootBottom => DockOperation.Bottom,
            _ => operation
        };
    }

    /// <summary>
    /// The single layout node a root dock hosts. Edge docking inserts next to
    /// this node, which is what makes the new region span the whole window.
    ///
    /// DefaultDockable comes first because that is what RootDockControl actually
    /// binds its content to — anchoring anywhere else would build a correct tree
    /// that never reaches the screen.
    /// </summary>
    public static IDock? GetRootLayout(IRootDock rootDock)
    {
        if (rootDock.VisibleDockables is not { } dockables)
        {
            return null;
        }

        if (rootDock.DefaultDockable is IDock defaultDock && dockables.Contains(defaultDock))
        {
            return defaultDock;
        }

        if (rootDock.ActiveDockable is IDock active && dockables.Contains(active))
        {
            return active;
        }

        foreach (var dockable in dockables)
        {
            if (dockable is IDock dock)
            {
                return dock;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public virtual IDockWindow? CreateWindowFrom(IDockable dockable)
    {
        IDockable? target;

        switch (dockable)
        {
            case ITool:
                {
                    // Kind is left to the concrete dockable's constructor so a host
                    // that overrides CreateToolDock keeps its own kind.
                    target = CreateToolDock();
                    target.Title = nameof(IToolDock);
                    if (target is IDock dock)
                    {
                        dock.VisibleDockables = new ObservableCollection<IDockable>(CreateList<IDockable>());
                        if (dock.VisibleDockables is not null)
                        {
                            AddVisibleDockable(dock, dockable);
                            OnDockableAdded(dockable);
                            dock.ActiveDockable = dockable;
                        }
                    }
                    break;
                }
            case IDocument:
                {
                    target = CreateDocumentDock();
                    target.Title = nameof(IDocumentDock);
                    if (target is IDock dock)
                    {
                        dock.VisibleDockables = new ObservableCollection<IDockable>(CreateList<IDockable>());
                        if (dockable.Owner is IDocumentDock sourceDocumentDock)
                        {
                            if (target is IDocumentDock targetDocumentDock)
                            {
                                // Carry the category over, not the instance identity —
                                // the floated dock is a new instance.
                                targetDocumentDock.Kind = sourceDocumentDock.Kind;
                                targetDocumentDock.CanCreateDocument = sourceDocumentDock.CanCreateDocument;

                                if (sourceDocumentDock is IDocumentDockContent sourceDocumentDockContent
                                    && targetDocumentDock is IDocumentDockContent targetDocumentDockContent)
                                {

                                    targetDocumentDockContent.DocumentTemplate = sourceDocumentDockContent.DocumentTemplate;
                                }
                            }
                        }
                        if (dock.VisibleDockables is not null)
                        {
                            AddVisibleDockable(dock, dockable);
                            OnDockableAdded(dockable);
                            dock.ActiveDockable = dockable;
                        }
                    }
                    break;
                }
            case IToolDock:
                {
                    target = dockable;
                    break;
                }
            case IDocumentDock:
                {
                    target = dockable;
                    break;
                }
            case IProportionalDock proportionalDock:
                {
                    target = proportionalDock;
                    break;
                }
            case IDockDock dockDock:
                {
                    target = dockDock;
                    break;
                }
            case IRootDock rootDock:
                {
                    target = rootDock.ActiveDockable;
                    break;
                }
            default:
                {
                    return null;
                }
        }

        var root = CreateRootDock();
        root.Title = nameof(IRootDock);
        root.VisibleDockables = new ObservableCollection<IDockable>(CreateList<IDockable>());
        if (root.VisibleDockables is not null && target is not null)
        {
            if (target is not IProportionalDock proportionDock)
            {
                proportionDock = CreateProportionalDock();
            }

            AddVisibleDockable(root, proportionDock);
            OnDockableAdded(proportionDock);
            AddVisibleDockable(proportionDock, target);
            OnDockableAdded(target);
            root.ActiveDockable = proportionDock;
            root.DefaultDockable = proportionDock;
        }
        root.Owner = null;

        var window = CreateDockWindow();
        window.Title = "";
        window.WindowWidth = double.NaN;
        window.WindowHeight = double.NaN;
        window.Layout = root;

        root.Window = window;

        return window;
    }

    /// <inheritdoc/>
    public virtual void SplitToWindow(IDock dock, IDockable dockable, double x, double y, double width, double height)
    {
        var rootDock = FindRoot(dock, _ => true);
        if (rootDock is null)
        {
            return;
        }

        // A dockable leaving for its own window must not stay pinned — the pin
        // flyout would keep a live host that fights the float window's one for
        // the shared content. Here at the choke point, not in each caller.
        DockDiagnostics.Log(() =>
            $"SplitToWindow: {DockDiagnostics.Describe(dockable)} owner={DockDiagnostics.Describe(dockable.Owner)} "
            + $"pinned={IsDockablePinned(dockable)}");
        UnpinDockable(dockable);

        RemoveDockable(dockable, true);

        var window = CreateWindowFrom(dockable);
        if (window is not null)
        {
            AddWindow(rootDock, window);
            window.X = x;
            window.Y = y;
            window.WindowWidth = width;

            //TODO: fix height reduction after multiple splits
            window.WindowHeight = height;
            window.Present(false);

            if (window.Layout is { })
            {
                SetFocusedDockable(window.Layout, dockable);
            }
        }
    }
}

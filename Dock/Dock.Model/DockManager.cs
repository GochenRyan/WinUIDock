using Dock.Model.Controls;
using Dock.Model.Core;
using System.Collections.ObjectModel;

namespace Dock.Model;

/// <summary>
/// Docking manager.
/// </summary>
public class DockManager : IDockManager
{
    /// <inheritdoc/>
    public DockPoint Position { get; set; }

    /// <inheritdoc/>
    public DockPoint ScreenPosition { get; set; }

    private bool MoveDockable(IDockable sourceDockable, IDock sourceDockableOwner, IDock targetDock, bool bExecute)
    {
        if (sourceDockableOwner == targetDock)
        {
            if (targetDock.VisibleDockables?.Count == 1)
            {
                return false;
            }
        }
        var targetDockable = targetDock.ActiveDockable;
        if (targetDockable is null)
        {
            targetDockable = targetDock.VisibleDockables?.LastOrDefault();
            if (targetDockable is null)
            {
                if (bExecute)
                {
                    if (sourceDockableOwner.Factory is { } factory)
                    {
                        factory.MoveDockable(sourceDockableOwner, targetDock, sourceDockable, null);
                    }
                }
                return true;
            }
        }
        if (bExecute)
        {
            if (sourceDockableOwner.Factory is { } factory)
            {
                factory.MoveDockable(sourceDockableOwner, targetDock, sourceDockable, targetDockable);
            }
        }
        return true;
    }

    private bool SwapDockable(IDockable sourceDockable, IDock sourceDockableOwner, IDock targetDock, bool bExecute)
    {
        var targetDockable = targetDock.ActiveDockable;
        if (targetDockable is null)
        {
            targetDockable = targetDock.VisibleDockables?.LastOrDefault();
            if (targetDockable is null)
            {
                return false;
            }
        }
        if (bExecute)
        {
            if (sourceDockableOwner.Factory is { } factory)
            {
                factory.SwapDockable(sourceDockableOwner, targetDock, sourceDockable, targetDockable);
            }
        }
        return true;
    }

    private void SplitToolDockable(IDockable sourceDockable, IDock sourceDockableOwner, IDock targetDock, DockOperation operation)
    {
        if (targetDock.Factory is not { } factory)
        {
            return;
        }

        var targetToolDock = factory.CreateToolDock();
        targetToolDock.Title = nameof(IToolDock);
        targetToolDock.Alignment = operation.ToAlignment();
        targetToolDock.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>());
        factory.MoveDockable(sourceDockableOwner, targetToolDock, sourceDockable, null);
        factory.SplitToDock(targetDock, targetToolDock, operation);
    }

    private void SplitDocumentDockable(IDockable sourceDockable, IDock sourceDockableOwner, IDock targetDock, DockOperation operation)
    {
        if (targetDock.Factory is not { } factory)
        {
            return;
        }

        var targetDocumentDock = factory.CreateDocumentDock();
        targetDocumentDock.Title = nameof(IDocumentDock);
        targetDocumentDock.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>());
        if (sourceDockableOwner is IDocumentDock sourceDocumentDock)
        {
            // Carry the category over, not the instance identity — the split
            // creates a new dock instance.
            targetDocumentDock.Kind = sourceDocumentDock.Kind;
            targetDocumentDock.CanCreateDocument = sourceDocumentDock.CanCreateDocument;

            if (sourceDocumentDock is IDocumentDockContent sourceDocumentDockContent
                && targetDocumentDock is IDocumentDockContent targetDocumentDockContent)
            {
                targetDocumentDockContent.DocumentTemplate = sourceDocumentDockContent.DocumentTemplate;
            }
        }
        factory.MoveDockable(sourceDockableOwner, targetDocumentDock, sourceDockable, null);
        factory.SplitToDock(targetDock, targetDocumentDock, operation);
    }

    private bool SplitDockable(IDockable sourceDockable, IDock sourceDockableOwner, IDock targetDock, DockOperation operation, bool bExecute)
    {
        switch (sourceDockable)
        {
            case ITool _:
                {
                    if (sourceDockableOwner == targetDock)
                    {
                        if (targetDock.VisibleDockables?.Count == 1)
                        {
                            return false;
                        }
                    }

                    if (bExecute)
                    {
                        SplitToolDockable(sourceDockable, sourceDockableOwner, targetDock, operation);
                    }

                    return true;
                }
            case IDocument _:
                {
                    if (sourceDockableOwner == targetDock)
                    {
                        if (targetDock.VisibleDockables?.Count == 1)
                        {
                            return false;
                        }
                    }

                    if (bExecute)
                    {
                        SplitDocumentDockable(sourceDockable, sourceDockableOwner, targetDock, operation);
                    }

                    return true;
                }
            default:
                {
                    return false;
                }
        }
    }

    private bool DockDockableIntoWindow(IDockable sourceDockable, IDockable targetDockable, bool bExecute)
    {
        if (sourceDockable == targetDockable)
        {
            return false;
        }

        if (!sourceDockable.CanFloat)
        {
            return false;
        }

        if (sourceDockable.Owner is not IDock sourceDockableOwner)
        {
            return false;
        }

        if (sourceDockableOwner.Factory is not { } factory)
        {
            return false;
        }

        if (factory.FindRoot(sourceDockable, _ => true) is { } sourceRoot
            && sourceRoot.ActiveDockable is IDock targetWindowOwner)
        {
            // Dragging the ONLY content of a float window "out" would tear that
            // window down and immediately rebuild an identical one for the same
            // dockable (SplitToWindow = RemoveDockable + CreateWindowFrom):
            // visible flicker, a brand new layout tree, and the window shrinks
            // because its size gets re-derived from the content bounds. The
            // dockable already floats on its own — treat it as a no-op.
            if (sourceRoot.Window is not null && CountDockables(sourceRoot) <= 1)
            {
                return false;
            }

            if (bExecute)
            {
                sourceDockableOwner.GetVisibleBounds(out _, out _, out var ownerWidth, out var ownerHeight);
                sourceDockable.GetVisibleBounds(out _, out _, out var width, out var height);
                var x = ScreenPosition.X;
                var y = ScreenPosition.Y;

                // Splitting out of an existing float window: inherit that
                // window's size. Deriving it from the dockable's visible bounds
                // yields the CONTENT size, so the new window loses the chrome
                // every round (the "height reduction after multiple splits"
                // TODO in FactoryBase.SplitToWindow).
                if (sourceRoot.Window is { } sourceWindow
                    && !double.IsNaN(sourceWindow.WindowWidth) && sourceWindow.WindowWidth > 0
                    && !double.IsNaN(sourceWindow.WindowHeight) && sourceWindow.WindowHeight > 0)
                {
                    width = sourceWindow.WindowWidth;
                    height = sourceWindow.WindowHeight;
                }

                if (double.IsNaN(width))
                {
                    width = double.IsNaN(ownerWidth) ? 300 : ownerWidth;
                }

                if (double.IsNaN(height))
                {
                    height = double.IsNaN(ownerHeight) ? 400 : ownerHeight;
                }

                factory.SplitToWindow(targetWindowOwner, sourceDockable, x, y, width, height);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Counts the leaf dockables (tools/documents) under a dockable. Stops
    /// early past one — callers only ask "is there more than a single one".
    /// </summary>
    private static int CountDockables(IDockable? dockable)
    {
        switch (dockable)
        {
            case null:
                return 0;
            case IDock dock:
                {
                    var count = 0;
                    if (dock.VisibleDockables is { } children)
                    {
                        foreach (var child in children)
                        {
                            count += CountDockables(child);
                            if (count > 1)
                            {
                                return count;
                            }
                        }
                    }

                    return count;
                }
            default:
                return 1;
        }
    }

    /// <summary>
    /// True when moving <paramref name="sourceDockable"/> out would collapse the
    /// root's layout node away entirely: the source's dock is left empty and gets
    /// collapsed, which empties ITS container, and so on up to the root. Once the
    /// root layout is gone there is nothing left to insert an edge region beside,
    /// and the dockable ends up in a dock attached to nothing.
    ///
    /// Walks the owner chain rather than counting leaves — the cascade is
    /// structural, and one surviving sibling anywhere along the chain stops it
    /// (even an empty dock, which is a node the layout keeps).
    /// </summary>
    private static bool WouldEmptyRootLayout(IDockable sourceDockable, IRootDock rootDock)
    {
        IDockable node = sourceDockable;

        for (var owner = node.Owner as IDock; owner is not null; owner = owner.Owner as IDock)
        {
            var keepsContent = false;

            if (owner.VisibleDockables is { } children)
            {
                foreach (var child in children)
                {
                    // Splitters do not keep a dock alive — RemoveDockable strips the
                    // orphaned ones as the neighbours go.
                    if (!ReferenceEquals(child, node) && child is not IProportionalDockSplitter)
                    {
                        keepsContent = true;
                        break;
                    }
                }
            }

            if (keepsContent)
            {
                return false;
            }

            if (ReferenceEquals(owner, rootDock))
            {
                return true;
            }

            node = owner;
        }

        // The source does not live under this root — dragging in from another
        // window empties nothing here.
        return false;
    }

    private bool DockDockableIntoDockable(IDockable sourceDockable, IDockable targetDockable, DragAction action, bool bExecute)
    {
        if (sourceDockable.Owner is not IDock sourceDockableOwner || targetDockable.Owner is not IDock targetDockableOwner)
        {
            return false;
        }

        return sourceDockableOwner == targetDockableOwner
            ? DockDockable(sourceDockable, sourceDockableOwner, targetDockable, action, bExecute)
            : DockDockable(sourceDockable, sourceDockableOwner, targetDockable, targetDockableOwner, action, bExecute);
    }

    private bool DockDockable(IDockable sourceDockable, IDock sourceDockableOwner, IDockable targetDockable, DragAction action, bool bExecute)
    {
        switch (action)
        {
            case DragAction.Copy:
                {
                    return false;
                }
            case DragAction.Move:
                {
                    if (bExecute && sourceDockableOwner.Factory is { } factory)
                    {
                        factory.MoveDockable(sourceDockableOwner, sourceDockable, targetDockable);
                    }

                    return true;
                }
            case DragAction.Link:
                {
                    if (bExecute && sourceDockableOwner.Factory is { } factory)
                    {
                        factory.SwapDockable(sourceDockableOwner, sourceDockable, targetDockable);
                    }

                    return true;
                }
            default:
                {
                    return false;
                }
        }
    }

    private bool DockDockable(IDockable sourceDockable, IDock sourceDockableOwner, IDockable targetDockable, IDock targetDockableOwner, DragAction action, bool bExecute)
    {
        switch (action)
        {
            case DragAction.Copy:
                {
                    return false;
                }
            case DragAction.Move:
                {
                    if (bExecute && sourceDockableOwner.Factory is { } factory)
                    {
                        factory.MoveDockable(sourceDockableOwner, targetDockableOwner, sourceDockable, targetDockable);
                    }

                    return true;
                }
            case DragAction.Link:
                {
                    if (bExecute && sourceDockableOwner.Factory is { } factory)
                    {
                        factory.SwapDockable(sourceDockableOwner, targetDockableOwner, sourceDockable, targetDockable);
                    }

                    return true;
                }
            default:
                {
                    return false;
                }
        }
    }

    private bool DockDockable(IDockable sourceDockable, IDock sourceDockableOwner, IDock targetDock, DockOperation operation, bool bExecute)
    {
        return operation switch
        {
            DockOperation.Fill => MoveDockable(sourceDockable, sourceDockableOwner, targetDock, bExecute),
            DockOperation.Left => SplitDockable(sourceDockable, sourceDockableOwner, targetDock, operation, bExecute),
            DockOperation.Right => SplitDockable(sourceDockable, sourceDockableOwner, targetDock, operation, bExecute),
            DockOperation.Top => SplitDockable(sourceDockable, sourceDockableOwner, targetDock, operation, bExecute),
            DockOperation.Bottom => SplitDockable(sourceDockable, sourceDockableOwner, targetDock, operation, bExecute),
            DockOperation.Window => DockDockableIntoWindow(sourceDockable, targetDock, bExecute),
            _ => false
        };
    }

    private bool DockDockable(IDock sourceDock, IDock targetDock, DragAction action, DockOperation operation, bool bExecute)
    {
        return DockDockable(sourceDock, targetDock, targetDock, action, operation, bExecute);
    }

    private static bool IsRootOperation(DockOperation operation)
    {
        return operation is DockOperation.RootLeft or DockOperation.RootRight or DockOperation.RootTop or DockOperation.RootBottom;
    }

    private static IRootDock? ResolveRootDock(IDockable targetDockable)
    {
        return targetDockable switch
        {
            IRootDock root => root,
            _ => targetDockable.Factory?.FindRoot(targetDockable, _ => true)
                 ?? targetDockable.Owner?.Factory?.FindRoot(targetDockable, _ => true)
        };
    }

    /// <summary>
    /// The four window-edge guides. The dropped dockable gets a region of its own
    /// spanning the whole window edge, inserted at the ROOT level — so every
    /// existing pane gives way proportionally. That is the one thing the inner
    /// guides cannot express: they only subdivide the rectangle of the pane under
    /// the cursor.
    ///
    /// Returns false when this is not an edge operation, in which case the caller
    /// falls through to normal docking. When it returns true the operation was
    /// claimed, and <paramref name="result"/> says whether it can be / was done.
    /// </summary>
    private bool TryDockToRoot(IDockable sourceDockable, IDockable targetDockable, DragAction action, DockOperation operation, bool bExecute, out bool result)
    {
        result = false;

        if (!IsRootOperation(operation))
        {
            return false;
        }

        // A proportional dock is a container of panes, not a pane. Leave it to the
        // caller's normal path, which recurses into its children — each child then
        // arrives here on its own and gets its own edge region.
        if (sourceDockable is IProportionalDock)
        {
            return false;
        }

        // Copy has no docking meaning at all, and swapping with a region that does
        // not exist yet is meaningless too.
        if (action != DragAction.Move)
        {
            return true;
        }

        if (ResolveRootDock(targetDockable) is not { } rootDock)
        {
            return true;
        }

        result = DockToRootEdge(sourceDockable, rootDock, operation, bExecute);
        return true;
    }

    /// <summary>True when <paramref name="dockable"/> is, or lives under, <paramref name="branch"/>.</summary>
    private static bool IsInside(IDockable dockable, IDockable branch)
    {
        for (IDockable? node = dockable; node is not null; node = node.Owner)
        {
            if (ReferenceEquals(node, branch))
            {
                return true;
            }
        }

        return false;
    }

    private bool DockToRootEdge(IDockable sourceDockable, IRootDock rootDock, DockOperation operation, bool bExecute)
    {
        var factory = rootDock.Factory ?? sourceDockable.Factory ?? sourceDockable.Owner?.Factory;
        if (factory is null || rootDock.VisibleDockables is not { Count: > 0 })
        {
            return false;
        }

        // Refuse when nothing would be left behind. Moving the source out empties
        // its dock, which collapses; that empties the container, which collapses
        // too; and by the time the insert looks for a layout to sit beside there is
        // none — so the dockable ends up in a dock attached to nothing and vanishes
        // from the UI. A float window holding a single tool is the everyday case.
        // DockDockableIntoWindow already refuses the same situation.
        if (WouldEmptyRootLayout(sourceDockable, rootDock))
        {
            return false;
        }

        return DockToEdge(
            sourceDockable,
            factory,
            operation,
            dock => factory.SplitToRootEdge(rootDock, dock, operation),
            bExecute);
    }

    /// <summary>
    /// Turns the source into a dock and hands it to <paramref name="insert"/>, which
    /// knows where the finished dock goes. Written as a callback because the source
    /// side is identical no matter which container receives it.
    /// </summary>
    private bool DockToEdge(
        IDockable sourceDockable,
        IFactory factory,
        DockOperation operation,
        Func<IDock, bool> insert,
        bool bExecute)
    {
        switch (sourceDockable)
        {
            case ITool tool:
                {
                    if (tool.Owner is not IDock owner)
                    {
                        return false;
                    }

                    if (bExecute)
                    {
                        var edgeDock = CreateEdgeToolDock(factory, operation);
                        factory.MoveDockable(owner, edgeDock, tool, null);
                        insert(edgeDock);
                    }

                    return true;
                }
            case IDocument document:
                {
                    if (document.Owner is not IDock owner)
                    {
                        return false;
                    }

                    if (bExecute)
                    {
                        var edgeDock = CreateEdgeDocumentDock(factory, owner as IDocumentDock);
                        factory.MoveDockable(owner, edgeDock, document, null);
                        insert(edgeDock);
                    }

                    return true;
                }
            case IRootDock:
                {
                    return false;
                }
            case IDock sourceDock:
                {
                    return DockNodeToEdge(factory, sourceDock, operation, insert, bExecute);
                }
            default:
                {
                    return false;
                }
        }
    }

    /// <summary>
    /// Dragging a WHOLE dock onto a window edge (D18) — the dock node becomes the
    /// edge region itself, keeping its Id and Proportion.
    /// </summary>
    private static bool DockNodeToEdge(IFactory factory, IDock sourceDock, DockOperation operation, Func<IDock, bool> insert, bool bExecute)
    {
        if (sourceDock.Owner is not IDock || sourceDock.VisibleDockables is not { Count: > 0 })
        {
            return false;
        }

        if (bExecute)
        {
            DockDiagnostics.Log(() =>
                $"edge-node op={operation} source={DockDiagnostics.Describe(sourceDock)} "
                + $"from={DockDiagnostics.Describe(sourceDock.Owner)}");

            // The alignment describes which edge the dock now lives on, so it follows
            // the drop even though everything else about the node is preserved.
            if (sourceDock is IToolDock toolDock)
            {
                toolDock.Alignment = operation.ToAlignment();
            }

            factory.RemoveDockable(sourceDock, true);

            // Drop the old share. It was measured against different siblings, and a
            // dock coming out of a float window carries 1.0.
            sourceDock.Proportion = double.NaN;

            insert(sourceDock);
        }

        return true;
    }

    private static IToolDock CreateEdgeToolDock(IFactory factory, DockOperation operation)
    {
        var edgeDock = factory.CreateToolDock();
        edgeDock.Title = nameof(IToolDock);
        edgeDock.Alignment = operation.ToAlignment();
        edgeDock.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>());
        return edgeDock;
    }

    private static IDocumentDock CreateEdgeDocumentDock(IFactory factory, IDocumentDock? source)
    {
        var edgeDock = factory.CreateDocumentDock();
        edgeDock.Title = nameof(IDocumentDock);
        edgeDock.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>());

        if (source is not null)
        {
            // Carry the category over, not the instance identity — this is a new
            // dock instance, same as in SplitDocumentDockable.
            edgeDock.Kind = source.Kind;
            edgeDock.CanCreateDocument = source.CanCreateDocument;

            if (source is IDocumentDockContent sourceContent && edgeDock is IDocumentDockContent targetContent)
            {
                targetContent.DocumentTemplate = sourceContent.DocumentTemplate;
            }
        }

        return edgeDock;
    }

    private bool DockDockableIntoDock(IDockable sourceDockable, IDock targetDock, DragAction action, DockOperation operation, bool bExecute)
    {
        if (sourceDockable.Owner is not IDock sourceDockableOwner)
        {
            return false;
        }

        return DockDockableIntoDock(sourceDockable, sourceDockableOwner, targetDock, action, operation, bExecute);
    }

    private bool DockDockableIntoDock(IDockable sourceDockable, IDock sourceDockableOwner, IDock targetDock, DragAction action, DockOperation operation, bool bExecute)
    {
        return action switch
        {
            DragAction.Copy => false,
            DragAction.Move => DockDockable(sourceDockable, sourceDockableOwner, targetDock, operation, bExecute),
            DragAction.Link => SwapDockable(sourceDockable, sourceDockableOwner, targetDock, bExecute),
            _ => false
        };
    }

    private bool DockDockableIntoDockVisible(IDock sourceDock, IDock targetDock, DragAction action, DockOperation operation, bool bExecute)
    {
        var visible = sourceDock.VisibleDockables?.ToList();
        if (visible is null)
        {
            return true;
        }

        foreach (var dockable in visible)
        {
            if (DockDockableIntoDock(dockable, targetDock, action, operation, bExecute) == false)
            {
                return false;
            }
        }

        return true;
    }

    private bool DockDockIntoDock(IDock sourceDock, IDock targetDock, DragAction action, DockOperation operation, bool bExecute)
    {
        var visible = sourceDock.VisibleDockables?.ToList();
        if (visible is null)
        {
            return true;
        }

        if (visible.Count == 1)
        {
            var sourceDockable = visible.FirstOrDefault();
            if (sourceDockable is null || DockDockableIntoDock(sourceDockable, targetDock, action, operation, bExecute) == false)
            {
                return false;
            }
        }
        else
        {
            var sourceDockable = visible.FirstOrDefault();
            if (sourceDockable is null || DockDockableIntoDock(sourceDockable, targetDock, action, operation, bExecute) == false)
            {
                return false;
            }

            foreach (var dockable in visible.Skip(1))
            {
                var targetDockable = visible.FirstOrDefault();
                if (targetDockable is null || DockDockableIntoDockable(dockable, targetDockable, action, bExecute) == false)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private bool DockDockable(IDock sourceDock, IDockable targetDockable, IDock targetDock, DragAction action, DockOperation operation, bool bExecute)
    {
        return operation switch
        {
            // Fill means "become tabs of the target", and a dock cannot be a tab —
            // this is the one case where the contents really do have to be moved.
            DockOperation.Fill => DockDockableIntoDockVisible(sourceDock, targetDock, action, operation, bExecute),
            DockOperation.Window => DockDockableIntoWindow(sourceDock, targetDockable, bExecute),
            _ => SplitDockNode(sourceDock, targetDock, operation, bExecute)
        };
    }

    /// <summary>
    /// Directional drop of a WHOLE dock (D18): the dock node is re-parented rather
    /// than having its contents shovelled into a freshly created dock, so its Id,
    /// Alignment and Proportion survive the move.
    /// </summary>
    private static bool SplitDockNode(IDock sourceDock, IDock targetDock, DockOperation operation, bool bExecute)
    {
        if (ReferenceEquals(sourceDock, targetDock))
        {
            return false;
        }

        // Dropping a dock into its own subtree would detach that whole branch.
        if (IsSelfOrDescendant(sourceDock, targetDock))
        {
            return false;
        }

        if (sourceDock.Owner is not IDock || targetDock.Owner is not IDock)
        {
            return false;
        }

        if ((targetDock.Factory ?? sourceDock.Factory) is not { } factory)
        {
            return false;
        }

        if (bExecute)
        {
            // Detach FIRST: SplitToDock replaces the target inside its owner, and the
            // source must not still be listed under its old owner when that happens.
            factory.RemoveDockable(sourceDock, true);

            // Drop the old share. It was measured against different siblings, and a
            // dock coming out of a float window carries 1.0 (ProportionalStackPanel
            // assigns the whole container to a lone child), which would squeeze
            // everything else in the new container down to nothing. NaN means "give
            // me an equal share of what is left", which is what a newly arrived pane
            // should get.
            sourceDock.Proportion = double.NaN;

            factory.SplitToDock(targetDock, sourceDock, operation);
        }

        return true;
    }

    /// <summary>
    /// True when <paramref name="candidate"/> is <paramref name="dock"/> itself or
    /// sits underneath it.
    /// </summary>
    private static bool IsSelfOrDescendant(IDock dock, IDockable candidate)
    {
        for (IDockable? node = candidate; node is not null; node = node.Owner)
        {
            if (ReferenceEquals(node, dock))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual bool ValidateTool(ITool sourceTool, IDockable targetDockable, DragAction action, DockOperation operation, bool bExecute)
    {
        if (TryDockToRoot(sourceTool, targetDockable, action, operation, bExecute, out var rootResult))
        {
            return rootResult;
        }

        return targetDockable switch
        {
            IRootDock _ => DockDockableIntoWindow(sourceTool, targetDockable, bExecute),
            IToolDock toolDock => DockDockableIntoDock(sourceTool, toolDock, action, operation, bExecute),
            IDocumentDock documentDock => DockDockableIntoDock(sourceTool, documentDock, action, operation, bExecute),
            ITool tool => DockDockableIntoDockable(sourceTool, tool, action, bExecute),
            IDocument document => DockDockableIntoDockable(sourceTool, document, action, bExecute),
            _ => false
        };
    }

    /// <inheritdoc/>
    public bool ValidateDocument(IDocument sourceDocument, IDockable targetDockable, DragAction action, DockOperation operation, bool bExecute)
    {
        if (TryDockToRoot(sourceDocument, targetDockable, action, operation, bExecute, out var rootResult))
        {
            return rootResult;
        }

        return targetDockable switch
        {
            ITool => false,
            IRootDock _ => DockDockableIntoWindow(sourceDocument, targetDockable, bExecute),
            IDocumentDock documentDock => DockDockableIntoDock(sourceDocument, documentDock, action, operation, bExecute),
            IDocument document => DockDockableIntoDockable(sourceDocument, document, action, bExecute),
            _ => false
        };
    }

    /// <inheritdoc/>
    public bool ValidateDock(IDock sourceDock, IDockable targetDockable, DragAction action, DockOperation operation, bool bExecute)
    {
        if (TryDockToRoot(sourceDock, targetDockable, action, operation, bExecute, out var rootResult))
        {
            return rootResult;
        }

        return targetDockable switch
        {
            IRootDock _ => DockDockableIntoWindow(sourceDock, targetDockable, bExecute),
            IToolDock toolDock => sourceDock == toolDock
                ? ValidateSelfDrop(operation)
                : DockDockable(sourceDock, targetDockable, toolDock, action, operation, bExecute),
            IDocumentDock documentDock => sourceDock == documentDock
                ? ValidateSelfDrop(operation)
                : DockDockable(sourceDock, targetDockable, documentDock, action, operation, bExecute),
            _ => false
        };
    }

    /// <summary>
    /// Dropping a dock back onto itself is how a drag gets cancelled, so the whole
    /// self-drop must not be refused: with no guide over the source there is nowhere
    /// to let go, and the user is forced to commit the drag somewhere else.
    ///
    /// Only Fill validates: the centre guide appears over the source and accepts the
    /// drop. A directional operation on oneself has no meaning (a dock cannot be
    /// split against itself), so those stay refused.
    ///
    /// Nothing is executed either way — the dockable is already exactly where the
    /// drop would put it, so "success" here means "leave the layout alone".
    /// </summary>
    private static bool ValidateSelfDrop(DockOperation operation)
        => operation == DockOperation.Fill;

    private bool ValidateProportionalDock(IProportionalDock sourceDock, IDockable targetDockable, DragAction action, DockOperation operation, bool bExecute)
    {
        if (sourceDock.VisibleDockables == null ||
            sourceDock.VisibleDockables.Count == 0)
            return false;

        bool all = true;
        for (int i = sourceDock.VisibleDockables.Count - 1; i >= 0; --i)
        {
            var dockable = sourceDock.VisibleDockables[i];
            if (dockable is not IDock dock)
                continue;

            all &= ValidateDockable(dock, targetDockable, action, operation, bExecute);
        }

        return all;
    }

    /// <inheritdoc/>
    public bool ValidateDockable(IDockable sourceDockable, IDockable targetDockable, DragAction action, DockOperation operation, bool bExecute)
    {
        if (TryDockToRoot(sourceDockable, targetDockable, action, operation, bExecute, out var rootResult))
        {
            return rootResult;
        }

        return sourceDockable switch
        {
            IToolDock toolDock => ValidateDock(toolDock, targetDockable, action, operation, bExecute),
            IDocumentDock documentDock => ValidateDock(documentDock, targetDockable, action, operation, bExecute),
            ITool tool => ValidateTool(tool, targetDockable, action, operation, bExecute),
            IDocument document => ValidateDocument(document, targetDockable, action, operation, bExecute),
            IProportionalDock proportionalDock => ValidateProportionalDock(proportionalDock, targetDockable, action, operation, bExecute),
            _ => false
        };
    }
}

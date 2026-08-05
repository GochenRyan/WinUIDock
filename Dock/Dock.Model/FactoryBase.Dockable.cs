using Dock.Model.Controls;
using Dock.Model.Core;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dock.Model;

/// <summary>
/// Factory base class.
/// </summary>
public abstract partial class FactoryBase
{
    /// <inheritdoc/>
    public virtual void AddDockable(IDock dock, IDockable dockable)
    {
        InitDockable(dockable, dock);
        dock.VisibleDockables ??= new ObservableCollection<IDockable>(CreateList<IDockable>());
        AddVisibleDockable(dock, dockable);
        OnDockableAdded(dockable);
    }

    /// <inheritdoc/>
    public virtual void InsertDockable(IDock dock, IDockable dockable, int index)
    {
        if (index >= 0)
        {
            InitDockable(dockable, dock);
            dock.VisibleDockables ??= new ObservableCollection<IDockable>(CreateList<IDockable>());
            InsertVisibleDockable(dock, index, dockable);
            OnDockableAdded(dockable);
        }
    }

    /// <inheritdoc/>
    public virtual void RemoveDockable(IDockable dockable, bool collapse)
    {
        // to correctly remove a pinned dockable, it needs to be unpinned
        UnpinDockable(dockable);

        if (dockable.Owner is not IDock dock || dock.VisibleDockables is null)
        {
            return;
        }

        var index = dock.VisibleDockables.IndexOf(dockable);
        if (index < 0)
        {
            return;
        }

        RemoveVisibleDockable(dock, dockable);
        OnDockableRemoved(dockable);

        var indexActiveDockable = index > 0 ? index - 1 : 0;
        if (dock.VisibleDockables.Count > 0)
        {
            var nextActiveDockable = dock.VisibleDockables[indexActiveDockable];
            dock.ActiveDockable = nextActiveDockable is not IProportionalDockSplitter ? nextActiveDockable : null;
        }
        else
        {
            dock.ActiveDockable = null;
        }

        NormalizeSplitters(dock);

        if (collapse)
        {
            CollapseDock(dock);
        }
    }

    /// <summary>
    /// Drops splitters that no longer separate anything: a leading one, a trailing
    /// one, or two in a row.
    ///
    /// The old cleanup only fired when the child count fell to 1 or 2, so removing a
    /// child from a container that still had three or more left an orphan behind —
    /// a stray drag handle and a gap at the edge of the pane. Rare while only single
    /// tools moved (they leave their dock in place); routine once whole docks are
    /// relocated.
    /// </summary>
    private void NormalizeSplitters(IDock dock)
    {
        if (dock.VisibleDockables is not { } dockables)
        {
            return;
        }

        // Back to front: removing at i never shifts anything still to be examined.
        for (var i = dockables.Count - 1; i >= 0; i--)
        {
            if (dockables[i] is not IProportionalDockSplitter splitter)
            {
                continue;
            }

            var orphaned =
                i == 0
                || i == dockables.Count - 1
                || dockables[i - 1] is IProportionalDockSplitter
                || dockables[i + 1] is IProportionalDockSplitter;

            if (orphaned)
            {
                RemoveDockable(splitter, false);
            }
        }
    }

    /// <inheritdoc/>
    public virtual void MoveDockable(IDock dock, IDockable sourceDockable, IDockable targetDockable)
    {
        if (dock.VisibleDockables is null)
        {
            return;
        }

        var sourceIndex = dock.VisibleDockables.IndexOf(sourceDockable);
        var targetIndex = dock.VisibleDockables.IndexOf(targetDockable);

        if (sourceIndex >= 0 && targetIndex >= 0 && sourceIndex != targetIndex)
        {
            RemoveVisibleDockableAt(dock, sourceIndex);
            OnDockableRemoved(sourceDockable);
            InsertVisibleDockable(dock, targetIndex, sourceDockable);
            OnDockableAdded(sourceDockable);
            OnDockableMoved(sourceDockable);
            dock.ActiveDockable = sourceDockable;
        }
    }

    /// <inheritdoc/>
    public virtual void MoveDockable(IDock sourceDock, IDock targetDock, IDockable sourceDockable, IDockable? targetDockable)
    {
        UnpinDockable(sourceDockable);

        if (targetDock.VisibleDockables is null)
        {
            targetDock.VisibleDockables = new ObservableCollection<IDockable>(CreateList<IDockable>());
            if (targetDock.VisibleDockables is null)
            {
                return;
            }
        }

        var isSameOwner = sourceDock == targetDock;

        var targetIndex = 0;

        if (sourceDock.VisibleDockables is not null && targetDock.VisibleDockables is not null && targetDock.VisibleDockables.Count > 0)
        {
            if (isSameOwner)
            {
                var sourceIndex = sourceDock.VisibleDockables.IndexOf(sourceDockable);

                if (targetDockable is not null)
                {
                    targetIndex = targetDock.VisibleDockables.IndexOf(targetDockable);
                }
                else
                {
                    targetIndex = targetDock.VisibleDockables.Count - 1;
                }

                if (sourceIndex == targetIndex)
                {
                    return;
                }
            }
            else
            {
                if (targetDockable is not null)
                {
                    targetIndex = targetDock.VisibleDockables.IndexOf(targetDockable);
                    if (targetIndex >= 0)
                    {
                        targetIndex += 1;
                    }
                    else
                    {
                        targetIndex = targetDock.VisibleDockables.Count - 1;
                    }
                }
                else
                {
                    targetIndex = targetDock.VisibleDockables.Count - 1;
                }
            }
        }

        if (sourceDock.VisibleDockables is not null && targetDock.VisibleDockables is not null)
        {
            if (isSameOwner)
            {
                var sourceIndex = sourceDock.VisibleDockables.IndexOf(sourceDockable);
                if (sourceIndex < targetIndex)
                {
                    InsertVisibleDockable(targetDock, targetIndex + 1, sourceDockable);
                    OnDockableAdded(sourceDockable);
                    RemoveVisibleDockableAt(targetDock, sourceIndex);
                    OnDockableRemoved(sourceDockable);
                    OnDockableMoved(sourceDockable);
                }
                else
                {
                    var removeIndex = sourceIndex + 1;
                    if (targetDock.VisibleDockables.Count + 1 > removeIndex)
                    {
                        InsertVisibleDockable(targetDock, targetIndex, sourceDockable);
                        OnDockableAdded(sourceDockable);
                        RemoveVisibleDockableAt(targetDock, removeIndex);
                        OnDockableRemoved(sourceDockable);
                        OnDockableMoved(sourceDockable);
                    }
                }
            }
            else
            {
                RemoveDockable(sourceDockable, true);
                InsertVisibleDockable(targetDock, targetIndex, sourceDockable);
                OnDockableAdded(sourceDockable);
                OnDockableMoved(sourceDockable);
                InitDockable(sourceDockable, targetDock);
                targetDock.ActiveDockable = sourceDockable;
            }
        }
    }

    /// <inheritdoc/>
    public virtual void SwapDockable(IDock dock, IDockable sourceDockable, IDockable targetDockable)
    {
        if (dock.VisibleDockables is null)
        {
            return;
        }

        var sourceIndex = dock.VisibleDockables.IndexOf(sourceDockable);
        var targetIndex = dock.VisibleDockables.IndexOf(targetDockable);

        if (sourceIndex >= 0 && targetIndex >= 0 && sourceIndex != targetIndex)
        {
            var originalSourceDockable = dock.VisibleDockables[sourceIndex];
            var originalTargetDockable = dock.VisibleDockables[targetIndex];

            dock.VisibleDockables[targetIndex] = originalSourceDockable;
            OnDockableRemoved(originalTargetDockable);
            OnDockableAdded(originalSourceDockable);
            dock.VisibleDockables[sourceIndex] = originalTargetDockable;
            OnDockableAdded(originalTargetDockable);
            OnDockableSwapped(originalSourceDockable);
            OnDockableSwapped(originalTargetDockable);
            dock.ActiveDockable = originalTargetDockable;
        }
    }

    /// <inheritdoc/>
    public virtual void SwapDockable(IDock sourceDock, IDock targetDock, IDockable sourceDockable, IDockable targetDockable)
    {
        if (sourceDock.VisibleDockables is null || targetDock.VisibleDockables is null)
        {
            return;
        }

        var sourceIndex = sourceDock.VisibleDockables.IndexOf(sourceDockable);
        var targetIndex = targetDock.VisibleDockables.IndexOf(targetDockable);

        if (sourceIndex >= 0 && targetIndex >= 0)
        {
            var originalSourceDockable = sourceDock.VisibleDockables[sourceIndex];
            var originalTargetDockable = targetDock.VisibleDockables[targetIndex];
            sourceDock.VisibleDockables[sourceIndex] = originalTargetDockable;
            targetDock.VisibleDockables[targetIndex] = originalSourceDockable;

            InitDockable(originalSourceDockable, targetDock);
            InitDockable(originalTargetDockable, sourceDock);

            OnDockableSwapped(originalTargetDockable);
            OnDockableSwapped(originalSourceDockable);

            sourceDock.ActiveDockable = originalTargetDockable;
            targetDock.ActiveDockable = originalSourceDockable;
        }
    }

    /// <inheritdoc/>
    public bool IsDockablePinned(IDockable dockable, IRootDock? rootDock = null)
    {
        if (rootDock == null)
        {
            rootDock = FindRoot(dockable);

            if (rootDock == null)
            {
                return false;
            }
        }

        if (rootDock.LeftPinnedDockables is not null)
        {
            if (rootDock.LeftPinnedDockables.Contains(dockable))
            {
                return true;
            }
        }

        if (rootDock.RightPinnedDockables is not null)
        {
            if (rootDock.RightPinnedDockables.Contains(dockable))
            {
                return true;
            }
        }

        if (rootDock.TopPinnedDockables is not null)
        {
            if (rootDock.TopPinnedDockables.Contains(dockable))
            {
                return true;
            }
        }

        if (rootDock.BottomPinnedDockables is not null)
        {
            if (rootDock.BottomPinnedDockables.Contains(dockable))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public void HidePreviewingDockables(IRootDock rootDock)
    {
        DockDiagnostics.Log(() =>
            $"HidePreviewingDockables: pinnedDock={DockDiagnostics.Describe(rootDock.PinnedDock)} "
            + $"count={rootDock.PinnedDock?.VisibleDockables?.Count ?? -1}");

        if (rootDock.PinnedDock == null)
            return;

        if (rootDock.PinnedDock.VisibleDockables != null)
        {
            foreach (var dockable in rootDock.PinnedDock.VisibleDockables)
            {
                dockable.Owner = dockable.OriginalOwner;
                dockable.OriginalOwner = null;
            }
            RemoveAllVisibleDockables(rootDock.PinnedDock);
        }
    }

    /// <inheritdoc/>
    public void PreviewPinnedDockable(IDockable dockable)
    {
        DockDiagnostics.Log(() =>
            $"PreviewPinnedDockable: {DockDiagnostics.Describe(dockable)} owner={DockDiagnostics.Describe(dockable.Owner)}");

        var rootDock = FindRoot(dockable, _ => true);
        if (rootDock is null)
        {
            return;
        }

        HidePreviewingDockables(rootDock);

        var alignment = (dockable.Owner as IToolDock)?.Alignment ?? Alignment.Unset;

        if (rootDock.PinnedDock == null)
        {
            rootDock.PinnedDock = CreateToolDock();
            InitDockable(rootDock.PinnedDock, rootDock);
        }
        rootDock.PinnedDock.Alignment = alignment;

        Debug.Assert(rootDock.PinnedDock != null);

        RemoveAllVisibleDockables(rootDock.PinnedDock);

        dockable.OriginalOwner = dockable.Owner;
        AddVisibleDockable(rootDock.PinnedDock, dockable);
    }

    /// <inheritdoc/>
    public virtual void PinDockable(IDockable dockable)
    {
        DockDiagnostics.Log(() =>
            $"PinDockable: {DockDiagnostics.Describe(dockable)} owner={DockDiagnostics.Describe(dockable.Owner)} "
            + $"originalOwner={DockDiagnostics.Describe(dockable.OriginalOwner)}");

        switch (dockable.Owner)
        {
            case IToolDock toolDock:
                {
                    var rootDock = FindRoot(dockable, _ => true);
                    if (rootDock is null)
                    {
                        DockDiagnostics.Log(() => "PinDockable: no root — BAILING");
                        return;
                    }

                    var isVisible = false;

                    if (toolDock.VisibleDockables is not null)
                    {
                        isVisible = toolDock.VisibleDockables.Contains(dockable);
                    }

                    var isPinned = IsDockablePinned(dockable, rootDock);

                    var originalToolDock = dockable.OriginalOwner as IToolDock;

                    var alignment = originalToolDock?.Alignment ?? toolDock.Alignment;

                    DockDiagnostics.Log(() =>
                        $"PinDockable: isVisible={isVisible} isPinned={isPinned} alignment={alignment} "
                        + $"-> branch={(isVisible && !isPinned ? "PIN" : isPinned ? "UNPIN" : "INVALID")}");

                    if (isVisible && !isPinned)
                    {
                        // Pin dockable.

                        switch (alignment)
                        {
                            case Alignment.Unset:
                            case Alignment.Left:
                                {
                                    rootDock.LeftPinnedDockables ??= new ObservableCollection<IDockable>(CreateList<IDockable>());
                                    break;
                                }
                            case Alignment.Right:
                                {
                                    rootDock.RightPinnedDockables ??= new ObservableCollection<IDockable>(CreateList<IDockable>());
                                    break;
                                }
                            case Alignment.Top:
                                {
                                    rootDock.TopPinnedDockables ??= new ObservableCollection<IDockable>(CreateList<IDockable>());
                                    break;
                                }
                            case Alignment.Bottom:
                                {
                                    rootDock.BottomPinnedDockables ??= new ObservableCollection<IDockable>(CreateList<IDockable>());
                                    break;
                                }
                        }

                        if (toolDock.VisibleDockables is not null)
                        {
                            RemoveVisibleDockable(toolDock, dockable);
                            OnDockableRemoved(dockable);
                        }

                        switch (alignment)
                        {
                            case Alignment.Unset:
                            case Alignment.Left:
                                {
                                    if (rootDock.LeftPinnedDockables is not null)
                                    {
                                        rootDock.LeftPinnedDockables.Add(dockable);
                                        OnDockablePinned(dockable);
                                    }

                                    break;
                                }
                            case Alignment.Right:
                                {
                                    if (rootDock.RightPinnedDockables is not null)
                                    {
                                        rootDock.RightPinnedDockables.Add(dockable);
                                        OnDockablePinned(dockable);
                                    }

                                    break;
                                }
                            case Alignment.Top:
                                {
                                    if (rootDock.TopPinnedDockables is not null)
                                    {
                                        rootDock.TopPinnedDockables.Add(dockable);
                                        OnDockablePinned(dockable);
                                    }

                                    break;
                                }
                            case Alignment.Bottom:
                                {
                                    if (rootDock.BottomPinnedDockables is not null)
                                    {
                                        rootDock.BottomPinnedDockables.Add(dockable);
                                        OnDockablePinned(dockable);
                                    }

                                    break;
                                }
                        }

                        // TODO: Handle ActiveDockable state.
                        // TODO: Handle IsExpanded property of IToolDock.
                        // TODO: Handle AutoHide property of IToolDock.
                    }
                    else if (isPinned)
                    {
                        // Unpin dockable.

                        toolDock.VisibleDockables ??= new ObservableCollection<IDockable>(CreateList<IDockable>());

                        switch (alignment)
                        {
                            case Alignment.Unset:
                            case Alignment.Left:
                                {
                                    if (rootDock.LeftPinnedDockables is not null)
                                    {
                                        rootDock.LeftPinnedDockables.Remove(dockable);
                                        OnDockableUnpinned(dockable);
                                    }

                                    break;
                                }
                            case Alignment.Right:
                                {
                                    if (rootDock.RightPinnedDockables is not null)
                                    {
                                        rootDock.RightPinnedDockables.Remove(dockable);
                                        OnDockableUnpinned(dockable);
                                    }

                                    break;
                                }
                            case Alignment.Top:
                                {
                                    if (rootDock.TopPinnedDockables is not null)
                                    {
                                        rootDock.TopPinnedDockables.Remove(dockable);
                                        OnDockableUnpinned(dockable);
                                    }

                                    break;
                                }
                            case Alignment.Bottom:
                                {
                                    if (rootDock.BottomPinnedDockables is not null)
                                    {
                                        rootDock.BottomPinnedDockables.Remove(dockable);
                                        OnDockableUnpinned(dockable);
                                    }

                                    break;
                                }
                        }

                        if (!isVisible)
                        {
                            AddVisibleDockable(toolDock, dockable);
                        }
                        else
                        {
                            Debug.Assert(dockable.OriginalOwner is IDock);
                            var originalOwner = (IDock)dockable.OriginalOwner;
                            HidePreviewingDockables(rootDock);
                            AddVisibleDockable(originalOwner, dockable);
                        }

                        OnDockableAdded(dockable);

                        // TODO: Handle ActiveDockable state.
                        // TODO: Handle IsExpanded property of IToolDock.
                        // TODO: Handle AutoHide property of IToolDock.
                    }
                    else
                    {
                        // TODO: Handle invalid state.
                    }

                    break;
                }
        }
    }

    /// <inheritdoc/>
    public virtual void UnpinDockable(IDockable dockable)
    {
        if (IsDockablePinned(dockable))
        {
            PinDockable(dockable);
        }
    }

    /// <inheritdoc/>
    public virtual void FloatDockable(IDockable dockable)
    {
        if (dockable.Owner is not IDock dock)
        {
            return;
        }

        // Already floating alone: re-floating would rebuild the window from the
        // CONTENT bounds and lose one chrome per repeat. No-op, same rule as
        // DockManager's window path.
        var sourceRoot = FindRoot(dockable, _ => true);
        if (sourceRoot?.Window is not null
            && !Find(sourceRoot, d => d is not IDock and not IProportionalDockSplitter).Skip(1).Any())
        {
            return;
        }

        UnpinDockable(dockable);

        dock.GetVisibleBounds(out var ownerX, out var ownerY, out var ownerWidth, out var ownerHeight);
        dockable.GetVisibleBounds(out var dockableX, out var dockableY, out var dockableWidth, out var dockableHeight);

        // Float IN PLACE: the window appears where the panel currently sits on
        // screen (bounds origins are recorded in screen space by the view). The
        // last pointer position is only a fallback — preferring it made the
        // "Float" command drop the window wherever the mouse happened to be
        // when the chrome menu was opened, which reads as random.
        var x = dockableX;
        var y = dockableY;

        if (double.IsNaN(x) || double.IsNaN(y))
        {
            x = ownerX;
            y = ownerY;
        }

        if (double.IsNaN(x) || double.IsNaN(y))
        {
            dock.GetPointerScreenPosition(out var dockPointerScreenX, out var dockPointerScreenY);
            dockable.GetPointerScreenPosition(out var dockablePointerScreenX, out var dockablePointerScreenY);

            x = !double.IsNaN(dockablePointerScreenX) ? dockablePointerScreenX : dockPointerScreenX;
            y = !double.IsNaN(dockablePointerScreenY) ? dockablePointerScreenY : dockPointerScreenY;
        }

        if (double.IsNaN(x))
        {
            x = 0;
        }
        if (double.IsNaN(y))
        {
            y = 0;
        }
        if (double.IsNaN(dockableWidth))
        {
            dockableWidth = double.IsNaN(ownerWidth) ? 300 : ownerWidth;
        }
        if (double.IsNaN(dockableHeight))
        {
            dockableHeight = double.IsNaN(ownerHeight) ? 400 : ownerHeight;
        }

        // Torn out of a SHARED float window: inherit that window's size instead
        // of the chrome-less content bounds.
        if (sourceRoot?.Window is { } sourceWindow
            && !double.IsNaN(sourceWindow.WindowWidth) && sourceWindow.WindowWidth > 0
            && !double.IsNaN(sourceWindow.WindowHeight) && sourceWindow.WindowHeight > 0)
        {
            dockableWidth = sourceWindow.WindowWidth;
            dockableHeight = sourceWindow.WindowHeight;
        }

        SplitToWindow(dock, dockable, x, y, dockableWidth, dockableHeight);
    }

    /// <inheritdoc/>
    public virtual void CloseDockable(IDockable dockable)
    {
        if (dockable.CanClose && dockable.OnClose())
        {
            // Remember the spot so RestoreDockable can put it back. Recorded here
            // rather than in RemoveDockable because that runs on drag/move paths too,
            // where the dockable is not "closed" and has no spot to return to.
            if (dockable.Owner is IDock closeOwner && closeOwner.VisibleDockables is { } siblings)
            {
                var closeIndex = siblings.IndexOf(dockable);
                if (closeIndex >= 0)
                {
                    dockable.RestoreOwner = closeOwner;
                    dockable.RestoreIndex = closeIndex;
                }
            }

            RemoveDockable(dockable, true);
            OnDockableClosed(dockable);
        }
    }

    /// <inheritdoc/>
    public virtual bool RestoreDockable(IDockable dockable)
    {
        if (dockable.RestoreOwner is not IDock owner)
        {
            return false;
        }

        // The owner may itself have been collapsed away when this dockable closed —
        // put it back first, otherwise there is nothing to insert into.
        if (!IsInLayout(owner))
        {
            if (!RestoreDockable(owner))
            {
                return false;
            }
        }

        if (FindRoot(owner, _ => true) is { } root)
        {
            root.HiddenDockables?.Remove(dockable);
        }

        owner.VisibleDockables ??= new ObservableCollection<IDockable>(CreateList<IDockable>());

        // Siblings may have come and gone since, so treat the index as a hint.
        var index = dockable.RestoreIndex;
        if (index < 0 || index > owner.VisibleDockables.Count)
        {
            index = owner.VisibleDockables.Count;
        }

        InsertDockable(owner, dockable, index);

        // CollapseDock took the neighbouring splitters with it; put them back so the
        // restored dock gets its share of the space.
        EnsureSplittersAround(owner, index);

        dockable.RestoreOwner = null;
        dockable.RestoreIndex = -1;

        SetActiveDockable(dockable);
        return true;
    }

    /// <summary>
    /// True when the dockable is currently reachable in its owner's visible tree.
    /// A parked (hidden) dock keeps its Owner link, so an Owner check alone is not enough.
    /// </summary>
    private static bool IsInLayout(IDockable dockable)
        => dockable.Owner is IDock owner && owner.VisibleDockables?.Contains(dockable) == true;

    /// <summary>
    /// Puts a splitter on each side of the item just re-inserted at <paramref name="index"/>,
    /// where one is missing. Only the two neighbouring gaps are touched — rebuilding
    /// every splitter would work too, but would discard the ids and any customization
    /// the host gave the existing ones.
    /// </summary>
    private void EnsureSplittersAround(IDock dock, int index)
    {
        if (dock is not IProportionalDock || dock.VisibleDockables is not { } dockables)
        {
            return;
        }

        if (index < 0 || index >= dockables.Count)
        {
            return;
        }

        // Trailing gap first: inserting there does not shift the item's own index.
        if (index + 1 < dockables.Count && dockables[index + 1] is not IProportionalDockSplitter)
        {
            InsertSplitter(dock, index + 1);
        }

        if (index > 0 && dockables[index - 1] is not IProportionalDockSplitter)
        {
            InsertSplitter(dock, index);
        }
    }

    private void InsertSplitter(IDock dock, int index)
    {
        var splitter = CreateProportionalDockSplitter();
        InitDockable(splitter, dock);
        dock.VisibleDockables?.Insert(index, splitter);
    }

    private void CloseDockablesRange(IDock dock, int start, int end, IDockable? excluding = null)
    {
        if (dock.VisibleDockables is null)
        {
            return;
        }

        for (var i = end; i >= start; --i)
        {
            if (excluding == null || dock.VisibleDockables[i] != excluding)
            {
                CloseDockable(dock.VisibleDockables[i]);
            }
        }

        UpdateActiveAfterDeleted(dock);
    }

    /// <inheritdoc/>
    public virtual void CloseOtherDockables(IDockable dockable)
    {
        if (dockable.Owner is not IDock dock || dock.VisibleDockables is null)
        {
            return;
        }

        CloseDockablesRange(dock, 0, dock.VisibleDockables.Count - 1, dockable);
    }

    /// <inheritdoc/>
    public virtual void CloseAllDockables(IDockable dockable)
    {
        if (dockable.Owner is not IDock dock || dock.VisibleDockables is null)
        {
            return;
        }

        CloseDockablesRange(dock, 0, dock.VisibleDockables.Count - 1);
    }

    /// <inheritdoc/>
    public virtual void CloseLeftDockables(IDockable dockable)
    {
        if (dockable.Owner is not IDock dock || dock.VisibleDockables is null)
        {
            return;
        }

        int indexOf = dock.VisibleDockables.IndexOf(dockable);
        if (indexOf == -1)
        {
            return;
        }

        CloseDockablesRange(dock, 0, indexOf - 1);
    }

    /// <inheritdoc/>
    public virtual void CloseRightDockables(IDockable dockable)
    {
        if (dockable.Owner is not IDock dock || dock.VisibleDockables is null)
        {
            return;
        }

        int indexOf = dock.VisibleDockables.IndexOf(dockable);
        if (indexOf == -1)
        {
            return;
        }

        CloseDockablesRange(dock, indexOf + 1, dock.VisibleDockables.Count - 1);
    }

    /// <summary>
    /// Adds the dockable to the visible dockables list of the dock.
    /// </summary>
    protected void AddVisibleDockable(IDock dock, IDockable dockable)
    {
        if (dock.VisibleDockables == null)
        {
            dock.VisibleDockables = new ObservableCollection<IDockable>(CreateList<IDockable>());
        }

        dock.VisibleDockables.Add(dockable);

        if (dock.VisibleDockables.Count == 1)
        {
            dock.ActiveDockable = dockable;
        }
        UpdateIsEmpty(dock);
    }

    /// <summary>
    /// Inserts the dockable to the visible dockables list of the dock at the specified index.
    /// </summary>
    protected void InsertVisibleDockable(IDock dock, int index, IDockable dockable)
    {
        if (dock.VisibleDockables == null)
        {
            dock.VisibleDockables = new ObservableCollection<IDockable>(CreateList<IDockable>());
        }

        dock.VisibleDockables.Insert(index, dockable);

        if (dock.VisibleDockables.Count == 1)
        {
            dock.ActiveDockable = dockable;
        }
        UpdateIsEmpty(dock);
    }

    /// <summary>
    /// Removes the dockable from the visible dockables list of the dock.
    /// </summary>
    protected void RemoveVisibleDockable(IDock dock, IDockable dockable)
    {
        if (dock.VisibleDockables != null)
        {
            dock.VisibleDockables.Remove(dockable);
            UpdateActiveAfterDeleted(dock);
            UpdateIsEmpty(dock);
        }
    }

    /// <summary>
    /// Removes all visible dockable of the dock.
    /// </summary>
    protected void RemoveAllVisibleDockables(IDock dock)
    {
        if (dock.VisibleDockables != null)
        {
            if (dock.VisibleDockables.Count > 0)
            {
                dock.VisibleDockables.Clear();
                dock.ActiveDockable = null;
                UpdateIsEmpty(dock);
            }
        }
    }

    /// <summary>
    /// Removes the dockable at the specified index from the visible dockables list of the dock.
    /// </summary>
    protected void RemoveVisibleDockableAt(IDock dock, int index)
    {
        if (dock.VisibleDockables != null)
        {
            dock.VisibleDockables.RemoveAt(index);
            UpdateActiveAfterDeleted(dock);
            UpdateIsEmpty(dock);
        }
    }

    private void UpdateIsEmpty(IDock dock)
    {
        bool oldIsEmpty = dock.IsEmpty;

        var newIsEmpty = dock.VisibleDockables == null
                         || dock.VisibleDockables?.Count == 0
                         || dock.VisibleDockables!.All(x => x is IDock { IsEmpty: true } or IProportionalDockSplitter);

        if (oldIsEmpty != newIsEmpty)
        {
            dock.IsEmpty = newIsEmpty;
            if (dock.Owner is IDock parent)
                UpdateIsEmpty(parent);
        }

        UpdateOpenedDockablesCount(dock);
    }

    private void UpdateOpenedDockablesCount(IDockable dockable)
    {
        switch (dockable)
        {
            case IProportionalDock proportionalDock:
                proportionalDock.OpenedDockablesCount = proportionalDock.VisibleDockables?.Sum(x => (x as IDock)?.OpenedDockablesCount ?? 0) ?? 0;
                break;
            case IRootDock rootDock:
                rootDock.OpenedDockablesCount = rootDock.VisibleDockables?.Sum(x => (x as IDock)?.OpenedDockablesCount ?? 0) ?? 0;
                break;
            case IDock dock:
                dock.OpenedDockablesCount = 1;
                break;
            default:
                break;
        }

        if (dockable.Owner != null)
            UpdateOpenedDockablesCount(dockable.Owner);
    }

    private void UpdateActiveAfterDeleted(IDock dock)
    {
        var cnt = dock.VisibleDockables.Count;
        if (cnt == 0)
        {
            dock.ActiveDockable = null;
        }
        else if (dock.ActiveDockable == null || !dock.VisibleDockables.Contains(dock.ActiveDockable))
        {
            dock.ActiveDockable = dock.VisibleDockables[cnt - 1];
        }
    }
}

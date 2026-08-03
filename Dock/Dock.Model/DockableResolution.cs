using Dock.Model.Controls;
using Dock.Model.Core;
using System.Collections.ObjectModel;

namespace Dock.Model;

/// <summary>
/// Applies a <see cref="DockableResolver"/> across a freshly deserialized layout.
/// Kept separate from the serializer: turning JSON into objects and re-associating
/// those objects with the running application are different jobs.
/// </summary>
public static class DockableResolution
{
    /// <summary>
    /// Walks the tree depth-first, offering every dockable to the resolver and
    /// substituting or dropping nodes according to what it returns.
    /// </summary>
    /// <returns>The resolved root, or null if the resolver dropped it.</returns>
    public static IDockable? Apply(IDockable? root, DockableResolver? resolver)
    {
        if (root is null || resolver is null)
        {
            return root;
        }

        var resolved = new Dictionary<IDockable, IDockable?>(ReferenceEqualityComparer.Instance);
        return Resolve(root, resolver, resolved);
    }

    /// <summary>
    /// Moves the structural state of a deserialized dock onto the live instance a
    /// resolver is about to adopt: children, selection, proportion and — for a root
    /// dock — its pinned, hidden and window collections.
    ///
    /// Call this from the resolver just before returning the live object, so the
    /// layout comes from the file while the object identity stays the host's:
    /// <code>
    /// if (deserialized is IDock loaded &amp;&amp; deserialized.Id == "Docs")
    /// {
    ///     DockableResolution.TransplantStructure(loaded, Docs);
    ///     return Docs;
    /// }
    /// </code>
    /// Resolution continues into the transplanted children afterwards, and the
    /// selection references are retargeted automatically.
    /// </summary>
    /// <param name="from">The deserialized dock being replaced.</param>
    /// <param name="to">The live dock being adopted.</param>
    public static void TransplantStructure(IDock from, IDock to)
    {
        if (from is null || to is null || ReferenceEquals(from, to))
        {
            return;
        }

        to.VisibleDockables = from.VisibleDockables;
        to.ActiveDockable = from.ActiveDockable;
        to.DefaultDockable = from.DefaultDockable;
        to.FocusedDockable = from.FocusedDockable;
        to.Proportion = from.Proportion;
        to.Dock = from.Dock;
        to.IsCollapsable = from.IsCollapsable;

        if (from is IRootDock fromRoot && to is IRootDock toRoot)
        {
            toRoot.LeftPinnedDockables = fromRoot.LeftPinnedDockables;
            toRoot.RightPinnedDockables = fromRoot.RightPinnedDockables;
            toRoot.TopPinnedDockables = fromRoot.TopPinnedDockables;
            toRoot.BottomPinnedDockables = fromRoot.BottomPinnedDockables;
            toRoot.HiddenDockables = fromRoot.HiddenDockables;
            toRoot.Windows = fromRoot.Windows;
        }

        if (from is IToolDock fromTool && to is IToolDock toTool)
        {
            toTool.Alignment = fromTool.Alignment;
            toTool.IsExpanded = fromTool.IsExpanded;
            toTool.AutoHide = fromTool.AutoHide;
            toTool.GripMode = fromTool.GripMode;
        }

        if (from is IProportionalDock fromProportional && to is IProportionalDock toProportional)
        {
            toProportional.Orientation = fromProportional.Orientation;
        }

        if (from is IDocumentDock fromDocument && to is IDocumentDock toDocument)
        {
            toDocument.CanCreateDocument = fromDocument.CanCreateDocument;
        }
    }

    private static IDockable? Resolve(
        IDockable node,
        DockableResolver resolver,
        Dictionary<IDockable, IDockable?> resolved)
    {
        // The same instance can be reachable twice (a dock's ActiveDockable is also
        // one of its VisibleDockables), and the resolver must see each node once.
        if (resolved.TryGetValue(node, out var cached))
        {
            return cached;
        }

        var adopted = resolver(node);
        resolved[node] = adopted;

        if (adopted is not IDock dock)
        {
            return adopted;
        }

        ResolveCollection(dock.VisibleDockables, resolver, resolved);

        if (dock is IRootDock rootDock)
        {
            ResolveCollection(rootDock.LeftPinnedDockables, resolver, resolved);
            ResolveCollection(rootDock.RightPinnedDockables, resolver, resolved);
            ResolveCollection(rootDock.TopPinnedDockables, resolver, resolved);
            ResolveCollection(rootDock.BottomPinnedDockables, resolver, resolved);
            ResolveCollection(rootDock.HiddenDockables, resolver, resolved);
            ResolveWindows(rootDock, resolver, resolved);
        }

        // Cross-references point at pre-resolution instances; retarget them, and
        // clear the ones whose target was dropped.
        dock.ActiveDockable = Remap(dock.ActiveDockable, resolved);
        dock.DefaultDockable = Remap(dock.DefaultDockable, resolved);

        if (dock is IRootDock root)
        {
            root.FocusedDockable = Remap(root.FocusedDockable, resolved);
        }

        return adopted;
    }

    private static void ResolveCollection(
        ObservableCollection<IDockable>? dockables,
        DockableResolver resolver,
        Dictionary<IDockable, IDockable?> resolved)
    {
        if (dockables is null)
        {
            return;
        }

        // Backwards so removals do not disturb the indices still to be visited.
        for (var i = dockables.Count - 1; i >= 0; i--)
        {
            var result = Resolve(dockables[i], resolver, resolved);

            if (result is null)
            {
                dockables.RemoveAt(i);
            }
            else if (!ReferenceEquals(result, dockables[i]))
            {
                dockables[i] = result;
            }
        }
    }

    private static void ResolveWindows(
        IRootDock rootDock,
        DockableResolver resolver,
        Dictionary<IDockable, IDockable?> resolved)
    {
        if (rootDock.Windows is null)
        {
            return;
        }

        for (var i = rootDock.Windows.Count - 1; i >= 0; i--)
        {
            var window = rootDock.Windows[i];
            if (window.Layout is null)
            {
                continue;
            }

            // A window whose layout is dropped has nothing left to show.
            if (Resolve(window.Layout, resolver, resolved) is IRootDock layout)
            {
                window.Layout = layout;
            }
            else
            {
                rootDock.Windows.RemoveAt(i);
            }
        }
    }

    private static IDockable? Remap(IDockable? dockable, Dictionary<IDockable, IDockable?> resolved)
    {
        if (dockable is null)
        {
            return null;
        }

        return resolved.TryGetValue(dockable, out var result) ? result : dockable;
    }
}

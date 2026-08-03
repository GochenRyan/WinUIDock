namespace Dock.Model.Core;

/// <summary>
/// Called once for every dockable produced by deserialization, giving the host a
/// chance to swap in its own live object before the layout goes into use.
/// Counterpart of DockPanelSuite's DeserializeDockContent.
/// </summary>
/// <param name="deserialized">The node just read from the layout file. Inspect
/// <see cref="IDockable.Id"/> for leaves and <see cref="IDockable.Kind"/> for
/// structural nodes to decide what it is.</param>
/// <returns>
/// <list type="bullet">
/// <item>A live object owned by the host — it is adopted in place of the
/// deserialized node, keeping its Content, event wiring and object identity.</item>
/// <item><paramref name="deserialized"/> itself — kept as is (the common case).</item>
/// <item><c>null</c> — the node is dropped from the layout, which is how a file
/// referencing a panel that no longer exists degrades gracefully.</item>
/// </list>
/// </returns>
/// <remarks>
/// Adopting a <see cref="IDock"/> replaces the node wholesale: the resolver is
/// responsible for moving whatever it wants off <paramref name="deserialized"/>
/// (typically <see cref="IDock.VisibleDockables"/>) onto the object it returns —
/// see DockableResolution.TransplantStructure. Resolution then continues into the
/// adopted object's children, so transplanted nodes still get resolved.
///
/// Careful when adopting the ROOT: the load then returns the very instance the
/// DockControl already hosts, so assigning it back is a same-value assignment,
/// which is a no-op — InitLayout never runs and the transplanted children keep
/// pointing at the discarded tree as their owner. Either leave the root
/// deserialized, or call InitLayout explicitly after assigning.
/// </remarks>
public delegate IDockable? DockableResolver(IDockable deserialized);

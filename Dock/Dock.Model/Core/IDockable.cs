
namespace Dock.Model.Core;

/// <summary>
/// Dockable contract.
/// </summary>
public interface IDockable
{
    /// <summary>
    /// Gets or sets the unique instance identity of this dockable.
    /// Supplied by the host application; the framework never writes it.
    /// Used for content re-association, navigation by id and layout round-trips.
    /// Contract: when non-empty it must be unique within a single factory; an
    /// empty value simply means "does not participate in id-based matching".
    /// For the key the factory locators are keyed by, see <see cref="Kind"/>.
    /// </summary>
    string Id { get; set; }

    /// <summary>
    /// Gets or sets the locator key / category of this dockable.
    /// The framework writes a default (the contract type name); hosts may override it.
    /// Used to look up ContextLocator / HostWindowLocator / DockableLocator and to
    /// match dockables by category.
    /// Contract: always has a value and may repeat across instances of the same kind.
    /// </summary>
    string Kind { get; set; }

    /// <summary>
    /// Gets or sets dockable title.
    /// </summary>
    string Title { get; set; }

    /// <summary>
    /// Gets or sets dockable context.
    /// </summary>
    object? Context { get; set; }

    /// <summary>
    /// Gets or sets dockable owner.
    /// </summary>
    IDockable? Owner { get; set; }

    /// <summary>
    /// Gets or sets dockable original owner.
    /// </summary>
    IDockable? OriginalOwner { get; set; }

    /// <summary>
    /// Gets or sets the dock this dockable sat in before it was closed or collapsed,
    /// so it can be put back there. Deliberately separate from <see cref="OriginalOwner"/>,
    /// which pinning uses for a different purpose.
    /// Written by CloseDockable / CollapseDock, consumed and cleared by RestoreDockable.
    /// </summary>
    IDockable? RestoreOwner { get; set; }

    /// <summary>
    /// Gets or sets the index this dockable had in <see cref="RestoreOwner"/>.
    /// Clamped on restore, since siblings may have come and gone meanwhile.
    /// </summary>
    int RestoreIndex { get; set; }

    /// <summary>
    /// Gets or sets dockable factory.
    /// </summary>
    IFactory? Factory { get; set; }

    /// <summary>
    /// Gets or sets if the dockable can be closed.
    /// </summary>
    bool CanClose { get; set; }

    /// <summary>
    /// Gets or sets if the dockable can be pinned.
    /// </summary>
    bool CanPin { get; set; }

    /// <summary>
    /// Gets or sets if the dockable can be floated.
    /// </summary>
    bool CanFloat { get; set; }

    /// <summary>
    /// Called when the dockable is closed.
    /// </summary>
    /// <returns>true to accept the close, and false to cancel the close.</returns>
    bool OnClose();

    /// <summary>
    /// Called when the dockable becomes the selected dockable.
    /// </summary>
    void OnSelected();

    /// <summary>
    /// Gets dockable visible bounds information used for tracking.
    /// </summary>
    /// <param name="x">The dockable x axis position.</param>
    /// <param name="y">The dockable y axis position.</param>
    /// <param name="width">The dockable width.</param>
    /// <param name="height">The dockable height.</param>
    void GetVisibleBounds(out double x, out double y, out double width, out double height);

    /// <summary>
    /// Sets dockable visible bounds information used for tracking.
    /// </summary>
    /// <param name="x">The dock x axis position.</param>
    /// <param name="y">The dock y axis position.</param>
    /// <param name="width">The dockable width.</param>
    /// <param name="height">The dockable height.</param>
    void SetVisibleBounds(double x, double y, double width, double height);

    /// <summary>
    /// Called when dockable visible bounds changed.
    /// </summary>
    /// <param name="x">The dock x axis position.</param>
    /// <param name="y">The dock y axis position.</param>
    /// <param name="width">The dockable width.</param>
    /// <param name="height">The dockable height.</param>
    void OnVisibleBoundsChanged(double x, double y, double width, double height);

    /// <summary>
    /// Gets dockable pinned bounds information used for tracking.
    /// </summary>
    /// <param name="x">The dockable x axis position.</param>
    /// <param name="y">The dockable y axis position.</param>
    /// <param name="width">The dockable width.</param>
    /// <param name="height">The dockable height.</param>
    void GetPinnedBounds(out double x, out double y, out double width, out double height);

    /// <summary>
    /// Sets dockable pinned bounds information used for tracking.
    /// </summary>
    /// <param name="x">The dock x axis position.</param>
    /// <param name="y">The dock y axis position.</param>
    /// <param name="width">The dockable width.</param>
    /// <param name="height">The dockable height.</param>
    void SetPinnedBounds(double x, double y, double width, double height);

    /// <summary>
    /// Called when dockable pinned bounds changed.
    /// </summary>
    /// <param name="x">The dock x axis position.</param>
    /// <param name="y">The dock y axis position.</param>
    /// <param name="width">The dockable width.</param>
    /// <param name="height">The dockable height.</param>
    void OnPinnedBoundsChanged(double x, double y, double width, double height);

    /// <summary>
    /// Gets dockable tab bounds information used for tracking.
    /// </summary>
    /// <param name="x">The dockable x axis position.</param>
    /// <param name="y">The dockable y axis position.</param>
    /// <param name="width">The dockable width.</param>
    /// <param name="height">The dockable height.</param>
    void GetTabBounds(out double x, out double y, out double width, out double height);

    /// <summary>
    /// Sets dockable tab bounds information used for tracking.
    /// </summary>
    /// <param name="x">The dock x axis position.</param>
    /// <param name="y">The dock y axis position.</param>
    /// <param name="width">The dockable width.</param>
    /// <param name="height">The dockable height.</param>
    void SetTabBounds(double x, double y, double width, double height);

    /// <summary>
    /// Called when dockable tab bounds changed.
    /// </summary>
    /// <param name="x">The dock x axis position.</param>
    /// <param name="y">The dock y axis position.</param>
    /// <param name="width">The dockable width.</param>
    /// <param name="height">The dockable height.</param>
    void OnTabBoundsChanged(double x, double y, double width, double height);

    /// <summary>
    /// Gets dockable pointer position used for tracking.
    /// </summary>
    /// <param name="x">The pointer x axis position.</param>
    /// <param name="y">The pointer y axis position.</param>
    void GetPointerPosition(out double x, out double y);

    /// <summary>
    /// Sets dockable pointer position used for tracking.
    /// </summary>
    /// <param name="x">The pointer x axis position.</param>
    /// <param name="y">The pointer y axis position.</param>
    void SetPointerPosition(double x, double y);

    /// <summary>
    /// Called when dockable pointer position changed.
    /// </summary>
    /// <param name="x">The pointer x axis position.</param>
    /// <param name="y">The pointer y axis position.</param>
    void OnPointerPositionChanged(double x, double y);

    /// <summary>
    /// Gets dockable pointer screen position used for tracking.
    /// </summary>
    /// <param name="x">The pointer x axis position.</param>
    /// <param name="y">The pointer y axis position.</param>
    void GetPointerScreenPosition(out double x, out double y);

    /// <summary>
    /// Sets dockable pointer screen position used for tracking.
    /// </summary>
    /// <param name="x">The pointer x axis position.</param>
    /// <param name="y">The pointer y axis position.</param>
    void SetPointerScreenPosition(double x, double y);

    /// <summary>
    /// Called when dockable pointer screen position changed.
    /// </summary>
    /// <param name="x">The pointer x axis position.</param>
    /// <param name="y">The pointer y axis position.</param>
    void OnPointerScreenPositionChanged(double x, double y);
}
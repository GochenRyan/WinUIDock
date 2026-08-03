using Dock.Model.Controls;

namespace Dock.Model.Core;

/// <summary>
/// Dock factory contract.
/// </summary>
public partial interface IFactory
{
    /// <summary>
    /// Gets visible dockable controls.
    /// </summary>
    IDictionary<IDockable, IDockableControl> VisibleDockableControls { get; }

    /// <summary>
    /// Gets pinned dockable controls.
    /// </summary>
    IDictionary<IDockable, IDockableControl> PinnedDockableControls { get; }

    /// <summary>
    /// Gets tab dockable controls.
    /// </summary>
    IDictionary<IDockable, IDockableControl> TabDockableControls { get; }

    /// <summary>
    /// Gets dock controls.
    /// </summary>
    IList<IDockControl> DockControls { get; }

    /// <summary>
    /// Gets host windows.
    /// </summary>
    IList<IHostWindow> HostWindows { get; }

    /// <summary>
    /// Gets or sets <see cref="IDockable.Context"/> default locator.
    /// </summary>
    Func<object?>? DefaultContextLocator { get; set; }

    /// <summary>
    /// Gets or sets <see cref="IHostWindow"/> default locator.
    /// </summary>
    Func<IHostWindow?>? DefaultHostWindowLocator { get; set; }

    /// <summary>
    /// Gets or sets <see cref="IDockable.Context"/> locator registry.
    /// </summary>
    Dictionary<string, Func<object?>>? ContextLocator { get; set; }

    /// <summary>
    /// Gets or sets <see cref="IHostWindow"/> locator registry.
    /// </summary>
    Dictionary<string, Func<IHostWindow?>>? HostWindowLocator { get; set; }

    /// <summary>
    /// Gets or sets <see cref="IDockable"/> locator registry.
    /// </summary>
    IDictionary<string, Func<IDockable?>>? DockableLocator { get; set; }

    /// <summary>
    /// Creates list of type <see cref="IList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The list item type.</typeparam>
    /// <param name="items">The initial list items.</param>
    /// <returns>The new instance of <see cref="IList{T}"/>.</returns>
    IList<T> CreateList<T>(params T[] items);

    /// <summary>
    /// Creates <see cref="IRootDock"/>.
    /// </summary>
    /// <returns>The new instance of the <see cref="IRootDock"/> class.</returns>
    IRootDock CreateRootDock();

    /// <summary>
    /// Creates <see cref="IProportionalDock"/>.
    /// </summary>
    /// <returns>The new instance of the <see cref="IProportionalDock"/> class.</returns>
    IProportionalDock CreateProportionalDock();

    /// <summary>
    /// Creates <see cref="IDockDock"/>.
    /// </summary>
    /// <returns>The new instance of the <see cref="IDockDock"/> class.</returns>
    IDockDock CreateDockDock();

    /// <summary>
    /// Creates <see cref="IProportionalDockSplitter"/>.
    /// </summary>
    /// <returns>The new instance of the <see cref="IProportionalDockSplitter"/> class.</returns>
    IProportionalDockSplitter CreateProportionalDockSplitter();

    /// <summary>
    /// Creates <see cref="IToolDock"/>.
    /// </summary>
    /// <returns>The new instance of the <see cref="IToolDock"/> class.</returns>
    IToolDock CreateToolDock();

    /// <summary>
    /// Creates <see cref="IDocumentDock"/>.
    /// </summary>
    /// <returns>The new instance of the <see cref="IDocumentDock"/> class.</returns>
    IDocumentDock CreateDocumentDock();

    /// <summary>
    /// Creates <see cref="IDockWindow"/>.
    /// </summary>
    /// <returns>The new instance of the <see cref="IDockWindow"/> class.</returns>
    IDockWindow CreateDockWindow();

    /// <summary>
    /// Creates layout.
    /// </summary>
    /// <returns>The new instance of the <see cref="IRootDock"/> class.</returns>
    IRootDock? CreateLayout();

    /// <summary>
    /// Gets registered context in <see cref="ContextLocator"/>.
    /// </summary>
    /// <param name="kind">The dockable kind, see <see cref="IDockable.Kind"/>.</param>
    /// <returns>The located context.</returns>
    object? GetContext(string kind);

    /// <summary>
    /// Gets registered host window.
    /// </summary>
    /// <param name="kind">The window kind, see <see cref="IDockWindow.Kind"/>.</param>
    /// <returns>The located host.</returns>
    IHostWindow? GetHostWindow(string kind);

    /// <summary>
    /// Gets registered dockable in <see cref="DockableLocator"/>. This is the
    /// spawner entry point: register a factory per kind to make a dockable
    /// re-creatable after it has been closed.
    /// </summary>
    /// <param name="kind">The dockable kind, see <see cref="IDockable.Kind"/>.</param>
    /// <typeparam name="T">The dockable return type.</typeparam>
    /// <returns>The located dockable.</returns>
    T? GetDockable<T>(string kind) where T : class, IDockable;

    /// <summary>
    /// Initialize layout.
    /// </summary>
    /// <param name="layout">The layout to initialize.</param>
    void InitLayout(IDockable layout);

    /// <summary>
    /// Initialize dockable.
    /// </summary>
    /// <param name="dockable">The dockable to update.</param>
    /// <param name="owner">The owner dockable.</param>
    void InitDockable(IDockable dockable, IDockable? owner);

    /// <summary>
    /// Initialize dock window.
    /// </summary>
    /// <param name="window">The window to update.</param>
    /// <param name="owner">The window owner dockable.</param>
    void InitDockWindow(IDockWindow window, IDockable? owner);

    /// <summary>
    /// Initialize active dockable.
    /// </summary>
    /// <param name="dockable">The dockable to update.</param>
    /// <param name="owner">The owner dockable.</param>
    void InitActiveDockable(IDockable? dockable, IDock owner);

    /// <summary>
    /// Sets an active dockable. If the dockable is contained inside an dock it
    /// will become the selected dockable.
    /// </summary>
    /// <param name="dockable">The dockable to select.</param>
    void SetActiveDockable(IDockable dockable);

    /// <summary>
    /// Sets the currently focused dockable updating IsActive flags.
    /// </summary>
    /// <param name="dock">The dock to set the focused dockable on.</param>
    /// <param name="dockable">The dockable to set.</param>
    void SetFocusedDockable(IDock dock, IDockable? dockable);

    /// <summary>
    /// Searches for root dockable.
    /// </summary>
    /// <param name="dockable">The dockable to find root for.</param>
    /// <param name="predicate">The predicate to filter root docks.</param>
    /// <returns>The root dockable instance or null if root dockable was not found.</returns>
    IRootDock? FindRoot(IDockable dockable, Func<IRootDock, bool>? predicate = null);

    /// <summary>
    /// Searches dock for dockable.
    /// </summary>
    /// <param name="dock">The dock.</param>
    /// <param name="predicate">The predicate to filter dockables.</param>
    /// <returns>The dockable instance or null if dockable was not found.</returns>
    IDockable? FindDockable(IDock dock, Func<IDockable, bool> predicate);

    /// <summary>
    /// Finds the single dockable carrying the given <see cref="IDockable.Id"/>.
    /// Id is an instance identity, so this returns one value or null — never a set.
    /// An empty id never matches: it means "does not participate in id-based lookup".
    /// Duplicates break the contract and are reported through the factory's
    /// violation handler (throws in Debug, traces in Release).
    /// </summary>
    /// <param name="id">The instance id to look for.</param>
    /// <param name="scope">Limits the search to this subtree; null searches the
    /// whole factory (every registered <see cref="DockControls"/> layout).</param>
    /// <returns>The matching dockable, or null.</returns>
    IDockable? FindDockableById(string id, IDock? scope = null);

    /// <summary>
    /// Checks that this dockable's <see cref="IDockable.Id"/> does not collide with
    /// one already in the factory. No-op for empty ids. Reports through the same
    /// violation handler as <see cref="FindDockableById"/>.
    /// </summary>
    /// <remarks>
    /// Each call walks the tree, so this is meant for one-off checks (adopting a
    /// dockable, wiring up a new panel) — not for every insert during a bulk load.
    /// Use <see cref="ValidateIds"/> once after loading instead.
    /// </remarks>
    void ValidateId(IDockable dockable);

    /// <summary>
    /// Validates Id uniqueness across the tree in one pass. Intended to run after
    /// deserialization and before saving.
    /// </summary>
    /// <param name="scope">Limits validation to this subtree; null validates the
    /// whole factory.</param>
    /// <returns>One entry per offending id with all dockables sharing it; empty
    /// when the tree is compliant.</returns>
    IReadOnlyList<(string Id, IReadOnlyList<IDockable> Dockables)> ValidateIds(IDock? scope = null);

    /// <summary>
    /// Searches for dockables in all registered <see cref="IDockControl"/>.
    /// </summary>
    /// <param name="predicate">The predicate to filter dockables.></param>
    /// <returns>The dockables collection.</returns>
    IEnumerable<IDockable> Find(Func<IDockable, bool> predicate);

    /// <summary>
    /// Searches dock for dockables in all registered <see cref="IDockControl"/>.
    /// </summary>
    /// <param name="dock"></param>
    /// <param name="predicate">The predicate to filter dockables.></param>
    /// <returns>The dockables collection.</returns>
    IEnumerable<IDockable> Find(IDock dock, Func<IDockable, bool> predicate);

    /// <summary>
    /// Adds <see cref="IDockable"/> into dock <see cref="IDock.VisibleDockables"/> collection.
    /// </summary>
    /// <param name="dock">The owner dock.</param>
    /// <param name="dockable">The dockable to add.</param>
    void AddDockable(IDock dock, IDockable dockable);

    /// <summary>
    /// Inserts <see cref="IDockable"/> into dock <see cref="IDock.VisibleDockables"/> collection.
    /// </summary>
    /// <param name="dock">The owner dock.</param>
    /// <param name="dockable">The dockable to add.</param>
    /// <param name="index">The dockable index.</param>
    void InsertDockable(IDock dock, IDockable dockable, int index);

    /// <summary>
    /// Removes dockable from owner <see cref="IDock.VisibleDockables"/> collection.
    /// </summary>
    /// <param name="dockable">The dockable to remove.</param>
    /// <param name="collapse">The flag indicating whether to collapse empty dock.</param>
    void RemoveDockable(IDockable dockable, bool collapse);

    /// <summary>
    /// Moves dockable inside <see cref="IDock.VisibleDockables"/> collection.
    /// </summary>
    /// <param name="dock">The dock.</param>
    /// <param name="sourceDockable">The source dockable.</param>
    /// <param name="targetDockable">The target dockable.</param>
    void MoveDockable(IDock dock, IDockable sourceDockable, IDockable targetDockable);

    /// <summary>
    /// Moves dockable into another <see cref="IDock.VisibleDockables"/> collection.
    /// </summary>
    /// <param name="sourceDock">The source dock.</param>
    /// <param name="targetDock">The target dock.</param>
    /// <param name="sourceDockable">The source dockable.</param>
    /// <param name="targetDockable">The target dockable.</param>
    void MoveDockable(IDock sourceDock, IDock targetDock, IDockable sourceDockable, IDockable? targetDockable);

    /// <summary>
    /// Swaps dockable in inside <see cref="IDock.VisibleDockables"/> collections.
    /// </summary>
    /// <param name="dock">The dock.</param>
    /// <param name="sourceDockable">The source dockable.</param>
    /// <param name="targetDockable">The target dockable.</param>
    void SwapDockable(IDock dock, IDockable sourceDockable, IDockable targetDockable);

    /// <summary>
    /// Swaps dockable into between <see cref="IDock.VisibleDockables"/> collections.
    /// </summary>
    /// <param name="sourceDock">The source dock.</param>
    /// <param name="targetDock">The target dock.</param>
    /// <param name="sourceDockable">The source dockable.</param>
    /// <param name="targetDockable">The target dockable.</param>
    void SwapDockable(IDock sourceDock, IDock targetDock, IDockable sourceDockable, IDockable targetDockable);

    /// <summary>
    /// Pins or unpins a dockable.
    /// </summary>
    /// <param name="dockable">The dockable to pin/unpin.</param>
    void PinDockable(IDockable dockable);

    /// <summary>
    /// Unpins a dockable.
    /// </summary>
    /// <param name="dockable">The dockable to unpin.</param>
    void UnpinDockable(IDockable dockable);

    /// <summary>
    /// Temporarily shows a pinned dockable.
    /// </summary>
    /// <param name="dockable">The dockable to show.</param>
    void PreviewPinnedDockable(IDockable dockable);

    /// <summary>
    /// Hides all temporarily shown pinned dockables.
    /// </summary>
    /// <param name="rootDock">The owner of the pinned dockables</param>
    void HidePreviewingDockables(IRootDock rootDock);

    /// <summary>
    /// Returns true if dockable is pinned.
    /// </summary>
    /// <param name="dockable">The dockable to check.</param>
    /// <param name="rootDock">The root dock. If null, the root will be automatically found.</param>
    bool IsDockablePinned(IDockable dockable, IRootDock? rootDock = null);

    /// <summary>
    /// Floats dockable.
    /// </summary>
    /// <param name="dockable">The dockable to float.</param>
    void FloatDockable(IDockable dockable);

    /// <summary>
    /// Removes dockable from owner <see cref="IDock.VisibleDockables"/> collection, and call IDockable.OnClose.
    /// </summary>
    /// <param name="dockable">The dockable to remove.</param>
    void CloseDockable(IDockable dockable);

    /// <summary>
    /// Calls <see cref="IFactory.CloseDockable"/> on all <see cref="IDock.VisibleDockables"/> of the dockable owner, excluding the dockable itself.
    /// </summary>
    /// <param name="dockable">The dockable owner source.</param>
    void CloseOtherDockables(IDockable dockable);

    /// <summary>
    /// Calls <see cref="IFactory.CloseDockable"/> on all <see cref="IDock.VisibleDockables"/> of the dockable owner.
    /// </summary>
    /// <param name="dockable">The dockable owner source.</param>
    void CloseAllDockables(IDockable dockable);

    /// <summary>
    /// Calls <see cref="IFactory.CloseDockable"/> on all tabs to the left of the dockable, from the <see cref="IDock.VisibleDockables"/> collection of the dockable owner.
    /// </summary>
    /// <param name="dockable">The dockable owner source.</param>
    void CloseLeftDockables(IDockable dockable);

    /// <summary>
    /// Calls <see cref="IFactory.CloseDockable"/> on all tabs to the right of the dockable, from the <see cref="IDock.VisibleDockables"/> collection of the dockable owner.
    /// </summary>
    /// <param name="dockable">The dockable owner source.</param>
    void CloseRightDockables(IDockable dockable);

    /// <summary>
    /// Adds window into dock windows list.
    /// </summary>
    /// <param name="rootDock">The root dock.</param>
    /// <param name="window">The window to add.</param>
    void AddWindow(IRootDock rootDock, IDockWindow window);

    /// <summary>
    /// Inserts window into dock windows list.
    /// </summary>
    /// <param name="rootDock">The root dock.</param>
    /// <param name="window">The window to add.</param>
    /// <param name="index">The window index.</param>
    void InsertWindow(IRootDock rootDock, IDockWindow window, int index);

    /// <summary>
    /// Removes window from owner windows list.
    /// </summary>
    /// <param name="window">The window to remove.</param>
    void RemoveWindow(IDockWindow window);

    /// <summary>
    /// Collapses dock.
    /// </summary>
    /// <param name="dock">The dock to collapse.</param>
    void CollapseDock(IDock dock);

    /// <summary>
    /// Puts a closed or collapsed dockable back where it came from, using the anchor
    /// recorded by <see cref="CloseDockable"/> / <see cref="CollapseDock"/>.
    ///
    /// Restores recursively: if closing the dockable emptied its dock and that dock
    /// was collapsed away, the dock is brought back first. This is what makes
    /// "close the last tool in a pane, then reopen it from the menu" put both the
    /// pane and the tool back in their original spot.
    /// </summary>
    /// <param name="dockable">The dockable to restore.</param>
    /// <returns>True if an anchor was available and the dockable was restored.</returns>
    bool RestoreDockable(IDockable dockable);

    /// <summary>
    /// Creates a new split layout from source dockable.
    /// </summary>
    /// <param name="dock">The dock to perform operation on.</param>
    /// <param name="dockable">The optional dockable to add to a split side.</param>
    /// <param name="operation">The dock operation.</param>
    /// <returns>The new instance of the <see cref="IDock"/> class.</returns>
    IDock CreateSplitLayout(IDock dock, IDockable dockable, DockOperation operation);

    /// <summary>
    /// Splits dock and updates owner layout.
    /// </summary>
    /// <param name="dock">The dock to perform operation on.</param>
    /// <param name="dockable">The optional dockable to add to a split side.</param>
    /// <param name="operation"> The dock operation to perform.</param>
    void SplitToDock(IDock dock, IDockable dockable, DockOperation operation);

    /// <summary>
    /// Inserts a dock along one edge of the root layout, spanning the whole
    /// window width or height. Unlike <see cref="SplitToDock"/> — which
    /// subdivides the rectangle of the dock under the cursor — this inserts at
    /// the root level, so every existing pane gives way proportionally.
    /// </summary>
    /// <param name="rootDock">The root dock whose layout gains the edge region.</param>
    /// <param name="dock">The dock to place along the edge.</param>
    /// <param name="operation">One of the <c>Root*</c> dock operations.</param>
    /// <returns>True when the root layout could be resolved and the dock was inserted.</returns>
    bool SplitToRootEdge(IRootDock rootDock, IDock dock, DockOperation operation);

    /// <summary>
    /// Creates dock window from source dockable.
    /// </summary>
    /// <param name="dockable">The dockable to embed into window.</param>
    /// <returns>The new instance of the <see cref="IDockWindow"/> class.</returns>
    IDockWindow? CreateWindowFrom(IDockable dockable);

    /// <summary>
    /// Splits dock to the <see cref="DockOperation.Window"/> and updates <see cref="IDockable.Owner"/> layout.
    /// </summary>
    /// <param name="dock">The window owner.</param>
    /// <param name="dockable">The dockable to add to a split window.</param>
    /// <param name="x">The window X coordinate.</param>
    /// <param name="y">The window Y coordinate.</param>
    /// <param name="width">The window width.</param>
    /// <param name="height">The window height.</param>
    void SplitToWindow(IDock dock, IDockable dockable, double x, double y, double width, double height);
}

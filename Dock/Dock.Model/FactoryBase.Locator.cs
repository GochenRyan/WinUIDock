using Dock.Model.Controls;
using Dock.Model.Core;

namespace Dock.Model;

/// <summary>
/// Factory base class.
/// </summary>
public abstract partial class FactoryBase
{
    /// <inheritdoc/>
    public virtual Func<object?>? DefaultContextLocator { get; set; }

    /// <inheritdoc/>
    public virtual Func<IHostWindow?>? DefaultHostWindowLocator { get; set; }

    /// <inheritdoc/>
    public virtual Dictionary<string, Func<object?>>? ContextLocator { get; set; }

    /// <inheritdoc/>
    public virtual Dictionary<string, Func<IHostWindow?>>? HostWindowLocator { get; set; }

    /// <inheritdoc/>
    public virtual IDictionary<string, Func<IDockable?>>? DockableLocator { get; set; }

    /// <inheritdoc/>
    public virtual object? GetContext(string kind)
    {
        if (string.IsNullOrEmpty(kind))
        {
            return null;
        }

        if (ContextLocator?.TryGetValue(kind, out var locator) == true)
        {
            return locator?.Invoke();
        }

        return DefaultContextLocator?.Invoke();
    }

    /// <inheritdoc/>
    public virtual IHostWindow? GetHostWindow(string kind)
    {
        if (string.IsNullOrEmpty(kind))
        {
            return null;
        }

        if (HostWindowLocator?.TryGetValue(kind, out var locator) == true)
        {
            return locator?.Invoke();
        }

        return DefaultHostWindowLocator?.Invoke();
    }

    /// <inheritdoc/>
    public virtual T? GetDockable<T>(string kind) where T : class, IDockable
    {
        if (string.IsNullOrEmpty(kind))
        {
            return default;
        }

        if (DockableLocator?.TryGetValue(kind, out var locator) == true)
        {
            return locator?.Invoke() as T;
        }

        return default;
    }

    /// <inheritdoc/>
    public virtual IRootDock? FindRoot(IDockable dockable, Func<IRootDock, bool>? predicate = null)
    {
        if (dockable.Owner is null)
        {
            return null;
        }
        if (dockable.Owner is IRootDock rootDock && (predicate?.Invoke(rootDock) ?? true))
        {
            return rootDock;
        }
        return FindRoot(dockable.Owner, predicate);
    }

    /// <inheritdoc/>
    public virtual IDockable? FindDockable(IDock dock, Func<IDockable, bool> predicate)
    {
        if (predicate(dock))
        {
            return dock;
        }

        if (dock.VisibleDockables is not null)
        {
            foreach (var dockable in dock.VisibleDockables)
            {
                if (predicate(dockable))
                {
                    return dockable;
                }

                if (dockable is IDock childDock)
                {
                    var result = FindDockable(childDock, predicate);
                    if (result is not null)
                    {
                        return result;
                    }
                }
            }
        }

        if (dock is IRootDock rootDock && rootDock.Windows is not null)
        {
            foreach (var window in rootDock.Windows)
            {
                if (window.Layout is null)
                {
                    continue;
                }

                if (predicate(window.Layout))
                {
                    return window.Layout;
                }

                var result = FindDockable(window.Layout, predicate);
                if (result is not null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<IDockable> Find(Func<IDockable, bool> predicate)
    {
        var layouts = new List<IDock>();
        foreach (var dockControl in DockControls)
        {
            if (dockControl.Layout is { } dock)
            {
                layouts.Add(dock);
            }
        }

        // A float window's DockControl only registers when it LOADS — until then
        // its dockables exist in the model but under no registered layout. Walk
        // Windows for exactly those; the reference set keeps loaded ones from
        // being enumerated twice.
        var registered = new HashSet<IDockable>(layouts);

        foreach (var dock in layouts)
        {
            foreach (var result in Find(dock, predicate))
            {
                yield return result;
            }

            if (dock is not IRootDock { Windows: { } windows })
            {
                continue;
            }

            foreach (var window in windows)
            {
                if (window.Layout is { } layout && !registered.Contains(layout))
                {
                    foreach (var result in Find(layout, predicate))
                    {
                        yield return result;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Enumerates the dockables under <paramref name="dock"/> matching the predicate.
    ///
    /// Covers VisibleDockables recursively plus a root's pinned and hidden
    /// collections — a pinned tool is still a live dockable that owns its Id, and
    /// leaving it out made callers believe it was gone (and re-create it under the
    /// same Id).
    ///
    /// Deliberately does NOT descend into <see cref="IRootDock.Windows"/>: a
    /// LOADED floating window registers its own DockControl and is reached that
    /// way; walking Windows here as well would yield the same instance twice and
    /// make uniqueness checks report phantom duplicates. The not-yet-loaded gap
    /// is closed by <see cref="Find(Func{IDockable, bool})"/>, which walks
    /// Windows for exactly the layouts no registered control covers.
    /// </summary>
    public IEnumerable<IDockable> Find(IDock dock, Func<IDockable, bool> predicate)
    {
        if (predicate(dock))
        {
            yield return dock;
        }

        if (dock.VisibleDockables is not null)
        {
            foreach (var dockable in dock.VisibleDockables)
            {
                // A child dock is yielded by the recursive call's first statement,
                // so testing it here as well would emit the same instance twice.
                if (dockable is IDock childDock)
                {
                    foreach (var result in Find(childDock, predicate))
                    {
                        yield return result;
                    }
                }
                else if (predicate(dockable))
                {
                    yield return dockable;
                }
            }
        }

        if (dock is IRootDock rootDock)
        {
            foreach (var result in FindInDetached(rootDock.LeftPinnedDockables, predicate))
            {
                yield return result;
            }

            foreach (var result in FindInDetached(rootDock.RightPinnedDockables, predicate))
            {
                yield return result;
            }

            foreach (var result in FindInDetached(rootDock.TopPinnedDockables, predicate))
            {
                yield return result;
            }

            foreach (var result in FindInDetached(rootDock.BottomPinnedDockables, predicate))
            {
                yield return result;
            }

            foreach (var result in FindInDetached(rootDock.HiddenDockables, predicate))
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Walks a collection that sits outside VisibleDockables (pinned / hidden).
    /// A pinned dockable is normally a leaf, but recurse anyway so a pinned dock
    /// carrying children is covered too.
    /// </summary>
    private IEnumerable<IDockable> FindInDetached(
        IEnumerable<IDockable>? dockables,
        Func<IDockable, bool> predicate)
    {
        if (dockables is null)
        {
            yield break;
        }

        foreach (var dockable in dockables)
        {
            if (dockable is IDock childDock)
            {
                foreach (var result in Find(childDock, predicate))
                {
                    yield return result;
                }
            }
            else if (predicate(dockable))
            {
                yield return dockable;
            }
        }
    }

    /// <inheritdoc/>
    public virtual IDockable? FindDockableById(string id, IDock? scope = null)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var matches = scope is null ? Find(x => x.Id == id) : Find(scope, x => x.Id == id);

        IDockable? match = null;
        var duplicates = 0;

        foreach (var dockable in matches)
        {
            if (match is null)
            {
                match = dockable;
            }
            else if (!ReferenceEquals(match, dockable))
            {
                duplicates++;
            }
        }

        if (duplicates > 0)
        {
            OnIdViolation(
                $"Duplicate dockable id '{id}': {duplicates + 1} instances share it. " +
                "Id must be unique within a factory; use Kind for category matching.");
        }

        return match;
    }

    /// <inheritdoc/>
    public virtual void ValidateId(IDockable dockable)
    {
        if (string.IsNullOrEmpty(dockable.Id))
        {
            return;
        }

        foreach (var other in Find(x => x.Id == dockable.Id))
        {
            if (ReferenceEquals(other, dockable))
            {
                continue;
            }

            OnIdViolation(
                $"Duplicate dockable id '{dockable.Id}': '{dockable.Title}' collides with " +
                $"'{other.Title}'. Id must be unique within a factory; use Kind for category matching.");
            return;
        }
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<(string Id, IReadOnlyList<IDockable> Dockables)> ValidateIds(IDock? scope = null)
    {
        var byId = new Dictionary<string, List<IDockable>>();
        var matches = scope is null ? Find(_ => true) : Find(scope, _ => true);

        foreach (var dockable in matches)
        {
            // Empty ids mean "does not participate in id-based matching", so they
            // must not be reported as colliding with each other.
            if (string.IsNullOrEmpty(dockable.Id))
            {
                continue;
            }

            if (!byId.TryGetValue(dockable.Id, out var list))
            {
                byId[dockable.Id] = list = new List<IDockable>();
            }

            if (!list.Any(x => ReferenceEquals(x, dockable)))
            {
                list.Add(dockable);
            }
        }

        var violations = new List<(string, IReadOnlyList<IDockable>)>();
        foreach (var pair in byId)
        {
            if (pair.Value.Count > 1)
            {
                violations.Add((pair.Key, pair.Value));
            }
        }

        return violations;
    }

    /// <summary>
    /// Called when the Id uniqueness contract is broken. Throws in Debug so the
    /// violation surfaces during development, and traces in Release so a shipped
    /// app keeps running. Override to route violations to a host logger.
    /// </summary>
    protected virtual void OnIdViolation(string message)
    {
#if DEBUG
        throw new InvalidOperationException(message);
#else
        System.Diagnostics.Trace.WriteLine($"[WinUIDock] {message}");
#endif
    }
}

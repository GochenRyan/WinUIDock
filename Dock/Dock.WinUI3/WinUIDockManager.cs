using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace Dock.WinUI3
{
    public static class WinUIDockManager
    {
        private static IDockable CreateDockable(DockableType DockableType)
        {
            IDockable dockable = null;
            switch (DockableType)
            {
                case DockableType.Tool:
                    dockable = new Tool();
                    break;
                case DockableType.Document:
                    dockable = new Document();
                    break;
            }

            return dockable;
        }

        public static IDockable CreateDockable(DockableType DockableType, string id, string title, Control control)
        {
            IDockable dockable = CreateDockable(DockableType);

            if (dockable == null)
                return null;

            switch (DockableType)
            {
                case DockableType.Tool:
                    var tool = dockable as Tool;
                    tool.Id = id;
                    tool.Title = title;
                    tool.Content = control;
                    break;
                case DockableType.Document:
                    var document = dockable as Document;
                    document.Id = id;
                    document.Title = title;
                    document.Content = control;
                    break;
            }

            return dockable;
        }

        public static void SplitToWindow(IDock dock, IDockable dockable, double x, double y, double width, double height)
        {
            _factory.SplitToWindow(dock, dockable, x, y, width, height);
        }

        public static void SplitToDock(IDock dock, IDockable dockable, DockOperation operation)
        {
            _factory.SplitToDock(dock, dockable, operation);
        }

        /// <summary>
        /// Finds the dockable carrying the given <see cref="IDockable.Id"/>.
        /// Id is an instance identity and must be unique, so this returns a single
        /// value: the match, or null when there is none. An empty id never matches —
        /// it means "does not participate in id-based lookup".
        /// </summary>
        public static IDockable FindDockableByID(string id)
        {
            return FindSingleById<IDockable>(id, _ => true);
        }

        /// <summary>
        /// Finds the dock carrying the given <see cref="IDockable.Id"/>.
        /// See <see cref="FindDockableByID"/> for the single-value contract.
        /// </summary>
        public static IDock FindDockByID(string id)
        {
            return FindSingleById<IDock>(id, x => x is IDock);
        }

        /// <summary>
        /// Shared single-value lookup. Duplicate ids violate the uniqueness
        /// contract, so they throw in Debug and are recorded as diagnostics in
        /// Release (where the first match is returned to keep the app running).
        /// </summary>
        private static T FindSingleById<T>(string id, Func<IDockable, bool> filter)
            where T : class, IDockable
        {
            if (_factory is null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            T match = null;
            var duplicates = 0;

            foreach (var dockable in _factory.Find(x => x.Id == id && filter(x)))
            {
                if (dockable is not T typed)
                {
                    continue;
                }

                if (match is null)
                {
                    match = typed;
                }
                else if (!ReferenceEquals(match, typed))
                {
                    duplicates++;
                }
            }

            if (duplicates > 0)
            {
                var message =
                    $"Duplicate dockable id '{id}': {duplicates + 1} instances share it. " +
                    "Id must be unique within a factory; use Kind for category matching.";
#if DEBUG
                throw new InvalidOperationException(message);
#else
                Internal.DockDiag.Log($"WinUIDockManager {message}");
#endif
            }

            return match;
        }

        public static int GetIndex(IDockable dockable)
        {
            if (dockable.Owner == null)
                return -1;

            var onwer = dockable.Owner as IDock;

            return onwer.VisibleDockables.IndexOf(dockable);
        }

        public static void AddDockableTo(IDockable dockable, IDock dock)
        {
            _factory.AddDockable(dock, dockable);
        }

        public static void InsertDockableTo(IDockable dockable, IDock dock, int index)
        {
            _factory.InsertDockable(dock, dockable, index);
        }

        public static void CloseDockable(IDockable dockable)
        {
            _factory.CloseDockable(dockable);
        }

        public static void CloseOtherDockables(IDockable dockable)
        {
            _factory.CloseOtherDockables(dockable);
        }

        public static void CloseAllDockables(IDockable dockable)
        {
            _factory.CloseAllDockables(dockable);
        }

        public static void CloseLeftDockables(IDockable dockable)
        {
            _factory.CloseLeftDockables(dockable);
        }

        public static void CloseRightDockables(IDockable dockable)
        {
            _factory.CloseRightDockables(dockable);
        }

        public static void MoveDockable(IDock dock, IDockable sourceDockable, IDockable targetDockable)
        {
            _factory.MoveDockable(dock, sourceDockable, targetDockable);
        }

        public static void MoveDockable(IDock sourceDock, IDock targetDock, IDockable sourceDockable, IDockable targetDockable)
        {
            _factory.MoveDockable(sourceDock, targetDock, sourceDockable, targetDockable);
        }

        public static void SwapDockable(IDock dock, IDockable sourceDockable, IDockable targetDockable)
        {
            _factory.SwapDockable(dock, sourceDockable, targetDockable);
        }

        public static void SwapDockable(IDock sourceDock, IDock targetDock, IDockable sourceDockable, IDockable targetDockable)
        {
            _factory.SwapDockable(sourceDock, targetDock, sourceDockable, targetDockable);
        }

        public static void ActiveDockable(string id)
        {
            if (FindDockableByID(id) is { } dockable)
            {
                ActiveDockable(dockable);
            }
        }

        public static void ActiveDockable(IDockable dockable)
        {
            _factory.SetActiveDockable(dockable);
        }

        public static void SetFocusedDockable(IDockable dockable)
        {
            if (dockable.Owner == null)
                return;

            IDock dock = dockable.Owner as IDock;
            _factory.SetFocusedDockable(dock, dockable);
        }

        public static void SetFocusedDock(IDock dock)
        {
            _factory.SetFocusedDockable(dock, null);
        }

        public static void FloatDockable(IDockable dockable)
        {
            _factory.FloatDockable(dockable);
        }

        public static void PinDockable(IDockable dockable)
        {
            _factory.PinDockable(dockable);
        }

        public static void UnpinDockable(IDockable dockable)
        {
            _factory.UnpinDockable(dockable);
        }

        public static void SetFactory(IFactory factory)
        {
            _factory = factory;
        }

        public static IFactory GetFactory()
        {
            return _factory;
        }

        private static IFactory _factory;
    }
}

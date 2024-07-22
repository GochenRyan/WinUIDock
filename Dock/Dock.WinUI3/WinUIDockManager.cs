using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

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

        public static IEnumerable<IDockable> FindDockableByID(string id)
        {
            var res = _factory.Find(x => x.Id == id);
            return res;
        }

        public static IEnumerable<IDock> FindDockByID(string id)
        {
            IEnumerable<IDock> res = _factory.Find(x => x is IDock && x.Id == id).Select(x => (IDock)x);
            return res;
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
            var res = FindDockableByID(id);

            if (res.Count() > 0)
            {
                ActiveDockable(res.First());
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

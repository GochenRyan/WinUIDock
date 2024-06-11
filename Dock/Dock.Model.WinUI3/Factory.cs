using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Dock.Model.WinUI3.Core;
using Dock.Model.WinUI3.Internal;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dock.Model.WinUI3
{
    public class Factory : FactoryBase
    {
        public Factory()
        {
            VisibleDockableControls = new Dictionary<IDockable, IDockableControl>();
            PinnedDockableControls = new Dictionary<IDockable, IDockableControl>();
            TabDockableControls = new Dictionary<IDockable, IDockableControl>();
            DockControls = new ObservableCollection<IDockControl>();
            HostWindows = new ObservableCollection<IHostWindow>();
            FloatDockableCmd = new Command(FloatDockable);
            PinDockableCmd = new Command(PinDockable);
            CloseDockableCmd = new Command(CloseDockable);
            PreviewPinnedDockableCmd = new Command(PreviewPinnedDockable);
            SetActiveDockableCmd = new Command(SetActiveDockable);
        }

        public Command FloatDockableCmd { get; private set; }
        public Command PinDockableCmd {  get; private set; }
        public Command CloseDockableCmd { get; private set; }
        public Command PreviewPinnedDockableCmd { get; private set; }
        public Command SetActiveDockableCmd { get; private set; }

        public override IDictionary<IDockable, IDockableControl> VisibleDockableControls { get; }

        public override IDictionary<IDockable, IDockableControl> PinnedDockableControls { get; }

        public override IDictionary<IDockable, IDockableControl> TabDockableControls { get; }

        public override IList<IDockControl> DockControls { get; }

        public override IList<IHostWindow> HostWindows { get; }

        public override IDockDock CreateDockDock() => new DockDock();

        public override IDockWindow CreateDockWindow() => new DockWindow();

        public override IDocumentDock CreateDocumentDock() => new DocumentDock();

        public override IRootDock CreateLayout() => CreateRootDock();

        public override IList<T> CreateList<T>(params T[] items) => new List<T>(items);

        public override IProportionalDock CreateProportionalDock() => new ProportionalDock();

        public override IProportionalDockSplitter CreateProportionalDockSplitter() => new ProportionalDockSplitter();

        public override IRootDock CreateRootDock() => new RootDock();

        public override IToolDock CreateToolDock() => new ToolDock();
    }
}

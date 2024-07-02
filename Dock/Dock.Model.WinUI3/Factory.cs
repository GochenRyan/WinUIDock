using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.WinUI3.Controls;
using Dock.Model.WinUI3.Core;
using Dock.Model.WinUI3.Internal;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

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

        [JsonIgnore]
        public Command FloatDockableCmd { get; private set; }

        [JsonIgnore]
        public Command PinDockableCmd { get; private set; }

        [JsonIgnore]
        public Command CloseDockableCmd { get; private set; }

        [JsonIgnore]
        public Command PreviewPinnedDockableCmd { get; private set; }

        [JsonIgnore]
        public Command SetActiveDockableCmd { get; private set; }

        [JsonIgnore]
        public override IDictionary<IDockable, IDockableControl> VisibleDockableControls { get; }

        [JsonIgnore]
        public override IDictionary<IDockable, IDockableControl> PinnedDockableControls { get; }

        [JsonIgnore]
        public override IDictionary<IDockable, IDockableControl> TabDockableControls { get; }

        [JsonIgnore]
        public override IList<IDockControl> DockControls { get; }

        [JsonIgnore]
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

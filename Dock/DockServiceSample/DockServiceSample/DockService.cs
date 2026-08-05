using CommunityToolkit.WinUI;
using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Serializer;
using Dock.Settings;
using Dock.WinUI3;
using Dock.WinUI3.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace DockServiceSample
{
    public class DockService
    {
        public DockService()
        {
            DockControl = HostWindow.MainWindow.Content.FindDescendant<DockControl>();
            DockSettings.DockBetweenFloatWindows = true;

            RegisterDockableControls();

            m_serializer = new DockSerializer(typeof(List<>));
        }

        /// <summary>
        /// Saves the current layout and immediately loads it back — a serialization
        /// round-trip. Nothing moves on screen (what comes back is what went out);
        /// the point is to prove save/load is lossless and to re-run the content
        /// re-association path.
        ///
        /// Also the startup path: on the first call the tree is still the bare XAML
        /// skeleton, so the load is what makes <see cref="Link"/> populate the panes.
        /// That first snapshot is kept as the "default layout" for
        /// <see cref="ResetToDefault"/>.
        /// </summary>
        public void LoadDefault()
        {
            try
            {
                using (var stream = new FileStream(DefaultPath, FileMode.Create, FileAccess.Write))
                {
                    SaveLayout(stream);
                }

                // Captured once, from the very first save: at that moment the layout
                // is the empty XAML skeleton, which is exactly what "default" means —
                // reloading it makes Link() rebuild every panel from scratch.
                m_defaultLayout ??= File.ReadAllText(DefaultPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                App.Log("DockService", e);
            }

            try
            {
                using (var stream = new FileStream(DefaultPath, FileMode.Open, FileAccess.Read))
                {
                    LoadLayout(stream);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                App.Log("DockService", e);
            }
        }

        /// <summary>
        /// Restores the layout captured at startup, undoing any docking, floating or
        /// panel closing the user did since. Unlike <see cref="LoadDefault"/> this
        /// produces a visible change whenever the layout has been rearranged.
        /// </summary>
        public void ResetToDefault()
        {
            if (m_defaultLayout is null)
            {
                return;
            }

            try
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(m_defaultLayout));
                LoadLayout(stream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                App.Log("DockService", e);
            }
        }

        public void SaveLayout(Stream stream)
        {
            var layout = DockControl.Layout;
            if (layout is { })
            {
                m_serializer.Save(stream, layout);
            }
        }

        public void LoadLayout(Stream stream)
        {
            // Release the shared control instances from the OUTGOING tree first.
            //
            // Both trees are briefly alive during the swap: DeInitialize does not
            // tear down the old visual tree, and template rebuilds are asynchronous.
            // Since the resolver hands the very same UIElement to the new tree, two
            // ToolContentControls end up claiming one element and reparent it back
            // and forth every frame — the 017 watchdog logs
            // "hosts fighting over the element" and suspends itself after 10 rounds,
            // leaving the element in whatever state it was last yanked into.
            ReleaseContent(DockControl.Layout);

            m_resolvedIds.Clear();
            var layout = m_serializer.Load<IDock>(stream, ResolveDockable);
            if (layout is null)
            {
                return;
            }

            // Tear the old float windows down and let that finish BEFORE the new tree
            // builds its own.
            //
            // Assigning DockControl.Layout does both in one statement: DeInitialize
            // runs ExitWindows (whose actual Close is deferred to a Low-priority
            // callback since 017), then Initialize runs ShowWindows, which creates the
            // new windows SYNCHRONOUSLY. So new windows are constructed while the old
            // ones are still queued for closing. Creating a window touches the
            // non-client area, and that call landing inside this overlap is what turns
            // a harmless "no HWND associated with the provided WindowId" into a
            // process-killing fail-fast.
            //
            // Closing first and applying the layout from a Low-priority callback puts
            // the two phases in separate turns of the message loop: the queued closes
            // run first (FIFO at the same priority), the rebuild after.
            CloseFloatWindows(DockControl.Layout as IRootDock);

            if (DockControl.DispatcherQueue is { } queue
                && queue.TryEnqueue(DispatcherQueuePriority.Low, () => ApplyLayout(layout)))
            {
                return;
            }

            ApplyLayout(layout);
        }

        /// <summary>Closes every float window of the outgoing layout.</summary>
        private static void CloseFloatWindows(IRootDock root)
        {
            if (root?.Windows is null)
            {
                return;
            }

            foreach (var window in root.Windows.ToList())
            {
                try
                {
                    window.Exit();
                }
                catch (Exception e)
                {
                    App.Log("CloseFloatWindows", e);
                }
            }
        }

        private void ApplyLayout(IDock layout)
        {
            DockControl.Layout = layout;

            // Only windows whose host control has NOT loaded yet will ever raise
            // Loaded, so count exactly those. Counting every window in the map —
            // as this did — leaves the counter stuck above zero whenever a window
            // is already loaded or its host control cannot be found, and Link()
            // then never runs, so the panels come back empty.
            m_loadingCnt = 0;
            foreach (var window in HostWindow.windowMap.Values.ToList())
            {
                if (window.Content is not FrameworkElement windowContent
                    || windowContent.FindChild<HostWindowControl>() is not { } hostWindowControl
                    || hostWindowControl.IsLoaded)
                {
                    continue;
                }

                m_loadingCnt++;
                // Reloading must not stack subscriptions on a reused control.
                hostWindowControl.Loaded -= HostWindowControl_Loaded;
                hostWindowControl.Loaded += HostWindowControl_Loaded;
            }

            LinkRegisterControls();
        }

        private void LinkRegisterControls()
        {
            if (m_loadingCnt == 0)
            {
                Link();
            }
        }

        private void HostWindowControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // One-shot: Loaded can fire again if the control is re-parented, and a
            // second decrement would drive the counter negative.
            if (sender is FrameworkElement element)
            {
                element.Loaded -= HostWindowControl_Loaded;
            }

            m_loadingCnt--;
            LinkRegisterControls();
        }

        /// <summary>
        /// Detaches the registered controls from a tree that is about to be replaced,
        /// so the incoming tree is the only claimant of each shared UIElement.
        /// </summary>
        private void ReleaseContent(IDock dock)
        {
            if (dock is null || WinUIDockManager.GetFactory() is not { } factory)
            {
                return;
            }

            foreach (var dockable in factory.Find(dock, _ => true).ToList())
            {
                if (dockable is IToolContent toolContent)
                {
                    toolContent.Content = null;
                }
                else if (dockable is IDocumentContent documentContent)
                {
                    documentContent.Content = null;
                }
            }
        }

        /// <summary>
        /// Re-injects each registered control into the dockable that carries its id.
        /// Content is [JsonIgnore], so a loaded layout arrives with empty panels; doing
        /// this during deserialization means the tree is never observed in that state.
        /// </summary>
        private IDockable ResolveDockable(IDockable deserialized)
        {
            var id = deserialized.Id;
            if (!string.IsNullOrEmpty(id) && m_controlInfoDict.TryGetValue(id, out var info))
            {
                if (deserialized is IToolContent toolContent)
                {
                    toolContent.Content = info.Control;
                    m_resolvedIds.Add(id);
                }
                else if (deserialized is IDocumentContent documentContent)
                {
                    documentContent.Content = info.Control;
                    m_resolvedIds.Add(id);
                }
            }

            return deserialized;
        }

        /// <summary>
        /// Creates the registered controls the loaded layout did not contain. The
        /// resolver already handled everything the file did have.
        /// </summary>
        private void Link()
        {
            foreach (var pair in m_controlInfoDict)
            {
                if (m_resolvedIds.Contains(pair.Key))
                {
                    continue;
                }

                // Isolate each panel: this loop runs inside LoadDefault's single
                // try/catch, so one panel throwing would otherwise abort every panel
                // after it.
                try
                {
                    ShowUnlinkedDockableControls(pair.Key, pair.Value);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    App.Log("Link", e, $"failed to create '{pair.Value.Name}' — continuing with the rest");
                }
            }

            ReproIfRequested();
        }

        private void ShowUnlinkedDockableControls(string id, ControlInfo info)
        {
            switch (info.Group)
            {
                case StandardControlGroup.Top:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(TopPaneName);
                        if (dock != null)
                            WinUIDockManager.AddDockableTo(tool, dock);
                    }
                    break;
                case StandardControlGroup.Bottom:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(BottomPaneName);
                        if (dock != null)
                            WinUIDockManager.AddDockableTo(tool, dock);
                    }
                    break;
                case StandardControlGroup.Left:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(LeftPaneName);
                        if (dock != null)
                            WinUIDockManager.AddDockableTo(tool, dock);
                    }
                    break;
                case StandardControlGroup.Right:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(RightPaneName);
                        if (dock != null)
                            WinUIDockManager.AddDockableTo(tool, dock);
                    }
                    break;
                case StandardControlGroup.Center:
                    {
                        var document = WinUIDockManager.CreateDockable(DockableType.Document, id, info.Name, info.Control) as IDocument;
                        var dock = WinUIDockManager.FindDockByID(DocumentPaneName);
                        if (dock != null)
                            WinUIDockManager.AddDockableTo(document, dock);
                    }
                    break;
                case StandardControlGroup.CenterPermanent:
                    {
                        var document = WinUIDockManager.CreateDockable(DockableType.Document, id, info.Name, info.Control) as IDocument;
                        document.CanClose = false;
                        var dock = WinUIDockManager.FindDockByID(DocumentPaneName);
                        if (dock != null)
                            WinUIDockManager.AddDockableTo(document, dock);
                    }
                    break;
                case StandardControlGroup.LeftHidden:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(LeftPaneName);
                        if (dock != null)
                        {
                            WinUIDockManager.AddDockableTo(tool, dock);
                            WinUIDockManager.PinDockable(tool);
                        }
                    }
                    break;
                case StandardControlGroup.RightHidden:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(RightPaneName);
                        if (dock != null)
                        {
                            WinUIDockManager.AddDockableTo(tool, dock);
                            WinUIDockManager.PinDockable(tool);
                        }
                    }
                    break;
                case StandardControlGroup.TopHidden:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(TopPaneName);
                        if (dock != null)
                        {
                            WinUIDockManager.AddDockableTo(tool, dock);
                            WinUIDockManager.PinDockable(tool);
                        }
                    }
                    break;
                case StandardControlGroup.BottomHidden:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        var dock = WinUIDockManager.FindDockByID(BottomPaneName);
                        if (dock != null)
                        {
                            WinUIDockManager.AddDockableTo(tool, dock);
                            WinUIDockManager.PinDockable(tool);
                        }
                    }
                    break;
                case StandardControlGroup.Floating:
                    {
                        var tool = WinUIDockManager.CreateDockable(DockableType.Tool, id, info.Name, info.Control) as ITool;
                        // The dock argument only serves to locate the root that will
                        // own the new window, so any pane present in the layout works.
                        var dock = WinUIDockManager.FindDockByID(TopPaneName);
                        if (dock != null)
                        {
                            WinUIDockManager.SplitToWindow(dock, tool, 0, 0, 800, 600);
                        }
                    }
                    break;
            }
        }

        private void RegisterDockableControls()
        {
            var documentControl = new DocumentSampleControl1
            {
                DocumentText = "Central document area (CenterPermanent group): cannot be closed. "
                             + "Drag the surrounding tool tabs to rearrange the layout around it."
            };
            var info = new ControlInfo("document1", StandardControlGroup.CenterPermanent)
            {
                Control = documentControl
            };
            var id = GetPersistenceId(info);
            m_controlInfoDict[id] = info;

            var leftToolControl = new ToolSampleControl1
            {
                ToolText = "Left tool (Left group): a scene hierarchy would live here. "
                         + "Close it from the tab, then bring it back from the View menu."
            };
            info = new ControlInfo("left_tool", StandardControlGroup.Left)
            {
                Control = leftToolControl
            };
            id = GetPersistenceId(info);
            m_controlInfoDict[id] = info;

            var rightToolControl = new ToolSampleControl1
            {
                ToolText = "Right tool (Right group): a properties inspector would live here. "
                         + "Use the chrome pin button to auto-hide it to the edge."
            };
            info = new ControlInfo("right_tool", StandardControlGroup.Right)
            {
                Control = rightToolControl
            };
            id = GetPersistenceId(info);
            m_controlInfoDict[id] = info;

            var bottomToolControl = new ToolSampleControl1
            {
                ToolText = "Bottom tool (Bottom group): output and logs would live here. "
                         + "Layout checks report their results to this pane."
            };
            info = new ControlInfo("bottom_tool", StandardControlGroup.Bottom)
            {
                Control = bottomToolControl
            };
            id = GetPersistenceId(info);
            m_controlInfoDict[id] = info;

            // A FLOAT panel, not a window: it is torn out of the main window's
            // layout and stays part of that context. The real window is
            // SampleWindow, opened from the Sample menu.
            var floatToolControl = new ToolSampleControl1
            {
                ToolText = "Float tool (Floating group): starts in a float window of its own, "
                         + "torn out of the main layout but still part of it — drag it onto "
                         + "the main window's guides to dock it, or toggle it from the View menu."
            };
            info = new ControlInfo("float_tool", StandardControlGroup.Floating)
            {
                Control = floatToolControl
            };
            id = GetPersistenceId(info);
            m_controlInfoDict[id] = info;
        }

        public void Hide(string name)
        {
            foreach (var pair in m_controlInfoDict)
            {
                if (pair.Value.Name == name)
                {
                    var dockable = WinUIDockManager.FindDockableByID(pair.Key);
                    if (dockable != null)
                    {
                        WinUIDockManager.CloseDockable(dockable);
                    }
                }
            }
        }

        public void Show(string name)
        {
            foreach (var pair in m_controlInfoDict)
            {
                if (pair.Value.Name == name)
                {
                    // Already in the layout — nothing to show.
                    if (WinUIDockManager.FindDockableByID(pair.Key) != null)
                        return;
                    ShowUnlinkedDockableControls(pair.Key, pair.Value);
                }
            }
        }

        /// <summary>
        /// True while the registered control's dockable is somewhere in the layout —
        /// including auto-hidden or floated. Backs the View menu's check state.
        /// </summary>
        public bool IsVisible(string name)
        {
            foreach (var pair in m_controlInfoDict)
            {
                if (pair.Value.Name == name)
                {
                    return WinUIDockManager.FindDockableByID(pair.Key) != null;
                }
            }

            return false;
        }

        /// <summary>Writes to the bottom tool panel, newest line first — this sample has
        /// no console, and the panel control outlives being hidden.</summary>
        private void LogToBottom(string message)
        {
            foreach (var pair in m_controlInfoDict)
            {
                if (pair.Value.Name == "bottom_tool" && pair.Value.Control is ToolSampleControl1 tool)
                {
                    tool.ToolText = message + Environment.NewLine + tool.ToolText;
                    return;
                }
            }
        }

        /// <summary>Lists the docks parked by CollapseDock, with the spot each will
        /// return to — the visible face of the restore anchors.</summary>
        public void LogHiddenDockables()
        {
            var hidden = (DockControl.Layout as IRootDock)?.HiddenDockables;
            if (hidden is null || hidden.Count == 0)
            {
                LogToBottom("no parked dockables");
                return;
            }

            foreach (var dockable in hidden)
            {
                LogToBottom($"parked: {dockable.GetType().Name} Id='{dockable.Id}' " +
                            $"-> returns to '{dockable.RestoreOwner?.Id ?? "?"}' at {dockable.RestoreIndex}");
            }
        }

        /// <summary>Ids must be unique per factory; this reports any that are not.</summary>
        public void LogIdValidation()
        {
            var violations = WinUIDockManager.GetFactory().ValidateIds();
            if (violations.Count == 0)
            {
                LogToBottom("id check: every id is unique");
                return;
            }

            foreach (var violation in violations)
            {
                LogToBottom($"id check: '{violation.Id}' is used by {violation.Dockables.Count} dockables");
            }
        }

        /// <summary>
        /// Exercises window-edge docking on a THROWAWAY layout — the live one is
        /// never touched, so this can run at startup without moving anything.
        ///
        /// It drives the real entry point (<see cref="DockManager.ValidateDockable"/>),
        /// not the factory method, so the whole chain is covered. What it asserts is
        /// the tree SHAPE, which is where edge docking can be wrong while still
        /// looking like it worked: the region has to become a direct child of the
        /// node the root actually renders, at the correct end of it.
        /// </summary>
        public void LogRootEdgeCheck()
        {
            foreach (var line in RunRootEdgeCheck())
            {
                LogToBottom(line);
                App.Log("RootEdgeCheck", null, line);
            }
        }

        /// <summary>Same check at startup, log only — the sample's panels stay clean
        /// while every launch still leaves evidence in crash.log.</summary>
        public void CheckRootEdgeQuietly()
        {
            foreach (var line in RunRootEdgeCheck())
            {
                App.Log("RootEdgeCheck", null, line);
            }
        }

        private IEnumerable<string> RunRootEdgeCheck()
        {
            if (WinUIDockManager.GetFactory() is not { } factory)
            {
                yield return "edge check: no factory";
                yield break;
            }

            var manager = new DockManager();

            foreach (var operation in s_rootEdgeOperations)
            {
                yield return ProbeRootEdge(factory, manager, operation);
            }

            yield return ProbeLoneToolRootEdge(factory, manager);
            yield return ProbeCrossRootEdgeNodeMove(factory, Orientation.Vertical, "wrap");
            yield return ProbeCrossRootEdgeNodeMove(factory, Orientation.Horizontal, "flat");
            yield return ProbeRootWithoutDefaultDockable(factory);
            yield return ProbeToolDockWithoutActiveDockable(factory);
            yield return ProbeEmptyDocumentDockKeepsAnchor(factory, manager);
            yield return ProbeFindCoversUnloadedFloatWindow(factory);
        }

        /// <summary>Stand-in for a dock control whose window has not loaded yet —
        /// carries a layout, nothing else.</summary>
        private sealed class ProbeDockControl : IDockControl
        {
            public IDockManager DockManager => null;
            public IDockControlState DockControlState => null;
            public IDock Layout { get; set; }
            public object DefaultContext { get; set; }
            public bool InitializeLayout { get; set; }
            public bool InitializeFactory { get; set; }
            public IFactory Factory { get; set; }
        }

        /// <summary>
        /// A float window's OWN dock control only registers when it loads, so for a
        /// beat after SplitToWindow the floated dockable exists in the model but
        /// under no registered layout — and an id lookup in that window reported it
        /// missing (the View menu unticked a panel that was visibly on screen).
        /// Find must reach dockables through IRootDock.Windows for exactly those
        /// not-yet-registered layouts.
        /// </summary>
        private static string ProbeFindCoversUnloadedFloatWindow(IFactory factory)
        {
            var tool = WinUIDockManager.CreateDockable(DockableType.Tool, "probe_unloaded_float_tool", "probe", null);

            var floatPane = factory.CreateToolDock();
            floatPane.Id = "ProbeUnloadedFloatPane";
            floatPane.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList(tool));

            var floatRoot = factory.CreateRootDock();
            floatRoot.Id = "ProbeUnloadedFloatRoot";
            floatRoot.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(floatPane));
            floatRoot.DefaultDockable = floatPane;

            var window = factory.CreateDockWindow();
            window.Id = "ProbeUnloadedFloatWindow";
            window.Layout = floatRoot;

            var mainRoot = factory.CreateRootDock();
            mainRoot.Id = "ProbeUnloadedMainRoot";
            mainRoot.VisibleDockables = new ObservableCollection<IDockable>(
                factory.CreateList<IDockable>(factory.CreateToolDock()));
            mainRoot.Windows = new ObservableCollection<IDockWindow> { window };

            // The main layout is registered; the float window's control is NOT —
            // exactly the state right after SplitToWindow returns.
            var stub = new ProbeDockControl { Layout = mainRoot };
            factory.DockControls.Add(stub);

            try
            {
                var found = factory.Find(d => ReferenceEquals(d, tool)).Any();
                return found
                    ? "unloaded float lookup: PASS"
                    : "unloaded float lookup: FAIL — the floated tool is invisible to Find";
            }
            finally
            {
                factory.DockControls.Remove(stub);
            }
        }

        /// <summary>
        /// VS document-well semantics (D22): moving the LAST document out of an
        /// IsCollapsable=False pane must leave the pane in the layout, empty, and
        /// still able to take the document back by Fill. Exercises the whole chain:
        /// cross-owner move → RemoveDockable(collapse) → the CollapseDock guard,
        /// then Fill into a dock with no ActiveDockable and no children.
        /// </summary>
        private static string ProbeEmptyDocumentDockKeepsAnchor(IFactory factory, IDockManager manager)
        {
            var document = WinUIDockManager.CreateDockable(DockableType.Document, "probe_anchor_doc", "probe", null);
            var other = WinUIDockManager.CreateDockable(DockableType.Document, "probe_anchor_other", "other", null);

            var home = factory.CreateDocumentDock();
            home.Id = "ProbeAnchorHome";
            home.IsCollapsable = false;
            home.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(document));

            var away = factory.CreateDocumentDock();
            away.Id = "ProbeAnchorAway";
            away.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(other));

            var layout = factory.CreateProportionalDock();
            layout.Id = "ProbeAnchorLayout";
            layout.Orientation = Orientation.Horizontal;
            layout.VisibleDockables = new ObservableCollection<IDockable>(
                factory.CreateList<IDockable>(home, factory.CreateProportionalDockSplitter(), away));

            var root = factory.CreateRootDock();
            root.Id = "ProbeAnchorRoot";
            root.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(layout));
            root.DefaultDockable = layout;

            factory.InitDockable(root, null);

            // The same move a drag onto the other pane makes.
            if (!manager.ValidateDockable(document, away, DragAction.Move, DockOperation.Fill, true))
            {
                return "document anchor: FAIL — moving the last document out was refused";
            }

            if (!factory.Find(root, d => ReferenceEquals(d, home)).Any())
            {
                return "document anchor: FAIL — the emptied pane collapsed away";
            }

            if (home.VisibleDockables?.Count != 0)
            {
                return "document anchor: FAIL — the emptied pane still lists something";
            }

            // And back into the EMPTY pane.
            if (!manager.ValidateDockable(document, home, DragAction.Move, DockOperation.Fill, true))
            {
                return "document anchor: FAIL — fill into the empty pane was refused";
            }

            return home.VisibleDockables?.Contains(document) == true
                ? "document anchor: PASS"
                : "document anchor: FAIL — the document did not land back home";
        }

        /// <summary>
        /// A tabbed dock with tabs but no ActiveDockable draws an empty chrome
        /// while its tab strip still lists everything — the pane-level sibling of
        /// the root's missing DefaultDockable. Same source too: a layout file only
        /// carries the property if it was set when the file was written.
        /// </summary>
        private static string ProbeToolDockWithoutActiveDockable(IFactory factory)
        {
            var tool = WinUIDockManager.CreateDockable(DockableType.Tool, "probe_no_active_tool", "probe", null);

            var pane = factory.CreateToolDock();
            pane.Id = "ProbeNoActivePane";
            pane.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList(tool));
            pane.ActiveDockable = null;

            var layout = factory.CreateProportionalDock();
            layout.Id = "ProbeNoActiveLayout";
            layout.Orientation = Orientation.Vertical;
            layout.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(pane));

            var root = factory.CreateRootDock();
            root.Id = "ProbeNoActiveRoot";
            root.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(layout));
            root.DefaultDockable = layout;

            factory.InitLayout(root);

            return ReferenceEquals(pane.ActiveDockable, tool)
                ? "edge check no-active: PASS"
                : "edge check no-active: FAIL — the pane still has no active tab";
        }

        /// <summary>
        /// A root whose DefaultDockable is missing must still render. Deserialized
        /// layouts are the usual source: the property only reaches the file if it
        /// was set when the file was written, and the symptom — an empty window —
        /// says nothing about the tree underneath being perfectly intact.
        /// </summary>
        private static string ProbeRootWithoutDefaultDockable(IFactory factory)
        {
            var layout = factory.CreateProportionalDock();
            layout.Id = "ProbeNoDefaultLayout";
            layout.Orientation = Orientation.Vertical;
            layout.VisibleDockables = new ObservableCollection<IDockable>(
                factory.CreateList<IDockable>(factory.CreateToolDock()));

            var root = factory.CreateRootDock();
            root.Id = "ProbeNoDefaultRoot";
            root.VisibleDockables = new ObservableCollection<IDockable>(
                factory.CreateList<IDockable>(layout));

            // Deliberately not set — this is the state a layout file without the
            // property deserializes into.
            root.DefaultDockable = null;

            factory.InitLayout(root);

            return ReferenceEquals(root.DefaultDockable, layout)
                ? "edge check no-default: PASS"
                : "edge check no-default: FAIL — root still has nothing to render";
        }

        /// <summary>
        /// Opt-in (DOCKSAMPLE_REPRO=1) because it rearranges the LIVE layout. Hooked
        /// to the end of Link() rather than to startup: LoadDefault applies the layout
        /// from a deferred callback (035), so at Dock_Loaded time the panes are still
        /// empty and the repro finds nothing to move.
        /// </summary>
        private void ReproIfRequested()
        {
            if (Environment.GetEnvironmentVariable("DOCKSAMPLE_REPRO") != "1")
            {
                return;
            }

            foreach (var line in ReproCrossRootEdgeOnLiveLayout())
            {
                App.Log("RootEdgeCheck", null, line);
            }
        }

        /// <summary>
        /// Reproduces the 053 accident on the LIVE layout rather than a throwaway
        /// tree: a real float window (with a real host), the real factory, the real
        /// root. The synthetic probes above pass, so whatever goes wrong must live in
        /// one of the differences — this removes the guesswork about which.
        ///
        /// Float a tool, pull a second tool in beside it, then drop that whole float
        /// dock on the main window's right edge, exactly as the user did.
        /// </summary>
        private IEnumerable<string> ReproCrossRootEdgeOnLiveLayout()
        {
            if (DockControl.Layout is not IRootDock root || root.Factory is not { } factory)
            {
                yield return "repro: no live layout";
                yield break;
            }

            var manager = new DockManager();

            var tools = factory.Find(root, dockable => dockable is ITool).OfType<ITool>().ToList();
            if (tools.Count < 3)
            {
                yield return $"repro: needs 3 tools, live layout has {tools.Count}";
                yield break;
            }

            var floated = tools[0];
            var companion = tools[1];
            var dropTarget = tools[2];

            yield return $"repro: BEFORE {DockDiagnostics.DescribeTree(root)}";

            // NOT the root: SplitToWindow resolves the root via FindRoot, which walks
            // Owner UPWARD — handed the root itself it finds null and returns without
            // doing anything at all.
            if (floated.Owner is not IDock floatSourceOwner)
            {
                yield return "repro: FAIL — tool has no owner";
                yield break;
            }

            factory.SplitToWindow(floatSourceOwner, floated, 120, 120, 420, 420);

            if (floated.Owner is not IDock floatDock)
            {
                yield return "repro: FAIL — floated tool has no owner dock";
                yield break;
            }

            manager.ValidateDockable(companion, floatDock, DragAction.Move, DockOperation.Fill, true);

            yield return $"repro: floated {DockDiagnostics.Describe(floatDock)} "
                         + $"tools={floatDock.VisibleDockables?.Count} "
                         + $"root={DockDiagnostics.DescribeTree(root)}";

            // The live float window has not been arranged yet at this point, so its
            // dock still carries NaN. On screen it would carry 1.0 — force that, or
            // the repro exercises a state the user never encounters.
            floatDock.Proportion = 1.0;

            var accepted = manager.ValidateDockable(floatDock, dropTarget, DragAction.Move, DockOperation.RootRight, true);

            yield return $"repro: edge drop returned {accepted}";
            yield return $"repro: AFTER {DockDiagnostics.DescribeTree(root)}";

            var reachable = factory.Find(root, _ => true).ToList();
            var lost = tools.Where(tool => !reachable.Contains(tool)).Select(tool => tool.Id ?? tool.Title).ToList();

            yield return lost.Count == 0
                ? "repro: all tools still reachable"
                : $"repro: LOST {string.Join(", ", lost)}";

            yield return floatDock.Proportion > 0 && floatDock.Proportion < 1.0
                ? $"repro: moved dock proportion {floatDock.Proportion:0.###} (leaves room for the rest)"
                : $"repro: FAIL — moved dock proportion {floatDock.Proportion:0.###} hides everything else";
        }

        private static string ProbeCrossRootEdgeNodeMove(IFactory factory, Orientation mainOrientation, string branch)
        {
            // Target: mainRoot -> mainLayout(Vertical) -> [ topPane , splitter , body ]
            var topTool = WinUIDockManager.CreateDockable(DockableType.Tool, "probe_x_top", "probe", null);
            var bodyTool = WinUIDockManager.CreateDockable(DockableType.Tool, "probe_x_body", "probe", null);

            var topPane = factory.CreateToolDock();
            topPane.Id = "ProbeXTop";
            topPane.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList(topTool));

            var body = factory.CreateToolDock();
            body.Id = "ProbeXBody";
            body.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList(bodyTool));

            var mainLayout = factory.CreateProportionalDock();
            mainLayout.Id = "ProbeXMainLayout";
            // Vertical vs RootRight takes SplitToRootEdge's wrap branch; Horizontal
            // takes the flat-insert branch. Both must survive a cross-root node move.
            mainLayout.Orientation = mainOrientation;
            mainLayout.VisibleDockables = new ObservableCollection<IDockable>(
                factory.CreateList<IDockable>(topPane, factory.CreateProportionalDockSplitter(), body));

            var mainRoot = factory.CreateRootDock();
            mainRoot.Id = "ProbeXMainRoot";
            mainRoot.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(mainLayout));
            mainRoot.DefaultDockable = mainLayout;
            factory.InitDockable(mainRoot, null);

            // Source: floatRoot -> floatLayout -> [ floatDock(2 tools) ], with a window.
            var toolA = WinUIDockManager.CreateDockable(DockableType.Tool, "probe_x_a", "probe", null);
            var toolB = WinUIDockManager.CreateDockable(DockableType.Tool, "probe_x_b", "probe", null);

            var floatDock = factory.CreateToolDock();
            floatDock.Id = "ProbeXFloatDock";
            floatDock.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList(toolA, toolB));

            var floatLayout = factory.CreateProportionalDock();
            floatLayout.Id = "ProbeXFloatLayout";
            floatLayout.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(floatDock));

            var floatRoot = factory.CreateRootDock();
            floatRoot.Id = "ProbeXFloatRoot";
            floatRoot.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(floatLayout));
            floatRoot.DefaultDockable = floatLayout;
            factory.InitDockable(floatRoot, null);

            // A float window's layout has exactly one child, and
            // ProportionalStackPanel writes 1.0 back into the model for the lone
            // child of a container — so any float dock that has been on screen
            // arrives carrying 1.0. Reproduce that state explicitly; without it the
            // probe tests a shape the user never sees.
            floatDock.Proportion = 1.0;

            factory.RemoveDockable(floatDock, true);
            factory.SplitToRootEdge(mainRoot, floatDock, DockOperation.RootRight);

            var reachable = factory.Find(mainRoot, _ => true).ToList();
            var missing = new List<string>();

            if (!reachable.Contains(mainLayout)) missing.Add("mainLayout");
            if (!reachable.Contains(topPane)) missing.Add("topPane");
            if (!reachable.Contains(body)) missing.Add("body");
            if (!reachable.Contains(topTool)) missing.Add("topTool");
            if (!reachable.Contains(bodyTool)) missing.Add("bodyTool");
            if (!reachable.Contains(floatDock)) missing.Add("floatDock");

            if (missing.Count > 0)
            {
                return $"cross-root edge check ({branch}): FAIL — lost {string.Join(", ", missing)} "
                       + $"(root children now: {DescribeChildren(mainRoot)}; default={mainRoot.DefaultDockable?.Id})";
            }

            if (mainRoot.DefaultDockable is not IDock rendered
                || mainRoot.VisibleDockables?.Contains(rendered) != true)
            {
                return $"cross-root edge check ({branch}): FAIL — DefaultDockable is not a rendered root child";
            }

            // Reachable is not enough — a pane squeezed to zero width is still in the
            // tree, and that is what "every panel disappeared" actually looks like.
            if (!(floatDock.Proportion > 0 && floatDock.Proportion < 1.0))
            {
                return $"cross-root edge check ({branch}): FAIL — moved dock takes "
                       + $"{floatDock.Proportion:0.###} of the container, hiding everything else";
            }

            return $"cross-root edge check ({branch}): PASS";
        }

        private static string DescribeChildren(IDock dock)
        {
            if (dock.VisibleDockables is not { } children)
            {
                return "<null>";
            }

            return string.Join("|", children.Select(c => $"{c.GetType().Name}:{c.Id}"));
        }

        /// <summary>
        /// The degenerate case: a root holding exactly ONE tool still shows all four
        /// edge guides (a single-tool float window is the everyday example).
        /// Dropping there empties the source dock, which collapses, which empties its
        /// container, which collapses too — so by the time the insert looks for a
        /// layout to sit next to, there is none left, and the tool has already been
        /// moved out. Asserts the tool is still reachable afterwards.
        /// </summary>
        private static string ProbeLoneToolRootEdge(IFactory factory, IDockManager manager)
        {
            var probeTool = WinUIDockManager.CreateDockable(DockableType.Tool, "probe_lone_tool", "probe", null);

            var source = factory.CreateToolDock();
            source.Id = "ProbeLoneSource";
            source.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList(probeTool));

            var layout = factory.CreateProportionalDock();
            layout.Id = "ProbeLoneLayout";
            layout.Orientation = Orientation.Vertical;
            layout.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(source));

            var root = factory.CreateRootDock();
            root.Id = "ProbeLoneRoot";
            root.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(layout));
            root.DefaultDockable = layout;

            factory.InitDockable(root, null);

            manager.ValidateDockable(probeTool, source, DragAction.Move, DockOperation.RootLeft, true);

            // Find covers VisibleDockables recursively plus the pinned and hidden
            // collections, so "not found" really does mean detached from the tree.
            var reachable = factory.Find(root, dockable => ReferenceEquals(dockable, probeTool)).Any();

            return reachable
                ? "edge check lone-tool: PASS"
                : "edge check lone-tool: FAIL — the root's only tool was lost";
        }

        private static string ProbeRootEdge(IFactory factory, IDockManager manager, DockOperation operation)
        {
            //  root -> layout (Vertical) -> [ body , splitter , source ]
            //
            // Vertical on purpose: Top/Bottom then match the layout's axis and must
            // insert FLAT into it, while Left/Right must wrap it in a new horizontal
            // container. Both branches get covered by the same probe.
            var probeTool = WinUIDockManager.CreateDockable(DockableType.Tool, "probe_edge_tool", "probe", null);

            var source = factory.CreateToolDock();
            source.Id = "ProbeSource";
            source.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList(probeTool));

            var body = factory.CreateToolDock();
            body.Id = "ProbeBody";
            body.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>());

            var splitter = factory.CreateProportionalDockSplitter();

            var layout = factory.CreateProportionalDock();
            layout.Id = "ProbeLayout";
            layout.Orientation = Orientation.Vertical;
            layout.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(body, splitter, source));

            var root = factory.CreateRootDock();
            root.Id = "ProbeRoot";
            root.VisibleDockables = new ObservableCollection<IDockable>(factory.CreateList<IDockable>(layout));
            root.DefaultDockable = layout;

            // Wires Owner/Factory all the way down. InitLayout would also run
            // ShowWindows, which a throwaway tree has no business doing.
            factory.InitDockable(root, null);

            if (!manager.ValidateDockable(probeTool, body, DragAction.Move, operation, true))
            {
                return $"edge check {operation}: FAIL — refused";
            }

            // What RootDockControl binds its content to. A tree that is correct but
            // hangs off a stale DefaultDockable never reaches the screen.
            if (root.DefaultDockable is not IDock rendered
                || root.VisibleDockables?.Contains(rendered) != true)
            {
                return $"edge check {operation}: FAIL — DefaultDockable is not the rendered root child";
            }

            var expectedOrientation = operation is DockOperation.RootLeft or DockOperation.RootRight
                ? Orientation.Horizontal
                : Orientation.Vertical;

            if (rendered is not IProportionalDock proportional || proportional.Orientation != expectedOrientation)
            {
                return $"edge check {operation}: FAIL — rendered node is not a {expectedOrientation} proportional dock";
            }

            if (proportional.VisibleDockables is not { Count: > 0 } children)
            {
                return $"edge check {operation}: FAIL — rendered node is empty";
            }

            // The edge region spans the whole window only by being a DIRECT child of
            // the rendered node, at the end the operation names.
            var atStart = operation is DockOperation.RootLeft or DockOperation.RootTop;
            var edge = atStart ? children[0] : children[children.Count - 1];

            if (edge is not IDock edgeDock || edgeDock.VisibleDockables?.Contains(probeTool) != true)
            {
                return $"edge check {operation}: FAIL — dropped tool is not in the {(atStart ? "first" : "last")} region";
            }

            if (source.VisibleDockables?.Contains(probeTool) == true)
            {
                return $"edge check {operation}: FAIL — tool still in its old dock";
            }

            return $"edge check {operation}: PASS";
        }

        private static readonly DockOperation[] s_rootEdgeOperations =
        {
            DockOperation.RootLeft, DockOperation.RootRight, DockOperation.RootTop, DockOperation.RootBottom
        };

        private string GetPersistenceId(ControlInfo info)
        {
            // first, try Name            
            string name = info.Name;

            // don't use name as a part of id if it is too long 
            bool usedefault
                = string.IsNullOrEmpty(name)
                || name.Length > 64
                || name.IndexOfAny(s_pathDelimiters) > 0
                || name.Contains(".");

            if (usedefault)
                name = "document_panel";

            string id = info.Control.GetType().Name + "_" + name;
            id = m_idNamer.Name(id);
            return id;
        }

        public readonly string DefaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample.json");

        private readonly Dictionary<string, ControlInfo> m_controlInfoDict = new();

        public DockControl DockControl { get; set; }

        private readonly IDockSerializer m_serializer;
        private readonly HashSet<string> m_resolvedIds = new();
        private readonly UniqueNamer m_idNamer = new();
        private static readonly char[] s_pathDelimiters = new[] { '/', '\\' };

        private const string TopPaneName = "TopPane";
        private const string BottomPaneName = "BottomPane";
        private const string LeftPaneName = "LeftPane";
        private const string RightPaneName = "RightPane";
        private const string DocumentPaneName = "DocumentPane";

        private string m_defaultLayout;
        private int m_loadingCnt = 0;
    }
}

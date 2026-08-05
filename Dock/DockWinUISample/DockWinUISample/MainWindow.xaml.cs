using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Serializer;
using Dock.WinUI3;
using Dock.WinUI3.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT;
using WinUIEx;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DockWinUISample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Registers the content root AND the OS title bar for theming.
            DockThemeManager.RegisterWindow(this);
            // The black theme is the primary look; opt into it regardless of the
            // OS light/dark setting. Remove this to follow the OS theme instead.
            DockThemeManager.SetTheme(ElementTheme.Dark);

            _serializer = new DockSerializer(typeof(List<>));

            // The XAML sets these too, but x:Bind's first pass runs after the
            // constructor body — and the snapshot below happens here. Left to the
            // bindings, "the default layout" would be captured with no
            // DefaultDockable and no active tab anywhere: resetting to it would
            // render an empty window (the former) or empty panes (the latter).
            Root.DefaultDockable = Body;
            LeftPane.ActiveDockable = Outliner;
            DocumentsPane.ActiveDockable = Document1;
            BottomPane.ActiveDockable = Output;
            RightPane.ActiveDockable = Properties;

            // Index the XAML-declared dockables by Id so a loaded layout can adopt
            // them instead of replacing them. That restores their [JsonIgnore]
            // Content and, unlike a content-only cache, keeps object identity too.
            if (Dock?.Layout is { } layout)
            {
                _declaredRoot = layout;
                IndexDeclared(layout);

                // Snapshot the XAML-declared layout so "Reset to default" has
                // something to go back to after the user rearranges everything.
                _defaultLayout = _serializer.Serialize(layout);
            }

            // The factory only exists once the dock control has loaded.
            Dock.Loaded += (_, _) =>
            {
                HookFactoryEvents();

                // Opt-in rather than automatic: the check opens and closes a real
                // window, which is not something every launch should do.
                if (Environment.GetEnvironmentVariable("DOCKSAMPLE_CONTEXTCHECK") == "1")
                {
                    CheckEditorContexts_Click(this, null);
                }

                // Repro hook: float a panel, then reset the layout while its float
                // window is still open — the user-reported crash. "1" resets one
                // tick later (window not yet presented); "2" waits until the float
                // window has fully presented and settled, which is what a real hand
                // reaching the menu does — the two states fail differently.
                var resetRepro = Environment.GetEnvironmentVariable("DOCKSAMPLE_RESETREPRO");
                if (resetRepro is "1" or "2")
                {
                    FloatPanel_Click(this, null);

                    void RunReset()
                    {
                        App.Log("ResetRepro: resetting layout with a float window open");
                        ResetLayout_Click(this, null);
                        App.Log("ResetRepro: reset returned without throwing");
                    }

                    if (resetRepro == "1")
                    {
                        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, RunReset);
                    }
                    else
                    {
                        // Field-held: a local DispatcherQueueTimer is garbage once
                        // the handler returns, and a collected timer never ticks.
                        _reproTimer = DispatcherQueue.CreateTimer();
                        _reproTimer.Interval = TimeSpan.FromSeconds(2);
                        _reproTimer.IsRepeating = false;
                        _reproTimer.Tick += (_, _) => RunReset();
                        _reproTimer.Start();
                    }
                }
                else if (resetRepro == "3")
                {
                    // Pin a panel, then float it out of the pin flyout — the same
                    // model transition a drag out of the flyout performs.
                    PinPanel_Click(this, null);
                    _reproTimer = DispatcherQueue.CreateTimer();
                    _reproTimer.Interval = TimeSpan.FromSeconds(2);
                    _reproTimer.IsRepeating = false;
                    _reproTimer.Tick += (_, _) =>
                    {
                        App.Log("PinFloatRepro: floating the pinned panel");
                        FloatPanel_Click(this, null);
                        App.Log("PinFloatRepro: float returned without throwing");
                    };
                    _reproTimer.Start();
                }
                else if (resetRepro == "4")
                {
                    // The user's exact flow, minus the pointer gesture: pin →
                    // OPEN the flyout (PreviewPinnedDockable is what the tab
                    // click calls) → run the drag's Window drop through the real
                    // DockManager while the flyout is showing the panel.
                    PinPanel_Click(this, null);
                    _reproTimer = DispatcherQueue.CreateTimer();
                    _reproTimer.Interval = TimeSpan.FromSeconds(2);
                    _reproTimer.IsRepeating = false;
                    _reproTimer.Tick += (_, _) =>
                    {
                        if (Declared("Outliner") is not { } panel || Factory is not { } factory)
                        {
                            return;
                        }

                        App.Log("PinPreviewRepro: opening the pin flyout");
                        factory.PreviewPinnedDockable(panel);

                        _reproTimer = DispatcherQueue.CreateTimer();
                        _reproTimer.Interval = TimeSpan.FromSeconds(2);
                        _reproTimer.IsRepeating = false;
                        _reproTimer.Tick += (_, _) =>
                        {
                            App.Log("PinPreviewRepro: dropping to Window from the open flyout");
                            var manager = Dock.DockManager;
                            manager.ScreenPosition = new global::Dock.Model.Core.DockPoint(500, 300);
                            var ok = manager.ValidateDockable(panel, Root, DragAction.Move, DockOperation.Window, true);
                            App.Log($"PinPreviewRepro: window drop returned {ok} without throwing");
                        };
                        _reproTimer.Start();
                    };
                    _reproTimer.Start();
                }
            };
        }

        private void IndexDeclared(IDockable dockable)
        {
            if (!string.IsNullOrEmpty(dockable.Id))
            {
                _declared[dockable.Id] = dockable;
            }

            if (dockable is IDock dock && dock.VisibleDockables is { } children)
            {
                foreach (var child in children)
                {
                    IndexDeclared(child);
                }
            }
        }

        private IDockable ResolveDockable(IDockable deserialized)
        {
            if (string.IsNullOrEmpty(deserialized.Id)
                || !_declared.TryGetValue(deserialized.Id, out var live)
                || ReferenceEquals(live, deserialized))
            {
                return deserialized;
            }

            // Never adopt the root: it is the very object the DockControl already
            // hosts, so assigning the result back would be a no-op and InitLayout
            // would not run, leaving the adopted children owned by the old tree.
            if (ReferenceEquals(live, _declaredRoot))
            {
                return deserialized;
            }

            // Structure comes from the file; identity stays with the XAML instance.
            if (deserialized is IDock loaded && live is IDock target)
            {
                DockableResolution.TransplantStructure(loaded, target);
            }

            return live;
        }


        private void ThemeDark_Click(object sender, RoutedEventArgs e)
        {
            DockThemeManager.SetTheme(ElementTheme.Dark);
        }

        private void ThemeLight_Click(object sender, RoutedEventArgs e)
        {
            DockThemeManager.SetTheme(ElementTheme.Light);
        }

        private void AcrylicToggle_Click(object sender, RoutedEventArgs e)
        {
            DockThemeManager.SetAcrylicEnabled(AcrylicToggleItem.IsChecked);
        }

        private void BackdropNone_Click(object sender, RoutedEventArgs e)
        {
            ApplyBackdrop(DockBackdrop.None);
        }

        private void BackdropMica_Click(object sender, RoutedEventArgs e)
        {
            ApplyBackdrop(DockBackdrop.Mica);
        }

        private void BackdropAcrylic_Click(object sender, RoutedEventArgs e)
        {
            ApplyBackdrop(DockBackdrop.Acrylic);
        }

        private void ApplyBackdrop(DockBackdrop backdrop)
        {
            // Main window now; float windows created later pick up the default.
            DockThemeManager.DefaultBackdrop = backdrop;
            DockThemeManager.SetBackdrop(this, backdrop);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await SaveLayout();
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            await OpenLayout();
        }

        private async Task SaveLayout()
        {

            // Create a file picker
            FileSavePicker savePicker = new FileSavePicker();

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(HostWindow.MainWindow);

            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

            // Set options for your file picker
            savePicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Json", new List<string>() { ".json" });

            // Open the picker for the user to pick a file
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    using (var stream = await file.OpenStreamForWriteAsync())
                    {

                        var dock = Dock;
                        if (dock?.Layout is { })
                        {
                            _serializer.Save(stream, dock.Layout);
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private async Task OpenLayout()
        {
            // Create a file picker
            var openPicker = new FileOpenPicker();

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(HostWindow.MainWindow);

            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your file picker
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.FileTypeFilter.Add(".json");

            // Open the picker for the user to pick a file
            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    using (var stream = await file.OpenStreamForReadAsync())
                    {
                        var layout = _serializer.Load<IDock>(stream, ResolveDockable);
                        if (layout is { })
                        {
                            Dock.Layout = layout;
                            SyncViewMenu();
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        // ---- Sample menu: each handler exercises one dock feature end to end ----

        private IFactory? Factory => Dock?.Factory;

        /// <summary>Writes to the Output panel, newest line first. The panel object
        /// outlives being closed, so this stays valid even while it is not shown.</summary>
        private void Log(string message)
        {
            OutputText.Text = message + Environment.NewLine + OutputText.Text;
        }

        private IDockable? Declared(string id)
            => _declared.TryGetValue(id, out var d) ? d : null;

        /// <summary>
        /// A panel counts as open while it is anywhere in the layout — including
        /// auto-hidden (pinned to an edge) or sitting in a float window. Since 022
        /// the factory's lookup covers the pinned collections, so a single id lookup
        /// answers all of those cases; checking VisibleDockables alone would report
        /// an auto-hidden panel as closed.
        /// </summary>
        private bool IsOpen(string id) => Factory?.FindDockableById(id) is not null;

        private IEnumerable<ToggleMenuFlyoutItem> ViewMenuItems
            => ViewMenu.Items.OfType<ToggleMenuFlyoutItem>();

        /// <summary>
        /// Mirrors the real layout into the View menu. Driven by the factory's own
        /// events, so closing a panel from its tab button — not just from this menu —
        /// unticks it too.
        /// </summary>
        private void SyncViewMenu()
        {
            foreach (var item in ViewMenuItems)
            {
                if (item.Tag is string id)
                {
                    item.IsChecked = IsOpen(id);
                }
            }
        }

        private void HookFactoryEvents()
        {
            if (Factory is not { } factory)
            {
                return;
            }

            void Refresh(object? sender, object e) => SyncViewMenu();

            factory.DockableAdded += Refresh;
            factory.DockableRemoved += Refresh;
            factory.DockableClosed += Refresh;
            factory.DockablePinned += Refresh;
            factory.DockableUnpinned += Refresh;
            // Float windows change what "is open" without touching any dockable
            // event this menu listens to — closing one via its caption X, most
            // visibly. Window events keep the ticks honest.
            factory.WindowOpened += Refresh;
            factory.WindowClosed += Refresh;

            SyncViewMenu();
        }

        private void TogglePanel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleMenuFlyoutItem item
                || item.Tag is not string id
                || Declared(id) is not { } panel
                || Factory is not { } factory)
            {
                return;
            }

            if (IsOpen(id))
            {
                _lastClosed = panel;
                factory.CloseDockable(panel);
                Log($"closed '{id}'");
            }
            else if (factory.RestoreDockable(panel))
            {
                Log($"restored '{id}' to its original spot");
            }
            else
            {
                Log($"'{id}' has no anchor to return to");
            }

            // The toggle flipped itself on click; put it back in line with reality
            // (the operation may have been refused, e.g. CanClose=false).
            SyncViewMenu();
        }

        private void FloatPanel_Click(object sender, RoutedEventArgs e)
        {
            if (Declared("Outliner") is { } panel && Factory is { } factory)
            {
                factory.FloatDockable(panel);
                Log("floated 'Outliner' into its own window");
            }
        }

        private void PinPanel_Click(object sender, RoutedEventArgs e)
        {
            if (Declared("Outliner") is { } panel && Factory is { } factory)
            {
                factory.PinDockable(panel);
                Log("pinned 'Outliner' — it is now a tab on the window edge");
            }
        }

        private void CloseLeftPane_Click(object sender, RoutedEventArgs e)
        {
            if (Factory is not { } factory || LeftPane.VisibleDockables is not { } tools)
            {
                return;
            }

            // Closing the last one empties the pane, so the pane itself collapses
            // out of the layout — and gets parked with its position remembered.
            foreach (var tool in tools.ToList())
            {
                _lastClosed = tool;
                factory.CloseDockable(tool);
            }

            var parked = (Dock?.Layout as IRootDock)?.HiddenDockables?.Contains(LeftPane) == true;
            Log($"closed every tool in the left pane; pane parked = {parked}");
        }

        private void RestoreLast_Click(object sender, RoutedEventArgs e)
        {
            if (_lastClosed is not { } panel || Factory is not { } factory)
            {
                Log("nothing was closed yet");
                return;
            }

            // Restores recursively: if the pane collapsed away when this panel
            // closed, the pane comes back first, then the panel goes inside it.
            Log(factory.RestoreDockable(panel)
                ? $"restored '{panel.Id}' (and its pane, if it had collapsed)"
                : $"'{panel.Id}' has no anchor to return to");

            _lastClosed = null;
        }

        private void ListHidden_Click(object sender, RoutedEventArgs e)
        {
            var hidden = (Dock?.Layout as IRootDock)?.HiddenDockables;
            if (hidden is null || hidden.Count == 0)
            {
                Log("no parked dockables");
                return;
            }

            foreach (var d in hidden)
            {
                Log($"parked: {d.GetType().Name} Id='{d.Id}' Kind='{d.Kind}' -> returns to '{(d.RestoreOwner?.Id ?? "?")}' at {d.RestoreIndex}");
            }
        }

        private void NewDocument_Click(object sender, RoutedEventArgs e)
        {
            if (Factory is not { } factory)
            {
                return;
            }

            var n = ++_documentSeq;
            var document = new Dock.Model.WinUI3.Controls.Document
            {
                Id = $"SampleDocument{n}",       // unique instance identity
                Title = $"Untitled{n}.cs",
                Content = new TextBlock { Margin = new Thickness(8), Text = $"New document {n}" }
            };

            factory.AddDockable(DocumentsPane, document);
            factory.SetActiveDockable(document);
            Log($"added document '{document.Id}'");
        }

        private void ValidateIds_Click(object sender, RoutedEventArgs e)
        {
            if (Factory is not { } factory)
            {
                return;
            }

            var violations = factory.ValidateIds();
            if (violations.Count == 0)
            {
                Log("id check: every id is unique");
                return;
            }

            foreach (var v in violations)
            {
                Log($"id check: '{v.Id}' is used by {v.Dockables.Count} dockables");
            }
        }

        private void ResetLayout_Click(object sender, RoutedEventArgs e)
        {
            if (_defaultLayout is null)
            {
                return;
            }

            var layout = _serializer.Deserialize<IDock>(_defaultLayout);
            if (DockableResolution.Apply(layout, ResolveDockable) is IDock restored)
            {
                Dock.Layout = restored;
                SyncViewMenu();
                Log("layout reset to the XAML-declared default");
            }
        }

        // ---- Editors menu: one asset-editor window per context ----

        private EditorWindow OpenEditor(EditorKind kind)
        {
            // Owned by this window, so it closes when the application does — and
            // so does anything torn off it.
            var editor = new EditorWindow(kind, this);
            editor.Activate();
            Log($"opened the {kind} editor in a context of its own");
            return editor;
        }

        private void OpenAnimationEditor_Click(object sender, RoutedEventArgs e)
            => OpenEditor(EditorKind.Animation);

        private void OpenShaderGraph_Click(object sender, RoutedEventArgs e)
            => OpenEditor(EditorKind.ShaderGraph);

        private void ResetEditorTemplates_Click(object sender, RoutedEventArgs e)
        {
            EditorWindow.ResetTemplates();
            Log("deleted the per-kind editor layout templates");
        }

        private void CheckEditorContexts_Click(object sender, RoutedEventArgs e)
        {
            var editor = OpenEditor(EditorKind.Animation);

            if (editor.DockControl.IsLoaded)
            {
                RunEditorContextChecks(editor);
                return;
            }

            void OnLoaded(object s, RoutedEventArgs args)
            {
                editor.DockControl.Loaded -= OnLoaded;
                RunEditorContextChecks(editor);
            }

            editor.DockControl.Loaded += OnLoaded;
        }

        /// <summary>
        /// Asserts the two properties that make an editor window a context rather
        /// than just another window: its factory is private to it (which is what
        /// isolates drag and drop), and the dock registry both knows it and lets
        /// go of it again.
        /// </summary>
        private void RunEditorContextChecks(EditorWindow editor)
        {
            var pass = 0;
            var fail = 0;

            void Check(string name, bool ok)
            {
                if (ok)
                {
                    pass++;
                }
                else
                {
                    fail++;
                }

                var line = $"{(ok ? "PASS" : "FAIL")} {name}";
                Log(line);
                App.Log($"EditorContextCheck: {line}");
            }

            var mainFactory = Factory;
            var editorFactory = editor.DockControl.Factory;

            Check("editor owns a separate factory",
                editorFactory is { } && !ReferenceEquals(editorFactory, mainFactory));
            Check("main factory does not list the editor's dock control",
                mainFactory?.DockControls.Contains(editor.DockControl) == false);
            Check("editor factory does not list the main dock control",
                editorFactory?.DockControls.Contains(Dock) == false);
            Check("editor window is in the dock registry",
                ReferenceEquals(HostWindow.GetWindowForElement(editor.DockControl), editor));

            // RootDockControl renders DefaultDockable, so a root without one is a
            // window that shows nothing at all — with a perfectly intact tree
            // underneath, which is what makes it so confusing to diagnose. The
            // pane-level version of the same disease: a tabbed dock with tabs but
            // no ActiveDockable draws an empty chrome.
            static bool PanesHaveActiveTabs(IDockable? node)
            {
                if (node is IToolDock or IDocumentDock)
                {
                    var dock = (IDock)node;
                    if (dock.VisibleDockables?.Count > 0 && dock.ActiveDockable is null)
                    {
                        return false;
                    }
                }

                if (node is IDock parent && parent.VisibleDockables is { } children)
                {
                    foreach (var child in children)
                    {
                        if (!PanesHaveActiveTabs(child))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            static bool Renderable(DockControl control)
                => control.Layout is IRootDock { DefaultDockable: IDock node }
                   && control.Layout is IDock root
                   && root.VisibleDockables?.Contains(node) == true
                   && PanesHaveActiveTabs(root);

            Check("editor root has something to render", Renderable(editor.DockControl));

            // Both contexts declare a tool with id "Properties" — each must find
            // its own, which is the whole point of scoping ids to a factory.
            var mine = mainFactory?.FindDockableById("Properties");
            var theirs = editorFactory?.FindDockableById("Properties");
            Check("the same id resolves to a different panel per context",
                mine is { } && theirs is { } && !ReferenceEquals(mine, theirs));

            // Last, because it swaps the whole tree out.
            editor.ResetToBuiltIn();
            Check("editor root still renders after resetting to the built-in layout",
                Renderable(editor.DockControl));

            // Queued so the assertions above are not running inside the window
            // that is being destroyed, and the registry check a turn later still,
            // because unregistration happens on the window's own Closed event.
            DispatcherQueue.TryEnqueue(() =>
            {
                editor.Close();

                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    Check("closing the editor unregisters it",
                        !HostWindow.windowMap.Values.Contains(editor));
                    var summary = $"editor context check: {pass} PASS / {fail} FAIL";
                    Log(summary);
                    App.Log($"EditorContextCheck: {summary}");
                });
            });
        }

        private readonly IDockSerializer _serializer;
        private readonly Dictionary<string, IDockable> _declared = new();
        private IDockable? _declaredRoot;
        private string? _defaultLayout;
        private IDockable? _lastClosed;
        private int _documentSeq;
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _reproTimer;
    }
}

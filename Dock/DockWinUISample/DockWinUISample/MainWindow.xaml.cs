using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Serializer;
using Dock.WinUI3;
using Dock.WinUI3.Controls;
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
            Dock.Loaded += (_, _) => HookFactoryEvents();
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

        private readonly IDockSerializer _serializer;
        private readonly Dictionary<string, IDockable> _declared = new();
        private IDockable? _declaredRoot;
        private string? _defaultLayout;
        private IDockable? _lastClosed;
        private int _documentSeq;
    }
}

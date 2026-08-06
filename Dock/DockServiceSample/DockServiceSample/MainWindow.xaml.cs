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

namespace DockServiceSample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Placement (size/position/maximized) persists via WinUIEx; the dock
            // layout itself is auto-saved on Closed below.
            PersistenceId = "MainWindow";
            Closed += (_, _) => SaveLayoutAutomatically();

            DockThemeManager.RegisterRoot(Content as FrameworkElement);
            DockThemeManager.SetTheme(ElementTheme.Dark);

            // Safe lookup: this sample registers NO dock theme dictionaries, so the
            // brush resolves from the library defaults (DockDefaults.xaml).
            if (DockThemeManager.TryGetResource("DockBackgroundBrush", out var value)
                && value is Microsoft.UI.Xaml.Media.SolidColorBrush brush)
            {
                this.SetTitleBarBackgroundColors(brush.Color);
            }

            Dock.Loaded += Dock_Loaded;
        }

        private void Dock_Loaded(object sender, RoutedEventArgs e)
        {
            m_dockService = new();
            m_dockService.LoadDefault();
            HookFactoryEvents();

            // Runs against a throwaway layout, so it is safe here and every launch
            // leaves a verdict in crash.log. The opt-in DOCKSAMPLE_REPRO branch is
            // the exception — that one rearranges the live layout on purpose.
            m_dockService.CheckRootEdgeQuietly();

            LoadAutoSavedLayout();

            // Opt-in: opens and closes a real window, which is not something
            // every launch should do.
            if (Environment.GetEnvironmentVariable("DOCKSAMPLE_WINDOWCHECK") == "1")
            {
                var window = OpenSampleWindow();
                DispatcherQueue.TryEnqueue(() => window.Close());
            }
        }

        private void OpenSampleWindow_Click(object sender, RoutedEventArgs e) => OpenSampleWindow();

        /// <summary>
        /// Opens the extra window and asserts the one thing that can silently rot:
        /// the WinUIDockManager facade must still point at the MAIN dock, because
        /// every service call in this sample goes through it.
        /// </summary>
        private SampleWindow OpenSampleWindow()
        {
            var before = WinUIDockManager.GetFactory();

            var window = new SampleWindow(this);
            window.Activate();

            var facadeIntact = ReferenceEquals(before, WinUIDockManager.GetFactory());
            var isolated = window.DockControl.Factory is { } extra && !ReferenceEquals(extra, before);

            App.Log("SampleWindow", null,
                $"facade check: {(facadeIntact ? "PASS" : "FAIL — the sample window clobbered the facade")}");
            App.Log("SampleWindow", null,
                $"isolation check: {(isolated ? "PASS" : "FAIL — the sample window shares the main factory")}");

            return window;
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
                        m_dockService.SaveLayout(stream);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                App.Log("MainWindow", e);
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
                        m_dockService.LoadLayout(stream);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                App.Log("MainWindow", e);
                }
            }
        }

        private void TogglePanel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleMenuFlyoutItem item || item.Tag is not string name)
            {
                return;
            }

            if (m_dockService.IsVisible(name))
            {
                m_dockService.Hide(name);
            }
            else
            {
                m_dockService.Show(name);
            }

            // The toggle flipped itself on click; realign it with the actual layout.
            SyncViewMenu();
        }

        /// <summary>
        /// Mirrors the real layout into the View menu, driven by the factory's own
        /// events so closing a panel from its tab button unticks it here too.
        /// </summary>
        private void SyncViewMenu()
        {
            if (m_dockService is null)
            {
                return;
            }

            foreach (var item in ViewMenu.Items.OfType<ToggleMenuFlyoutItem>())
            {
                if (item.Tag is string name)
                {
                    item.IsChecked = m_dockService.IsVisible(name);
                }
            }
        }

        private void HookFactoryEvents()
        {
            if (WinUIDockManager.GetFactory() is not { } factory)
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

        /// <summary>Undoes any rearranging and returns to the startup layout.</summary>
        private void ResetDefault_Click(object sender, RoutedEventArgs e)
        {
            m_dockService.ResetToDefault();
            SyncViewMenu();
        }

        /// <summary>Saves and immediately reloads. Nothing should move — that IS the
        /// test: the layout survives a serialization round-trip unchanged.</summary>
        private void RoundTrip_Click(object sender, RoutedEventArgs e)
        {
            m_dockService.LoadDefault();
            SyncViewMenu();
        }

        private void ListHidden_Click(object sender, RoutedEventArgs e)
        {
            m_dockService.LogHiddenDockables();
        }

        private void ValidateIds_Click(object sender, RoutedEventArgs e)
        {
            m_dockService.LogIdValidation();
        }

        private void CheckRootEdge_Click(object sender, RoutedEventArgs e)
        {
            m_dockService.LogRootEdgeCheck();
        }

        // ---- Auto-persistence: layout saved on close, restored on the next run ----

        private static string AutoLayoutPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "winuidock-servicesample", "layout-main.json");

        private void LoadAutoSavedLayout()
        {
            try
            {
                if (!File.Exists(AutoLayoutPath))
                {
                    return;
                }

                using var stream = File.OpenRead(AutoLayoutPath);
                m_dockService.LoadLayout(stream);
                SyncViewMenu();
                App.Log("AutoLayout", null, "restored from " + AutoLayoutPath);
            }
            catch (Exception ex)
            {
                // A broken file falls back to the default layout.
                App.Log("AutoLayout", ex, "load failed");
            }
        }

        private void SaveLayoutAutomatically()
        {
            if (m_dockService is null)
            {
                return;
            }

            try
            {
                // Float geometry is flushed inside DockSerializer.Save.
                Directory.CreateDirectory(Path.GetDirectoryName(AutoLayoutPath));
                using var stream = File.Create(AutoLayoutPath);
                m_dockService.SaveLayout(stream);
            }
            catch (Exception ex)
            {
                App.Log("AutoLayout", ex, "save failed");
            }
        }

        private DockService m_dockService;
    }
}

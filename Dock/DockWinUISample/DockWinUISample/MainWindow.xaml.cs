using Dock.Model;
using Dock.Model.Core;
using Dock.Serializer;
using Dock.WinUI3;
using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
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
            _dockState = new DockState();

            if (Dock is { })
            {
                var layout = Dock.Layout;
                if (layout is { })
                {
                    _dockState.Save(layout);
                }
            }
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
                        var layout = _serializer.Load<IDock>(stream);
                        if (layout is { })
                        {
                            Dock.Layout = layout;
                            _dockState.Restore(layout);
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private readonly IDockSerializer _serializer;
        private readonly IDockState _dockState;
    }
}

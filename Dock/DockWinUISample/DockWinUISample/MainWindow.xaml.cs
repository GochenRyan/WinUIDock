using Dock.Model;
using Dock.Model.Core;
using Dock.Serializer;
using Dock.WinUI3.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DockWinUISample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            _serializer = new DockSerializer(typeof(List<>));
            // _serializer = new AvaloniaDockSerializer();

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
                        var layout = _serializer.Load<IDock?>(stream);
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

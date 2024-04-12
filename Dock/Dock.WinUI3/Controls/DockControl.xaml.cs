using Dock.Model;
using Dock.Model.Core;
using Dock.WinUI3.Internal;
using Microsoft.UI.Xaml.Controls;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    public sealed partial class DockControl : ContentControl, IDockControl
    {
        private readonly DockManager _dockManager;
        private readonly DockControlState _dockControlState;

        public DockControl()
        {
            this.InitializeComponent();
        }

        public IDockManager DockManager => throw new NotImplementedException();

        public IDockControlState DockControlState => throw new NotImplementedException();

        public IDock Layout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public object DefaultContext { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool InitializeLayout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool InitializeFactory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFactory Factory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}

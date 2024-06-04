using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dock.WinUI3.Controls
{
    /// <summary>
    /// Specifies the Dock position of a child element that is inside a <see cref="DockPanel"/>.
    /// </summary>
    public enum Dock
    {
        /// <summary>
        /// Dock left
        /// </summary>
        Left,

        /// <summary>
        /// Dock Top
        /// </summary>
        Top,

        /// <summary>
        /// Dock Right
        /// </summary>
        Right,

        /// <summary>
        /// Dock Bottom
        /// </summary>
        Bottom
    }
}

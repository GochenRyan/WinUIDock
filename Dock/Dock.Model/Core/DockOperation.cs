
namespace Dock.Model.Core;

/// <summary>
/// Defines the available dock operations.
/// </summary>
public enum DockOperation
{
    /// <summary>
    /// Fill dock.
    /// </summary>
    Fill,

    /// <summary>
    /// Dock to left.
    /// </summary>
    Left,

    /// <summary>
    /// Dock to root left.
    /// </summary>
    RootLeft,

    /// <summary>
    /// Dock to bottom.
    /// </summary>
    Bottom,

    /// <summary>
    /// Dock to root bottom.
    /// </summary>
    RootBottom,

    /// <summary>
    /// Dock to right.
    /// </summary>
    Right,

    /// <summary>
    /// Dock to root right.
    /// </summary>
    RootRight,

    /// <summary>
    /// Dock to top.
    /// </summary>
    Top,

    /// <summary>
    /// Dock to root top.
    /// </summary>
    RootTop,

    /// <summary>
    /// Dock to window.
    /// </summary>
    Window
}
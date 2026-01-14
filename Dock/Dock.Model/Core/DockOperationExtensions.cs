namespace Dock.Model.Core;

internal static class DockOperationExtensions
{
    public static Alignment ToAlignment(this DockOperation operation)
    {
        return operation switch
        {
            DockOperation.Left => Alignment.Left,
            DockOperation.RootLeft => Alignment.Left,
            DockOperation.Bottom => Alignment.Bottom,
            DockOperation.RootBottom => Alignment.Bottom,
            DockOperation.Right => Alignment.Right,
            DockOperation.RootRight => Alignment.Right,
            DockOperation.Top => Alignment.Top,
            DockOperation.RootTop => Alignment.Top,
            _ => Alignment.Unset
        };
    }
}

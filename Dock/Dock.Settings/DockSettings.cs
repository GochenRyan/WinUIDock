namespace Dock.Settings
{
    public static class DockSettings
    {
        /// <summary>
        /// Minimum horizontal drag distance to initiate drag operation.
        /// </summary>
        public static double MinimumHorizontalDragDistance = 4;

        /// <summary>
        /// Minimum vertical drag distance to initiate drag operation.
        /// </summary>
        public static double MinimumVerticalDragDistance = 4;

        public static bool DockBetweenFloatWindows = true;

        /// <summary>
        /// Whether dragging a tool pane's CAPTION moves only that pane's active
        /// tool, instead of the whole dock with every tool docked in it.
        ///
        /// The caption's DataContext is the dock, so the raw gesture carries all
        /// of its tools away at once — correct by the object model, but not what
        /// someone dragging the tab in front of them expects. Set false for the
        /// whole-dock behaviour. Dragging a TAB is unaffected either way: a tab's
        /// DataContext is already the individual tool.
        /// </summary>
        public static bool DragActiveToolOnly = true;
    }
}

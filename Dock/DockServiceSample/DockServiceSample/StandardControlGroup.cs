namespace DockServiceSample
{
    /// <summary>
    /// Standard docking groups, which determine where controls are initially docked</summary>
    public enum StandardControlGroup
    {
        /// <summary>
        /// Left control group</summary>
        Left,

        /// <summary>
        /// Right control group</summary>
        Right,

        /// <summary>
        /// Top control group</summary>
        Top,

        /// <summary>
        /// Bottom control group</summary>
        Bottom,

        /// <summary>
        /// Center control group</summary>
        Center,

        /// <summary>
        /// Center control group, can't be closed</summary>
        CenterPermanent,

        /// <summary>
        /// Floating control</summary>
        Floating,

        /// <summary>
        /// Left Hidden control</summary>
        LeftHidden,

        /// <summary>
        /// Right Hidden control</summary>
        RightHidden,

        /// <summary>
        /// Top Hidden control</summary>
        TopHidden,

        /// <summary>
        /// Bottom Hidden control</summary>
        BottomHidden,

    }
}

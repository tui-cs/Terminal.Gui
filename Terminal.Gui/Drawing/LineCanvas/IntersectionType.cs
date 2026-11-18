namespace Terminal.Gui.Drawing;

internal enum IntersectionType
{
    /// <summary>There is no intersection</summary>
    None,

    /// <summary>A line passes directly over this point traveling along the horizontal axis</summary>
    PassOverHorizontal,

    /// <summary>A line passes directly over this point traveling along the vertical axis</summary>
    PassOverVertical,

    /// <summary>A line starts at this point and is traveling up</summary>
    StartUp,

    /// <summary>A line starts at this point and is traveling right</summary>
    StartRight,

    /// <summary>A line starts at this point and is traveling down</summary>
    StartDown,

    /// <summary>A line starts at this point and is traveling left</summary>
    StartLeft,

    /// <summary>A line exists at this point who has 0 length</summary>
    Dot
}

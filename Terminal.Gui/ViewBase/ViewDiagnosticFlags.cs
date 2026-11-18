namespace Terminal.Gui.ViewBase;

/// <summary>Enables diagnostic functions for <see cref="View"/>.</summary>
[Flags]
public enum ViewDiagnosticFlags : uint
{
    /// <summary>All diagnostics off</summary>
    Off = 0b_0000_0000,

    /// <summary>
    ///     When enabled, <see cref="AdornmentView"/> will draw a ruler in the Thickness. See <see cref="AdornmentView.Diagnostics"/>.
    /// </summary>
    Ruler = 0b_0000_0001,

    /// <summary>
    ///     When enabled, <see cref="AdornmentView"/> will draw the first letter of the Adornment name ('M', 'B', or 'P')
    ///     in the Thickness. See <see cref="AdornmentView.Diagnostics"/>.
    /// </summary>
    Thickness = 0b_0000_0010,

    ///// <summary>
    /////     When enabled the View's colors will be darker when the mouse is hovering over the View (See <see cref="View.MouseEnter"/> and <see cref="View.MouseLeave"/>.
    ///// </summary>
    //Hover = 0b_0000_00100,

    /// <summary>
    ///     When enabled a draw indicator will be shown; the indicator will change each time the View's Draw method is called with NeedsDraw set to true.
    /// </summary>
    DrawIndicator = 0b_0000_01000,
}

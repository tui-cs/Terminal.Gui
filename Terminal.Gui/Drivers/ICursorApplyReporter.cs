namespace Terminal.Gui.Drivers;

/// <summary>
///     INTERNAL: Lets <see cref="Terminal.Gui.App.ApplicationNavigation.UpdateCursor"/> ask a driver whether its last
///     <see cref="IDriver.SetCursor"/> call reached the terminal. A driver that does not implement it is treated as
///     always applying the cursor.
/// </summary>
internal interface ICursorApplyReporter
{
    /// <summary>Gets whether the last <see cref="IDriver.SetCursor"/> call reached the terminal.</summary>
    bool LastSetCursorApplied { get; }
}

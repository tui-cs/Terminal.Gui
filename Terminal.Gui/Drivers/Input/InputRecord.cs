namespace Terminal.Gui.Drivers;

/// <summary>
///     Platform-independent input event record base class for the input injection system.
/// </summary>
public abstract record InputEventRecord
{
    /// <summary>
    ///     When this input occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
///     Keyboard input event record.
/// </summary>
/// <param name="Key">The key that was pressed.</param>
public record KeyboardEventRecord (Key Key) : InputEventRecord;

/// <summary>
///     Mouse input event record (raw, before click synthesis).
/// </summary>
/// <param name="MouseEvent">The mouse event data.</param>
public record MouseEventRecord (Mouse MouseEvent) : InputEventRecord;

/// <summary>
///     ANSI sequence event record (for ANSI driver testing).
/// </summary>
/// <param name="Sequence">The ANSI escape sequence.</param>
public record AnsiEventRecord (string Sequence) : InputEventRecord;

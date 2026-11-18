namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IKeyConverter{T}"/> for converting ANSI character sequences
///     into Terminal.Gui <see cref="Key"/> representation.
/// </summary>
/// <remarks>
///     <para>
///         This converter processes character-based ANSI input using <see cref="EscSeqUtils"/>
///         for escape sequence parsing.
///     </para>
///     <list type="bullet">
///         
///         <item><see cref="AnsiInputProcessor"/> - ANSI-based driver for Unix/Linux/macOS and testing</item>
///     </list>
///     <para>
///         The conversion uses <see cref="ConsoleKeyInfo"/> as an intermediary format,
///         leveraging proven cross-platform key mapping logic in <see cref="EscSeqUtils"/>
///         and <see cref="ConsoleKeyMapping"/>.
///     </para>
/// </remarks>
internal class AnsiKeyConverter : IKeyConverter<char>
{
    /// <inheritdoc/>
    public Key ToKey (char value)
    {
        ConsoleKeyInfo adjustedInput = EscSeqUtils.MapChar (value);

        return EscSeqUtils.MapKey (adjustedInput);
    }

    /// <inheritdoc/>
    public char ToKeyInfo (Key key)
    {
        // Convert Key to ConsoleKeyInfo using the cross-platform mapping utility
        ConsoleKeyInfo consoleKeyInfo = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key.KeyCode);

        // Return the character representation for ANSI-based input
        return consoleKeyInfo.KeyChar;
    }
}

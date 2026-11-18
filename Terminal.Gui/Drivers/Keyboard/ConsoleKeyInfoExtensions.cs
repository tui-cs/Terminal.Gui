namespace Terminal.Gui.Drivers;

/// <summary>
///     Extension methods for <see cref="ConsoleKeyInfo"/>.
/// </summary>
public static class ConsoleKeyInfoExtensions
{
    /// <summary>
    ///     Returns a string representation of the <see cref="ConsoleKeyInfo"/> suitable for debugging and logging.
    /// </summary>
    /// <param name="consoleKeyInfo">The ConsoleKeyInfo to convert to string.</param>
    /// <returns>A formatted string showing the key, character, and modifiers.</returns>
    /// <remarks>
    ///     <para>
    ///         Examples:
    ///         <list type="bullet">
    ///             <item><c>Key: A ('a')</c> - lowercase 'a' pressed</item>
    ///             <item><c>Key: A ('A'), Modifiers: Shift</c> - uppercase 'A' pressed</item>
    ///             <item><c>Key: A (\0), Modifiers: Control</c> - Ctrl+A (no printable char)</item>
    ///             <item><c>Key: Enter (0x000D)</c> - Enter key (carriage return)</item>
    ///             <item><c>Key: F5 (\0)</c> - F5 function key</item>
    ///             <item><c>Key: D2 ('@'), Modifiers: Shift</c> - Shift+2 on US keyboard</item>
    ///             <item><c>Key: None ('é')</c> - Accented character</item>
    ///             <item><c>Key: CursorUp (\0), Modifiers: Shift | Control</c> - Ctrl+Shift+Up Arrow</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public static string ToString (this ConsoleKeyInfo consoleKeyInfo)
    {
        var sb = new StringBuilder ();

        // Always show the ConsoleKey enum value
        sb.Append ("Key: ");
        sb.Append (consoleKeyInfo.Key);

        // Show the character if it's printable, otherwise show hex representation
        sb.Append (" (");

        if (consoleKeyInfo.KeyChar >= 32 && consoleKeyInfo.KeyChar <= 126) // Printable ASCII range
        {
            sb.Append ('\'');
            sb.Append (consoleKeyInfo.KeyChar);
            sb.Append ('\'');
        }
        else if (consoleKeyInfo.KeyChar == 0)
        {
            sb.Append ("\\0");
        }
        else
        {
            // Show special characters or non-printable as hex
            sb.Append ("0x");
            sb.Append (((int)consoleKeyInfo.KeyChar).ToString ("X4"));
        }

        sb.Append (')');

        // Show modifiers if any are set
        if (consoleKeyInfo.Modifiers != 0)
        {
            sb.Append (", Modifiers: ");

            var needsSeparator = false;

            if ((consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0)
            {
                sb.Append ("Shift");
                needsSeparator = true;
            }

            if ((consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0)
            {
                if (needsSeparator)
                {
                    sb.Append (" | ");
                }

                sb.Append ("Alt");
                needsSeparator = true;
            }

            if ((consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0)
            {
                if (needsSeparator)
                {
                    sb.Append (" | ");
                }

                sb.Append ("Control");
            }
        }

        return sb.ToString ();
    }
}

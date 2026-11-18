namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IKeyConverter{T}"/> capable of converting the
///     dotnet <see cref="ConsoleKeyInfo"/> class into Terminal.Gui
///     shared <see cref="Key"/> representation (used by <see cref="View"/>
///     etc).
/// </summary>
internal class NetKeyConverter : IKeyConverter<ConsoleKeyInfo>
{
    /// <inheritdoc/>
    public Key ToKey (ConsoleKeyInfo input)
    {
        ConsoleKeyInfo adjustedInput = EscSeqUtils.MapConsoleKeyInfo (input);

        // TODO : EscSeqUtils.MapConsoleKeyInfo is wrong for e.g. '{' - it winds up clearing the Key
        //        So if the method nuked it then we should just work with the original.
        if (adjustedInput.Key == ConsoleKey.None && input.Key != ConsoleKey.None)
        {
            return EscSeqUtils.MapKey (input);
        }

        return EscSeqUtils.MapKey (adjustedInput);
    }

    /// <inheritdoc/>
    public ConsoleKeyInfo ToKeyInfo (Key key) { return ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key.KeyCode); }
}

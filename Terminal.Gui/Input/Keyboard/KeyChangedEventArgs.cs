namespace Terminal.Gui.Input;

/// <summary>
///     Event args for when a <see cref="Key"/> is changed from one value to a new value (e.g. in
///     <see cref="View.HotKeyChanged"/>)
/// </summary>
public class KeyChangedEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="KeyChangedEventArgs"/> class</summary>
    /// <param name="oldKey"></param>
    /// <param name="newKey"></param>
    public KeyChangedEventArgs (Key oldKey, Key newKey)
    {
        OldKey = oldKey;
        NewKey = newKey;
    }

    /// <summary>Gets the new <see cref="Key"/> that is being used. Use <see cref="Key.Empty"/> to check for empty.</summary>
    public Key NewKey { get; }

    /// <summary>Gets the old <see cref="Key"/> that was set before the event. Use <see cref="Key.Empty"/> to check for empty.</summary>
    public Key OldKey { get; }
}

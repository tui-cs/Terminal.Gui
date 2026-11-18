namespace Terminal.Gui.Input;

/// <summary>
///     Provides a collection of <see cref="Key"/>s bound to <see cref="Command"/>s.
/// </summary>
/// <seealso cref="KeyBindings"/>
/// <seealso cref="KeyBindings"/>
/// <seealso cref="Command"/>
public class KeyBindings : CommandBindingsBase<Key, KeyBinding>
{
    /// <summary>Initializes a new instance.</summary>
    public KeyBindings ()
        : base ((commands, key, source) => new KeyBinding (commands, key, source, null, null), new KeyEqualityComparer ()) { }

    /// <inheritdoc/>
    public override bool IsValid (Key eventArgs) => eventArgs.IsValid;

    /// <summary>
    ///     <para>
    ///         For Application-level HotKey Bindings (<see cref="IKeyboard.KeyBindings"/>); Adds key
    ///         will trigger the commands in <paramref name="commands"/> on the View
    ///         specified by <paramref name="target"/>.
    ///     </para>
    ///     <para>
    ///         If the key is already bound to a different array of <see cref="Command"/>s it will be rebound
    ///         <paramref name="commands"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="target">For Application-level HotKey Bindings; the view the key binding is bound to.</param>
    /// <param name="commands">
    ///     The command to invoked on the <see paramref="target"/> when <paramref name="key"/> is pressed. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="key"/> will be
    ///     consumed if any took effect.
    /// </param>
    /// <seealso cref="IKeyboard.KeyBindings"/>
    public void AddApp (Key key, View? target, params Command [] commands)
    {
        KeyBinding binding = new (commands, key, source: null, target: target, data: null);
        Add (key, binding);
    }
}

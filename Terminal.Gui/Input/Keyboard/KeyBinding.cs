// These classes use a key binding system based on the design implemented in Scintilla.Net which is an
// MIT licensed open source project https://github.com/jacobslusser/ScintillaNET/blob/master/src/ScintillaNET/Command.cs

namespace Terminal.Gui.Input;

/// <summary>
///     Provides a collection of <see cref="Command"/> objects stored in <see cref="KeyBindings"/>. Carried
///     as context in command invocations (see <see cref="CommandContext"/>).
/// </summary>
/// <seealso cref="KeyBindings"/>
/// <seealso cref="MouseBinding"/>
/// <seealso cref="CommandContext"/>
public record struct KeyBinding : ICommandBinding
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this key binding will invoke.</param>
    /// <param name="data">Arbitrary context that can be associated with this key binding.</param>
    public KeyBinding (Command [] commands, object? data = null)
    {
        Commands = commands;
        Data = data;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this key binding will invoke.</param>
    /// <param name="target">For Application-level HotKey Bindings; the view the key binding is bound to.</param>
    /// <param name="data">Arbitrary data that can be associated with this key binding.</param>
    public KeyBinding (Command [] commands, View? target, object? data = null)
    {
        Commands = commands;
        Target = target;
        Data = data;
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="commands">The commands this key binding will invoke.</param>
    /// <param name="newKey">The key this binding is associated with.</param>
    /// <param name="source">The view where this key binding was created.</param>
    /// <param name="target">For Application-level HotKey Bindings; the view the key binding is bound to.</param>
    /// <param name="data">Arbitrary data that can be associated with this key binding.</param>
    /// <seealso cref="IKeyboard.KeyBindings"/>
    public KeyBinding (Command [] commands, Key newKey, View? source = null, View? target = null, object? data = null)
    {
        Commands = commands;
        Source = source is { } ? new WeakReference<View> (source) : null;
        Key = newKey;
        Target = target;
        Data = data;
    }

    /// <inheritdoc/>
    public Command [] Commands { get; init; }

    /// <inheritdoc/>
    public object? Data { get; init; }

    /// <summary>
    ///     The Key that is bound to the <see cref="Commands"/>.
    /// </summary>
    public Key? Key { get; set; }

    /// <inheritdoc/>
    public WeakReference<View>? Source { get; init; }

    // TODO: Determine if Target is duplicative of Source
    /// <summary>
    ///     The view the key binding is bound to.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is used for application-level hotkeys where the binding is added at the application level
    ///         but the target view is where the command should be invoked.
    ///     </para>
    ///     <para>
    ///         This is distinct from <see cref="Source"/> which indicates where the binding was created.
    ///     </para>
    /// </remarks>
    public View? Target { get; set; }

    /// <inheritdoc />
    public override string ToString ()
    {
        string sourceStr = "";

        if (Source?.TryGetTarget (out View? sv) == true)
        {
            sourceStr = $", Source={sv.ToIdentifyingString ()}";
        }

        string targetStr = "";

        if (Target is { })
        {
            targetStr = $", Target={Target.ToIdentifyingString ()}";
        }

        return $"[{string.Join (", ", Commands)}], Key={Key}{sourceStr}{targetStr}{(Data is { } ? $", Data={Data}" : "")}";
    }
}

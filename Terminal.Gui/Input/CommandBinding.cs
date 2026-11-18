namespace Terminal.Gui.Input;

/// <summary>
///     A generic command binding used for programmatic command invocations
///     or when a specific binding type is not needed.
/// </summary>
/// <remarks>
///     <para>
///         Use <see cref="CommandBinding"/> when invoking commands programmatically via
///         <see cref="View.InvokeCommand(Command)"/> or when a binding is needed
///         but is not associated with a specific key or mouse event.
///     </para>
///     <para>
///         <see cref="Source"/> is a weak reference to the View that called <see cref="View.InvokeCommand(Command)"/>.
///         If <see langword="null"/> the CommandBinding instance was created dynamically (not from a mouse or keyboard
///         binding).
///     </para>
///     <para>
///         Pattern match on binding types to discriminate between input sources:
///         <code>
///         if (ctx.Binding is KeyBinding kb) { /* key input */ }
///         else if (ctx.Binding is MouseBinding mb) { /* mouse input */ }
///         else if (ctx.Binding is CommandBinding ib) { /* programmatic */ }
///         </code>
///     </para>
/// </remarks>
/// <seealso cref="KeyBinding"/>
/// <seealso cref="MouseBinding"/>
/// <seealso cref="ICommandBinding"/>
public readonly record struct CommandBinding : ICommandBinding
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this binding will invoke.</param>
    /// <param name="source">The view that is the origin of this binding.</param>
    /// <param name="data">Arbitrary context data that can be associated with this binding.</param>
    public CommandBinding (Command [] commands, View? source = null, object? data = null)
    {
        Commands = commands;
        Source = source is { } ? new WeakReference<View> (source) : null;
        Data = data;
    }

    /// <inheritdoc/>
    public Command [] Commands { get; init; }

    /// <inheritdoc/>
    public object? Data { get; init; }

    /// <inheritdoc/>
    public WeakReference<View>? Source { get; init; }

    /// <inheritdoc/>
    public override string ToString ()
    {
        string sourceStr = "null";

        if (Source?.TryGetTarget (out View? sv) == true)
        {
            sourceStr = sv.ToIdentifyingString ();
        }

        return $"[{string.Join (", ", Commands)}], Source={sourceStr}, Data={Data}";
    }
}

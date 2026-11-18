namespace Terminal.Gui.Input;

/// <summary>
///     Provides a collection of <see cref="Command"/> objects stored in <see cref="MouseBindings"/>. Carried
///     as context in command invocations (see <see cref="CommandContext"/>).
/// </summary>
/// <seealso cref="MouseBindings"/>
/// <seealso cref="KeyBinding"/>
/// <seealso cref="CommandContext"/>
public record struct MouseBinding : ICommandBinding
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this mouse binding will invoke.</param>
    /// <param name="mouseFlags">The mouse flags that triggered this binding.</param>
    /// <param name="source"></param>
    public MouseBinding (Command [] commands, MouseFlags mouseFlags, View? source = null)
    {
        Commands = commands;
        MouseEvent = new Mouse { Timestamp = DateTime.Now, Flags = mouseFlags };
        Source = source is { } ? new WeakReference<View> (source) : null;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this mouse binding will invoke.</param>
    /// <param name="args">The mouse event that triggered this binding.</param>
    public MouseBinding (Command [] commands, Mouse args)
    {
        Commands = commands;
        MouseEvent = args;
    }

    /// <inheritdoc/>
    public Command [] Commands { get; init; }

    /// <inheritdoc/>
    public object? Data { get; init; }

    /// <inheritdoc/>
    public WeakReference<View>? Source { get; init; }

    /// <summary>
    ///     The mouse event data associated with this binding.
    /// </summary>
    public Mouse? MouseEvent { get; set; }

    /// <inheritdoc/>
    public override string ToString ()
    {
        string sourceStr = "";

        if (Source?.TryGetTarget (out View? sv) == true)
        {
            sourceStr = $", Source={sv.ToIdentifyingString ()}";
        }

        return $"[{string.Join (", ", Commands)}] (MouseEvent={MouseEvent}{sourceStr}{(Data is { } data ? $", Data={data}" : "")})";
    }
}

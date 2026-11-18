namespace Terminal.Gui.Input;

#pragma warning disable CS1574, CS0419 // XML comment has cref attribute that could not be resolved
/// <summary>
///     Provides context for a <see cref="Command"/> invocation.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="CommandContext"/> is immutable. Use <see cref="WithCommand"/> or <see cref="WithRouting"/>
///         to create a new context with modified values while preserving all other fields.
///     </para>
///     <para>
///         Use pattern matching to access specific binding types:
///         <code>
///         if (ctx.Binding is KeyBinding kb) { /* key input */ }
///         else if (ctx.Binding is MouseBinding mb) { /* mouse input */ }
///         else if (ctx.Binding is CommandBinding ib) { /* programmatic */ }
///         </code>
///     </para>
/// </remarks>
/// <seealso cref="View.InvokeCommand"/>
/// .
#pragma warning restore CS1574, CS0419 // XML comment has cref attribute that could not be resolved
public readonly record struct CommandContext : ICommandContext
{
    /// <summary>
    ///     Initializes a new instance with the specified <see cref="Command"/>.
    /// </summary>
    /// <param name="command">The command being invoked.</param>
    /// <param name="source">A weak reference to the view that is the source of the command invocation.</param>
    /// <param name="binding">The binding that triggered the command, if any.</param>
    public CommandContext (Command command, WeakReference<View>? source, ICommandBinding? binding)
    {
        Command = command;
        Binding = binding;
        Source = source;
        Values = [];
    }

    /// <inheritdoc/>
    public Command Command { get; init; }

    /// <inheritdoc/>
    public WeakReference<View>? Source { get; init; }

    /// <inheritdoc/>
    public ICommandBinding? Binding { get; init; }

    /// <inheritdoc/>
    public CommandRouting Routing { get; init; }

    /// <inheritdoc/>
    public IReadOnlyList<object?> Values
    {
        get => field ?? [];
        init;
    } = [];

    /// <inheritdoc/>
    public object? Value => Values is { Count: > 0 } ? Values [^1] : null;

    /// <summary>
    ///     Creates a new context with a different command, preserving all other fields.
    /// </summary>
    /// <param name="command">The new command.</param>
    /// <returns>A new <see cref="CommandContext"/> with the specified command.</returns>
    public CommandContext WithCommand (Command command) => this with { Command = command };

    /// <summary>
    ///     Creates a new context with different routing, preserving all other fields.
    /// </summary>
    /// <param name="routing">The new routing mode.</param>
    /// <returns>A new <see cref="CommandContext"/> with the specified routing.</returns>
    public CommandContext WithRouting (CommandRouting routing) => this with { Routing = routing };

    /// <summary>
    ///     Creates a new context with the specified value appended to the <see cref="Values"/> chain.
    ///     The new value becomes the last element, making it the new <see cref="Value"/>.
    /// </summary>
    /// <remarks>
    ///     The spread expression <c>[..Values, value]</c> copies the entire list on each call,
    ///     giving O(N²) total allocations for N appends. This is acceptable for typical UI hierarchies
    ///     (3–5 levels deep). If extreme depths are ever needed, consider an immutable linked list or builder.
    /// </remarks>
    /// <param name="value">The value to append.</param>
    /// <returns>A new <see cref="CommandContext"/> with the value appended to <see cref="Values"/>.</returns>
    public CommandContext WithValue (object? value) => this with { Values = [.. Values, value] };

    /// <inheritdoc/>
    public override string ToString ()
    {
        string routing = Routing == CommandRouting.BubblingUp
                             ? Glyphs.UpArrow.ToString ()
                             : Routing == CommandRouting.DispatchingDown
                                 ? Glyphs.DownArrow.ToString ()
                                 : "";
        string source = Source is { } ? $"Source={Source.ToIdentifyingString ()}" : "";
        string binding = Binding is { } ? $", Binding={Binding}" : "";
        string value = Values is { Count: > 0 } ? $", Value={Value}" : "";
        string values = Values is { Count: > 1 } ? $", Values=[{string.Join (", ", Values)}]" : "";

        return $"{routing}{Command} ({source}{binding}{value}{values})";
    }
}

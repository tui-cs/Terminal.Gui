namespace Terminal.Gui.Input;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
/// <summary>
///     Describes the context in which a <see cref="Command"/> is being invoked.
///     When a <see cref="Command"/> is invoked via <see cref="View.InvokeCommand(Command)"/>
///     a context object is passed to Command handlers as an <see cref="ICommandContext"/> reference.
/// </summary>
/// <seealso cref="View.AddCommand()"/>
/// <seealso cref="View.InvokeCommand(Command)"/>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
public interface ICommandContext
{
    /// <summary>
    ///     The <see cref="Command"/> that is being invoked.
    /// </summary>
    public Command Command { get; }

    /// <summary>
    ///     A weak reference to the View that was the source of the command invocation, if any.
    ///     (e.g. the view the user clicked on or the view that had focus when a key was pressed).
    ///     Use <c>Source?.TryGetTarget(out View? view)</c> to safely access the source view.
    /// </summary>
    /// <remarks>
    ///     Uses WeakReference to prevent memory leaks and access to disposed views when views are disposed during command
    ///     propagation.
    /// </remarks>
    public WeakReference<View>? Source { get; }

    /// <summary>
    ///     The binding that triggered the command.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use pattern matching to access specific binding types:
    ///         <code>
    ///         if (ctx.Binding is KeyBinding kb) { /* key binding */ }
    ///         else if (ctx.Binding is MouseBinding mb) { /* mouse binding */ }
    ///         else if (ctx.Binding is CommandBinding ib) { /* programmatic */ }
    ///         </code>
    ///     </para>
    /// </remarks>
    public ICommandBinding? Binding { get; }

    /// <summary>
    ///     Gets the routing mode for this command invocation.
    /// </summary>
    public CommandRouting Routing { get; }

    /// <summary>
    ///     Gets all values accumulated as the command propagated up the view hierarchy.
    ///     Each <see cref="IValue"/>-implementing view in the chain appends its value via
    ///     <see cref="IValue.GetValue"/> as the command bubbles up. The list is ordered from
    ///     innermost (originator) to outermost (last composite to append).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use <see cref="Value"/> as a shortcut to access the most recently appended value
    ///         (<c>Values[^1]</c>), which is typically the outermost composite's value.
    ///     </para>
    ///     <para>
    ///         Will be empty if no <see cref="IValue"/>-implementing views participated.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Value"/>
    /// <seealso cref="IValue"/>
    public IReadOnlyList<object?> Values { get; }

    /// <summary>
    ///     Gets the most recently appended value from <see cref="Values"/>, or <see langword="null"/>
    ///     if <see cref="Values"/> is empty. This is a convenience accessor equivalent to
    ///     <c>Values.Count > 0 ? Values[^1] : null</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This enables command handlers up the hierarchy to access the source view's value
    ///         without needing to know the generic type parameter of <see cref="IValue{TValue}"/>.
    ///     </para>
    ///     <para>
    ///         In a simple hierarchy (e.g., <c>OptionSelector</c> → ancestor), <c>Value</c> will be
    ///         the composite's semantic value (e.g., <c>int?</c> index). In a multi-layer hierarchy
    ///         (e.g., <c>OptionSelector</c> inside <c>MenuItem</c> inside <c>PopoverMenu</c>),
    ///         <c>Value</c> will be the outermost composite's value (e.g., <c>MenuItem</c>).
    ///         Use <see cref="Values"/> to inspect inner values.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Values"/>
    /// <seealso cref="IValue"/>
    /// <seealso cref="IValue.GetValue"/>
    public object? Value { get; }
}

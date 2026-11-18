using System.ComponentModel;

namespace Terminal.Gui.Input;

/// <summary>
///     Event arguments for <see cref="Command"/> events. Set <see cref="HandledEventArgs.Handled"/> to
///     <see langword="true"/> to indicate a command was handled.
/// </summary>
public class CommandEventArgs : HandledEventArgs
{
    /// <summary>
    ///     The context for the command, if any.
    /// </summary>
    /// <remarks>
    ///     If <see langword="null"/> the command was invoked without context.
    /// </remarks>
    public required ICommandContext? Context { get; init; }

    /// <inheritdoc />
    public override string ToString () => $"{Context}";
}

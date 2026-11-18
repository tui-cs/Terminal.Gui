namespace Terminal.Gui.Input;

/// <summary>
///     Dedicated command-context payload used to carry bracketed-paste text through
///     <see cref="Command.Paste"/> without colliding with unrelated <see cref="IValue"/> values already
///     present in the command context.
/// </summary>
internal readonly record struct PastePayload (string Text);

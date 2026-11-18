using System.ComponentModel;

namespace Terminal.Gui.Input;

/// <summary>
///     Event arguments for the cancellable <see cref="View.Pasting"/> event raised by the default
///     <see cref="Command.Paste"/> handler. <see cref="Text"/> is mutable so subscribers can
///     normalize or filter the payload before the view inserts it; set
///     <see cref="HandledEventArgs.Handled"/> to <see langword="true"/> to cancel the paste.
/// </summary>
public class PastingEventArgs : HandledEventArgs
{
    /// <summary>Initializes a new <see cref="PastingEventArgs"/>.</summary>
    /// <param name="text">The (already sanitized) pasted text that the view is about to insert.</param>
    public PastingEventArgs (string text) { Text = text; }

    /// <summary>
    ///     The pasted text the view is about to insert. Already sanitized by
    ///     <see cref="View.OnSanitizingPaste"/>. Subscribers may replace it with a different string
    ///     to alter what gets inserted.
    /// </summary>
    public string Text { get; set; }

    /// <inheritdoc/>
    public override string ToString () => $"Pasting ({Text.Length} chars)";
}

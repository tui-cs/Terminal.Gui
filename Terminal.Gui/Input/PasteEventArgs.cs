using System.ComponentModel;

namespace Terminal.Gui.Input;

/// <summary>
///     Event arguments for the application-level <see cref="IApplication.Paste"/> event. Carries the
///     raw payload delivered by the terminal's bracketed-paste mode, before any view-level
///     sanitization. Set <see cref="HandledEventArgs.Handled"/> to <see langword="true"/> to stop
///     the paste from being dispatched to the focused view.
/// </summary>
/// <remarks>
///     <para>
///         Bracketed paste delivers the entire pasted payload as a single string, distinct from
///         keyboard input. Subscribers at the application boundary observe the unmodified text.
///         View-level sanitization (line-ending normalization, control-character stripping) is
///         performed downstream by <see cref="View.OnSanitizingPaste"/> when the
///         <see cref="Command.Paste"/> handler runs.
///     </para>
/// </remarks>
public class PasteEventArgs : HandledEventArgs
{
    /// <summary>
    ///     Initializes a new <see cref="PasteEventArgs"/>.
    /// </summary>
    /// <param name="text">The pasted text with bracketing markers stripped.</param>
    public PasteEventArgs (string text) { Text = text; }

    /// <summary>
    ///     The pasted text. Bracketing markers (<c>ESC[200~</c> / <c>ESC[201~</c>) are stripped by
    ///     the parser and are never present in this string.
    /// </summary>
    public string Text { get; }

    /// <inheritdoc/>
    public override string ToString () => $"Paste ({Text.Length} chars)";
}

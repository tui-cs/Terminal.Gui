using System.Text;

namespace Terminal.Gui.ViewBase;

public partial class View // Paste APIs
{
    private protected bool CurrentPasteUsesPayload { get; private set; }

    /// <summary>
    ///     Default handler for <see cref="Command.Paste"/>. Resolves the paste payload (from a
    ///     dedicated <see cref="PastePayload"/> when bracketed paste delivered one, otherwise from
    ///     <see cref="IApplication.Clipboard"/>), sanitizes it via <see cref="OnSanitizingPaste"/>,
    ///     raises the cancellable <see cref="Pasting"/> event, calls <see cref="OnPaste"/> to insert
    ///     the text, then raises <see cref="Pasted"/> if insertion actually occurred.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Plain <see cref="View"/> instances return <see langword="false"/> from
    ///         <see cref="OnPaste"/> by default. Subclasses that accept text (for example
    ///         <see cref="TextField"/>) overrides
    ///         <see cref="OnPaste"/> to perform the insertion.
    ///     </para>
    ///     <para>
    ///         Bracketed-paste payloads carry the raw bytes delivered by the terminal between
    ///         <c>ESC[200~</c> and <c>ESC[201~</c>. Keyboard-driven pastes (<c>Ctrl+V</c>) have no
    ///         payload and fall through to the clipboard. Both paths share this handler, so any
    ///         sanitization or event subscription works for both.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if the paste was consumed (sanitized text inserted, or cancelled
    ///     by a subscriber); <see langword="false"/> if nothing was pasted.
    /// </returns>
    private bool? DefaultPasteHandler (ICommandContext? ctx)
    {
        if (!Enabled)
        {
            return false;
        }

        PastePayload? pastePayload = ctx?.Value is PastePayload value ? value : null;
        bool usesPayload = pastePayload is { };
        string? payload = usesPayload ? pastePayload?.Text : App?.Clipboard?.GetClipboardData ();

        if (string.IsNullOrEmpty (payload))
        {
            return false;
        }

        string sanitized = OnSanitizingPaste (payload);

        if (string.IsNullOrEmpty (sanitized))
        {
            return false;
        }

        PastingEventArgs pasting = new (sanitized);
        Pasting?.Invoke (this, pasting);

        if (pasting.Handled)
        {
            return true;
        }

        if (string.IsNullOrEmpty (pasting.Text))
        {
            return false;
        }

        CurrentPasteUsesPayload = usesPayload;

        try
        {
            if (!OnPaste (pasting.Text))
            {
                return false;
            }

            string? pastedText = GetPastedEventText (pasting.Text);

            if (ShouldRaisePastedEvent (pasting.Text) && !string.IsNullOrEmpty (pastedText))
            {
                Pasted?.Invoke (this, new PastedEventArgs (pastedText));
            }

            return true;
        }
        finally
        {
            CurrentPasteUsesPayload = false;
        }
    }

    /// <summary>
    ///     Override to filter or transform raw paste payloads before they are inserted into the
    ///     view. The default implementation strips C0/C1 control characters (including ESC) but
    ///     preserves tab, line feed, and carriage return — matching Windows Terminal's
    ///     <c>FilterStringForPaste</c> baseline.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="TextField"/> overrides this to take only the first line and drop tab/CR/LF.
    ///         <see cref="TextView"/> overrides this to normalize <c>\r\n</c> / <c>\r</c> to <c>\n</c>.
    ///     </para>
    /// </remarks>
    /// <param name="raw">The raw payload, either from the terminal or the clipboard.</param>
    /// <returns>The sanitized text that will be passed to <see cref="OnPaste"/>.</returns>
    protected virtual string OnSanitizingPaste (string raw) => StripControlCharsExceptTabAndNewline (raw);

    /// <summary>
    ///     Override to insert sanitized paste text into the view. The default returns
    ///     <see langword="false"/> because a plain <see cref="View"/> has no text model. Text-input
    ///     views (<see cref="TextField"/>) override this to perform the
    ///     insertion.
    /// </summary>
    /// <param name="text">The sanitized text to insert. Never <see langword="null"/> or empty.</param>
    /// <returns><see langword="true"/> if the view consumed the paste.</returns>
    protected virtual bool OnPaste (string text) => false;

    /// <summary>
    ///     Override to suppress <see cref="Pasted"/> when <see cref="OnPaste"/> consumes a paste
    ///     without inserting the text.
    /// </summary>
    /// <param name="text">The sanitized text passed to <see cref="OnPaste"/>.</param>
    /// <returns>
    ///     <see langword="true"/> to raise <see cref="Pasted"/>; <see langword="false"/> when the
    ///     paste was consumed without insertion.
    /// </returns>
    protected virtual bool ShouldRaisePastedEvent (string text) => true;

    /// <summary>
    ///     Override to provide the text for <see cref="Pasted"/> after <see cref="OnPaste"/> has run.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The default returns the sanitized text passed into <see cref="OnPaste"/>.
    ///         Views that rewrite or partially reject the paste during insertion can override this to
    ///         report the segment of final view text that corresponds to the pasted range.
    ///     </para>
    /// </remarks>
    /// <param name="text">The sanitized text passed to <see cref="OnPaste"/>.</param>
    /// <returns>
    ///     The text to expose through <see cref="Pasted"/>; return <see langword="null"/> to suppress
    ///     the event.
    /// </returns>
    protected virtual string? GetPastedEventText (string text) => text;

    /// <summary>
    ///     Raised by the default <see cref="Command.Paste"/> handler after sanitization but before
    ///     insertion. Subscribers may rewrite <see cref="PastingEventArgs.Text"/> or set
    ///     <see cref="System.ComponentModel.HandledEventArgs.Handled"/> to cancel.
    /// </summary>
    public event EventHandler<PastingEventArgs>? Pasting;

    /// <summary>
    ///     Raised by the default <see cref="Command.Paste"/> handler after <see cref="OnPaste"/>
    ///     consumes a paste and the view reports that the text was inserted. Observation only — the
    ///     text has already been inserted.
    /// </summary>
    public event EventHandler<PastedEventArgs>? Pasted;

    private static string StripControlCharsExceptTabAndNewline (string text)
    {
        StringBuilder sb = new (text.Length);

        foreach (char c in text)
        {
            if (c == '\t' || c == '\n' || c == '\r' || (c >= 0x20 && c < 0x7F) || c >= 0xA0)
            {
                sb.Append (c);
            }
        }

        return sb.ToString ();
    }
}

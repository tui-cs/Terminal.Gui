namespace Terminal.Gui.Views;

/// <summary>TextValidateField Providers Interface. All TextValidateField are created with a ITextValidateProvider.</summary>
public interface ITextValidateProvider
{
    /// <summary>Gets the formatted string for display.</summary>
    string DisplayText { get; }

    /// <summary>Set that this provider uses a fixed width. e.g. Masked ones are fixed.</summary>
    bool Fixed { get; }

    /// <summary>True if the input is valid, otherwise false.</summary>
    bool IsValid { get; }

    /// <summary>Set the input text and get the current value.</summary>
    string Text { get; set; }

    /// <summary>Set Cursor position to <paramref name="pos"/>.</summary>
    /// <param name="pos"></param>
    /// <returns>Return first valid position.</returns>
    int Cursor (int pos);

    /// <summary>Find the last valid character position.</summary>
    /// <returns>New cursor position.</returns>
    int CursorEnd ();

    /// <summary>First valid position before <paramref name="pos"/>.</summary>
    /// <param name="pos"></param>
    /// <returns>New cursor position if any, otherwise returns <paramref name="pos"/></returns>
    int CursorLeft (int pos);

    /// <summary>First valid position after <paramref name="pos"/>.</summary>
    /// <param name="pos">Current position.</param>
    /// <returns>New cursor position if any, otherwise returns <paramref name="pos"/></returns>
    int CursorRight (int pos);

    /// <summary>Find the first valid character position.</summary>
    /// <returns>New cursor position.</returns>
    int CursorStart ();

    /// <summary>Deletes the current character in <paramref name="pos"/>.</summary>
    /// <param name="pos"></param>
    /// <returns>true if the character was successfully removed, otherwise false.</returns>
    bool Delete (int pos);

    /// <summary>Insert character <paramref name="ch"/> in position <paramref name="pos"/>.</summary>
    /// <param name="ch"></param>
    /// <param name="pos"></param>
    /// <returns>true if the character was successfully inserted, otherwise false.</returns>
    bool InsertAt (char ch, int pos);

    /// <summary>
    ///     Changed event, raised when the text has changed.
    ///     <remarks>
    ///         This event is raised when the <see cref="Text"/> changes. The passed <see cref="EventArgs"/> is a
    ///         <see cref="string"/> containing the old value.
    ///     </remarks>
    /// </summary>
    event EventHandler<EventArgs<string>> TextChanged;
}

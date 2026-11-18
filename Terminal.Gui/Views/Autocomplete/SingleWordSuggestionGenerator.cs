#nullable disable
namespace Terminal.Gui.Views;

/// <summary>
///     <see cref="ISuggestionGenerator"/> which suggests from a collection of words those that match the
///     <see cref="AutocompleteContext"/>. You can update <see cref="AllSuggestions"/> at any time to change candidates
///     considered for autocomplete.
/// </summary>
public class SingleWordSuggestionGenerator : ISuggestionGenerator
{
    /// <summary>The full set of all strings that can be suggested.</summary>
    /// <returns></returns>
    public virtual List<string> AllSuggestions { get; set; } = new ();

    /// <inheritdoc/>
    public IEnumerable<Suggestion> GenerateSuggestions (AutocompleteContext context)
    {
        // if there is nothing to pick from
        if (AllSuggestions.Count == 0)
        {
            return [];
        }

        List<string> line = context.CurrentLine.Select (c => c.Grapheme).ToList ();
        string currentWord = IdxToWord (line, context.CursorPosition, out int startIdx);
        context.CursorPosition = startIdx < 1 ? startIdx : Math.Min (startIdx + 1, line.Count);

        if (string.IsNullOrWhiteSpace (currentWord))
        {
            return Enumerable.Empty<Suggestion> ();
        }

        return AllSuggestions.Where (
                                     o =>
                                         o.StartsWith (currentWord, StringComparison.CurrentCultureIgnoreCase)
                                         && !o.Equals (currentWord, StringComparison.CurrentCultureIgnoreCase)
                                    )
                             .Select (o => new Suggestion (currentWord.Length, o))
                             .ToList ()
                             .AsReadOnly ();
    }

    /// <summary>
    ///     Return true if the given symbol should be considered part of a word and can be contained in matches. Base
    ///     behavior is to use <see cref="char.IsLetterOrDigit(char)"/>
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns></returns>
    public virtual bool IsWordChar (string text)
    {
        return !string.IsNullOrEmpty (text)
               && Rune.IsLetterOrDigit (text.EnumerateRunes ().First ());
    }

    /// <summary>
    ///     <para>
    ///         Given a <paramref name="line"/> of characters, returns the word which ends at <paramref name="idx"/> or null.
    ///         Also returns null if the <paramref name="idx"/> is positioned in the middle of a word.
    ///     </para>
    ///     <para>
    ///         Use this method to determine whether autocomplete should be shown when the cursor is at a given point in a
    ///         line and to get the word from which suggestions should be generated. Use the <paramref name="columnOffset"/> to
    ///         indicate if search the word at left (negative), at right (positive) or at the current column (zero) which is
    ///         the default.
    ///     </para>
    /// </summary>
    /// <param name="line"></param>
    /// <param name="idx"></param>
    /// <param name="startIdx">The start index of the word.</param>
    /// <param name="columnOffset"></param>
    /// <returns></returns>
    protected virtual string IdxToWord (List<string> line, int idx, out int startIdx, int columnOffset = 0)
    {
        var sb = new StringBuilder ();
        startIdx = idx;

        // get the ending word index
        while (startIdx < line.Count)
        {
            if (IsWordChar (line [startIdx]))
            {
                startIdx++;
            }
            else
            {
                break;
            }
        }

        // It isn't a word char then there is no way to autocomplete that word
        if (startIdx == idx && columnOffset != 0)
        {
            return null;
        }

        // we are at the end of a word. Work out what has been typed so far
        while (startIdx-- > 0)
        {
            if (IsWordChar (line [startIdx]))
            {
                sb.Insert (0, line [startIdx]);
            }
            else
            {
                break;
            }
        }

        startIdx = Math.Max (startIdx, 0);

        return sb.ToString ();
    }
}

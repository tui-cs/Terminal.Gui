using System.Globalization;

namespace Terminal.Gui.Drawing;

/// <summary>
///     Provides utility methods for enumerating Unicode grapheme clusters (user-perceived characters)
///     in a string. A grapheme cluster may consist of one or more <see cref="Rune"/> values,
///     including combining marks or zero-width joiner (ZWJ) sequences such as emoji family groups.
/// </summary>
/// <remarks>
///     <para>
///         This helper uses <see cref="StringInfo.GetTextElementEnumerator(string)"/> to enumerate
///         text elements according to the Unicode Standard Annex #29 (UAX #29) rules for
///         extended grapheme clusters.
///     </para>
///     <para>
///         On legacy Windows consoles (e.g., <c>cmd.exe</c>, <c>conhost.exe</c>), complex grapheme
///         sequences such as ZWJ emoji or combining marks may not render correctly, even though
///         the underlying string data is valid.
///     </para>
///     <para>
///         For most accurate visual rendering, prefer modern terminals such as Windows Terminal
///         or Linux-based terminals with full Unicode and font support.
///     </para>
/// </remarks>
public static class GraphemeHelper
{
    /// <summary>
    ///     Enumerates extended grapheme clusters from a string.
    ///     Handles surrogate pairs, combining marks, and basic ZWJ sequences.
    ///     Safe for legacy consoles; memory representation is correct.
    /// </summary>
    public static IEnumerable<string> GetGraphemes (string text)
    {
        if (string.IsNullOrEmpty (text))
        {
            yield break;
        }

        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator (text);

        while (enumerator.MoveNext ())
        {
            string element = enumerator.GetTextElement ();

            yield return element;
        }
    }

    // Match StringExtensions.GetColumns for one already segmented grapheme without allocating a string.
    internal static int GetGraphemeColumns (ReadOnlySpan<char> grapheme, bool ignoreLessThanZero = true)
    {
        var columns = 0;
        foreach (Rune rune in grapheme.EnumerateRunes ())
        {
            int width = rune.GetColumns ();
            columns = checked(columns + (ignoreLessThanZero && width < 0 ? 0 : width));
        }
        return Math.Min (columns, 2);
    }

    /// <summary>
    ///     Counts the number of grapheme clusters in a string without allocating intermediate collections.
    /// </summary>
    /// <param name="text">The string to count graphemes in.</param>
    /// <returns>The number of grapheme clusters, or 0 if the string is null or empty.</returns>
    public static int GetGraphemeCount (string text)
    {
        if (string.IsNullOrEmpty (text))
        {
            return 0;
        }

        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator (text);
        var count = 0;

        while (enumerator.MoveNext ())
        {
            count++;
        }

        return count;
    }
}


namespace Terminal.Gui.Drawing;

/// <summary>
///     Represents a single row/column in a Terminal.Gui rendering surface (e.g. <see cref="LineCanvas"/> and
///     <see cref="IDriver"/>).
/// </summary>
public record struct Cell (Attribute? Attribute = null, bool IsDirty = false, string Grapheme = "")
{
    /// <summary>The attributes to use when drawing the Glyph.</summary>
    public Attribute? Attribute { get; set; } = Attribute;

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.Drawing.Cell"/> has been modified since the
    ///     last time it was drawn.
    /// </summary>
    public bool IsDirty { get; set; } = IsDirty;

    private string _grapheme = Grapheme;

    /// <summary>
    ///     The single grapheme cluster to display from this cell. If <see cref="Grapheme"/> is <see langword="null"/> or
    ///     <see cref="string.Empty"/>, then <see cref="Cell"/> is ignored.
    /// </summary>
    public string Grapheme
    {
        readonly get => _grapheme;
        set
        {
            if (GraphemeHelper.GetGraphemeCount (value) > 1)
            {
                throw new InvalidOperationException ($"Only a single {nameof (Grapheme)} cluster is allowed per Cell.");
            }

            if (!string.IsNullOrEmpty (value) && value.Length == 1 && char.IsSurrogate (value [0]))
            {
                throw new ArgumentException ($"Only valid Unicode scalar values are allowed in a single {nameof (Grapheme)} cluster.");
            }

            try
            {
                _grapheme = !string.IsNullOrEmpty (value) && !value.IsNormalized (NormalizationForm.FormC)
                                ? value.Normalize (NormalizationForm.FormC)
                                : value;
            }
            catch (ArgumentException)
            {
                // leave text unnormalized
                _grapheme = value;
            }
        }
    }

    /// <summary>
    ///     The rune for <see cref="Grapheme"/> or runes for <see cref="Grapheme"/> that when combined makes this Cell a combining sequence.
    /// </summary>
    /// <remarks>
    ///     In the case where <see cref="Grapheme"/> has more than one rune it is a combining sequence that is normalized to a
    ///     single Text which may occupies 1 or 2 columns.
    /// </remarks>
    public IReadOnlyList<Rune> Runes => string.IsNullOrEmpty (Grapheme) ? [] : Grapheme.EnumerateRunes ().ToList ();

    /// <inheritdoc/>
    public override string ToString ()
    {
        string visibleText = EscapeControlAndInvisible (Grapheme);

        return $"[\"{visibleText}\":{Attribute}]";
    }

    private static string EscapeControlAndInvisible (string text)
    {
        if (string.IsNullOrEmpty (text))
        {
            return "";
        }

        var sb = new StringBuilder ();

        foreach (var rune in text.EnumerateRunes ())
        {
            switch (rune.Value)
            {
                case '\0': sb.Append ("␀"); break;
                case '\t': sb.Append ("\\t"); break;
                case '\r': sb.Append ("\\r"); break;
                case '\n': sb.Append ("\\n"); break;
                case '\f': sb.Append ("\\f"); break;
                case '\v': sb.Append ("\\v"); break;
                default:
                    if (char.IsControl ((char)rune.Value))
                    {
                        // show as \uXXXX
                        sb.Append ($"\\u{rune.Value:X4}");
                    }
                    else
                    {
                        sb.Append (rune);
                    }
                    break;
            }
        }

        return sb.ToString ();
    }

    /// <summary>Converts the string into a <see cref="List{Cell}"/>.</summary>
    /// <param name="str">The string to convert.</param>
    /// <param name="attribute">The <see cref="Scheme"/> to use.</param>
    /// <returns></returns>
    public static List<Cell> ToCellList (string str, Attribute? attribute = null)
    {
        List<Cell> cells = [];
        cells.AddRange (GraphemeHelper.GetGraphemes (str).Select (grapheme => new Cell { Grapheme = grapheme, Attribute = attribute }));

        return cells;
    }

    /// <summary>
    ///     Splits a string into a List that will contain a <see cref="List{Cell}"/> for each line.
    /// </summary>
    /// <param name="content">The string content.</param>
    /// <param name="attribute">The scheme.</param>
    /// <returns>A <see cref="List{Cell}"/> for each line.</returns>
    public static List<List<Cell>> StringToLinesOfCells (string content, Attribute? attribute = null)
    {
        List<Cell> cells = ToCellList (content, attribute);

        return SplitNewLines (cells);
    }

    /// <summary>Converts a <see cref="Cell"/> generic collection into a string.</summary>
    /// <param name="cells">The enumerable cell to convert.</param>
    /// <returns></returns>
    public static string ToString (IEnumerable<Cell> cells)
    {
        StringBuilder sb = new ();

        foreach (Cell cell in cells)
        {
            sb.Append (cell.Grapheme);
        }

        return sb.ToString ();
    }

    /// <summary>Converts a <see cref="List{Cell}"/> generic collection into a string.</summary>
    /// <param name="cellsList">The enumerable cell to convert.</param>
    /// <returns></returns>
    public static string ToString (List<List<Cell>> cellsList)
    {
        var str = string.Empty;

        for (var i = 0; i < cellsList.Count; i++)
        {
            IEnumerable<Cell> cellList = cellsList [i];
            str += ToString (cellList);

            if (i + 1 < cellsList.Count)
            {
                str += Environment.NewLine;
            }
        }

        return str;
    }

    // Turns the string into cells, this does not split the contents on a newline if it is present.

    internal static List<Cell> StringToCells (string str, Attribute? attribute = null)
    {
        return ToCellList (str, attribute);
    }

    internal static List<Cell> ToCells (IEnumerable<string> strings, Attribute? attribute = null)
    {
        StringBuilder sb = new ();

        foreach (string str in strings)
        {
            sb.Append (str);
        }

        return ToCellList (sb.ToString (), attribute);
    }

    private static List<List<Cell>> SplitNewLines (List<Cell> cells)
    {
        List<List<Cell>> lines = [];
        int start = 0, i = 0;
        var hasCR = false;

        // ASCII code 13 = Carriage Return.
        // ASCII code 10 = Line Feed.
        for (; i < cells.Count; i++)
        {
            if (cells [i].Grapheme.Length == 1 && cells [i].Grapheme [0] == 13)
            {
                hasCR = true;

                continue;
            }

            if ((cells [i].Grapheme.Length == 1 && cells [i].Grapheme [0] == 10)
                || cells [i].Grapheme == "\r\n")
            {
                if (i - start > 0)
                {
                    lines.Add (cells.GetRange (start, hasCR ? i - 1 - start : i - start));
                }
                else
                {
                    lines.Add (StringToCells (string.Empty));
                }

                start = i + 1;
                hasCR = false;
            }
        }

        if (i - start >= 0)
        {
            lines.Add (cells.GetRange (start, i - start));
        }

        return lines;
    }

    /// <summary>
    ///     Splits a rune cell list into a List that will contain a <see cref="List{Cell}"/> for each line.
    /// </summary>
    /// <param name="cells">The cells list.</param>
    /// <returns></returns>
    public static List<List<Cell>> ToCells (List<Cell> cells) { return SplitNewLines (cells); }
}

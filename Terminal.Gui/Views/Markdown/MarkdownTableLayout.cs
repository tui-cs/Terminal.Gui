namespace Terminal.Gui.Views;

/// <summary>A completed layout in table content coordinates. Consumers never wrap text again.</summary>
internal sealed class MarkdownTableLayout
{
    public int [] ColumnWidths { get; init; } = [];
    public int [] RowHeights { get; init; } = [];
    public int Width { get; init; }
    public int Height { get; init; }
    public IReadOnlyList<MarkdownTableCellLayout> Cells { get; init; } = [];
    public IReadOnlyList<MarkdownTableGlyph> Glyphs { get; init; } = [];
    public IReadOnlyDictionary<int, IReadOnlyList<MarkdownTableGlyph>> GlyphsByRow { get; init; } = new Dictionary<int, IReadOnlyList<MarkdownTableGlyph>> ();
    public IReadOnlyList<MarkdownTableLink> Links { get; init; } = [];
    public IReadOnlyList<(Point Start, int Length, Orientation Orientation)> Borders { get; init; } = [];

    public IReadOnlyList<MarkdownTableGlyph> GetLine (int row) => GlyphsByRow.TryGetValue (row, out IReadOnlyList<MarkdownTableGlyph>? glyphs) ? glyphs : [];

    public string SelectLine (int row, int startX, int endX)
    {
        StringBuilder text = new ();
        int column = startX;

        foreach (MarkdownTableGlyph glyph in GetLine (row))
        {
            if (glyph.Position.X + glyph.Width <= startX || glyph.Position.X >= endX)
            {
                continue;
            }

            text.Append (' ', Math.Max (0, glyph.Position.X - column));
            text.Append (glyph.Text);
            column = glyph.Position.X + glyph.Width;
        }

        return text.ToString ().TrimEnd ();
    }
}

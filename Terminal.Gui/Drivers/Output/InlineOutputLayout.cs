using System.Globalization;

namespace Terminal.Gui.Drivers;

// Immutable request preparation; enumerating physical rows uses only one row of scratch storage.
internal sealed class InlineOutputLayout
{
    internal const int MaximumUnits = 4 * 1024 * 1024;
    private readonly PreparedLine [] _lines;
    internal int Units { get; }
    internal bool IsEmpty => _lines.Length == 0;

    internal InlineOutputLayout (IReadOnlyList<InlineOutputLine> lines)
    {
        ArgumentNullException.ThrowIfNull (lines);
        List<PreparedLine> snapshot = [];
        var units = 0;
        foreach (InlineOutputLine line in lines)
        {
            ArgumentNullException.ThrowIfNull (line);
            units = checked(units + 1);
            if (units > MaximumUnits)
            {
                throw new InvalidOperationException ("Inline output payload is too large.");
            }
            StringBuilder text = new ();
            foreach (InlineOutputSpan span in line.Spans)
            {
                ArgumentNullException.ThrowIfNull (span);
                ArgumentNullException.ThrowIfNull (span.Text);
                units = checked(units + Math.Max (1, span.Text.Length));
                if (units > MaximumUnits)
                {
                    throw new InvalidOperationException ("Inline output payload is too large. Submit smaller awaited batches.");
                }
                text.Append (span.Text);
            }
            string content = text.ToString ();
            ValidateText (content);
            snapshot.Add (new PreparedLine (content, line.Spans));
        }
        if (units > MaximumUnits)
        {
            throw new InvalidOperationException ("Inline output payload is too large.");
        }
        _lines = snapshot.ToArray ();
        Units = units;
    }

    private static void ValidateText (string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            char c = text [i];
            if ((c < ' ' && c != '\t') || c is >= '\u007f' and <= '\u009f')
            {
                throw new ArgumentException ("Inline text cannot contain terminal controls or line breaks.");
            }
            if (char.IsHighSurrogate (c) && i + 1 < text.Length && char.IsLowSurrogate (text [i + 1]))
            {
                i++;
                continue;
            }
            if (char.IsSurrogate (c))
            {
                throw new ArgumentException ("Inline text must contain valid UTF-16.");
            }
        }
    }

    // Count widths only: no rows, spans, grapheme strings, or expanded tab cells are allocated.
    internal int CountRows (int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero (width);
        var count = 0;
        foreach (PreparedLine line in _lines)
        {
            count = checked(count + 1);
            var column = 0;
            for (var offset = 0; offset < line.Text.Length;)
            {
                int length = StringInfo.GetNextTextElementLength (line.Text.AsSpan (offset));
                if (column == width)
                {
                    count = checked(count + 1);
                    column = 0;
                }
                if (line.Text [offset] == '\t')
                {
                    int end = column + 8 - column % 8;
                    count = checked(count + (end - 1) / width);
                    column = (end - 1) % width + 1;
                    offset += length;
                    continue;
                }
                int cells = GraphemeHelper.GetGraphemeColumns (line.Text.AsSpan (offset, length));
                if (cells > width || cells == 0 && column == 0)
                {
                    cells = 1;
                }
                if (column + cells > width)
                {
                    count = checked(count + 1);
                    column = 0;
                }
                column += cells;
                offset += length;
            }
        }
        return count;
    }

    internal IEnumerable<InlineOutputRow> Rows (int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero (width);
        RowBuilder row = new ();
        foreach (PreparedLine line in _lines)
        {
            var spanIndex = 0;
            int spanEnd = line.Spans.Count > 0 ? line.Spans [0].Text.Length : 0;
            var column = 0;
            for (var offset = 0; offset < line.Text.Length;)
            {
                int length = StringInfo.GetNextTextElementLength (line.Text.AsSpan (offset));
                while (spanIndex + 1 < line.Spans.Count && offset >= spanEnd)
                {
                    spanIndex++;
                    spanEnd += line.Spans [spanIndex].Text.Length;
                }
                Attribute? style = line.Spans [spanIndex].Attribute;
                if (column == width)
                {
                    yield return row.Build (column);
                    column = 0;
                }
                if (line.Text [offset] == '\t')
                {
                    int spaces = 8 - column % 8;
                    while (spaces > 0)
                    {
                        if (column == width)
                        {
                            yield return row.Build (column);
                            column = 0;
                        }
                        int take = Math.Min (spaces, width - column);
                        row.AppendSpaces (take, style);
                        column += take;
                        spaces -= take;
                    }
                    offset += length;
                    continue;
                }
                int cells = GraphemeHelper.GetGraphemeColumns (line.Text.AsSpan (offset, length));
                bool needsBase = cells == 0 && column == 0;
                bool needsReplacement = cells > width;
                if (needsBase || needsReplacement)
                {
                    cells = 1;
                }
                if (column + cells > width)
                {
                    yield return row.Build (column);
                    column = 0;
                }
                if (needsBase)
                {
                    row.Append ("◌", style);
                }
                if (needsReplacement)
                {
                    row.Append ("�", style);
                }
                else
                {
                    row.Append (line.Text.AsSpan (offset, length), style);
                }
                column += cells;
                offset += length;
            }
            yield return row.Build (column);
        }
    }

    private sealed record PreparedLine (string Text, IReadOnlyList<InlineOutputSpan> Spans);

    // Reuse scratch storage, allocating a span only at a style transition or the end of a row.
    private sealed class RowBuilder
    {
        private readonly List<InlineOutputSpan> _spans = [];
        private readonly StringBuilder _text = new ();
        private Attribute? _style;

        internal void Append (ReadOnlySpan<char> text, Attribute? style)
        {
            SetStyle (style);
            _text.Append (text);
        }

        internal void AppendSpaces (int count, Attribute? style)
        {
            SetStyle (style);
            _text.Append (' ', count);
        }

        internal InlineOutputRow Build (int columns)
        {
            FlushRun ();
            InlineOutputRow result = new (Array.AsReadOnly (_spans.ToArray ()), columns);
            _spans.Clear ();
            return result;
        }

        private void SetStyle (Attribute? style)
        {
            if (_style != style)
            {
                FlushRun ();
            }
            _style = style;
        }

        private void FlushRun ()
        {
            if (_text.Length == 0)
            {
                return;
            }
            _spans.Add (new InlineOutputSpan (_text.ToString (), _style));
            _text.Clear ();
        }
    }
}

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides a default implementation of <see cref="IListDataSource"/> that renders <see cref="ListView"/> items
///     using <see cref="object.ToString()"/>.
/// </summary>
public class ListWrapper<T> : IListDataSource, IListViewLayout
{
    /// <summary>
    ///     Creates a new instance of <see cref="ListWrapper{T}"/> that wraps the specified
    ///     <see cref="ObservableCollection{T}"/>.
    /// </summary>
    /// <param name="source"></param>
    public ListWrapper (ObservableCollection<T>? source)
    {
        if (source is null)
        {
            return;
        }
        _count = source.Count;
        _marks = new BitArray (_count);
        _source = source;
        _source.CollectionChanged += Source_CollectionChanged;
        MaxItemLength = GetMaxLengthItem ();
    }

    private readonly ObservableCollection<T>? _source;
    private int _count;
    private BitArray? _marks;

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public int Count => _source?.Count ?? 0;

    /// <inheritdoc/>
    public int MaxItemLength { get; private set; }

    /// <inheritdoc/>
    public bool SuspendCollectionChangedEvent
    {
        get;
        set
        {
            field = value;

            if (!field)
            {
                CheckAndResizeMarksIfRequired ();
            }
        }
    }

    /// <inheritdoc/>
    public void PrepareRender (ListView listView, int width)
    {
        if (listView.ItemHeight <= 1 && listView.ColumnHeaders is not { Count: > 0 })
        {
            return;
        }

        MeasureColumns (listView, Math.Max (width, 0));
    }

    /// <inheritdoc/>
    public void Render (ListView container, bool marked, int item, int col, int line, int width, int viewportX = 0)
    {
        if (container.ShowColumnHeaders && line == 0)
        {
            RenderHeader (container, col, line, width, viewportX);

            return;
        }

        if (container.ItemHeight <= 1 && container.ColumnHeaders is not { Count: > 0 })
        {
            container.Move (Math.Max (col - viewportX, 0), line);

            if (_source is null)
            {
                return;
            }

            object? t = _source [item];

            switch (t)
            {
                case null:
                    RenderString (container, "", width);

                    break;

                case string s:
                    RenderString (container, NewlineLine (s, 0), width, viewportX);

                    break;

                default:
                    RenderString (container, NewlineLine (t.ToString (), 0), width, viewportX);

                    break;
            }

            return;
        }

        RenderMultiLine (container, item, col, line, width, viewportX);
    }

    /// <inheritdoc/>
    public bool IsMarked (int item)
    {
        if (item >= 0 && item < _count)
        {
            return _marks! [item];
        }

        return false;
    }

    /// <inheritdoc/>
    public void SetMark (int item, bool value)
    {
        if (item >= 0 && item < _count)
        {
            _marks! [item] = value;
        }
    }

    /// <inheritdoc/>
    public IList ToList () => _source ?? [];

    /// <inheritdoc/>
    /// <remarks>
    ///     The default implementation returns <see langword="false"/>, which causes <see cref="ListView"/> to use its
    ///     built-in mark rendering. Override this method in a derived class to provide custom mark rendering.
    /// </remarks>
    public virtual bool RenderMark (ListView listView, int item, int row, bool isMarked, bool allowsMultiple) => false;

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_source is { })
        {
            _source.CollectionChanged -= Source_CollectionChanged;
        }
    }

    /// <summary>
    ///     INTERNAL: Searches the underlying collection for the first string element that starts with the specified search
    ///     value,
    ///     using a case-insensitive comparison.
    /// </summary>
    /// <remarks>
    ///     The comparison is performed in a case-insensitive manner using invariant culture rules. Only
    ///     elements of type string are considered; other types in the collection are ignored.
    /// </remarks>
    /// <param name="search">
    ///     The string value to compare against the start of each string element in the collection. Cannot be
    ///     null.
    /// </param>
    /// <returns>
    ///     The zero-based index of the first matching string element if found; otherwise, -1 if no match is found or the
    ///     collection is empty.
    /// </returns>
    internal int StartsWith (string search)
    {
        if (_source is null || _source?.Count == 0)
        {
            return -1;
        }

        for (var i = 0; i < _source!.Count; i++)
        {
            object? t = _source [i];

            if (t is string u)
            {
                if (u.ToUpper ().StartsWith (search.ToUpperInvariant (), StringComparison.Ordinal))
                {
                    return i;
                }
            }
            else if (t is string s && s.StartsWith (search, StringComparison.InvariantCultureIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void CheckAndResizeMarksIfRequired ()
    {
        if (_source == null || _count == _source.Count || _marks is null)
        {
            return;
        }
        _count = _source.Count;
        var newMarks = new BitArray (_count);

        for (var i = 0; i < Math.Min (_marks.Length, newMarks.Length); i++)
        {
            newMarks [i] = _marks [i];
        }

        _marks = newMarks;

        MaxItemLength = GetMaxLengthItem ();
    }

    private int GetMaxLengthItem ()
    {
        if (_source is null || _source?.Count == 0)
        {
            return 0;
        }

        var maxLength = 0;

        for (var i = 0; i < _source!.Count; i++)
        {
            object? t = _source [i];

            if (t is null)
            {
                continue;
            }

            int l = t is string s ? s.GetColumns () : t.ToString ()!.GetColumns ();

            if (l > maxLength)
            {
                maxLength = l;
            }
        }

        return maxLength;
    }

    private static void RenderString (View driver, string str, int width, int viewportX = 0)
    {
        if (string.IsNullOrEmpty (str) || viewportX >= str.GetColumns ())
        {
            // Empty string or viewport beyond string - just fill with spaces
            for (var i = 0; i < width; i++)
            {
                driver.AddRune ((Rune)' ');
            }

            return;
        }

        int runeLength = str.ToRunes ().Length;
        int startIndex = Math.Min (viewportX, Math.Max (0, runeLength - 1));
        string substring = str [startIndex..];
        string u = TextFormatter.ClipAndJustify (substring, width, Alignment.Start);
        driver.AddStr (u);
        width -= u.GetColumns ();

        while (width-- > 0)
        {
            driver.AddRune ((Rune)' ');
        }
    }

    private int [] _columnStarts = [];
    private int [] _columnWidths = [];

    private static string Align (string? text, int width, Alignment alignment)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        text ??= string.Empty;

        if (alignment == Alignment.Fill)
        {
            // Justify expands spaces between words, but a single word stays short. The cell must still
            // occupy exactly width columns so the next column starts at its own boundary.
            string filled = TextFormatter.ClipAndJustify (text, width, true);

            if (filled.GetColumns () > width)
            {
                filled = TextFormatter.ClipAndJustify (filled, width, false);
            }

            int missing = Math.Max (0, width - filled.GetColumns ());

            return filled + new string (' ', missing);
        }

        string clipped = text.GetColumns () > width
                             ? TextFormatter.ClipAndJustify (text, width, false)
                             : text;
        int pad = Math.Max (0, width - clipped.GetColumns ());

        if (alignment == Alignment.End)
        {
            return new string (' ', pad) + clipped;
        }

        if (alignment == Alignment.Center)
        {
            int left = pad / 2;

            return new string (' ', left) + clipped + new string (' ', pad - left);
        }

        return clipped + new string (' ', pad);
    }

    private string BuildRowText (ListView listView, object? value, int row, bool header, int width)
    {
        int columnCount = ColumnCount (listView);

        if (listView.ColumnHeaders is not { Count: > 0 } || _columnWidths.Length == 0)
        {
            if (header)
            {
                return " ";
            }

            if (listView.ItemHeight <= 1)
            {
                return CellLine (listView, value, 0, row);
            }

            string text = value as string ?? value?.ToString () ?? string.Empty;

            // Newlines start a new row. A line wider than the column wraps onto the rows that remain.

            return Align (
                          AlignedWrappedLine (text, width, row, listView.ItemHeight, listView.ItemsVerticalAlignment),
                          width,
                          listView.ItemsHorizontalAlignment);
        }

        StringBuilder builder = new ();
        int margin = Math.Max (0, listView.ColumnMargin);

        for (int c = 0; c < columnCount && c < _columnWidths.Length; c++)
        {
            if (c < _columnStarts.Length && _columnStarts [c] < 0)
            {
                continue;
            }

            int columnWidth = _columnWidths [c];
            int gap = c < columnCount - 1 ? margin : 0;
            int contentWidth = Math.Max (0, columnWidth - gap);
            ListView.ColumnHeader? columnHeader = HeaderAt (listView, c);
            string text = header
                              ? HeaderTitle (columnHeader, c)
                              : CellText (listView, value, c, row, contentWidth);
            Alignment alignment = header
                                      ? columnHeader?.HeaderAlignment ?? listView.HeadersAlignment
                                      : columnHeader?.MemberAlignment ?? listView.ItemsHorizontalAlignment;
            builder.Append (Align (text, contentWidth, alignment));

            if (gap > 0)
            {
                builder.Append (new string (' ', gap));
            }
        }

        return builder.ToString ();
    }

    /// <summary>The newline segment at <paramref name="line"/>, or empty when the item has fewer lines.</summary>
    private static string NewlineLine (string? text, int line)
    {
        if (string.IsNullOrEmpty (text) || line < 0)
        {
            return string.Empty;
        }

        List<string> lines = TextFormatter.SplitNewLine (text);

        if (line >= lines.Count)
        {
            return string.Empty;
        }

        return lines [line];
    }

    /// <summary>
    ///     One wrapped row of <paramref name="text"/> after <paramref name="verticalAlignment"/>. Newlines stay hard
    ///     breaks. A line wider than <paramref name="width"/> continues on the following rows.
    /// </summary>
    private static string AlignedWrappedLine (string? text, int width, int row, int height, Alignment verticalAlignment)
    {
        if (width <= 0 || row < 0 || height <= 0)
        {
            return string.Empty;
        }

        List<string> lines = WrapLines (text, width);
        int index = ContentLineIndex (lines.Count, height, row, verticalAlignment);

        if (index < 0 || index >= lines.Count)
        {
            return string.Empty;
        }

        return lines [index];
    }

    private static List<string> WrapLines (string? text, int width)
    {
        text ??= string.Empty;
        List<string> lines = [];

        foreach (string paragraph in TextFormatter.SplitNewLine (text))
        {
            if (paragraph.GetColumns () <= width)
            {
                lines.Add (paragraph);

                continue;
            }

            lines.AddRange (TextFormatter.Format (paragraph, width, justify: false, wordWrap: true));
        }

        return lines;
    }

    /// <summary>Maps a row inside an item to a content line, or -1 when that row is padding.</summary>
    private static int ContentLineIndex (int lineCount, int height, int row, Alignment alignment)
    {
        if (row < 0 || row >= height || lineCount <= 0)
        {
            return -1;
        }

        if (alignment == Alignment.End)
        {
            int index = row - (height - lineCount);

            if (index < 0 || index >= lineCount)
            {
                return -1;
            }

            return index;
        }

        if (alignment == Alignment.Center && lineCount <= height)
        {
            int index = row - ((height - lineCount) / 2);

            if (index < 0 || index >= lineCount)
            {
                return -1;
            }

            return index;
        }

        if (alignment == Alignment.Fill && lineCount > 1 && lineCount <= height)
        {
            return FilledLineIndex (lineCount, height, row);
        }

        if (row >= lineCount)
        {
            return -1;
        }

        return row;
    }

    /// <summary>
    ///     Spreads <paramref name="lineCount"/> lines across <paramref name="height"/> rows. Extra rows sit between the
    ///     lines, and any remainder goes to the later gaps.
    /// </summary>
    private static int FilledLineIndex (int lineCount, int height, int row)
    {
        int extra = height - lineCount;
        int gaps = lineCount - 1;
        int baseGap = extra / gaps;
        int remainder = extra % gaps;
        int cursor = 0;

        for (int i = 0; i < lineCount; i++)
        {
            if (cursor == row)
            {
                return i;
            }

            if (i == lineCount - 1)
            {
                break;
            }

            int gap = baseGap;

            if (i >= gaps - remainder)
            {
                gap++;
            }

            cursor += 1 + gap;
        }

        return -1;
    }

    /// <summary>
    ///     Cell text before alignment. A column value wider than its width wraps when <see cref="ListView.ItemHeight"/>
    ///     is greater than one.
    /// </summary>
    private static string CellText (ListView listView, object? value, int column, int line, int width)
    {
        if (listView.ItemHeight > 1 && TryGetColumnText (listView, value, column, out string columnText))
        {
            return AlignedWrappedLine (columnText, width, line, listView.ItemHeight, listView.ItemsVerticalAlignment);
        }

        return CellLine (listView, value, column, line);
    }

    /// <summary>
    ///     Full text of one column. A property comes from <see cref="ListView.ColumnHeader.MemberName"/>. A string item
    ///     is the first column. A list or array item places element <paramref name="column"/> in that column. Line
    ///     breaks stay in the column that contains them.
    /// </summary>
    private static bool TryGetColumnText (ListView listView, object? value, int column, out string text)
    {
        text = string.Empty;

        if (value is null)
        {
            return false;
        }

        if (value is not string && HeaderAt (listView, column) is { MemberName.Length: > 0 } header)
        {
            text = FormatMember (value, header);

            return true;
        }

        if (value is string source && column == 0)
        {
            text = source;

            return true;
        }

        if (value is not IList cells || column < 0 || column >= cells.Count)
        {
            return false;
        }

        object? cell = cells [column];

        if (cell is null)
        {
            text = " ";

            return true;
        }

        if (HeaderAt (listView, column) is { } cellHeader)
        {
            text = FormatText (cell.ToString () ?? string.Empty, cellHeader);

            return true;
        }

        text = cell.ToString () ?? " ";

        return true;
    }

    /// <summary>
    ///     Text for one column on one row of an item. Each column keeps its own value. Further lines of that value use
    ///     the extra rows when <see cref="ListView.ItemHeight"/> is greater than one.
    /// </summary>
    private static string CellLine (ListView listView, object? value, int column, int line)
    {
        if (value is null)
        {
            return " ";
        }

        if (TryGetColumnText (listView, value, column, out string columnText))
        {
            List<string> columnLines = TextFormatter.SplitNewLine (columnText);

            if (line < 0 || line >= columnLines.Count)
            {
                return " ";
            }

            return columnLines [line];
        }

        if (listView.ColumnHeaders is { Count: > 0 })
        {
            return " ";
        }

        string text = value as string ?? value.ToString () ?? string.Empty;
        List<string> split = TextFormatter.SplitNewLine (text);

        if (line < 0 || line >= split.Count)
        {
            return " ";
        }

        return split [line];
    }

    private static int ColumnCount (ListView listView)
    {
        List<ListView.ColumnHeader>? headers = listView.ColumnHeaders;

        if (headers is not { Count: > 0 })
        {
            return 1;
        }

        return headers.Count;
    }

    private static string FormatMember (object source, ListView.ColumnHeader header)
    {
        if (string.IsNullOrEmpty (header.MemberName))
        {
            return " ";
        }

#pragma warning disable IL2075 // MemberName is an app-supplied public property.
        PropertyInfo? property = source.GetType ().GetProperty (header.MemberName);
#pragma warning restore IL2075
        object? raw = property?.GetValue (source);

        if (raw is null)
        {
            return " ";
        }

        if (string.IsNullOrEmpty (header.MemberFormat))
        {
            return raw.ToString () ?? " ";
        }

        object converted = Convert.ChangeType (raw, header.MemberType);

        return string.Format (header.MemberFormat, converted);
    }

    private static string FormatText (string text, ListView.ColumnHeader header)
    {
        object converted = Convert.ChangeType (text, header.MemberType);

        return string.Format (header.MemberFormat ?? "{0}", converted);
    }

    private static ListView.ColumnHeader? HeaderAt (ListView listView, int index)
    {
        List<ListView.ColumnHeader>? headers = listView.ColumnHeaders;

        if (headers is not { Count: > 0 } || index < 0 || index >= headers.Count)
        {
            return null;
        }

        return headers [index];
    }

    private static string HeaderTitle (ListView.ColumnHeader? header, int index)
    {
        if (header is not null && !string.IsNullOrEmpty (header.HeaderName))
        {
            return header.HeaderName;
        }

        if (header is not null && !string.IsNullOrEmpty (header.MemberName))
        {
            return header.MemberName;
        }

        return $"Col{index}";
    }

    private void MeasureColumns (ListView listView, int width)
    {
        int height = Math.Max (1, listView.ItemHeight);
        int columnCount = ColumnCount (listView);
        int margin = Math.Max (0, listView.ColumnMargin);
        _columnStarts = new int [columnCount];
        _columnWidths = new int [columnCount];
        int used = 0;

        for (int c = 0; c < columnCount; c++)
        {
            ListView.ColumnHeader? header = HeaderAt (listView, c);

            if (header is { Width: -1 })
            {
                _columnStarts [c] = -1;
                _columnWidths [c] = 0;

                continue;
            }

            int contentWidth = MeasureCell (listView, c, height);
            int columnWidth;
            int remaining = Math.Max (0, width - used);

            if (header is null || header.Width == 0)
            {
                if (c == columnCount - 1)
                {
                    columnWidth = Math.Max (contentWidth, remaining);

                    // A tall item can show the rest of a long value on its extra rows. Keep the last auto
                    // column inside the viewport so that text wraps instead of widening the list.
                    if (listView.ItemHeight > 1 && remaining > 0 && columnWidth > remaining)
                    {
                        columnWidth = remaining;
                    }
                }
                else
                {
                    columnWidth = contentWidth;

                    if (listView.ItemHeight > 1 && columnWidth > 0)
                    {
                        int later = columnCount - c - 1;
                        int reserved = later * (1 + margin);
                        int available = Math.Max (1, remaining - reserved);

                        if (columnWidth > available)
                        {
                            columnWidth = available;
                        }
                    }
                }
            }
            else
            {
                columnWidth = Math.Max (header.Width, 0);
            }

            int gap = c < columnCount - 1 ? margin : 0;
            _columnStarts [c] = used;
            _columnWidths [c] = columnWidth + gap;
            used += _columnWidths [c];
        }

        MaxItemLength = Math.Max (used, 0);
    }

    private int MeasureCell (ListView listView, int column, int height)
    {
        int max = 0;
        ListView.ColumnHeader? header = HeaderAt (listView, column);

        if (header is not null)
        {
            max = HeaderTitle (header, column).GetColumns ();
        }

        if (_source is null)
        {
            return max;
        }

        for (int i = 0; i < _source.Count; i++)
        {
            for (int line = 0; line < height; line++)
            {
                max = Math.Max (max, CellLine (listView, _source [i], column, line).GetColumns ());
            }
        }

        return max;
    }

    private void RenderHeader (ListView container, int col, int line, int width, int viewportX)
    {
        if (_columnWidths.Length == 0 && container.ColumnHeaders is { Count: > 0 })
        {
            MeasureColumns (container, Math.Max (width, 0));
        }

        string text = container.ColumnHeaders is not { Count: > 0 }
                          ? Align ("Col0", Math.Max (width, 0), container.HeadersAlignment)
                          : BuildRowText (container, null, 0, true, width);
        container.Move (Math.Max (col - viewportX, 0), line);
        RenderString (container, text, width, viewportX);
    }

    private void RenderMultiLine (ListView container, int item, int col, int line, int width, int viewportX)
    {
        if (_columnWidths.Length == 0 && (container.ColumnHeaders is { Count: > 0 } || container.ItemHeight > 1))
        {
            MeasureColumns (container, Math.Max (width, 0));
        }

        object? value = _source is not null && item >= 0 && item < _source.Count ? _source [item] : null;
        int height = Math.Max (1, container.ItemHeight);

        for (int r = 0; r < height; r++)
        {
            if (line + r >= container.Viewport.Height)
            {
                break;
            }

            container.Move (Math.Max (col - viewportX, 0), line + r);
            RenderString (container, BuildRowText (container, value, r, false, width), width, viewportX);
        }
    }

    private void Source_CollectionChanged (object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (SuspendCollectionChangedEvent)
        {
            return;
        }
        CheckAndResizeMarksIfRequired ();
        CollectionChanged?.Invoke (sender, e);
    }
}

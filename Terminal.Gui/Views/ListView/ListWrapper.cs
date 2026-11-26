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

    private int _widestItemLength;

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
        width = Math.Max (width, 0);

        if (listView.ColumnHeaders is not { Count: > 0 })
        {
            if (!listView.ItemAutoHeight && listView.ItemHeight <= 1)
            {
                // One row and no wrap. Restore the widest item so a previous auto-height pass
                // does not leave the content clamped to the viewport.
                MaxItemLength = _widestItemLength;

                return;
            }

            // Wrapped lines stay inside the viewport, so the content is not wider than that.
            if (listView.ItemAutoHeight)
            {
                if (width > 0)
                {
                    MaxItemLength = width;
                }

                return;
            }
        }

        MeasureColumns (listView, width);
    }

    /// <inheritdoc/>
    public int MeasureItemHeight (ListView listView, int item)
    {
        if (_source is null || item < 0 || item >= _source.Count)
        {
            return 1;
        }

        int available = Math.Max (0, listView.Viewport.Width - (listView.ShowMarks ? 2 : 0));

        if (listView.ColumnHeaders is not { Count: > 0 })
        {
            object? value = _source [item];
            string text = value as string ?? value?.ToString () ?? string.Empty;

            return LineCount (text, available);
        }

        if (_columnWidths.Length == 0)
        {
            MeasureColumns (listView, available);
        }

        return ColumnLineCount (listView, _source [item]);
    }

    /// <inheritdoc/>
    public void Render (ListView container, bool marked, int item, int col, int line, int width, int viewportX = 0)
    {
        if (container.ShowColumnHeaders && line == 0)
        {
            RenderHeader (container, col, line, width, viewportX);

            return;
        }

        if (!container.ItemAutoHeight && container.ItemHeight <= 1 && container.ColumnHeaders is not { Count: > 0 })
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
        if (_source is null || _source.Count == 0)
        {
            _widestItemLength = 0;

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

        _widestItemLength = maxLength;

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

    private string BuildRowText (ListView listView, object? value, int row, bool header, int width, int itemHeight)
    {
        int columnCount = ColumnCount (listView);

        if (listView.ColumnHeaders is not { Count: > 0 } || _columnWidths.Length == 0)
        {
            if (header)
            {
                return " ";
            }

            if (!listView.ItemAutoHeight && listView.ItemHeight <= 1)
            {
                return CellLine (listView, value, 0, row);
            }

            string text = value as string ?? value?.ToString () ?? string.Empty;

            // Newlines start a new row. A line wider than the column wraps onto the rows that remain.

            return Align (
                          AlignedWrappedLine (text, width, row, itemHeight, listView.ItemsVerticalAlignment),
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
                              ? HeaderLine (listView, columnHeader, c, row, contentWidth)
                              : CellText (listView, value, c, row, contentWidth, itemHeight);
            Alignment alignment = header
                                      ? columnHeader?.HeaderHorizontalAlignment ?? listView.HeadersAlignment
                                      : columnHeader?.MemberHorizontalAlignment ?? listView.ItemsHorizontalAlignment;
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

    /// <summary>
    ///     One header row. A height greater than one wraps a line that is wider than <paramref name="width"/>. A height of
    ///     one keeps the first line.
    /// </summary>
    internal static string HeaderTextLine (string? text, int row, int height, int width, Alignment verticalAlignment)
    {
        if (height > 1 && width > 0)
        {
            return AlignedWrappedLine (text, width, row, height, verticalAlignment);
        }

        return PlainLine (text, row, height, verticalAlignment);
    }

    /// <summary>
    ///     One hard-broken row of <paramref name="text"/> after <paramref name="verticalAlignment"/>. Newlines split the
    ///     text. A height of one keeps the first line.
    /// </summary>
    internal static string PlainLine (string? text, int row, int height, Alignment verticalAlignment)
    {
        if (row < 0 || height <= 0)
        {
            return string.Empty;
        }

        List<string> lines = TextFormatter.SplitNewLine (text ?? string.Empty);

        if (lines.Count == 0)
        {
            return string.Empty;
        }

        if (height <= 1)
        {
            if (row > 0)
            {
                return string.Empty;
            }

            return lines [0];
        }

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
    private static string CellText (ListView listView, object? value, int column, int line, int width, int itemHeight)
    {
        if (itemHeight > 1 && TryGetColumnText (listView, value, column, out string columnText))
        {
            Alignment vertical = HeaderAt (listView, column) is { } header
                                     ? header.MemberVerticalAlignment
                                     : listView.ItemsVerticalAlignment;

            return AlignedWrappedLine (columnText, width, line, itemHeight, vertical);
        }

        return CellLine (listView, value, column, line);
    }

    /// <summary>Wrapped rows of <paramref name="text"/> at <paramref name="width"/>, or hard breaks when the width is unknown.</summary>
    private static int LineCount (string? text, int width)
    {
        if (string.IsNullOrEmpty (text))
        {
            return 1;
        }

        if (width <= 0)
        {
            return Math.Max (1, TextFormatter.SplitNewLine (text).Count);
        }

        return Math.Max (1, WrapLines (text, width).Count);
    }

    /// <summary>Tallest wrapped column of one item. The item is at least one row.</summary>
    private int ColumnLineCount (ListView listView, object? value)
    {
        int columnCount = Math.Min (ColumnCount (listView), _columnWidths.Length);
        int margin = Math.Max (0, listView.ColumnMargin);
        int height = 1;

        for (int c = 0; c < columnCount; c++)
        {
            if (c < _columnStarts.Length && _columnStarts [c] < 0)
            {
                continue;
            }

            if (!TryGetColumnText (listView, value, c, out string columnText))
            {
                continue;
            }

            int gap = c < columnCount - 1 ? margin : 0;
            int contentWidth = Math.Max (0, _columnWidths [c] - gap);
            height = Math.Max (height, LineCount (columnText, contentWidth));
        }

        return height;
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

    /// <summary>
    ///     Header text for one row inside <see cref="ListView.ColumnHeader.Height"/>. A line wider than
    ///     <paramref name="width"/> wraps when the header is taller than one row. Later rows of a taller band are blank.
    /// </summary>
    private static string HeaderLine (ListView listView, ListView.ColumnHeader? columnHeader, int column, int row, int width)
    {
        int height = Math.Max (1, columnHeader?.Height ?? 1);
        Alignment vertical = columnHeader?.HeaderVerticalAlignment ?? listView.HeadersVerticalAlignment;

        return HeaderTextLine (HeaderTitle (columnHeader, column), row, height, width, vertical);
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

            bool wrapInsideViewport = listView.ItemAutoHeight || listView.ItemHeight > 1;
            int contentWidth = header is null || header.Width == 0 ? MeasureCell (listView, c, height) : 0;
            int columnWidth;
            int remaining = Math.Max (0, width - used);

            if (header is null || header.Width == 0)
            {
                if (c == columnCount - 1)
                {
                    columnWidth = Math.Max (contentWidth, remaining);

                    // A tall item can show the rest of a long value on its extra rows. Keep the last auto
                    // column inside the viewport so that text wraps instead of widening the list.
                    if (wrapInsideViewport && remaining > 0 && columnWidth > remaining)
                    {
                        columnWidth = remaining;
                    }
                }
                else
                {
                    columnWidth = contentWidth;

                    if (wrapInsideViewport && columnWidth > 0)
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
            max = TextFormatter.GetWidestLineLength (HeaderTitle (header, column));
        }

        if (listView.ShowColumnFooters && FooterAt (listView, column) is { } footer)
        {
            string formatted = ListView.FormatFooterText (footer, header);
            max = Math.Max (max, TextFormatter.GetWidestLineLength (formatted));
        }

        if (_source is null)
        {
            return max;
        }

        for (int i = 0; i < _source.Count; i++)
        {
            if (listView.ItemAutoHeight && TryGetColumnText (listView, _source [i], column, out string columnText))
            {
                max = Math.Max (max, TextFormatter.GetWidestLineLength (columnText));

                continue;
            }

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

        int band = container.HeaderRowCount;

        for (int r = 0; r < band; r++)
        {
            if (line + r >= container.Viewport.Height)
            {
                break;
            }

            string text = container.ColumnHeaders is not { Count: > 0 }
                              ? r == 0
                                    ? Align ("Col0", Math.Max (width, 0), container.HeadersAlignment)
                                    : string.Empty
                              : BuildRowText (container, null, r, true, width, 1);
            container.Move (Math.Max (col - viewportX, 0), line + r);
            RenderString (container, text, width, viewportX);
        }
    }

    /// <inheritdoc/>
    public void RenderColumnFooter (ListView listView, int col, int line, int width, int viewportX)
    {
        if (_columnWidths.Length == 0 && listView.ColumnHeaders is { Count: > 0 })
        {
            MeasureColumns (listView, Math.Max (width, 0));
        }

        int band = listView.FooterRowCount;

        for (int r = 0; r < band; r++)
        {
            if (line + r >= listView.Viewport.Height)
            {
                break;
            }

            listView.Move (Math.Max (col - viewportX, 0), line + r);
            RenderString (listView, FooterRowText (listView, r, width), width, viewportX);
        }
    }

    private string FooterRowText (ListView listView, int row, int width)
    {
        List<ListView.ColumnFooter>? footers = listView.ColumnFooters;

        if (footers is not { Count: > 0 })
        {
            if (row > 0)
            {
                return string.Empty;
            }

            return Align ("Footer0", Math.Max (width, 0), listView.FootersHorizontalAlignment);
        }

        if (listView.ColumnHeaders is not { Count: > 0 } || _columnWidths.Length == 0)
        {
            ListView.ColumnFooter footer = footers [0];
            ListView.ColumnHeader? header = HeaderAt (listView, 0);
            int footerHeight = Math.Max (1, footer.Height);

            if (row < 0 || row >= footerHeight)
            {
                return Align (string.Empty, width, footer.ResolveHorizontalAlignment (header, listView.FootersHorizontalAlignment));
            }

            string formatted = ListView.FormatFooterText (footer, header);
            string line = HeaderTextLine (
                                           formatted,
                                           row,
                                           footerHeight,
                                           width,
                                           footer.ResolveVerticalAlignment (header, listView.FootersVerticalAlignment));

            return Align (line, width, footer.ResolveHorizontalAlignment (header, listView.FootersHorizontalAlignment));
        }

        StringBuilder builder = new ();
        int columnCount = ColumnCount (listView);
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
            ListView.ColumnFooter? footer = c < footers.Count ? footers [c] : null;
            string line = string.Empty;
            Alignment horizontal = listView.FootersHorizontalAlignment;

            if (footer is not null)
            {
                int footerHeight = Math.Max (1, footer.Height);
                horizontal = footer.ResolveHorizontalAlignment (columnHeader, listView.FootersHorizontalAlignment);

                if (row >= 0 && row < footerHeight)
                {
                    string formatted = ListView.FormatFooterText (footer, columnHeader);
                    line = HeaderTextLine (
                                            formatted,
                                            row,
                                            footerHeight,
                                            contentWidth,
                                            footer.ResolveVerticalAlignment (columnHeader, listView.FootersVerticalAlignment));
                }
            }

            builder.Append (Align (line, contentWidth, horizontal));

            if (gap > 0)
            {
                builder.Append (new string (' ', gap));
            }
        }

        return builder.ToString ();
    }

    private static ListView.ColumnFooter? FooterAt (ListView listView, int index)
    {
        List<ListView.ColumnFooter>? footers = listView.ColumnFooters;

        if (footers is not { Count: > 0 } || index < 0 || index >= footers.Count)
        {
            return null;
        }

        return footers [index];
    }

    private void RenderMultiLine (ListView container, int item, int col, int line, int width, int viewportX)
    {
        if (_columnWidths.Length == 0
            && (container.ColumnHeaders is { Count: > 0 } || (!container.ItemAutoHeight && container.ItemHeight > 1)))
        {
            MeasureColumns (container, Math.Max (width, 0));
        }

        object? value = _source is not null && item >= 0 && item < _source.Count ? _source [item] : null;
        int height = container.ItemAutoHeight
                         ? Math.Max (1, MeasureItemHeight (container, item))
                         : Math.Max (1, container.ItemHeight);

        for (int r = 0; r < height; r++)
        {
            if (line + r >= container.Viewport.Height)
            {
                break;
            }

            container.Move (Math.Max (col - viewportX, 0), line + r);
            RenderString (container, BuildRowText (container, value, r, false, width, height), width, viewportX);
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

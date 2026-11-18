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
                    RenderString (container, s, width, viewportX);

                    break;

                default:
                    RenderString (container, t.ToString ()!, width, viewportX);

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
            return TextFormatter.ClipAndJustify (text, width, true);
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

    private string BuildRowText (ListView listView, List<string> lines, int row, bool header)
    {
        int height = Math.Max (1, listView.ItemHeight);
        int columnCount = ColumnCount (listView);

        if (listView.ColumnHeaders is not { Count: > 0 } || _columnWidths.Length == 0)
        {
            int index = Math.Max (row, 0);

            if (index >= lines.Count)
            {
                return " ";
            }

            return lines [index];
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
            ListView.ColumnHeader? columnHeader = HeaderAt (listView, header ? c : ColumnIndex (c, row, columnCount, height));
            string text = header
                              ? HeaderTitle (columnHeader, c)
                              : CellAt (lines, c, row, columnCount, height);
            Alignment alignment = header
                                      ? columnHeader?.HeaderAlignment ?? listView.HeadersAlignment
                                      : columnHeader?.MemberAlignment ?? listView.ItemsAlignment;
            builder.Append (Align (text, contentWidth, alignment));

            if (gap > 0)
            {
                builder.Append (new string (' ', gap));
            }
        }

        return builder.ToString ();
    }

    private static string CellAt (List<string> lines, int column, int row, int columnCount, int height)
    {
        int index = columnCount > height ? column : row;

        if (index < 0 || index >= lines.Count)
        {
            return " ";
        }

        return lines [index];
    }

    private static string CellText (ListView listView, object? value, int index)
    {
        ListView.ColumnHeader? header = HeaderAt (listView, index);

        if (value is null)
        {
            return " ";
        }

        if (value is not string && header is { MemberName.Length: > 0 })
        {
            return FormatMember (value, header);
        }

        string text = value as string ?? value.ToString () ?? string.Empty;
        List<string> split = TextFormatter.SplitNewLine (text);
        string piece = index < split.Count ? split [index] : " ";

        if (header is null || string.IsNullOrEmpty (header.MemberFormat))
        {
            return piece;
        }

        return FormatText (piece, header);
    }

    private static int ColumnCount (ListView listView)
    {
        int height = Math.Max (1, listView.ItemHeight);
        List<ListView.ColumnHeader>? headers = listView.ColumnHeaders;

        if (headers is not { Count: > 0 })
        {
            return 1;
        }

        return Math.Max (Math.Max (headers.Count, 1) / height, 1);
    }

    private static int ColumnIndex (int column, int row, int columnCount, int height)
    {
        if (columnCount > height)
        {
            return column;
        }

        return row;
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

    private List<string> GetItemLines (ListView listView, int item)
    {
        int height = Math.Max (1, listView.ItemHeight);
        int columnCount = ColumnCount (listView);
        int count = columnCount > height ? columnCount : height;
        object? value = _source is not null && item >= 0 && item < _source.Count ? _source [item] : null;
        List<string> lines = [];

        for (int i = 0; i < count; i++)
        {
            lines.Add (CellText (listView, value, i));
        }

        return lines;
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

            int contentWidth = MeasureCell (listView, c, columnCount, height);
            int columnWidth;

            if (header is null || header.Width == 0)
            {
                if (c == columnCount - 1)
                {
                    columnWidth = Math.Max (contentWidth, Math.Max (0, width - used));
                }
                else
                {
                    columnWidth = contentWidth;
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

    private int MeasureCell (ListView listView, int column, int columnCount, int height)
    {
        int max = 0;
        ListView.ColumnHeader? header = HeaderAt (listView, column);

        if (header is not null && columnCount > height)
        {
            max = HeaderTitle (header, column).GetColumns ();
        }
        else if (columnCount <= height && listView.ColumnHeaders is { Count: > 0 })
        {
            foreach (ListView.ColumnHeader columnHeader in listView.ColumnHeaders)
            {
                max = Math.Max (max, HeaderTitle (columnHeader, 0).GetColumns ());
            }
        }

        if (_source is null)
        {
            return max;
        }

        for (int i = 0; i < _source.Count; i++)
        {
            List<string> lines = GetItemLines (listView, i);

            if (columnCount > height)
            {
                if (column < lines.Count)
                {
                    max = Math.Max (max, lines [column].GetColumns ());
                }

                continue;
            }

            foreach (string line in lines)
            {
                max = Math.Max (max, line.GetColumns ());
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
                          : BuildRowText (container, [], 0, true);
        container.Move (Math.Max (col - viewportX, 0), line);
        RenderString (container, text, width, viewportX);
    }

    private void RenderMultiLine (ListView container, int item, int col, int line, int width, int viewportX)
    {
        if (_columnWidths.Length == 0 && (container.ColumnHeaders is { Count: > 0 } || container.ItemHeight > 1))
        {
            MeasureColumns (container, Math.Max (width, 0));
        }

        List<string> lines = GetItemLines (container, item);
        int height = Math.Max (1, container.ItemHeight);

        for (int r = 0; r < height; r++)
        {
            if (line + r >= container.Viewport.Height)
            {
                break;
            }

            container.Move (Math.Max (col - viewportX, 0), line + r);
            RenderString (container, BuildRowText (container, lines, r, false), width, viewportX);
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

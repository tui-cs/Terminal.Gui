using System.Buffers;
using System.Diagnostics;

namespace Terminal.Gui.Text;

/// <summary>
///     Provides text formatting. Supports <see cref="View.HotKey"/>s, horizontal and vertical alignment, text direction,
///     multiple lines, and word-based line wrap.
/// </summary>
public class TextFormatter
{
    // Utilized in CRLF related helper methods for faster newline char index search.
    private static readonly SearchValues<char> NewlineSearchValues = SearchValues.Create (['\r', '\n']);

    private Key _hotKey = new ();
    private int _hotKeyPos = -1;
    private List<string> _lines = new ();
    private bool _multiLine;
    private bool _preserveTrailingSpaces;
    private int _tabWidth = 4;
    private string? _text;
    private Alignment _textAlignment = Alignment.Start;
    private TextDirection _textDirection;
    private Alignment _textVerticalAlignment = Alignment.Start;
    private bool _wordWrap = true;

    /// <summary>Get or sets the horizontal text alignment.</summary>
    /// <value>The text alignment.</value>
    public Alignment Alignment
    {
        get => _textAlignment;
        set => _textAlignment = EnableNeedsFormat (value);
    }

    /// <summary>
    ///     Gets the cursor position of the <see cref="HotKey"/>. If the <see cref="HotKey"/> is defined, the cursor will
    ///     be positioned over it.
    /// </summary>
    public int CursorPosition { get; internal set; }

    /// <summary>Gets or sets the text-direction.</summary>
    /// <value>The text direction.</value>
    public TextDirection Direction
    {
        get => _textDirection;
        set => _textDirection = EnableNeedsFormat (value);
    }

    /// <summary>Draws the text held by <see cref="TextFormatter"/> to <see cref="IDriver"/> using the colors specified.</summary>
    /// <remarks>
    ///     Causes the text to be formatted (references <see cref="GetLines"/>). Sets <see cref="NeedsFormat"/> to
    ///     <c>false</c>.
    /// </remarks>
    /// <param name="driver">The console driver currently used by the application.</param>
    /// <param name="screen">Specifies the screen-relative location and maximum size for drawing the text.</param>
    /// <param name="normalColor">The color to use for all text except the hotkey</param>
    /// <param name="hotColor">The color to use to draw the hotkey</param>
    /// <param name="maximum">Specifies the screen-relative location and maximum container size.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Draw (
        IDriver? driver,
        Rectangle screen,
        Attribute normalColor,
        Attribute hotColor,
        Rectangle maximum = default
    )
    {
        ArgumentNullException.ThrowIfNull (driver);

        // With this check, we protect against subclasses with overrides of Text (like Button)
        if (string.IsNullOrEmpty (Text))
        {
            return;
        }

        driver?.SetAttribute (normalColor);

        List<string> linesFormatted = GetLines ();

        bool isVertical = IsVerticalDirection (Direction);
        Rectangle maxScreen = screen;

        if (driver is { })
        {
            // INTENT: What, exactly, is the intent of this?
            maxScreen = maximum == default (Rectangle)
                            ? screen
                            : new (
                                   Math.Max (maximum.X, screen.X),
                                   Math.Max (maximum.Y, screen.Y),
                                   Math.Max (
                                             Math.Min (maximum.Width, maximum.Right - screen.Left),
                                             0
                                            ),
                                   Math.Max (
                                             Math.Min (
                                                       maximum.Height,
                                                       maximum.Bottom - screen.Top
                                                      ),
                                             0
                                            )
                                  );
        }

        if (maxScreen.Width == 0 || maxScreen.Height == 0)
        {
            return;
        }

        int lineOffset = !isVertical && screen.Y < 0 ? Math.Abs (screen.Y) : 0;

        for (int line = lineOffset; line < linesFormatted.Count; line++)
        {
            if ((isVertical && line > screen.Width) || (!isVertical && line > screen.Height))
            {
                continue;
            }

            if ((isVertical && line >= maxScreen.Left + maxScreen.Width)
                || (!isVertical && line >= maxScreen.Top + maxScreen.Height + lineOffset))
            {
                break;
            }

            string strings = linesFormatted [line];

            // Use ArrayPool to avoid per-draw allocations
            int estimatedCount = strings.Length + 10; // Add buffer for grapheme clusters
            string [] graphemes = ArrayPool<string>.Shared.Rent (estimatedCount);
            var graphemeCount = 0;

            try
            {
                foreach (string grapheme in GraphemeHelper.GetGraphemes (strings))
                {
                    if (graphemeCount >= graphemes.Length)
                    {
                        // Need larger array (rare case for complex text)
                        string [] larger = ArrayPool<string>.Shared.Rent (graphemes.Length * 2);
                        Array.Copy (graphemes, larger, graphemeCount);
                        ArrayPool<string>.Shared.Return (graphemes, clearArray: true);
                        graphemes = larger;
                    }

                    graphemes [graphemeCount++] = grapheme;
                }

                // When text is justified, we lost left or right, so we use the direction to align.

                int x = 0, y = 0;

                // Horizontal Alignment
                if (Alignment is Alignment.End)
                {
                    if (isVertical)
                    {
                        int runesWidth = GetColumnsRequiredForVerticalText (linesFormatted, line, linesFormatted.Count - line, TabWidth);
                        x = screen.Right - runesWidth;
                        CursorPosition = screen.Width - runesWidth + (_hotKeyPos > -1 ? _hotKeyPos : 0);
                    }
                    else
                    {
                        int runesWidth = strings.GetColumns ();
                        x = screen.Right - runesWidth;
                        CursorPosition = screen.Width - runesWidth + (_hotKeyPos > -1 ? _hotKeyPos : 0);
                    }
                }
                else if (Alignment is Alignment.Start)
                {
                    if (isVertical)
                    {
                        int runesWidth = line > 0
                                             ? GetColumnsRequiredForVerticalText (linesFormatted, 0, line, TabWidth)
                                             : 0;
                        x = screen.Left + runesWidth;
                    }
                    else
                    {
                        x = screen.Left;
                    }

                    CursorPosition = _hotKeyPos > -1 ? _hotKeyPos : 0;
                }
                else if (Alignment is Alignment.Fill)
                {
                    if (isVertical)
                    {
                        int runesWidth = GetColumnsRequiredForVerticalText (linesFormatted, 0, linesFormatted.Count, TabWidth);
                        int prevLineWidth = line > 0 ? GetColumnsRequiredForVerticalText (linesFormatted, line - 1, 1, TabWidth) : 0;
                        int firstLineWidth = GetColumnsRequiredForVerticalText (linesFormatted, 0, 1, TabWidth);
                        int lastLineWidth = GetColumnsRequiredForVerticalText (linesFormatted, linesFormatted.Count - 1, 1, TabWidth);
                        var interval = (int)Math.Round ((double)(screen.Width + firstLineWidth + lastLineWidth) / linesFormatted.Count);

                        x = line == 0
                                ? screen.Left
                                : line < linesFormatted.Count - 1
                                    ? screen.Width - runesWidth <= lastLineWidth ? screen.Left + prevLineWidth : screen.Left + line * interval
                                    : screen.Right - lastLineWidth;
                    }
                    else
                    {
                        x = screen.Left;
                    }

                    CursorPosition = _hotKeyPos > -1 ? _hotKeyPos : 0;
                }
                else if (Alignment is Alignment.Center)
                {
                    if (isVertical)
                    {
                        int runesWidth = GetColumnsRequiredForVerticalText (linesFormatted, 0, linesFormatted.Count, TabWidth);
                        int linesWidth = GetColumnsRequiredForVerticalText (linesFormatted, 0, line, TabWidth);
                        x = screen.Left + linesWidth + (screen.Width - runesWidth) / 2;

                        CursorPosition = (screen.Width - runesWidth) / 2 + (_hotKeyPos > -1 ? _hotKeyPos : 0);
                    }
                    else
                    {
                        int runesWidth = strings.GetColumns ();
                        x = screen.Left + (screen.Width - runesWidth) / 2;

                        CursorPosition = (screen.Width - runesWidth) / 2 + (_hotKeyPos > -1 ? _hotKeyPos : 0);
                    }
                }
                else
                {
                    Debug.WriteLine ($"Unsupported Alignment: {nameof (VerticalAlignment)}");

                    return;
                }

                // Vertical Alignment
                if (VerticalAlignment is Alignment.End)
                {
                    if (isVertical)
                    {
                        y = screen.Bottom - graphemeCount;
                    }
                    else
                    {
                        y = screen.Bottom - linesFormatted.Count + line;
                    }
                }
                else if (VerticalAlignment is Alignment.Start)
                {
                    if (isVertical)
                    {
                        y = screen.Top;
                    }
                    else
                    {
                        y = screen.Top + line;
                    }
                }
                else if (VerticalAlignment is Alignment.Fill)
                {
                    if (isVertical)
                    {
                        y = screen.Top;
                    }
                    else
                    {
                        var interval = (int)Math.Round ((double)(screen.Height + 2) / linesFormatted.Count);

                        y = line == 0 ? screen.Top :
                            line < linesFormatted.Count - 1 ? screen.Height - interval <= 1 ? screen.Top + 1 : screen.Top + line * interval : screen.Bottom - 1;
                    }
                }
                else if (VerticalAlignment is Alignment.Center)
                {
                    if (isVertical)
                    {
                        int s = (screen.Height - graphemeCount) / 2;
                        y = screen.Top + s;
                    }
                    else
                    {
                        int s = (screen.Height - linesFormatted.Count) / 2;
                        y = screen.Top + line + s;
                    }
                }
                else
                {
                    Debug.WriteLine ($"Unsupported Alignment: {nameof (VerticalAlignment)}");

                    return;
                }

                int colOffset = screen.X < 0 ? Math.Abs (screen.X) : 0;
                int start = isVertical ? screen.Top : screen.Left;
                int size = isVertical ? screen.Height : screen.Width;
                int current = start + colOffset;
                List<Point?> lastZeroWidthPos = null!;
                string text = string.Empty;
                int zeroLengthCount = isVertical ? GraphemeHelper.GetGraphemes (strings).Sum (s => s.GetColumns (false) == 0 ? 1 : 0) : 0;

                for (int idx = (isVertical ? start - y : start - x) + colOffset;
                     current < start + size + zeroLengthCount;
                     idx++)
                {
                    string lastTextUsed = text;

                    if (lastZeroWidthPos is null)
                    {
                        if (idx < 0
                            || (isVertical
                                    ? VerticalAlignment != Alignment.End && current < 0
                                    : Alignment != Alignment.End && x + current + colOffset < 0))
                        {
                            current++;

                            continue;
                        }

                        if (!FillRemaining && idx > graphemeCount - 1)
                        {
                            break;
                        }

                        if ((!isVertical
                             && (current - start > maxScreen.Left + maxScreen.Width - screen.X + colOffset
                                 || (idx < graphemeCount && graphemes [idx].GetColumns () > screen.Width)))
                            || (isVertical
                                && ((current > start + size + zeroLengthCount && idx > maxScreen.Top + maxScreen.Height - screen.Y)
                                    || (idx < graphemeCount && graphemes [idx].GetColumns () > screen.Width))))
                        {
                            break;
                        }
                    }

                    //if ((!isVertical && idx > maxBounds.Left + maxBounds.Width - viewport.X + colOffset)
                    //	|| (isVertical && idx > maxBounds.Top + maxBounds.Height - viewport.Y))

                    //	break;

                    text = " ";

                    if (isVertical)
                    {
                        if (idx >= 0 && idx < graphemeCount)
                        {
                            text = graphemes [idx];
                        }

                        if (lastZeroWidthPos is null)
                        {
                            driver?.Move (x, current);
                        }
                        else
                        {
                            int foundIdx = lastZeroWidthPos.IndexOf (
                                                                     p =>
                                                                         p is { } && p.Value.Y == current
                                                                    );

                            if (foundIdx > -1)
                            {
                                if (Rune.GetRuneAt (text, 0).IsCombiningMark ())
                                {
                                    lastZeroWidthPos [foundIdx] =
                                        new Point (
                                                   lastZeroWidthPos [foundIdx]!.Value.X + 1,
                                                   current
                                                  );

                                    driver?.Move (
                                                  lastZeroWidthPos [foundIdx]!.Value.X,
                                                  current
                                                 );
                                }
                                else if (!Rune.GetRuneAt (text, 0).IsCombiningMark () && Rune.GetRuneAt (lastTextUsed, 0).IsCombiningMark ())
                                {
                                    current++;
                                    driver?.Move (x, current);
                                }
                                else
                                {
                                    driver?.Move (x, current);
                                }
                            }
                            else
                            {
                                driver?.Move (x, current);
                            }
                        }
                    }
                    else
                    {
                        driver?.Move (current, y);

                        if (idx >= 0 && idx < graphemeCount)
                        {
                            text = graphemes [idx];
                        }
                    }

                    int textWidth = GetTextWidth (text, TabWidth);

                    if (HotKeyPos > -1 && idx == HotKeyPos)
                    {
                        if ((isVertical && VerticalAlignment == Alignment.Fill) || (!isVertical && Alignment == Alignment.Fill))
                        {
                            CursorPosition = idx - start;
                        }

                        driver?.SetAttribute (hotColor);
                        driver?.AddStr (text);
                        driver?.SetAttribute (normalColor);
                    }
                    else
                    {
                        if (isVertical)
                        {
                            if (textWidth == 0)
                            {
                                if (lastZeroWidthPos is null)
                                {
                                    lastZeroWidthPos = new ();
                                }

                                int foundIdx = lastZeroWidthPos.IndexOf (
                                                                         p =>
                                                                             p is { } && p.Value.Y == current
                                                                        );

                                if (foundIdx == -1)
                                {
                                    current--;
                                    lastZeroWidthPos.Add (new Point (x + 1, current));
                                }

                                driver?.Move (x + 1, current);
                            }
                        }

                        driver?.AddStr (text);
                    }

                    if (isVertical)
                    {
                        if (textWidth > 0)
                        {
                            current++;
                        }
                    }
                    else
                    {
                        current += textWidth;
                    }

                    int nextTextWidth = idx + 1 > -1 && idx + 1 < graphemeCount
                                            ? graphemes [idx + 1].GetColumns ()
                                            : 0;

                    if (!isVertical && idx + 1 < graphemeCount && current + nextTextWidth > start + size)
                    {
                        break;
                    }
                }
            }
            finally
            {
                ArrayPool<string>.Shared.Return (graphemes, clearArray: true);
            }
        }
    }

    /// <summary>
    ///     Determines if the viewport width will be used or only the text width will be used,
    ///     If <see langword="true"/> all the viewport area will be filled with whitespaces and the same background color
    ///     showing a perfect rectangle.
    /// </summary>
    public bool FillRemaining { get; set; }

    /// <summary>Returns the formatted text, constrained to <see cref="ConstrainToSize"/>.</summary>
    /// <remarks>
    ///     If <see cref="NeedsFormat"/> is <see langword="true"/>, causes a format, resetting <see cref="NeedsFormat"/>
    ///     to <see langword="false"/>.
    /// </remarks>
    /// <returns>The formatted text.</returns>
    public string Format ()
    {
        var sb = new StringBuilder ();

        // Lines_get causes a Format
        foreach (string line in GetLines ())
        {
            sb.AppendLine (line);
        }

        return sb.ToString ().TrimEnd (Environment.NewLine.ToCharArray ());
    }

    /// <summary>Gets the size required to hold the formatted text, given the constraints placed by <see cref="ConstrainToSize"/>.</summary>
    /// <remarks>Causes a format, resetting <see cref="NeedsFormat"/> to <see langword="false"/>.</remarks>
    /// <param name="constrainSize">
    ///     If provided, will cause the text to be constrained to the provided size instead of <see cref="ConstrainToWidth"/> and
    ///     <see cref="ConstrainToHeight"/>.
    /// </param>
    /// <returns>The size required to hold the formatted text.</returns>
    public Size FormatAndGetSize (Size? constrainSize = null)
    {
        if (string.IsNullOrEmpty (Text))
        {
            return System.Drawing.Size.Empty;
        }

        int? prevWidth = _constrainToWidth;
        int? prevHeight = _constrainToHeight;

        if (constrainSize is { })
        {
            _constrainToWidth = constrainSize?.Width;
            _constrainToHeight = constrainSize?.Height;
        }

        // HACK: Fill normally will fill the entire constraint size, but we need to know the actual size of the text.
        Alignment prevAlignment = Alignment;

        if (Alignment == Alignment.Fill)
        {
            Alignment = Alignment.Start;
        }

        Alignment prevVerticalAlignment = VerticalAlignment;

        if (VerticalAlignment == Alignment.Fill)
        {
            VerticalAlignment = Alignment.Start;
        }

        // This calls Format
        List<string> lines = GetLines ();

        // Undo hacks
        Alignment = prevAlignment;
        VerticalAlignment = prevVerticalAlignment;

        if (constrainSize is { })
        {
            _constrainToWidth = prevWidth ?? null;
            _constrainToHeight = prevHeight ?? null;
        }

        if (lines.Count == 0)
        {
            return System.Drawing.Size.Empty;
        }

        int width;
        int height;

        if (IsVerticalDirection (Direction))
        {
            width = GetColumnsRequiredForVerticalText (lines, 0, lines.Count, TabWidth);
            height = lines.Max (static line => line.Length);
        }
        else
        {
            width = lines.Max (static line => line.GetColumns ());
            height = lines.Count;
        }

        return new (width, height);
    }

    /// <summary>
    ///     Gets the width or height of the <see cref="TextFormatter.HotKeySpecifier"/> characters
    ///     in the <see cref="Text"/> property.
    /// </summary>
    /// <remarks>
    ///     Only the first HotKey specifier found in <see cref="Text"/> is supported.
    /// </remarks>
    /// <param name="isWidth">
    ///     If <see langword="true"/> (the default) the width required for the HotKey specifier is returned. Otherwise, the
    ///     height is returned.
    /// </param>
    /// <returns>
    ///     The number of characters required for the <see cref="TextFormatter.HotKeySpecifier"/>. If the text
    ///     direction specified
    ///     by <see cref="TextDirection"/> does not match the <paramref name="isWidth"/> parameter, <c>0</c> is returned.
    /// </returns>
    public int GetHotKeySpecifierLength (bool isWidth = true)
    {
        if (isWidth)
        {
            return IsHorizontalDirection (Direction) && Text?.Contains ((char)HotKeySpecifier.Value) == true
                       ? Math.Max (HotKeySpecifier.GetColumns (), 0)
                       : 0;
        }

        return IsVerticalDirection (Direction) && Text?.Contains ((char)HotKeySpecifier.Value) == true
                   ? Math.Max (HotKeySpecifier.GetColumns (), 0)
                   : 0;
    }

    /// <summary>Gets a list of formatted lines, constrained to <see cref="ConstrainToSize"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         If the text needs to be formatted (if <see cref="NeedsFormat"/> is <see langword="true"/>)
    ///         <see cref="Format()"/> will be called and upon return
    ///         <see cref="NeedsFormat"/> will be <see langword="false"/>.
    ///     </para>
    ///     <para>
    ///         If either of the dimensions of <see cref="ConstrainToSize"/> are zero, the text will not be formatted and no lines will
    ///         be returned.
    ///     </para>
    /// </remarks>
    public List<string> GetLines ()
    {
        string text = _text!.ReplaceLineEndings ();

        // With this check, we protect against subclasses with overrides of Text
        if (string.IsNullOrEmpty (Text) || ConstrainToWidth is 0 || ConstrainToHeight is 0)
        {
            _lines = [string.Empty];
            NeedsFormat = false;

            return _lines;
        }

        if (!NeedsFormat)
        {
            return _lines;
        }

        int width = ConstrainToWidth ?? int.MaxValue;
        int height = ConstrainToHeight ?? int.MaxValue;

        if (FindHotKey (_text!, HotKeySpecifier, out _hotKeyPos, out Key newHotKey))
        {
            HotKey = newHotKey;
            text = RemoveHotKeySpecifier (Text, _hotKeyPos, HotKeySpecifier);
            text = ReplaceHotKeyWithTag (text, _hotKeyPos);
        }

        if (IsVerticalDirection (Direction))
        {
            int colsWidth = GetSumMaxCharWidth (text, 0, 1, TabWidth);

            _lines = Format (
                             text,
                             height,
                             VerticalAlignment == Alignment.Fill,
                             width > colsWidth && WordWrap,
                             PreserveTrailingSpaces,
                             TabWidth,
                             Direction,
                             MultiLine,
                             this,
                             PreserveTabs
                            );

            colsWidth = GetMaxColsForWidth (_lines, width, TabWidth);

            if (_lines.Count > colsWidth)
            {
                _lines.RemoveRange (colsWidth, _lines.Count - colsWidth);
            }
        }
        else
        {
            _lines = Format (
                             text,
                             width,
                             Alignment == Alignment.Fill,
                             height > 1 && WordWrap,
                             PreserveTrailingSpaces,
                             TabWidth,
                             Direction,
                             MultiLine,
                             this,
                             PreserveTabs
                            );

            if (_lines.Count > height)
            {
                _lines.RemoveRange (height, _lines.Count - height);
            }
        }

        NeedsFormat = false;

        return _lines;
    }

    private int? _constrainToWidth;

    /// <summary>Gets or sets the width <see cref="Text"/> will be constrained to when formatted.</summary>
    /// <remarks>
    ///     <para>
    ///         Does not return the width of the formatted text but the width that will be used to constrain the text when
    ///         formatted.
    ///     </para>
    ///     <para>
    ///         If <see langword="null"/> the height will be unconstrained. if both <see cref="ConstrainToWidth"/> and <see cref="ConstrainToHeight"/> are <see langword="null"/> the text will be formatted to the size of the text.
    ///     </para>
    ///     <para>
    ///         Use <see cref="FormatAndGetSize"/> to get the size of the formatted text.
    ///     </para>
    ///     <para>When set, <see cref="NeedsFormat"/> is set to <see langword="true"/>.</para>
    /// </remarks>
    public int? ConstrainToWidth
    {
        get => _constrainToWidth;
        set
        {
            if (_constrainToWidth == value)
            {
                return;
            }

            ArgumentOutOfRangeException.ThrowIfNegative (value.GetValueOrDefault (), nameof (ConstrainToWidth));

            _constrainToWidth = EnableNeedsFormat (value);
        }
    }

    private int? _constrainToHeight;

    /// <summary>Gets or sets the height <see cref="Text"/> will be constrained to when formatted.</summary>
    /// <remarks>
    ///     <para>
    ///         Does not return the height of the formatted text but the height that will be used to constrain the text when
    ///         formatted.
    ///     </para>
    ///     <para>
    ///         If <see langword="null"/> the height will be unconstrained. if both <see cref="ConstrainToWidth"/> and <see cref="ConstrainToHeight"/> are <see langword="null"/> the text will be formatted to the size of the text.
    ///     </para>
    ///     <para>
    ///         Use <see cref="FormatAndGetSize"/> to get the size of the formatted text.
    ///     </para>
    ///     <para>When set, <see cref="NeedsFormat"/> is set to <see langword="true"/>.</para>
    /// </remarks>

    public int? ConstrainToHeight
    {
        get => _constrainToHeight;
        set
        {
            if (_constrainToHeight == value)
            {
                return;
            }

            ArgumentOutOfRangeException.ThrowIfNegative (value.GetValueOrDefault (), nameof (ConstrainToHeight));

            _constrainToHeight = EnableNeedsFormat (value);
        }
    }

    /// <summary>Gets or sets the width and height <see cref="Text"/> will be constrained to when formatted.</summary>
    /// <remarks>
    ///     <para>
    ///         Does not return the size of the formatted text but the size that will be used to constrain the text when
    ///         formatted.
    ///     </para>
    ///     <para>
    ///         If <see langword="null"/> both the width and height will be unconstrained and text will be formatted to the size of the text.
    ///     </para>
    ///     <para>
    ///         Setting this property is the same as setting <see cref="ConstrainToWidth"/> and <see cref="ConstrainToHeight"/> separately.
    ///     </para>
    ///     <para>
    ///         Use <see cref="FormatAndGetSize"/> to get the size of the formatted text.
    ///     </para>
    ///     <para>When set, <see cref="NeedsFormat"/> is set to <see langword="true"/>.</para>
    /// </remarks>
    public Size? ConstrainToSize
    {
        get
        {
            if (_constrainToWidth is null || _constrainToHeight is null)
            {
                return null;
            }

            return new Size (_constrainToWidth.Value, _constrainToHeight.Value);
        }
        set
        {
            if (value is null)
            {
                _constrainToWidth = null;
                _constrainToHeight = null;
                EnableNeedsFormat (true);
            }
            else
            {
                _constrainToWidth = EnableNeedsFormat (value.Value.Width);
                _constrainToHeight = EnableNeedsFormat (value.Value.Height);
            }
        }
    }

    /// <summary>Gets or sets the hot key. Fires the <see cref="HotKeyChanged"/> event.</summary>
    public Key HotKey
    {
        get => _hotKey;
        internal set
        {
            if (_hotKey != value)
            {
                Key oldKey = _hotKey;
                _hotKey = value;
                HotKeyChanged?.Invoke (this, new (oldKey, value));
            }
        }
    }

    /// <summary>Event invoked when the <see cref="HotKey"/> is changed.</summary>
    public event EventHandler<KeyChangedEventArgs>? HotKeyChanged;

    /// <summary>The position in the text of the hot key. The hot key will be rendered using the hot color.</summary>
    public int HotKeyPos
    {
        get => _hotKeyPos;
        internal set => _hotKeyPos = value;
    }

    /// <summary>
    ///     The specifier character for the hot key (e.g. '_'). Set to '\xffff' to disable hot key support for this View
    ///     instance. The default is '\xffff'.
    /// </summary>
    public Rune HotKeySpecifier { get; set; } = (Rune)0xFFFF;

    /// <summary>Gets or sets a value indicating whether multi line is allowed.</summary>
    /// <remarks>Multi line is ignored if <see cref="WordWrap"/> is <see langword="true"/>.</remarks>
    public bool MultiLine
    {
        get => _multiLine;
        set => _multiLine = EnableNeedsFormat (value);
    }

    /// <summary>Gets or sets whether the <see cref="TextFormatter"/> needs to format the text.</summary>
    /// <remarks>
    ///     <para>If <see langword="false"/> when Draw is called, the Draw call will be faster.</para>
    ///     <para>Used by <see cref="Draw"/></para>
    ///     <para>Set to <see langword="true"/> when any of the properties of <see cref="TextFormatter"/> are set.</para>
    ///     <para>Set to <see langword="false"/> when the text is formatted (if <see cref="GetLines"/> is accessed).</para>
    /// </remarks>
    public bool NeedsFormat { get; set; }

    /// <summary>
    ///     Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved or not when
    ///     <see cref="TextFormatter.WordWrap"/> is enabled. If <see langword="true"/> trailing spaces at the end of wrapped
    ///     lines will be removed when <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
    /// </summary>
    public bool PreserveTrailingSpaces
    {
        get => _preserveTrailingSpaces;
        set => _preserveTrailingSpaces = EnableNeedsFormat (value);
    }

    /// <summary>
    ///     Gets or sets whether tabs will be preserved, considering the tab width or replaced with spaces.
    ///     If <see langword="true"/> tabs will be preserved, considering the spaces according to the provided <see cref="TabWidth"/>.
    ///     If <see langword="false"/>, default, tabs will be replaced with spaces according to the provided <see cref="TabWidth"/>.
    /// </summary>
    public bool PreserveTabs { get; set => field = EnableNeedsFormat (value); }

    /// <summary>Gets or sets the number of columns used for a tab.</summary>
    public int TabWidth
    {
        get => _tabWidth;
        set => _tabWidth = EnableNeedsFormat (value);
    }

    /// <summary>The text to be formatted. This string is never modified.</summary>
    public string Text
    {
        get => _text!;
        set => _text = EnableNeedsFormat (value);
    }

    /// <summary>Gets or sets the vertical text-alignment.</summary>
    /// <value>The text vertical alignment.</value>
    public Alignment VerticalAlignment
    {
        get => _textVerticalAlignment;
        set => _textVerticalAlignment = EnableNeedsFormat (value);
    }

    /// <summary>Gets or sets whether word wrap will be used to fit <see cref="Text"/> to <see cref="ConstrainToSize"/>.</summary>
    public bool WordWrap
    {
        get => _wordWrap;
        set => _wordWrap = EnableNeedsFormat (value);
    }

    /// <summary>Sets <see cref="NeedsFormat"/> to <see langword="true"/> and returns the value.</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    private T EnableNeedsFormat<T> (T value)
    {
        NeedsFormat = true;

        return value;
    }

    /// <summary>
    ///     Calculates and returns a <see cref="Region"/> describing the areas where text would be output, based on the
    ///     formatting rules of <see cref="TextFormatter"/>.
    /// </summary>
    /// <remarks>
    ///     Uses the same formatting logic as <see cref="Draw"/>, including alignment, direction, word wrap, and constraints,
    ///     but does not perform actual drawing to <see cref="IDriver"/>.
    /// </remarks>
    /// <param name="screen">Specifies the screen-relative location and maximum size for drawing the text.</param>
    /// <param name="maximum">Specifies the screen-relative location and maximum container size.</param>
    /// <returns>A <see cref="Region"/> representing the areas where text would be drawn.</returns>
    public Region GetDrawRegion (Rectangle screen, Rectangle maximum = default)
    {
        Region drawnRegion = new();

        // With this check, we protect against subclasses with overrides of Text (like Button)
        if (string.IsNullOrEmpty (Text))
        {
            return drawnRegion;
        }

        List<string> linesFormatted = GetLines ();

        bool isVertical = IsVerticalDirection (Direction);
        Rectangle maxScreen = screen;

        // INTENT: What, exactly, is the intent of this?
        maxScreen = maximum == default (Rectangle)
                        ? screen
                        : new (
                               Math.Max (maximum.X, screen.X),
                               Math.Max (maximum.Y, screen.Y),
                               Math.Max (
                                         Math.Min (maximum.Width, maximum.Right - screen.Left),
                                         0
                                        ),
                               Math.Max (
                                         Math.Min (
                                                   maximum.Height,
                                                   maximum.Bottom - screen.Top
                                                  ),
                                         0
                                        )
                              );

        if (maxScreen.Width == 0 || maxScreen.Height == 0)
        {
            return drawnRegion;
        }

        int lineOffset = !isVertical && screen.Y < 0 ? Math.Abs (screen.Y) : 0;

        for (int line = lineOffset; line < linesFormatted.Count; line++)
        {
            if ((isVertical && line > screen.Width) || (!isVertical && line > screen.Height))
            {
                continue;
            }

            if ((isVertical && line >= maxScreen.Left + maxScreen.Width)
                || (!isVertical && line >= maxScreen.Top + maxScreen.Height + lineOffset))
            {
                break;
            }

            string strings = linesFormatted [line];

            // Use ArrayPool to avoid per-line allocations
            int estimatedCount = strings.Length + 10; // Add buffer for grapheme clusters
            string [] graphemes = ArrayPool<string>.Shared.Rent (estimatedCount);
            var graphemeCount = 0;

            try
            {
                foreach (string grapheme in GraphemeHelper.GetGraphemes (strings))
                {
                    if (graphemeCount >= graphemes.Length)
                    {
                        // Need larger array (rare case for complex text)
                        string [] larger = ArrayPool<string>.Shared.Rent (graphemes.Length * 2);
                        Array.Copy (graphemes, larger, graphemeCount);
                        ArrayPool<string>.Shared.Return (graphemes, clearArray: true);
                        graphemes = larger;
                    }

                    graphemes [graphemeCount++] = grapheme;
                }

                // When text is justified, we lost left or right, so we use the direction to align.
                int x = 0, y = 0;

                switch (Alignment)
                {
                    // Horizontal Alignment
                    case Alignment.End when isVertical:
                        {
                            int runesWidth = GetColumnsRequiredForVerticalText (linesFormatted, line, linesFormatted.Count - line, TabWidth);
                            x = screen.Right - runesWidth;

                            break;
                        }
                    case Alignment.End:
                        {
                            int stringsWidth = strings.GetColumns ();
                            x = screen.Right - stringsWidth;

                            break;
                        }
                    case Alignment.Start when isVertical:
                        {
                            int stringsWidth = line > 0
                                                   ? GetColumnsRequiredForVerticalText (linesFormatted, 0, line, TabWidth)
                                                   : 0;
                            x = screen.Left + stringsWidth;

                            break;
                        }
                    case Alignment.Start:
                        x = screen.Left;

                        break;
                    case Alignment.Fill when isVertical:
                        {
                            int stringsWidth = GetColumnsRequiredForVerticalText (linesFormatted, 0, linesFormatted.Count, TabWidth);
                            int prevLineWidth = line > 0 ? GetColumnsRequiredForVerticalText (linesFormatted, line - 1, 1, TabWidth) : 0;
                            int firstLineWidth = GetColumnsRequiredForVerticalText (linesFormatted, 0, 1, TabWidth);
                            int lastLineWidth = GetColumnsRequiredForVerticalText (linesFormatted, linesFormatted.Count - 1, 1, TabWidth);
                            var interval = (int)Math.Round ((double)(screen.Width + firstLineWidth + lastLineWidth) / linesFormatted.Count);

                            x = line == 0
                                    ? screen.Left
                                    : line < linesFormatted.Count - 1
                                        ? screen.Width - stringsWidth <= lastLineWidth ? screen.Left + prevLineWidth : screen.Left + line * interval
                                        : screen.Right - lastLineWidth;

                            break;
                        }
                    case Alignment.Fill:
                        x = screen.Left;

                        break;
                    case Alignment.Center when isVertical:
                        {
                            int stringsWidth = GetColumnsRequiredForVerticalText (linesFormatted, 0, linesFormatted.Count, TabWidth);
                            int linesWidth = GetColumnsRequiredForVerticalText (linesFormatted, 0, line, TabWidth);
                            x = screen.Left + linesWidth + (screen.Width - stringsWidth) / 2;

                            break;
                        }
                    case Alignment.Center:
                        {
                            int stringsWidth = strings.GetColumns ();
                            x = screen.Left + (screen.Width - stringsWidth) / 2;

                            break;
                        }
                    default:
                        Debug.WriteLine ($"Unsupported Alignment: {nameof (VerticalAlignment)}");

                        return drawnRegion;
                }

                switch (VerticalAlignment)
                {
                    // Vertical Alignment
                    case Alignment.End when isVertical:
                        y = screen.Bottom - graphemeCount;

                        break;
                    case Alignment.End:
                        y = screen.Bottom - linesFormatted.Count + line;

                        break;
                    case Alignment.Start when isVertical:
                        y = screen.Top;

                        break;
                    case Alignment.Start:
                        y = screen.Top + line;

                        break;
                    case Alignment.Fill when isVertical:
                        y = screen.Top;

                        break;
                    case Alignment.Fill:
                        {
                            var interval = (int)Math.Round ((double)(screen.Height + 2) / linesFormatted.Count);

                            y = line == 0 ? screen.Top :
                                line < linesFormatted.Count - 1 ? screen.Height - interval <= 1 ? screen.Top + 1 : screen.Top + line * interval : screen.Bottom - 1;

                            break;
                        }
                    case Alignment.Center when isVertical:
                        {
                            int s = (screen.Height - graphemeCount) / 2;
                            y = screen.Top + s;

                            break;
                        }
                    case Alignment.Center:
                        {
                            int s = (screen.Height - linesFormatted.Count) / 2;
                            y = screen.Top + line + s;

                            break;
                        }
                    default:
                        Debug.WriteLine ($"Unsupported Alignment: {nameof (VerticalAlignment)}");

                        return drawnRegion;
                }

                int colOffset = screen.X < 0 ? Math.Abs (screen.X) : 0;
                int start = isVertical ? screen.Top : screen.Left;
                int size = isVertical ? screen.Height : screen.Width;
                int current = start + colOffset;
                int zeroLengthCount = isVertical ? GraphemeHelper.GetGraphemes (strings).Sum (s => s.GetColumns (false) == 0 ? 1 : 0) : 0;

                int lineX = x, lineY = y, lineWidth = 0, lineHeight = 1;

                for (int idx = (isVertical ? start - y : start - x) + colOffset;
                     current < start + size + zeroLengthCount;
                     idx++)
                {
                    if (idx < 0
                        || (isVertical
                                ? VerticalAlignment != Alignment.End && current < 0
                                : Alignment != Alignment.End && x + current + colOffset < 0))
                    {
                        current++;

                        continue;
                    }

                    if (!FillRemaining && idx > graphemeCount - 1)
                    {
                        break;
                    }

                    if ((!isVertical
                         && (current - start > maxScreen.Left + maxScreen.Width - screen.X + colOffset
                             || (idx < graphemeCount && graphemes [idx].GetColumns () > screen.Width)))
                        || (isVertical
                            && ((current > start + size + zeroLengthCount && idx > maxScreen.Top + maxScreen.Height - screen.Y)
                                || (idx < graphemeCount && graphemes [idx].GetColumns () > screen.Width))))
                    {
                        break;
                    }

                    string text = idx >= 0 && idx < graphemeCount ? graphemes [idx] : " ";
                    int textWidth = GetStringWidth (text, TabWidth);

                    if (isVertical)
                    {
                        if (textWidth > 0)
                        {
                            // Update line height for vertical text (each rune is a column)
                            lineHeight = Math.Max (lineHeight, current - y + 1);
                            lineWidth = Math.Max (lineWidth, 1); // Width is 1 per rune in vertical
                        }
                    }
                    else
                    {
                        // Update line width and position for horizontal text
                        lineWidth += textWidth;
                    }

                    current += isVertical && textWidth > 0 ? 1 : textWidth;

                    int nextStringWidth = idx + 1 > -1 && idx + 1 < graphemeCount
                                            ? graphemes [idx + 1].GetColumns ()
                                            : 0;

                    if (!isVertical && idx + 1 < graphemeCount && current + nextStringWidth > start + size)
                    {
                        break;
                    }
                }

                // Add the line's drawn region to the overall region
                if (lineWidth > 0 && lineHeight > 0)
                {
                    drawnRegion.Union (new Rectangle (lineX, lineY, lineWidth, lineHeight));
                }
            }
            finally
            {
                ArrayPool<string>.Shared.Return (graphemes, clearArray: true);
            }
        }

        return drawnRegion;
    }

    #region Static Members

    /// <summary>Check if it is a horizontal direction</summary>
    public static bool IsHorizontalDirection (TextDirection textDirection)
    {
        return textDirection switch
        {
            TextDirection.LeftRight_TopBottom => true,
            TextDirection.LeftRight_BottomTop => true,
            TextDirection.RightLeft_TopBottom => true,
            TextDirection.RightLeft_BottomTop => true,
            _ => false
        };
    }

    /// <summary>Check if it is a vertical direction</summary>
    public static bool IsVerticalDirection (TextDirection textDirection)
    {
        return textDirection switch
        {
            TextDirection.TopBottom_LeftRight => true,
            TextDirection.TopBottom_RightLeft => true,
            TextDirection.BottomTop_LeftRight => true,
            TextDirection.BottomTop_RightLeft => true,
            _ => false
        };
    }

    /// <summary>Check if it is Left to Right direction</summary>
    public static bool IsLeftToRight (TextDirection textDirection)
    {
        return textDirection switch
        {
            TextDirection.LeftRight_TopBottom => true,
            TextDirection.LeftRight_BottomTop => true,
            _ => false
        };
    }

    /// <summary>Check if it is Top to Bottom direction</summary>
    public static bool IsTopToBottom (TextDirection textDirection)
    {
        return textDirection switch
        {
            TextDirection.TopBottom_LeftRight => true,
            TextDirection.TopBottom_RightLeft => true,
            _ => false
        };
    }

    // TODO: Move to StringExtensions?
    internal static string StripCRLF (string str, bool keepNewLine = false)
    {
        ReadOnlySpan<char> remaining = str.AsSpan ();
        int firstNewlineCharIndex = remaining.IndexOfAny (NewlineSearchValues);
        // Early exit to avoid StringBuilder allocation if there are no newline characters.
        if (firstNewlineCharIndex < 0)
        {
            return str;
        }

        StringBuilder stringBuilder = new ();
        ReadOnlySpan<char> firstSegment = remaining [..firstNewlineCharIndex];
        stringBuilder.Append (firstSegment);

        // The first newline is not yet skipped because the "keepNewLine" condition has not been evaluated.
        // This means there will be 1 extra iteration because the same newline index is checked again in the loop.
        remaining = remaining [firstNewlineCharIndex..];

        while (remaining.Length > 0)
        {
            int newlineCharIndex = remaining.IndexOfAny (NewlineSearchValues);
            if (newlineCharIndex == -1)
            {
                break;
            }

            ReadOnlySpan<char> segment = remaining [..newlineCharIndex];
            stringBuilder.Append (segment);

            int stride = segment.Length;
            // Evaluate how many line break characters to preserve.
            char newlineChar = remaining [newlineCharIndex];
            if (newlineChar == '\n')
            {
                stride++;
                if (keepNewLine)
                {
                    stringBuilder.Append ('\n');
                }
            }
            else // '\r'
            {
                int nextCharIndex = newlineCharIndex + 1;
                bool crlf = nextCharIndex < remaining.Length && remaining [nextCharIndex] == '\n';
                if (crlf)
                {
                    stride += 2;
                    if (keepNewLine)
                    {
                        stringBuilder.Append ('\n');
                    }
                }
                else
                {
                    stride++;
                    if (keepNewLine)
                    {
                        stringBuilder.Append ('\r');
                    }
                }
            }
            remaining = remaining [stride..];
        }
        stringBuilder.Append (remaining);
        return stringBuilder.ToString ();
    }

    // TODO: Move to StringExtensions?
    internal static string ReplaceCRLFWithSpace (string str)
    {
        ReadOnlySpan<char> remaining = str.AsSpan ();
        int firstNewlineCharIndex = remaining.IndexOfAny (NewlineSearchValues);
        // Early exit to avoid StringBuilder allocation if there are no newline characters.
        if (firstNewlineCharIndex < 0)
        {
            return str;
        }

        StringBuilder stringBuilder = new ();
        ReadOnlySpan<char> firstSegment = remaining [..firstNewlineCharIndex];
        stringBuilder.Append (firstSegment);

        // The first newline is not yet skipped because the newline type has not been evaluated.
        // This means there will be 1 extra iteration because the same newline index is checked again in the loop.
        remaining = remaining [firstNewlineCharIndex..];

        while (remaining.Length > 0)
        {
            int newlineCharIndex = remaining.IndexOfAny (NewlineSearchValues);
            if (newlineCharIndex == -1)
            {
                break;
            }

            ReadOnlySpan<char> segment = remaining [..newlineCharIndex];
            stringBuilder.Append (segment);

            int stride = segment.Length;
            // Replace newlines
            char newlineChar = remaining [newlineCharIndex];
            if (newlineChar == '\n')
            {
                stride++;
                stringBuilder.Append (' ');
            }
            else // '\r'
            {
                int nextCharIndex = newlineCharIndex + 1;
                bool crlf = nextCharIndex < remaining.Length && remaining [nextCharIndex] == '\n';
                if (crlf)
                {
                    stride += 2;
                    stringBuilder.Append (' ');
                }
                else
                {
                    stride++;
                    stringBuilder.Append (' ');
                }
            }
            remaining = remaining [stride..];
        }
        stringBuilder.Append (remaining);
        return stringBuilder.ToString ();
    }

    // TODO: Move to StringExtensions?
    private static string ReplaceTABWithSpaces (string str, int tabWidth, bool preserveTabs = false)
    {
        if (preserveTabs || !str.Contains ('\t'))
        {
            return str;
        }

        var textWidth = 0;
        StringBuilder sb = new ();

        foreach (var grapheme in GraphemeHelper.GetGraphemes (str))
        {
            int graphemeWidth;

            if (grapheme == "\t")
            {
                if (tabWidth > 0)
                {
                    graphemeWidth = tabWidth - textWidth % tabWidth;
                    sb.Append (new string (' ', graphemeWidth));
                }
                else
                {
                    graphemeWidth = 0;
                }
            }
            else
            {
                graphemeWidth = grapheme.GetColumns ();
                sb.Append (grapheme);
            }

            textWidth += graphemeWidth;
        }

        return sb.ToString ();
    }

    // TODO: Move to StringExtensions?
    /// <summary>
    ///     Splits all newlines in the <paramref name="text"/> into a list and supports both CRLF and LF, preserving the
    ///     ending newline.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>A list of text without the newline characters.</returns>
    public static List<string> SplitNewLine (string text)
    {
        List<string> graphemes = GraphemeHelper.GetGraphemes (text).ToList ();
        List<string> lines = new ();
        var start = 0;

        for (var i = 0; i < graphemes.Count; i++)
        {
            int end = i;

            switch (graphemes [i])
            {
                case "\n":
                case "\r\n":
                    lines.Add (StringExtensions.ToString (graphemes.GetRange (start, end - start)));
                    i++;
                    start = i;

                    break;

                case "\r":
                    if (i + 1 < graphemes.Count && graphemes [i + 1] == "\n")
                    {
                        lines.Add (StringExtensions.ToString (graphemes.GetRange (start, end - start)));
                        i += 2;
                        start = i;
                    }
                    else
                    {
                        lines.Add (StringExtensions.ToString (graphemes.GetRange (start, end - start)));
                        i++;
                        start = i;
                    }

                    break;
            }
        }

        switch (graphemes.Count)
        {
            case > 0 when lines.Count == 0:
                lines.Add (StringExtensions.ToString (graphemes));

                break;
            case > 0 when start < graphemes.Count:
                lines.Add (StringExtensions.ToString (graphemes.GetRange (start, graphemes.Count - start)));

                break;
            default:
                lines.Add ("");

                break;
        }

        return lines;
    }

    // TODO: Move to StringExtensions?
    /// <summary>
    ///     Adds trailing whitespace or truncates <paramref name="text"/> so that it fits exactly <paramref name="width"/>
    ///     columns. Note that some unicode characters take 2+ columns
    /// </summary>
    /// <param name="text"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    public static string ClipOrPad (string text, int width)
    {
        if (string.IsNullOrEmpty (text))
        {
            return text;
        }

        // if value is not wide enough
        string [] graphemes = GraphemeHelper.GetGraphemes (text).ToArray ();
        int totalColumns = graphemes.Sum (s => s.GetColumns ());

        if (totalColumns < width)
        {
            // pad it out with spaces to the given Alignment
            int toPad = width - totalColumns;

            return text + new string (' ', toPad);
        }

        // value is too wide
        return string.Concat (graphemes.TakeWhile (t => (width -= t.GetColumns ()) >= 0));
    }

    /// <summary>Formats the provided text to fit within the width provided using word wrapping.</summary>
    /// <param name="text">The text to word wrap</param>
    /// <param name="width">The number of columns to constrain the text to</param>
    /// <param name="preserveTrailingSpaces">
    ///     If <see langword="true"/> trailing spaces at the end of wrapped lines will be
    ///     preserved. If <see langword="false"/> , trailing spaces at the end of wrapped lines will be trimmed.
    /// </param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <param name="textFormatter"><see cref="TextFormatter"/> instance to access any of his objects.</param>
    /// <param name="preserveTabs">
    ///     If <see langword="true"/> tabs will be preserved, considering the spaces according to the provided <see cref="TabWidth"/>.
    ///     If <see langword="false"/>, default, tabs will be replaced with spaces according to the provided <see cref="TabWidth"/>.
    /// </param>
    /// <returns>A list of word wrapped lines.</returns>
    /// <remarks>
    ///     <para>This method does not do any alignment.</para>
    ///     <para>This method strips Newline ('\n' and '\r\n') sequences before processing.</para>
    ///     <para>
    ///         If <paramref name="preserveTrailingSpaces"/> is <see langword="false"/> at most one space will be preserved
    ///         at the end of the last line.
    ///     </para>
    /// </remarks>
    /// <returns>A list of lines.</returns>
    public static List<string> WordWrapText (
        string text,
        int width,
        bool preserveTrailingSpaces = false,
        int tabWidth = 0,
        TextDirection textDirection = TextDirection.LeftRight_TopBottom,
        TextFormatter? textFormatter = null,
        bool preserveTabs = false
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative (width, nameof (width));

        List<string> lines = new ();

        if (string.IsNullOrEmpty (text))
        {
            return lines;
        }

        List<string> graphemes = GraphemeHelper.GetGraphemes (StripCRLF (text)).ToList ();

        int start = Math.Max (
                              !graphemes.Contains (" ") && textFormatter is { VerticalAlignment: Alignment.End } && IsVerticalDirection (textDirection)
                                  ? graphemes.Count - width
                                  : 0,
                              0);
        int end;

        if (preserveTrailingSpaces)
        {
            while (start < graphemes.Count)
            {
                end = GetNextWhiteSpace (start, width, out bool incomplete);

                if (end == 0 && incomplete)
                {
                    start = GraphemeHelper.GetGraphemeCount (text);

                    break;
                }

                lines.Add (StringExtensions.ToString (graphemes.GetRange (start, end - start)));
                start = end;

                if (incomplete)
                {
                    start = GraphemeHelper.GetGraphemeCount (text);

                    break;
                }
            }
        }
        else
        {
            if (IsHorizontalDirection (textDirection))
            {
                while ((end = start
                              + GetLengthThatFits (
                                                   string.Concat (graphemes.GetRange (start, graphemes.Count - start)),
                                                   width,
                                                   tabWidth,
                                                   textDirection
                                                  ))
                       < graphemes.Count)
                {
                    while (graphemes [end] != " " && end > start)
                    {
                        end--;
                    }

                    if (end == start)
                    {
                        end = start
                              + GetLengthThatFits (
                                                   string.Concat (graphemes.GetRange (end, graphemes.Count - end)),
                                                   width,
                                                   tabWidth,
                                                   textDirection
                                                  );
                    }

                    var str = StringExtensions.ToString (graphemes.GetRange (start, end - start));
                    int zeroLength = GraphemeHelper.GetGraphemes (text).Sum (s => s.GetColumns (false) == 0 ? 1 : 0);

                    if (end > start && GetTextWidth (str, tabWidth, textDirection) <= width + zeroLength)
                    {
                        lines.Add (str);
                        start = end;

                        if (graphemes [end] == " ")
                        {
                            start++;
                        }
                    }
                    else
                    {
                        end++;
                        start = end;
                    }
                }
            }
            else
            {
                while ((end = start + width) < graphemes.Count)
                {
                    while (graphemes [end] != " " && end > start)
                    {
                        end--;
                    }

                    if (end == start)
                    {
                        end = start + width;
                    }

                    var zeroLength = 0;

                    for (int i = end; i < graphemes.Count - start; i++)
                    {
                        string s = graphemes [i];

                        if (s.GetColumns () == 0)
                        {
                            zeroLength++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    lines.Add (
                               StringExtensions.ToString (
                                                          graphemes.GetRange (
                                                                          start,
                                                                          end - start + zeroLength
                                                                         )
                                                         )
                              );
                    end += zeroLength;
                    start = end;

                    if (graphemes [end] == " ")
                    {
                        start++;
                    }
                }
            }
        }

        int GetNextWhiteSpace (int from, int cWidth, out bool incomplete, int cLength = 0)
        {
            int to = from;
            int length = cLength;
            incomplete = false;

            while (length < cWidth && to < graphemes.Count)
            {
                string grapheme = graphemes [to];

                if (IsHorizontalDirection (textDirection))
                {
                    length += grapheme.GetColumns ();
                }
                else
                {
                    length++;
                }

                if (length > cWidth)
                {
                    if (to >= graphemes.Count || (length > 1 && cWidth <= 1))
                    {
                        incomplete = true;
                    }

                    return to;
                }

                switch (grapheme)
                {
                    case " " when length == cWidth:
                        return to + 1;
                    case " " when length > cWidth:
                        return to;
                    case " ":
                        return GetNextWhiteSpace (to + 1, cWidth, out incomplete, length);
                    case "\t":
                        {
                            length += tabWidth > 0 ? tabWidth - length % tabWidth : 0;

                            if (length == tabWidth && length > cWidth)
                            {
                                return to + 1;
                            }

                            if (length > cWidth && preserveTabs)
                            {
                                return to;
                            }

                            return GetNextWhiteSpace (to + 1, cWidth, out incomplete, length);
                        }
                    default:
                        to++;

                        break;
                }
            }

            return cLength switch
            {
                > 0 when to < graphemes.Count && graphemes [to] != " " && graphemes [to] != "\t" => from,
                > 0 when to < graphemes.Count && (graphemes [to] == " " || graphemes [to] == "\t") => from,
                _ => to
            };
        }

        if (start < GraphemeHelper.GetGraphemeCount (text))
        {
            string str = ReplaceTABWithSpaces (
                                               StringExtensions.ToString (graphemes.GetRange (start, graphemes.Count - start)),
                                               tabWidth
                                              );

            if (IsVerticalDirection (textDirection) || preserveTrailingSpaces || str.GetColumns () <= width)
            {
                lines.Add (str);
            }
        }

        return lines;
    }

    /// <summary>Justifies text within a specified width.</summary>
    /// <param name="text">The text to justify.</param>
    /// <param name="width">
    ///     The number of columns to clip the text to. Text longer than <paramref name="width"/> will be
    ///     clipped.
    /// </param>
    /// <param name="textAlignment">Alignment.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <param name="textFormatter"><see cref="TextFormatter"/> instance to access any of his objects.</param>
    /// <param name="preserveTabs">
    ///     If <see langword="true"/> tabs will be preserved, considering the spaces according to the provided <see cref="TabWidth"/>.
    ///     If <see langword="false"/>, default, tabs will be replaced with spaces according to the provided <see cref="TabWidth"/>.
    /// </param>
    /// <returns>Justified and clipped text.</returns>
    public static string ClipAndJustify (
        string text,
        int width,
        Alignment textAlignment,
        TextDirection textDirection = TextDirection.LeftRight_TopBottom,
        int tabWidth = 0,
        TextFormatter? textFormatter = null,
        bool preserveTabs = false
    )
    {
        return ClipAndJustify (text, width, textAlignment == Alignment.Fill, textDirection, tabWidth, textFormatter, preserveTabs);
    }

    /// <summary>Justifies text within a specified width.</summary>
    /// <param name="text">The text to justify.</param>
    /// <param name="width">
    ///     The number of columns to clip the text to. Text longer than <paramref name="width"/> will be
    ///     clipped.
    /// </param>
    /// <param name="justify">Justify.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <param name="textFormatter"><see cref="TextFormatter"/> instance to access any of his objects.</param>
    /// <param name="preserveTabs">
    ///     If <see langword="true"/> tabs will be preserved, considering the spaces according to the provided <see cref="TabWidth"/>.
    ///     If <see langword="false"/>, default, tabs will be replaced with spaces according to the provided <see cref="TabWidth"/>.
    /// </param>
    /// <returns>Justified and clipped text.</returns>
    public static string ClipAndJustify (
        string text,
        int width,
        bool justify,
        TextDirection textDirection = TextDirection.LeftRight_TopBottom,
        int tabWidth = 0,
        TextFormatter? textFormatter = null,
        bool preserveTabs = false
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative (width, nameof (width));

        if (string.IsNullOrEmpty (text))
        {
            return text;
        }

        text = ReplaceTABWithSpaces (text, tabWidth, preserveTabs);
        List<string> graphemes = [.. GraphemeHelper.GetGraphemes (text)];
        int zeroLength = graphemes.Sum (s => s.GetColumns (false) == 0 ? 1 : 0);

        if (graphemes.Count - zeroLength > width)
        {
            if (IsHorizontalDirection (textDirection))
            {
                if (textFormatter is { Alignment: Alignment.End })
                {
                    return GetRangeThatFits (graphemes, graphemes.Count - width, text, width, tabWidth, textDirection);
                }

                if (textFormatter is { Alignment: Alignment.Center })
                {
                    return GetRangeThatFits (graphemes, Math.Max ((graphemes.Count - width - zeroLength) / 2, 0), text, width, tabWidth, textDirection);
                }

                return GetRangeThatFits (graphemes, 0, text, width, tabWidth, textDirection);
            }

            if (IsVerticalDirection (textDirection))
            {
                if (textFormatter is { VerticalAlignment: Alignment.End })
                {
                    return GetRangeThatFits (graphemes, graphemes.Count - width, text, width, tabWidth, textDirection);
                }

                if (textFormatter is { VerticalAlignment: Alignment.Center })
                {
                    return GetRangeThatFits (graphemes, Math.Max ((graphemes.Count - width - zeroLength) / 2, 0), text, width, tabWidth, textDirection);
                }

                return GetRangeThatFits (graphemes, 0, text, width, tabWidth, textDirection);
            }

            return StringExtensions.ToString (graphemes.GetRange (0, width + zeroLength));
        }

        if (justify)
        {
            return Justify (text, width, ' ', textDirection, tabWidth);
        }

        if (IsHorizontalDirection (textDirection))
        {
            if (textFormatter is { Alignment: Alignment.End })
            {
                if (GetTextWidth (text, tabWidth, textDirection) > width)
                {
                    return GetRangeThatFits (graphemes, graphemes.Count - width, text, width, tabWidth, textDirection);
                }
            }
            else if (textFormatter is { Alignment: Alignment.Center })
            {
                return GetRangeThatFits (graphemes, Math.Max ((graphemes.Count - width - zeroLength) / 2, 0), text, width, tabWidth, textDirection);
            }
            else if (GetTextWidth (text, tabWidth, textDirection) > width)
            {
                return GetRangeThatFits (graphemes, 0, text, width, tabWidth, textDirection);
            }
        }

        if (IsVerticalDirection (textDirection))
        {
            if (textFormatter is { VerticalAlignment: Alignment.End })
            {
                if (graphemes.Count - zeroLength > width)
                {
                    return GetRangeThatFits (graphemes, graphemes.Count - width, text, width, tabWidth, textDirection);
                }
            }
            else if (textFormatter is { VerticalAlignment: Alignment.Center })
            {
                return GetRangeThatFits (graphemes, Math.Max ((graphemes.Count - width - zeroLength) / 2, 0), text, width, tabWidth, textDirection);
            }
            else if (graphemes.Count - zeroLength > width)
            {
                return GetRangeThatFits (graphemes, 0, text, width, tabWidth, textDirection);
            }
        }

        return text;
    }

    private static string GetRangeThatFits (List<string> strings, int index, string text, int width, int tabWidth, TextDirection textDirection)
    {
        return StringExtensions.ToString (
                                          strings.GetRange (
                                                          Math.Max (index, 0),
                                                          GetLengthThatFits (text, width, tabWidth, textDirection)
                                                         )
                                         );
    }

    /// <summary>
    ///     Justifies the text to fill the width provided. Space will be added between words to make the text just fit
    ///     <c>width</c>. Spaces will not be added to the start or end.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="width"></param>
    /// <param name="spaceChar">Character to replace whitespace and pad with. For debugging purposes.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <returns>The justified text.</returns>
    public static string Justify (
        string text,
        int width,
        char spaceChar = ' ',
        TextDirection textDirection = TextDirection.LeftRight_TopBottom,
        int tabWidth = 0
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative (width, nameof (width));

        if (string.IsNullOrEmpty (text))
        {
            return text;
        }

        text = ReplaceTABWithSpaces (text, tabWidth);
        string [] words = text.Split (' ');
        int textCount;

        if (IsHorizontalDirection (textDirection))
        {
            textCount = words.Sum (arg => GetTextWidth (arg, tabWidth, textDirection));
        }
        else
        {
            textCount = words.Sum (GraphemeHelper.GetGraphemeCount) - GraphemeHelper.GetGraphemes (text).Sum (s => s.GetColumns () == 0 ? 1 : 0);
        }

        int spaces = words.Length > 1 ? (width - textCount) / (words.Length - 1) : 0;
        int extras = words.Length > 1 ? (width - textCount) % (words.Length - 1) : 0;

        var s = new StringBuilder ();

        for (var w = 0; w < words.Length; w++)
        {
            string x = words [w];
            s.Append (x);

            if (w + 1 < words.Length)
            {
                for (var i = 0; i < spaces; i++)
                {
                    s.Append (spaceChar);
                }
            }

            if (extras > 0)
            {
                for (var i = 0; i < 1; i++)
                {
                    s.Append (spaceChar);
                }

                extras--;
            }

            if (w + 1 == words.Length - 1)
            {
                for (var i = 0; i < extras; i++)
                {
                    s.Append (spaceChar);
                }
            }
        }

        return s.ToString ();
    }

    /// <summary>Formats text into lines, applying text alignment and optionally wrapping text to new lines on word boundaries.</summary>
    /// <param name="text"></param>
    /// <param name="width">The number of columns to constrain the text to for word wrapping and clipping.</param>
    /// <param name="textAlignment">Specifies how the text will be aligned horizontally.</param>
    /// <param name="wordWrap">
    ///     If <see langword="true"/>, the text will be wrapped to new lines no longer than
    ///     <paramref name="width"/>. If <see langword="false"/>, forces text to fit a single line. Line breaks are converted
    ///     to spaces. The text will be clipped to <paramref name="width"/>.
    /// </param>
    /// <param name="preserveTrailingSpaces">
    ///     If <see langword="true"/> trailing spaces at the end of wrapped lines will be
    ///     preserved. If <see langword="false"/> , trailing spaces at the end of wrapped lines will be trimmed.
    /// </param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <param name="multiLine">If <see langword="true"/> new lines are allowed.</param>
    /// <param name="textFormatter"><see cref="TextFormatter"/> instance to access any of his objects.</param>
    /// <param name="preserveTabs">
    ///     If <see langword="true"/> tabs will be preserved, considering the spaces according to the provided <paramref name="tabWidth"/>.
    ///     If <see langword="false"/>, default, tabs will be replaced with spaces according to the provided <paramref name="tabWidth"/>.
    /// </param>
    /// <returns>A list of word wrapped lines.</returns>
    /// <remarks>
    ///     <para>An empty <paramref name="text"/> string will result in one empty line.</para>
    ///     <para>If <paramref name="width"/> is 0, a single, empty line will be returned.</para>
    ///     <para>If <paramref name="width"/> is int.MaxValue, the text will be formatted to the maximum width possible.</para>
    /// </remarks>
    public static List<string> Format (
        string text,
        int width,
        Alignment textAlignment,
        bool wordWrap,
        bool preserveTrailingSpaces = false,
        int tabWidth = 0,
        TextDirection textDirection = TextDirection.LeftRight_TopBottom,
        bool multiLine = false,
        TextFormatter? textFormatter = null,
        bool preserveTabs = false
    )
    {
        return Format (
                       text,
                       width,
                       textAlignment == Alignment.Fill,
                       wordWrap,
                       preserveTrailingSpaces,
                       tabWidth,
                       textDirection,
                       multiLine,
                       textFormatter,
                       preserveTabs
                      );
    }

    /// <summary>Formats text into lines, applying text alignment and optionally wrapping text to new lines on word boundaries.</summary>
    /// <param name="text"></param>
    /// <param name="width">The number of columns to constrain the text to for word wrapping and clipping.</param>
    /// <param name="justify">Specifies whether the text should be justified.</param>
    /// <param name="wordWrap">
    ///     If <see langword="true"/>, the text will be wrapped to new lines no longer than
    ///     <paramref name="width"/>. If <see langword="false"/>, forces text to fit a single line. Line breaks are converted
    ///     to spaces. The text will be clipped to <paramref name="width"/>.
    /// </param>
    /// <param name="preserveTrailingSpaces">
    ///     If <see langword="true"/> trailing spaces at the end of wrapped lines will be
    ///     preserved. If <see langword="false"/> , trailing spaces at the end of wrapped lines will be trimmed.
    /// </param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <param name="multiLine">If <see langword="true"/> new lines are allowed.</param>
    /// <param name="textFormatter"><see cref="TextFormatter"/> instance to access any of his objects.</param>
    /// <param name="preserveTabs">
    ///     If <see langword="true"/> tabs will be preserved, considering the spaces according to the provided <paramref name="tabWidth"/>.
    ///     If <see langword="false"/>, default, tabs will be replaced with spaces according to the provided <paramref name="tabWidth"/>.
    /// </param>
    /// <returns>A list of word wrapped lines.</returns>
    /// <remarks>
    ///     <para>An empty <paramref name="text"/> string will result in one empty line.</para>
    ///     <para>If <paramref name="width"/> is 0, a single, empty line will be returned.</para>
    ///     <para>If <paramref name="width"/> is int.MaxValue, the text will be formatted to the maximum width possible.</para>
    /// </remarks>
    public static List<string> Format (
        string text,
        int width,
        bool justify,
        bool wordWrap,
        bool preserveTrailingSpaces = false,
        int tabWidth = 0,
        TextDirection textDirection = TextDirection.LeftRight_TopBottom,
        bool multiLine = false,
        TextFormatter? textFormatter = null,
        bool preserveTabs = false
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative (width, nameof (width));

        List<string> lineResult = new ();

        if (string.IsNullOrEmpty (text) || width == 0)
        {
            lineResult.Add (string.Empty);

            return lineResult;
        }

        if (!wordWrap)
        {
            text = ReplaceTABWithSpaces (text, tabWidth, preserveTabs);

            if (multiLine)
            {
                // Abhorrent case: Just a new line
                if (text == "\n")
                {
                    lineResult.Add (string.Empty);

                    return lineResult;
                }

                string []? lines = null;

                if (text.Contains ("\r\n"))
                {
                    lines = text.Split ("\r\n");
                }
                else if (text.Contains ('\n'))
                {
                    lines = text.Split ('\n');
                }

                lines ??= new [] { text };

                foreach (string line in lines)
                {
                    lineResult.Add (
                                    ClipAndJustify (
                                                    PerformCorrectFormatDirection (textDirection, line),
                                                    width,
                                                    justify,
                                                    textDirection,
                                                    tabWidth,
                                                    textFormatter,
                                                    preserveTabs));
                }

                return PerformCorrectFormatDirection (textDirection, lineResult);
            }

            text = ReplaceCRLFWithSpace (text);
            lineResult.Add (ClipAndJustify (PerformCorrectFormatDirection (textDirection, text), width, justify, textDirection, tabWidth, textFormatter, preserveTabs));

            return PerformCorrectFormatDirection (textDirection, lineResult);
        }

        List<Rune> runes = StripCRLF (text, true).ToRuneList ();
        int runeCount = runes.Count;
        var lp = 0;

        for (var i = 0; i < runeCount; i++)
        {
            Rune c = runes [i];

            if (c.Value == '\n')
            {
                List<string> wrappedLines =
                    WordWrapText (
                                  StringExtensions.ToString (PerformCorrectFormatDirection (textDirection, runes.GetRange (lp, i - lp))),
                                  width,
                                  preserveTrailingSpaces,
                                  tabWidth,
                                  textDirection,
                                  textFormatter,
                                  preserveTabs
                                 );

                foreach (string line in wrappedLines)
                {
                    lineResult.Add (ClipAndJustify (line, width, justify, textDirection, tabWidth, preserveTabs: preserveTabs));
                }

                if (wrappedLines.Count == 0)
                {
                    lineResult.Add (string.Empty);
                }

                lp = i + 1;
            }
        }

        foreach (string line in WordWrapText (
                                              StringExtensions.ToString (PerformCorrectFormatDirection (textDirection, runes.GetRange (lp, runeCount - lp))),
                                              width,
                                              preserveTrailingSpaces,
                                              tabWidth,
                                              textDirection,
                                              textFormatter,
                                              preserveTabs
                                             ))
        {
            lineResult.Add (ClipAndJustify (line, width, justify, textDirection, tabWidth, preserveTabs: preserveTabs));
        }

        return PerformCorrectFormatDirection (textDirection, lineResult);
    }

    private static string PerformCorrectFormatDirection (TextDirection textDirection, string line)
    {
        return textDirection switch
        {
            TextDirection.RightLeft_BottomTop
                or TextDirection.RightLeft_TopBottom
                or TextDirection.BottomTop_LeftRight
                or TextDirection.BottomTop_RightLeft => StringExtensions.ToString (GraphemeHelper.GetGraphemes (line).Reverse ()),
            _ => line
        };
    }

    private static List<Rune> PerformCorrectFormatDirection (TextDirection textDirection, List<Rune> runes)
    {
        return PerformCorrectFormatDirection (textDirection, StringExtensions.ToString (runes)).ToRuneList ();
    }

    private static List<string> PerformCorrectFormatDirection (TextDirection textDirection, List<string> lines)
    {
        return textDirection switch
        {
            TextDirection.TopBottom_RightLeft
                or TextDirection.LeftRight_BottomTop
                or TextDirection.RightLeft_BottomTop
                or TextDirection.BottomTop_RightLeft => [.. lines.ToArray ().Reverse ()],
            _ => lines
        };
    }

    /// <summary>
    ///     Returns the number of columns required to render <paramref name="lines"/> oriented vertically.
    /// </summary>
    /// <remarks>
    ///     This API will return incorrect results if the text includes glyphs whose width is dependent on surrounding
    ///     glyphs (e.g. Arabic).
    /// </remarks>
    /// <param name="lines">The lines.</param>
    /// <param name="startLine">The line in the list to start with (any lines before will be ignored).</param>
    /// <param name="linesCount">
    ///     The number of lines to process (if less than <c>lines.Count</c>, any lines after will be
    ///     ignored).
    /// </param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <returns>The width required.</returns>
    public static int GetColumnsRequiredForVerticalText (
        List<string> lines,
        int startLine = -1,
        int linesCount = -1,
        int tabWidth = 0
    )
    {
        var max = 0;

        for (int i = startLine == -1 ? 0 : startLine;
             i < (linesCount == -1 ? lines.Count : startLine + linesCount);
             i++)
        {
            string strings = lines [i];

            if (strings.Length > 0)
            {
                max += GraphemeHelper.GetGraphemes (strings).Max (s => GetStringWidth (s, tabWidth));
            }
        }

        return max;
    }

    /// <summary>
    ///     Returns the number of columns in the widest line in the text, without word wrap, accounting for wide-glyphs
    ///     (uses <see cref="StringExtensions.GetColumns"/>). <paramref name="text"/> if it contains newlines.
    /// </summary>
    /// <remarks>
    ///     This API will return incorrect results if the text includes glyphs whose width is dependent on surrounding
    ///     glyphs (e.g. Arabic).
    /// </remarks>
    /// <param name="text">Text, may contain newlines.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <returns>The length of the longest line.</returns>
    public static int GetWidestLineLength (string text, int tabWidth = 0)
    {
        List<string> result = SplitNewLine (text);

        return result.Max (x => GetTextWidth (x, tabWidth));
    }

    /// <summary>
    ///     Gets the maximum number of columns from the text based on the <paramref name="startIndex"/> and the
    ///     <paramref name="length"/>.
    /// </summary>
    /// <remarks>
    ///     This API will return incorrect results if the text includes glyphs whose width is dependent on surrounding
    ///     glyphs (e.g. Arabic).
    /// </remarks>
    /// <param name="text">The text.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="length">The length.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <returns>The maximum characters width.</returns>
    public static int GetSumMaxCharWidth (string text, int startIndex = -1, int length = -1, int tabWidth = 0)
    {
        var max = 0;
        string [] graphemes = [.. GraphemeHelper.GetGraphemes (text)];

        for (int i = startIndex == -1 ? 0 : startIndex;
             i < (length == -1 ? graphemes.Length : startIndex + length);
             i++)
        {
            max += GetStringWidth (graphemes [i], tabWidth, textWidth: max);
        }

        return max;
    }

    /// <summary>Gets the number of the Runes in the text that will fit in <paramref name="width"/>.</summary>
    /// <remarks>
    ///     This API will return incorrect results if the text includes glyphs whose width is dependent on surrounding
    ///     glyphs (e.g. Arabic).
    /// </remarks>
    /// <param name="text">The text.</param>
    /// <param name="width">The width.</param>
    /// <param name="tabWidth">The width used for a tab.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <returns>The index of the text that fit the width.</returns>
    public static int GetLengthThatFits (string text, int width, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
    {
        if (string.IsNullOrEmpty (text))
        {
            return 0;
        }

        var textLength = 0;
        var stringIdx = 0;

        foreach (string grapheme in GraphemeHelper.GetGraphemes (text))
        {
            int lastTextWidth = textLength;
            int textWidth = GetStringWidth (grapheme, tabWidth, textDirection, textLength) - lastTextWidth;

            if (textLength + textWidth > width)
            {
                break;
            }

            textLength += textWidth;
            stringIdx++;
        }

        return stringIdx;
    }

    private static int GetTextWidth (string str, int tabWidth, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
    {
        int graphemesWidth = 0;

        foreach (string grapheme in GraphemeHelper.GetGraphemes (str))
        {
            graphemesWidth = GetStringWidth (grapheme, tabWidth, textDirection, graphemesWidth);
        }

        return graphemesWidth;
    }

    private static int GetStringWidth (string text, int tabWidth, TextDirection textDirection = TextDirection.LeftRight_TopBottom, int textWidth = 0)
    {
        foreach (string grapheme in GraphemeHelper.GetGraphemes (text))
        {
            if (IsHorizontalDirection (textDirection))
            {
                if (grapheme == "\t")
                {
                    if (tabWidth > 0)
                    {
                        textWidth += tabWidth - textWidth % tabWidth;
                    }
                    else
                    {
                        textWidth += 0;
                    }
                }
                else
                {
                    int glyphWidth = grapheme.GetColumns (false);

                    if (glyphWidth is < 0 or > 0)
                    {
                        textWidth += Math.Max (glyphWidth, 1);
                    }
                }
            }
            else
            {
                textWidth += grapheme.GetColumns () == 0 ? 0 : 1;
            }
        }

        return textWidth;
    }

    /// <summary>Gets the index position from the list based on the <paramref name="width"/>.</summary>
    /// <remarks>
    ///     This API will return incorrect results if the text includes glyphs whose width is dependent on surrounding
    ///     glyphs (e.g. Arabic).
    /// </remarks>
    /// <param name="lines">The lines.</param>
    /// <param name="width">The width.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <returns>The index of the list that fit the width.</returns>
    public static int GetMaxColsForWidth (List<string> lines, int width, int tabWidth = 0)
    {
        var textLength = 0;
        var lineIdx = 0;

        for (; lineIdx < lines.Count; lineIdx++)
        {
            string [] graphemes = [.. GraphemeHelper.GetGraphemes (lines [lineIdx])];
            int lastTextLength = textLength;
            int maxTextWidth = graphemes.Length > 0
                                    ? graphemes.Max (r => GetStringWidth (r, tabWidth, textWidth: lastTextLength) - lastTextLength)
                                    : 1;

            if (textLength + maxTextWidth > width)
            {
                break;
            }

            textLength += maxTextWidth;
        }

        return lineIdx;
    }

    /// <summary>Finds the HotKey and its location in text.</summary>
    /// <param name="text">The text to look in.</param>
    /// <param name="hotKeySpecifier">The HotKey specifier (e.g. '_') to look for.</param>
    /// <param name="hotPos">Outputs the Rune index into <c>text</c>.</param>
    /// <param name="hotKey">Outputs the hotKey. <see cref="Key.Empty"/> if not found.</param>
    /// <param name="firstUpperCase">
    ///     If <c>true</c> the legacy behavior of identifying the first upper case character as the
    ///     HotKey will be enabled. Regardless of the value of this parameter, <c>hotKeySpecifier</c> takes precedence.
    ///     Defaults to <see langword="false"/>.
    /// </param>
    /// <returns><c>true</c> if a HotKey was found; <c>false</c> otherwise.</returns>
    public static bool FindHotKey (
        string text,
        Rune hotKeySpecifier,
        out int hotPos,
        out Key hotKey,
        bool firstUpperCase = false
    )
    {
        if (string.IsNullOrEmpty (text) || hotKeySpecifier == (Rune)0xFFFF)
        {
            hotPos = -1;
            hotKey = Key.Empty;

            return false;
        }

        var curHotKey = (Rune)0;
        int curHotPos = -1;

        // Use first hot_key char passed into 'hotKey'.
        // TODO: Ignore hot_key of two are provided
        // TODO: Do not support non-alphanumeric chars that can't be typed
        var i = 0;

        foreach (Rune c in text.EnumerateRunes ())
        {
            if ((char)c.Value != 0xFFFD)
            {
                if (c == hotKeySpecifier)
                {
                    curHotPos = i;
                }
                else if (curHotPos > -1)
                {
                    curHotKey = c;

                    break;
                }
            }

            i++;
        }

        // Legacy support - use first upper case char if the specifier was not found
        if (curHotPos == -1 && firstUpperCase)
        {
            i = 0;

            foreach (Rune c in text.EnumerateRunes ())
            {
                if ((char)c.Value != 0xFFFD)
                {
                    if (Rune.IsUpper (c))
                    {
                        curHotKey = c;
                        curHotPos = i;

                        break;
                    }
                }

                i++;
            }
        }

        if (curHotKey != (Rune)0 && curHotPos != -1)
        {
            hotPos = curHotPos;

            var newHotKey = (KeyCode)curHotKey.Value;

            if (newHotKey != KeyCode.Null && !(newHotKey == KeyCode.Space || Rune.IsControl (curHotKey)))
            {
                if ((newHotKey & ~KeyCode.Space) is >= KeyCode.A and <= KeyCode.Z)
                {
                    newHotKey &= ~KeyCode.Space;
                }

                hotKey = newHotKey;

                //hotKey.Scope = KeyBindingScope.HotKey;

                return true;
            }
        }

        hotPos = -1;
        hotKey = KeyCode.Null;

        return false;
    }

    /// <summary>
    ///     Replaces the Rune at the index specified by the <c>hotPos</c> parameter with a tag identifying it as the
    ///     hotkey.
    /// </summary>
    /// <param name="text">The text to tag the hotkey in.</param>
    /// <param name="hotPos">The Rune index of the hotkey in <c>text</c>.</param>
    /// <returns>The text with the hotkey tagged.</returns>
    /// <remarks>The returned string will not render correctly without first un-doing the tag. To undo the tag, search for</remarks>
    public static string ReplaceHotKeyWithTag (string text, int hotPos)
    {
        // Set the high bit
        List<Rune> runes = text.ToRuneList ();

        if (Rune.IsLetterOrDigit (runes [hotPos]))
        {
            runes [hotPos] = new ((uint)runes [hotPos].Value);
        }

        return StringExtensions.ToString (runes);
    }

    /// <summary>Removes the hotkey specifier from text.</summary>
    /// <param name="text">The text to manipulate.</param>
    /// <param name="hotKeySpecifier">The hot-key specifier (e.g. '_') to look for.</param>
    /// <param name="hotPos">Returns the position of the hot-key in the text. -1 if not found.</param>
    /// <returns>The input text with the hotkey specifier ('_') removed.</returns>
    public static string RemoveHotKeySpecifier (string text, int hotPos, Rune hotKeySpecifier)
    {
        if (string.IsNullOrEmpty (text))
        {
            return text;
        }

        const int maxStackallocCharBufferSize = 512; // ~1 kB
        char []? rentedBufferArray = null;
        try
        {
            Span<char> buffer = text.Length <= maxStackallocCharBufferSize
                ? stackalloc char [text.Length]
                : (rentedBufferArray = ArrayPool<char>.Shared.Rent (text.Length));

            int i = 0;
            var remainingBuffer = buffer;
            foreach (Rune c in text.EnumerateRunes ())
            {
                if (c == hotKeySpecifier && i == hotPos)
                {
                    i++;
                    continue;
                }
                int charsWritten = c.EncodeToUtf16 (remainingBuffer);
                remainingBuffer = remainingBuffer [charsWritten..];
                i++;
            }

            ReadOnlySpan<char> newText = buffer [..^remainingBuffer.Length];
            // If the resulting string would be the same as original then just return the original.
            if (newText.Equals (text, StringComparison.Ordinal))
            {
                return text;
            }

            return new string (newText);
        }
        finally
        {
            if (rentedBufferArray != null)
            {
                ArrayPool<char>.Shared.Return (rentedBufferArray);
            }
        }
    }

    #endregion // Static Members
}

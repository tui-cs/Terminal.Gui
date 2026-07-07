using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Views;

/// <summary>
///     A scrollable map of the Unicode codepoints.
/// </summary>
/// <remarks>
/// <img src="../images/views/CharMap.gif" alt="CharMap demo"/>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Left / Right</term> <description>Moves one codepoint left or right.</description>
///         </item>
///         <item>
///             <term>Up / Down</term> <description>Moves one row up or down.</description>
///         </item>
///         <item>
///             <term>PageUp / PageDown</term> <description>Moves one page up or down.</description>
///         </item>
///         <item>
///             <term>Home / End</term> <description>Moves to the first or last codepoint.</description>
///         </item>
///     </list>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Click</term>
///             <description>Selects the clicked codepoint (<see cref="Command.Activate"/>).</description>
///         </item>
///         <item>
///             <term>Double-Click</term>
///             <description>Accepts the clicked codepoint (<see cref="Command.Accept"/>).</description>
///         </item>
///         <item>
///             <term>Right-Click / Ctrl+Click</term> <description>Opens the context menu.</description>
///         </item>
///         <item>
///             <term>Wheel Up / Down / Left / Right</term> <description>Scrolls the map.</description>
///         </item>
///     </list>
/// </remarks>
public class CharMap : View, IDesignable, IValue<Rune>
{
    /// <summary>
    ///     Gets or sets the default cursor style.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static CursorStyle DefaultCursorStyle
    {
        get => CharMapSettings.Defaults.DefaultCursorStyle;
        set => CharMapSettings.Defaults.DefaultCursorStyle = value;
    }

    private const int COLUMN_WIDTH = 3; // Width of each column of glyphs
    private const int HEADER_HEIGHT = 1; // Height of the header

    // ReSharper disable once InconsistentNaming
    private static readonly int MAX_CODE_POINT = UnicodeRange.Ranges.Max (r => r.End);
    private static readonly int MAX_ROW = MAX_CODE_POINT / 16;
    private const int SURROGATE_ROW_START = 0xD800 / 16;
    private const int SURROGATE_ROW_END = 0xDFFF / 16;
    private const int SURROGATE_ROW_COUNT = SURROGATE_ROW_END - SURROGATE_ROW_START + 1;

    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    public CharMap ()
    {
        CanFocus = true;

        AddCommand (Command.Up, commandContext => Move (commandContext, -16));
        AddCommand (Command.Down, commandContext => Move (commandContext, 16));
        AddCommand (Command.Left, commandContext => Move (commandContext, -1));
        AddCommand (Command.Right, commandContext => Move (commandContext, 1));

        AddCommand (Command.PageUp, commandContext => Move (commandContext, -(Viewport.Height - HEADER_HEIGHT / _rowHeight) * 16));
        AddCommand (Command.PageDown, commandContext => Move (commandContext, (Viewport.Height - HEADER_HEIGHT / _rowHeight) * 16));
        AddCommand (Command.Start, commandContext => Move (commandContext, -SelectedCodePoint));
        AddCommand (Command.End, commandContext => Move (commandContext, MAX_CODE_POINT - SelectedCodePoint));

        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));
        AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));

        AddCommand (Command.Context, HandleContextCommand);

        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);
        // ReplaceCommands, not Add: PopoverMenu.DefaultKey is configurable and may collide with a key
        // that is already bound (e.g. Ctrl+P); the user's context-menu key wins.
        KeyBindings.ReplaceCommands (PopoverMenu.DefaultKey, Command.Context);

        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Activate);
        MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Accept);
        MouseBindings.ReplaceCommands (MouseFlags.RightButtonClicked, Command.Context);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked | MouseFlags.Ctrl, Command.Context);
        MouseBindings.Add (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.Add (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.Add (MouseFlags.WheeledLeft, Command.ScrollLeft);
        MouseBindings.Add (MouseFlags.WheeledRight, Command.ScrollRight);

        // Initial content size; height will be corrected by RebuildVisibleRows()
        SetContentSize (new Size (COLUMN_WIDTH * 16 + RowLabelWidth, HEADER_HEIGHT + _rowHeight));

        // Set up the horizontal scrollbar with manual visibility control.
        HorizontalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Manual;
        HorizontalScrollBar.Increment = COLUMN_WIDTH;

        // This prevents scrolling past the last column
        HorizontalScrollBar.ScrollableContentSize = GetContentWidth () - RowLabelWidth;
        HorizontalScrollBar.X = RowLabelWidth;
        HorizontalScrollBar.Y = Pos.AnchorEnd ();
        HorizontalScrollBar.Width = Dim.Fill (1);

        // CharMap has fixed row labels on the left that don't scroll, with scrollable content to the right.
        // The default viewport clamping (which prevents blank space) doesn't work well with this model,
        // so we allow the viewport to extend beyond content bounds.
        ViewportSettings |= ViewportSettingsFlags.AllowLocationPlusSizeGreaterThanContentSize;

        // We want the horizontal scrollbar to only show when needed.
        // We can't use ScrollBarVisibilityMode.Auto because we are using custom ContentSize
        // So, we do it manually on ViewportChanged events.
        ViewportChanged += (_, _) =>
                           {
                               HorizontalScrollBar.Visible = Viewport.Width < GetContentWidth ();
                               UpdateCursor ();
                           };

        // Set up the vertical scrollbar with auto-show behavior.
        ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
        VerticalScrollBar.X = Pos.AnchorEnd ();
        VerticalScrollBar.Y = HEADER_HEIGHT; // Header

        // The scrollbars are in the Padding. VisualRole.Focus/Active are used to draw the
        // CharMap headers. Override Padding to force it to draw to match.
        Padding.View!.GettingAttributeForRole += PaddingOnGettingAttributeForRole;

        // Build initial visible rows (all rows with at least one valid codepoint)
        RebuildVisibleRows ();

        Cursor = new Cursor { Style = DefaultCursorStyle };
    }

    // Visible rows management when filtering by Unicode category.
    private List<int>? _visibleRowStarts;
    private Dictionary<int, int>? _rowStartToVisibleIndex;

    private void RebuildVisibleRows ()
    {
        if (!ShowUnicodeCategory.HasValue)
        {
            _visibleRowStarts = null;
            _rowStartToVisibleIndex = null;
            SetContentSize (new Size (COLUMN_WIDTH * 16 + RowLabelWidth, VisibleRowCount * _rowHeight + HEADER_HEIGHT));
            VerticalScrollBar.ScrollableContentSize = GetContentHeight ();

            return;
        }

        _visibleRowStarts ??= [];
        _rowStartToVisibleIndex ??= [];

        _visibleRowStarts.Clear ();
        _rowStartToVisibleIndex.Clear ();

        int maxRow = MAX_ROW;

        for (var row = 0; row <= maxRow; row++)
        {
            int start = row * 16;
            var anyValid = false;
            var anyVisible = false;

            for (var col = 0; col < 16; col++)
            {
                int cp = start + col;

                if (cp > RuneExtensions.MaxUnicodeCodePoint)
                {
                    break;
                }

                if (!Rune.IsValid (cp))
                {
                    continue;
                }

                anyValid = true;

                if (!ShowUnicodeCategory.HasValue)
                {
                    // With no filter, a row is displayed if it has any valid codepoint
                    anyVisible = true;

                    break;
                }

                UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory (cp);

                if (cat != ShowUnicodeCategory.Value)
                {
                    continue;
                }
                anyVisible = true;

                break;
            }

            if (!anyValid || (ShowUnicodeCategory.HasValue && !anyVisible))
            {
                continue;
            }
            _rowStartToVisibleIndex [start] = _visibleRowStarts.Count;
            _visibleRowStarts.Add (start);
        }

        // Update content size to match visible rows
        SetContentSize (new Size (COLUMN_WIDTH * 16 + RowLabelWidth, VisibleRowCount * _rowHeight + HEADER_HEIGHT));

        // Keep vertical scrollbar aligned with new content size
        VerticalScrollBar.ScrollableContentSize = GetContentHeight ();
    }

    private int VisibleRowCount
    {
        get
        {
            if (ShowUnicodeCategory.HasValue)
            {
                return _visibleRowStarts?.Count ?? 0;
            }

            int rowCount = MAX_ROW + 1;

            if (MAX_ROW >= SURROGATE_ROW_START)
            {
                rowCount -= Math.Min (MAX_ROW, SURROGATE_ROW_END) - SURROGATE_ROW_START + 1;
            }

            return rowCount;
        }
    }

    private int VisibleRowIndexForCodePoint (int codePoint)
    {
        int start = codePoint / 16 * 16;

        if (ShowUnicodeCategory.HasValue)
        {
            return _rowStartToVisibleIndex?.GetValueOrDefault (start, -1) ?? -1;
        }

        int row = start / 16;

        if (row > MAX_ROW || row is >= SURROGATE_ROW_START and <= SURROGATE_ROW_END)
        {
            return -1;
        }

        return row > SURROGATE_ROW_END ? row - SURROGATE_ROW_COUNT : row;
    }

    private bool TryGetVisibleRowStart (int visibleRow, out int rowStart)
    {
        if (visibleRow < 0 || visibleRow >= VisibleRowCount)
        {
            rowStart = 0;

            return false;
        }

        if (ShowUnicodeCategory.HasValue)
        {
            rowStart = _visibleRowStarts! [visibleRow];

            return true;
        }

        int row = visibleRow >= SURROGATE_ROW_START ? visibleRow + SURROGATE_ROW_COUNT : visibleRow;
        rowStart = row * 16;

        return rowStart <= MAX_CODE_POINT;
    }

    private bool TryGetNearestVisibleRowStart (int desiredRowStart, out int rowStart)
    {
        if (ShowUnicodeCategory.HasValue)
        {
            List<int>? visibleRowStarts = _visibleRowStarts;
            int idx = visibleRowStarts?.FindIndex (s => s >= desiredRowStart) ?? -1;

            if (idx < 0 && visibleRowStarts?.Count > 0)
            {
                idx = visibleRowStarts.Count - 1;
            }

            if (idx >= 0)
            {
                rowStart = visibleRowStarts! [idx];

                return true;
            }

            rowStart = 0;

            return false;
        }

        int row = Math.Clamp (desiredRowStart / 16, 0, MAX_ROW);

        if (row is >= SURROGATE_ROW_START and <= SURROGATE_ROW_END)
        {
            row = SURROGATE_ROW_END + 1;
        }

        rowStart = Math.Min (row * 16, MAX_CODE_POINT);

        return true;
    }

    private int _rowHeight = 1; // Height of each row of 16 glyphs - changing this is not tested

    /// <summary>
    ///     Gets or sets the currently selected codepoint. Causes the Viewport to scroll to make the selected code point
    ///     visible.
    /// </summary>
    public int SelectedCodePoint
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            int oldSelectedCodePoint = field;
            int newSelectedCodePoint = Math.Clamp (value, 0, MAX_CODE_POINT);

            Rune oldValue = new (oldSelectedCodePoint);
            Rune newValue = new (newSelectedCodePoint);

            ValueChangingEventArgs<Rune> changingArgs = new (oldValue, newValue);

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            Point offsetToNewCursor = GetCursor (newSelectedCodePoint);

            field = newSelectedCodePoint;

            // Ensure the new cursor position is visible
            ScrollToMakeCursorVisible (offsetToNewCursor);

            SetNeedsDraw ();
            UpdateCursor ();

            ValueChangedEventArgs<Rune> changedArgs = new (oldValue, new Rune (field));
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, new Rune (field)));
        }
    }

    #region IValue<Rune> Implementation

    /// <inheritdoc/>
    public Rune Value { get => new (SelectedCodePoint); set => SelectedCodePoint = value.Value; }

    /// <inheritdoc/>
    object IValue.GetValue () => new Rune (SelectedCodePoint);

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Called when the <see cref="CharMap"/> <see cref="Value"/> is changing.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<Rune> args) => false;

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<Rune>>? ValueChanging;

    /// <summary>
    ///     Called when the <see cref="CharMap"/> <see cref="Value"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<Rune> args) { }

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<Rune>>? ValueChanged;

    #endregion

    /// <summary>
    ///     Gets or sets whether the number of columns each glyph is displayed.
    /// </summary>
    public bool ShowGlyphWidths
    {
        get => _rowHeight == 2;
        set
        {
            _rowHeight = value ? 2 : 1;

            // height changed => content height depends on row height
            RebuildVisibleRows ();
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Specifies the starting offset for the character map. The default is 0x2500 which is the Box Drawing
    ///     characters.
    /// </summary>
    public int StartCodePoint
    {
        get;
        set
        {
            field = value;
            SelectedCodePoint = value;
        }
    }

    /// <summary>
    ///     When set, only glyphs whose UnicodeCategory matches the value are rendered. If <see langword="null"/> (default),
    ///     all glyphs are rendered.
    /// </summary>
    public UnicodeCategory? ShowUnicodeCategory
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            RebuildVisibleRows ();

            // Ensure selection is on a visible row
            int desiredRowStart = SelectedCodePoint / 16 * 16;

            if (VisibleRowIndexForCodePoint (desiredRowStart) < 0)
            {
                if (TryGetNearestVisibleRowStart (desiredRowStart, out int rowStart))
                {
                    SelectedCodePoint = rowStart;
                }
            }

            SetNeedsDraw ();
        }
    }

    private void CopyCodePoint () => App?.Clipboard?.SetClipboardData ($"U+{SelectedCodePoint:x5}");
    private void CopyGlyph () => App?.Clipboard?.SetClipboardData ($"{new Rune (SelectedCodePoint)}");

    private bool? Move (ICommandContext? commandContext, int cpOffset)
    {
        if (RaiseActivating (commandContext) is true)
        {
            return true;
        }

        SelectedCodePoint += cpOffset;

        return true;
    }

    private void PaddingOnGettingAttributeForRole (object? sender, VisualRoleEventArgs e)
    {
        if (e.Role != VisualRole.Focus && e.Role != VisualRole.Active)
        {
            e.Result = GetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);
        }

        e.Handled = true;
    }

    private void ScrollToMakeCursorVisible (Point offsetToNewCursor)
    {
        // Adjust vertical scrolling
        if (offsetToNewCursor.Y < 1) // Header is at Y = 0
        {
            ScrollVertical (offsetToNewCursor.Y - HEADER_HEIGHT);
        }
        else if (offsetToNewCursor.Y >= Viewport.Height)
        {
            ScrollVertical (offsetToNewCursor.Y - Viewport.Height + HEADER_HEIGHT);
        }

        // Adjust horizontal scrolling
        if (offsetToNewCursor.X < RowLabelWidth + 1)
        {
            ScrollHorizontal (offsetToNewCursor.X - (RowLabelWidth + 1));
        }
        else if (offsetToNewCursor.X >= Viewport.Width)
        {
            ScrollHorizontal (offsetToNewCursor.X - Viewport.Width + 1);
        }
    }

    #region Details Dialog

    private void ShowDetails ()
    {
        if (App is not { Initialized: true })
        {
            // Some unit tests invoke Accept without Init
            return;
        }

        UcdApiClient client = new ();
        var decResponse = string.Empty;
        var getCodePointError = string.Empty;

        Dialog waitIndicator = new () { Title = Strings.charMapCPInfoDlgTitle, Buttons = [new Button { Text = Strings.btnCancel }] };

        Label errorLabel = new () { Text = UcdApiClient.BaseUrl, X = 0, Y = 0, TextAlignment = Alignment.Center };

        SpinnerView spinner = new () { X = Pos.Center (), Y = Pos.Bottom (errorLabel), Style = new SpinnerStyle.Aesthetic () };
        spinner.AutoSpin = true;
        waitIndicator.Add (errorLabel);
        waitIndicator.Add (spinner);

        waitIndicator.IsModalChanged += async (s, a) =>
                                        {
                                            if (!a.Value)
                                            {
                                                return;
                                            }

                                            try
                                            {
                                                decResponse = await client.GetCodepointDec (SelectedCodePoint).ConfigureAwait (false);
                                                App?.Invoke (_ => (s as Dialog)?.RequestStop ());
                                            }
                                            catch (HttpRequestException e)
                                            {
                                                getCodePointError = errorLabel.Text = e.Message;
                                                App?.Invoke (_ => (s as Dialog)?.RequestStop ());
                                            }
                                        };
        App?.Run (waitIndicator);
        waitIndicator.Dispose ();

        var name = string.Empty;

        if (!string.IsNullOrEmpty (decResponse))
        {
            using JsonDocument document = JsonDocument.Parse (decResponse);

            JsonElement root = document.RootElement;

            // Get a property by name and output its value
            if (root.TryGetProperty ("name", out JsonElement nameElement))
            {
                name = nameElement.GetString ();
            }

            decResponse = JsonSerializer.Serialize (document.RootElement, CharMapJsonContext.Default.JsonElement);
        }
        else
        {
            decResponse = getCodePointError;
        }

        var title = $"{ToCamelCase (name!)} - {new Rune (SelectedCodePoint)} U+{SelectedCodePoint:x5}";

        Button copyGlyph = new () { Text = Strings.charMapCopyGlyph };
        Button copyCodepoint = new () { Text = Strings.charMapCopyCP };
        Button cancel = new () { Text = Strings.btnCancel };

        using Dialog dlg = new ();
        dlg.Buttons = [copyGlyph, copyCodepoint, cancel];
        dlg.Title = title;

        var rune = (Rune)SelectedCodePoint;
        Label label = new () { Text = "IsAscii: ", X = 0, Y = 0 };
        dlg.Add (label);

        label = new Label { Text = $"{rune.IsAscii}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label { Text = ", Bmp: ", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label { Text = $"{rune.IsBmp}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label { Text = ", CombiningMark: ", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label { Text = $"{rune.IsCombiningMark ()}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label { Text = ", SurrogatePair: ", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label { Text = $"{rune.IsSurrogatePair ()}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label { Text = ", Plane: ", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label { Text = $"{rune.Plane}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label { Text = "Columns: ", X = 0, Y = Pos.Bottom (label) };
        dlg.Add (label);

        label = new Label { Text = $"{rune.GetColumns ()}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label { Text = ", Utf16SequenceLength: ", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label { Text = $"{rune.Utf16SequenceLength}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label { Text = "Category: ", X = 0, Y = Pos.Bottom (label) };
        dlg.Add (label);
        Span<char> utf16 = stackalloc char [2];

        // Get the bidi class for the first code unit
        // For most bidi characters, the first code unit is sufficient
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory (utf16 [0]);

        label = new Label { Text = $"{category}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new Label
        {
            Text = $"{Strings.charMapInfoDlgInfoLabel} {UcdApiClient.BaseUrl}codepoint/dec/{SelectedCodePoint}:", X = 0, Y = Pos.Bottom (label)
        };
        dlg.Add (label);

        Code json = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = Dim.Fill (0, 60),
            Height = Dim.Fill (0, 5),
            Language = "json",
            Text = decResponse
        };

        dlg.Add (json);

        var result = App?.Run (dlg) as int?;

        switch (result!)
        {
            case 0:
                CopyGlyph ();

                break;

            case 1:
                CopyCodePoint ();

                break;
        }
    }

    #endregion Details Dialog

    #region Cursor

    private Point GetCursor (int codePoint)
    {
        // + 1 for padding between label and first column
        int x = codePoint % 16 * COLUMN_WIDTH + RowLabelWidth + 1 - Viewport.X;

        int visibleRowIndex = VisibleRowIndexForCodePoint (codePoint);

        if (visibleRowIndex < 0)
        {
            // If filtered out, stick to current Y to avoid jumping; caller will clamp
            int fallbackY = HEADER_HEIGHT - Viewport.Y;

            return new Point (x, fallbackY);
        }

        int y = visibleRowIndex * _rowHeight + HEADER_HEIGHT - Viewport.Y;

        return new Point (x, y);
    }

    /// <summary>Updates the cursor position based on the selected code point.</summary>
    /// <remarks>
    ///     This method calculates the cursor position and calls <see cref="View.SetCursor"/>.
    ///     The framework automatically handles hiding the cursor when the view loses focus.
    /// </remarks>
    private void UpdateCursor ()
    {
        Point cursor = GetCursor (SelectedCodePoint);

        if (cursor.X >= RowLabelWidth && cursor.X < Viewport.Width && cursor.Y > 0 && cursor.Y < Viewport.Height)
        {
            // Convert to Screen coordinates
            Cursor = Cursor with { Position = ViewportToScreen (cursor) };
        }
        else
        {
            // Cursor is scrolled out of view
            Cursor = Cursor with { Position = null };
        }
    }

    #endregion Cursor

    #region Drawing

    private static int RowLabelWidth => $"U+{MAX_CODE_POINT:x5}".Length + 1;

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (Viewport.Height == 0 || Viewport.Width == 0)
        {
            return true;
        }

        int selectedCol = SelectedCodePoint % 16;
        int selectedRowIndex = VisibleRowIndexForCodePoint (SelectedCodePoint);

        // Headers

        // Clear the header area
        Move (0, 0);
        SetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);
        AddStr (new string (' ', Viewport.Width));

        int firstColumnX = RowLabelWidth - Viewport.X;

        // Header
        int x;

        for (var hexDigit = 0; hexDigit < 16; hexDigit++)
        {
            x = firstColumnX + hexDigit * COLUMN_WIDTH;

            if (x <= RowLabelWidth - 2)
            {
                continue;
            }
            Move (x, 0);
            SetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);
            AddStr (" ");

            // Swap Active/Focus so the selected column is highlighted
            if (hexDigit == selectedCol)

            {
                SetAttributeForRole (HasFocus ? VisualRole.Active : VisualRole.Focus);
            }

            AddStr ($"{hexDigit:x}");
            SetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);
            AddStr (" ");
        }

        // Start at 1 because Header.
        for (var y = 1; y < Viewport.Height; y++)
        {
            // Which visible row is this?
            int visibleRow = (y + Viewport.Y - 1) / _rowHeight;

            if (!TryGetVisibleRowStart (visibleRow, out int rowStart))
            {
                // No row at this y; clear label area and continue
                Move (0, y);
                AddStr (new string (' ', Viewport.Width));

                continue;
            }

            // Draw the row label (U+XXXX_)
            SetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);
            Move (0, y);

            // Swap Active/Focus so the selected row is highlighted
            if (visibleRow == selectedRowIndex)
            {
                SetAttributeForRole (HasFocus ? VisualRole.Active : VisualRole.Focus);
            }

            if (!ShowGlyphWidths || (y + Viewport.Y) % _rowHeight > 0)
            {
                AddStr ($"U+{rowStart / 16:x5}_");
            }
            else
            {
                AddStr (new string (' ', RowLabelWidth));
            }

            // Draw the row
            SetAttributeForRole (VisualRole.Normal);

            for (var col = 0; col < 16; col++)
            {
                x = firstColumnX + COLUMN_WIDTH * col + 1;

                if (x < RowLabelWidth || x > Viewport.Width - 1)
                {
                    continue;
                }

                Move (x, y);

                // If we're at the cursor position highlight the cell
                if (visibleRow == selectedRowIndex && col == selectedCol)
                {
                    SetAttributeForRole (VisualRole.Active);
                }

                int scalar = rowStart + col;

                // Don't render out-of-range scalars
                if (scalar > MAX_CODE_POINT)
                {
                    AddStr (" ");

                    if (visibleRow == selectedRowIndex && col == selectedCol)
                    {
                        SetAttributeForRole (VisualRole.Normal);
                    }

                    continue;
                }

                var grapheme = "?";

                if (Rune.IsValid (scalar))
                {
                    grapheme = new Rune (scalar).ToString ();
                }

                int width = grapheme.GetColumns ();

                // Compute visibility based on ShowUnicodeCategory
                bool isVisible = Rune.IsValid (scalar);

                if (isVisible && ShowUnicodeCategory.HasValue)
                {
                    UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory (scalar);
                    isVisible = cat == ShowUnicodeCategory.Value;
                }

                if (!ShowGlyphWidths || (y + Viewport.Y) % _rowHeight > 0)
                {
                    // Glyph row
                    if (isVisible)
                    {
                        RenderGrapheme (grapheme, width, scalar);
                    }
                    else
                    {
                        AddStr (" ");
                    }
                }
                else
                {
                    // Width row (ShowGlyphWidths)
                    if (isVisible)
                    {
                        // Draw the width of the rune faint
                        Attribute attr = GetAttributeForRole (VisualRole.Normal);
                        SetAttribute (attr with { Style = attr.Style | TextStyle.Faint });
                        AddStr ($"{width}");
                    }
                    else
                    {
                        AddStr (" ");
                    }
                }

                // If we're at the cursor position, and we don't have focus
                if (visibleRow == selectedRowIndex && col == selectedCol)
                {
                    SetAttributeForRole (VisualRole.Normal);
                }
            }
        }

        return true;

        void RenderGrapheme (string grapheme, int width, int scalar)
        {
            // Get the UnicodeCategory
            // Get the bidi class for the first code unit
            // For most bidi characters, the first code unit is sufficient
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory (scalar);

            switch (category)
            {
                case UnicodeCategory.OtherNotAssigned:
                    SetAttributeForRole (VisualRole.Highlight);
                    AddStr (Rune.ReplacementChar.ToString ());
                    SetAttributeForRole (VisualRole.Normal);

                    break;

                // Format character that affects the layout of text or the operation of text processes, but is not normally rendered.
                // These report width of 0 and don't render on their own.
                case UnicodeCategory.Format:
                    SetAttributeForRole (VisualRole.Highlight);
                    AddStr ("F");
                    SetAttributeForRole (VisualRole.Normal);

                    break;

                // Nonspacing character that indicates modifications of a base character.
                case UnicodeCategory.NonSpacingMark:
                // Spacing character that indicates modifications of a base character and affects the width of the glyph for that base character.
                case UnicodeCategory.SpacingCombiningMark:
                // Enclosing mark character, which is a nonspacing combining character that surrounds all previous characters up to and including a base character.
                case UnicodeCategory.EnclosingMark:
                    if (width > 0)
                    {
                        AddStr (grapheme);
                    }

                    break;

                // These report width of 0, but render as 1
                case UnicodeCategory.Control:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.Surrogate:
                    AddStr (grapheme);

                    break;

                // Modifier symbol (e.g. emoji skin tone modifiers U+1F3FB-U+1F3FF) that reports
                // width of 0 because it is designed to combine with a preceding base character.
                case UnicodeCategory.ModifierSymbol:
                    if (width > 0)
                    {
                        AddStr (grapheme);
                    }
                    else
                    {
                        SetAttributeForRole (VisualRole.Highlight);
                        AddStr ("M");
                        SetAttributeForRole (VisualRole.Normal);
                    }

                    break;

                case UnicodeCategory.OtherLetter:
                    AddStr (grapheme);

                    if (width == 0)
                    {
                        AddStr (" ");
                    }

                    break;

                default:

                    // Draw the rune
                    if (width > 0)
                    {
                        AddStr (grapheme);
                    }
                    else
                    {
                        // Fallback for any zero-width rune whose UnicodeCategory is not explicitly handled above.
                        // Display a highlighted placeholder to avoid crashes for future Unicode categories.
                        SetAttributeForRole (VisualRole.Highlight);
                        AddStr ("?");
                        SetAttributeForRole (VisualRole.Normal);
                    }

                    break;
            }
        }
    }

    /// <summary>
    ///     Helper to convert a string into camel case.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToCamelCase (string str)
    {
        if (string.IsNullOrEmpty (str))
        {
            return str;
        }

        TextInfo textInfo = new CultureInfo ("en-US", false).TextInfo;

        str = textInfo.ToLower (str);
        str = textInfo.ToTitleCase (str);

        return str;
    }

    #endregion Drawing

    #region Input Handling

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? commandContext)
    {
        base.OnActivated (commandContext);

        Point position = GetCursor (SelectedCodePoint);

        if (commandContext?.Binding is MouseBinding { MouseEvent: { } mouse })
        {
            // If the mouse is clicked on the headers, map it to the first glyph of the row/col
            position = mouse.Position!.Value;

            if (position.Y == 0)
            {
                position = position with { Y = GetCursor (SelectedCodePoint).Y };
            }

            if (position.X < RowLabelWidth || position.X > RowLabelWidth + 16 * COLUMN_WIDTH - 1)
            {
                position = position with { X = GetCursor (SelectedCodePoint).X };
            }
        }

        if (!TryGetCodePointFromPosition (position, out int cp))
        {
            return;
        }

        if (cp == SelectedCodePoint)
        {
            return;
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        SelectedCodePoint = cp;
    }

    /// <inheritdoc/>
    protected override void OnAccepted (ICommandContext? ctx)
    {
        base.OnAccepted (ctx);

        if (ctx?.Binding is MouseBinding { MouseEvent: { } mouse })
        {
            if (!HasFocus && CanFocus)
            {
                SetFocus ();
            }

            if (!TryGetCodePointFromPosition (mouse.Position!.Value, out int cp))
            {
                return;
            }

            SelectedCodePoint = cp;
        }

        ShowDetails ();
    }

    private bool? HandleContextCommand (ICommandContext? commandContext)
    {
        int newCodePoint = SelectedCodePoint;

        if (commandContext?.Binding is MouseBinding { MouseEvent: { } mouse })
        {
            if (!TryGetCodePointFromPosition (mouse.Position!.Value, out newCodePoint))
            {
                return false;
            }
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        SelectedCodePoint = newCodePoint;

        // This demonstrates how to create an ephemeral Popover; one that exists
        // ony as long as the popover is visible.
        // Note, for ephemeral Popovers, hotkeys are not supported.
        PopoverMenu contextMenu = new ([
                                           new MenuItem (Strings.charMapCopyGlyph, string.Empty, CopyGlyph),
                                           new MenuItem (Strings.charMapCopyCP, string.Empty, CopyCodePoint)
                                       ]);

        // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
        // and the context menu is disposed when it is closed.
        App!.Popovers?.Register (contextMenu);

        contextMenu.MakeVisible (ViewportToScreen (GetCursor (SelectedCodePoint)));

        return true;
    }

    private bool TryGetCodePointFromPosition (Point position, out int codePoint)
    {
        if (position.X < RowLabelWidth || position.Y < 1)
        {
            codePoint = 0;

            return false;
        }

        int visibleRow = (position.Y - 1 - -Viewport.Y) / _rowHeight;

        if (!TryGetVisibleRowStart (visibleRow, out int rowStart))
        {
            codePoint = 0;

            return false;
        }

        int col = (position.X - RowLabelWidth - -Viewport.X) / COLUMN_WIDTH;

        if (col > 15)
        {
            col = 15;
        }

        codePoint = rowStart + col;

        if (codePoint > MAX_CODE_POINT)
        {
            return false;
        }

        return true;
    }

    #endregion Mouse Handling

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:300," + string.Join (",", Enumerable.Repeat ("PageDown,wait:100", 40)) + ",wait:500,Shift+F10,wait:1500";
}

/// <summary>
///     Source generation context for AOT-compatible JSON serialization in CharMap.
/// </summary>
[JsonSerializable (typeof (JsonElement))]
[JsonSourceGenerationOptions (WriteIndented = true)]
internal partial class CharMapJsonContext : JsonSerializerContext;

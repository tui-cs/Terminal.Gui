//
// HexView.cs: A hexadecimal viewer
//
// TODO: Support searching and highlighting of the search result
// TODO: Support shrinking the stream (e.g. del/backspace should work).
// 

using System.Buffers;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides a hex editor with the left side
///     showing the hex values of the bytes in a `Stream` and the right side showing the contents
///     (filtered
///     to printable Unicode glyphs).
/// </summary>
/// <remarks>
/// <img src="../images/views/HexView.gif" alt="HexView demo"/>
///     <para>Users can switch from one side to the other by using the tab key.</para>
///     <para>
///         To enable editing, set <see cref="ReadOnly"/> to true. When <see cref="ReadOnly"/> is true the user can
///         make changes to the hexadecimal values of the <see cref="Stream"/>. Any changes are tracked in the
///         <see cref="Edits"/> property (a <see cref="SortedDictionary{TKey, TValue}"/>) indicating the position where the
///         changes were made and the new values. A convenience method, <see cref="ApplyEdits"/> will apply the edits to
///         the <see cref="Stream"/>.
///     </para>
///     <para>
///         Control the byte at the caret for editing by setting the <see cref="Address"/> property to an offset in the
///         stream.
///     </para>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Left / Right</term> <description>Moves the cursor one nibble/byte.</description>
///         </item>
///         <item>
///             <term>Up / Down</term> <description>Moves the cursor one row up or down.</description>
///         </item>
///         <item>
///             <term>PageUp / PageDown</term> <description>Moves one page up or down.</description>
///         </item>
///         <item>
///             <term>Home / End</term> <description>Moves to the first or last byte in the stream.</description>
///         </item>
///         <item>
///             <term>Ctrl+Left / Ctrl+Right</term> <description>Moves to the start or end of the current row.</description>
///         </item>
///         <item>
///             <term>Ctrl+Up / Ctrl+Down</term> <description>Moves to the start or end of the current page.</description>
///         </item>
///         <item>
///             <term>Backspace</term> <description>Deletes the byte before the cursor.</description>
///         </item>
///         <item>
///             <term>Delete</term> <description>Deletes the byte at the cursor.</description>
///         </item>
///         <item>
///             <term>Insert</term> <description>Toggles insert mode.</description>
///         </item>
///     </list>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Click / Double-Click</term> <description>Positions the cursor at the clicked byte.</description>
///         </item>
///         <item>
///             <term>Wheel Up / Down</term> <description>Scrolls the view.</description>
///         </item>
///     </list>
/// </remarks>
public class HexView : View, IDesignable
{
    /// <summary>
    ///     Gets or sets the default cursor style.
    /// </summary>
    public static CursorStyle DefaultCursorStyle => HexViewSettings.Current.DefaultCursorStyle;

    /// <summary>
    ///     Gets or sets the view-specific default key bindings for <see cref="HexView"/>. Contains only bindings
    ///     unique to this view; shared bindings come from <see cref="View.DefaultKeyBindings"/>.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
    {
        // HexView maps Home/End to stream start/end (overrides base layer's LeftStart/RightEnd)
        [Command.Start] = Bind.All (Key.Home),
        [Command.End] = Bind.All (Key.End),

        // Row start/end via Ctrl+Left/Right
        [Command.LeftStart] = Bind.All (Key.CursorLeft.WithCtrl),
        [Command.RightEnd] = Bind.All (Key.CursorRight.WithCtrl),

        [Command.StartOfPage] = Bind.All (Key.CursorUp.WithCtrl),
        [Command.EndOfPage] = Bind.All (Key.CursorDown.WithCtrl),
        [Command.Insert] = Bind.All (Key.InsertChar),
    };

    private const int DEFAULT_ADDRESS_WIDTH = 8; // The default value for AddressWidth
    private const int NUM_BYTES_PER_HEX_COLUMN = 4;
    private const int HEX_COLUMN_WIDTH = NUM_BYTES_PER_HEX_COLUMN * 3 + 2; // 3 cols per byte + 1 for vert separator + right space

    private bool _firstNibble;
    private bool _leftSideHasFocus;
    private static readonly Rune _spaceCharRune = new (' ');
    private static readonly Rune _periodCharRune = Glyphs.DottedSquare;
    private static readonly Rune _columnSeparatorRune = Glyphs.VLineDa4;

    /// <summary>Initializes a <see cref="HexView"/> class.</summary>
    /// <param name="source">
    ///     The <see cref="Stream"/> to view and edit as hex, this <see cref="Stream"/> must support seeking,
    ///     or an exception will be thrown.
    /// </param>
    public HexView (Stream? source)
    {
        Source = source;

        CanFocus = true;
        _leftSideHasFocus = true;
        _firstNibble = true;

        AddCommand (Command.Left, () => MoveLeft ());
        AddCommand (Command.Right, () => MoveRight ());
        AddCommand (Command.Down, () => MoveDown (BytesPerLine));
        AddCommand (Command.Up, () => MoveUp (BytesPerLine));
        AddCommand (Command.PageUp, () => MoveUp (BytesPerLine * Viewport.Height));
        AddCommand (Command.PageDown, () => MoveDown (BytesPerLine * Viewport.Height));
        AddCommand (Command.Start, () => MoveHome ());
        AddCommand (Command.End, () => MoveEnd ());
        AddCommand (Command.LeftStart, () => MoveLeftStart ());
        AddCommand (Command.RightEnd, () => MoveEndOfLine ());
        AddCommand (Command.StartOfPage, () => MoveUp (BytesPerLine * ((int)(Address - Viewport.Y) / BytesPerLine)));

        AddCommand (Command.EndOfPage, () => MoveDown (BytesPerLine * (Viewport.Height - 1 - (int)(Address - Viewport.Y) / BytesPerLine)));
        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.DeleteCharLeft, () => true);
        AddCommand (Command.DeleteCharRight, () => true);
        AddCommand (Command.Insert, () => true);

        ApplyKeyBindings (DefaultKeyBindings, View.DefaultKeyBindings);

        KeyBindings.Remove (Key.Space);
        KeyBindings.Remove (Key.Enter);

        // The Activate handler deals with both single and double clicks
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Activate);
        MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Activate);
        MouseBindings.Add (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.Add (MouseFlags.WheeledDown, Command.ScrollDown);

        SubViewsLaidOut += HexViewSubViewsLaidOut;

        Cursor = new Cursor { Style = DefaultCursorStyle };
    }

    private void HexViewSubViewsLaidOut (object? sender, LayoutEventArgs e)
    {
        SetBytesPerLine ();

        SetContentSize (new Size (GetLeftSideStartColumn () + BytesPerLine / NUM_BYTES_PER_HEX_COLUMN * HEX_COLUMN_WIDTH + BytesPerLine - 1,
                                  (int)(GetEditedSize () / BytesPerLine) + 1));
    }

    /// <summary>Initializes a <see cref="HexView"/> class.</summary>
    public HexView () : this (new MemoryStream ()) { }

    /// <summary>
    ///     Gets or sets whether this <see cref="HexView"/> allows editing of the <see cref="Stream"/> of the underlying
    ///     <see cref="Stream"/>. The default is <see langword="false"/>.
    /// </summary>
    public bool ReadOnly { get; set; } = false;

    /// <summary>Gets the current edit position.</summary>
    /// <param name="address"></param>
    public Point GetPosition (long address)
    {
        if (_source is null || BytesPerLine == 0)
        {
            return Point.Empty;
        }

        long line = address / BytesPerLine;
        long item = address % BytesPerLine;

        return new Point ((int)item, (int)line);
    }

    /// <summary>Gets cursor location, given an address.</summary>
    /// <param name="address"></param>
    public Point GetCursor (long address)
    {
        Point position = GetPosition (address);

        if (_leftSideHasFocus)
        {
            int block = position.X / NUM_BYTES_PER_HEX_COLUMN;
            int column = position.X % NUM_BYTES_PER_HEX_COLUMN;

            position.X = block * HEX_COLUMN_WIDTH + column * 3 + (_firstNibble ? 0 : 1);
        }
        else
        {
            position.X += BytesPerLine / NUM_BYTES_PER_HEX_COLUMN * HEX_COLUMN_WIDTH - 1;
        }

        position.X += GetLeftSideStartColumn ();

        position.Offset (-Viewport.X, -Viewport.Y);

        return position;
    }

    private void ScrollToMakeCursorVisible (Point offsetToNewCursor)
    {
        // Adjust vertical scrolling
        if (offsetToNewCursor.Y < 1)
        {
            ScrollVertical (offsetToNewCursor.Y);
        }
        else if (offsetToNewCursor.Y >= Viewport.Height)
        {
            ScrollVertical (offsetToNewCursor.Y);
        }

        if (offsetToNewCursor.X < 1)
        {
            ScrollHorizontal (offsetToNewCursor.X);
        }
        else if (offsetToNewCursor.X >= Viewport.Width)
        {
            ScrollHorizontal (offsetToNewCursor.X);
        }
    }

    private SortedDictionary<long, byte> _edits = [];

    /// <summary>
    ///     Gets a <see cref="SortedDictionary{TKey, TValue}"/> describing the edits done to the <see cref="HexView"/>.
    ///     Each Key indicates an offset where an edit was made and the Value is the changed byte.
    /// </summary>
    /// <value>The edits.</value>
    public IReadOnlyDictionary<long, byte> Edits => _edits;

    private long GetEditedSize ()
    {
        if (_edits.Count == 0)
        {
            return _source!.Length;
        }

        long maxEditAddress = _edits.Keys.Max ();

        return Math.Max (_source!.Length, maxEditAddress + 1);
    }

    /// <summary>
    ///     Applies and edits made to the <see cref="Stream"/> and resets the contents of the
    ///     <see cref="Edits"/> property.
    /// </summary>
    /// <param name="stream">If provided also applies the changes to the passed <see cref="Stream"/>.</param>
    /// .
    public void ApplyEdits (Stream? stream = null)
    {
        foreach (KeyValuePair<long, byte> kv in _edits)
        {
            _source!.Position = kv.Key;
            _source.WriteByte (kv.Value);
            _source.Flush ();

            if (stream is null)
            {
                continue;
            }
            stream.Position = kv.Key;
            stream.WriteByte (kv.Value);
            stream.Flush ();
        }

        _edits = new SortedDictionary<long, byte> ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Discards the edits made to the <see cref="Stream"/> by resetting the contents of the
    ///     <see cref="Edits"/> property.
    /// </summary>
    public void DiscardEdits () => _edits = new SortedDictionary<long, byte> ();

    private Stream? _source;

    /// <summary>
    ///     Sets or gets the <see cref="Stream"/> the <see cref="HexView"/> is operating on; the stream must support
    ///     seeking ( <see cref="Stream.CanSeek"/> == true).
    /// </summary>
    /// <value>The source.</value>
    public Stream? Source
    {
        get => _source;
        set
        {
            ArgumentNullException.ThrowIfNull (value);

            if (!value.CanSeek)
            {
                throw new ArgumentException (@"The source stream must be seekable (CanSeek property)");
            }

            DiscardEdits ();
            _source = value;
            SetBytesPerLine ();

            if (Address > _source.Length)
            {
                Address = 0;
            }

            SetNeedsLayout ();
            SetNeedsDraw ();
        }
    }

    /// <summary>The bytes length per line.</summary>
    public int BytesPerLine
    {
        get;
        set
        {
            field = value;
            RaisePositionChanged ();
        }
    }

    /// <summary>Gets or sets the current byte position in the <see cref="Stream"/>.</summary>
    public long Address
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            long newAddress = Math.Clamp (value, 0, GetEditedSize ());

            Point offsetToNewCursor = GetCursor (newAddress);

            field = newAddress;

            // Ensure the new cursor position is visible
            ScrollToMakeCursorVisible (offsetToNewCursor);
            RaisePositionChanged ();
        }
    }

    /// <summary>
    ///     Gets or sets the width of the Address column on the left. Set to 0 to hide. The default is 8.
    /// </summary>
    public int AddressWidth
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            SetNeedsDraw ();
            SetNeedsLayout ();
        }
    } = DEFAULT_ADDRESS_WIDTH;

    private int GetLeftSideStartColumn () => AddressWidth == 0 ? 0 : AddressWidth + 1;

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        if (args.Context?.Binding is not MouseBinding { MouseEvent: { } mouse })
        {
            return base.OnActivating (args);
        }

        if (!HasFocus)
        {
            SetFocus ();
        }

        if (mouse.Position!.Value.X < GetLeftSideStartColumn ())
        {
            args.Handled = true;

            return true;
        }

        int blocks = BytesPerLine / NUM_BYTES_PER_HEX_COLUMN;
        int blocksSize = blocks * HEX_COLUMN_WIDTH;
        int blocksRightOffset = GetLeftSideStartColumn () + blocksSize - 1;

        if (mouse.Position!.Value.X > blocksRightOffset + BytesPerLine - 1)
        {
            args.Handled = true;

            return true;
        }

        bool clickIsOnLeftSide = mouse.Position!.Value.X >= blocksRightOffset;
        long lineStart = mouse.Position!.Value.Y * BytesPerLine + Viewport.Y * BytesPerLine;
        int x = mouse.Position!.Value.X - GetLeftSideStartColumn () + 1;
        int block = x / HEX_COLUMN_WIDTH;
        x -= block * 2;
        int empty = x % 3;
        int item = x / 3;

        if (!clickIsOnLeftSide && item > 0 && (empty == 0 || x == block * HEX_COLUMN_WIDTH + HEX_COLUMN_WIDTH - 1 - block * 2))
        {
            args.Handled = true;

            return true;
        }

        _firstNibble = true;

        if (clickIsOnLeftSide)
        {
            Address = Math.Min (lineStart + mouse.Position!.Value.X - blocksRightOffset, GetEditedSize ());
        }
        else
        {
            Address = Math.Min (lineStart + item, GetEditedSize ());
        }

        if (mouse.Flags == MouseFlags.LeftButtonDoubleClicked)
        {
            _leftSideHasFocus = !clickIsOnLeftSide;

            if (_leftSideHasFocus)
            {
                _firstNibble = empty == 1;
            }
            else
            {
                _firstNibble = true;
            }

            SetNeedsDraw ();
            UpdateCursor ();
        }

        args.Handled = true;

        return true;
    }

    ///<inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (Source is null)
        {
            return true;
        }

        long addressOfFirstLine = Viewport.Y * BytesPerLine;

        int nBlocks = BytesPerLine / NUM_BYTES_PER_HEX_COLUMN;
        var data = new byte [nBlocks * NUM_BYTES_PER_HEX_COLUMN * Viewport.Height];
        Source.Position = addressOfFirstLine;
        long bytesRead = Source!.Read (data, 0, data.Length);

        Attribute selectedAttribute = GetAttributeForRole (VisualRole.Active);

        Attribute editedAttribute = GetAttributeForRole (VisualRole.Editable);
        editedAttribute = editedAttribute with { Style = editedAttribute.Style | TextStyle.Italic | TextStyle.Underline };
        Attribute editingAttribute = GetAttributeForRole (ReadOnly ? VisualRole.ReadOnly : VisualRole.Normal);
        Attribute addressAttribute = GetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);

        for (var line = 0; line < Viewport.Height; line++)
        {
            int max = -Viewport.X;

            Move (max, line);
            long addressOfLine = addressOfFirstLine + line * nBlocks * NUM_BYTES_PER_HEX_COLUMN;

            if (addressOfLine <= GetEditedSize ())
            {
                SetAttribute (addressAttribute);
            }
            else
            {
                SetAttributeForRole (VisualRole.Disabled);
            }

            var address = $"{addressOfLine:x8}";
            AddStr ($"{address [(8 - AddressWidth)..]}");

            SetAttribute (editingAttribute);

            if (AddressWidth > 0)
            {
                AddStr (" ");
            }

            for (var block = 0; block < nBlocks; block++)
            {
                for (var b = 0; b < NUM_BYTES_PER_HEX_COLUMN; b++)
                {
                    int offset = line * nBlocks * NUM_BYTES_PER_HEX_COLUMN + block * NUM_BYTES_PER_HEX_COLUMN + b;
                    byte value = GetData (data, offset, out bool edited);

                    if (offset + addressOfFirstLine == Address)
                    {
                        // Selected
                        SetAttribute (_leftSideHasFocus ? editingAttribute : selectedAttribute);
                    }
                    else
                    {
                        SetAttribute (edited ? editedAttribute : editingAttribute);
                    }

                    AddStr (offset >= bytesRead && !edited ? "  " : $"{value:x2}");
                    SetAttribute (editingAttribute);
                    AddRune (_spaceCharRune);
                }

                AddStr (block + 1 == nBlocks ? " " : $"{_columnSeparatorRune} ");
            }

            for (var byteIndex = 0; byteIndex < nBlocks * NUM_BYTES_PER_HEX_COLUMN; byteIndex++)
            {
                int offset = line * nBlocks * NUM_BYTES_PER_HEX_COLUMN + byteIndex;
                byte b = GetData (data, offset, out bool edited);
                Rune c;

                var utf8BytesConsumed = 0;

                if (offset >= bytesRead && !edited)
                {
                    c = _spaceCharRune;
                }
                else
                {
                    switch (b)
                    {
                        //case < 32:
                        //    c = _periodCharRune;

                        //    break;
                        case > 127:
                            {
                                byte [] utf8 = GetData (data, offset, 4, out bool _);

                                OperationStatus status = Rune.DecodeFromUtf8 (utf8, out c, out utf8BytesConsumed);

                                while (status == OperationStatus.NeedMoreData)
                                {
                                    status = Rune.DecodeFromUtf8 (utf8, out c, out utf8BytesConsumed);
                                }

                                break;
                            }

                        default:
                            Rune.DecodeFromUtf8 (new ReadOnlySpan<byte> (ref b), out c, out _);

                            break;
                    }
                }

                if (offset == Address)
                {
                    // Selected
                    SetAttribute (selectedAttribute);
                }
                else
                {
                    SetAttribute (edited ? editedAttribute : editingAttribute);
                }

                AddRune (c);

                for (var i = 1; i < utf8BytesConsumed; i++)
                {
                    byteIndex++;
                    AddRune (_periodCharRune);
                }
            }

            SetAttribute (editingAttribute);

            // Fill rest of line
            for (int x = max; x < Viewport.Width; x++)
            {
                AddRune (new Rune (' '));
            }
        }

        return true;
    }

    /// <summary>Raises the <see cref="Edited"/> event.</summary>
    protected void RaiseEdited (HexViewEditEventArgs e)
    {
        OnEdited (e);
        Edited?.Invoke (this, e);
    }

    /// <summary>Event to be invoked when an edit is made on the <see cref="Stream"/>.</summary>
    public event EventHandler<HexViewEditEventArgs>? Edited;

    /// <summary>
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnEdited (HexViewEditEventArgs e) { }

    /// <summary>
    ///     Call this when the position (see <see cref="GetPosition"/>) and <see cref="Address"/> have changed. Raises the
    ///     <see cref="PositionChanged"/> event.
    /// </summary>
    protected void RaisePositionChanged ()
    {
        UpdateCursor ();

        HexViewEventArgs args = new (Address, GetPosition (Address), BytesPerLine);
        OnPositionChanged (args);
        PositionChanged?.Invoke (this, args);
    }

    private void UpdateCursor () => Cursor = Cursor with { Position = ViewportToScreen (GetCursor (Address)) };

    /// <summary>
    ///     Called when the position (see <see cref="GetPosition"/>) and <see cref="Address"/> have changed.
    /// </summary>
    protected virtual void OnPositionChanged (HexViewEventArgs e) { }

    /// <summary>Raised when the position (see <see cref="GetPosition"/>) and <see cref="Address"/> have changed.</summary>
    public event EventHandler<HexViewEventArgs>? PositionChanged;

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key keyEvent)
    {
        if (ReadOnly || _source is null)
        {
            return false;
        }

        if (keyEvent.IsAlt)
        {
            return false;
        }

        if (keyEvent.IsCtrl)
        {
            return false;
        }

        if (_leftSideHasFocus)
        {
            int value;
            var k = (char)keyEvent.KeyCode;

            if (!char.IsAsciiHexDigit ((char)keyEvent.KeyCode))
            {
                return false;
            }

            if (k is >= 'A' and <= 'F')
            {
                value = k - 'A' + 10;
            }
            else if (k is >= 'a' and <= 'f')
            {
                value = k - 'a' + 10;
            }
            else if (k is >= '0' and <= '9')
            {
                value = k - '0';
            }
            else
            {
                return false;
            }

            if (!_edits.TryGetValue (Address, out byte b))
            {
                _source.Position = Address;
                b = (byte)_source.ReadByte ();
            }

            if (_firstNibble)
            {
                _firstNibble = false;
                b = (byte)((b & 0xf) | (value << NUM_BYTES_PER_HEX_COLUMN));
                _edits [Address] = b;
                RaiseEdited (new HexViewEditEventArgs (Address, _edits [Address]));
            }
            else
            {
                b = (byte)((b & 0xf0) | value);
                _edits [Address] = b;
                RaiseEdited (new HexViewEditEventArgs (Address, _edits [Address]));
                MoveRight ();
            }

            return true;
        }

        keyEvent = keyEvent.NoAlt.NoCtrl;
        Rune r = keyEvent.AsRune;

        if (Rune.IsControl (r))
        {
            return false;
        }

        var utf8 = new byte [4];

        // If the rune is a wide char, encode as utf8
        if (r.TryEncodeToUtf8 (utf8, out int bytesWritten))
        {
            if (bytesWritten > 1)
            {
                bytesWritten = 4;
            }

            for (var utfIndex = 0; utfIndex < bytesWritten; utfIndex++)
            {
                _edits [Address] = utf8 [utfIndex];
                RaiseEdited (new HexViewEditEventArgs (Address, _edits [Address]));
                MoveRight ();
            }
        }
        else
        {
            _edits [Address] = (byte)r.Value;
            RaiseEdited (new HexViewEditEventArgs (Address, _edits [Address]));
            MoveRight ();
        }

        return true;
    }

    //
    // This is used to support editing of the buffer on a peer List<>,
    // the offset corresponds to an offset relative to DisplayStart, and
    // the buffer contains the contents of a Viewport of data, so the
    // offset is relative to the buffer.
    //
    // 
    private byte GetData (byte [] buffer, int offset, out bool edited)
    {
        long pos = Viewport.Y * BytesPerLine + offset;

        if (_edits.TryGetValue (pos, out byte v))
        {
            edited = true;

            return v;
        }

        edited = false;

        return buffer [offset];
    }

    private byte [] GetData (byte [] buffer, int offset, int count, out bool edited)
    {
        var returnBytes = new byte [count];
        edited = false;

        long pos = Viewport.Y + offset;

        for (long i = pos; i < pos + count; i++)
        {
            if (_edits.TryGetValue (i, out byte v))
            {
                edited = true;
                returnBytes [i - pos] = v;
            }
            else
            {
                if (pos < buffer.Length - 1)
                {
                    returnBytes [i - pos] = buffer [pos];
                }
            }
        }

        return returnBytes;
    }

    private void SetBytesPerLine ()
    {
        // Small buffers will just show the position, with the bsize field value (4 bytes)
        BytesPerLine = NUM_BYTES_PER_HEX_COLUMN;

        if (Viewport.Width - GetLeftSideStartColumn () >= HEX_COLUMN_WIDTH)
        {
            BytesPerLine = Math.Max (NUM_BYTES_PER_HEX_COLUMN,
                                     NUM_BYTES_PER_HEX_COLUMN * ((Viewport.Width - GetLeftSideStartColumn ()) / (HEX_COLUMN_WIDTH + NUM_BYTES_PER_HEX_COLUMN)));
        }
    }

    private bool MoveDown (int bytes)
    {
        if (Address + bytes < GetEditedSize ())
        {
            // We can move down lines cleanly (without extending stream)
            Address += bytes;
        }
        else if ((bytes == BytesPerLine * Viewport.Height && _source!.Length >= Viewport.Y * BytesPerLine + BytesPerLine * Viewport.Height)
                 || (bytes <= BytesPerLine * Viewport.Height - BytesPerLine && _source!.Length <= Viewport.Y * BytesPerLine + BytesPerLine * Viewport.Height))
        {
            long p = Address;

            // This lets address go past the end of the stream one, enabling adding to the stream.
            while (p + BytesPerLine <= GetEditedSize ())
            {
                p += BytesPerLine;
            }

            Address = p;
        }

        return true;
    }

    private bool MoveEnd ()
    {
        // This lets address go past the end of the stream one, enabling adding to the stream.
        Address = GetEditedSize ();

        return true;
    }

    private bool MoveEndOfLine ()
    {
        // This lets address go past the end of the stream one, enabling adding to the stream.
        Address = Math.Min (Address / BytesPerLine * BytesPerLine + BytesPerLine - 1, GetEditedSize ());

        return true;
    }

    private bool MoveHome ()
    {
        Address = 0;

        return true;
    }

    private bool MoveLeft ()
    {
        if (_leftSideHasFocus)
        {
            if (!_firstNibble)
            {
                _firstNibble = true;

                return true;
            }

            _firstNibble = false;
        }

        if (Address == 0)
        {
            return true;
        }

        Address--;

        return true;
    }

    private bool MoveRight ()
    {
        if (_leftSideHasFocus)
        {
            if (_firstNibble)
            {
                _firstNibble = false;

                return true;
            }

            _firstNibble = true;
        }

        // This lets address go past the end of the stream one, enabling adding to the stream.
        if (Address < GetEditedSize ())
        {
            Address++;
        }

        return true;
    }

    private bool MoveLeftStart ()
    {
        Address = Address / BytesPerLine * BytesPerLine;

        return true;
    }

    private bool MoveUp (int bytes)
    {
        Address -= bytes;

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnAdvancingFocus (NavigationDirection direction, TabBehavior? behavior)
    {
        if (behavior is { } && behavior != TabStop)
        {
            return false;
        }

        if ((direction != NavigationDirection.Forward || !_leftSideHasFocus) && (direction != NavigationDirection.Backward || _leftSideHasFocus))
        {
            return false;
        }
        _leftSideHasFocus = !_leftSideHasFocus;
        _firstNibble = true;
        SetNeedsDraw ();

        UpdateCursor ();

        return true;
    }

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:500,CursorRight,wait:300,CursorRight,wait:300,CursorDown,wait:300,CursorDown,wait:800";

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Source = new MemoryStream ("HexView data with wide codepoints: 𝔹Aℝ𝔽!"u8.ToArray ());

        return true;
    }
}

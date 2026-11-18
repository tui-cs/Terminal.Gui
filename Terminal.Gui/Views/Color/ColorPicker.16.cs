namespace Terminal.Gui.Views;

/// <summary>A simple color picker that supports the legacy 16 ANSI colors.</summary>
/// <remarks>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Left / Right</term> <description>Moves the selection left or right.</description>
///         </item>
///         <item>
///             <term>Up / Down</term> <description>Moves the selection up or down.</description>
///         </item>
///     </list>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Double-Click</term> <description>Accepts the selected color (<see cref="Command.Accept"/>).</description>
///         </item>
///     </list>
/// </remarks>
public class ColorPicker16 : View, IValue<ColorName16>
{
    /// <summary>Columns of color boxes</summary>
    private const int COLS = 8;

    /// <summary>Rows of color boxes</summary>
    private const int ROWS = 2;

    /// <summary>Initializes a new instance of <see cref="ColorPicker16"/>.</summary>
    public ColorPicker16 ()
    {
        CanFocus = true;
        AddCommands ();
        AddKeyBindings ();

        Width = Dim.Auto (minimumContentDim: _boxWidth * COLS);
        Height = Dim.Auto (minimumContentDim: _boxHeight * ROWS);
        SetContentSize (new Size (_boxWidth * COLS, _boxHeight * ROWS));
    }

    private int _boxHeight = 2;

    /// <summary>Height of a color box</summary>
    public int BoxHeight
    {
        get => _boxHeight;
        set
        {
            if (_boxHeight == value)
            {
                return;
            }
            _boxHeight = value;
            Width = Dim.Auto (minimumContentDim: _boxWidth * COLS);
            Height = Dim.Auto (minimumContentDim: _boxHeight * ROWS);
            SetContentSize (new Size (_boxWidth * COLS, _boxHeight * ROWS));
            SetNeedsLayout ();
        }
    }

    private int _boxWidth = 4;

    /// <summary>Width of a color box</summary>
    public int BoxWidth
    {
        get => _boxWidth;
        set
        {
            if (_boxWidth == value)
            {
                return;
            }
            _boxWidth = value;
            Width = Dim.Auto (minimumContentDim: _boxWidth * COLS);
            Height = Dim.Auto (minimumContentDim: _boxHeight * ROWS);
            SetContentSize (new Size (_boxWidth * COLS, _boxHeight * ROWS));
            SetNeedsLayout ();
        }
    }

    private int _selectColorIndex = (int)Color.Black;

    /// <summary>Cursor for the selected color.</summary>
    public Point Caret
    {
        get => new (_selectColorIndex % COLS, _selectColorIndex / COLS);
        set
        {
            int colorIndex = value.Y * COLS + value.X;
            SelectedColor = (ColorName16)colorIndex;
        }
    }

    /// <summary>Moves the selected item index to the next row.</summary>
    /// <returns></returns>
    private bool MoveDown (ICommandContext? commandContext)
    {
        if (RaiseActivating (commandContext) == true)
        {
            return true;
        }

        if (Caret.Y < ROWS - 1)
        {
            SelectedColor += COLS;

            return true;
        }

        return false;
    }

    /// <summary>Moves the selected item index to the previous column.</summary>
    /// <returns></returns>
    private bool MoveLeft (ICommandContext? commandContext)
    {
        if (RaiseActivating (commandContext) == true)
        {
            return true;
        }

        if (Caret.X <= 0)
        {
            return false;
        }
        SelectedColor--;

        return true;
    }

    /// <summary>Moves the selected item index to the next column.</summary>
    /// <returns></returns>
    private bool MoveRight (ICommandContext? commandContext)
    {
        if (RaiseActivating (commandContext) == true)
        {
            return true;
        }

        if (Caret.X >= COLS - 1)
        {
            return false;
        }
        SelectedColor++;

        return true;
    }

    /// <summary>Moves the selected item index to the previous row.</summary>
    /// <returns></returns>
    private bool MoveUp (ICommandContext? commandContext)
    {
        if (RaiseActivating (commandContext) == true)
        {
            return true;
        }

        if (Caret.Y <= 0)
        {
            return false;
        }
        SelectedColor -= COLS;

        return true;
    }

    ///<inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        SetAttribute (HasFocus ? GetAttributeForRole (VisualRole.Focus) : GetAttributeForRole (VisualRole.Normal));
        var colorIndex = 0;

        for (var y = 0; y < Math.Max (2, Viewport.Height / BoxHeight); y++)
        {
            for (var x = 0; x < Math.Max (8, Viewport.Width / BoxWidth); x++)
            {
                int foregroundColorIndex = y == 0 ? colorIndex + COLS : colorIndex - COLS;

                if (foregroundColorIndex > 15 || colorIndex > 15)
                {
                    continue;
                }

                if (Enabled)
                {
                    SetAttribute (new Attribute ((ColorName16)foregroundColorIndex, (ColorName16)colorIndex));
                }
                else
                {
                    SetAttribute (new Attribute ((ColorName16)foregroundColorIndex, ((Color)(ColorName16)colorIndex).GetDimmerColor (), TextStyle.Faint));
                }

                bool selected = x == Caret.X && y == Caret.Y;
                DrawColorBox (x, y, selected);
                colorIndex++;
            }
        }

        return true;
    }

    #region IValue<ColorName16> Implementation

    /// <summary>Gets or sets the selected color.</summary>
    public ColorName16 SelectedColor
    {
        get => (ColorName16)_selectColorIndex;
        set
        {
            if (value == (ColorName16)_selectColorIndex)
            {
                return;
            }

            var oldValue = (ColorName16)_selectColorIndex;

            ValueChangingEventArgs<ColorName16> changingArgs = new (oldValue, value);

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            if (value is < 0 or > (ColorName16)15)
            {
                throw new ArgumentOutOfRangeException (nameof (value), @"SelectedColor must be between 0 and 15.");
            }

            _selectColorIndex = (int)value;
            SetNeedsDraw ();

            ValueChangedEventArgs<ColorName16> changedArgs = new (oldValue, value);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);

            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, value));
        }
    }

    /// <inheritdoc/>
    public ColorName16 Value { get => SelectedColor; set => SelectedColor = value; }

    /// <inheritdoc/>
    object IValue.GetValue () => SelectedColor;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Called when the <see cref="ColorPicker16"/> <see cref="Value"/> is changing.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<ColorName16> args) => false;

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<ColorName16>>? ValueChanging;

    /// <summary>
    ///     Called when the <see cref="ColorPicker16"/> <see cref="Value"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<ColorName16> args) { }

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<ColorName16>>? ValueChanged;

    #endregion

    /// <summary>Add the commands.</summary>
    private void AddCommands ()
    {
        AddCommand (Command.Left, ctx => MoveLeft (ctx));
        AddCommand (Command.Right, ctx => MoveRight (ctx));
        AddCommand (Command.Up, ctx => MoveUp (ctx));
        AddCommand (Command.Down, ctx => MoveDown (ctx));
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? commandContext)
    {
        base.OnActivated (commandContext);

        if (commandContext?.Binding is MouseBinding { MouseEvent.Position: { } pos })
        {
            int col = pos.X / _boxWidth;
            int row = pos.Y / _boxHeight;

            // Only set Caret if within valid color box bounds
            if (col is >= 0 and < COLS && row is >= 0 and < ROWS)
            {
                Caret = new Point (col, row);
            }
        }
    }

    /// <summary>Add the KeyBindings.</summary>
    private void AddKeyBindings ()
    {
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);

        MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Accept);
        MouseBindings.Remove (MouseFlags.LeftButtonClicked);
    }

    // TODO: Decouple Cursor from SelectedColor so that mouse press-and-hold can show the color under the cursor.

    /// <summary>Draw a box for one color.</summary>
    /// <param name="x">X location.</param>
    /// <param name="y">Y location</param>
    /// <param name="selected"></param>
    private void DrawColorBox (int x, int y, bool selected)
    {
        for (var zoomedY = 0; zoomedY < BoxHeight; zoomedY++)
        {
            for (var zoomedX = 0; zoomedX < BoxWidth; zoomedX++)
            {
                AddRune (x * BoxWidth + zoomedX, y * BoxHeight + zoomedY, (Rune)' ');
            }
        }

        if (selected)
        {
            DrawFocusRect (new Rectangle (x * BoxWidth, y * BoxHeight, BoxWidth, BoxHeight));
        }
    }

    private void DrawFocusRect (Rectangle rect)
    {
        LineCanvas lc = new ();

        if (rect.Width == 1)
        {
            lc.AddLine (rect.Location, rect.Height, Orientation.Vertical, LineStyle.Dotted);
        }
        else if (rect.Height == 1)
        {
            lc.AddLine (rect.Location, rect.Width, Orientation.Horizontal, LineStyle.Dotted);
        }
        else
        {
            lc.AddLine (rect.Location, rect.Width, Orientation.Horizontal, LineStyle.Dotted);

            lc.AddLine (rect.Location with { Y = rect.Location.Y + rect.Height - 1 }, rect.Width, Orientation.Horizontal, LineStyle.Dotted);

            lc.AddLine (rect.Location, rect.Height, Orientation.Vertical, LineStyle.Dotted);

            lc.AddLine (rect.Location with { X = rect.Location.X + rect.Width - 1 }, rect.Height, Orientation.Vertical, LineStyle.Dotted);
        }

        foreach (KeyValuePair<Point, Rune> p in lc.GetMap ())
        {
            AddRune (p.Key.X, p.Key.Y, p.Value);
        }
    }
}

using ColorHelper;

namespace Terminal.Gui.Views;

/// <summary>
///     A bar representing a single component of a <see cref="Color"/> e.g.
///     the Red portion of a <see cref="RGB"/>.
/// </summary>
internal abstract class ColorBar : View, IColorBar
{
    /// <summary>
    ///     Creates a new instance of the <see cref="ColorBar"/> class.
    /// </summary>
    protected ColorBar ()
    {
        Height = Dim.Auto (minimumContentDim: 1);
        Width = Dim.Fill ();
        CanFocus = true;

        AddCommand (Command.Left, _ => Adjust (-1));
        AddCommand (Command.Right, _ => Adjust (1));

        AddCommand (Command.LeftExtend, _ => Adjust (-MaxValue / 20));
        AddCommand (Command.RightExtend, _ => Adjust (MaxValue / 20));

        AddCommand (Command.LeftStart, _ => SetZero ());
        AddCommand (Command.RightEnd, _ => SetMax ());

        // Override Activate to handle mouse-press and drag. When triggered by a left-button press
        // (initial click or drag), extract the position from the MouseBinding context, update
        // Value, set focus, and grab the mouse so subsequent drag events are routed to this bar
        // exclusively. Grabbing here (rather than in OnMouseEvent) ensures that if a consumer
        // cancels the event by setting e.Handled=true in MouseEvent, the press command is never
        // reached and no grab occurs.
        AddCommand (
                    Command.Activate,
                    ctx =>
                    {
                        if (ctx?.Binding is MouseBinding { MouseEvent: { } mouse }
                            && mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed))
                        {
                            UpdateValueFromMousePosition (mouse);
                            SetFocus ();
                            App?.Mouse.GrabMouse (this);

                            return true;
                        }

                        return DefaultActivateHandler (ctx);
                    });

        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorLeft.WithShift, Command.LeftExtend);
        KeyBindings.Add (Key.CursorRight.WithShift, Command.RightExtend);
        KeyBindings.Add (Key.Home, Command.LeftStart);
        KeyBindings.Add (Key.End, Command.RightEnd);
        MouseBindings.Remove (MouseFlags.LeftButtonClicked);

        // Remove the base LeftButtonReleased → Activate binding so that releasing the mouse
        // does not fire DefaultActivateHandler (Activating/Activated). UngrabMouse is still
        // called from OnMouseEvent on release, so the grab lifecycle is unaffected.
        MouseBindings.Remove (MouseFlags.LeftButtonReleased);

        // Bind press and drag to Activate so external code can fully suppress mouse interaction
        // by removing these two bindings (e.g. TerminalGuiDesigner editor mode). When both are
        // absent no value update, focus change, grab, or drag routing occurs.
        MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Activate);
        MouseBindings.Add (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, Command.Activate);
    }

    /// <summary>
    ///     X coordinate that the bar starts at excluding any label.
    /// </summary>
    private int _barStartsAt;

    /// <summary>
    ///     0-1 for how much of the color element is present currently (HSL)
    /// </summary>
    private int _value;

    /// <summary>
    ///     The amount of <see cref="Value"/> represented by each cell width on the bar
    ///     Can be less than 1 e.g. if Saturation (0-100) and width > 100
    /// </summary>
    private double _cellValue = 1d;

    /// <summary>
    ///     Last known width of the bar as passed to <see cref="DrawBar"/>.
    /// </summary>
    private int _barWidth;

    /// <summary>
    ///     The currently selected amount of the color component stored by this class e.g.
    ///     the amount of Hue in a <see cref="HSL"/>.
    /// </summary>
    public int Value
    {
        get => _value;
        set
        {
            int clampedValue = Math.Clamp (value, 0, MaxValue);

            if (_value != clampedValue)
            {
                _value = clampedValue;
                OnValueChanged ();
            }
        }
    }

    /// <inheritdoc/>
    void IColorBar.SetValueWithoutRaisingEvent (int v)
    {
        _value = v;
        SetNeedsDraw ();
    }

    /// <inheritdoc/>
    protected override void OnSubViewsLaidOut (LayoutEventArgs args)
    {
        base.OnSubViewsLaidOut (args);
        var xOffset = 0;

        if (!string.IsNullOrWhiteSpace (Text))
        {
            xOffset = Text.GetColumns ();
        }

        _barWidth = Viewport.Width - xOffset;
        _barStartsAt = xOffset;

        // Each 1 unit of X in the bar corresponds to this much of Value
        _cellValue = (double)MaxValue / (_barWidth - 1);
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (!string.IsNullOrWhiteSpace (Text))
        {
            Move (0, 0);
            SetAttribute (HasFocus ? GetAttributeForRole (VisualRole.Focus) : GetAttributeForRole (VisualRole.Normal));
            AddStr (Text);
        }

        DrawBar (_barStartsAt, 0, _barWidth);

        return true;
    }

    /// <summary>
    ///     Event fired when <see cref="Value"/> is changed to a new value
    /// </summary>
    public event EventHandler<EventArgs<int>>? ValueChanged;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        // Release the grab on button-up so drag events stop routing exclusively to this bar.
        // The grab itself is established in the Command.Activate handler so that cancelling the
        // MouseEvent (e.Handled = true) prevents both the value update and the grab.
        if (mouse.IsReleased && App?.Mouse.IsGrabbed (this) == true)
        {
            App.Mouse.UngrabMouse ();
        }

        return false;
    }

    private void UpdateValueFromMousePosition (Mouse mouse)
    {
        if (mouse.Position is { } pos && pos.X >= _barStartsAt)
        {
            double v = MaxValue * ((double)pos.X - _barStartsAt) / (_barWidth - 1);
            Value = Math.Clamp ((int)v, 0, MaxValue);
        }
    }

    /// <summary>
    ///     When overriden in a derived class, returns the <see cref="Color"/> to
    ///     render at <paramref name="fraction"/> proportion of the full bars width.
    ///     e.g. 0.5 fraction of Saturation is 50% because Saturation goes from 0-100.
    /// </summary>
    /// <param name="fraction"></param>
    /// <returns></returns>
    protected abstract Color GetColor (double fraction);

    /// <summary>
    ///     The maximum value allowed for this component e.g. Saturation allows up to 100 as it
    ///     is a percentage while Hue allows up to 360 as it is measured in degrees.
    /// </summary>
    protected abstract int MaxValue { get; }

    /// <summary>
    ///     The last drawn location in View's viewport where the Triangle appeared.
    ///     Used exclusively for tests.
    /// </summary>
    internal int TrianglePosition { get; private set; }

    private bool? Adjust (int delta)
    {
        var change = (int)(delta * _cellValue);

        // Ensure that the change is at least 1 or -1 if delta is non-zero
        if (change == 0 && delta != 0)
        {
            change = delta > 0 ? 1 : -1;
        }

        Value += change;

        return true;
    }

    private void DrawBar (int xOffset, int yOffset, int width)
    {
        for (var x = 0; x < width; x++)
        {
            double fraction = (double)x / (width - 1);
            Color color = GetColor (fraction);

            // Adjusted isSelectedCell calculation
            double cellBottomThreshold = (x - 1) * _cellValue;
            double cellTopThreshold = x * _cellValue;

            if (x == width - 1)
            {
                cellTopThreshold = MaxValue;
            }

            bool isSelectedCell = Value > cellBottomThreshold && Value <= cellTopThreshold;

            // Check the brightness of the background color
            double brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;

            Color triangleColor = Color.Black;

            if (brightness < 0.15) // Threshold to determine if the color is too close to black
            {
                triangleColor = Color.DarkGray;
            }

            if (isSelectedCell)
            {
                // Draw the triangle at the closest position
                SetAttribute (new Attribute (triangleColor, color, Enabled ? TextStyle.None : TextStyle.Faint));
                AddRune (x + xOffset, yOffset, new Rune ('▲'));

                // Record for tests
                TrianglePosition = x + xOffset;
            }
            else
            {
                SetAttribute (new Attribute (color, color, Enabled ? TextStyle.None : TextStyle.Faint));
                AddRune (x + xOffset, yOffset, new Rune (' '));
            }
        }
    }

    private void OnValueChanged ()
    {
        ValueChanged?.Invoke (this, new EventArgs<int> (in _value));
        SetNeedsDraw ();
    }

    private bool? SetMax ()
    {
        Value = MaxValue;

        return true;
    }

    private bool? SetZero ()
    {
        Value = 0;

        return true;
    }
}

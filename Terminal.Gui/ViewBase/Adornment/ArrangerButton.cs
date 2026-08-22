namespace Terminal.Gui.ViewBase;

/// <summary>
///     A <see cref="Button"/> used in Arrange Mode to indicate and control arrangement operations.
///     Each button represents a specific arrangement action (move or resize from a particular edge).
/// </summary>
/// <remarks>
///     <para>
///         <see cref="ArrangerButton"/> implements <see cref="IOrientation"/> and uses a <see cref="Direction"/>
///         to determine which arrow keys are bound to their directional <see cref="Command"/>s
///         (<see cref="Command.Up"/>, <see cref="Command.Down"/>, <see cref="Command.Left"/>, <see cref="Command.Right"/>
///         ):
///     </para>
///     <list type="table">
///         <listheader>
///             <term>ButtonType</term><term>Orientation</term><term>Direction</term><term>Bound Keys</term>
///         </listheader>
///         <item>
///             <term>
///                 <see cref="ArrangeButtons.Move"/>
///             </term>
///             <term>n/a</term><term>n/a</term>
///             <term>All four arrow keys</term>
///         </item>
///         <item>
///             <term>
///                 <see cref="ArrangeButtons.AllSize"/>
///             </term>
///             <term>n/a</term><term>n/a</term>
///             <term>All four arrow keys</term>
///         </item>
///         <item>
///             <term>
///                 <see cref="ArrangeButtons.LeftSize"/>
///             </term>
///             <term>Horizontal</term><term>Backward</term>
///             <term>Left, Right</term>
///         </item>
///         <item>
///             <term>
///                 <see cref="ArrangeButtons.RightSize"/>
///             </term>
///             <term>Horizontal</term><term>Forward</term>
///             <term>Left, Right</term>
///         </item>
///         <item>
///             <term>
///                 <see cref="ArrangeButtons.TopSize"/>
///             </term>
///             <term>Vertical</term><term>Backward</term>
///             <term>Up, Down</term>
///         </item>
///         <item>
///             <term>
///                 <see cref="ArrangeButtons.BottomSize"/>
///             </term>
///             <term>Vertical</term><term>Forward</term>
///             <term>Up, Down</term>
///         </item>
///     </list>
///     <para>
///         The <see cref="Arranger"/> subscribes to each button's directional commands to drive
///         keyboard-based arrangement.
///     </para>
/// </remarks>
internal class ArrangerButton : Button, IOrientation
{
    private readonly OrientationHelper _orientationHelper;

    public ArrangerButton ()
    {
        CanFocus = true;
        Width = 1;
        Height = 1;
        NoDecorations = true;
        NoPadding = true;
        base.Visible = false;

        AddCommand (Command.Up, DefaultAcceptHandler);
        AddCommand (Command.Down, DefaultAcceptHandler);
        AddCommand (Command.Left, DefaultAcceptHandler);
        AddCommand (Command.Right, DefaultAcceptHandler);

        _orientationHelper = new OrientationHelper (this);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Sets <see cref="ValueChangingEventArgs{T}.NewValue"/> to <see langword="null"/> so that no shadow infrastructure is
    ///     allocated by default for arranger buttons.
    /// </remarks>
    protected override void OnInitializingShadowStyle (ValueChangingEventArgs<ShadowStyles?> args) => args.NewValue = null;

    private ArrangeButtons _buttonType = (ArrangeButtons)(-1);

    /// <summary>
    ///     Gets or sets the type of arrangement button. Setting this property updates
    ///     <see cref="Orientation"/> and <see cref="Direction"/> and rebinds arrow keys accordingly.
    /// </summary>
    public ArrangeButtons ButtonType
    {
        get => _buttonType;
        set
        {
            if (_buttonType == value)
            {
                return;
            }

            _buttonType = value;
            SetTextDirect (GetButtonGlyph ());
            ApplyOrientationAndDirection ();
            SetupKeyBindings ();
        }
    }

    /// <summary>
    ///     Gets or sets the navigation direction for this button.
    /// </summary>
    /// <value>
    ///     <see cref="NavigationDirection.Backward"/> for buttons that represent the start/top/left edge.
    ///     <see cref="NavigationDirection.Forward"/> for buttons that represent the end/bottom/right edge.
    /// </value>
    public NavigationDirection Direction { get; set; }

    /// <inheritdoc/>
    private string GetButtonGlyph () =>
        ButtonType switch
        {
            ArrangeButtons.Move => $"{Glyphs.Move}",
            ArrangeButtons.AllSize => $"{Glyphs.SizeBottomRight}",
            ArrangeButtons.LeftSize or ArrangeButtons.RightSize => $"{Glyphs.SizeHorizontal}",
            ArrangeButtons.TopSize or ArrangeButtons.BottomSize => $"{Glyphs.SizeVertical}",
            _ => Text
        };

    /// <summary>
    ///     Sets <see cref="Orientation"/> and <see cref="Direction"/> based on <see cref="ButtonType"/>.
    /// </summary>
    private void ApplyOrientationAndDirection ()
    {
        switch (_buttonType)
        {
            case ArrangeButtons.LeftSize:
                Orientation = Orientation.Horizontal;
                Direction = NavigationDirection.Backward;

                break;

            case ArrangeButtons.RightSize:
                Orientation = Orientation.Horizontal;
                Direction = NavigationDirection.Forward;

                break;

            case ArrangeButtons.TopSize:
                Orientation = Orientation.Vertical;
                Direction = NavigationDirection.Backward;

                break;

            case ArrangeButtons.BottomSize:
                Orientation = Orientation.Vertical;
                Direction = NavigationDirection.Forward;

                break;

            case ArrangeButtons.Move:
            case ArrangeButtons.AllSize:
                // Both orientations apply; handled in SetupKeyBindings
                Orientation = Orientation.Vertical;
                Direction = NavigationDirection.Forward;

                break;
        }
    }

    /// <summary>
    ///     Binds the appropriate arrow keys to their directional <see cref="Command"/> based on
    ///     the button's <see cref="Orientation"/> and <see cref="ButtonType"/>.
    /// </summary>
    private void SetupKeyBindings ()
    {
        // Remove any previously bound arrow keys
        KeyBindings.Remove (Key.CursorUp);
        KeyBindings.Remove (Key.CursorDown);
        KeyBindings.Remove (Key.CursorLeft);
        KeyBindings.Remove (Key.CursorRight);

        // Remove Enter and Space — arrangement buttons should not respond to Enter or Space
        KeyBindings.Remove (Key.Enter);
        KeyBindings.Remove (Key.Space);

        switch (_buttonType)
        {
            case ArrangeButtons.Move:
            case ArrangeButtons.AllSize:
                // All four arrow keys bound to their directional commands
                KeyBindings.Add (Key.CursorUp, Command.Up);
                KeyBindings.Add (Key.CursorDown, Command.Down);
                KeyBindings.Add (Key.CursorLeft, Command.Left);
                KeyBindings.Add (Key.CursorRight, Command.Right);

                break;

            case ArrangeButtons.LeftSize:
            case ArrangeButtons.RightSize:
                // Horizontal: Left and Right
                KeyBindings.Add (Key.CursorLeft, Command.Left);
                KeyBindings.Add (Key.CursorRight, Command.Right);

                break;

            case ArrangeButtons.TopSize:
            case ArrangeButtons.BottomSize:
                // Vertical: Up and Down
                KeyBindings.Add (Key.CursorUp, Command.Up);
                KeyBindings.Add (Key.CursorDown, Command.Down);

                break;
        }
    }

    #region IOrientation members

    /// <inheritdoc/>
    public Orientation Orientation { get => _orientationHelper.Orientation; set => _orientationHelper.Orientation = value; }

#pragma warning disable CS0067 // The event is never used
    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;
#pragma warning restore CS0067 // The event is never used

    /// <inheritdoc/>
    public void OnOrientationChanged (Orientation newOrientation) { }

    #endregion
}

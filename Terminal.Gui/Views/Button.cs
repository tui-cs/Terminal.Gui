namespace Terminal.Gui.Views;

/// <summary>
///     Raises the <see cref="View.Accepting"/> and <see cref="View.Accepted"/> events when the user presses
///     <see cref="View.HotKey"/>,
///     <c>Enter</c>, or <c>Space</c> or clicks with the mouse.
/// </summary>
/// <remarks>
/// <img src="../images/views/Button.gif" alt="Button demo"/>
///     <para>Use <see cref="View.HotKeySpecifier"/> to change the hot key specifier from the default of ('_').</para>
///     <para>
///         Button can act as the default <see cref="Command.Accept"/> handler for all peer-Views. See
///         <see cref="IsDefault"/>.
///     </para>
///     <para>
///         Set <see cref="View.MouseHoldRepeat"/> to <see langword="true"/> to have the
///         <see cref="View.Accepting"/> event invoked repeatedly while the button is pressed.
///     </para>
///     <para>
///         Button does not raise <see cref="View.Activating"/> events.
///     </para>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Space</term> <description>Accepts the button (<see cref="Command.Accept"/>).</description>
///         </item>
///         <item>
///             <term>Enter</term> <description>Accepts the button (<see cref="Command.Accept"/>).</description>
///         </item>
///     </list>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Click / Double-Click / Triple-Click</term>
///             <description>Accepts the button (<see cref="Command.Accept"/>).</description>
///         </item>
///     </list>
/// </remarks>
public class Button : View, IDesignable, IAcceptTarget
{
    private readonly TextFormatter _interiorTextFormatter = new ();
    private readonly Rune _leftBracket;
    private readonly Rune _leftDefault;
    private readonly Rune _rightBracket;
    private readonly Rune _rightDefault;

    /// <summary>
    ///     Gets or sets whether <see cref="Button"/>s are shown with a shadow effect by default.
    /// </summary>
    public static ShadowStyles DefaultShadow => ButtonSettings.Current.DefaultShadow;

    /// <summary>
    ///     Gets or sets the default Highlight Style.
    /// </summary>
    public static MouseState DefaultMouseHighlightStates => ButtonSettings.Current.DefaultMouseHighlightStates;

    /// <summary>Initializes a new instance of <see cref="Button"/>.</summary>
    public Button ()
    {
        TextAlignment = Alignment.Center;
        VerticalTextAlignment = Alignment.Center;

        _leftBracket = Glyphs.LeftBracket;
        _rightBracket = Glyphs.RightBracket;
        _leftDefault = Glyphs.LeftDefaultIndicator;
        _rightDefault = Glyphs.RightDefaultIndicator;

        _interiorTextFormatter.Alignment = Alignment.Center;
        _interiorTextFormatter.VerticalAlignment = Alignment.Center;
        _interiorTextFormatter.HotKeySpecifier = HotKeySpecifier;

        Height = Dim.Auto (DimAutoStyle.Text);
        Width = Dim.Auto (DimAutoStyle.Text);

        CanFocus = true;

        KeyBindings.ReplaceCommands (Key.Space, Command.Accept);
        KeyBindings.ReplaceCommands (Key.Enter, Command.Accept);

        // Replace default Accept binding with HotKey for mouse clicks
        // These are managed dynamically when MouseHoldRepeat changes
        SetMouseBindings (MouseHoldRepeat);

        TitleChanged += Button_TitleChanged;

        // Determine and apply the initial shadow via the CWP InitializingShadowStyle event, so that
        // subclasses and external subscribers can change or suppress the default allocation
        // before any shadow infrastructure is created.
        RaiseInitializingShadowStyle ();
        MouseHighlightStates = DefaultMouseHighlightStates;
    }

    /// <summary>
    ///     Called before the Button's initial <see cref="View.ShadowStyle"/> is applied during construction.
    ///     Override to change or suppress the default shadow � set <see cref="ValueChangingEventArgs{T}.NewValue"/>
    ///     to the desired style, or set <see cref="ValueChangingEventArgs{T}.Handled"/> to
    ///     <see langword="true"/> to skip applying any shadow.
    /// </summary>
    /// <param name="args">
    ///     Event args whose <see cref="ValueChangingEventArgs{T}.NewValue"/> is pre-set to
    ///     <see cref="DefaultShadow"/>. Subclasses that never display a shadow
    ///     (e.g. <see cref="ScrollButton"/> or internal buttons used by arrangement UI)
    ///     should set <c>args.NewValue = null</c> to avoid the create-then-destroy allocation pattern.
    /// </param>
    protected virtual void OnInitializingShadowStyle (ValueChangingEventArgs<ShadowStyles?> args) { }

    /// <summary>
    ///     Fired before the Button's initial <see cref="View.ShadowStyle"/> is applied during construction.
    ///     Subscribers can modify <see cref="ValueChangingEventArgs{T}.NewValue"/> or set
    ///     <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to suppress the shadow.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<ShadowStyles?>>? InitializingShadowStyle;

    private void RaiseInitializingShadowStyle ()
    {
        ValueChangingEventArgs<ShadowStyles?> args = new (null, DefaultShadow);

        // 1. Virtual method � subclasses override to change/suppress the default shadow.
        OnInitializingShadowStyle (args);

        // 2. Event � external subscribers get a chance to customize.
        InitializingShadowStyle?.Invoke (this, args);

        // 3. Apply the (potentially modified) shadow style unless already handled.
        if (!args.Handled)
        {
            base.ShadowStyle = args.NewValue;
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseHoldRepeatChanged (ValueChangedEventArgs<MouseFlags?> args) => SetMouseBindings (args.NewValue);

    private void SetMouseBindings (MouseFlags? mouseHoldRepeat)
    {
        if (mouseHoldRepeat.HasValue)
        {
            // MouseHoldRepeat enabled: Remove ALL Click/Release/Press bindings, add only configured event?HotKey
            MouseBindings.Remove (MouseFlags.LeftButtonPressed);
            MouseBindings.Remove (MouseFlags.LeftButtonClicked);
            MouseBindings.Remove (MouseFlags.LeftButtonDoubleClicked);
            MouseBindings.Remove (MouseFlags.LeftButtonTripleClicked);
            MouseBindings.Remove (MouseFlags.LeftButtonReleased);

            // Add configured mouse event?HotKey binding
            MouseBindings.Add (mouseHoldRepeat.Value, Command.Accept);
        }
        else
        {
            // MouseHoldRepeat disabled: Remove ALL Click/Release/Press bindings, add only Clicked?HotKey
            MouseBindings.Remove (MouseFlags.LeftButtonClicked);
            MouseBindings.Remove (MouseFlags.LeftButtonReleased);

            // Add Clicked?HotKey bindings (default behavior)
            MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Accept);
            MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Accept);
            MouseBindings.Add (MouseFlags.LeftButtonTripleClicked, Command.Accept);
        }
    }

    /// <inheritdoc/>
    protected override void OnHotKeyCommand (ICommandContext? commandContext) => InvokeCommand (Command.Accept);

    private void Button_TitleChanged (object? sender, EventArgs<string> e)
    {
        SetTextDirect (e.Value);
        TextFormatter.HotKeySpecifier = HotKeySpecifier;
        _interiorTextFormatter.HotKeySpecifier = HotKeySpecifier;
    }

    /// <inheritdoc/>
    protected override void OnTextChanged ()
    {
        Title = Text;

        base.OnTextChanged ();
    }

    /// <inheritdoc/>
    public override Rune HotKeySpecifier
    {
        get => base.HotKeySpecifier;
        set => _interiorTextFormatter.HotKeySpecifier = TextFormatter.HotKeySpecifier = base.HotKeySpecifier = value;
    }

    /// <inheritdoc/>
    public bool IsDefault
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            UpdateTextFormatterText ();
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     Gets or sets whether the Button will show decorations or not. If <see langword="true"/> the glyphs that normally
    ///     bracket the Button Title and the <see cref="IsDefault"/> indicator will not be shown.
    /// </summary>
    public bool NoDecorations { get; set; }

    /// <summary>
    ///     Gets or sets whether the Button will include padding on each side of the Title.
    /// </summary>
    public bool NoPadding { get; set; }

    /// <inheritdoc/>
    protected override void UpdateTextFormatterText ()
    {
        base.UpdateTextFormatterText ();
        TextFormatter.Text = GetDecoratedText ();
    }

    /// <inheritdoc/>
    protected override bool OnDrawingText (DrawContext? context)
    {
        if (NoDecorations || Driver is null)
        {
            return base.OnDrawingText (context);
        }

        Rectangle drawRect = new (ContentToScreen (Point.Empty), GetContentSize ());

        if (drawRect.Width < 2 || drawRect.Height < 1)
        {
            return base.OnDrawingText (context);
        }

        Rectangle interiorRect = new (drawRect.X + 1, drawRect.Y, drawRect.Width - 2, drawRect.Height);
        Attribute normalAttr = HasFocus ? GetAttributeForRole (VisualRole.Focus) : GetAttributeForRole (VisualRole.Normal);
        Attribute hotAttr = HasFocus ? GetAttributeForRole (VisualRole.HotFocus) : GetAttributeForRole (VisualRole.HotNormal);
        string interiorText = GetInteriorText ();

        // Mirror all TextFormatter settings onto _interiorTextFormatter. Guard each assignment
        // so NeedsFormat is not set when a value is unchanged.
        if (_interiorTextFormatter.Text != interiorText)
        {
            _interiorTextFormatter.Text = interiorText;
        }

        if (_interiorTextFormatter.Alignment != TextAlignment)
        {
            _interiorTextFormatter.Alignment = TextAlignment;
        }

        if (_interiorTextFormatter.VerticalAlignment != VerticalTextAlignment)
        {
            _interiorTextFormatter.VerticalAlignment = VerticalTextAlignment;
        }

        if (_interiorTextFormatter.Direction != TextDirection)
        {
            _interiorTextFormatter.Direction = TextDirection;
        }

        if (_interiorTextFormatter.PreserveTrailingSpaces != PreserveTrailingSpaces)
        {
            _interiorTextFormatter.PreserveTrailingSpaces = PreserveTrailingSpaces;
        }

        if (_interiorTextFormatter.MultiLine != TextFormatter.MultiLine)
        {
            _interiorTextFormatter.MultiLine = TextFormatter.MultiLine;
        }

        if (_interiorTextFormatter.WordWrap != TextFormatter.WordWrap)
        {
            _interiorTextFormatter.WordWrap = TextFormatter.WordWrap;
        }

        if (_interiorTextFormatter.TabWidth != TextFormatter.TabWidth)
        {
            _interiorTextFormatter.TabWidth = TextFormatter.TabWidth;
        }

        if (_interiorTextFormatter.PreserveTabs != TextFormatter.PreserveTabs)
        {
            _interiorTextFormatter.PreserveTabs = TextFormatter.PreserveTabs;
        }

        if (_interiorTextFormatter.ConstrainToWidth != interiorRect.Width)
        {
            _interiorTextFormatter.ConstrainToWidth = interiorRect.Width;
        }

        if (_interiorTextFormatter.ConstrainToHeight != interiorRect.Height)
        {
            _interiorTextFormatter.ConstrainToHeight = interiorRect.Height;
        }

        Region? interiorDrawRegion = (interiorRect.Width > 0 && !string.IsNullOrEmpty (interiorText))
            ? _interiorTextFormatter.GetDrawRegion (interiorRect)
            : null;

        context?.AddDrawnRegion (new Region (drawRect));

        int delimiterRow = GetDelimiterRow (drawRect, interiorDrawRegion);

        // Fill the entire content area with the focus/normal attribute to ensure continuous
        // highlight across all rows and across the bracket columns.
        Driver.SetAttribute (normalAttr);
        Driver.FillRect (drawRect);

        Driver.Move (drawRect.X, delimiterRow);
        Driver.AddRune (_leftBracket);

        Driver.Move (drawRect.X + drawRect.Width - 1, delimiterRow);
        Driver.AddRune (_rightBracket);

        if (interiorDrawRegion is not null)
        {
            _interiorTextFormatter.Draw (Driver, interiorRect, normalAttr, hotAttr, Rectangle.Empty);
        }

        SetSubViewNeedsDrawDownHierarchy ();

        return true;
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Title = "_Button";

        return true;
    }

    /// <inheritdoc/>
    public string? GetDemoKeyStrokes () => "wait:500,Enter,wait:1000";

    // GetDecoratedText (called by UpdateTextFormatterText for Dim.Auto sizing) and GetInteriorText
    // (called by OnDrawingText for rendering) must remain in sync when modifying button text formatting.
    private string GetDecoratedText ()
    {
        if (NoDecorations)
        {
            return Text;
        }

        return $"{_leftBracket}{GetInteriorText ()}{_rightBracket}";
    }

    private string GetInteriorText ()
    {
        if (IsDefault)
        {
            return $"{_leftDefault} {Text} {_rightDefault}";
        }

        if (NoPadding)
        {
            return Text;
        }

        return $" {Text} ";
    }

    private int GetDelimiterRow (Rectangle drawRect, Region? interiorDrawRegion)
    {
        if (interiorDrawRegion is not null)
        {
            Rectangle interiorBounds = interiorDrawRegion.GetBounds ();

            if (!interiorBounds.IsEmpty)
            {
                return interiorBounds.Y;
            }
        }

        return VerticalTextAlignment switch
        {
            Alignment.End => drawRect.Y + drawRect.Height - 1,
            Alignment.Center => drawRect.Y + (drawRect.Height - 1) / 2,
            _ => drawRect.Y
        };
    }
}

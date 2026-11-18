namespace Terminal.Gui.ViewBase;

/// <summary>
///     A lightweight <see cref="View"/> that renders title text with focus-appropriate attributes
///     and raises directional <see cref="Command"/>s based on its <see cref="Orientation"/> and
///     <see cref="Direction"/>.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="TitleView"/> implements <see cref="IOrientation"/>. When <see cref="Orientation"/> is
///         <see cref="Orientation.Horizontal"/>, <see cref="TextFormatter.Direction"/> is set to
///         <see cref="TextDirection.LeftRight_TopBottom"/> and Left/Right arrow keys are bound to
///         <see cref="Command.Left"/>/<see cref="Command.Right"/>.
///         When <see cref="Orientation.Vertical"/>, text renders top-to-bottom and Up/Down arrow keys
///         are bound to <see cref="Command.Up"/>/<see cref="Command.Down"/>.
///     </para>
///     <para>
///         The owning view subscribes to <see cref="TitleView"/>'s directional commands (via
///         <see cref="View.CommandsToBubbleUp"/>) to handle navigation.
///     </para>
/// </remarks>
public sealed class TitleView : View, ITitleView, IDesignable
{
    private readonly OrientationHelper _orientationHelper;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TitleView"/> class.
    /// </summary>
    public TitleView ()
    {
        CanFocus = true;

        Width = Dim.Auto ();
        Height = Dim.Auto ();

        TabStop = TabBehavior.TabStop;
        Border.Settings = BorderSettings.Default;
        SuperViewRendersLineCanvas = true;

        AddCommand (Command.HotKey,
                    ctx =>
                    {
                        if (RaiseHandlingHotKey (ctx) is true)
                        {
                            return true;
                        }

                        SetFocus ();

                        return true;
                    });

        // Remove Enter — title views should not respond to Enter
        KeyBindings.Remove (Key.Enter);

        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);

        MouseBindings.Clear ();
        MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);

        _orientationHelper = new OrientationHelper (this);

        // Default to horizontal — call SetupKeyBindings directly because OrientationHelper's
        // default is also Horizontal, so setting it won't trigger OnOrientationChanged.
        SetupKeyBindings ();

        TextFormatter.Alignment = Alignment.Center;
        TextFormatter.VerticalAlignment = Alignment.Center;

        // Setup defaults
        BorderStyle = LineStyle.Rounded;
        TabSide = Side.Top;
    }

#if TAB_COLOR_PROTOTYPE
    /// <inheritdoc />
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (base.OnGettingAttributeForRole (in role, ref currentAttribute))
        {
            return true;
        }

        if (role == VisualRole.Normal)
        {
            currentAttribute = new Attribute (Color.Red, currentAttribute.Background);

            return true;
        }

        if (role == VisualRole.HotNormal)
        {
            currentAttribute = new Attribute (Color.Red, currentAttribute.Background);

            return true;
        }

        return false;
    }
#endif

    /// <summary>
    ///     Gets or sets the navigation direction for this title view.
    /// </summary>
    /// <value>
    ///     <see cref="NavigationDirection.Backward"/> for titles on the top or left edge.
    ///     <see cref="NavigationDirection.Forward"/> for titles on the bottom or right edge.
    /// </value>
    public NavigationDirection Direction { get; set; }

    /// <inheritdoc/>
    protected override void OnTextChanged ()
    {
        Title = Text;

        base.OnTextChanged ();
    }

    /// <summary>
    ///     Binds the appropriate arrow keys to their directional <see cref="Command"/> based on
    ///     the current <see cref="Orientation"/>.
    /// </summary>
    private void SetupKeyBindings ()
    {
        KeyBindings.Remove (Key.CursorUp);
        KeyBindings.Remove (Key.CursorDown);
        KeyBindings.Remove (Key.CursorLeft);
        KeyBindings.Remove (Key.CursorRight);

        if (Orientation == Orientation.Horizontal)
        {
            KeyBindings.Add (Key.CursorLeft, Command.Left);
            KeyBindings.Add (Key.CursorRight, Command.Right);
        }
        else
        {
            KeyBindings.Add (Key.CursorUp, Command.Up);
            KeyBindings.Add (Key.CursorDown, Command.Down);
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
    public void OnOrientationChanged (Orientation newOrientation)
    {
        // Update TextFormatter direction to match the new orientation
        TextFormatter.Direction = newOrientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;

        // Ensure GetAutoWidth/GetAutoHeight will recalculate based on the new orientation.
        TextFormatter.ConstrainToSize = null;

        SetupKeyBindings ();
    }

    #endregion

    #region ITitleView members

    /// <inheritdoc/>
    public Side TabSide
    {
        get;
        set
        {
            field = value;
            MeasuredTabLength = 0;
            ApplyThickness ();
        }
    }

    private int _tabDepth = 3;

    /// <inheritdoc/>
    public int TabDepth
    {
        get => _tabDepth;
        set
        {
            _tabDepth = value;
            ApplyThickness ();
        }
    }

    /// <summary>
    ///     Recomputes and applies <see cref="View.Border"/> thickness from
    ///     <see cref="TabSide"/> and <see cref="TabDepth"/>.
    /// </summary>
    private void ApplyThickness () => Border.Thickness = ComputeTitleViewThickness (TabSide, TabDepth, true);

    /// <inheritdoc/>
    /// <inheritdoc/>
    public int MeasuredTabLength { get; set; }

    /// <inheritdoc/>
    public void UpdateLayout (in TabLayoutContext context)
    {
        if (context.BorderBounds is not { Width: > 0, Height: > 0 })
        {
            Visible = false;

            return;
        }

        int tabDepth = TabDepth;
        bool hasFocus = context.HasFocus;

        // 1. Set text, style, and orientation FIRST so auto-sizing measures correctly.
        Text = context.Title;

        if (context.LineStyle is { } ls)
        {
            BorderStyle = ls;
        }

        Border.Thickness = ComputeTitleViewThickness (TabSide, tabDepth, hasFocus);
        Orientation = TabSide is Side.Left or Side.Right ? Orientation.Vertical : Orientation.Horizontal;

        Thickness padding = hasFocus && tabDepth > 2
                                ? TabSide switch
                                {
                                    Side.Top => new Thickness (0, 0, 0, 1),
                                    Side.Bottom => new Thickness (0, 1, 0, 0),
                                    Side.Right => new Thickness (1, 0, 0, 0),
                                    Side.Left => new Thickness (0, 0, 1, 0),
                                    _ => new Thickness (0)
                                }
                                : new Thickness (0);

        Padding.Thickness = padding;

        // 2. Clear stale TextFormatter constraints so GetAutoWidth/GetAutoHeight
        //    recalculate fresh (orientation may have changed since last layout).
        TextFormatter.ConstrainToSize = null;

        // 3. Measure the natural tab header size via Dim.Auto.
        //    Use the explicit override if provided, otherwise auto-size.
        int tabLength = context.TabLengthOverride ?? (TabSide is Side.Top or Side.Bottom ? GetAutoWidth () : GetAutoHeight ());

        MeasuredTabLength = tabLength;

        Rectangle headerRect = ComputeHeaderRect (context.BorderBounds, TabSide, context.TabOffset, tabLength, tabDepth);
        Rectangle viewBounds = ComputeViewBounds (context.BorderBounds, TabSide, tabDepth);
        Rectangle clipped = Rectangle.Intersect (headerRect, viewBounds);

        if (clipped.IsEmpty)
        {
            Visible = false;

            return;
        }

        Visible = true;

        // 4. Position the frame using the computed header rect, converted to BorderView viewport coords.
        Frame = headerRect with { X = headerRect.X - context.ScreenOrigin.X, Y = headerRect.Y - context.ScreenOrigin.Y };
    }

    #endregion

    #region Static geometry helpers

    /// <summary>
    ///     Computes the unclipped header rectangle for the given side, offset, length, and depth. In content coordinates.
    /// </summary>
    internal static Rectangle ComputeHeaderRect (Rectangle contentBorderRect, Side side, int offset, int length, int depth) =>
        side switch
        {
            Side.Top => new Rectangle (contentBorderRect.X + offset, contentBorderRect.Y - (depth - 1), length, depth),
            Side.Bottom => new Rectangle (contentBorderRect.X + offset, contentBorderRect.Bottom - 1, length, depth),
            Side.Left => new Rectangle (contentBorderRect.X - (depth - 1), contentBorderRect.Y + offset, depth, length),
            Side.Right => new Rectangle (contentBorderRect.Right - 1, contentBorderRect.Y + offset, depth, length),
            _ => Rectangle.Empty
        };

    /// <summary>
    ///     Computes the full view bounds (content border + header protrusion area). In content coordinates.
    /// </summary>
    internal static Rectangle ComputeViewBounds (Rectangle contentBorderRect, Side side, int depth) =>
        side switch
        {
            Side.Top => contentBorderRect with { Y = contentBorderRect.Y - (depth - 1), Height = contentBorderRect.Height + (depth - 1) },
            Side.Bottom => contentBorderRect with { Height = contentBorderRect.Height + (depth - 1) },
            Side.Left => contentBorderRect with { X = contentBorderRect.X - (depth - 1), Width = contentBorderRect.Width + (depth - 1) },
            Side.Right => contentBorderRect with { Width = contentBorderRect.Width + (depth - 1) },
            _ => contentBorderRect
        };

    /// <summary>
    ///     Computes the <see cref="Thickness"/> for the tab TitleView's border based on
    ///     depth, focus state, and which side the tab is on.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         "Cap" is the outward edge (away from content). "Content" is the inward edge (toward content area).
    ///         For depth ≥ 3, the content-side thickness toggles with focus to create the open gap / separator.
    ///         For depth &lt; 3, no focus distinction in border lines.
    ///     </para>
    /// </remarks>
    internal static Thickness ComputeTitleViewThickness (Side tabSide, int depth, bool hasFocus)
    {
        int cap = depth >= 2 ? 1 : 0;
        int contentSide = depth >= 3 && !hasFocus ? 1 : 0;

        return tabSide switch
        {
            Side.Top => new Thickness (1, cap, 1, contentSide),
            Side.Bottom => new Thickness (1, contentSide, 1, cap),
            Side.Left => new Thickness (cap, 1, contentSide, 1),
            Side.Right => new Thickness (contentSide, 1, cap, 1),
            _ => Thickness.Empty
        };
    }

    #endregion

    #region IDesignable

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Text = "_Title";

        return true;
    }

    #endregion
}

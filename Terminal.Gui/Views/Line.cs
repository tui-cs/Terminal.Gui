namespace Terminal.Gui.Views;

/// <summary>
///     Draws a single line using the <see cref="LineStyle"/> specified by <see cref="Line.Style"/>.
/// </summary>
/// <remarks>
/// <img src="../images/views/Line.gif" alt="Line demo"/>
///     <para>
///         <see cref="Line"/> is a <see cref="View"/> that renders a single horizontal or vertical line
///         using the <see cref="LineCanvas"/> system. <see cref="Line"/> integrates with the LineCanvas
///         to enable proper box-drawing character selection and line intersection handling.
///     </para>
///     <para>
///         The line's appearance is controlled by the <see cref="Style"/> property, which supports
///         various line styles including Single, Double, Heavy, Rounded, Dashed, and Dotted.
///     </para>
///     <para>
///         Use the <see cref="Length"/> property to control the extent of the line regardless of its
///         <see cref="Orientation"/>. For horizontal lines, Length controls Width; for vertical lines,
///         it controls Height. The perpendicular dimension is always 1.
///     </para>
///     <para>
///         When multiple <see cref="Line"/> instances or other LineCanvas-aware views (like <see cref="Border"/>)
///         intersect, the LineCanvas automatically selects the appropriate box-drawing characters for corners,
///         T-junctions, and crosses.
///     </para>
///     <para>
///         <see cref="Line"/> sets <see cref="View.SuperViewRendersLineCanvas"/> to <see langword="true"/>,
///         meaning its parent view is responsible for rendering the line. This allows for proper intersection
///         handling when multiple views contribute lines to the same canvas.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         // Create a horizontal line
///         var hLine = new Line { Y = 5 };
///         
///         // Create a vertical line with specific length
///         var vLine = new Line { X = 10, Orientation = Orientation.Vertical, Length = 15 };
///         
///         // Create a double-line style horizontal line
///         var doubleLine = new Line { Y = 10, Style = LineStyle.Double };
///     </code>
/// </example>
public class Line : View, IOrientation
{
    private readonly OrientationHelper _orientationHelper;
    private Dim _length;

    /// <summary>
    ///     Constructs a new instance of the <see cref="Line"/> class with horizontal orientation.
    /// </summary>
    /// <remarks>
    ///     By default, a horizontal line fills the available width and has a height of 1.
    ///     The line style defaults to <see cref="LineStyle.Single"/>.
    /// </remarks>
    public Line ()
    {
        CanFocus = false;
        base.SuperViewRendersLineCanvas = true;

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new OrientationHelper (this);
        _orientationHelper.Orientation = Orientation.Horizontal;

        // Set default dimensions for horizontal orientation
        // Set Height first (this will update _length, but we'll override it next)
        Height = 1;

        // Now set Width and _length to Fill
        _length = Dim.Fill ();
        Width = _length;
    }

    /// <summary>
    ///     Gets or sets the length of the line along its orientation.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is the "source of truth" for the line's primary dimension.
    ///         For a horizontal line, Length controls Width.
    ///         For a vertical line, Length controls Height.
    ///     </para>
    ///     <para>
    ///         When Width or Height is set directly, Length is updated to match the primary dimension.
    ///         When Orientation changes, the appropriate dimension is set to Length and the perpendicular
    ///         dimension is set to 1.
    ///     </para>
    ///     <para>
    ///         This property provides a cleaner API for controlling the line's extent
    ///         without needing to know whether to use Width or Height.
    ///     </para>
    /// </remarks>
    public Dim Length
    {
        get => Orientation == Orientation.Horizontal ? Width : Height;
        set
        {
            _length = value;

            // Update the appropriate dimension based on current orientation
            if (Orientation == Orientation.Horizontal)
            {
                Width = _length;
            }
            else
            {
                Height = _length;
            }
        }
    }

    /// <summary>
    ///     Gets or sets the style of the line. This controls the visual appearance of the line.
    /// </summary>
    /// <remarks>
    ///     Supports various line styles including Single, Double, Heavy, Rounded, Dashed, and Dotted.
    ///     Note: This is separate from <see cref="View.BorderStyle"/> to avoid conflicts with the View's Border.
    /// </remarks>
    public LineStyle Style
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
        }
    } = LineStyle.Single;

    /// <summary>
    ///     Gets or sets an optional <see cref="Attribute"/> used to render the line.
    /// </summary>
    /// <remarks>
    ///     When <see langword="null"/> (the default), the line is drawn using
    ///     <see cref="View.GetAttributeForRole"/> with <see cref="VisualRole.Normal"/>.
    ///     Set this to override the scheme-derived colors and text style.
    /// </remarks>
    public Attribute? LineAttribute
    {
        get;
        set
        {
            field = value;
            SetNeedsDraw ();
        }
    }

    #region IOrientation members

    /// <summary>
    ///     The direction of the line.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When orientation changes, the appropriate dimension is set to <see cref="Length"/>
    ///         and the perpendicular dimension is set to 1.
    ///     </para>
    ///     <para>
    ///         For object initializers where dimensions are set before orientation:
    ///         <code>new Line { Height = 9, Orientation = Orientation.Vertical }</code>
    ///         Setting Height=9 updates Length to 9 (since default orientation is Horizontal and Height is perpendicular).
    ///         Then when Orientation is set to Vertical, Height is set to Length (9) and Width is set to 1,
    ///         resulting in the expected Width=1, Height=9.
    ///     </para>
    /// </remarks>
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
        // Set dimensions based on new orientation:
        // - Primary dimension (along orientation) = Length
        // - Perpendicular dimension = 1
        if (newOrientation == Orientation.Horizontal)
        {
            Width = _length;
            Height = 1;
        }
        else
        {
            Height = _length;
            Width = 1;
        }
    }

    /// <inheritdoc/>
    protected override bool OnWidthChanging (ValueChangingEventArgs<Dim> e)
    {
        if (!IsInitialized)
        {
            // If horizontal, allow width changes and update _length
            _length = e.NewValue;
        }

        if (Orientation == Orientation.Horizontal)
        {
            if (IsInitialized)
            {
                // If horizontal, allow width changes and update _length
                _length = e.NewValue;
            }

            return base.OnWidthChanging (e);
        }

        // If vertical, keep width at 1 (don't allow changes to perpendicular dimension)
        e.NewValue = 1;

        return base.OnWidthChanging (e);
    }

    /// <inheritdoc/>
    protected override bool OnHeightChanging (ValueChangingEventArgs<Dim> e)
    {
        if (!IsInitialized)
        {
            // If vertical, allow height changes and update _length
            _length = e.NewValue;
        }

        if (Orientation == Orientation.Vertical)
        {
            if (IsInitialized)
            {
                // If vertical, allow height changes and update _length
                _length = e.NewValue;
            }

            return base.OnHeightChanging (e);
        }

        // If horizontal, keep height at 1 (don't allow changes to perpendicular dimension)
        e.NewValue = 1;

        return base.OnHeightChanging (e);
    }

    #endregion

    /// <inheritdoc/>
    /// <remarks>
    ///     This method adds the line to the LineCanvas for rendering.
    ///     The actual rendering is performed by the parent view through <see cref="View.RenderLineCanvas"/>.
    /// </remarks>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        Point pos = ViewportToScreen (Viewport).Location;
        int length = Orientation == Orientation.Horizontal ? Frame.Width : Frame.Height;

        LineCanvas.AddLine (pos, length, Orientation, Style, LineAttribute ?? GetAttributeForRole (VisualRole.Normal));

        return true;
    }
}

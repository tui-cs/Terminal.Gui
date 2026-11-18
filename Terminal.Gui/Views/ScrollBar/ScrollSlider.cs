using System.ComponentModel;

namespace Terminal.Gui.Views;

/// <summary>
///     Represents the proportion of the visible content to the Viewport in a <see cref="ScrollBar"/>.
///     Can be dragged with the mouse, constrained by the size of the Viewport of it's superview. Can be
///     oriented either vertically or horizontally.
/// </summary>
public sealed class ScrollSlider : View, IOrientation, IDesignable, IValue<int>
{
    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    public ScrollSlider ()
    {
        Id = "scrollSlider";
        MousePositionTracking = true;

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new (this); // Do not use object initializer!
        _orientationHelper.Orientation = Orientation.Vertical;
        _orientationHelper.OrientationChanging += (_, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (_, e) => OrientationChanged?.Invoke (this, e);

        OnOrientationChanged (Orientation);

        MouseHighlightStates = MouseState.In;
    }

    #region IOrientation members

    private readonly OrientationHelper _orientationHelper;

    /// <inheritdoc/>
    public Orientation Orientation { get => _orientationHelper.Orientation; set => _orientationHelper.Orientation = value; }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;

    /// <inheritdoc/>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        TextDirection = Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;
        TextAlignment = Alignment.Center;
        VerticalTextAlignment = Alignment.Center;

        // Reset Position to 0 when changing orientation
        X = 0;
        Y = 0;
        Position = 0;

        // Reset opposite dim to Dim.Fill ()
        if (Orientation == Orientation.Vertical)
        {
            Height = Width;
            Width = Dim.Fill ();
        }
        else
        {
            Width = Height;
            Height = Dim.Fill ();
        }

        SetNeedsLayout ();
    }

    #endregion

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        if (Orientation == Orientation.Vertical)
        {
            FillRect (Viewport with { Height = Size }, Glyphs.ContinuousMeterSegment);
        }
        else
        {
            FillRect (Viewport with { Width = Size }, Glyphs.ContinuousMeterSegment);
        }

        return true;
    }

    private int? _size;

    /// <summary>
    ///     Gets or sets the size of the ScrollSlider. This is a helper that gets or sets Width or Height depending
    ///     on  <see cref="Orientation"/>. The size will be clamped between 1 and the dimension of
    ///     the <see cref="View.SuperView"/>'s Viewport.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The dimension of the ScrollSlider that is perpendicular to the <see cref="Orientation"/> will be set to
    ///         <see cref="Dim.Fill()"/>
    ///     </para>
    /// </remarks>
    public int Size
    {
        get => _size ?? 1;
        set
        {
            if (value == _size)
            {
                return;
            }

            _size = Math.Clamp (value, 1, VisibleContentSize);

            if (Orientation == Orientation.Vertical)
            {
                Height = _size;
            }
            else
            {
                Width = _size;
            }

            SetNeedsLayout ();
        }
    }

    private int? _visibleContentSize;

    /// <summary>
    ///     Gets or sets the size of the viewport into the content being scrolled. If not explicitly set, will be the
    ///     greater of 1 and the dimension of the <see cref="View.SuperView"/>.
    /// </summary>
    public int VisibleContentSize
    {
        get
        {
            if (_visibleContentSize.HasValue)
            {
                return _visibleContentSize.Value;
            }

            return Math.Max (1, Orientation == Orientation.Vertical ? SuperView?.Viewport.Height ?? 2048 : SuperView?.Viewport.Width ?? 2048);
        }
        set
        {
            if (value == _visibleContentSize)
            {
                return;
            }

            _visibleContentSize = int.Max (1, value);

            if (_position >= _visibleContentSize - _size)
            {
                Position = _position;
            }

            SetNeedsLayout ();
        }
    }

    private int _position;

    /// <summary>
    ///     Gets or sets the position of the ScrollSlider relative to the size of the ScrollSlider's Frame.
    ///     The position will be constrained such that the ScrollSlider will not go outside the Viewport of
    ///     the <see cref="View.SuperView"/>.
    /// </summary>
    public int Position
    {
        get => _position;
        set
        {
            int clampedPosition = ClampPosition (value);

            if (_position == clampedPosition)
            {
                return;
            }

            RaisePositionChangeEvents (clampedPosition);
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     Moves the scroll slider to the specified position. Does not clamp.
    /// </summary>
    /// <param name="position"></param>
    internal void MoveToPosition (int position)
    {
        if (Orientation == Orientation.Vertical)
        {
            Y = _position + SliderPadding / 2;
        }
        else
        {
            X = _position + SliderPadding / 2;
        }
    }

    /// <summary>
    ///     INTERNAL API (for unit tests) - Clamps the position such that the right side of the slider
    ///     never goes past the edge of the Viewport.
    /// </summary>
    /// <param name="newPosition"></param>
    /// <returns></returns>
    internal int ClampPosition (int newPosition) => Math.Clamp (newPosition, 0, Math.Max (SliderPadding / 2, VisibleContentSize - SliderPadding - Size));

    private void RaisePositionChangeEvents (int newPosition)
    {
        int oldPosition = _position;

        // Raise IValue<int> ValueChanging event
        ValueChangingEventArgs<int> valueChangingArgs = new (oldPosition, newPosition);

        if (OnValueChanging (valueChangingArgs) || valueChangingArgs.Handled)
        {
            return;
        }

        ValueChanging?.Invoke (this, valueChangingArgs);

        if (valueChangingArgs.Handled)
        {
            return;
        }

        CancelEventArgs<int> args = new (ref _position, ref newPosition);
        PositionChanging?.Invoke (this, args);

        if (args.Cancel)
        {
            return;
        }

        int distance = newPosition - _position;
        _position = ClampPosition (newPosition);

        MoveToPosition (_position);

        PositionChanged?.Invoke (this, new (in _position));

        // Raise IValue<int> ValueChanged and ValueChangedUntyped events
        ValueChangedEventArgs<int> valueChangedArgs = new (oldPosition, _position);
        OnValueChanged (valueChangedArgs);
        ValueChanged?.Invoke (this, valueChangedArgs);
        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldPosition, _position));

        Scrolled?.Invoke (this, new (in distance));

        RaiseActivating (new CommandContext (Command.Activate, new WeakReference<View> (this), new CommandBinding ([Command.Activate], null, distance)));
    }

    /// <summary>
    ///     Raised when the <see cref="Position"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to
    ///     <see langword="true"/> to prevent the position from being changed.
    /// </summary>
    public event EventHandler<CancelEventArgs<int>>? PositionChanging;

    /// <summary>Raised when the <see cref="Position"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? PositionChanged;

    /// <summary>Raised when the <see cref="Position"/> has changed. Indicates how much to scroll.</summary>
    public event EventHandler<EventArgs<int>>? Scrolled;

    #region IValue<int> Implementation

    /// <summary>
    ///     Gets or sets the position of the ScrollSlider. This is an alias for <see cref="Position"/>
    ///     provided to satisfy the <see cref="IValue{TValue}"/> interface.
    /// </summary>
    public int Value
    {
        get => Position;
        set => Position = value;
    }

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<int>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<int>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Called when <see cref="Value"/> is changing. Return <see langword="true"/> to cancel the change.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    private bool OnValueChanging (ValueChangingEventArgs<int> args) => false;

    /// <summary>
    ///     Called when <see cref="Value"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    private void OnValueChanged (ValueChangedEventArgs<int> args) { }

    #endregion

    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (role != VisualRole.Normal)
        {
            return base.OnGettingAttributeForRole (role, ref currentAttribute);
        }
        currentAttribute = GetAttributeForRole (VisualRole.HotNormal);

        return true;
    }

    private int _lastLocation = -1;

    /// <summary>
    ///     Gets or sets the amount to pad the start and end of the scroll slider. The default is 0.
    /// </summary>
    /// <remarks>
    ///     When the scroll slider is used by <see cref="ScrollBar"/>, which has increment and decrement buttons, the
    ///     SliderPadding should be set to the size of the buttons (typically 2).
    /// </remarks>
    public int SliderPadding { get; set; }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (SuperView is null)
        {
            return false;
        }

        if (mouse.IsSingleDoubleOrTripleClicked)
        {
            return true;
        }

        int location = Orientation == Orientation.Vertical ? mouse.Position!.Value.Y : mouse.Position!.Value.X;
        int offsetFromLastLocation = _lastLocation > -1 ? location - _lastLocation : 0;

        if (mouse is { IsPressed: false, IsReleased: false })
        {
            return false;
        }

        if (mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed) && _lastLocation == -1)
        {
            if (App is { } && App.Mouse.IsGrabbed (this))
            {
                return true;
            }
            App?.Mouse.GrabMouse (this);
            _lastLocation = location;
        }
        else if (mouse.Flags == (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport))
        {
            int currentLocation;

            if (Orientation == Orientation.Vertical)
            {
                currentLocation = Frame.Y;
            }
            else
            {
                currentLocation = Frame.X;
            }

            currentLocation -= SliderPadding / 2;
            int newLocation = currentLocation + offsetFromLastLocation;
            Position = newLocation;
        }
        else if (mouse.Flags == MouseFlags.LeftButtonReleased)
        {
            _lastLocation = -1;

            if (App is { } && App.Mouse.IsGrabbed (this))
            {
                App.Mouse.UngrabMouse ();
            }
        }

        return true;
    }

    /// <summary>
    ///     Gets the slider size.
    /// </summary>
    /// <param name="scrollableContentSize">The size of the content.</param>
    /// <param name="visibleContentSize">The size of the visible content.</param>
    /// <param name="sliderBounds">
    ///     The bounds of the area the slider moves in (e.g. the size of the <see cref="ScrollBar"/>
    ///     minus 2).
    /// </param>
    public static int CalculateSize (int scrollableContentSize, int visibleContentSize, int sliderBounds)
    {
        if (scrollableContentSize <= 0 || sliderBounds <= 0)
        {
            return 1; // Slider must be at least 1
        }

        if (visibleContentSize <= 0 || scrollableContentSize <= visibleContentSize)
        {
            return sliderBounds;
        }

        double sliderSizeD = (double)visibleContentSize / scrollableContentSize * sliderBounds;

        var sliderSize = (int)Math.Floor (sliderSizeD);

        return Math.Clamp (sliderSize, 1, sliderBounds);
    }

    /// <summary>
    ///     Calculates the slider position.
    /// </summary>
    /// <param name="scrollableContentSize">The size of the content.</param>
    /// <param name="visibleContentSize">The size of the visible content.</param>
    /// <param name="contentPosition">The position in the content (between 0 and <paramref name="scrollableContentSize"/>).</param>
    /// <param name="sliderBounds">
    ///     The bounds of the area the slider moves in (e.g. the size of the <see cref="ScrollBar"/>
    ///     minus 2).
    /// </param>
    /// <param name="direction">The direction the slider is moving.</param>
    internal static int CalculatePosition (int scrollableContentSize,
                                           int visibleContentSize,
                                           int contentPosition,
                                           int sliderBounds,
                                           NavigationDirection direction)
    {
        if (scrollableContentSize - visibleContentSize <= 0 || sliderBounds <= 0)
        {
            return 0;
        }

        int calculatedSliderSize = CalculateSize (scrollableContentSize, visibleContentSize, sliderBounds);

        double newSliderPosition = (double)contentPosition / (scrollableContentSize - visibleContentSize) * (sliderBounds - calculatedSliderSize);

        return Math.Clamp ((int)Math.Round (newSliderPosition), 0, sliderBounds - calculatedSliderSize);
    }

    /// <summary>
    ///     Calculates the content position.
    /// </summary>
    /// <param name="scrollableContentSize">The size of the content.</param>
    /// <param name="visibleContentSize">The size of the visible content.</param>
    /// <param name="sliderPosition">The position of the slider.</param>
    /// <param name="sliderBounds">
    ///     The bounds of the area the slider moves in (e.g. the size of the <see cref="ScrollBar"/>
    ///     minus 2).
    /// </param>
    internal static int CalculateContentPosition (int scrollableContentSize, int visibleContentSize, int sliderPosition, int sliderBounds)
    {
        int sliderSize = CalculateSize (scrollableContentSize, visibleContentSize, sliderBounds);

        double pos = (double)sliderPosition / (sliderBounds - sliderSize) * (scrollableContentSize - visibleContentSize);

        if (pos is double.NaN)
        {
            return 0;
        }

        double rounded = Math.Ceiling (pos);

        return (int)Math.Clamp (rounded, 0, Math.Max (0, scrollableContentSize - sliderSize));
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Size = 5;

        return true;
    }
}

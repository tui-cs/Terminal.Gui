namespace Terminal.Gui.Views;

/// <summary>
///     Indicates the size of scrollable content and controls the position of the visible content, either vertically or
///     horizontally.
///     Two <see cref="Button"/>s are provided, one to scroll up or left and one to scroll down or right. Between the
///     buttons is a <see cref="ScrollSlider"/> that can be dragged to
///     control the position of the visible content. The ScrollSlier is sized to show the proportion of the scrollable
///     content to the size of the <see cref="View.Viewport"/>.
/// </summary>
/// <remarks>
/// <img src="../images/views/ScrollBar.gif" alt="ScrollBar demo"/>
///     <para>
///         See the <see href="https://tui-cs.github.io/Terminal.Gui/docs/scrolling.html">Scrolling Deep Dive</see>.
///     </para>
///     <para>
///         By default, the built-in View scrollbars (<see cref="View.VerticalScrollBar"/>/
///         <see cref="View.HorizontalScrollBar"/>) have <see cref="View.Visible"/> set to <see langword="false"/> and
///         <see cref="VisibilityMode"/> set to <see cref="ScrollBarVisibilityMode.Manual"/>.
///         To enable them, use <see cref="ViewportSettingsFlags.HasVerticalScrollBar"/> or
///         <see cref="ViewportSettingsFlags.HasHorizontalScrollBar"/> flags on the owning View's
///         <see cref="View.ViewportSettings"/>.
///     </para>
///     <para>
///         By default, this view cannot be focused and does not support keyboard input.
///     </para>
/// </remarks>
public class ScrollBar : View, IOrientation, IDesignable, IValue<int>
{
    private readonly ScrollButton _decreaseButton;
    private readonly ScrollButton _increaseButton;

    /// <summary>
    ///     Gets the <see cref="ScrollSlider"/> used by this <see cref="ScrollBar"/>.
    /// </summary>
    public ScrollSlider Slider { get; }

    /// <inheritdoc/>
    public ScrollBar ()
    {
        // Set the default width and height based on the orientation - fill Viewport
        Width = Dim.Auto (DimAutoStyle.Content, Dim.Func (_ => Orientation == Orientation.Vertical ? 1 : SuperView?.Viewport.Width ?? 0));

        Height = Dim.Auto (DimAutoStyle.Content, Dim.Func (_ => Orientation == Orientation.Vertical ? SuperView?.Viewport.Height ?? 0 : 1));

        _decreaseButton = new ScrollButton { Direction = NavigationDirection.Backward };
        _decreaseButton.Accepting += OnDecreaseButtonOnAccept;

        Slider = new ScrollSlider
        {
            SliderPadding = 2 // For the buttons
        };
        Slider.Scrolled += SliderOnScroll;
        Slider.PositionChanged += SliderOnPositionChanged;

        _increaseButton = new ScrollButton { Direction = NavigationDirection.Forward };
        _increaseButton.Accepting += OnIncreaseButtonOnAccept;
        Add (_decreaseButton, Slider, _increaseButton);

        CanFocus = false;

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new OrientationHelper (this); // Do not use object initializer!
        _orientationHelper.Orientation = Orientation.Vertical;

        // This sets the width/height etc...
        OnOrientationChanged (Orientation);

        MouseHoldRepeat = MouseFlags.LeftButtonReleased;

        return;

        void OnDecreaseButtonOnAccept (object? s, CommandEventArgs e)
        {
            Value -= Increment;
            e.Handled = true;
        }

        void OnIncreaseButtonOnAccept (object? s, CommandEventArgs e)
        {
            Value += Increment;
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame) => ShowHide ();

    private void ShowHide ()
    {
        switch (VisibilityMode)
        {
            case ScrollBarVisibilityMode.Auto:
                if (VisibleContentSize < 2)
                {
                    // Not enough room to show both buttons, so hide the scrollbar
                    Visible = false;

                    break;
                }

                // VisibilityMode is the authority. ViewportSettings flags are a
                // convenience that *sets* VisibilityMode via SyncOneScrollBar; they
                // should not be re-checked here. When the flag is later removed,
                // SyncOneScrollBar sets VisibilityMode = None, which is handled below.
                Visible = VisibleContentSize < ScrollableContentSize;

                break;

            case ScrollBarVisibilityMode.Always:
                Visible = true;

                break;

            case ScrollBarVisibilityMode.Manual:
                // Hands off — the developer controls Visible directly
                break;

            case ScrollBarVisibilityMode.None:
                Visible = false;

                break;
        }

        Slider.VisibleContentSize = VisibleContentSize;
        Slider.Size = CalculateSliderSize ();
        _sliderPosition = CalculateSliderPositionFromContentPosition (_value);
        Slider.Position = _sliderPosition.Value;
    }

    private void PositionSubViews ()
    {
        _decreaseButton.Orientation = Orientation;
        _increaseButton.Orientation = Orientation;

        if (Orientation == Orientation.Vertical)
        {
            _decreaseButton.Y = 0;
            _decreaseButton.X = 0;
            _decreaseButton.Width = Dim.Fill ();
            _decreaseButton.Height = 1;
            _decreaseButton.Title = Glyphs.UpArrow.ToString ();

            Slider.X = 0;
            Slider.Y = 1;
            Slider.Width = Dim.Fill ();

            _increaseButton.Y = Pos.AnchorEnd ();
            _increaseButton.X = 0;
            _increaseButton.Width = Dim.Fill ();
            _increaseButton.Height = 1;
        }
        else
        {
            _decreaseButton.Y = 0;
            _decreaseButton.X = 0;
            _decreaseButton.Width = 1;
            _decreaseButton.Height = Dim.Fill ();

            Slider.Y = 0;
            Slider.X = 1;
            Slider.Height = Dim.Fill ();

            _increaseButton.Y = 0;
            _increaseButton.X = Pos.AnchorEnd ();
            _increaseButton.Width = 1;
            _increaseButton.Height = Dim.Fill ();
        }
    }

    #region IOrientation members

    private readonly OrientationHelper _orientationHelper;

    /// <inheritdoc/>
    public Orientation Orientation { get => _orientationHelper.Orientation; set => _orientationHelper.Orientation = value; }

#pragma warning disable CS0067 // The event is never used
    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;
#pragma warning restore CS0067 // The event is never used

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;
#pragma warning restore CS0067 // The event is never used

    /// <inheritdoc/>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        TextDirection = Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;
        TextAlignment = Alignment.Center;
        VerticalTextAlignment = Alignment.Center;
        Slider.Orientation = newOrientation;
        PositionSubViews ();

        OrientationChanged?.Invoke (this, new EventArgs<Orientation> (newOrientation));
    }

    #endregion

    /// <summary>
    ///     Gets or sets the amount each mouse wheel event, or click on the increment/decrement buttons, will
    ///     incremenet/decrement the <see cref="Value"/>.
    /// </summary>
    /// <remarks>
    ///     The default is 1.
    /// </remarks>
    public int Increment { get; set; } = 1;

    /// <summary>
    ///     Gets or sets how this <see cref="ScrollBar"/> manages its own <see cref="View.Visible"/> state.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The default is <see cref="ScrollBarVisibilityMode.Manual"/>, meaning the scrollbar does not automatically
    ///         manage its visibility.
    ///     </para>
    ///     <para>
    ///         For built-in View scrollbars (<see cref="View.VerticalScrollBar"/> and <see cref="View.HorizontalScrollBar"/>),
    ///         use the <see cref="ViewportSettingsFlags.HasVerticalScrollBar"/> and
    ///         <see cref="ViewportSettingsFlags.HasHorizontalScrollBar"/> flags to enable them. Setting these flags
    ///         automatically sets <see cref="VisibilityMode"/> to <see cref="ScrollBarVisibilityMode.Auto"/>.
    ///     </para>
    /// </remarks>
    public ScrollBarVisibilityMode VisibilityMode
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }
            field = value;
            ShowHide ();
            SetNeedsLayout ();
        }
    }

    private int? _visibleContentSize;

    /// <summary>
    ///     Gets or sets the size of the visible viewport into the content being scrolled, bounded by
    ///     <see cref="ScrollableContentSize"/>.
    /// </summary>
    /// <remarks>
    ///     If not explicitly set, the visible content size will be appropriate dimension of the ScrollBar's Frame.
    /// </remarks>
    public int VisibleContentSize
    {
        get
        {
            if (_visibleContentSize.HasValue)
            {
                return _visibleContentSize.Value;
            }

            return Orientation == Orientation.Vertical ? Frame.Height : Frame.Width;
        }
        set
        {
            _visibleContentSize = value;
            Slider.Size = CalculateSliderSize ();
            ShowHide ();
        }
    }

    private int? _scrollableContentSize;

    /// <summary>
    ///     Gets or sets the size of the content that can be scrolled. This is typically set to
    ///     <see cref="View.GetContentSize()"/>.
    /// </summary>
    public int ScrollableContentSize
    {
        get
        {
            if (_scrollableContentSize.HasValue)
            {
                return _scrollableContentSize.Value;
            }

            return Orientation == Orientation.Vertical ? SuperView?.GetContentHeight () ?? 0 : SuperView?.GetContentWidth () ?? 0;
        }
        set
        {
            if (value == _scrollableContentSize || value < 0)
            {
                return;
            }

            _scrollableContentSize = value;
            Slider.Size = CalculateSliderSize ();
            ShowHide ();

            if (!Visible)
            {
                return;
            }

            OnSizeChanged (value);
            ScrollableContentSizeChanged?.Invoke (this, new EventArgs<int> (in value));
            SetNeedsLayout ();
        }
    }

    /// <summary>Called when <see cref="ScrollableContentSize"/> has changed. </summary>
    protected virtual void OnSizeChanged (int size) { }

    /// <summary>Raised when <see cref="ScrollableContentSize"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? ScrollableContentSizeChanged;

    #region IValue<int> Implementation

    private int _value;

    /// <summary>
    ///     Gets or sets the scroll position relative to <see cref="ScrollableContentSize"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The scroll position is clamped to 0 and <see cref="ScrollableContentSize"/> minus
    ///         <see cref="VisibleContentSize"/>.
    ///     </para>
    ///     <para>
    ///         Setting will result in the <see cref="ValueChanging"/> and <see cref="ValueChanged"/>
    ///         events being raised.
    ///     </para>
    /// </remarks>
    public int Value
    {
        get => _value;
        set
        {
            if (value == _value || !Visible)
            {
                return;
            }

            // Clamp the value between 0 and Size - VisibleContentSize
            int newValue = Math.Clamp (value, 0, Math.Max (0, ScrollableContentSize - VisibleContentSize));
            NavigationDirection direction = newValue >= _value ? NavigationDirection.Forward : NavigationDirection.Backward;

            int oldValue = _value;

            ValueChangingEventArgs<int> changingArgs = new (oldValue, newValue);

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            int distance = newValue - _value;

            if (_value == newValue)
            {
                return;
            }

            _value = newValue;

            _sliderPosition = CalculateSliderPositionFromContentPosition (_value, direction);

            if (Slider.Position != _sliderPosition)
            {
                Slider.Position = _sliderPosition.Value;
            }

            ValueChangedEventArgs<int> changedArgs = new (oldValue, _value);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, _value));

            OnScrolled (distance);
            Scrolled?.Invoke (this, new EventArgs<int> (in distance));
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     Called when <see cref="Value"/> is changing. Return <see langword="true"/> to cancel the change.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<int> args) => false;

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<int>>? ValueChanging;

    /// <summary>Called when <see cref="Value"/> has changed.</summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<int> args) { }

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<int>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>Called when <see cref="Value"/> has changed. Indicates how much to scroll.</summary>
    protected virtual void OnScrolled (int distance) { }

    /// <summary>Raised when the <see cref="Value"/> has changed. Indicates how much was scrolled.</summary>
    public event EventHandler<EventArgs<int>>? Scrolled;

    #endregion

    /// <summary>
    ///     INTERNAL API (for unit tests) - Calculates the position within the <see cref="ScrollableContentSize"/> based on the
    ///     slider position.
    /// </summary>
    /// <remarks>
    ///     Clamps the sliderPosition, ensuring the returned content position is always less than
    ///     <see cref="ScrollableContentSize"/> - <see cref="VisibleContentSize"/>.
    /// </remarks>
    /// <param name="sliderPosition"></param>
    /// <returns></returns>
    internal int CalculatePositionFromSliderPosition (int sliderPosition)
    {
        int scrollBarSize = Orientation == Orientation.Vertical ? Viewport.Height : Viewport.Width;

        return ScrollSlider.CalculateContentPosition (ScrollableContentSize, VisibleContentSize, sliderPosition, scrollBarSize - Slider.SliderPadding);
    }

    #region Slider Management

    private int? _sliderPosition;

    /// <summary>
    ///     INTERNAL (for unit tests). Calculates the size of the slider based on the Orientation, VisibleContentSize, the
    ///     actual Viewport, and Size.
    /// </summary>
    /// <returns></returns>
    internal int CalculateSliderSize ()
    {
        int maxSliderSize = (Orientation == Orientation.Vertical ? Viewport.Height : Viewport.Width) - 2;

        return ScrollSlider.CalculateSize (ScrollableContentSize, VisibleContentSize, maxSliderSize);
    }

    private void SliderOnPositionChanged (object? sender, EventArgs<int> e)
    {
        if (VisibleContentSize == 0)
        {
            return;
        }

        RaiseSliderPositionChangeEvents (_sliderPosition, e.Value);
    }

    private void SliderOnScroll (object? sender, EventArgs<int> e)
    {
        if (VisibleContentSize == 0)
        {
            return;
        }

        int calculatedSliderPos =
            CalculateSliderPositionFromContentPosition (_value, e.Value >= 0 ? NavigationDirection.Forward : NavigationDirection.Backward);

        if (calculatedSliderPos == _sliderPosition)
        {
            return;
        }

        int sliderScrolledAmount = e.Value;
        int calculatedPosition = CalculatePositionFromSliderPosition (calculatedSliderPos + sliderScrolledAmount);

        Value = calculatedPosition;
    }

    /// <summary>
    ///     Gets or sets the position of the start of the Scroll slider, within the Viewport.
    /// </summary>
    public int GetSliderPosition () => CalculateSliderPositionFromContentPosition (_value);

    private void RaiseSliderPositionChangeEvents (int? currentSliderPosition, int newSliderPosition)
    {
        if (currentSliderPosition == newSliderPosition)
        {
            return;
        }

        _sliderPosition = newSliderPosition;

        OnSliderPositionChanged (newSliderPosition);
        SliderPositionChanged?.Invoke (this, new EventArgs<int> (in newSliderPosition));
    }

    /// <summary>Called when the slider position has changed.</summary>
    protected virtual void OnSliderPositionChanged (int position) { }

    /// <summary>Raised when the slider position has changed.</summary>
    public event EventHandler<EventArgs<int>>? SliderPositionChanged;

    /// <summary>
    ///     INTERNAL API (for unit tests) - Calculates the position of the slider based on the content position.
    /// </summary>
    /// <param name="contentPosition"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    internal int CalculateSliderPositionFromContentPosition (int contentPosition, NavigationDirection direction = NavigationDirection.Forward)
    {
        int scrollBarSize = Orientation == Orientation.Vertical ? Viewport.Height : Viewport.Width;

        return ScrollSlider.CalculatePosition (ScrollableContentSize, VisibleContentSize, contentPosition, scrollBarSize - 2, direction);
    }

    #endregion Slider Management

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        if (!Slider.Visible)
        {
            return true;
        }

        if (Orientation == Orientation.Vertical)
        {
            FillRect (Viewport with { Y = Viewport.Y + 1, Height = Viewport.Height - 2 }, Glyphs.Stipple);
        }
        else
        {
            FillRect (Viewport with { X = Viewport.X + 1, Width = Viewport.Width - 2 }, Glyphs.Stipple);
        }

        SetNeedsDraw ();

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        // Only handle mouse clicks
        if (args.Context?.Binding is not MouseBinding { MouseEvent: { } mouse })
        {
            return base.OnActivating (args);
        }

        int sliderCenter;
        int distanceFromCenter;

        if (Orientation == Orientation.Vertical)
        {
            sliderCenter = 1 + Slider.Frame.Y + Slider.Frame.Height / 2;
            distanceFromCenter = mouse.Position!.Value.Y - sliderCenter;
        }
        else
        {
            sliderCenter = 1 + Slider.Frame.X + Slider.Frame.Width / 2;
            distanceFromCenter = mouse.Position!.Value.X - sliderCenter;
        }

#if PROPORTIONAL_SCROLL_JUMP
        // TODO: This logic mostly works to provide a proportional jump. However, the math
        // TODO: falls apart in edge cases. Most scroll bars (e.g. Windows) do not do proportional
        // TODO: Thus, this is disabled; we just jump a page each click.
        // Ratio of the distance to the viewport dimension
        double ratio = (double)Math.Abs (distanceFromCenter) / (VisibleContentSize);
        // Jump based on the ratio and the total content size
        int jump = (int)(ratio * (Size - VisibleContentSize));
#else
        int jump = VisibleContentSize;
#endif

        // Adjust the content position based on the distance
        if (distanceFromCenter < 0)
        {
            Value = Math.Max (0, Value - jump);
        }
        else
        {
            Value = Math.Min (ScrollableContentSize - Slider.VisibleContentSize, Value + jump);
        }

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (SuperView is null)
        {
            return false;
        }

        if (!mouse.IsWheel)
        {
            return false;
        }

        if (Orientation == Orientation.Vertical)
        {
            if (mouse.Flags.FastHasFlags (MouseFlags.WheeledDown))
            {
                Value += Increment;
            }

            if (mouse.Flags.FastHasFlags (MouseFlags.WheeledUp))
            {
                Value -= Increment;
            }
        }
        else
        {
            if (mouse.Flags.FastHasFlags (MouseFlags.WheeledRight))
            {
                Value += Increment;
            }

            if (mouse.Flags.FastHasFlags (MouseFlags.WheeledLeft))
            {
                Value -= Increment;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        OrientationChanged += (_, args) =>
                              {
                                  if (args.Value == Orientation.Vertical)
                                  {
                                      Width = 1;
                                      Height = 10;
                                  }
                                  else
                                  {
                                      Width = 30;
                                      Height = 1;
                                  }
                              };

        Orientation = Orientation.Horizontal;
        Width = 30;
        Height = 1;
        VisibleContentSize = 10;
        ScrollableContentSize = 250;
        Value = 80;

        return true;
    }

    /// <inheritdoc/>
    public string? GetDemoKeyStrokes () => "wait:2000";
}

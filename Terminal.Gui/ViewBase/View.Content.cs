namespace Terminal.Gui.ViewBase;

public partial class View
{
    #region Content Area

    // Nullable holders of developer-specified content dimensions.
    // When null the corresponding dimension tracks the Viewport size automatically.
    private int? _contentWidth;
    private int? _contentHeight;

    /// <summary>
    ///     Sets the width of the View's content area independently of the height.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         Negative values are not supported.
    ///     </para>
    ///     <para>
    ///         If set to <see langword="null"/>, <see cref="GetContentWidth ()"/> will track the <see cref="Viewport"/> width.
    ///     </para>
    ///     <para>
    ///         If set to a non-<see langword="null"/> value, the content width is independent of the <see cref="Viewport"/>
    ///         width, enabling horizontal scrolling.
    ///     </para>
    ///     <para>
    ///         This method follows the Cancellable Work Pattern (CWP). The <see cref="ContentSizeChanging"/> event
    ///         is raised before the change, and <see cref="ContentSizeChanged"/> is raised after.
    ///     </para>
    /// </remarks>
    /// <param name="contentWidth">The new content width, or <see langword="null"/> to track the Viewport width.</param>
    /// <seealso cref="SetContentHeight"/>
    /// <seealso cref="SetContentSize"/>
    public void SetContentWidth (int? contentWidth)
    {
        if (contentWidth is < 0)
        {
            throw new ArgumentException (@"Content width cannot be negative.", nameof (contentWidth));
        }

        ApplyContentDimensionChange (contentWidth, _contentHeight);
    }

    /// <summary>
    ///     Sets the height of the View's content area independently of the width.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         Negative values are not supported.
    ///     </para>
    ///     <para>
    ///         If set to <see langword="null"/>, <see cref="GetContentHeight ()"/> will track the <see cref="Viewport"/>
    ///         height.
    ///     </para>
    ///     <para>
    ///         If set to a non-<see langword="null"/> value, the content height is independent of the <see cref="Viewport"/>
    ///         height, enabling vertical scrolling.
    ///     </para>
    ///     <para>
    ///         This method follows the Cancellable Work Pattern (CWP). The <see cref="ContentSizeChanging"/> event
    ///         is raised before the change, and <see cref="ContentSizeChanged"/> is raised after.
    ///     </para>
    /// </remarks>
    /// <param name="contentHeight">The new content height, or <see langword="null"/> to track the Viewport height.</param>
    /// <seealso cref="SetContentWidth"/>
    /// <seealso cref="SetContentSize"/>
    public void SetContentHeight (int? contentHeight)
    {
        if (contentHeight is < 0)
        {
            throw new ArgumentException (@"Content height cannot be negative.", nameof (contentHeight));
        }

        ApplyContentDimensionChange (_contentWidth, contentHeight);
    }

    /// <summary>
    ///     Sets the size of the View's content.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         Negative sizes are not supported.
    ///     </para>
    ///     <para>
    ///         If not explicitly set to a non-<see langword="null"/> value, and the View has Subviews,
    ///         <see cref="GetContentSize ()"/> will return
    ///         the size of the <see cref="Viewport"/>.
    ///     </para>
    ///     <para>
    ///         If set to a non-<see langword="null"/> value, <see cref="Viewport"/> describes the portion of the content
    ///         currently visible to the user. This enables
    ///         virtual scrolling and the behavior of <see cref="DimAutoStyle.Content"/> will be to use
    ///         <see cref="GetContentSize ()"/> to determine the size
    ///         of the view.
    ///     </para>
    ///     <para>
    ///         This method follows the Cancellable Work Pattern (CWP). The <see cref="ContentSizeChanging"/> event
    ///         is raised before the change, and <see cref="ContentSizeChanged"/> is raised after.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ContentSizeChanging"/>
    /// <seealso cref="ContentSizeChanged"/>
    /// <seealso cref="SetContentWidth"/>
    /// <seealso cref="SetContentHeight"/>
    public void SetContentSize (Size? contentSize)
    {
        if (contentSize is { } s && (s.Width < 0 || s.Height < 0))
        {
            throw new ArgumentException (@"ContentSize cannot be negative.", nameof (contentSize));
        }

        ApplyContentDimensionChange (contentSize?.Width, contentSize?.Height);
    }

    /// <summary>
    ///     Internal helper that applies a content dimension change using the CWP pattern.
    ///     Both dimensions are passed so the composite <see cref="Size"/>? events fire correctly.
    /// </summary>
    private void ApplyContentDimensionChange (int? newWidth, int? newHeight)
    {
        // Compute old and new composite sizes for CWP events.
        Size? oldComposite = _contentWidth is null && _contentHeight is null
                                 ? null
                                 : new Size (_contentWidth ?? Viewport.Size.Width, _contentHeight ?? Viewport.Size.Height);

        Size? newComposite = newWidth is null && newHeight is null ? null : new Size (newWidth ?? Viewport.Size.Width, newHeight ?? Viewport.Size.Height);

        if (EqualityComparer<Size?>.Default.Equals (oldComposite, newComposite))
        {
            return;
        }

        // CWP: Fire OnChanging / ChangingEvent (can cancel)
        ValueChangingEventArgs<Size?> changingArgs = new (oldComposite, newComposite);

        if (OnContentSizeChanging (changingArgs) || changingArgs.Handled)
        {
            return;
        }

        ContentSizeChanging?.Invoke (this, changingArgs);

        if (changingArgs.Handled)
        {
            return;
        }

        // Apply potentially modified new value
        Size? finalComposite = changingArgs.NewValue;

        // Update backing fields while preserving null semantics for dimensions
        // that should continue tracking the Viewport size.
        if (finalComposite is null)
        {
            _contentWidth = null;
            _contentHeight = null;
        }
        else
        {
            _contentWidth = newWidth is null ? null : finalComposite.Value.Width;
            _contentHeight = newHeight is null ? null : finalComposite.Value.Height;
        }

        // Do the work
        SetNeedsLayout ();

        // CWP: Fire OnChanged / ChangedEvent
        ValueChangedEventArgs<Size?> changedArgs = new (oldComposite, finalComposite);
        OnContentSizeChanged (changedArgs);
        ContentSizeChanged?.Invoke (this, changedArgs);
    }

    /// <summary>
    ///     Gets the width of the View's content area.
    /// </summary>
    /// <remarks>
    ///     If the content width was not explicitly set by <see cref="SetContentWidth"/>,
    ///     returns the <see cref="Viewport"/> width.
    /// </remarks>
    /// <returns>The content area width.</returns>
    public int GetContentWidth () => _contentWidth ?? Viewport.Size.Width;

    /// <summary>
    ///     Gets the height of the View's content area.
    /// </summary>
    /// <remarks>
    ///     If the content height was not explicitly set by <see cref="SetContentHeight"/>,
    ///     returns the <see cref="Viewport"/> height.
    /// </remarks>
    /// <returns>The content area height.</returns>
    public int GetContentHeight () => _contentHeight ?? Viewport.Size.Height;

    /// <summary>
    ///     Gets the size of the View's content.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         If the content size was not explicitly set by <see cref="SetContentSize"/>, and the View has no visible
    ///         subviews, <see cref="GetContentSize ()"/> will return the
    ///         size of
    ///         <see cref="Viewport"/>.
    ///     </para>
    ///     <para>
    ///         If the content size was not explicitly set by <see cref="SetContentSize"/>, this function will return the
    ///         Viewport size.
    ///     </para>
    ///     <para>
    ///         If set <see cref="Viewport"/> describes the portion of the content currently visible to the user. This enables
    ///         virtual scrolling.
    ///     </para>
    ///     <para>
    ///         If set the behavior of <see cref="DimAutoStyle.Content"/> will be to use the ContentSize to determine the size
    ///         of the view.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     If the content size was not explicitly set by <see cref="SetContentSize"/>, <see cref="GetContentSize ()"/> will
    ///     return the size of the <see cref="Viewport"/> and <see cref="ContentSizeTracksViewport"/> will be
    ///     <see langword="true"/>.
    /// </returns>
    /// <seealso cref="GetContentWidth"/>
    /// <seealso cref="GetContentHeight"/>
    public Size GetContentSize () => new (GetContentWidth (), GetContentHeight ());

    /// <summary>
    ///     Gets the minimum number of columns required for all the View's SubViews to fit in the content area.
    /// </summary>
    /// <returns></returns>
    public int GetWidthRequiredForSubViews () =>

        // DimAuto.Calculate adds the Adornments thickness, so we need to subtract it here since
        // we want the content size only.
        GetAutoWidth () - GetAdornmentsThickness ().Horizontal;

    /// <summary>
    ///     Gets the natural (auto-size) width of the view as calculated by <see cref="Dim.Auto"/>.
    /// </summary>
    /// <remarks>
    ///     The returned width is the full auto-calculated width for the view, including adornment thickness.
    ///     Unlike <see cref="GetWidthRequiredForSubViews ()"/>, this value is not content-only.
    ///     The calculation may also respect minimum and maximum content constraints applied by the auto dimension logic
    ///     before adornments are added.
    /// </remarks>
    /// <returns></returns>
    public int GetAutoWidth () => Dim.Auto ().Calculate (0, GetContainerSize ().Width, this, Dimension.Width);

    /// <summary>
    ///     Gets the natural (auto-size) height of the view as calculated by <see cref="Dim.Auto"/>.
    /// </summary>
    /// <remarks>
    ///     The returned height is the full auto-calculated height for the view, including adornment thickness.
    ///     Unlike <see cref="GetHeightRequiredForSubViews ()"/>, this value is not content-only.
    ///     The calculation may also respect minimum and maximum content constraints applied by the auto dimension logic
    ///     before adornments are added.
    /// </remarks>
    /// <returns></returns>
    public int GetAutoHeight () => Dim.Auto ().Calculate (0, GetContainerSize ().Height, this, Dimension.Height);

    /// <summary>
    ///     Gets the minimum number of rows required for all the View's SubViews to fit in the content area.
    /// </summary>
    /// <returns></returns>
    public int GetHeightRequiredForSubViews () =>

        // DimAuto.Calculate adds the Adornments thickness, so we need to subtract it here since
        // we want the content size only.
        GetAutoHeight () - GetAdornmentsThickness ().Vertical;

    /// <summary>
    ///     Gets or sets a value indicating whether the view's content size tracks the <see cref="Viewport"/>'s
    ///     size or not.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <list type="bullet">
    ///         <listheader>
    ///             <term>Value</term> <description>Result</description>
    ///         </listheader>
    ///         <item>
    ///             <term>
    ///                 <see langword="true"/>
    ///             </term>
    ///             <description>
    ///                 <para>
    ///                     <see cref="GetContentSize ()"/> will return the <see cref="Viewport"/>'s size. Content scrolling
    ///                     will be
    ///                     disabled.
    ///                 </para>
    ///                 <para>
    ///                     The behavior of <see cref="DimAutoStyle.Content"/> will be to use position and size of the SubViews
    ///                     to
    ///                     determine the size of the view, ignoring <see cref="GetContentSize ()"/>.
    ///                 </para>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <see langword="false"/>
    ///             </term>
    ///             <description>
    ///                 <para>
    ///                     The return value of <see cref="GetContentSize ()"/> is independent of <see cref="Viewport"/> and
    ///                     <see cref="Viewport"/>
    ///                     describes the portion of the content currently visible to the user enabling content scrolling.
    ///                 </para>
    ///                 <para>
    ///                     The behavior of <see cref="DimAutoStyle.Content"/> will be to use <see cref="GetContentSize ()"/>
    ///                     to
    ///                     determine the
    ///                     size of the view, ignoring the position and size of the SubViews.
    ///                 </para>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public bool ContentSizeTracksViewport
    {
        get => _contentWidth is null && _contentHeight is null;
        set
        {
            if (!value)
            {
                return;
            }
            _contentWidth = null;
            _contentHeight = null;
        }
    }

    /// <summary>
    ///     Called before the content size changes, allowing subclasses to cancel or modify the change.
    /// </summary>
    /// <param name="args">The event arguments containing the current and proposed new content size.</param>
    /// <returns>True to cancel the change, false to proceed.</returns>
    protected virtual bool OnContentSizeChanging (ValueChangingEventArgs<Size?> args) => false;

    /// <summary>
    ///     Called after the content size has changed.
    /// </summary>
    /// <param name="args">The event arguments containing the old and new content size.</param>
    protected virtual void OnContentSizeChanged (ValueChangedEventArgs<Size?> args) { }

    /// <summary>
    ///     Raised before the content size changes, allowing handlers to modify or cancel the change.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Set <see cref="ValueChangingEventArgs{T}.Handled"/> to true to cancel the change or modify
    ///         <see cref="ValueChangingEventArgs{T}.NewValue"/> to adjust the proposed value.
    ///     </para>
    ///     <para>
    ///         This event follows the Cancellable Work Pattern (CWP). See the
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/cancellable-work-pattern.html">CWP Deep Dive</see>
    ///         for more information.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         view.ContentSizeChanging += (sender, args) =>
    ///         {
    ///             if (args.NewValue?.Width > 1000)
    ///             {
    ///                 args.Handled = true;
    ///                 Console.WriteLine("Content size too large, change cancelled.");
    ///             }
    ///         };
    ///     </code>
    /// </example>
    public event EventHandler<ValueChangingEventArgs<Size?>>? ContentSizeChanging;

    /// <summary>
    ///     Raised after the content size has changed, notifying handlers of the completed change.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Provides the old and new content size via <see cref="ValueChangedEventArgs{T}.OldValue"/> and
    ///         <see cref="ValueChangedEventArgs{T}.NewValue"/>, which may be null.
    ///     </para>
    ///     <para>
    ///         This event follows the Cancellable Work Pattern (CWP). See the
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/cancellable-work-pattern.html">CWP Deep Dive</see>
    ///         for more information.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         view.ContentSizeChanged += (sender, args) =>
    ///         {
    ///             Console.WriteLine($"Content size changed from {args.OldValue} to {args.NewValue}.");
    ///         };
    ///     </code>
    /// </example>
    public event EventHandler<ValueChangedEventArgs<Size?>>? ContentSizeChanged;

    /// <summary>
    ///     Converts a Content-relative location to a Screen-relative location.
    /// </summary>
    /// <param name="location">The Content-relative location.</param>
    /// <returns>The Screen-relative location.</returns>
    public Point ContentToScreen (in Point location)
    {
        // Subtract the ViewportOffsetFromFrame to get the Viewport-relative location.
        Point contentRelativeToViewport = location;
        contentRelativeToViewport.Offset (-Viewport.X, -Viewport.Y);

        // Translate to Screen-Relative (our SuperView's Viewport-relative coordinates)
        return ViewportToScreen (contentRelativeToViewport);
    }

    #endregion Content Area

    #region Viewport

    /// <summary>
    ///     Gets or sets how scrolling the <see cref="View.Viewport"/> on the View's Content Area is handled.
    /// </summary>
    public ViewportSettingsFlags ViewportSettings
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            ViewportSettingsFlags oldFlags = field;
            field = value;

            SyncScrollBarsToSettings (oldFlags, value);

            if (IsInitialized)
            {
                // Force set Viewport to cause settings to be applied as needed
                SetViewport (Viewport);
            }
        }
    }

    /// <summary>
    ///     The location of the viewport into the view's content (0,0) is the top-left corner of the content. The Content
    ///     area's size
    ///     is <see cref="GetContentSize ()"/>.
    /// </summary>
    private Point _viewportLocation;

    /// <summary>
    ///     Gets or sets the rectangle describing the portion of the View's content that is visible to the user.
    ///     The viewport Location is relative to the top-left corner of the inner rectangle of <see cref="Padding"/>.
    ///     If the viewport Size is the same as <see cref="GetContentSize ()"/>, or <see cref="GetContentSize ()"/> is
    ///     <see langword="null"/> the Location will be <c>0, 0</c>.
    /// </summary>
    /// <value>
    ///     The rectangle describing the location and size of the viewport into the View's virtual content, described by
    ///     <see cref="GetContentSize ()"/>.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         Positive values for the location indicate the visible area is offset into (down-and-right) the View's virtual
    ///         <see cref="GetContentSize ()"/>. This enables scrolling down and to the right (e.g. in a <see cref="ListView"/>
    ///         .
    ///     </para>
    ///     <para>
    ///         Negative values for the location indicate the visible area is offset above (up-and-left) the View's virtual
    ///         <see cref="GetContentSize ()"/>. This enables scrolling up and to the left (e.g. in an image viewer that
    ///         supports
    ///         zoom
    ///         where the image stays centered).
    ///     </para>
    ///     <para>
    ///         The <see cref="ViewportSettings"/> property controls how scrolling is handled.
    ///     </para>
    ///     <para>
    ///         Updates to the Viewport Size updates <see cref="Frame"/>, and has the same impact as updating the
    ///         <see cref="Frame"/>.
    ///     </para>
    ///     <para>
    ///         Altering the Viewport Size will eventually (when the view is next laid out) cause the
    ///         <see cref="Layout()"/> and <see cref="OnDrawingContent(DrawContext)"/> methods to be called.
    ///     </para>
    /// </remarks>
    public virtual Rectangle Viewport
    {
        get
        {
            Thickness thickness = GetAdornmentsThickness ();

            return new Rectangle (_viewportLocation,
                                  new Size (Math.Max (0, Frame.Size.Width - thickness.Horizontal), Math.Max (0, Frame.Size.Height - thickness.Vertical)));
        }
        set => SetViewport (value);
    }

    private void SetViewport (Rectangle viewport)
    {
        Rectangle oldViewport = new (_viewportLocation, Viewport.Size);
        ApplySettings (ref viewport);

        Thickness thickness = GetAdornmentsThickness ();

        Size newSize = new (viewport.Size.Width + thickness.Horizontal, viewport.Size.Height + thickness.Vertical);

        // Detect the "adornments consume entire viewport" no-op: when the requested viewport size equals
        // the current Viewport size (which may already be clamped to zero when Frame is smaller than adornments).
        // In this case updating Frame would incorrectly grow it, so treat it as a no-op.
        if (newSize == Frame.Size || viewport.Size == Viewport.Size)
        {
            // The change is not changing the Frame, or the adornments consume the entire Frame, so we don't need to update it.
            // Just call SetNeedsLayout to update the layout.
            if (_viewportLocation != viewport.Location)
            {
                _viewportLocation = viewport.Location;
                SetNeedsLayout ();
            }

            // QUESTION: Shouldn't this be inside the if statement above?
            RaiseViewportChangedEvent (oldViewport);

            return;
        }

        _viewportLocation = viewport.Location;

        // Update the Frame because we made it bigger or smaller which impacts subviews.
        Frame = Frame with { Size = newSize };

        // Note, setting the Frame will cause ViewportChanged to be raised.

        return;

        void ApplySettings (ref Rectangle newViewport)
        {
            if (!ViewportSettings.FastHasFlags (ViewportSettingsFlags.AllowXGreaterThanContentWidth))
            {
                if (newViewport.X >= GetContentWidth ())
                {
                    newViewport.X = GetContentWidth () - 1;
                }
            }

            if (!ViewportSettings.FastHasFlags (ViewportSettingsFlags.AllowXPlusWidthGreaterThanContentWidth))
            {
                if (newViewport.X + newViewport.Width > GetContentWidth ())
                {
                    newViewport.X = GetContentWidth () - newViewport.Width;
                }
            }

            // IMPORTANT: Check for negative location AFTER checking for location greater than content size
            if (!ViewportSettings.FastHasFlags (ViewportSettingsFlags.AllowNegativeX))
            {
                if (newViewport.X < 0)
                {
                    newViewport.X = 0;
                }
            }

            if (!ViewportSettings.FastHasFlags (ViewportSettingsFlags.AllowNegativeXWhenWidthGreaterThanContentWidth))
            {
                if (Viewport.Width > GetContentWidth ())
                {
                    newViewport.X = 0;
                }
            }

            if (!ViewportSettings.FastHasFlags (ViewportSettingsFlags.AllowYGreaterThanContentHeight))
            {
                if (newViewport.Y >= GetContentHeight ())
                {
                    newViewport.Y = GetContentHeight () - 1;
                }
            }

            if (!ViewportSettings.FastHasFlags (ViewportSettingsFlags.AllowYPlusHeightGreaterThanContentHeight))
            {
                if (newViewport.Y + newViewport.Height > GetContentHeight ())
                {
                    newViewport.Y = GetContentHeight () - newViewport.Height;
                }
            }

            // IMPORTANT: Check for negative location AFTER checking for location greater than content size
            if (!ViewportSettings.FastHasFlags (ViewportSettingsFlags.AllowNegativeY))
            {
                if (newViewport.Y < 0)
                {
                    newViewport.Y = 0;
                }
            }

            if (!ViewportSettings.FastHasFlags (ViewportSettingsFlags.AllowNegativeYWhenHeightGreaterThanContentHeight))
            {
                if (Viewport.Height > GetContentHeight ())
                {
                    newViewport.Y = 0;
                }
            }
        }
    }

    private void RaiseViewportChangedEvent (Rectangle oldViewport)
    {
        if (Cursor.IsVisible)
        {
            // Adjust the cursor if visible
            int deltaX = oldViewport.X - Viewport.X;
            int deltaY = oldViewport.Y - Viewport.Y;

            Point currentCursorPos = ScreenToViewport (Cursor.Position!.Value);

            SetCursor (Cursor with { Position = ViewportToScreen (new Point (currentCursorPos.X + deltaX, currentCursorPos.Y + deltaY)) });
        }

        DrawEventArgs args = new (IsInitialized ? Viewport : Rectangle.Empty, oldViewport, null);
        OnViewportChanged (args);
        ViewportChanged?.Invoke (this, args);
    }

    /// <summary>
    ///     Fired when the <see cref="Viewport"/> changes. This event is fired after the <see cref="Viewport"/> has been
    ///     updated.
    /// </summary>
    public event EventHandler<DrawEventArgs>? ViewportChanged;

    /// <summary>
    ///     Called when the <see cref="Viewport"/> changes. Invokes the <see cref="ViewportChanged"/> event.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnViewportChanged (DrawEventArgs e) { }

    /// <summary>
    ///     Converts a <see cref="Viewport"/>-relative location and size to a screen-relative location and size.
    /// </summary>
    /// <remarks>
    ///     Viewport-relative means relative to the top-left corner of the inner rectangle of the <see cref="Padding"/>.
    /// </remarks>
    /// <param name="viewport">Viewport-relative location and size.</param>
    /// <returns>Screen-relative location and size.</returns>
    public Rectangle ViewportToScreen (in Rectangle viewport) => viewport with { Location = ViewportToScreen (viewport.Location) };

    /// <summary>
    ///     Converts a <see cref="Viewport"/>-relative location to a screen-relative location.
    /// </summary>
    /// <remarks>
    ///     Viewport-relative means relative to the top-left corner of the inner rectangle of the <see cref="Padding"/>.
    /// </remarks>
    /// <param name="viewportLocation">Viewport-relative location.</param>
    /// <returns>Screen-relative location.</returns>
    public Point ViewportToScreen (in Point viewportLocation)
    {
        // Translate bounds to Frame (our SuperView's Viewport-relative coordinates)
        Rectangle screen = FrameToScreen ();
        Point viewportOffset = GetViewportOffsetFromFrame ();
        screen.Offset (viewportOffset.X + viewportLocation.X, viewportOffset.Y + viewportLocation.Y);

        return screen.Location;
    }

    /// <summary>
    ///     Gets the Viewport rectangle with a screen-relative location.
    /// </summary>
    /// <returns>Screen-relative location and size.</returns>
    public Rectangle ViewportToScreen ()
    {
        // Translate bounds to Frame (our SuperView's Viewport-relative coordinates)
        Rectangle screen = FrameToScreen ();
        Point viewportOffset = GetViewportOffsetFromFrame ();
        screen.Offset (viewportOffset.X, viewportOffset.Y);

        return screen with { Size = Viewport.Size };
    }

    /// <summary>Converts a screen-relative coordinate to a Viewport-relative coordinate.</summary>
    /// <returns>The coordinate relative to this view's <see cref="Viewport"/>.</returns>
    /// <remarks>
    ///     Viewport-relative means relative to the top-left corner of the inner rectangle of the <see cref="Padding"/>.
    /// </remarks>
    /// <param name="location">Screen-Relative Coordinate.</param>
    /// <returns>Viewport-relative location.</returns>
    public Point ScreenToViewport (in Point location)
    {
        Point viewportOffset = GetViewportOffsetFromFrame ();
        Point frame = ScreenToFrame (location);
        frame.Offset (-viewportOffset.X, -viewportOffset.Y);

        return frame;
    }

    /// <summary>
    ///     Helper to get the X and Y offset of the Viewport from the Frame. This is the sum of the Left and Top properties
    ///     of <see cref="Margin"/>, <see cref="Border"/> and <see cref="Padding"/>.
    /// </summary>
    public Point GetViewportOffsetFromFrame () => Padding.Thickness.GetInside (Padding.GetFrame ()).Location;

    /// <summary>
    ///     Scrolls the view vertically by the specified number of rows.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     </para>
    /// </remarks>
    /// <param name="rows"></param>
    /// <returns><see langword="true"/> if the <see cref="Viewport"/> was changed.</returns>
    public bool? ScrollVertical (int rows)
    {
        if (GetContentSize () == Size.Empty || GetContentHeight () == Viewport.Size.Height)
        {
            return false;
        }

        Viewport = Viewport with { Y = Viewport.Y + rows };

        return true;
    }

    /// <summary>
    ///     Scrolls the view horizontally by the specified number of columns.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     </para>
    /// </remarks>
    /// <param name="cols"></param>
    /// <returns><see langword="true"/> if the <see cref="Viewport"/> was changed.</returns>
    public bool? ScrollHorizontal (int cols)
    {
        if (GetContentSize () == Size.Empty || GetContentWidth () == Viewport.Size.Width)
        {
            return false;
        }

        Viewport = Viewport with { X = Viewport.X + cols };

        return true;
    }

    #endregion Viewport
}

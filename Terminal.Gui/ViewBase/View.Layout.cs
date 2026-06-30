using System.Diagnostics;

#pragma warning disable CS0067 // Event is never used

namespace Terminal.Gui.ViewBase;

public partial class View // Layout APIs
{
    #region Frame/Position/Dimension

    /// <summary>
    ///     Indicates whether the specified SuperView-relative coordinates are within the View's <see cref="Frame"/>.
    /// </summary>
    /// <param name="location">SuperView-relative coordinate</param>
    /// <returns><see langword="true"/> if the specified SuperView-relative coordinates are within the View.</returns>
    public virtual bool Contains (in Point location) => Frame.Contains (location);

    private Rectangle? _frame;

    /// <summary>
    ///     Gets or sets the resolved location and dimension of the view.
    /// </summary>
    /// <value>
    ///     The rectangle describing absolute location and dimension of the view, in coordinates relative to the
    ///     <see cref="SuperView"/>'s Content, which is bound by <see cref="GetContentSize ()"/>.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         <see cref="Frame"/> is typically the output of Terminal.Gui's responsive layout system. To describe layout,
    ///         use <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/>. To inspect the
    ///         resolved result, read <see cref="Frame"/> after layout has resolved those relationships.
    ///     </para>
    ///     <para>
    ///         Frame is relative to the <see cref="SuperView"/>'s Content, which is bound by <see cref="GetContentSize ()"/>
    ///         .
    ///     </para>
    ///     <para>
    ///         Setting Frame will set <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> to
    ///         absolute values.
    ///     </para>
    ///     <para>
    ///         Changing this property will result in <see cref="NeedsLayout"/> and <see cref="NeedsDraw"/> to be set,
    ///         resulting in the
    ///         view being laid out and redrawn as appropriate in the next iteration.
    ///     </para>
    /// </remarks>
    public Rectangle Frame
    {
        get
        {
            if (NeedsLayout)
            {
                //Debug.WriteLine("Frame_get with _layoutNeeded");
            }

            return _frame ?? Rectangle.Empty;
        }
        set
        {
            // This will set _frame, call SetsNeedsLayout, and raise OnViewportChanged/ViewportChanged
            if (!SetFrame (value with { Width = Math.Max (value.Width, 0), Height = Math.Max (value.Height, 0) }))
            {
                return;
            }

            // We set the internal fields directly (not via the X/Y/Width/Height setters) to avoid recursion.
            // This is intentional and the two side effects of the setters are accounted for elsewhere (#5498):
            // - TextFormatter constraints: recomputed by the Layout () -> SetRelativeLayout call below
            //   (SetTextFormatterSize + FinalizeTextFormatterConstraints), so no reset is needed here.
            // - Width/Height (and X/Y) change events: deliberately NOT raised here. Those events observe
            //   declarative Dim/Pos assignment; this path overwrites them with Absolute values as bookkeeping
            //   to keep state consistent with an imperatively-set Frame. Use FrameChanged (raised by SetFrame
            //   above) to observe resolved-size changes from any cause.
            // If Frame gets set, set all Pos/Dim to Absolute values.
            _x = _frame!.Value.X;
            _y = _frame!.Value.Y;
            _width = _frame!.Value.Width;
            _height = _frame!.Value.Height;

            // Explicit layout is ok here because we are setting the Frame directly.
            Layout ();
        }
    }

    /// <summary>
    ///     INTERNAL API - Sets _frame, calls SetsNeedsLayout, and raises OnViewportChanged/ViewportChanged
    /// </summary>
    /// <param name="frame"></param>
    /// <returns><see langword="true"/> if the frame was changed.</returns>
    private bool SetFrame (in Rectangle frame)
    {
        if (_frame == frame)
        {
            return false;
        }

        Rectangle? oldFrame = _frame;
        var oldViewport = Rectangle.Empty;

        if (IsInitialized)
        {
            oldViewport = Viewport;
        }

        // This is the only place where _frame should be set directly. Use Frame = or SetFrame instead.
        _frame = frame;

        // Set the Margin's Frame. All other Adornments use this.
        Margin.View?.Frame = Frame with { Location = Point.Empty };

        SetNeedsDraw ();
        SetNeedsLayout ();

        // Issue #5358: when Frame shrinks or moves, the SuperView's old-frame area is now
        // uncovered and must be cleared on the next draw. Invalidate the union of the old and
        // new frames on the SuperView so its region-aware ClearViewport repaints just that area.
        // SetFrame is the single source of truth for this invalidation for both direct Frame
        // assignment and layout-driven frame updates.
        //
        // Issue #5359: Frames are in SuperView CONTENT coords, but SetNeedsDraw expects
        // SuperView VIEWPORT-LOCAL coords. Translate by subtracting the SuperView's scroll
        // offset (Viewport.Location).
        if (oldFrame is { } prev && SuperView is { })
        {
            Rectangle invalidationContent = Rectangle.Union (prev, frame);
            Point superScroll = SuperView.Viewport.Location;
            Rectangle invalidationViewport = invalidationContent;
            invalidationViewport.Offset (-superScroll.X, -superScroll.Y);
            SuperView.SetNeedsDraw (invalidationViewport);
        }

        // BUGBUG: When SetFrame is called from Frame_set, this event gets raised BEFORE OnResizeNeeded. Is that OK?
        OnFrameChanged (in frame);
        FrameChanged?.Invoke (this, new EventArgs<Rectangle> (in frame));

        if (oldViewport != Viewport)
        {
            RaiseViewportChangedEvent (oldViewport);
        }

        return true;
    }

    /// <summary>
    ///     Called when <see cref="Frame"/> changes.
    /// </summary>
    /// <param name="frame">The new Frame.</param>
    protected virtual void OnFrameChanged (in Rectangle frame) { }

    /// <summary>
    ///     Raised when the <see cref="Frame"/> changes. This event is raised after the <see cref="Frame"/> has been
    ///     updated.
    /// </summary>
    /// <remarks>
    ///     This is the canonical event for observing resolved-size and position changes. It fires whenever the
    ///     absolute <see cref="Frame"/> changes for any reason: assigning <see cref="X"/>, <see cref="Y"/>,
    ///     <see cref="Width"/>, or <see cref="Height"/>; setting <see cref="Frame"/> directly; or a layout pass
    ///     resolving a relative <see cref="Dim"/>/<see cref="Pos"/> (e.g. <see cref="DimFill"/>). By contrast, the
    ///     <see cref="WidthChanged"/>/<see cref="HeightChanged"/> events observe assignment of the declarative
    ///     <see cref="Width"/>/<see cref="Height"/> <see cref="Dim"/> only.
    /// </remarks>
    public event EventHandler<EventArgs<Rectangle>>? FrameChanged;

    /// <summary>Gets the <see cref="Frame"/> with a screen-relative location.</summary>
    /// <returns>The location and size of the view in screen-relative coordinates.</returns>
    public virtual Rectangle FrameToScreen ()
    {
        Rectangle screen = Frame;
        View? current = SuperView;

        while (current is { })
        {
            if (current is AdornmentView adornment)
            {
                // Adornments don't have SuperViews; use Adornment.FrameToScreen override
                // which will give us the screen coordinates of the parent

                Rectangle parentScreen = adornment.FrameToScreen ();

                // Now add our Frame location
                parentScreen.Offset (screen.X, screen.Y);

                return parentScreen with { Size = Frame.Size };
            }

            Point viewportOffset = current.GetViewportOffsetFromFrame ();
            viewportOffset.Offset (current.Frame.X - current.Viewport.X, current.Frame.Y - current.Viewport.Y);
            screen.X += viewportOffset.X;
            screen.Y += viewportOffset.Y;
            current = current.SuperView;
        }

        return screen;
    }

    /// <summary>
    ///     Converts a screen-relative coordinate to a Frame-relative coordinate. Frame-relative means relative to the
    ///     View's <see cref="SuperView"/>'s <see cref="Viewport"/>.
    /// </summary>
    /// <returns>The coordinate relative to the <see cref="SuperView"/>'s <see cref="Viewport"/>.</returns>
    /// <param name="location">Screen-relative coordinate.</param>
    public virtual Point ScreenToFrame (in Point location)
    {
        if (SuperView is null)
        {
            return new Point (location.X - Frame.X, location.Y - Frame.Y);
        }

        Point superViewViewportOffset = SuperView.GetViewportOffsetFromFrame ();
        superViewViewportOffset.Offset (-SuperView.Viewport.X, -SuperView.Viewport.Y);

        Point frame = location;
        frame.Offset (-superViewViewportOffset.X, -superViewViewportOffset.Y);

        frame = SuperView.ScreenToFrame (frame);
        frame.Offset (-Frame.X, -Frame.Y);

        return frame;
    }

    // helper for X, Y, Width, Height setters to ensure consistency
    private void PosDimSet ()
    {
        SetNeedsLayout ();

        if (_x is not PosAbsolute || _y is not PosAbsolute || _width is not DimAbsolute || _height is not DimAbsolute)
        {
            return;
        }

        // Implicit layout is ok here because all Pos/Dim are Absolute values.
        Layout ();

        if (SuperView is { } || this is AdornmentView { Adornment.Parent: null })
        {
            // Ensure the next Application iteration tries to layout again
            SetNeedsLayout ();
        }
    }

    private Pos _x = Pos.Absolute (0);

    /// <summary>
    ///     Gets or sets the declarative horizontal position for the view.
    /// </summary>
    /// <value>The <see cref="Pos"/> object representing the X position.</value>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         To express responsive relationships such as "center this view", "anchor it to the end", or "place it to
    ///         the right of another view", use <see cref="X"/> with <see cref="Pos"/>.
    ///     </para>
    ///     <para>
    ///         The position is relative to the <see cref="SuperView"/>'s Content, which is bound by
    ///         <see cref="GetContentSize ()"/>.
    ///     </para>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="Pos.Center"/>) the value is indeterminate until the view has been
    ///         laid out (e.g. <see cref="Layout(System.Drawing.Size)"/> has been called).
    ///     </para>
    ///     <para>
    ///         Changing this property will result in <see cref="NeedsLayout"/> and <see cref="NeedsDraw"/> to be set,
    ///         resulting in the
    ///         view being laid out and redrawn as appropriate in the next iteration.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated.
    ///     </para>
    ///     <para>The default value is <c>Pos.Absolute (0)</c>.</para>
    /// </remarks>
    public Pos X
    {
        get => _x;
        set
        {
            if (Equals (_x, value))
            {
                return;
            }

            _x = value ?? throw new ArgumentNullException (nameof (value), @$"{nameof (X)} cannot be null");

            PosDimSet ();

            NeedsClearScreenNextIteration ();
        }
    }

    private Pos _y = Pos.Absolute (0);

    /// <summary>
    ///     Gets or sets the declarative vertical position for the view.
    /// </summary>
    /// <value>The <see cref="Pos"/> object representing the Y position.</value>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         To express responsive relationships such as "center this view", "anchor it to the bottom", or "place it
    ///         below another view", use <see cref="Y"/> with <see cref="Pos"/>.
    ///     </para>
    ///     <para>
    ///         The position is relative to the <see cref="SuperView"/>'s Content, which is bound by
    ///         <see cref="GetContentSize ()"/>.
    ///     </para>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="Pos.Center"/>) the value is indeterminate until the view has been
    ///         laid out (e.g. <see cref="Layout(System.Drawing.Size)"/> has been called).
    ///     </para>
    ///     <para>
    ///         Changing this property will result in <see cref="NeedsLayout"/> and <see cref="NeedsDraw"/> to be set,
    ///         resulting in the
    ///         view being laid out and redrawn as appropriate in the next iteration.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated.
    ///     </para>
    ///     <para>The default value is <c>Pos.Absolute (0)</c>.</para>
    /// </remarks>
    public Pos Y
    {
        get => _y;
        set
        {
            if (Equals (_y, value))
            {
                return;
            }

            _y = value ?? throw new ArgumentNullException (nameof (value), @$"{nameof (Y)} cannot be null");
            PosDimSet ();

            NeedsClearScreenNextIteration ();
        }
    }

    private Dim _height = Dim.Absolute (0);

    /// <summary>
    ///     Gets or sets the declarative height for the view.
    /// </summary>
    /// <value>The <see cref="Dim"/> object representing the height of the view (the number of rows).</value>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         To express responsive sizing such as filling remaining space, using a percentage of the available height,
    ///         or growing to fit content with <c>Dim.Auto ()</c>, use <see cref="Height"/> with <see cref="Dim"/>.
    ///     </para>
    ///     <para>
    ///         The dimension is relative to the <see cref="SuperView"/>'s Content, which is bound by
    ///         <see cref="GetContentSize ()"/>         .
    ///     </para>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="DimFill"/>) the value is indeterminate until the view has been
    ///         laid out (e.g. <see cref="Layout(System.Drawing.Size)"/> has been called).
    ///     </para>
    ///     <para>
    ///         Changing this property will result in <see cref="NeedsLayout"/> and <see cref="NeedsDraw"/> to be set,
    ///         resulting in the
    ///         view being laid out and redrawn as appropriate in the next iteration.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated.
    ///     </para>
    ///     <para>
    ///         Setting this property raises pre- and post-change events via <see cref="CWPPropertyHelper"/>,
    ///         allowing customization or cancellation of the change. The <see cref="HeightChanging"/> event
    ///         is raised before the change, and <see cref="HeightChanged"/> is raised after.
    ///     </para>
    ///     <para>
    ///         <see cref="HeightChanging"/>/<see cref="HeightChanged"/> observe assignment of this declarative
    ///         <see cref="Dim"/> only. They are deliberately not raised when a layout pass resolves the view's size
    ///         or when <see cref="Frame"/> is set directly, because those paths change the resolved
    ///         <see cref="Frame"/> rather than the declarative <see cref="Height"/>. To observe resolved-size
    ///         changes from any cause, subscribe to <see cref="FrameChanged"/>.
    ///     </para>
    ///     <para>The default value is <c>Dim.Absolute (0)</c>.</para>
    /// </remarks>
    /// <seealso cref="HeightChanging"/>
    /// <seealso cref="HeightChanged"/>
    public Dim Height
    {
        get => _height;
        set
        {
            CWPPropertyHelper.ChangeProperty (this,
                                              ref _height,
                                              value,
                                              OnHeightChanging,
                                              HeightChanging,
                                              newValue =>
                                              {
                                                  _height = newValue;

                                                  // Reset TextFormatter - Will be recalculated in SetTextFormatterSize
                                                  TextFormatter.ConstrainToHeight = null;
                                                  PosDimSet ();
                                              },
                                              OnHeightChanged,
                                              HeightChanged,
                                              out Dim _);

            NeedsClearScreenNextIteration ();
        }
    }

    /// <summary>
    ///     Called before the <see cref="Height"/> property changes, allowing subclasses to cancel or modify the change.
    /// </summary>
    /// <param name="args">The event arguments containing the current and proposed new height.</param>
    /// <returns>True to cancel the change, false to proceed.</returns>
    protected virtual bool OnHeightChanging (ValueChangingEventArgs<Dim> args) => false;

    /// <summary>
    ///     Called after the <see cref="Height"/> property changes, allowing subclasses to react to the change.
    /// </summary>
    /// <param name="args">The event arguments containing the old and new height.</param>
    protected virtual void OnHeightChanged (ValueChangedEventArgs<Dim> args) { }

    /// <summary>
    ///     Raised before the <see cref="Height"/> property changes, allowing handlers to modify or cancel the change.
    /// </summary>
    /// <remarks>
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to true to cancel the change or modify
    ///     <see cref="ValueChangingEventArgs{T}.NewValue"/> to adjust the proposed value.
    /// </remarks>
    public event EventHandler<ValueChangingEventArgs<Dim>>? HeightChanging;

    /// <summary>
    ///     Raised after the <see cref="Height"/> property changes, allowing handlers to react to the change.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<Dim>>? HeightChanged;

    private Dim _width = Dim.Absolute (0);

    /// <summary>
    ///     Gets or sets the declarative width for the view.
    /// </summary>
    /// <value>The <see cref="Dim"/> object representing the width of the view (the number of columns).</value>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         To express responsive sizing such as filling remaining space, using a percentage of the available width,
    ///         or growing to fit content with <c>Dim.Auto ()</c>, use <see cref="Width"/> with <see cref="Dim"/>.
    ///     </para>
    ///     <para>
    ///         The dimension is relative to the <see cref="SuperView"/>'s Content, which is bound by
    ///         <see cref="GetContentSize ()"/>
    ///         .
    ///     </para>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="DimFill"/>) the value is indeterminate until the view has been
    ///         laid out (e.g. <see cref="Layout(System.Drawing.Size)"/> has been called).
    ///     </para>
    ///     <para>
    ///         Changing this property will result in <see cref="NeedsLayout"/> and <see cref="NeedsDraw"/> to be set,
    ///         resulting in the
    ///         view being laid out and redrawn as appropriate in the next iteration.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated.
    ///     </para>
    ///     <para>
    ///         Setting this property raises pre- and post-change events via <see cref="CWPPropertyHelper"/>,
    ///         allowing customization or cancellation of the change. The <see cref="WidthChanging"/> event
    ///         is raised before the change, and <see cref="WidthChanged"/> is raised after.
    ///     </para>
    ///     <para>
    ///         <see cref="WidthChanging"/>/<see cref="WidthChanged"/> observe assignment of this declarative
    ///         <see cref="Dim"/> only. They are deliberately not raised when a layout pass resolves the view's size
    ///         or when <see cref="Frame"/> is set directly, because those paths change the resolved
    ///         <see cref="Frame"/> rather than the declarative <see cref="Width"/>. To observe resolved-size
    ///         changes from any cause, subscribe to <see cref="FrameChanged"/>.
    ///     </para>
    ///     <para>The default value is <c>Dim.Absolute (0)</c>.</para>
    /// </remarks>
    /// <seealso cref="WidthChanging"/>
    /// <seealso cref="WidthChanged"/>
    public Dim Width
    {
        get => _width;
        set
        {
            CWPPropertyHelper.ChangeProperty (this,
                                              ref _width,
                                              value,
                                              OnWidthChanging,
                                              WidthChanging,
                                              newValue =>
                                              {
                                                  _width = newValue;

                                                  // Reset TextFormatter - Will be recalculated in SetTextFormatterSize
                                                  TextFormatter.ConstrainToWidth = null;
                                                  PosDimSet ();
                                              },
                                              OnWidthChanged,
                                              WidthChanged,
                                              out Dim _);

            NeedsClearScreenNextIteration ();
        }
    }

    private void NeedsClearScreenNextIteration ()
    {
        if (App?.SessionStack?.Select (r => r.Runnable as View).Count () == 1)
        {
            // this is the root IRunnable; we need to redraw the screen
            App?.ClearScreenNextIteration = true;
        }
    }

    /// <summary>
    ///     Called before the <see cref="Width"/> property changes, allowing subclasses to cancel or modify the change.
    /// </summary>
    /// <param name="args">The event arguments containing the current and proposed new width.</param>
    /// <returns>True to cancel the change, false to proceed.</returns>
    protected virtual bool OnWidthChanging (ValueChangingEventArgs<Dim> args) => false;

    /// <summary>
    ///     Called after the <see cref="Width"/> property changes, allowing subclasses to react to the change.
    /// </summary>
    /// <param name="args">The event arguments containing the old and new width.</param>
    protected virtual void OnWidthChanged (ValueChangedEventArgs<Dim> args) { }

    /// <summary>
    ///     Raised before the <see cref="Width"/> property changes, allowing handlers to modify or cancel the change.
    /// </summary>
    /// <remarks>
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to true to cancel the change or modify
    ///     <see cref="ValueChangingEventArgs{T}.NewValue"/> to adjust the proposed value.
    /// </remarks>
    public event EventHandler<ValueChangingEventArgs<Dim>>? WidthChanging;

    /// <summary>
    ///     Raised after the <see cref="Width"/> property changes, allowing handlers to react to the change.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<Dim>>? WidthChanged;

    #endregion Frame/Position/Dimension

    #region Core Layout API

    /// <summary>
    ///     INTERNAL API - Performs layout of the specified views within the specified content size. Called by the Application
    ///     main loop.
    /// </summary>
    /// <param name="views">The views to layout.</param>
    /// <param name="contentSize">The size to bound the views by.</param>
    /// <returns><see langword="true"/>If any of the views needed to be laid out.</returns>
    internal static bool Layout (IEnumerable<View> views, Size contentSize)
    {
        var neededLayout = false;

        foreach (View v in views)
        {
            if (!v.NeedsLayout)
            {
                continue;
            }
            neededLayout = true;
            v.Layout (contentSize);
        }

        return neededLayout;
    }

    /// <summary>
    ///     Resolves the view's declarative layout and then lays out its subviews within the specified content size.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         This method turns responsive <see cref="Pos"/> and <see cref="Dim"/> expressions into an absolute
    ///         <see cref="Frame"/>, then recursively lays out SubViews.
    ///     </para>
    ///     <para>
    ///         This method is intended to be called by the layout engine to prepare the view for layout and is exposed as
    ///         a public API primarily for testing purposes.
    ///     </para>
    /// </remarks>
    /// <param name="contentSize"></param>
    /// <returns><see langword="false"/>If the view could not be laid out (typically because a dependency was not ready). </returns>
    public bool Layout (Size contentSize)
    {
        bool needsDrawAfterLayout = _needsDrawAfterLayout;
        _needsDrawAfterLayout = false;
        Rectangle originalFrame = Frame;

        if (!SetRelativeLayout (contentSize))
        {
            _needsDrawAfterLayout = needsDrawAfterLayout;

            return false;
        }

        bool frameChanged = Frame != originalFrame;
        LayoutSubViews ();

        if (frameChanged || needsDrawAfterLayout)
        {
            // Ancestor-only layout propagation should not force redraw when a peer view is merely
            // recomputed during dependency resolution. Draw after layout only when this view was
            // directly invalidated for layout or its resolved frame actually changed.
            SetNeedsDraw ();

            // NOTE (#5358, review feedback item 4): the SuperView invalidation for frame
            // changes is handled in SetFrame, which is the single source of truth for Frame
            // mutation. Both direct (view.Frame = ...) and layout-driven (SetRelativeLayout
            // → SetFrame) paths go through it, so calling SuperView.SetNeedsDraw(union) here
            // too would be redundant and would do the cascade work twice on the hot path.
        }

        return true;
    }

    /// <summary>
    ///     Resolves the view's declarative layout and then lays out its subviews using the content size of either the
    ///     <see cref="SuperView"/> or <see cref="IApplication.Screen"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         To force a responsive layout pass after changing <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, or
    ///         <see cref="Height"/>, call this overload.
    ///     </para>
    ///     <para>
    ///         This method is intended to be called by the layout engine to prepare the view for layout and is exposed as
    ///         a public API primarily for testing purposes.
    ///     </para>
    /// </remarks>
    /// <returns><see langword="false"/>If the view could not be laid out (typically because dependency was not ready). </returns>
    public bool Layout () => Layout (GetContainerSize ());

    /// <summary>
    ///     Resolves this view's declarative layout into a concrete position and size, relative to the SuperView's
    ///     content size, based on <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is the core step that turns responsive <see cref="Pos"/> and <see cref="Dim"/> expressions
    ///         into the view's absolute <see cref="Frame"/>.
    ///     </para>
    ///     <para>
    ///         If <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, or <see cref="Height"/> are
    ///         absolute, they will be updated to reflect the new size and position of the view. Otherwise, they
    ///         are left unchanged.
    ///     </para>
    ///     <para>
    ///         This method does not arrange subviews or adornments. It is intended to be called by the layout engine to
    ///         prepare the view for layout and is exposed as a public API primarily for testing purposes.
    ///     </para>
    ///     <para>
    ///         Some subviews may have SetRelativeLayout called on them as a side effect, particularly in DimAuto scenarios.
    ///     </para>
    /// </remarks>
    /// <param name="superviewContentSize">
    ///     The size of the SuperView's content (nominally the same as <c>this.SuperView.GetContentSize ()</c>).
    /// </param>
    /// <returns><see langword="true"/> if successful. <see langword="false"/> means a dependent View still needs layout.</returns>
    public bool SetRelativeLayout (Size superviewContentSize)
    {
        Debug.Assert (_x is { });
        Debug.Assert (_y is { });

        CheckDimAuto ();

        // TODO: Should move to View.LayoutSubViews?
        SetTextFormatterSize ();

        if (!TryComputeRelativeFrame (superviewContentSize, out Rectangle newFrame))
        {
            //Debug.WriteLine ($"A Dim/PosFunc function threw (typically this is because a dependent View was not laid out)");
            return false;
        }

        if (Frame != newFrame)
        {
            // Set the frame. Do NOT use `Frame = newFrame` as it overwrites X, Y, Width, and Height
            // SetFrame will set _frame, call SetsNeedsLayout, and raise OnViewportChanged/ViewportChanged
            _suppressNeedsDrawAfterLayout = true;

            try
            {
                SetFrame (newFrame);
            }
            finally
            {
                _suppressNeedsDrawAfterLayout = false;
            }

            // We update the internal fields directly (not via the X/Y/Width/Height setters) to avoid recursion.
            // This is intentional and the two side effects of the setters are accounted for here (#5498):
            // - TextFormatter constraints: already recomputed by SetTextFormatterSize () above and finalized by
            //   FinalizeTextFormatterConstraints () below, so no reset is needed here.
            // - Width/Height (and X/Y) change events: deliberately NOT raised. A layout pass resolves the Frame
            //   without changing the declarative Dim/Pos (e.g. Dim.Fill () stays Dim.Fill ()); these assignments
            //   only collapse an already-Absolute dim onto its resolved value. FrameChanged (raised by SetFrame
            //   above) is the event for resolved-size changes.
            if (_x is PosAbsolute)
            {
                _x = Frame.X;
            }

            if (_y is PosAbsolute)
            {
                _y = Frame.Y;
            }

            if (_width is DimAbsolute)
            {
                _width = Frame.Width;
            }

            if (_height is DimAbsolute)
            {
                _height = Frame.Height;
            }

            if (!string.IsNullOrEmpty (Title))
            {
                SetTitleTextFormatterSize ();
            }

            if (SuperView is { })
            {
                SuperView?.SetNeedsDraw ();
            }
            else
            {
                NeedsClearScreenNextIteration ();
            }
        }

        FinalizeTextFormatterConstraints ();

        return true;
    }

    /// <summary>
    ///     Finalizes <see cref="TextFormatter"/>'s size constraints after the <see cref="Frame"/> has been resolved.
    ///     Any constraint left unset by <see cref="SetTextFormatterSize"/> (e.g. for fixed dimensions whose content
    ///     size was not explicitly set) is filled from the view's content size so wrapped/clipped text formats
    ///     correctly.
    /// </summary>
    /// <remarks>
    ///     Called by <see cref="SetRelativeLayout"/> and by the <see cref="Text"/> setter's same-frame fast path, which
    ///     skips <see cref="SetRelativeLayout"/> but must leave <see cref="TextFormatter"/> in the same state.
    /// </remarks>
    private void FinalizeTextFormatterConstraints ()
    {
        if (TextFormatter.ConstrainToWidth is null)
        {
            TextFormatter.ConstrainToWidth = GetContentWidth ();
        }

        if (TextFormatter.ConstrainToHeight is null)
        {
            TextFormatter.ConstrainToHeight = GetContentHeight ();
        }
    }

    /// <summary>
    ///     Resolves this view's <see cref="Pos"/>/<see cref="Dim"/> expressions into the absolute <see cref="Frame"/>
    ///     they would produce, without applying it. This is the size/position computation
    ///     <see cref="SetRelativeLayout"/> performs and is the single source of truth for both that method and any
    ///     speculative "would the Frame change?" check.
    /// </summary>
    /// <param name="superviewContentSize">The size of the SuperView's content.</param>
    /// <param name="frame">The resolved Frame. Valid only when the method returns <see langword="true"/>.</param>
    /// <returns>
    ///     <see langword="true"/> if the Frame could be resolved; <see langword="false"/> if a dependency was not ready
    ///     (a <see cref="LayoutException"/> was thrown during calculation).
    /// </returns>
    private bool TryComputeRelativeFrame (Size superviewContentSize, out Rectangle frame)
    {
        frame = Rectangle.Empty;

        int newX, newW, newY, newH;

        try
        {
            // Calculate the new X, Y, Width, and Height
            // If the Width or Height is Dim.Auto, calculate the Width or Height first. Otherwise, calculate the X or Y first.
            if (_width.Has<DimAuto> (out _))
            {
                newW = _width.Calculate (0, superviewContentSize.Width, this, Dimension.Width);
                newX = _x.Calculate (superviewContentSize.Width, newW, this, Dimension.Width);

                if (newW != Frame.Width)
                {
                    // Pos.Calculate got us a new position. We need to redo dimension
                    newW = _width.Calculate (newX, superviewContentSize.Width, this, Dimension.Width);
                }
            }
            else
            {
                newX = _x.Calculate (superviewContentSize.Width, _width, this, Dimension.Width);
                newW = _width.Calculate (newX, superviewContentSize.Width, this, Dimension.Width);
            }

            if (_height.Has<DimAuto> (out _))
            {
                newH = _height.Calculate (0, superviewContentSize.Height, this, Dimension.Height);
                newY = _y.Calculate (superviewContentSize.Height, newH, this, Dimension.Height);

                if (newH != Frame.Height)
                {
                    // Pos.Calculate got us a new position. We need to redo dimension
                    newH = _height.Calculate (newY, superviewContentSize.Height, this, Dimension.Height);
                }
            }
            else
            {
                newY = _y.Calculate (superviewContentSize.Height, _height, this, Dimension.Height);
                newH = _height.Calculate (newY, superviewContentSize.Height, this, Dimension.Height);
            }
        }
        catch (LayoutException)
        {
            return false;
        }

        frame = new (newX, newY, newW, newH);

        return true;
    }

    /// <summary>
    ///     INTERNAL API - Causes the view's subviews and adornments to be laid out within the view's content areas. Assumes
    ///     the view's relative layout has been set via <see cref="SetRelativeLayout"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         The position and dimensions of the view are indeterminate until the view has been initialized. Therefore, the
    ///         behavior of this method is indeterminate if <see cref="IsInitialized"/> is <see langword="false"/>.
    ///     </para>
    ///     <para>Raises the <see cref="SubViewsLaidOut"/> event before it returns.</para>
    /// </remarks>
    internal void LayoutSubViews ()
    {
        if (!NeedsLayout)
        {
            return;
        }

        CheckDimAuto ();

        Size contentSize = GetContentSize ();

        OnSubViewLayout (new LayoutEventArgs (contentSize));

        // Re-read content size — OnSubViewLayout handlers (e.g. Dialog.UpdateSizes) may have called SetContentSize.
        contentSize = GetContentSize ();

        SubViewLayout?.Invoke (this, new LayoutEventArgs (contentSize));

        // Re-read again — SubViewLayout event handlers may also have called SetContentSize, and the
        // value captured below is what every SubView is laid out against. See issue #4522 / #4863.
        contentSize = GetContentSize ();

        // The Adornments already have their Frame's set by SetRelativeLayout so we call LayoutSubViews vs. Layout here.
        Margin.View?.LayoutSubViews ();
        Border.View?.LayoutSubViews ();
        Padding.View?.LayoutSubViews ();

        // Sort out the dependencies of the X, Y, Width, Height properties
        HashSet<View> nodes = new ();
        HashSet<(View, View)> edges = new ();
        CollectAll (this, ref nodes, ref edges);
        List<View> ordered = TopologicalSort (SuperView!, nodes, edges);

        List<View> redo = new ();

        foreach (View v in ordered.Snapshot ())
        {
            if (!v.Layout (contentSize))
            {
                redo.Add (v);
            }
        }

        var layoutStillNeeded = false;

        if (redo.Count > 0)
        {
            foreach (View v in ordered)
            {
                if (!v.Layout (contentSize))
                {
                    layoutStillNeeded = true;
                }
            }
        }

        // If the 'to' is rooted to 'from' it's a special-case.
        // Use Layout with the ContentSize of the 'from'.
        // See the Nested_SubViews_Ref_Topmost_SuperView unit test
        if (edges.Count > 0 && GetTopSuperView () is { })
        {
            foreach ((View from, View to) in edges)
            {
                to.Layout (from.GetContentSize ());
            }
        }

        NeedsLayout = layoutStillNeeded;

        OnSubViewsLaidOut (new LayoutEventArgs (contentSize));
        SubViewsLaidOut?.Invoke (this, new LayoutEventArgs (contentSize));
    }

    /// <summary>
    ///     Called from <see cref="LayoutSubViews"/> before any subviews
    ///     have been laid out.
    /// </summary>
    /// <remarks>
    ///     Override to perform tasks when the layout is changing.
    /// </remarks>
    protected virtual void OnSubViewLayout (LayoutEventArgs args) { }

    /// <summary>
    ///     Raised by <see cref="LayoutSubViews"/> before any subviews
    ///     have been laid out.
    /// </summary>
    /// <remarks>
    ///     Subscribe to this event to perform tasks when the layout is changing.
    /// </remarks>
    public event EventHandler<LayoutEventArgs>? SubViewLayout;

    /// <summary>
    ///     Called from <see cref="LayoutSubViews"/> after all sub-views
    ///     have been laid out.
    /// </summary>
    /// <remarks>
    ///     Override to perform tasks after the <see cref="View"/> has been resized or the layout has
    ///     otherwise changed.
    /// </remarks>
    protected virtual void OnSubViewsLaidOut (LayoutEventArgs args)
    { /*Debug.Assert (!NeedsLayout);*/
    }

    /// <summary>Raised after all sub-views have been laid out.</summary>
    /// <remarks>
    ///     Subscribe to this event to perform tasks after the <see cref="View"/> has been resized or the layout has
    ///     otherwise changed.
    /// </remarks>
    public event EventHandler<LayoutEventArgs>? SubViewsLaidOut;

    #endregion Core Layout API

    #region NeedsLayout

    // NeedsLayout is mutated only from within View:
    //   * Set true by SetNeedsLayout and its helpers MarkSubtreeNeedsLayout / MarkAncestorsNeedLayout.
    //   * Cleared by LayoutSubViews once the view has been laid out, and by the Margin fast-path in
    //     View.Drawing.Adornments.
    // The setter is internal (rather than private) only so layout unit tests can establish a known
    // baseline; external code must never assign it directly — call SetNeedsLayout instead. Fully
    // removing the setter is tracked as part of the broader NeedsLayout lifecycle cleanup (issue #4522).

    /// <summary>
    ///     Indicates the View's Frame or the layout of the View's subviews (including Adornments) have
    ///     changed since the last time the View was laid out.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Used to prevent <see cref="Layout()"/> from needlessly computing
    ///         layout.
    ///     </para>
    /// </remarks>
    /// <value>
    ///     <see langword="true"/> if layout is needed.
    /// </value>
    public bool NeedsLayout { get; internal set; } = true;

    private bool _needsDrawAfterLayout = true;
    private bool _suppressNeedsDrawAfterLayout;

    /// <summary>
    ///     Sets <see cref="NeedsLayout"/> to return <see langword="true"/>, indicating this View and all of its subviews
    ///     (including adornments) need to be laid out in the next Application iteration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The next iteration will cause <see cref="Layout()"/> to be called on the next
    ///         <see cref="IApplication.Iteration"/> so there is normally no reason to call see <see cref="Layout()"/>.
    ///     </para>
    /// </remarks>
    public void SetNeedsLayout ()
    {
        if (!_suppressNeedsDrawAfterLayout)
        {
            _needsDrawAfterLayout = true;
        }

        MarkSubtreeNeedsLayout ();

        TextFormatter.NeedsFormat = true;

        if (SuperView is { NeedsLayout: false } superView)
        {
            superView.MarkAncestorsNeedLayout ();
        }

        if (this is AdornmentView adornment && adornment.Adornment?.Parent is { NeedsLayout: false } adornmentParent)
        {
            adornmentParent.MarkAncestorsNeedLayout ();
        }
    }

    // Marks this view and every descendant in its own subtree (including adornment subview
    // trees) as needing layout. Does NOT propagate to SuperView or Adornment.Parent. See
    // <see cref="SetNeedsLayout"/> and issue #5357.
    private void MarkSubtreeNeedsLayout ()
    {
        NeedsLayout = true;
        MarkAdornmentSubViewTrees ();

        // TODO: Optimize this - see Setting_Thickness_Causes_Adornment_SubView_Layout
        // Use a stack to avoid recursion
        Stack<View> stack = new (InternalSubViews.Snapshot ().ToList ());

        while (stack.Count > 0)
        {
            Debug.Assert (stack.Peek () is { });

            View current = stack.Pop ();

            if (current.NeedsLayout)
            {
                continue;
            }

            current.NeedsLayout = true;
            current.MarkAdornmentSubViewTrees ();

            foreach (View subview in current.SubViews)
            {
                stack.Push (subview);
            }
        }
    }

    // Marks this view's Margin/Border/Padding view trees as needing layout when they have
    // subviews. Adornment subview trees live separately from the regular SubViews tree.
    private void MarkAdornmentSubViewTrees ()
    {
        if (Margin.View is { SubViews.Count: > 0 })
        {
            Margin.View.MarkSubtreeNeedsLayout ();
        }

        if (Border.View is { SubViews.Count: > 0 })
        {
            Border.View.MarkSubtreeNeedsLayout ();
        }

        if (Padding.View is { SubViews.Count: > 0 })
        {
            Padding.View.MarkSubtreeNeedsLayout ();
        }
    }

    // Walks up the ancestor chain marking each ancestor as needing layout, without descending
    // into the ancestor's regular SubViews tree or any uninvolved ancestor adornment subview
    // trees. Stops at any ancestor already marked, mirroring the early-exit guards in the
    // original recursive call. See issue #5357.
    private void MarkAncestorsNeedLayout ()
    {
        if (NeedsLayout)
        {
            return;
        }

        NeedsLayout = true;
        TextFormatter.NeedsFormat = true;

        if (SuperView is { NeedsLayout: false } superView)
        {
            superView.MarkAncestorsNeedLayout ();
        }

        if (this is AdornmentView adornment && adornment.Adornment?.Parent is { NeedsLayout: false } adornmentParent)
        {
            adornmentParent.MarkAncestorsNeedLayout ();
        }
    }

    #endregion NeedsLayout

    #region Topological Sort

    /// <summary>
    ///     INTERNAL API - Collects all views and their dependencies from a given starting view for layout purposes. Used by
    ///     <see cref="TopologicalSort"/> to create an ordered list of views to layout.
    /// </summary>
    /// <param name="from">The starting view from which to collect dependencies.</param>
    /// <param name="nNodes">A reference to a set of views representing nodes in the layout graph.</param>
    /// <param name="nEdges">
    ///     A reference to a set of tuples representing edges in the layout graph, where each tuple consists of a pair of views
    ///     indicating a dependency.
    /// </param>
    internal void CollectAll (View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
    {
        foreach (View v in from.InternalSubViews)
        {
            nNodes.Add (v);
            CollectPos (v.X, v, ref nNodes, ref nEdges);
            CollectPos (v.Y, v, ref nNodes, ref nEdges);
            CollectDim (v.Width, v, ref nNodes, ref nEdges);
            CollectDim (v.Height, v, ref nNodes, ref nEdges);
        }
    }

    /// <summary>
    ///     INTERNAL API - Collects dimension (where Width or Height is `DimView`) dependencies for a given view.
    /// </summary>
    /// <param name="dim">The dimension (width or height) to collect dependencies for.</param>
    /// <param name="from">The view for which to collect dimension dependencies.</param>
    /// <param name="nNodes">A reference to a set of views representing nodes in the layout graph.</param>
    /// <param name="nEdges">
    ///     A reference to a set of tuples representing edges in the layout graph, where each tuple consists of a pair of views
    ///     indicating a dependency.
    /// </param>
    internal void CollectDim (Dim dim, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
    {
        foreach (View target in dim.GetReferencedViews ().Where (v => v != this))
        {
            nEdges.Add ((target, from));
        }
    }

    /// <summary>
    ///     INTERNAL API - Collects position (where X or Y is `PosView`) dependencies for a given view.
    /// </summary>
    /// <param name="pos">The position (X or Y) to collect dependencies for.</param>
    /// <param name="from">The view for which to collect position dependencies.</param>
    /// <param name="nNodes">A reference to a set of views representing nodes in the layout graph.</param>
    /// <param name="nEdges">
    ///     A reference to a set of tuples representing edges in the layout graph, where each tuple consists of a pair of views
    ///     indicating a dependency.
    /// </param>
    internal void CollectPos (Pos pos, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
    {
        foreach (View target in pos.GetReferencedViews ().Where (v => v != this))
        {
            nEdges.Add ((target, from));
        }
    }

    // https://en.wikipedia.org/wiki/Topological_sorting
    internal static List<View> TopologicalSort (View superView, IEnumerable<View> nodes, ICollection<(View From, View To)> edges)
    {
        List<View> result = new ();

        // Set of all nodes with no incoming edges
        HashSet<View> noEdgeNodes = new (nodes.Where (n => edges.All (e => !e.To.Equals (n))));

        while (noEdgeNodes.Any ())
        {
            //  remove a node n from S
            View n = noEdgeNodes.First ();
            noEdgeNodes.Remove (n);

            // add n to tail of L
            if (n != superView)
            {
                result.Add (n);
            }

            // for each node m with an edge e from n to m do
            foreach ((View From, View To) e in edges.Where (e => e.From.Equals (n)).ToArray ())
            {
                View m = e.To;

                // remove edge e from the graph
                edges.Remove (e);

                // if m has no other incoming edges then
                if (edges.All (me => !me.To.Equals (m)) && m != superView)
                {
                    // insert m into S
                    noEdgeNodes.Add (m);
                }
            }
        }

        if (!edges.Any ())
        {
            return result;
        }

        foreach ((View from, View to) in edges)
        {
            if (from == to)
            {
                // if not yet added to the result, add it and remove from edge
                if (result.Find (v => v == from) is null)
                {
                    result.Add (from);
                }

                edges.Remove ((from, to));
            }
            else if (from.SuperView == to.SuperView)
            {
                // if 'from' is not yet added to the result, add it
                if (result.Find (v => v == from) is null)
                {
                    result.Add (from);
                }

                // if 'to' is not yet added to the result, add it
                if (result.Find (v => v == to) is null)
                {
                    result.Add (to);
                }

                // remove from edge
                edges.Remove ((from, to));
            }
            else if (from != superView?.GetTopSuperView (to, from) && !ReferenceEquals (from, to))
            {
                if (ReferenceEquals (from.SuperView, to))
                {
                    throw new LayoutException ($"ComputedLayout for \"{superView}\": \"{to}\" " + $"references a SubView (\"{from}\").");
                }

                throw new LayoutException ($"ComputedLayout for \"{superView}\": \"{from}\" "
                                           + $"linked with \"{to}\" was not found. Did you forget to add it to {superView}?");
            }
        }

        // return L (a topologically sorted order)
        return result;
    } // TopologicalSort

    #endregion Topological Sort

    #region Utilities

    /// <summary>
    ///     Gets the size of the SuperView's content (nominally the same as
    ///     the SuperView's <see cref="GetContentSize ()"/>) or the screen size if there's no SuperView.
    /// </summary>
    /// <remarks>
    ///     This method provides fallback logic to ensure that a size is always returned, even if the SuperView is not set or
    ///     not initialized. The order of precedence is:
    ///     1. SuperView's content size (if SuperView is set and initialized)
    ///     2. TopRunnableView's content size (if TopRunnableView is set, not the current view, and initialized)
    ///     3. Application's screen size (if Application is set)
    ///     4. Driver's screen size (if Driver is set)
    ///     5. Fallback to a default size of 2048x2048
    /// </remarks>
    public Size GetContainerSize ()
    {
        // TODO: Get rid of refs to Top
        Size superViewContentSize = SuperView?.GetContentSize ()
                                    ?? (App?.TopRunnableView is { } && App?.TopRunnableView != this && App!.TopRunnableView.IsInitialized
                                            ? App.TopRunnableView.GetContentSize ()
                                            : App?.Screen.Size ?? Driver?.Screen.Size ?? new Size (2048, 2048));

        return superViewContentSize;
    }

    // BUGBUG: This method interferes with Dialog/MessageBox default min/max size.
    // TODO: Refactor / rewrite this - It's a mess
    /// <summary>
    ///     Gets a new location of the <see cref="View"/> that is within the Viewport of the <paramref name="viewToMove"/>'s
    ///     <see cref="View.SuperView"/> (e.g. for dragging a Window). The `out` parameters are the new X and Y coordinates.
    /// </summary>
    /// <remarks>
    ///     If <paramref name="viewToMove"/> does not have a <see cref="View.SuperView"/> or it's SuperView is not
    ///     <see cref="IApplication.TopRunnableView"/> the position will be bound by  <see cref="IApplication.Screen"/>.
    /// </remarks>
    /// <param name="viewToMove">The View that is to be moved.</param>
    /// <param name="targetX">The target x location.</param>
    /// <param name="targetY">The target y location.</param>
    /// <param name="nx">The new x location that will ensure <paramref name="viewToMove"/> will be fully visible.</param>
    /// <param name="ny">The new y location that will ensure <paramref name="viewToMove"/> will be fully visible.</param>
    /// <returns>
    ///     Either <see cref="IApplication.TopRunnableView"/> (if <paramref name="viewToMove"/> does not have a Super View) or
    ///     <paramref name="viewToMove"/>'s SuperView. This can be used to ensure LayoutSubViews is called on the correct View.
    /// </returns>
    internal static View? GetLocationEnsuringFullVisibility (View viewToMove, int targetX, int targetY, out int nx, out int ny)
    {
        int maxDimension;
        View? superView;

        IApplication? app = viewToMove.App;

        if (viewToMove.SuperView is null || viewToMove == app?.TopRunnableView || viewToMove.SuperView == app?.TopRunnableView)
        {
            maxDimension = app?.Screen.Width ?? 0;
            superView = app?.TopRunnableView;
        }
        else
        {
            // Use the SuperView's Viewport, not Frame
            maxDimension = viewToMove.SuperView.Viewport.Width;
            superView = viewToMove.SuperView;
        }

        if (superView?.Margin is { } && superView == viewToMove.SuperView)
        {
            maxDimension -= superView.GetAdornmentsThickness ().Left + superView.GetAdornmentsThickness ().Right;
        }

        if (viewToMove.Frame.Width <= maxDimension)
        {
            nx = Math.Max (targetX, 0);
            nx = nx + viewToMove.Frame.Width > maxDimension ? Math.Max (maxDimension - viewToMove.Frame.Width, 0) : nx;
        }
        else
        {
            nx = 0; //targetX;
        }

        maxDimension = 0;

        ny = Math.Max (targetY, maxDimension);

        if (viewToMove.SuperView is null || viewToMove == app?.TopRunnableView || viewToMove.SuperView == app?.TopRunnableView)
        {
            maxDimension = app is { } ? app.Screen.Height : 0;
        }
        else
        {
            maxDimension = viewToMove.SuperView.Viewport.Height;
        }

        if (superView?.Margin is { } && superView == viewToMove.SuperView)
        {
            maxDimension -= superView.GetAdornmentsThickness ().Top + superView.GetAdornmentsThickness ().Bottom;
        }

        ny = Math.Min (ny, maxDimension);

        if (viewToMove.Frame.Height <= maxDimension)
        {
            ny = ny + viewToMove.Frame.Height > maxDimension ? Math.Max (maxDimension - viewToMove.Frame.Height, 0) : ny;
        }
        else
        {
            ny = 0;
        }

        return superView;
    }

    /// <summary>
    ///     Gets the Views that are under <paramref name="screenLocation"/>, including Adornments. The list is ordered by
    ///     depth. The
    ///     deepest
    ///     View is at the end of the list (the top most View is at element 0).
    /// </summary>
    /// <param name="screenLocation">Screen-relative location.</param>
    /// <param name="excludeViewportSettingsFlags">
    ///     If set, excludes Views that have the <see cref="ViewportSettingsFlags.Transparent"/> or
    ///     <see cref="ViewportSettingsFlags.TransparentMouse"/>
    ///     flags set in their ViewportSettings.
    /// </param>
    public List<View?> GetViewsUnderLocation (in Point screenLocation, ViewportSettingsFlags excludeViewportSettingsFlags)
    {
        // PopoverHost - If visible, start with it instead of Top
        if (App?.Popovers?.GetActivePopover () is View { Visible: true } visiblePopover)
        {
            // BUGBUG: We do not traverse all visible runnables if there's an active popover. This may be a bug.
            List<View?> result = [];

            result.AddRange (GetViewsUnderLocation (visiblePopover, screenLocation, excludeViewportSettingsFlags));

            if (result.Count > 0)
            {
                return result;
            }
        }

        var checkedTop = false;

        // Traverse all visible runnables, topmost first (reverse stack order)
        if (App?.SessionStack!.Count > 0)
        {
            foreach (View? runnable in App.SessionStack!.Select (r => r.Runnable as View))
            {
                if (runnable!.Visible && runnable.Contains (screenLocation))
                {
                    List<View?> result = GetViewsUnderLocation (runnable, screenLocation, excludeViewportSettingsFlags);

                    // Only return if the result is not empty
                    if (result.Count > 0)
                    {
                        return result;
                    }
                }

                if (runnable == App.TopRunnableView)
                {
                    checkedTop = true;
                }
            }
        }

        // Fallback: If Runnables is empty or Top is not in Runnables, check Top directly (for test compatibility)
        if (checkedTop || App?.TopRunnableView is not { Visible: true } top)
        {
            return [];
        }

        // For root runnables, allow hit-testing even if location is outside bounds (for drag/move)
        List<View?> res = GetViewsUnderLocation (top, screenLocation, excludeViewportSettingsFlags);

        if (res.Count > 0)
        {
            return res;
        }

        return [];
    }

    /// <summary>
    ///     INTERNAL: Helper for GetViewsUnderLocation that starts from a given root view.
    ///     Gets the Views that are under <paramref name="screenLocation"/>, including Adornments. The list is ordered by
    ///     depth. The
    ///     deepest
    ///     View is at the end of the list (the topmost View is at element 0).
    /// </summary>
    /// <param name="root"></param>
    /// <param name="screenLocation">Screen-relative location.</param>
    /// <param name="excludeViewportSettingsFlags">
    ///     If set, excludes Views that have the <see cref="ViewportSettingsFlags.Transparent"/> or
    ///     <see cref="ViewportSettingsFlags.TransparentMouse"/>
    ///     flags set in their ViewportSettings.
    /// </param>
    internal static List<View?> GetViewsUnderLocation (View root, in Point screenLocation, ViewportSettingsFlags excludeViewportSettingsFlags)
    {
        List<View?> viewsUnderLocation = GetViewsAtLocation (root, screenLocation);

        if (!excludeViewportSettingsFlags.FastHasFlags (ViewportSettingsFlags.Transparent)
            && !excludeViewportSettingsFlags.FastHasFlags (ViewportSettingsFlags.TransparentMouse))
        {
            // Only filter views if we are excluding transparent views.
            return viewsUnderLocation;
        }

        // Capture to allow use inside lambda (in parameters can't be used in lambdas).
        Point location = screenLocation;

        // Remove all views that have an adornment with ViewportSettings.TransparentMouse; they are in the list
        // because the point was in their adornment, and if the adornment is transparent, they should be removed.
        // Per-cell check: if the adornment has a CachedDrawnRegion, only remove if the point is NOT in the
        // drawn region (the click is on an undrawn cell). If no cache, fall back to blanket removal.
        viewsUnderLocation.RemoveAll (v =>
                                      {
                                          if (v is null or AdornmentView)
                                          {
                                              return false;
                                          }

                                          bool? ret = null;

                                          if (v.Margin.ViewportSettings.HasFlag (excludeViewportSettingsFlags)
                                              && (viewsUnderLocation.Contains (v.Margin.View)
                                                  || (v.Margin.View is null
                                                      && v.Margin.Thickness != Thickness.Empty
                                                      && v.Margin.Thickness.Contains (v.Margin.FrameToScreen (), location))))
                                          {
                                              if (v.Margin.CachedDrawnRegion is null || !v.Margin.CachedDrawnRegion.Contains (location.X, location.Y))
                                              {
                                                  ret = true;
                                              }
                                          }

                                          if (v.Border.ViewportSettings.HasFlag (excludeViewportSettingsFlags)
                                              && (viewsUnderLocation.Contains (v.Border.View)
                                                  || (v.Border.View is null
                                                      && v.Border.Thickness != Thickness.Empty
                                                      && v.Border.Thickness.Contains (v.Border.FrameToScreen (), location))))
                                          {
                                              if (v.Border.CachedDrawnRegion is null || !v.Border.CachedDrawnRegion.Contains (location.X, location.Y))
                                              {
                                                  ret = true;
                                              }
                                          }

                                          if (v.Padding.ViewportSettings.HasFlag (excludeViewportSettingsFlags)
                                              && (viewsUnderLocation.Contains (v.Padding.View)
                                                  || (v.Padding.View is null
                                                      && v.Padding.Thickness != Thickness.Empty
                                                      && v.Padding.Thickness.Contains (v.Padding.FrameToScreen (), location))))
                                          {
                                              if (v.Padding.CachedDrawnRegion is null || !v.Padding.CachedDrawnRegion.Contains (location.X, location.Y))
                                              {
                                                  ret = true;
                                              }
                                          }

                                          return ret is true;
                                      });

        // Now remove all views that have ViewportSettings.TransparentMouse set.
        // Per-cell check: if a CachedDrawnRegion exists, only remove if the point is NOT in the drawn region.
        // If no cache (before first draw), fall back to blanket removal.
        viewsUnderLocation.RemoveAll (v =>
                                      {
                                          if (!v!.ViewportSettings.HasFlag (excludeViewportSettingsFlags))
                                          {
                                              return false;
                                          }

                                          if (v.CachedDrawnRegion is { } drawnRegion)
                                          {
                                              return !drawnRegion.Contains (location.X, location.Y);
                                          }

                                          return true;
                                      });

        return viewsUnderLocation;
    }

    /// <summary>
    ///     INTERNAL: Gets ALL Views (Subviews and Adornments) in the of <see cref="SuperView"/> hierarchcy that are at
    ///     <paramref name="location"/>,
    ///     regardless of whether they will be drawn or see mouse events or not. Views with <see cref="Visible"/> set to
    ///     <see langword="false"/> or <see cref="Enabled"/> set to <see langword="false"/> will not be included.
    ///     The list is ordered by depth. The deepest View is at the end of the list (the topmost View is at element 0).
    /// </summary>
    /// <param name="superView">The root view from which the search for subviews begins.</param>
    /// <param name="location">The screen-relative location where the search for views is focused.</param>
    /// <returns>A list of views that are located under the specified point.</returns>
    internal static List<View?> GetViewsAtLocation (View? superView, in Point location)
    {
        if (superView is null || !superView.Visible || !superView.Enabled)
        {
            return [];
        }

        List<View?> result = [];
        Stack<View> viewsToProcess = new ();

        // Start with the superview if it contains the location
        if (superView.FrameToScreen ().Contains (location))
        {
            viewsToProcess.Push (superView);
        }

        while (viewsToProcess.Count > 0)
        {
            View currentView = viewsToProcess.Pop ();

            // Add the current view to the result
            result.Add (currentView);

            // Push adornments onto the stack BEFORE subviews so adornments' subviews are processed AFTER regular subviews
            // This ensures that adornment subviews (e.g., ExpanderButton in Border) are considered "deeper" than
            // regular subviews' adornments (e.g., childView.Border) when they overlap.
            // Push in reverse order (Padding, Border, Margin) so they're processed in correct order (Margin, Border, Padding)
            Point superViewRelativeLocation = currentView.SuperView?.ScreenToViewport (location) ?? location;

            if (currentView.Padding.Thickness != Thickness.Empty)
            {
                if (currentView.Padding.Contains (superViewRelativeLocation) && currentView.Padding.FrameToScreen ().Contains (location))
                {
                    if (currentView.Padding.View is { } pv)
                    {
                        viewsToProcess.Push (pv);
                    }
                }
            }

            if (currentView.Border.Thickness != Thickness.Empty)
            {
                if (currentView.Border.Contains (superViewRelativeLocation) && currentView.Border.FrameToScreen ().Contains (location))
                {
                    if (currentView.Border.View is { } bv)
                    {
                        viewsToProcess.Push (bv);
                    }
                }
            }

            if (currentView.Margin.Thickness != Thickness.Empty)
            {
                if (currentView.Margin.Contains (superViewRelativeLocation) && currentView.Margin.FrameToScreen ().Contains (location))
                {
                    if (currentView.Margin.View is { } mv)
                    {
                        viewsToProcess.Push (mv);
                    }
                }
            }

            // Add subviews to the stack in reverse order
            // This maintains the original depth-first traversal order
            for (int i = currentView.InternalSubViews.Count - 1; i >= 0; i--)
            {
                View subview = currentView.InternalSubViews [i];

                if (subview is { Visible: true, Enabled: true } && subview.FrameToScreen ().Contains (location))
                {
                    viewsToProcess.Push (subview);
                }
            }
        }

        return result;
    }

    #endregion Utilities

    #region Diagnostics and Verification

    /// <summary>Gets or sets whether validation of <see cref="Pos"/> and <see cref="Dim"/> occurs.</summary>
    /// <remarks>
    ///     Setting this to <see langword="true"/> will enable validation of <see cref="X"/>, <see cref="Y"/>,
    ///     <see cref="Width"/>, and <see cref="Height"/> during set operations and in <see cref="LayoutSubViews"/>. If invalid
    ///     settings are discovered exceptions will be thrown indicating the error. This will impose a performance penalty and
    ///     thus should only be used for debugging.
    /// </remarks>
    public bool ValidatePosDim { get; set; }

    // TODO: Move this logic into the Pos/Dim classes
    /// <summary>
    ///     Throws an <see cref="InvalidOperationException"/> if any SubViews are using Dim objects that depend on this
    ///     Views dimensions.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void CheckDimAuto ()
    {
        if (!ValidatePosDim || !IsInitialized)
        {
            return;
        }

        var widthAuto = Width as DimAuto;
        var heightAuto = Height as DimAuto;

        // Verify none of the subviews are using Dim objects that depend on the SuperView's dimensions.
        foreach (View view in SubViews)
        {
            if (widthAuto is { } && widthAuto.Style.FastHasFlags (DimAutoStyle.Content) && ContentSizeTracksViewport)
            {
                ThrowInvalid (view, view.Width, nameof (view.Width));
                ThrowInvalid (view, view.X, nameof (view.X));
            }

            if (heightAuto is null || !heightAuto.Style.FastHasFlags (DimAutoStyle.Content) || !ContentSizeTracksViewport)
            {
                continue;
            }
            ThrowInvalid (view, view.Height, nameof (view.Height));
            ThrowInvalid (view, view.Y, nameof (view.Y));
        }

        return;

        static void ThrowInvalid (View view, object? checkPosDim, string name)
        {
            object? bad = null;

            switch (checkPosDim)
            {
                case PosAnchorEnd:
                    break;

                case Pos pos and not PosAbsolute and not PosView and not PosCombine:
                    bad = pos;

                    break;

                case Pos pos and PosCombine:
                    // Recursively check for not Absolute or not View
                    ThrowInvalid (view, (pos as PosCombine)?.Left, name);
                    ThrowInvalid (view, (pos as PosCombine)?.Right, name);

                    break;

                case DimAuto:
                    break;

                case DimFill:
                    break;

                case Dim dim and not DimAbsolute and not DimView and not DimCombine:
                    bad = dim;

                    break;

                case Dim dim and DimCombine:
                    // Recursively check for not Absolute or not View
                    ThrowInvalid (view, (dim as DimCombine)?.Left, name);
                    ThrowInvalid (view, (dim as DimCombine)?.Right, name);

                    break;
            }

            if (bad != null)
            {
                throw new LayoutException ($"{view.GetType ().Name}.{name} = {bad.GetType ().Name} "
                                           + $"which depends on the SuperView's dimensions and the SuperView uses Dim.Auto.");
            }
        }
    }

    #endregion Diagnostics and Verification
}

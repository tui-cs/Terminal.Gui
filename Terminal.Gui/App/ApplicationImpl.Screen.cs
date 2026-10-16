using System.Diagnostics;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    /// <inheritdoc/>
    public event EventHandler<EventArgs<Rectangle>>? ScreenChanged;

    /// <summary>
    ///     Backing field for <see cref="Screen"/> in inline mode. When set, <see cref="Screen"/>
    ///     returns this instead of <see cref="IDriver.Screen"/>. In fullscreen mode this is <see langword="null"/>.
    /// </summary>
    private Rectangle? _screen;

    /// <inheritdoc/>
    public Rectangle Screen
    {
        get => _screen ?? Driver?.Screen ?? new Rectangle (new Point (0, 0), new Size (2048, 2048));
        set
        {
            if (AppModel == AppModel.Inline)
            {
                // Inline mode: store the sub-rectangle independently.
                // Resize the output buffer to match the inline region dimensions.
                _screen = value;
                (Driver as DriverImpl)?.ResizeOutputBuffer (value.Width, value.Height);

                RaiseScreenChangedEvent (value);

                return;
            }

            _screen = null;

            if (Driver is null)
            {
                RaiseScreenChangedEvent (Screen);

                return;
            }

            // Fullscreen: sync with Driver.Screen (resizes both terminal tracking and buffer).
            // Driver_SizeChanged will raise ScreenChanged and invalidate layout.
            Driver.SetScreenSize (value.Width, value.Height);
        }
    }

    /// <inheritdoc/>
    public bool ClearScreenNextIteration { get; set; }

    /// <summary>
    ///     Tracks whether the inline-mode Screen has been sized from the first layout pass.
    /// </summary>
    private bool _inlineScreenSized;

    /// <summary>
    ///     INTERNAL: Called when the application's screen has changed.
    ///     Raises the <see cref="ScreenChanged"/> event.
    /// </summary>
    /// <param name="screen">The new screen size and position.</param>
    private void RaiseScreenChangedEvent (Rectangle screen)
    {
        //Screen = new (Point.Empty, screen.Size);

        ScreenChanged?.Invoke (this, new EventArgs<Rectangle> (screen));

        if (SessionStack is null || !Initialized || _dispatchStopping)
        {
            return;
        }
        foreach (SessionToken t in SessionStack)
        {
            if (t.Runnable is View runnableView)
            {
                runnableView.SetNeedsLayout ();
            }
        }

        if (Popovers?.GetActivePopover () is View { Visible: true } visiblePopover)
        {
            visiblePopover.SetNeedsLayout ();
        }
    }

    private void Driver_SizeChanged (object? sender, SizeChangedEventArgs e)
    {
        if (InlinePlacementLost)
        {
            return;
        }
        Size newSize = e.Size ?? Size.Empty;

        if (AppModel == AppModel.Inline && _screen is { } screen)
        {
            // On resize in inline mode, reset to row 0 and clear the screen.
            // The next LayoutAndDraw will re-size the inline region from scratch.
            _inlineScreenSized = false;

            if (Driver is { } driver)
            {
                driver.InlinePosition = Point.Empty;

                // Clear the entire terminal
                if (!WriteInlineMovement ($"{EscSeqUtils.CSI}H{EscSeqUtils.CSI}2J"))
                {
                    return;
                }
            }

            Screen = new Rectangle (0, 0, newSize.Width, screen.Height);
            ClearScreenNextIteration = true;
        }
        else
        {
            RaiseScreenChangedEvent (new Rectangle (new Point (0, 0), newSize));
        }
    }

    /// <inheritdoc/>
    public void LayoutAndDraw (bool forceRedraw = false)
    {
        if (Coordinator is IInlineRenderCoordinator coordinator)
        {
            coordinator.WithRenderGate (() => LayoutAndDrawGuarded (forceRedraw));
            return;
        }
        LayoutAndDrawGuarded (forceRedraw);
    }

    private void LayoutAndDrawGuarded (bool forceRedraw)
    {
        if (InlinePlacementLost)
        {
            return;
        }
        SessionToken? session = SessionStack?.TryPeek (out SessionToken? top) == true ? top : null;
        if (_layoutAndDrawActive && (session is null || _activeLayoutSessions.Contains (session)))
        {
            return;
        }
        if (AppModel == AppModel.Inline && Driver is { } inlineDriver)
        {
            if (inlineDriver.Screen.Width <= 0 || inlineDriver.Screen.Height <= 0
                || inlineDriver.AnsiStartupGate is { IsReady: false } && !ForceInlinePosition.HasValue)
            {
                return;
            }
            if (!_inlineOriginEstablished)
            {
                _inlineOriginEstablished = true;
                inlineDriver.InlinePosition = ForceInlinePosition ?? new Point (0, (Coordinator as IInlineRenderCoordinator)?.InitialCursorRow ?? 0);
            }
        }
        bool previousLayoutAndDrawActive = _layoutAndDrawActive;
        _layoutAndDrawActive = true;
        if (session is not null)
        {
            _activeLayoutSessions.Add (session);
        }
        // A nested modal may draw, but it must not drain another request from the outer pass.
        InlineOutputOperation? candidate = previousLayoutAndDrawActive ? null : CaptureInlineOutput ();
        try
        {
            LayoutAndDrawCore (forceRedraw, candidate);
        }
        finally
        {
            if (session is not null)
            {
                _activeLayoutSessions.Remove (session);
            }
            _layoutAndDrawActive = previousLayoutAndDrawActive;
        }
    }

    private void LayoutAndDrawCore (bool forceRedraw, InlineOutputOperation? candidate)
    {
        Tracing.Trace.Draw ("ApplicationImpl", "Start", $"forceRedraw={forceRedraw}, Screen={Screen}, _inlineScreenSized={_inlineScreenSized}");

        if (ClearScreenNextIteration)
        {
            forceRedraw = true;
            ClearScreenNextIteration = false;
        }

        if (forceRedraw)
        {
            Driver?.ClearContents ();
        }

        if (SessionStack is null)
        {
            return;
        }

        IDriver? passDriver = Driver;
        long passGeneration = _inlineOutputGeneration;
        bool PassIsValid () => Initialized && !_dispatchStopping && Driver == passDriver && passGeneration == _inlineOutputGeneration && !InlinePlacementLost && HasRunningSession;

        // A session ended beneath the top stays on the stack without a runnable until the sessions above it end.
        List<View?> views = [.. SessionStack.Select (r => r.Runnable).OfType<View> ()];

        if (Popovers?.GetActivePopover () is View { Visible: true, NeedsLayout: true } visiblePopoverNeedingLayout)
        {
            views.Insert (0, visiblePopoverNeedingLayout);
        }

        // Layout

        // Inline mode: on the first draw, position the inline region at the cursor's starting
        // terminal row. If the view doesn't fit, scroll the terminal. Then set App.Screen to
        // the sub-rectangle representing the inline region.
        if (AppModel == AppModel.Inline && !_inlineScreenSized && Driver is { })
        {
            // First layout pass uses full terminal to determine the view's desired height.
            View.Layout (views.ToArray ().Reverse ()!, Screen.Size);
            if (!PassIsValid ())
            {
                return;
            }

            View? topView = views.LastOrDefault (v => v is { });
            int viewHeight = topView?.Frame.Height ?? 0;

            int cursorRow = Driver.InlinePosition.Y;
            int termHeight = Driver.Screen.Height;
            int termWidth = Driver.Screen.Width;

            if (viewHeight > 0)
            {
                // If the view doesn't fit below the cursor, scroll the terminal up
                int overflow = cursorRow + viewHeight - termHeight;

                if (overflow > 0)
                {
                    if (!WriteInlineMovement (new string ('\n', overflow)))
                    {
                        return;
                    }
                    if (!WriteInlineMovement ($"{EscSeqUtils.CSI}{overflow}A"))
                    {
                        return;
                    }
                    cursorRow = Math.Max (0, cursorRow - overflow);
                }

            }

            int availableHeight = Math.Min (viewHeight, termHeight - cursorRow);

            if (availableHeight > 0)
            {
                // Set App.Screen to the inline sub-rectangle. This resizes the output buffer
                // to the inline region dimensions and stores the Y offset for cursor positioning.
                Screen = new Rectangle (0, cursorRow, termWidth, availableHeight);
                if (!PassIsValid ())
                {
                    return;
                }
            }

            // A zero-height runnable has no owned terminal region; retry sizing after it grows.
            _inlineScreenSized = availableHeight > 0;
        }

        // Inline mode: after initial setup, handle dynamic growth. If the view's desired height
        // exceeds Screen.Height, grow App.Screen. Grow DOWN first (into empty terminal rows
        // below the inline region), then UP (scrolling the terminal) only when there's no room below.
        if (AppModel == AppModel.Inline && _inlineScreenSized && Driver is { })
        {
            // Layout with current Screen to get desired heights from Dim.Auto views.
            View.Layout (views.ToArray ().Reverse ()!, Screen.Size);
            if (!PassIsValid ())
            {
                return;
            }

            // Reserve every visible active runnable's frame bottom, but at least its height: bottom-anchored
            // views can extend above the current reservation while waiting for it to grow.
            // Cap the reservation at the terminal height — the inline region can never exceed it.
            int viewHeight = 0;
            foreach (View runnable in SessionStack.Select (token => token.Runnable).OfType<View> ().Where (view => view.Visible))
            {
                long requiredHeight = GetRequiredInlineHeight (runnable);
                viewHeight = Math.Max (viewHeight, (int)Math.Clamp (requiredHeight, 0, Driver.Screen.Height));
            }

            if (viewHeight > Screen.Height)
            {
                int extraRows = viewHeight - Screen.Height;
                int termHeight = Driver.Screen.Height;

                // First, grow down: use empty rows below the current inline region.
                int bottomEdge = Screen.Y + Screen.Height;
                int canGrowDown = Math.Min (extraRows, termHeight - bottomEdge);

                // Start at the last owned row, not at the live caret. A zero-height region has no last row;
                // its new rows are already available below the origin and need no cursor movement.
                if (canGrowDown > 0 && Screen.Height > 0)
                {
                    if (!WriteInlineMovement (EscSeqUtils.CSI_SetCursorPosition (bottomEdge, 1) + new string ('\n', canGrowDown)))
                    {
                        return;
                    }
                    if (!WriteInlineMovement ($"{EscSeqUtils.CSI}{canGrowDown}A"))
                    {
                        return;
                    }
                }

                int remaining = extraRows - canGrowDown;
                int newY = Screen.Y;

                // Then, grow up: scroll the terminal to reclaim rows above.
                if (remaining > 0)
                {
                    int canScrollUp = Math.Min (remaining, Screen.Y);

                    if (canScrollUp > 0)
                    {
                        if (!WriteInlineMovement (EscSeqUtils.CSI_SetCursorPosition (termHeight, 1) + new string ('\n', canScrollUp)))
                        {
                            return;
                        }
                        if (!WriteInlineMovement ($"{EscSeqUtils.CSI}{canScrollUp}A"))
                        {
                            return;
                        }
                    }

                    newY -= canScrollUp;
                    canGrowDown += canScrollUp;
                }

                int newHeight = Screen.Height + canGrowDown;

                if (newHeight != Screen.Height)
                {
                    Driver.InlinePosition = new Point (0, newY);
                    Screen = new Rectangle (0, newY, Screen.Width, newHeight);
                    if (!PassIsValid ())
                    {
                        return;
                    }

                    // Terminal scrolling moved existing content — the output buffer's
                    // cached positions are stale. Force a full redraw so LineCanvas
                    // and all view content is re-rendered at the correct positions.
                    Driver.ClearContents ();
                    forceRedraw = true;
                }
            }
        }

        if (AppModel == AppModel.Inline && TryShrinkInlineRegion ())
        {
            forceRedraw = true;
        }
        if (!PassIsValid ())
        {
            return;
        }

        if (DrainInlineOutput (candidate))
        {
            forceRedraw = true;
            // ScreenChanged may end sessions, dispose, open a modal, or change geometry.
            views = [.. SessionStack?.Select (token => token.Runnable).OfType<View> () ?? []];
        }
        if (!PassIsValid ())
        {
            return;
        }

        views = [.. SessionStack!.Select (token => token.Runnable).OfType<View> ()];
        if (Popovers?.GetActivePopover () is View { Visible: true, NeedsLayout: true } currentPopover)
        {
            views.Insert (0, currentPopover);
        }

        // Final layout with the definitive Screen.Size — all inline adjustments are done.
        bool neededLayout = View.Layout (views.ToArray ().Reverse ()!, Screen.Size);
        if (!PassIsValid ())
        {
            return;
        }

        // Draw
        bool needsDraw = forceRedraw || views.Any (v => v is { NeedsDraw: true } or { SubViewNeedsDraw: true });

        if (Popovers?.GetActivePopover () is View { Visible: true } visiblePopover)
        {
            if (needsDraw)
            {
                visiblePopover.SetNeedsDraw ();

                if (!views.Contains (visiblePopover))
                {
                    views.Insert (0, visiblePopover);
                }
            }
            else if (visiblePopover.NeedsDraw || visiblePopover.SubViewNeedsDraw)
            {
                visiblePopover.SetNeedsDraw ();

                if (!views.Contains (visiblePopover))
                {
                    views.Insert (0, visiblePopover);
                }

                needsDraw = true;
            }
        }

        if (Driver is { } && (neededLayout || needsDraw))
        {
            Logging.Redraws.Add (1);

            // Clip uses the output buffer dimensions (0-indexed), not the terminal offset.
            Rectangle clipRect = Screen with { X = 0, Y = 0 };
            Driver.Clip = new Region (clipRect);

            // Only force a complete redraw when explicitly requested.
            // Otherwise, redraw only the views that need it.
            //
            // NOTE (#5358): passing force=true here calls SetNeedsDraw on the top runnable, which
            // cascades to all overlapping subviews via the existing SetNeedsDraw recursion. This
            // is the remaining draw-fan-out source documented by TabsFanOutIntegrationTests. The
            // proper fix requires tracking adornment thickness changes (in addition to Frame
            // changes) so the SuperView can be invalidated precisely instead of force-redrawing
            // the whole tree.
            View.Draw (views.ToArray ().Cast<View> (), forceRedraw);
            if (!PassIsValid ())
            {
                return;
            }

            // Release raster graphics (Sixel/Kitty) whose owning view is no longer rendered — hidden,
            // removed from the hierarchy, or on a runnable that has ended. They are emitted out-of-band
            // and persist on screen until explicitly deleted, so erasing their cells is not enough. See
            // https://github.com/tui-cs/Terminal.Gui/issues/5543.
            IOutputBuffer rasterBuffer = Driver.GetOutputBuffer ();
            HashSet<string> activeRasterIds = [];

            foreach (View? view in views)
            {
                view?.CollectActiveRasterImageIds (activeRasterIds);
            }

            rasterBuffer.RetainRasterImages (activeRasterIds);

            Driver.Clip = new Region (clipRect);

            // Cause the driver to flush any pending updates to the terminal
            Driver?.Refresh ();
        }

        // A failed physical frame leaves output dirty after View.Draw has cleared NeedsDraw.
        // Retry those cells without forcing a full repaint (which would erase inline content).
        if (!neededLayout && !needsDraw && Driver is DriverImpl { IsOutputRetryDue: true } retryDriver)
        {
            retryDriver.Refresh ();
        }

        if (neededLayout || needsDraw)
        {
            LayoutAndDrawComplete?.Invoke (this, EventArgs.Empty);
        }
        Tracing.Trace.Draw ("ApplicationImpl", "End", $"neededLayout={neededLayout}, needsDraw={needsDraw}");
    }

    /// <inheritdoc />
    public event EventHandler<EventArgs>? LayoutAndDrawComplete;
}

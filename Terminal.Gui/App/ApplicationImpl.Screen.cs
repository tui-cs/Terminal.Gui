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

        foreach (SessionToken t in SessionStack!)
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
                driver.WriteRaw ($"{EscSeqUtils.CSI}H{EscSeqUtils.CSI}2J");
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

        List<View?> views = [.. SessionStack.Select (r => r.Runnable! as View)!];

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
                    Driver.WriteRaw (new string ('\n', overflow));
                    Driver.WriteRaw ($"{EscSeqUtils.CSI}{overflow}A");
                    cursorRow = Math.Max (0, cursorRow - overflow);
                }

            }

            int availableHeight = Math.Min (viewHeight, termHeight - cursorRow);

            if (availableHeight > 0)
            {
                // Set App.Screen to the inline sub-rectangle. This resizes the output buffer
                // to the inline region dimensions and stores the Y offset for cursor positioning.
                Screen = new Rectangle (0, cursorRow, termWidth, availableHeight);
            }

            _inlineScreenSized = true;
        }

        // Inline mode: after initial setup, handle dynamic growth. If the view's desired height
        // exceeds Screen.Height, grow App.Screen. Grow DOWN first (into empty terminal rows
        // below the inline region), then UP (scrolling the terminal) only when there's no room below.
        if (AppModel == AppModel.Inline && _inlineScreenSized && Driver is { })
        {
            // Layout with current Screen to get desired heights from Dim.Auto views.
            View.Layout (views.ToArray ().Reverse ()!, Screen.Size);

            View? topView = views.LastOrDefault (v => v is { });

            // Cap the view height at the terminal height — the inline region can never exceed it.
            int viewHeight = Math.Min (topView?.Frame.Height ?? 0, Driver.Screen.Height);

            if (viewHeight > Screen.Height)
            {
                int extraRows = viewHeight - Screen.Height;
                int termHeight = Driver.Screen.Height;

                // First, grow down: use empty rows below the current inline region.
                int bottomEdge = Screen.Y + Screen.Height;
                int canGrowDown = Math.Min (extraRows, termHeight - bottomEdge);

                // Reserve the new rows by scrolling them into existence.
                if (canGrowDown > 0)
                {
                    Driver.WriteRaw (new string ('\n', canGrowDown));
                    Driver.WriteRaw ($"{EscSeqUtils.CSI}{canGrowDown}A");
                }

                int remaining = extraRows - canGrowDown;
                int newY = Screen.Y;

                // Then, grow up: scroll the terminal to reclaim rows above.
                if (remaining > 0)
                {
                    int canScrollUp = Math.Min (remaining, Screen.Y);

                    if (canScrollUp > 0)
                    {
                        Driver.WriteRaw (new string ('\n', canScrollUp));
                        Driver.WriteRaw ($"{EscSeqUtils.CSI}{canScrollUp}A");
                    }

                    newY -= canScrollUp;
                    canGrowDown += canScrollUp;
                }

                int newHeight = Screen.Height + canGrowDown;

                if (newHeight != Screen.Height)
                {
                    Screen = new Rectangle (0, newY, Screen.Width, newHeight);

                    // Terminal scrolling moved existing content — the output buffer's
                    // cached positions are stale. Force a full redraw so LineCanvas
                    // and all view content is re-rendered at the correct positions.
                    Driver.ClearContents ();
                    forceRedraw = true;
                }
            }
        }

        // Final layout with the definitive Screen.Size — all inline adjustments are done.
        bool neededLayout = View.Layout (views.ToArray ().Reverse ()!, Screen.Size);

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

namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    // tmux updates its grid before its throttled PTY resize notification (250 ms in 3.6).
    // Coalesce drag bursts before reserving a fresh region, including frames, printing and caret writes.
    private static readonly TimeSpan InlineResizeQuietPeriod = TimeSpan.FromMilliseconds (500);

    private InlineResize? _inlineResize;

    private void BeginInlineResize (Rectangle region, Size size, bool observePolledSize = true)
    {
        if (Driver is not { } currentDriver)
        {
            return;
        }

        if (currentDriver is not DriverImpl driver || driver.GetOutput () is not AnsiOutput { IsInlineOutputAvailable: true } output)
        {
            if (size.Width <= 0 || size.Height <= 0)
            {
                return;
            }

            // Headless/custom drivers have no physical cursor to recover. Preserve their known origin.
            int height = Math.Min (region.Height, Math.Max (0, size.Height));
            int origin = Math.Clamp (region.Y, 0, Math.Max (0, size.Height - height));
            currentDriver.InlinePosition = new Point (0, origin);
            _screen = new Rectangle (0, origin, Math.Max (0, size.Width), height);
            _inlineScreenHasValidOwnership = true;
            (currentDriver as DriverImpl)?.ResizeOutputBuffer (size.Width, height);
            currentDriver.ClearContents ();
            foreach (View runnable in SessionStack?.Select (token => token.Runnable).OfType<View> () ?? [])
            {
                runnable.SetNeedsDraw ();
            }

            RaiseScreenChangedEvent (Screen);
            return;
        }

        if (_inlineResize is { } pending)
        {
            ConsumeInlineSizeObservations (pending, output);
            pending.ObserveSize (size, _timeProvider.Now);
            if (pending.Size == size)
            {
                return;
            }

            pending.Size = size;
            return;
        }

        InlineResize resize = new ()
        {
            Region = region,
            Size = size,
            ObservedSize = size,
            LastChanged = _timeProvider.Now,
            Generation = _inlineOutputGeneration
        };
        ConsumeInlineSizeObservations (resize, output);
        if (observePolledSize)
        {
            resize.ObserveSize (size, _timeProvider.Now);
        }

        _inlineResize = resize;
        output.IsInlineResizePending = true;
    }

    private bool ObserveNativeInlineResizeSize (InlineResize resize, AnsiOutput output)
    {
        int revision = resize.Revision;
        Size? native = output.QueryNativeInlineSize ();
        ConsumeInlineSizeObservations (resize, output);
        return revision != resize.Revision || resize.ObservedSize != resize.Size || native is { } size && size != resize.Size;
    }

    private void ConsumeInlineSizeObservations (InlineResize resize, AnsiOutput output)
    {
        while (output.TryDequeueInlineSize (out Size size))
        {
            resize.ObserveSize (size, _timeProvider.Now);
        }
    }

    // Called after size polling, before layout/printing. A cursor report cannot prove which
    // old rows remain live: terminal grid changes and PTY size notifications are not atomic.
    private bool CompleteInlineResize ()
    {
        if (Driver is not DriverImpl driver)
        {
            return true;
        }

        if (_inlineResize is null && driver.GetOutput () is AnsiOutput { IsInlineOutputAvailable: true } observedOutput && _screen is { } ownedRegion)
        {
            observedOutput.QueryNativeInlineSize ();
            if (observedOutput.HasInlineSizeObservations)
            {
                BeginInlineResize (ownedRegion, driver.Screen.Size, observePolledSize: false);
            }
        }

        if (_inlineResize is not { } resize)
        {
            return true;
        }

        if (!Initialized || _dispatchStopping || resize.Generation != _inlineOutputGeneration)
        {
            return false;
        }

        bool nativeChanged = driver.GetOutput () is AnsiOutput currentOutput && ObserveNativeInlineResizeSize (resize, currentOutput);
        if (resize.Size.Width <= 0 || resize.Size.Height <= 0)
        {
            return false;
        }

        if (nativeChanged || _timeProvider.Now - resize.LastChanged < InlineResizeQuietPeriod)
        {
            return false;
        }

        // Preserve every surviving old cell, including old live frames. Never erase a range
        // inferred from a cursor report; tmux can restore history before reporting its new size.
        int height = Math.Clamp (resize.Region.Height, 0, resize.Size.Height);
        string reserve = EscSeqUtils.OSC_EndHyperlink () + EscSeqUtils.CSI_ResetAttributes
                         + EscSeqUtils.CSI_SetCursorPosition (resize.Size.Height, 1)
                         + new string ('\n', resize.Size.Height) + EscSeqUtils.CSI_SetCursorPosition (1, 1);
        if (!WriteInlineMovement (reserve, resize.Size))
        {
            return false;
        }

        _inlineResize = null;
        if (driver.GetOutput () is AnsiOutput ansiOutput)
        {
            ansiOutput.CompleteInlineSizeRecovery ();
            ansiOutput.InlineCursorRow = 0;
            ansiOutput.IsInlineResizePending = false;
            ansiOutput.ResetInlineTextState ();
        }

        _screen = new Rectangle (0, 0, resize.Size.Width, height);
        _inlineScreenHasValidOwnership = true;
        driver.InlinePosition = Point.Empty;
        driver.ResizeOutputBuffer (resize.Size.Width, height);
        _inlineScreenSized = height > 0;
        foreach (View runnable in SessionStack?.Select (token => token.Runnable).OfType<View> () ?? [])
        {
            runnable.SetNeedsLayout ();
            runnable.SetNeedsDraw ();
        }

        if (Popovers?.GetActivePopover () is View popover)
        {
            popover.SetNeedsLayout ();
            popover.SetNeedsDraw ();
        }

        driver.SetCursorNeedsUpdate (true);
        RaiseScreenChangedEvent (Screen);
        return Initialized && !_dispatchStopping && HasRunningSession && Driver == driver && !InlinePlacementLost && _inlineResize is null;
    }

    private sealed class InlineResize
    {
        internal Rectangle Region { get; init; }
        internal Size Size { get; set; }
        internal long Generation { get; init; }
        internal int Revision { get; set; }
        internal Size ObservedSize { get; set; }
        internal DateTime LastChanged { get; set; }

        internal void ObserveSize (Size size, DateTime now)
        {
            if (size == ObservedSize)
            {
                return;
            }

            ObservedSize = size;
            Revision++;
            LastChanged = now;
        }
    }
}

namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    // Growth and shrink must use the same floor, including when a runnable extends above row zero.
    private static long GetRequiredInlineHeight (View runnable) =>
        Math.Max (runnable.Frame.Height, (long)runnable.Frame.Y + runnable.Frame.Height);

    private bool TryShrinkInlineRegion ()
    {
        if (!_inlineScreenSized || _screen is not { Height: > 0 } region
            || Driver is not DriverImpl driver || driver.GetOutput () is not AnsiOutput { IsInlineOutputAvailable: true } output
            || !output.IsInlinePlacementKnown)
        {
            return false;
        }

        Size terminal = driver.Screen.Size;
        if (region.X != 0 || region.Width != terminal.Width || region.Y < 0 || (long)region.Y + region.Height > terminal.Height
            || terminal.Width <= 0 || terminal.Height <= 0 || Popovers?.GetActivePopover () is { Visible: true }
            || output.HasInlineRasterState (driver.GetOutputBuffer ()))
        {
            return false;
        }

        View [] runnables = [.. SessionStack!.Select (token => token.Runnable).OfType<View> ().Where (view => view.Visible)];
        if (runnables.Length == 0)
        {
            return false;
        }

        int height = 0;
        foreach (View runnable in runnables)
        {
            // Space-dependent dimensions must not feed the shrinking reservation back into their own size.
            if (runnable.Y.DependsOnSuperViewContentSize || runnable.Y.Has<PosCenter> (out _) || runnable.Y.Has<PosPercent> (out _)
                || runnable.Height.DependsOnSuperViewContentSize
                || runnable.Height.Has<DimPercent> (out _) || runnable.Height.Has<DimFill> (out _))
            {
                return false;
            }

            long requiredHeight = GetRequiredInlineHeight (runnable);
            if (requiredHeight >= region.Height)
            {
                return false;
            }

            height = Math.Max (height, (int)Math.Max (0, requiredHeight));
        }

        // Erase the old owned rows before releasing any of them. Otherwise a later insertion can scroll a stale
        // preview into native history. CUP/EL preserves all rows above and below the old region without scrolling.
        StringBuilder erase = new (EscSeqUtils.OSC_EndHyperlink () + EscSeqUtils.CSI_ResetAttributes);
        for (int row = region.Y; row < region.Bottom; row++)
        {
            erase.Append (EscSeqUtils.CSI_SetCursorPosition (row + 1, 1));
            erase.Append (EscSeqUtils.CSI + "2K");
        }
        erase.Append (EscSeqUtils.CSI_SetCursorPosition (region.Y + 1, 1));
        output.ResetInlineTextState ();
        if (!WriteInlineMovement (erase.ToString ()))
        {
            return false;
        }

        output.InlineCursorRow = region.Y;
        // Publish the compact geometry and invalidate the live frame before invoking ScreenChanged subscribers.
        // A throwing subscriber must not leave an empty buffer with no repaint scheduled.
        _screen = region with { Height = height };
        driver.InlinePosition = new Point (0, region.Y);
        driver.ResizeOutputBuffer (region.Width, height);
        foreach (View runnable in runnables)
        {
            runnable.SetNeedsLayout ();
            runnable.SetNeedsDraw ();
        }
        RaiseScreenChangedEvent (Screen);
        return true;
    }
}

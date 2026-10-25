namespace Terminal.Gui.Drivers;

public partial class AnsiOutput
{
    internal bool IsInlinePlacementKnown { get; private set; } = true;
    bool IInlineOutput.IsInlinePlacementKnown => IsInlinePlacementKnown;
    internal bool IsInlineOutputAvailable => GetType () == typeof (AnsiOutput) && AppModel == AppModel.Inline && _platform != AnsiPlatform.Degraded;
    bool IInlineOutput.IsInlineOutputAvailable => IsInlineOutputAvailable;

    void IInlineOutput.LoseInlinePlacement () => IsInlinePlacementKnown = false;

    void IInlineOutput.ValidateInsertion (IOutputBuffer buffer)
    {
        if (!IsInlineOutputAvailable)
        {
            throw new NotSupportedException ("PrintAboveAsync requires attached built-in ANSI inline output.");
        }
        if (!IsInlinePlacementKnown)
        {
            throw new IOException ("Inline terminal placement is unknown after a failed write. Stop and initialize a new application.");
        }
        if (HasInlineRasterState (buffer))
        {
            throw new NotSupportedException ("PrintAboveAsync cannot move active or pending raster graphics.");
        }
    }

    int IInlineOutput.InsertAbove (InlineOutputLayout layout, Rectangle region, Size terminal)
    {
        // Count before the first byte; retain only a single dense row while writing.
        int remaining = layout.CountRows (terminal.Width);
        using IEnumerator<InlineOutputRow> rows = layout.Rows (terminal.Width).GetEnumerator ();
        int destination = region.Y;
        try
        {
            ResetInlineTextState ();
            Write (EscSeqUtils.OSC_EndHyperlink () + EscSeqUtils.CSI_ResetAttributes);
            while ((long)remaining + region.Height > terminal.Height)
            {
                int take = Math.Min (remaining, terminal.Height);
                int scroll = Math.Max (0, destination + take - terminal.Height);
                Scroll (scroll);
                DrawRows (destination - scroll, take);
                destination += take - scroll;
                remaining -= take;
            }
            int finalScroll = Math.Max (0, destination + remaining + region.Height - terminal.Height);
            Scroll (finalScroll);
            DrawRows (destination - finalScroll, remaining);
            destination += remaining - finalScroll;
            for (var row = destination; row < destination + region.Height; row++)
            {
                Write ($"{EscSeqUtils.CSI_SetCursorPosition (row + 1, 1)}{EscSeqUtils.CSI_ResetAttributes}{EscSeqUtils.CSI}2K");
            }
            Write (EscSeqUtils.OSC_EndHyperlink () + EscSeqUtils.CSI_ResetAttributes);
            InlineCursorRow = destination + region.Height - 1;
            return destination;
        }
        catch
        {
            InlineCursorRow = null;
            IsInlinePlacementKnown = false;
            throw;
        }

        void Scroll (int count)
        {
            if (count == 0)
            {
                return;
            }
            Write (EscSeqUtils.CSI_SetCursorPosition (terminal.Height, 1));
            for (var i = 0; i < count; i++)
            {
                Write ("\r\n");
            }
        }

        void DrawRows (int start, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (!rows.MoveNext ())
                {
                    throw new InvalidOperationException ("Prepared inline row count changed.");
                }
                StringBuilder output = new (EscSeqUtils.CSI_SetCursorPosition (start + i + 1, 1));
                output.Append (EscSeqUtils.CSI_ResetAttributes);
                // Reset the terminal's stored line extent before replacing a live-frame row.
                output.Append (EscSeqUtils.CSI + "2K");
                Attribute? previous = null;
                foreach (InlineOutputSpan span in rows.Current.Spans)
                {
                    if (span.Attribute != previous)
                    {
                        output.Append (EscSeqUtils.CSI_ResetAttributes);
                        if (span.Attribute is { } style)
                        {
                            AppendOrWriteAttribute (output, style, TextStyle.None);
                        }
                        previous = span.Attribute;
                    }
                    output.Append (span.Text);
                }
                output.Append (EscSeqUtils.CSI_ResetAttributes);
                // Explicit CUP cancels pending autowrap even after an exact-width bottom row.
                output.Append (EscSeqUtils.CSI_SetCursorPosition (start + i + 1, 1));
                Write (output.ToString ().AsSpan ());
            }
        }
    }
}

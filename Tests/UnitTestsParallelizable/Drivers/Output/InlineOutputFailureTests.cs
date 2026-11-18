using System.Text;

namespace DriverTests.Output;

public class InlineOutputFailureTests
{
    [Fact]
    public void FailureAtEachPhysicalWritePoisonsPlacementAndNeverReplays ()
    {
        InlineOutputLayout layout = new (Enumerable.Range (0, 9).Select (row => new InlineOutputLine ([new InlineOutputSpan ($"P{row}")])).ToArray ());
        int successfulWrites = 0;
        using (AnsiOutput probe = new (_ => { successfulWrites++; return true; }, AppModel.Inline))
        {
            ((IInlineOutput)probe).InsertAbove (layout, new Rectangle (0, 1, 4, 2), new Size (4, 4));
        }
        // The probe count includes best-effort disposal; only insertion writes must throw.
        for (var failure = 1; failure <= successfulWrites - 6; failure++)
        {
            int calls = 0;
            int failAt = failure;
            StringBuilder accepted = new ();
            using AnsiOutput output = new (bytes =>
            {
                calls++;
                if (calls == failAt)
                {
                    // Simulate the native sink accepting a prefix and then failing.
                    accepted.Append (Encoding.UTF8.GetString (bytes [..Math.Min (bytes.Length, 2)]));
                    return false;
                }
                accepted.Append (Encoding.UTF8.GetString (bytes));
                return true;
            }, AppModel.Inline);
            IInlineOutput insertion = output;
            Assert.Throws<IOException> (() => insertion.InsertAbove (layout, new Rectangle (0, 1, 4, 2), new Size (4, 4)));
            Assert.False (insertion.IsInlinePlacementKnown);
            int writesAfterFailure = calls;
            OutputBufferImpl buffer = new ();
            buffer.SetSize (4, 2);
            output.Write (buffer);
            output.SetCursor (new Cursor ());
            Assert.Equal (writesAfterFailure, calls);
            Assert.Throws<IOException> (() => insertion.ValidateInsertion (buffer));
            string beforeDispose = accepted.ToString ();
            output.Dispose ();
            string cleanup = accepted.ToString () [beforeDispose.Length..];
            Assert.StartsWith ("\u001b\\" + EscSeqUtils.OSC_EndHyperlink (), cleanup);
            Assert.DoesNotContain ("H", cleanup);
            Assert.DoesNotContain ("P", cleanup);
        }
    }

    [Fact]
    public void PendingRasterStateIsRejectedBeforeAnyInsertionBytes ()
    {
        var writes = 0;
        using AnsiOutput output = new (_ => { writes++; return true; }, AppModel.Inline);
        OutputBufferImpl buffer = new ();
        buffer.SetSize (4, 2);
        buffer.AddRasterImage (new RasterImageCommand { Id = "image", DestinationCells = new Rectangle (0, 0, 1, 1), Pixels = new Color [1, 1] });
        Assert.Throws<NotSupportedException> (() => ((IInlineOutput)output).ValidateInsertion (buffer));
        Assert.Equal (0, writes);
        buffer.RemoveRasterImage ("image");
        ((IInlineOutput)output).ValidateInsertion (buffer);
    }
    [Fact]
    public void InsertionResetsCachedBoldBeforeLiveRepaint ()
    {
        StringBuilder bytes = new ();
        using AnsiOutput output = new (chunk => { bytes.Append (Encoding.UTF8.GetString (chunk)); return true; }, AppModel.Inline);
        OutputBufferImpl buffer = new ();
        buffer.SetSize (4, 1);
        buffer.CurrentAttribute = new Attribute (Color.Green, Color.None, TextStyle.Bold);
        buffer.AddStr ("LIVE");
        output.Write (buffer);
        ((IInlineOutput)output).InsertAbove (new InlineOutputLayout ([new InlineOutputLine ([new InlineOutputSpan ("done")])]), new Rectangle (0, 0, 4, 1), new Size (4, 2));
        bytes.Clear ();
        buffer.Move (0, 0);
        buffer.AddStr ("LIVE");
        output.Write (buffer);
        Assert.Contains ("\u001b[1m", bytes.ToString ());
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void RetainedRasterMustBePhysicallyCleanedBeforePrinting (bool kitty)
    {
        var fail = false;
        using AnsiOutput output = new (_ => !fail, AppModel.Inline);
        output.UseKittyGraphics = kitty;
        OutputBufferImpl buffer = new ();
        buffer.SetSize (4, 2);
        Color [,] pixels = new Color [1, 1];
        pixels [0, 0] = Color.Red;
        buffer.AddRasterImage (new RasterImageCommand { Id = "image", DestinationCells = new Rectangle (0, 0, 1, 1), Pixels = pixels });
        output.Write (buffer);
        buffer.RemoveRasterImage ("image");
        IInlineOutput insertion = output;
        Assert.Throws<NotSupportedException> (() => insertion.ValidateInsertion (buffer));
        fail = true;
        Assert.Throws<IOException> (() => output.Write (buffer));
        Assert.Throws<NotSupportedException> (() => insertion.ValidateInsertion (buffer));
        fail = false;
        output.Write (buffer);
        insertion.ValidateInsertion (buffer);
    }

    [Fact]
    public void LegacySixelWithoutDimensionsRemainsUnsupportedAfterDequeue ()
    {
        using AnsiOutput output = new (_ => true, AppModel.Inline);
        OutputBufferImpl buffer = new ();
        buffer.SetSize (4, 2);
        output.GetSixels ().Enqueue (new SixelToRender { SixelData = "\u001bPq~\u001b\\" });
        output.Write (buffer);
        Assert.True (output.GetSixels ().TryDequeue (out _));
        Assert.Throws<NotSupportedException> (() => ((IInlineOutput)output).ValidateInsertion (buffer));
    }

}

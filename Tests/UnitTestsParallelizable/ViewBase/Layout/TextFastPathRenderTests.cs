using UnitTests;
using static Terminal.Gui.ViewBase.Dim;

namespace ViewBaseTests.Layout;

// Claude - Opus 4.8
// End-to-end guard for the #5499 Text fast path: the redraw-only path (no layout pass after a same-Frame text change)
// must leave the view rendering exactly as a full layout pass would - including wrapped/clipped fixed-size text, whose
// TextFormatter constraints would otherwise be lost. Compares the FAST path (laid out only if the setter requested it)
// against a CONTROL that always runs a full layout.
public class TextFastPathRenderTests : TestDriverBase
{
    private static string DriverContents (IDriver driver)
    {
        System.Text.StringBuilder sb = new ();

        for (var r = 0; r < driver.Screen.Height; r++)
        {
            for (var c = 0; c < driver.Screen.Width; c++)
            {
                string g = driver.Contents! [r, c].Grapheme;
                sb.Append (string.IsNullOrEmpty (g) ? " " : g);
            }

            sb.Append ('\n');
        }

        return sb.ToString ().TrimEnd ();
    }

    private string RenderFast (View v, IDriver driver, string newText)
    {
        v.NeedsLayout = false;
        v.Text = newText;

        // Mirror the app loop: lay out only if the setter requested it. This keeps the redraw-only fast path isolated.
        if (v.NeedsLayout)
        {
            v.Layout ();
        }

        v.Draw ();

        return DriverContents (driver);
    }

    private string RenderControl (View v, IDriver driver, string newText)
    {
        v.Text = newText;
        v.SetNeedsLayout ();
        v.Layout ();
        v.Draw ();

        return DriverContents (driver);
    }

    [Theory]
    [InlineData (5, 2, true, "abcdefghi")] // fixed size + word wrap (the reported regression)
    [InlineData (5, 2, false, "abcdefghi")] // fixed size, no wrap (clipping)
    [InlineData (6, 1, true, "ab cd ef gh")] // fixed width, single fixed row
    public void FixedSize_TextChange_RendersLikeFullLayout (int width, int height, bool wrap, string newText)
    {
        IDriver dFast = CreateTestDriver (24, 8);
        IDriver dCtrl = CreateTestDriver (24, 8);

        View fast = new () { Driver = dFast, Width = width, Height = height, Text = "abc" };
        fast.TextFormatter.WordWrap = wrap;
        View ctrl = new () { Driver = dCtrl, Width = width, Height = height, Text = "abc" };
        ctrl.TextFormatter.WordWrap = wrap;

        fast.Layout ();
        fast.Draw ();
        ctrl.Layout ();
        ctrl.Draw ();

        string fastRender = RenderFast (fast, dFast, newText);
        string ctrlRender = RenderControl (ctrl, dCtrl, newText);

        Assert.False (fast.NeedsLayout); // confirms the fast path was actually taken
        Assert.Equal (ctrlRender, fastRender);

        fast.Dispose ();
        ctrl.Dispose ();
        dFast.Dispose ();
        dCtrl.Dispose ();
    }

    [Theory]
    [InlineData (5)] // Auto(Text, max:5) + wrap: text wider than max wraps to multiple rows
    [InlineData (4)]
    public void MaxConstrainedAuto_WordWrap_RendersLikeFullLayout (int max)
    {
        IDriver dFast = CreateTestDriver (24, 8);
        IDriver dCtrl = CreateTestDriver (24, 8);

        View fast = new () { Driver = dFast, Width = Auto (DimAutoStyle.Text, maximumContentDim: max), Height = Auto (DimAutoStyle.Text), Text = "ab cd" };
        fast.TextFormatter.WordWrap = true;
        View ctrl = new () { Driver = dCtrl, Width = Auto (DimAutoStyle.Text, maximumContentDim: max), Height = Auto (DimAutoStyle.Text), Text = "ab cd" };
        ctrl.TextFormatter.WordWrap = true;

        fast.Layout ();
        fast.Draw ();
        ctrl.Layout ();
        ctrl.Draw ();

        // "ab cd ef" keeps the same max-constrained width while wrapping - exercises the width->height prediction.
        string fastRender = RenderFast (fast, dFast, "ab cd ef");
        string ctrlRender = RenderControl (ctrl, dCtrl, "ab cd ef");

        Assert.Equal (ctrlRender, fastRender);

        fast.Dispose ();
        ctrl.Dispose ();
        dFast.Dispose ();
        dCtrl.Dispose ();
    }
}

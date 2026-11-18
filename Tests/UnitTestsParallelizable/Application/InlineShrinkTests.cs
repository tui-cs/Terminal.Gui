using System.Text;
using System.Text.RegularExpressions;

namespace ApplicationTests;

// CoPilot - GPT-6
[Collection ("Application Tests")]
public class InlineShrinkTests
{
    private static ApplicationImpl Create (StringBuilder bytes, out AnsiOutput output, int origin = 2)
    {
        output = new AnsiOutput (chunk => { bytes.Append (Encoding.UTF8.GetString (chunk)); return true; }, AppModel.Inline);
        output.SetSize (20, 12);
        ApplicationImpl app = new (new AnsiComponentFactory (output: output)) { AppModel = AppModel.Inline, ForceInlinePosition = new Point (0, origin) };
        app.Init ();
        return app;
    }

    [Fact]
    public void RemovingPreview_ShrinksAutoContentAndErasesOnlyOldOwnedRows ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _);
        using Runnable composer = new () { Width = Dim.Fill (), Height = Dim.Auto (DimAutoStyle.Content, minimumContentDim: 3) };
        using Label preview = new () { Y = 3, Text = "old\nold\nold", Width = Dim.Fill (), Height = 3 };
        composer.Add (preview);
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();
        Assert.Equal (new Rectangle (0, 2, 20, 6), app.Screen);
        bytes.Clear ();

        composer.Remove (preview);
        app.LayoutAndDraw ();

        Assert.Equal (new Rectangle (0, 2, 20, 3), app.Screen);
        Assert.Equal (new Point (0, 2), app.Driver!.InlinePosition);
        Assert.Equal (3, app.Driver.Rows);
        Assert.Equal (new Rectangle (0, 0, 20, 12), app.Driver.Screen);
        string text = bytes.ToString ();
        Assert.Equal ([3, 4, 5, 6, 7, 8], Regex.Matches (text, "\\x1b\\[(\\d+);1H\\x1b\\[2K").Select (match => int.Parse (match.Groups [1].Value)));
        Assert.DoesNotContain ("[2J", text);
        Assert.DoesNotContain ("[3J", text);
        Assert.DoesNotContain ('\n', text);
        app.End (owner);
    }

    [Fact]
    public void ShorterText_ShrinksToMeasuredHeightAndThenStaysStable ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _);
        using Runnable composer = new () { Width = Dim.Fill (), Height = Dim.Auto (DimAutoStyle.Content) };
        Label text = new () { Width = Dim.Fill (), Text = "one\ntwo\nthree\nfour" };
        composer.Add (text);
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();
        Assert.Equal (4, app.Screen.Height);

        text.Text = "ready";
        app.LayoutAndDraw ();

        Assert.Equal (1, app.Screen.Height);
        bytes.Clear ();
        app.LayoutAndDraw ();
        app.LayoutAndDraw ();
        Assert.DoesNotContain ("[2K", bytes.ToString ());
        app.End (owner);
    }

    [Fact]
    public void ExplicitHeight_CanShrinkAndGrowAgain ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 7 };
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();

        composer.Height = 2;
        app.LayoutAndDraw ();
        Assert.Equal (new Rectangle (0, 2, 20, 2), app.Screen);

        composer.Height = 7;
        app.LayoutAndDraw ();
        Assert.Equal (new Rectangle (0, 2, 20, 7), app.Screen);
        app.End (owner);
    }

    [Fact]
    public void ShrinkToZero_DefersOutputAndRetainsOriginUntilLiveContentReturns ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 6 };
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();
        composer.Height = 0;
        Task print = app.PrintAboveAsync (owner, "completed", TestContext.Current.CancellationToken);

        app.LayoutAndDraw ();

        Assert.Equal (new Rectangle (0, 2, 20, 0), app.Screen);
        Assert.False (print.IsCompleted);
        composer.Height = 2;
        app.LayoutAndDraw ();
        Assert.True (print.IsCompletedSuccessfully);
        Assert.Equal (new Rectangle (0, 3, 20, 2), app.Screen);
        app.End (owner);
    }

    [Fact]
    public void ActiveModal_KeepsItsRequiredRowsUntilItEnds ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable outer = new () { Width = Dim.Fill (), Height = 8 };
        SessionToken outerOwner = app.Begin (outer)!;
        app.LayoutAndDraw ();
        using Runnable modal = new () { Width = Dim.Fill (), Height = 5 };
        SessionToken modalOwner = app.Begin (modal)!;

        outer.Height = 2;
        app.LayoutAndDraw ();
        Assert.Equal (5, app.Screen.Height);

        app.End (modalOwner);
        app.LayoutAndDraw ();
        Assert.Equal (2, app.Screen.Height);
        app.End (outerOwner);
    }

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    public void SpaceDependentLayout_DoesNotCollapseAcrossRepeatedPasses (int dimension)
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 8 };
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();
        bytes.Clear ();
        switch (dimension)
        {
            case 0: composer.Height = Dim.Percent (50); break;
            case 1: composer.Height = Dim.Fill (1); break;
            case 2: composer.Height = Dim.Auto (minimumContentDim: Dim.Percent (50)); break;
            case 3: composer.Height = 2; composer.Y = Pos.Center (); break;
        }

        for (int pass = 0; pass < 5; pass++)
        {
            app.LayoutAndDraw ();
        }

        Assert.Equal (8, app.Screen.Height);
        Assert.DoesNotContain ("[2K", bytes.ToString ());
        app.End (owner);
    }

    [Fact]
    public void OffsetRunnable_ProtectsItsEntireFrameDuringShrinkAndRegrowth ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable outer = new () { Width = Dim.Fill (), Height = 8 };
        SessionToken owner = app.Begin (outer)!;
        app.LayoutAndDraw ();
        outer.Y = 2;
        outer.Height = 2;

        app.LayoutAndDraw ();

        Assert.Equal (4, app.Screen.Height);

        outer.Height = 4;
        app.LayoutAndDraw ();

        Assert.Equal (new Rectangle (0, 2, 20, 6), app.Screen);
        Assert.Equal (6, app.Driver!.Rows);
        app.End (owner);
    }

    [Theory]
    [InlineData (7, 8, 2)]
    [InlineData (20, 12, 0)]
    public void ActiveModal_CanShrinkAndRegrowBeyondOuterFrame (int height, int expectedHeight, int expectedOrigin)
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable outer = new () { Width = Dim.Fill (), Height = 8 };
        SessionToken outerOwner = app.Begin (outer)!;
        app.LayoutAndDraw ();
        using Runnable modal = new () { Y = 1, Width = Dim.Fill (), Height = 4 };
        SessionToken modalOwner = app.Begin (modal)!;
        outer.Height = 2;
        app.LayoutAndDraw ();
        Assert.Equal (5, app.Screen.Height);

        modal.Height = 1;
        app.LayoutAndDraw ();
        Assert.Equal (2, app.Screen.Height);

        modal.Height = height;
        app.LayoutAndDraw ();

        Assert.Equal (new Rectangle (0, expectedOrigin, 20, expectedHeight), app.Screen);
        Assert.Equal (expectedHeight, app.Driver!.Rows);
        Assert.Equal (new Point (0, expectedOrigin), app.Driver.InlinePosition);
        app.End (modalOwner);
        app.LayoutAndDraw ();
        Assert.Equal (2, app.Screen.Height);
        app.End (outerOwner);
    }

    [Theory]
    [InlineData (false, -1)]
    [InlineData (true, -1)]
    [InlineData (false, -8)]
    [InlineData (true, -8)]
    public void NegativeY_ShrinksToMinimumHeightAndStaysStable (bool nested, int y)
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _);
        using Runnable outer = new () { Width = Dim.Fill (), Height = 8 };
        SessionToken outerOwner = app.Begin (outer)!;
        app.LayoutAndDraw ();
        using Runnable modal = new () { Width = Dim.Fill (), Height = 6 };
        SessionToken? modalOwner = nested ? app.Begin (modal) : null;
        Runnable negative = nested ? modal : outer;
        outer.Height = nested ? 2 : 6;
        negative.Y = y;
        app.LayoutAndDraw ();
        bytes.Clear ();
        int changes = 0;
        EventHandler<EventArgs<Rectangle>> changed = (_, _) => changes++;
        app.ScreenChanged += changed;

        try
        {
            for (int pass = 0; pass < 5; pass++)
            {
                app.LayoutAndDraw ();
            }
        }
        finally
        {
            app.ScreenChanged -= changed;
        }

        Assert.Equal (0, changes);
        Assert.Equal (new Rectangle (0, 2, 20, 6), app.Screen);
        Assert.Equal (6, app.Driver!.Rows);
        Assert.Equal (new Point (0, 2), app.Driver.InlinePosition);
        Assert.DoesNotContain ("[2K", bytes.ToString ());
        Assert.DoesNotContain ('\n', bytes.ToString ());
        if (modalOwner is { })
        {
            app.End (modalOwner);
        }
        app.End (outerOwner);
    }

    [Fact]
    public void HiddenModal_DoesNotGrowReservationUntilVisible ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable outer = new () { Width = Dim.Fill (), Height = 8 };
        SessionToken outerOwner = app.Begin (outer)!;
        app.LayoutAndDraw ();
        using Runnable modal = new () { Width = Dim.Fill (), Height = 5 };
        SessionToken modalOwner = app.Begin (modal)!;
        outer.Height = 2;
        app.LayoutAndDraw ();
        Assert.Equal (5, app.Screen.Height);

        modal.Visible = false;
        app.LayoutAndDraw ();
        Assert.Equal (2, app.Screen.Height);
        modal.Height = 9;
        app.LayoutAndDraw ();
        Assert.Equal (2, app.Screen.Height);

        modal.Visible = true;
        app.LayoutAndDraw ();
        Assert.Equal (9, app.Screen.Height);
        app.End (modalOwner);
        app.End (outerOwner);
    }

    [Theory]
    [InlineData (false, 6, 6)]
    [InlineData (true, 6, 6)]
    [InlineData (false, 20, 12)]
    [InlineData (true, 20, 12)]
    public void BottomAnchoredRunnable_CanRegrowAfterShrink (bool nested, int height, int expectedHeight)
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable outer = new () { Width = Dim.Fill (), Height = 8 };
        SessionToken outerOwner = app.Begin (outer)!;
        app.LayoutAndDraw ();
        outer.Height = 2;
        app.LayoutAndDraw ();
        Assert.Equal (2, app.Screen.Height);

        using Runnable modal = new () { Width = Dim.Fill (), Height = 2 };
        SessionToken? modalOwner = nested ? app.Begin (modal) : null;
        Runnable anchored = nested ? modal : outer;
        anchored.Y = Pos.AnchorEnd ();
        app.LayoutAndDraw ();
        anchored.Height = height;

        app.LayoutAndDraw ();

        Assert.Equal (expectedHeight, app.Screen.Height);
        Assert.Equal (expectedHeight, app.Driver!.Rows);
        Assert.Equal (expectedHeight - height, anchored.Frame.Y);
        app.LayoutAndDraw ();
        Assert.Equal (expectedHeight, app.Screen.Height);
        if (modalOwner is { })
        {
            app.End (modalOwner);
        }
        app.End (outerOwner);
    }

    [Fact]
    public void ContentHeightFunction_CanShrinkAfterLayoutInvalidation ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        int height = 6;
        using Runnable composer = new () { Width = Dim.Fill (), Height = Dim.Func (_ => height) };
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();
        height = 2;
        composer.SetNeedsLayout ();

        app.LayoutAndDraw ();

        Assert.Equal (2, app.Screen.Height);
        app.End (owner);
    }

    [Fact]
    public void VisiblePopover_DefersShrinkUntilDismissed ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 6 };
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();
        using Popover<TextField, string> popover = new () { Width = 10, Height = 2 };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);
        composer.Height = 2;

        app.LayoutAndDraw ();
        Assert.Equal (6, app.Screen.Height);
        app.Popovers.Hide (popover);
        app.Popovers.DeRegister (popover);
        app.LayoutAndDraw ();

        Assert.Equal (2, app.Screen.Height);
        app.End (owner);
    }

    [Fact]
    public void PartialWidthRegion_DefersWholeRowErasure ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 6 };
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();
        app.Screen = new Rectangle (0, 2, 10, 6);
        composer.Height = 2;
        bytes.Clear ();

        app.LayoutAndDraw ();

        Assert.Equal (6, app.Screen.Height);
        Assert.DoesNotContain ("[2K", bytes.ToString ());
        app.End (owner);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void ShrinkNotification_CanEndSessionOrDisposeWithoutPrintingPendingOutput (bool dispose)
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 6 };
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();
        composer.Height = 2;
        Task pending = app.PrintAboveAsync (owner, "must not print", TestContext.Current.CancellationToken);
        app.ScreenChanged += Stop;
        bytes.Clear ();

        app.LayoutAndDraw ();

        Assert.True (pending.IsCanceled);
        Assert.DoesNotContain ("must not print", bytes.ToString ());

        void Stop (object? _, EventArgs<Rectangle> __)
        {
            app.ScreenChanged -= Stop;
            if (dispose)
            {
                app.Dispose ();
                return;
            }
            app.End (owner);
        }
    }

    [Fact]
    public void LegacyRasterState_DefersShrink ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out AnsiOutput output);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 6 };
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();
        output.GetSixels ().Enqueue (new SixelToRender { SixelData = "\u001bPq~\u001b\\" });
        composer.Height = 2;

        app.LayoutAndDraw ();
        app.LayoutAndDraw ();

        Assert.Equal (6, app.Screen.Height);
        app.End (owner);
    }

    [Fact]
    public void FailedErase_PreservesGeometryFaultsPendingOutputAndSuppressesPlacement ()
    {
        bool fail = false;
        StringBuilder bytes = new ();
        AnsiOutput output = new (chunk =>
        {
            bytes.Append (Encoding.UTF8.GetString (chunk));
            return !fail;
        }, AppModel.Inline);
        output.SetSize (20, 12);
        using ApplicationImpl app = new (new AnsiComponentFactory (output: output)) { AppModel = AppModel.Inline, ForceInlinePosition = new Point (0, 2) };
        app.Init ();
        using Runnable composer = new () { Width = Dim.Fill (), Height = 6 };
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();
        composer.Height = 2;
        Task pending = app.PrintAboveAsync (owner, "completed", TestContext.Current.CancellationToken);
        fail = true;

        app.LayoutAndDraw ();

        Assert.Equal (6, app.Screen.Height);
        Assert.False (output.IsInlinePlacementKnown);
        Assert.True (pending.IsFaulted);
        Assert.IsType<IOException> (pending.Exception!.InnerException);
        fail = false;
        bytes.Clear ();
        app.LayoutAndDraw (true);
        output.SetCursor (new Cursor ());
        Assert.Empty (bytes.ToString ());
        app.End (owner);
    }

    [Fact]
    public void ThrowingScreenSubscriber_LeavesCompactFrameReadyForRepaintAndOutput ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 6, Text = "ready" };
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();
        composer.Height = 2;
        Task pending = app.PrintAboveAsync (owner, "completed", TestContext.Current.CancellationToken);
        EventHandler<EventArgs<Rectangle>> throwing = (_, _) => throw new InvalidOperationException ("subscriber");
        app.ScreenChanged += throwing;
        Assert.Throws<InvalidOperationException> (() => app.LayoutAndDraw ());
        app.ScreenChanged -= throwing;
        Assert.Equal (2, app.Screen.Height);
        Assert.False (pending.IsCompleted);
        bytes.Clear ();

        app.LayoutAndDraw ();

        Assert.True (pending.IsCompletedSuccessfully);
        Assert.Contains ("ready", bytes.ToString ());
        Assert.Contains ("completed", bytes.ToString ());
        app.End (owner);
    }
}

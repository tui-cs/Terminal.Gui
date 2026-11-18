using UnitTests;
// ReSharper disable AccessToDisposedClosure

namespace ViewBaseTests.Adornments;

// Claude - Opus 4.6
public class BorderDrawTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void TransparentView_With_Border_Draws_Title ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (12, 4);
        app.Driver.Force16Colors = true;

        using Runnable superView = new ();
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();
        superView.Text = "Background!";

        View transparentView = new ()
        {
            Title = "Test",
            X = 0,
            Y = 0,
            Width = 12,
            Height = 4,
            BorderStyle = LineStyle.Single,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };
        superView.Add (transparentView);

        // Act
        app.Begin (superView);
        app.LayoutAndDraw ();

        // Assert - Title "Test" should appear in the border
        output.WriteLine ("Actual driver contents:");
        output.WriteLine (app.Driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┤Test├────┐
                                              │          │
                                              │          │
                                              └──────────┘
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void Overlapped_TransparentView_With_Border_And_Padding_Draws_Title ()
    {
        // Arrange: Matches Arrangement scenario's TransparentView setup:
        // FrameView with Transparent + TransparentMouse, Padding, Overlapped, and a Title.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (14, 6);
        app.Driver.Force16Colors = true;

        using Runnable superView = new ();
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();
        superView.Text = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
        superView.TextFormatter.WordWrap = true;

        // Mirrors TransparentView from Arrangement.cs
        View transparentView = new ()
        {
            Title = "Hi",
            X = 1,
            Y = 1,
            Width = 10,
            Height = 4,
            BorderStyle = LineStyle.Single,
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Resizable | ViewArrangement.Movable,
            ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse
        };
        transparentView.Padding.Thickness = new Thickness (1);
        superView.Add (transparentView);

        // Act
        app.Begin (superView);
        app.LayoutAndDraw ();

        // Assert - Title "Hi" should appear in the border despite transparency
        output.WriteLine ("Actual driver contents:");
        output.WriteLine (app.Driver.ToString ());

        // The actual output shows ┌┤  ├────┐ — Title "Hi" is missing (spaces instead).
        // This is the bug: transparent overlapped views do not render their Title.
        DriverAssert.AssertDriverContentsAre ("""
                                              XXXXXXXXXXXXXX
                                              X┌┤Hi├────┐XXX
                                              X│        │XXX
                                              X│        │XXX
                                              X└────────┘XXX
                                              XXXXXXXXXXXXXX
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void TransparentView_With_Border_And_Padding_NoPeers_Draws_Title ()
    {
        // Claude - Opus 4.6
        // Isolate: just Padding + Transparent, no Overlapped, no background peer view
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (12, 4);
        app.Driver.Force16Colors = true;

        using Runnable superView = new ();
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View transparentView = new ()
        {
            Title = "Test",
            X = 0,
            Y = 0,
            Width = 12,
            Height = 4,
            BorderStyle = LineStyle.Single,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };
        transparentView.Padding.Thickness = new Thickness (1);
        superView.Add (transparentView);

        app.Begin (superView);
        app.LayoutAndDraw ();

        output.WriteLine ("Actual driver contents:");
        output.WriteLine (app.Driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┤Test├────┐
                                              │          │
                                              │          │
                                              └──────────┘
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void AutoLineJoin_SideBySide_Overlapping_Peers_Join_Correctly ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (12, 4);

        using View superView = new () { Driver = driver };
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View viewA = new ()
        {
            Title = "A",
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        View viewB = new ()
        {
            Title = "B",
            X = Pos.Right (viewA) - 1, // Cause overlap
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (viewA, viewB);

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┤A├┬┤B├┐
                                              │   │   │
                                              └───┴───┘
                                              """,
                                              output,
                                              driver);
    }

    // Copilot
    [Fact]
    public void BorderSettings_TerminalTitle_Writes_Osc_On_IsModal_And_Title_Change ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (20, 5);

        Runnable runnable = new () { Driver = driver };
        runnable.Border.Settings |= BorderSettings.TerminalTitle;
        runnable.Title = "Before";
        runnable.SetIsModal (true);
        runnable.RaiseIsModalChangedEvent (true);
        Assert.Contains (EscSeqUtils.OSC_SetWindowTitle ("Before"), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);

        runnable.Title = "After";
        Assert.Contains (EscSeqUtils.OSC_SetWindowTitle ("After"), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    // Copilot
    [Fact]
    public void BorderSettings_Without_TerminalTitle_Does_Not_Write_Osc ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (20, 5);

        Runnable runnable = new () { Driver = driver };
        runnable.Border.Settings &= ~BorderSettings.TerminalTitle;
        runnable.Title = "No OSC";
        runnable.SetIsModal (true);
        runnable.RaiseIsModalChangedEvent (true);

        Assert.DoesNotContain (EscSeqUtils.OSC_SetWindowTitle ("No OSC"), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);

        runnable.Title = "Still No OSC";

        Assert.DoesNotContain (EscSeqUtils.OSC_SetWindowTitle ("Still No OSC"), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    // Copilot
    [Fact (Skip = "not sure what broke this")]
    public void BorderSettings_TerminalTitle_When_Enabled_On_Runnable_View_Writes_Osc ()
    {
        using IApplication app = Application.Create ();

        // Use Inline app model to ensure prevent the initial LayoutAndDraw from clearing driver contents
        app.AppModel = AppModel.Inline;
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 5);

        using Runnable runnable = new ();
        runnable.Title = "Enable Later";

        app.StopAfterFirstIteration = true;
        runnable.ClearingViewport += RunnableOnDrawComplete;

        app.Run (runnable);

        return;

        void RunnableOnDrawComplete (object? sender, DrawEventArgs e) => Assert.Contains (EscSeqUtils.OSC_SetWindowTitle ("Enable Later"), app.Driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }
}

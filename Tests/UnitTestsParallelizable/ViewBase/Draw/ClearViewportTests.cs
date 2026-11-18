using Moq;
using UnitTests;

namespace ViewBaseTests.Viewport;

[Trait ("Category", "Output")]
public class ClearViewportTests (ITestOutputHelper output)
{
    public class TestableView : View
    {
        public TestableView ()
        {
            Frame = new Rectangle (0, 0, 10, 10);
        }

        public bool TestOnClearingViewport () { return OnClearingViewport (); }

        public int OnClearingViewportCalled { get; set; }
        public bool CancelOnClearingViewport { get; set; }

        protected override bool OnClearingViewport ()
        {
            OnClearingViewportCalled++;

            return CancelOnClearingViewport;
        }

        public int OnClearedViewportCalled { get; set; }
        protected override void OnClearedViewport () { OnClearedViewportCalled++; }
    }

    [Fact]
    public void DoClearViewport_ViewportIsTransparent_DoesNotClear ()
    {
        // Arrange
        Mock<TestableView> view = new () { CallBase = true };
        view.Object.ViewportSettings = ViewportSettingsFlags.Transparent;

        // Act
        view.Object.DoClearViewport ();

        // Assert
        Assert.Equal (0, view.Object.OnClearingViewportCalled);
        Assert.Equal (0, view.Object.OnClearedViewportCalled);
    }

    [Fact]
    public void DoClearViewport_OnClearingViewportReturnsTrue_DoesNotClear ()
    {
        // Arrange
        Mock<TestableView> view = new () { CallBase = true };
        view.Object.CancelOnClearingViewport = true;

        // Act
        view.Object.DoClearViewport ();

        // Assert
        Assert.Equal (0, view.Object.OnClearedViewportCalled);
    }

    [Fact]
    public void DoClearViewport_ClearingViewportEventCancelled_DoesNotClear ()
    {
        // Arrange
        Mock<TestableView> view = new () { CallBase = true };
        view.Object.ClearingViewport += (sender, e) => e.Cancel = true;

        // Act
        view.Object.DoClearViewport ();

        // Assert
        Assert.Equal (0, view.Object.OnClearedViewportCalled);
    }

    [Fact]
    public void DoClearViewport_ClearsViewport ()
    {
        // Arrange
        Mock<TestableView> view = new () { CallBase = true };

        // Act
        view.Object.SetNeedsDraw ();
        view.Object.DoClearViewport ();

        // Assert
        Assert.Equal (1, view.Object.OnClearedViewportCalled);
    }

    [Fact]
    public void DoClearViewport_RaisesClearingViewportEvent ()
    {
        // Arrange
        Mock<TestableView> view = new () { CallBase = true };
        var eventRaised = false;
        view.Object.ClearingViewport += (sender, e) => eventRaised = true;

        // Act
        view.Object.SetNeedsDraw ();
        view.Object.DoClearViewport ();

        // Assert
        Assert.True (eventRaised);
    }

    [Fact]
    public void Clear_ClearsEntireViewport ()
    {
        using IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var superView = new Runnable
        {
            Width = Dim.Fill (), Height = Dim.Fill ()
        };

        var view = new View
        {
            Text = "X",
            X = 1, Y = 1,
            Width = 3, Height = 3,
            BorderStyle = LineStyle.Single
        };
        superView.Add (view);
        app.Begin (superView);
        superView.LayoutSubViews ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │X│
 └─┘",
                                                       output,
                                                       app.Driver);

        // On Draw exit the view is excluded from the clip, so this will do nothing.
        view.ClearViewport ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │X│
 └─┘",
                                                       output,
                                                       app.Driver);


        view.SetClipToScreen ();

        view.ClearViewport ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │ │
 └─┘",
                                                       output,
                                                       app.Driver);
    }

    [Fact]
    public void Clear_WithClearVisibleContentOnly_ClearsVisibleContentOnly ()
    {
        using IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var superView = new Runnable
        {
            Width = Dim.Fill (), Height = Dim.Fill ()
        };

        var view = new View
        {
            Text = "X",
            X = 1, Y = 1,
            Width = 3, Height = 3,
            BorderStyle = LineStyle.Single,
            ViewportSettings = ViewportSettingsFlags.ClearContentOnly
        };
        superView.Add (view);
        app.Begin (superView);
        superView.LayoutSubViews ();

        superView.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │X│
 └─┘",
                                                       output,
                                                       app.Driver);
        view.SetClipToScreen ();
        view.ClearViewport ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │ │
 └─┘",
                                                       output,
                                                       app.Driver);
    }

    [Fact]
    public void Clear_Viewport_Can_Use_Driver_AddRune_Or_AddStr_Methods ()
    {
        using IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var view = new FrameView {  Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single };

        view.DrawingContent += (s, e) =>
                               {
                                   Region? savedClip = view.AddViewportToClip ();

                                   for (var row = 0; row < view.Viewport.Height; row++)
                                   {
                                       app.Driver?.Move (1, row + 1);

                                       for (var col = 0; col < view.Viewport.Width; col++)
                                       {
                                           app.Driver?.AddStr ($"{col}");
                                       }
                                   }

                                   view.SetClip (savedClip);
                                   e.Cancel = true;
                               };
        var top = new Runnable ();
        top.Add (view);
        app.Begin (top);
        app.Driver!.SetScreenSize (20, 10);
        app.LayoutAndDraw ();

        var expected = @"
┌──────────────────┐
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
└──────────────────┘
"
            ;

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output, app.Driver);
        Assert.Equal (new (0, 0, 20, 10), pos);

        view.FillRect (view.Viewport);

        expected = @"
┌──────────────────┐
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘
"
            ;

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output, app.Driver);
        top.Dispose ();
    }

    [Fact]
    public void Clear_Can_Use_Driver_AddRune_Or_AddStr_Methods ()
    {
        using IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var view = new FrameView { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single };

        view.DrawingContent += (s, e) =>
                               {
                                   Region? savedClip = view.AddViewportToClip ();

                                   for (var row = 0; row < view.Viewport.Height; row++)
                                   {
                                       app.Driver?.Move (1, row + 1);

                                       for (var col = 0; col < view.Viewport.Width; col++)
                                       {
                                           app.Driver?.AddStr ($"{col}");
                                       }
                                   }

                                   view.SetClip (savedClip);
                                   e.Cancel = true;
                               };
        var top = new Runnable ();
        top.Add (view);
        app.Begin (top);
        app.Driver!.SetScreenSize (20, 10);
        app.LayoutAndDraw ();

        var expected = @"
┌──────────────────┐
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
└──────────────────┘
"
            ;

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output, app.Driver);
        Assert.Equal (new (0, 0, 20, 10), pos);

        view.FillRect (view.Viewport);

        expected = @"
┌──────────────────┐
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘
";

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output, app.Driver);

        top.Dispose ();
    }
}

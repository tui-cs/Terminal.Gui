#nullable disable

using static Terminal.Gui.ViewBase.Dim;
using static Terminal.Gui.ViewBase.Pos;

namespace ViewBaseTests.Layout;

public class PosCombineTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    public void PosCombine_Referencing_Same_View ()
    {
        var super = new View { Width = 10, Height = 10, Text = "super" };
        var view1 = new View { Width = 2, Height = 2, Text = "view1" };
        var view2 = new View { Width = 2, Height = 2, Text = "view2" };
        view2.X = AnchorEnd (0) - (Right (view2) - Left (view2));

        super.Add (view1, view2);
        super.BeginInit ();
        super.EndInit ();

        Exception exception = Record.Exception (super.LayoutSubViews);
        Assert.Null (exception);
        Assert.Equal (new (0, 0, 10, 10), super.Frame);
        Assert.Equal (new (0, 0, 2, 2), view1.Frame);
        Assert.Equal (new (8, 0, 2, 2), view2.Frame);

        super.Dispose ();
    }

    [Fact]
    public void PosCombine_DimCombine_View_With_SubViews ()
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new () { Width = 80, Height = 25 };
        app.Begin (runnable);
        var win1 = new Window { Id = "win1", Width = 20, Height = 10 };

        var view1 = new View
        {
            Text = "view1",
            Width = Auto (DimAutoStyle.Text),
            Height = Auto (DimAutoStyle.Text)
        };
        var win2 = new Window { Id = "win2", Y = Bottom (view1) + 1, Width = 10, Height = 3 };
        var view2 = new View { Id = "view2", Width = Fill (), Height = 1, CanFocus = true };

        var view3 = new View { Id = "view3", Width = Fill (1), Height = 1, CanFocus = true };

        view2.Add (view3);
        win2.Add (view2);
        win1.Add (view1, win2);
        runnable.Add (win1);

        Assert.Equal (new (0, 0, 80, 25), runnable.Frame);
        Assert.Equal (new (0, 0, 5, 1), view1.Frame);
        Assert.Equal (new (0, 0, 20, 10), win1.Frame);
        Assert.Equal (new (0, 2, 10, 3), win2.Frame);
        Assert.Equal (new (0, 0, 8, 1), view2.Frame);
        Assert.Equal (new (0, 0, 7, 1), view3.Frame);
        View foundView = runnable.GetViewsUnderLocation (new (9, 4), ViewportSettingsFlags.None).LastOrDefault ();
        Assert.Equal (foundView, view2);
        runnable.Dispose ();
    }

    [Fact]
    public void PosCombine_Will_Throws ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var t = new Runnable ();

        var w = new Window { X = Left (t) + 2, Y = Top (t) + 2 };
        var f = new FrameView ();
        var v1 = new View { X = Left (w) + 2, Y = Top (w) + 2 };
        var v2 = new View { X = Left (v1) + 2, Y = Top (v1) + 2 };

        f.Add (v1); // v2 not added
        w.Add (f);
        t.Add (w);

        f.X = X (v2) - X (v1);
        f.Y = Y (v2) - Y (v1);

        app.StopAfterFirstIteration = true;
        Assert.Throws<LayoutException> (() => app.Run (t));
        t.Dispose ();
        v2.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void PosCombine_Refs_SuperView_Throws ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var top = new Runnable ();
        var w = new Window { X = Left (top) + 2, Y = Top (top) + 2 };
        var f = new FrameView ();
        var v1 = new View { X = Left (w) + 2, Y = Top (w) + 2 };
        var v2 = new View { X = Left (v1) + 2, Y = Top (v1) + 2 };

        f.Add (v1, v2);
        w.Add (f);
        top.Add (w);
        SessionToken token = app.Begin (top);

        f.X = X (app.TopRunnableView) + X (v2) - X (v1);
        f.Y = Y (app.TopRunnableView) + Y (v2) - Y (v1);

        app.TopRunnableView!.SubViewsLaidOut += (s, e) =>
                                                {
                                                    Assert.Equal (0, app.TopRunnableView.Frame.X);
                                                    Assert.Equal (0, app.TopRunnableView.Frame.Y);
                                                    Assert.Equal (2, w.Frame.X);
                                                    Assert.Equal (2, w.Frame.Y);
                                                    Assert.Equal (2, f.Frame.X);
                                                    Assert.Equal (2, f.Frame.Y);
                                                    Assert.Equal (4, v1.Frame.X);
                                                    Assert.Equal (4, v1.Frame.Y);
                                                    Assert.Equal (6, v2.Frame.X);
                                                    Assert.Equal (6, v2.Frame.Y);
                                                };

        app.StopAfterFirstIteration = true;

        Assert.Throws<LayoutException> (() => app.Run (top));
        app.TopRunnableView?.Dispose ();
        top.Dispose ();
        app.Dispose ();
    }
}

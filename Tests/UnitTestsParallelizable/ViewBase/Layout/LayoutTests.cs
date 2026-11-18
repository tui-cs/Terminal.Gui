namespace ViewBaseTests.Layout;

public class LayoutTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Dispose_DoesNotThrow ()
    {
        var v = new View ();
        v.Dispose ();
    }

    [Fact]
    public void Constructor_Defaults_Are_Correct ()
    {
        // Tests defaults
        View v = new ();
        Assert.Equal (new Rectangle (0, 0, 0, 0), v.Frame);
        Assert.Equal (new Rectangle (0, 0, 0, 0), v.Viewport);
        Assert.Equal (Pos.Absolute (0), v.X);
        Assert.Equal (Pos.Absolute (0), v.Y);
        Assert.Equal (Dim.Absolute (0), v.Width);
        Assert.Equal (Dim.Absolute (0), v.Height);

        v.Layout ();
        Assert.Equal (new Rectangle (0, 0, 0, 0), v.Frame);
        Assert.Equal (new Rectangle (0, 0, 0, 0), v.Viewport);
        Assert.Equal (Pos.Absolute (0), v.X);
        Assert.Equal (Pos.Absolute (0), v.Y);
        Assert.Equal (Dim.Absolute (0), v.Width);
        Assert.Equal (Dim.Absolute (0), v.Height);
    }

    #endregion Constructor Tests

    [Fact]
    public void Screen_Size_Change_Causes_Layout ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        Runnable<bool>? runnable = new ();
        app.Begin (runnable);

        var view = new View
        {
            X = 3,
            Y = 2,
            Width = 10,
            Height = 1,
            Text = "0123456789"
        };
        runnable.Add (view);

        app.Driver!.SetScreenSize (80, 25);

        Assert.Equal (new Rectangle (0, 0, 80, 25), new Rectangle (0, 0, app.Screen.Width, app.Screen.Height));
        Assert.Equal (new Rectangle (0, 0, app.Screen.Width, app.Screen.Height), runnable.Frame);
        Assert.Equal (new Rectangle (0, 0, 80, 25), runnable.Frame);

        app.Driver!.SetScreenSize (20, 10);
        app.LayoutAndDraw ();
        Assert.Equal (new Rectangle (0, 0, app.Screen.Width, app.Screen.Height), runnable.Frame);

        Assert.Equal (new Rectangle (0, 0, 20, 10), runnable.Frame);
    }

    [Fact]
    public void Set_All_Absolute_Sets_Correctly ()
    {
        Rectangle frame = new (1, 2, 3, 4);
        View v = new () { X = frame.X, Y = frame.Y, Width = frame.Width, Height = frame.Height };
        Assert.Equal (new Rectangle (frame.X, frame.Y, 3, 4), v.Frame);
        Assert.Equal (new Rectangle (0, 0, 3, 4), v.Viewport);
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (3), v.Width);
        Assert.Equal (Dim.Absolute (4), v.Height);

        v.Layout ();
        Assert.Equal (new Rectangle (frame.X, frame.Y, 3, 4), v.Frame);
        Assert.Equal (new Rectangle (0, 0, 3, 4), v.Viewport);
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (3), v.Width);
        Assert.Equal (Dim.Absolute (4), v.Height);
    }

    [Fact]
    public void Add_Does_Not_Call_Layout ()
    {
        var superView = new View { Id = "superView" };
        var view = new View { Id = "view" };
        var layoutStartedRaised = false;
        var layoutCompleteRaised = false;
        superView.SubViewLayout += (sender, e) => layoutStartedRaised = true;
        superView.SubViewsLaidOut += (sender, e) => layoutCompleteRaised = true;

        superView.Add (view);

        Assert.False (layoutStartedRaised);
        Assert.False (layoutCompleteRaised);

        superView.Remove (view);

        superView.Add (view);

        Assert.False (layoutStartedRaised);
        Assert.False (layoutCompleteRaised);
    }

    [Fact]
    public void Set_X_Y_Does_Not_Impact_Dimensions ()
    {
        // Tests that setting X & Y does not change Frame, Viewport, Width, or Height
        Rectangle frame = new (1, 2, 3, 4);

        View v = new () { X = frame.X, Y = frame.Y };
        Assert.Equal (new Rectangle (frame.X, frame.Y, 0, 0), v.Frame);
        Assert.Equal (new Rectangle (0, 0, 0, 0), v.Viewport);
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (0), v.Width);
        Assert.Equal (Dim.Absolute (0), v.Height);

        v.Layout ();
        Assert.Equal (new Rectangle (frame.X, frame.Y, 0, 0), v.Frame);
        Assert.Equal (new Rectangle (0, 0, 0, 0), v.Viewport);
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (0), v.Width);
        Assert.Equal (Dim.Absolute (0), v.Height);
    }

    [Fact]
    public void Change_X_or_Y_Absolute ()
    {
        var frame = new Rectangle (1, 2, 3, 4);
        var newFrame = new Rectangle (10, 20, 3, 4);

        var v = new View { Frame = frame };
        v.X = newFrame.X;
        v.Y = newFrame.Y;
        v.Layout ();
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (new Rectangle (0, 0, newFrame.Width, newFrame.Height), v.Viewport); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal ($"Absolute({newFrame.X})", v.X.ToString ());
        Assert.Equal ($"Absolute({newFrame.Y})", v.Y.ToString ());
        Assert.Equal (Dim.Absolute (3), v.Width);
        Assert.Equal (Dim.Absolute (4), v.Height);
        v.Dispose ();
    }

    [Fact]
    public void Change_X_Y_Height_Width_Absolute ()
    {
        var v = new View { Frame = Rectangle.Empty };
        v.X = 1;
        v.Y = 2;
        v.Height = 3;
        v.Width = 4;
        v.Dispose ();

        v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();
        v.Dispose ();

        v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();

        v.X = 1;
        v.Dispose ();

        v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();

        v.Y = 2;
        v.Dispose ();

        v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();

        v.Width = 3;
        v.Dispose ();

        v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();

        v.Height = 3;
        v.Dispose ();

        v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();

        v.X = 1;
        v.Y = 2;
        v.Height = 3;
        v.Width = 4;
        v.Dispose ();
    }

    /// <summary>This is an intentionally obtuse test. See https://github.com/tui-cs/Terminal.Gui/issues/2461</summary>
    [Fact]
    public void Does_Not_Throw_If_Nested_SubViews_Ref_Topmost_SuperView ()
    {
        var t = new View { Width = 80, Height = 25, Text = "top" };

        var w = new Window
        {
            Width = Dim.Width (t) - 2, // 78
            Height = Dim.Height (t) - 2 // 23
        };
        var f = new FrameView ();

        var v1 = new View
        {
            Width = Dim.Width (w) - 2, // 76
            Height = Dim.Height (w) - 2 // 21
        };

        var v2 = new View
        {
            Width = Dim.Width (v1) - 2, // 74
            Height = Dim.Height (v1) - 2 // 19
        };

        f.Width = Dim.Width (t) - Dim.Width (w) + 4; // 80 - 74 = 6
        f.Height = Dim.Height (t) - Dim.Height (w) + 4; // 25 - 19 = 6

        f.Add (v1, v2);
        w.Add (f);
        t.Add (w);
        t.BeginInit ();
        t.EndInit ();

        // f references t and w here; t is f's super-superview and w is f's superview. This is supported!
        Exception? exception = Record.Exception (() => t.Layout ());
        Assert.Null (exception);
        Assert.Equal (80, t.Frame.Width);
        Assert.Equal (25, t.Frame.Height);
        Assert.Equal (78, w.Frame.Width);
        Assert.Equal (23, w.Frame.Height);
        Assert.Equal (6, f.Frame.Width);
        Assert.Equal (6, f.Frame.Height);
        Assert.Equal (76, v1.Frame.Width);
        Assert.Equal (21, v1.Frame.Height);
        Assert.Equal (74, v2.Frame.Width);
        Assert.Equal (19, v2.Frame.Height);
        t.Dispose ();
    }

    [Fact]
    public void LayoutSubViews ()
    {
        var superRect = new Rectangle (0, 0, 100, 100);
        var super = new View { Frame = superRect, Text = "super" };
        var v1 = new View { X = 0, Y = 0, Width = 10, Height = 10 };

        var v2 = new View { X = 10, Y = 10, Width = 10, Height = 10 };

        super.Add (v1, v2);

        super.LayoutSubViews ();
        Assert.Equal (new Rectangle (0, 0, 10, 10), v1.Frame);
        Assert.Equal (new Rectangle (10, 10, 10, 10), v2.Frame);
        super.Dispose ();
    }

    [Fact]
    public void LayoutSubViews_Honors_IsLayoutNeeded ()
    {
        // No adornment subviews
        var superView = new View ();
        var view = new View ();
        view.BorderStyle = LineStyle.None; // Calls GetOrCreateView

        var layoutStartedCount = 0;
        var layoutCompleteCount = 0;

        var borderLayoutStartedCount = 0;
        var borderLayoutCompleteCount = 0;

        view.SubViewLayout += (sender, e) => layoutStartedCount++;
        view.SubViewsLaidOut += (sender, e) => layoutCompleteCount++;

        view.Border.View!.SubViewLayout += (sender, e) => borderLayoutStartedCount++;
        view.Border.View!.SubViewsLaidOut += (sender, e) => borderLayoutCompleteCount++;

        superView.Add (view);

        superView.LayoutSubViews ();
        Assert.Equal (0, borderLayoutStartedCount);
        Assert.Equal (0, borderLayoutCompleteCount);
        Assert.Equal (1, layoutStartedCount);
        Assert.Equal (1, layoutCompleteCount);

        superView.LayoutSubViews ();
        Assert.Equal (0, borderLayoutStartedCount);
        Assert.Equal (0, borderLayoutCompleteCount);
        Assert.Equal (1, layoutStartedCount);
        Assert.Equal (1, layoutCompleteCount);

        superView.SetNeedsLayout ();
        superView.LayoutSubViews ();
        Assert.Equal (0, borderLayoutStartedCount);
        Assert.Equal (0, borderLayoutCompleteCount);
        Assert.Equal (2, layoutStartedCount);
        Assert.Equal (2, layoutCompleteCount);

        // With Border subview
        view.Border.GetOrCreateView ().Add (new View ());
        superView.LayoutSubViews ();
        Assert.Equal (1, borderLayoutStartedCount);
        Assert.Equal (1, borderLayoutCompleteCount);
        Assert.Equal (3, layoutStartedCount);
        Assert.Equal (3, layoutCompleteCount);

        superView.LayoutSubViews ();
        Assert.Equal (1, borderLayoutStartedCount);
        Assert.Equal (1, borderLayoutCompleteCount);
        Assert.Equal (3, layoutStartedCount);
        Assert.Equal (3, layoutCompleteCount);

        superView.SetNeedsLayout ();
        superView.LayoutSubViews ();
        Assert.Equal (2, borderLayoutStartedCount);
        Assert.Equal (2, borderLayoutCompleteCount);
        Assert.Equal (4, layoutStartedCount);

        superView.Dispose ();
    }

    // Test OnLayoutStarted/OnLayoutComplete - ensure that they are called at right times
    [Fact]
    public void LayoutSubViews_LayoutStarted_Complete ()
    {
        var superView = new View ();
        var view = new View ();
        view.BorderStyle = LineStyle.None; // Calls GetOrCreateView

        var layoutStartedCount = 0;
        var layoutCompleteCount = 0;

        var borderLayoutStartedCount = 0;
        var borderLayoutCompleteCount = 0;

        view.SubViewLayout += (sender, e) => layoutStartedCount++;
        view.SubViewsLaidOut += (sender, e) => layoutCompleteCount++;

        view.Border.View!.SubViewLayout += (sender, e) => borderLayoutStartedCount++;
        view.Border.View!.SubViewsLaidOut += (sender, e) => borderLayoutCompleteCount++;

        superView.Add (view);
        Assert.Equal (0, borderLayoutStartedCount);
        Assert.Equal (0, borderLayoutCompleteCount);
        Assert.Equal (0, layoutStartedCount);
        Assert.Equal (0, layoutCompleteCount);

        superView.BeginInit ();
        Assert.Equal (0, borderLayoutStartedCount);
        Assert.Equal (0, borderLayoutCompleteCount);
        Assert.Equal (0, layoutStartedCount);
        Assert.Equal (0, layoutCompleteCount);

        superView.EndInit ();
        Assert.Equal (1, borderLayoutStartedCount);
        Assert.Equal (1, borderLayoutCompleteCount);
        Assert.Equal (2, layoutStartedCount);
        Assert.Equal (2, layoutCompleteCount);

        superView.LayoutSubViews ();
        Assert.Equal (1, borderLayoutStartedCount);
        Assert.Equal (1, borderLayoutCompleteCount);
        Assert.Equal (3, layoutStartedCount);
        Assert.Equal (3, layoutCompleteCount);

        superView.SetNeedsLayout ();
        superView.LayoutSubViews ();
        Assert.Equal (1, borderLayoutStartedCount);
        Assert.Equal (1, borderLayoutCompleteCount);
        Assert.Equal (4, layoutStartedCount);
        Assert.Equal (4, layoutCompleteCount);

        superView.Dispose ();
    }

    [Fact]
    public void LayoutSubViews_No_SuperView ()
    {
        var root = new View ();

        var first = new View
        {
            Id = "first",
            X = 1,
            Y = 2,
            Height = 3,
            Width = 4
        };
        root.Add (first);

        var second = new View { Id = "second" };
        root.Add (second);

        second.X = Pos.Right (first) + 1;

        root.LayoutSubViews ();

        Assert.Equal (6, second.Frame.X);
        root.Dispose ();
        first.Dispose ();
        second.Dispose ();
    }

    [Fact]
    public void LayoutSubViews_Raises_LayoutStarted_LayoutComplete ()
    {
        var superView = new View { Id = "superView" };
        var layoutStartedRaised = 0;
        var layoutCompleteRaised = 0;
        superView.SubViewLayout += (sender, e) => layoutStartedRaised++;
        superView.SubViewsLaidOut += (sender, e) => layoutCompleteRaised++;

        superView.SetNeedsLayout ();
        superView.LayoutSubViews ();
        Assert.Equal (1, layoutStartedRaised);
        Assert.Equal (1, layoutCompleteRaised);

        superView.BeginInit ();
        superView.EndInit ();
        superView.LayoutSubViews ();
        Assert.Equal (2, layoutStartedRaised);
        Assert.Equal (2, layoutCompleteRaised);
    }

    [Fact]
    public void LayoutSubViews_Adornment_Views_Raises_LayoutStarted_LayoutComplete ()
    {
        var superView = new View { Id = "superView" };
        var layoutStartedRaised = 0;
        var layoutCompleteRaised = 0;

        superView.Border.GetOrCreateView ();
        superView.Padding.GetOrCreateView ();
        superView.Margin.GetOrCreateView ();

        superView.SubViewLayout += (sender, e) => layoutStartedRaised++;
        superView.SubViewsLaidOut += (sender, e) => layoutCompleteRaised++;

        superView.SetNeedsLayout ();
        superView.LayoutSubViews ();
        Assert.Equal (1, layoutStartedRaised);
        Assert.Equal (1, layoutCompleteRaised);

        superView.BeginInit ();
        superView.EndInit ();
        superView.LayoutSubViews ();
        Assert.Equal (3, layoutStartedRaised);
        Assert.Equal (3, layoutCompleteRaised);
    }

    [Fact]
    public void LayoutSubViews_RootHas_SuperView ()
    {
        var top = new View ();
        var root = new View ();
        top.Add (root);

        var first = new View
        {
            Id = "first",
            X = 1,
            Y = 2,
            Height = 3,
            Width = 4
        };
        root.Add (first);

        var second = new View { Id = "second" };
        root.Add (second);

        second.X = Pos.Right (first) + 1;

        root.LayoutSubViews ();

        Assert.Equal (6, second.Frame.X);
        root.Dispose ();
        top.Dispose ();
        first.Dispose ();
        second.Dispose ();
    }

    [Fact]
    public void LayoutSubViews_Uses_ContentSize ()
    {
        var superView = new View { Width = 5, Height = 5 };
        superView.SetContentSize (new Size (10, 10));

        var view = new View { X = Pos.Center () };
        superView.Add (view);

        superView.LayoutSubViews ();

        Assert.Equal (5, view.Frame.X);
        superView.Dispose ();
    }

    [Fact]
    public void LayoutSubViews_ViewThatRefsSubView_Throws ()
    {
        var root = new View ();
        var super = new View ();
        root.Add (super);
        var sub = new View ();
        super.Add (sub);
        super.Width = Dim.Width (sub);
        Assert.Throws<LayoutException> (() => root.Layout ());
        root.Dispose ();
        super.Dispose ();
    }

    /// <summary>
    ///     This tests the special case in LayoutSubViews. See https://github.com/tui-cs/Terminal.Gui/issues/2461
    /// </summary>
    [Fact]
    public void Nested_SubViews_Ref_Topmost_SuperView ()
    {
        var superView = new View { Width = 80, Height = 25, Text = "superView" };

        var subView1 = new View
        {
            Id = "subView1 - refs superView",
            Width = Dim.Width (superView) - 2, // 78
            Height = Dim.Height (superView) - 2 // 23
        };
        superView.Add (subView1);

        var subView1OfSubView1 = new View
        {
            Id = "subView1OfSubView1 - refs superView",
            Width = Dim.Width (superView) - 4, // 76
            Height = Dim.Height (superView) - 4 // 21
        };
        subView1.Add (subView1OfSubView1);

        superView.Layout ();

        Assert.Equal (80, superView.Frame.Width);
        Assert.Equal (25, superView.Frame.Height);
        Assert.Equal (78, subView1.Frame.Width);
        Assert.Equal (23, subView1.Frame.Height);

        //Assert.Equal (76, subView2.Frame.Width);
        //Assert.Equal (21, subView2.Frame.Height);

        Assert.Equal (76, subView1OfSubView1.Frame.Width);
        Assert.Equal (21, subView1OfSubView1.Frame.Height);

        superView.Dispose ();
    }

    [Fact]
    public void Set_Height_DimAbsolute_Layout_Is_Implicit ()
    {
        var v = new View ();

        v.Height = 1;
        Assert.False (v.NeedsLayout);
        Assert.Equal (1, v.Frame.Height);

        v.Height = 2;
        Assert.False (v.NeedsLayout);
        Assert.Equal (2, v.Frame.Height);

        v.Height = Dim.Absolute (3);
        Assert.False (v.NeedsLayout);
        Assert.Equal (3, v.Frame.Height);

        v.Height = Dim.Absolute (3) + 1;
        Assert.False (v.NeedsLayout);
        Assert.Equal (4, v.Frame.Height);

        v.Height = 1 + Dim.Absolute (1) + 1;
        Assert.False (v.NeedsLayout);
        Assert.Equal (3, v.Frame.Height);
    }

    [Fact]
    public void Set_Height_Non_DimAbsolute_Explicit_Layout_Required ()
    {
        var v = new View ();

        v.Height = Dim.Auto ();
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Height);

        v.Height = Dim.Percent (50);
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Height);

        v.Height = Dim.Fill ();
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Height);

        v.Height = Dim.Func (_ => 10);
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Height);

        v.Height = Dim.Height (new View ());
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Height);
    }

    [Fact]
    public void Set_Width_DimAbsolute_Layout_Is_Implicit ()
    {
        var v = new View ();

        v.Width = 1;
        Assert.False (v.NeedsLayout);
        Assert.Equal (1, v.Frame.Width);

        v.Width = 2;
        Assert.False (v.NeedsLayout);
        Assert.Equal (2, v.Frame.Width);

        v.Width = Dim.Absolute (3);
        Assert.False (v.NeedsLayout);
        Assert.Equal (3, v.Frame.Width);

        v.Width = Dim.Absolute (3) + 1;
        Assert.False (v.NeedsLayout);
        Assert.Equal (4, v.Frame.Width);

        v.Width = 1 + Dim.Absolute (1) + 1;
        Assert.False (v.NeedsLayout);
        Assert.Equal (3, v.Frame.Width);
    }

    [Fact]
    public void Set_Width_Non_DimAbsolute_Explicit_Layout_Required ()
    {
        var v = new View ();

        v.Width = Dim.Auto ();
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Width);

        v.Width = Dim.Percent (50);
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Width);

        v.Width = Dim.Fill ();
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Width);

        v.Width = Dim.Func (_ => 10);
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Width);

        v.Width = Dim.Width (new View ());
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Width);
    }

    [Fact]
    public void Set_X_Non_PosAbsolute_Explicit_Layout_Required ()
    {
        var v = new View ();

        v.X = Pos.Center ();
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.X);

        v.X = Pos.Percent (50);
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.X);

        v.X = Pos.Align (Alignment.Center);
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.X);

        v.X = Pos.Func (_ => 10);
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.X);

        v.X = Pos.AnchorEnd ();
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.X);

        v.X = Pos.Top (new View ());
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.X);
    }

    [Fact]
    public void Set_X_PosAbsolute_Layout_Is_Implicit ()
    {
        var v = new View ();

        v.X = 1;
        Assert.False (v.NeedsLayout);
        Assert.Equal (1, v.Frame.X);

        v.X = 2;
        Assert.False (v.NeedsLayout);
        Assert.Equal (2, v.Frame.X);

        v.X = Pos.Absolute (3);
        Assert.False (v.NeedsLayout);
        Assert.Equal (3, v.Frame.X);

        v.X = Pos.Absolute (3) + 1;
        Assert.False (v.NeedsLayout);
        Assert.Equal (4, v.Frame.X);

        v.X = 1 + Pos.Absolute (1) + 1;
        Assert.False (v.NeedsLayout);
        Assert.Equal (3, v.Frame.X);
    }

    [Fact]
    public void Set_Y_Non_PosAbsolute_Explicit_Layout_Required ()
    {
        var v = new View ();

        v.Y = Pos.Center ();
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Y);

        v.Y = Pos.Percent (50);
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Y);

        v.Y = Pos.Align (Alignment.Center);
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Y);

        v.Y = Pos.Func (_ => 10);
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Y);

        v.Y = Pos.AnchorEnd ();
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Y);

        v.Y = Pos.Top (new View ());
        Assert.True (v.NeedsLayout);
        Assert.Equal (0, v.Frame.Y);
    }

    [Fact]
    public void Set_Y_PosAbsolute_Layout_Is_Implicit ()
    {
        var v = new View ();

        v.Y = 1;
        Assert.False (v.NeedsLayout);
        Assert.Equal (1, v.Frame.Y);

        v.Y = 2;
        Assert.False (v.NeedsLayout);
        Assert.Equal (2, v.Frame.Y);

        v.Y = Pos.Absolute (3);
        Assert.False (v.NeedsLayout);
        Assert.Equal (3, v.Frame.Y);

        v.Y = Pos.Absolute (3) + 1;
        Assert.False (v.NeedsLayout);
        Assert.Equal (4, v.Frame.Y);

        v.Y = 1 + Pos.Absolute (1) + 1;
        Assert.False (v.NeedsLayout);
        Assert.Equal (3, v.Frame.Y);
    }
}

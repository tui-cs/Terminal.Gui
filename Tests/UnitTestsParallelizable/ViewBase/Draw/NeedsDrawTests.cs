using UnitTests;

namespace ViewBaseTests.Drawing;

[Trait ("Category", "Output")]
public class NeedsDrawTests : TestDriverBase
{
    [Fact]
    public void NeedsDraw_False_If_Width_Height_Zero ()
    {
        View view = new () { Width = 0, Height = 0 };
        view.BeginInit ();
        view.EndInit ();
        Assert.False (view.NeedsDraw);

        //Assert.False (view.SubViewNeedsDraw);
    }

    [Fact]
    public void NeedsDraw_True_Initially_If_Width_Height_Not_Zero ()
    {
        View superView = new () { Driver = CreateTestDriver (), Width = 1, Height = 1 };
        View view1 = new () { Width = 1, Height = 1 };
        View view2 = new () { Width = 1, Height = 1 };

        superView.Add (view1, view2);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.True (superView.NeedsDraw);
        Assert.True (superView.SubViewNeedsDraw);
        Assert.True (view1.NeedsDraw);
        Assert.True (view2.NeedsDraw);

        superView.Layout (); // NeedsDraw is always false if Layout is needed

        superView.Draw ();

        Assert.False (superView.NeedsDraw);
        Assert.False (superView.SubViewNeedsDraw);
        Assert.False (view1.NeedsDraw);
        Assert.False (view2.NeedsDraw);

        superView.SetNeedsDraw ();

        Assert.True (superView.NeedsDraw);
        Assert.True (superView.SubViewNeedsDraw);
        Assert.True (view1.NeedsDraw);
        Assert.True (view2.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_True_After_Constructor ()
    {
        var view = new View { Width = 2, Height = 2 };
        Assert.True (view.NeedsDraw);

        view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_True_After_BeginInit ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDraw);

        view.BeginInit ();
        Assert.True (view.NeedsDraw);

        view.ClearNeedsDraw ();

        view.BeginInit ();
        Assert.False (view.NeedsDraw); // Because layout is still needed

        view.Layout ();

        // NeedsDraw is true after layout and NeedsLayout is false if SubViewsLaidOut doesn't call SetNeedsLayout
        Assert.True (view.NeedsDraw);
        Assert.False (view.NeedsLayout);
    }

    [Fact]
    public void NeedsDraw_True_After_EndInit_Where_Call_Layout ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDraw);

        view.BeginInit ();
        Assert.True (view.NeedsDraw);

        view.EndInit ();
        Assert.True (view.NeedsDraw);

        view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        view.BeginInit ();
        view.ClearNeedsDraw ();
        view.EndInit ();
        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_After_SetLayoutNeeded_And_Layout ()
    {
        var view = new View { Driver = CreateTestDriver (), Width = 2, Height = 2 };
        Assert.True (view.NeedsDraw);
        Assert.False (view.NeedsLayout);

        view.Draw ();
        Assert.False (view.NeedsDraw);
        Assert.False (view.NeedsLayout);

        view.SetNeedsLayout ();
        Assert.False (view.NeedsDraw);
        Assert.True (view.NeedsLayout);

        view.Layout ();
        Assert.True (view.NeedsDraw);
        Assert.False (view.NeedsLayout);
    }

    [Fact]
    public void NeedsDraw_False_After_SetRelativeLayout_Absolute_Dims ()
    {
        var view = new View { Driver = CreateTestDriver (), Width = 2, Height = 2 };
        Assert.True (view.NeedsDraw);

        view.Draw ();
        Assert.False (view.NeedsDraw);
        Assert.False (view.NeedsLayout);

        // SRL won't change anything since the view frame wasn't changed
        view.SetRelativeLayout (new Size (100, 100));
        Assert.False (view.NeedsDraw);

        view.SetNeedsLayout ();

        // SRL won't change anything since the view frame wasn't changed
        // SRL doesn't depend on NeedsLayout, but LayoutSubViews does
        view.SetRelativeLayout (new Size (100, 100));
        Assert.False (view.NeedsDraw);
        Assert.True (view.NeedsLayout);

        view.Layout ();
        Assert.True (view.NeedsDraw);
        Assert.False (view.NeedsLayout);

        view.ClearNeedsDraw ();

        // SRL won't change anything since the view frame wasn't changed. However, Layout has not been called
        view.SetRelativeLayout (new Size (10, 10));
        Assert.False (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_False_After_SetRelativeLayout_Relative_Dims ()
    {
        var view = new View { Width = Dim.Percent (50), Height = Dim.Percent (50) };

        View superView = new () { Id = "superView", Width = Dim.Fill (), Height = Dim.Fill () };

        // A layout wasn't called yet, so NeedsDraw is still empty
        Assert.False (superView.NeedsDraw);

        superView.Add (view);

        // A layout wasn't called yet, so NeedsDraw is still empty
        Assert.False (view.NeedsDraw);
        Assert.False (superView.NeedsDraw);

        superView.BeginInit ();
        Assert.False (view.NeedsDraw);
        Assert.False (superView.NeedsDraw);

        superView.EndInit (); // Call Layout
        Assert.True (view.NeedsDraw);
        Assert.True (superView.NeedsDraw);

        superView.SetRelativeLayout (new Size (100, 100));
        Assert.True (view.NeedsDraw);
        Assert.True (superView.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_False_After_SetRelativeLayout_10x10 ()
    {
        View superView = new () { Id = "superView", Width = Dim.Fill (), Height = Dim.Fill () };
        Assert.False (superView.NeedsDraw);

        superView.Layout ();
        Assert.True (superView.NeedsDraw);

        superView.ClearNeedsDraw ();
        superView.SetRelativeLayout (new Size (10, 10));
        Assert.True (superView.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_True_After_LayoutSubViews ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDraw);

        view.BeginInit ();
        Assert.True (view.NeedsDraw);

        view.EndInit ();
        Assert.True (view.NeedsDraw);

        view.SetRelativeLayout (new Size (100, 100));
        Assert.True (view.NeedsDraw);

        view.LayoutSubViews ();
        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_False_After_Draw ()
    {
        var view = new View { Driver = CreateTestDriver (), Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDraw);

        view.BeginInit ();
        Assert.True (view.NeedsDraw);

        view.EndInit ();
        Assert.True (view.NeedsDraw);

        view.SetRelativeLayout (new Size (100, 100));
        Assert.True (view.NeedsDraw);

        view.LayoutSubViews ();
        Assert.True (view.NeedsDraw);

        view.Draw ();
        Assert.False (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDrawRect_Is_Viewport_Relative ()
    {
        // Issue #5358: NeedsDrawRect is now a true accumulating union of all dirty regions
        // since the last Draw() / ClearNeedsDraw(). Previously the broken union math clamped
        // each call's result to the current Viewport size, which incidentally produced
        // "current Viewport" semantics. This test asserts the accumulating union semantics.
        View superView = new () { Id = "superView", Width = 10, Height = 10 };
        Assert.Equal (new Rectangle (0, 0, 10, 10), superView.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), superView.Viewport);
        Assert.Equal (new Rectangle (0, 0, 10, 10), superView.NeedsDrawRect);

        var view = new View { Id = "view" };

        view.Frame = new Rectangle (0, 1, 2, 3);
        Assert.Equal (new Rectangle (0, 1, 2, 3), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 2, 3), view.Viewport);
        Assert.Equal (new Rectangle (0, 0, 2, 3), view.NeedsDrawRect);

        superView.Add (view);
        Assert.Equal (new Rectangle (0, 0, 10, 10), superView.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), superView.Viewport);
        Assert.Equal (new Rectangle (0, 0, 10, 10), superView.NeedsDrawRect);
        Assert.Equal (new Rectangle (0, 1, 2, 3), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 2, 3), view.Viewport);
        Assert.Equal (new Rectangle (0, 0, 2, 3), view.NeedsDrawRect);

        view.Frame = new Rectangle (3, 3, 5, 5);
        Assert.Equal (new Rectangle (3, 3, 5, 5), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 5, 5), view.Viewport);
        // Union((0,0,2,3),(0,0,5,5)) = (0,0,5,5)
        Assert.Equal (new Rectangle (0, 0, 5, 5), view.NeedsDrawRect);

        view.Frame = new Rectangle (3, 3, 6, 6); // Grow right/bottom 1
        Assert.Equal (new Rectangle (3, 3, 6, 6), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 6, 6), view.Viewport);
        // Union((0,0,5,5),(0,0,6,6)) = (0,0,6,6)
        Assert.Equal (new Rectangle (0, 0, 6, 6), view.NeedsDrawRect);

        view.Frame = new Rectangle (3, 3, 5, 5); // Shrink right/bottom 1
        Assert.Equal (new Rectangle (3, 3, 5, 5), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 5, 5), view.Viewport);
        // Union((0,0,6,6),(0,0,5,5)) = (0,0,6,6) — accumulates the prior larger area.
        Assert.Equal (new Rectangle (0, 0, 6, 6), view.NeedsDrawRect);

        view.SetContentSize (new Size (10, 10));
        Assert.Equal (new Rectangle (3, 3, 5, 5), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 5, 5), view.Viewport);
        // SetContentSize only calls SetNeedsLayout, not SetNeedsDraw. NeedsDrawRect unchanged.
        Assert.Equal (new Rectangle (0, 0, 6, 6), view.NeedsDrawRect);

        view.Viewport = new Rectangle (1, 1, 5, 5); // Scroll up/left 1
        Assert.Equal (new Rectangle (3, 3, 5, 5), view.Frame);
        Assert.Equal (new Rectangle (1, 1, 5, 5), view.Viewport);
        // Scroll calls SetNeedsLayout (not SetNeedsDraw). NeedsDrawRect unchanged.
        Assert.Equal (new Rectangle (0, 0, 6, 6), view.NeedsDrawRect);

        view.Frame = new Rectangle (3, 3, 6, 6); // Grow right/bottom 1
        Assert.Equal (new Rectangle (3, 3, 6, 6), view.Frame);
        Assert.Equal (new Rectangle (1, 1, 6, 6), view.Viewport);
        // Issue #5359: NeedsDrawRect is viewport-LOCAL — independent of Viewport.Location
        // (the scroll offset). SetFrame's SetNeedsDraw passes (0, 0, 6, 6), not (1, 1, 6, 6).
        // Union((0,0,6,6),(0,0,6,6)) = (0,0,6,6).
        Assert.Equal (new Rectangle (0, 0, 6, 6), view.NeedsDrawRect);

        view.Frame = new Rectangle (3, 3, 5, 5);
        Assert.Equal (new Rectangle (3, 3, 5, 5), view.Frame);
        Assert.Equal (new Rectangle (1, 1, 5, 5), view.Viewport);
        // Union((0,0,6,6),(0,0,5,5)) = (0,0,6,6).
        Assert.Equal (new Rectangle (0, 0, 6, 6), view.NeedsDrawRect);
    }

    [Fact]
    public void ClearNeedsDraw_ClearsOwnFlags ()
    {
        // Verify that ClearNeedsDraw properly clears the view's own flags
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 0,
            Y = 0,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.True (view.NeedsDraw);
        Assert.Equal (view.Viewport, view.NeedsDrawRect);

        view.Draw ();

        Assert.False (view.NeedsDraw);
        Assert.Equal (Rectangle.Empty, view.NeedsDrawRect);
        Assert.False (view.SubViewNeedsDraw);
    }

    [Fact]
    public void ClearNeedsDraw_ClearsAdornments ()
    {
        // Verify that ClearNeedsDraw clears adornment flags
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 0,
            Y = 0,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.Border.Thickness = new Thickness (1);
        view.BorderStyle = LineStyle.Single; // Force GetOrCreateView
        view.Padding.Thickness = new Thickness (1);
        view.Padding.GetOrCreateView ();
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.True (view.Border.View?.NeedsDraw);
        Assert.True (view.Padding.View?.NeedsDraw == true);

        view.Draw ();

        Assert.False (view.Border.View?.NeedsDraw);
        Assert.False (view.Padding.View?.NeedsDraw == true);
    }

    [Fact]
    public void ClearNeedsDraw_PropagatesDownToAllSubViews ()
    {
        // Verify that ClearNeedsDraw clears flags on all descendants
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        var topView = new View
        {
            X = 0,
            Y = 0,
            Width = 100,
            Height = 100,
            Driver = driver
        };

        var middleView = new View { X = 10, Y = 10, Width = 50, Height = 50 };
        var bottomView = new View { X = 5, Y = 5, Width = 20, Height = 20 };

        topView.Add (middleView);
        middleView.Add (bottomView);
        topView.BeginInit ();
        topView.EndInit ();
        topView.LayoutSubViews ();

        Assert.True (topView.NeedsDraw);
        Assert.True (middleView.NeedsDraw);
        Assert.True (bottomView.NeedsDraw);

        topView.Draw ();

        Assert.False (topView.NeedsDraw);
        Assert.False (topView.SubViewNeedsDraw);
        Assert.False (middleView.NeedsDraw);
        Assert.False (middleView.SubViewNeedsDraw);
        Assert.False (bottomView.NeedsDraw);
    }

    #region NeedsDraw Tests

    [Fact]
    public void NeedsDraw_InitiallyFalse_WhenNotVisible ()
    {
        var view = new View { Visible = false };
        view.BeginInit ();
        view.EndInit ();

        Assert.False (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_TrueAfterSetNeedsDraw ()
    {
        var view = new View { X = 0, Y = 0, Width = 10, Height = 10 };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetNeedsDraw ();

        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_ClearedAfterDraw ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetNeedsDraw ();
        Assert.True (view.NeedsDraw);

        view.Draw ();

        Assert.False (view.NeedsDraw);
    }

    [Fact]
    public void SetNeedsDraw_WithRectangle_UpdatesNeedsDrawRect ()
    {
        var view = new View
        {
            Driver = CreateTestDriver (),
            X = 0,
            Y = 0,
            Width = 20,
            Height = 20
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // After layout, view will have NeedsDrawRect set to the viewport
        // We need to clear it first
        view.Draw ();
        Assert.False (view.NeedsDraw);
        Assert.Equal (Rectangle.Empty, view.NeedsDrawRect);

        var rect = new Rectangle (5, 5, 10, 10);
        view.SetNeedsDraw (rect);

        Assert.True (view.NeedsDraw);
        Assert.Equal (rect, view.NeedsDrawRect);
    }

    [Fact]
    public void SetNeedsDraw_MultipleRectangles_Expands ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 0,
            Y = 0,
            Width = 30,
            Height = 30,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // After layout, clear NeedsDraw
        view.Draw ();
        Assert.False (view.NeedsDraw);

        view.SetNeedsDraw (new Rectangle (5, 5, 10, 10));
        view.SetNeedsDraw (new Rectangle (15, 15, 10, 10));

        // Issue #5358: NeedsDrawRect is now a true union of all SetNeedsDraw regions,
        // not a max-of-Viewport-and-region clamp. Previously the broken union math
        // expanded to viewport size on any second call; that masked partial-dirty regions
        // and made region-aware ClearViewport impossible.
        var expected = new Rectangle (5, 5, 20, 20);
        Assert.Equal (expected, view.NeedsDrawRect);
    }

    [Fact]
    public void SetNeedsDraw_NotVisible_DoesNotSet ()
    {
        var view = new View
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            Visible = false
        };
        view.BeginInit ();
        view.EndInit ();

        view.SetNeedsDraw ();

        Assert.False (view.NeedsDraw);
    }

    [Fact]
    public void SetNeedsDraw_PropagatesToSuperView ()
    {
        var parent = new View { X = 0, Y = 0, Width = 50, Height = 50 };
        var child = new View { X = 10, Y = 10, Width = 20, Height = 20 };
        parent.Add (child);
        parent.BeginInit ();
        parent.EndInit ();
        parent.LayoutSubViews ();

        child.SetNeedsDraw ();

        Assert.True (child.NeedsDraw);
        Assert.True (parent.SubViewNeedsDraw);
    }

    [Fact]
    public void SetNeedsDraw_SetsAdornmentsNeedsDraw ()
    {
        var view = new View { X = 0, Y = 0, Width = 20, Height = 20 };
        view.Border.Thickness = new Thickness (1);
        view.Border.GetOrCreateView ();
        view.Padding.Thickness = new Thickness (1);
        view.Padding.GetOrCreateView ();
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetNeedsDraw ();

        Assert.True (view.Border.View?.NeedsDraw == true);
        Assert.True (view.Padding.View?.NeedsDraw == true);
    }

    [Fact]
    public void IndividualViewDraw_DoesNotClearSuperViewSubViewNeedsDraw ()
    {
        // This test validates that individual view Draw() calls should NOT clear the superview's
        // SubViewNeedsDraw flag when sibling subviews still need drawing.
        //
        // This is the core behavior that enables the fix in the static Draw method.
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        View superview = new ()
        {
            X = 0,
            Y = 0,
            Width = 50,
            Height = 50,
            Driver = driver,
            Id = "SuperView"
        };

        View subview1 = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            Id = "SubView1"
        };

        View subview2 = new ()
        {
            X = 0,
            Y = 10,
            Width = 10,
            Height = 10,
            Id = "SubView2"
        };

        superview.Add (subview1, subview2);
        superview.BeginInit ();
        superview.EndInit ();
        superview.LayoutSubViews ();

        Assert.True (superview.SubViewNeedsDraw);
        Assert.True (subview1.NeedsDraw);
        Assert.True (subview2.NeedsDraw);

        // Draw only subview1 (NOT using the static Draw method)
        subview1.Draw ();

        // SubView1 should be cleared
        Assert.False (subview1.NeedsDraw);

        // SubView2 still needs drawing
        Assert.True (subview2.NeedsDraw);

        // THE KEY ASSERTION: SuperView's SubViewNeedsDraw should STILL be true
        // because subview2 still needs drawing
        //
        // This behavior is REQUIRED for the static Draw fix to work properly.
        // ClearNeedsDraw() does NOT clear SuperView.SubViewNeedsDraw anymore.
        Assert.True (superview.SubViewNeedsDraw, "SuperView's SubViewNeedsDraw must remain true when subview2 still needs drawing");

        // Now draw subview2
        subview2.Draw ();
        Assert.False (subview2.NeedsDraw);

        // SuperView's SubViewNeedsDraw should STILL be true because only the superview
        // itself (or the static Draw method on all subviews) should clear it
        Assert.True (superview.SubViewNeedsDraw, "SuperView's SubViewNeedsDraw should only be cleared by superview.Draw() or static Draw() on all subviews");
    }

    /// <summary>
    ///     Tests that SetNeedsDraw on TopRunnableView propagates to other runnables in the session stack.
    /// </summary>
    [Fact]
    public void NeedsDraw_OnTopRunnable_PropagatesOtherRunnables ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (10, 5);

        using Runnable bottomRunnable = new ();
        bottomRunnable.Width = Dim.Fill ();
        bottomRunnable.Height = Dim.Fill ();
        bottomRunnable.Id = "bottom";
        app.Begin (bottomRunnable);

        using Runnable topRunnable = new ();
        topRunnable.X = 1;
        topRunnable.Y = 1;
        topRunnable.Width = 5;
        topRunnable.Height = 3;
        topRunnable.Id = "top";
        app.Begin (topRunnable);

        // Clear NeedsDraw state
        app.LayoutAndDraw ();
        bottomRunnable.ClearNeedsDraw ();
        topRunnable.ClearNeedsDraw ();

        Assert.False (bottomRunnable.NeedsDraw);
        Assert.False (topRunnable.NeedsDraw);

        // When TopRunnableView calls SetNeedsDraw, other runnables should also be marked
        Assert.Equal (topRunnable, app.TopRunnableView);
        topRunnable.SetNeedsDraw ();

        // Both should now need redraw
        Assert.True (topRunnable.NeedsDraw);
        Assert.True (bottomRunnable.NeedsDraw); // This is the key assertion for the fix
    }

    #endregion

    #region SubViewNeedsDraw Tests

    [Fact]
    public void SubViewNeedsDraw_InitiallyFalse ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        var view = new View { Width = 10, Height = 10, Driver = driver };
        view.BeginInit ();
        view.EndInit ();
        view.Draw (); // Draw once to clear initial NeedsDraw

        Assert.False (view.SubViewNeedsDraw);
    }

    [Fact]
    public void SetSubViewNeedsDraw_PropagatesUp ()
    {
        var grandparent = new View { X = 0, Y = 0, Width = 100, Height = 100 };
        var parent = new View { X = 10, Y = 10, Width = 50, Height = 50 };
        var child = new View { X = 5, Y = 5, Width = 20, Height = 20 };

        grandparent.Add (parent);
        parent.Add (child);
        grandparent.BeginInit ();
        grandparent.EndInit ();
        grandparent.LayoutSubViews ();

        child.SetSubViewNeedsDrawDownHierarchy ();

        Assert.True (child.SubViewNeedsDraw);
        Assert.True (parent.SubViewNeedsDraw);
        Assert.True (grandparent.SubViewNeedsDraw);
    }

    [Fact]
    public void SubViewNeedsDraw_ClearedAfterDraw ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        var parent = new View
        {
            X = 0,
            Y = 0,
            Width = 50,
            Height = 50,
            Driver = driver
        };
        var child = new View { X = 10, Y = 10, Width = 20, Height = 20 };
        parent.Add (child);
        parent.BeginInit ();
        parent.EndInit ();
        parent.LayoutSubViews ();

        child.SetNeedsDraw ();
        Assert.True (parent.SubViewNeedsDraw);

        parent.Draw ();

        Assert.False (parent.SubViewNeedsDraw);
        Assert.False (child.SubViewNeedsDraw);
    }

    #endregion
}

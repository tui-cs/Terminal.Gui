#nullable enable
using UnitTests;

namespace ViewBaseTests.Drawing;

/// <summary>
/// Tests for the static View.Draw(IEnumerable&lt;View&gt;, bool) method
/// </summary>
[Trait ("Category", "Output")]
public class StaticDrawTests : TestDriverBase
{
    [Fact]
    public void StaticDraw_ClearsSubViewNeedsDraw_AfterMarginDrawMargins ()
    {
        // This test validates the fix where the static Draw method calls ClearNeedsDraw()
        // on all peer views after drawing them AND after calling Margin.DrawMargins().
        //
        // THE BUG (before the fix):
        // Margin.DrawMargins() can cause SubViewNeedsDraw to be set on views in the hierarchy.
        // This would leave SubViewNeedsDraw = true even after drawing completed.
        //
        // THE FIX (current code):
        // The static Draw() method explicitly calls ClearNeedsDraw() on all peer views
        // at the very end, AFTER Margin.DrawMargins(), clearing any SubViewNeedsDraw flags
        // that were set during margin drawing.

        IDriver driver = CreateTestDriver ();
        driver.Clip = new (driver.Screen);

        // Create a view hierarchy where a subview's subview has a margin
        // This reproduces the scenario where Margin.DrawMargins sets SubViewNeedsDraw
        View superview = new ()
        {
            X = 0,
            Y = 0,
            Width = 60,
            Height = 60,
            Driver = driver,
            Id = "SuperView"
        };

        View subview1 = new () { X = 0, Y = 0, Width = 40, Height = 40, Id = "SubView1" };
        View subview2 = new () { X = 0, Y = 20, Width = 20, Height = 20, Id = "SubView2" };

        // Add a subview to subview1 that has a margin with shadow
        // This is key to reproducing the bug
        View subSubView = new ()
        {
            X = 5,
            Y = 5,
            Width = 20,
            Height = 20,
            Id = "SubSubView"
        };
        subSubView.Margin.Thickness = new (1);
        subSubView.Margin.ShadowStyle = ShadowStyles.Transparent;

        subview1.Add (subSubView);
        superview.Add (subview1, subview2);

        superview.BeginInit ();
        superview.EndInit ();
        superview.LayoutSubViews ();

        // All views initially need drawing
        Assert.True (superview.NeedsDraw);
        Assert.True (superview.SubViewNeedsDraw);
        Assert.True (subview1.NeedsDraw);
        Assert.True (subview1.SubViewNeedsDraw);
        Assert.True (subview2.NeedsDraw);
        Assert.True (subSubView.NeedsDraw);
        Assert.True (subSubView.Margin.View?.NeedsDraw == true);

        // Call the static Draw method on the subviews
        // This will:
        // 1. Call view.Draw() on each subview
        // 2. Call Margin.DrawMargins() which may set SubViewNeedsDraw in the hierarchy
        // 3. Call ClearNeedsDraw() on each subview to clean up
        View.Draw (superview.InternalSubViews, force: false);

        // After the static Draw completes:
        // All subviews should have NeedsDraw = false
        Assert.False (subview1.NeedsDraw, "SubView1 should not need drawing after Draw()");
        Assert.False (subview2.NeedsDraw, "SubView2 should not need drawing after Draw()");
        Assert.False (subSubView.NeedsDraw, "SubSubView should not need drawing after Draw()");
        Assert.False (subSubView.Margin.View?.NeedsDraw == true, "SubSubView's Margin should not need drawing after Draw()");

        // SuperView's SubViewNeedsDraw should be false because the static Draw() method
        // calls ClearNeedsDraw() on all the subviews at the end, AFTER Margin.DrawMargins()
        // 
        // BEFORE THE FIX: This would be TRUE because Margin.DrawMargins() would
        //                 set SubViewNeedsDraw somewhere in the hierarchy and it
        //                 wouldn't be cleared
        // AFTER THE FIX: This is FALSE because the static Draw() calls ClearNeedsDraw()
        //                at the very end, cleaning up any SubViewNeedsDraw flags set
        //                by Margin.DrawMargins()
        Assert.False (superview.SubViewNeedsDraw,
                      "superview's SubViewNeedsDraw should be false after static Draw(). All subviews were drawn in the call to View.Draw");
        Assert.False (subview1.SubViewNeedsDraw,
                      "SubView1's SubViewNeedsDraw should be false after its subviews are drawn and cleared");
    }

    [Fact]
    public void StaticDraw_WithForceTrue_SetsNeedsDrawOnAllViews ()
    {
        // Verify that when force=true, all views get SetNeedsDraw() called before drawing
        IDriver driver = CreateTestDriver ();
        driver.Clip = new (driver.Screen);

        View view1 = new () { X = 0, Y = 0, Width = 10, Height = 10, Driver = driver, Id = "View1" };
        View view2 = new () { X = 10, Y = 0, Width = 10, Height = 10, Driver = driver, Id = "View2" };

        view1.BeginInit ();
        view1.EndInit ();
        view2.BeginInit ();
        view2.EndInit ();

        // Manually clear their NeedsDraw flags
        view1.Draw ();
        view2.Draw ();
        Assert.False (view1.NeedsDraw);
        Assert.False (view2.NeedsDraw);

        // Now call static Draw with force=true
        View.Draw ([view1, view2], force: true);

        // After drawing with force=true, they should be cleared again
        Assert.False (view1.NeedsDraw);
        Assert.False (view2.NeedsDraw);
    }

    [Fact]
    public void StaticDraw_HandlesEmptyCollection ()
    {
        // Verify that calling Draw with an empty collection doesn't crash
        View.Draw ([], force: false);
        View.Draw ([], force: true);
    }


    [Fact]
    public void StaticDraw_ClearsNestedSubViewNeedsDraw ()
    {
        // This test verifies that the static Draw method properly clears SubViewNeedsDraw
        // flags throughout a nested view hierarchy after Margin.DrawMargins
        IDriver driver = CreateTestDriver ();
        driver.Clip = new (driver.Screen);

        View topView = new ()
        {
            X = 0,
            Y = 0,
            Width = 60,
            Height = 60,
            Driver = driver,
            Id = "TopView"
        };

        View middleView1 = new () { X = 0, Y = 0, Width = 30, Height = 30, Id = "MiddleView1" };
        View middleView2 = new () { X = 30, Y = 0, Width = 30, Height = 30, Id = "MiddleView2" };

        View bottomView = new ()
        {
            X = 5,
            Y = 5,
            Width = 15,
            Height = 15,
            Id = "BottomView"
        };

        // Give the bottom view a margin to trigger the Margin.DrawMargins behavior
        bottomView.Margin.Thickness = new (1);
        bottomView.Margin.ShadowStyle = ShadowStyles.Transparent;

        middleView1.Add (bottomView);
        topView.Add (middleView1, middleView2);

        topView.BeginInit ();
        topView.EndInit ();
        topView.LayoutSubViews ();

        Assert.True (topView.SubViewNeedsDraw);
        Assert.True (middleView1.SubViewNeedsDraw);
        Assert.True (bottomView.NeedsDraw);

        // Draw the middle views using static Draw
        View.Draw (topView.InternalSubViews, force: false);

        // All SubViewNeedsDraw flags should be cleared after the static Draw
        Assert.False (topView.SubViewNeedsDraw,
            "TopView's SubViewNeedsDraw should be false after static Draw(). All subviews were drawn in the call to View.Draw");
        Assert.False (middleView1.SubViewNeedsDraw,
            "MiddleView1's SubViewNeedsDraw should be false after its subviews are drawn");
        Assert.False (middleView2.SubViewNeedsDraw,
            "MiddleView2's SubViewNeedsDraw should be false");
        Assert.False (bottomView.NeedsDraw,
            "BottomView should not need drawing after Draw()");
        Assert.False (bottomView.Margin.View?.NeedsDraw == true,
            "BottomView's Margin should not need drawing after Draw()");
    }
}



// GitHub Copilot - Tests for Pos.GetReferencedViews method

namespace ViewBaseTests.Layout;

public class PosGetReferencedViewsTests
{
    [Fact]
    public void GetReferencedViews_PosAbsolute_ReturnsEmpty ()
    {
        Pos pos = Pos.Absolute (10);
        Assert.Empty (pos.GetReferencedViews ());
    }

    [Fact]
    public void GetReferencedViews_PosPercent_ReturnsEmpty ()
    {
        Pos pos = Pos.Percent (50);
        Assert.Empty (pos.GetReferencedViews ());
    }

    [Fact]
    public void GetReferencedViews_PosCenter_ReturnsEmpty ()
    {
        Pos pos = Pos.Center ();
        Assert.Empty (pos.GetReferencedViews ());
    }

    [Fact]
    public void GetReferencedViews_PosAnchorEnd_ReturnsEmpty ()
    {
        Pos pos = Pos.AnchorEnd ();
        Assert.Empty (pos.GetReferencedViews ());
    }

    [Fact]
    public void GetReferencedViews_PosView_ReturnsTarget ()
    {
        View targetView = new () { Width = 10, Height = 5 };
        Pos pos = Pos.Left (targetView);

        View [] refs = pos.GetReferencedViews ().ToArray ();

        Assert.Single (refs);
        Assert.Same (targetView, refs [0]);

        targetView.Dispose ();
    }

    [Fact]
    public void GetReferencedViews_PosView_AllSides_ReturnsTarget ()
    {
        View targetView = new () { Width = 10, Height = 5 };

        Pos [] allSides = [Pos.Left (targetView), Pos.Right (targetView), Pos.Top (targetView), Pos.Bottom (targetView)];

        foreach (Pos pos in allSides)
        {
            View [] refs = pos.GetReferencedViews ().ToArray ();
            Assert.Single (refs);
            Assert.Same (targetView, refs [0]);
        }

        targetView.Dispose ();
    }

    [Fact]
    public void GetReferencedViews_PosCombine_ReturnsAllReferencedViews ()
    {
        View view1 = new () { Width = 10, Height = 5 };
        View view2 = new () { Width = 20, Height = 10 };

        Pos pos = Pos.Right (view1) + Pos.Bottom (view2);

        View [] refs = pos.GetReferencedViews ().ToArray ();

        Assert.Equal (2, refs.Length);
        Assert.Contains (view1, refs);
        Assert.Contains (view2, refs);

        view1.Dispose ();
        view2.Dispose ();
    }

    [Fact]
    public void GetReferencedViews_NestedPosCombine_ReturnsAllReferencedViews ()
    {
        View view1 = new () { Width = 10, Height = 5 };
        View view2 = new () { Width = 20, Height = 10 };
        View view3 = new () { Width = 30, Height = 15 };

        Pos pos = Pos.Right (view1) + Pos.Bottom (view2) - Pos.Left (view3);

        View [] refs = pos.GetReferencedViews ().ToArray ();

        Assert.Equal (3, refs.Length);
        Assert.Contains (view1, refs);
        Assert.Contains (view2, refs);
        Assert.Contains (view3, refs);

        view1.Dispose ();
        view2.Dispose ();
        view3.Dispose ();
    }

    [Fact]
    public void GetReferencedViews_PosCombine_WithAbsolute_ReturnsOnlyViewRefs ()
    {
        View view1 = new () { Width = 10, Height = 5 };

        Pos pos = Pos.Right (view1) + 5;

        View [] refs = pos.GetReferencedViews ().ToArray ();

        Assert.Single (refs);
        Assert.Same (view1, refs [0]);

        view1.Dispose ();
    }

    [Fact]
    public void GetReferencedViews_PosFunc_ReturnsEmpty ()
    {
        Pos pos = Pos.Func (_ => 42);
        Assert.Empty (pos.GetReferencedViews ());
    }

    [Fact]
    public void GetReferencedViews_PosAlign_ReturnsEmpty ()
    {
        Pos pos = Pos.Align (Alignment.Center);
        Assert.Empty (pos.GetReferencedViews ());
    }

    [Fact]
    public void GetReferencedViews_MatchesReferencesOtherViews ()
    {
        View view1 = new () { Width = 10, Height = 5 };

        // Types that don't reference views
        Pos [] nonRefTypes = [Pos.Absolute (10), Pos.Percent (50), Pos.Center (), Pos.AnchorEnd (), Pos.Func (_ => 42)];

        foreach (Pos pos in nonRefTypes)
        {
            Assert.False (pos.ReferencesOtherViews ());
            Assert.Empty (pos.GetReferencedViews ());
        }

        // Types that do reference views
        Pos [] refTypes = [Pos.Left (view1), Pos.Right (view1), Pos.Top (view1), Pos.Bottom (view1), Pos.Left (view1) + 5, Pos.Func (_ => 42, view1)];

        foreach (Pos pos in refTypes)
        {
            Assert.True (pos.ReferencesOtherViews ());
            Assert.NotEmpty (pos.GetReferencedViews ());
        }

        view1.Dispose ();
    }
}

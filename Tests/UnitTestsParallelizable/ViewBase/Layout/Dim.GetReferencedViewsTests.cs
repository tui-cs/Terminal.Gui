

// GitHub Copilot - Tests for Dim.GetReferencedViews method

namespace ViewBaseTests.Layout;

public class DimGetReferencedViewsTests
{
    [Fact]
    public void GetReferencedViews_DimAbsolute_ReturnsEmpty ()
    {
        Dim dim = Dim.Absolute (10);
        Assert.Empty (dim.GetReferencedViews ());
    }

    [Fact]
    public void GetReferencedViews_DimPercent_ReturnsEmpty ()
    {
        Dim dim = Dim.Percent (50);
        Assert.Empty (dim.GetReferencedViews ());
    }

    [Fact]
    public void GetReferencedViews_DimFill_WithoutTo_ReturnsEmpty ()
    {
        Dim dim = Dim.Fill ();
        Assert.Empty (dim.GetReferencedViews ());
    }

    [Fact]
    public void GetReferencedViews_DimFill_WithTo_ReturnsToView ()
    {
        View targetView = new () { Width = 10, Height = 5 };
        Dim dim = Dim.Fill (targetView);

        View [] refs = dim.GetReferencedViews ().ToArray ();

        Assert.Single (refs);
        Assert.Same (targetView, refs [0]);

        targetView.Dispose ();
    }

    [Fact]
    public void GetReferencedViews_DimView_ReturnsTarget ()
    {
        View targetView = new () { Width = 10, Height = 5 };
        Dim dim = Dim.Width (targetView);

        View [] refs = dim.GetReferencedViews ().ToArray ();

        Assert.Single (refs);
        Assert.Same (targetView, refs [0]);

        targetView.Dispose ();
    }

    [Fact]
    public void GetReferencedViews_DimCombine_ReturnsAllReferencedViews ()
    {
        View view1 = new () { Width = 10, Height = 5 };
        View view2 = new () { Width = 20, Height = 10 };

        Dim dim = Dim.Width (view1) + Dim.Height (view2);

        View [] refs = dim.GetReferencedViews ().ToArray ();

        Assert.Equal (2, refs.Length);
        Assert.Contains (view1, refs);
        Assert.Contains (view2, refs);

        view1.Dispose ();
        view2.Dispose ();
    }

    [Fact]
    public void GetReferencedViews_NestedDimCombine_ReturnsAllReferencedViews ()
    {
        View view1 = new () { Width = 10, Height = 5 };
        View view2 = new () { Width = 20, Height = 10 };
        View view3 = new () { Width = 30, Height = 15 };

        Dim dim = Dim.Width (view1) + Dim.Height (view2) - Dim.Width (view3);

        View [] refs = dim.GetReferencedViews ().ToArray ();

        Assert.Equal (3, refs.Length);
        Assert.Contains (view1, refs);
        Assert.Contains (view2, refs);
        Assert.Contains (view3, refs);

        view1.Dispose ();
        view2.Dispose ();
        view3.Dispose ();
    }

    [Fact]
    public void GetReferencedViews_DimCombine_WithAbsolute_ReturnsOnlyViewRefs ()
    {
        View view1 = new () { Width = 10, Height = 5 };

        Dim dim = Dim.Width (view1) + 5;

        View [] refs = dim.GetReferencedViews ().ToArray ();

        Assert.Single (refs);
        Assert.Same (view1, refs [0]);

        view1.Dispose ();
    }

    [Fact]
    public void GetReferencedViews_DimAuto_ReturnsEmpty ()
    {
        Dim dim = Dim.Auto ();
        Assert.Empty (dim.GetReferencedViews ());
    }

    [Fact]
    public void GetReferencedViews_DimFunc_ReturnsEmpty ()
    {
        Dim dim = Dim.Func (_ => 42);
        Assert.Empty (dim.GetReferencedViews ());
    }

    [Fact]
    public void GetReferencedViews_MatchesReferencesOtherViews ()
    {
        View view1 = new () { Width = 10, Height = 5 };

        // Types that don't reference views
        Dim [] nonRefTypes = [Dim.Absolute (10), Dim.Percent (50), Dim.Fill (), Dim.Auto (), Dim.Func (_ => 42)];

        foreach (Dim dim in nonRefTypes)
        {
            Assert.False (dim.ReferencesOtherViews ());
            Assert.Empty (dim.GetReferencedViews ());
        }

        // Types that do reference views
        Dim [] refTypes = [Dim.Width (view1), Dim.Fill (view1), Dim.Width (view1) + 5];

        foreach (Dim dim in refTypes)
        {
            Assert.True (dim.ReferencesOtherViews ());
            Assert.NotEmpty (dim.GetReferencedViews ());
        }

        view1.Dispose ();
    }
}

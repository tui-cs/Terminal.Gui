

// GitHub Copilot - Generated tests for Pos.Has<T> method

namespace ViewBaseTests.Layout;

public class PosHasTests
{
    [Fact]
    public void Has_DirectMatch_ReturnsTrue ()
    {
        Pos pos = Pos.Center ();
        Assert.True (pos.Has (out PosCenter result));
        Assert.Same (pos, result);
    }

    [Fact]
    public void Has_PosAbsolute_DirectMatch_ReturnsTrue ()
    {
        Pos pos = Pos.Absolute (10);
        Assert.True (pos.Has (out PosAbsolute result));
        Assert.Same (pos, result);
    }

    [Fact]
    public void Has_PosPercent_DirectMatch_ReturnsTrue ()
    {
        Pos pos = Pos.Percent (50);
        Assert.True (pos.Has (out PosPercent result));
        Assert.Same (pos, result);
    }

    [Fact]
    public void Has_PosAnchorEnd_DirectMatch_ReturnsTrue ()
    {
        Pos pos = Pos.AnchorEnd ();
        Assert.True (pos.Has (out PosAnchorEnd result));
        Assert.Same (pos, result);
    }

    [Fact]
    public void Has_NoMatch_ReturnsFalse ()
    {
        Pos pos = Pos.Absolute (10);
        Assert.False (pos.Has<PosCenter> (out _));
    }

    [Fact]
    public void Has_PosAbsolute_DoesNotMatchPosPercent_ReturnsFalse ()
    {
        Pos pos = Pos.Absolute (10);
        Assert.False (pos.Has<PosPercent> (out _));
    }

    [Fact]
    public void Has_InCombine_Left_ReturnsTrue ()
    {
        Pos combined = Pos.Center () + 5;
        Assert.True (combined.Has (out PosCenter result));
        Assert.IsType<PosCenter> (result);
    }

    [Fact]
    public void Has_InCombine_Right_ReturnsTrue ()
    {
        Pos combined = Pos.Absolute (5) + Pos.Percent (50);
        Assert.True (combined.Has (out PosPercent result));
        Assert.IsType<PosPercent> (result);
    }

    [Fact]
    public void Has_InCombine_Subtraction_ReturnsTrue ()
    {
        Pos combined = Pos.Center () - 5;
        Assert.True (combined.Has (out PosCenter result));
        Assert.IsType<PosCenter> (result);
    }

    [Fact]
    public void Has_NestedCombine_FindsBothTypes ()
    {
        Pos nested = Pos.Center () + 5 - Pos.Percent (10);
        Assert.True (nested.Has<PosCenter> (out _));
        Assert.True (nested.Has<PosPercent> (out _));
    }

    [Fact]
    public void Has_NestedCombine_FindsPosAbsolute ()
    {
        Pos nested = Pos.Center () + Pos.Absolute (5);
        Assert.True (nested.Has<PosAbsolute> (out _));
    }

    [Fact]
    public void Has_PosCombine_DirectMatch_ReturnsTrue ()
    {
        Pos combined = Pos.Center () + 5;
        Assert.True (combined.Has (out PosCombine result));
        Assert.Same (combined, result);
    }

    [Fact]
    public void Has_NestedPosCombine_FindsOuterCombine ()
    {
        // When searching for PosCombine, the outer one is found first (direct match)
        Pos inner = Pos.Center () + 5;
        Pos outer = inner + Pos.Absolute (10);
        Assert.True (outer.Has (out PosCombine result));
        Assert.Same (outer, result);
    }

    [Fact]
    public void Has_PosView_DirectMatch_ReturnsTrue ()
    {
        View view = new () { Width = 10, Height = 5 };
        Pos pos = Pos.Left (view);
        Assert.True (pos.Has (out PosView result));
        Assert.Same (pos, result);
        view.Dispose ();
    }

    [Fact]
    public void Has_PosFunc_DirectMatch_ReturnsTrue ()
    {
        Pos pos = Pos.Func (_ => 42);
        Assert.True (pos.Has (out PosFunc result));
        Assert.Same (pos, result);
    }

    [Fact]
    public void Has_OutParameter_ReturnsCorrectInstance ()
    {
        PosCenter originalCenter = new ();
        Pos combined = originalCenter + Pos.Absolute (10);

        Assert.True (combined.Has (out PosCenter result));
        Assert.Same (originalCenter, result);
    }

    [Fact]
    public void Has_OutParameter_IsNullWhenNotFound ()
    {
        Pos pos = Pos.Absolute (10);

        Assert.False (pos.Has (out PosCenter result));
        Assert.Null (result);
    }

    [Fact]
    public void Has_PosAlign_DirectMatch_ReturnsTrue ()
    {
        Pos pos = Pos.Align (Alignment.Center);
        Assert.True (pos.Has (out PosAlign result));
        Assert.Same (pos, result);
    }

    [Fact]
    public void Has_PosView_InCombine_ReturnsTrue ()
    {
        View view = new () { Width = 10, Height = 5 };
        Pos combined = Pos.Left (view) + 5;

        Assert.True (combined.Has (out PosView result));
        Assert.IsType<PosView> (result);

        view.Dispose ();
    }

    [Fact]
    public void Has_ComplexCombine_FindsAllTypes ()
    {
        View view = new () { Width = 10, Height = 5 };
        Pos complex = Pos.Left (view) + Pos.Percent (10) - Pos.Center ();

        Assert.True (complex.Has<PosView> (out _));
        Assert.True (complex.Has<PosPercent> (out _));
        Assert.True (complex.Has<PosCenter> (out _));

        view.Dispose ();
    }
}

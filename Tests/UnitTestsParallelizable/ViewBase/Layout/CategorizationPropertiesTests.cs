#nullable disable

// Claude - Opus 4.5

namespace ViewBaseTests.Layout;

/// <summary>
/// Tests for Phase 5 categorization properties: IsFixed and RequiresTargetLayout.
/// These properties help DimAuto categorize Pos/Dim types without type checking.
/// </summary>
public class CategorizationPropertiesTests
{
    #region IsFixed Tests - Dim

    [Fact]
    public void DimAbsolute_IsFixed ()
    {
        Dim dim = Dim.Absolute (42);
        Assert.True (dim.IsFixed);
    }

    [Fact]
    public void DimFunc_IsFixed ()
    {
        Dim dim = Dim.Func (_ => 25);
        Assert.True (dim.IsFixed);
    }

    [Fact]
    public void DimAuto_IsFixed ()
    {
        Dim dim = Dim.Auto ();
        Assert.True (dim.IsFixed);
    }

    [Fact]
    public void DimPercent_IsNotFixed ()
    {
        Dim dim = Dim.Percent (50);
        Assert.False (dim.IsFixed);
    }

    [Fact]
    public void DimFill_IsNotFixed ()
    {
        Dim dim = Dim.Fill ();
        Assert.False (dim.IsFixed);
    }

    [Fact]
    public void DimView_IsNotFixed ()
    {
        View view = new ();
        Dim dim = Dim.Width (view);
        Assert.False (dim.IsFixed);
    }

    #endregion

    #region IsFixed Tests - Pos

    [Fact]
    public void PosAbsolute_IsFixed ()
    {
        Pos pos = Pos.Absolute (10);
        Assert.True (pos.IsFixed);
    }

    [Fact]
    public void PosFunc_IsFixed ()
    {
        Pos pos = Pos.Func (_ => 15);
        Assert.True (pos.IsFixed);
    }

    [Fact]
    public void PosCenter_IsNotFixed ()
    {
        Pos pos = Pos.Center ();
        Assert.False (pos.IsFixed);
    }

    [Fact]
    public void PosPercent_IsNotFixed ()
    {
        Pos pos = Pos.Percent (50);
        Assert.False (pos.IsFixed);
    }

    [Fact]
    public void PosAnchorEnd_IsNotFixed ()
    {
        Pos pos = Pos.AnchorEnd ();
        Assert.False (pos.IsFixed);
    }

    [Fact]
    public void PosView_IsNotFixed ()
    {
        View view = new ();
        Pos pos = Pos.Left (view);
        Assert.False (pos.IsFixed);
    }

    #endregion

    #region RequiresTargetLayout Tests - Dim

    [Fact]
    public void DimView_RequiresTargetLayout ()
    {
        View view = new ();
        Dim dim = Dim.Width (view);
        Assert.True (dim.RequiresTargetLayout);
    }

    [Fact]
    public void DimAbsolute_DoesNotRequireTargetLayout ()
    {
        Dim dim = Dim.Absolute (42);
        Assert.False (dim.RequiresTargetLayout);
    }

    [Fact]
    public void DimFunc_DoesNotRequireTargetLayout ()
    {
        Dim dim = Dim.Func (_ => 25);
        Assert.False (dim.RequiresTargetLayout);
    }

    [Fact]
    public void DimPercent_DoesNotRequireTargetLayout ()
    {
        Dim dim = Dim.Percent (50);
        Assert.False (dim.RequiresTargetLayout);
    }

    [Fact]
    public void DimFill_DoesNotRequireTargetLayout ()
    {
        Dim dim = Dim.Fill ();
        Assert.False (dim.RequiresTargetLayout);
    }

    #endregion

    #region RequiresTargetLayout Tests - Pos

    [Fact]
    public void PosView_RequiresTargetLayout ()
    {
        View view = new ();
        Pos pos = Pos.Left (view);
        Assert.True (pos.RequiresTargetLayout);
    }

    [Fact]
    public void PosAbsolute_DoesNotRequireTargetLayout ()
    {
        Pos pos = Pos.Absolute (10);
        Assert.False (pos.RequiresTargetLayout);
    }

    [Fact]
    public void PosFunc_DoesNotRequireTargetLayout ()
    {
        Pos pos = Pos.Func (_ => 15);
        Assert.False (pos.RequiresTargetLayout);
    }

    [Fact]
    public void PosCenter_DoesNotRequireTargetLayout ()
    {
        Pos pos = Pos.Center ();
        Assert.False (pos.RequiresTargetLayout);
    }

    [Fact]
    public void PosPercent_DoesNotRequireTargetLayout ()
    {
        Pos pos = Pos.Percent (50);
        Assert.False (pos.RequiresTargetLayout);
    }

    #endregion
}

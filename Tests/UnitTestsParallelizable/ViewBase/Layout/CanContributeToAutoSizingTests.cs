#nullable disable

// Claude - Opus 4.5

namespace ViewBaseTests.Layout;

/// <summary>
///     Tests for the CanContributeToAutoSizing property on Dim types.
///     This property is used by DimAuto to determine if a Dim should contribute to auto-sizing calculations.
/// </summary>
public class CanContributeToAutoSizingTests
{
    [Fact]
    public void DimAbsolute_CanContributeToAutoSizing ()
    {
        Dim dim = Dim.Absolute (10);
        Assert.True (dim.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimAuto_CanContributeToAutoSizing ()
    {
        Dim dim = Dim.Auto ();
        Assert.True (dim.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimFunc_CanContributeToAutoSizing ()
    {
        Dim dim = Dim.Func (_ => 10);
        Assert.True (dim.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimView_CanContributeToAutoSizing ()
    {
        View view = new ();
        Dim dim = Dim.Width (view);
        Assert.True (dim.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimPercent_CannotContributeToAutoSizing ()
    {
        Dim dim = Dim.Percent (50);
        Assert.False (dim.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimPercent_AllModes_CannotContributeToAutoSizing ()
    {
        Dim dimContentSize = Dim.Percent (50);
        Assert.False (dimContentSize.CanContributeToAutoSizing);

        Dim dimPosition = Dim.Percent (50, DimPercentMode.Position);
        Assert.False (dimPosition.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimFill_WithoutMinimumOrTo_CannotContributeToAutoSizing ()
    {
        Dim dim = Dim.Fill ();
        Assert.False (dim.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimFill_WithMargin_WithoutMinimumOrTo_CannotContributeToAutoSizing ()
    {
        Dim dim = Dim.Fill (2);
        Assert.False (dim.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimFill_WithMinimumContentDim_CanContributeToAutoSizing ()
    {
        Dim dim = Dim.Fill (0, 10);
        Assert.True (dim.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimFill_WithMarginAndMinimumContentDim_CanContributeToAutoSizing ()
    {
        Dim dim = Dim.Fill (2, 10);
        Assert.True (dim.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimFill_WithTo_CanContributeToAutoSizing ()
    {
        View view = new ();
        Dim dim = Dim.Fill (view);
        Assert.True (dim.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimFill_WithMarginAndTo_CanContributeToAutoSizing ()
    {
        View view = new ();
        Dim dim = Dim.Fill (2, view);
        Assert.True (dim.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimFill_WithAllParameters_CanContributeToAutoSizing ()
    {
        View view = new ();
        Dim dim = Dim.Fill (2, 10, view);
        Assert.True (dim.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimCombine_CanContributeIfEitherChildCan ()
    {
        // Both can contribute
        Dim dim1 = Dim.Absolute (10) + Dim.Absolute (5);
        Assert.True (dim1.CanContributeToAutoSizing);

        // Left can contribute, right cannot
        Dim dim2 = Dim.Absolute (10) + Dim.Percent (50);
        Assert.True (dim2.CanContributeToAutoSizing);

        // Left cannot contribute, right can
        Dim dim3 = Dim.Percent (50) + Dim.Absolute (10);
        Assert.True (dim3.CanContributeToAutoSizing);

        // Neither can contribute
        Dim dim4 = Dim.Percent (50) + Dim.Fill ();
        Assert.False (dim4.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimCombine_Subtraction_CanContributeIfEitherChildCan ()
    {
        // Both can contribute
        Dim dim1 = Dim.Absolute (100) - Dim.Absolute (10);
        Assert.True (dim1.CanContributeToAutoSizing);

        // Left can contribute, right cannot
        Dim dim2 = Dim.Absolute (100) - Dim.Percent (10);
        Assert.True (dim2.CanContributeToAutoSizing);

        // Left cannot contribute, right can
        Dim dim3 = Dim.Percent (100) - Dim.Absolute (10);
        Assert.True (dim3.CanContributeToAutoSizing);

        // Neither can contribute
        Dim dim4 = Dim.Fill () - Dim.Percent (10);
        Assert.False (dim4.CanContributeToAutoSizing);
    }

    [Fact]
    public void DimCombine_Complex_CanContributeIfAnyChildCan ()
    {
        // Complex case: DimFill with MinimumContentDim + DimPercent
        Dim dim1 = Dim.Fill (0, 20) + Dim.Percent (10);
        Assert.True (dim1.CanContributeToAutoSizing);

        // Complex case: (DimFill without minimum) + (DimFill without minimum)
        Dim dim2 = Dim.Fill () + Dim.Fill ();
        Assert.False (dim2.CanContributeToAutoSizing);

        // Complex case: (DimFill with To) - DimPercent
        View view = new ();
        Dim dim3 = Dim.Fill (view) - Dim.Percent (10);
        Assert.True (dim3.CanContributeToAutoSizing);
    }
}

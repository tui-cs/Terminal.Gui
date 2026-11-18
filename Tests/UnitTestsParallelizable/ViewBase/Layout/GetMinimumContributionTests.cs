#nullable disable

// Claude - Opus 4.5

namespace ViewBaseTests.Layout;

/// <summary>
///     Tests for the GetMinimumContribution method on Dim types.
///     This method is used by DimAuto to get minimum size contribution during auto-sizing.
/// </summary>
public class GetMinimumContributionTests
{
    [Fact]
    public void DimAbsolute_GetMinimumContribution_ReturnsAbsoluteValue ()
    {
        Dim dim = Dim.Absolute (42);
        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.None);
        Assert.Equal (42, contribution);
    }

    [Fact]
    public void DimAuto_GetMinimumContribution_ReturnsCalculatedValue ()
    {
        var view = new View { Width = Dim.Auto (), Height = Dim.Auto () };
        view.BeginInit ();
        view.EndInit ();

        // DimAuto calculates based on content
        int contribution = view.Width.GetMinimumContribution (0, 100, view, Dimension.Width);
        Assert.True (contribution >= 0);
    }

    [Fact]
    public void DimFunc_GetMinimumContribution_ReturnsCalculatedValue ()
    {
        Dim dim = Dim.Func (_ => 25);
        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.None);
        Assert.Equal (25, contribution);
    }

    [Fact]
    public void DimView_GetMinimumContribution_ReturnsTargetViewSize ()
    {
        var targetView = new View { Width = 30, Height = 20 };
        Dim dim = Dim.Width (targetView);

        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.Width);
        Assert.Equal (30, contribution);
    }

    [Fact]
    public void DimPercent_GetMinimumContribution_ReturnsCalculatedPercentage ()
    {
        Dim dim = Dim.Percent (50);
        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.None);
        Assert.Equal (50, contribution);
    }

    [Fact]
    public void DimFill_WithoutMinimumContentDim_ReturnsZero ()
    {
        Dim dim = Dim.Fill ();
        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.None);
        Assert.Equal (0, contribution);
    }

    [Fact]
    public void DimFill_WithMargin_WithoutMinimumContentDim_ReturnsZero ()
    {
        Dim dim = Dim.Fill (5);
        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.None);
        Assert.Equal (0, contribution);
    }

    [Fact]
    public void DimFill_WithMinimumContentDim_ReturnsMinimumSize ()
    {
        Dim dim = Dim.Fill (0, 35);
        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.None);
        Assert.Equal (35, contribution);
    }

    [Fact]
    public void DimFill_WithMarginAndMinimumContentDim_ReturnsMinimumSize ()
    {
        Dim dim = Dim.Fill (5, 40);
        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.None);
        Assert.Equal (40, contribution);
    }

    [Fact]
    public void DimFill_WithMinimumContentDim_UsesCalculatedValue ()
    {
        // MinimumContentDim can also be dynamic (e.g., Percent)
        Dim dim = Dim.Fill (0, Dim.Percent (25));
        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.None);
        Assert.Equal (25, contribution);
    }

    [Fact]
    public void DimFill_WithTo_ReturnsZero ()
    {
        // When only To is set (without MinimumContentDim), GetMinimumContribution returns 0
        // because the actual contribution depends on the To view's position, which DimAuto handles specially
        var toView = new View { X = 50, Y = 30 };
        Dim dim = Dim.Fill (toView);

        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.Width);
        Assert.Equal (0, contribution);
    }

    [Fact]
    public void DimFill_WithMarginAndTo_ReturnsZero ()
    {
        var toView = new View { X = 50, Y = 30 };
        Dim dim = Dim.Fill (5, toView);

        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.Width);
        Assert.Equal (0, contribution);
    }

    [Fact]
    public void DimCombine_GetMinimumContribution_CombinesValues ()
    {
        // Addition
        Dim dim1 = Dim.Absolute (10) + Dim.Absolute (5);
        int contribution1 = dim1.GetMinimumContribution (0, 100, null, Dimension.None);
        Assert.Equal (15, contribution1);

        // Subtraction
        Dim dim2 = Dim.Absolute (20) - Dim.Absolute (8);
        int contribution2 = dim2.GetMinimumContribution (0, 100, null, Dimension.None);
        Assert.Equal (12, contribution2);
    }

    [Fact]
    public void DimCombine_WithDimFill_HandlesMinimumContentDim ()
    {
        // DimFill with MinimumContentDim + Absolute
        Dim dim = Dim.Fill (0, 30) + Dim.Absolute (10);
        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.None);
        Assert.Equal (40, contribution);
    }

    [Fact]
    public void DimCombine_WithDimFill_WithoutMinimum_UsesZero ()
    {
        // DimFill without MinimumContentDim + Absolute = 0 + 10 = 10
        Dim dim = Dim.Fill () + Dim.Absolute (10);
        int contribution = dim.GetMinimumContribution (0, 100, null, Dimension.None);
        Assert.Equal (10, contribution);
    }
}

namespace ViewBaseTests.Layout;

public partial class DimAutoTests
{
    // Tests all the Dim Types in SubView scenarios to ensure DimAutoStyle.Content is calculated correctly

    #region DimAbsolute

    [Theory]
    [InlineData (0, 15, 15)]
    [InlineData (1, 15, 16)]
    [InlineData (-1, 15, 14)]
    public void With_SubView_Using_DimAbsolute (int subViewOffset, int dimAbsoluteSize, int expectedSize)
    {
        View view = new ();
        View subview = new () { X = subViewOffset, Y = subViewOffset, Width = Dim.Absolute (dimAbsoluteSize), Height = Dim.Absolute (dimAbsoluteSize) };
        view.Add (subview);

        Dim dim = Dim.Auto (DimAutoStyle.Content);
        int calculatedWidth = dim.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = dim.Calculate (0, 100, view, Dimension.Height);

        Assert.Equal (expectedSize, calculatedWidth);
        Assert.Equal (expectedSize, calculatedHeight);
    }

    #endregion DimAbsolute

    #region DimPercent

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData (0, 50, 0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 100, 100, 100, 100, 100, 100)]
    [InlineData (0, 50, 100, 100, 100, 100, 100, 100)]
    public void With_SubView_Using_DimPercent (
        int subViewOffset,
        int percent,
        int minWidth,
        int maxWidth,
        int minHeight,
        int maxHeight,
        int expectedWidth,
        int expectedHeight
    )
    {
        View view = new ()
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };
        View subview = new () { X = subViewOffset, Y = subViewOffset, Width = Dim.Percent (percent), Height = Dim.Percent (percent) };
        view.Add (subview);

        // Assuming the calculation is done after layout
        int calculatedX = view.X.Calculate (100, view.Width, view, Dimension.Width);
        int calculatedY = view.Y.Calculate (100, view.Height, view, Dimension.Height);
        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        Assert.Equal (expectedWidth, calculatedWidth); // subview's width
        Assert.Equal (expectedHeight, calculatedHeight); // subview's height
        Assert.Equal (subViewOffset, calculatedX);
        Assert.Equal (subViewOffset, calculatedY);

        view.SetRelativeLayout (new (100, 100));
        view.LayoutSubViews ();

        Assert.Equal (expectedWidth * (percent / 100f), subview.Viewport.Width);
        Assert.Equal (expectedHeight * (percent / 100f), subview.Viewport.Height);
    }

    #endregion DimPercent

    #region DimFill

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 0, 0)]
    [InlineData (0, 20, 0, 10, 0, 0)]
    [InlineData (0, 21, 0, 11, 0, 0)]
    [InlineData (1, 21, 1, 11, 1, 1)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    public void With_SubView_Using_DimFill (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        View view = new ()
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };
        View subview = new () { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
        view.Add (subview);

        // Assuming the calculation is done after layout
        int calculatedX = view.X.Calculate (100, view.Width, view, Dimension.Width);
        int calculatedY = view.Y.Calculate (100, view.Height, view, Dimension.Height);
        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        Assert.Equal (expectedWidth, calculatedWidth);
        Assert.Equal (expectedHeight, calculatedHeight);
        Assert.Equal (0, calculatedX);
        Assert.Equal (0, calculatedY);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 2, 4)]
    [InlineData (0, 20, 0, 10, 2, 4)]
    [InlineData (0, 21, 0, 11, 2, 4)]
    [InlineData (1, 21, 1, 11, 2, 4)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    public void With_SubView_Using_DimFill_And_Another_SubView (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        View view = new ()
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };
        View absView = new () { X = 1, Y = 2, Width = 1, Height = 2 };
        view.Add (absView);

        View subview = new () { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
        view.Add (subview);

        // Assuming the calculation is done after layout
        int calculatedX = view.X.Calculate (100, view.Width, view, Dimension.Width);
        int calculatedY = view.Y.Calculate (100, view.Height, view, Dimension.Height);
        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        Assert.Equal (expectedWidth, calculatedWidth);
        Assert.Equal (expectedHeight, calculatedHeight);
        Assert.Equal (0, calculatedX);
        Assert.Equal (0, calculatedY);
    }

    [Fact]
    public void With_SubView_Using_DimFill_Does_Not_Expand_SuperView_To_SuperSuperView_Size ()
    {
        // This test verifies the bug fix where DimFill subviews were causing
        // their Dim.Auto SuperView to expand to the SuperSuperView's size
        View superSuperView = new () { Width = 100, Height = 50 };
        View autoView = new () { X = 0, Y = 0, Width = Dim.Auto (), Height = Dim.Auto () };
        superSuperView.Add (autoView);

        View fillView = new () { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
        autoView.Add (fillView);

        // Calculate the auto size
        int calculatedWidth = autoView.Width.Calculate (0, 100, autoView, Dimension.Width);
        int calculatedHeight = autoView.Height.Calculate (0, 50, autoView, Dimension.Height);

        // The autoView should NOT expand to superSuperView size (100x50)
        // It should be 0x0 since DimFill doesn't contribute to auto-sizing
        Assert.Equal (0, calculatedWidth);
        Assert.Equal (0, calculatedHeight);
    }

    [Fact]
    public void With_SubView_Using_DimFill_With_MinimumContentDim_Respects_Minimum ()
    {
        // This test verifies that when minimumContentDim is set on the Dim.Auto SuperView,
        // the minimum is respected even when only DimFill subviews are present
        View view = new () { Width = Dim.Auto (minimumContentDim: 20), Height = Dim.Auto (minimumContentDim: 10) };
        View fillView = new () { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
        view.Add (fillView);

        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        // Should respect the minimum even with only DimFill subviews
        Assert.Equal (20, calculatedWidth);
        Assert.Equal (10, calculatedHeight);
    }

    [Fact]
    public void With_SubView_Using_DimFill_With_MinimumContentDim_On_Fill ()
    {
        // This test verifies that DimFill with minimumContentDim contributes to Dim.Auto SuperView sizing
        View view = new () { Width = Dim.Auto (), Height = Dim.Auto () };
        View fillView = new () { X = 0, Y = 0, Width = Dim.Fill (0, minimumContentDim: 40), Height = Dim.Fill (0, minimumContentDim: 20) };
        view.Add (fillView);

        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        // The fill view's minimum should make the auto view at least that size
        Assert.Equal (40, calculatedWidth);
        Assert.Equal (20, calculatedHeight);
    }

    [Theory]
    [InlineData (0, 40, 20, 40, 20)]
    [InlineData (5, 40, 20, 45, 25)]
    [InlineData (10, 30, 15, 40, 25)]
    public void With_SubView_Using_DimFill_With_MinimumContentDim_And_Position (int x, int minW, int minH, int expectedW, int expectedH)
    {
        // Verify that position offset is added to minimum when calculating auto size
        View view = new () { Width = Dim.Auto (), Height = Dim.Auto () };
        View fillView = new () { X = x, Y = x, Width = Dim.Fill (0, minimumContentDim: minW), Height = Dim.Fill (0, minimumContentDim: minH) };
        view.Add (fillView);

        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        Assert.Equal (expectedW, calculatedWidth);
        Assert.Equal (expectedH, calculatedHeight);
    }

    [Fact]
    public void With_SubView_Using_DimFill_With_MinimumContentDim_And_Other_SubViews ()
    {
        // Verify that DimFill minimum and other subviews both contribute
        View view = new () { Width = Dim.Auto (), Height = Dim.Auto () };
        View absView = new () { X = 0, Y = 0, Width = 30, Height = 15 };
        view.Add (absView);

        View fillView = new () { X = 0, Y = 0, Width = Dim.Fill (0, minimumContentDim: 50), Height = Dim.Fill (0, minimumContentDim: 25) };
        view.Add (fillView);

        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        // Should use the larger of the two
        Assert.Equal (50, calculatedWidth);
        Assert.Equal (25, calculatedHeight);
    }

    [Theory]
    [InlineData (0, 0, 40, 20, 40, 20)]
    [InlineData (50, 30, 40, 20, 50, 30)]
    [InlineData (40, 20, 40, 20, 40, 20)]
    public void With_SubView_Using_DimFill_MinimumContentDim_Respects_SuperView_Min (int superMin, int superMinH, int fillMin, int fillMinH, int expectedW, int expectedH)
    {
        // Verify interaction between SuperView minimumContentDim and DimFill minimumContentDim
        View view = new () { Width = Dim.Auto (minimumContentDim: superMin), Height = Dim.Auto (minimumContentDim: superMinH) };
        View fillView = new () { X = 0, Y = 0, Width = Dim.Fill (0, minimumContentDim: fillMin), Height = Dim.Fill (0, minimumContentDim: fillMinH) };
        view.Add (fillView);

        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        // Should use the larger of SuperView min or Fill min
        Assert.Equal (expectedW, calculatedWidth);
        Assert.Equal (expectedH, calculatedHeight);
    }

    // Claude - Opus 4.5
    [Fact]
    public void With_SubView_Using_DimFill_With_To_Contributes_To_AutoSizing ()
    {
        // This test verifies that DimFill with To parameter contributes to Dim.Auto SuperView sizing
        View view = new () { Width = Dim.Auto (), Height = Dim.Auto () };
        
        View toView = new () { X = 80, Y = 40, Width = 10, Height = 5 };
        view.Add (toView);
        
        View fillView = new () { X = 10, Y = 10, Width = Dim.Fill (to: toView), Height = Dim.Fill (to: toView) };
        view.Add (fillView);

        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        // The auto view should be large enough to contain both the fillView and the toView
        // Width: max(fillView.X, toView.X + toView.Width) = max(10, 80 + 10) = 90
        // Height: max(fillView.Y, toView.Y + toView.Height) = max(10, 40 + 5) = 45
        Assert.Equal (90, calculatedWidth);
        Assert.Equal (45, calculatedHeight);
    }

    // Claude - Opus 4.5
    [Fact]
    public void With_SubView_Using_DimFill_With_To_At_Position_Zero ()
    {
        // Test when fillView starts at position 0
        View view = new () { Width = Dim.Auto (), Height = Dim.Auto () };
        
        View toView = new () { X = 50, Y = 30, Width = 20, Height = 10 };
        view.Add (toView);
        
        View fillView = new () { X = 0, Y = 0, Width = Dim.Fill (to: toView), Height = Dim.Fill (to: toView) };
        view.Add (fillView);

        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        // Width: max(0, 50 + 20) = 70
        // Height: max(0, 30 + 10) = 40
        Assert.Equal (70, calculatedWidth);
        Assert.Equal (40, calculatedHeight);
    }

    // Claude - Opus 4.5
    [Fact]
    public void With_SubView_Using_DimFill_With_To_And_Margin ()
    {
        // Test DimFill with both To and margin parameters
        View view = new () { Width = Dim.Auto (), Height = Dim.Auto () };
        
        View toView = new () { X = 80, Y = 40, Width = 10, Height = 5 };
        view.Add (toView);
        
        View fillView = new () { X = 10, Y = 10, Width = Dim.Fill (margin: 5, to: toView), Height = Dim.Fill (margin: 3, to: toView) };
        view.Add (fillView);

        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        // The auto view should still be large enough to contain both views
        // The margin affects the fill size but not the auto-sizing calculation
        Assert.Equal (90, calculatedWidth);
        Assert.Equal (45, calculatedHeight);
    }

    // Claude - Opus 4.5
    [Fact]
    public void With_SubView_Using_DimFill_With_To_And_MinimumContentDim ()
    {
        // Test DimFill with both To and MinimumContentDim - both should contribute
        View view = new () { Width = Dim.Auto (), Height = Dim.Auto () };
        
        View toView = new () { X = 50, Y = 30, Width = 10, Height = 5 };
        view.Add (toView);
        
        // MinimumContentDim is larger than what To would give
        View fillView = new () { X = 10, Y = 10, Width = Dim.Fill (margin: 0, minimumContentDim: 100, to: toView), Height = Dim.Fill (margin: 0, minimumContentDim: 50, to: toView) };
        view.Add (fillView);

        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        // Should use minimum since it's larger
        // Width: 10 (position) + 100 (minimum) = 110
        // Height: 10 (position) + 50 (minimum) = 60
        Assert.Equal (110, calculatedWidth);
        Assert.Equal (60, calculatedHeight);
    }

    // Claude - Opus 4.5
    [Fact]
    public void With_SubView_Using_DimFill_With_To_And_Other_SubViews ()
    {
        // Verify that DimFill with To and other subviews both contribute
        View view = new () { Width = Dim.Auto (), Height = Dim.Auto () };
        
        View absView = new () { X = 5, Y = 5, Width = 100, Height = 50 };
        view.Add (absView);
        
        View toView = new () { X = 80, Y = 40, Width = 10, Height = 10 };
        view.Add (toView);
        
        View fillView = new () { X = 10, Y = 10, Width = Dim.Fill (to: toView), Height = Dim.Fill (to: toView) };
        view.Add (fillView);

        int calculatedWidth = view.Width.Calculate (0, 150, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 150, view, Dimension.Height);

        // Should use the larger of the absolute view or the To-based calculation
        // absView: 5 + 100 = 105
        // fillView with To: max(10, 80 + 10) = 90
        // Result: max(105, 90) = 105
        Assert.Equal (105, calculatedWidth);
        Assert.Equal (55, calculatedHeight); // absView: 5 + 50 = 55 is larger
    }

    #endregion

    #region DimFunc

    [Fact]
    public void With_SubView_Using_DimFunc ()
    {
        View view = new ();
        View subview = new () { Width = Dim.Func (_ => 20), Height = Dim.Func (_ => 25) };
        view.Add (subview);

        subview.SetRelativeLayout (new (100, 100));

        Dim dim = Dim.Auto (DimAutoStyle.Content);
        int calculatedWidth = dim.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = dim.Calculate (0, 100, view, Dimension.Height);

        Assert.Equal (20, calculatedWidth);
        Assert.Equal (25, calculatedHeight);
    }

    #endregion DimFunc

    #region DimView

    [Fact]
    public void With_SubView_Using_DimView ()
    {
        View view = new ();
        View subview = new () { Width = 30, Height = 40 };
        View subSubView = new () { Width = Dim.Width (subview), Height = Dim.Height (subview) };
        view.Add (subview);
        view.Add (subSubView);

        subview.SetRelativeLayout (new (100, 100));

        Dim dim = Dim.Auto (DimAutoStyle.Content);
        int calculatedWidth = dim.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = dim.Calculate (0, 100, view, Dimension.Height);

        // Expecting the size to match the subview, which is the largest
        Assert.Equal (30, calculatedWidth);
        Assert.Equal (40, calculatedHeight);
    }

    #endregion DimView

    #region DimCombine

    // TODO: Need DimCombine tests

    #endregion DimCombine
}

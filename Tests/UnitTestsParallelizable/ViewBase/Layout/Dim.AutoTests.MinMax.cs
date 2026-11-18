namespace ViewBaseTests.Layout;

public partial class DimAutoTests
{
    #region minimumContentDim Tests

    [Fact]
    public void Min_Is_Honored ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (minimumContentDim: 10),
            Height = Dim.Auto (minimumContentDim: 10),
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = 0,
            Y = 0,
            Width = 5,
            Height = 5
        };

        superView.Add (subView);
        superView.BeginInit ();
        superView.EndInit ();

        superView.SetRelativeLayout (new (10, 10));
        superView.LayoutSubViews (); // no throw

        Assert.Equal (10, superView.Frame.Width);
        Assert.Equal (10, superView.Frame.Height);
    }

    [Theory]
    [InlineData (0, 2, 4)]
    [InlineData (1, 2, 4)]
    [InlineData (2, 2, 4)]
    [InlineData (3, 2, 5)]
    [InlineData (1, 0, 3)]
    public void Min_Absolute_Is_Content_Relative (int contentSize, int minAbsolute, int expected)
    {
        var view = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (minimumContentDim: minAbsolute),
            BorderStyle = LineStyle.Single, // a 1 thick adornment
            ValidatePosDim = true
        };
        view.SetContentSize (new (contentSize, 0));
        view.Layout ();

        Assert.Equal (expected, view.Frame.Width);
    }

    [Theory]
    [InlineData (1, 100, 100)]
    [InlineData (1, 50, 50)]
    public void Min_Percent (int contentSize, int minPercent, int expected)
    {
        var view = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (minimumContentDim: Dim.Percent (minPercent)),
            ValidatePosDim = true
        };

        view.SetContentSize (new (contentSize, 0));
        view.SetRelativeLayout (new (100, 100));

        Assert.Equal (expected, view.Frame.Width);
    }

    [Theory]
    [InlineData (1, 100, 102)]
    [InlineData (1, 50, 52)] // 50% of 100 is 50, but the border adds 2
    [InlineData (1, 30, 32)] // 30% of 100 is 30, but the border adds 2
    [InlineData (2, 30, 32)] // 30% of 100 is 30, but the border adds 2
    public void Min_Percent_Is_Content_Relative (int contentSize, int minPercent, int expected)
    {
        var view = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (minimumContentDim: Dim.Percent (minPercent)),
            BorderStyle = LineStyle.Single, // a 1 thick adornment
            ValidatePosDim = true
        };

        view.SetContentSize (new (contentSize, 0));
        view.SetRelativeLayout (new (100, 100));

        Assert.Equal (expected, view.Frame.Width);
    }

    [Fact]
    public void Min_Resets_If_SubView_Moves_Negative ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (minimumContentDim: 10),
            Height = Dim.Auto (minimumContentDim: 10),
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = 0,
            Y = 0,
            Width = 5,
            Height = 5
        };

        superView.Add (subView);
        superView.BeginInit ();
        superView.EndInit ();

        superView.SetRelativeLayout (new (10, 10));
        superView.LayoutSubViews (); // no throw

        Assert.Equal (10, superView.Frame.Width);
        Assert.Equal (10, superView.Frame.Height);

        subView.X = -1;
        subView.Y = -1;
        superView.SetRelativeLayout (new (10, 10));
        superView.LayoutSubViews (); // no throw

        Assert.Equal (5, subView.Frame.Width);
        Assert.Equal (5, subView.Frame.Height);

        Assert.Equal (10, superView.Frame.Width);
        Assert.Equal (10, superView.Frame.Height);
    }

    [Fact]
    public void Min_Resets_If_SubView_Shrinks ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (minimumContentDim: 10),
            Height = Dim.Auto (minimumContentDim: 10),
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = 0,
            Y = 0,
            Width = 5,
            Height = 5
        };

        superView.Add (subView);
        superView.BeginInit ();
        superView.EndInit ();

        superView.SetRelativeLayout (new (10, 10));
        superView.LayoutSubViews (); // no throw

        Assert.Equal (10, superView.Frame.Width);
        Assert.Equal (10, superView.Frame.Height);

        subView.Width = 3;
        subView.Height = 3;
        superView.SetRelativeLayout (new (10, 10));
        superView.LayoutSubViews (); // no throw

        Assert.Equal (3, subView.Frame.Width);
        Assert.Equal (3, subView.Frame.Height);

        Assert.Equal (10, superView.Frame.Width);
        Assert.Equal (10, superView.Frame.Height);
    }

    #endregion minimumContentDim Tests

    // Test min - ensure that if min is specified in the DimAuto constructor it is honored

    // what happens if DimAuto (min: 10) and the subview moves to a negative coord?

    #region maximumContentDim Tests
    // TODO: Add tests

    #endregion maximumContentDim Tests
}

namespace ViewBaseTests.Layout;

public partial class DimAutoTests
{
    // Tests all the Pos Types in SubView scenarios to ensure DimAutoStyle.Content is calculated correctly

    #region PosAbsolute

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 19, 9)]
    [InlineData (0, 20, 0, 10, 20, 10)]
    [InlineData (0, 21, 0, 11, 21, 11)]
    [InlineData (1, 21, 1, 11, 21, 11)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    public void With_SubView_Using_PosAbsolute (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var subview = new View
        {
            X = Pos.Absolute (10),
            Y = Pos.Absolute (5),
            Width = 20,
            Height = 10
        };
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

    #endregion PosAbsolute

    #region PosAlign

    [Theory]
    [InlineData (null, null, null, null, 20, 15)]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 19, 9)]
    [InlineData (0, 20, 0, 10, 20, 10)]
    [InlineData (0, 21, 0, 11, 20, 11)]
    [InlineData (1, 21, 1, 11, 20, 11)]
    [InlineData (20, 21, 15, 16, 20, 15)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    public void With_SubView_Using_PosAlign (int? minWidth, int? maxWidth, int? minHeight, int? maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var subview = new View
        {
            X = Pos.Align (Alignment.Center),
            Y = Pos.Absolute (5),
            Width = 20,
            Height = 10
        };
        view.Add (subview);

        view.BeginInit ();
        view.EndInit ();

        // Assuming the calculation is done after layout
        int calculatedX = view.X.Calculate (100, view.Width, view, Dimension.Width);
        int calculatedY = view.Y.Calculate (100, view.Height, view, Dimension.Height);
        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        Assert.Equal (expectedWidth, calculatedWidth);
        Assert.Equal (expectedHeight, calculatedHeight);

        // Without min and max limitations the view size is (20,15)
        Assert.Equal (0, calculatedX);
        Assert.Equal (0, calculatedY);
    }

    #endregion PosAlign

    #region PosPercent

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 19, 9)]
    [InlineData (0, 20, 0, 10, 20, 10)]
    [InlineData (0, 21, 0, 11, 20, 10)]
    [InlineData (1, 21, 1, 11, 20, 10)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    public void With_SubView_Using_PosPercent (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var subview = new View
        {
            X = Pos.Percent (50),
            Y = Pos.Percent (50),
            Width = 20,
            Height = 10
        };
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

        view.BeginInit ();
        view.EndInit ();

        // subview should be at 50% in the parent view
        Assert.Equal ((int)(view.Viewport.Width * .50), subview.Frame.X);
        Assert.Equal ((int)(view.Viewport.Height * .50), subview.Frame.Y);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 19, 9)]
    [InlineData (0, 20, 0, 10, 20, 10)]
    [InlineData (0, 21, 0, 11, 21, 11)]
    [InlineData (1, 21, 1, 11, 21, 11)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    public void With_SubView_Using_PosPercent_Combine (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var subview = new View
        {
            X = Pos.Percent (50) + 1,
            Y = 1 + Pos.Percent (50),
            Width = 20,
            Height = 10
        };
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

        view.BeginInit ();
        view.EndInit ();

        // subview should be at 50% in the parent view
        Assert.Equal ((int)(view.Viewport.Width * .50) + 1, subview.Frame.X);
        Assert.Equal ((int)(view.Viewport.Height * .50) + 1, subview.Frame.Y);
    }

    #endregion PosPercent

    #region PosCenter

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 19, 9)]
    [InlineData (0, 20, 0, 10, 20, 10)]
    [InlineData (0, 21, 0, 11, 20, 10)]
    [InlineData (1, 21, 1, 11, 20, 10)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    public void With_SubView_Using_PosCenter (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var subview = new View
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = 20,
            Height = 10
        };
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

        view.BeginInit ();
        view.EndInit ();

        // subview should be centered in the parent view + 1
        Assert.Equal ((view.Viewport.Width - subview.Frame.Width) / 2, subview.Frame.X);
        Assert.Equal ((view.Viewport.Height - subview.Frame.Height) / 2, subview.Frame.Y);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 19, 9)]
    [InlineData (0, 18, 0, 8, 18, 8)]
    [InlineData (0, 20, 0, 10, 20, 10)]
    [InlineData (0, 21, 0, 11, 21, 11)]
    [InlineData (1, 21, 1, 11, 21, 11)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    public void With_SubView_Using_PosCenter_Combine (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var subview = new View
        {
            X = Pos.Center () + 1,
            Y = 1 + Pos.Center (),
            Width = 20,
            Height = 10
        };
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

        view.BeginInit ();
        view.EndInit ();

        // subview should be centered in the parent view + 1
        Assert.Equal ((view.Viewport.Width - subview.Frame.Width) / 2 + 1, subview.Frame.X);
        Assert.Equal ((view.Viewport.Height - subview.Frame.Height) / 2 + 1, subview.Frame.Y);
    }

    #endregion PosCenter

    #region PosView

    // TODO: Need PosView tests

    [Fact]
    public void With_SubView_Using_PosView ()
    {
        var view = new View ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
        };
        var subview1 = new View { X = 1, Y = 2, Width = 1, Height = 2 };
        var subview2 = new View { X = Pos.Top (subview1), Y = Pos.Bottom (subview1), Width = 1, Height = 2 };
        view.Add (subview1, subview2);

        view.SetRelativeLayout (new (100, 100));

        // subview1.X + subview1.Width + subview2.Width
        Assert.Equal (1 + 1 + 1, view.Frame.Width);
        // subview1.Y + subview1.Height + subview2.Height
        Assert.Equal (2 + 2 + 2, view.Frame.Height);
    }

    #endregion PosView

    #region PosAnchorEnd

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 19, 9)]
    [InlineData (0, 18, 0, 8, 18, 8)]
    [InlineData (0, 20, 0, 10, 20, 10)]
    [InlineData (0, 21, 0, 11, 20, 10)]
    [InlineData (1, 21, 1, 11, 20, 10)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    public void With_SubView_Using_PosAnchorEnd (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var subview = new View
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Width = 20,
            Height = 10
        };
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

        view.BeginInit ();
        view.EndInit ();

        // subview should be at the end of the view
        Assert.Equal (view.Viewport.Width - subview.Frame.Width, subview.Frame.X);
        Assert.Equal (view.Viewport.Height - subview.Frame.Height, subview.Frame.Y);
    }


    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 19, 9)]
    [InlineData (0, 18, 0, 8, 18, 8)]
    [InlineData (0, 20, 0, 10, 20, 10)]
    [InlineData (0, 21, 0, 11, 21, 11)]
    [InlineData (1, 21, 1, 11, 21, 11)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    [InlineData (0, 30, 0, 20, 25, 15)]
    public void With_SubView_And_SubView_Using_PosAnchorEnd (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var otherView = new View
        {
            Width = 5,
            Height = 5
        };
        view.Add (otherView);

        var subview = new View
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Width = 20,
            Height = 10
        };
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

        view.BeginInit ();
        view.EndInit ();

        // subview should be at the end of the view
        Assert.Equal (view.Viewport.Width - subview.Frame.Width, subview.Frame.X);
        Assert.Equal (view.Viewport.Height - subview.Frame.Height, subview.Frame.Y);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 19, 9)]
    [InlineData (0, 18, 0, 8, 18, 8)]
    [InlineData (0, 20, 0, 10, 20, 10)]
    [InlineData (0, 21, 0, 11, 21, 11)]
    [InlineData (1, 21, 1, 11, 21, 11)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    [InlineData (0, 30, 0, 20, 25, 15)]
    public void With_DimAutoSubView_And_SubView_Using_PosAnchorEnd (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var otherView = new View
        {
            Text = "01234\n01234\n01234\n01234\n01234",
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };
        view.Add (otherView);

        var subview = new View
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Width = 20,
            Height = 10
        };
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

        view.BeginInit ();
        view.EndInit ();

        // subview should be at the end of the view
        Assert.Equal (view.Viewport.Width - subview.Frame.Width, subview.Frame.X);
        Assert.Equal (view.Viewport.Height - subview.Frame.Height, subview.Frame.Y);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 19, 9)]
    [InlineData (0, 18, 0, 8, 18, 8)]
    [InlineData (0, 20, 0, 10, 20, 10)]
    [InlineData (0, 21, 0, 11, 21, 11)]
    [InlineData (1, 21, 1, 11, 21, 11)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    [InlineData (0, 30, 0, 20, 26, 16)]
    public void With_PosViewSubView_And_SubView_Using_PosAnchorEnd (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var otherView = new View
        {
            Width = 1,
            Height = 1,
        };
        view.Add (otherView);

        var posViewView = new View
        {
            X = Pos.Bottom (otherView),
            Y = Pos.Right (otherView),
            Width = 5,
            Height = 5,
        };
        view.Add (posViewView);

        var subview = new View
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Width = 20,
            Height = 10
        };
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

        view.BeginInit ();
        view.EndInit ();

        // subview should be at the end of the view
        Assert.Equal (view.Viewport.Width - subview.Frame.Width, subview.Frame.X);
        Assert.Equal (view.Viewport.Height - subview.Frame.Height, subview.Frame.Y);
    }


    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 19, 9)]
    [InlineData (0, 18, 0, 8, 18, 8)]
    [InlineData (0, 20, 0, 10, 20, 10)]
    [InlineData (0, 21, 0, 11, 21, 11)]
    [InlineData (1, 21, 1, 11, 21, 11)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    [InlineData (0, 30, 0, 20, 22, 12)]
    public void With_DimViewSubView_And_SubView_Using_PosAnchorEnd (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var otherView = new View
        {
            Width = 1,
            Height = 1,
        };
        view.Add (otherView);

        var dimViewView = new View
        {
            Id = "dimViewView",
            X = 1,
            Y = 1,
            Width = Dim.Width (otherView),
            Height = Dim.Height (otherView),
        };
        view.Add (dimViewView);

        var subview = new View
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Width = 20,
            Height = 10
        };
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

        view.BeginInit ();
        view.EndInit ();

        // subview should be at the end of the view
        Assert.Equal (view.Viewport.Width - subview.Frame.Width, subview.Frame.X);
        Assert.Equal (view.Viewport.Height - subview.Frame.Height, subview.Frame.Y);
    }

    [Theory]
    [InlineData (0, 10, 0, 10, 10, 2)]
    [InlineData (0, 5, 0, 5, 5, 3)] // max width of 5 should cause wordwrap at 5 giving a height of 2 + 1
    [InlineData (0, 19, 0, 9, 11, 2)]
    //[InlineData (0, 20, 0, 10, 20, 10)]
    //[InlineData (0, 21, 0, 11, 21, 11)]
    //[InlineData (1, 21, 1, 11, 21, 11)]
    //[InlineData (21, 21, 11, 11, 21, 11)]
    public void With_Text_And_SubView_Using_PosAnchorEnd (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Text = "01234ABCDE",
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };

        // Without a subview, width should be 10
        // Without a subview, height should be 1
        view.SetRelativeLayout (new (100, 100));
        Assert.Equal (10, view.Frame.Width);
        Assert.Equal (1, view.Frame.Height);

        view.Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth);
        view.Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight);

        var subview = new View
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Width = 1,
            Height = 1
        };

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

    #endregion PosAnchorEnd

    #region PosFunc

    [Fact]
    public void With_SubView_Using_PosFunc ()
    {
        var view = new View ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
        };
        var subview = new View
        {
            X = Pos.Func (_ => 20),
            Y = Pos.Func (_ => 25)
        };
        view.Add (subview);

        view.SetRelativeLayout (new (100, 100));

        Assert.Equal (20, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);
    }

    #endregion PosFunc

    #region PosCombine

    // TODO: Need more PosCombine tests

    #endregion PosCombine
}

namespace ViewBaseTests.Layout;

[Trait ("Category", "Layout")]
public class GetViewsAtLocationTests
{
    private class TestView : View
    {
        public TestView (int x, int y, int w, int h, bool visible = true)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            base.Visible = visible;
        }
    }

    [Fact]
    public void ReturnsEmpty_WhenRootIsNull ()
    {
        List<View?> result = View.GetViewsAtLocation (null, new Point (0, 0));
        Assert.Empty (result);
    }

    [Fact]
    public void ReturnsEmpty_WhenRootIsNotVisible ()
    {
        TestView root = new (0, 0, 10, 10, false);
        List<View?> result = View.GetViewsAtLocation (root, new Point (5, 5));
        Assert.Empty (result);
    }

    [Fact]
    public void ReturnsEmpty_WhenPointOutsideRoot ()
    {
        TestView root = new (0, 0, 10, 10);
        List<View?> result = View.GetViewsAtLocation (root, new Point (20, 20));
        Assert.Empty (result);
    }

    [Fact]
    public void ReturnsEmpty_WhenPointOutsideRoot_AndSubview ()
    {
        TestView root = new (0, 0, 10, 10);
        TestView sub = new (5, 5, 2, 2);
        root.Add (sub);
        List<View?> result = View.GetViewsAtLocation (root, new Point (20, 20));
        Assert.Empty (result);
    }

    [Fact]
    public void ReturnsRoot_WhenPointInsideRoot_NoSubviews ()
    {
        TestView root = new (0, 0, 10, 10);
        List<View?> result = View.GetViewsAtLocation (root, new Point (5, 5));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }

    [Fact]
    public void ReturnsRoot_And_Subview_WhenPointInsideRootMargin ()
    {
        TestView root = new (0, 0, 10, 10);
        root.Margin.Thickness = new Thickness (1);
        TestView sub = new (2, 2, 5, 5);
        root.Add (sub);
        List<View?> result = View.GetViewsAtLocation (root, new Point (3, 3));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
    }

    [Fact]
    public void ReturnsRoot_And_Subview_Border_WhenPointInsideRootMargin ()
    {
        TestView root = new (0, 0, 10, 10);
        root.Margin.Thickness = new Thickness (1);
        TestView sub = new (2, 2, 5, 5);
        sub.BorderStyle = LineStyle.Dotted;
        root.Add (sub);
        List<View?> result = View.GetViewsAtLocation (root, new Point (3, 3));
        Assert.Equal (3, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Border.View!, result [2]);
    }

    [Fact]
    public void ReturnsRoot_And_Margin_WhenPointInside_With_Margin ()
    {
        TestView root = new (0, 0, 10, 10);
        root.Margin.Thickness = new Thickness (1);
        root.Margin.GetOrCreateView ();
        List<View?> result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (root.Margin.View!, result [1]);
    }

    [Fact]
    public void ReturnsRoot_WhenPointOutsideSubview_With_Margin ()
    {
        TestView root = new (0, 0, 10, 10);
        root.Margin.Thickness = new Thickness (1);
        root.Margin.GetOrCreateView ();
        TestView sub = new (2, 2, 5, 5);
        root.Add (sub);
        List<View?> result = View.GetViewsAtLocation (root, new Point (2, 2));
        Assert.Single (result);
        Assert.Equal (root, result [0]);

        result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (root.Margin.View!, result [1]);

        result = View.GetViewsAtLocation (root, new Point (1, 1));
        Assert.Single (result);
        Assert.Equal (root, result [0]);

        result = View.GetViewsAtLocation (root, new Point (8, 8));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }

    [Fact]
    public void ReturnsRoot_And_BorderView_WhenPointInside_With_BorderView ()
    {
        TestView root = new (0, 0, 10, 10);
        root.BorderStyle = LineStyle.Dashed; // Sets thickness to 1 and EnsuresView
        List<View?> result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (root.Border.View!, result [1]);
    }

    [Fact]
    public void Returns_Root_WhenPointInside_With_Border ()
    {
        TestView root = new (0, 0, 10, 10);
        root.Border.Thickness = new Thickness (1);
        List<View?> result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }

    [Fact]
    public void ReturnsRoot_WhenPointOutsideSubview_With_BorderView ()
    {
        TestView root = new (0, 0, 10, 10);
        root.BorderStyle = LineStyle.Single;
        TestView sub = new (2, 2, 5, 5);
        root.Add (sub);
        List<View?> result = View.GetViewsAtLocation (root, new Point (2, 2));
        Assert.Single (result);
        Assert.Equal (root, result [0]);

        result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (root.Border.View!, result [1]);

        result = View.GetViewsAtLocation (root, new Point (1, 1));
        Assert.Single (result);
        Assert.Equal (root, result [0]);

        result = View.GetViewsAtLocation (root, new Point (8, 8));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }

    [Fact]
    public void ReturnsRoot_And_BorderView_WhenPointInsideRootBorderView ()
    {
        TestView root = new (0, 0, 10, 10);
        root.BorderStyle = LineStyle.Single;
        List<View?> result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (root.Border.View!, result [1]);
    }

    [Fact]
    public void ReturnsRoot_WhenPointInsideRootBorder ()
    {
        TestView root = new (0, 0, 10, 10);
        root.Border.Thickness = new Thickness (1);
        List<View?> result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }

    [Fact]
    public void ReturnsRoot_And_Padding_WhenPointInsideRootPadding ()
    {
        TestView root = new (0, 0, 10, 10);
        root.Padding.Thickness = new Thickness (1);
        root.Padding.GetOrCreateView ();
        List<View?> result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (root.Padding.View!, result [1]);
    }

    [Fact]
    public void ReturnsRootAndSubview_WhenPointInsideSubview ()
    {
        TestView root = new (0, 0, 10, 10);
        TestView sub = new (2, 2, 5, 5);
        root.Add (sub);

        List<View?> result = View.GetViewsAtLocation (root, new Point (3, 3));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
    }

    [Fact]
    public void ReturnsRootAndSubviewAndMargin_WhenPointInsideSubviewMargin ()
    {
        TestView root = new (0, 0, 10, 10);
        TestView sub = new (2, 2, 5, 5);
        sub.Margin.Thickness = new Thickness (1);
        sub.Margin.GetOrCreateView ();
        root.Add (sub);

        List<View?> result = View.GetViewsAtLocation (root, new Point (6, 6));
        Assert.Equal (3, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Margin.View!, result [2]);
    }

    [Fact]
    public void ReturnsRootAndSubviewAndBorderView_WhenPointInsideSubviewBorderView ()
    {
        TestView root = new (0, 0, 10, 10);
        TestView sub = new (2, 2, 5, 5);
        sub.BorderStyle = LineStyle.Single;
        root.Add (sub);

        List<View?> result = View.GetViewsAtLocation (root, new Point (2, 2));
        Assert.Equal (3, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Border.View!, result [2]);
    }

    [Fact]
    public void ReturnsRootAndSubviewAndSubviewAndBorderView_WhenPointInsideSubviewBorderView ()
    {
        TestView root = new (2, 2, 10, 10);
        TestView sub = new (2, 2, 5, 5);
        sub.BorderStyle = LineStyle.Dashed;
        root.Add (sub);

        List<View?> result = View.GetViewsAtLocation (root, new Point (4, 4));
        Assert.Equal (3, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Border.View!, result [2]);
    }

    [Fact]
    public void ReturnsRootAndSubviewAndBorder_WhenPointInsideSubviewPadding ()
    {
        TestView root = new (0, 0, 10, 10);
        TestView sub = new (2, 2, 5, 5);
        sub.Padding.Thickness = new Thickness (1);
        sub.Padding.GetOrCreateView ();
        root.Add (sub);

        List<View?> result = View.GetViewsAtLocation (root, new Point (2, 2));
        Assert.Equal (3, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Padding.View!, result [2]);
    }

    [Fact]
    public void ReturnsRootAndSubviewAndMarginAndShadowView_WhenPointInsideSubviewMargin ()
    {
        TestView root = new (0, 0, 10, 10);
        TestView sub = new (2, 2, 5, 5);
        sub.ShadowStyle = ShadowStyles.Opaque;
        root.Add (sub);

        root.Layout ();

        List<View?> result = View.GetViewsAtLocation (root, new Point (6, 6));
        Assert.Equal (5, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Margin.View!, result [2]);
        Assert.Equal (sub.Margin.View!.SubViews.ElementAt (0), result [3]);
        Assert.Equal (sub.Margin.View!.SubViews.ElementAt (1), result [4]);
    }

    [Fact]
    public void ReturnsRootAndSubviewAndBorderAndButton_WhenPointInsideSubviewBorder ()
    {
        TestView root = new (0, 0, 10, 10);
        TestView sub = new (2, 2, 5, 5);
        sub.Border.Thickness = new Thickness (1);

        var closeButton = new Button
        {
            NoDecorations = true,
            NoPadding = true,
            Title = "X",
            Width = 1,
            Height = 1,
            X = Pos.AnchorEnd (),
            Y = 0,
            ShadowStyle = null
        };
        sub.Border.GetOrCreateView ().Add (closeButton);
        root.Add (sub);

        root.Layout ();

        List<View?> result = View.GetViewsAtLocation (root, new Point (6, 2));
        Assert.Equal (4, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Border.View!, result [2]);
        Assert.Equal (closeButton, result [3]);
    }

    [Fact]
    public void ReturnsDeepestSubview_WhenNested ()
    {
        TestView root = new (0, 0, 20, 20);
        var sub1 = new TestView (2, 2, 16, 16);
        var sub2 = new TestView (3, 3, 10, 10);
        var sub3 = new TestView (1, 1, 5, 5);
        root.Add (sub1);
        sub1.Add (sub2);
        sub2.Add (sub3);

        // Point inside all
        List<View?> result = View.GetViewsAtLocation (root, new Point (7, 7));
        Assert.Equal (4, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub1, result [1]);
        Assert.Equal (sub2, result [2]);
        Assert.Equal (sub3, result [3]);
    }

    [Fact]
    public void ReturnsTopmostSubview_WhenOverlapping ()
    {
        TestView root = new (0, 0, 10, 10);
        var sub1 = new TestView (2, 2, 6, 6);
        var sub2 = new TestView (4, 4, 6, 6);
        root.Add (sub1);
        root.Add (sub2); // sub2 is on top

        List<View?> result = View.GetViewsAtLocation (root, new Point (5, 5));
        Assert.Equal (3, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub1, result [1]);
        Assert.Equal (sub2, result [2]);
    }

    [Fact]
    public void ReturnsTopmostSubview_WhenNotOverlapping ()
    {
        TestView root = new (0, 0, 10, 10); // under 5,5,
        var sub1 = new TestView (10, 10, 6, 6); // not under location 5,5
        var sub2 = new TestView (4, 4, 6, 6); // under 5,5,
        root.Add (sub1);
        root.Add (sub2); // sub2 is on top

        List<View?> result = View.GetViewsAtLocation (root, new Point (5, 5));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub2, result [1]);
    }

    [Fact]
    public void SkipsInvisibleSubviews ()
    {
        TestView root = new (0, 0, 10, 10);
        var sub1 = new TestView (2, 2, 6, 6, false);
        var sub2 = new TestView (4, 4, 6, 6);
        root.Add (sub1);
        root.Add (sub2);

        List<View?> result = View.GetViewsAtLocation (root, new Point (5, 5));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub2, result [1]);
    }

    [Fact]
    public void ReturnsRoot_WhenPointOnEdge ()
    {
        TestView root = new (0, 0, 10, 10);
        List<View?> result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }

    [Fact]
    public void ReturnsRoot_WhenPointOnBottomRightCorner ()
    {
        TestView root = new (0, 0, 10, 10);
        List<View?> result = View.GetViewsAtLocation (root, new Point (9, 9));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }

    [Fact]
    public void ReturnsEmpty_WhenAllSubviewsInvisible ()
    {
        TestView root = new (0, 0, 10, 10);
        var sub1 = new TestView (2, 2, 6, 6, false);
        root.Add (sub1);

        List<View?> result = View.GetViewsAtLocation (root, new Point (3, 3));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }

    // TODO: Ensure this test is expanded to test when Border.View is non-null
    [Theory]
    [InlineData (0, 0, 0, 0, 0, -1, -1, new string [] { })]
    [InlineData (0, 0, 0, 0, 0, 0, 0, new [] { "Top" })]
    [InlineData (0, 0, 0, 0, 0, 1, 1, new [] { "Top" })]
    [InlineData (0, 0, 0, 0, 0, 4, 4, new [] { "Top" })]
    [InlineData (0, 0, 0, 0, 0, 9, 9, new [] { "Top" })]
    [InlineData (0, 0, 0, 0, 0, 10, 10, new string [] { })]
    [InlineData (1, 1, 0, 0, 0, -1, -1, new string [] { })]
    [InlineData (1, 1, 0, 0, 0, 0, 0, new string [] { })]
    [InlineData (1, 1, 0, 0, 0, 1, 1, new [] { "Top" })]
    [InlineData (1, 1, 0, 0, 0, 4, 4, new [] { "Top" })]
    [InlineData (1, 1, 0, 0, 0, 9, 9, new [] { "Top" })]
    [InlineData (1, 1, 0, 0, 0, 10, 10, new [] { "Top" })]
    [InlineData (0, 0, 1, 0, 0, -1, -1, new string [] { })]
    [InlineData (0, 0, 1, 0, 0, 0, 0, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (0, 0, 1, 0, 0, 1, 1, new [] { "Top" })]
    [InlineData (0, 0, 1, 0, 0, 4, 4, new [] { "Top" })]
    [InlineData (0, 0, 1, 0, 0, 9, 9, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (0, 0, 1, 0, 0, 10, 10, new string [] { })]
    [InlineData (0, 0, 1, 1, 0, -1, -1, new string [] { })]
    [InlineData (0, 0, 1, 1, 0, 0, 0, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (0, 0, 1, 1, 0, 1, 1, new [] { "Top", "BorderView" })]
    [InlineData (0, 0, 1, 1, 0, 4, 4, new [] { "Top" })]
    [InlineData (0, 0, 1, 1, 0, 9, 9, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (0, 0, 1, 1, 0, 10, 10, new string [] { })]
    [InlineData (0, 0, 1, 1, 1, -1, -1, new string [] { })]
    [InlineData (0, 0, 1, 1, 1, 0, 0, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (0, 0, 1, 1, 1, 1, 1, new [] { "Top", "BorderView" })]
    [InlineData (0, 0, 1, 1, 1, 2, 2, new [] { "Top", "PaddingView" })]
    [InlineData (0, 0, 1, 1, 1, 4, 4, new [] { "Top" })]
    [InlineData (0, 0, 1, 1, 1, 9, 9, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (0, 0, 1, 1, 1, 10, 10, new string [] { })]
    [InlineData (1, 1, 1, 0, 0, -1, -1, new string [] { })]
    [InlineData (1, 1, 1, 0, 0, 0, 0, new string [] { })]
    [InlineData (1, 1, 1, 0, 0, 1, 1, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (1, 1, 1, 0, 0, 4, 4, new [] { "Top" })]
    [InlineData (1, 1, 1, 0, 0, 9, 9, new [] { "Top" })]
    [InlineData (1, 1, 1, 0, 0, 10, 10, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (1, 1, 1, 1, 0, -1, -1, new string [] { })]
    [InlineData (1, 1, 1, 1, 0, 0, 0, new string [] { })]
    [InlineData (1, 1, 1, 1, 0, 1, 1, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (1, 1, 1, 1, 0, 4, 4, new [] { "Top" })]
    [InlineData (1, 1, 1, 1, 0, 9, 9, new [] { "Top", "BorderView" })]
    [InlineData (1, 1, 1, 1, 0, 10, 10, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (1, 1, 1, 1, 1, -1, -1, new string [] { })]
    [InlineData (1, 1, 1, 1, 1, 0, 0, new string [] { })]
    [InlineData (1, 1, 1, 1, 1, 1, 1, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (1, 1, 1, 1, 1, 2, 2, new [] { "Top", "BorderView" })]
    [InlineData (1, 1, 1, 1, 1, 3, 3, new [] { "Top", "PaddingView" })]
    [InlineData (1, 1, 1, 1, 1, 4, 4, new [] { "Top" })]
    [InlineData (1, 1, 1, 1, 1, 8, 8, new [] { "Top", "PaddingView" })]
    [InlineData (1, 1, 1, 1, 1, 9, 9, new [] { "Top", "BorderView" })]
    [InlineData (1, 1, 1, 1, 1, 10, 10, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    public void Top_Adornments_Returns_Correct_View (int frameX,
                                                     int frameY,
                                                     int marginThickness,
                                                     int borderThickness,
                                                     int paddingThickness,
                                                     int testX,
                                                     int testY,
                                                     string [] expectedViewsFound)
    {
        // Arrange
        Runnable<bool>? runnable = new () { Id = "Top", Frame = new Rectangle (frameX, frameY, 10, 10) };
        IApplication? app = Application.Create ();
        app.Begin (runnable);
        runnable.Margin.Thickness = new Thickness (marginThickness);
        runnable.Margin.GetOrCreateView ();
        runnable.Margin.View?.Id = "MarginView";
        runnable.Border.Thickness = new Thickness (borderThickness);
        runnable.Border.GetOrCreateView ();
        runnable.Border.View?.Id = "BorderView";
        runnable.Padding.Thickness = new Thickness (paddingThickness);
        runnable.Padding.GetOrCreateView ();
        runnable.Padding.View?.Id = "PaddingView";

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = runnable.GetViewsUnderLocation (location, ViewportSettingsFlags.TransparentMouse);

        // Assert
        if (expectedViewsFound.Length == 0)
        {
            Assert.Empty (viewsUnderMouse);
        }
        else
        {
            string [] foundIds = viewsUnderMouse.Select (v => v!.Id).ToArray ();
            Assert.Equal (expectedViewsFound, foundIds);
        }
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (2, 2)]
    public void Returns_Top_If_No_SubViews (int testX, int testY)
    {
        // Arrange
        Runnable<bool>? runnable = new () { Frame = new Rectangle (0, 0, 10, 10) };
        IApplication? app = Application.Create ();
        app.Begin (runnable);

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = runnable.GetViewsUnderLocation (location, ViewportSettingsFlags.TransparentMouse);

        // Assert
        Assert.Contains (viewsUnderMouse, v => v == runnable);
        runnable.Dispose ();
    }

    // Test that GetViewsUnderLocation returns the correct view if the start view has no subviews
    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (2, 2)]
    public void Returns_Start_If_No_SubViews (int testX, int testY)
    {
        Runnable<bool>? runnable = new () { Width = 10, Height = 10 };
        IApplication? app = Application.Create ();
        app.Begin (runnable);

        Assert.Same (runnable, runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ());
        runnable.Dispose ();
    }

    // Test that GetViewsUnderLocation returns the correct view if the start view has subviews
    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (6, 7, false)]
    [InlineData (1, 2, true)]
    [InlineData (5, 6, true)]
    public void Returns_Correct_If_SubViews (int testX, int testY, bool expectedSubViewFound)
    {
        Runnable<bool>? runnable = new () { Width = 10, Height = 10 };
        IApplication? app = Application.Create ();
        app.Begin (runnable);

        var subview = new View { X = 1, Y = 2, Width = 5, Height = 5 };
        runnable.Add (subview);

        View? found = runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        runnable.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (6, 7, false)]
    [InlineData (1, 2, false)]
    [InlineData (5, 6, false)]
    public void Returns_Null_If_SubView_NotVisible (int testX, int testY, bool expectedSubViewFound)
    {
        Runnable<bool>? runnable = new () { Width = 10, Height = 10 };

        var subview = new View
        {
            X = 1,
            Y = 2,
            Width = 5,
            Height = 5,
            Visible = false
        };
        runnable.Add (subview);

        View? found = runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        runnable.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (6, 7, false)]
    [InlineData (1, 2, false)]
    [InlineData (5, 6, false)]
    public void Returns_Null_If_Not_Visible_And_SubView_Visible (int testX, int testY, bool expectedSubViewFound)
    {
        Runnable<bool>? runnable = new () { Width = 10, Height = 10, Visible = false };

        var subview = new View { X = 1, Y = 2, Width = 5, Height = 5 };
        runnable.Add (subview);
        subview.Visible = true;
        Assert.True (subview.Visible);
        Assert.False (runnable.Visible);
        View? found = runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        runnable.Dispose ();
    }

    // Test that GetViewsUnderLocation works if the start view has positive Adornments
    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (7, 8, false)]
    [InlineData (1, 2, false)]
    [InlineData (2, 3, true)]
    [InlineData (5, 6, true)]
    [InlineData (6, 7, true)]
    public void Returns_Correct_If_Start_Has_Adornments (int testX, int testY, bool expectedSubViewFound)
    {
        Runnable<bool>? runnable = new () { Width = 10, Height = 10 };
        IApplication? app = Application.Create ();
        app.Begin (runnable);
        runnable.Margin.Thickness = new Thickness (1);

        var subview = new View { X = 1, Y = 2, Width = 5, Height = 5 };
        runnable.Add (subview);

        View? found = runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        runnable.Dispose ();
    }

    // Test that GetViewsUnderLocation works if the start view has offset Viewport location
    [Theory]
    [InlineData (1, 0, 0, true)]
    [InlineData (1, 1, 1, true)]
    [InlineData (1, 2, 2, false)]
    [InlineData (-1, 3, 3, true)]
    [InlineData (-1, 2, 2, true)]
    [InlineData (-1, 1, 1, false)]
    [InlineData (-1, 0, 0, false)]
    public void Returns_Correct_If_Start_Has_Offset_Viewport (int offset, int testX, int testY, bool expectedSubViewFound)
    {
        Runnable<bool>? runnable = new ()
        {
            Width = 10,
            Height = 10,
            ViewportSettings = ViewportSettingsFlags.AllowNegativeLocation | ViewportSettingsFlags.AllowLocationPlusSizeGreaterThanContentSize
        };
        IApplication? app = Application.Create ();
        app.Begin (runnable);
        runnable.Viewport = new Rectangle (offset, offset, 10, 10);

        var subview = new View { X = 1, Y = 1, Width = 2, Height = 2 };
        runnable.Add (subview);

        View? found = runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        runnable.Dispose ();
    }

    [Theory]
    [InlineData (9, 9, true)]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (10, 10, false)]
    [InlineData (7, 8, false)]
    [InlineData (1, 2, false)]
    [InlineData (2, 3, false)]
    [InlineData (5, 6, false)]
    [InlineData (6, 7, false)]
    public void Returns_Correct_If_Start_Has_Adornment_WithSubView (int testX, int testY, bool expectedSubViewFound)
    {
        Runnable<bool>? runnable = new () { Width = 10, Height = 10 };
        IApplication? app = Application.Create ();
        app.Begin (runnable);
        runnable.Padding.Thickness = new Thickness (1);

        var subview = new View { X = Pos.AnchorEnd (1), Y = Pos.AnchorEnd (1), Width = 1, Height = 1 };
        runnable.Padding.GetOrCreateView ().Add (subview);

        View? found = runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        runnable.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, new string [] { })]
    [InlineData (9, 9, new string [] { })]
    [InlineData (1, 1, new [] { "Top", "BorderView" })]
    [InlineData (8, 8, new [] { "Top", "BorderView" })]
    [InlineData (2, 2, new [] { "Top", "PaddingView" })]
    [InlineData (7, 7, new [] { "Top", "PaddingView" })]
    [InlineData (5, 5, new [] { "Top" })]
    public void Returns_Adornment_If_Start_Has_Adornments (int testX, int testY, string [] expectedViewsFound)
    {
        IApplication? app = Application.Create ();

        Runnable<bool>? runnable = new () { Id = "Top", Width = 10, Height = 10 };
        app.Begin (runnable);

        runnable.Margin.Thickness = new Thickness (1);
        runnable.Margin.GetOrCreateView ();
        runnable.Margin.View!.Id = "MarginView";
        runnable.Border.Thickness = new Thickness (1);
        runnable.Border.GetOrCreateView ();
        runnable.Border.View!.Id = "BorderView";
        runnable.Padding.Thickness = new Thickness (1);
        runnable.Padding.GetOrCreateView ();
        runnable.Padding.View!.Id = "PaddingView";

        var subview = new View
        {
            Id = "SubView",
            X = 1,
            Y = 1,
            Width = 1,
            Height = 1
        };
        runnable.Add (subview);

        List<View?> viewsUnderMouse = runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse);
        string [] foundIds = viewsUnderMouse.Select (v => v!.Id).ToArray ();

        Assert.Equal (expectedViewsFound, foundIds);
        runnable.Dispose ();
    }

    // Test that GetViewsUnderLocation works if the subview has positive Adornments
    [Theory]
    [InlineData (0, 0, new [] { "Top" })]
    [InlineData (1, 1, new [] { "Top" })]
    [InlineData (9, 9, new [] { "Top" })]
    [InlineData (10, 10, new string [] { })]
    [InlineData (7, 8, new [] { "Top" })]
    [InlineData (6, 7, new [] { "Top" })]
    [InlineData (1, 2, new [] { "Top", "subview", "border" })]
    [InlineData (5, 6, new [] { "Top", "subview", "border" })]
    [InlineData (2, 3, new [] { "Top", "subview" })]
    public void Returns_Correct_If_SubView_Has_Adornments (int testX, int testY, string [] expectedViewsFound)
    {
        Runnable<bool>? runnable = new () { Id = "Top", Width = 10, Height = 10 };
        IApplication? app = Application.Create ();
        app.Begin (runnable);

        var subview = new View
        {
            Id = "subview",
            X = 1,
            Y = 2,
            Width = 5,
            Height = 5
        };
        subview.BorderStyle = LineStyle.Dashed; // Sets thickness to 1 and EnsuresView
        subview.Border.View?.Id = "border";
        runnable.Add (subview);

        List<View?> viewsUnderMouse = runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse);
        string [] foundIds = viewsUnderMouse.Select (v => v!.Id).ToArray ();

        Assert.Equal (expectedViewsFound, foundIds);
        runnable.Dispose ();
    }

    // Test that GetViewsUnderLocation works if the subview has positive Adornments
    [Theory]
    [InlineData (0, 0, new [] { "Top" })]
    [InlineData (1, 1, new [] { "Top" })]
    [InlineData (9, 9, new [] { "Top" })]
    [InlineData (10, 10, new string [] { })]
    [InlineData (7, 8, new [] { "Top" })]
    [InlineData (6, 7, new [] { "Top" })]
    [InlineData (1, 2, new [] { "Top" })]
    [InlineData (5, 6, new [] { "Top" })]
    [InlineData (2, 3, new [] { "Top", "subview" })]
    public void Returns_Correct_If_SubView_Has_Adornments_With_TransparentMouse (int testX, int testY, string [] expectedViewsFound)
    {
        Runnable<bool>? runnable = new () { Id = "Top", Width = 10, Height = 10 };
        IApplication? app = Application.Create ();
        app.Begin (runnable);

        var subview = new View
        {
            Id = "subview",
            X = 1,
            Y = 2,
            Width = 5,
            Height = 5
        };

        subview.BorderStyle = LineStyle.Dashed; // Sets thickness to 1 and EnsuresView
        subview.Border.ViewportSettings = ViewportSettingsFlags.TransparentMouse;
        subview.Border.View!.Id = "border";
        runnable.Add (subview);

        List<View?> viewsUnderMouse = runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse);
        string [] foundIds = viewsUnderMouse.Select (v => v!.Id).ToArray ();

        Assert.Equal (expectedViewsFound, foundIds);
        runnable.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (7, 8, false)]
    [InlineData (6, 7, false)]
    [InlineData (1, 2, false)]
    [InlineData (5, 6, false)]
    [InlineData (6, 5, false)]
    [InlineData (5, 5, true)]
    public void Returns_Correct_If_SubView_Has_Adornment_WithSubView (int testX, int testY, bool expectedSubViewFound)
    {
        Runnable<bool>? runnable = new () { Width = 10, Height = 10 };
        IApplication? app = Application.Create ();
        app.Begin (runnable);

        // A subview with + Padding
        var subview = new View { X = 1, Y = 1, Width = 5, Height = 5 };
        subview.Padding.Thickness = new Thickness (1);

        // This subview will be at the bottom-right-corner of subview
        // So screen-relative location will be X + Width - 1 = 5
        var paddingSubView = new View { X = Pos.AnchorEnd (1), Y = Pos.AnchorEnd (1), Width = 1, Height = 1 };
        subview.Padding.GetOrCreateView ().Add (paddingSubView);
        runnable.Add (subview);

        View? found = runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == paddingSubView);
        runnable.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (7, 8, false)]
    [InlineData (6, 7, false)]
    [InlineData (1, 2, false)]
    [InlineData (5, 6, false)]
    [InlineData (6, 5, false)]
    [InlineData (5, 5, true)]
    public void Returns_Correct_If_SubView_Is_Scrolled_And_Has_Adornment_WithSubView (int testX, int testY, bool expectedSubViewFound)
    {
        Runnable<bool>? runnable = new () { Width = 10, Height = 10 };
        IApplication? app = Application.Create ();
        app.Begin (runnable);

        // A subview with + Padding
        var subview = new View { X = 1, Y = 1, Width = 5, Height = 5 };
        subview.Padding.Thickness = new Thickness (1);

        // Scroll the subview
        subview.SetContentSize (new Size (10, 10));
        subview.Viewport = subview.Viewport with { Location = new Point (1, 1) };

        // This subview will be at the bottom-right-corner of subview
        // So screen-relative location will be X + Width - 1 = 5
        var paddingSubView = new View { X = Pos.AnchorEnd (1), Y = Pos.AnchorEnd (1), Width = 1, Height = 1 };
        subview.Padding.GetOrCreateView ().Add (paddingSubView);
        runnable.Add (subview);

        View? found = runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == paddingSubView);
        runnable.Dispose ();
    }

    // Test that GetViewsUnderLocation works with nested subviews
    [Theory]
    [InlineData (0, 0, -1)]
    [InlineData (9, 9, -1)]
    [InlineData (10, 10, -1)]
    [InlineData (1, 1, 0)]
    [InlineData (1, 2, 0)]
    [InlineData (2, 2, 1)]
    [InlineData (3, 3, 2)]
    [InlineData (5, 5, 2)]
    public void Returns_Correct_With_NestedSubViews (int testX, int testY, int expectedSubViewFound)
    {
        Runnable<bool>? runnable = new () { Width = 10, Height = 10 };
        IApplication? app = Application.Create ();
        app.Begin (runnable);

        var numSubViews = 3;
        List<View> subviews = new ();

        for (var i = 0; i < numSubViews; i++)
        {
            var subview = new View { X = 1, Y = 1, Width = 5, Height = 5 };
            subviews.Add (subview);

            if (i > 0)
            {
                subviews [i - 1].Add (subview);
            }
        }

        runnable.Add (subviews [0]);

        View? found = runnable.GetViewsUnderLocation (new Point (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();
        Assert.Equal (expectedSubViewFound, subviews.IndexOf (found!));
        runnable.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, new [] { "top" })]
    [InlineData (9, 9, new [] { "top" })]
    [InlineData (10, 10, new string [] { })]
    [InlineData (1, 1, new [] { "top", "view" })]
    [InlineData (1, 2, new [] { "top", "view" })]
    [InlineData (2, 1, new [] { "top", "view" })]
    [InlineData (2, 2, new [] { "top", "view", "subView" })]
    [InlineData (3, 3, new [] { "top" })] // clipped
    [InlineData (2, 3, new [] { "top" })] // clipped
    public void Tiled_SubViews (int mouseX, int mouseY, string [] viewIdStrings)
    {
        // Arrange
        Runnable<bool>? runnable = new () { Frame = new Rectangle (0, 0, 10, 10), Id = "top" };
        IApplication? app = Application.Create ();
        app.Begin (runnable);

        var view = new View
        {
            Id = "view",
            X = 1,
            Y = 1,
            Width = 2,
            Height = 2,
            Arrangement = ViewArrangement.Overlapped
        }; // at 1,1 to 3,2 (screen)

        var subView = new View
        {
            Id = "subView",
            X = 1,
            Y = 1,
            Width = 2,
            Height = 2,
            Arrangement = ViewArrangement.Overlapped
        }; // at 2,2 to 4,3 (screen)
        view.Add (subView);
        runnable.Add (view);

        List<View?> found = runnable.GetViewsUnderLocation (new Point (mouseX, mouseY), ViewportSettingsFlags.TransparentMouse);

        string [] foundIds = found.Select (v => v!.Id).ToArray ();

        Assert.Equal (viewIdStrings, foundIds);

        runnable.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, new [] { "top" })]
    [InlineData (9, 9, new [] { "top" })]
    [InlineData (10, 10, new string [] { })]
    [InlineData (-1, -1, new string [] { })]
    [InlineData (1, 1, new [] { "top", "view" })]
    [InlineData (1, 2, new [] { "top", "view" })]
    [InlineData (2, 1, new [] { "top", "view" })]
    [InlineData (2, 2, new [] { "top", "view", "popover" })]
    [InlineData (3, 3, new [] { "top" })] // clipped
    [InlineData (2, 3, new [] { "top" })] // clipped
    public void Popover (int mouseX, int mouseY, string [] viewIdStrings)
    {
        // Arrange
        Runnable<bool>? runnable = new () { Frame = new Rectangle (0, 0, 10, 10), Id = "top" };

        IApplication? app = Application.Create ();
        app.Begin (runnable);

        var view = new View
        {
            Id = "view",
            X = 1,
            Y = 1,
            Width = 2,
            Height = 2,
            Arrangement = ViewArrangement.Overlapped
        }; // at 1,1 to 3,2 (screen)

        var popOver = new View
        {
            Id = "popover",
            X = 1,
            Y = 1,
            Width = 2,
            Height = 2,
            Arrangement = ViewArrangement.Overlapped
        }; // at 2,2 to 4,3 (screen)

        view.Add (popOver);
        runnable.Add (view);

        List<View?> found = runnable.GetViewsUnderLocation (new Point (mouseX, mouseY), ViewportSettingsFlags.TransparentMouse);

        string [] foundIds = found.Select (v => v!.Id).ToArray ();

        Assert.Equal (viewIdStrings, foundIds);

        runnable.Dispose ();
    }

    [Fact]
    public void Returns_TopRunnable_When_Point_Inside_Only_TopRunnable ()
    {
        IApplication? app = Application.Create ();

        Runnable<bool> runnable = new () { Id = "topRunnable", Frame = new Rectangle (0, 0, 20, 20) };

        Runnable<bool> secondaryRunnable = new () { Id = "secondaryRunnable", Frame = new Rectangle (5, 5, 10, 10) };
        secondaryRunnable.Margin.Thickness = new Thickness (1);
        secondaryRunnable.Layout ();

        app.Begin (runnable);
        app.Begin (secondaryRunnable);

        List<View?> found = runnable.GetViewsUnderLocation (new Point (2, 2), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (found, v => v?.Id == runnable.Id);
        Assert.Contains (found, v => v == runnable);

        runnable.Dispose ();
        secondaryRunnable.Dispose ();
    }

    [Fact]
    public void Returns_SecondaryRunnable_When_Point_Inside_Only_SecondaryRunnable ()
    {
        IApplication? app = Application.Create ();

        Runnable<bool> runnable = new () { Id = "topRunnable", Frame = new Rectangle (0, 0, 20, 20) };

        Runnable<bool> secondaryRunnable = new () { Id = "secondaryRunnable", Frame = new Rectangle (5, 5, 10, 10) };
        secondaryRunnable.Margin.Thickness = new Thickness (1);
        secondaryRunnable.Layout ();

        app.Begin (runnable);
        app.Begin (secondaryRunnable);

        List<View?> found = runnable.GetViewsUnderLocation (new Point (7, 7), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (found, v => v?.Id == secondaryRunnable.Id);
        Assert.DoesNotContain (found, v => v?.Id == runnable.Id);

        runnable.Dispose ();
        secondaryRunnable.Dispose ();
    }

    [Fact]
    public void Returns_Depends_On_Margin_ViewportSettings_When_Point_In_Margin_Of_SecondaryRunnable ()
    {
        IApplication? app = Application.Create ();

        Runnable<bool> runnable = new () { Id = "topRunnable", Frame = new Rectangle (0, 0, 20, 20) };

        Runnable<bool> secondaryRunnable = new () { Id = "secondaryRunnable", Frame = new Rectangle (5, 5, 10, 10) };
        secondaryRunnable.Margin.Thickness = new Thickness (1);
        secondaryRunnable.Margin.GetOrCreateView ();

        app.Begin (runnable);
        app.Begin (secondaryRunnable);

        secondaryRunnable.Margin.ViewportSettings = ViewportSettingsFlags.None;

        List<View?> found = runnable.GetViewsUnderLocation (new Point (5, 5), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (found, v => v == secondaryRunnable);
        Assert.Contains (found, v => v == secondaryRunnable.Margin.View!);
        Assert.DoesNotContain (found, v => v?.Id == runnable.Id);

        secondaryRunnable.Margin.ViewportSettings = ViewportSettingsFlags.TransparentMouse;
        found = runnable.GetViewsUnderLocation (new Point (5, 5), ViewportSettingsFlags.TransparentMouse);
        Assert.DoesNotContain (found, v => v == secondaryRunnable);
        Assert.DoesNotContain (found, v => v == secondaryRunnable.Margin.View!);
        Assert.Contains (found, v => v?.Id == runnable.Id);

        runnable.Dispose ();
        secondaryRunnable.Dispose ();
    }

    [Fact]
    public void Returns_Empty_When_Point_Outside_All_Runnables ()
    {
        IApplication? app = Application.Create ();

        Runnable<bool> runnable = new () { Id = "topRunnable", Frame = new Rectangle (0, 0, 20, 20) };

        Runnable<bool> secondaryRunnable = new () { Id = "secondaryRunnable", Frame = new Rectangle (5, 5, 10, 10) };
        secondaryRunnable.Margin.Thickness = new Thickness (1);
        secondaryRunnable.Layout ();

        app.Begin (runnable);
        app.Begin (secondaryRunnable);

        List<View?> found = runnable.GetViewsUnderLocation (new Point (20, 20), ViewportSettingsFlags.TransparentMouse);
        Assert.Empty (found);

        runnable.Dispose ();
        secondaryRunnable.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void ReturnsEmpty_WhenRootIsDisabled ()
    {
        TestView root = new (0, 0, 10, 10) { Enabled = false };
        List<View?> result = View.GetViewsAtLocation (root, new Point (5, 5));
        Assert.Empty (result);
    }

    // Claude - Opus 4.5
    [Fact]
    public void ExcludesDisabledSubview ()
    {
        TestView root = new (0, 0, 10, 10);
        TestView enabledSub = new (2, 2, 3, 3) { Enabled = true };
        TestView disabledSub = new (6, 6, 3, 3) { Enabled = false };
        root.Add (enabledSub);
        root.Add (disabledSub);

        // Point inside disabled subview - should only return root
        List<View?> result = View.GetViewsAtLocation (root, new Point (7, 7));
        Assert.Single (result);
        Assert.Equal (root, result [0]);

        // Point inside enabled subview - should return root and enabled subview
        result = View.GetViewsAtLocation (root, new Point (3, 3));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (enabledSub, result [1]);
    }

    // Claude - Opus 4.5
    [Fact]
    public void ExcludesDisabledSubview_WithNestedSubviews ()
    {
        TestView root = new (0, 0, 20, 20);
        TestView disabledParent = new (5, 5, 10, 10) { Enabled = false };
        TestView enabledChild = new (2, 2, 4, 4) { Enabled = true };
        root.Add (disabledParent);
        disabledParent.Add (enabledChild);

        // Point inside enabled child of disabled parent - should only return root
        // because disabled parent is excluded from hit testing
        List<View?> result = View.GetViewsAtLocation (root, new Point (9, 9));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }

    // Claude - Opus 4.5
    [Fact]
    public void IncludesEnabledSubview_WhenBothEnabledAndDisabledSubviewsAtSameLocation ()
    {
        TestView root = new (0, 0, 20, 20);
        TestView disabledSub = new (5, 5, 10, 10) { Enabled = false };
        TestView enabledSub = new (5, 5, 10, 10) { Enabled = true };
        root.Add (disabledSub);
        root.Add (enabledSub);

        // Point where both subviews overlap - should only return root and enabled subview
        List<View?> result = View.GetViewsAtLocation (root, new Point (10, 10));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (enabledSub, result [1]);
    }
}

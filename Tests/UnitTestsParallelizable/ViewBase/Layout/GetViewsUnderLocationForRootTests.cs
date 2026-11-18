namespace ViewBaseTests.Layout;

[Trait ("Category", "Input")]
public class GetViewsUnderLocationForRootTests
{
    [Fact]
    public void ReturnsRoot_WhenPointInsideRoot_NoSubviews ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        List<View?> result = View.GetViewsUnderLocation (top, new Point (5, 5), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
    }

    [Fact]
    public void ReturnsEmpty_WhenPointOutsideRoot ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        List<View?> result = View.GetViewsUnderLocation (top, new Point (20, 20), ViewportSettingsFlags.TransparentMouse);
        Assert.Empty (result);
    }

    [Fact]
    public void ReturnsSubview_WhenPointInsideSubview ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };

        View sub = new () { X = 2, Y = 2, Width = 5, Height = 5 };
        top.Add (sub);
        List<View?> result = View.GetViewsUnderLocation (top, new Point (3, 3), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.Contains (sub, result);
        Assert.Equal (sub, result.Last ());
    }

    [Fact]
    public void ReturnsTop_WhenPointInsideSubview_With_TransparentMouse ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };

        View sub = new ()
        {
            X = 2,
            Y = 2,
            Width = 5,
            Height = 5,
            ViewportSettings = ViewportSettingsFlags.TransparentMouse
        };
        top.Add (sub);
        List<View?> result = View.GetViewsUnderLocation (top, new Point (3, 3), ViewportSettingsFlags.TransparentMouse);
        Assert.Single (result);
        Assert.Contains (top, result);

        result = View.GetViewsUnderLocation (top, new Point (3, 3), ViewportSettingsFlags.None);
        Assert.Equal (2, result.Count);
        Assert.Contains (top, result);
        Assert.Contains (sub, result);
    }

    [Fact]
    public void ReturnsAdornment_WhenPointInMargin ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        top.Margin.Thickness = new Thickness (1);
        top.Margin.GetOrCreateView ();
        top.Margin.ViewportSettings = ViewportSettingsFlags.None;
        List<View?> result = View.GetViewsUnderLocation (top, new Point (0, 0), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.Contains (top.Margin.View!, result);
    }

    [Fact]
    public void Returns_WhenPointIn_TransparentToMouseMarginView_None ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        top.Margin.Thickness = new Thickness (1);
        top.Margin.GetOrCreateView ();
        top.Margin.ViewportSettings = ViewportSettingsFlags.TransparentMouse;
        List<View?> result = View.GetViewsUnderLocation (top, new Point (0, 0), ViewportSettingsFlags.TransparentMouse);
        Assert.DoesNotContain (top, result);
        Assert.DoesNotContain (top.Margin.View, result);
    }

    [Fact]
    public void Returns_WhenPointIn_TransparentToMouseMargin_None ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        top.Margin.Thickness = new Thickness (1);
        top.Margin.ViewportSettings = ViewportSettingsFlags.TransparentMouse;
        List<View?> result = View.GetViewsUnderLocation (top, new Point (0, 0), ViewportSettingsFlags.TransparentMouse);
        Assert.DoesNotContain (top, result);
        Assert.DoesNotContain (top.Margin.View, result);
    }

    [Fact]
    public void Returns_WhenPointIn_NotTransparentToMouseMarginView_Top_And_Margin ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        top.Margin.Thickness = new Thickness (1);
        top.Margin.GetOrCreateView ();
        top.Margin.ViewportSettings = ViewportSettingsFlags.None;
        List<View?> result = View.GetViewsUnderLocation (top, new Point (0, 0), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.Contains (top.Margin.View!, result);
    }

    [Fact]
    public void Returns_WhenPointIn_NotTransparentToMouseMargin_Top ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        top.Margin.Thickness = new Thickness (1);
        top.Margin.ViewportSettings = ViewportSettingsFlags.None;
        List<View?> result = View.GetViewsUnderLocation (top, new Point (0, 0), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.DoesNotContain (top.Margin.View, result);
    }

    [Fact]
    public void Returns_WhenPointIn_TransparentToMouseBorder_None ()
    {
        // Claude - Opus 4.6
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        top.Border.Thickness = new Thickness (1);
        top.Border.ViewportSettings = ViewportSettingsFlags.TransparentMouse;
        List<View?> result = View.GetViewsUnderLocation (top, new Point (0, 0), ViewportSettingsFlags.TransparentMouse);
        Assert.DoesNotContain (top, result);
        Assert.DoesNotContain (top.Border.View, result);
    }

    [Fact]
    public void Returns_WhenPointIn_Runnable_NotTransparentToMouseBorder_Top_And_BorderView ()
    {
        // Claude - Opus 4.6
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };

        top.Border.Thickness = new Thickness (1);
        top.Border.ViewportSettings = ViewportSettingsFlags.None;
        List<View?> result = View.GetViewsUnderLocation (top, new Point (0, 0), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);

        // Runnable sets Border.Settings to include TerminalTitle, which causes the Border to create
        // a BorderView.
        Assert.Contains (top.Border.View, result);
    }

    [Fact]
    public void Returns_WhenPointIn_NotTransparentToMouseBorder_Top_And_BorderView ()
    {
        // Claude - Opus 4.6
        View top = new () { Frame = new Rectangle (0, 0, 10, 10) };

        top.Border.Thickness = new Thickness (1);
        top.Border.ViewportSettings = ViewportSettingsFlags.None;
        List<View?> result = View.GetViewsUnderLocation (top, new Point (0, 0), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.DoesNotContain (top.Border.View, result);
    }

    [Fact]
    public void Returns_WhenPointIn_TransparentToMousePadding_None ()
    {
        // Claude - Opus 4.6
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        top.Border.Thickness = new Thickness (1);
        top.Padding.Thickness = new Thickness (1);
        top.Padding.ViewportSettings = ViewportSettingsFlags.TransparentMouse;
        top.Layout ();
        List<View?> result = View.GetViewsUnderLocation (top, new Point (1, 1), ViewportSettingsFlags.TransparentMouse);
        Assert.DoesNotContain (top, result);
        Assert.DoesNotContain (top.Padding.View, result);
    }

    [Fact]
    public void Returns_WhenPointIn_NotTransparentToMousePadding_Top ()
    {
        // Claude - Opus 4.6
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        top.Border.Thickness = new Thickness (1);
        top.Padding.Thickness = new Thickness (1);
        top.Padding.ViewportSettings = ViewportSettingsFlags.None;
        top.Layout ();
        List<View?> result = View.GetViewsUnderLocation (top, new Point (1, 1), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.DoesNotContain (top.Padding.View, result);
    }

    [Fact]
    public void Returns_Top_WhenPointInContentArea_TransparentToMouseMargin_None ()
    {
        // Claude - Opus 4.6
        // Verifies that a point in the content area (inside the margin ring) does NOT
        // cause the view to be removed, even when the margin has TransparentMouse set.
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        top.Margin.Thickness = new Thickness (1);
        top.Margin.ViewportSettings = ViewportSettingsFlags.TransparentMouse;
        List<View?> result = View.GetViewsUnderLocation (top, new Point (5, 5), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
    }

    [Fact]
    public void ReturnsAdornment_WhenPointInBorderView ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        top.BorderStyle = LineStyle.Single;
        List<View?> result = View.GetViewsUnderLocation (top, new Point (0, 0), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.Contains (top.Border.View!, result);
    }

    [Fact]
    public void ReturnsAdornment_WhenPointInPadding ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };
        top.Border.Thickness = new Thickness (1);
        top.Padding.Thickness = new Thickness (1);
        top.Padding.GetOrCreateView ();
        top.Layout ();
        List<View?> result = View.GetViewsUnderLocation (top, new Point (1, 1), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.Contains (top.Padding.View, result);
    }

    [Fact]
    public void HonorsIgnoreTransparentMouseParam ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10), ViewportSettings = ViewportSettingsFlags.TransparentMouse };
        List<View?> result = View.GetViewsUnderLocation (top, new Point (5, 5), ViewportSettingsFlags.TransparentMouse);
        Assert.Empty (result);

        result = View.GetViewsUnderLocation (top, new Point (5, 5), ViewportSettingsFlags.None);
        Assert.NotEmpty (result);
    }

    [Fact]
    public void ReturnsDeepestSubview_WhenNested ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };

        View sub1 = new () { X = 1, Y = 1, Width = 8, Height = 8 };

        View sub2 = new () { X = 1, Y = 1, Width = 6, Height = 6 };
        sub1.Add (sub2);
        top.Add (sub1);
        List<View?> result = View.GetViewsUnderLocation (top, new Point (3, 3), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (sub2, result);
        Assert.Equal (sub2, result.Last ());
    }

    [Fact]
    public void Returns_Subview_WhenPointIn_TransparentToMouseMarginView_Top ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 20, 20) };

        View subView = new () { Frame = new Rectangle (0, 0, 5, 5) };
        subView.Margin.Thickness = new Thickness (1);
        subView.Margin.GetOrCreateView ();
        subView.Margin.ViewportSettings = ViewportSettingsFlags.None;
        top.Add (subView);

        Assert.True (subView.Contains (new Point (4, 4)));
        List<View?> result = View.GetViewsUnderLocation (top, new Point (4, 4), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.Contains (subView.Margin.View!, result);
        Assert.Contains (subView, result);

        subView.Margin.ViewportSettings = ViewportSettingsFlags.TransparentMouse;

        result = View.GetViewsUnderLocation (top, new Point (4, 4), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.DoesNotContain (subView.Margin.View, result);
        Assert.DoesNotContain (subView, result);
    }

    [Fact]
    public void Returns_Subview_WhenPointIn_TransparentToMouseMargin_Top ()
    {
        Runnable top = new () { Frame = new Rectangle (0, 0, 20, 20) };

        View subView = new () { Frame = new Rectangle (0, 0, 5, 5) };
        subView.Margin.Thickness = new Thickness (1);
        subView.Margin.ViewportSettings = ViewportSettingsFlags.None;
        top.Add (subView);

        Assert.True (subView.Contains (new Point (4, 4)));
        List<View?> result = View.GetViewsUnderLocation (top, new Point (4, 4), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.DoesNotContain (subView.Margin.View, result);
        Assert.Contains (subView, result);

        subView.Margin.ViewportSettings = ViewportSettingsFlags.TransparentMouse;

        result = View.GetViewsUnderLocation (top, new Point (4, 4), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.DoesNotContain (subView.Margin.View!, result);
        Assert.DoesNotContain (subView, result);
    }

    [Theory]
    [InlineData ("Border")]
    [InlineData ("Padding")]
    public void Returns_Subview_Of_Adornment (string adornmentType)
    {
        // Arrange: top -> subView -> subView.[Adornment] -> adornmentSubView
        Runnable top = new () { Frame = new Rectangle (0, 0, 10, 10) };

        View subView = new () { X = 0, Y = 0, Width = 8, Height = 8 };
        top.Add (subView);

        IAdornment? adornment = null;

        switch (adornmentType)
        {
            case "Margin":
                subView.Margin.Thickness = new Thickness (2);
                adornment = subView.Margin;

                break;

            case "Border":
                subView.Border.Thickness = new Thickness (2);
                adornment = subView.Border;

                break;

            case "Padding":
                subView.Padding.Thickness = new Thickness (2);
                adornment = subView.Padding;

                break;
        }

        (adornment as AdornmentImpl)?.GetOrCreateView ();

        subView.Layout ();

        // Add a child to the adornment
        View adornmentSubView = new () { X = 0, Y = 0, Width = 2, Height = 2 };
        (adornment?.View as View)?.Add (adornmentSubView);

        // Set adornment ViewportSettings to None so it doesn't interfere with the test
        (adornment as AdornmentImpl)?.ViewportSettings = ViewportSettingsFlags.None;

        // Act: Point inside adornmentSubView (which is inside the adornment)
        List<View?> result = View.GetViewsUnderLocation (top, new Point (0, 0), ViewportSettingsFlags.TransparentMouse);

        // Assert: Should contain top, subView, adornment, and adornmentSubView
        Assert.Contains (top, result);
        Assert.Contains (subView, result);
        Assert.Contains (adornmentSubView, result);
        Assert.Equal (top, result [0]);
        Assert.Equal (adornmentSubView, result [^1]);
    }

    [Theory]
    [InlineData ("Border")]
    [InlineData ("Padding")]
    public void Returns_OnlyParentsSuperView_Of_AdornmentView_If_TransparentMouse (string adornmentType)
    {
        // Arrange: top -> subView -> subView.[Adornment] -> adornmentSubView
        Runnable top = new () { Id = "top", Frame = new Rectangle (0, 0, 10, 10) };

        View subView = new ()
        {
            Id = "subView",
            X = 0,
            Y = 0,
            Width = 8,
            Height = 8
        };
        top.Add (subView);

        AdornmentImpl? adornment = null;

        switch (adornmentType)
        {
            case "Margin":
                subView.Margin.Thickness = new Thickness (2);
                adornment = subView.Margin;

                break;

            case "Border":
                subView.Border.Thickness = new Thickness (2);
                adornment = subView.Border;

                break;

            case "Padding":
                subView.Padding.Thickness = new Thickness (2);
                adornment = subView.Padding;

                break;
        }

        adornment?.GetOrCreateView ();

        subView.Layout ();

        // Add a child to the adornment
        View adornmentSubView = new ()
        {
            Id = "adornmentSubView",
            X = 0,
            Y = 0,
            Width = 2,
            Height = 2
        };
        adornment?.GetOrCreateView ().Add (adornmentSubView);

        adornment?.ViewportSettings = ViewportSettingsFlags.TransparentMouse;

        // Act: Point inside adornmentSubView (which is inside the adornment)
        List<View?> result = View.GetViewsUnderLocation (top, new Point (0, 0), ViewportSettingsFlags.TransparentMouse);

        // Assert: Should contain top, subView, adornment, and adornmentSubView
        Assert.Contains (top, result);
        Assert.DoesNotContain (adornment?.View, result);
        Assert.Contains (adornmentSubView, result);
        Assert.DoesNotContain (subView, result);
        Assert.Equal (top, result [0]);
        Assert.Equal (adornmentSubView, result [^1]);
    }

    [Theory]
    [InlineData ("Border")]
    [InlineData ("Padding")]
    public void Returns_OnlyParentsSuperView_Of_Adornment_If_TransparentMouse (string adornmentType)
    {
        // Arrange: top -> subView -> subView.[Adornment] -> adornmentSubView
        Runnable top = new () { Id = "top", Frame = new Rectangle (0, 0, 10, 10) };

        View subView = new ()
        {
            Id = "subView",
            X = 0,
            Y = 0,
            Width = 8,
            Height = 8
        };
        top.Add (subView);

        AdornmentImpl? adornment = null;

        switch (adornmentType)
        {
            case "Margin":
                subView.Margin.Thickness = new Thickness (2);
                adornment = subView.Margin;

                break;

            case "Border":
                subView.Border.Thickness = new Thickness (2);
                adornment = subView.Border;

                break;

            case "Padding":
                subView.Padding.Thickness = new Thickness (2);
                adornment = subView.Padding;

                break;
        }

        subView.Layout ();

        // Add a child to the adornment
        View adornmentSubView = new ()
        {
            Id = "adornmentSubView",
            X = 0,
            Y = 0,
            Width = 2,
            Height = 2
        };
        adornment?.GetOrCreateView ().Add (adornmentSubView);

        adornment?.ViewportSettings = ViewportSettingsFlags.TransparentMouse;

        // Act: Point inside adornmentSubView (which is inside the adornment)
        List<View?> result = View.GetViewsUnderLocation (top, new Point (0, 0), ViewportSettingsFlags.TransparentMouse);

        // Assert: Should contain top, subView, adornment, and adornmentSubView
        Assert.Contains (top, result);
        Assert.DoesNotContain (adornment?.View, result);
        Assert.Contains (adornmentSubView, result);
        Assert.DoesNotContain (subView, result);
        Assert.Equal (top, result [0]);
        Assert.Equal (adornmentSubView, result [^1]);

        Assert.NotNull (adornment?.View);
    }
}

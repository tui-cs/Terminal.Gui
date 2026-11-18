using UnitTests;

namespace ViewBaseTests.Adornments;

public class AdornmentTests (ITestOutputHelper output) : TestDriverBase
{
    // Test that Adornment.Viewport_get override returns Frame.Size minus Thickness
    [Theory]
    [InlineData (0, 0, 0, 0, 0)]
    [InlineData (0, 0, 0, 1, 1)]
    [InlineData (0, 0, 0, 1, 0)]
    [InlineData (0, 0, 0, 0, 1)]
    [InlineData (1, 0, 0, 0, 0)]
    [InlineData (1, 0, 0, 1, 1)]
    [InlineData (1, 0, 0, 1, 0)]
    [InlineData (1, 0, 0, 0, 1)]
    [InlineData (1, 0, 0, 4, 4)]
    [InlineData (1, 0, 0, 4, 0)]
    [InlineData (1, 0, 0, 0, 4)]
    [InlineData (0, 1, 0, 0, 0)]
    [InlineData (0, 1, 0, 1, 1)]
    [InlineData (0, 1, 0, 1, 0)]
    [InlineData (0, 1, 0, 0, 1)]
    [InlineData (1, 1, 0, 0, 0)]
    [InlineData (1, 1, 0, 1, 1)]
    [InlineData (1, 1, 0, 1, 0)]
    [InlineData (1, 1, 0, 0, 1)]
    [InlineData (1, 1, 0, 4, 4)]
    [InlineData (1, 1, 0, 4, 0)]
    [InlineData (1, 1, 0, 0, 4)]
    [InlineData (0, 1, 1, 0, 0)]
    [InlineData (0, 1, 1, 1, 1)]
    [InlineData (0, 1, 1, 1, 0)]
    [InlineData (0, 1, 1, 0, 1)]
    [InlineData (1, 1, 1, 0, 0)]
    [InlineData (1, 1, 1, 1, 1)]
    [InlineData (1, 1, 1, 1, 0)]
    [InlineData (1, 1, 1, 0, 1)]
    [InlineData (1, 1, 1, 4, 4)]
    [InlineData (1, 1, 1, 4, 0)]
    [InlineData (1, 1, 1, 0, 4)]
    public void Viewport_Width_Is_Frame_Width (int thickness, int x, int y, int w, int h)
    {
        AdornmentView adornment = new (new AdornmentTests.TestAdornment ());
        adornment.Adornment!.Thickness = new Thickness (thickness);
        adornment.Frame = new Rectangle (x, y, w, h);
        Assert.Equal (new Rectangle (x, y, w, h), adornment.Frame);

        var expectedBounds = new Rectangle (0, 0, w, h);
        Assert.Equal (expectedBounds, adornment.Viewport);
    }

    // Test that Adornment.Viewport_get override uses Parent not SuperView
    [Fact]
    public void ViewportToScreen_Uses_Parent_Not_SuperView ()
    {
        var parent = new View { X = 1, Y = 2, Width = 10, Height = 10 };
        parent.Margin.GetOrCreateView ();

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Viewport);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.GetFrame ());
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.View?.Viewport);

        Assert.Null (parent.Margin.View?.SuperView);
        Rectangle boundsAsScreen = parent.Margin.View!.ViewportToScreen (new Rectangle (1, 2, 5, 5));
        Assert.Equal (new Rectangle (2, 4, 5, 5), boundsAsScreen);
    }

    [Fact]
    public void Frames_are_Parent_SuperView_Relative ()
    {
        var view = new View { X = 1, Y = 2, Width = 20, Height = 31 };

        var marginThickness = 1;
        view.Margin.Thickness = new Thickness (marginThickness);

        var borderThickness = 2;
        view.Border.Thickness = new Thickness (borderThickness);

        var paddingThickness = 3;
        view.Padding.Thickness = new Thickness (paddingThickness);

        view.BeginInit ();
        view.EndInit ();

        Assert.Equal (new Rectangle (1, 2, 20, 31), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 8, 19), view.Viewport);

        // Margin.Frame is always at (0,0) and the same as the view frame
        Assert.Equal (new Rectangle (0, 0, 20, 31), view.Margin.GetFrame ());

        // Border.Frame is View.Frame minus the Margin thickness 
        Assert.Equal (new Rectangle (marginThickness, marginThickness, view.Frame.Width - marginThickness * 2, view.Frame.Height - marginThickness * 2),
                      view.Border.GetFrame ());

        // Padding.Frame is View.Frame minus the Border thickness plus Margin thickness
        Assert.Equal (new Rectangle (marginThickness + borderThickness,
                                     marginThickness + borderThickness,
                                     view.Frame.Width - (marginThickness + borderThickness) * 2,
                                     view.Frame.Height - (marginThickness + borderThickness) * 2),
                      view.Padding.GetFrame ());
    }

    // Test that Adornment.FrameToScreen override retains Frame.Size
    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (0, 1, 1)]
    [InlineData (0, 10, 10)]
    [InlineData (1, 0, 0)]
    [InlineData (1, 1, 1)]
    [InlineData (1, 10, 10)]
    public void FrameToScreen_Retains_Frame_Size (int marginThickness, int w, int h)
    {
        var parent = new View { X = 1, Y = 2, Width = w, Height = h };
        parent.Margin.Thickness = new Thickness (marginThickness);

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (1, 2, w, h), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, w, h), parent.Margin.GetFrame ());

        Assert.Equal (parent.Frame, parent.Margin.FrameToScreen ());
    }

    // Test that Adornment.FrameToScreen override returns Frame if Parent is null
    [Fact]
    public void FrameToScreen_Returns_Frame_If_Parent_Is_Null ()
    {
        var a = new AdornmentView { X = 1, Y = 2, Width = 3, Height = 4 };

        Assert.Null (a.Adornment?.Parent);
        Assert.Equal (a.Frame, a.FrameToScreen ());
    }

    // Test that Adornment.FrameToScreen override returns correct location
    [Theory]
    [InlineData (0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1)]
    [InlineData (0, 0, 10, 10)]
    [InlineData (1, 0, 0, 0)]
    [InlineData (1, 0, 1, 1)]
    [InlineData (1, 0, 10, 10)]
    [InlineData (0, 1, 0, 0)]
    [InlineData (0, 1, 1, 1)]
    [InlineData (0, 1, 10, 10)]
    [InlineData (1, 1, 0, 0)]
    [InlineData (1, 1, 1, 1)]
    [InlineData (1, 1, 10, 10)]
    public void FrameToScreen_Returns_Screen_Location (int marginThickness, int borderThickness, int x, int y)
    {
        var superView = new View { X = 1, Y = 1, Width = 20, Height = 20 };
        superView.Margin.Thickness = new Thickness (marginThickness);
        superView.Border.Thickness = new Thickness (borderThickness);

        var view = new View { X = x, Y = y, Width = 1, Height = 1 };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (new Rectangle (x, y, 1, 1), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 20, 20), superView.Margin.GetFrame ());

        Assert.Equal (new Rectangle (marginThickness, marginThickness, 20 - marginThickness * 2, 20 - marginThickness * 2), superView.Border.GetFrame ());

        Assert.Equal (new Rectangle (superView.Frame.X + marginThickness,
                                     superView.Frame.Y + marginThickness,
                                     20 - marginThickness * 2,
                                     20 - marginThickness * 2),
                      superView.Border.FrameToScreen ());
    }

    // Test that Adornment.FrameToScreen override uses Parent not SuperView
    [Fact]
    public void FrameToScreen_Uses_Parent_Not_SuperView ()
    {
        var parent = new View { X = 1, Y = 2, Width = 10, Height = 10 };
        parent.Margin.GetOrCreateView ();

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Viewport);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.GetFrame ());
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.View?.Viewport);

        Assert.Null (parent.Margin.View?.SuperView);
        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Margin.FrameToScreen ());
    }

    [Fact]
    public void GetAdornmentsThickness ()
    {
        var view = new View ();
        Assert.Equal (Thickness.Empty, view.GetAdornmentsThickness ());

        view.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Thickness (1), view.GetAdornmentsThickness ());

        view.Border.Thickness = new Thickness (1);
        Assert.Equal (new Thickness (2), view.GetAdornmentsThickness ());

        view.Padding.Thickness = new Thickness (1);
        Assert.Equal (new Thickness (3), view.GetAdornmentsThickness ());

        view.Padding.Thickness = new Thickness (2);
        Assert.Equal (new Thickness (4), view.GetAdornmentsThickness ());

        view.Padding.Thickness = new Thickness (1, 2, 3, 4);
        Assert.Equal (new Thickness (3, 4, 5, 6), view.GetAdornmentsThickness ());

        view.Margin.Thickness = new Thickness (1, 2, 3, 4);
        Assert.Equal (new Thickness (3, 5, 7, 9), view.GetAdornmentsThickness ());
        view.Dispose ();
    }

    [Fact]
    public void Setting_Viewport_Throws ()
    {
        var adornment = new AdornmentView ();
        Assert.Throws<InvalidOperationException> (() => adornment.Viewport = new Rectangle (1, 2, 3, 4));
    }

    [Fact]
    public void Setting_SuperViewRendersLineCanvas_Throws ()
    {
        var adornment = new AdornmentView ();
        Assert.Throws<InvalidOperationException> (() => adornment.SuperViewRendersLineCanvas = true);
    }

    [Fact]
    public void Setting_Thickness_Changes_Parent_Bounds ()
    {
        var parent = new View { Width = 10, Height = 10 };
        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Viewport);

        parent.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 8, 8), parent.Viewport);
    }

    [Fact]
    public void Setting_Thickness_Raises_ThicknessChanged ()
    {
        View view = new ();
        var raised = false;

        view.Margin.ThicknessChanged += (s, e) =>
                                        {
                                            raised = true;
                                            Assert.Equal (new Thickness (1, 2, 3, 4), view.Margin.Thickness);
                                        };
        view.Margin.Thickness = new Thickness (1, 2, 3, 4);
        Assert.True (raised);
    }

    [Fact]
    public void Setting_Thickness_Causes_Parent_Layout ()
    {
        var parent = new View ();
        var raised = false;
        parent.BeginInit ();
        parent.EndInit ();

        parent.SubViewLayout += LayoutStarted;
        parent.Margin.Thickness = new Thickness (1, 2, 3, 4);
        Assert.True (parent.NeedsLayout);
        parent.Layout ();
        Assert.True (raised);

        return;

        void LayoutStarted (object? sender, LayoutEventArgs e) => raised = true;
    }

    [Fact]
    public void Setting_Thickness_Causes_Adornment_Layout ()
    {
        var parent = new View ();
        parent.Margin.GetOrCreateView ();
        var raised = false;
        parent.BeginInit ();
        parent.EndInit ();

        parent.Margin.View?.SubViewLayout += LayoutStarted;
        parent.Margin.Thickness = new Thickness (1, 2, 3, 4);
        Assert.True (parent.NeedsLayout);
        Assert.True (parent.Margin.View?.NeedsLayout == true);
        parent.Layout ();
        Assert.True (raised);

        return;

        void LayoutStarted (object? sender, LayoutEventArgs e) => raised = true;
    }

    [Fact]
    public void Set_Viewport_Throws ()
    {
        View view = new ();

        view.BeginInit ();
        view.EndInit ();
        view.Padding.Thickness = new Thickness (2, 2, 2, 2);
        Assert.Throws<NullReferenceException> (() => view.Padding.View!.Viewport = view.Padding.View!.Viewport with { Location = new Point (1, 1) });
    }

    // Contains tests

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0, false)]
    [InlineData (0, 0, 0, 1, 0, 0, false)]
    [InlineData (0, 0, 1, 0, 0, 0, false)]
    [InlineData (0, 0, 1, 1, 0, 0, true)]
    [InlineData (0, 0, 1, 2, 0, 0, true)]
    [InlineData (1, 1, 0, 0, 0, 0, false)]
    [InlineData (1, 1, 0, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 1, false)]
    [InlineData (1, 1, 1, 1, 1, 0, false)]
    [InlineData (1, 1, 1, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 1, 1, 1, true)]
    [InlineData (1, 1, 1, 2, 1, 1, true)]
    public void Contains_Left_Only (int x, int y, int width, int height, int pointX, int pointY, bool expected)
    {
        AdornmentView adornment = new (new Border ()) { Id = "adornment" };
        adornment.Adornment?.Parent = new View { Id = "parent" };
        adornment.Adornment?.Parent?.Frame = new Rectangle (x, y, width, height);
        adornment.Adornment!.Thickness = new Thickness (1, 0, 0, 0);
        adornment.Frame = adornment.Adornment!.Parent!.Frame with { Location = Point.Empty };

        bool result = adornment.Contains (new Point (pointX, pointY));
        Assert.Equal (expected, result);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0, false)]
    [InlineData (0, 0, 0, 1, 0, 0, false)]
    [InlineData (0, 0, 1, 0, 0, 0, false)]
    [InlineData (0, 0, 1, 1, 0, 0, true)]
    [InlineData (0, 0, 1, 2, 0, 0, true)]
    [InlineData (1, 1, 0, 0, 0, 0, false)]
    [InlineData (1, 1, 0, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 1, false)]
    [InlineData (1, 1, 1, 1, 1, 0, false)]
    [InlineData (1, 1, 1, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 1, 1, 1, true)]
    [InlineData (1, 1, 1, 2, 1, 1, true)]
    public void Contains_Right_Only (int x, int y, int width, int height, int pointX, int pointY, bool expected)
    {
        AdornmentView adornment = new (new Border ()) { Id = "adornment" };
        adornment.Adornment!.Parent = new View { Id = "parent" };
        adornment.Adornment.Parent.Frame = new Rectangle (x, y, width, height);
        adornment.Adornment!.Thickness = new Thickness (0, 0, 1, 0);
        adornment.Frame = adornment.Adornment.Parent.Frame with { Location = Point.Empty };

        bool result = adornment.Contains (new Point (pointX, pointY));
        Assert.Equal (expected, result);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0, false)]
    [InlineData (0, 0, 0, 1, 0, 0, false)]
    [InlineData (0, 0, 1, 0, 0, 0, false)]
    [InlineData (0, 0, 1, 1, 0, 0, true)]
    [InlineData (0, 0, 1, 2, 0, 0, true)]
    [InlineData (1, 1, 0, 0, 0, 0, false)]
    [InlineData (1, 1, 0, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 1, false)]
    [InlineData (1, 1, 1, 1, 1, 0, false)]
    [InlineData (1, 1, 1, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 1, 1, 1, true)]
    [InlineData (1, 1, 1, 2, 1, 1, true)]
    public void Contains_Top_Only (int x, int y, int width, int height, int pointX, int pointY, bool expected)
    {
        AdornmentView adornment = new (new Border ()) { Id = "adornment" };
        adornment.Adornment!.Parent = new View { Id = "parent" };
        adornment.Adornment.Parent.Frame = new Rectangle (x, y, width, height);
        adornment.Adornment!.Thickness = new Thickness (0, 1, 0, 0);
        adornment.Frame = adornment.Adornment.Parent.Frame with { Location = Point.Empty };

        bool result = adornment.Contains (new Point (pointX, pointY));
        Assert.Equal (expected, result);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0, false)]
    [InlineData (0, 0, 0, 1, 0, 0, false)]
    [InlineData (0, 0, 1, 0, 0, 0, false)]
    [InlineData (0, 0, 1, 1, 0, 0, true)]
    [InlineData (0, 0, 1, 2, 0, 0, true)]
    [InlineData (1, 1, 0, 0, 0, 0, false)]
    [InlineData (1, 1, 0, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 1, false)]
    [InlineData (1, 1, 1, 1, 1, 0, false)]
    [InlineData (1, 1, 1, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 1, 1, 1, true)]
    [InlineData (1, 1, 1, 2, 1, 1, true)]
    public void Contains_TopLeft_Only (int x, int y, int width, int height, int pointX, int pointY, bool expected)
    {
        TestAdornment adornment = new () { Parent = new View { Id = "parent" } };
        adornment.Parent.Frame = new Rectangle (x, y, width, height);
        adornment.Thickness = new Thickness (1, 1, 0, 0);

        bool result = adornment.Contains (new Point (pointX, pointY));
        Assert.Equal (expected, result);
    }

    [Fact]
    public void Border_Is_Cleared_After_Margin_Thickness_Change ()
    {
        IDriver driver = CreateTestDriver ();

        View view = new ()
        {
            Driver = driver,
            Text = "View",
            Width = 6,
            Height = 3,
            BorderStyle = LineStyle.Rounded
        };

        // Remove border bottom thickness
        view.Border.Thickness = new Thickness (1, 1, 1, 0);

        // Add margin bottom thickness
        view.Margin.Thickness = new Thickness (0, 0, 0, 1);

        Assert.Equal (6, view.Width);
        Assert.Equal (3, view.Height);

        view.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre ("""
                                                       ╭────╮
                                                       │View│
                                                       """,
                                                       output,
                                                       driver);

        // Add border bottom thickness
        view.Border.Thickness = new Thickness (1, 1, 1, 1);

        // Remove margin bottom thickness
        view.Margin.Thickness = new Thickness (0, 0, 0, 0);

        view.Draw ();

        Assert.Equal (6, view.Width);
        Assert.Equal (3, view.Height);

        DriverAssert.AssertDriverContentsWithFrameAre ("""
                                                       ╭────╮
                                                       │View│
                                                       ╰────╯
                                                       """,
                                                       output,
                                                       driver);

        // Remove border bottom thickness
        view.Border.Thickness = new Thickness (1, 1, 1, 0);

        // Add margin bottom thickness
        view.Margin.Thickness = new Thickness (0, 0, 0, 1);

        Assert.Equal (6, view.Width);
        Assert.Equal (3, view.Height);

        // Because view has no SuperView, and because there's no LayoutAndDraw loop
        // and because the Margin is transparent, the bottom border drawn above
        // will persist if we don't explicitly ClearContents
        driver.ClearContents ();
        view.Layout ();
        view.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre ("""
                                                       ╭────╮
                                                       │View│
                                                       """,
                                                       output,
                                                       driver);
    }

    public class TestAdornment : AdornmentImpl
    {
        /// <inheritdoc/>
        public override Rectangle GetFrame ()
        {
            if (Parent is { })
            {
                return Parent.Margin.Thickness.GetInside (Parent!.Margin.GetFrame ());
            }

            return Rectangle.Empty;
        }

        /// <inheritdoc/>
        protected override AdornmentView CreateView () => new ();
    }
}

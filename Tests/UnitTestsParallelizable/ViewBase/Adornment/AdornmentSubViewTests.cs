using UnitTests;

namespace ViewBaseTests.Adornments;

public class AdornmentSubViewTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Setting_Thickness_Causes_Adornment_SubView_Layout ()
    {
        var view = new View ();
        var subView = new View ();
        view.Padding.GetOrCreateView ().Add (subView);
        view.BeginInit ();
        view.EndInit ();
        var raised = false;

        subView.SubViewLayout += LayoutStarted;
        view.Padding.Thickness = new Thickness (1, 2, 3, 4);
        view.Layout ();
        Assert.True (raised);

        return;

        void LayoutStarted (object? sender, LayoutEventArgs e) => raised = true;
    }

    [Theory]
    [InlineData (0, 0, false)] // Padding has no thickness, so false
    [InlineData (0, 1, false)] // Padding has no thickness, so false
    [InlineData (1, 0, true)]
    [InlineData (1, 1, true)]
    [InlineData (2, 1, true)]
    public void Adornment_WithSubView_Finds (int viewPadding, int subViewPadding, bool expectedFound)
    {
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        runnable.Padding.Thickness = new Thickness (viewPadding);

        // Turn of TransparentMouse for the test
        runnable.Padding.ViewportSettings = ViewportSettingsFlags.None;

        var subView = new View { X = 0, Y = 0, Width = 5, Height = 5 };
        subView.Padding.Thickness = new Thickness (subViewPadding);

        // Turn of TransparentMouse for the test
        subView.Padding.ViewportSettings = ViewportSettingsFlags.None;

        runnable.Padding.GetOrCreateView ().Add (subView);
        runnable.Layout ();

        View? foundView = runnable.GetViewsUnderLocation (new Point (0, 0), ViewportSettingsFlags.None).LastOrDefault ();

        bool found = foundView == subView || foundView == subView.Padding.View!;
        Assert.Equal (expectedFound, found);
    }

    [Fact]
    public void Adornment_WithNonVisibleSubView_Finds_Adornment ()
    {
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);
        runnable.Padding.Thickness = new Thickness (1);

        var subView = new View
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1,
            Visible = false
        };
        runnable.Padding.GetOrCreateView ().Add (subView);
        runnable.Layout ();

        Assert.Equal (runnable.Padding.View!, runnable.GetViewsUnderLocation (new Point (0, 0), ViewportSettingsFlags.None).LastOrDefault ());
    }

    [Fact]
    public void Button_With_Opaque_ShadowStyle_In_Border_Should_Draw_Shadow ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (1, 4);
        app.Driver!.Force16Colors = true;

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();
        window.Text = @"XXXXXX";
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        // Setup padding with some thickness so we have space for the button
        window.Border.Thickness = new Thickness (0, 3, 0, 0);

        // Add a button with a transparent shadow to the Padding adornment
        Button buttonInBorder = new ()
        {
            X = 0,
            Y = 0,
            Text = "B",
            NoDecorations = true,
            NoPadding = true,
            ShadowStyle = ShadowStyles.Opaque
        };

        window.Border.GetOrCreateView ().Add (buttonInBorder);
        app.Begin (window);

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[30m\x1b[107mB▝ \x1b[97m\x1b[40mX
                                           """,
                                           _output,
                                           app.Driver);
    }

    [Fact]
    public void Button_With_Opaque_ShadowStyle_In_Padding_Should_Draw_Shadow ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (1, 4);
        app.Driver!.Force16Colors = true;

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();
        window.Text = @"XXXXXX";
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        // Setup padding with some thickness so we have space for the button
        window.Padding.Thickness = new Thickness (0, 3, 0, 0);

        // Add a button with a transparent shadow to the Padding adornment
        Button buttonInPadding = new ()
        {
            X = 0,
            Y = 0,
            Text = "B",
            NoDecorations = true,
            NoPadding = true,
            ShadowStyle = ShadowStyles.Opaque
        };

        window.Padding.GetOrCreateView ().Add (buttonInPadding);
        app.Begin (window);

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[97m\x1b[40mB\x1b[30m\x1b[107m▝ \x1b[97m\x1b[40mX
                                           """,
                                           _output,
                                           app.Driver);
    }
}

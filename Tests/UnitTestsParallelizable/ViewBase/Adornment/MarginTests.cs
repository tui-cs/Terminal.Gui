using UnitTests;

namespace ViewBaseTests.Adornments;

public class MarginTests (ITestOutputHelper output)
{
    [Fact]
    public void Constructor_Defaults ()
    {
        Margin margin = new ();
        Assert.Null (margin.View);
        Assert.Null (margin.ShadowStyle);
    }

    [Fact]
    public void View_Constructor_Defaults ()
    {
        View view = new () { Height = 3, Width = 3 };
        Assert.Null (view.Margin.View);

        view.Margin.GetOrCreateView ();
        Assert.False (view.Margin.View?.CanFocus);
        Assert.Equal (TabBehavior.NoStop, view.Margin.View?.TabStop);
        Assert.Empty (view.Margin.View?.KeyBindings.GetBindings ()!);
    }

    [Fact]
    public void Margin_Is_Transparent ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (5, 5);

        var view = new View { Height = 3, Width = 3 };
        view.Margin.Diagnostics = ViewDiagnosticFlags.Thickness;
        view.Margin.Thickness = new Thickness (1);
        view.Margin.GetOrCreateView ();

        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        runnable.SetScheme (new Scheme { Normal = new Attribute (Color.Red, Color.Green), Focus = new Attribute (Color.Green, Color.Red) });

        runnable.Add (view);
        Assert.Equal (ColorName16.Red, view.Margin.View!.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (ColorName16.Red, runnable.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());

        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (@"", output, app.Driver);
        DriverAssert.AssertDriverAttributesAre ("0", output, app.Driver, runnable.GetAttributeForRole (VisualRole.Normal));
    }

    [Fact]
    public void Margin_ViewPortSettings_Not_Transparent_Is_NotTransparent ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (5, 5);

        View view = new () { Height = 3, Width = 3 };
        view.Margin.GetOrCreateView ();
        view.Margin.View?.Diagnostics = ViewDiagnosticFlags.Thickness;
        view.Margin.Thickness = new Thickness (1);
        view.Margin.View?.ViewportSettings = ViewportSettingsFlags.None;

        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        runnable.SetScheme (new Scheme { Normal = new Attribute (Color.Red, Color.Green), Focus = new Attribute (Color.Green, Color.Red) });

        runnable.Add (view);
        Assert.Equal (ColorName16.Red, view.Margin.View!.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (ColorName16.Red, runnable.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());

        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre ("""

                                              MMM
                                              M M
                                              MMM
                                              """,
                                              output,
                                              app.Driver);
        DriverAssert.AssertDriverAttributesAre ("0", output, app.Driver, runnable.GetAttributeForRole (VisualRole.Normal));
    }

    [Fact]
    public void Is_Visually_Transparent ()
    {
        View view = new () { Height = 3, Width = 3 };
        view.Margin.GetOrCreateView ();
        Assert.True (view.Margin.View?.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent), "Margin should be transparent by default.");
    }

    [Fact]
    public void Is_Transparent_To_Mouse_With_View ()
    {
        View view = new () { Height = 3, Width = 3 };
        view.Margin.GetOrCreateView ();
        Assert.True (view.Margin.View?.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse), "Margin should be transparent to mouse by default.");
    }

    [Fact]
    public void Is_Transparent_To_Mouse ()
    {
        Margin margin = new Margin ();
        Assert.True (margin.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse), "Margin should be transparent to mouse by default.");
    }

    [Fact]
    public void When_Not_Visually_Transparent ()
    {
        var view = new View { Height = 3, Width = 3 };

        // Give the Margin some size
        view.Margin.Thickness = new Thickness (1, 1, 1, 1);
        view.Margin.GetOrCreateView ();

        // Give it Text
        view.Margin.View?.Text = "Test";

        // Strip off ViewportSettings.Transparent
        view.Margin.View?.ViewportSettings &= ~ViewportSettingsFlags.Transparent;

        // 
    }

    [Fact]
    public void Thickness_Is_Empty_By_Default ()
    {
        var view = new View { Height = 3, Width = 3 };
        Assert.Equal (Thickness.Empty, view.Margin.Thickness);
    }

    [Fact]
    public void Thickness_Set_Does_Not_GetOrCreateView ()
    {
        View view = new () { Height = 3, Width = 3 };
        Assert.Null (view.Margin.View);

        view.Margin.Thickness = new Thickness (1);
        Assert.Null (view.Margin.View);
    }

    // ShadowStyle
    [Fact]
    public void Margin_Uses_ShadowStyle_Transparent ()
    {
        var view = new View { Height = 3, Width = 3, ShadowStyle = ShadowStyles.Transparent };
        Assert.Equal (ShadowStyles.Transparent, view.Margin.ShadowStyle);

        Assert.True (view.Margin.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse),
                     "Margin should be transparent to mouse when ShadowStyle is Transparent.");

        Assert.True (view.Margin.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent),
                     "Margin should be transparent when ShadowStyle is Transparent..");
    }

    [Fact]
    public void Margin_Uses_ShadowStyle_Opaque ()
    {
        var view = new View { Height = 3, Width = 3, ShadowStyle = ShadowStyles.Opaque };
        Assert.Equal (ShadowStyles.Opaque, view.Margin.ShadowStyle);

        Assert.True (view.Margin.View?.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse),
                     "Margin should be transparent to mouse when ShadowStyle is Opaque.");
        Assert.True (view.Margin.View?.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent), "Margin should be transparent when ShadowStyle is Opaque..");
    }

    [Fact]
    public void Margin_Layouts_Correctly ()
    {
        View superview = new () { Width = 10, Height = 5 };
        View view = new () { Width = 3, Height = 1, BorderStyle = LineStyle.Single };
        view.Margin.Thickness = new Thickness (1);
        View view2 = new () { X = Pos.Right (view), Width = 3, Height = 1, BorderStyle = LineStyle.Single };
        view2.Margin.Thickness = new Thickness (1);
        View view3 = new () { Y = Pos.Bottom (view), Width = 3, Height = 1, BorderStyle = LineStyle.Single };
        view3.Margin.Thickness = new Thickness (1);
        superview.Add (view, view2, view3);

        superview.LayoutSubViews ();

        Assert.Equal (new Rectangle (0, 0, 10, 5), superview.Frame);
        Assert.Equal (new Rectangle (0, 0, 3, 1), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);
        Assert.Equal (new Rectangle (3, 0, 3, 1), view2.Frame);
        Assert.Equal (Rectangle.Empty, view2.Viewport);
        Assert.Equal (new Rectangle (0, 1, 3, 1), view3.Frame);
        Assert.Equal (Rectangle.Empty, view3.Viewport);
    }

    [Fact]
    public void Margin_GetFrame_Without_View_Is_Parent_With_Empty_Location ()
    {
        Margin margin = new ();

        Assert.Equal (Rectangle.Empty, margin.GetFrame ());

        margin.Parent = new View ()
        {
            Frame = new Rectangle (1, 2, 3, 4)
        };

        Assert.Equal (new Rectangle (0, 0, 3, 4), margin.GetFrame ());

        margin.Parent.Frame = new Rectangle (10, 20, 30, 40);
        Assert.Equal (new Rectangle (0, 0, 30, 40), margin.GetFrame ());
    }


    [Fact]
    public void Margin_GetFrame_With_View_Tracks_View_Frame ()
    {
        Margin margin = new ();

        Assert.Equal (Rectangle.Empty, margin.GetFrame ());

        margin.View = new MarginView () { Id = "margin.View" };

        View parent = new ()
        {
            Id = "margin.Parent",
            Frame = new Rectangle (1, 2, 3, 4)
        };

        margin.Parent = parent;
        Assert.Equal (margin.View.Frame with { Location = Point.Empty }, margin.GetFrame ());

        margin.Parent.Frame = new Rectangle (10, 20, 30, 40);
        Assert.Equal (margin.View.Frame with { Location = Point.Empty }, margin.GetFrame ());
    }

    [Fact]
    public void Margin_GetFrame_With_View_Is_Parent_With_Empty_Location ()
    {
        Margin margin = new ();

        Assert.Equal (Rectangle.Empty, margin.GetFrame ());

        margin.View = new MarginView ();

        margin.Parent = new View ()
        {
            Frame = new Rectangle (1, 2, 3, 4)
        };

        Assert.Equal (new Rectangle (0, 0, 3, 4), margin.GetFrame ());

        margin.Parent.Frame = new Rectangle (10, 20, 30, 40);
        Assert.Equal (new Rectangle (0, 0, 30, 40), margin.GetFrame ());
    }

    [Fact]
    public void View_Viewport_Location_Always_Empty_Size_Correct ()
    {
        var view = new View { Frame = new Rectangle (1, 2, 20, 20) };

        Assert.Equal (new Rectangle (1, 2, 20, 20), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 20, 20), view.Viewport);
        view.Margin.GetOrCreateView ();
        Assert.Equal (new Rectangle (0, 0, 20, 20), view.Margin.View?.Viewport);

        view.Margin.Thickness = new Thickness (1);

        Assert.Equal (new Rectangle (0, 0, 18, 18), view.Viewport);

        Assert.Equal (new Rectangle (0, 0, 20, 20), view.Margin.View?.Viewport);
    }
}

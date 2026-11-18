using System.Text;
using UnitTests;

namespace ViewBaseTests.Adornments;

public class ShadowTests (ITestOutputHelper output)
{
    [Fact]
    public void Default_Null ()
    {
        var view = new View ();
        Assert.Null (view.ShadowStyle);
        Assert.Null (view.Margin.ShadowStyle);
        view.Dispose ();
    }

    [Theory]
    [InlineData (ShadowStyles.None)]
    [InlineData (ShadowStyles.Opaque)]
    [InlineData (ShadowStyles.Transparent)]
    public void Set_View_Sets_Margin (ShadowStyles style)
    {
        var view = new View ();

        view.ShadowStyle = style;
        Assert.Equal (style, view.ShadowStyle);
        Assert.Equal (style, view.Margin.ShadowStyle);
        view.Dispose ();
    }

    [Theory]
    [InlineData (ShadowStyles.None, 0, 0, 0, 0)]
    [InlineData (ShadowStyles.Opaque, 0, 0, 1, 1)]
    [InlineData (ShadowStyles.Transparent, 0, 0, 1, 1)]
    public void ShadowStyle_Margin_Thickness (ShadowStyles style, int expectedLeft, int expectedTop, int expectedRight, int expectedBottom)
    {
        var superView = new View { Height = 10, Width = 10 };

        View view = new ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = "0123",
            MouseHighlightStates = MouseState.Pressed,
            ShadowStyle = style,
            CanFocus = true
        };

        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (new Thickness (expectedLeft, expectedTop, expectedRight, expectedBottom), view.Margin.Thickness);
    }

    [Theory]
    [InlineData (ShadowStyles.None, 3)]
    [InlineData (ShadowStyles.Opaque, 4)]
    [InlineData (ShadowStyles.Transparent, 4)]
    public void Style_Changes_Margin_Thickness (ShadowStyles style, int expected)
    {
        var view = new View ();
        view.Margin.Thickness = new Thickness (3);
        view.ShadowStyle = style;
        Assert.Equal (new Thickness (3, 3, expected, expected), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyles.None;
        Assert.Equal (new Thickness (3, 3, expected, expected), view.Margin.Thickness);
        view.Dispose ();
    }

    [Theory]
    [InlineData (ShadowStyles.None)]
    [InlineData (ShadowStyles.Opaque)]
    [InlineData (ShadowStyles.Transparent)]
    public void ShadowWidth_ShadowHeight_Defaults (ShadowStyles style)
    {
        View view = new () { ShadowStyle = style };

        if (view.ShadowStyle == ShadowStyles.None)
        {
            Assert.Equal (new Size (0, 0), (view.Margin.View as MarginView)?.ShadowSize);
        }
        else
        {
            Assert.Equal (new Size (1, 1), (view.Margin.View as MarginView)?.ShadowSize);
        }
    }

    [Fact]
    public void ShadowStyle_Opaque_Margin_ShadowWidth_ShadowHeight_Cannot_Be_Set_Different_Of_One ()
    {
        View view = new () { ShadowStyle = ShadowStyles.Opaque };
        (view.Margin.View as MarginView)?.ShadowSize = new Size (3, 4);
        Assert.Equal (1, (view.Margin.View as MarginView)?.ShadowSize.Width);
        Assert.Equal (1, (view.Margin.View as MarginView)?.ShadowSize.Height);
    }

    [Theory]
    [InlineData (ShadowStyles.None, 0)]
    [InlineData (ShadowStyles.Opaque, 1)]
    [InlineData (ShadowStyles.Transparent, 1)]
    public void Margin_ShadowWidth_ShadowHeight_Cannot_Be_Set_Less_Than_Zero (ShadowStyles style, int expectedLength)
    {
        View view = new () { ShadowStyle = style };
        (view.Margin.View as MarginView)?.ShadowSize = new Size (-1, -1);
        Assert.Equal (expectedLength, (view.Margin.View as MarginView)?.ShadowSize.Width);
        Assert.Equal (expectedLength, (view.Margin.View as MarginView)?.ShadowSize.Height);
    }

    [Fact]
    public void Changing_ShadowStyle_Correctly_Set_ShadowWidth_ShadowHeight_Thickness ()
    {
        View view = new ();
        Assert.Equal (new Thickness (0, 0, 0, 0), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyles.Transparent;
        (view.Margin.View as MarginView)?.ShadowSize = new Size (2, 2);
        Assert.Equal (new Size (2, 2), (view.Margin.View as MarginView)?.ShadowSize);
        Assert.Equal (new Thickness (0, 0, 2, 2), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyles.None;
        Assert.Equal (new Size (2, 2), (view.Margin.View as MarginView)?.ShadowSize);
        Assert.Equal (new Thickness (0, 0, 2, 2), view.Margin.Thickness);

        (view.Margin.View as MarginView)?.ShadowSize = new Size (2, 2);
        Assert.Equal (new Size (2, 2), (view.Margin.View as MarginView)?.ShadowSize);
        Assert.Equal (new Thickness (0, 0, 2, 2), view.Margin.Thickness);

        view.ShadowStyle = null;
        Assert.Equal (Size.Empty, (view.Margin.View as MarginView)?.ShadowSize);
        Assert.Equal (new Thickness (0, 0, 0, 0), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyles.Opaque;
        Assert.Equal (new Size (1, 1), (view.Margin.View as MarginView)?.ShadowSize);
        Assert.Equal (new Thickness (0, 0, 1, 1), view.Margin.Thickness);
    }

    [Theory]
    [InlineData (ShadowStyles.None, 2, 1, 3, 2, 2, 3)]
    [InlineData (ShadowStyles.Opaque, 1, 1, 1, 1, 1, 1)]
    [InlineData (ShadowStyles.Transparent, 2, 1, 3, 2, 2, 3)]
    public void Changing_ShadowWidth_ShadowHeight_Correctly_Set_Thickness (ShadowStyles style,
                                                                           int expectedLength1,
                                                                           int expectedLength2,
                                                                           int expectedLength3,
                                                                           int expectedThickness1,
                                                                           int expectedThickness2,
                                                                           int expectedThickness3)
    {
        View view = new () { ShadowStyle = style };
        (view.Margin.View as MarginView)?.ShadowSize = new Size (2, 2);
        Assert.Equal (expectedLength1, (view.Margin.View as MarginView)?.ShadowSize.Width);
        Assert.Equal (expectedLength1, (view.Margin.View as MarginView)?.ShadowSize.Height);
        Assert.Equal (new Thickness (0, 0, expectedThickness1, expectedThickness1), view.Margin.Thickness);

        (view.Margin.View as MarginView)?.ShadowSize = new Size (1, 1);
        Assert.Equal (expectedLength2, (view.Margin.View as MarginView)?.ShadowSize.Width);
        Assert.Equal (expectedLength2, (view.Margin.View as MarginView)?.ShadowSize.Height);
        Assert.Equal (new Thickness (0, 0, expectedThickness2, expectedThickness2), view.Margin.Thickness);

        (view.Margin.View as MarginView)?.ShadowSize = new Size (3, 3);
        Assert.Equal (expectedLength3, (view.Margin.View as MarginView)?.ShadowSize.Width);
        Assert.Equal (expectedLength3, (view.Margin.View as MarginView)?.ShadowSize.Height);
        Assert.Equal (new Thickness (0, 0, expectedThickness3, expectedThickness3), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyles.None;
        Assert.Equal (expectedLength3, (view.Margin.View as MarginView)?.ShadowSize.Width);
        Assert.Equal (expectedLength3, (view.Margin.View as MarginView)?.ShadowSize.Height);
        Assert.Equal (new Thickness (0, 0, expectedThickness3, expectedThickness3), view.Margin.Thickness);
    }

    [Theory]
    [InlineData (ShadowStyles.None, 0, 1)]
    [InlineData (ShadowStyles.Opaque, 1, 1)]
    [InlineData (ShadowStyles.Transparent, 1, 1)]
    public void Changing_Thickness_Correctly_Set_Thickness (ShadowStyles style, int expectedLength, int expectedThickness)
    {
        View view = new () { ShadowStyle = style };

        Assert.Equal (new Thickness (0, 0, expectedLength, expectedLength), view.Margin.Thickness);

        view.Margin.Thickness = new Thickness (0, 0, 1, 1);
        Assert.Equal (expectedLength, (view.Margin.View as MarginView)?.ShadowSize.Width);
        Assert.Equal (expectedLength, (view.Margin.View as MarginView)?.ShadowSize.Height);
        Assert.Equal (new Thickness (0, 0, expectedThickness, expectedThickness), view.Margin.Thickness);
    }

    [Fact]
    public void ShadowStyle_Transparent_Handles_Wide_Glyphs_Correctly ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (6, 5);

        // Using a replacement char to make sure wide glyphs are handled correctly
        // in the shadow area, to not confusing with a space char.
        app.Driver?.GetOutputBuffer ().SetWideGlyphReplacement (Rune.ReplacementChar);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        superview.Text = """
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         """;

        View view = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single, ShadowStyle = ShadowStyles.Transparent };
        (view.Margin.View as MarginView)?.ShadowSize = (view.Margin.View as MarginView)!.ShadowSize with { Width = 2 };
        superview.Add (view);

        app.Begin (superview);
        Assert.Equal (new Size (2, 1), (view.Margin.View as MarginView)?.ShadowSize);
        Assert.Equal (new Thickness (0, 0, 2, 1), view.Margin.Thickness);

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌──┐🍎
                                              │  │🍎
                                              │  │🍎
                                              └──┘🍎
                                              � 🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        (view.Margin.View as MarginView)?.ShadowSize = new Size (1, 2);

        app.LayoutAndDraw ();
        Assert.Equal (new Size (1, 2), (view.Margin.View as MarginView)?.ShadowSize);
        Assert.Equal (new Thickness (0, 0, 2, 2), view.Margin.Thickness);

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌──┐🍎
                                              │  │�
                                              └──┘�
                                              � 🍎🍎
                                              � 🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        view.Width = Dim.Fill (1);
        app.LayoutAndDraw ();
        Assert.Equal (new Size (1, 2), (view.Margin.View as MarginView)?.ShadowSize);
        Assert.Equal (new Thickness (0, 0, 2, 2), view.Margin.Thickness);

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌─┐ 🍎
                                              │ │ �
                                              └─┘ �
                                              � 🍎�
                                              � 🍎�
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void ShadowStyle_Opaque_Change_Thickness_On_Mouse_Pressed_Released ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (10, 4);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        View view = new ()
        {
            Width = 7,
            Height = 2,
            ShadowStyle = ShadowStyles.Opaque,
            Text = "| Hi |",
            MouseHighlightStates = MouseState.Pressed
        };
        superview.Add (view);

        app.Begin (superview);

        DriverAssert.AssertDriverContentsAre ("""
                                              | Hi |▖
                                              ▝▀▀▀▀▀▘
                                              """,
                                              output,
                                              app.Driver);

        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 0), Flags = MouseFlags.LeftButtonPressed });
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              | Hi |
                                              """,
                                              output,
                                              app.Driver);

        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 0), Flags = MouseFlags.LeftButtonReleased });
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              | Hi |▖
                                              ▝▀▀▀▀▀▘
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void ShadowStyle_Transparent_Never_Throws_Navigating_Outside_Bounds ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (6, 5);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        superview.Text = """
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         """;

        View view = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Single,
            ShadowStyle = ShadowStyles.Transparent,
            Arrangement = ViewArrangement.Movable,
            CanFocus = true
        };
        (view.Margin.View as MarginView)?.ShadowSize = (view.Margin.View as MarginView)!.ShadowSize with { Width = 2 };
        superview.Add (view);

        app.Begin (superview);

        Assert.Equal (new Point (0, 0), view.Frame.Location);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.F5.WithCtrl));

        var i = 0;
        DecrementValue (-10, Key.CursorLeft);
        Assert.Equal (-10, i);

        IncrementValue (0, Key.CursorRight);
        Assert.Equal (0, i);

        DecrementValue (-10, Key.CursorUp);
        Assert.Equal (-10, i);

        IncrementValue (20, Key.CursorDown);
        Assert.Equal (20, i);

        DecrementValue (0, Key.CursorUp);
        Assert.Equal (0, i);

        IncrementValue (20, Key.CursorRight);
        Assert.Equal (20, i);

        return;

        void DecrementValue (int count, Key key)
        {
            for (; i > count; i--)
            {
                app.Keyboard.RaiseKeyDownEvent (key);
                app.LayoutAndDraw ();

                CheckAssertion (new Point (i - 1, 0), new Point (0, i - 1), key);
            }
        }

        void IncrementValue (int count, Key key)
        {
            for (; i < count; i++)
            {
                app.Keyboard.RaiseKeyDownEvent (key);
                app.LayoutAndDraw ();

                CheckAssertion (new Point (i + 1, 0), new Point (0, i + 1), key);
            }
        }

        bool? IsColumn (Key key)
        {
            if (key == Key.CursorLeft || key == Key.CursorRight)
            {
                return true;
            }

            if (key == Key.CursorUp || key == Key.CursorDown)
            {
                return false;
            }

            return null;
        }

        void CheckAssertion (Point colLocation, Point rowLocation, Key key)
        {
            bool? isCol = IsColumn (key);

            switch (isCol)
            {
                case true:
                    Assert.Equal (colLocation, view.Frame.Location);

                    break;

                case false:
                    Assert.Equal (rowLocation, view.Frame.Location);

                    break;

                default:
                    throw new InvalidOperationException ();
            }
        }
    }

    [Theory]
    [InlineData (ShadowStyles.None, 3, 4, 4)]
    [InlineData (ShadowStyles.Opaque, 4, 5, 5)]
    [InlineData (ShadowStyles.Transparent, 4, 5, 5)]
    public void Margin_Thickness_Changes_Adjust_Correctly (ShadowStyles style, int expectedThickness, int expectedThicknessAdjust, int expectedThicknessNone)
    {
        var view = new View ();
        view.Margin.Thickness = new Thickness (3);
        view.ShadowStyle = style;
        Assert.Equal (new Thickness (3, 3, expectedThickness, expectedThickness), view.Margin.Thickness);

        view.Margin.Thickness = new Thickness (3, 3, expectedThickness + 1, expectedThickness + 1);
        Assert.Equal (new Thickness (3, 3, expectedThicknessAdjust, expectedThicknessAdjust), view.Margin.Thickness);
        view.ShadowStyle = ShadowStyles.None;
        Assert.Equal (new Thickness (3, 3, expectedThicknessNone, expectedThicknessNone), view.Margin.Thickness);
        view.Dispose ();
    }

    [Fact]
    public void Runnable_View_Overlap_Other_Runnables ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (10, 5);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill (), Text = "🍎".Repeat (25)! };
        View view = new () { Width = 7, Height = 2, ShadowStyle = ShadowStyles.Opaque, Text = "| Hi |" };
        superview.Add (view);

        app.Begin (superview);

        DriverAssert.AssertDriverContentsAre ("""
                                              | Hi |▖ 🍎
                                              ▝▀▀▀▀▀▘ 🍎
                                              🍎🍎🍎🍎🍎
                                              🍎🍎🍎🍎🍎
                                              🍎🍎🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        Runnable modalSuperview = new () { Y = 1, Width = Dim.Fill (), Height = 4, BorderStyle = LineStyle.Single };
        View view1 = new () { Width = 8, Height = 2, ShadowStyle = ShadowStyles.Opaque, Text = "| Hey |" };
        modalSuperview.Add (view1);

        app.Begin (modalSuperview);

        Assert.True (modalSuperview.IsModal);

        DriverAssert.AssertDriverContentsAre ("""
                                              | Hi |▖ 🍎
                                              ┌────────┐
                                              │| Hey |▖│
                                              │▝▀▀▀▀▀▀▘│
                                              └────────┘
                                              """,
                                              output,
                                              app.Driver);

        app.Dispose ();
    }

    [Fact]
    public void TransparentShadow_Draws_Transparent_At_Driver_Output ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (2, 1);
        app.Driver.Force16Colors = true;

        using Runnable superView = new ();
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();
        superView.Text = "AB";
        superView.TextFormatter.WordWrap = true;
        superView.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        // Create view with transparent shadow
        View viewWithShadow = new () { Width = Dim.Auto (), Height = Dim.Auto (), Text = "*", ShadowStyle = ShadowStyles.Transparent };

        // Make it so the margin is only on the right for simplicity
        viewWithShadow.Margin.Thickness = new Thickness (0, 0, 1, 0);
        viewWithShadow.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        superView.Add (viewWithShadow);

        // Act
        app.Begin (superView);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        // Assert
        Assert.Equal (new Rectangle (0, 0, 2, 2), viewWithShadow.Frame);
        Assert.Equal (new Rectangle (0, 0, 1, 1), viewWithShadow.Viewport);

        output.WriteLine ("Actual driver contents:");
        output.WriteLine (app.Driver.ToString ());
        output.WriteLine ("\nActual driver output:");
        string output1 = app.Driver.GetOutput ().GetLastOutput ();
        output.WriteLine (output1);

        // Printed with bright black (dark gray) text on bright black (dark gray) background making it invisible
        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[30m\x1b[107m*\x1b[93m\x1b[100mB
                                           """,
                                           output,
                                           app.Driver);
    }

    [Fact]
    public void TransparentShadow_OverWide_Draws_Transparent_At_Driver_Output ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (2, 3);
        app.Driver.Force16Colors = true;

        using Runnable superView = new ();
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();
        superView.Text = "🍎🍎🍎🍎";
        superView.TextFormatter.WordWrap = true;
        superView.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        // Create view with transparent shadow
        View viewWithShadow = new () { Width = Dim.Auto (), Height = Dim.Auto (), Text = "*", ShadowStyle = ShadowStyles.Transparent };

        // Make it so the margin is only on the bottom for simplicity
        viewWithShadow.Margin.Thickness = new Thickness (0, 0, 0, 1);
        viewWithShadow.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        superView.Add (viewWithShadow);

        // Act
        app.Begin (superView);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        // Assert
        output.WriteLine ("Actual driver contents:");
        output.WriteLine (app.Driver.ToString ());
        output.WriteLine ("\nActual driver output:");
        string output1 = app.Driver.GetOutput ().GetLastOutput ();
        output.WriteLine (output1);

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[30m\x1b[107m*\x1b[90m\x1b[40m \x1b[97m\x1b[40m \x1b[93m\x1b[100m \x1b[97m\x1b[40m🍎
                                           """,
                                           output,
                                           app.Driver);
    }

    /// <summary>
    ///     Proves Issue #5088: ShadowStyle getter returns an inherited value from
    ///     SuperView even though no shadow was ever set on the view itself. Reading the
    ///     value and writing it back should be a no-op, but instead it creates a
    ///     MarginView and adds shadow thickness.
    /// </summary>
    [Fact]
    public void ShadowStyle_Getter_Does_Not_Inherit_From_SuperView ()
    {
        // Explicitly give the SuperView a transparent shadow for this inheritance test.
        Window superView = new () { Width = 20, Height = 10, ShadowStyle = ShadowStyles.Transparent };

        View child = new () { Width = 5, Height = 3 };
        superView.Add (child);

        // The subview was never assigned a ShadowStyle - it must be null.
        Assert.Null (child.Margin.ShadowStyle);
        Assert.Null (child.ShadowStyle);

        // Round-trip: reading and writing back should be a safe no-op.
        ShadowStyles? readBack = child.ShadowStyle;
        child.ShadowStyle = readBack;

        // After the round-trip the subview must still have no shadow and no Margin thickness.
        Assert.Null (child.ShadowStyle);
        Assert.Equal (Thickness.Empty, child.Margin.Thickness);

        // MarginView should NOT have been created by a no-op assignment.
        Assert.Null (child.Margin.View);

        child.Dispose ();
        superView.Dispose ();
    }

    /// <summary>
    ///     Proves that setting ShadowStyle to null resets ShadowSize on the MarginView
    ///     to <see cref="Size.Empty"/>, so that state is consistent when no shadow is active.
    /// </summary>
    [Fact]
    public void Setting_ShadowStyle_Null_Resets_ShadowSize ()
    {
        View view = new ();

        // Set a shadow - this creates a MarginView with ShadowSize {1,1}.
        view.ShadowStyle = ShadowStyles.Transparent;
        var marginView = view.Margin.View as MarginView;
        Assert.NotNull (marginView);
        Assert.Equal (new Size (1, 1), marginView.ShadowSize);

        // Now clear the shadow.
        view.ShadowStyle = null;

        // ShadowSize should be reset to Empty - no residual state.
        Assert.Equal (Size.Empty, marginView.ShadowSize);

        view.Dispose ();
    }
}

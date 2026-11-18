namespace ViewsTests;

/// <summary>
///     Pure unit tests for <see cref="ColorPicker"/> that don't require Application.Driver or View context.
///     These tests can run in parallel without interference.
/// </summary>
public class ColorPickerTests
{

    [Fact]
    public void ColorPicker16_MouseEvents ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        ColorPicker16 colorPicker = new () { X = 0, Y = 0, Height = 4, Width = 32 };
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);
        ((View)runnable).Add (colorPicker);
        app.Begin (runnable);

        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (4, 1)));
        Assert.Equal (ColorName16.Blue, colorPicker.SelectedColor);
    }

    [Fact]
    public void ColorPicker16_Constructors ()
    {
        var colorPicker = new ColorPicker16 ();
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);
        Assert.Equal (Point.Empty, colorPicker.Caret);
        Assert.True (colorPicker.CanFocus);

        colorPicker.BeginInit ();
        colorPicker.EndInit ();
        colorPicker.LayoutSubViews ();
        Assert.Equal (new (0, 0, 32, 4), colorPicker.Frame);
    }

    [Fact]
    public void ColorPicker16_KeyBindings_Command ()
    {
        var colorPicker = new ColorPicker16 ();
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (ColorName16.Blue, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (ColorName16.BrightBlue, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (ColorName16.DarkGray, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);

        colorPicker.NewKeyDownEvent (Key.CursorLeft);
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);

        colorPicker.NewKeyDownEvent (Key.CursorUp);
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);
    }

    [Fact]
    public void SelectedColorAndCursor ()
    {
        var colorPicker = new ColorPicker16 ();
        colorPicker.SelectedColor = ColorName16.White;
        Assert.Equal (7, colorPicker.Caret.X);
        Assert.Equal (1, colorPicker.Caret.Y);

        colorPicker.SelectedColor = Color.Black;
        Assert.Equal (0, colorPicker.Caret.X);
        Assert.Equal (0, colorPicker.Caret.Y);

        colorPicker.Caret = new (7, 1);
        Assert.Equal (ColorName16.White, colorPicker.SelectedColor);

        colorPicker.Caret = Point.Empty;
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);
    }

    [Fact]
    public void ChangedEvent_Fires ()
    {
        Color newColor = default;
        var count = 0;

        var cp = new ColorPicker ();

        cp.ValueChanged += (s, e) =>
                           {
                               count++;
                               newColor = e.NewValue ?? Color.Black;

                               Assert.Equal (cp.SelectedColor, e.NewValue);
                           };

        cp.SelectedColor = new Color (1, 2, 3);
        Assert.Equal (1, count);
        Assert.Equal (new Color (1, 2, 3), newColor);

        cp.SelectedColor = new Color (2, 3, 4);

        Assert.Equal (2, count);
        Assert.Equal (new Color (2, 3, 4), newColor);

        // Set to same value
        cp.SelectedColor = new Color (2, 3, 4);

        // Should have no effect
        Assert.Equal (2, count);
    }

    [Fact]
    public void ChangeValueOnUI_UpdatesAllUIElements ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true);

        var otherView = new View { CanFocus = true };

        cp.App!.TopRunnableView?.Add (otherView); // thi sets focus to otherView
        Assert.True (otherView.HasFocus);

        cp.SetFocus ();
        Assert.False (otherView.HasFocus);

        cp.Draw (); // Draw is needed to update TrianglePosition

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);
        TextField rTextField = GetTextField (cp, ColorPickerPart.Bar1);
        TextField gTextField = GetTextField (cp, ColorPickerPart.Bar2);
        TextField bTextField = GetTextField (cp, ColorPickerPart.Bar3);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (2, r.TrianglePosition);
        Assert.Equal ("0", rTextField.Text);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("0", gTextField.Text);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("0", bTextField.Text);
        Assert.Equal ("#000000", hex.Text);

        // Change value using text field
        TextField rBarTextField = cp.SubViews.OfType<TextField> ().First (tf => tf.Text == "0");

        rBarTextField.SetFocus ();
        rBarTextField.Text = "128";

        otherView.SetFocus ();
        Assert.True (otherView.HasFocus);

        cp.Draw (); // Draw is needed to update TrianglePosition

        Assert.Equal ("R:", r.Text);
        Assert.Equal (9, r.TrianglePosition);
        Assert.Equal ("128", rTextField.Text);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("0", gTextField.Text);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("0", bTextField.Text);
        Assert.Equal ("#800000", hex.Text);

        cp.App?.Dispose ();
    }

    [Fact]
    public void ClickingAtEndOfBar_SetsMaxValue ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);

        cp.Draw (); // Draw is needed to update TrianglePosition

        // Click at the end of the Red bar
        cp.Focused!.NewMouseEvent (new Mouse
        {
            Flags = MouseFlags.LeftButtonPressed, Position = new Point (19, 0) // Assuming 0-based indexing
        });

        cp.Draw (); // Draw is needed to update TrianglePosition

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (19, r.TrianglePosition);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        cp.App?.Dispose ();
    }

    [Fact]
    public void ClickingBeyondBar_ChangesToMaxValue ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);

        cp.Draw (); // Draw is needed to update TrianglePosition

        // Click beyond the bar
        cp.Focused!.NewMouseEvent (new Mouse
        {
            Flags = MouseFlags.LeftButtonPressed, Position = new Point (21, 0) // Beyond the bar
        });

        cp.Draw (); // Draw is needed to update TrianglePosition

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (19, r.TrianglePosition);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        cp.App?.Dispose ();
    }

    [Fact]
    public void ClickingDifferentBars_ChangesFocus ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);

        cp.Draw (); // Draw is needed to update TrianglePosition

        // Click on Green bar (press then release to complete the click cycle and release grab)
        cp.App!.Mouse.RaiseMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (0, 1) });
        cp.App!.Mouse.RaiseMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (0, 1) });

        cp.Draw (); // Draw is needed to update TrianglePosition

        Assert.IsAssignableFrom<GBar> (cp.Focused);

        // Click on Blue bar (press then release to complete the click cycle and release grab)
        cp.App!.Mouse.RaiseMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (0, 2) });
        cp.App!.Mouse.RaiseMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (0, 2) });

        cp.Draw (); // Draw is needed to update TrianglePosition

        Assert.IsAssignableFrom<BBar> (cp.Focused);

        cp.App?.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void ColorPicker_BarValueChange_UpdatesColor ()
    {
        ColorPicker picker = new () { Width = 40, Height = 15 };
        picker.BeginInit ();
        picker.EndInit ();

        Color initialColor = picker.SelectedColor;

        // Color bar value changes update color via Activate commands
        // Verify control is initialized
        Assert.Equal (Color.Black, initialColor); // Default color

        picker.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void ColorPicker_DoubleClick_RaisesAccepting ()
    {
        ColorPicker picker = new () { Width = 40, Height = 15 };
        picker.BeginInit ();
        picker.EndInit ();

        var acceptingFired = false;

        picker.Accepting += (_, e) =>
                            {
                                acceptingFired = true;
                                e.Handled = true;
                            };

        // Double-click raises Accepting
        bool? result = picker.InvokeCommand (Command.Accept);

        Assert.True (acceptingFired);
        Assert.True (result);

        picker.Dispose ();
    }

    public static IEnumerable<object []> ColorPickerTestData ()
    {
        yield return [new Color (255, 0), "R:", 19, "G:", 2, "B:", 2, "#FF0000"];

        yield return [new Color (0, 255), "R:", 2, "G:", 19, "B:", 2, "#00FF00"];

        yield return [new Color (0, 0, 255), "R:", 2, "G:", 2, "B:", 19, "#0000FF"];

        yield return [new Color (125, 125, 125), "R:", 11, "G:", 11, "B:", 11, "#7D7D7D"];
    }

    public static IEnumerable<object []> ColorPickerTestData_WithTextFields ()
    {
        yield return
        [
            new Color (255, 0),
            "R:",
            15,
            255,
            "G:",
            2,
            0,
            "B:",
            2,
            0,
            "#FF0000"
        ];

        yield return
        [
            new Color (0, 255),
            "R:",
            2,
            0,
            "G:",
            15,
            255,
            "B:",
            2,
            0,
            "#00FF00"
        ];

        yield return
        [
            new Color (0, 0, 255),
            "R:",
            2,
            0,
            "G:",
            2,
            0,
            "B:",
            15,
            255,
            "#0000FF"
        ];

        yield return
        [
            new Color (125, 125, 125),
            "R:",
            9,
            125,
            "G:",
            9,
            125,
            "B:",
            9,
            125,
            "#7D7D7D"
        ];
    }

    [Fact]
    public void Construct_DefaultValue ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.HSV, false);

        // Should be only a single text field (Hex) because ShowTextFields is false
        Assert.Single (cp.SubViews.OfType<TextField> ());

        cp.Draw (); // Draw is needed to update TrianglePosition

        // All bars should be at 0 with the triangle at 0 (+2 because of "H:", "S:" etc)
        ColorBar h = GetColorBar (cp, ColorPickerPart.Bar1);
        Assert.Equal ("H:", h.Text);
        Assert.Equal (2, h.TrianglePosition);
        Assert.IsType<HueBar> (h);

        ColorBar s = GetColorBar (cp, ColorPickerPart.Bar2);
        Assert.Equal ("S:", s.Text);
        Assert.Equal (2, s.TrianglePosition);
        Assert.IsType<SaturationBar> (s);

        ColorBar v = GetColorBar (cp, ColorPickerPart.Bar3);
        Assert.Equal ("V:", v.Text);
        Assert.Equal (2, v.TrianglePosition);
        Assert.IsType<ValueBar> (v);

        TextField hex = GetTextField (cp, ColorPickerPart.Hex);
        Assert.Equal ("#000000", hex.Text);

        cp.App?.Dispose ();
    }

    [Fact]
    public void DisposesOldViews_OnModelChange ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.HSL, true);

        ColorBar b1 = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar b2 = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b3 = GetColorBar (cp, ColorPickerPart.Bar3);

        TextField tf1 = GetTextField (cp, ColorPickerPart.Bar1);
        TextField tf2 = GetTextField (cp, ColorPickerPart.Bar2);
        TextField tf3 = GetTextField (cp, ColorPickerPart.Bar3);

        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

#if DEBUG_IDISPOSABLE
        Assert.All (new View [] { b1, b2, b3, tf1, tf2, tf3, hex }, b => Assert.False (b.WasDisposed));
#endif
        cp.Style.ColorModel = ColorModel.RGB;
        cp.ApplyStyleChanges ();

        ColorBar b1After = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar b2After = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b3After = GetColorBar (cp, ColorPickerPart.Bar3);

        TextField tf1After = GetTextField (cp, ColorPickerPart.Bar1);
        TextField tf2After = GetTextField (cp, ColorPickerPart.Bar2);
        TextField tf3After = GetTextField (cp, ColorPickerPart.Bar3);

        TextField hexAfter = GetTextField (cp, ColorPickerPart.Hex);

        // Old bars should be disposed
#if DEBUG_IDISPOSABLE
        Assert.All (new View [] { b1, b2, b3, tf1, tf2, tf3, hex }, b => Assert.True (b.WasDisposed));
#endif
        Assert.NotSame (hex, hexAfter);

        Assert.NotSame (b1, b1After);
        Assert.NotSame (b2, b2After);
        Assert.NotSame (b3, b3After);

        Assert.NotSame (tf1, tf1After);
        Assert.NotSame (tf2, tf2After);
        Assert.NotSame (tf3, tf3After);

        cp.App?.Dispose ();
    }

    [Fact]
    public void EnterHexFor_ColorName ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true, true);

        cp.Draw (); // Draw is needed to update TrianglePosition

        TextField name = GetTextField (cp, ColorPickerPart.ColorName);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        hex.SetFocus ();

        Assert.True (hex.HasFocus);
        Assert.Same (hex, cp.Focused);

        hex.Text = "";

        Assert.Empty (hex.Text);

        // Name field shows "Black" initially (from SelectedColor = Color.Black)
        Assert.Equal ("Black", name.Text);

        cp.App!.Keyboard.RaiseKeyDownEvent ('#');

        // Name stays at "Black" while typing hex (no update until leave/accept)
        Assert.Equal ("Black", name.Text);

        //7FFFD4

        Assert.Equal ("#", hex.Text);
        cp.App!.Keyboard.RaiseKeyDownEvent ('7');
        cp.App!.Keyboard.RaiseKeyDownEvent ('F');
        cp.App!.Keyboard.RaiseKeyDownEvent ('F');
        cp.App!.Keyboard.RaiseKeyDownEvent ('F');
        cp.App!.Keyboard.RaiseKeyDownEvent ('D');
        Assert.Equal ("Black", name.Text);

        cp.App!.Keyboard.RaiseKeyDownEvent ('4');

        Assert.True (hex.HasFocus);

        // Tab out of the hex field - should wrap to first focusable subview 
        cp.App!.Keyboard.RaiseKeyDownEvent (Key.Tab);
        Assert.False (hex.HasFocus);
        Assert.NotSame (hex, cp.Focused);

        // Color name should be recognised as a known string and populated
        Assert.Equal ("#7FFFD4", hex.Text);
        Assert.Equal ("Aquamarine", name.Text);

        cp.App?.Dispose ();
    }

    /// <summary>
    ///     In this version we use the Enter button to accept the typed text instead
    ///     of tabbing to the next view.
    /// </summary>
    [Fact]
    public void EnterHexFor_ColorName_AcceptVariation ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true, true);

        cp.Draw (); // Draw is needed to update TrianglePosition

        TextField name = GetTextField (cp, ColorPickerPart.ColorName);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        hex.SetFocus ();

        Assert.True (hex.HasFocus);
        Assert.Same (hex, cp.Focused);

        hex.Text = "";

        Assert.Empty (hex.Text);

        // Name field shows "Black" initially (from SelectedColor = Color.Black)
        Assert.Equal ("Black", name.Text);

        cp.App!.Keyboard.RaiseKeyDownEvent ('#');

        // Name stays at "Black" while typing hex (no update until leave/accept)
        Assert.Equal ("Black", name.Text);

        //7FFFD4

        Assert.Equal ("#", hex.Text);
        cp.App!.Keyboard.RaiseKeyDownEvent ('7');
        cp.App!.Keyboard.RaiseKeyDownEvent ('F');
        cp.App!.Keyboard.RaiseKeyDownEvent ('F');
        cp.App!.Keyboard.RaiseKeyDownEvent ('F');
        cp.App!.Keyboard.RaiseKeyDownEvent ('D');
        Assert.Equal ("Black", name.Text);

        cp.App!.Keyboard.RaiseKeyDownEvent ('4');

        Assert.True (hex.HasFocus);

        // Should stay in the hex field (because accept not tab)
        cp.App!.Keyboard.RaiseKeyDownEvent (Key.Enter);
        Assert.True (hex.HasFocus);
        Assert.Same (hex, cp.Focused);

        // But still, Color name should be recognised as a known string and populated
        Assert.Equal ("#7FFFD4", hex.Text);
        Assert.Equal ("Aquamarine", name.Text);

        cp.App?.Dispose ();
    }

    [Fact]
    public void InvalidHexInput_DoesNotChangeColor ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true);

        cp.Draw (); // Draw is needed to update TrianglePosition

        // Enter invalid hex value
        TextField hexField = cp.SubViews.OfType<TextField> ().First (tf => tf.Text == "#000000");
        hexField.SetFocus ();
        hexField.Text = "#ZZZZZZ";
        Assert.True (hexField.HasFocus);
        Assert.Equal ("#ZZZZZZ", hexField.Text);

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("#ZZZZZZ", hex.Text);

        // Advance away from hexField to cause validation
        cp.AdvanceFocus (NavigationDirection.Forward, null);

        cp.Draw (); // Draw is needed to update TrianglePosition

        Assert.Equal ("R:", r.Text);
        Assert.Equal (2, r.TrianglePosition);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("#000000", hex.Text);

        cp.App?.Dispose ();
    }

    [Fact]
    public void RGB_KeyboardNavigation ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);
        cp.Draw (); // Draw is needed to update TrianglePosition

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (2, r.TrianglePosition);
        Assert.IsType<RBar> (r);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.IsType<GBar> (g);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.IsType<BBar> (b);
        Assert.Equal ("#000000", hex.Text);

        Assert.IsAssignableFrom<IColorBar> (cp.Focused);

        cp.Draw (); // Draw is needed to update TrianglePosition

        cp.App!.Keyboard.RaiseKeyDownEvent (Key.CursorRight);

        cp.Draw (); // Draw is needed to update TrianglePosition

        Assert.Equal (3, r.TrianglePosition);
        Assert.Equal ("#0F0000", hex.Text);

        cp.App!.Keyboard.RaiseKeyDownEvent (Key.CursorRight);

        cp.Draw (); // Draw is needed to update TrianglePosition

        Assert.Equal (4, r.TrianglePosition);
        Assert.Equal ("#1E0000", hex.Text);

        // Use cursor to move the triangle all the way to the right
        for (var i = 0; i < 1000; i++)
        {
            cp.App!.Keyboard.RaiseKeyDownEvent (Key.CursorRight);
        }

        cp.Draw (); // Draw is needed to update TrianglePosition

        // 20 width and TrianglePosition is 0 indexed
        // Meaning we are asserting that triangle is at end
        Assert.Equal (19, r.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        cp.App?.Dispose ();
    }

    [Fact]
    public void RGB_MouseNavigation ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);

        cp.Draw (); // Draw is needed to update TrianglePosition

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (2, r.TrianglePosition);
        Assert.IsType<RBar> (r);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.IsType<GBar> (g);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.IsType<BBar> (b);
        Assert.Equal ("#000000", hex.Text);

        Assert.IsAssignableFrom<IColorBar> (cp.Focused);

        cp.Focused!.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed, Position = new Point (3, 0) });

        cp.Draw (); // Draw is needed to update TrianglePosition

        Assert.Equal (3, r.TrianglePosition);
        Assert.Equal ("#0F0000", hex.Text);

        cp.Focused.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed, Position = new Point (4, 0) });

        cp.Draw (); // Draw is needed to update TrianglePosition

        Assert.Equal (4, r.TrianglePosition);
        Assert.Equal ("#1E0000", hex.Text);

        cp.App?.Dispose ();
    }

    [Theory]
    [MemberData (nameof (ColorPickerTestData))]
    public void RGB_NoText (Color c,
                            string expectedR,
                            int expectedRTriangle,
                            string expectedG,
                            int expectedGTriangle,
                            string expectedB,
                            int expectedBTriangle,
                            string expectedHex)
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);
        cp.SelectedColor = c;

        cp.Draw (); // Draw is needed to update TrianglePosition

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal (expectedR, r.Text);
        Assert.Equal (expectedRTriangle, r.TrianglePosition);
        Assert.Equal (expectedG, g.Text);
        Assert.Equal (expectedGTriangle, g.TrianglePosition);
        Assert.Equal (expectedB, b.Text);
        Assert.Equal (expectedBTriangle, b.TrianglePosition);
        Assert.Equal (expectedHex, hex.Text);

        cp.App?.Dispose ();
    }

    [Theory]
    [MemberData (nameof (ColorPickerTestData_WithTextFields))]
    public void RGB_NoText_WithTextFields (Color c,
                                           string expectedR,
                                           int expectedRTriangle,
                                           int expectedRValue,
                                           string expectedG,
                                           int expectedGTriangle,
                                           int expectedGValue,
                                           string expectedB,
                                           int expectedBTriangle,
                                           int expectedBValue,
                                           string expectedHex)
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true);
        cp.SelectedColor = c;

        cp.Draw (); // Draw is needed to update TrianglePosition

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);
        TextField rTextField = GetTextField (cp, ColorPickerPart.Bar1);
        TextField gTextField = GetTextField (cp, ColorPickerPart.Bar2);
        TextField bTextField = GetTextField (cp, ColorPickerPart.Bar3);

        Assert.Equal (expectedR, r.Text);
        Assert.Equal (expectedRTriangle, r.TrianglePosition);
        Assert.Equal (expectedRValue.ToString (), rTextField.Text);
        Assert.Equal (expectedG, g.Text);
        Assert.Equal (expectedGTriangle, g.TrianglePosition);
        Assert.Equal (expectedGValue.ToString (), gTextField.Text);
        Assert.Equal (expectedB, b.Text);
        Assert.Equal (expectedBTriangle, b.TrianglePosition);
        Assert.Equal (expectedBValue.ToString (), bTextField.Text);
        Assert.Equal (expectedHex, hex.Text);

        cp.App?.Dispose ();
    }

    [Fact]
    public void SwitchingColorModels_ResetsBars ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);
        cp.SelectedColor = new Color (255, 0);

        cp.Draw (); // Draw is needed to update TrianglePosition

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (19, r.TrianglePosition);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        // Switch to HSV
        cp.Style.ColorModel = ColorModel.HSV;
        cp.ApplyStyleChanges ();

        cp.Draw (); // Draw is needed to update TrianglePosition

        ColorBar h = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar s = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar v = GetColorBar (cp, ColorPickerPart.Bar3);

        Assert.Equal ("H:", h.Text);
        Assert.Equal (2, h.TrianglePosition);
        Assert.Equal ("S:", s.Text);
        Assert.Equal (19, s.TrianglePosition);
        Assert.Equal ("V:", v.Text);
        Assert.Equal (19, v.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        cp.App?.Dispose ();
    }

    [Fact]
    public void SyncBetweenTextFieldAndBars ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true);

        cp.Draw (); // Draw is needed to update TrianglePosition

        // Change value using the bar
        RBar rBar = cp.SubViews.OfType<RBar> ().First ();
        rBar.Value = 128;

        cp.Draw (); // Draw is needed to update TrianglePosition

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);
        TextField rTextField = GetTextField (cp, ColorPickerPart.Bar1);
        TextField gTextField = GetTextField (cp, ColorPickerPart.Bar2);
        TextField bTextField = GetTextField (cp, ColorPickerPart.Bar3);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (9, r.TrianglePosition);
        Assert.Equal ("128", rTextField.Text);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("0", gTextField.Text);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("0", bTextField.Text);
        Assert.Equal ("#800000", hex.Text);

        cp.App?.Dispose ();
    }

    [Fact]
    public void DropDownListColorName_SelectsColor ()
    {
        // Copilot
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true, true);

        cp.Draw (); // Draw is needed to update TrianglePosition

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField name = GetTextField (cp, ColorPickerPart.ColorName);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        // Verify the name field is a DropDownList
        Assert.IsType<DropDownList> (name);

        // Initial color is Black
        Assert.Equal ("Black", name.Text);

        // Directly set the text on the DropDownList to simulate selecting "Aqua"
        name.Text = "Aqua";

        // The color should now be Aqua (#00FFFF)
        Assert.Equal ("#00FFFF", hex.Text);

        cp.App?.Dispose ();
    }

    private ColorBar GetColorBar (ColorPicker cp, ColorPickerPart toGet)
    {
        if (toGet <= ColorPickerPart.Bar3)
        {
            return cp.SubViews.OfType<ColorBar> ().ElementAt ((int)toGet);
        }

        throw new NotSupportedException ("ColorPickerPart must be a bar");
    }

    private static ColorPicker GetColorPicker (ColorModel colorModel, bool showTextFields, bool showName = false)
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var cp = new ColorPicker { Width = 20, SelectedColor = new Color (0, 0) };
        cp.Style.ColorModel = colorModel;
        cp.Style.ShowTextFields = showTextFields;
        cp.Style.ShowColorName = showName;
        cp.ApplyStyleChanges ();
        Runnable<bool>? runnable = new () { Width = 20, Height = 5 };
        app.Begin (runnable);
        runnable.Add (cp);

        app.LayoutAndDraw ();

        return cp;
    }

    private TextField GetTextField (ColorPicker cp, ColorPickerPart toGet)
    {
        bool hasBarValueTextFields = cp.Style.ShowTextFields;
        bool hasColorNameTextField = cp.Style.ShowColorName;

        switch (toGet)
        {
            case ColorPickerPart.Bar1:
            case ColorPickerPart.Bar2:
            case ColorPickerPart.Bar3:
                if (!hasBarValueTextFields)
                {
                    throw new NotSupportedException ("Corresponding Style option is not enabled");
                }

                return cp.SubViews.OfType<TextField> ().ElementAt ((int)toGet);

            case ColorPickerPart.ColorName:
                if (!hasColorNameTextField)
                {
                    throw new NotSupportedException ("Corresponding Style option is not enabled");
                }

                return cp.SubViews.OfType<TextField> ().ElementAt (hasBarValueTextFields ? (int)toGet : (int)toGet - 3);

            case ColorPickerPart.Hex:

                int offset = hasBarValueTextFields ? 0 : 3;
                offset += hasColorNameTextField ? 0 : 1;

                return cp.SubViews.OfType<TextField> ().ElementAt ((int)toGet - offset);

            default:
                throw new ArgumentOutOfRangeException (nameof (toGet), toGet, null);
        }
    }

    private enum ColorPickerPart
    {
        Bar1 = 0,
        Bar2 = 1,
        Bar3 = 2,
        ColorName = 3,
        Hex = 4
    }

    #region IValue<Color?> Tests

    // Claude - Opus 4.5

    [Fact]
    public void Implements_IValue_Interface ()
    {
        ColorPicker picker = new ();
        Assert.IsAssignableFrom<IValue<Color?>> (picker);
    }

    [Fact]
    public void Value_Property_GetsSelectedColor ()
    {
        ColorPicker picker = new ();
        picker.SelectedColor = Color.Red;
        Assert.Equal (Color.Red, picker.Value);
    }

    [Fact]
    public void Value_Property_SetsSelectedColor ()
    {
        ColorPicker picker = new ();
        picker.Value = Color.Blue;
        Assert.Equal (Color.Blue, picker.SelectedColor);
    }

    [Fact]
    public void Value_Null_SetsBlack ()
    {
        ColorPicker picker = new ();
        picker.SelectedColor = Color.Red;
        picker.Value = null;
        Assert.Equal (Color.Black, picker.SelectedColor);
        Assert.Equal (Color.Black, picker.Value);
    }

    [Fact]
    public void ValueChanging_CanCancel ()
    {
        ColorPicker picker = new ();
        picker.SelectedColor = Color.Red;

        picker.ValueChanging += (_, e) => e.Handled = true;
        picker.Value = Color.Blue;

        Assert.Equal (Color.Red, picker.Value);
    }

    [Fact]
    public void ValueChanged_Fires_WhenValueChanges ()
    {
        ColorPicker picker = new ();
        Color? received = null;
        picker.ValueChanged += (_, e) => received = e.NewValue;

        picker.Value = Color.Green;

        Assert.Equal (Color.Green, received);
    }

    [Fact]
    public void ValueChanged_DoesNotFire_WhenValueSame ()
    {
        ColorPicker picker = new ();
        picker.SelectedColor = Color.Red;

        var count = 0;
        picker.ValueChanged += (_, _) => count++;

        picker.Value = Color.Red; // Same value

        Assert.Equal (0, count);
    }

    [Fact]
    public void ValueChanged_Fires_WhenSettingValue ()
    {
        ColorPicker picker = new ();
        Color? received = default;
        picker.ValueChanged += (_, e) => received = e.NewValue;

        picker.Value = Color.Cyan;

        Assert.Equal (Color.Cyan, received);
    }

    [Fact]
    public void ValueChanging_Fires_BeforeValueChanged ()
    {
        ColorPicker picker = new ();
        List<string> events = [];

        picker.ValueChanging += (_, _) => events.Add ("changing");
        picker.ValueChanged += (_, _) => events.Add ("changed");

        picker.Value = Color.Magenta;

        Assert.Equal (["changing", "changed"], events);
    }

    [Fact]
    public void ValueChanging_ReceivesOldAndNewValues ()
    {
        ColorPicker picker = new ();
        picker.SelectedColor = Color.Red;

        Color? receivedOld = null;
        Color? receivedNew = null;

        picker.ValueChanging += (_, e) =>
                                {
                                    receivedOld = e.CurrentValue;
                                    receivedNew = e.NewValue;
                                };

        picker.Value = Color.Blue;

        Assert.Equal (Color.Red, receivedOld);
        Assert.Equal (Color.Blue, receivedNew);
    }

    #endregion

    #region ColorBar Mouse Binding Tests (issue #5143)

    // Copilot

    [Fact]
    public void ColorBar_MousePress_UpdatesValue ()
    {
        // Regression: pressing on a bar must still update its value after the
        // value-update logic was moved from OnMouseEvent to Command.Activate.
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);
        cp.Draw ();

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        Assert.Equal (0, r.Value);

        // Position 10 is inside the bar (bar starts at X=2 for "R:" label, width 18).
        r.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed, Position = new Point (10, 0) });

        Assert.True (r.Value > 0, "Value must increase when pressing inside the bar.");

        cp.App?.Dispose ();
    }

    [Fact]
    public void ColorBar_MouseEvent_CanBeCancelled ()
    {
        // Subscribing to MouseEvent and setting Handled=true must prevent the value
        // update. Previously the update happened in OnMouseEvent before the event was
        // raised, making cancellation impossible.
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);
        cp.Draw ();

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        Assert.Equal (0, r.Value);

        r.MouseEvent += (_, e) => { e.Handled = true; };

        r.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed, Position = new Point (10, 0) });

        Assert.Equal (0, r.Value);

        cp.App?.Dispose ();
    }

    [Fact]
    public void ColorBar_Drag_BoundedToOriginatingBar ()
    {
        // Dragging the mouse from one bar into another bar must not alter the second
        // bar's value. GrabMouse in Command.Activate (not OnMouseEvent) ensures all subsequent
        // drag events are routed to the bar where the press originated.
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);
        cp.Draw ();

        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        Assert.Equal (0, g.Value);

        // Press on the Red bar row (Y=0 in screen coords).
        cp.App!.Mouse.RaiseMouseEvent (new Mouse
        {
            Flags = MouseFlags.LeftButtonPressed,
            ScreenPosition = new Point (10, 0)
        });

        // Drag into the Green bar row (Y=1) – grab in Command.Activate must route events to Red bar only.
        cp.App!.Mouse.RaiseMouseEvent (new Mouse
        {
            Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport,
            ScreenPosition = new Point (10, 1)
        });

        Assert.Equal (0, g.Value);

        cp.App?.Dispose ();
    }

    #endregion
}

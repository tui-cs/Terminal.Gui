namespace ViewBaseTests.Arrangement;

// Claude - Opus 4.5
/// <summary>
///     Tests that validate focus indication is shown correctly during Arrangement mode.
///     Specifically tests issue #4549 - that focused arrangement buttons show focus colors
///     instead of mouse highlight colors.
/// </summary>
public class ArrangementFocusTests
{
    [Fact]
    public void FocusedButton_WithMouseOver_ShowsFocusColors_NotHighlightColors ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();

        // Create a button with MouseHighlightStates (like arrange mode buttons)
        Button button = new ()
        {
            X = 5,
            Y = 5,
            Text = "Test",
            CanFocus = true,
            MouseHighlightStates = MouseState.In | MouseState.Pressed
        };

        // Set up distinct colors
        Attribute focus = new (ColorName16.White, ColorName16.Blue, TextStyle.None);
        Attribute highlight = new (ColorName16.Black, ColorName16.Yellow, TextStyle.None);
        Attribute normal = new (ColorName16.Black, ColorName16.White, TextStyle.None);

        button.SetScheme (new Scheme { Focus = focus, Highlight = highlight, Normal = normal });

        runnable.Add (button);
        app.Begin (runnable);

        // Give the button focus
        button.SetFocus ();
        Assert.True (button.HasFocus);

        // Simulate mouse over the button
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (6, 5), Flags = MouseFlags.PositionReport });
        Assert.True (button.MouseState.HasFlag (MouseState.In));

        // Act: Get the attribute for Focus role when button has focus AND mouse is over it
        Attribute result = button.GetAttributeForRole (VisualRole.Focus);

        // Assert: Should return FOCUS colors, not HIGHLIGHT colors
        // The fix (adding && !HasFocus) ensures mouse highlight doesn't override focus
        Assert.Equal (focus.Foreground, result.Foreground);
        Assert.Equal (focus.Background, result.Background);
        Assert.NotEqual (highlight.Foreground, result.Foreground);
        Assert.NotEqual (highlight.Background, result.Background);
    }

    [Fact]
    public void FocusedButton_WithoutMouseOver_ShowsFocusColors ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();

        Button button = new ()
        {
            X = 5,
            Y = 5,
            Text = "Test",
            CanFocus = true,
            MouseHighlightStates = MouseState.In | MouseState.Pressed
        };

        Attribute focus = new (ColorName16.White, ColorName16.Blue, TextStyle.None);
        Attribute highlight = new (ColorName16.Black, ColorName16.Yellow, TextStyle.None);
        Attribute normal = new (ColorName16.Black, ColorName16.White, TextStyle.None);

        button.SetScheme (new Scheme { Focus = focus, Highlight = highlight, Normal = normal });

        runnable.Add (button);
        app.Begin (runnable);

        // Give the button focus but NO mouse over
        button.SetFocus ();
        Assert.True (button.HasFocus);
        Assert.Equal (MouseState.None, button.MouseState);

        // Act
        Attribute result = button.GetAttributeForRole (VisualRole.Focus);

        // Assert: Should return focus colors
        Assert.Equal (focus.Foreground, result.Foreground);
        Assert.Equal (focus.Background, result.Background);
    }

    [Fact]
    public void UnfocusedButton_WithMouseOver_ShowsHighlightColors ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();

        Button button = new ()
        {
            X = 5,
            Y = 5,
            Text = "Test",
            CanFocus = true,
            MouseHighlightStates = MouseState.In | MouseState.Pressed
        };

        Attribute focus = new (ColorName16.White, ColorName16.Blue, TextStyle.None);
        Attribute highlight = new (ColorName16.Black, ColorName16.Yellow, TextStyle.None);
        Attribute normal = new (ColorName16.Black, ColorName16.White, TextStyle.None);

        button.SetScheme (new Scheme { Focus = focus, Highlight = highlight, Normal = normal });

        // Add another focusable view so button doesn't automatically get focus
        View otherView = new ()
        {
            X = 0,
            Y = 0,
            Width = 5,
            Height = 5,
            CanFocus = true
        };
        runnable.Add (otherView, button);
        app.Begin (runnable);

        // Give focus to the other view, not the button
        otherView.SetFocus ();

        // Button does NOT have focus
        Assert.False (button.HasFocus);

        // Simulate mouse over the button
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (6, 5), Flags = MouseFlags.PositionReport });
        Assert.True (button.MouseState.HasFlag (MouseState.In));

        // Act: Get the attribute for Normal role when button does NOT have focus BUT mouse is over it
        Attribute result = button.GetAttributeForRole (VisualRole.Normal);

        // Assert: Should return HIGHLIGHT colors because mouse is over and button doesn't have focus
        Assert.Equal (highlight.Foreground, result.Foreground);
        Assert.Equal (highlight.Background, result.Background);
    }

    [Fact]
    public void View_WithMouseHighlightStates_FocusedAndRendering_ShowsFocusColors ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();

        // Create a view with MouseHighlightStates (like any view with hover effects)
        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 20,
            Height = 10,
            CanFocus = true,
            MouseHighlightStates = MouseState.In | MouseState.Pressed
        };

        Attribute focus = new (ColorName16.White, ColorName16.Blue, TextStyle.None);
        Attribute highlight = new (ColorName16.Black, ColorName16.Yellow, TextStyle.None);
        Attribute normal = new (ColorName16.Black, ColorName16.White, TextStyle.None);

        view.SetScheme (new Scheme { Focus = focus, Highlight = highlight, Normal = normal });

        runnable.Add (view);
        app.Begin (runnable);

        // Give the view focus
        view.SetFocus ();
        Assert.True (view.HasFocus);

        // Simulate mouse over the view
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (10, 8), Flags = MouseFlags.PositionReport });
        Assert.True (view.MouseState.HasFlag (MouseState.In));

        // Act: Get the attribute the view would use when rendering with focus
        Attribute result = view.GetAttributeForRole (VisualRole.Focus);

        // Assert: Should use FOCUS colors even though mouse is over it
        // This ensures focus indication is visible when MouseHighlightStates is configured
        Assert.Equal (focus.Foreground, result.Foreground);
        Assert.Equal (focus.Background, result.Background);
        Assert.NotEqual (highlight.Foreground, result.Foreground);
        Assert.NotEqual (highlight.Background, result.Background);
    }

    [Fact]
    public void GetAttributeForRole_WithMousePressed_AndHasFocus_ReturnsFocusNotHighlight ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();

        Button button = new ()
        {
            X = 5,
            Y = 5,
            Text = "Test",
            CanFocus = true,
            MouseHighlightStates = MouseState.In | MouseState.Pressed
        };

        Attribute focus = new (ColorName16.White, ColorName16.Blue, TextStyle.None);
        Attribute highlight = new (ColorName16.Black, ColorName16.Yellow, TextStyle.None);

        button.SetScheme (new Scheme { Focus = focus, Highlight = highlight });

        runnable.Add (button);
        app.Begin (runnable);

        // Button has focus
        button.SetFocus ();
        Assert.True (button.HasFocus);

        // Simulate mouse pressed on button
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (6, 5), Flags = MouseFlags.LeftButtonPressed });
        Assert.True (button.MouseState.HasFlag (MouseState.Pressed));

        // Act: Even with Pressed state, should return focus colors
        Attribute result = button.GetAttributeForRole (VisualRole.Focus);

        // Assert
        Assert.Equal (focus.Foreground, result.Foreground);
        Assert.Equal (focus.Background, result.Background);
    }
}

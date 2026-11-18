using UnitTests;

namespace ViewsTests;

/// <summary>
///     Visual + input-driven tests for <see cref="LinearSelector{T}"/>. Uses
///     <see cref="DriverAssert.AssertDriverContentsWithFrameAre"/> together with
///     <c>InjectKey</c> / <c>InjectMouse</c> through the application pipeline.
/// </summary>
public class LinearSelectorVisualTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // Copilot
    [Fact]
    public void Renders_Initial_Selection_Horizontal ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (10, 3);

        IRunnable runnable = new Runnable ();
        LinearSelector<int> sel = new ([1, 2, 3]) { Value = 2 };
        (runnable as View)?.Add (sel);
        app.Begin (runnable);

        app.LayoutAndDraw ();

        // Glyphs: '●' (option), '─' (space), '█' (set). Index 1 is selected.
        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       """
                                                       ●─█─●
                                                       1 2 3
                                                       """,
                                                       _output,
                                                       app.Driver);
    }

    // Copilot
    [Fact]
    public void Renders_Legends_Without_Highlighting_Set_Option ()
    {
        // Verifies the fix for "Label texts are showing underlined for focused value" —
        // legend rows must use a uniform attribute regardless of which option is set.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (10, 3);

        IRunnable runnable = new Runnable ();
        LinearSelector<int> sel = new ([1, 2, 3]) { Value = 2 };
        (runnable as View)?.Add (sel);
        app.Begin (runnable);

        app.LayoutAndDraw ();

        // All three legend cells should share the same Attribute (no HotNormal / underline for the set option).
        Cell [,] contents = app.Driver!.Contents!;
        Attribute a0 = contents [1, 0].Attribute!.Value;
        Attribute a1 = contents [1, 2].Attribute!.Value;
        Attribute a2 = contents [1, 4].Attribute!.Value;

        Assert.Equal (a0, a1);
        Assert.Equal (a1, a2);
    }

    // Copilot
    [Fact]
    public void Renders_Vertical ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (10, 6);

        IRunnable runnable = new Runnable ();
        LinearSelector<int> sel = new ([1, 2, 3]) { Orientation = Orientation.Vertical, Value = 2 };
        (runnable as View)?.Add (sel);
        app.Begin (runnable);

        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       "●1\n│ \n█2\n│ \n●3",
                                                       _output,
                                                       app.Driver);
    }

    // Copilot
    [Fact]
    public void Keyboard_Right_Moves_Focus_Without_Changing_Value_When_AllowEmpty ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (10, 3);

        IRunnable runnable = new Runnable ();
        LinearSelector<int> sel = new ([1, 2, 3]) { Value = 1, AllowEmpty = true };
        (runnable as View)?.Add (sel);
        app.Begin (runnable);
        sel.SetFocus ();

        Assert.Equal (0, sel.FocusedOption);
        Assert.Equal (1, sel.Value);

        app.InjectKey (new Key (KeyCode.CursorRight));

        Assert.Equal (1, sel.FocusedOption);

        // With AllowEmpty=true, focus moves but value does not change until activation.
        Assert.Equal (1, sel.Value);
    }

    // Copilot
    [Fact]
    public void Keyboard_Space_Activates_FocusedOption ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (10, 3);

        IRunnable runnable = new Runnable ();
        LinearSelector<int> sel = new ([1, 2, 3]) { AllowEmpty = true };
        (runnable as View)?.Add (sel);
        app.Begin (runnable);
        sel.SetFocus ();

        var changedCount = 0;
        sel.ValueChanged += (_, _) => changedCount++;

        app.InjectKey (new Key (KeyCode.CursorRight));
        app.InjectKey (new Key (KeyCode.Space));

        Assert.Equal (2, sel.Value);
        Assert.Equal (1, changedCount);
    }

    // Copilot
    [Fact]
    public void Mouse_Click_Selects_Option_Under_Cursor ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (12, 3);

        IRunnable runnable = new Runnable ();
        LinearSelector<int> sel = new ([10, 20, 30]) { AllowEmpty = true };
        (runnable as View)?.Add (sel);
        app.Begin (runnable);
        app.LayoutAndDraw ();

        // Resolve the screen position of option index 2 from the view itself.
        Assert.True (sel.TryGetPositionByOption (2, out (int x, int y) pos));
        Point screenPos = sel.ViewportToScreen (new Point (pos.x, pos.y));

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = screenPos });
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = screenPos });

        Assert.Equal (30, sel.Value);
    }

    // Copilot
    [Fact]
    public void Mouse_Drag_Updates_Selection_When_AllowEmpty ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (15, 3);

        IRunnable runnable = new Runnable ();

        // AllowEmpty=true was the case where drag-update did NOT work before the fix.
        LinearSelector<int> sel = new ([10, 20, 30, 40, 50]) { AllowEmpty = true };
        (runnable as View)?.Add (sel);
        app.Begin (runnable);
        app.LayoutAndDraw ();

        // Resolve real screen positions from the view.
        Assert.True (sel.TryGetPositionByOption (0, out (int x, int y) p0));
        Assert.True (sel.TryGetPositionByOption (2, out (int x, int y) p2));
        Assert.True (sel.TryGetPositionByOption (4, out (int x, int y) p4));
        Point s0 = sel.ViewportToScreen (new Point (p0.x, p0.y));
        Point s2 = sel.ViewportToScreen (new Point (p2.x, p2.y));
        Point s4 = sel.ViewportToScreen (new Point (p4.x, p4.y));

        // Press at index 0.
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = s0 });

        // Drag to index 2 — the bug was that focus did NOT advance here when AllowEmpty=true.
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = s2 });
        Assert.Equal (2, sel.FocusedOption);
        Assert.Equal (30, sel.Value);

        // Drag further to index 4.
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = s4 });
        Assert.Equal (4, sel.FocusedOption);
        Assert.Equal (50, sel.Value);

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = s4 });

        // After release the value must remain at the dragged target.
        Assert.Equal (50, sel.Value);
    }
}

using UnitTests;

namespace ViewsTests;

/// <summary>
///     Visual + input-driven tests for <see cref="LinearMultiSelector{T}"/>.
/// </summary>
public class LinearMultiSelectorVisualTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // Copilot
    [Fact]
    public void Renders_Multiple_Set_Options ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (10, 3);

        IRunnable runnable = new Runnable ();
        LinearMultiSelector<string> ms = new (["A", "B", "C"]) { Value = ["A", "C"], AllowEmpty = true };
        (runnable as View)?.Add (ms);
        app.Begin (runnable);

        app.LayoutAndDraw ();

        // Indexes 0 and 2 set; index 1 not set.
        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       """
                                                       █─●─█
                                                       A B C
                                                       """,
                                                       _output,
                                                       app.Driver);
    }

    // Copilot
    [Fact]
    public void Keyboard_Space_Toggles_Selection ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (10, 3);

        IRunnable runnable = new Runnable ();
        LinearMultiSelector<string> ms = new (["A", "B", "C"]) { AllowEmpty = true };
        (runnable as View)?.Add (ms);
        app.Begin (runnable);
        ms.SetFocus ();

        // Toggle index 0 on, then move and toggle index 2 on.
        app.InjectKey (new Key (KeyCode.Space));
        Assert.Equal (["A"], ms.Value);

        app.InjectKey (new Key (KeyCode.CursorRight));
        app.InjectKey (new Key (KeyCode.CursorRight));
        app.InjectKey (new Key (KeyCode.Space));
        Assert.Equal (["A", "C"], ms.Value);

        // Move back and toggle off.
        app.InjectKey (new Key (KeyCode.CursorLeft));
        app.InjectKey (new Key (KeyCode.CursorLeft));
        app.InjectKey (new Key (KeyCode.Space));
        Assert.Equal (["C"], ms.Value);
    }

    // Copilot
    [Fact]
    public void Mouse_Click_Toggles_Selection ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (12, 3);

        IRunnable runnable = new Runnable ();
        LinearMultiSelector<string> ms = new (["A", "B", "C"]) { AllowEmpty = true };
        (runnable as View)?.Add (ms);
        app.Begin (runnable);
        app.LayoutAndDraw ();

        Assert.True (ms.TryGetPositionByOption (1, out (int x, int y) p1));
        Point screen = ms.ViewportToScreen (new Point (p1.x, p1.y));

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = screen });
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = screen });

        Assert.Equal (["B"], ms.Value);
    }
}

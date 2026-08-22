using UnitTests;

namespace ViewsTests;

public class ShortcutDrawingTests (ITestOutputHelper output) : TestDriverBase
{
    // CoPilot - GPT-5.6
    [Fact]
    public void HelpText_Does_Not_Draw_Over_CommandView ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 1);

        Runnable runnable = new () { Width = 20, Height = 1 };
        app.Begin (runnable);

        Shortcut shortcut = new ()
        {
            Title = "_New",
            HelpText = "New file",
            Width = 20,
            Height = 1
        };

        runnable.Add (shortcut);
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (" New       New file", output, app.Driver);
    }
}

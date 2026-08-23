using System.Text;
using UnitTests;

namespace ViewsTests;

public class TextValidateFieldDrawingTests : TestDriverBase
{
    private const string LongText = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // CoPilot - GPT-5.6
    [Fact]
    public void DefaultHeight_LongText_HasOneRowAndDoesNotOverdrawFollowingView ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 3);

        TextRegexProvider provider = new (".*") { Text = LongText, ValidateOnInput = false };
        TextValidateField field = new () { Width = 20, Provider = provider };
        Label nextRow = new () { Y = 1, Text = "NEXT ROW" };
        using Runnable runnable = new () { Width = 20, Height = 3 };

        runnable.Add (field, nextRow);
        SessionToken token = app.Begin (runnable)!;

        try
        {
            app.LayoutAndDraw ();

            Assert.Equal (1, field.Viewport.Height);
            Assert.Equal ("NEXT ROW", GetRow (app.Driver, 1, 20).TrimEnd ());
        }
        finally
        {
            app.End (token);
        }
    }

    // CoPilot - GPT-5.6
    [Fact]
    public void ExplicitHeight_DoesNotDrawWrappedBaseText ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (5, 3);

        TextRegexProvider provider = new (".*") { Text = LongText, ValidateOnInput = false };
        TextValidateField field = new () { Width = 5, Height = 3, Provider = provider };
        using Runnable runnable = new () { Width = 5, Height = 3 };

        runnable.Add (field);
        SessionToken token = app.Begin (runnable)!;

        try
        {
            app.LayoutAndDraw ();

            Assert.Empty (GetRow (app.Driver, 1, 5).TrimEnd ());
            Assert.Empty (GetRow (app.Driver, 2, 5).TrimEnd ());
        }
        finally
        {
            app.End (token);
        }
    }

    private static string GetRow (IDriver driver, int row, int width)
    {
        StringBuilder builder = new ();

        for (int column = 0; column < width; column++)
        {
            builder.Append (driver.Contents! [row, column]!.Grapheme);
        }

        return builder.ToString ();
    }
}

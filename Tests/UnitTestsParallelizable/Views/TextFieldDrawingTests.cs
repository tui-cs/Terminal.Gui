using System.Text;
using UnitTests;

namespace ViewsTests;

public class TextFieldDrawingTests : TestDriverBase
{
    private const string LongText = "C/W Liners, Fabrics, Pipe Connections, 150mm Dia Perf Drain Pipe, Drain Works";

    // CoPilot - GPT-5.6
    [Fact]
    public void DefaultHeight_LongText_HasOneRowOfContent ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 5);

        using Runnable runnable = new () { Width = 30, Height = 5 };
        TextField textField = new ()
        {
            Width = 30,
            Text = LongText,
            BorderStyle = LineStyle.Single
        };

        runnable.Add (textField);
        SessionToken token = app.Begin (runnable)!;

        try
        {
            app.LayoutAndDraw ();

            Assert.Equal (1, textField.Viewport.Height);
            Assert.Equal (3, textField.Frame.Height);
        }
        finally
        {
            app.End (token);
        }
    }

    // CoPilot - GPT-5.6
    [Fact]
    public void LongText_DoesNotOverdrawFollowingView ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 3);

        using Runnable runnable = new () { Width = 30, Height = 3 };
        TextField textField = new () { Width = 30, Text = LongText };
        Label nextRow = new () { Y = 1, Text = "NEXT ROW" };

        runnable.Add (textField, nextRow);
        SessionToken token = app.Begin (runnable)!;

        try
        {
            app.LayoutAndDraw ();

            Assert.Equal ("NEXT ROW", GetRow (app.Driver, 1, 30).TrimEnd ());
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

        using Runnable runnable = new () { Width = 5, Height = 3 };
        TextField textField = new () { Width = 5, Height = 3, Text = "abcdefghij" };

        runnable.Add (textField);
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

using System.Text;
using UnitTests;

namespace DriverTests;

/// <summary>
///     Collection definition for Application Timer Related tests.
///     Tests in this collection run sequentially to avoid conflicts with Application lifecycle and state.
/// </summary>
[CollectionDefinition ("Driver Tests")]
public class DriverTestCollection
{
    // This class is never instantiated - it's just a marker for xUnit
}

[Collection ("Driver Tests")]
public class AllDriverTests (ITestOutputHelper output) : TestDriverBase
{
    [Theory]
    [InlineData ("", true)]
    [InlineData ("a", true)]
    [InlineData ("👩‍❤️‍💋‍👨", false)]
    public void IsValidLocation (string text, bool positive)
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (10, 10);

        // positive
        Assert.True (driver.IsValidLocation (text, 0, 0));
        Assert.True (driver.IsValidLocation (text, 1, 1));
        Assert.Equal (positive, driver.IsValidLocation (text, driver.Cols - 1, driver.Rows - 1));

        // negative
        Assert.False (driver.IsValidLocation (text, -1, 0));
        Assert.False (driver.IsValidLocation (text, 0, -1));
        Assert.False (driver.IsValidLocation (text, -1, -1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows));

        // Define a clip rectangle
        driver.Clip = new Region (new Rectangle (5, 5, 5, 5));

        // positive
        Assert.True (driver.IsValidLocation (text, 5, 5));
        Assert.Equal (positive, driver.IsValidLocation (text, 9, 9));

        // negative
        Assert.False (driver.IsValidLocation (text, 4, 5));
        Assert.False (driver.IsValidLocation (text, 5, 4));
        Assert.False (driver.IsValidLocation (text, 10, 9));
        Assert.False (driver.IsValidLocation (text, 9, 10));
        Assert.False (driver.IsValidLocation (text, -1, 0));
        Assert.False (driver.IsValidLocation (text, 0, -1));
        Assert.False (driver.IsValidLocation (text, -1, -1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows - 1));
        Assert.False (driver.IsValidLocation (text, driver.Cols, driver.Rows));

        driver.Dispose ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void All_Drivers_Init_Dispose_Cross_Platform (string driverName)
    {
        IApplication? app = Application.Create ();
        app.Init (driverName);
        app.Dispose ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void All_Drivers_Run_Cross_Platform (string driverName)
    {
        IApplication? app = Application.Create ();
        app.Init (driverName);
        app.StopAfterFirstIteration = true;
        app.Run<Runnable<bool>> ();
        app.Dispose ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void All_Drivers_LayoutAndDraw_Cross_Platform (string driverName)
    {
        IApplication? app = Application.Create ();
        app.Init (driverName);
        app.StopAfterFirstIteration = true;
        app.Run<TestTop> ();

        DriverAssert.AssertDriverContentsWithFrameAre (driverName!, output, app.Driver);

        app.Dispose ();
    }

    // Tests fix for https://github.com/tui-cs/Terminal.Gui/issues/4258
    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void All_Drivers_When_Clipped_AddStr_Glyph_On_Second_Cell_Of_Wide_Glyph_Outputs_Correctly (string driverName)
    {
        IApplication? app = Application.Create ();
        app.Init (driverName);
        IDriver driver = app.Driver!;
        driver.GetOutputBuffer ().SetWideGlyphReplacement ((Rune)'①');

        // Need to force "windows" driver to override legacy console mode for this test
        driver.IsLegacyConsole = false;
        driver.Force16Colors = false;

        driver.SetScreenSize (6, 3);

        driver!.Clip = new Region (driver.Screen);

        driver.Move (1, 0);
        driver.AddStr ("┌");
        driver.Move (2, 0);
        driver.AddStr ("─");
        driver.Move (3, 0);
        driver.AddStr ("┐");
        driver.Clip.Exclude (new Region (new Rectangle (1, 0, 3, 1)));

        driver.Move (0, 0);
        driver.AddStr ("🍎🍎🍎🍎");

        DriverAssert.AssertDriverContentsAre ("""
                                              ①┌─┐🍎
                                              """,
                                              output,
                                              driver);

        driver.Refresh ();

        DriverAssert.AssertDriverOutputIs (@"\x1b[38;2;0;0;0m\x1b[48;2;0;0;0m①┌─┐🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m", output, driver);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void All_Drivers_Handles_Suspend_Themselves (string driverName)
    {
        // Arrange
        using IApplication? app = Application.Create ().Init (driverName);

        // Act & Assert
        if (Application.GetDefaultKey (Command.Suspend) != Key.Empty)
        {
            Assert.True (app.Keyboard.RaiseKeyDownEvent (Application.GetDefaultKey (Command.Suspend)));
        }
    }
}

public class TestTop : Runnable
{
    /// <inheritdoc/>
    public override void BeginInit ()
    {
        Text = Driver!.GetName ()!;
        BorderStyle = LineStyle.None;
        base.BeginInit ();
    }
}

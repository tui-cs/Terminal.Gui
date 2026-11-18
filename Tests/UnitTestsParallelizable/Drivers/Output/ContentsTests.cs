using System.Text;
using UnitTests;

// Alias Console to MockConsole so we don't accidentally use Console

namespace DriverTests.Output;

[Collection ("Driver Tests")]
public class ContentsTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void AddStr_Combining_Character_1st_Column ()
    {
        IDriver driver = CreateTestDriver ();

        var expected = "\u0301!";
        driver.AddStr ("\u0301!"); // acute accent + exclamation mark
        DriverAssert.AssertDriverContentsAre (expected, output, driver);

        driver.Dispose ();
    }

    [Fact]
    public void AddStr_With_Combining_Characters ()
    {
        IDriver driver = CreateTestDriver ();

        var acuteAccent = new Rune (0x0301); // Combining acute accent (é)
        string combined = "e" + acuteAccent;
        var expected = "é";

        driver.AddStr (combined);
        DriverAssert.AssertDriverContentsAre (expected, output, driver);

        // 3 char combine
        // a + ogonek + acute = <U+0061, U+0328, U+0301> ( ą́ )
        var oGonek = new Rune (0x0328); // Combining ogonek (a small hook or comma shape)
        combined = "a" + oGonek + acuteAccent;
        expected = ("a" + oGonek + acuteAccent).Normalize (NormalizationForm.FormC); // See Issue #2616

        driver.Move (0, 0);
        driver.AddStr (combined);
        DriverAssert.AssertDriverContentsAre (expected, output, driver);

        // e + ogonek + acute = <U+0061, U+0328, U+0301> ( ę́́ )
        combined = "e" + oGonek + acuteAccent;
        expected = ("e" + oGonek + acuteAccent).Normalize (NormalizationForm.FormC); // See Issue #2616

        driver.Move (0, 0);
        driver.AddStr (combined);
        DriverAssert.AssertDriverContentsAre (expected, output, driver);

        // i + ogonek + acute = <U+0061, U+0328, U+0301> ( į́́́ )
        combined = "i" + oGonek + acuteAccent;
        expected = ("i" + oGonek + acuteAccent).Normalize (NormalizationForm.FormC); // See Issue #2616

        driver.Move (0, 0);
        driver.AddStr (combined);
        DriverAssert.AssertDriverContentsAre (expected, output, driver);

        // u + ogonek + acute = <U+0061, U+0328, U+0301> ( ų́́́́ )
        combined = "u" + oGonek + acuteAccent;
        expected = ("u" + oGonek + acuteAccent).Normalize (NormalizationForm.FormC); // See Issue #2616

        driver.Move (0, 0);
        driver.AddStr (combined);
        DriverAssert.AssertDriverContentsAre (expected, output, driver);

        driver.Dispose ();
    }

    [Fact]
    public void Move_Bad_Coordinates ()
    {
        IDriver driver = CreateTestDriver ();

        Assert.Equal (0, driver.Col);
        Assert.Equal (0, driver.Row);

        driver.Move (-1, 0);
        Assert.Equal (-1, driver.Col);
        Assert.Equal (0, driver.Row);

        driver.Move (0, -1);
        Assert.Equal (0, driver.Col);
        Assert.Equal (-1, driver.Row);

        driver.Move (driver.Cols, 0);
        Assert.Equal (driver.Cols, driver.Col);
        Assert.Equal (0, driver.Row);

        driver.Move (0, driver.Rows);
        Assert.Equal (0, driver.Col);
        Assert.Equal (driver.Rows, driver.Row);

        driver.Move (500, 500);
        Assert.Equal (500, driver.Col);
        Assert.Equal (500, driver.Row);
        driver.Dispose ();
    }

    // TODO: Add these unit tests

    // AddRune moves correctly

    // AddRune with wide characters are handled correctly

    // AddRune with wide characters and Col < 0 are handled correctly

    // AddRune with wide characters and Col == Cols - 1 are handled correctly

    // AddRune with wide characters and Col == Cols are handled correctly

    // AddStr moves correctly

    // AddStr with wide characters moves correctly

    // AddStr where Col is negative works

    // AddStr where Col is negative and characters include wide / combining characters works

    // AddStr where Col is near Cols and characters include wide / combining characters works

    // Clipping works correctly

    // Clipping works correctly with wide characters

    // Clipping works correctly with combining characters

    // Clipping works correctly with combining characters and wide characters

    // ResizeScreen works correctly

    // Refresh works correctly

    // IsDirty tests
}

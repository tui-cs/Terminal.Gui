using UnitTests;

namespace ViewBaseTests.Drawing;

public class DrawHotStringTests (ITestOutputHelper output) : TestDriverBase
{
    /// <summary>
    ///     Verifies that <see cref="View.DrawHotString(string, Attribute, Attribute)" /> iterates by grapheme cluster,
    ///     not by rune. When iterating by rune, combining marks (e.g., acute accent U+0301) are sent individually
    ///     via AddRune and fail to compose with their base character. Iterating by grapheme and using AddStr
    ///     ensures the combining mark stays attached to its base.
    /// </summary>
    [Theory]
    [InlineData ("e\u0301", "é")] // e + combining acute → é
    [InlineData ("n\u0303o", "ño")] // n + combining tilde + o → ño
    [InlineData ("Les Mise\u0301rables", "Les Misérables")] // combining acute inside word
    public void DrawHotString_CombiningMarks (string input, string expectedRendered)
    {
        // setup
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);
        var view = new View
        {
            Driver = driver,
            Width = 20, Height = 1
        };

        // execute
        view.DrawHotString (input, Attribute.Default, Attribute.Default);

        // verify
        DriverAssert.AssertDriverContentsWithFrameAre (expectedRendered, output, driver);
    }

    /// <summary>
    ///     Verifies that <see cref="View.DrawHotString(string, Attribute, Attribute)" /> correctly handles
    ///     the hotkey specifier when combined with grapheme clusters. The hotkey specifier ('_') should
    ///     switch to hot color, and subsequent grapheme clusters (including combining marks) should render
    ///     correctly.
    /// </summary>
    [Fact]
    public void DrawHotString_HotkeyWithCombiningMarks ()
    {
        // setup — "_Re\u0301sume\u0301" → hotkey on 'R', combining accents compose correctly
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);
        var view = new View
        {
            Driver = driver,
            Width = 20, Height = 1
        };

        // execute
        var hotColor = new Attribute (Color.Red, Color.Black);
        var normalColor = new Attribute (Color.White, Color.Black);
        view.DrawHotString ("_Re\u0301sume\u0301", hotColor, normalColor);

        // verify — the rendered text should show "Résumé" (without the underscore)
        DriverAssert.AssertDriverContentsWithFrameAre ("Résumé", output, driver);
    }
}

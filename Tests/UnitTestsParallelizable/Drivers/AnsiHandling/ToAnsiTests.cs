using UnitTests;

namespace DriverTests.AnsiHandling;

/// <summary>
///     Tests for the ToAnsi functionality that generates ANSI escape sequences from buffer contents.
/// </summary>
[Collection ("Driver Tests")]
public class ToAnsiTests : TestDriverBase
{
    [Fact]
    public void ToAnsi_Empty_Buffer ()
    {
        IDriver driver = CreateTestDriver (10, 5);
        string ansi = driver.ToAnsi ();

        // Empty buffer should have newlines for each row
        Assert.Contains ("\n", ansi);

        // Should have 5 newlines (one per row)
        Assert.Equal (5, ansi.Count (c => c == '\n'));
    }

    [Fact]
    public void ToAnsi_Simple_Text ()
    {
        IDriver driver = CreateTestDriver (10, 3);
        driver.AddStr ("Hello");
        driver.Move (0, 1);
        driver.AddStr ("World");

        string ansi = driver.ToAnsi ();

        // Should contain the text
        Assert.Contains ("Hello", ansi);
        Assert.Contains ("World", ansi);

        // Should have proper structure with newlines
        string [] lines = ansi.Split (['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal (3, lines.Length);
    }

    [Theory]
    [InlineData (true, "\u001b[31m", "\u001b[34m")]
    [InlineData (false, "\u001b[38;2;255;0;0m", "\u001b[38;2;0;0;255")]
    public void ToAnsi_With_Colors (bool force16Colors, string expectedRed, string expectedBue)
    {
        IDriver driver = CreateTestDriver (10, 2);
        driver.Force16Colors = force16Colors;

        // Set red foreground
        driver.CurrentAttribute = new (Color.Red, Color.Black);
        driver.AddStr ("Red");
        driver.Move (0, 1);

        // Set blue foreground
        driver.CurrentAttribute = new (Color.Blue, Color.Black);
        driver.AddStr ("Blue");

        string ansi = driver.ToAnsi ();

        Assert.True (driver.Force16Colors == force16Colors);

        // Should contain ANSI color codes
        Assert.Contains (expectedRed, ansi); // Red foreground
        Assert.Contains (expectedBue, ansi); // Blue foreground
        Assert.Contains ("Red", ansi);
        Assert.Contains ("Blue", ansi);
    }

    [Theory]
    [InlineData (false, "\u001b[48;2;")]
    [InlineData (true, "\u001b[41m")]
    public void ToAnsi_With_Background_Colors (bool force16Colors, string expected)
    {
        IDriver driver = CreateTestDriver (10, 2);
        driver.Force16Colors = force16Colors;

        // Set background color
        driver.CurrentAttribute = new (Color.White, Color.Red);
        driver.AddStr ("WhiteOnRed");

        string ansi = driver.ToAnsi ();

        /*
         The ANSI escape sequence for red background (8-color) is ESC[41m — where ESC is \x1b (or \u001b).
           Examples:
           •	C# string: "\u001b[41m" or "\x1b[41m"
           •	Reset (clear attributes): "\u001b[0m"
           Notes:
           •	Bright/red background (16-color bright variant) uses ESC[101m ("\u001b[101m").
           •	For 24-bit RGB background use ESC[48;2;<r>;<g>;<b>m, e.g. "\u001b[48;2;255;0;0m" for pure red.
         */

        Assert.True (driver.Force16Colors == force16Colors);

        // Should contain ANSI background color code
        Assert.Contains (expected, ansi); // Red background
        Assert.Contains ("WhiteOnRed", ansi);
    }

    [Fact]
    public void ToAnsi_With_Text_Styles ()
    {
        IDriver driver = CreateTestDriver (10, 3);

        // Bold text
        driver.CurrentAttribute = new (Color.White, Color.Black, TextStyle.Bold);
        driver.AddStr ("Bold");
        driver.Move (0, 1);

        // Italic text
        driver.CurrentAttribute = new (Color.White, Color.Black, TextStyle.Italic);
        driver.AddStr ("Italic");
        driver.Move (0, 2);

        // Underline text
        driver.CurrentAttribute = new (Color.White, Color.Black, TextStyle.Underline);
        driver.AddStr ("Underline");

        string ansi = driver.ToAnsi ();

        // Should contain ANSI style codes
        Assert.Contains ("\u001b[1m", ansi); // Bold
        Assert.Contains ("\u001b[3m", ansi); // Italic
        Assert.Contains ("\u001b[4m", ansi); // Underline
    }

    [Fact]
    public void ToAnsi_With_Wide_Characters ()
    {
        IDriver driver = CreateTestDriver (10, 2);

        // Add a wide character (Chinese character)
        driver.AddStr ("??");
        driver.Move (0, 1);
        driver.AddStr ("??");

        string ansi = driver.ToAnsi ();

        Assert.Contains ("??", ansi);
        Assert.Contains ("??", ansi);
    }

    [Fact]
    public void ToAnsi_With_Unicode_Characters ()
    {
        IDriver driver = CreateTestDriver (10, 2);

        // Add various Unicode characters
        driver.AddStr ("???"); // Greek letters
        driver.Move (0, 1);
        driver.AddStr ("???"); // Emoji

        string ansi = driver.ToAnsi ();

        Assert.Contains ("???", ansi);
        Assert.Contains ("???", ansi);
    }

    [Theory]
    [InlineData (true, "\u001b[31m", "\u001b[34m")]
    [InlineData (false, "\u001b[38;2;", "\u001b[48;2;")]
    public void ToAnsi_Attribute_Changes_Within_Line (bool force16Colors, string expectedRed, string expectedBlue)
    {
        IDriver driver = CreateTestDriver (20, 1);
        driver.Force16Colors = force16Colors;

        driver.AddStr ("Normal");
        driver.CurrentAttribute = new (Color.Red, Color.Black);
        driver.AddStr ("Red");
        driver.CurrentAttribute = new (Color.Blue, Color.Black);
        driver.AddStr ("Blue");

        string ansi = driver.ToAnsi ();

        Assert.True (driver.Force16Colors == force16Colors);

        // Should contain color changes within the line
        Assert.Contains ("Normal", ansi);
        Assert.Contains (expectedRed, ansi); // Red
        Assert.Contains (expectedBlue, ansi); // Blue
    }

    [Fact]
    public void ToAnsi_Large_Buffer ()
    {
        // Test with a larger buffer to stress performance
        IDriver driver = CreateTestDriver (200, 50);

        // Fill with some content
        for (var row = 0; row < 50; row++)
        {
            driver.Move (0, row);
            driver.CurrentAttribute = new ((ColorName16)(row % 16), Color.Black);
            driver.AddStr ($"Row {row:D2} content");
        }

        string ansi = driver.ToAnsi ();

        // Should contain all rows
        Assert.Contains ("Row 00", ansi);
        Assert.Contains ("Row 49", ansi);

        // Should have proper newlines (50 content lines + 50 newlines)
        Assert.Equal (50, ansi.Count (c => c == '\n'));
    }

    [Fact]
    public void ToAnsi_RGB_Colors ()
    {
        IDriver driver = CreateTestDriver (10, 1);

        // Use RGB colors (when not forcing 16 colors)
        driver.Force16Colors = false;

        try
        {
            driver.CurrentAttribute = new (new Color (255, 0), new Color (0, 255));
            driver.AddStr ("RGB");

            string ansi = driver.ToAnsi ();

            // Should contain RGB color codes
            Assert.Contains ("\u001b[38;2;255;0;0m", ansi); // Red foreground RGB
            Assert.Contains ("\u001b[48;2;0;255;0m", ansi); // Green background RGB
        }
        finally
        {
            driver.Force16Colors = true; // Reset
        }
    }

    [Fact]
    public void ToAnsi_Force16Colors ()
    {
        IDriver driver = CreateTestDriver (10, 1);

        // Force 16 colors
        driver.Force16Colors = true;
        driver.CurrentAttribute = new (Color.Red, Color.Blue);
        driver.AddStr ("16Color");

        string ansi = driver.ToAnsi ();

        // Should contain 16-color codes, not RGB
        Assert.Contains ("\u001b[31m", ansi); // Red foreground (16-color)
        Assert.Contains ("\u001b[44m", ansi); // Blue background (16-color)
        Assert.DoesNotContain ("\u001b[38;2;", ansi); // No RGB codes
    }

    [Theory]
    [InlineData (true, "\u001b[31m", "\u001b[32m", "\u001b[34m", "\u001b[33m", "\u001b[35m", "\u001b[36m")]
    [InlineData (
                    false,
                    "\u001b[38;2;255;0;0m",
                    "\u001b[38;2;0;128;0m",
                    "\u001b[38;2;0;0;255",
                    "\u001b[38;2;255;255;0m",
                    "\u001b[38;2;255;0;255m",
                    "\u001b[38;2;0;255;255m")]
    public void ToAnsi_Multiple_Attributes_Per_Line (
        bool force16Colors,
        string expectedRed,
        string expectedGreen,
        string expectedBlue,
        string expectedYellow,
        string expectedMagenta,
        string expectedCyan
    )
    {
        IDriver driver = CreateTestDriver (50, 1);
        driver.Force16Colors = force16Colors;

        // Create a line with many attribute changes
        string [] colors = { "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan" };

        foreach (string colorName in colors)
        {
            Color fg = colorName switch
                       {
                           "Red" => Color.Red,
                           "Green" => Color.Green,
                           "Blue" => Color.Blue,
                           "Yellow" => Color.Yellow,
                           "Magenta" => Color.Magenta,
                           "Cyan" => Color.Cyan,
                           _ => Color.White
                       };

            driver.CurrentAttribute = new (fg, Color.Black);
            driver.AddStr (colorName);
        }

        string ansi = driver.ToAnsi ();

        Assert.True (driver.Force16Colors == force16Colors);

        // Should contain multiple color codes
        Assert.Contains (expectedRed, ansi); // Red
        Assert.Contains (expectedGreen, ansi); // Green
        Assert.Contains (expectedBlue, ansi); // Blue
        Assert.Contains (expectedYellow, ansi); // Yellow
        Assert.Contains (expectedMagenta, ansi); // Magenta
        Assert.Contains (expectedCyan, ansi); // Cyan
    }

    [Fact]
    public void ToAnsi_Special_Characters ()
    {
        IDriver driver = CreateTestDriver (20, 1);

        // Test backslash character
        driver.AddStr ("Backslash:");
        driver.AddRune ('\\');

        string ansi = driver.ToAnsi ();

        Assert.Contains ("Backslash:", ansi);
        Assert.Contains ("\\", ansi);
    }

    [Fact]
    public void ToAnsi_Buffer_Boundary_Conditions ()
    {
        // Test with minimum buffer size
        IDriver driver = CreateTestDriver (1, 1);
        driver.AddStr ("X");

        string ansi = driver.ToAnsi ();

        Assert.Contains ("X", ansi);
        Assert.Contains ("\n", ansi);

        // Test with very wide buffer
        driver = CreateTestDriver (1000, 1);
        driver.AddStr ("Wide");

        ansi = driver.ToAnsi ();

        Assert.Contains ("Wide", ansi);
        Assert.True (ansi.Length > 1000); // Should have many spaces
    }

    [Fact]
    public void ToAnsi_Empty_Lines ()
    {
        IDriver driver = CreateTestDriver (10, 3);

        // Only write to first and third lines
        driver.AddStr ("First");
        driver.Move (0, 2);
        driver.AddStr ("Third");

        string ansi = driver.ToAnsi ();

        string [] lines = ansi.Split ('\n');
        Assert.Equal (4, lines.Length); // 3 content lines + 1 empty line at end
        Assert.Contains ("First", lines [0]);
        Assert.Contains ("Third", lines [2]);
    }

    [Fact]
    public void ToAnsi_Performance_Stress_Test ()
    {
        // Create a large buffer and fill it completely
        const int width = 200;
        const int height = 100;
        IDriver driver = CreateTestDriver (width, height);

        // Fill every cell with different content and colors
        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                driver.Move (col, row);
                driver.CurrentAttribute = new ((ColorName16)((row + col) % 16), Color.Black);
                driver.AddRune ((char)('A' + (row + col) % 26));
            }
        }

        // This should complete in reasonable time and not throw
        string ansi = driver.ToAnsi ();

        Assert.NotNull (ansi);
        Assert.True (ansi.Length > width * height); // Should contain all characters plus ANSI codes
    }
}

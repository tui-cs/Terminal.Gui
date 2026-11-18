using System.Buffers;
using System.Text;
using UnitTests;

namespace DriverTests.Output;

[Collection ("Driver Tests")]
public class AddRuneTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void AddRune ()
    {
        IDriver driver = CreateTestDriver ();

        driver.Rows = 25;
        driver.Cols = 80;
        driver.AddRune (new Rune ('a'));
        Assert.Equal ("a", driver.Contents? [0, 0].Grapheme);

        driver.Dispose ();
    }

    [Fact]
    public void AddRune_Accented_Letter_With_Three_Combining_Unicode_Chars ()
    {
        IDriver driver = CreateTestDriver ();

        var expected = "ắ";

        var text = "\u1eaf";
        driver.AddStr (text);
        Assert.Equal (expected, driver.Contents! [0, 0].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 1].Grapheme);

        driver.ClearContents ();
        driver.Move (0, 0);

        expected = "ắ";
        text = "\u0103\u0301";
        driver.AddStr (text);
        Assert.Equal (expected, driver.Contents [0, 0].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 1].Grapheme);

        driver.ClearContents ();
        driver.Move (0, 0);

        expected = "ắ";
        text = "\u0061\u0306\u0301";
        driver.AddStr (text);
        Assert.Equal (expected, driver.Contents [0, 0].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 1].Grapheme);

        driver.Dispose ();
    }

    [Fact]
    public void AddRune_InvalidLocation_DoesNothing ()
    {
        IDriver driver = CreateTestDriver ();

        driver.Move (driver.Cols, driver.Rows);
        driver.AddRune ('a');

        for (var col = 0; col < driver.Cols; col++)
        {
            for (var row = 0; row < driver.Rows; row++)
            {
                Assert.Equal (" ", driver.Contents? [row, col].Grapheme);
            }
        }

        driver.Dispose ();
    }

    [Fact]
    public void AddRune_MovesToNextColumn ()
    {
        IDriver driver = CreateTestDriver ();

        driver.AddRune ('a');
        Assert.Equal ("a", driver.Contents? [0, 0].Grapheme);
        Assert.Equal (0, driver.Row);
        Assert.Equal (1, driver.Col);

        driver.AddRune ('b');
        Assert.Equal ("b", driver.Contents? [0, 1].Grapheme);
        Assert.Equal (0, driver.Row);
        Assert.Equal (2, driver.Col);

        // Move to the last column of the first row
        int lastCol = driver.Cols - 1;
        driver.Move (lastCol, 0);
        Assert.Equal (0, driver.Row);
        Assert.Equal (lastCol, driver.Col);

        // Add a rune to the last column of the first row; should increment the row or col even though it's now invalid
        driver.AddRune ('c');
        Assert.Equal ("c", driver.Contents? [0, lastCol].Grapheme);
        Assert.Equal (lastCol + 1, driver.Col);

        // Add a rune; should succeed but do nothing as it's outside of Contents
        driver.AddRune ('d');
        Assert.Equal (lastCol + 2, driver.Col);

        for (var col = 0; col < driver.Cols; col++)
        {
            for (var row = 0; row < driver.Rows; row++)
            {
                Assert.NotEqual ("d", driver.Contents? [row, col].Grapheme);
            }
        }

        driver.Dispose ();
    }

    [Fact]
    public void AddRune_MovesToNextColumn_Wide ()
    {
        IDriver driver = CreateTestDriver ();

        // 🍕 Slice of Pizza "\U0001F355"
        OperationStatus operationStatus = Rune.DecodeFromUtf16 ("\U0001F355", out Rune rune, out int charsConsumed);
        Assert.Equal (OperationStatus.Done, operationStatus);
        Assert.Equal (charsConsumed, rune.Utf16SequenceLength);
        Assert.Equal (2, rune.GetColumns ());

        driver.AddRune (rune);
        Assert.Equal (rune.ToString (), driver.Contents? [0, 0].Grapheme);
        Assert.Equal (0, driver.Row);
        Assert.Equal (2, driver.Col);

        driver.Dispose ();
    }

    [Fact]
    public void AddStr_Glyph_On_Second_Cell_Of_Wide_Glyph_Outputs_Correctly ()
    {
        IDriver? driver = CreateTestDriver ();
        driver.SetScreenSize (6, 3);
        driver.GetOutputBuffer ().SetWideGlyphReplacement ((Rune)'①');

        driver.Clip = new (driver.Screen);
        driver.Move (1, 0);
        driver.AddStr ("┌");
        driver.Move (2, 0);
        driver.AddStr ("─");
        driver.Move (3, 0);
        driver.AddStr ("┐");
        driver.Clip.Exclude (new Region (new (1, 0, 3, 1)));

        driver.Move (0, 0);
        driver.AddStr ("🍎🍎🍎🍎");

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              ①┌─┐🍎
                                              """,
                                              output,
                                              driver);

        driver.Refresh ();

        DriverAssert.AssertDriverOutputIs (@"\x1b[38;2;0;0;0m\x1b[48;2;0;0;0m①┌─┐🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m",
                                           output, driver);
    }

    [Fact]
    public void AddStr_WideGlyph_Second_Column_Attribute_Set_When_In_Clip ()
    {
        // This test verifies the fix for issue #4258
        // When a wide glyph is added and the second column is within the clip region,
        // the attribute for column N+1 should be set to match the current attribute.
        // See: OutputBufferImpl.cs line 194
        using IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (4, 2);

        // Set a specific attribute for the wide glyph
        Attribute wideGlyphAttr = new (Color.BrightRed, Color.BrightYellow);
        driver.CurrentAttribute = wideGlyphAttr;

        // Add a wide glyph at position (0, 0)
        driver.Move (0, 0);
        driver.AddStr ("🍎");

        // Verify the wide glyph is in column 0
        Assert.Equal ("🍎", driver.Contents! [0, 0].Grapheme);
        Assert.Equal (wideGlyphAttr, driver.Contents [0, 0].Attribute);

        // Verify column 1 (the second column of the wide glyph) has the correct attribute set
        // This is the fix: column N+1 should have CurrentAttribute set (line 194 in OutputBufferImpl.cs)
        Assert.Equal (wideGlyphAttr, driver.Contents [0, 1].Attribute);

        // Verify cursor moved to column 2
        Assert.Equal (2, driver.Col);
    }

    [Fact]
    public void AddStr_WideGlyph_Second_Column_Attribute_Not_Set_When_Outside_Clip ()
    {
        // This test verifies that when a wide glyph's second column is outside the clip,
        // the attribute for column N+1 is NOT modified
        using IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (4, 2);

        // Set initial attribute for the entire contents
        Attribute initialAttr = new (Color.White, Color.Black);
        driver.CurrentAttribute = initialAttr;
        driver.Move (0, 0);
        driver.AddStr ("    ");
        driver.Move (0, 1);
        driver.AddStr ("    ");

        // Create a clip that excludes column 1
        driver.Clip = new (new Rectangle (0, 0, 1, 2));

        // Set a different attribute for the wide glyph
        Attribute wideGlyphAttr = new (Color.BrightRed, Color.BrightYellow);
        driver.CurrentAttribute = wideGlyphAttr;

        // Try to add a wide glyph at position (0, 0)
        // Column 0 is in clip, but column 1 is NOT
        driver.Move (0, 0);
        driver.AddStr ("🍎");

        // Verify column 0 has the replacement character (can't fit wide glyph)
        Assert.NotEqual ("🍎", driver.Contents! [0, 0].Grapheme);

        // Verify column 1 still has the original attribute (NOT modified)
        Assert.Equal (initialAttr, driver.Contents [0, 1].Attribute);
    }

    [Fact]
    public void AddStr_WideGlyph_Second_Column_Attribute_Outputs_Correctly ()
    {
        // This test verifies the fix for issue #4258 by checking the actual driver output
        // This mimics what happens when TransparentShadow redraws a wide glyph from ScreenContents
        // WITHOUT line 194, column N+1's attribute doesn't get set, causing wrong colors in output
        // See: OutputBufferImpl.cs line ~196 (Contents [Row, Col].Attribute = CurrentAttribute;)
        using IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (3, 1);
        driver.Force16Colors = true;

        // Step 1: Draw initial content - a wide glyph at column 1 with white-on-black
        driver.CurrentAttribute = new Attribute (Color.White, Color.Black);
        driver.Move (1, 0);
        driver.AddStr ("🍎X");  // Wide glyph at columns 1-2, 'X' at column 3 doesn't exist (off-screen)

        // At this point:
        // - Column 0: space (default) with white-on-black
        // - Column 1: 🍎 with white-on-black
        // - Column 2: (part of 🍎) with white-on-black (from initial ClearContents)

        // Step 2: Now redraw the SAME wide glyph at column 1 but with a DIFFERENT attribute (red-on-yellow)
        // This simulates what transparent shadow does - it redraws what's underneath with a dimmed attribute
        driver.CurrentAttribute = new Attribute (Color.BrightRed, Color.BrightYellow);
        driver.Move (1, 0);
        driver.AddStr ("🍎");

        // Verify internal state
        Assert.Equal ("🍎", driver.Contents! [0, 1].Grapheme);
        Assert.Equal (new Attribute (Color.BrightRed, Color.BrightYellow), driver.Contents [0, 1].Attribute);

        // THIS is the critical assertion - column 2's attribute MUST be red-on-yellow
        // WITHOUT line 194: column 2 retains white-on-black
        // WITH line 194: column 2 gets red-on-yellow
        Assert.Equal (new Attribute (Color.BrightRed, Color.BrightYellow), driver.Contents [0, 2].Attribute);

        driver.Refresh ();

        // Expected output:
        // Column 0: space with white-on-black
        // Columns 1-2: 🍎 with red-on-yellow (both columns must have same attribute!)
        //
        // WITHOUT line 196, the output would be:
        // \x1b[97m\x1b[40m  (white-on-black for column 0)
        // \x1b[91m\x1b[103m🍎 (red-on-yellow starts at column 1)
        // \x1b[97m\x1b[40m (WRONG! Attribute changes mid-glyph because column 2 still has white-on-black)
        //
        // WITH line 196, the output is:
        // \x1b[97m\x1b[40m  (white-on-black for column 0)
        // \x1b[91m\x1b[103m🍎 (red-on-yellow for both columns 1 and 2)
        DriverAssert.AssertDriverOutputIs (
            "\x1b[97m\x1b[40m \x1b[91m\x1b[103m🍎",
            output,
            driver);
    }

    [Fact]
    public void AddStr_WideGlyph_Continuation_Cell_Dirty_On_Attribute_Change ()
    {
        // Verifies fix for #4780: when a wide character is redrawn with a different attribute
        // on a SECOND frame (after Refresh has cleared dirty flags), the continuation cell
        // must be marked dirty so the attribute change is flushed to the terminal.
        //
        // Without the fix, after the first Refresh clears IsDirty, redrawing the same glyph
        // with a new attribute updates column N but the continuation cell (N+1) is not marked
        // dirty. The second Refresh then skips it, leaving a stale attribute mid-glyph.
        using IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (4, 1);
        driver.Force16Colors = true;

        // Frame 1: Draw a wide glyph with white-on-black
        driver.CurrentAttribute = new Attribute (Color.White, Color.Black);
        driver.Move (0, 0);
        driver.AddStr ("🍎ab");

        driver.Refresh ();

        // Frame 2: Redraw the SAME glyph with a new attribute (simulates highlight/selection change)
        driver.CurrentAttribute = new Attribute (Color.BrightRed, Color.BrightYellow);
        driver.Move (0, 0);
        driver.AddStr ("🍎");

        // The continuation cell (column 1) must be dirty for Refresh to output the new attribute
        Assert.True (driver.Contents! [0, 1].IsDirty,
            "Continuation cell must be dirty when attribute changes");
        Assert.Equal (new Attribute (Color.BrightRed, Color.BrightYellow),
            driver.Contents [0, 1].Attribute);

        driver.Refresh ();

        // The second Refresh must output the attribute change for the wide glyph.
        // Without the fix, column 1 is not dirty, so Refresh skips it and the output
        // has a stale attribute change mid-glyph or omits the glyph entirely.
        DriverAssert.AssertDriverOutputIs (
            "\x1b[91m\x1b[103m🍎",
            output,
            driver);
    }
}

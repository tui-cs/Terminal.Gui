using System.Text;

namespace DriverTests.Output;

/// <summary>
///     Tests for https://github.com/tui-cs/Terminal.Gui/issues/4466.
///     These tests validate that FillRect properly handles wide characters when overlapping existing content.
///     Specifically, they ensure that wide characters are properly invalidated and replaced when a MessageBox border or
///     similar UI element is drawn over them, preventing visual corruption.
/// </summary>
[Collection ("Driver Tests")]
public class OutputBufferWideCharTests
{
    /// <summary>
    ///     Tests that FillRect properly invalidates wide characters when overwriting them.
    ///     This is the core issue in #4466 - when a MessageBox border is drawn over Chinese text,
    ///     the wide characters need to be properly invalidated.
    /// </summary>
    [Fact]
    [Trait ("Category", "Output")]
    public void FillRect_OverwritesWideChar_InvalidatesProperly ()
    {
        // Arrange - Create a buffer and draw a wide character
        OutputBufferImpl buffer = new ()
        {
            Rows = 5, Cols = 10,
            CurrentAttribute = new (Color.White, Color.Black)
        };

        // Draw a Chinese character (2 columns wide) at position 2,1
        buffer.Move (2, 1);
        buffer.AddStr ("你"); // Chinese character "you", 2 columns wide

        // Verify the wide character was drawn
        Assert.Equal ("你", buffer.Contents! [1, 2].Grapheme);
        Assert.True (buffer.Contents [1, 2].IsDirty);

        // With the fix, the second column should NOT be modified by AddStr
        // The wide glyph naturally renders across both columns
        Assert.NotEqual ("你", buffer.Contents [1, 3].Grapheme);

        // Clear dirty flags to test FillRect behavior
        for (var r = 0; r < buffer.Rows; r++)
        {
            for (var c = 0; c < buffer.Cols; c++)
            {
                buffer.Contents [r, c].IsDirty = false;
            }
        }

        // Act - Fill a rectangle that overlaps the first column of the wide character
        // This simulates drawing a MessageBox border over Chinese text
        buffer.FillRect (new (2, 1, 1, 1), new Rune ('│'));

        // Assert

        // With FIXES_4466: FillRect calls AddStr, which properly invalidates the wide character
        // The wide character at [1,2] should be replaced with replacement char or the new content
        Assert.Equal ("│", buffer.Contents [1, 2].Grapheme);
        Assert.True (buffer.Contents [1, 2].IsDirty, "Cell [1,2] should be marked dirty after FillRect");

        // The adjacent cell should also be marked dirty for proper rendering
        Assert.True (buffer.Contents [1, 3].IsDirty, "Adjacent cell [1,3] should be marked dirty to ensure proper rendering");
    }

    /// <summary>
    ///     Tests that FillRect handles overwriting the second column of a wide character.
    ///     When drawing at an odd column that's the second half of a wide glyph, the
    ///     wide glyph should be invalidated.
    /// </summary>
    [Fact]
    [Trait ("Category", "Output")]
    public void FillRect_OverwritesSecondColumnOfWideChar_InvalidatesWideChar ()
    {
        // Arrange
        OutputBufferImpl buffer = new ()
        {
            Rows = 5, Cols = 10,
            CurrentAttribute = new (Color.White, Color.Black)
        };

        // Draw a wide character at position 2,1
        buffer.Move (2, 1);
        buffer.AddStr ("好"); // Chinese character, 2 columns wide

        Assert.Equal ("好", buffer.Contents! [1, 2].Grapheme);

        // Clear dirty flags
        for (var r = 0; r < buffer.Rows; r++)
        {
            for (var c = 0; c < buffer.Cols; c++)
            {
                buffer.Contents [r, c].IsDirty = false;
            }
        }

        // Act - Fill at the second column of the wide character (position 3)
        buffer.FillRect (new (3, 1, 1, 1), new Rune ('│'));

        // Assert
        // With the fix: The original wide character at col 2 should be invalidated
        // because we're overwriting its second column
        Assert.True (buffer.Contents [1, 2].IsDirty, "Wide char at col 2 should be invalidated when its second column is overwritten");
        Assert.Equal (buffer.Contents [1, 2].Grapheme, Glyphs.WideGlyphReplacement.ToString ());

        Assert.Equal ("│", buffer.Contents [1, 3].Grapheme);
        Assert.True (buffer.Contents [1, 3].IsDirty);
    }

    /// <summary>
    ///     Tests the ChineseUI scenario: Drawing a MessageBox with borders over Chinese button text.
    ///     This simulates the specific repro case from the issue. See: https://github.com/tui-cs/Terminal.Gui/issues/4466
    /// </summary>
    [Fact]
    [Trait ("Category", "Output")]
    public void ChineseUI_MessageBox_Over_WideChars ()
    {
        // Arrange - Simulate the ChineseUI scenario
        OutputBufferImpl buffer = new ()
        {
            Rows = 10, Cols = 30,
            CurrentAttribute = new (Color.White, Color.Black)
        };

        // Draw Chinese button text (like "你好呀")
        buffer.Move (5, 3);
        buffer.AddStr ("你好呀"); // 3 Chinese characters, 6 columns total

        // Verify initial state
        Assert.Equal ("你", buffer.Contents! [3, 5].Grapheme);
        Assert.Equal ("好", buffer.Contents [3, 7].Grapheme);
        Assert.Equal ("呀", buffer.Contents [3, 9].Grapheme);

        // Clear dirty flags to simulate the state before MessageBox draws
        for (var r = 0; r < buffer.Rows; r++)
        {
            for (var c = 0; c < buffer.Cols; c++)
            {
                buffer.Contents [r, c].IsDirty = false;
            }
        }

        // Act - Draw a MessageBox border that partially overlaps the Chinese text
        // This simulates the mouse moving over the border, causing HighlightState changes
        // Draw vertical line at column 8 (overlaps second char "好")
        for (var row = 2; row < 6; row++)
        {
            buffer.FillRect (new (8, row, 1, 1), new Rune ('│'));
        }

        // Assert - The wide characters should be properly handled
        // With the fix: Wide characters are properly invalidated
        // The first character "你" at col 5 should be unaffected
        Assert.Equal ("你", buffer.Contents [3, 5].Grapheme);

        // The second character "好" at col 7 had its second column overwritten
        // so it should be replaced with replacement char
        Assert.Equal (buffer.Contents [3, 7].Grapheme, Glyphs.WideGlyphReplacement.ToString ());
        Assert.True (buffer.Contents [3, 7].IsDirty, "Invalidated wide char should be marked dirty");

        // The border should be drawn at col 8
        Assert.Equal ("│", buffer.Contents [3, 8].Grapheme);
        Assert.True (buffer.Contents [3, 8].IsDirty);

        // The third character "呀" at col 9 should be unaffected
        Assert.Equal ("呀", buffer.Contents [3, 9].Grapheme);
    }

    /// <summary>
    ///     Tests that FillRect works correctly with single-width characters (baseline behavior).
    ///     This should work the same with or without FIXES_4466.
    /// </summary>
    [Fact]
    [Trait ("Category", "Output")]
    public void FillRect_SingleWidthChars_WorksCorrectly ()
    {
        // Arrange
        OutputBufferImpl buffer = new ()
        {
            Rows = 5, Cols = 10,
            CurrentAttribute = new (Color.White, Color.Black)
        };

        // Draw some ASCII text
        buffer.Move (2, 1);
        buffer.AddStr ("ABC");

        Assert.Equal ("A", buffer.Contents! [1, 2].Grapheme);
        Assert.Equal ("B", buffer.Contents [1, 3].Grapheme);
        Assert.Equal ("C", buffer.Contents [1, 4].Grapheme);

        // Clear dirty flags
        for (var r = 0; r < buffer.Rows; r++)
        {
            for (var c = 0; c < buffer.Cols; c++)
            {
                buffer.Contents [r, c].IsDirty = false;
            }
        }

        // Act - Overwrite with FillRect
        buffer.FillRect (new (3, 1, 1, 1), new Rune ('X'));

        // Assert - This should work the same regardless of FIXES_4466
        Assert.Equal ("A", buffer.Contents [1, 2].Grapheme);
        Assert.Equal ("X", buffer.Contents [1, 3].Grapheme);
        Assert.True (buffer.Contents [1, 3].IsDirty);
        Assert.Equal ("C", buffer.Contents [1, 4].Grapheme);
    }

    /// <summary>
    ///     Tests FillRect with wide characters at buffer boundaries.
    /// </summary>
    [Fact]
    [Trait ("Category", "Output")]
    public void FillRect_WideChar_AtBufferBoundary ()
    {
        // Arrange
        OutputBufferImpl buffer = new ()
        {
            Rows = 5, Cols = 10,
            CurrentAttribute = new (Color.White, Color.Black)
        };

        // Draw a wide character at the right edge (col 8, which would extend to col 9)
        buffer.Move (8, 1);
        buffer.AddStr ("山"); // Chinese character "mountain", 2 columns wide

        Assert.Equal ("山", buffer.Contents! [1, 8].Grapheme);

        // Clear dirty flags
        for (var r = 0; r < buffer.Rows; r++)
        {
            for (var c = 0; c < buffer.Cols; c++)
            {
                buffer.Contents [r, c].IsDirty = false;
            }
        }

        // Act - FillRect at the wide character position
        buffer.FillRect (new (8, 1, 1, 1), new Rune ('│'));

        // Assert
        Assert.Equal ("│", buffer.Contents [1, 8].Grapheme);
        Assert.True (buffer.Contents [1, 8].IsDirty);

        // Adjacent cell should be marked dirty
        Assert.True (
                     buffer.Contents [1, 9].IsDirty,
                     "Cell after wide char replacement should be marked dirty");
    }

    /// <summary>
    ///     Tests OutputBase.Write method marks cells dirty correctly for wide characters.
    ///     This tests the other half of the fix in OutputBase.cs.
    /// </summary>
    [Fact]
    [Trait ("Category", "Output")]
    public void OutputBase_Write_WideChar_MarksCellsDirty ()
    {
        // Arrange
        OutputBufferImpl buffer = new ()
        {
            Rows = 5, Cols = 20,
            CurrentAttribute = new (Color.White, Color.Black)
        };

        // Draw a line with wide characters
        buffer.Move (0, 1);
        buffer.AddStr ("你好"); // Two wide characters

        // Mark all as not dirty to simulate post-Write state
        for (var r = 0; r < buffer.Rows; r++)
        {
            for (var c = 0; c < buffer.Cols; c++)
            {
                buffer.Contents! [r, c].IsDirty = false;
            }
        }

        // Verify initial state
        Assert.Equal ("你", buffer.Contents! [1, 0].Grapheme);
        Assert.Equal ("好", buffer.Contents [1, 2].Grapheme);

        // Act - Now overwrite the first wide char by writing at its position
        buffer.Move (0, 1);
        buffer.AddStr ("A"); // Single width char

        // Assert
        // With the fix: The first cell is replaced with 'A' and marked dirty
        Assert.Equal ("A", buffer.Contents [1, 0].Grapheme);
        Assert.True (buffer.Contents [1, 0].IsDirty);

        // The adjacent cell (col 1) should be marked dirty for proper rendering
        Assert.True (
                     buffer.Contents [1, 1].IsDirty,
                     "Adjacent cell should be marked dirty after writing single-width char over wide char");

        // The second wide char should remain
        Assert.Equal ("好", buffer.Contents [1, 2].Grapheme);
    }

    /// <summary>
    ///     Tests that filling a rectangle with spaces properly handles wide character cleanup.
    ///     This simulates clearing a region that contains wide characters.
    /// </summary>
    [Fact]
    [Trait ("Category", "Output")]
    public void FillRect_WithSpaces_OverWideChars ()
    {
        // Arrange
        OutputBufferImpl buffer = new ()
        {
            Rows = 5, Cols = 15,
            CurrentAttribute = new (Color.White, Color.Black)
        };

        // Draw a line of mixed content
        buffer.Move (2, 2);
        buffer.AddStr ("A你B好C");

        // Verify setup
        Assert.Equal ("A", buffer.Contents! [2, 2].Grapheme);
        Assert.Equal ("你", buffer.Contents [2, 3].Grapheme);
        Assert.Equal ("B", buffer.Contents [2, 5].Grapheme);
        Assert.Equal ("好", buffer.Contents [2, 6].Grapheme);
        Assert.Equal ("C", buffer.Contents [2, 8].Grapheme);

        // Clear dirty flags
        for (var r = 0; r < buffer.Rows; r++)
        {
            for (var c = 0; c < buffer.Cols; c++)
            {
                buffer.Contents [r, c].IsDirty = false;
            }
        }

        // Act - Fill the region with spaces (simulating clearing)
        buffer.FillRect (new (3, 2, 4, 1), new Rune (' '));

        // Assert
        // With the fix: Wide characters are properly handled
        Assert.Equal (" ", buffer.Contents [2, 3].Grapheme);
        Assert.True (buffer.Contents [2, 3].IsDirty);

        // Wide character '你' at col 3 was replaced, so col 4 should be marked dirty
        Assert.True (
                     buffer.Contents [2, 4].IsDirty,
                     "Cell after replaced wide char should be dirty");

        Assert.Equal (" ", buffer.Contents [2, 4].Grapheme);
        Assert.Equal (" ", buffer.Contents [2, 5].Grapheme);
        Assert.Equal (" ", buffer.Contents [2, 6].Grapheme);

        // Cell 7 should be dirty because '好' was partially overwritten
        Assert.True (
                     buffer.Contents [2, 7].IsDirty,
                     "Adjacent cell should be dirty after wide char replacement");
    }

    /// <summary>
    ///     Tests the edge case where a wide character's first column is outside the clip region
    ///     but the second column is inside.
    ///     IMPORTANT: This test documents that the code path in WriteWideGrapheme where:
    ///     - !Clip.Contains(col, row) is true (first column outside)
    ///     - Clip.Contains(col + 1, row) is true (second column inside)
    ///     is CURRENTLY UNREACHABLE because IsValidLocation checks Clip.Contains(col, row) and
    ///     returns false before WriteWideGrapheme is called. This test verifies the current behavior
    ///     (nothing is written when first column is outside clip).
    ///     If the behavior should change to write the second column with a replacement character,
    ///     the logic in IsValidLocation or AddGrapheme needs to be modified.
    /// </summary>
    [Fact]
    [Trait ("Category", "Output")]
    public void AddStr_WideChar_FirstColumnOutsideClip_SecondColumnInside_CurrentBehavior ()
    {
        // Arrange
        OutputBufferImpl buffer = new ()
        {
            Rows = 5,
            Cols = 10,
            CurrentAttribute = new (Color.White, Color.Black)
        };

        // Set custom replacement characters to verify they're being used
        Rune customColumn1Replacement = new ('◄');
        Rune customColumn2Replacement = new ('►');
        buffer.SetWideGlyphReplacement (customColumn1Replacement);

        // Set clip region that starts at column 3 (odd column)
        // This creates a scenario where col 2 is outside clip, but col 3 is inside
        buffer.Clip = new (new (3, 1, 5, 3));

        // Clear initial contents to ensure clean state
        for (var r = 0; r < buffer.Rows; r++)
        {
            for (var c = 0; c < buffer.Cols; c++)
            {
                buffer.Contents! [r, c].IsDirty = false;
                buffer.Contents [r, c].Grapheme = " ";
            }
        }

        // Act - Try to draw a wide character at column 2
        // Column 2 is outside clip, but column 3 is inside clip
        buffer.Move (2, 1);
        buffer.AddStr ("你"); // Chinese character "you", 2 columns wide

        // Assert
        // CURRENT BEHAVIOR: IsValidLocation returns false when col 2 is outside clip,
        // so NOTHING is written - neither column 2 nor column 3
        Assert.Equal (" ", buffer.Contents! [1, 2].Grapheme);
        Assert.False (buffer.Contents [1, 2].IsDirty, "Cell outside clip should not be marked dirty");

        // Column 3 is also not written because IsValidLocation returned false
        // The code path in WriteWideGrapheme that would write the replacement char
        // to column 3 is never reached
        Assert.Equal (" ", buffer.Contents [1, 3].Grapheme);

        Assert.False (
                      buffer.Contents [1, 3].IsDirty,
                      "Currently, second column is not written when first column is outside clip");

        // Verify Col has been advanced by only 1 (not by the wide character width)
        // because the grapheme was not validated/processed when IsValidLocation returned false
        Assert.Equal (3, buffer.Col);
    }

    /// <summary>
    ///     Tests the complementary case: wide character's second column is outside clip
    ///     but first column is inside. This should use the column 1 replacement character.
    /// </summary>
    [Fact]
    [Trait ("Category", "Output")]
    public void AddStr_WideChar_SecondColumnOutsideClip_FirstColumnInside_UsesColumn1Replacement ()
    {
        // Arrange
        OutputBufferImpl buffer = new ()
        {
            Rows = 5,
            Cols = 10,
            CurrentAttribute = new (Color.White, Color.Black)
        };

        // Set custom replacement characters
        Rune customColumn1Replacement = new ('◄');
        Rune customColumn2Replacement = new ('►');
        buffer.SetWideGlyphReplacement (customColumn1Replacement);

        // Set clip region that ends at column 6 (even column)
        // This creates a scenario where col 5 is inside, but col 6 is outside
        buffer.Clip = new (new (0, 1, 6, 3));

        // Clear initial contents
        for (var r = 0; r < buffer.Rows; r++)
        {
            for (var c = 0; c < buffer.Cols; c++)
            {
                buffer.Contents! [r, c].IsDirty = false;
                buffer.Contents [r, c].Grapheme = " ";
            }
        }

        // Act - Try to draw a wide character at column 5
        // Column 5 is inside clip, but column 6 is outside clip
        buffer.Move (5, 1);
        buffer.AddStr ("好"); // Chinese character, 2 columns wide

        // Assert
        // The first column (col 5) is inside clip but second column (6) is outside
        // Should use column 1 replacement char to indicate it can't fit
        Assert.Equal (
                      customColumn1Replacement.ToString (),
                      buffer.Contents! [1, 5].Grapheme);

        Assert.True (
                     buffer.Contents [1, 5].IsDirty,
                     "First column should be marked dirty with replacement char when second column is clipped");

        // The second column is outside clip boundaries entirely
        Assert.Equal (" ", buffer.Contents [1, 6].Grapheme);
        Assert.False (buffer.Contents [1, 6].IsDirty, "Cell outside clip should not be modified");

        // Verify Col has been advanced by 2 (wide character width)
        Assert.Equal (7, buffer.Col);
    }

    /// <summary>
    ///     Tests that when both columns of a wide character are inside the clip,
    ///     the character is drawn normally without replacement characters.
    /// </summary>
    [Fact]
    [Trait ("Category", "Output")]
    public void AddStr_WideChar_BothColumnsInsideClip_DrawsNormally ()
    {
        // Arrange
        OutputBufferImpl buffer = new ()
        {
            Rows = 5,
            Cols = 10,
            CurrentAttribute = new (Color.White, Color.Black)
        };

        // Set custom replacement characters (should NOT be used in this case)
        Rune customColumn1Replacement = new ('◄');
        Rune customColumn2Replacement = new ('►');
        buffer.SetWideGlyphReplacement (customColumn1Replacement);

        // Set clip region that includes columns 2-7
        buffer.Clip = new (new (2, 1, 6, 3));

        // Clear initial contents
        for (var r = 0; r < buffer.Rows; r++)
        {
            for (var c = 0; c < buffer.Cols; c++)
            {
                buffer.Contents! [r, c].IsDirty = false;
                buffer.Contents [r, c].Grapheme = " ";
            }
        }

        // Act - Draw a wide character at column 4 (both 4 and 5 are inside clip)
        buffer.Move (4, 1);
        buffer.AddStr ("山"); // Chinese character "mountain", 2 columns wide

        // Assert
        // Both columns are inside clip, so the wide character should be drawn normally
        Assert.Equal ("山", buffer.Contents! [1, 4].Grapheme);
        Assert.True (buffer.Contents [1, 4].IsDirty, "First column should be marked dirty");

        // The second column should NOT be marked dirty by WriteWideGrapheme
        // The wide glyph naturally renders across both columns without modifying column N+1
        // See: https://github.com/tui-cs/Terminal.Gui/issues/4258
        Assert.False (
                      buffer.Contents [1, 5].IsDirty,
                      "Adjacent cell should NOT be marked dirty when writing wide char (see #4258)");

        // Verify no replacement characters were used
        Assert.NotEqual (customColumn1Replacement.ToString (), buffer.Contents [1, 4].Grapheme);
        Assert.NotEqual (customColumn2Replacement.ToString (), buffer.Contents [1, 5].Grapheme);

        // Verify Col has been advanced by 2
        Assert.Equal (6, buffer.Col);
    }
}

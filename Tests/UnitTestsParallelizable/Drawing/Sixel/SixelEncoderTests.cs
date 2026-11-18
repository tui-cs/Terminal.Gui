
namespace DrawingTests;

public class SixelEncoderTests
{
    [Fact]
    public void EncodeSixel_RedSquare12x12_ReturnsExpectedSixel ()
    {
        string expected = "\u001bP" // Start sixel sequence
                          + "0;0;0" // Defaults for aspect ratio and grid size
                          + "q" // Signals beginning of sixel image data
                          + "\"1;1;12;12" // no scaling factors (1x1) and filling 12x12 pixel area
                          /*
                           * Definition of the color palette
                           * #<index>;<type>;<R>;<G>;<B>" - 2 means RGB. The values range 0 to 100
                           */
                          + "#0;2;100;0;0" // Red color definition
                          /*
                           * Start of the Pixel data
                           * We draw 6 rows at once, so end up with 2 'lines'
                           * Both are basically the same and terminate with dollar hyphen (except last row)
                           * Format is:
                           *     #0 (selects to use color palette index 0 i.e. red)
                           *     !12 (repeat next byte 12 times i.e. the whole length of the row)
                           *     ~ (the byte 111111 i.e. fill completely)
                           *     $ (return to start of line)
                           *     - (move down to next line)
                           */
                          + "#0!12~-"
                          + "#0!12~" // Next 6 rows of red pixels (no trailing $ — last color layer)
                          + "\u001b\\"; // End sixel sequence

        // Arrange: Create a 12x12 bitmap filled with red
        Color [,] pixels = new Color [12, 12];

        for (var x = 0; x < 12; x++)
        {
            for (var y = 0; y < 12; y++)
            {
                pixels [x, y] = new (255, 0);
            }
        }

        // Act: Encode the image
        var encoder = new SixelEncoder (); // Assuming SixelEncoder is the class that contains the EncodeSixel method
        string result = encoder.EncodeSixel (pixels);

        // Since image is only red we should only have 1 color definition
        Color c1 = Assert.Single (encoder.Quantizer.Palette);

        Assert.Equal (new (255, 0), c1);
        Assert.Equal (expected, result);
    }

    [Fact]
    public void EncodeSixel_12x12GridPattern3x3_ReturnsExpectedSixel ()
    {
        /*
         * Each block is a 3x3 square, alternating black and white.
         * The pattern alternates between rows, creating a checkerboard.
         * We have 4 blocks per row, and this repeats over 12x12 pixels.
         *
         * ███...███...
         * ███...███...
         * ███...███...
         * ...███...███
         * ...███...███
         * ...███...███
         * ███...███...
         * ███...███...
         * ███...███...
         * ...███...███
         * ...███...███
         * ...███...███
         *
         * Because we are dealing with sixels (drawing 6 rows at once), we will
         * see 2 bands being drawn. We will also see how we have to 'go back over'
         * the current line after drawing the black (so we can draw the white).
         */

        string expected = "\u001bP" // Start sixel sequence
                          + "0;0;0" // Defaults for aspect ratio and grid size
                          + "q" // Signals beginning of sixel image data
                          + "\"1;1;12;12" // no scaling factors (1x1) and filling 12x12 pixel area
                          /*
                           * Definition of the color palette
                           */
                          + "#0;2;0;0;0" // Black color definition (index 0: RGB 0,0,0)
                          + "#1;2;100;100;100" // White color definition (index 1: RGB 100,100,100)
                          /*
                           * Start of the Pixel data
                           * 
                           * Lets consider only the first 6 pixel (vertically). We have to fill the top 3 black and bottom 3 white.
                           * So we need to select black and fill 000111. To convert this into a character we must +63 and convert to ASCII.
                           * Later on we will also need to select white and fill the inverse, i.e. 111000.
                           *
                           * 111000 (binary) → w (ASCII 119).
                           * 000111 (binary) → F (ASCII 70).
                           *
                           * Therefore the lines become
                           *
                           *   #0 (Select black)
                           *   FFF (fill first 3 pixels horizontally - and top half of band black)
                           *   www (fill next 3 pixels horizontally - bottom half of band black)
                           *   FFFwww (as above to finish the line)
                           *
                           * Next we must go back and fill the white (on the same band)
                           *   #1 (Select white)
                           */
                          + "#0FFFwwwFFFwww$" // First pass of top band (Filling black)
                          + "#1wwwFFFwwwFFF-" // Second pass of top band (Filling white) — no trailing $ on last color layer
                                              // Sequence repeats exactly the same because top band is actually identical pixels to bottom band
                          + "#0FFFwwwFFFwww$" // First pass of bottom band (Filling black)
                          + "#1wwwFFFwwwFFF" // Second pass of bottom band (Filling white) — no trailing $ on last color layer
                          + "\u001b\\"; // End sixel sequence

        // Arrange: Create a 12x12 bitmap with a 3x3 checkerboard pattern
        Color [,] pixels = new Color [12, 12];

        for (var y = 0; y < 12; y++)
        {
            for (var x = 0; x < 12; x++)
            {
                // Create a 3x3 checkerboard by alternating the color based on pixel coordinates
                if ((x / 3 + y / 3) % 2 == 0)
                {
                    pixels [x, y] = new (0, 0); // Black
                }
                else
                {
                    pixels [x, y] = new (255, 255, 255); // White
                }
            }
        }

        // Act: Encode the image
        var encoder = new SixelEncoder (); // Assuming SixelEncoder is the class that contains the EncodeSixel method
        string result = encoder.EncodeSixel (pixels);

        // We should have only black and white in the palette
        Assert.Equal (2, encoder.Quantizer.Palette.Count);
        Color black = encoder.Quantizer.Palette.ElementAt (0);
        Color white = encoder.Quantizer.Palette.ElementAt (1);

        Assert.Equal (new (0, 0), black);
        Assert.Equal (new (255, 255, 255), white);

        // Compare the generated SIXEL string with the expected one
        Assert.Equal (expected, result);
    }

    [Fact]
    public void EncodeSixel_Transparent12x12_ReturnsExpectedSixel ()
    {
        string expected = "\u001bP" // Start sixel sequence
                          + "0;1;0" // Defaults for aspect ratio and grid size
                          + "q" // Signals beginning of sixel image data
                          + "\"1;1;12;12" // no scaling factors (1x1) and filling 12x12 pixel area
                          + "#0;2;0;0;0" // Black transparent (TODO: Shouldn't really be output this if it is transparent)
                          // Since all pixels are transparent we don't output any colors at all, so its just newline
                          + "-" // Nothing on first or second lines
                          + "\u001b\\"; // End sixel sequence

        // Arrange: Create a 12x12 bitmap filled with fully transparent pixels
        Color [,] pixels = new Color [12, 12];

        for (var x = 0; x < 12; x++)
        {
            for (var y = 0; y < 12; y++)
            {
                pixels [x, y] = new (0, 0, 0, 0); // Fully transparent
            }
        }

        // Act: Encode the image
        var encoder = new SixelEncoder ();
        string result = encoder.EncodeSixel (pixels);

        // Assert: Expect the result to be fully transparent encoded output
        Assert.Equal (expected, result);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void EncodeSixel_VerticalMix_TransparentAndColor_ReturnsExpectedSixel (bool avoidBottomScroll)
    {
        string expected = "\u001bP" // Start sixel sequence
                          + "0;1;0" // Defaults for aspect ratio and grid size (1 indicates support for transparent pixels)
                          + "q" // Signals beginning of sixel image data
                          + "\"1;1;12;12" // No scaling factors (1x1) and filling 12x12 pixel area
                          /*
                           * Define the color palette:
                           * We'll use one color (Red) for the colored pixels.
                           */
                          + "#0;2;100;0;0" // Red color definition (index 0: RGB 100,0,0)
                          + "#1;2;0;0;0" // Black transparent (TODO: Shouldn't really be output this if it is transparent)
                          /*
                           * Start of the Pixel data
                           * We have alternating transparent (0) and colored (red) pixels in a vertical band.
                           * The pattern for each sixel byte is 101010, which in binary (+63) converts to ASCII character 'T'.
                           * Since we have 12 pixels horizontally, we'll see this pattern repeat across the row so we see
                           * the 'sequence repeat' 12 times i.e. !12 (do the next letter 'T' 12 times).
                           */
                          + "#0!12T-" // First band of alternating red and transparent pixels (no trailing $ — single color layer)
                          + "#0!12T" // Second band, same alternating red and transparent pixels (no trailing $)
                          + "\u001b\\"; // End sixel sequence

        // Arrange: Create a 12x12 bitmap with alternating transparent and red pixels in a vertical band
        Color [,] pixels = new Color [12, 12];

        for (var x = 0; x < 12; x++)
        {
            for (var y = 0; y < 12; y++)
            {
                // For simplicity, we'll make every other row transparent
                if (y % 2 == 0)
                {
                    pixels [x, y] = new (255, 0); // Red pixel
                }
                else
                {
                    pixels [x, y] = new (0, 0, 0, 0); // Transparent pixel
                }
            }
        }

        // Act: Encode the image
        var encoder = new SixelEncoder () { AvoidBottomScroll = avoidBottomScroll };
        string result = encoder.EncodeSixel (pixels);

        if (avoidBottomScroll)
        {
            string [] bands = expected.Split ('-');
            bands [0] = bands [0].Replace ("12#", "6#");
            expected = bands [0] + "\u001b\\";
        }

        // Assert: Expect the result to match the expected sixel output
        Assert.Equal (expected, result);
    }

    [Fact]
    public void EncodeSixel_OnePixel_ReturnsExpectedSequence ()
    {
        // Arrange: 1x1 red pixel
        Color [,] pixels = new Color [1, 1];
        pixels [0, 0] = new (255, 0);

        var encoder = new SixelEncoder ();

        // Act
        string result = encoder.EncodeSixel (pixels);

        // Build expected output
        string expected = "\u001bP" // start
                          + "0;0;0"
                          + "q"
                          + "\"1;1;1;1" // no-scaling + width;height
                          + "#0;2;100;0;0" // palette
                          + "#0@" // single column, single row -> code 1 -> char(1+63) = '@' (no trailing $ — last color layer)
                          + "\u001b\\";

        Assert.Equal (expected, result);
    }

    [Fact]
    public void EncodeSixel_WidthRepeat_UsesSequenceRepeatSyntax ()
    {
        // Arrange: width 5, height 1, all same color so sequence repeat > 3
        int width = 5;
        Color [,] pixels = new Color [width, 1];

        for (var x = 0; x < width; x++)
        {
            pixels [x, 0] = new (255, 0);
        }

        var encoder = new SixelEncoder ();

        // Act
        string result = encoder.EncodeSixel (pixels);

        // Assert contains the repeat sequence for 5 identical columns: "!5"
        Assert.Contains ("!5", result);

        // And final payload for the color should include the palette definition
        Assert.Contains ("#0;2;100;0;0", result);
    }

    [Fact]
    public void EncodeSixel_HeightNotMultipleOfSix_IncludesBandSeparator ()
    {
        // Arrange: width 2, height 7 to force two bands (6 rows + 1 row)
        Color [,] pixels = new Color [2, 7];

        for (var x = 0; x < 2; x++)
        {
            for (var y = 0; y < 7; y++)
            {
                pixels [x, y] = new (0, 0, 255);
            }
        }

        var encoder = new SixelEncoder ();

        // Act
        string result = encoder.EncodeSixel (pixels);

        // Assert: there must be a band separator '-' between the bands
        Assert.Contains ("-", result);
    }

    [Fact]
    public void EncodeSixel_AnyTransparentPixel_SetsTransparencyFlagInHeader ()
    {
        // Arrange: 2x2 with one fully transparent pixel
        Color [,] pixels = new Color [2, 2];
        pixels [0, 0] = new (255, 0);
        pixels [0, 1] = new (0, 0, 0, 0); // fully transparent
        pixels [1, 0] = new (0, 255);
        pixels [1, 1] = new (0, 0, 255);

        var encoder = new SixelEncoder ();

        // Act
        string result = encoder.EncodeSixel (pixels);

        // defaultRatios should be "0;1;0" when any pixel has alpha == 0
        Assert.Contains ("\u001bP0;1;0q", result);
    }

    [Fact]
    public void EncodeSixel_MaxPaletteHonored_WhenReducedMaxColors ()
    {
        // Arrange: create three distinct colors but restrict max palette to 2
        Color [,] pixels = new Color [3, 1];
        pixels [0, 0] = new (255, 0);
        pixels [1, 0] = new (0, 255);
        pixels [2, 0] = new (0, 0, 255);

        var encoder = new SixelEncoder ();
        encoder.Quantizer.MaxColors = 2;

        // Act
        string result = encoder.EncodeSixel (pixels);

        // Assert: palette count must respect MaxColors (<= 2) and encoding must not throw
        Assert.True (encoder.Quantizer.Palette.Count <= 2);
        Assert.False (string.IsNullOrEmpty (result));
    }

    [Fact]
    public void GetHeightInPixels_DoesNotRound_WhenAvoidBottomScrollIsFalse ()
    {
        SixelEncoder encoder = new ()
        {
            AvoidBottomScroll = false
        };

        int result = encoder.GetHeightInPixels (maxSizeHeight: 13, pixelsPerCellY: 2);

        Assert.Equal (13 * 2, result);
    }

    [Theory]
    [InlineData (13, 2, 24)]
    [InlineData (26, 20, 516)]
    public void GetHeightInPixels_RoundsDownToMultipleOf6_WhenAvoidBottomScrollIsTrue (int maxSizeHeight, int pixelsPerCellY, int expected)
    {
        SixelEncoder encoder = new ()
        {
            AvoidBottomScroll = true
        };

        int result = encoder.GetHeightInPixels (maxSizeHeight, pixelsPerCellY);

        // Max height rounded down to nearest multiple of 6
        Assert.Equal (expected, result);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void GetHeightInPixels_ExactMultipleOf6_IsNotModified (bool avoidBottomScroll)
    {
        SixelEncoder encoder = new ()
        {
            AvoidBottomScroll = avoidBottomScroll
        };

        int result = encoder.GetHeightInPixels (maxSizeHeight: 12, pixelsPerCellY: 2);

        Assert.Equal (12 * 2, result);
    }

    [Fact]
    public void GetHeightInPixels_HeightBelowPixelHigh_IsNotRounded_WhenAvoidBottomScrollIsFalse ()
    {
        SixelEncoder encoder = new ()
        {
            AvoidBottomScroll = false
        };

        const int SMALL_HEIGHT = 5;

        int result = encoder.GetHeightInPixels (maxSizeHeight: SMALL_HEIGHT, pixelsPerCellY: 2);

        Assert.Equal (SMALL_HEIGHT * 2, result);
    }

    [Theory]
    [InlineData (5, 2, 6)]
    [InlineData (5, 20, 96)]
    [InlineData (1, 2, 0)]
    [InlineData (1, 20, 18)]
    public void GetHeightInPixels_HeightBelowPixelHigh_WhenAvoidBottomScrollIsTrue (int maxSizeHeight, int pixelsPerCellY, int expected)
    {
        SixelEncoder encoder = new ()
        {
            AvoidBottomScroll = true
        };

        int result = encoder.GetHeightInPixels (maxSizeHeight, pixelsPerCellY);

        Assert.Equal (expected, result);
    }

    [Fact] // Copilot
    public void EncodeSixel_MultiBandMultiColor_NoTrailingDollarOnLastColorLayer ()
    {
        // Arrange: 4x12 image — two bands of 6 rows each, two colors (red left half, blue right half)
        Color [,] pixels = new Color [4, 12];

        for (var y = 0; y < 12; y++)
        {
            for (var x = 0; x < 4; x++)
            {
                pixels [x, y] = x < 2 ? new Color (255, 0) : new Color (0, 0, 255);
            }
        }

        var encoder = new SixelEncoder ();

        // Act
        string result = encoder.EncodeSixel (pixels);

        // Assert: No color layer data should end with '$' immediately before '-' or before the terminator.
        // Between color layers '$' IS expected.
        // Band structure: #<idx>data$#<idx>data- (for non-last band)
        //                 #<idx>data$#<idx>data   (for last band — no trailing $)
        Assert.DoesNotContain ("$-", result); // No trailing $ before band separator
        Assert.DoesNotContain ("$\u001b\\", result); // No trailing $ before sequence terminator
        Assert.Contains ("-", result); // Has band separator (two bands)
        Assert.Contains ("$", result); // Has $ between color layers within a band
    }
}

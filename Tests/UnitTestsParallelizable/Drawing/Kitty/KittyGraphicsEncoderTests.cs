// Copilot - Claude Sonnet 4.6

namespace DrawingTests;

public class KittyGraphicsEncoderTests
{
    private static Color [,] CreateSolidPixels (int width, int height, Color color)
    {
        Color [,] pixels = new Color [width, height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                pixels [x, y] = color;
            }
        }

        return pixels;
    }

    [Fact]
    public void EncodeKitty_RedSquare_StartsWithApcAndEndsWithStringTerminator ()
    {
        KittyGraphicsEncoder encoder = new ();
        Color [,] pixels = CreateSolidPixels (2, 2, new Color (255, 0, 0));

        string result = encoder.EncodeKitty (pixels, 2, 1);

        Assert.StartsWith ("\x1b_G", result);
        Assert.EndsWith ("\x1b\\", result);
    }

    [Fact]
    public void EncodeKitty_RedSquare_ContainsRequiredMetadataFields ()
    {
        KittyGraphicsEncoder encoder = new ();
        Color [,] pixels = CreateSolidPixels (2, 2, new Color (255, 0, 0));

        string result = encoder.EncodeKitty (pixels, 2, 1);

        Assert.Contains ("a=T", result);
        Assert.Contains ("f=32", result);
        Assert.Contains ("s=2", result);
        Assert.Contains ("v=2", result);
        Assert.Contains ("c=2", result);
        Assert.Contains ("r=1", result);
        Assert.Contains ("q=2", result);
    }

    [Fact]
    public void EncodeKitty_DoesNotMoveCursor ()
    {
        // Claude - Opus 4.8
        // The Kitty protocol moves the cursor to just after the image by default (C=0).
        // When an image is placed near the bottom of the screen, the resulting cursor move
        // scrolls the terminal up one row on every frame. C=1 suppresses cursor movement so
        // animated images repaint in place instead of marching up the screen.
        KittyGraphicsEncoder encoder = new ();
        Color [,] pixels = CreateSolidPixels (2, 2, new Color (255, 0, 0));

        string result = encoder.EncodeKitty (pixels, 2, 1);

        Assert.Contains ("C=1", result);
    }

    [Fact]
    public void EncodeKitty_WithImageId_EmitsImageIdField ()
    {
        // Claude - Opus 4.8
        // The i=<id> field tags the placement so a prior placement can be deleted/replaced by id.
        // Without it, a resized or moved image leaves its old placement on screen.
        KittyGraphicsEncoder encoder = new ();
        Color [,] pixels = CreateSolidPixels (2, 2, new Color (255, 0, 0));

        string result = encoder.EncodeKitty (pixels, 2, 1, 12345);

        Assert.Contains ("i=12345", result);
    }

    [Fact]
    public void EncodeKitty_WithoutImageId_OmitsImageIdField ()
    {
        // Claude - Opus 4.8
        KittyGraphicsEncoder encoder = new ();
        Color [,] pixels = CreateSolidPixels (2, 2, new Color (255, 0, 0));

        string result = encoder.EncodeKitty (pixels, 2, 1);

        Assert.DoesNotContain ("i=", result);
    }

    [Fact]
    public void GetImageId_IsStableForSameString ()
    {
        // Claude - Opus 4.8
        Assert.Equal (KittyGraphicsEncoder.GetImageId ("ImageView_42"), KittyGraphicsEncoder.GetImageId ("ImageView_42"));
    }

    [Fact]
    public void GetImageId_IsPositiveAndNonZero ()
    {
        // Claude - Opus 4.8
        Assert.True (KittyGraphicsEncoder.GetImageId ("ImageView_42") > 0);
        Assert.True (KittyGraphicsEncoder.GetImageId ("anything") > 0);
    }

    [Fact]
    public void EncodeDeletePlacements_TargetsImageIdAndKeepsData ()
    {
        // Claude - Opus 4.8
        // a=d deletes; d=i targets placements of image id i, leaving the transmitted data intact.
        string result = KittyGraphicsEncoder.EncodeDeletePlacements (777);

        Assert.StartsWith ("\x1b_G", result);
        Assert.EndsWith ("\x1b\\", result);
        Assert.Contains ("a=d", result);
        Assert.Contains ("d=i", result);
        Assert.Contains ("i=777", result);
    }

    [Fact]
    public void EncodeKitty_SmallImage_HasSingleChunkWithMoreEqualZero ()
    {
        KittyGraphicsEncoder encoder = new ();
        Color [,] pixels = CreateSolidPixels (1, 1, new Color (0, 255, 0));

        string result = encoder.EncodeKitty (pixels, 1, 1);

        // Single chunk: m=0 (last/only chunk)
        Assert.Contains ("m=0", result);
        // Should NOT have m=1 (no continuation chunks)
        Assert.DoesNotContain ("m=1", result);
    }

    [Fact]
    public void EncodeKitty_LargeImage_EmitsMultipleChunksWithContinuationMarker ()
    {
        // 32×32 pixels = 32*32*4 = 4096 raw bytes = ~5460 base64 chars — exceeds MaxChunkSize
        KittyGraphicsEncoder encoder = new ();
        Color [,] pixels = CreateSolidPixels (32, 32, new Color (0, 0, 255));

        string result = encoder.EncodeKitty (pixels, 4, 2);

        // m=1 on the first chunk means more data follows
        Assert.Contains ("m=1", result);
        // The final chunk must have m=0
        Assert.Contains ("m=0", result);
    }

    [Fact]
    public void EncodeKitty_TransparentPixels_IncludesAlphaChannelInData ()
    {
        KittyGraphicsEncoder encoder = new ();
        Color [,] pixels = new Color [1, 1];

        // Fully transparent red
        pixels [0, 0] = new Color (255, 0, 0, 0);

        string result = encoder.EncodeKitty (pixels, 1, 1);

        // Extract the base64 payload from the APC sequence
        // Format: ESC_G <metadata>;<base64data> ESC\
        int semicolonIdx = result.IndexOf (';');
        int endIdx = result.LastIndexOf ('\x1b');

        Assert.True (semicolonIdx > 0, "No semicolon found in APC sequence");

        string base64 = result.Substring (semicolonIdx + 1, endIdx - semicolonIdx - 1);
        byte [] decoded = Convert.FromBase64String (base64);

        // RGBA: R=255, G=0, B=0, A=0
        Assert.Equal (4, decoded.Length);
        Assert.Equal (255, decoded [0]);
        Assert.Equal (0, decoded [1]);
        Assert.Equal (0, decoded [2]);
        Assert.Equal (0, decoded [3]);
    }

    [Fact]
    public void EncodeKitty_OpaquePixel_HasAlpha255 ()
    {
        KittyGraphicsEncoder encoder = new ();
        Color [,] pixels = new Color [1, 1];

        pixels [0, 0] = new Color (10, 20, 30, 255);

        string result = encoder.EncodeKitty (pixels, 1, 1);

        int semicolonIdx = result.IndexOf (';');
        int endIdx = result.LastIndexOf ('\x1b');
        string base64 = result.Substring (semicolonIdx + 1, endIdx - semicolonIdx - 1);
        byte [] decoded = Convert.FromBase64String (base64);

        Assert.Equal (4, decoded.Length);
        Assert.Equal (10, decoded [0]);
        Assert.Equal (20, decoded [1]);
        Assert.Equal (30, decoded [2]);
        Assert.Equal (255, decoded [3]);
    }

    [Fact]
    public void EncodeKitty_EmitsValidBase64Payload ()
    {
        KittyGraphicsEncoder encoder = new ();
        Color [,] pixels = CreateSolidPixels (2, 2, new Color (128, 64, 32));

        string result = encoder.EncodeKitty (pixels, 2, 1);

        // Extract the base64 data from first chunk
        int semicolonIdx = result.IndexOf (';');
        int endIdx = result.IndexOf ('\x1b', semicolonIdx);
        string base64 = result.Substring (semicolonIdx + 1, endIdx - semicolonIdx - 1);

        // Should not throw
        byte [] decoded = Convert.FromBase64String (base64);

        // 2×2 pixels × 4 bytes = 16 bytes raw, but may be chunked — at least first chunk data
        Assert.True (decoded.Length > 0);
    }
}

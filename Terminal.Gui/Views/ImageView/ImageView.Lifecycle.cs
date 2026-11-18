namespace Terminal.Gui.Views;

public partial class ImageView
{
    bool IDesignable.EnableForDesign ()
    {
        // Create a simple gradient test image for the designer
        const int WIDTH = 20;
        const int HEIGHT = 10;
        Color [,] testImage = new Color [WIDTH, HEIGHT];

        for (var y = 0; y < HEIGHT; y++)
        {
            for (var x = 0; x < WIDTH; x++)
            {
                var r = (byte)(x * 255 / Math.Max (1, WIDTH - 1));
                var g = (byte)(y * 255 / Math.Max (1, HEIGHT - 1));
                var b = (byte)128;
                testImage [x, y] = new Color (r, g, b);
            }
        }

        Image = testImage;

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            _disposed = true;

            lock (_renderLock)
            {
                _queuedRenderRequest = null;
                _backgroundRenderKey = null;
                _backgroundRenderRunning = false;
            }

            App?.Driver?.GetOutputBuffer ().RemoveRasterImage (RasterImageId);
        }

        base.Dispose (disposing);
    }
}

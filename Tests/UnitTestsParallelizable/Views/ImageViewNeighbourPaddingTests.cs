namespace ViewsTests;

/// <summary>
///     Regression for gui-cs/Terminal.Gui#5518.
///     <para>
///         A raster <see cref="ImageView"/> placed at <see cref="Pos.Right"/> of a view that has a right
///         <see cref="Adornment"/> padding column overwrites that padding column — the cell one to the
///         LEFT of the ImageView's frame — when it redraws. The full first draw repaints the neighbour so
///         it looks correct; a <em>partial</em> redraw (a focus round-trip here) leaves the overwrite
///         behind, bleeding the image pane's background into the neighbour.
///     </para>
///     <para>
///         Surfaced in winprint (gui-cs/Terminal.Gui#5518) as the page-preview pane bleeding its canvas
///         colour into the settings panel's seam, on both Sixel and Kitty. It is not actually
///         raster-specific — a plain <see cref="View"/> at <see cref="Pos.Right"/> reproduces it too — so
///         the fix most likely belongs in the View draw/clip path, not the raster code.
///     </para>
/// </summary>
public class ImageViewNeighbourPaddingTests
{
    [Fact]
    public void Draw_RightOfPaddedNeighbour_DoesNotOverwriteNeighboursPaddingColumn ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 12);

        Runnable runnable = new () { Width = 40, Height = 12 };
        runnable.SetScheme (new Scheme { Normal = new Attribute (new Color (255, 255, 255), new Color (26, 26, 26)) });
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (9, 20), MaxPaletteColors = 256 });

        // Left pane with a one-column right Padding — that padding column is the "seam".
        View left = new () { X = 0, Y = 0, Width = 10, Height = Dim.Fill (), CanFocus = true };
        left.Padding!.Thickness = new Thickness (0, 0, 1, 0);
        left.SetScheme (new Scheme { Normal = new Attribute (new Color (255, 255, 255), new Color (200, 0, 0)) });

        // Raster ImageView immediately to the right of the seam.
        Color gray = new (224, 224, 224);
        ImageView img = new () { X = Pos.Right (left), Y = 0, Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };
        img.SetScheme (new Scheme { Normal = new Attribute (new Color (0, 0, 0), gray) });
        img.Image = CreateSolidImage (90, 200, gray);

        // ImageView added first, neighbour on top: the full first draw repaints the seam correctly.
        runnable.Add (img, left);

        img.SetFocus ();
        app.LayoutAndDraw ();
        app.LayoutAndDraw ();

        int seam = img.ViewportToScreen ().X - 1; // the neighbour's padding column, outside the image frame
        int row = img.ViewportToScreen ().Y + 2;

        Attribute? before = driver.GetOutputBuffer ().Contents! [row, seam].Attribute;

        // Partial redraw: focus the neighbour, then focus back to the image.
        left.SetFocus ();
        app.LayoutAndDraw ();
        img.SetFocus ();
        app.LayoutAndDraw ();

        Attribute? after = driver.GetOutputBuffer ().Contents! [row, seam].Attribute;

        // The ImageView must not have changed a cell outside (to the left of) its own frame.
        Assert.Equal (before, after);

        runnable.Dispose ();
    }

    private static Color [,] CreateSolidImage (int width, int height, Color color)
    {
        Color [,] image = new Color [width, height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                image [x, y] = color;
            }
        }

        return image;
    }
}

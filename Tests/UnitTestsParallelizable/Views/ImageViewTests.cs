namespace ViewsTests;

/// <summary>
///     Unit tests for <see cref="ImageView"/>.
/// </summary>
public class ImageViewTests
{
    #region Construction and Defaults

    [Fact]
    public void Defaults_AreExpected ()
    {
        ImageView imageView = new ();

        Assert.Null (imageView.Image);
        Assert.True (imageView.UseSixel);
        Assert.Null (imageView.SixelEncoder);
        Assert.False (imageView.IsUsingSixel); // No driver, so sixel not available
        Assert.True (imageView.CanFocus);
        Assert.Equal (1d, imageView.ZoomLevel);
        Assert.Equal (64, imageView.MaxSixelPaletteColors);
        Assert.True (imageView.AllowSixelUpscaling);
        Assert.False (imageView.UseBackgroundRendering);

        imageView.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void Defaults_ConfigurePanAndZoomBindings ()
    {
        ImageView imageView = new ();

        Assert.Equal ([Command.ScrollLeft], imageView.KeyBindings.GetCommands (Key.CursorLeft));
        Assert.Equal ([Command.ScrollRight], imageView.KeyBindings.GetCommands (Key.CursorRight));
        Assert.Equal ([Command.ScrollUp], imageView.KeyBindings.GetCommands (Key.CursorUp));
        Assert.Equal ([Command.ScrollDown], imageView.KeyBindings.GetCommands (Key.CursorDown));
        Assert.Equal ([Command.Home], imageView.KeyBindings.GetCommands (Key.Home));
        Assert.Equal ([Command.Home], imageView.KeyBindings.GetCommands (Key.D0));
        Assert.Equal ([Command.ZoomIn], imageView.KeyBindings.GetCommands (new Key ('+')));
        Assert.Equal ([Command.ZoomIn], imageView.KeyBindings.GetCommands (new Key ('+').WithShift));
        Assert.Equal ([Command.ZoomIn], imageView.KeyBindings.GetCommands (new Key ('=')));
        Assert.Equal ([Command.ZoomIn], imageView.KeyBindings.GetCommands (new Key ('=').WithShift));
        Assert.Equal ([Command.ZoomOut], imageView.KeyBindings.GetCommands (new Key ('-')));
        Assert.Equal ([Command.PageUp], imageView.KeyBindings.GetCommands (Key.PageUp));
        Assert.Equal ([Command.PageDown], imageView.KeyBindings.GetCommands (Key.PageDown));
        Assert.Equal ([Command.ZoomIn], imageView.MouseBindings.GetCommands (MouseFlags.WheeledUp));
        Assert.Equal ([Command.ZoomOut], imageView.MouseBindings.GetCommands (MouseFlags.WheeledDown));
        Assert.Equal ([Command.Center], imageView.MouseBindings.GetCommands (MouseFlags.LeftButtonDoubleClicked));

        imageView.Dispose ();
    }

    #endregion Construction and Defaults

    #region Background Rendering

    // Copilot - GPT-5.5
    [Fact]
    public void BackgroundRendering_WhenEnabled_ShowsSpinnerBeforeFirstDraw ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        ImageView imageView = new ()
        {
            Width = 2,
            Height = 2,
            UseBackgroundRendering = true,
            UseSixel = false,
            Image = CreateCoordinateImage (4, 4)
        };
        runnable.Add (imageView);

        SpinnerView overlay = Assert.Single (imageView.SubViews.OfType<SpinnerView> ());
        Assert.True (overlay.Visible);
        Assert.True (overlay.AutoSpin);
        Assert.IsType<SpinnerStyle.Aesthetic2> (overlay.Style);

        app.LayoutAndDraw ();

        Assert.True (overlay.Visible);
        Assert.True (overlay.AutoSpin);
        Assert.IsType<SpinnerStyle.Aesthetic2> (overlay.Style);

        runnable.Dispose ();
    }

    #endregion Background Rendering

    #region Image Property

    [Fact]
    public void Image_Set_SetsNeedsDraw ()
    {
        ImageView imageView = new () { Width = 10, Height = 10 };
        View host = new () { Width = 10, Height = 10 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        imageView.ClearNeedsDraw ();
        Assert.False (imageView.NeedsDraw);

        Color [,] pixels = CreateSolidImage (5, 5, new Color (255, 0, 0));
        imageView.Image = pixels;

        Assert.True (imageView.NeedsDraw);

        host.Dispose ();
    }

    [Fact]
    public void Image_SetNull_ClearsState ()
    {
        ImageView imageView = new ();

        Color [,] pixels = CreateSolidImage (5, 5, new Color (255, 0, 0));
        imageView.Image = pixels;
        Assert.NotNull (imageView.Image);

        imageView.Image = null;
        Assert.Null (imageView.Image);

        imageView.Dispose ();
    }

    [Fact]
    public void Image_Set_ClearsCachedScaledImage ()
    {
        ImageView imageView = new () { Width = 10, Height = 10, UseSixel = false };
        View host = new () { Width = 10, Height = 10 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // Set an initial image
        Color [,] pixels1 = CreateSolidImage (5, 5, new Color (255, 0, 0));
        imageView.Image = pixels1;

        // Set a different image — should clear cached scaled image
        Color [,] pixels2 = CreateSolidImage (3, 3, new Color (0, 255, 0));
        imageView.Image = pixels2;

        Assert.Same (pixels2, imageView.Image);

        host.Dispose ();
    }

    #endregion Image Property

    #region UseSixel Property

    [Fact]
    public void UseSixel_DefaultsToTrue ()
    {
        ImageView imageView = new ();
        Assert.True (imageView.UseSixel);
        imageView.Dispose ();
    }

    [Fact]
    public void IsUsingSixel_FalseWhenUseSixelFalse ()
    {
        ImageView imageView = new () { UseSixel = false };
        Assert.False (imageView.IsUsingSixel);
        imageView.Dispose ();
    }

    [Fact]
    public void IsUsingSixel_FalseWhenNoDriver ()
    {
        ImageView imageView = new () { UseSixel = true };

        // No App/Driver available, so sixel shouldn't be active
        Assert.False (imageView.IsUsingSixel);
        imageView.Dispose ();
    }

    #endregion UseSixel Property

    #region SixelEncoder Property

    [Fact]
    public void SixelEncoder_DefaultsToNull ()
    {
        ImageView imageView = new ();
        Assert.Null (imageView.SixelEncoder);
        imageView.Dispose ();
    }

    [Fact]
    public void SixelEncoder_CanBeSet ()
    {
        SixelEncoder encoder = new () { AvoidBottomScroll = true };
        ImageView imageView = new () { SixelEncoder = encoder };

        Assert.Same (encoder, imageView.SixelEncoder);

        imageView.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void MaxSixelPaletteColors_SetInvalid_Throws ()
    {
        ImageView imageView = new ();

        Assert.Throws<ArgumentOutOfRangeException> (() => imageView.MaxSixelPaletteColors = 0);

        imageView.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void SixelRendering_DefaultEncoder_UsesInteractivePaletteLimit ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (1, 1), MaxPaletteColors = 256 });

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4) };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Assert.NotNull (imageView.SixelEncoder);
        Assert.Equal (64, imageView.SixelEncoder!.Quantizer.MaxColors);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void SixelRendering_DefaultEncoder_ClampsPaletteLimitToTerminalSupport ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (1, 1), MaxPaletteColors = 64 });

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4) };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Assert.NotNull (imageView.SixelEncoder);
        Assert.Equal (64, imageView.SixelEncoder!.Quantizer.MaxColors);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void SixelRendering_CustomEncoder_KeepsPaletteLimitAboveInteractiveDefault ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (1, 1), MaxPaletteColors = 256 });

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        SixelEncoder encoder = new ();
        encoder.Quantizer.MaxColors = 200;
        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4), SixelEncoder = encoder };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Assert.Equal (200, encoder.Quantizer.MaxColors);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void SixelRendering_CanDisableUpscaling_WhenViewportWouldUpscale ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (10, 10), MaxPaletteColors = 256 });

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        ImageView imageView = new ()
        {
            Width = 4,
            Height = 4,
            AllowSixelUpscaling = false,
            Image = CreateCoordinateImage (4, 4)
        };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        RasterImageCommand command = Assert.Single (driver.GetOutputBuffer ().GetRasterImages ());
        Assert.NotNull (command.Pixels);
        Assert.Equal (4, command.Pixels!.GetLength (0));
        Assert.Equal (4, command.Pixels.GetLength (1));
        Assert.Equal (new Rectangle (0, 0, 1, 1), command.DestinationCells);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void SixelRendering_DefaultUpscaling_ScalesToViewport ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (10, 10), MaxPaletteColors = 256 });

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        ImageView imageView = new ()
        {
            Width = 4,
            Height = 4,
            Image = CreateCoordinateImage (4, 4)
        };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        RasterImageCommand command = Assert.Single (driver.GetOutputBuffer ().GetRasterImages ());
        Assert.NotNull (command.Pixels);
        Assert.Equal (40, command.Pixels!.GetLength (0));
        Assert.Equal (40, command.Pixels.GetLength (1));
        Assert.Equal (new Rectangle (0, 0, 4, 4), command.DestinationCells);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void SixelRendering_SupportResolutionChange_RecomputesWithoutResize ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (1, 1), MaxPaletteColors = 256 });

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4) };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        RasterImageCommand firstCommand = Assert.Single (driver.GetOutputBuffer ().GetRasterImages ());
        Assert.NotNull (firstCommand.Pixels);
        Assert.Equal (2, firstCommand.Pixels!.GetLength (0));
        Assert.Equal (2, firstCommand.Pixels.GetLength (1));

        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (10, 10), MaxPaletteColors = 256 });
        imageView.SetNeedsDraw ();
        app.LayoutAndDraw ();

        RasterImageCommand secondCommand = Assert.Single (driver.GetOutputBuffer ().GetRasterImages ());
        Assert.NotNull (secondCommand.Pixels);
        Assert.Equal (20, secondCommand.Pixels!.GetLength (0));
        Assert.Equal (20, secondCommand.Pixels.GetLength (1));

        runnable.Dispose ();
    }

    #endregion SixelEncoder Property

    #region ScaleNearestNeighbor

    [Fact]
    public void ScaleNearestNeighbor_IdentityScale_PreservesPixels ()
    {
        Color [,] source = CreateGradientImage (4, 4);
        Color [,] destination = new Color [4, 4];
        ImageView.ScaleNearestNeighbor (source, destination);

        Assert.Equal (4, destination.GetLength (0));
        Assert.Equal (4, destination.GetLength (1));

        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                Assert.Equal (source [x, y], destination [x, y]);
            }
        }
    }

    [Fact]
    public void ScaleNearestNeighbor_Upscale_CorrectDimensions ()
    {
        Color [,] source = CreateSolidImage (2, 2, new Color (100, 100, 100));
        Color [,] destination = new Color [6, 6];
        ImageView.ScaleNearestNeighbor (source, destination);

        Assert.Equal (6, destination.GetLength (0));
        Assert.Equal (6, destination.GetLength (1));

        // All pixels should be the same color since source is solid
        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                Assert.Equal (new Color (100, 100, 100), destination [x, y]);
            }
        }
    }

    [Fact]
    public void ScaleNearestNeighbor_Downscale_CorrectDimensions ()
    {
        Color [,] source = CreateSolidImage (10, 10, new Color (50, 50, 50));
        Color [,] destination = new Color [3, 3];
        ImageView.ScaleNearestNeighbor (source, destination);

        Assert.Equal (3, destination.GetLength (0));
        Assert.Equal (3, destination.GetLength (1));

        // All pixels should still be solid
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                Assert.Equal (new Color (50, 50, 50), destination [x, y]);
            }
        }
    }

    [Fact]
    public void ScaleNearestNeighbor_1x1_Target_ProducesOnePixel ()
    {
        Color [,] source = CreateGradientImage (10, 10);
        Color [,] destination = new Color [1, 1];
        ImageView.ScaleNearestNeighbor (source, destination);

        Assert.Equal (1, destination.GetLength (0));
        Assert.Equal (1, destination.GetLength (1));

        // Should be the top-left pixel (nearest neighbor from 0,0)
        Assert.Equal (source [0, 0], destination [0, 0]);
    }

    [Fact]
    public void ScaleNearestNeighbor_NonSquare_ScalesCorrectly ()
    {
        Color [,] source = CreateSolidImage (4, 2, new Color (200, 100, 50));
        Color [,] destination = new Color [8, 4];
        ImageView.ScaleNearestNeighbor (source, destination);

        Assert.Equal (8, destination.GetLength (0));
        Assert.Equal (4, destination.GetLength (1));

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                Assert.Equal (new Color (200, 100, 50), destination [x, y]);
            }
        }
    }

    #endregion ScaleNearestNeighbor

    #region Cell-Based Rendering

    [Fact]
    public void CellBasedRendering_DrawsBackgroundColors ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        ImageView imageView = new ()
        {
            Width = 4,
            Height = 2,
            UseSixel = false
        };

        Color red = new (255, 0, 0);
        imageView.Image = CreateSolidImage (4, 2, red);

        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Verify the cells have background color set to red (spaces with red background)
        Cell [,]? contents = app.Driver!.Contents;
        Assert.NotNull (contents);

        // Check that the first cell in the image area has the correct background color
        Attribute attr = contents! [0, 0].Attribute!.Value;
        Assert.Equal (red, attr.Background);

        runnable.Dispose ();
    }

    [Fact]
    public void CellBasedRendering_WithNullImage_DrawsNothing ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        ImageView imageView = new ()
        {
            Width = 4,
            Height = 2,
            UseSixel = false
        };

        // Image is null — should not throw
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        runnable.Dispose ();
    }

    [Fact]
    public void CellBasedRendering_ScalesImageToViewport ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 20, Height = 20 };
        app.Begin (runnable);

        // Create a 10x10 image but display in a 5x5 viewport
        ImageView imageView = new ()
        {
            Width = 5,
            Height = 5,
            UseSixel = false
        };

        Color blue = new (0, 0, 255);
        imageView.Image = CreateSolidImage (10, 10, blue);

        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Verify cells have the blue background (image was scaled down)
        Cell [,]? contents = app.Driver!.Contents;
        Assert.NotNull (contents);
        Assert.Equal (blue, contents! [0, 0].Attribute!.Value.Background);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void CellBasedRendering_TargetSizeChange_RecomputesScaledImage ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        Color [,] image = CreateCoordinateImage (4, 4);
        NonInvalidatingImageView imageView = new () { Width = 2, Height = 2, UseSixel = false, Image = image };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        imageView.Width = 4;
        imageView.Height = 4;
        app.LayoutAndDraw ();

        AssertCellBackground (app.Driver!, 3, 3, image [3, 3]);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void CellBasedRendering_StretchesImageToViewport ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        Color [,] image = CreateCoordinateImage (4, 4);
        ImageView imageView = new () { Width = 4, Height = 2, UseSixel = false, Image = image };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        AssertCellBackground (app.Driver!, 0, 0, image [0, 0]);
        AssertCellBackground (app.Driver!, 3, 1, image [3, 2]);

        runnable.Dispose ();
    }

    #endregion Cell-Based Rendering

    #region Pan and Zoom

    // Copilot - GPT-5.5
    [Fact]
    public void Commands_ZoomInZoomOutAndHome_UpdateZoomLevel ()
    {
        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4) };
        View host = new () { Width = 2, Height = 2 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        Assert.True (imageView.InvokeCommand (Command.ZoomIn));
        Assert.True (imageView.ZoomLevel > 1d);

        Assert.True (imageView.InvokeCommand (Command.ZoomOut));
        Assert.Equal (1d, imageView.ZoomLevel);

        imageView.ZoomLevel = 2d;
        Assert.True (imageView.InvokeCommand (Command.Home));
        Assert.Equal (1d, imageView.ZoomLevel);

        host.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void KeyBindings_ZoomScrollAndHome_UpdateView ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        Color [,] image = CreateCoordinateImage (4, 4);
        ImageView imageView = new () { Width = 2, Height = 2, UseSixel = false, Image = image };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Assert.True (imageView.NewKeyDownEvent (new Key ('+')));
        Assert.True (imageView.ZoomLevel > 1d);

        imageView.ZoomLevel = 2d;
        app.LayoutAndDraw ();
        AssertCellBackground (app.Driver!, 0, 0, image [1, 1]);

        Assert.True (imageView.NewKeyDownEvent (Key.CursorRight));
        app.LayoutAndDraw ();
        AssertCellBackground (app.Driver!, 0, 0, image [2, 1]);

        Assert.True (imageView.NewKeyDownEvent (Key.Home));
        Assert.Equal (1d, imageView.ZoomLevel);
        app.LayoutAndDraw ();
        AssertCellBackground (app.Driver!, 0, 0, image [0, 0]);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void KeyBindings_PageDown_ZoomsOutBelowFit ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        ImageView imageView = new () { Width = 4, Height = 4, UseSixel = false, Image = CreateCoordinateImage (4, 4) };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        for (int i = 0; i < 12; i++)
        {
            imageView.NewKeyDownEvent (Key.PageDown);
        }

        Assert.True (imageView.ZoomLevel < 1d);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void ApplicationKeyDispatch_WhenFocused_ZoomsAndScrolls ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        Color [,] image = CreateCoordinateImage (4, 4);
        ImageView imageView = new () { Width = 2, Height = 2, UseSixel = false, Image = image };
        runnable.Add (imageView);
        app.LayoutAndDraw ();
        imageView.SetFocus ();

        Assert.True (app.Keyboard.RaiseKeyDownEvent (new Key ('+')));
        Assert.True (imageView.ZoomLevel > 1d);

        imageView.ZoomLevel = 2d;
        app.LayoutAndDraw ();
        AssertCellBackground (app.Driver!, 0, 0, image [1, 1]);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorRight));
        app.LayoutAndDraw ();
        AssertCellBackground (app.Driver!, 0, 0, image [2, 1]);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void ApplicationKeyDispatch_WhenFocused_EatsScrollKeysEvenWithoutPan ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        ImageView imageView = new () { Width = 2, Height = 2, UseSixel = false, Image = CreateCoordinateImage (2, 2) };
        runnable.Add (imageView);
        app.LayoutAndDraw ();
        imageView.SetFocus ();

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorRight));
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorUp));
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorDown));
        Assert.Equal (1d, imageView.ZoomLevel);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void CellBasedRendering_ZoomAndScroll_ChangesVisiblePixels ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        Color [,] image = CreateCoordinateImage (4, 4);
        ImageView imageView = new () { Width = 2, Height = 2, UseSixel = false, Image = image };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        imageView.ZoomLevel = 2d;
        app.LayoutAndDraw ();
        AssertCellBackground (app.Driver!, 0, 0, image [1, 1]);

        Assert.True (imageView.InvokeCommand (Command.ScrollRight));
        app.LayoutAndDraw ();
        AssertCellBackground (app.Driver!, 0, 0, image [2, 1]);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void MouseWheel_ZoomsInAndOut ()
    {
        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4) };
        View host = new () { Width = 2, Height = 2 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        Assert.True (imageView.NewMouseEvent (new Mouse { Flags = MouseFlags.WheeledUp, Position = new (1, 1) }));
        Assert.True (imageView.ZoomLevel > 1d);

        Assert.True (imageView.NewMouseEvent (new Mouse { Flags = MouseFlags.WheeledDown, Position = new (1, 1) }));
        Assert.Equal (1d, imageView.ZoomLevel);

        host.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void DoubleClick_CentersOnClickedPoint ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        Color [,] image = CreateCoordinateImage (4, 4);
        ImageView imageView = new () { Width = 2, Height = 2, UseSixel = false, Image = image, ZoomLevel = 2d };
        runnable.Add (imageView);
        app.LayoutAndDraw ();
        AssertCellBackground (app.Driver!, 0, 0, image [1, 1]);

        Assert.True (imageView.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonDoubleClicked, Position = new (0, 0) }));
        app.LayoutAndDraw ();
        AssertCellBackground (app.Driver!, 0, 0, image [0, 0]);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void Drag_PansImage ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        Color [,] image = CreateCoordinateImage (4, 4);
        ImageView imageView = new () { Width = 2, Height = 2, UseSixel = false, Image = image, ZoomLevel = 2d };
        runnable.Add (imageView);
        app.LayoutAndDraw ();
        AssertCellBackground (app.Driver!, 0, 0, image [1, 1]);

        Assert.True (imageView.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed, Position = new (1, 0) }));
        Assert.True (imageView.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, Position = new (0, 0) }));
        Assert.True (imageView.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonReleased, Position = new (0, 0) }));
        app.LayoutAndDraw ();
        AssertCellBackground (app.Driver!, 0, 0, image [2, 1]);

        runnable.Dispose ();
    }

    // Copilot - GPT-5.5
    [Fact]
    public void SixelRendering_Zoom_UsesVisiblePixels ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (1, 1) });

        Runnable runnable = new () { Width = 4, Height = 4 };
        app.Begin (runnable);

        Color [,] image = CreateCoordinateImage (4, 4);
        ImageView imageView = new () { Width = 2, Height = 2, Image = image, ZoomLevel = 2d, SixelEncoder = new () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        RasterImageCommand command = Assert.Single (driver.GetOutputBuffer ().GetRasterImages ());
        Assert.NotNull (command.Pixels);
        Assert.Equal (image [1, 1], command.Pixels! [0, 0]);

        runnable.Dispose ();
    }

    // Copilot
    [Fact]
    public void KeyBindings_PlusKey_ZoomsIn ()
    {
        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4) };
        View host = new () { Width = 2, Height = 2 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        Assert.True (imageView.NewKeyDownEvent (new Key ('+')));
        Assert.True (imageView.ZoomLevel > 1d);

        host.Dispose ();
    }

    [Fact]
    public void KeyBindings_ShiftedPlusKey_ZoomsIn ()
    {
        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4) };
        View host = new () { Width = 2, Height = 2 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        Assert.True (imageView.NewKeyDownEvent (new Key ('+').WithShift));
        Assert.True (imageView.ZoomLevel > 1d);

        host.Dispose ();
    }

    // Copilot
    [Fact]
    public void KeyBindings_EqualsKey_ZoomsIn ()
    {
        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4) };
        View host = new () { Width = 2, Height = 2 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        Assert.True (imageView.NewKeyDownEvent (new Key ('=')));
        Assert.True (imageView.ZoomLevel > 1d);

        host.Dispose ();
    }

    // Copilot
    [Fact]
    public void KeyBindings_MinusKey_ZoomsOut ()
    {
        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4) };
        View host = new () { Width = 2, Height = 2 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        imageView.ZoomLevel = 2d;
        Assert.True (imageView.NewKeyDownEvent (new Key ('-')));
        Assert.True (imageView.ZoomLevel < 2d);

        host.Dispose ();
    }

    // Copilot
    [Fact]
    public void KeyBindings_ZeroKey_ResetsZoom ()
    {
        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4) };
        View host = new () { Width = 2, Height = 2 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        imageView.ZoomLevel = 2d;
        Assert.True (imageView.NewKeyDownEvent (Key.D0));
        Assert.Equal (1d, imageView.ZoomLevel);

        host.Dispose ();
    }

    // Copilot
    [Fact]
    public void KeyBindings_PageUpStillZoomsIn ()
    {
        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4) };
        View host = new () { Width = 2, Height = 2 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        Assert.True (imageView.NewKeyDownEvent (Key.PageUp));
        Assert.True (imageView.ZoomLevel > 1d);

        host.Dispose ();
    }

    // Copilot
    [Fact]
    public void KeyBindings_PageDownStillZoomsOut ()
    {
        ImageView imageView = new () { Width = 2, Height = 2, Image = CreateCoordinateImage (4, 4) };
        View host = new () { Width = 2, Height = 2 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        imageView.ZoomLevel = 2d;
        Assert.True (imageView.NewKeyDownEvent (Key.PageDown));
        Assert.True (imageView.ZoomLevel < 2d);

        host.Dispose ();
    }

    #endregion Pan and Zoom

    #region Dispose

    [Fact]
    public void Dispose_CleansUpSixelData ()
    {
        ImageView imageView = new ();
        Color [,] pixels = CreateSolidImage (5, 5, new Color (128, 128, 128));
        imageView.Image = pixels;

        // Should not throw
        imageView.Dispose ();

        // Accessing after dispose is a code smell but should not crash
        Assert.NotNull (imageView.Image); // Still set; just disposed
    }

    [Fact]
    public void Dispose_WithNoImage_DoesNotThrow ()
    {
        ImageView imageView = new ();
        imageView.Dispose (); // Should not throw
    }

    #endregion Dispose

    #region IDesignable

    [Fact]
    public void EnableForDesign_SetsTestImage ()
    {
        ImageView imageView = new ();
        bool result = ((IDesignable)imageView).EnableForDesign ();

        Assert.True (result);
        Assert.NotNull (imageView.Image);
        Assert.Equal (20, imageView.Image!.GetLength (0)); // width
        Assert.Equal (10, imageView.Image.GetLength (1));  // height

        imageView.Dispose ();
    }

    [Fact]
    public void EnableForDesign_CreatesGradientImage ()
    {
        ImageView imageView = new ();
        ((IDesignable)imageView).EnableForDesign ();

        Color [,] image = imageView.Image!;

        // Top-left should be (0, 0, 128)
        Assert.Equal (new Color (0, 0, 128), image [0, 0]);

        // Bottom-right should be (255, 255, 128)
        Assert.Equal (new Color (255, 255, 128), image [19, 9]);

        imageView.Dispose ();
    }

    #endregion IDesignable

    #region SixelToRender IsDirty Flag

    [Fact]
    public void SixelToRender_IsDirty_DefaultsToTrue ()
    {
        SixelToRender sixel = new ();
        Assert.True (sixel.IsDirty);
    }

    [Fact]
    public void SixelToRender_IsDirty_CanBeSetToFalse ()
    {
        SixelToRender sixel = new () { IsDirty = false };
        Assert.False (sixel.IsDirty);
    }

    [Fact]
    public void SixelToRender_AlwaysRender_DefaultsToFalse ()
    {
        SixelToRender sixel = new ();
        Assert.False (sixel.AlwaysRender);
    }

    #endregion SixelToRender IsDirty Flag

    #region ViewportToScreenInPixels
    [Fact]
    public void ViewportToScreenInPixels_Throws_WhenNoSixelSupport ()
    {
        ImageView imageView = new () { Width = 10, Height = 5 };
        View host = new () { Width = 20, Height = 20 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // No App/Driver — should throw
        Assert.Throws<InvalidOperationException> (() => imageView.ViewportToScreenInPixels ());

        host.Dispose ();
    }

    [Fact]
    public void ViewportToScreenInPixels_ReturnsCorrectPixelRect ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        // Configure sixel support with 10x20 pixel resolution per cell
        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        ImageView imageView = new () { Width = 8, Height = 4, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Rectangle pixelRect = imageView.ViewportToScreenInPixels ();

        // Width: 8 cells * 10 px/cell = 80 px
        Assert.Equal (80, pixelRect.Width);

        // Height: 4 cells * 20 px/cell = 80 px (via GetHeightInPixels)
        Assert.Equal (80, pixelRect.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void ViewportToScreenInPixels_AccountsForViewPosition ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        ImageView imageView = new () { X = 3, Y = 2, Width = 5, Height = 3, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Rectangle pixelRect = imageView.ViewportToScreenInPixels ();

        // X: 3 cells * 10 px/cell = 30 px
        Assert.Equal (30, pixelRect.X);

        // Y: 2 cells * 20 px/cell = 40 px
        Assert.Equal (40, pixelRect.Y);

        // Width: 5 cells * 10 px/cell = 50 px
        Assert.Equal (50, pixelRect.Width);

        // Height: 3 cells * 20 px/cell = 60 px
        Assert.Equal (60, pixelRect.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void ViewportToScreenInPixels_DifferentResolution ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        // Use a non-default resolution (8x16)
        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (8, 16) });

        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Rectangle pixelRect = imageView.ViewportToScreenInPixels ();

        // Width: 10 cells * 8 px/cell = 80 px
        Assert.Equal (80, pixelRect.Width);

        // Height: 5 cells * 16 px/cell = 80 px
        Assert.Equal (80, pixelRect.Height);

        runnable.Dispose ();
    }

    #endregion ViewportToScreenInPixels

    #region FitImageInViewportInPixels

    [Fact]
    public void FitImageInViewportInPixels_ZeroSizeImage_ReturnsEmpty ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Size result = imageView.FitImageInViewportInPixels (new Size (0, 0));
        Assert.Equal (Size.Empty, result);

        result = imageView.FitImageInViewportInPixels (new Size (100, 0));
        Assert.Equal (Size.Empty, result);

        result = imageView.FitImageInViewportInPixels (new Size (0, 100));
        Assert.Equal (Size.Empty, result);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportInPixels_ImageFitsExactly ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // Viewport: 10 cells * 10 px = 100 px wide, 5 cells * 20 px = 100 px tall
        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Image is exactly the viewport pixel size
        Size result = imageView.FitImageInViewportInPixels (new Size (100, 100));

        Assert.Equal (100, result.Width);
        Assert.Equal (100, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportInPixels_WideImage_ScalesToFitWidth ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // Viewport: 10 cells * 10 px = 100 px wide, 5 cells * 20 px = 100 px tall
        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Image is 200x100 (2:1 aspect) — width-constrained
        // Scale = min(100/200, 100/100) = min(0.5, 1.0) = 0.5
        // Result: 200*0.5 = 100 wide, 100*0.5 = 50 tall
        Size result = imageView.FitImageInViewportInPixels (new Size (200, 100));

        Assert.Equal (100, result.Width);
        Assert.Equal (50, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportInPixels_TallImage_ScalesToFitHeight ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // Viewport: 10 cells * 10 px = 100 px wide, 5 cells * 20 px = 100 px tall
        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Image is 100x200 (1:2 aspect) — height-constrained
        // Scale = min(100/100, 100/200) = min(1.0, 0.5) = 0.5
        // Result: 100*0.5 = 50 wide, 200*0.5 = 100 tall
        Size result = imageView.FitImageInViewportInPixels (new Size (100, 200));

        Assert.Equal (50, result.Width);
        Assert.Equal (100, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportInPixels_SmallImage_ScalesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // Viewport: 10 cells * 10 px = 100 px wide, 5 cells * 20 px = 100 px tall
        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Image is 10x20 (1:2 aspect) — height-constrained
        // Scale = min(100/10, 100/20) = min(10, 5) = 5
        // Result: 10*5 = 50 wide, 20*5 = 100 tall
        Size result = imageView.FitImageInViewportInPixels (new Size (10, 20));

        Assert.Equal (50, result.Width);
        Assert.Equal (100, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportInPixels_VerySmallImage_ClampsToMinimumOne ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // 1x1 viewport = 10x20 pixel viewport
        ImageView imageView = new () { Width = 1, Height = 1, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Even with a 1000x1 image scaled to fit 10x20, width and height should be >= 1
        Size result = imageView.FitImageInViewportInPixels (new Size (1000, 1));

        Assert.True (result.Width >= 1);
        Assert.True (result.Height >= 1);

        runnable.Dispose ();
    }

    #endregion FitImageInViewportInPixels

    #region FitImageInViewportCells

    [Fact]
    public void FitImageInViewportCells_ZeroSizeImage_ReturnsEmpty ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Size result = imageView.FitImageInViewportCells (new Size (0, 0));
        Assert.Equal (Size.Empty, result);

        result = imageView.FitImageInViewportCells (new Size (100, 0));
        Assert.Equal (Size.Empty, result);

        result = imageView.FitImageInViewportCells (new Size (0, 100));
        Assert.Equal (Size.Empty, result);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportCells_SquareImage_AccountsForCellAspectRatio ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        // Cell resolution 10x20: cell aspect ratio = 20/10 = 2.0
        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // Viewport: 10 cells wide x 5 cells tall
        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // 100x100 pixel image:
        // After aspect adjustment: imageSize = (100, 100/2.0) = (100, 50)
        // widthScale = 10/100 = 0.1, heightScale = 5/50 = 0.1
        // scale = 0.1, result = (100*0.1, 50*0.1) = (10, 5)
        Size result = imageView.FitImageInViewportCells (new Size (100, 100));

        Assert.Equal (10, result.Width);
        Assert.Equal (5, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportCells_WideImage_ConstrainedByWidth ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // Viewport: 10 cells wide x 10 cells tall
        ImageView imageView = new () { Width = 10, Height = 10, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // 200x100 pixel image:
        // After aspect adjustment: imageSize = (200, 100/2.0) = (200, 50)
        // widthScale = 10/200 = 0.05, heightScale = 10/50 = 0.2
        // scale = 0.05, result = (200*0.05, 50*0.05) = (10, 2)
        Size result = imageView.FitImageInViewportCells (new Size (200, 100));

        Assert.Equal (10, result.Width);
        Assert.Equal (2, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportCells_TallImage_ConstrainedByHeight ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // Viewport: 10 cells wide x 5 cells tall
        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // 100x400 pixel image:
        // After aspect adjustment: imageSize = (100, 400/2.0) = (100, 200)
        // widthScale = 10/100 = 0.1, heightScale = 5/200 = 0.025
        // scale = 0.025, result = (100*0.025, 200*0.025) = (2, 5)
        Size result = imageView.FitImageInViewportCells (new Size (100, 400));

        Assert.Equal (2, result.Width);
        Assert.Equal (5, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportCells_SmallImage_ScalesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // Viewport: 20 cells wide x 10 cells tall
        ImageView imageView = new () { Width = 20, Height = 10, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // 10x20 pixel image:
        // After aspect adjustment: imageSize = (10, 20/2.0) = (10, 10)
        // widthScale = 20/10 = 2.0, heightScale = 10/10 = 1.0
        // scale = 1.0, result = (10*1.0, 10*1.0) = (10, 10)
        Size result = imageView.FitImageInViewportCells (new Size (10, 20));

        Assert.Equal (10, result.Width);
        Assert.Equal (10, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportCells_NoDriver_UsesDefaultAspectRatio ()
    {
        // Without a driver, the fallback cell aspect ratio is 2.0
        ImageView imageView = new () { Width = 10, Height = 5 };
        View host = new () { Width = 20, Height = 20 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // 100x100 pixel image:
        // Default aspect ratio = 2.0 → imageSize = (100, 100/2.0) = (100, 50)
        // widthScale = 10/100 = 0.1, heightScale = 5/50 = 0.1
        // scale = 0.1, result = (100*0.1, 50*0.1) = (10, 5)
        Size result = imageView.FitImageInViewportCells (new Size (100, 100));

        Assert.Equal (10, result.Width);
        Assert.Equal (5, result.Height);

        host.Dispose ();
    }

    [Fact]
    public void FitImageInViewportCells_DifferentResolution_AdjustsAspectRatio ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        // Cell resolution 8x16: cell aspect ratio = 16/8 = 2.0
        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (8, 16) });

        // Viewport: 10 cells wide x 5 cells tall
        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // 80x80 pixel image:
        // After aspect adjustment: imageSize = (80, 80/2.0) = (80, 40)
        // widthScale = 10/80 = 0.125, heightScale = 5/40 = 0.125
        // scale = 0.125, result = (80*0.125, 40*0.125) = (10, 5)
        Size result = imageView.FitImageInViewportCells (new Size (80, 80));

        Assert.Equal (10, result.Width);
        Assert.Equal (5, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportCells_VerySmallImage_ClampsToMinimumOne ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // 1x1 viewport = very small cell area
        ImageView imageView = new () { Width = 1, Height = 1, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Even with a very wide image, result dimensions should be >= 1
        Size result = imageView.FitImageInViewportCells (new Size (1000, 1));

        Assert.True (result.Width >= 1);
        Assert.True (result.Height >= 1);

        runnable.Dispose ();
    }

    #endregion FitImageInViewportCells

    #region Resolution Selection Consistency

    // Copilot - Claude Sonnet 4.6
    // When both Sixel and Kitty are available, ViewportToScreenInPixels must use the Kitty
    // resolution (Kitty is the preferred protocol). Using the Sixel resolution for sizing
    // while Kitty does the actual rendering would cause mis-sized images.
    [Fact]
    public void ViewportToScreenInPixels_WhenBothSixelAndKittyAvailable_UsesKittyResolution ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;

        // Sixel: 10 px/cell. Kitty: 20 px/cell — deliberately different so we can tell which wins.
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 10) });
        driver.SetKittyGraphicsSupport (new KittyGraphicsSupportResult { IsSupported = true, Resolution = new Size (20, 20) });

        ImageView imageView = new () { Width = 2, Height = 2, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // With Kitty resolution (20 px/cell) and a 2×2 cell viewport the pixel rect is 40×40.
        // With Sixel resolution (10 px/cell) it would be 20×20 — wrong when Kitty is preferred.
        Rectangle pixelRect = imageView.ViewportToScreenInPixels ();

        Assert.Equal (40, pixelRect.Width);
        Assert.Equal (40, pixelRect.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void ViewportToScreenInPixels_WhenKittyActive_IgnoresSixelAvoidBottomScroll ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 10) });
        driver.SetKittyGraphicsSupport (new KittyGraphicsSupportResult { IsSupported = true, Resolution = new Size (10, 10) });

        ImageView imageView = new ()
        {
            Width = 2,
            Height = 4,
            SixelEncoder = new SixelEncoder { AvoidBottomScroll = true }
        };

        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Rectangle pixelRect = imageView.ViewportToScreenInPixels ();

        Assert.Equal (20, pixelRect.Width);
        Assert.Equal (40, pixelRect.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void ViewportToScreenInPixels_WhenKittyOutputDisabled_UsesSixelResolutionAndAvoidBottomScroll ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 10) });
        driver.SetKittyGraphicsSupport (new KittyGraphicsSupportResult { IsSupported = true, Resolution = new Size (20, 20) });
        driver.GetOutput ().UseKittyGraphics = false;

        ImageView imageView = new ()
        {
            Width = 2,
            Height = 4,
            SixelEncoder = new SixelEncoder { AvoidBottomScroll = true }
        };

        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Rectangle pixelRect = imageView.ViewportToScreenInPixels ();

        Assert.Equal (20, pixelRect.Width);
        Assert.Equal (36, pixelRect.Height);
        Assert.True (imageView.IsUsingSixel);

        runnable.Dispose ();
    }

    // Copilot - Claude Sonnet 4.6
    // FitImageInViewportCells must use Kitty resolution when both protocols are available,
    // because Kitty is the preferred protocol.
    [Fact]
    public void FitImageInViewportCells_WhenBothSixelAndKittyAvailable_UsesKittyResolution ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 20, Height = 10 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;

        // Sixel: 8×16 px/cell (cell aspect ratio 2.0). Kitty: 16×8 px/cell (aspect ratio 0.5).
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (8, 16) });
        driver.SetKittyGraphicsSupport (new KittyGraphicsSupportResult { IsSupported = true, Resolution = new Size (16, 8) });

        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Kitty cell aspect ratio = 8/16 = 0.5. 80×80 px image →
        //   adjusted height = 80 / 0.5 = 160 cells → constrained by height → (2, 5).
        // Sixel cell aspect ratio = 16/8 = 2.0. 80×80 px image →
        //   adjusted height = 80 / 2.0 = 40 cells → fits as (10, 5).
        Size result = imageView.FitImageInViewportCells (new Size (80, 80));

        // Must match the Kitty result, not the Sixel result.
        Assert.Equal (2, result.Width);
        Assert.Equal (5, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportCells_WhenKittyOutputDisabled_UsesSixelResolution ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 20, Height = 10 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (8, 16) });
        driver.SetKittyGraphicsSupport (new KittyGraphicsSupportResult { IsSupported = true, Resolution = new Size (16, 8) });
        driver.GetOutput ().UseKittyGraphics = false;

        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Size result = imageView.FitImageInViewportCells (new Size (80, 80));

        Assert.Equal (10, result.Width);
        Assert.Equal (5, result.Height);

        runnable.Dispose ();
    }

    #endregion Resolution Selection Consistency

    #region Raster Cleanup (issue #5543)

    // Claude - Opus 4.8
    // A raster ImageView removed from its SuperView (without being disposed) must not leave its
    // out-of-band Sixel/Kitty graphic resident in the output buffer. See issue #5543.
    [Fact]
    public void RasterImage_Released_WhenRemovedFromSuperView ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (10, 10), MaxPaletteColors = 256 });

        Runnable runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        ImageView imageView = new () { Width = 4, Height = 4, Image = CreateCoordinateImage (4, 4) };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Assert.Single (driver.GetOutputBuffer ().GetRasterImages ());

        runnable.Remove (imageView);
        app.LayoutAndDraw ();

        Assert.Empty (driver.GetOutputBuffer ().GetRasterImages ());

        imageView.Dispose ();
        runnable.Dispose ();
    }

    // Claude - Opus 4.8
    // A raster ImageView that becomes invisible must release its resident Sixel/Kitty graphic. See issue #5543.
    [Fact]
    public void RasterImage_Released_WhenHidden ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (10, 10), MaxPaletteColors = 256 });

        Runnable runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        ImageView imageView = new () { Width = 4, Height = 4, Image = CreateCoordinateImage (4, 4) };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Assert.Single (driver.GetOutputBuffer ().GetRasterImages ());

        imageView.Visible = false;
        app.LayoutAndDraw ();

        Assert.Empty (driver.GetOutputBuffer ().GetRasterImages ());

        runnable.Dispose ();
    }

    // Claude - Opus 4.8
    // A raster ImageView nested in a container that is removed (e.g. the container revealing the view
    // beneath it) must release its resident Sixel/Kitty graphic. This generalizes the issue #5543 case
    // beyond modal Dialogs to any overlapping container.
    [Fact]
    public void RasterImage_Released_WhenContainerRemoved ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (10, 10), MaxPaletteColors = 256 });

        Runnable runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        View container = new () { Width = 6, Height = 6 };
        ImageView imageView = new () { Width = 4, Height = 4, Image = CreateCoordinateImage (4, 4) };
        container.Add (imageView);
        runnable.Add (container);
        app.LayoutAndDraw ();

        Assert.Single (driver.GetOutputBuffer ().GetRasterImages ());

        runnable.Remove (container);
        app.LayoutAndDraw ();

        Assert.Empty (driver.GetOutputBuffer ().GetRasterImages ());

        container.Dispose ();
        runnable.Dispose ();
    }

    // Claude - Opus 4.8
    // The issue #5543 scenario: a modal runnable hosting a raster ImageView leaves its Sixel/Kitty graphic
    // on screen after it closes. When the modal session ends, its raster graphic must be released.
    [Fact]
    public void RasterImage_Released_WhenModalRunnableEnds ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (10, 10), MaxPaletteColors = 256 });
        app.Driver!.SetScreenSize (10, 10);

        Runnable main = new () { Width = 10, Height = 10 };
        app.Begin (main);
        app.LayoutAndDraw ();

        Assert.Empty (driver.GetOutputBuffer ().GetRasterImages ());

        // Run a modal runnable hosting a raster ImageView on top of the main view.
        Runnable dialog = new () { Width = 6, Height = 6 };
        ImageView imageView = new () { Width = 4, Height = 4, Image = CreateCoordinateImage (4, 4) };
        dialog.Add (imageView);
        SessionToken? dialogToken = app.Begin (dialog);
        app.LayoutAndDraw ();

        Assert.Single (driver.GetOutputBuffer ().GetRasterImages ());

        // Close the modal — the main view is revealed and the dialog's graphic must not linger.
        app.End (dialogToken!);
        app.LayoutAndDraw ();

        Assert.Empty (driver.GetOutputBuffer ().GetRasterImages ());

        dialog.Dispose ();
        main.Dispose ();
    }

    #endregion Raster Cleanup (issue #5543)

    #region Helper Methods

    /// <summary>Creates a solid-color image of the specified dimensions.</summary>
    private static Color [,] CreateSolidImage (int width, int height, Color color)
    {
        Color [,] image = new Color [width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                image [x, y] = color;
            }
        }

        return image;
    }

    private static Color [,] CreateCoordinateImage (int width, int height)
    {
        Color [,] image = new Color [width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                image [x, y] = new ((byte)(x * 40 + 10), (byte)(y * 40 + 20), (byte)(x * 10 + y));
            }
        }

        return image;
    }

    private static void AssertCellBackground (IDriver driver, int col, int row, Color expected)
    {
        Cell [,]? contents = driver.Contents;
        Assert.NotNull (contents);
        Assert.Equal (expected, contents! [row, col].Attribute!.Value.Background);
    }

    // Claude - Opus 4.8
    // A Kitty image draws below text (z=-1). When an ImageView grows, the framework can leave a glyph
    // in a now-interior viewport cell (e.g. the old border column) that resize invalidation never
    // clears — it would render as a stale line over the image. The raster draw must blank every
    // viewport cell so no stale glyph survives.
    [Fact]
    public void Draw_KittyRaster_ClearsStaleGlyphInViewport ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 20, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetKittyGraphicsSupport (new KittyGraphicsSupportResult { IsSupported = true, Resolution = new Size (10, 10) });

        ImageView imageView = new () { X = 0, Y = 0, Width = 6, Height = 6, Image = CreateGradientImage (12, 12) };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Plant a stale glyph in a cell inside the image's viewport (as resize leaves an old border).
        Rectangle viewport = imageView.ViewportToScreen ();
        int staleX = viewport.X + 1;
        int staleY = viewport.Y + 1;
        driver.GetOutputBuffer ().Contents! [staleY, staleX].Grapheme = "║";

        // Act — redraw the image.
        imageView.SetNeedsDraw ();
        app.LayoutAndDraw ();

        // Assert — the stale glyph was blanked so it cannot render over the z=-1 image.
        Assert.Equal (" ", driver.GetOutputBuffer ().Contents! [staleY, staleX].Grapheme);

        runnable.Dispose ();
    }

    // Copilot - Claude Opus 4.8
    // Kitty source-crop: the full image is transmitted once (a=t), then panning a zoomed-in image only
    // moves the crop with a placement update (a=p) — it must NOT re-transmit the pixels (no a=t). Re-sending
    // the whole image every step is what made a large preview flash on each pan ("whole pane flashes").
    [Fact]
    public void Draw_KittyRaster_SourceCrop_PanUpdatesPlacementWithoutRetransmit ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 16, Height = 16 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetKittyGraphicsSupport (new KittyGraphicsSupportResult { IsSupported = true, Resolution = new Size (10, 20) });
        driver.GetOutput ().UseKittyGraphics = true;

        // Zoom in so the image fills the viewport and is pannable.
        ImageView imageView = new () { X = 0, Y = 0, Width = 8, Height = 8, Image = CreateGradientImage (16, 16), ZoomLevel = 2d };
        runnable.Add (imageView);
        app.LayoutAndDraw ();
        string first = driver.GetOutput ().GetLastOutput ();

        Assert.Contains ("a=t,", first); // the full image is transmitted once...
        Assert.Contains ("a=p,", first); // ...and displayed via a placement.

        // Pan one cell — same image, different crop.
        imageView.NewKeyDownEvent (Key.CursorRight);
        app.LayoutAndDraw ();
        string panFrame = driver.GetOutput ().GetLastOutput ();

        Assert.Contains ("a=p,", panFrame); // the crop moves with a tiny placement update...
        Assert.DoesNotContain ("a=t,", panFrame); // ...with no pixel re-transmit (the flash fix).
        Assert.DoesNotContain ("a=d", panFrame); // ...and no delete.

        runnable.Dispose ();
    }

    // Copilot - Claude Opus 4.8
    // A fresh Image must re-transmit (the placement would otherwise show stale pixels), and it must be
    // double-buffered: the new pixels go to the OTHER image id and are placed before the old id is deleted,
    // so a sharpen-on-zoom re-raster never blanks the on-screen image.
    [Fact]
    public void Draw_KittyRaster_SourceCrop_DoubleBuffersOnImageChange ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 16, Height = 16 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetKittyGraphicsSupport (new KittyGraphicsSupportResult { IsSupported = true, Resolution = new Size (10, 20) });
        driver.GetOutput ().UseKittyGraphics = true;

        ImageView imageView = new () { X = 0, Y = 0, Width = 8, Height = 8, Image = CreateGradientImage (16, 16), ZoomLevel = 2d };
        runnable.Add (imageView);
        app.LayoutAndDraw ();
        driver.GetOutput ().GetLastOutput ();

        imageView.NewKeyDownEvent (Key.CursorRight);
        app.LayoutAndDraw ();
        Assert.DoesNotContain ("a=t,", driver.GetOutput ().GetLastOutput ()); // pan: no re-transmit

        // A brand-new image re-transmits to the OTHER buffer and deletes the previous one.
        imageView.Image = CreateGradientImage (16, 16);
        app.LayoutAndDraw ();
        string frame = driver.GetOutput ().GetLastOutput ();

        System.Text.RegularExpressions.Match transmit = System.Text.RegularExpressions.Regex.Match (frame, @"a=t,f=32,i=(\d+)");
        System.Text.RegularExpressions.Match delete = System.Text.RegularExpressions.Regex.Match (frame, @"a=d,d=i,i=(\d+)");

        Assert.True (transmit.Success, "the new image must be transmitted");
        Assert.True (delete.Success, "the previous image must be deleted");

        // The deleted id is the OLD image id — different from the just-transmitted one — proving the new
        // pixels did not overwrite the on-screen image in place (no blank during the swap).
        Assert.NotEqual (transmit.Groups [1].Value, delete.Groups [1].Value);

        runnable.Dispose ();
    }

    // Copilot - Claude Opus 4.8
    // Re-assigning ImageView.Image — even the SAME Color[,] instance — bumps the image version and must
    // re-transmit (a=t). Keying "did the pixels change" on the array reference alone misses a buffer a
    // caller reused for a new render, leaving the terminal showing stale pixels. (Mirrors the legacy path,
    // which re-encodes on every image-version bump.)
    [Fact]
    public void Draw_KittyRaster_SourceCrop_RetransmitsWhenImageReassignedSameInstance ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 16, Height = 16 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetKittyGraphicsSupport (new KittyGraphicsSupportResult { IsSupported = true, Resolution = new Size (10, 20) });
        driver.GetOutput ().UseKittyGraphics = true;

        Color [,] image = CreateGradientImage (16, 16);
        ImageView imageView = new () { X = 0, Y = 0, Width = 8, Height = 8, Image = image, ZoomLevel = 2d };
        runnable.Add (imageView);
        app.LayoutAndDraw ();
        Assert.Contains ("a=t,", driver.GetOutput ().GetLastOutput ()); // first transmit

        imageView.NewKeyDownEvent (Key.CursorRight);
        app.LayoutAndDraw ();
        Assert.DoesNotContain ("a=t,", driver.GetOutput ().GetLastOutput ()); // pan: no re-transmit

        // Re-assign the SAME instance: the image version bumps, so the resident image must be refreshed.
        imageView.Image = image;
        app.LayoutAndDraw ();
        Assert.Contains ("a=t,", driver.GetOutput ().GetLastOutput ());

        runnable.Dispose ();
    }

    // Copilot - Claude Opus 4.8
    // ToAnsi() must recreate the *visible crop* of a panned/zoomed Kitty image, not the whole source. Two
    // different pan positions of the same image therefore export different raster payloads; encoding
    // command.Pixels (the full image) and ignoring SourceRect would make the two exports identical.
    [Fact]
    public void ToAnsi_KittyRaster_SourceCrop_ReflectsVisibleCrop ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 16, Height = 16 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetKittyGraphicsSupport (new KittyGraphicsSupportResult { IsSupported = true, Resolution = new Size (10, 20) });
        driver.GetOutput ().UseKittyGraphics = true;

        ImageView imageView = new () { X = 0, Y = 0, Width = 8, Height = 8, Image = CreateGradientImage (16, 16), ZoomLevel = 2d };
        runnable.Add (imageView);
        app.LayoutAndDraw ();
        string ansiLeft = driver.ToAnsi ();

        // Pan to a different crop of the same image.
        imageView.NewKeyDownEvent (Key.CursorRight);
        imageView.NewKeyDownEvent (Key.CursorRight);
        app.LayoutAndDraw ();
        string ansiPanned = driver.ToAnsi ();

        Assert.NotEqual (ansiLeft, ansiPanned);

        runnable.Dispose ();
    }

    // Copilot - Claude Opus 4.8
    // MapFragmentToSource must never return a crop that extends past the source rectangle: rounding can push
    // the last fragment's x/width (or y/height) beyond the source, which would emit out-of-bounds Kitty crop
    // coordinates. Here dest 2x2 cells over a 3x3 source rounds the bottom-right 1x1 fragment past the edge.
    [Fact]
    public void MapFragmentToSource_ClampsToSourceRect ()
    {
        Rectangle mapped = OutputBase.MapFragmentToSource (
                                                           new Rectangle (0, 0, 3, 3),
                                                           new Rectangle (0, 0, 2, 2),
                                                           new Rectangle (1, 1, 1, 1));

        Assert.True (mapped.X >= 0 && mapped.Y >= 0, $"crop origin {mapped.Location} is negative");
        Assert.True (mapped.Right <= 3, $"crop right {mapped.Right} exceeds source width 3");
        Assert.True (mapped.Bottom <= 3, $"crop bottom {mapped.Bottom} exceeds source height 3");
        Assert.True (mapped.Width >= 1 && mapped.Height >= 1, "crop must stay at least 1x1");
    }

    /// <summary>Creates a gradient image where pixel color varies by position.</summary>
    private static Color [,] CreateGradientImage (int width, int height)
    {
        Color [,] image = new Color [width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                byte r = (byte)(x * 255 / Math.Max (1, width - 1));
                byte g = (byte)(y * 255 / Math.Max (1, height - 1));
                image [x, y] = new Color (r, g, 128);
            }
        }

        return image;
    }

    private sealed class NonInvalidatingImageView : ImageView
    {
        protected override void OnFrameChanged (in Rectangle frame) { }
    }

    #endregion Helper Methods
}

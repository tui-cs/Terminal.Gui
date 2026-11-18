// Copilot

using UnitTests;

namespace ViewsTests;

/// <summary>
///     Parallelizable tests for <see cref="ProgressBar"/> behavior.
/// </summary>
public class ProgressBarTests : TestDriverBase
{
    [Fact]
    public void ProgressBarStyle_Setter_UpdatesSegmentCharacter ()
    {
        ProgressBar pb = new ();

        pb.ProgressBarStyle = ProgressBarStyle.Blocks;
        Assert.Equal (Glyphs.BlocksMeterSegment, pb.SegmentCharacter);

        pb.ProgressBarStyle = ProgressBarStyle.Continuous;
        Assert.Equal (Glyphs.ContinuousMeterSegment, pb.SegmentCharacter);

        pb.ProgressBarStyle = ProgressBarStyle.MarqueeBlocks;
        Assert.Equal (Glyphs.BlocksMeterSegment, pb.SegmentCharacter);

        pb.ProgressBarStyle = ProgressBarStyle.MarqueeContinuous;
        Assert.Equal (Glyphs.ContinuousMeterSegment, pb.SegmentCharacter);

        pb.ProgressBarStyle = ProgressBarStyle.Fire;
        Assert.Equal (Glyphs.ContinuousMeterSegment, pb.SegmentCharacter);
    }

    [Fact]
    public void Text_Setter_NotMarqueeStyle_ShowsPercentage ()
    {
        // In non-marquee mode, Text always returns the formatted percentage.
        ProgressBar pb = new () { Fraction = 0.25F };
        pb.Text = "ignored";

        Assert.Equal ("25%", pb.Text);
    }

    [Fact]
    public void Fraction_Half_Renders_HalfFilledBar ()
    {
        // Width=4, Fraction=0.5 means blocks at cols 0,1 and spaces at cols 2,3.
        IDriver driver = CreateTestDriver (4, 1);
        driver.Clip = new (driver.Screen);

        ProgressBar pb = new () { Driver = driver, Width = 4, Fraction = 0.5F };
        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        pb.Draw ();

        var block = Glyphs.BlocksMeterSegment.ToString ();
        Assert.Equal (block, driver.Contents! [0, 0].Grapheme);
        Assert.Equal (block, driver.Contents [0, 1].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 2].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 3].Grapheme);
    }

    // Copilot - GPT-5.5
    [Fact]
    public void FireStyle_NoSixelSupport_RendersContinuousFallback ()
    {
        IDriver driver = CreateTestDriver (4, 1);
        driver.Clip = new (driver.Screen);

        ProgressBar pb = new () { Driver = driver, Width = 4, Fraction = 0.5F, ProgressBarStyle = ProgressBarStyle.Fire };
        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        pb.Draw ();

        var continuous = Glyphs.ContinuousMeterSegment.ToString ();
        Assert.Equal (continuous, driver.Contents! [0, 0].Grapheme);
        Assert.Equal (continuous, driver.Contents [0, 1].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 2].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 3].Grapheme);
        Assert.Empty (driver.GetOutputBuffer ().GetRasterImages ());
    }

    // Copilot - GPT-5.5
    [Fact]
    public void FireStyle_SixelSupport_AddsRasterImageForFilledCells ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver (4, 2);
        driver.Clip = new (driver.Screen);
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (2, 3) });

        ProgressBar pb = new ()
        {
            Driver = driver,
            Width = 4,
            Height = 2,
            Fraction = 0.5F,
            ProgressBarStyle = ProgressBarStyle.Fire
        };
        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        pb.Draw ();

        RasterImageCommand command = Assert.Single (driver.GetOutputBuffer ().GetRasterImages ());
        Assert.Equal (new (0, 0, 2, 2), command.DestinationCells);
        Assert.Equal (4, command.Pixels!.GetLength (0));
        Assert.Equal (6, command.Pixels.GetLength (1));
        Assert.IsAssignableFrom<IStaticPaletteBuilder> (command.Encoder!.Quantizer.PaletteBuildingAlgorithm);
        Assert.False (driver.Contents! [0, 0].IsDirty);
        Assert.Equal (" ", driver.Contents [0, 2].Grapheme);
    }

    // Copilot - GPT-5.5
    [Fact]
    public void FireStyle_SixelSupport_ReusesEncoderAcrossFrames ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver (4, 2);
        driver.Clip = new (driver.Screen);
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (2, 3) });

        ProgressBar pb = new ()
        {
            Driver = driver,
            Width = 4,
            Height = 2,
            Fraction = 0.5F,
            ProgressBarStyle = ProgressBarStyle.Fire
        };
        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        pb.Draw ();
        SixelEncoder encoder = Assert.Single (driver.GetOutputBuffer ().GetRasterImages ()).Encoder!;

        pb.Draw ();

        Assert.Same (encoder, Assert.Single (driver.GetOutputBuffer ().GetRasterImages ()).Encoder);
    }

    // Copilot - GPT-5.5
    [Fact]
    public void FireStyle_SixelSupport_WritesPercentageAfterRasterImageOnRepeatedFrames ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver (8, 1);
        driver.Clip = new (driver.Screen);
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (1, 6) });

        ProgressBar pb = new ()
        {
            Driver = driver,
            Width = 8,
            Fraction = 0.5F,
            ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage,
            ProgressBarStyle = ProgressBarStyle.Fire
        };
        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        pb.Draw ();
        driver.Refresh ();

        pb.Draw ();
        driver.Refresh ();

        string output = driver.GetOutput ().GetLastOutput ();
        int sixelEnd = output.IndexOf ("\u001b\\", StringComparison.Ordinal);
        int percentage = output.IndexOf ("50%", StringComparison.Ordinal);

        Assert.True (sixelEnd >= 0);
        Assert.True (percentage > sixelEnd);
    }

    // Copilot - GPT-5.5
    [Fact]
    public void FireStyle_SwitchingAway_RemovesRasterImage ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver (4, 1);
        driver.Clip = new (driver.Screen);
        driver.SetSixelSupport (new () { IsSupported = true, Resolution = new (2, 3) });

        ProgressBar pb = new () { Driver = driver, Width = 4, Fraction = 0.5F, ProgressBarStyle = ProgressBarStyle.Fire };
        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();
        pb.Draw ();
        Assert.Single (driver.GetOutputBuffer ().GetRasterImages ());

        pb.ProgressBarStyle = ProgressBarStyle.Continuous;

        Assert.Empty (driver.GetOutputBuffer ().GetRasterImages ());
    }

    [Fact]
    public void Pulse_FirstCall_DrawsMarkerAtStartPosition ()
    {
        // Width=5 initializes a single activity marker at pos 0.
        // First Pulse sets activity mode; the single marker is at column 0.
        IDriver driver = CreateTestDriver (5, 1);
        driver.Clip = new (driver.Screen);

        ProgressBar pb = new () { Driver = driver, Width = 5, ProgressBarStyle = ProgressBarStyle.MarqueeBlocks };
        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        pb.Pulse ();
        pb.Draw ();

        var block = Glyphs.BlocksMeterSegment.ToString ();
        Assert.Equal (block, driver.Contents! [0, 0].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 1].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 2].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 3].Grapheme);
        Assert.Equal (" ", driver.Contents [0, 4].Grapheme);
    }

    [Fact]
    public void SyncWithTerminal_Fraction_Writes_Osc_Progress ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new (driver);
        ProgressBar pb = new () { Driver = driver, SyncWithTerminal = true };

        pb.Fraction = 0.5F;

        Assert.Contains (EscSeqUtils.OSC_SetProgressValue (50), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void SyncWithTerminal_Pulse_Writes_Indeterminate_Osc_Progress ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver (5, 1);
        driver.ProgressIndicator = new (driver);
        driver.Clip = new (driver.Screen);
        ProgressBar pb = new () { Driver = driver, SyncWithTerminal = true, Width = 5 };

        pb.BeginInit ();
        pb.EndInit ();
        pb.Frame = new (0, 0, 5, 1);
        pb.LayoutSubViews ();

        pb.Pulse ();

        Assert.Contains (EscSeqUtils.OSC_SetProgressIndeterminate (), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void SyncWithTerminal_Hidden_ProgressBar_Still_Writes_Osc_Progress ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new (driver);
        ProgressBar pb = new () { Driver = driver, SyncWithTerminal = true, Visible = false };

        pb.Fraction = 0.25F;

        Assert.Contains (EscSeqUtils.OSC_SetProgressValue (25), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void SyncWithTerminal_LegacyConsole_Does_Not_Write_Osc_Progress ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new (driver);
        driver.IsLegacyConsole = true;
        ProgressBar pb = new () { Driver = driver, SyncWithTerminal = true };

        pb.Fraction = 0.5F;

        Assert.DoesNotContain (EscSeqUtils.OSC_SetProgressValue (50), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void SyncWithTerminal_Disabling_Clears_Terminal_Progress ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new (driver);
        ProgressBar pb = new () { Driver = driver, SyncWithTerminal = true };

        pb.Fraction = 0.5F;
        pb.SyncWithTerminal = false;

        Assert.Contains (EscSeqUtils.OSC_ClearProgress (), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    // Copilot - GPT-5.5
    [Fact]
    public void Activate_CyclesStyles_WhenFocusable ()
    {
        ProgressBar pb = new () { CanFocus = true };

        pb.InvokeCommand (Command.Activate);
        Assert.Equal (ProgressBarStyle.Continuous, pb.ProgressBarStyle);

        pb.InvokeCommand (Command.Activate);
        Assert.Equal (ProgressBarStyle.MarqueeBlocks, pb.ProgressBarStyle);

        pb.InvokeCommand (Command.Activate);
        Assert.Equal (ProgressBarStyle.MarqueeContinuous, pb.ProgressBarStyle);

        pb.InvokeCommand (Command.Activate);
        Assert.Equal (ProgressBarStyle.Fire, pb.ProgressBarStyle);

        pb.InvokeCommand (Command.Activate);
        Assert.Equal (ProgressBarStyle.Blocks, pb.ProgressBarStyle);
    }

    // Copilot - GPT-5.5
    [Fact]
    public void Activate_DoesNotCycleStyles_WhenNotFocusable ()
    {
        ProgressBar pb = new ();

        pb.InvokeCommand (Command.Activate);

        Assert.Equal (ProgressBarStyle.Blocks, pb.ProgressBarStyle);
    }

    // Copilot - GPT-5.5
    [Fact]
    public void EnableForDesign_MakesProgressBarFocusable ()
    {
        ProgressBar pb = new ();

        pb.EnableForDesign ();

        Assert.True (pb.CanFocus);
    }

    [Fact]
    public void Dispose_Without_SyncWithTerminal_Does_Not_Write_Clear_Progress ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new (driver);
        ProgressBar pb = new () { Driver = driver };

        pb.Dispose ();

        Assert.DoesNotContain (EscSeqUtils.OSC_ClearProgress (), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    // Claude - Opus 4.8
    // The Fire progress style renders via a raster image, so it must work under Kitty graphics —
    // not just Sixel. On Kitty/Ghostty (which report no Sixel support) the fire silently vanished
    // because DrawFireProgress gated solely on SixelSupport.
    [Fact]
    public void Fire_WithKittyGraphicsActive_AddsRasterImage ()
    {
        IDriver driver = CreateTestDriver (8, 1);
        driver.Clip = new (driver.Screen);
        driver.SetKittyGraphicsSupport (new KittyGraphicsSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        Assert.True (driver.GetOutput ().UseKittyGraphics);

        ProgressBar pb = new () { Driver = driver, Width = 8, Height = 1, Fraction = 1F, ProgressBarStyle = ProgressBarStyle.Fire };
        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        pb.Draw ();

        Assert.Single (driver.GetOutputBuffer ().GetRasterImages ());
    }

    // Claude - Opus 4.8
    [Fact]
    public void Fire_WithNoRasterSupport_AddsNoRasterImage ()
    {
        IDriver driver = CreateTestDriver (8, 1);
        driver.Clip = new (driver.Screen);

        ProgressBar pb = new () { Driver = driver, Width = 8, Height = 1, Fraction = 1F, ProgressBarStyle = ProgressBarStyle.Fire };
        pb.BeginInit ();
        pb.EndInit ();
        pb.LayoutSubViews ();

        pb.Draw ();

        Assert.Empty (driver.GetOutputBuffer ().GetRasterImages ());
    }
}

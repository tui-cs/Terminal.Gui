namespace DriverTests;

/// <summary>
///     Verifies the public <see cref="IDriver.SetSixelSupport"/> / <see cref="IDriver.SetKittyGraphicsSupport"/>
///     setters. These let a host override capability detection — for example, to force inline-image output on
///     a headless or PTY driver that never runs the terminal handshake.
/// </summary>
[Collection ("Driver Tests")]
public class GraphicsSupportSetterTests
{
    private static DriverImpl NewDriver (out AnsiOutput output)
    {
        output = new ();

        return new (
                    new AnsiComponentFactory (),
                    new AnsiInputProcessor (null!),
                    new OutputBufferImpl (),
                    output,
                    new (new AnsiResponseParser (new SystemTimeProvider ())),
                    new SizeMonitorImpl (output));
    }

    [Fact]
    public void SetSixelSupport_UpdatesProperty_AndRaisesChanged ()
    {
        IDriver driver = NewDriver (out _);
        SixelSupportResult? raised = null;
        driver.SixelSupportChanged += (_, e) => raised = e.NewValue;

        var result = new SixelSupportResult { IsSupported = true, MaxPaletteColors = 256, SupportsTransparency = true };
        driver.SetSixelSupport (result);

        Assert.Same (result, driver.SixelSupport);
        Assert.Same (result, raised);
    }

    [Fact]
    public void SetKittyGraphicsSupport_WhenSupported_EnablesOutput_AndRaisesChanged ()
    {
        IDriver driver = NewDriver (out AnsiOutput output);
        KittyGraphicsSupportResult? raised = null;
        driver.KittyGraphicsSupportChanged += (_, e) => raised = e.NewValue;

        var result = new KittyGraphicsSupportResult { IsSupported = true };
        driver.SetKittyGraphicsSupport (result);

        Assert.Same (result, driver.KittyGraphicsSupport);
        Assert.Same (result, raised);
        Assert.True (output.UseKittyGraphics);
    }

    [Fact]
    public void SetKittyGraphicsSupport_WhenUnsupported_LeavesOutputDisabled ()
    {
        IDriver driver = NewDriver (out AnsiOutput output);

        driver.SetKittyGraphicsSupport (new KittyGraphicsSupportResult { IsSupported = false });

        Assert.False (output.UseKittyGraphics);
    }
}

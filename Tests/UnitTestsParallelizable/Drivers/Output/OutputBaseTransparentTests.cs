using System.Text;

namespace DriverTests.Output;

[Collection ("Driver Tests")]
public class OutputBaseTransparentTests
{
    [Fact]
    public void ToAnsi_TransparentForeground_EmitsCSI39m ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);

        // Set foreground to Transparent, background to a specific color
        Color bg = new (10, 20, 30);
        buffer.CurrentAttribute = new (Color.None, bg);
        buffer.AddStr ("X");

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: foreground should use reset (CSI 39m), background should use RGB
        Assert.Contains ("\u001b[39m", ansi);
        Assert.Contains ("\u001b[48;2;10;20;30m", ansi);
        Assert.Contains ("X", ansi);
    }

    [Fact]
    public void ToAnsi_TransparentBackground_EmitsCSI49m ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);

        // Set foreground to a specific color, background to Transparent
        Color fg = new (10, 20, 30);
        buffer.CurrentAttribute = new (fg, Color.None);
        buffer.AddStr ("X");

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: foreground should use RGB, background should use reset (CSI 49m)
        Assert.Contains ("\u001b[38;2;10;20;30m", ansi);
        Assert.Contains ("\u001b[49m", ansi);
        Assert.Contains ("X", ansi);
    }

    [Fact]
    public void ToAnsi_BothTransparent_EmitsCSI39m_And_CSI49m ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);

        // Set both foreground and background to Transparent
        buffer.CurrentAttribute = new (Color.None, Color.None);
        buffer.AddStr ("X");

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: both should use reset sequences
        Assert.Contains ("\u001b[39m", ansi);
        Assert.Contains ("\u001b[49m", ansi);
        Assert.Contains ("X", ansi);
    }

    [Fact]
    public void ToAnsi_NonTransparent_DoesNotEmitResetSequences ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);

        // Set both to specific non-transparent colors
        Color fg = new (100, 150, 200);
        Color bg = new (50, 75, 100);
        buffer.CurrentAttribute = new (fg, bg);
        buffer.AddStr ("X");

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: should use RGB sequences, not reset
        Assert.Contains ("\u001b[38;2;100;150;200m", ansi);
        Assert.Contains ("\u001b[48;2;50;75;100m", ansi);
        Assert.DoesNotContain ("\u001b[39m", ansi);
        Assert.DoesNotContain ("\u001b[49m", ansi);
    }

    [Fact]
    public void ToAnsi_TransparentForeground_With16Colors_StillEmitsCSI39m ()
    {
        // Arrange: force 16-color mode but Transparent should still use CSI 39m
        AnsiOutput output = new ();

        IDriver driver = new DriverImpl (
                                         new AnsiComponentFactory (),
                                         new AnsiInputProcessor (null!),
                                         new OutputBufferImpl (),
                                         output,
                                         new (new AnsiResponseParser (new SystemTimeProvider ())),
                                         new SizeMonitorImpl (output));

        driver.Force16Colors = true;

        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);

        Color bg = new (0, 128, 0); // Green
        buffer.CurrentAttribute = new (Color.None, bg);
        buffer.AddStr ("X");

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: Transparent foreground should emit CSI 39m even in 16-color mode
        Assert.Contains ("\u001b[39m", ansi);
        Assert.Contains ("X", ansi);

        driver.Dispose ();
    }

    [Fact]
    public void ToAnsi_TransparentBackground_With16Colors_StillEmitsCSI49m ()
    {
        // Arrange: force 16-color mode but Transparent should still use CSI 49m
        AnsiOutput output = new ();

        IDriver driver = new DriverImpl (
                                         new AnsiComponentFactory (),
                                         new AnsiInputProcessor (null!),
                                         new OutputBufferImpl (),
                                         output,
                                         new (new AnsiResponseParser (new SystemTimeProvider ())),
                                         new SizeMonitorImpl (output));

        driver.Force16Colors = true;

        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);

        Color fg = new (255, 0, 0); // Red
        buffer.CurrentAttribute = new (fg, Color.None);
        buffer.AddStr ("X");

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: Transparent background should emit CSI 49m even in 16-color mode
        Assert.Contains ("\u001b[49m", ansi);
        Assert.Contains ("X", ansi);

        driver.Dispose ();
    }
}

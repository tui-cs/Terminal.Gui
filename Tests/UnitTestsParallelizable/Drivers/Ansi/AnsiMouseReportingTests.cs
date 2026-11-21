using System.Text;

namespace DriverTests.AnsiDriver;

// CoPilot - GPT-6
public class AnsiMouseReportingTests
{
    private static AnsiOutput.UnixWriter Capture (StringBuilder bytes) =>
        chunk =>
        {
            bytes.Append (Encoding.UTF8.GetString (chunk));

            return true;
        };

    [Theory]
    [InlineData (AppModel.Inline, false)]
    [InlineData (AppModel.Inline, true)]
    [InlineData (AppModel.FullScreen, false)]
    [InlineData (AppModel.FullScreen, true)]
    public void Startup_HonorsMouseSettingWithoutEnablingAnOptedOutTerminal (AppModel model, bool disabled)
    {
        StringBuilder bytes = new ();
        using AnsiOutput output = new (Capture (bytes), model, disabled, initializeTerminal: true);

        string startup = bytes.ToString ();
        Assert.Contains (disabled ? EscSeqUtils.CSI_DisableMouseEvents : EscSeqUtils.CSI_EnableMouseEvents, startup);
        Assert.DoesNotContain (disabled ? EscSeqUtils.CSI_EnableMouseEvents : EscSeqUtils.CSI_DisableMouseEvents, startup);
        Assert.Contains (EscSeqUtils.CSI_EnableBracketedPaste, startup);
        Assert.Equal (model == AppModel.FullScreen, startup.Contains (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll));
    }

    [Fact]
    public void Resume_RestoresDisabledReporting ()
    {
        StringBuilder bytes = new ();
        using AnsiOutput output = new (Capture (bytes), AppModel.Inline, isMouseDisabled: true);

        output.RestoreMouseReporting ();

        Assert.Equal (EscSeqUtils.CSI_DisableMouseEvents, bytes.ToString ());
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void Dispose_DisablesReportingAndRestoresCursor (bool disabled)
    {
        StringBuilder bytes = new ();
        AnsiOutput output = new (Capture (bytes), AppModel.Inline, disabled);

        output.Dispose ();

        Assert.Contains (EscSeqUtils.CSI_DisableMouseEvents, bytes.ToString ());
        Assert.Contains (EscSeqUtils.CSI_ShowCursor, bytes.ToString ());
        Assert.DoesNotContain (EscSeqUtils.CSI_EnableMouseEvents, bytes.ToString ());
    }

    [Theory]
    [InlineData (0u)]
    [InlineData (0x0010u)]
    [InlineData (0x0040u)]
    [InlineData (0x03FFu)]
    public void WindowsMouseMode_AllowsSelectionAndPreservesUnrelatedFlags (uint original)
    {
        const uint mouseInput = 0x0010;
        const uint quickEdit = 0x0040;
        const uint extendedFlags = 0x0080;

        uint disabled = WindowsVTInputHelper.GetMouseMode (original, true);
        Assert.Equal (0u, disabled & mouseInput);
        Assert.Equal (quickEdit | extendedFlags, disabled & (quickEdit | extendedFlags));
        Assert.Equal (original & ~(mouseInput | quickEdit | extendedFlags), disabled & ~(mouseInput | quickEdit | extendedFlags));

        uint enabled = WindowsVTInputHelper.GetMouseMode (disabled, false);
        Assert.Equal (mouseInput | extendedFlags, enabled & (mouseInput | extendedFlags));
        Assert.Equal (0u, enabled & quickEdit);
        Assert.Equal (original & ~(mouseInput | quickEdit | extendedFlags), enabled & ~(mouseInput | quickEdit | extendedFlags));
    }
}

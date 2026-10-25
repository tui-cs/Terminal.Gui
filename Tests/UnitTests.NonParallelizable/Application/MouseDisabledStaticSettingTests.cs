using System.Text;

namespace UnitTests.NonParallelizable.ApplicationTests;

/// <summary>
///     Tests for how the process-wide <c>Application.IsMouseDisabled</c> setting reaches instance-based mouse state.
/// </summary>
public class MouseDisabledStaticSettingTests
{
    // Copilot - Claude Opus 5.5
    [Fact]
    public void NewInstance_IsSeededFromConfiguredDefault ()
    {
        bool original = ApplicationSettings.Defaults.IsMouseDisabled;
        ApplicationSettings.Defaults.IsMouseDisabled = true;

        try
        {
            using ApplicationMouse mouse = new ();

            Assert.True (mouse.IsMouseDisabled);
        }
        finally
        {
            ApplicationSettings.Defaults.IsMouseDisabled = original;
        }
    }

    // Copilot - Claude Opus 5.5
    [Fact]
    public void StaticChange_FailingInstanceDoesNotBlockOtherInstances ()
    {
        bool fail = false;
        AnsiOutput failingOutput = new (_ => !fail, AppModel.Inline);
        using ApplicationImpl failing = new (new AnsiComponentFactory (output: failingOutput)) { AppModel = AppModel.Inline };
        failing.Init ();

        StringBuilder bytes = new ();

        AnsiOutput workingOutput = new (chunk =>
                                        {
                                            bytes.Append (Encoding.UTF8.GetString (chunk));

                                            return true;
                                        },
                                        AppModel.Inline);
        using ApplicationImpl working = new (new AnsiComponentFactory (output: workingOutput)) { AppModel = AppModel.Inline };
        working.Init ();
        bytes.Clear ();
        fail = true;

        try
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Application.IsMouseDisabled = true;
#pragma warning restore CS0618

            Assert.False (failing.Mouse.IsMouseDisabled);
            Assert.True (working.Mouse.IsMouseDisabled);
            Assert.Equal (EscSeqUtils.CSI_DisableMouseEvents, bytes.ToString ());
        }
        finally
        {
            fail = false;
#pragma warning disable CS0618 // Type or member is obsolete
            Application.IsMouseDisabled = false;
#pragma warning restore CS0618
        }
    }
}

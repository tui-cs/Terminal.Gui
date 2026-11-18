// Copilot
#nullable enable
using UnitTests;

namespace ViewsTests;

/// <summary>
///     Parallelizable tests for <see cref="SpinnerView"/> render behavior.
/// </summary>
public class SpinnerViewTests : TestDriverBase
{
    [Fact]
    public void AdvanceAnimation_DefaultDelay_ThrottlesRapidCalls ()
    {
        // SpinnerStyle.Line default delay is 130 ms; calling immediately does not advance the frame.
        IDriver driver = CreateTestDriver (5, 1);
        driver.Clip = new Region (driver.Screen);

        SpinnerView view = new ()
        {
            Driver = driver,
            X = 0,
            Y = 0
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();
        string frame1 = driver.Contents! [0, 0].Grapheme;

        // Called immediately — not enough time has elapsed (130 ms throttle).
        view.AdvanceAnimation ();
        view.Draw ();
        string frame2 = driver.Contents [0, 0].Grapheme;

        Assert.Equal (frame1, frame2);
    }

    [Fact]
    public void AutoSpin_SetBeforeEndInit_GetterReturnsTrueWithNoApp ()
    {
        // Regression test for https://github.com/tui-cs/Terminal.Gui/issues/4879
        // Before the fix AutoSpin returned `_timeout != null`. When App is null the timeout
        // can never be registered, so the getter falsely returned false even though the
        // caller had set AutoSpin = true. The fix uses a dedicated _autoSpin backing field.
        SpinnerView spinner = new () { AutoSpin = true };

        // App is null here (no running application), so _timeout is null.
        // The getter must still report true based on the backing field.
        Assert.True (spinner.AutoSpin);

        spinner.BeginInit ();
        spinner.EndInit ();

        // After init the intent must be preserved.
        Assert.True (spinner.AutoSpin);
    }

    [Fact]
    public void AdvanceAnimation_ZeroDelay_AdvancesFrame ()
    {
        // SpinnerStyle.Line sequence: ["-", @"\", "|", "/"]
        // Constructor calls AdvanceAnimation() once (from DateTime.MinValue) → _currentIdx = 1 ("\").
        // After setting SpinDelay = 0 and sleeping 1 ms the next call advances to index 2 ("|").
        IDriver driver = CreateTestDriver (5, 1);
        driver.Clip = new Region (driver.Screen);

        SpinnerView view = new ()
        {
            Driver = driver,
            SpinDelay = 0,
            X = 0,
            Y = 0
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Guarantee >0 ms has elapsed since _lastRender was set in the constructor.
        Thread.Sleep (2);

        view.AdvanceAnimation ();
        view.Draw ();

        Assert.Equal ("|", driver.Contents! [0, 0].Grapheme);
    }

    [Fact]
    public void AutoSpin_WithSyncWithTerminal_True_Writes_Indeterminate ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new ProgressIndicator (driver);
        SpinnerView spinner = new () { Driver = driver, SyncWithTerminal = true };

        spinner.AutoSpin = true;

        Assert.Contains (EscSeqUtils.OSC_SetProgressIndeterminate (), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void AutoSpin_WithSyncWithTerminal_False_Clears_Indicator ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new ProgressIndicator (driver);
        SpinnerView spinner = new () { Driver = driver, SyncWithTerminal = true };

        spinner.AutoSpin = true;
        spinner.AutoSpin = false;

        Assert.Contains (EscSeqUtils.OSC_ClearProgress (), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void AutoSpin_WithoutSyncWithTerminal_DoesNotWrite_Indicator ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new ProgressIndicator (driver);
        SpinnerView spinner = new () { Driver = driver };

        spinner.AutoSpin = true;
        spinner.AutoSpin = false;

        string output = driver.GetOutput ().GetLastOutput ();
        Assert.DoesNotContain (EscSeqUtils.OSC_SetProgressIndeterminate (), output, StringComparison.Ordinal);
        Assert.DoesNotContain (EscSeqUtils.OSC_ClearProgress (), output, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncWithTerminal_SetFalse_WhileAutoSpinTrue_Clears_Indicator ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new ProgressIndicator (driver);
        SpinnerView spinner = new () { Driver = driver, SyncWithTerminal = true, AutoSpin = true };

        spinner.SyncWithTerminal = false;

        string output = driver.GetOutput ().GetLastOutput ();
        Assert.Contains (EscSeqUtils.OSC_SetProgressIndeterminate (), output, StringComparison.Ordinal);
        Assert.Contains (EscSeqUtils.OSC_ClearProgress (), output, StringComparison.Ordinal);
    }

    [Fact]
    public void EndInit_Syncs_ProgressIndicator_WhenAutoSpin_Already_True ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new ProgressIndicator (driver);
        SpinnerView spinner = new () { SyncWithTerminal = true, AutoSpin = true };
        spinner.Driver = driver;

        spinner.BeginInit ();
        spinner.EndInit ();

        Assert.Contains (EscSeqUtils.OSC_SetProgressIndeterminate (), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void Dispose_Clears_ProgressIndicator_WhenEnabled ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        driver.ProgressIndicator = new ProgressIndicator (driver);
        SpinnerView spinner = new () { Driver = driver, SyncWithTerminal = true, AutoSpin = true };

        spinner.Dispose ();

        Assert.Contains (EscSeqUtils.OSC_ClearProgress (), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }
}

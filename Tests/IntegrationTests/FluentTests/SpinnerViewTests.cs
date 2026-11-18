// Copilot
using System.Reflection;
using AppTestHelpers;
using AppTestHelpers.XunitHelpers;

namespace IntegrationTests;

/// <summary>
///     Integration tests for <see cref="SpinnerView"/> that require a running application.
/// </summary>
public class SpinnerViewTests (ITestOutputHelper outputHelper) : TestsAllDrivers
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void AutoSpin_SetBeforeAppRun_TimeoutRegisteredAfterEndInit (string d)
    {
        // Regression test for https://github.com/tui-cs/Terminal.Gui/issues/4879.
        //
        // Before the fix: AutoSpin = true set before App.Run() left _timeout null because
        // AddAutoSpinTimeout() silently exited early (App was null). The spinner showed its
        // first frame and never moved.
        //
        // After the fix: EndInit() calls AddAutoSpinTimeout() again once App is set, so the
        // timeout IS registered and the spinner animates.

        SpinnerView spinner = new () { AutoSpin = true };

        FieldInfo timeoutField = typeof (SpinnerView)
            .GetField ("_timeout", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Before being part of a running application, _timeout must be null.
        Assert.Null (timeoutField.GetValue (spinner));

        // Add the spinner to a running Window. Because the Window is already initialised,
        // View.Add() triggers BeginInit()/EndInit() on the spinner immediately, with App set.
        using AppTestHelper _ = With.A<Window> (40, 10, d, _out).Add (spinner);

        // EndInit() should have called AddAutoSpinTimeout(), which registers the timeout.
        Assert.NotNull (timeoutField.GetValue (spinner));
    }
}

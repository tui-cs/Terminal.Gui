using System.Drawing;
using AppTestHelpers;
using AppTestHelpers.XunitHelpers;

namespace IntegrationTests;

/// <summary>
///     Basic tests for AppTestHelper functionality including constructor, lifecycle, and resize operations.
/// </summary>
public class TestContextTests (ITestOutputHelper outputHelper) : TestsAllDrivers
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void DisableRealDriverIO_EnvironmentVariable_IsSet ()
    {
        // Diagnostic: verify the env var actually reaches the test process.
        string? value = Environment.GetEnvironmentVariable ("DisableRealDriverIO");
        outputHelper.WriteLine ($"DisableRealDriverIO = '{value ?? "(null)"}'");

        Assert.Equal ("1", value);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void IsAttachedToTerminal_ReturnsFalse_WhenDisableRealDriverIO_IsSet ()
    {
        // Arrange – the env var should already be "1" via the test harness.
        string? value = Environment.GetEnvironmentVariable ("DisableRealDriverIO");
        outputHelper.WriteLine ($"DisableRealDriverIO = '{value ?? "(null)"}'");

        // Act
        bool result = Driver.IsAttachedToTerminal (out bool inputAttached, out bool outputAttached);

        // Assert
        Assert.False (result, "IsAttachedToTerminal should return false when DisableRealDriverIO=1");
        Assert.False (inputAttached, "inputAttached should be false when DisableRealDriverIO=1");
        Assert.False (outputAttached, "outputAttached should be false when DisableRealDriverIO=1");
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Constructor_Sets_Application_Screen (string d)
    {
        using var context = new AppTestHelper (d, _out, TimeSpan.FromSeconds (10));

        Assert.NotEqual (Rectangle.Empty, context.App?.Screen);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void ResizeConsole_Resizes (string d)
    {
        var lbl = new Label
        {
            Width = Dim.Fill ()
        };

        using AppTestHelper c = With.A<Window> (40, 10, d)
                                        .Add (lbl)
                                        .AssertEqual (38, lbl.Frame.Width) // Window has 2 border
                                        .ResizeConsole (20, 20)
                                        .WaitIteration ()
                                        .AssertEqual (18, lbl.Frame.Width);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void With_New_A_Runs (string d)
    {
        using AppTestHelper helper = With.A<Window> (40, 10, d, _out);
        Assert.True (helper.App!.TopRunnable!.IsRunning);
        Assert.NotEqual (Rectangle.Empty, helper.App!.Screen);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void AnsiScreenShot_Renders_Ansi_Stream (string d)
    {
        using AppTestHelper helper = With.A<Window> (10, 3, d, _out)
                                              .Then (app =>
                                                     {
                                                         app.TopRunnableView!.BorderStyle = LineStyle.None;
                                                         app.TopRunnableView!.Border.Thickness = Thickness.Empty;
                                                         app.TopRunnableView.Text = "hello";
                                                     })
                                              .ScreenShot ("ScreenShot", _out)
                                              .AnsiScreenShot ("AnsiScreenShot", _out)
            ;
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void With_Starts_Stops_Without_Error (string d)
    {
        using AppTestHelper helper = With.A<Window> (40, 10, d, _out);

        // No actual assertions are needed — if no exceptions are thrown, it's working
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void RunAsync_Cancellation_After_Boot_Stops_Application ()
    {
        // Copilot
        using AppTestHelper helper = With.A<Window> (40, 10, DriverRegistry.Names.ANSI, _out);
        helper.CancelRun ();

        Assert.True (
            SpinWait.SpinUntil (() => helper.Finished, TimeSpan.FromSeconds (5)),
            "AppTestHelper did not finish after cancellation.");
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void Then_Exception_HardStops_Without_Hanging ()
    {
        // Copilot
        using AppTestHelper helper = With.A<Window> (40, 10, DriverRegistry.Names.ANSI, _out);
        InvalidOperationException expectedException = new ("Expected test failure");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException> (
            () => helper.Then (_ => throw expectedException));

        Assert.Same (expectedException, exception);
        Assert.True (
            SpinWait.SpinUntil (() => helper.Finished, TimeSpan.FromSeconds (5)),
            "AppTestHelper did not finish after action failure.");
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void With_Without_Stop_Still_Cleans_Up (string d)
    {
        AppTestHelper? context;

        using (context = With.A<Window> (40, 10, d, _out))
        {
            Assert.False (context.Finished);
        }

        Assert.True (context.Finished);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void AssertCursorPos_Works (string d)
    {
        // Simulates typing abcd into a TextField with width 3 (wide enough to render 2 characters only)
        using AppTestHelper c = With.A<Window> (100, 20, d, _out)
                                        .Add (
                                              new ()
                                              {
                                                  Height = 1,
                                                  Width = 1,
                                                  CanFocus = true
                                              })
                                        .Then (app =>
                                               {
                                                   app!.TopRunnableView!.SubViews.ElementAt (0).Cursor = new () { Style = CursorStyle.BlinkingBar, Position = new Point (1, 1) };
                                               })
                                        .AssertCursorPosition (new (1, 1)) // Initial cursor position (because Window has border)
            ;
    }
}

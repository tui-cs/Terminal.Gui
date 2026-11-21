using AppTestHelpers;
using Terminal.Gui.Drivers;

namespace IntegrationTests;

// CoPilot - GitHub Copilot
/// <summary>
///     Demonstrates <see cref="AppTestHelper.AssertAnsiSnapshot" />: render a screen, capture it
///     as pure ANSI into a golden, compare byte-for-byte thereafter. The recorded
///     <c>__snapshots__/*.ans</c> can be <c>cat</c>'d in a truecolor terminal to see the exact
///     look without an interactive run.
/// </summary>
public class AnsiSnapshotTests (ITestOutputHelper outputHelper)
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Fact]
    public void AssertAnsiSnapshot_Records_Then_Compares ()
    {
        using AppTestHelper c = With.A<Window> (20, 4, DriverRegistry.Names.ANSI, _out)
                                    .Add (
                                          new Label
                                          {
                                              X = 1,
                                              Y = 1,
                                              Text = "Hello, snapshot!"
                                          })
                                    .WaitIteration ()
                                    .AssertAnsiSnapshot (nameof (AssertAnsiSnapshot_Records_Then_Compares))
                                    .Stop ();
    }

    [Fact]
    public void AssertAnsiSnapshot_Mismatch_Stops_App_Before_Throwing ()
    {
        string oldSnapshotDir = Environment.GetEnvironmentVariable ("SNAPSHOT_DIR") ?? string.Empty;
        string oldUpdateSnapshots = Environment.GetEnvironmentVariable ("UPDATE_SNAPSHOTS") ?? string.Empty;
        string snapshotDir = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N"));
        string snapshotName = nameof (AssertAnsiSnapshot_Mismatch_Stops_App_Before_Throwing);
        AppTestHelper? c = null;

        try
        {
            Directory.CreateDirectory (snapshotDir);
            Environment.SetEnvironmentVariable ("SNAPSHOT_DIR", snapshotDir);
            Environment.SetEnvironmentVariable ("UPDATE_SNAPSHOTS", null);
            File.WriteAllText (Path.Combine (snapshotDir, snapshotName + ".ans"), "not the current screen");

            c = With.A<Window> (20, 4, DriverRegistry.Names.ANSI, _out)
                    .Add (
                          new Label
                          {
                              X = 1,
                              Y = 1,
                              Text = "Hello, snapshot!"
                          })
                    .WaitIteration ();

            AnsiSnapshotException exception = Assert.Throws<AnsiSnapshotException> (() => c.AssertAnsiSnapshot (snapshotName));

            Assert.Contains ("did not match", exception.Message);
            Assert.True (c.Finished);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("SNAPSHOT_DIR", string.IsNullOrEmpty (oldSnapshotDir) ? null : oldSnapshotDir);
            Environment.SetEnvironmentVariable ("UPDATE_SNAPSHOTS", string.IsNullOrEmpty (oldUpdateSnapshots) ? null : oldUpdateSnapshots);

            if (c is { Finished: false })
            {
                c.Dispose ();
            }

            if (Directory.Exists (snapshotDir))
            {
                Directory.Delete (snapshotDir, true);
            }
        }
    }
}

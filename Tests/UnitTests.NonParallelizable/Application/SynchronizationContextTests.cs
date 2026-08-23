// Copilot
// Claude - Fable 5
#nullable enable

namespace UnitTests.NonParallelizable.ApplicationTests;

/// <summary>
///     Tests for the <see cref="SynchronizationContext"/> contract of the Terminal.Gui application lifecycle.
///     As of #5636, <see cref="IApplication.Init"/> does NOT install the app's main-loop context as the
///     thread's ambient context — it becomes ambient only while a session is running (see Begin/Run) —
///     so an <c>await</c> between <c>Init</c> and <c>Run</c>/<c>RunAsync</c> cannot capture a context
///     whose continuations would be stranded on a not-yet-running main loop.
/// </summary>
public class SynchronizationContextTests
{
    [Fact]
    public void Init_LeavesAmbientContext_Dispose_LeavesForeignContext ()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext marker = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (marker);

            IApplication app = Application.Create ();

            try
            {
                app.Init (DriverRegistry.Names.ANSI);

                Assert.Same (marker, SynchronizationContext.Current);
            }
            finally
            {
                app.Dispose ();
            }

            Assert.Same (marker, SynchronizationContext.Current);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (previous);
        }
    }

    [Fact]
    public void App_SynchronizationContext_CreateCopy_ReturnsDifferentInstance ()
    {
        IApplication app = Application.Create ();

        try
        {
            app.Init (DriverRegistry.Names.ANSI);

            SynchronizationContext context = ((ApplicationImpl)app).SynchronizationContext!;
            SynchronizationContext copy = context.CreateCopy ();

            Assert.NotNull (copy);
            Assert.NotSame (context, copy);
        }
        finally
        {
            app.Dispose ();
        }
    }
}

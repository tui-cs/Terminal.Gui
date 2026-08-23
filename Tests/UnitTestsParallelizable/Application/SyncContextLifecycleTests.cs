// Claude - Fable 5
#nullable enable

namespace ApplicationTests;

/// <summary>
///     Tests for the ambient <see cref="SynchronizationContext"/> contract across the application
///     lifecycle (#5636): <see cref="IApplication.Init"/> must not install the app's main-loop context
///     as the thread's ambient context (an <c>await</c> before <c>Run</c> would capture a context whose
///     continuations only run once the loop is pumping — a startup deadlock). The app context is
///     ambient only while a session is running, and the caller's context is restored afterwards.
/// </summary>
[Collection ("Application Tests")]
public class SyncContextLifecycleTests
{
    [Fact]
    public void Init_DoesNotChangeAmbientSynchronizationContext ()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext marker = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (marker);

            IApplication app = Application.Create ();
            app.Init (DriverRegistry.Names.ANSI);

            Assert.Same (marker, SynchronizationContext.Current);

            app.Dispose ();

            Assert.Same (marker, SynchronizationContext.Current);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (previous);
        }
    }

    [Fact]
    public void Run_SetsAppSyncContextDuringRun_AndRestoresAmbientAfter ()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext marker = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (marker);

            IApplication app = Application.Create ();
            app.Init (DriverRegistry.Names.ANSI);

            SynchronizationContext? duringRun = null;

            void OnIteration (object? s, EventArgs<IApplication?> a)
            {
                duringRun = SynchronizationContext.Current;
                app.RequestStop ();
            }

            app.Iteration += OnIteration;

            using (Runnable runnable = new ())
            {
                app.Run (runnable);
            }

            app.Iteration -= OnIteration;

            Assert.Same (((ApplicationImpl)app).SynchronizationContext, duringRun);
            Assert.Same (marker, SynchronizationContext.Current);

            app.Dispose ();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (previous);
        }
    }

    [Fact]
    public void Post_AfterDispose_StillExecutesCallback ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        SynchronizationContext context = ((ApplicationImpl)app).SynchronizationContext!;
        app.Dispose ();

        // No main loop can ever pump this; the callback must not be stranded (or throw).
        using ManualResetEventSlim callbackCalled = new (false);
        context.Post (_ => callbackCalled.Set (), null);

        Assert.True (callbackCalled.Wait (TimeSpan.FromSeconds (2), TestContext.Current.CancellationToken));
    }
}

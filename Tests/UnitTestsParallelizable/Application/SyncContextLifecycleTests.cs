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

            IApplication app = CreateApplication ();
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

            IApplication app = CreateApplication ();
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

    // Helper: a distinct, recognizable caller context.
    private sealed class CallerSyncContext : SynchronizationContext
    {
    }

    [Fact]
    public void Run_SingleRunnable_InstallsAppContext_AndRestoresCallerContext ()
    {
        SynchronizationContext? before = SynchronizationContext.Current;

        try
        {
            using IApplication app = CreateApplication ();
            app.Init (DriverRegistry.Names.ANSI);
            SynchronizationContext appContext = ((ApplicationImpl)app).SynchronizationContext!;
            using Runnable runnable = new ();

            SynchronizationContext? duringBegin = null;
            SynchronizationContext? duringIteration = null;

            // Init alone does not install it.
            Assert.Same (before, SynchronizationContext.Current);
            Assert.NotSame (appContext, SynchronizationContext.Current);

            app.SessionBegun += OnSessionBegun;
            app.Iteration += OnIteration;
            app.Run (runnable);
            app.Iteration -= OnIteration;
            app.SessionBegun -= OnSessionBegun;

            // Installed before Begin ran, and while the loop ran.
            Assert.Same (appContext, duringBegin);
            Assert.Same (appContext, duringIteration);

            // Restored once Run returns.
            Assert.Same (before, SynchronizationContext.Current);

            return;

            void OnSessionBegun (object? _, SessionTokenEventArgs __) => duringBegin = SynchronizationContext.Current;

            void OnIteration (object? _, EventArgs<IApplication?> __)
            {
                duringIteration = SynchronizationContext.Current;
                app.RequestStop (runnable);
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (before);
        }
    }

    [Fact]
    public void Run_SingleRunnable_RestoresExistingCallerContext ()
    {
        SynchronizationContext? before = SynchronizationContext.Current;
        CallerSyncContext caller = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (caller);

            using IApplication app = CreateApplication ();
            app.Init (DriverRegistry.Names.ANSI);
            SynchronizationContext appContext = ((ApplicationImpl)app).SynchronizationContext!;
            using Runnable runnable = new ();

            SynchronizationContext? during = null;

            app.Iteration += OnIteration;
            app.Run (runnable);
            app.Iteration -= OnIteration;

            Assert.Same (appContext, during);
            Assert.Same (caller, SynchronizationContext.Current);

            return;

            void OnIteration (object? _, EventArgs<IApplication?> __)
            {
                during = SynchronizationContext.Current;
                app.RequestStop (runnable);
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (before);
        }
    }

    [Fact]
    public void Run_SingleRunnable_RestoresCallerContext_WhenIterationThrows ()
    {
        SynchronizationContext? before = SynchronizationContext.Current;
        CallerSyncContext caller = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (caller);

            using IApplication app = CreateApplication ();
            app.Init (DriverRegistry.Names.ANSI);
            using Runnable runnable = new ();
            InvalidOperationException failure = new ("iteration failed");

            app.Iteration += OnIteration;

            Assert.Same (failure, Assert.Throws<InvalidOperationException> (() => app.Run (runnable)));

            app.Iteration -= OnIteration;

            Assert.Same (caller, SynchronizationContext.Current);

            return;

            void OnIteration (object? _, EventArgs<IApplication?> __) => throw failure;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (before);
        }
    }

    [Fact]
    public void Run_NestedRunnables_KeepsAppContext_UntilOutermostReturns ()
    {
        SynchronizationContext? before = SynchronizationContext.Current;
        CallerSyncContext caller = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (caller);

            using IApplication app = CreateApplication ();
            app.Init (DriverRegistry.Names.ANSI);
            SynchronizationContext appContext = ((ApplicationImpl)app).SynchronizationContext!;

            using Runnable outer = new () { Id = "outer" };
            using Runnable inner = new () { Id = "inner" };

            var depth = 0;
            SynchronizationContext? insideOuter = null;
            SynchronizationContext? insideInner = null;
            SynchronizationContext? afterInnerReturned = null;

            app.Iteration += OnIteration;
            app.Run (outer);
            app.Iteration -= OnIteration;

            Assert.Same (appContext, insideOuter);
            Assert.Same (appContext, insideInner);

            // The inner Run returning must not hand the caller's context back while the outer loop still runs.
            Assert.Same (appContext, afterInnerReturned);

            // Only the outermost Run restores the caller's context.
            Assert.Same (caller, SynchronizationContext.Current);

            return;

            void OnIteration (object? _, EventArgs<IApplication?> __)
            {
                depth++;

                switch (depth)
                {
                    case 1:
                        insideOuter = SynchronizationContext.Current;
                        app.Run (inner); // re-enters this handler as depth 2
                        afterInnerReturned = SynchronizationContext.Current;
                        app.RequestStop (outer);

                        break;

                    case 2:
                        insideInner = SynchronizationContext.Current;
                        app.RequestStop (inner);

                        break;
                }
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (before);
        }
    }

    [Fact]
    public void Run_NestedRunnables_RestoresCallerContext_WhenInnerRunThrows ()
    {
        SynchronizationContext? before = SynchronizationContext.Current;
        CallerSyncContext caller = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (caller);

            using IApplication app = CreateApplication ();
            app.Init (DriverRegistry.Names.ANSI);
            SynchronizationContext appContext = ((ApplicationImpl)app).SynchronizationContext!;

            using Runnable outer = new () { Id = "outer" };
            using Runnable inner = new () { Id = "inner" };
            InvalidOperationException failure = new ("inner failed");

            var depth = 0;
            SynchronizationContext? afterInnerThrew = null;

            app.Iteration += OnIteration;
            app.Run (outer);
            app.Iteration -= OnIteration;

            // The inner failure must not leak the caller's context early or leave the app's installed afterward.
            Assert.Same (appContext, afterInnerThrew);
            Assert.Same (caller, SynchronizationContext.Current);

            return;

            void OnIteration (object? _, EventArgs<IApplication?> __)
            {
                depth++;

                switch (depth)
                {
                    case 1:
                        Assert.Same (failure, Assert.Throws<InvalidOperationException> (() => app.Run (inner)));
                        afterInnerThrew = SynchronizationContext.Current;
                        app.RequestStop (outer);

                        break;

                    case 2:
                        throw failure;
                }
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (before);
        }
    }

    // The documented Begin/End building-block sequence must restore the caller's ambient context
    // just like Run does — including nested sessions, where only the outermost End restores it.
    [Fact]
    public void Begin_End_RestoresAmbientSyncContext_IncludingNestedSessions ()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext marker = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (marker);

            IApplication app = CreateApplication ();
            app.Init (DriverRegistry.Names.ANSI);
            SynchronizationContext appContext = ((ApplicationImpl)app).SynchronizationContext!;

            using Runnable outer = new ();
            using Runnable inner = new ();

            SessionToken outerToken = app.Begin (outer)!;
            Assert.NotSame (appContext, SynchronizationContext.Current);
            Assert.Same (marker, SynchronizationContext.Current);

            SessionToken innerToken = app.Begin (inner)!;
            Assert.NotSame (appContext, SynchronizationContext.Current);
            Assert.Same (marker, SynchronizationContext.Current);

            app.End (innerToken);

            // The outer session is still active; the app context stays ambient.
            Assert.NotSame (appContext, SynchronizationContext.Current);
            Assert.Same (marker, SynchronizationContext.Current);

            app.End (outerToken);

            Assert.Same (marker, SynchronizationContext.Current);

            app.Dispose ();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (previous);
        }
    }

    // Claude - Opus 5.5
    [Fact]
    public void Begin_End_OuterEndedFirst_DoesNotChangeCallerContextUntilLastSessionEnds ()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext marker = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (marker);

            IApplication app = CreateApplication ();
            app.Init (DriverRegistry.Names.ANSI);
            SynchronizationContext appContext = ((ApplicationImpl)app).SynchronizationContext!;

            using Runnable outer = new ();
            using Runnable inner = new ();

            SessionToken outerToken = app.Begin (outer)!;
            SessionToken innerToken = app.Begin (inner)!;

            app.End (outerToken);

            // The inner session is still running, so its awaits must keep resuming on the thread loop.
            Assert.NotSame (appContext, SynchronizationContext.Current);
            Assert.Same (marker, SynchronizationContext.Current);

            app.End (innerToken);

            Assert.Same (marker, SynchronizationContext.Current);

            app.Dispose ();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (previous);
        }
    }

    // Claude - Opus 5.5
    [Fact]
    public void End_WhenStoppedHandlerBeginsSession_KeepsAppContextUntilThatSessionEnds ()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext marker = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (marker);

            IApplication app = CreateApplication ();
            app.Init (DriverRegistry.Names.ANSI);
            SynchronizationContext appContext = ((ApplicationImpl)app).SynchronizationContext!;

            using Runnable first = new ();
            using Runnable next = new ();
            SessionToken? nextToken = null;

            SessionToken firstToken = app.Begin (first)!;
            first.IsRunningChanged += (_, args) =>
                                      {
                                          if (args.Value)
                                          {
                                              return;
                                          }

                                          nextToken = app.Begin (next);
                                      };

            app.End (firstToken);

            // The session begun while the last one was ending is running, so its awaits must resume on the UI loop.
            Assert.True (next.IsRunning);
            Assert.NotSame (appContext, SynchronizationContext.Current);
            Assert.Same (marker, SynchronizationContext.Current);

            app.End (nextToken!);

            Assert.Same (marker, SynchronizationContext.Current);

            app.Dispose ();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (previous);
        }
    }

    // Claude - Opus 5.5
    [Fact]
    public void Run_NestedInBegunSession_RestoresCallerContextWhenOuterEndedFirst ()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext marker = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (marker);

            IApplication app = CreateApplication ();
            app.Init (DriverRegistry.Names.ANSI);

            using Runnable outer = new ();
            using Runnable inner = new ();
            SessionToken outerToken = app.Begin (outer)!;

            // The outer session ends first, while the inner session is still stopping.
            inner.IsRunningChanging += (_, e) =>
                                       {
                                           if (e.NewValue)
                                           {
                                               return;
                                           }

                                           app.End (outerToken);
                                       };

            void OnIteration (object? s, EventArgs<IApplication?> a) => app.RequestStop ();

            app.Iteration += OnIteration;
            app.Run (inner);
            app.Iteration -= OnIteration;

            // No session is running, so Run must not reinstall the app context it found when it started.
            Assert.False (outer.IsRunning);
            Assert.Same (marker, SynchronizationContext.Current);

            app.Dispose ();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (previous);
        }
    }

    // Claude - Opus 5.5
    [Fact]
    public void Begin_EndedOnWorker_DoesNotChangeCallerContext ()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext marker = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (marker);

            IApplication app = CreateApplication ();
            app.Init (DriverRegistry.Names.ANSI);
            SynchronizationContext appContext = ((ApplicationImpl)app).SynchronizationContext!;

            using Runnable first = new ();
            using Runnable second = new ();
            SessionToken firstToken = app.Begin (first)!;

            // A worker cannot change this thread's context, so the app context stays ambient here.
            Thread ender = new (() => app.End (firstToken));
            ender.Start ();
            Assert.True (ender.Join (TimeSpan.FromSeconds (10)));
            Assert.NotSame (appContext, SynchronizationContext.Current);
            Assert.Same (marker, SynchronizationContext.Current);

            SessionToken secondToken = app.Begin (second)!;
            app.End (secondToken);

            Assert.Same (marker, SynchronizationContext.Current);

            app.Dispose ();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (previous);
        }
    }

    // Claude - Opus 5.5
    [Fact]
    public void Dispose_AfterFinalSessionEndedOnWorker_RestoresCallerContext ()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext marker = new ();

        try
        {
            SynchronizationContext.SetSynchronizationContext (marker);

            IApplication app = CreateApplication ();
            app.Init (DriverRegistry.Names.ANSI);
            SynchronizationContext appContext = ((ApplicationImpl)app).SynchronizationContext!;

            using Runnable runnable = new ();
            SessionToken token = app.Begin (runnable)!;

            // A worker cannot change this thread's context, so the app context stays ambient here until Dispose.
            Thread ender = new (() => app.End (token));
            ender.Start ();
            Assert.True (ender.Join (TimeSpan.FromSeconds (10)));
            Assert.NotSame (appContext, SynchronizationContext.Current);
            Assert.Same (marker, SynchronizationContext.Current);

            app.Dispose ();

            Assert.Same (marker, SynchronizationContext.Current);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (previous);
        }
    }

    // A continuation that captured the app context during a session can resume after the session
    // ends; with no loop pumping (and the app still Initialized), it must not be stranded.
    [Fact]
    public void Post_AfterSessionEnded_StillExecutesCallback ()
    {
        IApplication app = CreateApplication ();

        try
        {
            app.Init (DriverRegistry.Names.ANSI);

            void OnIteration (object? s, EventArgs<IApplication?> a) => app.RequestStop ();

            app.Iteration += OnIteration;

            using (Runnable runnable = new ())
            {
                app.Run (runnable);
            }

            app.Iteration -= OnIteration;

            ApplicationImpl impl = (ApplicationImpl)app;

            // After the session ends the loop is no longer a reliable pump; Post must
            // fall back to the thread pool (#5636). Ubuntu CI failed this at 2s under
            // parallel load. The pool can take longer than a tight Wait to inject a worker.
            Assert.False (impl.CanPumpPostedWork);

            SynchronizationContext context = impl.SynchronizationContext!;

            using ManualResetEventSlim callbackCalled = new (false);
            context.Post (_ => callbackCalled.Set (), null);

            Assert.True (
                         callbackCalled.Wait (TimeSpan.FromSeconds (10), TestContext.Current.CancellationToken),
                         "Post after session end must run on the thread pool, not wait for a loop that is no longer pumping.");
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void Post_AfterDispose_StillExecutesCallback ()
    {
        IApplication app = CreateApplication ();
        app.Init (DriverRegistry.Names.ANSI);
        SynchronizationContext context = ((ApplicationImpl)app).SynchronizationContext!;
        app.Dispose ();

        // No main loop can ever pump this; the callback must not be stranded (or throw).
        using ManualResetEventSlim callbackCalled = new (false);
        context.Post (_ => callbackCalled.Set (), null);

        Assert.True (
                     callbackCalled.Wait (TimeSpan.FromSeconds (10), TestContext.Current.CancellationToken),
                     "Post after Dispose must run on the thread pool.");
    }

    // CoPilot - GPT-6.1 Sol
    protected virtual IApplication CreateApplication () => Application.Create ();

}

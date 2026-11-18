#nullable enable

// Copilot

namespace ApplicationTests;

[Collection ("Application Tests")]
public class RunAsyncTests
{
    [Fact]
    public async Task RunAsync_TokenCancelledBefore_ReturnsImmediately ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new ();
        using CancellationTokenSource cts = new ();
        cts.Cancel (); // Cancel before calling RunAsync

        // Act
        object? result = await app.RunAsync (runnable, cts.Token);

        // Assert - should return null immediately without starting
        Assert.Null (result);
        Assert.False (runnable.IsRunning);

        runnable.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public async Task RunAsync_TokenCancelledMidRun_LoopExits ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new ();
        using CancellationTokenSource cts = new ();
        var iterationCount = 0;

        void OnIteration (object? s, EventArgs<IApplication?> a)
        {
            iterationCount++;

            if (iterationCount >= 2)
            {
                cts.Cancel ();
            }
        }

        app.Iteration += OnIteration;

        // Act
        object? result = await app.RunAsync (runnable, cts.Token);

        app.Iteration -= OnIteration;

        // Assert - loop should have exited cleanly
        Assert.False (runnable.IsRunning);
        Assert.True (iterationCount >= 2);

        runnable.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public async Task RunAsync_BothRequestStopAndToken_IdempotentShutdown ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new ();
        using CancellationTokenSource cts = new ();

        void OnIteration (object? s, EventArgs<IApplication?> a)
        {
            // Both cancel the token AND call RequestStop
            cts.Cancel ();
            app.RequestStop (runnable);
        }

        app.Iteration += OnIteration;

        // Act - should not throw or deadlock
        object? result = await app.RunAsync (runnable, cts.Token);

        app.Iteration -= OnIteration;

        // Assert - clean shutdown
        Assert.False (runnable.IsRunning);

        runnable.Dispose ();
        app.Dispose ();
    }

    // Claude - Fable 5
    // Repro for #5636: an await between Init and RunAsync must not deadlock. Pre-fix, Init
    // installed the app's MainLoopSyncContext as the thread's ambient context; the await's
    // continuation was posted to the not-yet-running main loop, so RunAsync was never reached.
    [Fact]
    public async Task RunAsync_AfterAwaitFollowingInit_DoesNotDeadlock ()
    {
        Task scenario = Task.Run (async () =>
        {
            IApplication app = Application.Create ();
            app.Init (DriverRegistry.Names.ANSI);

            // Resumes via the thread's ambient SynchronizationContext when one is installed.
            await Task.Yield ();

            Runnable runnable = new ();
            using CancellationTokenSource cts = new ();

            void OnIteration (object? s, EventArgs<IApplication?> a) => cts.Cancel ();

            app.Iteration += OnIteration;
            await app.RunAsync (runnable, cts.Token);
            app.Iteration -= OnIteration;

            runnable.Dispose ();
            app.Dispose ();
        });

        // Fails with a timeout when the continuation after Task.Yield is stranded.
        await scenario.WaitAsync (TimeSpan.FromSeconds (10), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RunAsync_UnhandledException_FaultedTask ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new ();
        using CancellationTokenSource cts = new ();
        InvalidOperationException expectedException = new ("Test exception from run loop");

        void OnIteration (object? s, EventArgs<IApplication?> a) => throw expectedException;

        app.Iteration += OnIteration;

        // Act & Assert - the task should propagate the exception
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException> (
            async () => await app.RunAsync (runnable, cts.Token));

        Assert.Same (expectedException, ex);

        app.Iteration -= OnIteration;
        runnable.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public async Task RunAsync_Generic_TokenCancelledBefore_ReturnsImmediately ()
    {
        // Arrange
        IApplication app = Application.Create ();
        using CancellationTokenSource cts = new ();
        cts.Cancel (); // Cancel before calling RunAsync

        app.StopAfterFirstIteration = true;

        // Act
        IApplication result = await app.RunAsync<Runnable> (cts.Token, driverName: DriverRegistry.Names.ANSI);

        // Assert - should return immediately without starting
        Assert.Same (app, result);

        app.Dispose ();
    }

    [Fact]
    public async Task RunAsync_Generic_TokenCancelledMidRun_LoopExits ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        using CancellationTokenSource cts = new ();
        var iterationCount = 0;

        void OnIteration (object? s, EventArgs<IApplication?> a)
        {
            iterationCount++;

            if (iterationCount >= 2)
            {
                cts.Cancel ();
            }
        }

        app.Iteration += OnIteration;

        // Act
        IApplication result = await app.RunAsync<Runnable> (cts.Token);

        app.Iteration -= OnIteration;

        // Assert
        Assert.Same (app, result);
        Assert.True (iterationCount >= 2);

        app.Dispose ();
    }
}

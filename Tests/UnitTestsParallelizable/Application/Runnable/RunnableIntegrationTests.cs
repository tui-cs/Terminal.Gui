#nullable enable

namespace ApplicationTests.RunnableTests;

/// <summary>
///     Integration tests for IApplication's IRunnable support.
///     Tests the full lifecycle of IRunnable instances through Application methods.
/// </summary>
[Collection ("Application Tests")]
public class ApplicationRunnableIntegrationTests
{
    [Fact]
    public void Begin_AddsRunnableToStack ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();
        int stackCountBefore = app.SessionStack?.Count ?? 0;

        // Act
        SessionToken? token = app.Begin (runnable);

        // Assert
        Assert.NotNull (token);
        Assert.NotNull (token.Runnable);
        Assert.Same (runnable, token.Runnable);
        Assert.Equal (stackCountBefore + 1, app.SessionStack?.Count ?? 0);

        // Cleanup
        app.End (token!);
    }

    [Fact]
    public void Begin_CanBeCanceled_ByIsRunningChanging ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        CancelableRunnable runnable = new () { CancelStart = true };

        // Act
        SessionToken? token = app.Begin (runnable);

        // Assert - Should not be added to stack if canceled
        Assert.False (runnable.IsRunning);

        // Token not created
        Assert.Null (token);
    }

    [Fact]
    public void Begin_RaisesIsModalChangedEvent ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();
        var isModalChangedRaised = false;
        bool? receivedValue = null;

        runnable.IsModalChanged += (s, e) =>
                                   {
                                       isModalChangedRaised = true;
                                       receivedValue = e.Value;
                                   };

        // Act
        SessionToken? token = app.Begin (runnable);

        // Assert
        Assert.True (isModalChangedRaised);
        Assert.True (receivedValue);

        // Cleanup
        app.End (token!);
    }

    [Fact]
    public void Begin_RaisesIsRunningChangedEvent ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();
        var isRunningChangedRaised = false;
        bool? receivedValue = null;

        runnable.IsRunningChanged += (s, e) =>
                                     {
                                         isRunningChangedRaised = true;
                                         receivedValue = e.Value;
                                     };

        // Act
        SessionToken? token = app.Begin (runnable);

        // Assert
        Assert.True (isRunningChangedRaised);
        Assert.True (receivedValue);

        // Cleanup
        app.End (token!);
    }

    [Fact]
    public void Begin_RaisesIsRunningChangingEvent ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();
        var isRunningChangingRaised = false;
        bool? oldValue = null;
        bool? newValue = null;

        runnable.IsRunningChanging += (s, e) =>
                                      {
                                          isRunningChangingRaised = true;
                                          oldValue = e.CurrentValue;
                                          newValue = e.NewValue;
                                      };

        // Act
        SessionToken? token = app.Begin (runnable);

        // Assert
        Assert.True (isRunningChangingRaised);
        Assert.False (oldValue);
        Assert.True (newValue);

        // Cleanup
        app.End (token!);
    }

    [Fact]
    public void Begin_SetsIsModalToTrue ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();

        // Act
        SessionToken? token = app.Begin (runnable);

        // Assert
        Assert.True (runnable.IsModal);

        // Cleanup
        app.End (token!);
    }

    [Fact]
    public void Begin_SetsIsRunningToTrue ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();

        // Act
        SessionToken? token = app.Begin (runnable);

        // Assert
        Assert.True (runnable.IsRunning);

        // Cleanup
        app.End (token!);
    }

    [Fact]
    public void Begin_ThrowsOnNullRunnable ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();

        // Act & Assert
        Assert.Throws<ArgumentNullException> (() => app.Begin ((IRunnable)null!));
    }

    [Fact]
    public void End_CanBeCanceled_ByIsRunningChanging ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        CancelableRunnable runnable = new () { CancelStop = true };
        SessionToken? token = app.Begin (runnable);
        runnable.CancelStop = true; // Enable cancellation

        // Act
        app.End (token!);

        // Assert - Should still be running if canceled
        Assert.True (runnable.IsRunning);

        // Force end by disabling cancellation
        runnable.CancelStop = false;
        app.End (token!);
    }

    [Fact]
    public void End_ClearsTokenRunnable ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();
        SessionToken? token = app.Begin (runnable);

        // Act
        app.End (token!);

        // Assert
        Assert.Null (token!.Runnable);
    }

    // Claude - Opus 5.5
    [Fact]
    public void End_WhenStateChangedHandlerThrows_CompletesSessionTeardown ()
    {
        SynchronizationContext? originalContext = SynchronizationContext.Current;
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();
        SessionToken token = app.Begin (runnable)!;
        SessionToken? endedToken = null;
        app.SessionEnded += OnSessionEnded;
        runnable.IsRunningChanged += (_, _) => throw new InvalidOperationException ("handler failed");

        try
        {
            Assert.Throws<InvalidOperationException> (() => app.End (token));

            Assert.Null (token.Runnable);
            Assert.Same (token, endedToken);
            Assert.Empty (app.SessionStack!);
            Assert.Same (originalContext, SynchronizationContext.Current);

            app.End (token);
        }
        finally
        {
            app.SessionEnded -= OnSessionEnded;
            SynchronizationContext.SetSynchronizationContext (originalContext);
            app.Dispose ();
        }

        void OnSessionEnded (object? sender, SessionTokenEventArgs e) => endedToken = e.State;
    }

    [Fact]
    public void End_RaisesIsRunningChangedEvent ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();
        SessionToken? token = app.Begin (runnable);
        var isRunningChangedRaised = false;
        bool? receivedValue = null;

        runnable.IsRunningChanged += (s, e) =>
                                     {
                                         isRunningChangedRaised = true;
                                         receivedValue = e.Value;
                                     };

        // Act
        app.End (token!);

        // Assert
        Assert.True (isRunningChangedRaised);
        Assert.False (receivedValue);
    }

    [Fact]
    public void End_RaisesIsRunningChangingEvent ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();
        SessionToken? token = app.Begin (runnable);
        var isRunningChangingRaised = false;
        bool? oldValue = null;
        bool? newValue = null;

        runnable.IsRunningChanging += (s, e) =>
                                      {
                                          isRunningChangingRaised = true;
                                          oldValue = e.CurrentValue;
                                          newValue = e.NewValue;
                                      };

        // Act
        app.End (token!);

        // Assert
        Assert.True (isRunningChangingRaised);
        Assert.True (oldValue);
        Assert.False (newValue);
    }

    [Fact]
    public void End_RemovesRunnableFromStack ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();
        SessionToken? token = app.Begin (runnable);
        int stackCountBefore = app.SessionStack?.Count ?? 0;

        // Act
        app.End (token!);

        // Assert
        Assert.Equal (stackCountBefore - 1, app.SessionStack?.Count ?? 0);
    }

    [Fact]
    public void End_SetsIsModalToFalse ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();
        SessionToken? token = app.Begin (runnable);

        // Act
        app.End (token!);

        // Assert
        Assert.False (runnable.IsModal);
    }

    [Fact]
    public void End_SetsIsRunningToFalse ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable = new ();
        SessionToken? token = app.Begin (runnable);

        // Act
        app.End (token!);

        // Assert
        Assert.False (runnable.IsRunning);
    }

    [Fact]
    public void End_ThrowsOnNullToken ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();

        // Act & Assert
        Assert.Throws<ArgumentNullException> (() => app.End ((SessionToken)null!));
    }

    [Fact]
    public void End_ClearsIsGrabbed ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();

        Runnable<int> runnable = new ();
        SessionToken? token = app.Begin (runnable);
        app.Mouse.GrabMouse (runnable);
        app.End (token!);

        Assert.False (app.Mouse.IsGrabbed (runnable));

        runnable.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void MultipleRunnables_IndependentResults ()
    {
        // Arrange
        Runnable<int> runnable1 = new ();
        Runnable<string> runnable2 = new ();

        // Act
        runnable1.Result = 42;
        runnable2.Result = "test";

        // Assert
        Assert.Equal (42, runnable1.Result);
        Assert.Equal ("test", runnable2.Result);
    }

    [Fact]
    public void NestedBegin_MaintainsStackOrder ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable1 = new () { Id = "1" };
        Runnable<int> runnable2 = new () { Id = "2" };

        // Act
        SessionToken token1 = app.Begin (runnable1)!;
        SessionToken token2 = app.Begin (runnable2)!;

        // Assert - runnable2 should be on top
        Assert.True (runnable2.IsModal);
        Assert.False (runnable1.IsModal);
        Assert.True (runnable1.IsRunning); // Still running, just not modal
        Assert.True (runnable2.IsRunning);

        // Cleanup
        app.End (token2);
        app.End (token1);
    }

    [Fact]
    public void NestedEnd_RestoresPreviousModal ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnable1 = new () { Id = "1" };
        Runnable<int> runnable2 = new () { Id = "2" };
        SessionToken token1 = app.Begin (runnable1)!;
        SessionToken token2 = app.Begin (runnable2)!;

        // Act - End the top runnable
        app.End (token2);

        // Assert - runnable1 should become modal again
        Assert.True (runnable1.IsModal);
        Assert.False (runnable2.IsModal);
        Assert.True (runnable1.IsRunning);
        Assert.False (runnable2.IsRunning);

        // Cleanup
        app.End (token1);
    }

    // Claude - Opus 5.5
    [Fact]
    public void NestedEnd_AfterNonTopSessionEnds_RestoresNearestRunningSession ()
    {
        IApplication app = CreateAndInitApp ();
        Runnable<int> runnableA = new () { Id = "A" };
        Runnable<int> runnableB = new () { Id = "B" };
        Runnable<int> runnableC = new () { Id = "C" };
        SessionToken tokenA = app.Begin (runnableA)!;
        SessionToken tokenB = app.Begin (runnableB)!;
        SessionToken tokenC = app.Begin (runnableC)!;

        app.End (tokenB);
        app.End (tokenC);

        Assert.Same (runnableA, app.TopRunnable);
        Assert.True (runnableA.IsModal);
        Assert.True (runnableA.IsRunning);
        Assert.Single (app.SessionStack!);

        app.End (tokenA);

        Assert.Null (app.TopRunnable);
        Assert.Empty (app.SessionStack!);
    }

    [Fact]
    public void RequestStop_WithIRunnable_WorksCorrectly ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        StoppableRunnable runnable = new ();
        SessionToken? token = app.Begin (runnable);

        // Act
        app.RequestStop (runnable);

        // Assert - RequestStop should trigger End eventually
        // For now, just verify it doesn't throw
        Assert.NotNull (runnable);

        // Cleanup
        app.End (token!);
    }

    [Fact]
    public void RequestStop_WithNull_UsesTopRunnable ()
    {
        // Arrange
        IApplication app = CreateAndInitApp ();
        StoppableRunnable runnable = new ();
        SessionToken? token = app.Begin (runnable);

        // Act
        app.RequestStop ((IRunnable?)null);

        // Assert - Should not throw
        Assert.NotNull (runnable);

        // Cleanup
        app.End (token!);
    }

    private IApplication CreateAndInitApp ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        return app;
    }

    /// <summary>
    ///     Test runnable that can cancel lifecycle changes.
    /// </summary>
    private class CancelableRunnable : Runnable<int>
    {
        public bool CancelStart { get; set; }
        public bool CancelStop { get; set; }

        protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
        {
            if (newIsRunning && CancelStart)
            {
                return true; // Cancel starting
            }

            if (!newIsRunning && CancelStop)
            {
                return true; // Cancel stopping
            }

            return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
        }
    }

    /// <summary>
    ///     Test runnable that can be stopped.
    /// </summary>
    private class StoppableRunnable : Runnable<int>
    {
        public override void RequestStop ()
        {
            WasStopRequested = true;
            base.RequestStop ();
        }

        public bool WasStopRequested { get; private set; }
    }

    /// <summary>
    ///     Test runnable for generic Run tests.
    /// </summary>
    private class TestRunnable : Runnable<int>
    {
        public TestRunnable () { Id = "TestRunnable"; }
    }
}

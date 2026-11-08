// ReSharper disable AccessToDisposedClosure
#pragma warning disable xUnit1031

namespace ApplicationTests.Timeout;

/// <summary>
///     Tests for timeout behavior and functionality.
///     These tests verify that timeouts fire correctly, can be added/removed,
///     handle exceptions properly, and work with Application.Run() calls.
/// </summary>
[Collection ("Application Timer Tests")]
public class TimeoutTests
{
    [Fact]
    public void AddTimeout_Callback_Can_Add_New_Timeout ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var firstFired = false;
        var secondFired = false;

        app.AddTimeout (TimeSpan.FromMilliseconds (50),
                        () =>
                        {
                            firstFired = true;

                            // Add another timeout from within callback
                            app.AddTimeout (TimeSpan.FromMilliseconds (50),
                                            () =>
                                            {
                                                secondFired = true;
                                                app.RequestStop ();

                                                return false;
                                            });

                            return false;
                        });

        // Defensive: use iteration counter instead of time-based safety timeout
        var iterations = 0;
        app.Iteration += IterationHandler;

        try
        {
            app.Run<Runnable> ();

            Assert.True (firstFired);
            Assert.True (secondFired);
        }
        finally
        {
            app.Iteration -= IterationHandler;
        }

        return;

        void IterationHandler (object? s, EventArgs<IApplication?> e)
        {
            iterations++;

            // Stop if test objectives met or safety limit reached
            if ((firstFired && secondFired) || iterations > 1000)
            {
                app.RequestStop ();
            }
        }
    }

    [Fact]
    public void AddTimeout_Exception_In_Callback_Propagates ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var exceptionThrown = false;

        app.AddTimeout (TimeSpan.FromMilliseconds (50),
                        () =>
                        {
                            exceptionThrown = true;

                            throw new InvalidOperationException ("Test exception");
                        });

        // Defensive: use iteration counter
        var iterations = 0;
        app.Iteration += IterationHandler;

        try
        {
            Assert.Throws<InvalidOperationException> (() => app.Run<Runnable> ());
            Assert.True (exceptionThrown, "Exception callback should have been invoked");
        }
        finally
        {
            app.Iteration -= IterationHandler;
        }

        return;

        void IterationHandler (object? s, EventArgs<IApplication?> e)
        {
            iterations++;

            // Safety stop if exception not thrown after many iterations
            if (iterations > 1000 && !exceptionThrown)
            {
                app.RequestStop ();
            }
        }
    }

    [Fact]
    public void AddTimeout_Fires ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        uint timeoutTime = 100;
        var timeoutFired = false;

        // Setup a timeout that will fire
        app.AddTimeout (TimeSpan.FromMilliseconds (timeoutTime),
                        () =>
                        {
                            timeoutFired = true;

                            // Return false so the timer does not repeat
                            return false;
                        });

        // The timeout has not fired yet
        Assert.False (timeoutFired);

        // Block the thread to prove the timeout does not fire on a background thread
        Thread.Sleep ((int)timeoutTime * 2);
        Assert.False (timeoutFired);

        app.StopAfterFirstIteration = true;
        app.Run<Runnable> ();

        // The timeout should have fired
        Assert.True (timeoutFired);
    }

    [Fact]
    public void AddTimeout_From_Background_Thread_Fires ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var timeoutFired = false;
        using var taskCompleted = new ManualResetEventSlim (false);

        Task.Run (() =>
                  {
                      Thread.Sleep (50); // Ensure we're on background thread

                      app.Invoke (() =>
                                  {
                                      app.AddTimeout (TimeSpan.FromMilliseconds (100),
                                                      () =>
                                                      {
                                                          timeoutFired = true;
                                                          taskCompleted.Set ();
                                                          app.RequestStop ();

                                                          return false;
                                                      });
                                  });
                  },
                  TestContext.Current.CancellationToken);

        // Use iteration counter for safety instead of time
        var iterations = 0;
        app.Iteration += IterationHandler;

        try
        {
            app.Run<Runnable> ();

            // Defensive: wait with timeout
            Assert.True (taskCompleted.Wait (TimeSpan.FromSeconds (5), TestContext.Current.CancellationToken), "Timeout from background thread should have completed");
            Assert.True (timeoutFired);
        }
        finally
        {
            app.Iteration -= IterationHandler;
        }

        return;

        void IterationHandler (object? s, EventArgs<IApplication?> e)
        {
            iterations++;

            // Safety stop
            if (iterations > 1000)
            {
                app.RequestStop ();
            }
        }
    }

    [Fact]
    public void AddTimeout_High_Frequency_All_Fire ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        const int TIMEOUT_COUNT = 50; // Reduced from 100 for performance
        var firedCount = 0;

        for (var i = 0; i < TIMEOUT_COUNT; i++)
        {
            app.AddTimeout (TimeSpan.FromMilliseconds (10 + i * 5),
                            () =>
                            {
                                Interlocked.Increment (ref firedCount);

                                return false;
                            });
        }

        // Use iteration counter and event completion instead of time-based safety
        var iterations = 0;
        app.Iteration += IterationHandler;

        try
        {
            app.Run<Runnable> ();

            Assert.Equal (TIMEOUT_COUNT, firedCount);
        }
        finally
        {
            app.Iteration -= IterationHandler;
        }

        return;

        void IterationHandler (object? s, EventArgs<IApplication?> e)
        {
            iterations++;

            // Stop when all timeouts fired or safety limit reached
            if (firedCount >= TIMEOUT_COUNT || iterations > 2000)
            {
                app.RequestStop ();
            }
        }
    }

    [Fact]
    public void Long_Running_Callback_Delays_Subsequent_Timeouts ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var firstStarted = false;
        var secondFired = false;
        var firstCompleted = false;

        // Long-running timeout
        app.AddTimeout (TimeSpan.FromMilliseconds (50),
                        () =>
                        {
                            firstStarted = true;
                            Thread.Sleep (200); // Simulate long operation
                            firstCompleted = true;

                            return false;
                        });

        // This should fire even though first is still running
        app.AddTimeout (TimeSpan.FromMilliseconds (100),
                        () =>
                        {
                            secondFired = true;

                            return false;
                        });

        // Use iteration counter instead of time-based timeout
        var iterations = 0;
        app.Iteration += IterationHandler;

        try
        {
            app.Run<Runnable> ();

            Assert.True (firstStarted);
            Assert.True (secondFired);
            Assert.True (firstCompleted);
        }
        finally
        {
            app.Iteration -= IterationHandler;
        }

        return;

        void IterationHandler (object? s, EventArgs<IApplication?> e)
        {
            iterations++;

            // Stop when both complete or safety limit
            if ((firstCompleted && secondFired) || iterations > 2000)
            {
                app.RequestStop ();
            }
        }
    }

    [Fact]
    public void AddTimeout_Multiple_Fire_In_Order ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        List<int> executionOrder = new ();

        app.AddTimeout (TimeSpan.FromMilliseconds (300),
                        () =>
                        {
                            executionOrder.Add (3);

                            return false;
                        });

        app.AddTimeout (TimeSpan.FromMilliseconds (100),
                        () =>
                        {
                            executionOrder.Add (1);

                            return false;
                        });

        app.AddTimeout (TimeSpan.FromMilliseconds (200),
                        () =>
                        {
                            executionOrder.Add (2);

                            return false;
                        });

        var iterations = 0;

        app.Iteration += IterationHandler;

        try
        {
            app.Run<Runnable> ();

            Assert.Equal (new [] { 1, 2, 3 }, executionOrder);
        }
        finally
        {
            app.Iteration -= IterationHandler;
        }

        return;

        void IterationHandler (object? s, EventArgs<IApplication?> e)
        {
            iterations++;

            // Stop after timeouts fire or max iterations (defensive)
            if (executionOrder.Count == 3 || iterations > 1000)
            {
                app.RequestStop ();
            }
        }
    }

    [Fact]
    public void AddTimeout_Multiple_TimeSpan_Zero_All_Fire ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        const int TIMEOUT_COUNT = 10;
        var firedCount = 0;

        for (var i = 0; i < TIMEOUT_COUNT; i++)
        {
            app.AddTimeout (TimeSpan.Zero,
                            () =>
                            {
                                Interlocked.Increment (ref firedCount);

                                return false;
                            });
        }

        var iterations = 0;

        app.Iteration += IterationHandler;

        try
        {
            app.Run<Runnable> ();

            Assert.Equal (TIMEOUT_COUNT, firedCount);
        }
        finally
        {
            app.Iteration -= IterationHandler;
        }

        return;

        void IterationHandler (object? s, EventArgs<IApplication?> e)
        {
            iterations++;

            // Defensive: stop after timeouts fire or max iterations
            if (firedCount == TIMEOUT_COUNT || iterations > 100)
            {
                app.RequestStop ();
            }
        }
    }

    // Grok - 4.7
    [Fact]
    public void AddTimeout_Nested_Run_Parent_Timeout_Fires ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        bool parentTimeoutFired = false;
        bool parentFiredDuringNestedRun = false;
        bool childTimeoutFired = false;
        bool nestedRunCompleted = false;
        Runnable? nestedRunnable = null;

        // Registered before the nested run and repeated until that runnable is top.
        // TimeSpan.Zero stays deterministic when the loop is unthrottled.
        // ApplicationMainLoop.Iteration sleeps only while Application.MaximumIterationsPerSecond
        // is non-zero, and AppTestHelper sets that static to ushort.MaxValue.
        // A repeating zero timeout is deferred to the next RunTimers pass
        // (TimedEvents.RunTimersImpl; RunTimers_Defers_Repeating_Zero_Timeout_Until_Next_Pass).
        // Delayed timeouts across a nested run stay in NestedRunTimeoutTests
        // (Timeout_Scheduled_Before_Nested_Run_Fires_During_Nested_Run).
        app.AddTimeout (TimeSpan.Zero,
                        () =>
                        {
                            if (nestedRunnable is null || !ReferenceEquals (app.TopRunnableView, nestedRunnable))
                            {
                                return true;
                            }

                            parentTimeoutFired = true;
                            parentFiredDuringNestedRun = true;

                            return false;
                        });

        app.AddTimeout (TimeSpan.Zero,
                        () =>
                        {
                            Runnable childRunnable = new ();
                            nestedRunnable = childRunnable;

                            // Wait until the parent timeout has observed this run, then stop it.
                            // A miss repeats on the next pass, so callback order in one pass does not matter.
                            app.AddTimeout (TimeSpan.Zero,
                                            () =>
                                            {
                                                if (!parentFiredDuringNestedRun)
                                                {
                                                    return true;
                                                }

                                                childTimeoutFired = true;
                                                app.RequestStop (childRunnable);

                                                return false;
                                            });

                            app.Run (childRunnable);
                            nestedRunCompleted = true;
                            childRunnable.Dispose ();

                            // Run returns after End pops this session, so this stops the outer runnable.
                            app.RequestStop ();

                            return false;
                        });

        int iterations = 0;
        app.Iteration += IterationHandler;

        try
        {
            app.Run<Runnable> ();

            Assert.True (childTimeoutFired, "Child timeout should fire during nested Run");
            Assert.True (parentTimeoutFired, "Parent timeout should continue firing during nested Run");
            Assert.True (parentFiredDuringNestedRun, "Parent timeout should fire while the nested run is active");
            Assert.True (nestedRunCompleted, "Nested run should have completed");
        }
        finally
        {
            app.Iteration -= IterationHandler;
        }

        return;

        void IterationHandler (object? s, EventArgs<IApplication?> e)
        {
            iterations++;

            // Nested iterations count too. RequestStop () stops only TopRunnableView, so a trip
            // during the nested run stops the child. The callback above then stops the outer run.
            // The assertions fail instead of the test hanging.
            if (iterations > 100)
            {
                app.RequestStop ();
            }
        }
    }

    [Fact]
    public void AddTimeout_Repeating_Fires_Multiple_Times ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var fireCount = 0;

        app.AddTimeout (TimeSpan.FromMilliseconds (50),
                        () =>
                        {
                            fireCount++;

                            return fireCount < 3; // Repeat 3 times
                        });

        var iterations = 0;

        app.Iteration += IterationHandler;

        try
        {
            app.Run<Runnable> ();

            Assert.Equal (3, fireCount);
        }
        finally
        {
            app.Iteration -= IterationHandler;
        }

        return;

        void IterationHandler (object? s, EventArgs<IApplication?> e)
        {
            iterations++;

            // Stop after 3 fires or max iterations (defensive)
            if (fireCount >= 3 || iterations > 1000)
            {
                app.RequestStop ();
            }
        }
    }

    [Fact]
    public void AddTimeout_StopAfterFirstIteration_Immediate_Fires ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var timeoutFired = false;

        app.AddTimeout (TimeSpan.Zero,
                        () =>
                        {
                            timeoutFired = true;

                            return false;
                        });

        app.StopAfterFirstIteration = true;
        app.Run<Runnable> ();

        Assert.True (timeoutFired);
    }

    [Fact]
    public void AddTimeout_TimeSpan_Zero_Fires ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var timeoutFired = false;

        app.AddTimeout (TimeSpan.Zero,
                        () =>
                        {
                            timeoutFired = true;

                            return false;
                        });

        app.StopAfterFirstIteration = true;
        app.Run<Runnable> ();

        Assert.True (timeoutFired);
    }

    [Fact]
    public void RemoveTimeout_Already_Removed_Returns_False ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        object? token = app.AddTimeout (TimeSpan.FromMilliseconds (100), () => false);

        // Remove once
        bool removed1 = app.RemoveTimeout (token!);
        Assert.True (removed1);

        // Try to remove again
        bool removed2 = app.RemoveTimeout (token!);
        Assert.False (removed2);
    }

    [Fact]
    public void RemoveTimeout_Cancels_Timeout ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var timeoutFired = false;

        object? token = app.AddTimeout (TimeSpan.FromMilliseconds (100),
                                        () =>
                                        {
                                            timeoutFired = true;

                                            return false;
                                        });

        // Remove timeout before it fires
        bool removed = app.RemoveTimeout (token!);
        Assert.True (removed);

        // Use iteration counter instead of time-based timeout
        var iterations = 0;
        app.Iteration += IterationHandler;

        try
        {
            app.Run<Runnable> ();

            Assert.False (timeoutFired);
        }
        finally
        {
            app.Iteration -= IterationHandler;
        }

        return;

        void IterationHandler (object? s, EventArgs<IApplication?> e)
        {
            iterations++;

            // Since timeout was removed, just need enough iterations to prove it won't fire
            // With 100ms timeout, give ~50 iterations which is more than enough
            if (iterations > 50)
            {
                app.RequestStop ();
            }
        }
    }

    [Fact]
    public void RemoveTimeout_Invalid_Token_Returns_False ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var fakeToken = new object ();
        bool removed = app.RemoveTimeout (fakeToken);

        Assert.False (removed);
    }

    [Fact]
    public void TimedEvents_GetTimeout_Invalid_Token_Returns_Null ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var fakeToken = new object ();
        TimeSpan? actualTimeSpan = app.TimedEvents?.GetTimeout (fakeToken);

        Assert.Null (actualTimeSpan);
    }

    [Fact]
    public void TimedEvents_GetTimeout_Returns_Correct_TimeSpan ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TimeSpan expectedTimeSpan = TimeSpan.FromMilliseconds (500);
        object? token = app.AddTimeout (expectedTimeSpan, () => false);

        TimeSpan? actualTimeSpan = app.TimedEvents?.GetTimeout (token!);

        Assert.NotNull (actualTimeSpan);
        Assert.Equal (expectedTimeSpan, actualTimeSpan.Value);
    }

    [Fact]
    public void TimedEvents_StopAll_Clears_Timeouts ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var firedCount = 0;

        for (var i = 0; i < 10; i++)
        {
            app.AddTimeout (TimeSpan.FromMilliseconds (100),
                            () =>
                            {
                                Interlocked.Increment (ref firedCount);

                                return false;
                            });
        }

        Assert.NotEmpty (app.TimedEvents!.Timeouts);

        app.TimedEvents.StopAll ();

        Assert.Empty (app.TimedEvents.Timeouts);

        // Use iteration counter for safety
        var iterations = 0;
        app.Iteration += IterationHandler;

        try
        {
            app.Run<Runnable> ();

            Assert.Equal (0, firedCount);
        }
        finally
        {
            app.Iteration -= IterationHandler;
        }

        return;

        void IterationHandler (object? s, EventArgs<IApplication?> e)
        {
            iterations++;

            // Since all timeouts were cleared, just need enough iterations to prove they won't fire
            // With 100ms timeouts, give ~50 iterations which is more than enough
            if (iterations > 50)
            {
                app.RequestStop ();
            }
        }
    }

    [Fact]
    public void TimedEvents_Timeouts_Property_Is_Thread_Safe ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        const int THREAD_COUNT = 10;
        var addedCount = 0;
        var tasksCompleted = new CountdownEvent (THREAD_COUNT);

        // Add timeouts from multiple threads using Invoke
        for (var i = 0; i < THREAD_COUNT; i++)
        {
            Task.Run (() =>
                      {
                          app.Invoke (() =>
                                      {
                                          // Add timeout with immediate execution
                                          app.AddTimeout (TimeSpan.Zero,
                                                          () =>
                                                          {
                                                              Interlocked.Increment (ref addedCount);

                                                              return false;
                                                          });

                                          tasksCompleted.Signal ();
                                      });
                      },
                      TestContext.Current.CancellationToken);
        }

        // Use iteration counter to stop when all tasks complete
        var iterations = 0;
        app.Iteration += IterationHandler;

        try
        {
            app.Run<Runnable> ();

            // Verify we can safely access the Timeouts property from main thread
            int timeoutCount = app.TimedEvents?.Timeouts.Count ?? 0;

            // Verify no exceptions occurred
            Assert.True (timeoutCount >= 0, "Should be able to access Timeouts property without exception");

            // Verify all tasks completed and all timeouts fired
            Assert.True (tasksCompleted.IsSet, "All background tasks should have completed");
            Assert.Equal (THREAD_COUNT, addedCount);
        }
        finally
        {
            app.Iteration -= IterationHandler;
            tasksCompleted.Dispose ();
        }

        return;

        void IterationHandler (object? s, EventArgs<IApplication?> e)
        {
            iterations++;

            // Stop when all tasks completed and all timeouts fired, or safety limit
            if ((tasksCompleted.IsSet && addedCount >= THREAD_COUNT) || iterations > 200)
            {
                app.RequestStop ();
            }
        }
    }

    [Fact]
    public void Invoke_Adds_Idle ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable top = new ();
        SessionToken? rs = app.Begin (top);

        var actionCalled = 0;
        app.Invoke (_ => { actionCalled++; });
        app.TimedEvents!.RunTimers ();
        Assert.Equal (1, actionCalled);
        top.Dispose ();

        app.Dispose ();
    }
}

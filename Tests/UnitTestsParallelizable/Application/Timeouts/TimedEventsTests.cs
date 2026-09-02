#nullable enable
namespace ApplicationTests.TimedEventTests;

/// <summary>
///     Tests for TimedEvents class, focusing on high-resolution timing with Stopwatch.
/// </summary>
[Collection("Application Timer Tests")]
public class TimedEventsTests
{
    // CoPilot - GPT-5
    [Fact]
    public async Task RunTimers_Callback_Does_Not_Block_Add_From_Other_Thread ()
    {
        TimedEvents timedEvents = new ();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ManualResetEventSlim addStarted = new ();
        using ManualResetEventSlim addCompleted = new ();
        Task? addTask = null;
        var addStartedBeforeCallbackReturned = false;
        var addCompletedBeforeCallbackReturned = false;

        timedEvents.Add (
                         TimeSpan.Zero,
                         () =>
                         {
                             addTask = RunOnDedicatedThread (
                                                             () =>
                                                             {
                                                                 try
                                                                 {
                                                                     addStarted.Set ();
                                                                     timedEvents.Add (TimeSpan.FromHours (1), () => false);
                                                                 }
                                                                 finally
                                                                 {
                                                                     addCompleted.Set ();
                                                                 }
                                                             });
                             addStartedBeforeCallbackReturned = addStarted.Wait (
                                                                                 TimeSpan.FromSeconds (5),
                                                                                 cancellationToken);
                             addCompletedBeforeCallbackReturned = addCompleted.Wait (
                                                                                     TimeSpan.FromSeconds (5),
                                                                                     cancellationToken);

                             return false;
                         });

        timedEvents.RunTimers ();
        await addTask!;

        Assert.True (addStartedBeforeCallbackReturned, "The Add task should start before the callback returns.");
        Assert.True (addCompletedBeforeCallbackReturned, "Add should complete before the callback returns.");
    }

    // CoPilot - GPT-5
    [Fact]
    public async Task RunTimers_Callback_Does_Not_Block_Invoke_From_Other_Thread ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ManualResetEventSlim invokeStarted = new ();
        using ManualResetEventSlim invokeCompleted = new ();
        Task? invokeTask = null;
        Exception? invokeError = null;
        var invokeStartedBeforeCallbackCompleted = false;
        var invokeReturnedBeforeCallbackCompleted = false;
        var invoked = false;

        app.AddTimeout (
                        TimeSpan.Zero,
                        () =>
                        {
                            invokeTask = RunOnDedicatedThread (
                                                                () =>
                                                                {
                                                                    try
                                                                    {
                                                                        invokeStarted.Set ();
                                                                        app.Invoke (() => invoked = true);
                                                                    }
                                                                    catch (Exception ex)
                                                                    {
                                                                        invokeError = ex;
                                                                    }
                                                                    finally
                                                                    {
                                                                        invokeCompleted.Set ();
                                                                    }
                                                                });
                            invokeStartedBeforeCallbackCompleted = invokeStarted.Wait (
                                                                                       TimeSpan.FromSeconds (5),
                                                                                       cancellationToken);
                            invokeReturnedBeforeCallbackCompleted = invokeCompleted.Wait (
                                                                                           TimeSpan.FromSeconds (5),
                                                                                           cancellationToken);

                            return false;
                        });

        try
        {
            app.TimedEvents!.RunTimers ();

            if (invokeCompleted.Wait (TimeSpan.FromSeconds (5), cancellationToken))
            {
                app.TimedEvents.RunTimers ();
            }
        }
        finally
        {
            app.Dispose ();
        }

        await invokeTask!;

        Assert.NotNull (invokeTask);
        Assert.Null (invokeError);
        Assert.True (invokeStartedBeforeCallbackCompleted, "The Invoke task should start before the callback returns.");
        Assert.True (invokeReturnedBeforeCallbackCompleted, "Invoke should enqueue before the callback returns.");
        Assert.True (invoked);
    }

    // CoPilot - GPT-5
    [Fact]
    public async Task RunTimers_Callback_Does_Not_Block_SynchronizationContext_Post ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SynchronizationContext context = new MainLoopSyncContext (app);
        using ManualResetEventSlim postStarted = new ();
        using ManualResetEventSlim postCompleted = new ();
        Task? postTask = null;
        Exception? postError = null;
        var postStartedBeforeCallbackCompleted = false;
        var postReturnedBeforeCallbackCompleted = false;
        var posted = false;

        app.AddTimeout (
                        TimeSpan.Zero,
                        () =>
                        {
                            postTask = RunOnDedicatedThread (
                                                              () =>
                                                              {
                                                                  try
                                                                  {
                                                                      postStarted.Set ();
                                                                      context.Post (_ => posted = true, null);
                                                                  }
                                                                  catch (Exception ex)
                                                                  {
                                                                      postError = ex;
                                                                  }
                                                                  finally
                                                                  {
                                                                      postCompleted.Set ();
                                                                  }
                                                              });
                            postStartedBeforeCallbackCompleted = postStarted.Wait (
                                                                                   TimeSpan.FromSeconds (5),
                                                                                   cancellationToken);
                            postReturnedBeforeCallbackCompleted = postCompleted.Wait (
                                                                                       TimeSpan.FromSeconds (5),
                                                                                       cancellationToken);

                            return false;
                        });

        try
        {
            app.TimedEvents!.RunTimers ();

            if (postCompleted.Wait (TimeSpan.FromSeconds (5), cancellationToken))
            {
                app.TimedEvents.RunTimers ();
            }
        }
        finally
        {
            app.Dispose ();
        }

        await postTask!;

        Assert.NotNull (postTask);
        Assert.Null (postError);
        Assert.True (postStartedBeforeCallbackCompleted, "The Post task should start before the callback returns.");
        Assert.True (postReturnedBeforeCallbackCompleted, "Post should enqueue before the callback returns.");
        Assert.True (posted);
    }

    // CoPilot - GPT-5
    [Fact]
    public async Task RunTimers_Callback_Does_Not_Block_SynchronizationContext_Send_Enqueue ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SynchronizationContext context = new MainLoopSyncContext (app);
        using ManualResetEventSlim sendStarted = new ();
        using ManualResetEventSlim sendQueued = new ();
        using ManualResetEventSlim sendReturned = new ();
        Task? sendTask = null;
        Exception? sendError = null;
        var sendStartedBeforeCallbackCompleted = false;
        var sendQueuedBeforeCallbackCompleted = false;
        var sendQueuedAfterCallbackCompleted = false;
        var sendReturnedAfterCallback = false;
        var sent = false;
        EventHandler<TimeoutEventArgs> handler = (_, _) => sendQueued.Set ();

        app.AddTimeout (
                        TimeSpan.Zero,
                        () =>
                        {
                            sendTask = RunOnDedicatedThread (
                                                              () =>
                                                              {
                                                                  try
                                                                  {
                                                                      sendStarted.Set ();
                                                                      context.Send (_ => sent = true, null);
                                                                  }
                                                                  catch (Exception ex)
                                                                  {
                                                                      sendError = ex;
                                                                  }
                                                                  finally
                                                                  {
                                                                      sendReturned.Set ();
                                                                  }
                                                              });
                            sendStartedBeforeCallbackCompleted = sendStarted.Wait (
                                                                                   TimeSpan.FromSeconds (5),
                                                                                   cancellationToken);
                            sendQueuedBeforeCallbackCompleted = sendQueued.Wait (
                                                                                 TimeSpan.FromSeconds (5),
                                                                                 cancellationToken);

                            return false;
                        });

        app.TimedEvents!.Added += handler;

        try
        {
            app.TimedEvents.RunTimers ();
            sendQueuedAfterCallbackCompleted = sendQueued.Wait (TimeSpan.FromSeconds (5), cancellationToken);

            if (sendQueuedAfterCallbackCompleted && !sendReturned.IsSet)
            {
                app.TimedEvents.RunTimers ();
            }

            sendReturnedAfterCallback = sendReturned.Wait (TimeSpan.FromSeconds (5), cancellationToken);
        }
        finally
        {
            if (sendTask is not null && !sendReturned.IsSet)
            {
                sendQueuedAfterCallbackCompleted = WaitForCleanup (sendQueued);

                if (sendQueuedAfterCallbackCompleted)
                {
                    app.TimedEvents.RunTimers ();
                    sendReturnedAfterCallback = WaitForCleanup (sendReturned);
                }
            }

            app.TimedEvents.Added -= handler;
            app.Dispose ();

            if (sendTask is not null)
            {
                await sendTask;
            }
        }

        Assert.NotNull (sendTask);
        Assert.Null (sendError);
        Assert.True (sendStartedBeforeCallbackCompleted, "The Send task should start before the callback returns.");
        Assert.True (sendQueuedBeforeCallbackCompleted, "Send should enqueue before the callback returns.");
        Assert.True (sendQueuedAfterCallbackCompleted, "Send should enqueue before cleanup times out.");
        Assert.True (sendReturnedAfterCallback, "Send should return after its callback runs on the timer runner.");
        Assert.True (sent);
    }

    // CoPilot - GPT-5
    [Fact]
    public async Task RunTimers_Callback_Does_Not_Block_Remove_From_Other_Thread ()
    {
        TimedEvents timedEvents = new ();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        object timeoutToRemove = timedEvents.Add (TimeSpan.FromHours (1), () => false);
        using ManualResetEventSlim removeStarted = new ();
        using ManualResetEventSlim removeCompleted = new ();
        Task<bool>? removeTask = null;
        var removeStartedBeforeCallbackReturned = false;
        var removeCompletedBeforeCallbackReturned = false;

        timedEvents.Add (
                         TimeSpan.Zero,
                         () =>
                         {
                             removeTask = RunOnDedicatedThread (
                                                                () =>
                                                                {
                                                                    try
                                                                    {
                                                                        removeStarted.Set ();

                                                                        return timedEvents.Remove (timeoutToRemove);
                                                                    }
                                                                    finally
                                                                    {
                                                                        removeCompleted.Set ();
                                                                    }
                                                                });
                             removeStartedBeforeCallbackReturned = removeStarted.Wait (
                                                                                       TimeSpan.FromSeconds (5),
                                                                                       cancellationToken);
                             removeCompletedBeforeCallbackReturned = removeCompleted.Wait (
                                                                                           TimeSpan.FromSeconds (5),
                                                                                           cancellationToken);

                             return false;
                         });

        timedEvents.RunTimers ();
        bool removed = await removeTask!;

        Assert.True (removeStartedBeforeCallbackReturned, "The Remove task should start before the callback returns.");
        Assert.True (removeCompletedBeforeCallbackReturned, "Remove should complete before the callback returns.");
        Assert.True (removed);
    }

    // CoPilot - GPT-5
    [Fact]
    public async Task RunTimers_Concurrent_Callers_Do_Not_Execute_Callbacks_Concurrently ()
    {
        TimedEvents timedEvents = new ();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ManualResetEventSlim firstCallbackStarted = new ();
        using ManualResetEventSlim releaseFirstCallback = new ();
        using ManualResetEventSlim concurrentCallbackStarted = new ();
        var activeCallbacks = 0;
        var callbackCount = 0;
        var firstCallbackReleased = false;

        Func<bool> callback = () =>
                              {
                                  if (Interlocked.Increment (ref activeCallbacks) > 1)
                                  {
                                      concurrentCallbackStarted.Set ();
                                  }

                                  int currentCallback = Interlocked.Increment (ref callbackCount);

                                  if (currentCallback == 1)
                                  {
                                      firstCallbackStarted.Set ();
                                      firstCallbackReleased = releaseFirstCallback.Wait (
                                                                                         TimeSpan.FromSeconds (5),
                                                                                         cancellationToken);
                                  }

                                  Interlocked.Decrement (ref activeCallbacks);

                                  return false;
                              };

        timedEvents.Add (TimeSpan.Zero, callback);
        timedEvents.Add (TimeSpan.Zero, callback);

        Task firstRunner = RunOnDedicatedThread (timedEvents.RunTimers);
        Task? secondRunner = null;
        var firstCallbackStartedBeforeTimeout = false;
        var secondRunnerReturned = false;
        var callbacksRanConcurrently = false;

        try
        {
            firstCallbackStartedBeforeTimeout = firstCallbackStarted.Wait (
                                                                            TimeSpan.FromSeconds (5),
                                                                            cancellationToken);

            if (firstCallbackStartedBeforeTimeout)
            {
                secondRunner = RunOnDedicatedThread (timedEvents.RunTimers);
                secondRunnerReturned = await CompletesWithinAsync (
                                                                    secondRunner,
                                                                    cancellationToken);
                callbacksRanConcurrently = concurrentCallbackStarted.IsSet;
            }
        }
        finally
        {
            releaseFirstCallback.Set ();
        }

        await firstRunner;

        if (secondRunner is not null)
        {
            await secondRunner;
        }

        Assert.True (firstCallbackStartedBeforeTimeout, "The first callback should start.");
        Assert.True (firstCallbackReleased, "The first callback should be released before its wait times out.");
        Assert.True (secondRunnerReturned, "A competing RunTimers caller should return while another runner is active.");
        Assert.False (callbacksRanConcurrently);
        Assert.Equal (2, callbackCount);
    }

    // CoPilot - GPT-5
    [Fact]
    public async Task Added_Handler_Does_Not_Block_Add_From_Other_Thread ()
    {
        TimedEvents timedEvents = new ();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ManualResetEventSlim addStarted = new ();
        using ManualResetEventSlim addCompleted = new ();
        Task? addTask = null;
        var addStartedBeforeHandlerReturned = false;
        var addCompletedBeforeHandlerReturned = false;
        EventHandler<TimeoutEventArgs>? handler = null;

        handler = (_, _) =>
                  {
                      timedEvents.Added -= handler;
                      addTask = RunOnDedicatedThread (
                                                        () =>
                                                        {
                                                            try
                                                            {
                                                                addStarted.Set ();
                                                                timedEvents.Add (TimeSpan.FromHours (1), () => false);
                                                            }
                                                            finally
                                                            {
                                                                addCompleted.Set ();
                                                            }
                                                        });
                      addStartedBeforeHandlerReturned = addStarted.Wait (
                                                                         TimeSpan.FromSeconds (5),
                                                                         cancellationToken);
                      addCompletedBeforeHandlerReturned = addCompleted.Wait (
                                                                             TimeSpan.FromSeconds (5),
                                                                             cancellationToken);
                  };
        timedEvents.Added += handler;

        timedEvents.Add (TimeSpan.FromHours (1), () => false);
        await addTask!;

        Assert.True (addStartedBeforeHandlerReturned, "The Add task should start before the Added handler returns.");
        Assert.True (addCompletedBeforeHandlerReturned, "Add should complete before the Added handler returns.");
    }

    // CoPilot - GPT-5
    [Fact]
    public async Task RunTimers_Remove_Active_Repeating_Timeout_Prevents_Reschedule ()
    {
        TimedEvents timedEvents = new ();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ManualResetEventSlim callbackStarted = new ();
        using ManualResetEventSlim releaseCallback = new ();
        var callbackReleased = false;
        object timeout = timedEvents.Add (
                                                 TimeSpan.Zero,
                                                 () =>
                                                 {
                                                     callbackStarted.Set ();
                                                     callbackReleased = releaseCallback.Wait (
                                                                                              TimeSpan.FromSeconds (5),
                                                                                              cancellationToken);

                                                     return true;
                                                 });
        Task runnerTask = RunOnDedicatedThread (timedEvents.RunTimers);
        Task<bool>? removeTask = null;
        var callbackStartedBeforeTimeout = false;
        var removeReturnedBeforeCallback = false;

        try
        {
            callbackStartedBeforeTimeout = callbackStarted.Wait (
                                                                 TimeSpan.FromSeconds (5),
                                                                 cancellationToken);

            if (callbackStartedBeforeTimeout)
            {
                removeTask = RunOnDedicatedThread (() => timedEvents.Remove (timeout));
                removeReturnedBeforeCallback = await CompletesWithinAsync (
                                                                           removeTask,
                                                                           cancellationToken);
            }
        }
        finally
        {
            releaseCallback.Set ();
        }

        await runnerTask;
        bool removed = removeTask is not null && await removeTask;

        Assert.True (callbackStartedBeforeTimeout, "The repeating callback should start.");
        Assert.True (callbackReleased, "The repeating callback should be released before its wait times out.");
        Assert.True (removeReturnedBeforeCallback, "Remove should return while the callback is active.");
        Assert.True (removed);
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - Claude Opus 5
    [Fact]
    public async Task Remove_Returns_True_For_Active_Occurrence_And_Does_Not_Interrupt_It ()
    {
        // Pins the documented contract: Remove reports true when the only match is an occurrence that has already been
        // dequeued and is executing, and it neither waits for nor interrupts that callback.
        TimedEvents timedEvents = new ();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ManualResetEventSlim callbackStarted = new ();
        using ManualResetEventSlim releaseCallback = new ();
        var callbackReleased = false;
        var callbackCompleted = false;

        object timeout = timedEvents.Add (
                                          TimeSpan.Zero,
                                          () =>
                                          {
                                              callbackStarted.Set ();
                                              callbackReleased = releaseCallback.Wait (
                                                                                       TimeSpan.FromSeconds (5),
                                                                                       cancellationToken);
                                              callbackCompleted = true;

                                              return false;
                                          });

        Task runnerTask = RunOnDedicatedThread (timedEvents.RunTimers);
        Task<bool>? removeTask = null;
        var callbackStartedBeforeTimeout = false;
        var removeReturnedBeforeCallback = false;
        var queueWasEmptyWhenRemoveRan = false;
        var callbackStillActiveWhenRemoveReturned = false;

        try
        {
            callbackStartedBeforeTimeout = callbackStarted.Wait (TimeSpan.FromSeconds (5), cancellationToken);

            if (callbackStartedBeforeTimeout)
            {
                // The occurrence was dequeued before the callback ran, so nothing is left for Remove to unqueue.
                queueWasEmptyWhenRemoveRan = timedEvents.Timeouts.Count == 0;
                removeTask = RunOnDedicatedThread (() => timedEvents.Remove (timeout));
                removeReturnedBeforeCallback = await CompletesWithinAsync (removeTask, cancellationToken);
                callbackStillActiveWhenRemoveReturned = !callbackCompleted;
            }
        }
        finally
        {
            releaseCallback.Set ();
        }

        await runnerTask;
        bool removed = removeTask is not null && await removeTask;

        Assert.True (callbackStartedBeforeTimeout, "The callback should start.");
        Assert.True (queueWasEmptyWhenRemoveRan, "The active occurrence should already be dequeued.");
        Assert.True (removeReturnedBeforeCallback, "Remove should return while the callback is active.");
        Assert.True (callbackStillActiveWhenRemoveReturned, "Remove should not wait for the active callback to finish.");
        Assert.True (removed, "Remove should report true for the cancelled active occurrence.");
        Assert.True (callbackReleased, "The callback should be released before its wait times out.");
        Assert.True (callbackCompleted, "Remove should not interrupt the active callback.");
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public async Task RunTimers_StopAll_During_Active_Repeating_Timeout_Prevents_Reschedule ()
    {
        TimedEvents timedEvents = new ();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ManualResetEventSlim callbackStarted = new ();
        using ManualResetEventSlim releaseCallback = new ();
        var callbackReleased = false;

        timedEvents.Add (
                         TimeSpan.Zero,
                         () =>
                         {
                             callbackStarted.Set ();
                             callbackReleased = releaseCallback.Wait (TimeSpan.FromSeconds (5), cancellationToken);

                             return true;
                         });

        Task runnerTask = RunOnDedicatedThread (timedEvents.RunTimers);
        Task? stopAllTask = null;
        var callbackStartedBeforeTimeout = false;
        var stopAllReturnedBeforeCallback = false;

        try
        {
            callbackStartedBeforeTimeout = callbackStarted.Wait (
                                                                 TimeSpan.FromSeconds (5),
                                                                 cancellationToken);

            if (callbackStartedBeforeTimeout)
            {
                stopAllTask = RunOnDedicatedThread (timedEvents.StopAll);
                stopAllReturnedBeforeCallback = await CompletesWithinAsync (
                                                                            stopAllTask,
                                                                            cancellationToken);
            }
        }
        finally
        {
            releaseCallback.Set ();
        }

        await runnerTask;

        if (stopAllTask is not null)
        {
            await stopAllTask;
        }

        Assert.True (callbackStartedBeforeTimeout, "The repeating callback should start.");
        Assert.True (callbackReleased, "The repeating callback should be released before its wait times out.");
        Assert.True (stopAllReturnedBeforeCallback, "StopAll should return while the callback is active.");
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public async Task RunTimers_Competing_Caller_Returns_Without_Running_Newly_Due_Timeout ()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        long currentTicks = DateTime.UnixEpoch.Ticks;
        var timeReadCount = 0;
        var callbackCount = 0;
        using ManualResetEventSlim dueCheckStarted = new ();
        using ManualResetEventSlim releaseDueCheck = new ();
        var dueCheckReleased = false;

        DateTime GetCurrentTime ()
        {
            long capturedTicks = Interlocked.Read (ref currentTicks);

            if (Interlocked.Increment (ref timeReadCount) == 2)
            {
                dueCheckStarted.Set ();
                dueCheckReleased = releaseDueCheck.Wait (TimeSpan.FromSeconds (5), cancellationToken);
            }

            return new (capturedTicks, DateTimeKind.Utc);
        }

        TimedEvents timedEvents = new (new FuncTimeProvider (GetCurrentTime));
        timedEvents.Add (
                         TimeSpan.FromSeconds (1),
                         () =>
                         {
                             Interlocked.Increment (ref callbackCount);

                             return false;
                         });

        Task firstRunner = RunOnDedicatedThread (timedEvents.RunTimers);
        Task? secondRunner = null;
        var dueCheckStartedBeforeTimeout = false;
        var secondRunnerReturned = false;

        try
        {
            dueCheckStartedBeforeTimeout = dueCheckStarted.Wait (
                                                                 TimeSpan.FromSeconds (5),
                                                                 cancellationToken);

            if (dueCheckStartedBeforeTimeout)
            {
                Interlocked.Exchange (ref currentTicks, DateTime.UnixEpoch.AddSeconds (2).Ticks);
                secondRunner = RunOnDedicatedThread (timedEvents.RunTimers);
                secondRunnerReturned = await CompletesWithinAsync (
                                                                    secondRunner,
                                                                    cancellationToken);
            }
        }
        finally
        {
            releaseDueCheck.Set ();
        }

        await firstRunner;

        if (secondRunner is not null)
        {
            await secondRunner;
        }

        Assert.True (dueCheckStartedBeforeTimeout, "The first runner should reach the due-time check.");
        Assert.True (dueCheckReleased, "The due-time check should be released before its wait times out.");
        Assert.True (secondRunnerReturned, "The competing RunTimers call should return while the runner is active.");
        Assert.Equal (0, callbackCount);
        Assert.Single (timedEvents.Timeouts);

        timedEvents.RunTimers ();

        Assert.Equal (1, callbackCount);
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public void RunTimers_Callback_Exception_Propagates_And_Ends_Pass ()
    {
        TimedEvents timedEvents = new ();
        InvalidOperationException expectedException = new ("Expected callback failure.");
        var followUpCallbackCount = 0;

        timedEvents.Add (TimeSpan.Zero, () => throw expectedException);
        timedEvents.Add (
                         TimeSpan.Zero,
                         () =>
                         {
                             Interlocked.Increment (ref followUpCallbackCount);

                             return false;
                         });

        InvalidOperationException actualException = Assert.Throws<InvalidOperationException> (timedEvents.RunTimers);

        Assert.Same (expectedException, actualException);
        Assert.Equal (0, followUpCallbackCount);
        Assert.Single (timedEvents.Timeouts);

        timedEvents.RunTimers ();

        Assert.Equal (1, followUpCallbackCount);
        Assert.Empty (timedEvents.Timeouts);
    }

    // Claude - Opus 5
    [Fact]
    public void RunTimers_Added_Handler_Exception_Does_Not_Replace_Callback_Exception ()
    {
        TimedEvents timedEvents = new ();
        InvalidOperationException expectedException = new ("Expected callback failure.");

        // A repeating timeout that throws. Added is raised outside the finally that completes the
        // occurrence, so a throwing subscriber must not be able to displace the callback's exception.
        timedEvents.Add (TimeSpan.FromMilliseconds (1), () => throw expectedException);
        timedEvents.Added += (_, _) => throw new NotSupportedException ("Added handler failure.");

        Thread.Sleep (5);

        InvalidOperationException actualException = Assert.Throws<InvalidOperationException> (timedEvents.RunTimers);

        Assert.Same (expectedException, actualException);

        // A throwing callback is not rescheduled, so the queue drains.
        Assert.Empty (timedEvents.Timeouts);
    }

    // Claude - Opus 5
    [Fact]
    public void RunTimers_Added_Handler_Exception_During_Reschedule_Leaves_Timeout_Scheduled ()
    {
        TimedEvents timedEvents = new ();
        var callbackCount = 0;

        timedEvents.Add (
                         TimeSpan.FromMilliseconds (1),
                         () =>
                         {
                             Interlocked.Increment (ref callbackCount);

                             return true;
                         });

        timedEvents.Added += (_, _) => throw new InvalidOperationException ("Added handler failure.");

        Thread.Sleep (5);

        // The reschedule succeeds before Added is raised, so the timeout survives the handler exception
        // and later passes keep running it rather than losing it.
        Assert.Throws<InvalidOperationException> (timedEvents.RunTimers);
        Assert.Equal (1, callbackCount);
        Assert.Single (timedEvents.Timeouts);

        Thread.Sleep (5);

        Assert.Throws<InvalidOperationException> (timedEvents.RunTimers);
        Assert.Equal (2, callbackCount);
        Assert.Single (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public void RunTimers_StopAll_Cancels_Duplicate_Active_Timeout_Occurrences ()
    {
        VirtualTimeProvider timeProvider = new ();
        TimedEvents timedEvents = new (timeProvider);
        var callbackCount = 0;
        Terminal.Gui.App.Timeout timeout = new () { Span = TimeSpan.FromSeconds (1) };

        timeout.Callback = () =>
                           {
                               callbackCount++;

                               if (callbackCount == 1)
                               {
                                   timedEvents.RunTimers ();

                                   return true;
                               }

                               timedEvents.StopAll ();

                               return true;
                           };

        timedEvents.Add (timeout);
        timedEvents.Add (timeout);
        timeProvider.Advance (TimeSpan.FromSeconds (2));

        timedEvents.RunTimers ();

        Assert.Equal (2, callbackCount);
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public void RunTimers_StopAll_Cancels_Active_Occurrence_Not_Later_Same_Timeout_Occurrence ()
    {
        VirtualTimeProvider timeProvider = new ();
        TimedEvents timedEvents = new (timeProvider);
        var callbackCount = 0;
        Terminal.Gui.App.Timeout timeout = new () { Span = TimeSpan.FromSeconds (1) };

        timeout.Callback = () =>
                           {
                               callbackCount++;

                               if (callbackCount == 1)
                               {
                                   timedEvents.StopAll ();
                                   timeout.Span = TimeSpan.FromSeconds (1);
                                   timedEvents.Add (timeout);
                                   timeProvider.Advance (TimeSpan.FromSeconds (2));
                                   timedEvents.RunTimers ();

                                   // Distinguish an incorrect reschedule of this pre-StopAll occurrence.
                                   timeout.Span = TimeSpan.FromSeconds (10);

                                   return true;
                               }

                               return callbackCount == 2;
                           };

        timedEvents.Add (timeout);
        timeProvider.Advance (TimeSpan.FromSeconds (2));

        timedEvents.RunTimers ();

        Assert.Equal (2, callbackCount);
        Assert.Single (timedEvents.Timeouts);

        timeProvider.Advance (TimeSpan.FromSeconds (2));
        timedEvents.RunTimers ();

        Assert.Equal (3, callbackCount);
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public async Task RunTimers_Remove_Cancels_Repeating_Duplicate_Active_Timeout_Occurrence ()
    {
        VirtualTimeProvider timeProvider = new ();
        TimedEvents timedEvents = new (timeProvider);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ManualResetEventSlim removeCompleted = new ();
        var callbackCount = 0;
        var removeCompletedBeforeCallbackReturned = false;
        Task<bool>? removeTask = null;
        Terminal.Gui.App.Timeout timeout = new () { Span = TimeSpan.FromSeconds (1) };

        timeout.Callback = () =>
                           {
                               callbackCount++;

                               if (callbackCount == 1)
                               {
                                   timedEvents.RunTimers ();

                                   return true;
                               }

                               removeTask = RunOnDedicatedThread (
                                                                  () =>
                                                                  {
                                                                      try
                                                                      {
                                                                          return timedEvents.Remove (timeout);
                                                                      }
                                                                      finally
                                                                      {
                                                                          removeCompleted.Set ();
                                                                      }
                                                                  });
                               removeCompletedBeforeCallbackReturned = removeCompleted.Wait (
                                                                                                TimeSpan.FromSeconds (5),
                                                                                                cancellationToken);

                               return false;
                           };

        timedEvents.Add (timeout);
        timedEvents.Add (timeout);
        timeProvider.Advance (TimeSpan.FromSeconds (2));

        timedEvents.RunTimers ();
        bool removed = await removeTask!;

        Assert.Equal (2, callbackCount);
        Assert.True (removeCompletedBeforeCallbackReturned, "Remove should return while both occurrences are active.");
        Assert.True (removed);
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public void Remove_Cancels_Queued_And_Active_Occurrences_With_Same_Token ()
    {
        VirtualTimeProvider timeProvider = new ();
        TimedEvents timedEvents = new (timeProvider);
        var callbackCount = 0;
        var removed = false;
        Terminal.Gui.App.Timeout timeout = new () { Span = TimeSpan.FromSeconds (1) };

        timeout.Callback = () =>
                           {
                               callbackCount++;
                               timedEvents.Add (timeout);
                               removed = timedEvents.Remove (timeout);

                               return true;
                           };

        timedEvents.Add (timeout);
        timeProvider.Advance (TimeSpan.FromSeconds (2));

        timedEvents.RunTimers ();

        Assert.True (removed);
        Assert.Equal (1, callbackCount);
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public void Remove_Cancels_All_Queued_Occurrences_With_Same_Token ()
    {
        TimedEvents timedEvents = new ();
        Terminal.Gui.App.Timeout timeout = new ()
        {
            Span = TimeSpan.FromHours (1),
            Callback = () => false
        };

        timedEvents.Add (timeout);
        timedEvents.Add (timeout);

        bool removed = timedEvents.Remove (timeout);

        Assert.True (removed);
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public void GetTimeout_Returns_Null_For_Active_Occurrence_That_Remove_Can_Cancel ()
    {
        TimedEvents timedEvents = new ();
        TimeSpan? actualSpan = TimeSpan.MaxValue;
        var removed = false;
        object? token = null;

        token = timedEvents.Add (
                                 TimeSpan.Zero,
                                 () =>
                                 {
                                     actualSpan = timedEvents.GetTimeout (token!);
                                     removed = timedEvents.Remove (token!);

                                     return true;
                                 });

        timedEvents.RunTimers ();

        Assert.Null (actualSpan);
        Assert.True (removed);
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public void Remove_Does_Not_Cancel_Same_Timeout_Added_After_Remove ()
    {
        VirtualTimeProvider timeProvider = new ();
        TimedEvents timedEvents = new (timeProvider);
        var callbackCount = 0;
        var removed = false;
        Terminal.Gui.App.Timeout timeout = new () { Span = TimeSpan.FromSeconds (1) };

        timeout.Callback = () =>
                           {
                               callbackCount++;

                               if (callbackCount > 1)
                               {
                                   return false;
                               }

                               removed = timedEvents.Remove (timeout);
                               timedEvents.Add (timeout);

                               return true;
                           };

        timedEvents.Add (timeout);
        timeProvider.Advance (TimeSpan.FromSeconds (2));
        timedEvents.RunTimers ();

        Assert.True (removed);
        Assert.Single (timedEvents.Timeouts);

        timeProvider.Advance (TimeSpan.FromSeconds (2));
        timedEvents.RunTimers ();

        Assert.Equal (2, callbackCount);
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public void Remove_Does_Not_Cancel_Nested_Active_Occurrence_Added_After_Remove ()
    {
        VirtualTimeProvider timeProvider = new ();
        TimedEvents timedEvents = new (timeProvider);
        var callbackCount = 0;
        var removed = false;
        Terminal.Gui.App.Timeout timeout = new () { Span = TimeSpan.FromSeconds (1) };

        timeout.Callback = () =>
                           {
                               callbackCount++;

                               if (callbackCount == 1)
                               {
                                   removed = timedEvents.Remove (timeout);
                                   timedEvents.Add (timeout);
                                   timeProvider.Advance (TimeSpan.FromSeconds (2));
                                   timedEvents.RunTimers ();

                                   return true;
                               }

                               return callbackCount == 2;
                           };

        timedEvents.Add (timeout);
        timeProvider.Advance (TimeSpan.FromSeconds (2));
        timedEvents.RunTimers ();

        Assert.True (removed);
        Assert.Equal (2, callbackCount);
        Assert.Single (timedEvents.Timeouts);

        timeProvider.Advance (TimeSpan.FromSeconds (2));
        timedEvents.RunTimers ();

        Assert.Equal (3, callbackCount);
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public void Remove_Returns_False_When_Active_Occurrence_Is_Already_Cancelled ()
    {
        TimedEvents timedEvents = new ();
        var firstRemove = false;
        var secondRemove = true;
        object? token = null;

        token = timedEvents.Add (
                                 TimeSpan.Zero,
                                 () =>
                                 {
                                     firstRemove = timedEvents.Remove (token!);
                                     secondRemove = timedEvents.Remove (token!);

                                     return true;
                                 });

        timedEvents.RunTimers ();

        Assert.True (firstRemove);
        Assert.False (secondRemove);
        Assert.Empty (timedEvents.Timeouts);
    }

    // CoPilot - GPT-5
    [Fact]
    public void Remove_Returns_False_When_StopAll_Already_Cancelled_Active_Occurrence ()
    {
        TimedEvents timedEvents = new ();
        var removed = true;
        object? token = null;

        token = timedEvents.Add (
                                 TimeSpan.Zero,
                                 () =>
                                 {
                                     timedEvents.StopAll ();
                                     removed = timedEvents.Remove (token!);

                                     return true;
                                 });

        timedEvents.RunTimers ();

        Assert.False (removed);
        Assert.Empty (timedEvents.Timeouts);
    }

    [Fact]
    public void HighFrequency_Concurrent_Invocations_No_Lost_Timeouts ()
    {
        var timedEvents = new TimedEvents ();
        var counter = 0;
        var expected = 1000;
        var completed = new ManualResetEventSlim (false);

        // Add many timeouts with TimeSpan.Zero concurrently
        Parallel.For (
                      0,
                      expected,
                      i =>
                      {
                          timedEvents.Add (
                                           TimeSpan.Zero,
                                           () =>
                                           {
                                               int current = Interlocked.Increment (ref counter);

                                               if (current == expected)
                                               {
                                                   completed.Set ();
                                               }

                                               return false; // One-shot
                                           });
                      });

        // Run timers multiple times to ensure all are processed
        for (var i = 0; i < 10; i++)
        {
            timedEvents.RunTimers ();

            if (completed.IsSet)
            {
                break;
            }

            Thread.Sleep (10);
        }

        Assert.Equal (expected, counter);
    }

    [Fact]
    public void GetTimestampTicks_Provides_High_Resolution ()
    {
        var timedEvents = new TimedEvents ();

        // Add multiple timeouts with TimeSpan.Zero rapidly
        List<long> timestamps = new ();

        // Single event handler to capture all timestamps
        EventHandler<TimeoutEventArgs>? handler = null;
        handler = (s, e) => { timestamps.Add (e.Ticks); };

        timedEvents.Added += handler;

        for (var i = 0; i < 100; i++)
        {
            timedEvents.Add (TimeSpan.Zero, () => false);
        }

        timedEvents.Added -= handler;

        // Verify that we got timestamps
        Assert.True (timestamps.Count > 0, $"Should have captured timestamps. Got {timestamps.Count}");

        // Verify that we got unique timestamps (or very close)
        // With Stopwatch, we should have much better resolution than DateTime.UtcNow
        int uniqueTimestamps = timestamps.Distinct ().Count ();

        // We should have mostly unique timestamps
        // Allow some duplicates due to extreme speed, but should be > 50% unique
        Assert.True (
                     uniqueTimestamps > timestamps.Count / 2,
                     $"Expected more unique timestamps. Got {uniqueTimestamps} unique out of {timestamps.Count} total");
    }

    // Claude - Opus 5
    [Fact]
    public void Add_Timeout_Throws_For_Null_Timeout ()
    {
        TimedEvents timedEvents = new ();

        Assert.Throws<ArgumentNullException> (() => timedEvents.Add ((Terminal.Gui.App.Timeout)null!));
    }

    // Claude - Opus 5
    [Fact]
    public void Add_Timeout_Throws_For_Null_Callback ()
    {
        TimedEvents timedEvents = new ();
        Terminal.Gui.App.Timeout timeout = new () { Span = TimeSpan.Zero };

        Assert.Throws<ArgumentNullException> (() => timedEvents.Add (timeout));
        Assert.Empty (timedEvents.Timeouts);
    }

    // Claude - Opus 5
    [Fact]
    public void Added_Reports_Actual_Queue_Key_After_Collision ()
    {
        VirtualTimeProvider timeProvider = new ();
        TimedEvents timedEvents = new (timeProvider);
        List<long> addedTicks = [];
        timedEvents.Added += (_, e) => addedTicks.Add (e.Ticks);

        timedEvents.Add (TimeSpan.Zero, () => false);
        timedEvents.Add (TimeSpan.Zero, () => false);

        Assert.Equal (timedEvents.Timeouts.Keys, addedTicks);
        Assert.Equal (2, addedTicks.Distinct ().Count ());
    }

    // Claude - Opus 5
    [Fact]
    public void Timeouts_Returns_Snapshot ()
    {
        TimedEvents timedEvents = new ();
        timedEvents.Add (TimeSpan.FromHours (1), () => false);

        SortedList<long, Terminal.Gui.App.Timeout> snapshot = timedEvents.Timeouts;

        timedEvents.Add (TimeSpan.FromHours (2), () => false);
        snapshot.Clear ();

        Assert.Equal (2, timedEvents.Timeouts.Count);
    }

    // Claude - Opus 5
    [Fact]
    public async Task RunTimers_Defers_Repeating_Zero_Timeout_Until_Next_Pass ()
    {
        TimedEvents timedEvents = new (new VirtualTimeProvider ());
        var callbackCount = 0;
        object token = timedEvents.Add (
                                        TimeSpan.Zero,
                                        () =>
                                        {
                                            Interlocked.Increment (ref callbackCount);

                                            return true;
                                        });
        Task runner = RunOnDedicatedThread (timedEvents.RunTimers);
        Task completedTask = await Task.WhenAny (
                                                 runner,
                                                 Task.Delay (TimeSpan.FromSeconds (1), TestContext.Current.CancellationToken));
        var returned = ReferenceEquals (completedTask, runner);
        int countAfterPass = Volatile.Read (ref callbackCount);
        int queuedAfterPass = returned ? timedEvents.Timeouts.Count : 0;
        int countAfterNextPass = countAfterPass;

        if (returned)
        {
            timedEvents.RunTimers ();
            countAfterNextPass = Volatile.Read (ref callbackCount);
        }

        timedEvents.Remove (token);
        await runner.WaitAsync (TimeSpan.FromSeconds (5), TestContext.Current.CancellationToken);

        Assert.True (returned, "RunTimers should return after the callbacks that were due when the pass started.");
        Assert.Equal (1, countAfterPass);
        Assert.Equal (1, queuedAfterPass);
        Assert.Equal (2, countAfterNextPass);
    }

    // CoPilot - GPT-5
    [Fact]
    public void RunTimers_Repeating_Zero_Timeout_Does_Not_Starve_Due_Peer ()
    {
        TimedEvents timedEvents = new (new VirtualTimeProvider ());
        var repeatingCallbackCount = 0;
        var peerCallbackCount = 0;
        object repeatingToken = timedEvents.Add (
                                                 TimeSpan.Zero,
                                                 () =>
                                                 {
                                                     repeatingCallbackCount++;

                                                     return true;
                                                 });
        timedEvents.Add (
                         TimeSpan.Zero,
                         () =>
                         {
                             peerCallbackCount++;

                             return false;
                         });

        timedEvents.RunTimers ();

        Assert.Equal (1, repeatingCallbackCount);
        Assert.Equal (1, peerCallbackCount);
        Assert.Same (repeatingToken, Assert.Single (timedEvents.Timeouts).Value);

        timedEvents.Remove (repeatingToken);
    }

    [Fact]
    public void TimeSpan_Zero_Executes_Immediately ()
    {
        var timedEvents = new TimedEvents ();
        var executed = false;

        timedEvents.Add (
                         TimeSpan.Zero,
                         () =>
                         {
                             executed = true;

                             return false;
                         });

        Assert.True (timedEvents.Timeouts.Keys [0] > 0);

        // Should execute on first RunTimers call
        timedEvents.RunTimers ();

        Assert.Empty (timedEvents.Timeouts);
        Assert.True (executed);
    }

    [Fact]
    public void Multiple_TimeSpan_Zero_Timeouts_All_Execute ()
    {
        var timedEvents = new TimedEvents ();
        var executeCount = 0;
        var expected = 100;

        for (var i = 0; i < expected; i++)
        {
            timedEvents.Add (
                             TimeSpan.Zero,
                             () =>
                             {
                                 Interlocked.Increment (ref executeCount);

                                 return false;
                             });
        }

        // Run timers once
        timedEvents.RunTimers ();

        Assert.Equal (expected, executeCount);
    }

    [Fact]
    public void StopAll_Stops_All_Timeouts ()
    {
        var timedEvents = new TimedEvents ();
        var executeCount = 0;
        var expected = 100;

        for (var i = 0; i < expected; i++)
        {
            timedEvents.Add (
                             TimeSpan.Zero,
                             () =>
                             {
                                 Interlocked.Increment (ref executeCount);

                                 return false;
                             });
        }

        Assert.Equal (expected, timedEvents.Timeouts.Count);

        timedEvents.StopAll ();

        Assert.Empty (timedEvents.Timeouts);

        // Run timers once
        timedEvents.RunTimers ();

        Assert.Equal (0, executeCount);
    }

    private static async Task<bool> CompletesWithinAsync (Task task, CancellationToken cancellationToken)
    {
        try
        {
            await task.WaitAsync (TimeSpan.FromSeconds (5), cancellationToken).ConfigureAwait (false);

            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static Task RunOnDedicatedThread (Action action) =>
        Task.Factory.StartNew (
                               action,
                               CancellationToken.None,
                               TaskCreationOptions.LongRunning,
                               TaskScheduler.Default);

    private static Task<TResult> RunOnDedicatedThread<TResult> (Func<TResult> action) =>
        Task.Factory.StartNew (
                               action,
                               CancellationToken.None,
                               TaskCreationOptions.LongRunning,
                               TaskScheduler.Default);

    private static bool WaitForCleanup (ManualResetEventSlim waitHandle) => waitHandle.Wait (TimeSpan.FromSeconds (5));
}

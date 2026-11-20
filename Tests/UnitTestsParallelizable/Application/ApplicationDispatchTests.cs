// CoPilot - GPT-6
#nullable enable

using System.Reflection;
using Moq;

namespace ApplicationTests;

[Collection ("Application Tests")]
public class ApplicationDispatchTests
{
    [Fact]
    public void WorkerDispatch_CompletesOnlyAfterUiThreadRunsCallback ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;

        try
        {
            int? callbackThreadId = null;
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (() => callbackThreadId = Thread.CurrentThread.ManagedThreadId));

            Assert.False (dispatch.IsCompleted, $"Dispatch status: {dispatch.Status}; main thread: {app.MainThreadId}; test thread: {Thread.CurrentThread.ManagedThreadId}; callback thread: {callbackThreadId}");
            Assert.Null (callbackThreadId);

            ((ApplicationImpl)app).Coordinator!.RunIteration ();

            Assert.True (dispatch.IsCompletedSuccessfully);
            Assert.Equal (app.MainThreadId, callbackThreadId);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    // CoPilot - GPT-6
    [Fact]
    public void RunLoop_DrainsQueuedDispatchOnUiThread ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        app.StopAfterFirstIteration = true;
        Task? dispatch = null;
        int? callbackThreadId = null;
        EventHandler<SessionTokenEventArgs> handler = (_, args) =>
        {
            Thread worker = new (() => dispatch = app.InvokeAsync (args.State, () => callbackThreadId = Thread.CurrentThread.ManagedThreadId));
            worker.Start ();
            Assert.True (worker.Join (TimeSpan.FromSeconds (5)));
        };
        app.SessionBegun += handler;

        try
        {
            app.Run (runnable);

            Assert.NotNull (dispatch);
            Assert.True (dispatch.IsCompletedSuccessfully);
            Assert.Equal (app.MainThreadId, callbackThreadId);
        }
        finally
        {
            app.SessionBegun -= handler;
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    // CoPilot - GPT-6
    [Fact]
    public void ThrowingAddedHandler_DoesNotAffectDispatch ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        TimedEvents timedEvents = Assert.IsType<TimedEvents> (app.TimedEvents);
        InvalidOperationException failure = new ("Added handler failure.");
        EventHandler<TimeoutEventArgs> handler = (_, _) => throw failure;
        timedEvents.Added += handler;

        try
        {
            bool ran = false;
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (() => ran = true));

            Assert.False (dispatch.IsCompleted);
            Assert.Empty (timedEvents.Timeouts);
            PumpUiDispatches (app);

            Assert.True (dispatch.IsCompletedSuccessfully);
            Assert.True (ran);
        }
        finally
        {
            timedEvents.Added -= handler;
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    // CoPilot - GPT-6
    [Fact]
    public void WorkerTimerPump_CannotRunQueuedDispatchOffUiThread ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;

        try
        {
            int? callbackThreadId = null;
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (() => callbackThreadId = Thread.CurrentThread.ManagedThreadId));
            Thread worker = new (() =>
            {
                app.TimedEvents!.RunTimers ();
                ((ApplicationImpl)app).DrainDispatches ();
            });
            worker.Start ();
            Assert.True (worker.Join (TimeSpan.FromSeconds (5)));

            Assert.False (dispatch.IsCompleted);
            Assert.Null (callbackThreadId);
            PumpUiDispatches (app);

            Assert.True (dispatch.IsCompletedSuccessfully);
            Assert.Equal (app.MainThreadId, callbackThreadId);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    // Claude - Opus 5.5
    [Fact]
    public void WorkerTimerPump_CannotRunPostedCallbackOffUiThread ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        SynchronizationContext context = ((ApplicationImpl)app).SynchronizationContext!;
        Runnable runnable = new ();
        SessionToken? owner = null;

        try
        {
            int? callbackThreadId = null;

            // Post before the first session, then pump timers from a worker before and after the session begins.
            RunOnWorker (() =>
                         {
                             context.Post (_ => callbackThreadId = Thread.CurrentThread.ManagedThreadId, null);
                             app.TimedEvents!.RunTimers ();
                         });
            Assert.Null (callbackThreadId);

            owner = app.Begin (runnable)!;
            RunOnWorker (() =>
                         {
                             app.TimedEvents!.RunTimers ();
                             ((ApplicationImpl)app).DrainDispatches ();
                         });
            Assert.Null (callbackThreadId);

            PumpUiDispatches (app);

            Assert.Equal (app.MainThreadId, callbackThreadId);
        }
        finally
        {
            if (owner is { })
            {
                app.End (owner);
            }

            runnable.Dispose ();
            app.Dispose ();
        }
    }

    // CoPilot - GPT-6
    [Fact]
    public void RemovingOrStoppingTimers_DoesNotStrandQueuedDispatch ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;

        try
        {
            bool ran = false;
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (() => ran = true));
            bool removedInAdded = false;
            EventHandler<TimeoutEventArgs> removeOnAdd = (_, args) => removedInAdded = app.TimedEvents!.Remove (args.Timeout);
            app.TimedEvents!.Added += removeOnAdd;

            try
            {
                app.AddTimeout (TimeSpan.FromHours (1), () => false);
            }
            finally
            {
                app.TimedEvents.Added -= removeOnAdd;
            }

            Assert.True (removedInAdded);
            object timeout = app.AddTimeout (TimeSpan.FromHours (1), () => false)!;
            Assert.True (app.TimedEvents!.Remove (timeout));
            app.TimedEvents.StopAll ();

            Assert.False (dispatch.IsCompleted);
            PumpUiDispatches (app);

            Assert.True (dispatch.IsCompletedSuccessfully);
            Assert.True (ran);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void UiThreadDispatch_CompletesImmediately ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;

        try
        {
            bool ran = false;
            Task dispatch = app.InvokeAsync (owner, instance =>
            {
                Assert.Same (app, instance);
                ran = true;
            }, TestContext.Current.CancellationToken);

            Assert.True (ran);
            Assert.True (dispatch.IsCompletedSuccessfully);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void UiThreadDispatch_AlsoRunsInlineForNonViewRunnable ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Mock<IRunnable> runnable = new ();
        runnable.SetupGet (instance => instance.IsRunning).Returns (true);
        app.SessionStack!.Push (new SessionToken (runnable.Object));

        try
        {
            bool ran = false;

            Task dispatch = app.InvokeAsync (() => ran = true, TestContext.Current.CancellationToken);

            Assert.True (dispatch.IsCompletedSuccessfully);
            Assert.True (ran);
        }
        finally
        {
            app.SessionStack.Clear ();
            app.Dispose ();
        }
    }

    [Fact]
    public void UiThreadDispatch_CanInvokeAsyncReentrantly ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;

        try
        {
            List<string> order = [];
            Task outer = app.InvokeAsync (() =>
            {
                order.Add ("outer start");
                Task inner = app.InvokeAsync (() => order.Add ("inner"), TestContext.Current.CancellationToken);
                Assert.True (inner.IsCompletedSuccessfully);
                order.Add ("outer end");
            }, TestContext.Current.CancellationToken);

            Assert.True (outer.IsCompletedSuccessfully);
            Assert.Equal (["outer start", "inner", "outer end"], order);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void DisposeInsideDispatch_CompletesStartedActionAndCancelsPendingAction ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        app.Begin (runnable);

        try
        {
            bool pendingRan = false;
            Task pending = QueueOnWorker (() => app.InvokeAsync (() => pendingRan = true));
            Task running = app.InvokeAsync (() => app.Dispose (), TestContext.Current.CancellationToken);

            Assert.True (running.IsCompletedSuccessfully);
            Assert.True (pending.IsCanceled);
            Assert.False (pendingRan);
        }
        finally
        {
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void CancellationBeforeQueue_DoesNotRunAction ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        using CancellationTokenSource cancellation = new ();
        cancellation.Cancel ();

        try
        {
            bool ran = false;
            Task dispatch = app.InvokeAsync (() => ran = true, cancellation.Token);

            Assert.True (dispatch.IsCanceled);
            Assert.False (ran);
            Assert.Empty (app.TimedEvents!.Timeouts);
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void CancellationAfterQueue_RemovesCallback ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        using CancellationTokenSource cancellation = new ();

        try
        {
            bool ran = false;
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (() => ran = true, cancellation.Token));
            Assert.False (dispatch.IsCompleted);

            cancellation.Cancel ();
            PumpUiDispatches (app);

            Assert.True (dispatch.IsCanceled);
            Assert.False (ran);
            Assert.Equal (0, ((ApplicationImpl)app).QueuedDispatchCount);
            Assert.Empty (app.TimedEvents!.Timeouts);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void CancellationFromEarlierTimeout_PreventsQueuedAction ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        using CancellationTokenSource cancellation = new ();

        try
        {
            bool ran = false;
            app.AddTimeout (TimeSpan.Zero, () =>
            {
                cancellation.Cancel ();

                return false;
            });
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (() => ran = true, cancellation.Token));

            app.TimedEvents!.RunTimers ();
            PumpUiDispatches (app);

            Assert.True (dispatch.IsCanceled);
            Assert.False (ran);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void CancellationAfterActionStarts_DoesNotReplaceItsResult ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        using CancellationTokenSource cancellation = new ();

        try
        {
            Task dispatch = app.InvokeAsync (() => cancellation.Cancel (), cancellation.Token);

            Assert.True (dispatch.IsCompletedSuccessfully);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void EndingInnerSession_CancelsOnlyItsPendingDispatch ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable outer = new ();
        Runnable inner = new ();
        SessionToken outerToken = app.Begin (outer)!;
        SessionToken innerToken = app.Begin (inner)!;

        try
        {
            bool innerRan = false;
            bool outerRan = false;
            Task innerDispatch = QueueOnWorker (() => app.InvokeAsync (innerToken, () => innerRan = true));
            Task outerDispatch = QueueOnWorker (() => app.InvokeAsync (outerToken, () => outerRan = true));

            app.End (innerToken);
            PumpUiDispatches (app);

            Assert.True (innerDispatch.IsCanceled);
            Assert.False (innerRan);
            Assert.True (outerDispatch.IsCompletedSuccessfully);
            Assert.True (outerRan);
        }
        finally
        {
            app.End (outerToken);
            inner.Dispose ();
            outer.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void EndingSession_CancelsOwnedDispatchBeforeStateChangedHandlersPumpTimers ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;

        try
        {
            bool ran = false;
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (owner, () => ran = true));
            bool modalHandlerSawCancellation = false;
            bool runningHandlerSawCancellation = false;
            bool runningHandlerSawRunnable = false;

            runnable.IsModalChanged += (_, _) =>
            {
                app.TimedEvents!.RunTimers ();
                modalHandlerSawCancellation = dispatch.IsCanceled;
            };
            runnable.IsRunningChanged += (_, _) =>
            {
                app.TimedEvents!.RunTimers ();
                runningHandlerSawCancellation = dispatch.IsCanceled;
                runningHandlerSawRunnable = ReferenceEquals (owner.Runnable, runnable);
            };

            app.End (owner);

            Assert.True (modalHandlerSawCancellation);
            Assert.True (runningHandlerSawCancellation);
            Assert.True (runningHandlerSawRunnable);
            Assert.False (ran);
            Assert.Null (owner.Runnable);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void SessionBegunHandler_CanQueueOwnedDispatchBeforeRunnableIsRunning ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        Task? dispatch = null;
        bool wasRunningDuringEvent = true;
        bool ran = false;
        EventHandler<SessionTokenEventArgs> handler = (_, args) =>
        {
            wasRunningDuringEvent = args.State.Runnable!.IsRunning;
            dispatch = app.InvokeAsync (args.State, () => ran = true);
        };
        app.SessionBegun += handler;

        try
        {
            SessionToken owner = app.Begin (runnable)!;
            Assert.False (wasRunningDuringEvent);
            Assert.NotNull (dispatch);
            Assert.False (dispatch.IsCompleted);

            PumpUiDispatches (app);

            Assert.True (dispatch.IsCompletedSuccessfully);
            Assert.True (ran);
            app.End (owner);
        }
        finally
        {
            app.SessionBegun -= handler;
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    // Claude - Opus 5.5
    [Fact]
    public void NestedSessionBegunHandler_DefersOwnedDispatchUntilOwnerIsRunning ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable outer = new ();
        Runnable inner = new ();
        SessionToken outerToken = app.Begin (outer)!;
        Task? dispatch = null;
        bool? ownerRunningInCallback = null;
        EventHandler<SessionTokenEventArgs> handler = (_, args) =>
        {
            dispatch = app.InvokeAsync (args.State, () => ownerRunningInCallback = inner.IsRunning);

            // The outer session lets UI work start here, but the inner owner is not running yet.
            PumpUiDispatches (app);
        };
        app.SessionBegun += handler;

        try
        {
            SessionToken innerToken = app.Begin (inner)!;
            Assert.NotNull (dispatch);
            Assert.False (dispatch.IsCompleted);
            Assert.Null (ownerRunningInCallback);

            PumpUiDispatches (app);

            Assert.True (dispatch.IsCompletedSuccessfully);
            Assert.True (ownerRunningInCallback);
            app.End (innerToken);
        }
        finally
        {
            app.SessionBegun -= handler;
            app.End (outerToken);
            inner.Dispose ();
            outer.Dispose ();
            app.Dispose ();
        }
    }

    // Claude - Opus 5.5
    [Fact]
    public void NestedSessionBegunHandler_OwnedDispatchCannotEndOwnerBeforeItRuns ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable outer = new ();
        Runnable inner = new ();
        SessionToken outerToken = app.Begin (outer)!;
        Task? dispatch = null;
        EventHandler<SessionTokenEventArgs> handler = (_, args) => dispatch = app.InvokeAsync (args.State, () => app.End (args.State));
        app.SessionBegun += handler;

        try
        {
            SessionToken innerToken = app.Begin (inner)!;
            Assert.True (inner.IsRunning);
            Assert.Same (inner, innerToken.Runnable);

            PumpUiDispatches (app);

            Assert.True (dispatch!.IsCompletedSuccessfully);
            Assert.False (inner.IsRunning);
            Assert.Null (innerToken.Runnable);
            Assert.Same (outer, app.TopRunnable);
        }
        finally
        {
            app.SessionBegun -= handler;
            app.End (outerToken);
            inner.Dispose ();
            outer.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void EndingFinalSession_CancelsApplicationDispatch ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;

        try
        {
            bool ran = false;
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (() => ran = true));

            app.End (owner);

            Assert.True (dispatch.IsCanceled);
            Assert.False (ran);
            Assert.Empty (app.TimedEvents!.Timeouts);
            Assert.True (app.InvokeAsync (() => { }, TestContext.Current.CancellationToken).IsCanceled);
        }
        finally
        {
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public async Task FinalSessionEndOrDispose_ResumesUiAwaiters (bool dispose)
    {
        for (int round = 0; round < 5; round++)
        {
            IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
            Runnable runnable = new ();
            SessionToken owner = app.Begin (runnable)!;
            using CountdownEvent queued = new (40);
            Task [] handlers = new Task [40];

            async Task Handler ()
            {
                try
                {
                    await Task.Run (async () =>
                    {
                        Task dispatch = app.InvokeAsync (owner, () => { });
                        queued.Signal ();
                        await dispatch.ConfigureAwait (false);
                    }, TestContext.Current.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // The session ended before the queued UI action started.
                }
            }

            try
            {
                for (int i = 0; i < handlers.Length; i++)
                {
                    handlers [i] = Handler ();
                }

                Assert.True (queued.Wait (TimeSpan.FromSeconds (10), TestContext.Current.CancellationToken));

                if (dispose)
                {
                    app.Dispose ();
                }
                else
                {
                    app.End (owner);
                }

                await Task.WhenAll (handlers).WaitAsync (TimeSpan.FromSeconds (5), TestContext.Current.CancellationToken);
                Assert.All (handlers, handler => Assert.True (handler.IsCompletedSuccessfully));
            }
            finally
            {
                // With the loop stopped, the await may resume on a worker. End application association before
                // disposing its views; the enabled diagnostics variant deliberately enforces that distinction.
                app.Dispose ();
                runnable.Dispose ();
            }
        }
    }

    // Claude - Opus 5.5
    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public async Task CallerCancellationBeforeFinalSessionEndOrDispose_ResumesUiAwaiter (bool dispose)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        using CancellationTokenSource cts = new ();
        Task dispatch;
        Task awaiter;

        try
        {
            SynchronizationContext.SetSynchronizationContext (((ApplicationImpl)app).SynchronizationContext);
            dispatch = QueueOnWorker (() => app.InvokeAsync (() => { }, cts.Token));
            awaiter = AwaitOnUiContext (dispatch);

            // The canceled task posts its UI-context continuation to the loop that is about to stop.
            RunOnWorker (cts.Cancel);

            if (dispose)
            {
                app.Dispose ();
            }
            else
            {
                app.End (owner);
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (previous);
        }

        try
        {
            await awaiter.WaitAsync (TimeSpan.FromSeconds (5), TestContext.Current.CancellationToken);
            Assert.True (dispatch.IsCanceled);
        }
        finally
        {
            app.Dispose ();
            runnable.Dispose ();
        }

        static async Task AwaitOnUiContext (Task dispatch)
        {
            try
            {
                await dispatch;
            }
            catch (OperationCanceledException)
            {
                // Expected: the caller canceled before the UI action started.
            }
        }
    }

    // Claude - Opus 5.5
    [Fact]
    public void WorkerSendBeforeFinalSessionEnd_DoesNotBlockForever ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        SynchronizationContext context = ((ApplicationImpl)app).SynchronizationContext!;

        try
        {
            int runs = 0;
            Thread sender = new (() => context.Send (_ => Interlocked.Increment (ref runs), null)) { IsBackground = true };
            sender.Start ();
            Assert.True (SpinWait.SpinUntil (() => ((ApplicationImpl)app).QueuedPostCount > 0, TimeSpan.FromSeconds (5)));

            app.End (owner);

            Assert.True (sender.Join (TimeSpan.FromSeconds (5)));
            Assert.Equal (1, runs);
            Assert.Equal (0, ((ApplicationImpl)app).QueuedPostCount);
        }
        finally
        {
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    // Copilot - GPT-5.4 mini
    [Fact]
    public void Send_RacingWithEnd_ExecutesAtMostOnce_AndCompletesAfterShutdown ()
    {
        for (int iteration = 0; iteration < 25; iteration++)
        {
            IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
            Runnable runnable = new ();
            SessionToken owner = app.Begin (runnable)!;
            SynchronizationContext context = ((ApplicationImpl)app).SynchronizationContext!;
            ManualResetEventSlim senderStarted = new (false);
            int runs = 0;
            int? callbackThreadId = null;
            Exception? senderError = null;
            Exception? endError = null;

            try
            {
                Thread sender = new (() =>
                                     {
                                         try
                                         {
                                             senderStarted.Set ();
                                             context.Send (_ =>
                                                           {
                                                               callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                                                               Interlocked.Increment (ref runs);
                                                           },
                                                           null);
                                         }
                                         catch (Exception ex)
                                         {
                                             senderError = ex;
                                         }
                                     })
                                { IsBackground = true };
                Thread ender = new (() =>
                                    {
                                        try
                                        {
                                            Assert.True (senderStarted.Wait (TimeSpan.FromSeconds (5)));
                                            Assert.True (SpinWait.SpinUntil (() => ((ApplicationImpl)app).QueuedPostCount > 0, TimeSpan.FromSeconds (5)));
                                            app.End (owner);
                                        }
                                        catch (Exception ex)
                                        {
                                            endError = ex;
                                        }
                                    })
                               { IsBackground = true };

                sender.Start ();
                ender.Start ();

                Assert.True (sender.Join (TimeSpan.FromSeconds (10)), $"Sender did not complete for iteration {iteration}.");
                Assert.True (ender.Join (TimeSpan.FromSeconds (10)), $"End did not complete for iteration {iteration}.");
                Assert.Null (senderError);
                Assert.Null (endError);
                Assert.Equal (1, runs);
                Assert.NotNull (callbackThreadId);
                Assert.Equal (0, ((ApplicationImpl)app).QueuedPostCount);
            }
            finally
            {
                runnable.Dispose ();
                app.Dispose ();
            }
        }
    }

    // Copilot - GPT-5.3-Codex
    [Fact]
    public void Send_PropagatesCallbackException ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        SynchronizationContext context = ((ApplicationImpl)app).SynchronizationContext!;
        InvalidOperationException failure = new ("callback failed");

        try
        {
            Exception? thrown = null;
            Thread sender = new (() =>
                                 {
                                     try
                                     {
                                         context.Send (_ => throw failure, null);
                                     }
                                     catch (Exception ex)
                                     {
                                         thrown = ex;
                                     }
                                 })
                            { IsBackground = true };

            sender.Start ();
            Assert.True (SpinWait.SpinUntil (() => ((ApplicationImpl)app).QueuedPostCount > 0, TimeSpan.FromSeconds (5)));

            PumpUiDispatches (app);

            Assert.True (sender.Join (TimeSpan.FromSeconds (5)));
            Assert.Same (failure, thrown);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    // Copilot - GPT-5.3-Codex
    [Fact]
    public void Send_DoesNotDeadlock_WhenRunLoopStops ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        SynchronizationContext context = ((ApplicationImpl)app).SynchronizationContext!;

        try
        {
            int runs = 0;
            Exception? sendError = null;
            Exception? endError = null;
            Thread sender = new (() =>
                                 {
                                     try
                                     {
                                         context.Send (_ => Interlocked.Increment (ref runs), null);
                                     }
                                     catch (Exception ex)
                                     {
                                         sendError = ex;
                                     }
                                 })
                            { IsBackground = true };
            Thread ender = new (() =>
                                {
                                    try
                                    {
                                        app.End (owner);
                                    }
                                    catch (Exception ex)
                                    {
                                        endError = ex;
                                    }
                                })
                           { IsBackground = true };

            sender.Start ();
            Assert.True (SpinWait.SpinUntil (() => ((ApplicationImpl)app).QueuedPostCount > 0, TimeSpan.FromSeconds (5)));

            ender.Start ();

            Assert.True (sender.Join (TimeSpan.FromSeconds (10)));
            Assert.True (ender.Join (TimeSpan.FromSeconds (10)));
            Assert.Null (sendError);
            Assert.Null (endError);
            Assert.Equal (1, runs);
        }
        finally
        {
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    // Claude - Opus 5.5
    [Fact]
    public void FinalSessionStoppingDuringDrain_ReleasesUnstartedPostToThreadPool ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        using ManualResetEventSlim endNow = new ();
        using ManualResetEventSlim stopPublished = new ();
        using ManualResetEventSlim finishStopping = new ();
        using ManualResetEventSlim secondRan = new ();
        SessionToken session = CreateSessionThatPausesWhenStopping (stopPublished, finishStopping);
        app.SessionStack!.Push (session);
        SynchronizationContext context = new MainLoopSyncContext (app);
        Thread ender = EndOnWorkerWhenSignaled (app, session, endNow);
        int? secondThreadId = null;

        try
        {
            // The first post ends the final session from another thread, so the second has not started when it stops.
            RunOnWorker (() =>
                         {
                             context.Post (_ =>
                                           {
                                               endNow.Set ();
                                               stopPublished.Wait (TimeSpan.FromSeconds (10));
                                           },
                                           null);

                             context.Post (_ =>
                                           {
                                               secondThreadId = Thread.CurrentThread.ManagedThreadId;
                                               secondRan.Set ();
                                           },
                                           null);
                         });

            PumpUiDispatches (app);

            Assert.True (stopPublished.IsSet);
            Assert.Null (secondThreadId);

            finishStopping.Set ();

            Assert.True (ender.Join (TimeSpan.FromSeconds (10)));
            Assert.True (secondRan.Wait (TimeSpan.FromSeconds (10), TestContext.Current.CancellationToken));
            Assert.NotEqual (app.MainThreadId, secondThreadId);
        }
        finally
        {
            finishStopping.Set ();
            ender.Join ();
            app.SessionStack.Clear ();
            app.Dispose ();
        }
    }

    // Claude - Opus 5.5
    [Fact]
    public void NestedSessionStoppingDuringDrain_DoesNotStartItsOwnedDispatch ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        using ManualResetEventSlim endNow = new ();
        using ManualResetEventSlim stopPublished = new ();
        using ManualResetEventSlim finishStopping = new ();
        Mock<IRunnable> outer = new ();
        outer.SetupGet (instance => instance.IsRunning).Returns (true);
        SessionToken inner = CreateSessionThatPausesWhenStopping (stopPublished, finishStopping);
        app.SessionStack!.Push (new SessionToken (outer.Object));
        app.SessionStack.Push (inner);
        Thread ender = EndOnWorkerWhenSignaled (app, inner, endNow);
        var ownedRan = false;

        try
        {
            // The first dispatch ends the inner session from another thread before the inner-owned dispatch starts.
            Task first = QueueOnWorker (() => app.InvokeAsync (() =>
                                                               {
                                                                   endNow.Set ();
                                                                   stopPublished.Wait (TimeSpan.FromSeconds (10));
                                                               }));
            Task owned = QueueOnWorker (() => app.InvokeAsync (inner, () => ownedRan = true));

            PumpUiDispatches (app);

            Assert.True (first.IsCompletedSuccessfully);
            Assert.True (stopPublished.IsSet);
            Assert.False (ownedRan);

            // The closed owner's work stays queued until its End releases it.
            Assert.False (owned.IsCompleted);
            finishStopping.Set ();
            Assert.True (ender.Join (TimeSpan.FromSeconds (10)));
            Assert.False (ownedRan);
            Assert.True (owned.IsCanceled);
        }
        finally
        {
            finishStopping.Set ();
            ender.Join ();
            app.SessionStack.Clear ();
            app.Dispose ();
        }
    }

    [Fact]
    public void OuterOwnedDispatch_RemainsValidWithNoCachedTopRunnable ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable outer = new ();
        Runnable inner = new ();
        SessionToken outerToken = app.Begin (outer)!;
        SessionToken innerToken = app.Begin (inner)!;
        app.End (innerToken);
        PropertyInfo topProperty = typeof (ApplicationImpl).GetProperty (nameof (ApplicationImpl.TopRunnable))!;
        IRunnable? savedTop = app.TopRunnable;

        try
        {
            topProperty.SetValue (app, null);
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (outerToken, () => { }));
            topProperty.SetValue (app, savedTop);
            PumpUiDispatches (app);

            Assert.True (dispatch.IsCompletedSuccessfully);
        }
        finally
        {
            topProperty.SetValue (app, savedTop);
            app.End (outerToken);
            inner.Dispose ();
            outer.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void StoppedCachedTop_DoesNotMoveOuterContinuationOffUiThread ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable first = new ();
        app.End (app.Begin (first)!);
        first.Dispose ();

        Runnable outer = new ();
        Runnable inner = new ();
        SessionToken outerToken = app.Begin (outer)!;
        SessionToken innerToken = app.Begin (inner)!;
        app.End (innerToken);
        ApplicationImpl impl = (ApplicationImpl)app;
        PropertyInfo topProperty = typeof (ApplicationImpl).GetProperty (nameof (ApplicationImpl.TopRunnable))!;
        IRunnable? savedTop = app.TopRunnable;

        try
        {
            // Model the moment End has stopped the inner runnable but the cached top still points to it.
            topProperty.SetValue (app, inner);
            Assert.True (impl.HasRunningSession);
            Assert.True (impl.CanPumpPostedWork);

            int? callbackThreadId = null;
            Thread worker = new (() => impl.SynchronizationContext!.Post (
                                                                       _ => callbackThreadId = Thread.CurrentThread.ManagedThreadId,
                                                                       null));
            worker.Start ();
            worker.Join ();

            Assert.Null (callbackThreadId);
            PumpUiDispatches (app);

            Assert.Equal (app.MainThreadId, callbackThreadId);
        }
        finally
        {
            topProperty.SetValue (app, savedTop);
            app.End (outerToken);
            inner.Dispose ();
            outer.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void EndingNonTopSession_PreservesInnerOwnedDispatch ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable outer = new ();
        Runnable inner = new ();
        SessionToken outerToken = app.Begin (outer)!;
        SessionToken innerToken = app.Begin (inner)!;

        try
        {
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (innerToken, () => { }));
            app.End (outerToken);
            PumpUiDispatches (app);

            Assert.True (dispatch.IsCompletedSuccessfully);
            Assert.True (inner.IsRunning);
        }
        finally
        {
            app.End (innerToken);
            inner.Dispose ();
            outer.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void EndingNonTopSession_StateChangedHandlerCannotStartOwnedDispatch ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable outer = new ();
        Runnable inner = new ();
        SessionToken outerToken = app.Begin (outer)!;
        SessionToken innerToken = app.Begin (inner)!;
        Task? dispatch = null;
        bool ran = false;
        outer.IsRunningChanged += (_, _) => dispatch = app.InvokeAsync (outerToken, () => ran = true);

        try
        {
            app.End (outerToken);

            Assert.NotNull (dispatch);
            Assert.True (dispatch.IsCanceled);
            Assert.False (ran);
            Assert.True (inner.IsRunning);
        }
        finally
        {
            app.End (innerToken);
            inner.Dispose ();
            outer.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void ConcurrentSessionEnd_DoesNotRunUnstartedOwnedDispatches ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        const int count = 2_000;
        Task? [] dispatches = new Task? [count];
        using ManualResetEventSlim firstQueued = new ();
        int ran = 0;
        Exception? producerFailure = null;

        try
        {
            Thread producer = new (() =>
            {
                try
                {
                    for (int i = 0; i < dispatches.Length; i++)
                    {
                        dispatches [i] = app.InvokeAsync (owner, () => Interlocked.Increment (ref ran));

                        if (i == 0)
                        {
                            firstQueued.Set ();
                        }
                    }
                }
                catch (Exception ex)
                {
                    producerFailure = ex;
                    firstQueued.Set ();
                }
            });
            producer.Start ();
            Assert.True (firstQueued.Wait (TimeSpan.FromSeconds (10), TestContext.Current.CancellationToken));
            app.End (owner);
            producer.Join ();
            Assert.Null (producerFailure);

            Assert.All (dispatches, dispatch => Assert.True (dispatch?.IsCanceled));
            Assert.Equal (0, ran);
            Assert.Equal (0, ((ApplicationImpl)app).QueuedDispatchCount);
            Assert.Empty (app.TimedEvents!.Timeouts);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void EndingSession_CancelsPendingDispatchesWithoutTouchingOtherTimeouts ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;

        try
        {
            object unrelated = app.AddTimeout (TimeSpan.FromHours (1), () => false)!;
            Task [] dispatches = new Task [500];
            Thread worker = new (() =>
            {
                for (int i = 0; i < dispatches.Length; i++)
                {
                    dispatches [i] = app.InvokeAsync (owner, () => { });
                }
            });
            worker.Start ();
            worker.Join ();

            app.End (owner);

            Assert.All (dispatches, dispatch => Assert.True (dispatch.IsCanceled));
            Assert.Equal (0, ((ApplicationImpl)app).QueuedDispatchCount);
            Assert.Single (app.TimedEvents!.Timeouts);
            Assert.True (app.TimedEvents.Remove (unrelated));
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void ConcurrentCancellationDuringQueueing_LeavesNoQueuedDispatches ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        const int count = 5_000;
        Task [] dispatches = new Task [count];
        CancellationTokenSource [] sources = new CancellationTokenSource [count];
        int published = -1;

        try
        {
            for (int i = 0; i < count; i++)
            {
                sources [i] = new CancellationTokenSource ();
            }

            Thread canceller = new (() =>
            {
                for (int i = 0; i < count; i++)
                {
                    if (!SpinWait.SpinUntil (() => Volatile.Read (ref published) >= i, TimeSpan.FromSeconds (10)))
                    {
                        return;
                    }
                    sources [i].Cancel ();
                }
            });
            canceller.Start ();

            Thread producer = new (() =>
            {
                for (int i = 0; i < count; i++)
                {
                    Volatile.Write (ref published, i);
                    dispatches [i] = app.InvokeAsync (owner, () => { }, sources [i].Token);
                }
            });
            producer.Start ();
            producer.Join ();
            canceller.Join ();

            Assert.All (dispatches, dispatch => Assert.True (dispatch.IsCanceled));
            Assert.Equal (0, ((ApplicationImpl)app).QueuedDispatchCount);
            Assert.Empty (app.TimedEvents!.Timeouts);
        }
        finally
        {
            foreach (CancellationTokenSource source in sources)
            {
                source?.Dispose ();
            }

            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void CanceledSessionEnd_DoesNotCancelOwnedDispatch ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;

        void VetoStop (object? _, CancelEventArgs<bool> args)
        {
            if (!args.NewValue)
            {
                args.Cancel = true;
            }
        }

        runnable.IsRunningChanging += VetoStop;

        try
        {
            bool ran = false;
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (owner, () => ran = true));

            app.End (owner);
            PumpUiDispatches (app);

            Assert.True (runnable.IsRunning);
            Assert.True (dispatch.IsCompletedSuccessfully);
            Assert.True (ran);
        }
        finally
        {
            runnable.IsRunningChanging -= VetoStop;
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void NewSessionAfterEarlierEnd_AllowsDispatchAgain ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable first = new ();
        Runnable second = new ();
        SessionToken firstToken = app.Begin (first)!;
        app.End (firstToken);

        try
        {
            SessionToken secondToken = app.Begin (second)!;
            bool ran = false;
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (secondToken, () => ran = true));

            PumpUiDispatches (app);

            Assert.True (dispatch.IsCompletedSuccessfully);
            Assert.True (ran);
            app.End (secondToken);
        }
        finally
        {
            second.Dispose ();
            first.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void DisposeBeforeFirstSession_CancelsQueuedDispatch ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        bool ran = false;
        Task dispatch = QueueOnWorker (() => app.InvokeAsync (() => ran = true));

        app.Dispose ();

        Assert.True (dispatch.IsCanceled);
        Assert.False (ran);
    }

    [Fact]
    public void QueueBeforeFirstSession_RunsAfterItStarts ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();

        try
        {
            bool ran = false;
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (() => ran = true));
            Assert.False (dispatch.IsCompleted);
            PumpUiDispatches (app);
            Assert.False (dispatch.IsCompleted);

            SessionToken owner = app.Begin (runnable)!;
            PumpUiDispatches (app);

            Assert.True (dispatch.IsCompletedSuccessfully);
            Assert.True (ran);
            app.End (owner);
        }
        finally
        {
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void UiThreadCallbackException_FaultsReturnedTask ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;

        try
        {
            InvalidOperationException failure = new ("callback failed");
            Task dispatch = app.InvokeAsync (() => throw failure, TestContext.Current.CancellationToken);

            Assert.Same (failure, Assert.Throws<InvalidOperationException> (() => dispatch.GetAwaiter ().GetResult ()));
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void CallbackException_FaultsTaskWithoutEscapingDispatchDrain ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;

        try
        {
            InvalidOperationException failure = new ("callback failed");
            Task dispatch = QueueOnWorker (() => app.InvokeAsync (() => throw failure));

            PumpUiDispatches (app);

            Assert.Same (failure, Assert.Throws<InvalidOperationException> (() => dispatch.GetAwaiter ().GetResult ()));
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void CallbackCancellationWithItsCallerToken_CancelsTask ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        using CancellationTokenSource cancellation = new ();

        try
        {
            Task dispatch = app.InvokeAsync (() =>
            {
                cancellation.Cancel ();
                throw new OperationCanceledException (cancellation.Token);
            }, cancellation.Token);

            Assert.True (dispatch.IsCanceled);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void CallbackCancellationWithoutMatchingCanceledToken_FaultsTask ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        using CancellationTokenSource cancellation = new ();
        using CancellationTokenSource other = new ();
        other.Cancel ();

        try
        {
            Task dispatch = app.InvokeAsync (() => throw new OperationCanceledException (other.Token), cancellation.Token);

            Assert.True (dispatch.IsFaulted);
            Assert.IsType<OperationCanceledException> (dispatch.Exception!.InnerException);
        }
        finally
        {
            app.End (owner);
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void OwnerFromAnotherApplication_ReturnsCanceledTask ()
    {
        IApplication first = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        IApplication second = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = first.Begin (runnable)!;

        try
        {
            Assert.True (second.InvokeAsync (owner, () => { }, TestContext.Current.CancellationToken).IsCanceled);
        }
        finally
        {
            first.End (owner);
            runnable.Dispose ();
            second.Dispose ();
            first.Dispose ();
        }
    }

    [Fact]
    public void BeforeInitAndAfterDispose_ThrowNotInitialized ()
    {
        IApplication app = CreateApplication ();

        Assert.Throws<NotInitializedException> (() => { _ = app.InvokeAsync (() => { }, TestContext.Current.CancellationToken); });

        app.Init (DriverRegistry.Names.ANSI);
        app.Dispose ();

        Assert.Throws<NotInitializedException> (() => { _ = app.InvokeAsync (() => { }, TestContext.Current.CancellationToken); });
    }

    [Fact]
    public void ShutdownTakesPrecedenceOverAlreadyCanceledToken ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        using CancellationTokenSource cancellation = new ();
        cancellation.Cancel ();
        FieldInfo stoppingField = typeof (ApplicationImpl).GetField ("_dispatchStopping", BindingFlags.Instance | BindingFlags.NonPublic)!;

        try
        {
            stoppingField.SetValue (app, true);

            Assert.Throws<NotInitializedException> (() => { _ = app.InvokeAsync (() => { }, cancellation.Token); });
        }
        finally
        {
            stoppingField.SetValue (app, false);
            app.Dispose ();
        }
    }

    [Fact]
    public void CustomApplicationCanProvideAwaitableDispatch ()
    {
        Mock<IApplication> app = new ();
        Mock<IApplicationAsyncDispatcher> dispatcher = app.As<IApplicationAsyncDispatcher> ();
        dispatcher.Setup (instance => instance.InvokeAsync (It.IsAny<Action<IApplication>> (), null, It.IsAny<CancellationToken> ()))
                  .Returns (Task.CompletedTask);

        Task dispatch = app.Object.InvokeAsync (() => { }, TestContext.Current.CancellationToken);

        Assert.True (dispatch.IsCompletedSuccessfully);
        dispatcher.Verify (instance => instance.InvokeAsync (It.IsAny<Action<IApplication>> (), null, TestContext.Current.CancellationToken), Times.Once);
    }

    private static void PumpUiDispatches (IApplication app) => ((ApplicationImpl)app).DrainDispatches ();

    // Reports the session as stopped, then pauses End before it releases queued work, until finishStopping is set.
    private static SessionToken CreateSessionThatPausesWhenStopping (ManualResetEventSlim stopPublished, ManualResetEventSlim finishStopping)
    {
        Mock<IRunnable> runnable = new ();
        runnable.SetupGet (instance => instance.IsRunning).Returns (() => !stopPublished.IsSet);

        runnable.Setup (instance => instance.SetIsRunning (false))
                .Callback (() =>
                           {
                               stopPublished.Set ();
                               finishStopping.Wait (TimeSpan.FromSeconds (10));
                           });

        return new (runnable.Object);
    }

    private static Thread EndOnWorkerWhenSignaled (IApplication app, SessionToken session, ManualResetEventSlim endNow)
    {
        Thread ender = new (() =>
                            {
                                if (!endNow.Wait (TimeSpan.FromSeconds (10)))
                                {
                                    return;
                                }

                                app.End (session);
                            });
        ender.Start ();

        return ender;
    }

    private static void RunOnWorker (Action action)
    {
        Thread worker = new (() => action ());
        worker.Start ();
        worker.Join ();
    }

    private static Task QueueOnWorker (Func<Task> dispatch)
    {
        Task? queued = null;
        Exception? failure = null;
        Thread worker = new (() =>
        {
            try
            {
                queued = dispatch ();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        worker.Start ();
        worker.Join ();

        if (failure is { })
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture (failure).Throw ();
        }

        return queued!;
    }

    // Regression: a Post that arrives only after the final session has ended must not
    // sit on the toolkit queue forever; it falls back to the thread pool (#5636 / #5680).
    [Fact]
    public void Post_AfterFinalSessionEnds_RunsOnThreadPool ()
    {
        IApplication app = CreateApplication ().Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        SynchronizationContext context = ((ApplicationImpl)app).SynchronizationContext!;

        app.End (owner);

        try
        {
            using ManualResetEventSlim ran = new (false);
            int? callbackThreadId = null;

            // No running session and no pump: Post must not require DrainDispatches / RunIteration.
            context.Post (
                          _ =>
                          {
                              callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                              ran.Set ();
                          },
                          null);

            Assert.True (ran.Wait (TimeSpan.FromSeconds (5), TestContext.Current.CancellationToken));
            Assert.NotNull (callbackThreadId);
            Assert.NotEqual (app.MainThreadId, callbackThreadId);
            Assert.Equal (0, ((ApplicationImpl)app).QueuedPostCount);
        }
        finally
        {
            runnable.Dispose ();
            app.Dispose ();
        }
    }

    // CoPilot - GPT-6.1 Sol
    protected virtual IApplication CreateApplication () => Application.Create ();

}

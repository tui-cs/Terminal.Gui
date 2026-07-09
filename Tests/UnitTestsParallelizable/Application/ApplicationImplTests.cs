using System.Collections.Concurrent;
using Moq;

namespace ApplicationTests;

[Collection ("Application Tests")]
public class ApplicationImplTests
{
    [Fact]
    public void Internal_Properties_Correct ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Assert.True (app.Initialized);
        Assert.Null (app.TopRunnableView);
        SessionToken? rs = app.Begin (new Runnable<bool> ());
        Assert.Equal (app.TopRunnable, rs!.Runnable);
        Assert.False (app.Mouse.IsGrabbed (rs!.Runnable as View)); // public

        app.Dispose ();
    }

    #region DisposeTests

    [Fact]
    public async Task Dispose_Allows_Async ()
    {
        var isCompletedSuccessfully = false;

        async Task TaskWithAsyncContinuation ()
        {
            await Task.Yield ();
            await Task.Yield ();

            isCompletedSuccessfully = true;
        }

        IApplication app = Application.Create ();
        app.Dispose ();

        Assert.False (isCompletedSuccessfully);
        await TaskWithAsyncContinuation ();
        Thread.Sleep (100);
        Assert.True (isCompletedSuccessfully);
    }

    [Fact]
    public void Dispose_Resets_SyncContext ()
    {
        IApplication app = Application.Create ();
        app.Dispose ();
        Assert.Null (SynchronizationContext.Current);
    }

    [Fact]
    public void Init_Posts_To_The_App_Main_Thread ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        int mainThreadId = app.MainThreadId ?? Thread.CurrentThread.ManagedThreadId;
        int? callbackThreadId = null;
        ManualResetEventSlim callbackCalled = new (false);

        SynchronizationContext.Current!.Post (_ =>
                                              {
                                                  callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                                                  callbackCalled.Set ();
                                              },
                                              null);

        app.AddTimeout (TimeSpan.FromMilliseconds (100),
                        () =>
                        {
                            app.RequestStop ();

                            return false;
                        });

        app.Run (new Window ());

        Assert.True (callbackCalled.Wait (TimeSpan.FromMilliseconds (200), TestContext.Current.CancellationToken));
        Assert.Equal (mainThreadId, callbackThreadId);

        app.Dispose ();
    }

    [Fact]
    public void Init_Posts_To_The_App_Instance_Main_Thread ()
    {
        IApplication app1 = Application.Create ();
        app1.Init (DriverRegistry.Names.ANSI);

        IApplication app2 = Application.Create ();
        app2.Init (DriverRegistry.Names.ANSI);

        int mainThreadId1 = app1.MainThreadId ?? Thread.CurrentThread.ManagedThreadId;
        int? callbackThreadId1 = null;
        SynchronizationContext? synchronizationContext1 = null;

        ManualResetEventSlim callbackCalled = new (false);

        app1.AddTimeout (TimeSpan.FromMilliseconds (100),
                        () =>
                        {
                            if (synchronizationContext1 is null)
                            {
                                synchronizationContext1 = SynchronizationContext.Current;
                                synchronizationContext1?.Post (_ =>
                                                                      {
                                                                          callbackThreadId1 = Thread.CurrentThread.ManagedThreadId;
                                                                          callbackCalled.Set ();
                                                                      },
                                                                      null);

                                return true;
                            }
                            else
                            {
                                app1.RequestStop ();

                                return false;
                            }
                        });

        app1.Run (new Window ());

        Assert.True (callbackCalled.Wait (TimeSpan.FromMilliseconds (200), TestContext.Current.CancellationToken));
        Assert.Equal (mainThreadId1, callbackThreadId1);
        Assert.Equal (synchronizationContext1, SynchronizationContext.Current);

        callbackCalled.Reset ();

        int mainThreadId2 = app2.MainThreadId ?? Thread.CurrentThread.ManagedThreadId;
        int? callbackThreadId2 = null;
        SynchronizationContext? synchronizationContext2 = null;

        app2.AddTimeout (TimeSpan.FromMilliseconds (100),
                         () =>
                         {
                             if (synchronizationContext2 is null)
                             {
                                 synchronizationContext2 = SynchronizationContext.Current;
                                 synchronizationContext2?.Post (_ =>
                                                                      {
                                                                          callbackThreadId2 = Thread.CurrentThread.ManagedThreadId;
                                                                          callbackCalled.Set ();
                                                                      },
                                                                      null);

                                 return true;
                             }
                             else
                             {
                                 app2.RequestStop ();

                                 return false;
                             }
                         });

        app2.Run (new Window ());

        Assert.True (callbackCalled.Wait (TimeSpan.FromMilliseconds (200), TestContext.Current.CancellationToken));
        Assert.Equal (mainThreadId2, callbackThreadId2);
        Assert.NotEqual (synchronizationContext1, SynchronizationContext.Current);
        Assert.Equal (synchronizationContext2, SynchronizationContext.Current);
        Assert.NotEqual (synchronizationContext1, synchronizationContext2);

        app1.Dispose ();
        app2.Dispose ();
    }

    [Fact]
    public void Dispose_Alone_Does_Nothing ()
    {
        IApplication app = Application.Create ();
        app.Dispose ();
    }

    #endregion

    /// <summary>
    ///     Creates a new ApplicationImpl instance for testing. The input and size monitor components are mocked.
    ///     Output is AnsiOutput.
    /// </summary>
    private IApplication NewMockedApplicationImpl ()
    {
        Mock<INetInput> netInput = new ();
        SetupRunInputMockMethodToBlock (netInput);

        Mock<IComponentFactory<ConsoleKeyInfo>> m = new ();
        m.Setup (f => f.CreateInput ()).Returns (netInput.Object);
        Mock<IInputProcessor> inputProcessor = new ();
        inputProcessor.Setup (p => p.GetParser ()).Returns (Mock.Of<IAnsiResponseParser> ());
        m.Setup (f => f.CreateInputProcessor (It.IsAny<ConcurrentQueue<ConsoleKeyInfo>> (), It.IsAny<ITimeProvider?> ())).Returns (inputProcessor.Object);

        AnsiOutput consoleOutput = new ();
        consoleOutput.SetSize (80, 25);

        m.Setup (f => f.CreateOutput ()).Returns (consoleOutput);
        m.Setup (f => f.CreateSizeMonitor (It.IsAny<IOutput> (), It.IsAny<IOutputBuffer> ())).Returns (Mock.Of<ISizeMonitor> ());

        return new ApplicationImpl (m.Object);
    }

    private void SetupRunInputMockMethodToBlock (Mock<INetInput> netInput) =>
        netInput.Setup (r => r.Run (It.IsAny<CancellationToken> ()))
                .Callback<CancellationToken> (token =>
                                              {
                                                  // Simulate an infinite loop that checks for cancellation
                                                  while (!token.IsCancellationRequested)
                                                  {
                                                      // Perform the action that should repeat in the loop
                                                      // This could be some mock behavior or just an empty loop depending on the context
                                                  }
                                              })
                .Verifiable (Times.Once);

    [Fact]
    public void NoInitThrowOnRun ()
    {
        IApplication app = NewMockedApplicationImpl ();
        var ex = Assert.Throws<NotInitializedException> (() => app.Run (new Window ()));
        app.Dispose ();
    }

    [Fact]
    public void InitRunShutdown_Top_Set_To_Null_After_Shutdown ()
    {
        IApplication app = NewMockedApplicationImpl ();

        app.Init (DriverRegistry.Names.ANSI);

        object? timeoutToken = app.AddTimeout (TimeSpan.FromMilliseconds (150),
                                               () =>
                                               {
                                                   if (app.TopRunnableView is { })
                                                   {
                                                       app.RequestStop ();

                                                       return false;
                                                   }

                                                   return false;
                                               });
        Assert.Null (app.TopRunnableView);

        // Blocks until the timeout call is hit

        app.Run (new Window ());

        // We returned false above, so we should not have to remove the timeout
        Assert.False (app.RemoveTimeout (timeoutToken!));

        Assert.Null (app.TopRunnableView);
        app.Dispose ();
        Assert.Null (app.TopRunnableView);
    }

    [Fact]
    public void InitRunShutdown_Running_Set_To_False ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init (DriverRegistry.Names.ANSI);

        IRunnable top = new Window { Title = "InitRunShutdown_Running_Set_To_False" };

        object? timeoutToken = app.AddTimeout (TimeSpan.FromMilliseconds (150),
                                               () =>
                                               {
                                                   Assert.True (top!.IsRunning);

                                                   if (app.TopRunnableView != null)
                                                   {
                                                       app.RequestStop ();

                                                       return false;
                                                   }

                                                   return false;
                                               });

        Assert.False (top.IsRunning);

        // Blocks until the timeout call is hit
        app.Run (top);

        // We returned false above, so we should not have to remove the timeout
        Assert.False (app.RemoveTimeout (timeoutToken!));

        Assert.False (top.IsRunning);

        // BUGBUG: Shutdown sets Top to null, not End.
        //Assert.Null (Application.TopRunnable);
        app.TopRunnableView?.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void InitRunShutdown_StopAfterFirstIteration_Stops ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        Assert.Null (app.TopRunnableView);
        Assert.Null (app.Driver);

        app.Init (DriverRegistry.Names.ANSI);

        IRunnable top = new Window ();
        var isIsModalChanged = 0;

        top.IsModalChanged += (_, a) => { isIsModalChanged++; };

        var isRunningChangedCount = 0;

        top.IsRunningChanged += (_, a) => { isRunningChangedCount++; };

        object? timeoutToken = app.AddTimeout (TimeSpan.FromMilliseconds (150),
                                               () =>
                                               {
                                                   //Assert.Fail (@"Didn't stop after first iteration.");

                                                   return false;
                                               });

        Assert.Equal (0, isIsModalChanged);
        Assert.Equal (0, isRunningChangedCount);

        app.StopAfterFirstIteration = true;
        app.Run (top);

        Assert.Equal (2, isIsModalChanged);
        Assert.Equal (2, isRunningChangedCount);

        app.TopRunnableView?.Dispose ();
        app.Dispose ();
        Assert.Equal (2, isIsModalChanged);
        Assert.Equal (2, isRunningChangedCount);
    }

    [Fact]
    public void InitRunShutdown_End_Is_Called ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        Assert.Null (app.TopRunnableView);
        Assert.Null (app.Driver);

        app.Init (DriverRegistry.Names.ANSI);

        IRunnable top = new Window ();

        var isIsModalChanged = 0;

        top.IsModalChanged += (_, a) => { isIsModalChanged++; };

        var isRunningChangedCount = 0;

        top.IsRunningChanged += (_, a) => { isRunningChangedCount++; };

        object? timeoutToken = app.AddTimeout (TimeSpan.FromMilliseconds (150),
                                               () =>
                                               {
                                                   Assert.True (top!.IsRunning);

                                                   if (app.TopRunnableView != null)
                                                   {
                                                       app.RequestStop ();

                                                       return false;
                                                   }

                                                   return false;
                                               });

        Assert.Equal (0, isIsModalChanged);
        Assert.Equal (0, isRunningChangedCount);

        // Blocks until the timeout call is hit
        app.Run (top);

        Assert.Equal (2, isIsModalChanged);
        Assert.Equal (2, isRunningChangedCount);

        // We returned false above, so we should not have to remove the timeout
        Assert.False (app.RemoveTimeout (timeoutToken!));

        app.TopRunnableView?.Dispose ();
        app.Dispose ();
        Assert.Equal (2, isIsModalChanged);
        Assert.Equal (2, isRunningChangedCount);
    }

    [Fact]
    public void InitRunShutdown_QuitKey_Quits ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init (DriverRegistry.Names.ANSI);

        IRunnable top = new Window { Title = "InitRunShutdown_QuitKey_Quits" };

        object? timeoutToken = app.AddTimeout (TimeSpan.FromMilliseconds (150),
                                               () =>
                                               {
                                                   Assert.True (top!.IsRunning);

                                                   if (app.TopRunnableView != null)
                                                   {
                                                       app.Keyboard.RaiseKeyDownEvent (Application.GetDefaultKey (Command.Quit));
                                                   }

                                                   return false;
                                               });

        Assert.False (top!.IsRunning);

        // Blocks until the timeout call is hit
        app.Run (top);

        // We returned false above, so we should not have to remove the timeout
        Assert.False (app.RemoveTimeout (timeoutToken!));

        Assert.False (top!.IsRunning);

        Assert.Null (app.TopRunnableView);
        (top as Window)!.Dispose ();
        app.Dispose ();
        Assert.Null (app.TopRunnableView);
    }

    [Fact]
    public void InitRunShutdown_Generic_IdleForExit ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init (DriverRegistry.Names.ANSI);

        app.AddTimeout (TimeSpan.Zero, () => IdleExit (app));
        Assert.Null (app.TopRunnableView);

        // Blocks until the timeout call is hit

        app.Run<Window> ();

        Assert.Null (app.TopRunnableView);
        app.Dispose ();
        Assert.Null (app.TopRunnableView);
    }

    [Fact]
    public void Run_IsRunningChanging_And_IsRunningChanged_Raised ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init (DriverRegistry.Names.ANSI);

        var isRunningChanging = 0;
        var isRunningChanged = 0;
        Runnable<bool> t = new ();

        t.IsRunningChanging += (_, a) => { isRunningChanging++; };

        t.IsRunningChanged += (_, a) => { isRunningChanged++; };

        app.AddTimeout (TimeSpan.Zero, () => IdleExit (app));

        // Blocks until the timeout call is hit
        app.Run (t);

        Assert.Equal (2, isRunningChanging);
        Assert.Equal (2, isRunningChanged);
    }

    [Fact]
    public void Run_IsRunningChanging_Cancel_IsRunningChanged_Not_Raised ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init (DriverRegistry.Names.ANSI);

        var isRunningChanging = 0;
        var isRunningChanged = 0;
        Runnable<bool> t = new ();

        t.IsRunningChanging += (_, a) =>
                               {
                                   // Cancel the first time
                                   if (isRunningChanging == 0)
                                   {
                                       a.Cancel = true;
                                   }

                                   isRunningChanging++;
                               };

        t.IsRunningChanged += (_, a) => { isRunningChanged++; };

        app.AddTimeout (TimeSpan.Zero, () => IdleExit (app));

        // Blocks until the timeout call is hit

        app.Run (t);

        Assert.Equal (1, isRunningChanging);
        Assert.Equal (0, isRunningChanged);
    }

    // Copilot
    /// <summary>
    ///     Regression test for: https://github.com/tui-cs/Terminal.Gui/issues/4920
    ///     StopRequested not reset when IsRunningChanging cancels a stop.
    ///     When a handler vetoes the stop (by cancelling IsRunningChanging), Run() must re-enter
    ///     the RunLoop instead of returning prematurely with IsRunning still true.
    /// </summary>
    [Fact]
    public void Run_WhenStopCancelledByIsRunningChanging_RunLoopResumes ()
    {
        IApplication app = NewMockedApplicationImpl ()!;
        app.Init (DriverRegistry.Names.ANSI);
        app.StopAfterFirstIteration = true;

        OnceCancelStopRunnable runnable = new ();

        // Act — must complete without hanging
        app.Run (runnable);

        // The first stop attempt was cancelled (IsRunning stayed true temporarily).
        // After the fix, StopRequested was reset and RunLoop re-entered; the second stop
        // attempt succeeded so IsRunning is now false.
        Assert.False (runnable.IsRunning);
        Assert.Equal (1, runnable.CancelCount);

        app.Dispose ();
    }

    private bool IdleExit (IApplication app)
    {
        if (app.TopRunnableView != null)
        {
            app.RequestStop ();
        }

        // Return false so the timer does not repeat
        return false;
    }

    [Fact]
    public void Open_Calls_ContinueWith_On_UIThread ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init (DriverRegistry.Names.ANSI);
        var b = new Button ();

        var result = false;

        b.Accepting += (_, _) =>
                       {
                           Task.Run (() => { Task.Delay (300).Wait (); })
                               .ContinueWith ((t, _) =>
                                              {
                                                  // no longer loading
                                                  app.Invoke (() =>
                                                              {
                                                                  result = true;
                                                                  app.RequestStop ();
                                                              });
                                              },
                                              TaskScheduler.FromCurrentSynchronizationContext ());
                       };

        app.AddTimeout (TimeSpan.FromMilliseconds (150),
                        () =>
                        {
                            // Run asynchronous logic inside Task.Run
                            if (app.TopRunnableView != null)
                            {
                                b.NewKeyDownEvent (Key.Enter);
                            }

                            return false;
                        });

        Assert.Null (app.TopRunnableView);

        var w = new Window { Title = "Open_CallsContinueWithOnUIThread" };
        w.Add (b);

        // Blocks until the timeout call is hit
        app.Run (w);

        w?.Dispose ();
        app.Dispose ();

        Assert.True (result);
    }

    [Fact]
    public void ApplicationImpl_UsesInstanceFields_NotStaticReferences ()
    {
        // This test verifies that ApplicationImpl uses instance fields instead of static Application references
        IApplication v2 = NewMockedApplicationImpl ()!;

        // Before Init, all fields should be null/default
        Assert.Null (v2.Driver);
        Assert.False (v2.Initialized);

        //Assert.Null (v2.Popover);
        //Assert.Null (v2.Navigation);
        Assert.Null (v2.TopRunnableView);
        Assert.Empty (v2.SessionStack!);

        // Init should populate instance fields
        v2.Init (DriverRegistry.Names.ANSI);

        // After Init, Driver, Navigation, and Popover should be populated
        Assert.NotNull (v2.Driver);
        Assert.True (v2.Initialized);
        Assert.NotNull (v2.Popovers);
        Assert.NotNull (v2.Navigation);
        Assert.Null (v2.TopRunnableView); // Top is still null until Run

        // Shutdown should clean up instance fields
        v2.Dispose ();

        Assert.Null (v2.Driver);
        Assert.False (v2.Initialized);

        //Assert.Null (v2.Popover);
        //Assert.Null (v2.Navigation);
        Assert.Null (v2.TopRunnableView);
        Assert.Empty (v2.SessionStack!);
    }

    /// <summary>
    ///     A runnable that cancels only the first stop attempt.
    ///     Used to simulate a "Are you sure?" veto pattern.
    /// </summary>
    private class OnceCancelStopRunnable : Runnable<int>
    {
        private bool _firstStop = true;

        /// <summary>Gets how many times the stop was cancelled.</summary>
        public int CancelCount { get; private set; }

        protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
        {
            if (!newIsRunning && _firstStop)
            {
                _firstStop = false;
                CancelCount++;

                return true; // Cancel this stop attempt
            }

            return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
        }
    }
}

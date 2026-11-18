using System.Diagnostics;

// ReSharper disable AccessToDisposedClosure

namespace StressTests;

public class ApplicationStressTests
{
    private const int NUM_INCREMENTS = 500;

    private const int NUM_PASSES = 50;
    private const int POLL_MS_DEBUGGER = 500;
    private const int POLL_MS_NORMAL = 100;

    private static volatile int _tbCounter;
#pragma warning disable IDE1006 // Naming Styles
    private static readonly ManualResetEventSlim _wakeUp = new (false);
#pragma warning restore IDE1006 // Naming Styles


    /// <summary>
    ///     Stress test for Application.Invoke to verify that invocations from background threads
    ///     are not lost or delayed indefinitely. Tests 25,000 concurrent invocations (50 passes × 500 increments).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This test automatically adapts its timeout when running under a debugger (500ms vs 100ms)
    ///         to account for slower iteration times caused by debugger overhead.
    ///     </para>
    ///     <para>
    ///         See InvokeLeakTest_Analysis.md for technical details about the timing improvements made
    ///         to TimedEvents (Stopwatch-based timing) and Application.Invoke (MainLoop wakeup).
    ///     </para>
    /// </remarks>
    [Fact]
    public async Task InvokeLeakTest ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Random r = new ();
        TextField tf = new ();
        var top = new Window ();
        top.Add (tf);

        _tbCounter = 0;

        int pollMs = Debugger.IsAttached ? POLL_MS_DEBUGGER : POLL_MS_NORMAL;
        Task task = Task.Run (() => RunTest (app, r, tf, NUM_PASSES, NUM_INCREMENTS, pollMs), TestContext.Current.CancellationToken);

        // blocks here until the RequestStop is processed at the end of the test
        app.Run (top);

        await task; // Propagate exception if any occurred

        Assert.Equal (NUM_INCREMENTS * NUM_PASSES, _tbCounter);
        top.Dispose ();
        app.Dispose ();

        return;

        void RunTest (IApplication application, Random random, TextField textField, int numPasses, int numIncrements, int pollMsValue)
        {
            for (var j = 0; j < numPasses; j++)
            {
                _wakeUp.Reset ();

                for (var i = 0; i < numIncrements; i++)
                {
                    Launch (application, random, textField, (j + 1) * numIncrements);
                }

                int maxWaitMs = pollMsValue * 50; // Maximum total wait time (5s normal, 25s debugger)
                var elapsedMs = 0;

                while (_tbCounter != (j + 1) * numIncrements) // Wait for tbCounter to reach expected value
                {
                    int tbNow = _tbCounter;

                    // Wait for Application.TopRunnable to be running to ensure timed events can be processed
                    var topRunnableWaitMs = 0;

                    while (application.TopRunnableView is null or IRunnable { IsRunning: false })
                    {
                        Thread.Sleep (1);
                        topRunnableWaitMs++;

                        if (topRunnableWaitMs > maxWaitMs)
                        {
                            application.Invoke (() => application.RequestStop ());

                            throw new TimeoutException (
                                                        $"Timeout: TopRunnableView never started running on pass {j + 1}"
                                                       );
                        }
                    }

                    _wakeUp.Wait (pollMsValue);
                    elapsedMs += pollMsValue;

                    if (_tbCounter != tbNow)
                    {
                        elapsedMs = 0; // Reset elapsed time on progress

                        continue;
                    }

                    if (elapsedMs > maxWaitMs)
                    {
                        // No change after maximum wait: Idle handlers added via Application.Invoke have gone missing
                        application.Invoke (() => application.RequestStop ());

                        throw new TimeoutException (
                                                    $"Timeout: Increment lost. _tbCounter ({_tbCounter}) didn't "
                                                    + $"change after waiting {maxWaitMs} ms (pollMs={pollMsValue}). "
                                                    + $"Failed to reach {(j + 1) * numIncrements} on pass {j + 1}"
                                                   );
                    }
                }
            }

            application.Invoke (() => application.RequestStop ());
        }

        static void Launch (IApplication application, Random random, TextField textField, int target)
        {
            Task.Run (() =>
                      {
                          Thread.Sleep (random.Next (2, 4));

                          application.Invoke (() =>
                                              {
                                                  textField.Text = $"index{random.Next ()}";
                                                  Interlocked.Increment (ref _tbCounter);

                                                  if (target == _tbCounter)
                                                  {
                                                      // On last increment wake up the check
                                                      _wakeUp.Set ();
                                                  }
                                              }
                                             );
                      }
                     );
        }
    }
}

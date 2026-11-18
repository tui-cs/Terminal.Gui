using System.Collections.Concurrent;
using System.Text;

namespace DriverTests.Output;

/// <summary>
///     Proves the race conditions in <see cref="OutputBufferImpl"/> where
///     <see cref="OutputBufferImpl.ClearContents()"/> replaces the <c>Contents</c>
///     reference while other methods are concurrently operating on it.
///     See: https://github.com/tui-cs/Terminal.Gui/issues/5130
/// </summary>
public class OutputBufferImplConcurrencyTests
{
    // Copilot

    private const int ITERATIONS = 5_000;

    /// <summary>
    ///     Concurrently calls <see cref="OutputBufferImpl.AddStr"/> and
    ///     <see cref="OutputBufferImpl.ClearContents()"/> from separate threads.
    ///     With the current broken <c>lock (Contents)</c> pattern this reproduces
    ///     crashes (AccessViolationException, NullReferenceException, or
    ///     IndexOutOfRangeException) within a few hundred iterations.
    /// </summary>
    [Fact]
    public void AddStr_And_ClearContents_Concurrent_DoesNotThrow ()
    {
        ConcurrentBag<Exception> exceptions = RunConcurrent ((buffer, i, _) =>
                                                             {
                                                                 buffer.Move (0, i % 25);
                                                                 buffer.AddStr (new string ('A', 80));
                                                             },
                                                             (buffer, i, _) =>
                                                             {
                                                                 // Alternate sizes to maximize chance of IndexOutOfRange
                                                                 buffer.SetSize (i % 2 == 0 ? 80 : 40, i % 2 == 0 ? 25 : 10);
                                                             });

        AssertNoExceptions (exceptions);
    }

    /// <summary>
    ///     Runs <see cref="OutputBufferImpl.AddStr"/> and <see cref="OutputBufferImpl.FillRect(Rectangle, Rune)"/>
    ///     concurrently with <see cref="OutputBufferImpl.ClearContents()"/> to exercise all three write paths
    ///     simultaneously.
    /// </summary>
    [Fact]
    public void AddStr_FillRect_And_ClearContents_ThreeWay_Concurrent_DoesNotThrow ()
    {
        OutputBufferImpl buffer = new ();
        buffer.SetSize (80, 25);

        ConcurrentBag<Exception> exceptions = [];
        CancellationTokenSource cts = new ();

        Thread addStrThread = new (() =>
                                   {
                                       try
                                       {
                                           for (var i = 0; i < ITERATIONS && !cts.IsCancellationRequested; i++)
                                           {
                                               try
                                               {
                                                   buffer.Move (0, i % 25);
                                                   buffer.AddStr (new string ('X', 40));
                                               }
                                               catch (Exception ex) when (ex is not OutOfMemoryException)
                                               {
                                                   exceptions.Add (ex);

                                                   break;
                                               }
                                           }
                                       }
                                       catch (Exception ex)
                                       {
                                           exceptions.Add (ex);
                                       }
                                   }) { IsBackground = true };

        Thread fillRectThread = new (() =>
                                     {
                                         try
                                         {
                                             for (var i = 0; i < ITERATIONS && !cts.IsCancellationRequested; i++)
                                             {
                                                 try
                                                 {
                                                     buffer.FillRect (new Rectangle (0, 0, 20, 5), new Rune ('Z'));
                                                 }
                                                 catch (Exception ex) when (ex is not OutOfMemoryException)
                                                 {
                                                     exceptions.Add (ex);

                                                     break;
                                                 }
                                             }
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }) { IsBackground = true };

        Thread clearerThread = new (() =>
                                    {
                                        try
                                        {
                                            for (var i = 0; i < ITERATIONS && !cts.IsCancellationRequested; i++)
                                            {
                                                try
                                                {
                                                    buffer.SetSize (i % 2 == 0 ? 80 : 40, i % 2 == 0 ? 25 : 10);
                                                }
                                                catch (Exception ex) when (ex is not OutOfMemoryException)
                                                {
                                                    exceptions.Add (ex);

                                                    break;
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            exceptions.Add (ex);
                                        }
                                    }) { IsBackground = true };

        addStrThread.Start ();
        fillRectThread.Start ();
        clearerThread.Start ();

        TimeSpan joinTimeout = TimeSpan.FromSeconds (10);
        bool addStrJoined = addStrThread.Join (joinTimeout);
        bool fillRectJoined = fillRectThread.Join (joinTimeout);
        bool clearerJoined = clearerThread.Join (joinTimeout);

        cts.Cancel ();

        if (!addStrJoined)
        {
            addStrJoined = addStrThread.Join (TimeSpan.FromSeconds (5));
        }

        if (!fillRectJoined)
        {
            fillRectJoined = fillRectThread.Join (TimeSpan.FromSeconds (5));
        }

        if (!clearerJoined)
        {
            clearerJoined = clearerThread.Join (TimeSpan.FromSeconds (5));
        }

        Assert.True (addStrJoined, "addStrThread did not stop within the timeout.");
        Assert.True (fillRectJoined, "fillRectThread did not stop within the timeout.");
        Assert.True (clearerJoined, "clearerThread did not stop within the timeout.");

        AssertNoExceptions (exceptions);
    }

    /// <summary>
    ///     Concurrently calls <see cref="OutputBufferImpl.FillRect(Rectangle, Rune)"/> and
    ///     <see cref="OutputBufferImpl.ClearContents()"/> from separate threads.
    ///     The <c>FillRect(Rectangle, Rune)</c> overload has the same broken <c>lock (Contents!)</c> pattern.
    /// </summary>
    [Fact]
    public void FillRect_And_ClearContents_Concurrent_DoesNotThrow ()
    {
        ConcurrentBag<Exception> exceptions = RunConcurrent ((buffer, _, _) =>
                                                             {
                                                                 // Fill a region that fits within the smallest size (40x10)
                                                                 buffer.FillRect (new Rectangle (0, 0, 30, 8), new Rune ('B'));
                                                             },
                                                             (buffer, i, _) => { buffer.SetSize (i % 2 == 0 ? 80 : 40, i % 2 == 0 ? 25 : 10); });

        AssertNoExceptions (exceptions);
    }

    /// <summary>
    ///     Concurrently calls <see cref="OutputBufferImpl.Move"/>, <see cref="OutputBufferImpl.AddStr"/>,
    ///     and <see cref="OutputBufferImpl.ClearContents()"/> from separate threads to verify that
    ///     interleaved Move + AddStr doesn't corrupt state when Contents is being replaced.
    /// </summary>
    [Fact]
    public void Move_AddStr_And_ClearContents_Concurrent_DoesNotThrow ()
    {
        ConcurrentBag<Exception> exceptions = RunConcurrent ((buffer, i, _) =>
                                                             {
                                                                 // Rapidly move around and write short strings
                                                                 buffer.Move (i % 40, i % 10);
                                                                 buffer.AddStr ("Hello");
                                                                 buffer.Move ((i + 20) % 40, (i + 5) % 10);
                                                                 buffer.AddStr ("World!");
                                                             },
                                                             (buffer, i, _) => { buffer.SetSize (i % 2 == 0 ? 80 : 40, i % 2 == 0 ? 25 : 10); });

        AssertNoExceptions (exceptions);
    }

    /// <summary>
    ///     Proves that <see cref="OutputBufferImpl.FillRect(Rectangle, char)"/> has a race condition
    ///     because it calls <c>Move</c> + <c>AddRune</c> without holding <c>_contentsLock</c> across
    ///     the pair. A concurrent writer can change <c>Col</c>/<c>Row</c> between the Move and AddRune,
    ///     causing characters to be written at wrong positions.
    /// </summary>
    [Fact]
    public void FillRect_Char_NonAtomic_Move_AddRune_Causes_Wrong_Position ()
    {
        // Copilot
        // This test proves that FillRect(Rectangle, char) is not atomic:
        // Two threads both call FillRect(char) targeting different rows. If Move+AddRune
        // were atomic, each row should contain only its own character. Because they
        // are NOT atomic, Col/Row shared state gets corrupted between threads.
        OutputBufferImpl buffer = new ();
        buffer.SetSize (80, 25);

        ConcurrentBag<Exception> exceptions = [];
        var wrongPositionDetected = false;

        const int iterations = 50_000;
        CancellationTokenSource cts = new ();

        // Thread 1: fills row 0, cols 0-79 with 'A'
        Thread thread1 = new (() =>
                               {
                                   try
                                   {
                                       for (var i = 0; i < iterations && !cts.IsCancellationRequested; i++)
                                       {
                                           buffer.FillRect (new Rectangle (0, 0, 80, 1), 'A');
                                       }
                                   }
                                   catch (Exception ex) when (ex is not OutOfMemoryException)
                                   {
                                       exceptions.Add (ex);
                                   }
                               }) { IsBackground = true };

        // Thread 2: fills row 20, cols 0-79 with 'B'
        Thread thread2 = new (() =>
                               {
                                   try
                                   {
                                       for (var i = 0; i < iterations && !cts.IsCancellationRequested; i++)
                                       {
                                           buffer.FillRect (new Rectangle (0, 20, 80, 1), 'B');
                                       }
                                   }
                                   catch (Exception ex) when (ex is not OutOfMemoryException)
                                   {
                                       exceptions.Add (ex);
                                   }
                               }) { IsBackground = true };

        // Checker thread: looks for 'B' in row 0 or 'A' in row 20
        Thread checker = new (() =>
                               {
                                   try
                                   {
                                       while (!cts.IsCancellationRequested)
                                       {
                                           Cell [,]? contents = buffer.Contents;

                                           if (contents is null)
                                           {
                                               continue;
                                           }

                                           // Check if row 0 has 'B' (should only have 'A' or space)
                                           for (var c = 0; c < 80; c++)
                                           {
                                               string grapheme = contents [0, c].Grapheme;

                                               if (grapheme == "B")
                                               {
                                                   wrongPositionDetected = true;
                                                   cts.Cancel ();

                                                   return;
                                               }
                                           }

                                           // Check if row 20 has 'A' (should only have 'B' or space)
                                           for (var c = 0; c < 80; c++)
                                           {
                                               string grapheme = contents [20, c].Grapheme;

                                               if (grapheme == "A")
                                               {
                                                   wrongPositionDetected = true;
                                                   cts.Cancel ();

                                                   return;
                                               }
                                           }
                                       }
                                   }
                                   catch
                                   {
                                       // Buffer access may race; ignore exceptions in checker
                                   }
                               }) { IsBackground = true };

        thread1.Start ();
        thread2.Start ();
        checker.Start ();

        thread1.Join (TimeSpan.FromSeconds (10));
        thread2.Join (TimeSpan.FromSeconds (10));
        cts.Cancel ();
        checker.Join (TimeSpan.FromSeconds (5));

        // The test proves the bug exists: wrong position IS detected due to the race.
        // After the fix, this should no longer detect wrong positions.
        Assert.False (wrongPositionDetected,
                      "FillRect(Rectangle, char) wrote characters to wrong positions due to non-atomic Move+AddRune.");
    }

    /// <summary>
    ///     Concurrently calls <see cref="OutputBufferImpl.FillRect(Rectangle, char)"/> and
    ///     <see cref="OutputBufferImpl.SetSize"/> from separate threads.
    ///     The <c>FillRect(Rectangle, char)</c> overload lacks locking, so the non-atomic
    ///     <c>Move</c> + <c>AddRune</c> pair can be interleaved with buffer resizing,
    ///     causing IndexOutOfRangeException or NullReferenceException.
    /// </summary>
    [Fact]
    public void FillRect_Char_And_SetSize_Concurrent_DoesNotThrow ()
    {
        // Copilot
        ConcurrentBag<Exception> exceptions = RunConcurrent ((buffer, i, _) =>
                                                             {
                                                                 // Use the char overload which lacks a lock
                                                                 buffer.FillRect (new Rectangle (0, 0, 30, 8), 'C');
                                                             },
                                                             (buffer, i, _) =>
                                                             {
                                                                 buffer.SetSize (i % 2 == 0 ? 80 : 40, i % 2 == 0 ? 25 : 10);
                                                             });

        AssertNoExceptions (exceptions);
    }

    private static void AssertNoExceptions (ConcurrentBag<Exception> exceptions) =>
        Assert.True (exceptions.IsEmpty,
                     $"Caught {exceptions.Count} exception(s) during concurrent access. "
                     + $"First: {exceptions.FirstOrDefault ()?.GetType ().Name}: {exceptions.FirstOrDefault ()?.Message}");

    /// <summary>
    ///     Runs a writer action and a clearer action concurrently, collecting any exceptions.
    ///     Returns the collected exceptions for assertion.
    /// </summary>
    private static ConcurrentBag<Exception> RunConcurrent (Action<OutputBufferImpl, int, CancellationToken> writerAction,
                                                           Action<OutputBufferImpl, int, CancellationToken> clearerAction)
    {
        OutputBufferImpl buffer = new ();
        buffer.SetSize (80, 25);

        ConcurrentBag<Exception> exceptions = [];
        CancellationTokenSource cts = new ();

        Thread writer = new (() =>
                             {
                                 try
                                 {
                                     for (var i = 0; i < ITERATIONS && !cts.IsCancellationRequested; i++)
                                     {
                                         try
                                         {
                                             writerAction (buffer, i, cts.Token);
                                         }
                                         catch (Exception ex) when (ex is not OutOfMemoryException)
                                         {
                                             exceptions.Add (ex);

                                             break;
                                         }
                                     }
                                 }
                                 catch (Exception ex)
                                 {
                                     exceptions.Add (ex);
                                 }
                             }) { IsBackground = true };

        Thread clearer = new (() =>
                              {
                                  try
                                  {
                                      for (var i = 0; i < ITERATIONS && !cts.IsCancellationRequested; i++)
                                      {
                                          try
                                          {
                                              clearerAction (buffer, i, cts.Token);
                                          }
                                          catch (Exception ex) when (ex is not OutOfMemoryException)
                                          {
                                              exceptions.Add (ex);

                                              break;
                                          }
                                      }
                                  }
                                  catch (Exception ex)
                                  {
                                      exceptions.Add (ex);
                                  }
                              }) { IsBackground = true };

        writer.Start ();
        clearer.Start ();

        try
        {
            bool writerCompleted = writer.Join (TimeSpan.FromSeconds (10));
            bool clearerCompleted = clearer.Join (TimeSpan.FromSeconds (10));

            if (!writerCompleted || !clearerCompleted)
            {
                cts.Cancel ();

                if (!writerCompleted && !writer.Join (TimeSpan.FromSeconds (5)))
                {
                    exceptions.Add (new TimeoutException ("Writer thread did not stop after cancellation."));
                }

                if (!clearerCompleted && !clearer.Join (TimeSpan.FromSeconds (5)))
                {
                    exceptions.Add (new TimeoutException ("Clearer thread did not stop after cancellation."));
                }
            }
        }
        finally
        {
            cts.Cancel ();
            cts.Dispose ();
        }

        return exceptions;
    }
}

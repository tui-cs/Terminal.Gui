// Claude - Opus 4.8 (1M context)

using System.Runtime.ExceptionServices;
using UICatalog;
using UICatalog.Scenarios;

namespace IntegrationTests;

/// <summary>
///     Regression coverage for issue #5474: the Progress scenario crashed on exit ("Esc") when the
///     threaded <see cref="System.Threading.Timer"/> demo was running.
/// </summary>
/// <remarks>
///     The threaded demo's timer callback runs on a thread-pool thread and calls
///     <see cref="IApplication.Invoke(System.Action)"/>. Once the application is disposed, Invoke throws
///     <see cref="NotInitializedException"/>; thrown on a thread-pool thread it is unhandled and
///     terminates the process. The scenario previously only stopped the timer in
///     <see cref="System.IDisposable.Dispose"/>, which runs after the application (and the window's
///     SubViews) have already been disposed, so the timer was never stopped. The fix stops the timer in
///     <c>win.IsRunningChanged</c>, while the application is still initialized.
/// </remarks>
public class ProgressScenarioTests (ITestOutputHelper output)
{
    [Fact]
    public void Progress_With_Running_Threaded_Timer_Quits_Without_Crashing ()
    {
        Application.ResetState (true);

        IApplication? app = null;
        var iterationCount = 0;
        var startInvoked = false;
        var timerRunningConfirmed = false;
        var notInitializedThrows = 0;

        // The threaded timer's post-shutdown Invoke throws NotInitializedException on a thread-pool
        // thread. FirstChanceException records the throw even though it occurs off the test thread.
        void OnFirstChance (object? s, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is NotInitializedException)
            {
                Interlocked.Increment (ref notInitializedThrows);
            }
        }

        AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;

        Progress scenario = new ();

        Application.InstanceInitialized += OnInit;
        Application.InstanceDisposed += OnDisposed;
        Application.ForceDriver = DriverRegistry.Names.ANSI;

        try
        {
            scenario.Main ();
            scenario.Dispose ();
        }
        finally
        {
            Application.ForceDriver = string.Empty;
            Application.InstanceInitialized -= OnInit;
            Application.InstanceDisposed -= OnDisposed;
        }

        // The scenario started the threaded System.Timer and then quit. Before the fix, the timer was
        // never stopped and kept calling Invoke on the disposed application, throwing
        // NotInitializedException. Give any leaked timer time to fire after shutdown.
        Thread.Sleep (300);

        AppDomain.CurrentDomain.FirstChanceException -= OnFirstChance;

        output.WriteLine ($"NotInitializedException throws after run: {notInitializedThrows}");

        Assert.True (startInvoked, "Failed to activate the 'Start Both' button.");
        Assert.True (timerRunningConfirmed, "The threaded timer demo never reported as Started.");
        Assert.False (app?.Initialized ?? true);
        Assert.Equal (0, notInitializedThrows);

        return;

        void OnInit (object? s, EventArgs<IApplication> a)
        {
            app = a.Value;

            // Safety net so the test can never hang if RequestStop is missed.
            app.AddTimeout (TimeSpan.FromMilliseconds (5000), ForceClose);
            app.Iteration += OnIteration;
        }

        void OnDisposed (object? s, EventArgs<IApplication> a)
        {
            if (app is { })
            {
                app.Iteration -= OnIteration;
            }
        }

        void OnIteration (object? s, EventArgs<IApplication?> a)
        {
            iterationCount++;

            if (!startInvoked)
            {
                Button? startBoth = FindRecursive<Button> (app!.TopRunnableView, b => b.Text == "Start Both");

                if (startBoth is { })
                {
                    startBoth.InvokeCommand (Command.Accept);
                    startInvoked = true;
                }

                return;
            }

            // Confirm the threaded demo actually started (its label flips to "Started"), so the timer
            // is genuinely running when we quit.
            if (!timerRunningConfirmed)
            {
                timerRunningConfirmed = FindRecursive<Label> (app!.TopRunnableView, l => l.Text == "Started") is { };

                return;
            }

            if (iterationCount > 8)
            {
                app!.RequestStop ();
            }
        }

        bool ForceClose ()
        {
            output.WriteLine ("Force-closing Progress scenario via timeout.");
            app?.RequestStop ();

            return false;
        }
    }

    private static T? FindRecursive<T> (View? current, Func<T, bool> predicate) where T : View
    {
        if (current is null)
        {
            return null;
        }

        foreach (View subView in current.GetSubViews (includeBorder: true, includePadding: true))
        {
            if (subView is T match && predicate (match))
            {
                return match;
            }

            if (FindRecursive (subView, predicate) is { } deep)
            {
                return deep;
            }
        }

        return null;
    }
}

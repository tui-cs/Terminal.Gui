#nullable enable

namespace ApplicationTests;

[Collection("Application Tests")]
public class RunTests
{
    [Fact]
    public void Run_RequestStop_Stops ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var top = new Runnable ();
        SessionToken? sessionToken = app.Begin (top);
        Assert.NotNull (sessionToken);

        app.Iteration += OnApplicationOnIteration;
        app.Run (top);
        app.Iteration -= OnApplicationOnIteration;

        top.Dispose ();

        return;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a) { app.RequestStop (); }
    }

    [Fact]
    public void Run_T_Init_Driver_Cleared_with_Runnable_Throws ()
    {
        IApplication? app = Application.Create ();

        app.Init (DriverRegistry.Names.ANSI);
        app.Driver = null;

        app.StopAfterFirstIteration = true;

        // Init has been called, but Driver has been set to null. Bad.
        Assert.Throws<InvalidOperationException> (() => app.Run<Runnable> ());
    }

    [Fact]
    public void Run_Iteration_Fires ()
    {
        var iteration = 0;

        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Iteration += Application_Iteration;
        app.Run<Runnable> ();
        app.Iteration -= Application_Iteration;

        Assert.Equal (1, iteration);
        app.Dispose ();

        return;

        void Application_Iteration (object? sender, EventArgs<IApplication?> e)
        {

            iteration++;
            app.RequestStop ();
        }
    }


    [Fact]
    public void Run_T_After_InitWithDriver_with_Runnable_and_Driver_Does_Not_Throw ()
    {
        IApplication app = Application.Create ();
        app.StopAfterFirstIteration = true;

        // Run<Runnable<bool>> when already initialized or not with a Driver will not throw (because Window is derived from Runnable)
        // Using another type not derived from Runnable will throws at compile time
        app.Run<Window> (null, DriverRegistry.Names.ANSI);

        // Run<Runnable<bool>> when already initialized or not with a Driver will not throw (because Dialog is derived from Runnable)
        app.Run<Dialog> (null, DriverRegistry.Names.ANSI);

        app.Dispose ();
    }

    [Fact]
    public void Run_T_After_Init_Does_Not_Disposes_Application_Top ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Init doesn't create a Runnable and assigned it to app.TopRunnable
        // but Begin does
        var initTop = new Runnable ();

        app.Iteration += OnApplicationOnIteration;

        app.Run<Runnable> ();
        app.Iteration -= OnApplicationOnIteration;

#if DEBUG_IDISPOSABLE
        Assert.False (initTop.WasDisposed);
        initTop.Dispose ();
        Assert.True (initTop.WasDisposed);
#endif
        initTop.Dispose ();

        app.Dispose ();

        return;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            Assert.NotEqual (initTop, app.TopRunnableView);
#if DEBUG_IDISPOSABLE
            Assert.False (initTop.WasDisposed);
#endif
            app.RequestStop ();
        }
    }

    [Fact]
    public void Run_T_After_InitWithDriver_with_TestRunnable_DoesNotThrow ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.StopAfterFirstIteration = true;

        // Init has been called and we're passing no driver to Run<TestRunnable>. This is ok.
        app.Run<Window> ();

        app.Dispose ();
    }

    [Fact]
    public void Run_T_After_InitNullDriver_with_TestRunnable_DoesNotThrow ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.StopAfterFirstIteration = true;

        // Init has been called, selecting ANSI; we're passing no driver to Run<TestRunnable>. Should be fine.
        app.Run<Window> ();

        app.Dispose ();
    }

    [Fact]
    public void Run_T_NoInit_DoesNotThrow ()
    {
        IApplication app = Application.Create ();
        app.StopAfterFirstIteration = true;

        app.Run<Window> ();

        app.Dispose ();
    }

    [Fact]
    public void Run_T_NoInit_WithDriver_DoesNotThrow ()
    {
        IApplication app = Application.Create ();
        app.StopAfterFirstIteration = true;

        // Init has NOT been called and we're passing a valid driver to Run<TestRunnable>. This is ok.
        app.Run<Runnable> (null, DriverRegistry.Names.ANSI);

        app.Dispose ();
    }

    [Fact]
    public void Run_Sets_Running_True ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var top = new Runnable ();
        SessionToken? rs = app.Begin (top);
        Assert.NotNull (rs);

        app.Iteration += OnApplicationOnIteration;
        app.Run (top);
        app.Iteration -= OnApplicationOnIteration;

        top.Dispose ();

        app.Dispose ();

        return;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            Assert.True (top.IsRunning);
            top.RequestStop ();
        }
    }

    [Fact]
    public void Run_A_Modal_Runnable_Refresh_Background_On_Moving ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Don't use Dialog here as it has more layout logic. Use Window instead.
        var w = new Window
        {
            Width = 5, Height = 5,
            Arrangement = ViewArrangement.Movable
        };
        app.Driver!.SetScreenSize (10, 10);
        SessionToken? rs = app.Begin (w);

        // Don't use visuals to test as style of border can change over time.
        Assert.Equal (new (0, 0), w.Frame.Location);

        app.Mouse.RaiseMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed });
        Assert.True (app.Mouse.IsGrabbed (w.Border.View!));
        Assert.Equal (new (0, 0), w.Frame.Location);

        // Move down and to the right.
        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (1, 1), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });
        Assert.Equal (new (1, 1), w.Frame.Location);

        app.End (rs!);
        w.Dispose ();

        app.Dispose ();
    }

    [Fact]
    public void Run_T_Creates_Top_Without_Init ()
    {
        IApplication app = Application.Create ();
        app.StopAfterFirstIteration = true;

        app.SessionEnded += OnApplicationOnSessionEnded;

        app.Run<Window> (null, DriverRegistry.Names.ANSI);

        Assert.Null (app.TopRunnableView);

        app.Dispose ();
        Assert.Null (app.TopRunnableView);

        return;

        void OnApplicationOnSessionEnded (object? sender, SessionTokenEventArgs e)
        {
            app.SessionEnded -= OnApplicationOnSessionEnded;
            e.State.Result = (e.State.Runnable as IRunnable<object?>)?.Result;
        }
    }
}

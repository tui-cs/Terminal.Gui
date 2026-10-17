// Copilot - Claude Sonnet 5.5
#nullable enable

namespace ApplicationTests;

/// <summary>Tests for <c>ApplicationImpl.IsRunLoopRunning</c>.</summary>
[Collection ("Application Tests")]
public class ApplicationImplIsRunLoopRunningTests
{
    [Fact]
    public void IsRunLoopRunning_IsFalse_Initially ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Assert.False (((ApplicationImpl)app).IsRunLoopRunning);
    }

    [Fact]
    public void IsRunLoopRunning_TracksNestedRunnables_UntilAllEnded ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var impl = (ApplicationImpl)app;

        using Runnable first = new ();
        using Runnable second = new ();
        using Runnable third = new ();

        var depth = 0;
        var afterThirdEnded = false;
        var afterSecondEnded = false;
        var afterFirstEnded = true;

        var insideFirst = false;
        var insideSecond = false;
        var insideThird = false;

        void OnIteration (object? _, EventArgs<IApplication?> __)
        {
            depth++;

            switch (depth)
            {
                case 1:
                    insideFirst = impl.IsRunLoopRunning;
                    app.Run (second);
                    afterSecondEnded = impl.IsRunLoopRunning;
                    app.RequestStop (first);

                    break;

                case 2:
                    insideSecond = impl.IsRunLoopRunning;
                    app.Run (third);
                    afterThirdEnded = impl.IsRunLoopRunning;
                    app.RequestStop (second);

                    break;

                case 3:
                    insideThird = impl.IsRunLoopRunning;
                    app.RequestStop (third);

                    break;
            }
        }

        app.Iteration += OnIteration;
        app.Run (first);
        app.Iteration -= OnIteration;

        afterFirstEnded = impl.IsRunLoopRunning;

        Assert.True (insideFirst);
        Assert.True (insideSecond);
        Assert.True (insideThird);

        // Ending an inner runnable leaves the outer run loops running.
        Assert.True (afterThirdEnded);
        Assert.True (afterSecondEnded);

        // Only after the last runnable ends is no run loop running.
        Assert.False (afterFirstEnded);
    }

    [Fact]
    public void IsRunLoopRunning_IsFalse_AfterStopRequest ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        ApplicationImpl impl = (ApplicationImpl)app;

        using Runnable runnable = new ();
        app.StopAfterFirstIteration = true;

        app.Iteration += OnIteration;
        app.Run (runnable);
        app.Iteration -= OnIteration;

        Assert.False (impl.IsRunLoopRunning);

        return;

        void OnIteration (object? _, EventArgs<IApplication?> __)
        {
            Assert.True (impl.IsRunLoopRunning);
            app.RequestStop (runnable);
        }
    }

    [Fact]
    public void IsRunLoopRunning_IsFalse_AfterException ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        ApplicationImpl impl = (ApplicationImpl)app;

        using Runnable runnable = new ();
        InvalidOperationException failure = new ("iteration failed");

        app.Iteration += OnIteration;

        Assert.Same (failure, Assert.Throws<InvalidOperationException> (() => app.Run (runnable)));
        app.Iteration -= OnIteration;

        Assert.False (impl.IsRunLoopRunning);

        return;

        void OnIteration (object? _, EventArgs<IApplication?> __)
        {
            Assert.True (impl.IsRunLoopRunning);
            throw failure;
        }
    }

    [Fact]
    public void IsRunLoopRunning_IsFalse_AfterNestedRunException ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        ApplicationImpl impl = (ApplicationImpl)app;

        using Runnable outer = new () { Id = "outer" };
        using Runnable inner = new () { Id = "inner" };
        InvalidOperationException failure = new ("nested iteration failed");

        bool innerFailed = false;

        app.Iteration += OnOuterIteration;

        app.Run (outer);
        app.Iteration -= OnOuterIteration;

        Assert.False (impl.IsRunLoopRunning);

        return;

        void OnOuterIteration (object? _, EventArgs<IApplication?> __)
        {
            Assert.True (impl.IsRunLoopRunning);

            if (!innerFailed && !inner.IsRunning)
            {
                Assert.Same (failure, Assert.Throws<InvalidOperationException> (() => app.Run (inner)));
            }
            else if (!innerFailed && !outer.StopRequested)
            {
                innerFailed = true;
                throw failure;
            }
            else
            {
                app.RequestStop (outer);
            }
        }
    }
}

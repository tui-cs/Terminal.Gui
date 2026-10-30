using System.Reflection;
using System.Text;
using Moq;

namespace ApplicationTests;

[Collection ("Application Tests")]
public class ApplicationInlineOutputTests
{
    private static ApplicationImpl Create (StringBuilder bytes, out AnsiOutput output, int width = 12, int height = 6, AppModel model = AppModel.Inline,
                                           ISizeMonitor? sizeMonitor = null, ITimeProvider? timeProvider = null)
    {
        output = new AnsiOutput (chunk => { bytes.Append (Encoding.UTF8.GetString (chunk)); return true; }, model);
        output.SetSize (width, height);
        ApplicationImpl app = new (new AnsiComponentFactory (output: output, sizeMonitor: sizeMonitor), timeProvider ?? new SystemTimeProvider ());
        app.AppModel = model;
        if (model == AppModel.Inline)
        {
            app.ForceInlinePosition = Point.Empty;
        }
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.InlinePosition = Point.Empty;
        return app;
    }

    [Fact]
    public void CallsAlwaysQueueAndIdleDrawMovesOriginWithoutResizingPhysicalScreen ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _);
        using Runnable view = new () { Width = Dim.Fill (), Height = 2 };
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        bytes.Clear ();
        Task first = app.PrintAboveAsync (owner, "first", TestContext.Current.CancellationToken);
        Task second = app.PrintAboveAsync ("second", TestContext.Current.CancellationToken);
        Assert.False (first.IsCompleted);
        Assert.False (second.IsCompleted);
        Assert.Empty (bytes.ToString ());
        app.DrainDispatches ();
        Assert.False (first.IsCompleted);
        app.LayoutAndDraw ();
        Assert.True (first.IsCompletedSuccessfully);
        Assert.False (second.IsCompleted);
        Assert.Equal (1, app.Screen.Y);
        Assert.Equal (2, app.Screen.Height);
        Assert.Equal (new Rectangle (0, 0, 12, 6), app.Driver!.Screen);
        Assert.Equal (new Point (0, 1), app.Driver.InlinePosition);
        app.LayoutAndDraw ();
        Assert.True (second.IsCompletedSuccessfully);
        Assert.Equal (2, app.Screen.Y);
        Assert.True (bytes.ToString ().IndexOf ("first", StringComparison.Ordinal) < bytes.ToString ().IndexOf ("second", StringComparison.Ordinal));
        app.LayoutAndDraw (true);
        Assert.Equal (1, Count (bytes.ToString (), "first"));
        app.End (owner);
    }

    [Fact]
    public void PendingRequestsCancelWithOwnerAndFinalEndAndReleaseCapacity ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable outer = new () { Height = 2 };
        using Runnable inner = new () { Height = 2 };
        SessionToken outerOwner = app.Begin (outer)!;
        SessionToken innerOwner = app.Begin (inner)!;
        using CancellationTokenSource source = new ();
        Task canceled = app.PrintAboveAsync (outerOwner, "cancel", source.Token);
        source.Cancel ();
        Assert.True (canceled.IsCanceled);
        Task ownedInner = app.PrintAboveAsync (innerOwner, "inner", TestContext.Current.CancellationToken);
        Task ownedOuter = app.PrintAboveAsync (outerOwner, "outer", TestContext.Current.CancellationToken);
        app.End (innerOwner);
        Assert.True (ownedInner.IsCanceled);
        Assert.False (ownedOuter.IsCompleted);
        app.LayoutAndDraw ();
        Assert.True (ownedOuter.IsCompletedSuccessfully);
        Task last = app.PrintAboveAsync ("last", TestContext.Current.CancellationToken);
        app.End (outerOwner);
        Assert.True (last.IsCanceled);
        Assert.True (app.PrintAboveAsync ("later", TestContext.Current.CancellationToken).IsCanceled);
    }

    // CoPilot - GPT-6
    [Fact]
    public void FinalSessionTeardownDoesNotCancelOutputAdmittedForNewSession ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using ManualResetEventSlim teardownPaused = new ();
        using ManualResetEventSlim resumeTeardown = new ();
        CancellationToken testToken = TestContext.Current.CancellationToken;
        Lock dispatchLock = (Lock)typeof (ApplicationImpl).GetField ("_dispatchLock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue (app)!;
        bool running = true;
        Mock<IRunnable> ending = new ();
        ending.SetupGet (runnable => runnable.IsRunning).Returns (() =>
        {
            bool result = Volatile.Read (ref running);
            if (result || dispatchLock.IsHeldByCurrentThread || Thread.CurrentThread.ManagedThreadId == app.MainThreadId)
            {
                return result;
            }
            PauseTeardown ();
            return result;
        });
        ending.Setup (runnable => runnable.SetIsRunning (false)).Callback (() => Volatile.Write (ref running, false));
        ending.Setup (runnable => runnable.RaiseIsRunningChangedEvent (false)).Callback (PauseTeardown);
        SessionToken oldOwner = new (ending.Object);
        app.SessionStack!.Push (oldOwner);
        Task oldPrint = app.PrintAboveAsync (oldOwner, "old", testToken);
        Exception? failure = null;
        Thread worker = new (() =>
        {
            try { app.End (oldOwner); }
            catch (Exception error) { failure = error; }
        }) { IsBackground = true };
        using Runnable next = new () { Width = Dim.Fill (), Height = 2 };
        SessionToken? nextOwner = null;
        Task? nextPrint = null;
        worker.Start ();
        try
        {
            Assert.True (teardownPaused.Wait (TimeSpan.FromSeconds (5), testToken));
            nextOwner = app.Begin (next)!;
            nextPrint = app.PrintAboveAsync (nextOwner, "new", testToken);
            Assert.False (nextPrint.IsCompleted);
        }
        finally
        {
            resumeTeardown.Set ();
            Assert.True (worker.Join (TimeSpan.FromSeconds (5)));
        }

        Assert.Null (failure);
        Assert.True (oldPrint.IsCanceled);
        Assert.NotNull (nextPrint);
        Assert.False (nextPrint.IsCompleted);
        app.LayoutAndDraw ();
        Assert.True (nextPrint.IsCompletedSuccessfully);
        app.End (nextOwner!);

        void PauseTeardown ()
        {
            if (teardownPaused.IsSet)
            {
                return;
            }
            // Pause the stale off-lock state read, or the later lifecycle notification once that read is removed.
            teardownPaused.Set ();
            Assert.True (resumeTeardown.Wait (TimeSpan.FromSeconds (5), testToken));
        }
    }

    [Fact]
    public void PreBeginWaitsAndQueueHasBoundedCapacity ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        List<Task> tasks = [];
        for (var i = 0; i < 256; i++)
        {
            tasks.Add (app.PrintAboveAsync ("x", TestContext.Current.CancellationToken));
        }
        Assert.Throws<InvalidOperationException> (() => { _ = app.PrintAboveAsync ("overflow", TestContext.Current.CancellationToken); });
        app.Dispose ();
        Assert.All (tasks, task => Assert.True (task.IsCanceled));
        Assert.Throws<NotInitializedException> (() => { _ = app.PrintAboveAsync ("disposed", TestContext.Current.CancellationToken); });
    }

    [Fact]
    public void InvalidGeometryFaultsBeforeInsertionAndResizeUsesExecutionWidth ()
    {
        StringBuilder bytes = new ();
        VirtualTimeProvider time = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, sizeMonitor: Mock.Of<ISizeMonitor> (monitor => monitor.InitialSizeReceived == true), timeProvider: time);
        using Runnable view = new () { Width = Dim.Fill (), Height = 2 };
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        app.Screen = new Rectangle (1, 0, 10, 2);
        Task invalid = app.PrintAboveAsync ("invalid", TestContext.Current.CancellationToken);
        bytes.Clear ();
        app.LayoutAndDraw ();
        Assert.True (invalid.IsFaulted);
        Assert.DoesNotContain ("invalid", bytes.ToString ());
        // Restore a valid full-width reservation before changing terminal width.
        app.Screen = new Rectangle (0, 1, 12, 2);
        output.SetCursor (new Cursor { Position = Point.Empty });
        Task print = app.PrintAboveAsync ("abcdefgh", TestContext.Current.CancellationToken);
        app.Driver!.SetScreenSize (4, 6);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Assert.True (print.IsCompletedSuccessfully);
        // Width reflow starts a fresh region at row zero, followed by two completed rows.
        Assert.Equal (2, app.Screen.Y);
        Assert.Equal (4, app.Screen.Width);
        app.End (owner);
    }

    [Fact]
    public void ScreenChangedCannotRecursivelyDrainAndExceptionDoesNotFailCompletedPrint ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable view = new () { Height = 2 };
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        Task first = app.PrintAboveAsync ("one", TestContext.Current.CancellationToken);
        Task second = app.PrintAboveAsync ("two", TestContext.Current.CancellationToken);
        EventHandler<EventArgs<Rectangle>> handler = (_, _) => { app.LayoutAndDraw (); throw new InvalidOperationException ("handler"); };
        app.ScreenChanged += handler;
        Assert.Throws<InvalidOperationException> (() => { app.LayoutAndDraw (); });
        Assert.True (first.IsCompletedSuccessfully);
        Assert.False (second.IsCompleted);
        app.ScreenChanged -= handler;
        app.LayoutAndDraw ();
        Assert.True (second.IsCompletedSuccessfully);
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Fact]
    public void InsertionScreenChangedCanRunAndRenderSynchronousModal ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _);
        using Runnable view = new () { Width = Dim.Fill (), Height = 2 };
        using Runnable modal = new () { Width = Dim.Fill (), Height = 2 };
        modal.Add (new Label { Text = "MODAL", Width = 5 });
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        bytes.Clear ();
        Task print = app.PrintAboveAsync ("above", TestContext.Current.CancellationToken);
        Task nextPrint = app.PrintAboveAsync ("next", TestContext.Current.CancellationToken);
        bool opened = false;
        app.StopAfterFirstIteration = true;
        app.ScreenChanged += OpenModal;

        app.LayoutAndDraw ();

        app.ScreenChanged -= OpenModal;
        Assert.True (opened);
        Assert.True (print.IsCompletedSuccessfully);
        Assert.False (nextPrint.IsCompleted);
        Assert.Contains ("MODAL", bytes.ToString ());
        app.LayoutAndDraw ();
        Assert.True (nextPrint.IsCompletedSuccessfully);
        app.End (owner);

        void OpenModal (object? _, EventArgs<Rectangle> __)
        {
            app.ScreenChanged -= OpenModal;
            opened = true;
            app.Run (modal);
        }
    }

    // CoPilot - GPT-6
    [Theory]
    [InlineData (AppModel.Inline)]
    [InlineData (AppModel.FullScreen)]
    public void LayoutAndDrawCompleteCanRunAndRenderSynchronousModal (AppModel model)
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _, model: model);
        using Runnable view = new () { Width = Dim.Fill (), Height = 2 };
        using Runnable modal = new () { Width = Dim.Fill (), Height = 2 };
        modal.Add (new Label { Text = "MODAL", Width = 5 });
        bool opened = false;
        app.StopAfterFirstIteration = true;
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        bytes.Clear ();
        app.LayoutAndDrawComplete += OpenModal;
        view.SetNeedsDraw ();

        app.LayoutAndDraw ();

        app.LayoutAndDrawComplete -= OpenModal;
        Assert.True (opened);
        Assert.Contains ("MODAL", bytes.ToString ());
        app.End (owner);

        void OpenModal (object? _, EventArgs __)
        {
            app.LayoutAndDrawComplete -= OpenModal;
            opened = true;
            app.Run (modal);
        }
    }

    // CoPilot - GPT-6
    [Theory]
    [InlineData (AppModel.Inline, false, false)]
    [InlineData (AppModel.Inline, true, false)]
    [InlineData (AppModel.FullScreen, false, false)]
    [InlineData (AppModel.FullScreen, true, false)]
    [InlineData (AppModel.Inline, false, true)]
    [InlineData (AppModel.Inline, true, true)]
    [InlineData (AppModel.FullScreen, false, true)]
    [InlineData (AppModel.FullScreen, true, true)]
    public void WorkerDisposalStopsModalOpenedFromDrawCompletion (AppModel model, bool throughIteration, bool threadAccessChecks) =>
        VerifyWorkerDisposalFromRenderingNotification (model, throughIteration, insertion: false, threadAccessChecks);

    // CoPilot - GPT-6
    [Theory]
    [InlineData (false, false)]
    [InlineData (true, false)]
    [InlineData (false, true)]
    [InlineData (true, true)]
    public void WorkerDisposalStopsModalOpenedFromInsertionScreenChanged (bool throughIteration, bool threadAccessChecks) =>
        VerifyWorkerDisposalFromRenderingNotification (AppModel.Inline, throughIteration, insertion: true, threadAccessChecks);

    [Fact]
    public void FullscreenAndDegradedOutputRejectPrinting ()
    {
        using IApplication fullscreen = Application.Create ().Init (DriverRegistry.Names.ANSI);
        Assert.Throws<InvalidOperationException> (() => { _ = fullscreen.PrintAboveAsync ("x", TestContext.Current.CancellationToken); });
        using IApplication inline = Application.Create ();
        inline.AppModel = AppModel.Inline;
        Assert.Throws<NotInitializedException> (() => { _ = inline.PrintAboveAsync ("x", TestContext.Current.CancellationToken); });
        inline.Init (DriverRegistry.Names.ANSI);
        if (inline.Driver is DriverImpl driver && driver.GetOutput () is AnsiOutput { IsInlineOutputAvailable: false })
        {
            Assert.Throws<NotSupportedException> (() => { _ = inline.PrintAboveAsync ("x", TestContext.Current.CancellationToken); });
        }
    }

    [Fact]
    public void StartupGateBlocksDirectDrawingAndCancellationStillSettles ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _);
        app.ForceInlinePosition = null;
        using Runnable view = new () { Height = 2 };
        SessionToken owner = app.Begin (view)!;
        using CancellationTokenSource cancellation = new ();
        Task task = app.PrintAboveAsync (owner, "pending", cancellation.Token);
        bytes.Clear ();
        app.LayoutAndDraw ();
        Assert.False (task.IsCompleted);
        Assert.Empty (bytes.ToString ());
        cancellation.Cancel ();
        Assert.True (task.IsCanceled);
        app.End (owner);
    }

    [Fact]
    public void SubmissionDuringLayoutWaitsForAnotherPass ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable view = new () { Height = 2 };
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        Task? submitted = null;
        view.SubViewsLaidOut += (_, _) => submitted ??= app.PrintAboveAsync ("layout", TestContext.Current.CancellationToken);
        view.SetNeedsLayout ();
        app.LayoutAndDraw ();
        Assert.NotNull (submitted);
        Assert.False (submitted.IsCompleted);
        app.LayoutAndDraw ();
        Assert.True (submitted.IsCompletedSuccessfully);
        app.End (owner);
    }

    [Fact]
    public void ForeignOwnerAndEndedOuterCancelWhileInnerRemainsRunning ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using ApplicationImpl other = Create (new StringBuilder (), out _);
        using Runnable outer = new () { Height = 2 };
        using Runnable inner = new () { Height = 2 };
        using Runnable foreign = new () { Height = 2 };
        SessionToken outerOwner = app.Begin (outer)!;
        SessionToken innerOwner = app.Begin (inner)!;
        SessionToken foreignOwner = other.Begin (foreign)!;
        Assert.True (app.PrintAboveAsync (foreignOwner, "foreign", TestContext.Current.CancellationToken).IsCanceled);
        Task pending = app.PrintAboveAsync (outerOwner, "outer", TestContext.Current.CancellationToken);
        app.End (outerOwner);
        Assert.True (pending.IsCanceled);
        Task innerTask = app.PrintAboveAsync (innerOwner, "inner", TestContext.Current.CancellationToken);
        app.LayoutAndDraw ();
        Assert.True (innerTask.IsCompletedSuccessfully);
        app.End (innerOwner);
        other.End (foreignOwner);
    }

    [Fact]
    public void FailedInsertionFaultsPendingAndNewRequestsAndSuppressesResizeAndRedraw ()
    {
        var fail = false;
        var calls = 0;
        AnsiOutput output = new (_ => { calls++; return !fail; }, AppModel.Inline);
        output.SetSize (12, 6);
        using ApplicationImpl app = new (new AnsiComponentFactory (output: output));
        app.AppModel = AppModel.Inline;
        app.ForceInlinePosition = Point.Empty;
        app.Init (DriverRegistry.Names.ANSI);
        using Runnable view = new () { Height = 2 };
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        Task first = app.PrintAboveAsync ("first", TestContext.Current.CancellationToken);
        Task second = app.PrintAboveAsync ("second", TestContext.Current.CancellationToken);
        fail = true;
        app.LayoutAndDraw ();
        Assert.True (first.IsFaulted);
        Assert.True (second.IsFaulted);
        Assert.True (app.PrintAboveAsync ("new", TestContext.Current.CancellationToken).IsFaulted);
        int after = calls;
        app.LayoutAndDraw (true);
        app.Driver!.SetScreenSize (10, 5);
        app.LayoutAndDraw (true);
        app.Navigation!.UpdateCursor ();
        Assert.Equal (after, calls);
        app.End (owner);
    }

    [Fact]
    public void ZeroTerminalSizeDefersAndRestoredSizeResumes ()
    {
        VirtualTimeProvider time = new ();
        using ApplicationImpl app = Create (new StringBuilder (), out _, sizeMonitor: Mock.Of<ISizeMonitor> (monitor => monitor.InitialSizeReceived == true), timeProvider: time);
        app.ForceInlinePosition = new Point (0, 1);
        using Runnable view = new () { Height = 2 };
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        Task print = app.PrintAboveAsync ("pending", TestContext.Current.CancellationToken);
        app.Driver!.SetScreenSize (0, 0);
        app.LayoutAndDraw ();
        Assert.False (print.IsCompleted);
        app.Driver.SetScreenSize (12, 6);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        app.LayoutAndDraw ();
        Assert.True (print.IsCompletedSuccessfully);
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Fact]
    public void ZeroHeightLiveRegionDefersPrintingAndPreservesShellOutputUntilGrowth ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _);
        using Runnable view = new () { Width = Dim.Fill (), Height = 0 };
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        bytes.Clear ();
        bytes.Append ("SHELL BEFORE\r\n");
        Task print = app.PrintAboveAsync (owner, "message", TestContext.Current.CancellationToken);

        app.LayoutAndDraw ();

        Assert.False (print.IsCompleted);
        Assert.DoesNotContain ("message", bytes.ToString ());
        Assert.DoesNotContain ($"{EscSeqUtils.CSI}2K", bytes.ToString ());
        Assert.DoesNotContain ($"{EscSeqUtils.CSI}2J", bytes.ToString ());

        view.Height = 2;
        app.LayoutAndDraw ();

        Assert.True (print.IsCompletedSuccessfully);
        Assert.Equal (2, app.Screen.Height);
        Assert.True (bytes.ToString ().IndexOf ("SHELL BEFORE", StringComparison.Ordinal) < bytes.ToString ().IndexOf ("message", StringComparison.Ordinal));
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Fact]
    public void ClearedContentsSubscriberFailureSettlesPrintedRequestAndReleasesCapacity ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable view = new () { Height = 2 };
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        Task print = app.PrintAboveAsync (owner, "printed", TestContext.Current.CancellationToken);
        EventHandler<EventArgs> throwOnClear = (_, _) => throw new InvalidOperationException ("clear callback");
        app.Driver!.ClearedContents += throwOnClear;
        try
        {
            Assert.Throws<InvalidOperationException> (() => app.LayoutAndDraw ());
        }
        finally
        {
            app.Driver.ClearedContents -= throwOnClear;
        }

        Assert.True (print.IsCompletedSuccessfully);
        Assert.True (view.NeedsDraw);
        List<Task> pending = [];
        for (var i = 0; i < 256; i++)
        {
            pending.Add (app.PrintAboveAsync (owner, "x", TestContext.Current.CancellationToken));
        }
        app.LayoutAndDraw ();
        Assert.True (pending [0].IsCompletedSuccessfully);
        app.End (owner);
        Assert.All (pending.Skip (1), task => Assert.True (task.IsCanceled));
    }

    [Fact]
    public void CancellationAfterClaimCannotInterruptInsertion ()
    {
        using CancellationTokenSource source = new ();
        var cancelOnWrite = false;
        AnsiOutput output = new (_ => { if (cancelOnWrite) { source.Cancel (); } return true; }, AppModel.Inline);
        output.SetSize (12, 6);
        using ApplicationImpl app = new (new AnsiComponentFactory (output: output));
        app.AppModel = AppModel.Inline;
        app.ForceInlinePosition = Point.Empty;
        app.Init (DriverRegistry.Names.ANSI);
        using Runnable view = new () { Height = 2 };
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        Task print = app.PrintAboveAsync ("claimed", source.Token);
        cancelOnWrite = true;
        app.LayoutAndDraw ();
        Assert.True (source.IsCancellationRequested);
        Assert.True (print.IsCompletedSuccessfully);
        app.End (owner);
    }

    [Fact]
    public void SuccessfulInsertionRemainsSuccessfulWhenLiveFrameFails ()
    {
        var fail = false;
        StringBuilder bytes = new ();
        AnsiOutput output = new (chunk => { if (fail) { return false; } bytes.Append (Encoding.UTF8.GetString (chunk)); return true; }, AppModel.Inline);
        output.SetSize (12, 6);
        using ApplicationImpl app = new (new AnsiComponentFactory (output: output));
        app.AppModel = AppModel.Inline;
        app.ForceInlinePosition = Point.Empty;
        app.Init (DriverRegistry.Names.ANSI);
        using Runnable view = new () { Height = 2 };
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        Task print = app.PrintAboveAsync ("unique", TestContext.Current.CancellationToken);
        EventHandler<EventArgs<Rectangle>> failFrame = (_, _) => fail = true;
        app.ScreenChanged += failFrame;
        app.LayoutAndDraw ();
        Assert.True (print.IsCompletedSuccessfully);
        Assert.True (output.IsInlinePlacementKnown);
        fail = false;
        app.LayoutAndDraw (true);
        Assert.Equal (1, Count (bytes.ToString (), "unique"));
        app.ScreenChanged -= failFrame;
        app.End (owner);
    }

    [Fact]
    public void MouseUsesPublishedOriginAndFocusIsPreserved ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable view = new () { Height = 2, Width = Dim.Fill () };
        TextField field = new () { Width = Dim.Fill (), Text = "edit" };
        view.Add (field);
        SessionToken owner = app.Begin (view)!;
        app.LayoutAndDraw ();
        field.SetFocus ();
        Task print = app.PrintAboveAsync ("above", TestContext.Current.CancellationToken);
        app.LayoutAndDraw ();
        Assert.True (print.IsCompletedSuccessfully);
        Assert.True (field.HasFocus);
        List<Point> positions = [];
        app.Mouse.MouseEvent += (_, e) => positions.Add (e.ScreenPosition);
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, app.Screen.Y), Flags = MouseFlags.LeftButtonPressed });
        Assert.Contains (new Point (2, 0), positions);
        Assert.Equal (new Size (12, 2), new Size (app.Driver!.GetOutputBuffer ().Cols, app.Driver.GetOutputBuffer ().Rows));
        app.End (owner);
    }

    [Fact]
    public void VetoedEndPreservesOwnedRequest ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using Runnable view = new () { Height = 2 };
        SessionToken owner = app.Begin (view)!;
        Task print = app.PrintAboveAsync (owner, "kept", TestContext.Current.CancellationToken);
        void VetoStop (object? _, CancelEventArgs<bool> e) { if (!e.NewValue) { e.Cancel = true; } }
        view.IsRunningChanging += VetoStop;
        app.End (owner);
        Assert.False (print.IsCompleted);
        app.LayoutAndDraw ();
        Assert.True (print.IsCompletedSuccessfully);
        view.IsRunningChanging -= VetoStop;
        app.End (owner);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public async Task DisposeWaitsForClaimedInsertionWithoutInterleavingCleanup (bool threadAccessChecks)
    {
        CancellationToken testToken = TestContext.Current.CancellationToken;
        using ManualResetEventSlim entered = new ();
        using ManualResetEventSlim release = new ();
        var block = false;
        AnsiOutput output = new (_ =>
        {
            if (block && !entered.IsSet)
            {
                entered.Set ();
                Assert.True (release.Wait (TimeSpan.FromSeconds (5), testToken));
            }
            return true;
        }, AppModel.Inline);
        output.SetSize (12, 6);
        using ApplicationImpl app = new (new AnsiComponentFactory (output: output));
        app.AppModel = AppModel.Inline;
        app.ForceInlinePosition = Point.Empty;
        app.EnableThreadAccessChecks (threadAccessChecks);
        app.Init (DriverRegistry.Names.ANSI);
        using Runnable view = new () { Height = 2 };
        app.Begin (view);
        app.LayoutAndDraw ();
        Task print = app.PrintAboveAsync ("claimed", testToken);
        block = true;
        Task disposer = Task.Run (() => { entered.Wait (testToken); app.Dispose (); }, testToken);
        Task releaser = Task.Run (() =>
        {
            entered.Wait (testToken);
            try
            {
                Assert.True (SpinWait.SpinUntil (() =>
                {
                    try { _ = app.InvokeAsync (() => { }, new CancellationToken (true)); return false; }
                    catch (NotInitializedException) { return true; }
                }, TimeSpan.FromSeconds (5)));
                Assert.False (disposer.IsCompleted);
            }
            finally
            {
                release.Set ();
            }
        }, testToken);
        app.LayoutAndDraw ();
        Assert.True (print.IsCompletedSuccessfully);
        await Task.WhenAll (disposer, releaser).WaitAsync (TimeSpan.FromSeconds (5), testToken);
    }

    [Fact]
    public void DisposalDuringCallerSnapshotCancelsWithoutAdmittingOldWork ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        using ManualResetEventSlim preparing = new ();
        using ManualResetEventSlim resume = new ();
        BlockingLines lines = new (preparing, resume);
        Task? print = null;
        Exception? error = null;
        CancellationToken token = TestContext.Current.CancellationToken;
        Thread worker = new (() =>
        {
            try { print = app.PrintAboveAsync (lines, token); }
            catch (Exception failure) { error = failure; }
        });
        worker.Start ();
        Assert.True (preparing.Wait (TimeSpan.FromSeconds (5), token));
        try
        {
            app.Dispose ();
        }
        finally
        {
            resume.Set ();
        }
        Assert.True (worker.Join (TimeSpan.FromSeconds (5)));
        Assert.Null (error);
        Assert.True (print?.IsCanceled);
    }

    [Fact]
    public void PreBeginAndSessionBegunOutputWaitForRunningSession ()
    {
        using ApplicationImpl app = Create (new StringBuilder (), out _);
        Task early = app.PrintAboveAsync ("early", TestContext.Current.CancellationToken);
        Task? owned = null;
        EventHandler<SessionTokenEventArgs> handler = (_, e) =>
        {
            owned = app.PrintAboveAsync (e.State, "owned", TestContext.Current.CancellationToken);
            Assert.False (owned.IsCompleted);
        };
        app.SessionBegun += handler;
        using Runnable view = new () { Height = 2 };
        SessionToken owner = app.Begin (view)!;
        app.SessionBegun -= handler;
        Assert.False (early.IsCompleted);
        app.LayoutAndDraw ();
        Assert.True (early.IsCompletedSuccessfully);
        Assert.False (owned!.IsCompleted);
        app.LayoutAndDraw ();
        Assert.True (owned.IsCompletedSuccessfully);
        app.End (owner);
    }

    [Fact]
    public void CapturedAwaitDoesNotResumeInsideInlineRenderingPass ()
    {
        SynchronizationContext? before = SynchronizationContext.Current;

        try
        {
            using ApplicationImpl app = Create (new StringBuilder (), out _);
            using Runnable view = new () { Height = 2 };
            SessionToken owner = app.Begin (view)!;
            app.LayoutAndDraw ();
            Task print = app.PrintAboveAsync ("awaited", TestContext.Current.CancellationToken);
            var continuedDuringDraw = false;
            var continued = false;

            // The await captures the context current when it runs, so install the app's context only for that call.
            SynchronizationContext.SetSynchronizationContext (app.SynchronizationContext);
            Task continuation = ContinueAsync ();
            SynchronizationContext.SetSynchronizationContext (before);

            app.LayoutAndDraw ();
            app.DrainDispatches ();
            Assert.True (continuation.IsCompletedSuccessfully);
            Assert.True (continued);
            Assert.False (continuedDuringDraw);
            app.End (owner);

            async Task ContinueAsync ()
            {
                await print;
                continued = true;

                continuedDuringDraw =
                    (bool)typeof (ApplicationImpl).GetField ("_layoutAndDrawActive",
                                                             System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                                                  .GetValue (app)!;
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (before);
        }
    }

    [Fact]
    public void CapturedAwaitContinuationFromInlineOutputWaitsForDispatchDrain ()
    {
        SynchronizationContext? before = SynchronizationContext.Current;

        try
        {
            using ApplicationImpl app = Create (new StringBuilder (), out _);
            using Runnable view = new () { Height = 2 };
            SessionToken owner = app.Begin (view)!;
            app.LayoutAndDraw ();
            Task print = app.PrintAboveAsync ("awaited", TestContext.Current.CancellationToken);
            var continued = false;
            var continuedDuringDraw = false;

            // The await captures the context current when it runs, so install the app's context only for that call.
            SynchronizationContext.SetSynchronizationContext (app.SynchronizationContext);
            Task continuation = ContinueAsync ();
            SynchronizationContext.SetSynchronizationContext (before);

            // Regression guard: settling inline output must not pump posted continuations
            // before the render pass unwinds.
            app.LayoutAndDraw ();
            Assert.False (continuation.IsCompleted);
            Assert.False (continued);
            Assert.False (continuedDuringDraw);

            app.DrainDispatches ();
            Assert.True (continuation.IsCompletedSuccessfully);
            Assert.True (continued);
            Assert.False (continuedDuringDraw);
            app.End (owner);

            async Task ContinueAsync ()
            {
                await print;
                continued = true;

                continuedDuringDraw =
                    (bool)typeof (ApplicationImpl).GetField ("_layoutAndDrawActive",
                                                             System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                                                  .GetValue (app)!;
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext (before);
        }
    }

    private static void VerifyWorkerDisposalFromRenderingNotification (AppModel model, bool throughIteration, bool insertion, bool threadAccessChecks)
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _, model: model);
        app.EnableThreadAccessChecks (threadAccessChecks);
        using Runnable view = new () { Width = Dim.Fill (), Height = 2 };
        using Runnable modal = new () { Width = Dim.Fill (), Height = 2 };
        modal.Add (new Label { Text = "MODAL", Width = 5 });
        app.Begin (view);
        app.LayoutAndDraw ();
        using ManualResetEventSlim modalRunning = new ();
        using ManualResetEventSlim disposalFinished = new ();
        Exception? disposalFailure = null;
        bool forcedStop = false;
        bool modalDrawn = false;
        Thread disposer = new (() =>
        {
            try
            {
                if (!modalRunning.Wait (TimeSpan.FromSeconds (5)))
                {
                    throw new TimeoutException ("The modal did not enter its nested loop.");
                }
                app.Dispose ();
            }
            catch (Exception error)
            {
                disposalFailure = error;
            }
            finally
            {
                disposalFinished.Set ();
            }
        }) { IsBackground = true };
        Thread watchdog = new (() =>
        {
            if (!modalRunning.Wait (TimeSpan.FromSeconds (5)) || disposalFinished.Wait (TimeSpan.FromSeconds (5)))
            {
                return;
            }
            // Bound the failing regression's cleanup without letting an external close count as success.
            forcedStop = true;
            app.RequestStop (modal);
        }) { IsBackground = true };
        app.Iteration += OnModalIteration;
        app.LayoutAndDrawComplete += OnModalDrawn;
        if (insertion)
        {
            app.ScreenChanged += OpenScreenChangedModal;
            _ = app.PrintAboveAsync ("above", TestContext.Current.CancellationToken);
        }
        else
        {
            app.LayoutAndDrawComplete += OpenCompletionModal;
            view.SetNeedsDraw ();
        }
        disposer.Start ();
        watchdog.Start ();
        try
        {
            if (throughIteration)
            {
                app.Run (view);
            }
            else
            {
                app.LayoutAndDraw ();
            }
        }
        finally
        {
            Assert.True (disposer.Join (TimeSpan.FromSeconds (10)));
            Assert.True (watchdog.Join (TimeSpan.FromSeconds (10)));
            app.Iteration -= OnModalIteration;
            app.LayoutAndDrawComplete -= OnModalDrawn;
            app.ScreenChanged -= OpenScreenChangedModal;
            app.LayoutAndDrawComplete -= OpenCompletionModal;
        }

        Assert.Null (disposalFailure);
        Assert.False (forcedStop, "Worker disposal needed an external modal close to release the rendering lock.");
        Assert.Contains ("MODAL", bytes.ToString ());
        Assert.False (app.Initialized);
        Assert.False (view.IsRunning);
        Assert.False (modal.IsRunning);

        void OnModalIteration (object? _, EventArgs<IApplication?> __)
        {
            if (!modalDrawn || !ReferenceEquals (app.TopRunnable, modal))
            {
                return;
            }
            app.Iteration -= OnModalIteration;
            modalRunning.Set ();
        }

        void OnModalDrawn (object? _, EventArgs __)
        {
            if (!ReferenceEquals (app.TopRunnable, modal))
            {
                return;
            }
            app.LayoutAndDrawComplete -= OnModalDrawn;
            modalDrawn = true;
        }

        void OpenScreenChangedModal (object? _, EventArgs<Rectangle> __)
        {
            app.ScreenChanged -= OpenScreenChangedModal;
            app.Run (modal);
        }

        void OpenCompletionModal (object? _, EventArgs __)
        {
            app.LayoutAndDrawComplete -= OpenCompletionModal;
            app.Run (modal);
        }
    }

    private sealed class BlockingLines (ManualResetEventSlim preparing, ManualResetEventSlim resume) : IReadOnlyList<InlineOutputLine>
    {
        public int Count => 1;
        public InlineOutputLine this [int index] => new ([new InlineOutputSpan ("snapshot")]);
        public IEnumerator<InlineOutputLine> GetEnumerator ()
        {
            preparing.Set ();
            if (!resume.Wait (TimeSpan.FromSeconds (5)))
            {
                throw new TimeoutException ("Snapshot was not released.");
            }
            yield return this [0];
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () => GetEnumerator ();
    }

    private static int Count (string text, string value) => text.Split (value, StringSplitOptions.None).Length - 1;
}

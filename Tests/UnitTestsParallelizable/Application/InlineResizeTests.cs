using System.Text;
using Moq;

namespace ApplicationTests;

// CoPilot - GPT-6
[Collection ("Application Tests")]
public class InlineResizeTests
{
    private static ApplicationImpl Create (StringBuilder bytes, out AnsiOutput output, out VirtualTimeProvider clock, VirtualTimeProvider? time = null, Func<bool>? accept = null)
    {
        output = new AnsiOutput (chunk => { bytes.Append (Encoding.UTF8.GetString (chunk)); return accept?.Invoke () ?? true; }, AppModel.Inline);
        output.SetSize (90, 25);
        ISizeMonitor sizeMonitor = Mock.Of<ISizeMonitor> (monitor => monitor.InitialSizeReceived == true);
        clock = time ?? new ();
        ApplicationImpl app = new (new AnsiComponentFactory (output: output, sizeMonitor: sizeMonitor), clock)
        {
            AppModel = AppModel.Inline,
            ForceInlinePosition = new Point (0, 15)
        };
        app.Init ();
        return app;
    }

    // CoPilot - GPT-6
    [Fact]
    public void Resize_ToZero_DefersWithoutErasingOrFaultingUntilSizeReturns ()
    {
        VirtualTimeProvider time = new ();
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out _, time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3 };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Task pending = app.PrintAboveAsync (owner, "completed", TestContext.Current.CancellationToken);
        bytes.Clear ();

        app.Driver!.SetScreenSize (0, 0);
        time.Advance (TimeSpan.FromSeconds (10));
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();

        Assert.Empty (bytes.ToString ());
        Assert.False (pending.IsCompleted);
        Assert.True (output.IsInlinePlacementKnown);
        app.Driver.SetScreenSize (90, 20);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Assert.True (pending.IsCompletedSuccessfully);
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Fact]
    public void FailedFreshReservation_PreservesPublishedGeometryAndFaultsPrinting ()
    {
        bool accept = true;
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time, accept: () => accept);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3 };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        app.Driver!.SetScreenSize (90, 20);
        Task pending = app.PrintAboveAsync (owner, "must not print", TestContext.Current.CancellationToken);
        accept = false;

        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();

        Assert.Equal (new Rectangle (0, 15, 90, 3), app.Screen);
        Assert.False (output.IsInlinePlacementKnown);
        Assert.True (pending.IsFaulted);
        accept = true;
        bytes.Clear ();
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw (true);
        output.SetCursor (new Cursor ());
        Assert.Empty (bytes.ToString ());
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void ResizeNotification_CanEndSessionOrDisposeWithoutPrinting (bool dispose)
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _, out VirtualTimeProvider time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3 };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        app.Driver!.SetScreenSize (90, 20);
        Task pending = app.PrintAboveAsync (owner, "must not print", TestContext.Current.CancellationToken);
        app.ScreenChanged += Stop;
        bytes.Clear ();

        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();

        Assert.True (pending.IsCanceled);
        Assert.DoesNotContain ("must not print", bytes.ToString ());

        void Stop (object? _, EventArgs<Rectangle> __)
        {
            app.ScreenChanged -= Stop;
            if (dispose)
            {
                app.Dispose ();
                return;
            }
            app.End (owner);
        }
    }

    // CoPilot - GPT-6
    [Fact]
    public void ThrowingResizeNotification_LeavesNewGeometryReadyForRepaint ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3, Text = "ready" };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        app.Driver!.SetScreenSize (90, 20);
        EventHandler<EventArgs<Rectangle>> throwing = (_, _) => throw new InvalidOperationException ("subscriber");
        app.ScreenChanged += throwing;

        time.Advance (TimeSpan.FromMilliseconds (500));
        Assert.Throws<InvalidOperationException> (() => app.LayoutAndDraw ());

        app.ScreenChanged -= throwing;
        Assert.Equal (90, app.Screen.Width);
        Assert.False (output.IsInlineResizePending);
        bytes.Clear ();
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Assert.Contains ("ready", bytes.ToString ());
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Theory]
    [InlineData (0, 20)]
    [InlineData (50, 0)]
    [InlineData (0, 0)]
    public void HeadlessZeroSize_RetainsOriginUntilPositiveDimensionsReturn (int width, int height)
    {
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.Inline;
        app.ForceInlinePosition = new Point (0, 15);
        app.Init (DriverRegistry.Names.ANSI);
        app.Screen = new Rectangle (0, 15, 90, 3);
        app.Driver!.SetScreenSize (width, height);
        app.LayoutAndDraw ();
        Assert.Equal (new Rectangle (0, 15, 90, 3), app.Screen);

        app.Driver.SetScreenSize (50, 20);
        app.LayoutAndDraw ();

        Assert.Equal (new Rectangle (0, 15, 50, 3), app.Screen);
        Assert.Equal (new Point (0, 15), app.Driver.InlinePosition);
    }

    // CoPilot - GPT-6
    [Fact]
    public void NativeResizeBetweenPollAndCaretUpdate_SuppressesStaleWrites ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3, Text = "live" };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        int? physical = output.InlineCursorRow;
        Size nativeSize = new (90, 25);
        output.NativeSizeQuery = () => nativeSize;
        nativeSize = new Size (90, 20);
        bytes.Clear ();

        output.SetCursor (new Cursor { Position = Point.Empty, Style = CursorStyle.SteadyBar });
        app.Driver!.Refresh ();

        Assert.Empty (bytes.ToString ());
        Assert.Equal (physical, output.InlineCursorRow);
        app.Driver.SetScreenSize (90, 20);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Assert.Equal (0, app.Screen.Y);
        Assert.Equal (90, app.Screen.Width);
        Assert.True (output.IsInlinePlacementKnown);
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Fact]
    public void NativeResizeBeforeDisposal_RestoresModesWithoutStaleCursorPlacement ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3 };
        app.Begin (composer);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        output.NativeSizeQuery = () => new Size (90, 10);
        bytes.Clear ();

        app.Dispose ();

        Assert.Contains ("\u001b[?2004l", bytes.ToString ());
        Assert.Contains ("\u001b[?25h", bytes.ToString ());
        Assert.Contains ("\u001b[10;1H\n", bytes.ToString ());
        Assert.DoesNotContain ("\u001b[18;1H", bytes.ToString ());
    }

    // CoPilot - GPT-6
    [Theory]
    [InlineData (1)]
    [InlineData (6)]
    [InlineData (3)]
    public void NativeResizeBeforeShrinkGrowOrPrint_DefersRawWrites (int requestedHeight)
    {
        VirtualTimeProvider time = new ();
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out _, time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3, Text = "live" };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        composer.Height = requestedHeight;
        Task pending = app.PrintAboveAsync (owner, "completed", TestContext.Current.CancellationToken);
        output.NativeSizeQuery = () => new Size (90, 20);
        bytes.Clear ();

        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();

        Assert.Empty (bytes.ToString ());
        Assert.False (pending.IsCompleted);
        Assert.True (output.IsInlinePlacementKnown);
        Assert.Equal (new Rectangle (0, 15, 90, 3), app.Screen);
        app.Driver!.SetScreenSize (90, 20);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Assert.True (pending.IsCompletedSuccessfully);
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (2)]
    public void NativeSizesReturningBeforePolling_RequireRecoveryBeforeWritesResume (int surface)
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3, Text = "live" };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Size native = new (90, 23);
        output.NativeSizeQuery = () => native;
        bytes.Clear ();
        Observe ();
        native = new Size (90, 25);
        Observe ();
        output.SetCursor (new Cursor { Position = Point.Empty });
        Assert.Empty (bytes.ToString ());

        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Assert.Empty (bytes.ToString ());
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Assert.StartsWith (FreshReservation (app.Driver!.Screen.Height), bytes.ToString ());

        Assert.Equal (new Rectangle (0, 0, 90, 3), app.Screen);
        Assert.False (output.IsInlineResizePending);
        Assert.True (output.IsInlinePlacementKnown);
        app.End (owner);

        void Observe ()
        {
            if (surface == 0)
            {
                output.Write (((DriverImpl)app.Driver!).GetOutputBuffer ());
                return;
            }
            if (surface == 1)
            {
                output.SetCursor (new Cursor { Position = Point.Empty });
                return;
            }
            output.GetSize ();
        }
    }

    // CoPilot - GPT-6
    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void ExcessiveUnprocessedNativeTransitions_ReserveFreshRegionAndResumeWrites (bool overflowReturnsToPolledSize)
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3 };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Size native = new (90, 25);
        output.NativeSizeQuery = () => native;
        bytes.Clear ();
        for (var i = 0; i < 100; i++)
        {
            int width = i % 2 == 0 ? 50 : 90;
            if (overflowReturnsToPolledSize)
            {
                width = i < 64 ? 50 + i % 2 * 10 : 90;
            }

            native = new Size (width, 25);
            output.SetCursor (new Cursor { Position = Point.Empty });
        }
        Task pending = app.PrintAboveAsync (owner, "completed", TestContext.Current.CancellationToken);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Assert.Empty (bytes.ToString ());
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();

        Assert.True (pending.IsCompletedSuccessfully);
        Assert.True (output.IsInlinePlacementKnown);
        Assert.StartsWith (FreshReservation (app.Driver!.Screen.Height), bytes.ToString ());
        Assert.DoesNotContain ("[2J", bytes.ToString ());
        Assert.DoesNotContain ("[3J", bytes.ToString ());
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Fact]
    public void PartialWidthResize_DoesNotEraseUnownedColumns ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3 };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        app.Screen = new Rectangle (0, 15, 40, 3);
        Task pending = app.PrintAboveAsync (owner, "completed", TestContext.Current.CancellationToken);
        bytes.Clear ();
        string? reservation = null;
        app.ScreenChanged += CaptureReservation;

        void CaptureReservation (object? _, EventArgs<Rectangle> __)
        {
            app.ScreenChanged -= CaptureReservation;
            reservation = bytes.ToString ();
        }

        app.Driver!.SetScreenSize (90, 20);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();

        Assert.True (pending.IsCompletedSuccessfully);
        Assert.True (output.IsInlinePlacementKnown);
        Assert.NotNull (reservation);
        Assert.Equal (FreshReservation (20), reservation);
        Assert.DoesNotContain ("[2K", reservation);
        Assert.DoesNotContain ("[2J", bytes.ToString ());
        Assert.DoesNotContain ("[3J", bytes.ToString ());
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Fact]
    public void FailedFrameBeforeResize_DoesNotTrustCachedCaretOrPartiallyWrittenPosition ()
    {
        bool accept = true;
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time, accept: () => accept);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3, Text = "live" };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        output.SetCursor (new Cursor { Position = new Point (0, 1), Style = CursorStyle.SteadyBar });
        accept = false;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw (true);
        accept = true;
        Assert.Null (output.InlineCursorRow);
        Task pending = app.PrintAboveAsync (owner, "completed", TestContext.Current.CancellationToken);
        bytes.Clear ();

        app.Driver!.SetScreenSize (90, 20);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();

        Assert.True (pending.IsCompletedSuccessfully);
        Assert.True (output.IsInlinePlacementKnown);
        Assert.StartsWith (FreshReservation (app.Driver!.Screen.Height), bytes.ToString ());
        Assert.DoesNotContain ("[2J", bytes.ToString ());
        Assert.DoesNotContain ("[3J", bytes.ToString ());
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Fact]
    public void ResizeInDrawingCallback_StopsStaleFrameBeforeDriverRefresh ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3, Text = "live" };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        composer.DrawingContent += Resize;
        bytes.Clear ();

        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw (true);

        Assert.True (output.IsInlineResizePending);
        Assert.Empty (bytes.ToString ());
        app.End (owner);

        void Resize (object? _, DrawEventArgs __)
        {
            composer.DrawingContent -= Resize;
            app.Driver!.SetScreenSize (90, 20);
        }
    }

    // CoPilot - GPT-6
    [Fact]
    public void NativeTransitionsReturningDuringFinalGuard_DeferPlannedReservationAndUseFreshRegion ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3 };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        app.Driver!.SetScreenSize (90, 20);
        int reads = 0;
        bool nested = false;
        Size native = new (90, 20);
        output.NativeSizeQuery = () =>
        {
            if (nested || ++reads != 2)
            {
                return native;
            }

            nested = true;
            native = new Size (50, 20);
            output.GetSize ();
            native = new Size (90, 20);
            output.GetSize ();
            nested = false;
            return native;
        };
        bytes.Clear ();

        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();

        Assert.Empty (bytes.ToString ());
        Assert.Equal (15, app.Screen.Y);
        Assert.True (output.IsInlineResizePending);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Assert.Contains ("\u001b[20;1H" + new string ('\n', 20), bytes.ToString ());
        Assert.Equal (0, app.Screen.Y);
        Assert.False (output.IsInlineResizePending);
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Theory]
    [InlineData (0, 3, 50, 25)]
    [InlineData (22, 3, 50, 25)]
    [InlineData (24, 1, 50, 25)]
    [InlineData (0, 25, 50, 25)]
    [InlineData (15, 3, 90, 24)]
    [InlineData (15, 3, 90, 30)]
    [InlineData (24, 1, 90, 1)]
    [InlineData (0, 25, 90, 1)]
    public void EveryResize_ReservesWholeFreshViewportWithoutCursorQuery (int origin, int height, int width, int rows)
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time);
        app.ForceInlinePosition = new Point (0, origin);
        using Runnable composer = new () { Width = Dim.Fill (), Height = height, Text = "live" };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Task pending = app.PrintAboveAsync (owner, "completed", TestContext.Current.CancellationToken);
        bytes.Clear ();

        app.Driver!.SetScreenSize (width, rows);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();

        Assert.True (pending.IsCompletedSuccessfully);
        Assert.Equal (width, app.Screen.Width);
        Assert.Equal (Math.Min (height, rows), app.Screen.Height);
        Assert.True (output.IsInlinePlacementKnown);
        Assert.False (output.IsInlineResizePending);
        Assert.StartsWith (FreshReservation (rows), bytes.ToString ());
        Assert.DoesNotContain ("\u001b[6n", bytes.ToString ());
        Assert.DoesNotContain ("[2K", bytes.ToString () [..bytes.ToString ().IndexOf ('\n')]);
        app.End (owner);
    }

    private static string FreshReservation (int rows) =>
        EscSeqUtils.OSC_EndHyperlink () + EscSeqUtils.CSI_ResetAttributes + EscSeqUtils.CSI_SetCursorPosition (rows, 1)
        + new string ('\n', rows) + EscSeqUtils.CSI_SetCursorPosition (1, 1);

    // CoPilot - GPT-6
    [Fact]
    public void ResizeBurst_DefersAllWritesUntilQuietThenReservesOnce ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 3, Text = "live" };
        SessionToken owner = app.Begin (composer)!;
        app.LayoutAndDraw ();
        Task pending = app.PrintAboveAsync (owner, "completed", TestContext.Current.CancellationToken);
        bytes.Clear ();
        app.Driver!.SetScreenSize (90, 20);
        time.Advance (TimeSpan.FromMilliseconds (499));
        app.LayoutAndDraw ();
        output.SetCursor (new Cursor { Position = Point.Empty });
        app.Driver.Refresh ();
        Assert.Empty (bytes.ToString ());
        Assert.False (pending.IsCompleted);

        app.Driver.SetScreenSize (90, 22);
        time.Advance (TimeSpan.FromMilliseconds (499));
        app.LayoutAndDraw ();
        Assert.Empty (bytes.ToString ());
        time.Advance (TimeSpan.FromMilliseconds (1));
        app.LayoutAndDraw ();

        Assert.StartsWith (FreshReservation (22), bytes.ToString ());
        Assert.Equal (1, bytes.ToString ().Split (FreshReservation (22)).Length - 1);
        Assert.Equal (1, bytes.ToString ().Split ("completed").Length - 1);
        Assert.True (pending.IsCompletedSuccessfully);
        Assert.False (output.IsInlineResizePending);
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Fact]
    public void ZeroHeightFreshRegion_DefersPrintingUntilRunnableGrows ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out AnsiOutput output, out VirtualTimeProvider time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 0 };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        app.Screen = new Rectangle (0, 15, 90, 0);
        Task pending = app.PrintAboveAsync (owner, "completed", TestContext.Current.CancellationToken);
        bytes.Clear ();

        app.Driver!.SetScreenSize (90, 20);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();

        Assert.StartsWith (FreshReservation (20), bytes.ToString ());
        Assert.Equal (new Rectangle (0, 0, 90, 0), app.Screen);
        Assert.False (pending.IsCompleted);
        Assert.False (output.IsInlineResizePending);
        composer.Height = 3;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        Assert.True (pending.IsCompletedSuccessfully);
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Fact]
    public void FreshResize_RepaintsVisiblePopover ()
    {
        StringBuilder bytes = new ();
        using ApplicationImpl app = Create (bytes, out _, out VirtualTimeProvider time);
        using Runnable composer = new () { Width = Dim.Fill (), Height = 6 };
        SessionToken owner = app.Begin (composer)!;
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        using Popover<TextField, string> popover = new () { Width = 10, Height = 2 };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();
        int draws = 0;
        popover.DrawingContent += (_, _) => draws++;
        bytes.Clear ();

        app.Driver!.SetScreenSize (90, 20);
        time.Advance (TimeSpan.FromMilliseconds (500));
        app.LayoutAndDraw ();

        Assert.StartsWith (FreshReservation (20), bytes.ToString ());
        Assert.Equal (0, app.Screen.Y);
        Assert.True (draws > 0);
        app.Popovers.Hide (popover);
        app.Popovers.DeRegister (popover);
        app.End (owner);
    }

    // CoPilot - GPT-6
    [Fact]
    public void HeadlessResize_RetainsOriginAndClampsRegionWithoutWholeDisplayErase ()
    {
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.Inline;
        app.ForceInlinePosition = new Point (0, 15);
        app.Init (DriverRegistry.Names.ANSI);
        app.Screen = new Rectangle (0, 15, 90, 3);
        app.Driver!.SetScreenSize (50, 20);

        app.LayoutAndDraw ();

        Assert.Equal (new Rectangle (0, 15, 50, 3), app.Screen);
        Assert.Equal (new Point (0, 15), app.Driver.InlinePosition);
        Assert.False (app.ClearScreenNextIteration);
    }
}

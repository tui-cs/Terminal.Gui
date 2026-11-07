// CoPilot - GPT-6.1 Sol
using System.Drawing;
using Moq;

namespace ApplicationTests;

[Collection ("Application Tests")]
public class ThreadAccessTests
{
    [Fact]
    public void ApplicationAccess_DistinguishesUnavailableAndWrongThread ()
    {
        using IApplication app = Application.Create ();
        Assert.False (app.CheckAccess ());
        Assert.Throws<NotInitializedException> (() => app.VerifyAccess ());

        app.Init (DriverRegistry.Names.ANSI);
        Assert.True (app.CheckAccess ());
        app.VerifyAccess ();
        Assert.IsType<InvalidOperationException> (OnWorker (() => app.VerifyAccess ()));
        Assert.Null (OnWorker (() => Assert.False (app.CheckAccess ())));

        app.Dispose ();
        Assert.False (app.CheckAccess ());
        Assert.Throws<NotInitializedException> (() => app.VerifyAccess ());
    }

    [Fact]
    public void CustomApplication_CanOptIntoDiagnosticsWithoutNewRequiredMembers ()
    {
        Mock<IApplication> app = new ();
        app.SetupGet (a => a.Initialized).Returns (true);
        app.SetupGet (a => a.MainThreadId).Returns (Environment.CurrentManagedThreadId);
        Assert.True (app.Object.CheckAccess ());
        Assert.Throws<NotSupportedException> (() => app.Object.EnableThreadAccessChecks ());

        Mock<IApplication> capableApp = new ();
        capableApp.SetupGet (a => a.Initialized).Returns (true);
        capableApp.SetupGet (a => a.MainThreadId).Returns (Environment.CurrentManagedThreadId);
        capableApp.As<IApplicationThreadAccess> ().SetupProperty (a => a.ThreadAccessChecksEnabled);
        capableApp.Object.EnableThreadAccessChecks ();
        using View view = new () { App = capableApp.Object };
        Assert.IsType<InvalidOperationException> (OnWorker (() => view.Title = "worker"));
        view.App = null;
    }

    [Fact]
    public void ExplicitViewVerification_ChecksEvenWhenAutomaticChecksAreDisabled ()
    {
        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        using View view = new () { App = app };
        Assert.Null (OnWorker (() => view.Title = "unchecked"));
        Assert.IsType<InvalidOperationException> (OnWorker (() => view.VerifyAccess ()));
        view.VerifyAccess ();
    }

    [Fact]
    public void PreInitAssociation_PermitsSetup_UntilTheApplicationEstablishesItsThread ()
    {
        using IApplication app = Application.Create ().EnableThreadAccessChecks ();
        using View view = new () { App = app };
        Assert.Null (OnWorker (() => view.Text = "setup"));
        app.Init (DriverRegistry.Names.ANSI);
        // Even a no-op setter must check before taking its equality shortcut.
        Assert.IsType<InvalidOperationException> (OnWorker (() => view.Text = "setup"));
        Assert.Equal ("setup", view.Text);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void AutomaticCheck_OnUiThread_DoesNotAllocate (bool enabled)
    {
        using IApplication app = Application.Create ().EnableThreadAccessChecks (enabled).Init (DriverRegistry.Names.ANSI);
        using View view = new () { App = app };

        for (var iteration = 0; iteration < 10000; iteration++)
        {
            view.VerifyAccessIfEnabled ();
        }

        long before = GC.GetAllocatedBytesForCurrentThread ();

        for (var iteration = 0; iteration < 10000; iteration++)
        {
            view.VerifyAccessIfEnabled ();
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread () - before;
        Assert.Equal (0, allocated);
    }

    [Fact]
    public void WorkerDisposeView_FailsBeforeDisposalNotification ()
    {
        using IApplication app = CreateApp ();
        using View view = new () { App = app };
        var notified = false;
        view.Disposing += (_, _) => notified = true;
        Assert.IsType<InvalidOperationException> (OnWorker (view.Dispose));
        Assert.False (notified);
        view.Title = "usable after failed disposal";
        view.Dispose ();
        Assert.True (notified);
    }

    [Fact]
    public void ViewDisposal_AfterApplicationDisposal_DoesNotRecreateMouseServices ()
    {
        Mock<IApplication> app = new (MockBehavior.Strict);
        app.SetupGet (a => a.Initialized).Returns (false);
        using View view = new () { App = app.Object };
        view.Dispose ();
        app.VerifyGet (a => a.Mouse, Times.Never);
    }

    [Theory]
    [InlineData ("Text")]
    [InlineData ("Title")]
    [InlineData ("Visible")]
    [InlineData ("Enabled")]
    [InlineData ("X")]
    [InlineData ("Y")]
    [InlineData ("Width")]
    [InlineData ("Height")]
    [InlineData ("Frame")]
    [InlineData ("Viewport")]
    [InlineData ("ViewportSettings")]
    [InlineData ("ContentSizeTracksViewport")]
    [InlineData ("SetContentWidth")]
    [InlineData ("SetContentHeight")]
    [InlineData ("SetContentSize")]
    [InlineData ("CanFocus")]
    [InlineData ("HasFocus")]
    [InlineData ("TabStop")]
    [InlineData ("SetFocus")]
    [InlineData ("AdvanceFocus")]
    [InlineData ("ClearFocus")]
    [InlineData ("FocusDeepest")]
    [InlineData ("Layout")]
    [InlineData ("SetRelativeLayout")]
    [InlineData ("SetNeedsLayout")]
    [InlineData ("SetNeedsDraw")]
    [InlineData ("SetNeedsDrawRegion")]
    public void WorkerMutation_FailsBeforeStateOrEventsChange (string operation)
    {
        using IApplication app = CreateApp ();
        using View view = new () { Id = "subject", Text = "before", Title = "before", CanFocus = true, Width = 10, Height = 5, App = app };
        view.SetContentSize (new Size (20, 20));
        view.NeedsLayout = false;
        view.ClearNeedsDraw ();
        Rectangle frame = view.Frame;
        Rectangle viewport = view.Viewport;
        Pos x = view.X;
        Pos y = view.Y;
        Dim width = view.Width;
        Dim height = view.Height;
        TabBehavior? tabStop = view.TabStop;
        var events = 0;
        view.TextChanging += (_, _) => events++;
        view.TextChanged += (_, _) => events++;
        view.TitleChanging += (_, _) => events++;
        view.TitleChanged += (_, _) => events++;
        view.VisibleChanging += (_, _) => events++;
        view.VisibleChanged += (_, _) => events++;
        view.EnabledChanged += (_, _) => events++;
        view.WidthChanging += (_, _) => events++;
        view.WidthChanged += (_, _) => events++;
        view.HeightChanging += (_, _) => events++;
        view.HeightChanged += (_, _) => events++;
        view.FrameChanged += (_, _) => events++;
        view.ViewportChanged += (_, _) => events++;
        view.CanFocusChanged += (_, _) => events++;
        view.HasFocusChanging += (_, _) => events++;
        view.HasFocusChanged += (_, _) => events++;
        view.AdvancingFocus += (_, _) => events++;
        view.ContentSizeChanging += (_, _) => events++;
        view.ContentSizeChanged += (_, _) => events++;

        InvalidOperationException error = Assert.IsType<InvalidOperationException> (OnWorker (Mutate));

        Assert.Contains ("subject", error.Message);
        Assert.Contains (operation == "SetNeedsDrawRegion" ? "SetNeedsDraw" : operation, error.Message);
        Assert.Contains ("expected thread " + app.MainThreadId, error.Message);
        Assert.Contains ("current thread", error.Message);
        Assert.Equal (0, events);
        Assert.Equal ("before", view.Text);
        Assert.Equal ("before", view.Title);
        Assert.True (view.Visible);
        Assert.True (view.Enabled);
        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);
        Assert.Equal (tabStop, view.TabStop);
        Assert.Same (x, view.X);
        Assert.Same (y, view.Y);
        Assert.Same (width, view.Width);
        Assert.Same (height, view.Height);
        Assert.Equal (frame, view.Frame);
        Assert.Equal (viewport, view.Viewport);
        Assert.Equal (ViewportSettingsFlags.None, view.ViewportSettings);
        Assert.Equal (new Size (20, 20), view.GetContentSize ());
        Assert.False (view.NeedsLayout);
        Assert.False (view.NeedsDraw);

        return;

        void Mutate ()
        {
            switch (operation)
            {
                case "Text": view.Text = "after"; break;
                case "Title": view.Title = "after"; break;
                case "Visible": view.Visible = false; break;
                case "Enabled": view.Enabled = false; break;
                case "X": view.X = 2; break;
                case "Y": view.Y = 2; break;
                case "Width": view.Width = 12; break;
                case "Height": view.Height = 6; break;
                case "Frame": view.Frame = new Rectangle (1, 1, 12, 6); break;
                case "Viewport": view.Viewport = view.Viewport with { X = 1 }; break;
                case "ViewportSettings": view.ViewportSettings = ViewportSettingsFlags.AllowNegativeX; break;
                case "ContentSizeTracksViewport": view.ContentSizeTracksViewport = true; break;
                case "SetContentWidth": view.SetContentWidth (30); break;
                case "SetContentHeight": view.SetContentHeight (30); break;
                case "SetContentSize": view.SetContentSize (new Size (30, 30)); break;
                case "CanFocus": view.CanFocus = false; break;
                case "HasFocus": view.HasFocus = true; break;
                case "TabStop": view.TabStop = TabBehavior.NoStop; break;
                case "SetFocus": view.SetFocus (); break;
                case "AdvanceFocus": view.AdvanceFocus (NavigationDirection.Forward, null); break;
                case "ClearFocus": view.ClearFocus (); break;
                case "FocusDeepest": view.FocusDeepest (NavigationDirection.Forward, null); break;
                case "Layout": view.Layout (); break;
                case "SetRelativeLayout": view.SetRelativeLayout (new Size (80, 25)); break;
                case "SetNeedsLayout": view.SetNeedsLayout (); break;
                case "SetNeedsDraw": view.SetNeedsDraw (); break;
                case "SetNeedsDrawRegion": view.SetNeedsDraw (new Rectangle (0, 0, 1, 1)); break;
                default: throw new ArgumentOutOfRangeException (nameof (operation));
            }
        }
    }

    [Theory]
    [InlineData (false, true)]
    [InlineData (true, false)]
    [InlineData (false, false)]
    public void HiddenAndDisabledViews_RemainAssociated (bool visible, bool enabled)
    {
        using IApplication app = CreateApp ();
        using View root = new () { App = app };
        View view = new () { Visible = visible, Enabled = enabled };
        root.Add (view);
        Assert.IsType<InvalidOperationException> (OnWorker (() => view.Text = "worker"));
        Assert.IsType<InvalidOperationException> (OnWorker (view.SetNeedsDraw));
        view.Text = "UI";
        Assert.Equal ("UI", view.Text);
    }

    [Fact]
    public void HierarchyMutations_FailBeforeCollectionsOrEventsChange ()
    {
        using IApplication app = CreateApp ();
        using View root = new () { App = app };
        View first = new ();
        View second = new ();
        using View extra = new ();
        root.Add (first, second);
        var events = 0;
        root.SubViewAdding += (_, _) => events++;
        root.SubViewAdded += (_, _) => events++;
        first.SuperViewChanging += (_, _) => events++;
        root.SubViewRemoved += (_, _) => events++;

        Action [] mutations =
        [
            () => root.Add (extra), () => root.AddAt (0, extra), () => root.Remove (first),
            () => root.RemoveAll (), () => root.RemoveAll<View> (), () => root.MoveSubViewToEnd (first),
            () => root.MoveSubViewToStart (second), () => root.MoveSubViewTowardsStart (second),
            () => root.MoveSubViewTowardsEnd (first)
        ];

        foreach (Action mutation in mutations)
        {
            Assert.IsType<InvalidOperationException> (OnWorker (mutation));
            Assert.Equal ([first, second], root.SubViews);
            Assert.Same (root, first.SuperView);
            Assert.Null (extra.SuperView);
            Assert.Equal (0, events);
        }
    }

    [Fact]
    public void ApplicationAssociation_CannotBeClearedOrReplacedFromWorker ()
    {
        using IApplication app = CreateApp ();
        using IApplication other = CreateApp ();
        using View view = new () { App = app };
        Assert.IsType<InvalidOperationException> (OnWorker (() => view.App = null));
        Assert.IsType<InvalidOperationException> (OnWorker (() => view.App = other));
        Assert.Same (app, view.App);
        view.App = null;
        Assert.Null (OnWorker (() => view.Text = "detached"));
        view.App = other;
        view.Text = "reassociated";
    }

    [Fact]
    public void DestinationAndExplicitlyAssociatedSubViews_AreCheckedBeforeAdding ()
    {
        using IApplication app = CreateApp ();
        using View root = new ();
        using View view = new () { App = app };
        Assert.IsType<InvalidOperationException> (OnWorker (() => root.Add (view)));
        Assert.Empty (root.SubViews);
        Assert.Null (view.SuperView);
        root.Add (view);

        Assert.IsType<InvalidOperationException> (OnWorker (() => root.App = app));
        Assert.Null (root.App);
    }

    [Fact]
    public void DetachedTree_CanBePreparedOnWorkerAndAddedOnUiThread ()
    {
        using IApplication app = CreateApp ();
        using View root = new () { App = app };
        View tree = new ();
        Assert.Null (OnWorker (() =>
                             {
                                 tree.Title = "prepared";
                                 tree.Add (new Label { Text = "result" });
                             }));
        root.Add (tree);
        Assert.IsType<InvalidOperationException> (OnWorker (() => tree.Text = "bound"));
        root.Remove (tree);
        Assert.Null (OnWorker (() => tree.Text = "removed"));
        tree.Dispose ();
    }

    [Fact]
    public void RemovedView_WithExplicitApp_RemainsAssociated ()
    {
        using IApplication app = CreateApp ();
        using View root = new () { App = app };
        using View view = new () { App = app };
        root.Add (view);
        root.Remove (view);
        Assert.IsType<InvalidOperationException> (OnWorker (() => view.Text = "still bound"));
    }

    [Fact]
    public void Diagnostics_AreIndependentBetweenApplications ()
    {
        using IApplication checkedApp = CreateApp ();
        using IApplication uncheckedApp = Application.Create ().Init (DriverRegistry.Names.ANSI);
        using View checkedView = new () { App = checkedApp };
        using View uncheckedView = new () { App = uncheckedApp };
        Assert.IsType<InvalidOperationException> (OnWorker (() => checkedView.Title = "checked"));
        Assert.Null (OnWorker (() => uncheckedView.Title = "unchecked"));
        checkedApp.EnableThreadAccessChecks (false);
        Assert.Null (OnWorker (() => checkedView.Title = "disabled"));
    }

    [Fact]
    public void SessionEnd_DoesNotDetachViews_AndDisposalEndsAffinity ()
    {
        using IApplication app = CreateApp ();
        using Runnable runnable = new () { Id = "owner" };
        SessionToken owner = app.Begin (runnable)!;
        InvalidOperationException error = Assert.IsType<InvalidOperationException> (OnWorker (() => runnable.Title = "worker"));
        Assert.Contains ("Runnable (Id='owner')", error.Message);
        Assert.Contains ("session " + owner.Id, error.Message);
        app.End (owner);
        Assert.Same (app, runnable.App);
        Assert.IsType<InvalidOperationException> (OnWorker (() => runnable.Title = "ended"));
        runnable.Title = "correct thread, ended owner";
        app.Dispose ();
        Assert.Null (OnWorker (() => runnable.Title = "application unavailable"));
    }

    [Fact]
    public void AdornmentAndPopoverViews_UseTheirEffectiveApplication ()
    {
        using IApplication app = CreateApp ();
        using View view = new () { App = app };
        View borderView = view.Border.GetOrCreateView ();
        Assert.Same (app, borderView.App);
        Assert.IsType<InvalidOperationException> (OnWorker (() => borderView.Title = "border"));
        borderView.Title = "UI border";
        Assert.Equal ("UI border", borderView.Title);
        using PopoverMenu popover = new ();
        app.Popovers!.Register (popover);
        Assert.IsType<InvalidOperationException> (OnWorker (() => popover.Title = "popover"));
        popover.Title = "UI popover";
        Assert.Equal ("UI popover", popover.Title);
        app.Popovers.DeRegister (popover);
    }

    [Theory]
    [InlineData ("Parent")]
    [InlineData ("Thickness")]
    [InlineData ("GetOrCreateView")]
    public void AdornmentMutation_FailsBeforeChangingOwnerThicknessOrLazyView (string operation)
    {
        using IApplication app = CreateApp ();
        using View view = new () { App = app };
        Assert.Null (view.Border.View);
        Thickness thickness = view.Border.Thickness;
        var notified = false;
        view.Border.ThicknessChanged += (_, _) => notified = true;

        Assert.IsType<InvalidOperationException> (OnWorker (() =>
                                                          {
                                                              switch (operation)
                                                              {
                                                                  case "Parent": view.Border.Parent = null; break;
                                                                  case "Thickness": view.Border.Thickness = new Thickness (1); break;
                                                                  case "GetOrCreateView": view.Border.GetOrCreateView (); break;
                                                              }
                                                          }));

        Assert.Same (view, view.Border.Parent);
        Assert.Equal (thickness, view.Border.Thickness);
        Assert.Null (view.Border.View);
        Assert.False (notified);
    }

    [Fact]
    public void BeginOnlyAndNestedSessions_AcceptUiUpdatesAndRejectWorkerMutations ()
    {
        using IApplication app = CreateApp ();
        using Runnable outer = new ();
        using Runnable inner = new ();
        SynchronizationContext? before = SynchronizationContext.Current;
        SessionToken outerOwner = app.Begin (outer)!;
        SessionToken innerOwner = app.Begin (inner)!;

        try
        {
            Assert.Same (before, SynchronizationContext.Current);
            outer.Visible = false;
            outer.Title = "hidden outer";
            inner.Title = "inner";
            Assert.IsType<InvalidOperationException> (OnWorker (() => outer.Title = "worker"));
            Assert.IsType<InvalidOperationException> (OnWorker (() => inner.Title = "worker"));
        }
        finally
        {
            app.End (innerOwner);
            app.End (outerOwner);
        }
    }

    [Fact]
    public void Dispatches_RunWithDiagnostics_AndEndedOwnerIsCanceled ()
    {
        using IApplication app = CreateApp ();
        using Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;

        try
        {
            Task? dispatch = null;
            Assert.Null (OnWorker (() =>
                                 {
                                     app.Invoke (() => runnable.Text = "invoke");
                                     dispatch = app.InvokeAsync (owner, () => runnable.Title = "async");
                                 }));
            ((ApplicationImpl)app).Coordinator!.RunIteration ();
            Assert.True (dispatch!.IsCompletedSuccessfully);
            Assert.Equal ("invoke", runnable.Text);
            Assert.Equal ("async", runnable.Title);

            Assert.Null (OnWorker (() => dispatch = app.InvokeAsync (owner, () => runnable.Title = "stale")));
            app.End (owner);
            Assert.True (dispatch!.IsCanceled);
            Assert.Equal ("async", runnable.Title);
        }
        finally
        {
            app.End (owner);
        }
    }

    [Fact]
    public void WorkerEnd_DoesNotExemptUserLifecycleNotifications ()
    {
        using IApplication app = CreateApp ();
        using Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        runnable.IsRunningChanged += (_, _) => runnable.Title = "invalid notification";

        Assert.IsType<InvalidOperationException> (OnWorker (() => app.End (owner)));
        Assert.Null (owner.Runnable);
        Assert.False (runnable.IsRunning);
        Assert.NotEqual ("invalid notification", runnable.Title);
    }

    [Fact]
    public void WorkerEnd_RestoresOuterFocusOnUiIteration ()
    {
        using IApplication app = CreateApp ();
        using Runnable outer = new ();
        using Runnable inner = new ();
        SessionToken outerOwner = app.Begin (outer)!;
        SessionToken innerOwner = app.Begin (inner)!;

        try
        {
            Assert.Null (OnWorker (() => app.End (innerOwner)));
            Assert.False (inner.IsRunning);
            Assert.True (outer.IsRunning);
            ((ApplicationImpl)app).Coordinator!.RunIteration ();
            Assert.True (outer.HasFocus);
        }
        finally
        {
            app.End (innerOwner);
            app.End (outerOwner);
        }
    }

    [Fact]
    public void DeferredFocusRestoration_DoesNotFocusAnOuterSessionThatHasEnded ()
    {
        using IApplication app = CreateApp ();
        using Runnable outer = new ();
        using Runnable inner = new ();
        SessionToken outerOwner = app.Begin (outer)!;
        SessionToken innerOwner = app.Begin (inner)!;

        Assert.Null (OnWorker (() => app.End (innerOwner)));
        app.End (outerOwner);
        bool focusedBeforeDrain = outer.HasFocus;
        ((ApplicationImpl)app).DrainDispatches ();

        Assert.Equal (focusedBeforeDrain, outer.HasFocus);
        Assert.False (outer.IsModal);
        Assert.False (outer.IsRunning);
    }

    [Fact]
    public void WorkerDispose_CancelsPendingWorkAndCompletesCleanup ()
    {
        using IApplication app = CreateApp ();
        using Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        Task? pending = null;
        Assert.Null (OnWorker (() => pending = app.InvokeAsync (owner, () => runnable.Text = "must not run")));
        Assert.Null (OnWorker (app.Dispose));
        Assert.True (pending!.IsCanceled);
        Assert.False (app.Initialized);
        Assert.Null (app.MainThreadId);
        Assert.Null (owner.Runnable);
    }

    [Theory]
    [InlineData ("changing")]
    [InlineData ("changed")]
    [InlineData ("modal")]
    public void WorkerDispose_WithInvalidNotification_StillReleasesEverySessionAndDriver (string notification)
    {
        using IApplication app = CreateApp ();
        using Runnable outer = new ();
        using Runnable inner = new ();
        SessionToken outerOwner = app.Begin (outer)!;
        SessionToken innerOwner = app.Begin (inner)!;

        switch (notification)
        {
            case "changing": inner.IsRunningChanging += (_, _) => inner.Title = "invalid"; break;
            case "changed": inner.IsRunningChanged += (_, _) => inner.Title = "invalid"; break;
            case "modal": inner.IsModalChanged += (_, _) => inner.Title = "invalid"; break;
        }

        Assert.IsType<InvalidOperationException> (OnWorker (app.Dispose));
        Assert.False (app.Initialized);
        Assert.Null (app.MainThreadId);
        Assert.Null (app.Driver);
        Assert.Empty (app.SessionStack!);
        Assert.Null (outerOwner.Runnable);
        Assert.Null (innerOwner.Runnable);
        Assert.False (outer.IsRunning);
        Assert.False (inner.IsRunning);
        Assert.Null (OnWorker (app.Dispose));
    }

    [Fact]
    public void DisposalOverridesStopVeto_AndRaisesFinalStateNotifications ()
    {
        using IApplication app = CreateApp ();
        using Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        var runningChanged = 0;
        var modalChanged = 0;
        runnable.IsRunningChanging += (_, args) => args.Cancel = true;
        runnable.IsRunningChanged += (_, _) => runningChanged++;
        runnable.IsModalChanged += (_, _) => modalChanged++;

        app.Dispose ();

        Assert.False (runnable.IsRunning);
        Assert.False (runnable.IsModal);
        Assert.Null (owner.Runnable);
        Assert.Equal (1, runningChanged);
        Assert.Equal (1, modalChanged);
    }

    [Fact]
    public void DisposeDuringStopPreNotification_DoesNotDuplicateFinalNotifications ()
    {
        using IApplication app = CreateApp ();
        using Runnable runnable = new ();
        SessionToken owner = app.Begin (runnable)!;
        var runningChanged = 0;
        runnable.IsRunningChanging += (_, _) => app.Dispose ();
        runnable.IsRunningChanged += (_, _) => runningChanged++;

        app.End (owner);

        Assert.Equal (1, runningChanged);
        Assert.False (runnable.IsRunning);
        Assert.Null (owner.Runnable);
        Assert.False (app.Initialized);
        Assert.Null (app.Driver);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void DeferredCleanupFailure_DoesNotPreventAnotherSessionTransition (bool begin)
    {
        using IApplication app = CreateApp ();
        using Runnable first = new ();
        using Runnable second = new ();
        SessionToken owner = app.Begin (first)!;
        InvalidOperationException failure = new ("earlier queued cleanup failed");
        var continued = false;
        Assert.Null (OnWorker (() =>
                             {
                                 app.End (owner);
                                 ApplicationImpl.RunLifecycleUiWork (app, () => throw failure);
                                 ApplicationImpl.RunLifecycleUiWork (app, () => continued = true);
                             }));

        if (!begin)
        {
            // Even End of an already-ended token must drain cleanup without throwing another session's error.
            app.End (owner);
            Assert.True (continued);
            Assert.Null (owner.Runnable);

            return;
        }

        SessionToken secondOwner = app.Begin (second)!;

        try
        {
            Assert.True (continued);
            Assert.True (second.IsRunning);
        }
        finally
        {
            app.End (secondOwner);
        }
    }

    [Fact]
    public void DeferredCleanupFailure_DoesNotPreventAnActiveSessionFromEnding ()
    {
        using IApplication app = CreateApp ();
        using Runnable outer = new ();
        using Runnable inner = new ();
        SessionToken outerOwner = app.Begin (outer)!;
        SessionToken innerOwner = app.Begin (inner)!;
        Assert.Null (OnWorker (() =>
                             {
                                 app.End (outerOwner);
                                 ApplicationImpl.RunLifecycleUiWork (app, () => throw new InvalidOperationException ("queued cleanup"));
                             }));

        app.End (innerOwner);

        Assert.False (inner.IsRunning);
        Assert.Null (innerOwner.Runnable);
    }

    [Fact]
    public void DeferredUngrab_DoesNotReleaseAReplacementGrab ()
    {
        using IApplication app = CreateApp ();
        using Runnable outer = new ();
        using Runnable inner = new ();
        View first = new ();
        View replacement = new ();
        outer.Add (first, replacement);
        SessionToken outerOwner = app.Begin (outer)!;
        SessionToken innerOwner = app.Begin (inner)!;

        try
        {
            app.Mouse.GrabMouse (first);
            Assert.Null (OnWorker (() => app.End (innerOwner)));
            app.Mouse.GrabMouse (replacement);
            ((ApplicationImpl)app).DrainDispatches ();
            Assert.True (app.Mouse.IsGrabbed (replacement));
        }
        finally
        {
            app.Mouse.UngrabMouse ();
            app.End (innerOwner);
            app.End (outerOwner);
        }
    }

    [Fact]
    public void ResetAndReinit_DoNotReplayOldLifecycleCleanup ()
    {
        using IApplication app = CreateApp ();
        using Runnable first = new ();
        SessionToken owner = app.Begin (first)!;
        Assert.Null (OnWorker (() => app.End (owner)));
        Assert.Null (OnWorker (() => app.ResetState ()));
        Assert.False (app.Initialized);
        Assert.Null (app.Driver);

        app.Init (DriverRegistry.Names.ANSI);
        using Runnable second = new ();
        SessionToken secondOwner = app.Begin (second)!;

        try
        {
            Assert.NotEqual (owner.Id, secondOwner.Id);
            app.Mouse.GrabMouse (second);
            ((ApplicationImpl)app).DrainDispatches ();
            Assert.True (app.Mouse.IsGrabbed (second));
            Assert.IsType<InvalidOperationException> (OnWorker (() => second.Title = "still enabled"));
        }
        finally
        {
            app.End (secondOwner);
        }
    }

    [Fact]
    public void NestedWindow_DiagnosticIdentifiesTheActualSession ()
    {
        using IApplication app = CreateApp ();
        using Runnable runnable = new () { Id = "session-owner" };
        Window nested = new () { Id = "non-session-window" };
        Label label = new ();
        nested.Add (label);
        runnable.Add (nested);
        SessionToken owner = app.Begin (runnable)!;

        try
        {
            InvalidOperationException error = Assert.IsType<InvalidOperationException> (OnWorker (() => label.Text = "invalid"));
            Assert.Contains ("runnable Runnable (Id='session-owner')", error.Message);
            Assert.Contains ("session " + owner.Id, error.Message);
        }
        finally
        {
            app.End (owner);
        }
    }

    [Fact]
    public void InheritedAssociationCache_TracksSwitchChangesRemovalAndReassociation ()
    {
        using IApplication first = Application.Create ().Init (DriverRegistry.Names.ANSI);
        using IApplication second = CreateApp ();
        using View root = new () { App = first };
        View subView = new ();
        View leaf = new ();
        root.Add (subView);
        subView.Add (leaf);

        Assert.Null (OnWorker (() => leaf.Title = "disabled"));
        first.EnableThreadAccessChecks ();
        Assert.IsType<InvalidOperationException> (OnWorker (() => leaf.Title = "enabled later"));
        root.App = second;
        first.EnableThreadAccessChecks (false);
        Assert.IsType<InvalidOperationException> (OnWorker (() => leaf.Title = "new owner"));
        root.Remove (subView);
        Assert.Null (OnWorker (() => leaf.Title = "detached"));
        subView.Dispose ();
    }

    [Fact]
    public void ExplicitApplicationInDeepSubtree_IsNotSkippedByAssociationValidation ()
    {
        Mock<IApplication> foreign = new ();
        foreign.As<IApplicationThreadAccess> ().SetupProperty (a => a.ThreadAccessChecksEnabled, true);
        foreign.SetupGet (a => a.MainThreadId).Returns (-1);
        using View root = new ();
        using View tree = new ();
        View branch = new ();
        View leaf = new () { App = foreign.Object };
        tree.Add (branch);
        branch.Add (leaf);
        foreign.SetupGet (a => a.Initialized).Returns (true);

        Assert.Throws<InvalidOperationException> (() => root.Add (tree));
        Assert.Empty (root.SubViews);
        Assert.Null (tree.SuperView);

        foreign.SetupGet (a => a.Initialized).Returns (false);
        branch.Remove (leaf);
        leaf.App = null;
        leaf.Dispose ();
        root.Add (tree);
        root.Remove (tree);
    }

    [Fact]
    public void CustomApplicationOwner_RefreshesAutomaticChecks ()
    {
        using IApplication app = CreateApp ();
        using AssociatedView view = new (app);
        Assert.IsType<InvalidOperationException> (OnWorker (() => view.Title = "custom owner"));
        view.ChangeOwner (null);
        Assert.Null (OnWorker (() => view.Title = "custom detached"));
    }

    private sealed class AssociatedView : View
    {
        private IApplication? _owner;

        public AssociatedView (IApplication? owner)
        {
            _owner = owner;
            RefreshApplicationAssociation ();
        }

        protected override IApplication? GetApp () => _owner;

        public void ChangeOwner (IApplication? owner)
        {
            VerifyAccess ();
            owner?.VerifyAccess ();
            _owner = owner;
            RefreshApplicationAssociation ();
        }
    }

    private static IApplication CreateApp () => Application.Create ().EnableThreadAccessChecks ().Init (DriverRegistry.Names.ANSI);

    private static Exception? OnWorker (Action action)
    {
        Exception? error = null;
        Thread worker = new (() => error = Record.Exception (action)) { IsBackground = true };
        worker.Start ();
        Assert.True (worker.Join (TimeSpan.FromSeconds (10)), "Worker failed to finish.");

        return error;
    }
}

using Moq;

namespace ApplicationTests.Popover;

[Collection ("Application Tests")]
public class ApplicationPopoverTests
{
    [Fact]
    public void Register_AddsPopover ()
    {
        // Arrange
        IPopoverView popover = new Mock<IPopoverView> ().Object;
        var popoverManager = new ApplicationPopover ();

        // Act
        popoverManager.Register (popover);

        // Assert
        Assert.Contains (popover, popoverManager.Popovers);
    }

    [Fact]
    public void DeRegister_RemovesPopover ()
    {
        // Arrange
        IPopoverView popover = new Mock<IPopoverView> ().Object;
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (popover);

        // Act
        bool result = popoverManager.DeRegister (popover);

        // Assert
        Assert.True (result);
        Assert.DoesNotContain (popover, popoverManager.Popovers);
    }

    // CoPilot - Grok 4.6
    /// <summary>
    ///     Regression for https://github.com/tui-cs/Terminal.Gui/issues/5635.
    ///     DeRegister of the active popover must Hide it so the covered region is invalidated.
    /// </summary>
    [Fact]
    public void DeRegister_ActivePopover_HidesAndInvalidatesTopRunnable ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 15);

        using Runnable top = new ();
        SessionToken? token = app.Begin (top);

        PopoverTestClass popover = new () { App = app, Width = 10, Height = 3 };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);
        app.LayoutAndDraw ();

        Assert.Equal (popover, app.Popovers.GetActivePopover ());
        Assert.True (popover.Visible);
        Assert.False (top.NeedsDraw);

        bool result = app.Popovers.DeRegister (popover);

        Assert.True (result);
        Assert.DoesNotContain (popover, app.Popovers.Popovers);
        Assert.Null (app.Popovers.GetActivePopover ());
        Assert.False (popover.Visible);
        Assert.True (top.NeedsDraw);

        app.End (token!);
    }

    // CoPilot - Grok 4.6
    /// <summary>
    ///     If Hide raises VisibleChanged and a handler calls Show, DeRegister must not
    ///     leave that popover active after removing it from the registry.
    /// </summary>
    [Fact]
    public void DeRegister_ActivePopover_ReentrantShow_DoesNotLeaveUnregisteredActive ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 15);

        using Runnable top = new ();
        SessionToken? token = app.Begin (top);

        PopoverTestClass popover = new () { App = app, Width = 10, Height = 3 };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        var reopened = false;

        popover.VisibleChanged += (_, _) =>
                                  {
                                      if (popover.Visible || reopened)
                                      {
                                          return;
                                      }

                                      reopened = true;
                                      app.Popovers.Show (popover);
                                  };

        bool result = app.Popovers.DeRegister (popover);

        Assert.True (result);
        Assert.True (reopened);
        Assert.DoesNotContain (popover, app.Popovers.Popovers);
        Assert.Null (app.Popovers.GetActivePopover ());

        app.End (token!);
    }

    [Fact]
    public void Show_SetsActivePopover ()
    {
        // Arrange
        PopoverTestClass popover = new Mock<PopoverTestClass> ().Object;
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (popover);

        // Act
        popoverManager.Show (popover);

        // Assert
        Assert.Equal (popover, popoverManager.GetActivePopover ());
    }

    [Fact]
    public void Hide_ClearsActivePopover ()
    {
        // Arrange
        IPopoverView popover = new Mock<IPopoverView> ().Object;
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (popover);
        popoverManager.Show (popover);

        // Act
        popoverManager.Hide (popover);

        // Assert
        Assert.Null (popoverManager.GetActivePopover ());
    }

    [Fact]
    public void DispatchKeyDown_ActivePopoverGetsKey ()
    {
        // Arrange
        var popover = new PopoverTestClass ();
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (popover);

        popoverManager.Show (popover);

        // Act
        popoverManager.DispatchKeyDownToActivePopover (Key.A);

        // Assert
        Assert.Contains (KeyCode.A, popover.HandledKeys);
    }

    [Fact]
    public void DispatchKeyDown_ActivePopoverGetsHotKey ()
    {
        // Arrange
        var popover = new PopoverTestClass ();
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (popover);
        popoverManager.Show (popover);

        // Act
        popoverManager.DispatchKeyDownToActivePopover (Key.N.WithCtrl);

        // Assert
        Assert.Equal (1, popover.NewCommandInvokeCount);
        Assert.Contains (Key.N.WithCtrl, popover.HandledKeys);
    }

    [Fact]
    public void DispatchKeyDown_InactivePopoverGetsHotKey ()
    {
        // Arrange
        // Arrange
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        var activePopover = new PopoverTestClass { Id = "activePopover", HandleNewCommand = false };
        var inactivePopover = new PopoverTestClass { Id = "inactivePopover", HandleNewCommand = true };

        popoverManager.Register (activePopover);
        popoverManager.Show (activePopover);
        popoverManager.Register (inactivePopover);

        // Act
        app.Keyboard.RaiseKeyDownEvent (Key.N.WithCtrl);

        // Assert
        Assert.Equal (1, activePopover.NewCommandInvokeCount);
        Assert.Equal (1, inactivePopover.NewCommandInvokeCount);
        Assert.Contains (Key.N.WithCtrl, activePopover.HandledKeys);
        Assert.NotEmpty (inactivePopover.HandledKeys);
    }

    [Fact]
    public void DispatchKeyDown_InactivePopoverDoesNotGetHotKey_If_Active_Handles ()
    {
        // Arrange
        var activePopover = new PopoverTestClass { Id = "activePopover", HandleNewCommand = true };
        var inactivePopover = new PopoverTestClass { Id = "inactivePopover", HandleNewCommand = false };

        var popoverManager = new ApplicationPopover ();

        popoverManager.Register (activePopover);
        popoverManager.Show (activePopover);
        popoverManager.Register (inactivePopover);

        // Act
        popoverManager.DispatchKeyDownToActivePopover (Key.N.WithCtrl);

        // Assert
        Assert.Equal (1, activePopover.NewCommandInvokeCount);
        Assert.Equal (0, inactivePopover.NewCommandInvokeCount);
        Assert.Empty (inactivePopover.HandledKeys);
    }

    [Fact]
    public void DispatchKeyDown_InactivePopoverDoesNotGetKeyDown ()
    {
        // Arrange
        var activePopover = new PopoverTestClass ();
        var inactivePopover = new PopoverTestClass ();
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (activePopover);

        popoverManager.Show (activePopover);
        popoverManager.Register (inactivePopover);

        // Act
        popoverManager.DispatchKeyDownToActivePopover (Key.A);

        // Assert
        Assert.Contains (Key.A, activePopover.HandledKeys);
        Assert.Empty (inactivePopover.HandledKeys);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Issue 2: DispatchKeyDown crashes if popover is deregistered during iteration.
    ///     Calling DeRegister during DispatchKeyDown iteration should not throw.
    /// </summary>
    [Fact]
    public void DispatchKeyDown_DeRegisterDuringIteration_DoesNotThrow ()
    {
        // Arrange
        ApplicationPopover popoverManager = new ();
        PopoverTestClass first = new () { Id = "first" };
        PopoverTestClass second = new () { Id = "second" };

        popoverManager.Register (first);
        popoverManager.Register (second);

        // Make the first popover deregister the second when it receives a key
        first.OnKeyDownCallback = _ =>
                                  {
                                      popoverManager.DeRegister (second);

                                      return false;
                                  };

        // Act & Assert — should not throw InvalidOperationException
        Exception? ex = Record.Exception (() => popoverManager.DispatchKeyDownToActivePopover (Key.A));
        Assert.Null (ex);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Issue 6: Dispose() doesn't clear _activePopover.
    ///     After Dispose, GetActivePopover should return null.
    /// </summary>
    [Fact]
    public void Dispose_ClearsActivePopover ()
    {
        // Arrange
        PopoverTestClass popover = new ();
        ApplicationPopover popoverManager = new ();
        popoverManager.Register (popover);
        popoverManager.Show (popover);
        Assert.NotNull (popoverManager.GetActivePopover ());

        // Act
        popoverManager.Dispose ();

        // Assert
        Assert.Null (popoverManager.GetActivePopover ());
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Issue 9: Hide() silently no-ops for non-active popovers.
    ///     This is a coverage test documenting existing behavior.
    /// </summary>
    [Fact]
    public void Hide_NonActivePopover_DoesNotAffectActivePopover ()
    {
        // Arrange
        PopoverTestClass popover1 = new () { Id = "popover1" };
        PopoverTestClass popover2 = new () { Id = "popover2" };
        ApplicationPopover popoverManager = new ();
        popoverManager.Register (popover1);
        popoverManager.Register (popover2);
        popoverManager.Show (popover1);

        // Remember popover2's initial visible state (Views default to Visible = true)
        bool initialVisibleState = popover2.Visible;

        // popover2 is registered but not active
        // Act
        popoverManager.Hide (popover2);

        // Assert — popover1 is still active
        Assert.Equal (popover1, popoverManager.GetActivePopover ());

        // popover2 was never shown via Show(), so its Visible state should be unchanged
        // (Hide only affects the active popover, which popover2 is not)
        Assert.Equal (initialVisibleState, popover2.Visible);
    }

    // CoPilot - ChatGPT v4
    [Fact]
    public void LayoutAndDraw_ScreenResize_LayoutsVisiblePopover ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        PopoverTestClass popover = new ()
        {
            App = app,
            Width = 10,
            Height = 5
        };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        app.LayoutAndDraw ();
        popover.LayoutPassCount = 0;

        app.Driver.SetScreenSize (100, 30);

        // Act
        app.LayoutAndDraw ();

        // Assert
        Assert.True (popover.LayoutPassCount > 0);

        app.End (token!);
        top.Dispose ();
    }

    // CoPilot - ChatGPT v4
    [Fact]
    public void LayoutAndDraw_VisibleIdlePopover_DoesNotRaiseLayoutAndDrawComplete ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        PopoverTestClass popover = new ()
        {
            App = app,
            Width = 10,
            Height = 5
        };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        // Clear the initial invalidation caused by showing the popover.
        app.LayoutAndDraw ();

        int layoutAndDrawCompleteCount = 0;
        app.LayoutAndDrawComplete += CountLayoutAndDrawComplete;

        // Act
        app.LayoutAndDraw ();

        // Assert
        Assert.Equal (0, layoutAndDrawCompleteCount);

        app.LayoutAndDrawComplete -= CountLayoutAndDrawComplete;
        app.End (token!);
        top.Dispose ();

        return;

        void CountLayoutAndDrawComplete (object? _, EventArgs __) => layoutAndDrawCompleteCount++;
    }

    // CoPilot - ChatGPT v4
    [Fact]
    public void LayoutAndDraw_VisiblePopoverNeedingDraw_RaisesLayoutAndDrawComplete ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        PopoverTestClass popover = new ()
        {
            App = app,
            Width = 10,
            Height = 5
        };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);
        app.LayoutAndDraw ();

        int layoutAndDrawCompleteCount = 0;
        app.LayoutAndDrawComplete += CountLayoutAndDrawComplete;
        popover.SetNeedsDraw ();

        // Act
        app.LayoutAndDraw ();

        // Assert
        Assert.Equal (1, layoutAndDrawCompleteCount);

        app.LayoutAndDrawComplete -= CountLayoutAndDrawComplete;
        app.End (token!);
        top.Dispose ();

        return;

        void CountLayoutAndDrawComplete (object? _, EventArgs __) => layoutAndDrawCompleteCount++;
    }

    // CoPilot - ChatGPT v4
    [Fact]
    public void LayoutAndDraw_VisiblePopoverNeedingLayout_RaisesLayoutAndDrawComplete ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        PopoverTestClass popover = new ()
        {
            App = app,
            Width = 10,
            Height = 5
        };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);
        app.LayoutAndDraw ();

        int layoutAndDrawCompleteCount = 0;
        app.LayoutAndDrawComplete += CountLayoutAndDrawComplete;
        popover.SetNeedsLayout ();

        // Act
        app.LayoutAndDraw ();

        // Assert
        Assert.Equal (1, layoutAndDrawCompleteCount);

        app.LayoutAndDrawComplete -= CountLayoutAndDrawComplete;
        app.End (token!);
        top.Dispose ();

        return;

        void CountLayoutAndDrawComplete (object? _, EventArgs __) => layoutAndDrawCompleteCount++;
    }

    // CoPilot - ChatGPT v4
    [Fact]
    public void LayoutAndDraw_UnderlyingViewNeedsDraw_RedrawsPopoverWithoutLayout ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };

        View content = new ()
        {
            Width = 10,
            Height = 5
        };
        top.Add (content);

        SessionToken? token = app.Begin (top);

        PopoverTestClass popover = new ()
        {
            App = app,
            Width = 10,
            Height = 5
        };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        app.LayoutAndDraw ();
        popover.DrawCompleteCount = 0;
        popover.LayoutPassCount = 0;

        content.SetNeedsDraw ();

        // Act
        app.LayoutAndDraw ();

        // Assert
        Assert.Equal (1, popover.DrawCompleteCount);
        Assert.Equal (0, popover.LayoutPassCount);

        app.End (token!);
        top.Dispose ();
    }

    public class PopoverTestClass : View, IPopoverView
    {
        public List<Key> HandledKeys { get; } = [];

        public int NewCommandInvokeCount { get; private set; }

        public int DrawCompleteCount { get; set; }

        public int LayoutPassCount { get; set; }

        public bool HandleNewCommand { get; set; }

        /// <summary>
        ///     Optional callback invoked during OnKeyDown. Can be used to test
        ///     modification of popover state during key dispatch.
        /// </summary>
        public Func<Key, bool>? OnKeyDownCallback { get; set; }

        public bool GrabsMouseOnPress
        {
            get;
            set
            {
                field = value;

                if (value && MouseGrabbingSubView is null)
                {
                    MouseGrabbingSubView = new MouseGrabbingView
                    {
                        X = 0,
                        Y = 0,
                        Width = 10,
                        Height = 10,
                        CanFocus = true
                    };
                    Add (MouseGrabbingSubView);
                }
                else if (!value && MouseGrabbingSubView is not null)
                {
                    Remove (MouseGrabbingSubView);
                    MouseGrabbingSubView.Dispose ();
                    MouseGrabbingSubView = null;
                }
            }
        }

        /// <summary>
        ///     Gets the SubView that grabs the mouse, if <see cref="GrabsMouseOnPress"/> is <see langword="true"/>.
        /// </summary>
        public MouseGrabbingView? MouseGrabbingSubView { get; private set; }

        public PopoverTestClass ()
        {
            ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse;
            CanFocus = true;
            AddCommand (Command.New, NewCommandHandler!);
            HotKeyBindings.Add (Key.N.WithCtrl, Command.New);

            AddCommand (Command.Quit, Quit);
            KeyBindings.Add (Application.GetDefaultKey (Command.Quit), Command.Quit);

            return;

            bool? Quit (ICommandContext? ctx)
            {
                if (!Visible)
                {
                    return false;
                }

                Visible = false;

                return true;
            }

            bool? NewCommandHandler (ICommandContext ctx)
            {
                NewCommandInvokeCount++;

                return HandleNewCommand;
            }
        }

        protected override bool OnKeyDown (Key key)
        {
            HandledKeys.Add (key);

            // Call the callback if set, allowing tests to modify state during key dispatch
            if (OnKeyDownCallback is { })
            {
                return OnKeyDownCallback (key);
            }

            return false;
        }

        protected override void OnDrawComplete (DrawContext? context)
        {
            DrawCompleteCount++;
            base.OnDrawComplete (context);
        }

        protected override void OnSubViewLayout (LayoutEventArgs args)
        {
            LayoutPassCount++;
            base.OnSubViewLayout (args);
        }

        /// <inheritdoc/>
        public IRunnable? Owner { get; set; }

        /// <inheritdoc/>
        public Func<Rectangle?>? Anchor { get; set; }

        /// <inheritdoc/>
        public WeakReference<View>? Target { get; set; }

        /// <inheritdoc/>
        public void MakeVisible (Point? idealScreenPosition = null, Rectangle? anchor = null) => Visible = true;
    }

    /// <summary>
    ///     A View that grabs the mouse on left button press and releases it on left button release.
    ///     Used for testing drag scenarios where mouse grab should prevent popover dismissal.
    /// </summary>
    public class MouseGrabbingView : View
    {
        /// <summary>Gets whether this view currently has the mouse grabbed.</summary>
        public bool IsMouseGrabbed { get; private set; }

        /// <inheritdoc/>
        protected override bool OnMouseEvent (Mouse mouse)
        {
            if (mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed))
            {
                App?.Mouse.GrabMouse (this);
                IsMouseGrabbed = true;

                return true;
            }

            if (!mouse.Flags.HasFlag (MouseFlags.LeftButtonReleased))
            {
                return base.OnMouseEvent (mouse);
            }
            App?.Mouse.UngrabMouse ();
            IsMouseGrabbed = false;

            return true;
        }
    }
}

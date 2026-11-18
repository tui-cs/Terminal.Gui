using UnitTests;

namespace ViewBaseTests.MouseTests;

[Trait ("Category", "Input")]
public class MouseTests (ITestOutputHelper output) : TestsAllViews
{
    [Fact]
    public void Default_MouseBindings ()
    {
        var testView = new View ();

        // Default bindings changed to Released (issue #4674)
        Assert.Contains (MouseFlags.LeftButtonReleased, testView.MouseBindings.GetAllFromCommands (Command.Activate));
        Assert.Contains (MouseFlags.LeftButtonReleased | MouseFlags.Ctrl, testView.MouseBindings.GetAllFromCommands (Command.Context));

        Assert.Equal (2, testView.MouseBindings.GetBindings ().Count ());

        testView = new ()
        {
            MouseHoldRepeat = MouseFlags.LeftButtonReleased
        };

        // With MouseHoldRepeat set, the Released binding is used for repeat behavior
        Assert.Contains (MouseFlags.LeftButtonReleased, testView.MouseBindings.GetAllFromCommands (Command.Activate));
        Assert.Contains (MouseFlags.LeftButtonReleased | MouseFlags.Ctrl, testView.MouseBindings.GetAllFromCommands (Command.Context));

        Assert.Equal (2, testView.MouseBindings.GetBindings ().Count ());

    }

    [Fact]
    public void LeftButtonClicked_OnSubView_Does_Not_RaiseAcceptingEvent ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 20,
            Height = 20
        };

        View subView = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10
        };

        superView.Add (subView);

        int acceptingCount = 0;
        subView.Accepting += (_, _) => acceptingCount++;

        Mouse mouse = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.LeftButtonClicked
        };

        // Act
        subView.NewMouseEvent (mouse);

        // Assert
        Assert.Equal (0, acceptingCount);

        subView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void LeftButtonReleased_RaisesActivating_WhenCanFocus ()
    {
        // Arrange
        View superView = new () { CanFocus = true, Width = 20, Height = 20 };
        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            CanFocus = true
        };

        superView.Add (view);

        int acceptingCount = 0;
        view.Activating += (_, _) => acceptingCount++;

        // Act - Press then Release (default behavior changed to Released, issue #4674)
        Mouse pressedMouse = new ()
        {
            Position = new (5, 5),
            Flags = MouseFlags.LeftButtonPressed
        };
        view.NewMouseEvent (pressedMouse);
        Assert.Equal (0, acceptingCount); // Should NOT activate on press

        Mouse releasedMouse = new ()
        {
            Position = new (5, 5),
            Flags = MouseFlags.LeftButtonReleased
        };
        view.NewMouseEvent (releasedMouse);

        // Assert
        Assert.Equal (1, acceptingCount);

        view.Dispose ();
        superView.Dispose ();
    }

    // BUGUBG: This is a bogus test now. LeftButtonClicked should not set focus; Release should.
    [Theory]
    [InlineData (false, false, false)]
    [InlineData (true, false, true)]
    [InlineData (true, true, true)]
    public void LeftButtonClicked_SetsFocus_If_CanFocus (bool canFocus, bool setFocus, bool expectedHasFocus)
    {
        var superView = new View { CanFocus = true, Height = 1, Width = 15 };
        var focusedView = new View { CanFocus = true, Width = 1, Height = 1 };
        var testView = new View { CanFocus = canFocus, X = 4, Width = 4, Height = 1 };
        superView.Add (focusedView, testView);

        focusedView.SetFocus ();

        Assert.True (superView.HasFocus);
        Assert.True (focusedView.HasFocus);
        Assert.False (testView.HasFocus);

        if (setFocus)
        {
            testView.SetFocus ();
        }

        testView.NewMouseEvent (new () { Timestamp = DateTime.Now, Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        testView.NewMouseEvent (new () { Timestamp = DateTime.Now, Position = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased });
        testView.NewMouseEvent (new () { Timestamp = DateTime.Now, Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });
        Assert.True (superView.HasFocus);
        Assert.Equal (expectedHasFocus, testView.HasFocus);
    }


    [Theory]
    [InlineData (false, false, false)]
    [InlineData (true, false, true)]
    [InlineData (true, true, true)]
    public void LeftButtonPressed_SetsFocus_If_CanFocus (bool canFocus, bool setFocus, bool expectedHasFocus)
    {
        var superView = new View { CanFocus = true, Height = 1, Width = 15 };
        var focusedView = new View { CanFocus = true, Width = 1, Height = 1 };
        var testView = new View { CanFocus = canFocus, X = 4, Width = 4, Height = 1, MouseHighlightStates = MouseState.Pressed };
        superView.Add (focusedView, testView);

        focusedView.SetFocus ();

        Assert.True (superView.HasFocus);
        Assert.True (focusedView.HasFocus);
        Assert.False (testView.HasFocus);

        if (setFocus)
        {
            testView.SetFocus ();
        }

        // Note: Pressed still sets focus (via HandleAutoGrabPress), even though activation moved to Released
        testView.NewMouseEvent (new () { Timestamp = DateTime.Now, Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        Assert.True (superView.HasFocus);
        Assert.Equal (expectedHasFocus, testView.HasFocus);
    }


    [Theory]
    [InlineData (false, false, 1)]
    [InlineData (true, false, 1)]
    [InlineData (true, true, 1)]
    public void LeftButtonReleased_Raises_Activating (bool canFocus, bool setFocus, int expectedAcceptingCount)
    {
        var superView = new View { CanFocus = true, Height = 1, Width = 15 };
        var focusedView = new View { CanFocus = true, Width = 1, Height = 1 };
        var testView = new View { CanFocus = canFocus, X = 4, Width = 4, Height = 1 };
        superView.Add (focusedView, testView);

        focusedView.SetFocus ();

        Assert.True (superView.HasFocus);
        Assert.True (focusedView.HasFocus);
        Assert.False (testView.HasFocus);

        if (setFocus)
        {
            testView.SetFocus ();
        }

        var acceptingCount = 0;
        testView.Activating += (sender, args) => acceptingCount++;

        // Default behavior changed to Released (issue #4674)
        testView.NewMouseEvent (new () { Timestamp = DateTime.Now, Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        Assert.Equal (0, acceptingCount); // Should NOT activate on press

        testView.NewMouseEvent (new () { Timestamp = DateTime.Now, Position = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased });
        Assert.True (superView.HasFocus);
        Assert.Equal (expectedAcceptingCount, acceptingCount);
    }

    [Theory]
    [InlineData (MouseFlags.WheeledUp | MouseFlags.Ctrl, MouseFlags.WheeledLeft)]
    [InlineData (MouseFlags.WheeledDown | MouseFlags.Ctrl, MouseFlags.WheeledRight)]
    public void WheeledLeft_WheeledRight (MouseFlags mouseFlags, MouseFlags expectedMouseFlagsFromEvent)
    {
        var mouseFlagsFromEvent = MouseFlags.None;
        var view = new View ();
        view.MouseEvent += (s, e) => mouseFlagsFromEvent = e.Flags;

        view.NewMouseEvent (new () { Timestamp = DateTime.Now, Flags = mouseFlags });
        Assert.Equal (mouseFlagsFromEvent, expectedMouseFlagsFromEvent);
    }

    [Fact]
    public void NewMouseEvent_Invokes_MouseEvent_Properly ()
    {
        View view = new ()
        {
            Width = 1,
            Height = 1
        };
        var mouseEventInvoked = false;

        view.MouseEvent += (s, e) =>
                           {
                               mouseEventInvoked = true;
                               e.Handled = true;
                           };

        Mouse me = new () { Timestamp = DateTime.Now };
        view.NewMouseEvent (me);
        Assert.True (mouseEventInvoked);
        Assert.True (me.Handled);

        view.Dispose ();
    }

    [Fact]
    public void NewMouseEvent_DoubleClick_Pattern_MouseEvent_Raised_Correctly ()
    {
        View view = new ()
        {
            Visible = true,
            Enabled = true,
            Width = 1,
            Height = 1
        };
        int mouseEventCount = 0;

        view.MouseEvent += (s, e) =>
                           {
                               mouseEventCount++;
                              // e.Handled = true;
                           };

        Mouse mouseEventPressed1 = new () { Timestamp = DateTime.Now, Flags = MouseFlags.LeftButtonPressed };
        view.NewMouseEvent (mouseEventPressed1);
        Mouse mouseEventReleased1 = new () { Timestamp = DateTime.Now, Flags = MouseFlags.LeftButtonReleased };
        view.NewMouseEvent (mouseEventReleased1);
        Mouse mouseEventClicked = new () { Timestamp = DateTime.Now, Flags = MouseFlags.LeftButtonClicked };
        view.NewMouseEvent (mouseEventClicked);

        Mouse mouseEventPressed2 = new () { Timestamp = DateTime.Now, Flags = MouseFlags.LeftButtonPressed };
        view.NewMouseEvent (mouseEventPressed2);
        Mouse mouseEventReleased2 = new () { Timestamp = DateTime.Now, Flags = MouseFlags.LeftButtonReleased };
        view.NewMouseEvent (mouseEventReleased2);
        Mouse mouseEventDoubleClicked = new () { Timestamp = DateTime.Now, Flags = MouseFlags.LeftButtonDoubleClicked };
        view.NewMouseEvent (mouseEventDoubleClicked);

        Assert.Equal (6, mouseEventCount);

        view.Dispose ();
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_NewMouseEvent_Enabled_False_Does_Not_Set_Handled (Type viewType)
    {
        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        view.Enabled = false;
        var me = new Mouse () { Timestamp = DateTime.Now };
        view.NewMouseEvent (me);
        Assert.False (me.Handled);
        view.Dispose ();
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_NewMouseEvent_Clicked_Enabled_False_Does_Not_Set_Handled (Type viewType)
    {
        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        view.Enabled = false;

        var me = new Mouse
        {
            Timestamp = DateTime.Now,
            Flags = MouseFlags.LeftButtonClicked
        };
        view.NewMouseEvent (me);
        Assert.False (me.Handled);
        view.Dispose ();
    }

    [Fact]
    public void NewMouseEnterEvent_SubView_CanFocus_False_With_Two_Overlapped_Views_SetFocus_To_SuperView ()
    {
        // This view will be behind and start with CanFocus = false and Enabled = false, proving that the focus can't be set to it
        View superView1 = new () { CanFocus = false, Enabled = false, Width = 20, Height = 3 };
        View view1 = new () { CanFocus = false, Width = 20, Height = 1, MouseHighlightStates = MouseState.Pressed };
        superView1.Add (view1);
        View superView2 = new () { CanFocus = true, X = 10, Width = 20, Height = 3 };
        View view2 = new ()
        {
            CanFocus = false, Width = 20, Height = 1, Y = 1,
            MouseHighlightStates = MouseState.Pressed
        };
        superView2.Add (view2);
        Runnable runnable = new ();
        runnable.Add (superView1, superView2);
        runnable.BeginInit ();
        runnable.EndInit ();

        // Set focus to superView2
        superView2.SetFocus ();

        Assert.False (superView1.HasFocus);
        Assert.False (view1.HasFocus);
        Assert.True (superView2.HasFocus);
        Assert.False (view2.HasFocus);
        Assert.Equal (superView2, runnable.MostFocused);

        view1.NewMouseEvent (new Mouse { Timestamp = DateTime.Now, Flags = MouseFlags.LeftButtonPressed });

        Assert.False (superView1.HasFocus);
        Assert.False (view1.HasFocus);
        Assert.True (superView2.HasFocus);
        Assert.False (view2.HasFocus);
        Assert.Equal (superView2, runnable.MostFocused);

        // Now set the behind view to CanFocus = true, and verify that it doesn't get focus yet because Enabled is still false
        superView1.CanFocus = true;

        view1.NewMouseEvent (new Mouse { Timestamp = DateTime.Now, Flags = MouseFlags.LeftButtonPressed });

        Assert.False (superView1.HasFocus);
        Assert.False (view1.HasFocus);
        Assert.True (superView2.HasFocus);
        Assert.False (view2.HasFocus);
        Assert.Equal (superView2, runnable.MostFocused);

        // Now set the behind view to Enabled = true, and verify that it gets focus on mouse event
        superView1.Enabled = true;

        view1.NewMouseEvent (new Mouse { Timestamp = DateTime.Now, Flags = MouseFlags.LeftButtonPressed });

        Assert.True (superView1.HasFocus);
        Assert.False (view1.HasFocus);
        Assert.False (superView2.HasFocus);
        Assert.False (view2.HasFocus);
        Assert.Equal (superView1, runnable.MostFocused);

        // Now set focus back to the front view, and verify that it gets focus on mouse event
        view2.NewMouseEvent (new Mouse { Timestamp = DateTime.Now, Flags = MouseFlags.LeftButtonPressed });

        Assert.False (superView1.HasFocus);
        Assert.False (view1.HasFocus);
        Assert.True (superView2.HasFocus);
        Assert.False (view2.HasFocus);
        Assert.Equal (superView2, runnable.MostFocused);

        superView1.Dispose ();
        superView2.Dispose ();
    }
}

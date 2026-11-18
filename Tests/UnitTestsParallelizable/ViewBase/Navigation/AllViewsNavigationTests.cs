using UnitTests;

namespace ViewBaseTests.Navigation;

public class AllViewsNavigationTests (ITestOutputHelper output) : TestsAllViews
{
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_AtLeastOneNavKey_Advances (Type viewType)
    {
        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (!view.CanFocus)
        {
            output.WriteLine ($"Ignoring {viewType} - It can't focus.");

            return;
        }

        if (view is IDesignable designable)
        {
            // designable.EnableForDesign ();
        }

        IApplication app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        View otherView = new () { Id = "otherView", CanFocus = true, TabStop = view.TabStop == TabBehavior.NoStop ? TabBehavior.TabStop : view.TabStop };

        app.TopRunnableView!.Add (view, otherView);

        // Start with the focus on our test view
        view.SetFocus ();

        Key [] navKeys = [Key.Tab, Key.Tab.WithShift, Key.CursorUp, Key.CursorDown, Key.CursorLeft, Key.CursorRight];

        if (view.TabStop == TabBehavior.TabGroup)
        {
            navKeys = [Key.F6, Key.F6.WithShift];
        }

        var left = false;

        foreach (Key key in navKeys)
        {
            switch (view.TabStop)
            {
                case TabBehavior.TabStop:
                case TabBehavior.NoStop:
                case TabBehavior.TabGroup:
                    app.Keyboard.RaiseKeyDownEvent (key);

                    if (view.HasFocus)
                    {
                        // Try once more (HexView)
                        app.Keyboard.RaiseKeyDownEvent (key);
                    }

                    break;

                default:
                    app.Keyboard.RaiseKeyDownEvent (Key.Tab);

                    break;
            }

            if (!view.HasFocus)
            {
                left = true;
                output.WriteLine ($"{view.GetType ().Name} - {key} Left.");

                break;
            }

            output.WriteLine ($"{view.GetType ().Name} - {key} did not Leave.");
        }

        Assert.True (left);
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_HasFocus_Changed_Event (Type viewType)
    {
        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (!view.CanFocus)
        {
            output.WriteLine ($"Ignoring {viewType} - It can't focus.");

            return;
        }

        if (view is IRunnable)
        {
            output.WriteLine ($"Ignoring {viewType} - It's an IRunnable");

            return;
        }

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }

        IApplication app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        View otherView = new () { Id = "otherView", CanFocus = true, TabStop = view.TabStop == TabBehavior.NoStop ? TabBehavior.TabStop : view.TabStop };

        var hasFocusTrue = 0;
        var hasFocusFalse = 0;

        // Ensure the view is Visible
        view.Visible = true;
        view.HasFocus = false;

        view.HasFocusChanged += (s, e) =>
                                {
                                    if (e.NewValue)
                                    {
                                        hasFocusTrue++;
                                    }
                                    else
                                    {
                                        hasFocusFalse++;
                                    }
                                };

        Assert.Equal (0, hasFocusTrue);
        Assert.Equal (0, hasFocusFalse);

        app.TopRunnableView!.Add (view, otherView);
        Assert.False (view.HasFocus);
        Assert.True (otherView.HasFocus);

        Assert.Equal (1, hasFocusTrue);
        Assert.Equal (1, hasFocusFalse);

        // Start with the focus on our test view
        view.SetFocus ();
        Assert.True (view.HasFocus);

        Assert.Equal (2, hasFocusTrue);
        Assert.Equal (1, hasFocusFalse);

        // Use keyboard to navigate to next view (otherView).
        var tries = 0;

        while (view.HasFocus)
        {
            if (++tries > 20)
            {
                Assert.Fail ($"{view} is not leaving after {tries} attempts.");
            }

            switch (view.TabStop)
            {
                case null:
                case TabBehavior.NoStop:
                case TabBehavior.TabStop:
                    if (app.Keyboard.RaiseKeyDownEvent (Key.Tab))
                    {
                        if (view.HasFocus)
                        {
                            // Try another nav key (e.g. for TextView that eats Tab)
                            app.Keyboard.RaiseKeyDownEvent (Key.CursorDown);
                        }
                    }
                    ;

                    break;

                case TabBehavior.TabGroup:
                    app.Keyboard.RaiseKeyDownEvent (Key.F6);

                    break;

                default:
                    throw new ArgumentOutOfRangeException ();
            }
        }

        Assert.Equal (2, hasFocusTrue);
        Assert.Equal (2, hasFocusFalse);

        Assert.False (view.HasFocus);
        Assert.True (otherView.HasFocus);

        // Now navigate back to our test view
        switch (view.TabStop)
        {
            case TabBehavior.NoStop:
                view.SetFocus ();

                break;

            case TabBehavior.TabStop:
                app.Keyboard.RaiseKeyDownEvent (Key.Tab);

                break;

            case TabBehavior.TabGroup:
                if (!app.Keyboard.RaiseKeyDownEvent (Key.F6))
                {
                    view.SetFocus ();
                }

                break;

            case null:
                app.Keyboard.RaiseKeyDownEvent (Key.Tab);

                break;

            default:
                throw new ArgumentOutOfRangeException ();
        }

        Assert.Equal (3, hasFocusTrue);
        Assert.Equal (2, hasFocusFalse);

        Assert.True (view.HasFocus);
        Assert.False (otherView.HasFocus);

        // Cache state because Shutdown has side effects.
        // Also ensures other tests can continue running if there's a fail
        bool otherViewHasFocus = otherView.HasFocus;
        bool viewHasFocus = view.HasFocus;

        int enterCount = hasFocusTrue;
        int leaveCount = hasFocusFalse;

        Assert.False (otherViewHasFocus);
        Assert.True (viewHasFocus);

        Assert.Equal (3, enterCount);
        Assert.Equal (2, leaveCount);
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Visible_False_No_HasFocus_Events (Type viewType)
    {
        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (!view.CanFocus)
        {
            output.WriteLine ($"Ignoring {viewType} - It can't focus.");

            return;
        }

        if (view is IRunnable)
        {
            output.WriteLine ($"Ignoring {viewType} - It's an IRunnable");

            return;
        }

        IApplication? app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        View otherView = new () { CanFocus = true };

        view.Visible = false;

        var hasFocusChangingCount = 0;
        var hasFocusChangedCount = 0;

        view.HasFocusChanging += (s, e) => hasFocusChangingCount++;
        view.HasFocusChanged += (s, e) => hasFocusChangedCount++;

        app.TopRunnableView!.Add (view, otherView);

        // Start with the focus on our test view
        view.SetFocus ();

        Assert.Equal (0, hasFocusChangingCount);
        Assert.Equal (0, hasFocusChangedCount);

        app.Keyboard.RaiseKeyDownEvent (Key.Tab);

        Assert.Equal (0, hasFocusChangingCount);
        Assert.Equal (0, hasFocusChangedCount);

        app.Keyboard.RaiseKeyDownEvent (Key.F6);

        Assert.Equal (0, hasFocusChangingCount);
        Assert.Equal (0, hasFocusChangedCount);
    }

    [Fact]
    public void Application_Begin_FocusesDeepest ()
    {
        var win1 = new Window { Id = "win1", Width = 10, Height = 1 };
        var view1 = new View { Id = "view1", Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };
        var win2 = new Window { Id = "win2", Y = 6, Width = 10, Height = 1 };
        var view2 = new View { Id = "view2", Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };
        win2.Add (view2);
        win1.Add (view1, win2);

        IApplication app = Application.Create ();
        app.Begin (win1);

        Assert.True (win1.HasFocus);
        Assert.True (view1.HasFocus);
        Assert.False (win2.HasFocus);
        Assert.False (view2.HasFocus);
    }

    // View.Focused & View.MostFocused tests

    // View.Focused - No subviews
    [Fact]
    public void Focused_NoSubViews ()
    {
        var view = new View ();
        Assert.Null (view.Focused);

        view.CanFocus = true;
        view.SetFocus ();
    }

    [Fact]
    public void GetMostFocused_NoSubViews_Returns_This ()
    {
        var view = new View ();
        Assert.Null (view.Focused);

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Equal (view, view.MostFocused);
    }

    [Fact]
    public void GetMostFocused_Returns_Most ()
    {
        var view = new View { Id = "view", CanFocus = true };

        var subview = new View { Id = "subview", CanFocus = true };

        view.Add (subview);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subview.HasFocus);
        Assert.Equal (subview, view.MostFocused);

        var subview2 = new View { Id = "subview2", CanFocus = true };

        view.Add (subview2);
        Assert.Equal (subview2, view.MostFocused);
    }
}

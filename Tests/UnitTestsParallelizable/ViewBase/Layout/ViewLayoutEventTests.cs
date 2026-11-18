#nullable enable

namespace ViewBaseTests.Layout;

public class ViewLayoutEventTests
{
    [Fact]
    public void View_WidthChanging_Event_Fires ()
    {
        var view = new View ();
        bool eventFired = false;
        Dim? oldValue = null;
        Dim? newValue = null;

        view.WidthChanging += (sender, args) =>
        {
            eventFired = true;
            oldValue = args.CurrentValue;
            newValue = args.NewValue;
        };

        view.Width = 10;

        Assert.True (eventFired);
        Assert.NotNull (oldValue);
        Assert.NotNull (newValue);
    }

    [Fact]
    public void View_WidthChanged_Event_Fires ()
    {
        var view = new View ();
        bool eventFired = false;
        Dim? oldValue = null;
        Dim? newValue = null;

        view.WidthChanged += (sender, args) =>
        {
            eventFired = true;
            oldValue = args.OldValue;
            newValue = args.NewValue;
        };

        view.Width = 10;

        Assert.True (eventFired);
        Assert.NotNull (oldValue);
        Assert.NotNull (newValue);
    }

    [Fact]
    public void View_WidthChanging_CanCancel ()
    {
        var view = new View ();
        Dim? originalWidth = view.Width;

        view.WidthChanging += (sender, args) =>
        {
            args.Handled = true; // Cancel the change
        };

        view.Width = 10;

        // Width should not have changed
        Assert.Equal (originalWidth, view.Width);
    }

    [Fact]
    public void View_WidthChanging_CanModify ()
    {
        var view = new View ();

        view.WidthChanging += (sender, args) =>
        {
            // Modify the proposed value
            args.NewValue = 20;
        };

        view.Width = 10;

        // Width should be 20 (the modified value), not 10
        var container = new View { Width = 50, Height = 20 };
        container.Add (view);
        container.Layout ();
        Assert.Equal (20, view.Frame.Width);
    }

    [Fact]
    public void View_HeightChanging_Event_Fires ()
    {
        var view = new View ();
        bool eventFired = false;
        Dim? oldValue = null;
        Dim? newValue = null;

        view.HeightChanging += (sender, args) =>
        {
            eventFired = true;
            oldValue = args.CurrentValue;
            newValue = args.NewValue;
        };

        view.Height = 10;

        Assert.True (eventFired);
        Assert.NotNull (oldValue);
        Assert.NotNull (newValue);
    }

    [Fact]
    public void View_HeightChanged_Event_Fires ()
    {
        var view = new View ();
        bool eventFired = false;
        Dim? oldValue = null;
        Dim? newValue = null;

        view.HeightChanged += (sender, args) =>
        {
            eventFired = true;
            oldValue = args.OldValue;
            newValue = args.NewValue;
        };

        view.Height = 10;

        Assert.True (eventFired);
        Assert.NotNull (oldValue);
        Assert.NotNull (newValue);
    }

    [Fact]
    public void View_HeightChanging_CanCancel ()
    {
        var view = new View ();
        Dim? originalHeight = view.Height;

        view.HeightChanging += (sender, args) =>
        {
            args.Handled = true; // Cancel the change
        };

        view.Height = 10;

        // Height should not have changed
        Assert.Equal (originalHeight, view.Height);
    }

    [Fact]
    public void View_HeightChanging_CanModify ()
    {
        var view = new View ();

        view.HeightChanging += (sender, args) =>
        {
            // Modify the proposed value
            args.NewValue = 20;
        };

        view.Height = 10;

        // Height should be 20 (the modified value), not 10
        var container = new View { Width = 50, Height = 40 };
        container.Add (view);
        container.Layout ();
        Assert.Equal (20, view.Frame.Height);
    }

    [Fact]
    public void View_OnWidthChanging_CanCancel ()
    {
        var testView = new TestView ();
        testView.CancelWidthChange = true;
        Dim? originalWidth = testView.Width;

        testView.Width = 10;

        // Width should not have changed
        Assert.Equal (originalWidth, testView.Width);
    }

    [Fact]
    public void View_OnHeightChanging_CanCancel ()
    {
        var testView = new TestView ();
        testView.CancelHeightChange = true;
        Dim originalHeight = testView.Height;

        testView.Height = 10;

        // Height should not have changed
        Assert.Equal (originalHeight, testView.Height);
    }

    [Fact]
    public void View_WidthChanged_BackingFieldSetBeforeEvent ()
    {
        var view = new View ();
        Dim? widthInChangedEvent = null;

        view.WidthChanged += (sender, args) =>
        {
            // The backing field should already be set when Changed event fires
            widthInChangedEvent = view.Width;
        };

        view.Width = 25;

        // The width seen in the Changed event should be the new value
        var container = new View { Width = 50, Height = 20 };
        container.Add (view);
        container.Layout ();
        Assert.Equal (25, view.Frame.Width);
        Assert.Equal (widthInChangedEvent, view.Frame.Width);
    }

    [Fact]
    public void View_HeightChanged_BackingFieldSetBeforeEvent ()
    {
        var view = new View ();
        Dim? heightInChangedEvent = null;

        view.HeightChanged += (sender, args) =>
        {
            // The backing field should already be set when Changed event fires
            heightInChangedEvent = view.Height;
        };

        view.Height = 30;

        // The height seen in the Changed event should be the new value
        var container = new View { Width = 50, Height = 40 };
        container.Add (view);
        container.Layout ();
        Assert.Equal (30, view.Frame.Height);
        Assert.Equal (heightInChangedEvent, view.Frame.Height);
    }

    [Fact]
    public void View_SubViewLayout_SubViewsLaidOut_Events_Fires_EvenWidthOrHeightIsZero ()
    {
        var container = new View () { Width = 20, Height = 10 };
        var view = new View () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single };
        container.Add (view);
        bool eventSubViewLayoutFired = false;
        bool eventSubViewsLaidOutFired = false;
        Size? oldValue = null;

        view.SubViewLayout += (_, args) =>
                              {
                                  eventSubViewLayoutFired = true;
                                  oldValue = args.OldContentSize;
                              };

        view.SubViewsLaidOut += (_, args) =>
                                {
                                    eventSubViewsLaidOutFired = true;
                                    oldValue = args.OldContentSize;
                                };

        container.Layout ();

        Assert.True (eventSubViewLayoutFired);
        Assert.True (eventSubViewsLaidOutFired);
        Assert.Equal (new Size (18, 8), oldValue);

        eventSubViewLayoutFired = false;
        eventSubViewsLaidOutFired = false;
        oldValue = null;

        container.Width = 1;
        Assert.True (eventSubViewLayoutFired);
        Assert.True (eventSubViewsLaidOutFired);
        Assert.Equal (new Size (0, 8), oldValue);

        eventSubViewLayoutFired = false;
        eventSubViewsLaidOutFired = false;
        oldValue = null;

        container.Height = 1;
        Assert.True (eventSubViewLayoutFired);
        Assert.True (eventSubViewsLaidOutFired);
        Assert.Equal (new Size (0, 0), oldValue);
    }

    // The following tests lock the Width/Height vs Frame event contract (#5498):
    // WidthChanged/HeightChanged observe declarative Dim assignment only; FrameChanged observes
    // resolved-size changes from any cause (Frame setter, declarative assignment, or a layout pass).

    // Claude - Opus 4.8
    [Fact]
    public void View_FrameSetter_DoesNotRaise_WidthChanged ()
    {
        View view = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        bool fired = false;

        view.WidthChanged += (_, _) => fired = true;

        view.Frame = new (0, 0, 10, 5);

        // The Frame setter converts Width to Dim.Absolute as bookkeeping, but that is not a
        // declarative Width assignment, so WidthChanged must not fire.
        Assert.False (fired);
        Assert.Equal (Dim.Absolute (10), view.Width);
    }

    // Claude - Opus 4.8
    [Fact]
    public void View_FrameSetter_DoesNotRaise_HeightChanged ()
    {
        View view = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        bool fired = false;

        view.HeightChanged += (_, _) => fired = true;

        view.Frame = new (0, 0, 10, 5);

        Assert.False (fired);
        Assert.Equal (Dim.Absolute (5), view.Height);
    }

    // Claude - Opus 4.8
    [Fact]
    public void View_FrameSetter_Raises_FrameChanged ()
    {
        View view = new ();
        Rectangle? observed = null;

        view.FrameChanged += (_, args) => observed = args.Value;

        view.Frame = new (1, 2, 10, 5);

        Assert.Equal (new Rectangle (1, 2, 10, 5), observed);
    }

    [Fact]
    public void View_FrameSetter_DoesNotRaise_WidthHeightChangingChanged_Or_OnWidthHeightChangingChanged ()
    {
        EventProbeView view = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        view.ResetCounts ();
        int changingEvents = 0;
        int changedEvents = 0;

        view.WidthChanging += (_, _) => changingEvents++;
        view.HeightChanging += (_, _) => changingEvents++;
        view.WidthChanged += (_, _) => changedEvents++;
        view.HeightChanged += (_, _) => changedEvents++;

        view.Frame = new (0, 0, 10, 5);

        Assert.Equal (0, changingEvents);
        Assert.Equal (0, changedEvents);
        Assert.Equal (0, view.WidthChangingCount);
        Assert.Equal (0, view.HeightChangingCount);
        Assert.Equal (0, view.WidthChangedCount);
        Assert.Equal (0, view.HeightChangedCount);
    }

    // Claude - Opus 4.8
    [Fact]
    public void View_LayoutDrivenResize_DoesNotRaise_WidthChanged_Or_HeightChanged ()
    {
        View container = new () { Width = 20, Height = 10 };
        View child = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        container.Add (child);
        container.Layout ();
        Assert.Equal (20, child.Frame.Width);

        bool fired = false;
        child.WidthChanged += (_, _) => fired = true;
        child.HeightChanged += (_, _) => fired = true;

        // Growing the container re-resolves the child's Dim.Fill () to a new absolute size during
        // layout. The declarative Width/Height (still Dim.Fill ()) does not change, so no event fires.
        container.Width = 30;
        container.Height = 14;
        container.Layout ();

        Assert.False (fired);
        Assert.Equal (30, child.Frame.Width);
        Assert.Equal (14, child.Frame.Height);
        Assert.Equal (Dim.Fill (), child.Width);
        Assert.Equal (Dim.Fill (), child.Height);
    }

    [Fact]
    public void View_LayoutDrivenResize_DoesNotRaise_WidthHeightChangingChanged_Or_OnWidthHeightChangingChanged ()
    {
        View container = new () { Width = 20, Height = 10 };
        EventProbeView child = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        container.Add (child);
        container.Layout ();
        child.ResetCounts ();

        int changingEvents = 0;
        int changedEvents = 0;
        child.WidthChanging += (_, _) => changingEvents++;
        child.HeightChanging += (_, _) => changingEvents++;
        child.WidthChanged += (_, _) => changedEvents++;
        child.HeightChanged += (_, _) => changedEvents++;

        container.Width = 30;
        container.Height = 14;
        container.Layout ();

        Assert.Equal (0, changingEvents);
        Assert.Equal (0, changedEvents);
        Assert.Equal (0, child.WidthChangingCount);
        Assert.Equal (0, child.HeightChangingCount);
        Assert.Equal (0, child.WidthChangedCount);
        Assert.Equal (0, child.HeightChangedCount);
    }

    // Claude - Opus 4.8
    [Fact]
    public void View_LayoutDrivenResize_Raises_FrameChanged ()
    {
        View container = new () { Width = 20, Height = 10 };
        View child = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        container.Add (child);
        container.Layout ();

        bool fired = false;
        child.FrameChanged += (_, _) => fired = true;

        container.Width = 30;
        container.Layout ();

        Assert.True (fired);
        Assert.Equal (30, child.Frame.Width);
    }

    // Claude - Opus 4.8
    [Fact]
    public void View_DeclarativeWidthAssignment_Raises_FrameChanged_AfterLayout ()
    {
        View container = new () { Width = 50, Height = 20 };
        View child = new () { Width = 10, Height = 5 };
        container.Add (child);
        container.Layout ();

        bool fired = false;
        child.FrameChanged += (_, _) => fired = true;

        child.Width = 25;
        container.Layout ();

        Assert.True (fired);
        Assert.Equal (25, child.Frame.Width);
    }

    private class TestView : View
    {
        public bool CancelWidthChange { get; set; }
        public bool CancelHeightChange { get; set; }

        protected override bool OnWidthChanging (ValueChangingEventArgs<Dim> args)
        {
            return CancelWidthChange;
        }

        protected override bool OnHeightChanging (ValueChangingEventArgs<Dim> args)
        {
            return CancelHeightChange;
        }
    }

    private class EventProbeView : View
    {
        public int HeightChangedCount { get; private set; }
        public int HeightChangingCount { get; private set; }
        public int WidthChangedCount { get; private set; }
        public int WidthChangingCount { get; private set; }

        public void ResetCounts ()
        {
            HeightChangedCount = 0;
            HeightChangingCount = 0;
            WidthChangedCount = 0;
            WidthChangingCount = 0;
        }

        protected override void OnHeightChanged (ValueChangedEventArgs<Dim> args)
        {
            HeightChangedCount++;
        }

        protected override bool OnHeightChanging (ValueChangingEventArgs<Dim> args)
        {
            HeightChangingCount++;

            return false;
        }

        protected override void OnWidthChanged (ValueChangedEventArgs<Dim> args)
        {
            WidthChangedCount++;
        }

        protected override bool OnWidthChanging (ValueChangingEventArgs<Dim> args)
        {
            WidthChangingCount++;

            return false;
        }
    }
}

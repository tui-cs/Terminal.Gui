namespace ViewBaseTests.Hierarchy;

public class SubViewTests
{
    [Fact]
    public void SuperViewChanged_Raised_On_Add ()
    {
        var super = new View ();
        var sub = new View ();

        var superRaisedCount = 0;
        var subRaisedCount = 0;

        super.SuperViewChanged += (s, e) => { superRaisedCount++; };

        sub.SuperViewChanged += (s, e) =>
                                {
                                    if (sub.SuperView is { })
                                    {
                                        subRaisedCount++;
                                    }
                                };

        super.Add (sub);
        Assert.True (super.SubViews.Count == 1);
        Assert.Equal (super, sub.SuperView);
        Assert.Equal (0, superRaisedCount);
        Assert.Equal (1, subRaisedCount);
    }

    [Fact]
    public void SuperViewChanged_Raised_On_Remove ()
    {
        var super = new View ();
        var sub = new View ();

        var superRaisedCount = 0;
        var subRaisedCount = 0;

        super.SuperViewChanged += (s, e) => { superRaisedCount++; };

        sub.SuperViewChanged += (s, e) =>
                                {
                                    if (sub.SuperView is null)
                                    {
                                        subRaisedCount++;
                                    }
                                };

        super.Add (sub);
        Assert.True (super.SubViews.Count == 1);
        Assert.Equal (super, sub.SuperView);
        Assert.Equal (0, superRaisedCount);
        Assert.Equal (0, subRaisedCount);

        super.Remove (sub);
        Assert.Empty (super.SubViews);
        Assert.NotEqual (super, sub.SuperView);
        Assert.Equal (0, superRaisedCount);
        Assert.Equal (1, subRaisedCount);
    }

    [Fact]
    public void SuperView_Set_On_Add_Remove ()
    {
        var superView = new View ();
        var view = new View ();
        Assert.Null (view.SuperView);
        superView.Add (view);
        Assert.Equal (superView, view.SuperView);
        superView.Remove (view);
        Assert.Null (view.SuperView);
    }

    // TODO: Consider a feature that will change the ContentSize to fit the subviews.
    [Fact]
    public void Add_Does_Not_Impact_ContentSize ()
    {
        var view = new View ();
        view.SetContentSize (new Size (1, 1));

        var subview = new View { X = 10, Y = 10 };

        Assert.Equal (new Size (1, 1), view.GetContentSize ());
        view.Add (subview);
        Assert.Equal (new Size (1, 1), view.GetContentSize ());
    }

    [Fact]
    public void Add_Margin_Throws ()
    {
        View view = new ();
        Assert.Throws<InvalidOperationException> (() => view.Margin.GetOrCreateView ().Add (new View ()));
    }

    [Fact]
    public void Remove_Does_Not_Impact_ContentSize ()
    {
        var view = new View ();
        view.SetContentSize (new Size (1, 1));

        var subview = new View { X = 10, Y = 10 };

        Assert.Equal (new Size (1, 1), view.GetContentSize ());
        view.Add (subview);
        Assert.Equal (new Size (1, 1), view.GetContentSize ());

        view.SetContentSize (new Size (5, 5));
        Assert.Equal (new Size (5, 5), view.GetContentSize ());

        view.Remove (subview);
        Assert.Equal (new Size (5, 5), view.GetContentSize ());
    }

    [Theory]
    [InlineData (ViewArrangement.Fixed)]
    [InlineData (ViewArrangement.Overlapped)]
    public void MoveSubViewToEnd_ViewArrangement (ViewArrangement arrangement)
    {
        View superView = new () { Arrangement = arrangement };

        var subview1 = new View { Id = "subview1" };

        var subview2 = new View { Id = "subview2" };

        var subview3 = new View { Id = "subview3" };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubViewToEnd (subview1);
        Assert.Equal ([subview2, subview3, subview1], superView.SubViews.ToArray ());

        superView.MoveSubViewToEnd (subview2);
        Assert.Equal ([subview3, subview1, subview2], superView.SubViews.ToArray ());

        superView.MoveSubViewToEnd (subview3);
        Assert.Equal ([subview1, subview2, subview3], superView.SubViews.ToArray ());
    }

    [Fact]
    public void MoveSubViewToStart ()
    {
        View superView = new ();

        var subview1 = new View { Id = "subview1" };

        var subview2 = new View { Id = "subview2" };

        var subview3 = new View { Id = "subview3" };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubViewToStart (subview2);
        Assert.Equal (subview2, superView.SubViews.ElementAt (0));

        superView.MoveSubViewToStart (subview3);
        Assert.Equal (subview3, superView.SubViews.ElementAt (0));
    }

    [Fact]
    public void MoveSubViewTowardsFront ()
    {
        View superView = new ();

        var subview1 = new View { Id = "subview1" };

        var subview2 = new View { Id = "subview2" };

        var subview3 = new View { Id = "subview3" };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubViewTowardsStart (subview2);
        Assert.Equal (subview2, superView.SubViews.ElementAt (0));

        superView.MoveSubViewTowardsStart (subview3);
        Assert.Equal (subview3, superView.SubViews.ElementAt (1));

        // Already at front, what happens?
        superView.MoveSubViewTowardsStart (subview2);
        Assert.Equal (subview2, superView.SubViews.ElementAt (0));
    }

    [Fact]
    public void MoveSubViewToEnd ()
    {
        View superView = new ();

        var subview1 = new View { Id = "subview1" };

        var subview2 = new View { Id = "subview2" };

        var subview3 = new View { Id = "subview3" };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubViewToEnd (subview1);
        Assert.Equal (subview1, superView.SubViews.ToArray () [^1]);

        superView.MoveSubViewToEnd (subview2);
        Assert.Equal (subview2, superView.SubViews.ToArray () [^1]);
    }

    [Fact]
    public void MoveSubViewTowardsEnd ()
    {
        View superView = new ();

        var subview1 = new View { Id = "subview1" };

        var subview2 = new View { Id = "subview2" };

        var subview3 = new View { Id = "subview3" };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubViewTowardsEnd (subview2);
        Assert.Equal (subview2, superView.SubViews.ToArray () [^1]);

        superView.MoveSubViewTowardsEnd (subview1);
        Assert.Equal (subview1, superView.SubViews.ToArray () [1]);

        // Already at end, what happens?
        superView.MoveSubViewTowardsEnd (subview2);
        Assert.Equal (subview2, superView.SubViews.ToArray () [^1]);
    }

    [Fact]
    public void IsInHierarchy_ViewIsNull_ReturnsFalse ()
    {
        // Arrange
        var start = new View ();

        // Act
        bool result = View.IsInHierarchy (start, null);

        // Assert
        Assert.False (result);
    }

    [Fact]
    public void IsInHierarchy_StartIsNull_ReturnsFalse ()
    {
        // Arrange
        var view = new View ();

        // Act
        bool result = View.IsInHierarchy (null, view);

        // Assert
        Assert.False (result);
    }

    [Fact]
    public void IsInHierarchy_ViewIsStart_ReturnsTrue ()
    {
        // Arrange
        var start = new View ();

        // Act
        bool result = View.IsInHierarchy (start, start);

        // Assert
        Assert.True (result);
    }

    [Fact]
    public void IsInHierarchy_ViewIsDirectSubView_ReturnsTrue ()
    {
        // Arrange
        var start = new View ();
        var subview = new View ();
        start.Add (subview);

        // Act
        bool result = View.IsInHierarchy (start, subview);

        // Assert
        Assert.True (result);
    }

    [Fact]
    public void IsInHierarchy_ViewIsNestedSubView_ReturnsTrue ()
    {
        // Arrange
        var start = new View ();
        var subview = new View ();
        var nestedSubView = new View ();
        start.Add (subview);
        subview.Add (nestedSubView);

        // Act
        bool result = View.IsInHierarchy (start, nestedSubView);

        // Assert
        Assert.True (result);
    }

    [Fact]
    public void IsInHierarchy_ViewIsNotInHierarchy_ReturnsFalse ()
    {
        // Arrange
        var start = new View ();
        var subview = new View ();

        // Act
        bool result = View.IsInHierarchy (start, subview);

        // Assert
        Assert.False (result);
    }

    [Theory]
    [CombinatorialData]
    public void IsInHierarchy_ViewIsInAdornments_ReturnsTrue (bool includeAdornments)
    {
        // Arrange
        var start = new View { Id = "start" };

        var inPadding = new View { Id = "inPadding" };

        start.Padding.GetOrCreateView ().Add (inPadding);

        // Act
        bool result = View.IsInHierarchy (start, inPadding, includeAdornments);

        // Assert
        Assert.Equal (includeAdornments, result);
    }

    [Fact]
    public void SuperView_Set_Raises_SuperViewChangedEvents ()
    {
        // Arrange
        var view = new View ();
        var superView = new View ();

        var superViewChangedCount = 0;
        var superViewChangingCount = 0;

        view.SuperViewChanged += (s, e) => { superViewChangedCount++; };

        view.SuperViewChanging += (s, e) => { superViewChangingCount++; };

        // Act
        superView.Add (view);

        // Assert
        Assert.Equal (1, superViewChangingCount);
        Assert.Equal (1, superViewChangedCount);
    }

    [Fact]
    public void GetTopSuperView_Test ()
    {
        var v1 = new View ();
        var fv1 = new FrameView ();
        fv1.Add (v1);
        var tf1 = new TextField ();
        var w1 = new Window ();
        w1.Add (fv1, tf1);
        var top1 = new Runnable ();
        top1.Add (w1);

        var v2 = new View ();
        var fv2 = new FrameView ();
        fv2.Add (v2);
        var tf2 = new TextField ();
        var w2 = new Window ();
        w2.Add (fv2, tf2);
        var top2 = new Runnable ();
        top2.Add (w2);

        Assert.Equal (top1, v1.GetTopSuperView ());
        Assert.Equal (top2, v2.GetTopSuperView ());

        v1.Dispose ();
        fv1.Dispose ();
        tf1.Dispose ();
        w1.Dispose ();
        top1.Dispose ();
        v2.Dispose ();
        fv2.Dispose ();
        tf2.Dispose ();
        w2.Dispose ();
        top2.Dispose ();
    }

    [Fact]
    public void Initialized_Event_Comparing_With_Added_Event ()
    {
        var top = new Runnable { Id = "0" }; // Frame: 0, 0, 80, 25; Viewport: 0, 0, 80, 25

        var winAddedToTop = new Window { Id = "t", Width = Dim.Fill (), Height = Dim.Fill () }; // Frame: 0, 0, 80, 25; Viewport: 0, 0, 78, 23

        var v1AddedToWin = new View { Id = "v1", Width = Dim.Fill (), Height = Dim.Fill () }; // Frame: 1, 1, 78, 23 (because Windows has a border)

        var v2AddedToWin = new View { Id = "v2", Width = Dim.Fill (), Height = Dim.Fill () }; // Frame: 1, 1, 78, 23 (because Windows has a border)

        var svAddedTov1 = new View { Id = "sv1", Width = Dim.Fill (), Height = Dim.Fill () }; // Frame: 1, 1, 78, 23 (same as it's superview v1AddedToWin)

        int tc = 0, wc = 0, v1c = 0, v2c = 0, sv1c = 0;

        winAddedToTop.SubViewAdded += (s, e) =>
                                      {
                                          Assert.Equal (e.SuperView!.Frame.Width, winAddedToTop.Frame.Width);
                                          Assert.Equal (e.SuperView.Frame.Height, winAddedToTop.Frame.Height);
                                      };

        v1AddedToWin.SubViewAdded += (s, e) =>
                                     {
                                         Assert.Equal (e.SuperView!.Frame.Width, v1AddedToWin.Frame.Width);
                                         Assert.Equal (e.SuperView.Frame.Height, v1AddedToWin.Frame.Height);
                                     };

        v2AddedToWin.SubViewAdded += (s, e) =>
                                     {
                                         Assert.Equal (e.SuperView!.Frame.Width, v2AddedToWin.Frame.Width);
                                         Assert.Equal (e.SuperView.Frame.Height, v2AddedToWin.Frame.Height);
                                     };

        svAddedTov1.SubViewAdded += (s, e) =>
                                    {
                                        Assert.Equal (e.SuperView!.Frame.Width, svAddedTov1.Frame.Width);
                                        Assert.Equal (e.SuperView.Frame.Height, svAddedTov1.Frame.Height);
                                    };

        top.Initialized += (s, e) =>
                           {
                               tc++;
                               Assert.Equal (1, tc);
                               Assert.Equal (1, wc);
                               Assert.Equal (1, v1c);
                               Assert.Equal (1, v2c);
                               Assert.Equal (1, sv1c);

                               Assert.True (top.CanFocus);
                               Assert.True (winAddedToTop.CanFocus);
                               Assert.False (v1AddedToWin.CanFocus);
                               Assert.False (v2AddedToWin.CanFocus);
                               Assert.False (svAddedTov1.CanFocus);

                               top.Layout ();
                           };

        winAddedToTop.Initialized += (s, e) =>
                                     {
                                         wc++;
                                         Assert.Equal (top.Viewport.Width, winAddedToTop.Frame.Width);
                                         Assert.Equal (top.Viewport.Height, winAddedToTop.Frame.Height);
                                     };

        v1AddedToWin.Initialized += (s, e) =>
                                    {
                                        v1c++;

                                        // Top.Frame: 0, 0, 80, 25; Top.Viewport: 0, 0, 80, 25
                                        // BUGBUG: This is wrong, it should be 78, 23. This test has always been broken.
                                        // in no way should the v1AddedToWin.Frame be the same as the Top.Frame/Viewport
                                        // as it is a subview of winAddedToTop, which has a border!
                                        //Assert.Equal (top.Viewport.Width,  v1AddedToWin.Frame.Width);
                                        //Assert.Equal (top.Viewport.Height, v1AddedToWin.Frame.Height);
                                    };

        v2AddedToWin.Initialized += (s, e) =>
                                    {
                                        v2c++;

                                        // Top.Frame: 0, 0, 80, 25; Top.Viewport: 0, 0, 80, 25
                                        // BUGBUG: This is wrong, it should be 78, 23. This test has always been broken.
                                        // in no way should the v2AddedToWin.Frame be the same as the Top.Frame/Viewport
                                        // as it is a subview of winAddedToTop, which has a border!
                                        //Assert.Equal (top.Viewport.Width,  v2AddedToWin.Frame.Width);
                                        //Assert.Equal (top.Viewport.Height, v2AddedToWin.Frame.Height);
                                    };

        svAddedTov1.Initialized += (s, e) =>
                                   {
                                       sv1c++;

                                       // Top.Frame: 0, 0, 80, 25; Top.Viewport: 0, 0, 80, 25
                                       // BUGBUG: This is wrong, it should be 78, 23. This test has always been broken.
                                       // in no way should the svAddedTov1.Frame be the same as the Top.Frame/Viewport
                                       // because sv1AddedTov1 is a subview of v1AddedToWin, which is a subview of
                                       // winAddedToTop, which has a border!
                                       //Assert.Equal (top.Viewport.Width,  svAddedTov1.Frame.Width);
                                       //Assert.Equal (top.Viewport.Height, svAddedTov1.Frame.Height);
                                       Assert.False (svAddedTov1.CanFocus);

                                       //Assert.Throws<InvalidOperationException> (() => svAddedTov1.CanFocus = true);
                                       Assert.False (svAddedTov1.CanFocus);
                                   };

        v1AddedToWin.Add (svAddedTov1);
        winAddedToTop.Add (v1AddedToWin, v2AddedToWin);
        top.Add (winAddedToTop);

        top.BeginInit ();
        top.EndInit ();

        Assert.Equal (1, tc);
        Assert.Equal (1, wc);
        Assert.Equal (1, v1c);
        Assert.Equal (1, v2c);
        Assert.Equal (1, sv1c);

        Assert.True (top.CanFocus);
        Assert.True (winAddedToTop.CanFocus);
        Assert.False (v1AddedToWin.CanFocus);
        Assert.False (v2AddedToWin.CanFocus);
        Assert.False (svAddedTov1.CanFocus);

        v1AddedToWin.CanFocus = true;
        Assert.False (svAddedTov1.CanFocus); // False because sv1 was disposed and it isn't a subview of v1.
    }

    [Fact]
    public void SuperViewChanged_Raised_On_SubViewAdded_SubViewRemoved ()
    {
        var isAdded = false;

        View superView = new () { Id = "superView" };
        View subView = new () { Id = "subView" };

        superView.SubViewAdded += (s, e) =>
                                  {
                                      Assert.True (isAdded);
                                      Assert.Equal (superView, subView.SuperView);
                                      Assert.Equal (subView, e.SubView);
                                      Assert.Equal (superView, e.SuperView);
                                  };

        superView.SubViewRemoved += (s, e) =>
                                    {
                                        Assert.False (isAdded);
                                        Assert.NotEqual (superView, subView.SuperView);
                                        Assert.Equal (subView, e.SubView);
                                        Assert.Equal (superView, e.SuperView);
                                    };

        subView.SuperViewChanged += (s, e) => { isAdded = subView.SuperView == superView; };

        superView.Add (subView);
        Assert.True (isAdded);
        Assert.Equal (superView, subView.SuperView);
        Assert.Single (superView.SubViews);

        superView.Remove (subView);
        Assert.False (isAdded);
        Assert.NotEqual (superView, subView.SuperView);
        Assert.Empty (superView.SubViews);
    }

    [Fact]
    public void RemoveAll_Removes_All_SubViews ()
    {
        // Arrange
        var superView = new View ();
        var subView1 = new View ();
        var subView2 = new View ();
        var subView3 = new View ();

        superView.Add (subView1, subView2, subView3);

        // Act
        IReadOnlyCollection<View> removedViews = superView.RemoveAll ();

        // Assert
        Assert.Empty (superView.SubViews);
        Assert.Equal (3, removedViews.Count);
        Assert.Contains (subView1, removedViews);
        Assert.Contains (subView2, removedViews);
        Assert.Contains (subView3, removedViews);
    }

    [Fact]
    public void RemoveAllTView_Removes_All_SubViews_Of_Specific_Type ()
    {
        // Arrange
        var superView = new View ();
        var subView1 = new View ();
        var subView2 = new View ();
        var subView3 = new View ();
        var subView4 = new Button ();

        superView.Add (subView1, subView2, subView3, subView4);

        // Act
        IReadOnlyCollection<Button> removedViews = superView.RemoveAll<Button> ();

        // Assert
        Assert.Equal (3, superView.SubViews.Count);
        Assert.DoesNotContain (subView4, superView.SubViews);
        Assert.Single (removedViews);
        Assert.Contains (subView4, removedViews);
    }

    [Fact]
    public void RemoveAllTView_Does_Not_Remove_Other_Types ()
    {
        // Arrange
        var superView = new View ();
        var subView1 = new View ();
        var subView2 = new Button ();
        var subView3 = new Label ();

        superView.Add (subView1, subView2, subView3);

        // Act
        IReadOnlyCollection<Button> removedViews = superView.RemoveAll<Button> ();

        // Assert
        Assert.Equal (2, superView.SubViews.Count);
        Assert.Contains (subView1, superView.SubViews);
        Assert.Contains (subView3, superView.SubViews);
        Assert.Single (removedViews);
        Assert.Contains (subView2, removedViews);
    }

    [Fact]
    public void SuperViewChanging_Raised_Before_SuperViewChanged ()
    {
        // Arrange
        var superView = new View ();
        var subView = new View ();

        List<string> events = new ();

        subView.SuperViewChanging += (s, e) => { events.Add ("SuperViewChanging"); };

        subView.SuperViewChanged += (s, e) => { events.Add ("SuperViewChanged"); };

        // Act
        superView.Add (subView);

        // Assert
        Assert.Equal (2, events.Count);
        Assert.Equal ("SuperViewChanging", events [0]);
        Assert.Equal ("SuperViewChanged", events [1]);
    }

    [Fact]
    public void SuperViewChanging_Provides_OldSuperView_On_Add ()
    {
        // Arrange
        var superView = new View ();
        var subView = new View ();

        var currentValueInEvent = new View (); // Set to non-null to ensure it gets updated
        View? newValueInEvent = null;

        subView.SuperViewChanging += (s, e) =>
                                     {
                                         currentValueInEvent = e.CurrentValue;
                                         newValueInEvent = e.NewValue;
                                     };

        // Act
        superView.Add (subView);

        // Assert
        Assert.Null (currentValueInEvent); // Was null before add
        Assert.Equal (superView, newValueInEvent); // Will be superView after add
    }

    [Fact]
    public void SuperViewChanging_Provides_OldSuperView_On_Remove ()
    {
        // Arrange
        var superView = new View ();
        var subView = new View ();

        superView.Add (subView);

        View? currentValueInEvent = null;
        var newValueInEvent = new View (); // Set to non-null to ensure it gets updated

        subView.SuperViewChanging += (s, e) =>
                                     {
                                         currentValueInEvent = e.CurrentValue;
                                         newValueInEvent = e.NewValue;
                                     };

        // Act
        superView.Remove (subView);

        // Assert
        Assert.Equal (superView, currentValueInEvent); // Was superView before remove
        Assert.Null (newValueInEvent); // Will be null after remove
    }

    [Fact]
    public void SuperViewChanging_Allows_Access_To_App_Before_Remove ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        var subView = new View ();

        runnable.Add (subView);
        SessionToken? token = app.Begin (runnable);

        IApplication? appInEvent = null;

        subView.SuperViewChanging += (s, e) =>
                                     {
                                         Assert.NotNull (s);

                                         // At this point, SuperView is still set, so App should be accessible
                                         appInEvent = (s as View)?.App;
                                     };

        Assert.NotNull (runnable.App);

        // Act
        runnable.Remove (subView);

        // Assert
        Assert.NotNull (appInEvent);
        Assert.Equal (app, appInEvent);

        app.End (token!);
        runnable.Dispose ();
    }

    [Fact]
    public void OnSuperViewChanging_Called_Before_OnSuperViewChanged ()
    {
        // Arrange
        var superView = new View ();
        List<string> events = new ();

        var subView = new TestViewWithSuperViewEvents (events);

        // Act
        superView.Add (subView);

        // Assert
        Assert.Equal (2, events.Count);
        Assert.Equal ("OnSuperViewChanging", events [0]);
        Assert.Equal ("OnSuperViewChanged", events [1]);
    }

    [Fact]
    public void SuperViewChanging_Raised_When_Changing_Between_SuperViews ()
    {
        // Arrange
        var superView1 = new View ();
        var superView2 = new View ();
        var subView = new View ();

        superView1.Add (subView);

        View? currentValueInEvent = null;
        View? newValueInEvent = null;

        subView.SuperViewChanging += (s, e) =>
                                     {
                                         currentValueInEvent = e.CurrentValue;
                                         newValueInEvent = e.NewValue;
                                     };

        // Act
        superView2.Add (subView);

        // Assert
        Assert.Equal (superView1, currentValueInEvent);
        Assert.Equal (superView2, newValueInEvent);
    }

    // Helper class for testing virtual method calls
    private class TestViewWithSuperViewEvents : View
    {
        private readonly List<string> _events;

        public TestViewWithSuperViewEvents (List<string> events) => _events = events;

        protected override bool OnSuperViewChanging (ValueChangingEventArgs<View?> args)
        {
            _events.Add ("OnSuperViewChanging");

            return base.OnSuperViewChanging (args);
        }

        protected override void OnSuperViewChanged (ValueChangedEventArgs<View?> args)
        {
            _events.Add ("OnSuperViewChanged");
            base.OnSuperViewChanged (args);
        }
    }

    [Fact]
    public void SuperViewChanging_Can_Be_Cancelled_Via_Event ()
    {
        // Arrange
        var superView = new View ();
        var subView = new View ();

        subView.SuperViewChanging += (s, e) =>
                                     {
                                         e.Handled = true; // Cancel the change
                                     };

        // Act
        superView.Add (subView);

        // Assert - SuperView should not be set because the change was cancelled
        Assert.Null (subView.SuperView);
        Assert.Empty (superView.SubViews);
    }

    [Fact]
    public void SuperViewChanging_Can_Be_Cancelled_Via_Virtual_Method ()
    {
        // Arrange
        var superView = new View ();
        var subView = new TestViewThatCancelsChange ();

        // Act
        superView.Add (subView);

        // Assert - SuperView should not be set because the change was cancelled
        Assert.Null (subView.SuperView);
        Assert.Empty (superView.SubViews);
    }

    [Fact]
    public void SuperViewChanging_Virtual_Method_Cancellation_Prevents_Event ()
    {
        // Arrange
        var superView = new View ();
        var subView = new TestViewThatCancelsChange ();

        var eventRaised = false;
        subView.SuperViewChanging += (s, e) => { eventRaised = true; };

        // Act
        superView.Add (subView);

        // Assert - Event should not be raised because virtual method cancelled first
        Assert.False (eventRaised);
        Assert.Null (subView.SuperView);
    }

    [Fact]
    public void SuperViewChanging_Cancellation_On_Remove ()
    {
        // Arrange
        var superView = new View ();
        var subView = new View ();

        superView.Add (subView);
        Assert.Equal (superView, subView.SuperView);

        subView.SuperViewChanging += (s, e) =>
                                     {
                                         // Cancel removal if trying to set to null
                                         if (e.NewValue is null)
                                         {
                                             e.Handled = true;
                                         }
                                     };

        // Act
        superView.Remove (subView);

        // Assert - SuperView should still be set because removal was cancelled
        Assert.Equal (superView, subView.SuperView);
        Assert.Single (superView.SubViews);
    }

    [Fact]
    public void SuperViewChanging_Cancellation_When_Changing_Between_SuperViews ()
    {
        // Arrange
        var superView1 = new View ();
        var superView2 = new View ();
        var subView = new View ();

        superView1.Add (subView);

        subView.SuperViewChanging += (s, e) =>
                                     {
                                         // Cancel if trying to move to superView2
                                         if (e.NewValue == superView2)
                                         {
                                             e.Handled = true;
                                         }
                                     };

        // Act
        superView2.Add (subView);

        // Assert - Should still be in superView1 because change was cancelled
        Assert.Equal (superView1, subView.SuperView);
        Assert.Single (superView1.SubViews);
        Assert.Empty (superView2.SubViews);
    }

    // Helper class for testing cancellation
    private class TestViewThatCancelsChange : View
    {
        protected override bool OnSuperViewChanging (ValueChangingEventArgs<View?> args) => true; // Always cancel the change
    }

    #region AddAt Tests

    // Copilot
    [Fact]
    public void AddAt_Null_View_Returns_Null ()
    {
        // Arrange
        View superView = new ();

        // Act
        View? result = superView.AddAt (0, null);

        // Assert
        Assert.Null (result);
        Assert.Empty (superView.SubViews);
    }

    // Copilot
    [Fact]
    public void AddAt_Negative_Index_Throws ()
    {
        // Arrange
        View superView = new ();
        View subView = new () { Id = "subView" };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException> (() => superView.AddAt (-1, subView));
    }

    // Copilot
    [Fact]
    public void AddAt_Index_Greater_Than_Count_Throws ()
    {
        // Arrange
        View superView = new ();
        View subView = new () { Id = "subView" };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException> (() => superView.AddAt (1, subView));
    }

    // Copilot
    [Fact]
    public void AddAt_Index_Zero_Inserts_At_Beginning ()
    {
        // Arrange
        View superView = new ();
        View existing1 = new () { Id = "existing1" };
        View existing2 = new () { Id = "existing2" };
        superView.Add (existing1, existing2);

        View newView = new () { Id = "newView" };

        // Act
        View? result = superView.AddAt (0, newView);

        // Assert
        Assert.NotNull (result);
        Assert.Equal (3, superView.SubViews.Count);
        Assert.Equal (newView, superView.SubViews.ElementAt (0));
        Assert.Equal (existing1, superView.SubViews.ElementAt (1));
        Assert.Equal (existing2, superView.SubViews.ElementAt (2));
    }

    // Copilot
    [Fact]
    public void AddAt_Middle_Index_Inserts_At_Correct_Position ()
    {
        // Arrange
        View superView = new ();
        View existing1 = new () { Id = "existing1" };
        View existing2 = new () { Id = "existing2" };
        View existing3 = new () { Id = "existing3" };
        superView.Add (existing1, existing2, existing3);

        View newView = new () { Id = "newView" };

        // Act
        View? result = superView.AddAt (2, newView);

        // Assert
        Assert.NotNull (result);
        Assert.Equal (4, superView.SubViews.Count);
        Assert.Equal (existing1, superView.SubViews.ElementAt (0));
        Assert.Equal (existing2, superView.SubViews.ElementAt (1));
        Assert.Equal (newView, superView.SubViews.ElementAt (2));
        Assert.Equal (existing3, superView.SubViews.ElementAt (3));
    }

    // Copilot
    [Fact]
    public void AddAt_End_Index_Inserts_At_End ()
    {
        // Arrange
        View superView = new ();
        View existing1 = new () { Id = "existing1" };
        View existing2 = new () { Id = "existing2" };
        superView.Add (existing1, existing2);

        View newView = new () { Id = "newView" };

        // Act
        View? result = superView.AddAt (2, newView);

        // Assert
        Assert.NotNull (result);
        Assert.Equal (3, superView.SubViews.Count);
        Assert.Equal (existing1, superView.SubViews.ElementAt (0));
        Assert.Equal (existing2, superView.SubViews.ElementAt (1));
        Assert.Equal (newView, superView.SubViews.ElementAt (2));
    }

    // Copilot
    [Fact]
    public void AddAt_Sets_SuperView ()
    {
        // Arrange
        View superView = new ();
        View subView = new () { Id = "subView" };

        // Act
        superView.AddAt (0, subView);

        // Assert
        Assert.Equal (superView, subView.SuperView);
    }

    // Copilot
    [Fact]
    public void AddAt_Raises_SubViewAdded ()
    {
        // Arrange
        View superView = new ();
        View subView = new () { Id = "subView" };

        var eventRaised = false;
        View? eventSubView = null;

        superView.SubViewAdded += (s, e) =>
                                  {
                                      eventRaised = true;
                                      eventSubView = e.SubView;
                                  };

        // Act
        superView.AddAt (0, subView);

        // Assert
        Assert.True (eventRaised);
        Assert.Equal (subView, eventSubView);
    }

    // Copilot
    [Fact]
    public void AddAt_Raises_SuperViewChanged_On_SubView ()
    {
        // Arrange
        View superView = new ();
        View subView = new () { Id = "subView" };

        var eventRaised = false;

        subView.SuperViewChanged += (s, e) => { eventRaised = true; };

        // Act
        superView.AddAt (0, subView);

        // Assert
        Assert.True (eventRaised);
        Assert.Equal (superView, subView.SuperView);
    }

    // Copilot
    [Fact]
    public void AddAt_Cancelled_By_SuperViewChanging_Returns_Null_And_Reverts ()
    {
        // Arrange
        View superView = new ();
        View existing = new () { Id = "existing" };
        superView.Add (existing);

        View newView = new () { Id = "newView" };

        newView.SuperViewChanging += (s, e) =>
                                     {
                                         e.Handled = true; // Cancel the change
                                     };

        // Act
        View? result = superView.AddAt (0, newView);

        // Assert - Should be cancelled, only existing view remains
        Assert.Null (result);
        Assert.Single (superView.SubViews);
        Assert.Equal (existing, superView.SubViews.ElementAt (0));
        Assert.Null (newView.SuperView);
    }

    // Copilot
    [Fact]
    public void AddAt_At_Empty_List_Index_Zero_Works ()
    {
        // Arrange
        View superView = new ();
        View subView = new () { Id = "subView" };

        // Act
        View? result = superView.AddAt (0, subView);

        // Assert
        Assert.NotNull (result);
        Assert.Single (superView.SubViews);
        Assert.Equal (subView, superView.SubViews.ElementAt (0));
        Assert.Equal (superView, subView.SuperView);
    }

    // Copilot
    [Fact]
    public void Add_Single_View_Appends_To_End ()
    {
        // Arrange - Verify that Add(View?) still works as appending to end via AddAt
        View superView = new ();
        View existing = new () { Id = "existing" };
        superView.Add (existing);

        View newView = new () { Id = "newView" };

        // Act
        View? result = superView.Add (newView);

        // Assert
        Assert.NotNull (result);
        Assert.Equal (2, superView.SubViews.Count);
        Assert.Equal (existing, superView.SubViews.ElementAt (0));
        Assert.Equal (newView, superView.SubViews.ElementAt (1));
    }

    // Copilot
    [Fact]
    public void AddAt_Multiple_Insertions_Maintain_Order ()
    {
        // Arrange
        View superView = new ();
        View view1 = new () { Id = "view1" };
        View view2 = new () { Id = "view2" };
        View view3 = new () { Id = "view3" };

        // Act - Insert in reverse order at index 0
        superView.AddAt (0, view1);
        superView.AddAt (0, view2);
        superView.AddAt (0, view3);

        // Assert - view3 should be first, view2 second, view1 third
        Assert.Equal (3, superView.SubViews.Count);
        Assert.Equal (view3, superView.SubViews.ElementAt (0));
        Assert.Equal (view2, superView.SubViews.ElementAt (1));
        Assert.Equal (view1, superView.SubViews.ElementAt (2));
    }

    #endregion AddAt Tests

    #region GetSubViews Tests

    [Fact]
    public void GetSubViews_Returns_Empty_Collection_When_No_SubViews ()
    {
        // Arrange
        View view = new ();

        // Act
        IReadOnlyCollection<View> result = view.GetSubViews ();

        // Assert
        Assert.NotNull (result);
        Assert.Empty (result);
    }

    [Fact]
    public void GetSubViews_Returns_Direct_SubViews_By_Default ()
    {
        // Arrange
        View superView = new ();
        View subView1 = new () { Id = "subView1" };
        View subView2 = new () { Id = "subView2" };
        View subView3 = new () { Id = "subView3" };

        superView.Add (subView1, subView2, subView3);

        // Act
        IReadOnlyCollection<View> result = superView.GetSubViews ();

        // Assert
        Assert.NotNull (result);
        Assert.Equal (3, result.Count);
        Assert.Contains (subView1, result);
        Assert.Contains (subView2, result);
        Assert.Contains (subView3, result);
    }

    [Fact]
    public void GetSubViews_Does_Not_Include_Adornment_SubViews_By_Default ()
    {
        // Arrange
        View superView = new ();
        View subView = new () { Id = "subView" };

        superView.Add (subView);
        superView.BeginInit ();
        superView.EndInit ();

        // Add a subview to the Border (e.g., ShadowView)
        View borderSubView = new () { Id = "borderSubView" };
        superView.Border.GetOrCreateView ().Add (borderSubView);

        // Act
        IReadOnlyCollection<View> result = superView.GetSubViews ();

        // Assert
        Assert.Single (result);
        Assert.Contains (subView, result);
        Assert.DoesNotContain (borderSubView, result);
    }

    [Fact]
    public void GetSubViews_Includes_Border_SubViews_When_IncludeAdornments_Is_True ()
    {
        // Arrange
        View superView = new ();
        View subView = new () { Id = "subView" };

        superView.Add (subView);
        superView.BeginInit ();
        superView.EndInit ();

        // Add a subview to the Border
        View borderSubView = new () { Id = "borderSubView" };

        // Thickness matters
        superView.Border.Thickness = new Thickness (1);
        superView.Border.GetOrCreateView ().Add (borderSubView);

        // Act
        IReadOnlyCollection<View> result = superView.GetSubViews (includeBorder: true);

        // Assert
        Assert.Equal (2, result.Count);
        Assert.Contains (subView, result);
        Assert.Contains (borderSubView, result);
    }

    [Fact]
    public void GetSubViews_Includes_Padding_SubViews_When_IncludeAdornments_Is_True ()
    {
        // Arrange
        View superView = new ();
        View subView = new () { Id = "subView" };

        superView.Add (subView);
        superView.BeginInit ();
        superView.EndInit ();

        // Add a subview to the Padding
        View paddingSubView = new () { Id = "paddingSubView" };

        // Thickness matters
        superView.Padding.Thickness = new Thickness (1);
        superView.Padding.GetOrCreateView ().Add (paddingSubView);

        // Act
        IReadOnlyCollection<View> result = superView.GetSubViews (includePadding: true);

        // Assert
        Assert.Equal (2, result.Count);
        Assert.Contains (subView, result);
        Assert.Contains (paddingSubView, result);
    }

    [Fact]
    public void GetSubViews_Includes_All_Adornment_SubViews_When_IncludeAdornments_Is_True ()
    {
        // Arrange
        View superView = new ();
        View subView1 = new () { Id = "subView1" };
        View subView2 = new () { Id = "subView2" };

        superView.Add (subView1, subView2);
        superView.BeginInit ();
        superView.EndInit ();

        // Add subviews to each adornment
        View borderSubView = new () { Id = "borderSubView" };
        View paddingSubView = new () { Id = "paddingSubView" };

        // Thickness matters
        //superView.Margin.Thickness = new  (1);
        //superView.Margin.Add (marginSubView);
        superView.Border.Thickness = new Thickness (1);
        superView.Border.GetOrCreateView ().Add (borderSubView);
        superView.Padding.Thickness = new Thickness (1);
        superView.Padding.GetOrCreateView ().Add (paddingSubView);

        // Act
        IReadOnlyCollection<View> result = superView.GetSubViews (true, true, true);

        // Assert
        Assert.Equal (4, result.Count);
        Assert.Contains (subView1, result);
        Assert.Contains (subView2, result);
        Assert.Contains (borderSubView, result);
        Assert.Contains (paddingSubView, result);
    }

    [Fact]
    public void GetSubViews_Returns_Correct_Order ()
    {
        // Arrange
        View superView = new ();
        View subView1 = new () { Id = "subView1" };
        View subView2 = new () { Id = "subView2" };

        superView.Add (subView1, subView2);
        superView.BeginInit ();
        superView.EndInit ();

        View borderSubView = new () { Id = "borderSubView" };
        View paddingSubView = new () { Id = "paddingSubView" };

        // Thickness matters
        superView.Border.Thickness = new Thickness (1);
        superView.Border.GetOrCreateView ().Add (borderSubView);
        superView.Padding.Thickness = new Thickness (1);
        superView.Padding.GetOrCreateView ().Add (paddingSubView);

        // Act
        IReadOnlyCollection<View> result = superView.GetSubViews (true, true, true);
        List<View> resultList = result.ToList ();

        // Assert - Order should be: direct SubViews, Border, Padding
        Assert.Equal (4, resultList.Count);
        Assert.Equal (subView1, resultList [0]);
        Assert.Equal (subView2, resultList [1]);
        Assert.Equal (borderSubView, resultList [2]);
        Assert.Equal (paddingSubView, resultList [3]);
    }

    [Fact]
    public void GetSubViews_Returns_Snapshot_Safe_For_Modification ()
    {
        // Arrange
        View superView = new ();
        View subView1 = new () { Id = "subView1" };
        View subView2 = new () { Id = "subView2" };

        superView.Add (subView1, subView2);

        // Act
        IReadOnlyCollection<View> result = superView.GetSubViews ();

        // Modify the SuperView's SubViews
        View subView3 = new () { Id = "subView3" };
        superView.Add (subView3);

        // Assert - The snapshot should not include subView3
        Assert.Equal (2, result.Count);
        Assert.Contains (subView1, result);
        Assert.Contains (subView2, result);
        Assert.DoesNotContain (subView3, result);
    }

    [Fact]
    public void GetSubViews_Multiple_SubViews_In_Each_Adornment ()
    {
        // Arrange
        View superView = new ();
        View subView = new () { Id = "subView" };

        superView.Add (subView);
        superView.BeginInit ();
        superView.EndInit ();

        // Add multiple subviews to each adornment
        View borderSubView1 = new () { Id = "borderSubView1" };
        View borderSubView2 = new () { Id = "borderSubView2" };
        View paddingSubView1 = new () { Id = "paddingSubView1" };
        View paddingSubView2 = new () { Id = "paddingSubView2" };

        // Thickness matters
        superView.Border.Thickness = new Thickness (1);
        superView.Border.GetOrCreateView ().Add (borderSubView1);
        superView.Border.GetOrCreateView ().Add (borderSubView2);

        // Thickness matters
        superView.Padding.Thickness = new Thickness (1);
        superView.Padding.GetOrCreateView ().Add (paddingSubView1);
        superView.Padding.GetOrCreateView ().Add (paddingSubView2);

        // Act
        IReadOnlyCollection<View> result = superView.GetSubViews (true, true, true);

        // Assert
        Assert.Equal (5, result.Count);
        Assert.Contains (subView, result);
        Assert.Contains (borderSubView1, result);
        Assert.Contains (borderSubView2, result);
        Assert.Contains (paddingSubView1, result);
        Assert.Contains (paddingSubView2, result);
    }

    [Fact]
    public void GetSubViews_Works_With_Adornment_Itself ()
    {
        // Arrange - Test that an Adornment (which is a View) can also have GetSubViews called
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        View paddingSubView = new () { Id = "paddingSubView" };
        view.Padding.GetOrCreateView ().Add (paddingSubView);

        // Act - Call GetSubViews on the Margin itself
        IReadOnlyCollection<View> result = view.Padding.View!.GetSubViews ();

        // Assert
        Assert.Single (result);
        Assert.Contains (paddingSubView, result);
    }

    [Fact]
    public void GetSubViews_Handles_Null_Adornments_Gracefully ()
    {
        // Arrange - Create an Adornment view which doesn't have its own adornments
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        // Border is an Adornment and doesn't have Margin, Border, Padding
        View borderSubView = new () { Id = "borderSubView" };
        view.Border.GetOrCreateView ().Add (borderSubView);

        // Act - GetSubViews on Border (an Adornment) with includeAdornments
        IReadOnlyCollection<View> result = view.Border.View!.GetSubViews (true);

        // Assert - Should only return direct subviews, not crash
        Assert.Single (result);
        Assert.Contains (borderSubView, result);
    }

    [Fact]
    public void GetSubViews_Returns_IReadOnlyCollection ()
    {
        // Arrange
        View superView = new ();
        View subView = new () { Id = "subView" };
        superView.Add (subView);

        // Act
        IReadOnlyCollection<View> result = superView.GetSubViews ();

        // Assert
        Assert.IsAssignableFrom<IReadOnlyCollection<View>> (result);

        // Verify Count property is available and single item
        Assert.Single (result);
    }

    [Fact]
    public void GetSubViews_Empty_Adornments_Do_Not_Add_Nulls ()
    {
        // Arrange
        View superView = new ();
        View subView = new () { Id = "subView" };

        superView.Add (subView);
        superView.BeginInit ();
        superView.EndInit ();

        // Don't add any subviews to adornments

        // Act
        IReadOnlyCollection<View> result = superView.GetSubViews (true);

        // Assert - Should only have the direct subview, no nulls
        Assert.Single (result);
        Assert.Contains (subView, result);
        Assert.All (result, Assert.NotNull);
    }
}

#endregion GetSubViews Tests

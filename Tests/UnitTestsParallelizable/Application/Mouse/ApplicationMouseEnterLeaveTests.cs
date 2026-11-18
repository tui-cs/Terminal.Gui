#nullable enable
using System.ComponentModel;

namespace ApplicationTests.MouseTests;

[Trait ("Category", "Input")]
[Collection("Application Tests")]
public class ApplicationMouseEnterLeaveTests
{
    private class TestView : View
    {
        public TestView ()
        {
            X = 1;
            Y = 1;
            Width = 1;
            Height = 1;
        }

       // public bool CancelOnEnter { get; }
        public int OnMouseEnterCalled { get; private set; }
        public int OnMouseLeaveCalled { get; private set; }

        protected override bool OnMouseEnter (CancelEventArgs eventArgs)
        {
            OnMouseEnterCalled++;
           // eventArgs.Cancel = CancelOnEnter;

            base.OnMouseEnter (eventArgs);

            return eventArgs.Cancel;
        }

        protected override void OnMouseLeave ()
        {
            OnMouseLeaveCalled++;

            base.OnMouseLeave ();
        }
    }

    [Fact]
    public void RaiseMouseEnterLeaveEvents_MouseEntersView_CallsOnMouseEnter ()
    {
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new () { Frame = new (0, 0, 10, 10) };
        app.Begin (runnable);

        // Arrange
        var view = new TestView ();
        runnable.Add (view);
        var mousePosition = new Point (1, 1);
        List<View?> currentViewsUnderMouse = [view];

        var mouse = new Mouse
        {
            Position = mousePosition,
            ScreenPosition = mousePosition
        };

        app.Mouse.CachedViewsUnderMouse.Clear ();

        try
        {
            // Act
            app.Mouse.RaiseMouseEnterLeaveEvents (mousePosition, currentViewsUnderMouse);

            // Assert
            Assert.Equal (1, view.OnMouseEnterCalled);
        }
        finally
        {
            // Cleanup
            app.Mouse.ResetState ();
        }
    }

    [Fact]
    public void RaiseMouseEnterLeaveEvents_MouseLeavesView_CallsOnMouseLeave ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new () { Frame = new (0, 0, 10, 10) };
        app.Begin (runnable);

        var view = new TestView ();
        runnable.Add (view);
        var mousePosition = new Point (0, 0);
        List<View?> currentViewsUnderMouse = new ();
        var mouse = new Mouse ();

        app.Mouse.CachedViewsUnderMouse.Clear ();
        app.Mouse.CachedViewsUnderMouse.Add (view);

        // Act
        app.Mouse.RaiseMouseEnterLeaveEvents (mousePosition, currentViewsUnderMouse);

        // Assert
        Assert.Equal (0, view.OnMouseEnterCalled);
        Assert.Equal (1, view.OnMouseLeaveCalled);
    }

    [Fact]
    public void RaiseMouseEnterLeaveEvents_MouseMovesBetweenAdjacentViews_CallsOnMouseEnterAndLeave ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new () { Frame = new (0, 0, 10, 10) };
        app.Begin (runnable);
        var view1 = new TestView (); // at 1,1 to 2,2

        var view2 = new TestView () // at 2,2 to 3,3
        {
            X = 2,
            Y = 2
        };
        runnable.Add (view1);
        runnable.Add (view2);

        app.Mouse.CachedViewsUnderMouse.Clear ();

        // Act
        var mousePosition = new Point (0, 0);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (0, view1.OnMouseEnterCalled);
        Assert.Equal (0, view1.OnMouseLeaveCalled);
        Assert.Equal (0, view2.OnMouseEnterCalled);
        Assert.Equal (0, view2.OnMouseLeaveCalled);

        // Act
        mousePosition = new (1, 1);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (1, view1.OnMouseEnterCalled);
        Assert.Equal (0, view1.OnMouseLeaveCalled);
        Assert.Equal (0, view2.OnMouseEnterCalled);
        Assert.Equal (0, view2.OnMouseLeaveCalled);

        // Act
        mousePosition = new (2, 2);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (1, view1.OnMouseEnterCalled);
        Assert.Equal (1, view1.OnMouseLeaveCalled);
        Assert.Equal (1, view2.OnMouseEnterCalled);
        Assert.Equal (0, view2.OnMouseLeaveCalled);

        // Act
        mousePosition = new (3, 3);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                                mousePosition,
                                                runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (1, view1.OnMouseEnterCalled);
        Assert.Equal (1, view1.OnMouseLeaveCalled);
        Assert.Equal (1, view2.OnMouseEnterCalled);
        Assert.Equal (1, view2.OnMouseLeaveCalled);

        // Act
        mousePosition = new (0, 0);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (1, view1.OnMouseEnterCalled);
        Assert.Equal (1, view1.OnMouseLeaveCalled);
        Assert.Equal (1, view2.OnMouseEnterCalled);
        Assert.Equal (1, view2.OnMouseLeaveCalled);

    }

    [Fact]
    public void RaiseMouseEnterLeaveEvents_NoViewsUnderMouse_DoesNotCallOnMouseEnterOrLeave ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new () { Frame = new (0, 0, 10, 10) };
        app.Begin (runnable);
        var view = new TestView ();
        runnable.Add (view);
        var mousePosition = new Point (0, 0);
        List<View?> currentViewsUnderMouse = new ();
        var mouse = new Mouse ();

        app.Mouse.CachedViewsUnderMouse.Clear ();

        // Act
        app.Mouse.RaiseMouseEnterLeaveEvents (mousePosition, currentViewsUnderMouse);

        // Assert
        Assert.Equal (0, view.OnMouseEnterCalled);
        Assert.Equal (0, view.OnMouseLeaveCalled);

    }

    [Fact]
    public void RaiseMouseEnterLeaveEvents_MouseMovesBetweenOverlappingPeerViews_CallsOnMouseEnterAndLeave ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new () { Frame = new (0, 0, 10, 10) };
        app.Begin (runnable);

        var view1 = new TestView
        {
            Width = 2
        }; // at 1,1 to 3,2

        var view2 = new TestView () // at 2,2 to 4,3
        {
            Width = 2,
            X = 2,
            Y = 2
        };
        runnable.Add (view1);
        runnable.Add (view2);

        app.Mouse.CachedViewsUnderMouse.Clear ();

        // Act
        var mousePosition = new Point (0, 0);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (0, view1.OnMouseEnterCalled);
        Assert.Equal (0, view1.OnMouseLeaveCalled);
        Assert.Equal (0, view2.OnMouseEnterCalled);
        Assert.Equal (0, view2.OnMouseLeaveCalled);

        // Act
        mousePosition = new (1, 1);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (1, view1.OnMouseEnterCalled);
        Assert.Equal (0, view1.OnMouseLeaveCalled);
        Assert.Equal (0, view2.OnMouseEnterCalled);
        Assert.Equal (0, view2.OnMouseLeaveCalled);

        // Act
        mousePosition = new (2, 2);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (1, view1.OnMouseEnterCalled);
        Assert.Equal (1, view1.OnMouseLeaveCalled);
        Assert.Equal (1, view2.OnMouseEnterCalled);
        Assert.Equal (0, view2.OnMouseLeaveCalled);

        // Act
        mousePosition = new (3, 3);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (1, view1.OnMouseEnterCalled);
        Assert.Equal (1, view1.OnMouseLeaveCalled);
        Assert.Equal (1, view2.OnMouseEnterCalled);
        Assert.Equal (1, view2.OnMouseLeaveCalled);

        // Act
        mousePosition = new (0, 0);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (1, view1.OnMouseEnterCalled);
        Assert.Equal (1, view1.OnMouseLeaveCalled);
        Assert.Equal (1, view2.OnMouseEnterCalled);
        Assert.Equal (1, view2.OnMouseLeaveCalled);

        // Act
        mousePosition = new (2, 2);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (1, view1.OnMouseEnterCalled);
        Assert.Equal (1, view1.OnMouseLeaveCalled);
        Assert.Equal (2, view2.OnMouseEnterCalled);
        Assert.Equal (1, view2.OnMouseLeaveCalled);

    }

    [Fact]
    public void RaiseMouseEnterLeaveEvents_MouseMovesBetweenOverlappingSubViews_CallsOnMouseEnterAndLeave ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new () { Frame = new (0, 0, 10, 10) };
        app.Begin (runnable);

        var view1 = new TestView
        {
            Id = "view1",
            Width = 2,
            Height = 2,
            Arrangement = ViewArrangement.Overlapped
        }; // at 1,1 to 3,3 (screen)

        var subView = new TestView
        {
            Id = "subView",
            Width = 2,
            Height = 2,
            X = 1,
            Y = 1,
            Arrangement = ViewArrangement.Overlapped
        }; // at 2,2 to 4,4 (screen)
        view1.Add (subView);
        runnable.Add (view1);

        app.Mouse.CachedViewsUnderMouse.Clear ();

        Assert.Equal (1, view1.FrameToScreen ().X);
        Assert.Equal (2, subView.FrameToScreen ().X);

        // Act
        var mousePosition = new Point (0, 0);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (0, view1.OnMouseEnterCalled);
        Assert.Equal (0, view1.OnMouseLeaveCalled);
        Assert.Equal (0, subView.OnMouseEnterCalled);
        Assert.Equal (0, subView.OnMouseLeaveCalled);

        // Act
        mousePosition = new (1, 1);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (1, view1.OnMouseEnterCalled);
        Assert.Equal (0, view1.OnMouseLeaveCalled);
        Assert.Equal (0, subView.OnMouseEnterCalled);
        Assert.Equal (0, subView.OnMouseLeaveCalled);

        // Act
        mousePosition = new (2, 2);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (1, view1.OnMouseEnterCalled);
        Assert.Equal (0, view1.OnMouseLeaveCalled);
        Assert.Equal (1, subView.OnMouseEnterCalled);
        Assert.Equal (0, subView.OnMouseLeaveCalled);

        // Act
        mousePosition = new (0, 0);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (1, view1.OnMouseEnterCalled);
        Assert.Equal (1, view1.OnMouseLeaveCalled);
        Assert.Equal (1, subView.OnMouseEnterCalled);
        Assert.Equal (1, subView.OnMouseLeaveCalled);

        // Act
        mousePosition = new (2, 2);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (2, view1.OnMouseEnterCalled);
        Assert.Equal (1, view1.OnMouseLeaveCalled);
        Assert.Equal (2, subView.OnMouseEnterCalled);
        Assert.Equal (1, subView.OnMouseLeaveCalled);

        // Act
        mousePosition = new (3, 3);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (2, view1.OnMouseEnterCalled);
        Assert.Equal (2, view1.OnMouseLeaveCalled);
        Assert.Equal (2, subView.OnMouseEnterCalled);
        Assert.Equal (2, subView.OnMouseLeaveCalled);

        // Act
        mousePosition = new (0, 0);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (2, view1.OnMouseEnterCalled);
        Assert.Equal (2, view1.OnMouseLeaveCalled);
        Assert.Equal (2, subView.OnMouseEnterCalled);
        Assert.Equal (2, subView.OnMouseLeaveCalled);

        // Act
        mousePosition = new (2, 2);

        app.Mouse.RaiseMouseEnterLeaveEvents (
                                              mousePosition,
                                              runnable.GetViewsUnderLocation (mousePosition, ViewportSettingsFlags.TransparentMouse));

        // Assert
        Assert.Equal (3, view1.OnMouseEnterCalled);
        Assert.Equal (2, view1.OnMouseLeaveCalled);
        Assert.Equal (3, subView.OnMouseEnterCalled);
        Assert.Equal (2, subView.OnMouseLeaveCalled);

    }
}

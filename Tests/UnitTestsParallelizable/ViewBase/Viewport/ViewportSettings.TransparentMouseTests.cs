namespace ViewBaseTests.MouseTests;

public class TransparentMouseTests
{
    private class MouseTrackingView : View
    {
        public bool MouseEventReceived { get; private set; }

        protected override bool OnMouseEvent (Mouse mouse)
        {
            MouseEventReceived = true;

            return true;
        }
    }

    [Fact]
    public void TransparentMouse_Passes_Mouse_Events_To_Underlying_View ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        var top = new Runnable { Id = "top" };
        app.Begin (top);

        var underlying = new MouseTrackingView
        {
            Id = "underlying",
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };

        var overlay = new MouseTrackingView
        {
            Id = "overlay",
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            ViewportSettings = ViewportSettingsFlags.TransparentMouse
        };

        top.Add (underlying);
        top.Add (overlay);

        top.Layout ();

        var mouse = new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonClicked };

        // Act
        app.Mouse.RaiseMouseEvent (mouse);

        // Assert
        Assert.True (underlying.MouseEventReceived);
    }

    [Fact]
    public void NonTransparentMouse_Consumes_Mouse_Events ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        var top = new Runnable ();
        app.Begin (top);

        var underlying = new MouseTrackingView { X = 0, Y = 0, Width = 10, Height = 10 };

        var overlay = new MouseTrackingView
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            ViewportSettings = ViewportSettingsFlags.None
        };

        top.Add (underlying);
        top.Add (overlay);

        top.Layout ();

        var mouse = new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonClicked };

        // Act
        app.Mouse.RaiseMouseEvent (mouse);

        // Assert
        Assert.True (overlay.MouseEventReceived);
        Assert.False (underlying.MouseEventReceived);
    }

    [Fact]
    public void TransparentMouse_Stacked_TransparentMouse_Views ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        var top = new Runnable { Id = "top" };
        app.Begin (top);

        var underlying = new MouseTrackingView
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            ViewportSettings = ViewportSettingsFlags.TransparentMouse
        };

        var overlay = new MouseTrackingView
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            ViewportSettings = ViewportSettingsFlags.TransparentMouse
        };

        top.Add (underlying);
        top.Add (overlay);

        top.Layout ();

        var mouse = new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonClicked };

        var topHandled = false;

        top.MouseEvent += (sender, args) =>
                          {
                              topHandled = true;
                              args.Handled = true;
                          };

        // Act
        app.Mouse.RaiseMouseEvent (mouse);

        // Assert
        Assert.False (overlay.MouseEventReceived);
        Assert.False (underlying.MouseEventReceived);
        Assert.True (topHandled);
    }
}

using System.ComponentModel;

namespace ViewBaseTests.MouseTests;

[Trait ("Category", "Input")]
public class MouseEnterLeaveTests
{
    private class TestView : View
    {
        public TestView ()
        {
            MouseEnter += OnMouseEnterHandler;
            MouseLeave += OnMouseLeaveHandler;
        }

        public bool CancelOnEnter { get; init; }

        public bool CancelEnterEvent { get; init; }

        public bool OnMouseEnterCalled { get; private set; }
        public bool OnMouseLeaveCalled { get; private set; }

        protected override bool OnMouseEnter (CancelEventArgs eventArgs)
        {
            OnMouseEnterCalled = true;
            eventArgs.Cancel = CancelOnEnter;

            base.OnMouseEnter (eventArgs);

            return eventArgs.Cancel;
        }

        protected override void OnMouseLeave ()
        {
            OnMouseLeaveCalled = true;

            base.OnMouseLeave ();
        }

        public bool MouseEnterRaised { get; private set; }
        public bool MouseLeaveRaised { get; private set; }

        private void OnMouseEnterHandler (object? s, CancelEventArgs e)
        {
            MouseEnterRaised = true;

            if (CancelEnterEvent)
            {
                e.Cancel = true;
            }
        }

        private void OnMouseLeaveHandler (object? s, EventArgs e) { MouseLeaveRaised = true; }
    }

    [Fact]
    public void NewMouseEnterEvent_ViewIsEnabledAndVisible_CallsOnMouseEnter ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = true
        };

        var mouse = new Mouse ();

        var eventArgs = new CancelEventArgs ();

        // Act
        bool? cancelled = view.NewMouseEnterEvent (eventArgs);

        // Assert
        Assert.True (view.OnMouseEnterCalled);
        Assert.False (cancelled);
        Assert.False (eventArgs.Cancel);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseEnterEvent_ViewIsDisabled_DoesNotCallOnMouseEnter ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = false,
            Visible = true
        };

        var eventArgs = new CancelEventArgs ();

        // Act
        bool? cancelled = view.NewMouseEnterEvent (eventArgs);

        // Assert
        Assert.False (view.OnMouseEnterCalled);
        Assert.Null (cancelled);
        Assert.False (eventArgs.Cancel);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseEnterEvent_ViewIsNotVisible_DoesNotCallOnMouseEnter ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = false
        };

        var eventArgs = new CancelEventArgs ();

        // Act
        bool? cancelled = view.NewMouseEnterEvent (eventArgs);

        // Assert
        Assert.False (view.OnMouseEnterCalled);
        Assert.Null (cancelled);
        Assert.False (eventArgs.Cancel);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseLeaveEvent_ViewIsVisible_CallsOnMouseLeave ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true, Visible = true
        };

        var mouse = new Mouse ();

        // Act
        view.NewMouseLeaveEvent ();

        // Assert
        Assert.True (view.OnMouseLeaveCalled);
        Assert.False (mouse.Handled);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseLeaveEvent_ViewIsNotVisible_CallsOnMouseLeave ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = false
        };

        var mouse = new Mouse ();

        // Act
        view.NewMouseLeaveEvent ();

        // Assert
        Assert.True (view.OnMouseLeaveCalled);
        Assert.False (mouse.Handled);

        // Cleanup
        view.Dispose ();
    }

    // Events

    [Fact]
    public void NewMouseEnterEvent_ViewIsEnabledAndVisible_RaisesMouseEnter ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = true
        };

        var eventArgs = new CancelEventArgs ();

        // Act
        bool? cancelled = view.NewMouseEnterEvent (eventArgs);

        // Assert
        Assert.True (view.MouseEnterRaised);
        Assert.False (cancelled);
        Assert.False (eventArgs.Cancel);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseEnterEvent_ViewIsDisabled_DoesNotRaiseMouseEnter ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = false,
            Visible = true
        };

        var eventArgs = new CancelEventArgs ();

        // Act
        bool? cancelled = view.NewMouseEnterEvent (eventArgs);

        // Assert
        Assert.False (view.MouseEnterRaised);
        Assert.Null (cancelled);
        Assert.False (eventArgs.Cancel);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseEnterEvent_ViewIsNotVisible_DoesNotRaiseMouseEnter ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = false
        };

        var eventArgs = new CancelEventArgs ();

        // Act
        bool? cancelled = view.NewMouseEnterEvent (eventArgs);

        // Assert
        Assert.False (view.MouseEnterRaised);
        Assert.Null (cancelled);
        Assert.False (eventArgs.Cancel);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseLeaveEvent_ViewIsVisible_RaisesMouseLeave ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = true
        };

        var mouse = new Mouse ();

        // Act
        view.NewMouseLeaveEvent ();

        // Assert
        Assert.True (view.MouseLeaveRaised);
        Assert.False (mouse.Handled);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseLeaveEvent_ViewIsNotVisible_RaisesMouseLeave ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = false
        };

        var mouse = new Mouse ();

        // Act
        view.NewMouseLeaveEvent ();

        // Assert
        Assert.True (view.MouseLeaveRaised);
        Assert.False (mouse.Handled);

        // Cleanup
        view.Dispose ();
    }

    // Handled tests
    [Fact]
    public void NewMouseEnterEvent_HandleOnMouseEnter_Event_Not_Raised ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = true,
            CancelOnEnter = true
        };

        var eventArgs = new CancelEventArgs ();

        // Act
        bool? cancelled = view.NewMouseEnterEvent (eventArgs);

        // Assert
        Assert.True (view.OnMouseEnterCalled);
        Assert.True (cancelled);
        Assert.True (eventArgs.Cancel);

        Assert.False (view.MouseEnterRaised);

        // Cleanup
        view.Dispose ();
    }

    [Fact]
    public void NewMouseEnterEvent_HandleMouseEnterEvent ()
    {
        // Arrange
        var view = new TestView
        {
            Enabled = true,
            Visible = true,
            CancelEnterEvent = true
        };

        var eventArgs = new CancelEventArgs ();

        // Act
        bool? cancelled = view.NewMouseEnterEvent (eventArgs);

        // Assert
        Assert.True (view.OnMouseEnterCalled);
        Assert.True (cancelled);
        Assert.True (eventArgs.Cancel);

        Assert.True (view.MouseEnterRaised);

        // Cleanup
        view.Dispose ();
    }
}

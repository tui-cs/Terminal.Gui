namespace ViewBaseTests.Layout;

public class FrameTests
{
    [Fact]
    public void Frame_Empty_Default ()
    {
        View view = new ();
        Assert.Equal (Rectangle.Empty, view.Frame);

        view.BeginInit ();
        view.EndInit ();
        Assert.Equal (Rectangle.Empty, view.Frame);
    }

    [Fact]
    public void Frame_Empty_Initializer_Overrides_Base ()
    {
        // Prove TestView is correct
        FrameTestView view = new ();
        Assert.True (view.NeedsLayout);

        view.Layout ();
        Assert.Equal (new (10, 20, 30, 40), view.Frame);
        Assert.Equal (10, view.X.GetAnchor (0));
        Assert.Equal (20, view.Y.GetAnchor (0));
        Assert.Equal (30, view.Width!.GetAnchor (0));
        Assert.Equal (40, view.Height!.GetAnchor (0));
        Assert.Equal (new (0, 0, 30, 40), view.Viewport);

        // Set Frame via init
        Rectangle frame = new (1, 2, 3, 4);

        view = new ()
        {
            Frame = frame
        };
        Assert.Equal (frame, view.Frame);
        Assert.False (view.NeedsLayout);
        Assert.Equal (frame.Size, view.Viewport.Size);

        Assert.Equal (view.X, frame.X);
        Assert.Equal (view.Y, frame.Y);
        Assert.Equal (view.Width, frame.Width);
        Assert.Equal (view.Height, frame.Height);

        // Set Frame via init to empty
        frame = Rectangle.Empty;

        view = new ()
        {
            Frame = frame
        };
        Assert.Equal (frame, view.Frame);
        Assert.False (view.NeedsLayout);
        Assert.Equal (frame.Size, view.Viewport.Size);

        Assert.Equal (view.X, frame.X);
        Assert.Equal (view.Y, frame.Y);
        Assert.Equal (view.Width, frame.Width);
        Assert.Equal (view.Height, frame.Height);

        // Set back to original state
        view.X = Pos.Func (_ => 10);
        view.Y = Pos.Func (_ => 20);
        view.Width = Dim.Func (_ => 30);
        view.Height = Dim.Func (_ => 40);
        Assert.True (view.NeedsLayout);

        view.Layout ();
        Assert.Equal (new (10, 20, 30, 40), view.Frame);
        Assert.Equal (10, view.X.GetAnchor (0));
        Assert.Equal (20, view.Y.GetAnchor (0));
        Assert.Equal (30, view.Width!.GetAnchor (0));
        Assert.Equal (40, view.Height!.GetAnchor (0));
        Assert.Equal (new (0, 0, 30, 40), view.Viewport);

        view.Frame = frame;
        Assert.Equal (frame, view.Frame);
        Assert.False (view.NeedsLayout);
        Assert.Equal (frame.Size, view.Viewport.Size);

        Assert.Equal (view.X, frame.X);
        Assert.Equal (view.Y, frame.Y);
        Assert.Equal (view.Width, frame.Width);
        Assert.Equal (view.Height, frame.Height);
    }

    [Fact]
    public void Frame_Empty_Initializer_Sets ()
    {
        Rectangle frame = new (1, 2, 3, 4);

        View view = new ()
        {
            Frame = frame
        };

        Assert.Equal (frame, view.Frame);
        Assert.False (view.NeedsLayout);
        Assert.Equal (frame.Size, view.Viewport.Size);

        Assert.Equal (view.X, frame.X);
        Assert.Equal (view.Y, frame.Y);
        Assert.Equal (view.Width, frame.Width);
        Assert.Equal (view.Height, frame.Height);

        view.Frame = Rectangle.Empty;
        Assert.Equal (Rectangle.Empty, view.Frame);
        Assert.False (view.NeedsLayout);
        Assert.Equal (Rectangle.Empty.Size, view.Viewport.Size);

        Assert.Equal (view.X, Rectangle.Empty.X);
        Assert.Equal (view.Y, Rectangle.Empty.Y);
        Assert.Equal (view.Width, Rectangle.Empty.Width);
        Assert.Equal (view.Height, Rectangle.Empty.Height);

        view.Width = Dim.Fill ();
        view.Height = Dim.Fill ();
        Assert.True (view.NeedsLayout);
        view.Layout ();
        Assert.False (view.NeedsLayout);
        Assert.Equal (new Size (2048, 2048), view.Frame.Size);

        view.Frame = Rectangle.Empty;
        Assert.Equal (Rectangle.Empty, view.Frame);
        Assert.False (view.NeedsLayout);
        Assert.Equal (Rectangle.Empty.Size, view.Viewport.Size);

        Assert.Equal (view.X, Rectangle.Empty.X);
        Assert.Equal (view.Y, Rectangle.Empty.Y);
        Assert.Equal (view.Width, Rectangle.Empty.Width);
        Assert.Equal (view.Height, Rectangle.Empty.Height);
    }

    [Fact]
    public void Frame_Initializer_Sets ()
    {
        Rectangle frame = new (1, 2, 3, 4);

        View view = new ()
        {
            Frame = frame
        };

        Assert.Equal (frame, view.Frame);
        Assert.False (view.NeedsLayout);
        Assert.Equal (frame.Size, view.Viewport.Size);

        Assert.Equal (view.X, frame.X);
        Assert.Equal (view.Y, frame.Y);
        Assert.Equal (view.Width, frame.Width);
        Assert.Equal (view.Height, frame.Height);
    }

    // Moved this test from AbsoluteLayoutTests
    // TODO: Refactor as Theory
    [Fact]
    public void Frame_Set ()
    {
        var frame = new Rectangle (1, 2, 3, 4);
        var newFrame = new Rectangle (1, 2, 30, 40);

        var v = new View ();
        Assert.Equal (Rectangle.Empty, v.Frame);
        v.Dispose ();

        v = new () { Frame = frame };
        Assert.Equal (frame, v.Frame);

        v.Frame = newFrame;
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new (0, 0, newFrame.Width, newFrame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (30), v.Width);
        Assert.Equal (Dim.Absolute (40), v.Height);
        v.Dispose ();

        v = new () { X = frame.X, Y = frame.Y, Text = "v" };
        v.Frame = newFrame;
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new (0, 0, newFrame.Width, newFrame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (30), v.Width);
        Assert.Equal (Dim.Absolute (40), v.Height);
        v.Dispose ();

        newFrame = new (10, 20, 30, 40);
        v = new () { Frame = frame };
        v.Frame = newFrame;
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new (0, 0, newFrame.Width, newFrame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (10), v.X);
        Assert.Equal (Pos.Absolute (20), v.Y);
        Assert.Equal (Dim.Absolute (30), v.Width);
        Assert.Equal (Dim.Absolute (40), v.Height);
        v.Dispose ();

        v = new () { X = frame.X, Y = frame.Y, Text = "v" };
        v.Frame = newFrame;
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new (0, 0, newFrame.Width, newFrame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (10), v.X);
        Assert.Equal (Pos.Absolute (20), v.Y);
        Assert.Equal (Dim.Absolute (30), v.Width);
        Assert.Equal (Dim.Absolute (40), v.Height);
        v.Dispose ();
    }

    [Fact]
    public void Frame_Set_Sets ()
    {
        Rectangle frame = new (1, 2, 3, 4);
        View view = new ();
        Assert.True (view.NeedsLayout);
        Assert.Equal (Rectangle.Empty, view.Frame);

        view.Frame = frame;
        Assert.Equal (frame, view.Frame);
        Assert.False (view.NeedsLayout);

        Assert.Equal (view.X, frame.X);
        Assert.Equal (view.Y, frame.Y);
        Assert.Equal (view.Width, frame.Width);
        Assert.Equal (view.Height, frame.Height);
    }


    [Fact]
    public void Frame_Set_Sets_Viewport_Without_Layout ()
    {
        Rectangle frame = new (1, 2, 3, 4);

        View v = new () { Frame = frame };
        Assert.Equal (frame, v.Frame);

        Assert.Equal (
                      new (0, 0, frame.Width, frame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (3), v.Width);
        Assert.Equal (Dim.Absolute (4), v.Height);
    }

    [Fact]
    public void FrameChanged_Event_Raised_When_Frame_Changes ()
    {
        // Arrange
        var view = new TestFrameEventsView ();
        var initialFrame = new Rectangle (0, 0, 10, 10);
        var newFrame = new Rectangle (0, 0, 20, 20);
        view.Frame = initialFrame;
        Assert.Equal (1, view.FrameChangedEventCallCount);

        // Act
        view.Frame = newFrame;

        // Assert
        Assert.Equal (2, view.FrameChangedEventCallCount);
    }

    [Fact]
    public void OnFrameChanged_Called_When_Frame_Changes ()
    {
        // Arrange
        var view = new TestFrameEventsView ();
        var initialFrame = new Rectangle (0, 0, 10, 10);
        var newFrame = new Rectangle (0, 0, 20, 20);
        view.Frame = initialFrame;
        Assert.Equal (1, view.OnFrameChangedCallCount);

        // Act
        view.Frame = newFrame;

        // Assert
        Assert.Equal (2, view.OnFrameChangedCallCount);
    }

    private class FrameTestView : View
    {
        public FrameTestView ()
        {
            X = Pos.Func (_ => 10);
            Y = Pos.Func (_ => 20);
            Width = Dim.Func (_ => 30);
            Height = Dim.Func (_ => 40);
        }
    }

    private class TestFrameEventsView : View
    {
        public TestFrameEventsView () { FrameChanged += (sender, args) => FrameChangedEventCallCount++; }

        public int FrameChangedEventCallCount { get; private set; }
        public int OnFrameChangedCallCount { get; private set; }

        protected override void OnFrameChanged (in Rectangle frame)
        {
            OnFrameChangedCallCount++;
            base.OnFrameChanged (frame);
        }
    }
}

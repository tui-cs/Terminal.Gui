namespace ViewBaseTests.Viewport;

/// <summary>
///     Test the <see cref="View.Viewport"/>.
///     DOES NOT TEST Adornment.Viewport methods. Those are in ./Adornment/ViewportTests.cs
/// </summary>
/// <param name="output"></param>
public class ViewportTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [CombinatorialData]
    public void Get_Viewport_NoSuperView_WithoutAdornments ([CombinatorialRange (-11, 11)] int x)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (x, 0, 10, 10);

        var view = new View ();
        view.Frame = frame;
        view.BeginInit ();
        view.EndInit ();

        // Act
        Rectangle bounds = view.Viewport;

        // Assert
        Assert.Equal (10, bounds.Width);
    }

    [Theory]
    [InlineData (0, 0, 10)]
    [InlineData (1, 0, 9)]
    [InlineData (-1, 0, 11)]
    [InlineData (10, 0, 0)]
    [InlineData (11, 0, 0)]
    [InlineData (0, 1, 6)]
    [InlineData (1, 1, 5)]
    [InlineData (-1, 1, 7)]
    [InlineData (10, 1, 0)]
    [InlineData (11, 1, 0)]
    public void Get_Viewport_NestedSuperView_WithAdornments (int frameX, int borderThickness, int expectedW)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var superSuperView = new View { X = 0, Y = 0, Height = 10, Width = 10 };
        superSuperView.Border.Thickness = new Thickness (borderThickness);

        var superView = new View { X = 0, Y = 0, Height = Dim.Fill (), Width = Dim.Fill () };
        superView.Border.Thickness = new Thickness (borderThickness);

        superSuperView.Add (superView);

        var view = new View { X = frameX, Y = 0, Height = Dim.Fill (), Width = Dim.Fill () };

        superView.Add (view);
        superSuperView.BeginInit ();
        superSuperView.EndInit ();
        superSuperView.LayoutSubViews ();

        // Act
        Rectangle bounds = view.Viewport;

        // Assert
        Assert.Equal (expectedW, bounds.Width);
    }

    [Theory]
    [InlineData (0, 0, 10)]
    [InlineData (1, 0, 9)]
    [InlineData (-1, 0, 11)]
    [InlineData (10, 0, 0)]
    [InlineData (11, 0, 0)]
    [InlineData (0, 1, 4)]
    [InlineData (1, 1, 3)]
    [InlineData (-1, 1, 5)]
    [InlineData (10, 1, 0)]
    [InlineData (11, 1, 0)]
    public void Get_Viewport_NestedSuperView_WithAdornments_WithBorder (int frameX, int borderThickness, int expectedW)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var superSuperView = new View { X = 0, Y = 0, Height = 10, Width = 10 };
        superSuperView.Border.Thickness = new Thickness (borderThickness);

        var superView = new View { X = 0, Y = 0, Height = Dim.Fill (), Width = Dim.Fill () };
        superView.Border.Thickness = new Thickness (borderThickness);

        superSuperView.Add (superView);

        var view = new View { X = frameX, Y = 0, Height = Dim.Fill (), Width = Dim.Fill () };
        view.Border.Thickness = new Thickness (borderThickness);

        superView.Add (view);
        superSuperView.BeginInit ();
        superSuperView.EndInit ();
        superSuperView.LayoutSubViews ();

        // Act
        Rectangle bounds = view.Viewport;

        // Assert
        Assert.Equal (expectedW, bounds.Width);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 0)]
    [InlineData (0, 1)]
    [InlineData (-1, -1)]
    public void Set_Viewport_Location_Preserves_Size_And_Frame (int xOffset, int yOffset)
    {
        View view = new ()
        {
            Width = 10,
            Height = 10,
            ViewportSettings = ViewportSettingsFlags.AllowNegativeLocation | ViewportSettingsFlags.AllowLocationPlusSizeGreaterThanContentSize
        };
        view.Layout ();

        Assert.Equal (new Rectangle (0, 0, 10, 10), view.Frame);

        var testRect = new Rectangle (0, 0, 1, 1);
        Assert.Equal (new Point (0, 0), view.ViewportToScreen (testRect).Location);
        view.Viewport = view.Viewport with { Location = new Point (xOffset, yOffset) };
        Assert.Equal (new Rectangle (xOffset, yOffset, 10, 10), view.Viewport);

        Assert.Equal (new Rectangle (0, 0, 10, 10), view.Frame);
    }

    [Fact]
    public void Set_Viewport_Changes_Frame ()
    {
        var frame = new Rectangle (1, 2, 3, 4);
        var newViewport = new Rectangle (0, 0, 30, 40);

        var v = new View { Frame = frame };

        v.Viewport = newViewport;
        Assert.Equal (newViewport, v.Viewport);
        Assert.Equal (new Rectangle (1, 2, newViewport.Width, newViewport.Height), v.Frame);
        Assert.Equal (new Rectangle (0, 0, newViewport.Width, newViewport.Height), v.Viewport);
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (30), v.Width);
        Assert.Equal (Dim.Absolute (40), v.Height);

        newViewport = new Rectangle (0, 0, 3, 4);
        v.Viewport = newViewport;
        Assert.Equal (newViewport, v.Viewport);
        Assert.Equal (new Rectangle (1, 2, newViewport.Width, newViewport.Height), v.Frame);
        Assert.Equal (new Rectangle (0, 0, newViewport.Width, newViewport.Height), v.Viewport);
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (3), v.Width);
        Assert.Equal (Dim.Absolute (4), v.Height);

        v.BorderStyle = LineStyle.Single;

        // Viewport should shrink
        Assert.Equal (new Rectangle (0, 0, 1, 2), v.Viewport);

        // Frame should not change
        Assert.Equal (new Rectangle (1, 2, 3, 4), v.Frame);
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (3), v.Width);
        Assert.Equal (Dim.Absolute (4), v.Height);

        // Now set bounds bigger as before
        newViewport = new Rectangle (0, 0, 3, 4);
        v.Viewport = newViewport;
        Assert.Equal (newViewport, v.Viewport);

        // Frame grows because there's now a border
        Assert.Equal (new Rectangle (1, 2, 5, 6), v.Frame);
        Assert.Equal (new Rectangle (0, 0, newViewport.Width, newViewport.Height), v.Viewport);
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (5), v.Width);
        Assert.Equal (Dim.Absolute (6), v.Height);
    }

    [Theory]
    [InlineData (0, 0, 10, 10, 0, 0)]
    [InlineData (10, 0, 10, 10, 0, 0)] // 0 because without AllowXPlusWidthGreaterThanContentWidth, X is clamped so X + Width <= ContentSize.Width
    [InlineData (0, 10, 10, 10, 0, 0)]
    [InlineData (10, 10, 10, 10, 0, 0)]
    public void Set_Viewport_ValidValue_UpdatesViewport (int viewWidth, int viewHeight, int viewportX, int viewportY, int expectedX, int expectedY)
    {
        // Arrange
        var view = new View { Width = viewWidth, Height = viewHeight };
        view.Layout ();
        var newViewport = new Rectangle (viewportX, viewportY, viewWidth, viewHeight);

        // Act
        view.Viewport = newViewport;

        // Assert
        Assert.Equal (new Rectangle (expectedX, expectedY, viewWidth, viewHeight), view.Viewport);
    }

    [Theory]
    [CombinatorialData]
    public void Set_Viewport_ValidValue_UpdatesViewport_AllowLocationGreaterThanContentSize ([CombinatorialRange (0, 5)] int viewWidth,
                                                                                             [CombinatorialRange (0, 5)] int viewHeight)
    {
        // Arrange
        var view = new View
        {
            Width = viewWidth,
            Height = viewHeight,
            ViewportSettings = ViewportSettingsFlags.AllowLocationGreaterThanContentSize | ViewportSettingsFlags.AllowLocationPlusSizeGreaterThanContentSize
        };
        var newViewport = new Rectangle (10, 10, viewWidth, viewHeight);

        // Act
        view.Viewport = newViewport;

        // Assert
        Assert.Equal (new Rectangle (10, 10, viewWidth, viewHeight), view.Viewport);
    }

    [Fact]
    public void Set_Viewport_ValueGreaterThanContentSize_UpdatesViewportToContentSize ()
    {
        // Arrange
        var view = new View ();
        view.SetContentSize (new Size (100, 100));
        var newViewport = new Rectangle (0, 0, 200, 200);
        view.ViewportSettings = ViewportSettingsFlags.AllowLocationGreaterThanContentSize;

        // Act
        view.Viewport = newViewport;

        // Assert
        Assert.Equal (newViewport, view.Viewport);
    }

    [Fact]
    public void Set_Viewport_NegativeValue_AllowedBySettings ()
    {
        // Arrange
        var view = new View ();
        var newViewport = new Rectangle (-10, -10, 100, 100);
        view.ViewportSettings = ViewportSettingsFlags.AllowNegativeLocation | ViewportSettingsFlags.AllowLocationPlusSizeGreaterThanContentSize;

        // Act
        view.Viewport = newViewport;

        // Assert
        Assert.Equal (newViewport, view.Viewport);
    }

    [Fact]
    public void Set_Viewport_NegativeValue_NotAllowedBySettings ()
    {
        // Arrange
        var view = new View ();
        var newViewport = new Rectangle (-10, -10, 100, 100);
        view.ViewportSettings = ViewportSettingsFlags.None;

        // Act
        view.Viewport = newViewport;

        // Assert
        Assert.Equal (new Rectangle (0, 0, 100, 100), view.Viewport);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    public void GetViewportOffset_Returns_Offset_From_Frame (int adornmentThickness, int expectedOffset)
    {
        View view = new () { X = 1, Y = 1, Width = 10, Height = 10 };
        view.BeginInit ();
        view.EndInit ();
        view.Margin.Thickness = new Thickness (adornmentThickness);

        Assert.Equal (expectedOffset, view.GetViewportOffsetFromFrame ().X);
    }

    [Fact]
    public void ContentSize_Tracks_ViewportSize_If_Not_Set ()
    {
        View view = new () { Width = 1, Height = 1 };
        Assert.True (view.ContentSizeTracksViewport);
        Assert.Equal (view.Viewport.Size, view.GetContentSize ());
    }

    [Fact]
    public void ContentSize_Ignores_ViewportSize_If_Set ()
    {
        View view = new () { Width = 1, Height = 1 };
        view.SetContentSize (new Size (5, 5));
        Assert.False (view.ContentSizeTracksViewport);
        Assert.NotEqual (view.Viewport.Size, view.GetContentSize ());
    }

    [Fact]
    public void ContentSize_Tracks_ViewportSize_If_ContentSizeTracksViewport_Is_True ()
    {
        View view = new () { Width = 1, Height = 1 };
        view.SetContentSize (new Size (5, 5));
        view.Viewport = new Rectangle (0, 0, 10, 10);
        view.ContentSizeTracksViewport = true;
        Assert.Equal (view.Viewport.Size, view.GetContentSize ());
    }

    [Fact]
    public void ContentSize_Ignores_ViewportSize_If_ContentSizeTracksViewport_Is_False ()
    {
        View view = new () { Width = 1, Height = 1 };
        view.SetContentSize (new Size (5, 5));
        view.Viewport = new Rectangle (0, 0, 10, 10);
        view.ContentSizeTracksViewport = false;
        Assert.NotEqual (view.Viewport.Size, view.GetContentSize ());
    }

    private class TestViewportEventsView : View
    {
        public int OnViewportChangedCallCount { get; private set; }
        public int ViewportChangedEventCallCount { get; private set; }

        public TestViewportEventsView () => ViewportChanged += (_, _) => ViewportChangedEventCallCount++;

        protected override void OnViewportChanged (DrawEventArgs e)
        {
            OnViewportChangedCallCount++;
            base.OnViewportChanged (e);
        }
    }

    [Fact]
    public void OnViewportChanged_Called_When_Viewport_Changes ()
    {
        // Arrange
        var view = new TestViewportEventsView ();
        var initialViewport = new Rectangle (0, 0, 10, 10);
        var newViewport = new Rectangle (0, 0, 20, 20);
        Assert.Equal (0, view.OnViewportChangedCallCount);
        view.Viewport = initialViewport;
        Assert.Equal (1, view.OnViewportChangedCallCount);

        // Act
        view.Viewport = newViewport;

        // Assert
        Assert.Equal (2, view.OnViewportChangedCallCount);
    }

    [Fact]
    public void ViewportChanged_Event_Raised_When_Viewport_Changes ()
    {
        // Arrange
        var view = new TestViewportEventsView ();
        var initialViewport = new Rectangle (0, 0, 10, 10);
        var newViewport = new Rectangle (0, 0, 20, 20);
        view.Viewport = initialViewport;
        Assert.Equal (1, view.ViewportChangedEventCallCount);

        // Act
        view.Viewport = newViewport;

        // Assert
        Assert.Equal (2, view.ViewportChangedEventCallCount);
    }

    [Fact]
    public void OnViewportChanged_Called_When_Frame_Changes ()
    {
        // Arrange
        var view = new TestViewportEventsView ();
        var initialFrame = new Rectangle (0, 0, 10, 10);
        var newFrame = new Rectangle (0, 0, 20, 20);
        Assert.Equal (0, view.OnViewportChangedCallCount);
        view.Frame = initialFrame;
        Assert.Equal (1, view.OnViewportChangedCallCount);

        // Act
        view.Frame = newFrame;

        // Assert
        Assert.Equal (2, view.OnViewportChangedCallCount);
    }

    [Fact]
    public void ViewportChanged_Event_Raised_When_Frame_Changes ()
    {
        // Arrange
        var view = new TestViewportEventsView ();
        var initialFrame = new Rectangle (0, 0, 10, 10);
        var newFrame = new Rectangle (0, 0, 20, 20);
        view.Frame = initialFrame;
        Assert.Equal (1, view.ViewportChangedEventCallCount);

        // Act
        view.Frame = newFrame;

        // Assert
        Assert.Equal (2, view.ViewportChangedEventCallCount);
    }

    #region AllowXPlusWidthGreaterThanContentWidth and AllowYPlusHeightGreaterThanContentHeight tests

    [Fact]
    public void Set_Viewport_X_ClampsToPreventBlankSpace_ByDefault ()
    {
        // Arrange - View with content that fits exactly in viewport
        var view = new View { Width = 10, Height = 10 };
        view.SetContentSize (new Size (20, 20)); // Content larger than viewport
        view.Layout ();

        // Act - Try to set viewport X such that X + Width > ContentSize.Width
        // X=15, Width=10, ContentSize.Width=20 -> 15+10=25 > 20, should clamp to X=10
        view.Viewport = view.Viewport with { X = 15 };

        // Assert - Should be clamped so X + Width = ContentSize.Width
        Assert.Equal (10, view.Viewport.X); // 10 + 10 = 20 = ContentSize.Width
    }

    [Fact]
    public void Set_Viewport_Y_ClampsToPreventBlankSpace_ByDefault ()
    {
        // Arrange - View with content that fits exactly in viewport
        var view = new View { Width = 10, Height = 10 };
        view.SetContentSize (new Size (20, 20)); // Content larger than viewport
        view.Layout ();

        // Act - Try to set viewport Y such that Y + Height > ContentSize.Height
        // Y=15, Height=10, ContentSize.Height=20 -> 15+10=25 > 20, should clamp to Y=10
        view.Viewport = view.Viewport with { Y = 15 };

        // Assert - Should be clamped so Y + Height = ContentSize.Height
        Assert.Equal (10, view.Viewport.Y); // 10 + 10 = 20 = ContentSize.Height
    }

    [Fact]
    public void Set_Viewport_X_AllowsOverscroll_WhenFlagSet ()
    {
        // Arrange
        var view = new View { Width = 10, Height = 10, ViewportSettings = ViewportSettingsFlags.AllowXPlusWidthGreaterThanContentWidth };
        view.SetContentSize (new Size (20, 20));
        view.Layout ();

        // Act - Set viewport X such that X + Width > ContentSize.Width
        view.Viewport = view.Viewport with { X = 15 };

        // Assert - Should NOT be clamped because flag is set
        Assert.Equal (15, view.Viewport.X);
    }

    [Fact]
    public void Set_Viewport_Y_AllowsOverscroll_WhenFlagSet ()
    {
        // Arrange
        var view = new View { Width = 10, Height = 10, ViewportSettings = ViewportSettingsFlags.AllowYPlusHeightGreaterThanContentHeight };
        view.SetContentSize (new Size (20, 20));
        view.Layout ();

        // Act - Set viewport Y such that Y + Height > ContentSize.Height
        view.Viewport = view.Viewport with { Y = 15 };

        // Assert - Should NOT be clamped because flag is set
        Assert.Equal (15, view.Viewport.Y);
    }

    [Fact]
    public void Set_Viewport_Location_AllowsOverscroll_WhenCombinedFlagSet ()
    {
        // Arrange
        var view = new View { Width = 10, Height = 10, ViewportSettings = ViewportSettingsFlags.AllowLocationPlusSizeGreaterThanContentSize };
        view.SetContentSize (new Size (20, 20));
        view.Layout ();

        // Act - Set viewport location such that X + Width and Y + Height exceed content size
        view.Viewport = view.Viewport with { X = 15, Y = 15 };

        // Assert - Neither should be clamped because combined flag is set
        Assert.Equal (15, view.Viewport.X);
        Assert.Equal (15, view.Viewport.Y);
    }

    [Fact]
    public void Set_Viewport_X_ClampsCorrectly_WhenViewportSizeEqualsContentSize ()
    {
        // Arrange - ContentSize tracks Viewport (default behavior)
        var view = new View { Width = 10, Height = 10 };
        view.Layout ();

        // ContentSize = Viewport.Size = 10x10

        // Act - Try to set positive viewport X
        // X=1, Width=10, ContentSize.Width=10 -> 1+10=11 > 10, should clamp to X=0
        view.Viewport = view.Viewport with { X = 1 };

        // Assert - Should be clamped to 0
        Assert.Equal (0, view.Viewport.X);
    }

    [Fact]
    public void Set_Viewport_Y_ClampsCorrectly_WhenViewportSizeEqualsContentSize ()
    {
        // Arrange - ContentSize tracks Viewport (default behavior)
        var view = new View { Width = 10, Height = 10 };
        view.Layout ();

        // ContentSize = Viewport.Size = 10x10

        // Act - Try to set positive viewport Y
        // Y=1, Height=10, ContentSize.Height=10 -> 1+10=11 > 10, should clamp to Y=0
        view.Viewport = view.Viewport with { Y = 1 };

        // Assert - Should be clamped to 0
        Assert.Equal (0, view.Viewport.Y);
    }

    [Theory]
    [InlineData (0, 0)] // No scroll - no clamping needed
    [InlineData (5, 5)] // Mid-scroll - within bounds
    [InlineData (10, 10)] // Max scroll - exactly at bound
    [InlineData (15, 10)] // Over-scroll - clamped to max
    public void Set_Viewport_X_ClampsToMaxValidValue (int requestedX, int expectedX)
    {
        // Arrange - ContentSize larger than Viewport
        var view = new View { Width = 10, Height = 10 };
        view.SetContentSize (new Size (20, 20)); // ContentSize.Width = 20, Viewport.Width = 10
        view.Layout ();

        // Act
        view.Viewport = view.Viewport with { X = requestedX };

        // Assert - Max valid X is ContentSize.Width - Viewport.Width = 20 - 10 = 10
        Assert.Equal (expectedX, view.Viewport.X);
    }

    [Theory]
    [InlineData (0, 0)] // No scroll - no clamping needed
    [InlineData (5, 5)] // Mid-scroll - within bounds
    [InlineData (10, 10)] // Max scroll - exactly at bound
    [InlineData (15, 10)] // Over-scroll - clamped to max
    public void Set_Viewport_Y_ClampsToMaxValidValue (int requestedY, int expectedY)
    {
        // Arrange - ContentSize larger than Viewport
        var view = new View { Width = 10, Height = 10 };
        view.SetContentSize (new Size (20, 20)); // ContentSize.Height = 20, Viewport.Height = 10
        view.Layout ();

        // Act
        view.Viewport = view.Viewport with { Y = requestedY };

        // Assert - Max valid Y is ContentSize.Height - Viewport.Height = 20 - 10 = 10
        Assert.Equal (expectedY, view.Viewport.Y);
    }

    #endregion

    #region SetViewport should not grow Frame when adornments consume entire Viewport (Issue #5043)

    [Theory]
    [InlineData (0, 1, 0)] // Padding right=1 → Horizontal=1, Frame.Width=0, Viewport.Width=0
    [InlineData (1, 1, 0)] // Padding right=1 → Horizontal=1, Frame.Width=1, Viewport.Width=0
    [InlineData (2, 1, 1)] // Padding right=1 → Horizontal=1, Frame.Width=2, Viewport.Width=1
    [InlineData (2, 2, 0)] // Padding right=2 → Horizontal=2, Frame.Width=2, Viewport.Width=0
    [InlineData (3, 2, 1)] // Padding right=2 → Horizontal=2, Frame.Width=3, Viewport.Width=1
    [InlineData (2, 3, 0)] // Padding right=3 → Horizontal=3, Frame.Width=2, Viewport.Width=0
    public void Set_Viewport_DoesNotGrowFrame_WhenPaddingThicknessConsumesViewport (int frameSize, int paddingRight, int expectedViewportWidth)
    {
        // Arrange - simulate scrollbar adding padding (right/bottom) that consumes the Viewport
        var view = new View { Frame = new Rectangle (0, 0, frameSize, frameSize) };
        view.Padding.Thickness = new Thickness (0, 0, paddingRight, paddingRight);

        Rectangle originalFrame = view.Frame;

        // Sanity: verify Viewport is as expected
        Assert.Equal (expectedViewportWidth, view.Viewport.Width);
        Assert.Equal (expectedViewportWidth, view.Viewport.Height);

        // Act - setting Viewport to its current value should be a no-op
        view.Viewport = view.Viewport;

        // Assert - Frame must not have grown
        Assert.Equal (originalFrame, view.Frame);
    }

    [Theory]
    [InlineData (0, 1)] // Border=1 → Horizontal=2, Frame.Width=0, Viewport.Width=0
    [InlineData (1, 1)] // Border=1 → Horizontal=2, Frame.Width=1, Viewport.Width=0
    [InlineData (2, 1)] // Border=1 → Horizontal=2, Frame.Width=2, Viewport.Width=0
    [InlineData (3, 1)] // Border=1 → Horizontal=2, Frame.Width=3, Viewport.Width=1
    [InlineData (4, 2)] // Border=2 → Horizontal=4, Frame.Width=4, Viewport.Width=0
    public void Set_Viewport_DoesNotGrowFrame_WhenBorderThicknessConsumesViewport (int frameSize, int borderThickness)
    {
        // Arrange
        var view = new View { Frame = new Rectangle (0, 0, frameSize, frameSize) };
        view.Border.Thickness = new Thickness (borderThickness);

        Rectangle originalFrame = view.Frame;
        int expectedViewportW = Math.Max (0, frameSize - borderThickness * 2);

        // Sanity
        Assert.Equal (expectedViewportW, view.Viewport.Width);

        // Act
        view.Viewport = view.Viewport;

        // Assert
        Assert.Equal (originalFrame, view.Frame);
    }

    [Theory]
    [InlineData (0, 1)] // Margin=1 → Horizontal=2, Frame.Width=0, Viewport.Width=0
    [InlineData (1, 1)] // Margin=1 → Horizontal=2, Frame.Width=1, Viewport.Width=0
    [InlineData (2, 1)] // Margin=1 → Horizontal=2, Frame.Width=2, Viewport.Width=0
    [InlineData (3, 1)] // Margin=1 → Horizontal=2, Frame.Width=3, Viewport.Width=1
    public void Set_Viewport_DoesNotGrowFrame_WhenMarginThicknessConsumesViewport (int frameSize, int marginThickness)
    {
        // Arrange
        var view = new View { Frame = new Rectangle (0, 0, frameSize, frameSize) };
        view.Margin.Thickness = new Thickness (marginThickness);

        Rectangle originalFrame = view.Frame;
        int expectedViewportW = Math.Max (0, frameSize - marginThickness * 2);

        // Sanity
        Assert.Equal (expectedViewportW, view.Viewport.Width);

        // Act
        view.Viewport = view.Viewport;

        // Assert
        Assert.Equal (originalFrame, view.Frame);
    }

    [Theory]
    [InlineData (4, 1, 1, 1)] // All adornments = 1 → total Horizontal=6 > Frame.Width=4 → Viewport.Width=0
    [InlineData (6, 1, 1, 1)] // All = 1 → Horizontal=6, Frame.Width=6, Viewport.Width=0
    [InlineData (7, 1, 1, 1)] // All = 1 → Horizontal=6, Frame.Width=7, Viewport.Width=1
    [InlineData (3, 1, 0, 1)] // Margin=1, Padding=1 → Horizontal=4 > Frame.Width=3 → Viewport.Width=0
    [InlineData (4, 1, 0, 1)] // Margin=1, Padding=1 → Horizontal=4, Frame.Width=4, Viewport.Width=0
    [InlineData (5, 1, 0, 1)] // Margin=1, Padding=1 → Horizontal=4, Frame.Width=5, Viewport.Width=1
    public void Set_Viewport_DoesNotGrowFrame_WhenCombinedAdornmentsConsumeViewport (int frameSize,
                                                                                     int marginThickness,
                                                                                     int borderThickness,
                                                                                     int paddingThickness)
    {
        // Arrange
        var view = new View { Frame = new Rectangle (0, 0, frameSize, frameSize) };
        view.Margin.Thickness = new Thickness (marginThickness);
        view.Border.Thickness = new Thickness (borderThickness);
        view.Padding.Thickness = new Thickness (paddingThickness);

        Rectangle originalFrame = view.Frame;
        int totalThickness = (marginThickness + borderThickness + paddingThickness) * 2;
        int expectedViewportW = Math.Max (0, frameSize - totalThickness);

        // Sanity
        Assert.Equal (expectedViewportW, view.Viewport.Width);

        // Act
        view.Viewport = view.Viewport;

        // Assert
        Assert.Equal (originalFrame, view.Frame);
    }

    [Theory]
    [InlineData (2, 0, 0, 1, 0)] // Padding bottom=1, Frame.Height=2 → Viewport.Height=1, Viewport.Width=2 (only vertical affected)
    [InlineData (2, 0, 0, 0, 1)] // Padding right=1, Frame.Width=2 → Viewport.Width=1, Viewport.Height=2 (only horizontal affected)
    [InlineData (1, 0, 0, 1, 0)] // Padding bottom=1, Frame.Height=1 → Viewport.Height=0 (vertical consumed)
    [InlineData (1, 0, 0, 0, 1)] // Padding right=1, Frame.Width=1 → Viewport.Width=0 (horizontal consumed)
    public void Set_Viewport_DoesNotGrowFrame_WhenAsymmetricPaddingConsumesOneDimension (int frameSize,
                                                                                         int paddingLeft,
                                                                                         int paddingTop,
                                                                                         int paddingBottom,
                                                                                         int paddingRight)
    {
        // Arrange - simulates scrollbar adding thickness in only one direction
        var view = new View { Frame = new Rectangle (0, 0, frameSize, frameSize) };
        view.Padding.Thickness = new Thickness (paddingLeft, paddingTop, paddingRight, paddingBottom);

        Rectangle originalFrame = view.Frame;

        // Act
        view.Viewport = view.Viewport;

        // Assert
        Assert.Equal (originalFrame, view.Frame);
    }

    #endregion
}

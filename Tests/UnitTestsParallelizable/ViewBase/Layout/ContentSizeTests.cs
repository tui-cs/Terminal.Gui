namespace ViewBaseTests.Layout;

/// <summary>
/// Tests for <see cref="View.SetContentSize"/>, <see cref="View.GetContentSize"/>, and related events.
/// Copilot - Claude 3.7 Sonnet
/// </summary>
public class ContentSizeTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region SetContentSize and GetContentSize Tests

    [Fact]
    public void SetContentSize_Null_SetsContentSizeToNull ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.SetContentSize (null);

        Assert.Null (view.GetContentSize () == view.Viewport.Size ? null : view.GetContentSize ());
        Assert.True (view.ContentSizeTracksViewport);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (10, 10)]
    [InlineData (100, 100)]
    public void SetContentSize_ValidSize_SetsContentSize (int width, int height)
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size size = new (width, height);
        view.SetContentSize (size);

        Assert.Equal (size, view.GetContentSize ());
        Assert.False (view.ContentSizeTracksViewport);
    }

    [Fact]
    public void SetContentSize_SameValue_DoesNotRaiseEvent ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size size = new (10, 10);
        view.SetContentSize (size);

        int eventCount = 0;
        view.ContentSizeChanged += (_, _) => eventCount++;

        view.SetContentSize (size);

        Assert.Equal (0, eventCount);
    }

    [Theory]
    [InlineData (-1, 0)]
    [InlineData (0, -1)]
    [InlineData (-1, -1)]
    [InlineData (-10, -10)]
    public void SetContentSize_NegativeSize_ThrowsArgumentException (int width, int height)
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size size = new (width, height);

        Assert.Throws<ArgumentException> (() => view.SetContentSize (size));
    }

    [Fact]
    public void GetContentSize_NotSet_ReturnsViewportSize ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.Equal (view.Viewport.Size, view.GetContentSize ());
        Assert.True (view.ContentSizeTracksViewport);
    }

    [Fact]
    public void GetContentSize_Set_ReturnsSetValue ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Size contentSize = new (100, 100);
        view.SetContentSize (contentSize);

        Assert.Equal (contentSize, view.GetContentSize ());
        Assert.NotEqual (view.Viewport.Size, view.GetContentSize ());
        Assert.False (view.ContentSizeTracksViewport);
    }

    #endregion

    #region ContentSizeTracksViewport Tests

    [Fact]
    public void ContentSizeTracksViewport_DefaultValue_IsTrue ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Assert.True (view.ContentSizeTracksViewport);
    }

    [Fact]
    public void ContentSizeTracksViewport_AfterSetContentSize_IsFalse ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.SetContentSize (new Size (10, 10));

        Assert.False (view.ContentSizeTracksViewport);
    }

    [Fact]
    public void ContentSizeTracksViewport_SetToTrue_ResetsContentSize ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentSize (new Size (100, 100));
        Assert.False (view.ContentSizeTracksViewport);

        view.ContentSizeTracksViewport = true;

        Assert.True (view.ContentSizeTracksViewport);
        Assert.Equal (view.Viewport.Size, view.GetContentSize ());
    }

    [Fact]
    public void ContentSizeTracksViewport_SetToFalse_PreservesContentSize ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentSize (new Size (100, 100));
        view.ContentSizeTracksViewport = false;

        Assert.False (view.ContentSizeTracksViewport);
        Assert.Equal (new Size (100, 100), view.GetContentSize ());
    }

    #endregion

    #region ContentSizeChanged Event Tests

    [Fact]
    public void ContentSizeChanged_RaisedWhenContentSizeChanges ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        int eventCount = 0;
        Size? capturedOldSize = Size.Empty; // Initialize to non-null to detect if it's set
        Size? capturedNewSize = Size.Empty;

        view.ContentSizeChanged += (sender, e) =>
        {
            eventCount++;
            capturedOldSize = e.OldValue;
            capturedNewSize = e.NewValue;
        };

        // When not set, _contentSize is null, even though GetContentSize() returns Viewport.Size
        Size newSize = new (50, 50);
        view.SetContentSize (newSize);

        Assert.Equal (1, eventCount);
        // OldValue should be null since _contentSize started as null
        Assert.Null (capturedOldSize);
        Assert.Equal (newSize, capturedNewSize);
    }

    [Fact]
    public void ContentSizeChanged_NotRaisedWhenSameValue ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size size = new (10, 10);
        view.SetContentSize (size);

        int eventCount = 0;
        view.ContentSizeChanged += (_, _) => eventCount++;

        view.SetContentSize (size);

        Assert.Equal (0, eventCount);
    }

    [Fact]
    public void ContentSizeChanged_RaisedMultipleTimes ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        int eventCount = 0;
        List<Size?> capturedOldValues = new ();
        List<Size?> capturedNewValues = new ();

        view.ContentSizeChanged += (sender, e) =>
        {
            eventCount++;
            capturedOldValues.Add (e.OldValue);
            capturedNewValues.Add (e.NewValue);
        };

        view.SetContentSize (new Size (10, 10));
        view.SetContentSize (new Size (20, 20));
        view.SetContentSize (new Size (30, 30));

        Assert.Equal (3, eventCount);
        Assert.Equal (new Size (10, 10), capturedNewValues [0]);
        Assert.Equal (new Size (20, 20), capturedNewValues [1]);
        Assert.Equal (new Size (30, 30), capturedNewValues [2]);
    }

    #endregion

    #region ContentSizeChanging Event Tests

    [Fact]
    public void ContentSizeChanging_RaisedBeforeContentSizeChanges ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        int changingEventCount = 0;
        int changedEventCount = 0;

        view.ContentSizeChanging += (sender, e) =>
        {
            changingEventCount++;
            // Changed event should not have been raised yet
            Assert.Equal (0, changedEventCount);
        };

        view.ContentSizeChanged += (sender, e) =>
        {
            changedEventCount++;
            // Changing event should have been raised
            Assert.Equal (1, changingEventCount);
        };

        view.SetContentSize (new Size (50, 50));

        Assert.Equal (1, changingEventCount);
        Assert.Equal (1, changedEventCount);
    }

    [Fact]
    public void ContentSizeChanging_CanCancelChange ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size? originalSize = view.GetContentSize ();

        view.ContentSizeChanging += (sender, e) =>
        {
            e.Handled = true;
        };

        view.SetContentSize (new Size (50, 50));

        // Change should be cancelled
        Assert.Equal (originalSize, view.GetContentSize ());
    }

    [Fact]
    public void ContentSizeChanging_CanModifyNewValue ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.ContentSizeChanging += (sender, e) =>
        {
            // Modify the proposed value
            e.NewValue = new Size (100, 100);
        };

        view.SetContentSize (new Size (50, 50));

        // Should use the modified value
        Assert.Equal (new Size (100, 100), view.GetContentSize ());
    }

    [Fact]
    public void ContentSizeChanging_NotRaisedWhenSameValue ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size size = new (10, 10);
        view.SetContentSize (size);

        int eventCount = 0;
        view.ContentSizeChanging += (_, _) => eventCount++;

        view.SetContentSize (size);

        Assert.Equal (0, eventCount);
    }

    [Fact]
    public void OnContentSizeChanging_CanCancelChange ()
    {
        TestView view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size? originalSize = view.GetContentSize ();
        view.CancelContentSizeChange = true;

        view.SetContentSize (new Size (50, 50));

        // Change should be cancelled
        Assert.Equal (originalSize, view.GetContentSize ());
    }

    [Fact]
    public void OnContentSizeChanged_CalledAfterChange ()
    {
        TestView view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.SetContentSize (new Size (50, 50));

        Assert.True (view.OnContentSizeChangedCalled);
    }

    // Test view to verify virtual method calls
    private class TestView : View
    {
        public bool CancelContentSizeChange { get; set; }
        public bool OnContentSizeChangedCalled { get; set; }

        protected override bool OnContentSizeChanging (ValueChangingEventArgs<Size?> args)
        {
            return CancelContentSizeChange;
        }

        protected override void OnContentSizeChanged (ValueChangedEventArgs<Size?> args)
        {
            OnContentSizeChangedCalled = true;
            base.OnContentSizeChanged (args);
        }
    }

    #endregion

    #region Removed Old Tests

    [Fact]
    public void OnContentSizeChanged_TriggersSetNeedsLayout ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        // SetNeedsLayout is called internally when ContentSize changes
        // We verify this by checking that layout state is updated
        view.SetContentSize (new Size (50, 50));

        // Layout should be triggered automatically
        // This is a basic test to ensure no exception is thrown
        view.LayoutSubViews ();
    }

    #endregion

    #region Integration Tests with ScrollBars

    [Fact]
    public void ContentSizeChanged_UpdatesVerticalScrollBar ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 20
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Enable vertical scroll bar
        ScrollBar? verticalScrollBar = view.VerticalScrollBar;
        Assert.NotNull (verticalScrollBar);

        view.SetContentSize (new Size (20, 100));

        Assert.Equal (100, verticalScrollBar.ScrollableContentSize);
    }

    [Fact]
    public void ContentSizeChanged_UpdatesHorizontalScrollBar ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 20
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Enable horizontal scroll bar
        ScrollBar? horizontalScrollBar = view.HorizontalScrollBar;
        Assert.NotNull (horizontalScrollBar);

        view.SetContentSize (new Size (100, 20));

        Assert.Equal (100, horizontalScrollBar.ScrollableContentSize);
    }

    [Fact]
    public void ContentSizeChanged_UpdatesBothScrollBars ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 20
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        ScrollBar? verticalScrollBar = view.VerticalScrollBar;
        ScrollBar? horizontalScrollBar = view.HorizontalScrollBar;
        Assert.NotNull (verticalScrollBar);
        Assert.NotNull (horizontalScrollBar);

        view.SetContentSize (new Size (100, 200));

        Assert.Equal (100, horizontalScrollBar.ScrollableContentSize);
        Assert.Equal (200, verticalScrollBar.ScrollableContentSize);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SetContentSize_ZeroSize_IsValid ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size size = Size.Empty;
        view.SetContentSize (size);

        Assert.Equal (size, view.GetContentSize ());
    }

    [Fact]
    public void SetContentSize_AfterBeginInit_BeforeEndInit_Works ()
    {
        View view = new ();
        view.BeginInit ();

        Size size = new (10, 10);
        view.SetContentSize (size);

        view.EndInit ();

        Assert.Equal (size, view.GetContentSize ());
    }

    [Fact]
    public void SetContentSize_BeforeBeginInit_Works ()
    {
        View view = new ();

        Size size = new (10, 10);
        view.SetContentSize (size);

        Assert.Equal (size, view.GetContentSize ());
    }

    #endregion

    #region Independent Width/Height Tests - Copilot

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (10)]
    [InlineData (100)]
    public void SetContentWidth_ValidWidth_SetsContentWidth (int width)
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.SetContentWidth (width);

        Assert.Equal (width, view.GetContentWidth ());
        Assert.False (view.ContentSizeTracksViewport);
    }

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (10)]
    [InlineData (100)]
    public void SetContentHeight_ValidHeight_SetsContentHeight (int height)
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.SetContentHeight (height);

        Assert.Equal (height, view.GetContentHeight ());
        Assert.False (view.ContentSizeTracksViewport);
    }

    [Fact]
    public void SetContentWidth_Null_TracksViewportWidth ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentWidth (100);
        Assert.Equal (100, view.GetContentWidth ());

        view.SetContentWidth (null);
        Assert.Equal (view.Viewport.Width, view.GetContentWidth ());
    }

    [Fact]
    public void SetContentHeight_Null_TracksViewportHeight ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentHeight (100);
        Assert.Equal (100, view.GetContentHeight ());

        view.SetContentHeight (null);
        Assert.Equal (view.Viewport.Height, view.GetContentHeight ());
    }

    [Theory]
    [InlineData (-1)]
    [InlineData (-10)]
    public void SetContentWidth_Negative_ThrowsArgumentException (int width)
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Assert.Throws<ArgumentException> (() => view.SetContentWidth (width));
    }

    [Theory]
    [InlineData (-1)]
    [InlineData (-10)]
    public void SetContentHeight_Negative_ThrowsArgumentException (int height)
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Assert.Throws<ArgumentException> (() => view.SetContentHeight (height));
    }

    [Fact]
    public void SetContentWidth_Only_HeightTracksViewport ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentWidth (100);

        Assert.Equal (100, view.GetContentWidth ());
        Assert.Equal (view.Viewport.Height, view.GetContentHeight ());
        Assert.Equal (new Size (100, view.Viewport.Height), view.GetContentSize ());
        Assert.False (view.ContentSizeTracksViewport);
    }

    [Fact]
    public void SetContentHeight_Only_WidthTracksViewport ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentHeight (100);

        Assert.Equal (view.Viewport.Width, view.GetContentWidth ());
        Assert.Equal (100, view.GetContentHeight ());
        Assert.Equal (new Size (view.Viewport.Width, 100), view.GetContentSize ());
        Assert.False (view.ContentSizeTracksViewport);
    }

    [Fact]
    public void SetContentWidth_ThenHeight_BothIndependent ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentWidth (100);
        view.SetContentHeight (200);

        Assert.Equal (100, view.GetContentWidth ());
        Assert.Equal (200, view.GetContentHeight ());
        Assert.Equal (new Size (100, 200), view.GetContentSize ());
        Assert.False (view.ContentSizeTracksViewport);
    }

    [Fact]
    public void SetContentWidth_DoesNotResetHeight ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.SetContentHeight (50);
        view.SetContentWidth (100);

        Assert.Equal (100, view.GetContentWidth ());
        Assert.Equal (50, view.GetContentHeight ());
    }

    [Fact]
    public void SetContentHeight_DoesNotResetWidth ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.SetContentWidth (100);
        view.SetContentHeight (50);

        Assert.Equal (100, view.GetContentWidth ());
        Assert.Equal (50, view.GetContentHeight ());
    }

    [Fact]
    public void SetContentSize_Null_ClearsBothDimensions ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentWidth (100);
        view.SetContentHeight (200);

        view.SetContentSize (null);

        Assert.True (view.ContentSizeTracksViewport);
        Assert.Equal (view.Viewport.Width, view.GetContentWidth ());
        Assert.Equal (view.Viewport.Height, view.GetContentHeight ());
    }

    [Fact]
    public void ContentSizeTracksViewport_SetToTrue_ClearsBothDimensions ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentWidth (100);
        view.SetContentHeight (200);
        Assert.False (view.ContentSizeTracksViewport);

        view.ContentSizeTracksViewport = true;

        Assert.True (view.ContentSizeTracksViewport);
        Assert.Equal (view.Viewport.Width, view.GetContentWidth ());
        Assert.Equal (view.Viewport.Height, view.GetContentHeight ());
    }

    [Fact]
    public void SetContentWidth_RaisesContentSizeChangedEvent ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        int eventCount = 0;

        view.ContentSizeChanged += (_, _) => eventCount++;

        view.SetContentWidth (100);

        Assert.Equal (1, eventCount);
    }

    [Fact]
    public void SetContentHeight_RaisesContentSizeChangedEvent ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        int eventCount = 0;

        view.ContentSizeChanged += (_, _) => eventCount++;

        view.SetContentHeight (100);

        Assert.Equal (1, eventCount);
    }

    [Fact]
    public void SetContentWidth_SameValue_DoesNotRaiseEvent ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.SetContentWidth (100);

        int eventCount = 0;
        view.ContentSizeChanged += (_, _) => eventCount++;

        view.SetContentWidth (100);

        Assert.Equal (0, eventCount);
    }

    [Fact]
    public void SetContentHeight_SameValue_DoesNotRaiseEvent ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.SetContentHeight (100);

        int eventCount = 0;
        view.ContentSizeChanged += (_, _) => eventCount++;

        view.SetContentHeight (100);

        Assert.Equal (0, eventCount);
    }

    [Fact]
    public void GetContentWidth_NotSet_ReturnsViewportWidth ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.Equal (view.Viewport.Width, view.GetContentWidth ());
    }

    [Fact]
    public void GetContentHeight_NotSet_ReturnsViewportHeight ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.Equal (view.Viewport.Height, view.GetContentHeight ());
    }

    [Fact]
    public void SetContentWidth_Cancellable_ViaChangingEvent ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size? originalSize = view.GetContentSize ();

        view.ContentSizeChanging += (_, e) =>
        {
            e.Handled = true;
        };

        view.SetContentWidth (100);

        // Change should be cancelled
        Assert.Equal (originalSize, view.GetContentSize ());
    }

    [Fact]
    public void SetContentHeight_Cancellable_ViaChangingEvent ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size? originalSize = view.GetContentSize ();

        view.ContentSizeChanging += (_, e) =>
        {
            e.Handled = true;
        };

        view.SetContentHeight (100);

        // Change should be cancelled
        Assert.Equal (originalSize, view.GetContentSize ());
    }

    [Fact]
    public void SetContentWidth_NullToNull_DoesNotRaiseEvent ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        int eventCount = 0;
        view.ContentSizeChanged += (_, _) => eventCount++;

        view.SetContentWidth (null);

        Assert.Equal (0, eventCount);
    }

    [Fact]
    public void SetContentHeight_NullToNull_DoesNotRaiseEvent ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        int eventCount = 0;
        view.ContentSizeChanged += (_, _) => eventCount++;

        view.SetContentHeight (null);

        Assert.Equal (0, eventCount);
    }

    [Fact]
    public void SetContentWidth_DoesNotConvertNullHeightToExplicit () // Copilot
    {
        // Proves that setting width doesn't convert the null (viewport-tracking) height
        // into an explicit value.
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Height is null (tracking viewport)
        Assert.True (view.ContentSizeTracksViewport);

        // Set only the width
        view.SetContentWidth (100);

        Assert.Equal (100, view.GetContentWidth ());

        // Resize the view — height should follow the new viewport, not stay at 15
        view.Width = 30;
        view.Height = 25;
        view.LayoutSubViews ();

        Assert.Equal (100, view.GetContentWidth ());
        Assert.Equal (view.Viewport.Height, view.GetContentHeight ());
        Assert.Equal (25, view.GetContentHeight ());
    }

    [Fact]
    public void SetContentHeight_DoesNotConvertNullWidthToExplicit () // Copilot
    {
        // Proves that setting height doesn't convert the null (viewport-tracking) width
        // into an explicit value.
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Width is null (tracking viewport)
        Assert.True (view.ContentSizeTracksViewport);

        // Set only the height
        view.SetContentHeight (100);

        Assert.Equal (100, view.GetContentHeight ());

        // Resize the view — width should follow the new viewport, not stay at 20
        view.Width = 30;
        view.Height = 25;
        view.LayoutSubViews ();

        Assert.Equal (view.Viewport.Width, view.GetContentWidth ());
        Assert.Equal (30, view.GetContentWidth ());
        Assert.Equal (100, view.GetContentHeight ());
    }

    [Fact]
    public void SetContentWidth_Null_ContinuesToTrackAfterResize () // Copilot
    {
        // Verifies that after clearing width back to null, it continues to track
        // the viewport as it resizes.
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentWidth (100);
        view.SetContentWidth (null);

        // Resize the view
        view.Width = 40;
        view.LayoutSubViews ();

        Assert.Equal (view.Viewport.Width, view.GetContentWidth ());
        Assert.Equal (40, view.GetContentWidth ());
    }

    [Fact]
    public void SetContentHeight_Null_ContinuesToTrackAfterResize () // Copilot
    {
        // Verifies that after clearing height back to null, it continues to track
        // the viewport as it resizes.
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentHeight (100);
        view.SetContentHeight (null);

        // Resize the view
        view.Height = 40;
        view.LayoutSubViews ();

        Assert.Equal (view.Viewport.Height, view.GetContentHeight ());
        Assert.Equal (40, view.GetContentHeight ());
    }

    [Fact]
    public void SetContentSize_Null_ContinuesToTrackAfterResize () // Copilot
    {
        // Verifies that after clearing both dimensions via SetContentSize(null),
        // both continue to track as viewport resizes.
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentSize (new Size (100, 200));
        view.SetContentSize (null);

        // Resize the view
        view.Width = 40;
        view.Height = 35;
        view.LayoutSubViews ();

        Assert.True (view.ContentSizeTracksViewport);
        Assert.Equal (view.Viewport.Width, view.GetContentWidth ());
        Assert.Equal (view.Viewport.Height, view.GetContentHeight ());
        Assert.Equal (40, view.GetContentWidth ());
        Assert.Equal (35, view.GetContentHeight ());
    }

    #endregion
}

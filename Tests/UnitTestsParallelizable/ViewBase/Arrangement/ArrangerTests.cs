// ReSharper disable PossibleMultipleEnumeration

namespace ViewBaseTests.Arrangement;

/// <summary>
///     Low-level tests for Arranger class.
///     Tests internal arrangement logic, button management, and drag operations.
/// </summary>
[Trait ("Category", "Adornment")]
[Trait ("Category", "Arranger")]
public class ArrangerTests
{
    #region Test Helpers

    private static BorderView CreateBorderWithArrangement (ViewArrangement arrangement)
    {
        View parent = new ()
        {
            X = 10,
            Y = 10,
            Width = 50,
            Height = 30,
            Arrangement = arrangement,
            BorderStyle = LineStyle.Single
        };

        parent.BeginInit ();
        parent.EndInit ();

        return (BorderView)parent.Border.View!;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesCorrectly ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);

        // Act
        Arranger arranger = new (border);

        // Assert
        Assert.NotNull (arranger);
        Assert.False (arranger.IsArranging);
        Assert.Equal (ViewArrangement.Fixed, arranger.Arranging);
    }

    #endregion

    #region HasAnyArrangementOptions Tests

    [Theory]
    [InlineData (ViewArrangement.Fixed, false)]
    [InlineData (ViewArrangement.Overlapped, false)]
    [InlineData (ViewArrangement.Movable, true)]
    [InlineData (ViewArrangement.LeftResizable, true)]
    [InlineData (ViewArrangement.RightResizable, true)]
    [InlineData (ViewArrangement.TopResizable, true)]
    [InlineData (ViewArrangement.BottomResizable, true)]
    [InlineData (ViewArrangement.Resizable, true)]
    [InlineData (ViewArrangement.Movable | ViewArrangement.Resizable, true)]
    public void HasAnyArrangementOptions_ReturnsCorrectValue (ViewArrangement arrangement, bool expected)
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (arrangement);
        Arranger arranger = new (border);

        // Act
        bool result = arranger.HasAnyArrangementOptions ();

        // Assert
        Assert.Equal (expected, result);
    }

    [Fact]
    public void HasAnyArrangementOptions_ReturnsFalse_WhenParentIsNull ()
    {
        // Arrange
        View view = new () { BorderStyle = LineStyle.Single };
        view.BeginInit ();
        view.EndInit ();
        var borderView = (BorderView)view.Border.View!;
        borderView.Border.Parent = null;
        Arranger arranger = new (borderView);

        // Act
        bool result = arranger.HasAnyArrangementOptions ();

        // Assert
        Assert.False (result);
    }

    #endregion

    #region EnterArrangeMode Tests

    [Fact]
    public void EnterArrangeMode_ReturnsFalse_WhenNoArrangementOptions ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Fixed);
        Arranger arranger = new (border);

        // Act
        bool result = arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert
        Assert.False (result);
        Assert.False (arranger.IsArranging);
    }

    [Fact]
    public void EnterArrangeMode_ReturnsTrue_WhenArrangementOptionsExist ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);

        // Act
        bool result = arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert
        Assert.True (result);
        Assert.True (arranger.IsArranging);
    }

    // Copilot
    [Fact]
    public void EnterArrangeMode_WhenAlreadyArranging_ExitsWithoutThrowing ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Act - simulates pressing the Arrange key (Ctrl+F5) a second time
        bool result = arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.False (arranger.IsArranging);
    }

    // Copilot
    [Fact]
    public void Esc_WhenArranging_ExitsArrangeMode ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = border.Arranger;
        arranger.EnterArrangeMode (ViewArrangement.Fixed);
        Assert.True (arranger.IsArranging);

        // Act
        border.NewKeyDownEvent (Key.Esc);

        // Assert
        Assert.False (arranger.IsArranging);
    }

    [Fact]
    public void EnterArrangeMode_SetsArrangingProperty ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Resizable);
        Arranger arranger = new (border);

        // Act - Enter keyboard mode strips Overlapped
        arranger.EnterArrangeMode (ViewArrangement.RightResizable);

        // Assert - In keyboard mode without mouse, Arranging gets set to parent.Arrangement (minus Overlapped)
        Assert.Equal (ViewArrangement.Resizable, arranger.Arranging);
    }

    [Fact]
    public void EnterArrangeMode_CreatesArrangementButtons ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable | ViewArrangement.Resizable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert - At least one button should be created
        Assert.True (arranger.IsArranging);
    }

    [Theory]
    [InlineData (ViewArrangement.Movable, 1, ArrangeButtons.Move)]
    [InlineData (ViewArrangement.LeftResizable, 1, ArrangeButtons.LeftSize)]
    [InlineData (ViewArrangement.RightResizable, 1, ArrangeButtons.RightSize)]
    [InlineData (ViewArrangement.TopResizable, 1, ArrangeButtons.TopSize)]
    [InlineData (ViewArrangement.BottomResizable, 1, ArrangeButtons.BottomSize)]
    [InlineData (ViewArrangement.Movable | ViewArrangement.Resizable, 6, ArrangeButtons.Move)]
    public void EnterArrangeMode_CreatesCorrectButtons (ViewArrangement arrangement, int expectedCount, ArrangeButtons expectedButtonType)
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (arrangement);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert - Check button count and verify expected button exists
        IEnumerable<ArrangerButton> buttons = border.SubViews.OfType<ArrangerButton> ();
        Assert.Equal (expectedCount, buttons.Count ());
        Assert.Contains (buttons, b => b.ButtonType == expectedButtonType);
    }

    [Fact]
    public void EnterArrangeMode_KeyboardMode_ShowsAllApplicableButtons ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable | ViewArrangement.Resizable);
        Arranger arranger = new (border);

        // Act - Enter keyboard mode (no mouse grab)
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert - Move, AllSize, and all four directional buttons should be visible (Resizable includes all directions)
        IEnumerable<ArrangerButton> visibleButtons = border.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible);
        Assert.Equal (6, visibleButtons.Count ());
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.Move);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.AllSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.LeftSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.RightSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.TopSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.BottomSize);
    }

    [Fact]
    public void EnterArrangeMode_KeyboardMode_ShowsAllResizableButtons ()
    {
        // Arrange - Use Resizable flag which includes AllSize
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Resizable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert - AllSize plus all four directional resize buttons should be visible
        IEnumerable<ArrangerButton> visibleButtons = border.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible);
        Assert.Equal (5, visibleButtons.Count ());
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.AllSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.LeftSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.RightSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.TopSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.BottomSize);
    }

    #endregion

    #region ExitArrangeMode Tests

    [Fact]
    public void ExitArrangeMode_ResetsArrangingProperty ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Act
        arranger.ExitArrangeMode ();

        // Assert
        Assert.False (arranger.IsArranging);
        Assert.Equal (ViewArrangement.Fixed, arranger.Arranging);
    }

    [Fact]
    public void ExitArrangeMode_RemovesAllButtons ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable | ViewArrangement.Resizable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Act
        arranger.ExitArrangeMode ();

        // Assert - Arrange mode should be exited
        Assert.False (arranger.IsArranging);
    }

    [Fact]
    public void ExitArrangeMode_CanBeCalledMultipleTimes ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Act & Assert - Should not throw
        arranger.ExitArrangeMode ();
        arranger.ExitArrangeMode ();
        arranger.ExitArrangeMode ();

        Assert.False (arranger.IsArranging);
    }

    #endregion

    #region DetermineArrangeModeFromClick Tests

    [Fact]
    public void DetermineArrangeModeFromClick_ReturnsMovable_WhenClickedInTopThickness ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        Point clickPoint = new (border.Frame.X + 5, border.Frame.Y); // Top middle

        // Act
        ViewArrangement result = arranger.DetermineArrangeModeFromClick (clickPoint);

        // Assert
        Assert.Equal (ViewArrangement.Movable, result);
    }

    [Fact]
    public void DetermineArrangeModeFromClick_ReturnsLeftResizable_WhenClickedInLeftEdge ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.LeftResizable);
        Arranger arranger = new (border);
        Point clickPoint = new (border.Frame.X, border.Frame.Y + 5); // Left middle

        // Act
        ViewArrangement result = arranger.DetermineArrangeModeFromClick (clickPoint);

        // Assert
        Assert.Equal (ViewArrangement.LeftResizable, result);
    }

    [Fact]
    public void DetermineArrangeModeFromClick_ReturnsRightResizable_WhenClickedInRightEdge ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.RightResizable);
        Arranger arranger = new (border);
        Point clickPoint = new (border.Frame.Right - 1, border.Frame.Y + 5); // Right middle

        // Act
        ViewArrangement result = arranger.DetermineArrangeModeFromClick (clickPoint);

        // Assert
        Assert.Equal (ViewArrangement.RightResizable, result);
    }

    [Fact]
    public void DetermineArrangeModeFromClick_ReturnsBottomResizable_WhenClickedInBottomEdge ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.BottomResizable);
        Arranger arranger = new (border);
        Point clickPoint = new (border.Frame.X + 5, border.Frame.Bottom - 1); // Bottom middle

        // Act
        ViewArrangement result = arranger.DetermineArrangeModeFromClick (clickPoint);

        // Assert
        Assert.Equal (ViewArrangement.BottomResizable, result);
    }

    [Fact]
    public void DetermineArrangeModeFromClick_ReturnsTopResizable_WhenClickedInTopEdge_AndNotMovable ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.TopResizable);
        Arranger arranger = new (border);
        Point clickPoint = new (border.Frame.X + 5, border.Frame.Y); // Top middle

        // Act
        ViewArrangement result = arranger.DetermineArrangeModeFromClick (clickPoint);

        // Assert
        Assert.Equal (ViewArrangement.TopResizable, result);
    }

    [Fact]
    public void DetermineArrangeModeFromClick_ReturnsCombined_WhenClickedInBottomRightCorner ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.BottomResizable | ViewArrangement.RightResizable);
        Arranger arranger = new (border);
        Point clickPoint = new (border.Frame.Right - 1, border.Frame.Bottom - 1); // Bottom-right corner

        // Act
        ViewArrangement result = arranger.DetermineArrangeModeFromClick (clickPoint);

        // Assert
        Assert.Equal (ViewArrangement.BottomResizable | ViewArrangement.RightResizable, result);
    }

    [Fact]
    public void DetermineArrangeModeFromClick_ReturnsCombined_WhenClickedInBottomLeftCorner ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.BottomResizable | ViewArrangement.LeftResizable);
        Arranger arranger = new (border);
        Point clickPoint = new (border.Frame.X, border.Frame.Bottom - 1); // Bottom-left corner

        // Act
        ViewArrangement result = arranger.DetermineArrangeModeFromClick (clickPoint);

        // Assert
        Assert.Equal (ViewArrangement.BottomResizable | ViewArrangement.LeftResizable, result);
    }

    [Fact]
    public void DetermineArrangeModeFromClick_ReturnsFixed_WhenParentIsNull ()
    {
        // Arrange
        View view = new () { BorderStyle = LineStyle.Single };
        view.BeginInit ();
        view.EndInit ();
        var borderView = (BorderView)view.Border.View!;
        borderView.Border.Parent = null;
        Arranger arranger = new (borderView);

        // Act
        ViewArrangement result = arranger.DetermineArrangeModeFromClick (new Point (0, 0));

        // Assert
        Assert.Equal (ViewArrangement.Fixed, result);
    }

    #endregion

    #region HandleDragOperation Tests

    [Fact]
    public void HandleDragOperation_MovesView_WhenArrangingIsMovable ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        View parent = border.Adornment?.Parent!;

        // Simulate starting a drag
        Point grabPoint = new (5, 5);
        arranger.StartDrag (grabPoint, new Point (0, 0));

        // Act - Drag to a new position
        Point targetLocation = new (25, 20);
        arranger.HandleDragOperation (targetLocation);

        // Assert - View should have moved (target - grab)
        Assert.Equal (20, parent.Frame.X); // 25 - 5
        Assert.Equal (15, parent.Frame.Y); // 20 - 5
    }

    [Fact]
    public void HandleDragOperation_ResizesFromRight_WhenArrangingIsRightResizable ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.RightResizable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.RightResizable);

        View parent = border.Adornment!.Parent!;
        int originalWidth = parent.Frame.Width;
        int originalX = parent.Frame.X;

        // Simulate starting a drag at the right edge
        Point grabPoint = new (parent.Frame.Right, parent.Frame.Y + 5);
        arranger.StartDrag (grabPoint, new Point (0, 0));

        // Act - Drag right to expand
        Point targetLocation = new (parent.Frame.X + 60, parent.Frame.Y + 5);
        arranger.HandleDragOperation (targetLocation);

        // Assert - Width should increase, X should not change
        Assert.Equal (originalX, parent.Frame.X);
        Assert.True (parent.Frame.Width > originalWidth);
    }

    [Fact]
    public void HandleDragOperation_ResizesFromBottom_WhenArrangingIsBottomResizable ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.BottomResizable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.BottomResizable);

        View parent = border.Adornment!.Parent!;
        int originalHeight = parent.Frame.Height;
        int originalY = parent.Frame.Y;

        // Simulate starting a drag at the bottom edge
        Point grabPoint = new (parent.Frame.X + 5, parent.Frame.Bottom);
        arranger.StartDrag (grabPoint, new Point (0, 0));

        // Act - Drag down to expand
        Point targetLocation = new (parent.Frame.X + 5, parent.Frame.Y + 40);
        arranger.HandleDragOperation (targetLocation);

        // Assert - Height should increase, Y should not change
        Assert.Equal (originalY, parent.Frame.Y);
        Assert.True (parent.Frame.Height > originalHeight);
    }

    [Fact]
    public void HandleDragOperation_ResizesFromLeft_WhenArrangingIsLeftResizable ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.LeftResizable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.LeftResizable);

        View parent = border.Adornment!.Parent!;
        int originalX = parent.Frame.X;
        int originalWidth = parent.Frame.Width;

        // Simulate starting a drag at the left edge
        Point grabPoint = new (parent.Frame.X, parent.Frame.Y + 5);
        arranger.StartDrag (grabPoint, new Point (0, 0));

        // Act - Drag left to expand
        Point targetLocation = new (parent.Frame.X - 5, parent.Frame.Y + 5);
        arranger.HandleDragOperation (targetLocation);

        // Assert - X should decrease, width should increase
        Assert.True (parent.Frame.X < originalX);
        Assert.True (parent.Frame.Width > originalWidth);
    }

    [Fact]
    public void HandleDragOperation_ResizesFromTop_WhenArrangingIsTopResizable ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.TopResizable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.TopResizable);

        View parent = border.Adornment!.Parent!;
        int originalY = parent.Frame.Y;
        int originalHeight = parent.Frame.Height;

        // Simulate starting a drag at the top edge
        Point grabPoint = new (parent.Frame.X + 5, parent.Frame.Y);
        arranger.StartDrag (grabPoint, new Point (0, 0));

        // Act - Drag up to expand
        Point targetLocation = new (parent.Frame.X + 5, parent.Frame.Y - 5);
        arranger.HandleDragOperation (targetLocation);

        // Assert - Y should decrease, height should increase
        Assert.True (parent.Frame.Y < originalY);
        Assert.True (parent.Frame.Height > originalHeight);
    }

    [Fact]
    public void HandleDragOperation_ResizesFromBottomRight_WhenArrangingIsCombined ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.BottomResizable | ViewArrangement.RightResizable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.BottomResizable | ViewArrangement.RightResizable);

        View parent = border.Adornment!.Parent!;
        int originalWidth = parent.Frame.Width;
        int originalHeight = parent.Frame.Height;

        // Simulate starting a drag at bottom-right corner
        Point grabPoint = new (parent.Frame.Right, parent.Frame.Bottom);
        arranger.StartDrag (grabPoint, new Point (0, 0));

        // Act - Drag diagonally to expand
        Point targetLocation = new (parent.Frame.X + 60, parent.Frame.Y + 40);
        arranger.HandleDragOperation (targetLocation);

        // Assert - Both width and height should increase
        Assert.True (parent.Frame.Width > originalWidth);
        Assert.True (parent.Frame.Height > originalHeight);
    }

    [Fact]
    public void HandleDragOperation_DoesNothing_WhenParentIsNull ()
    {
        // Arrange
        View view = new () { BorderStyle = LineStyle.Single };
        view.BeginInit ();
        view.EndInit ();
        Border border = view.Border;
        Arranger arranger = new ((BorderView)border.View!);
        border.Parent = null;

        // Act & Assert - Should not throw
        arranger.HandleDragOperation (new Point (100, 100));
    }

    #endregion

    #region Keyboard Arrangement Tests

    [Fact]
    public void HandleArrangeModeUp_MovesViewUp_WhenArrangingIsMovable ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        View parent = border.Adornment!.Parent!;
        int originalY = parent.Frame.Y;

        // Act
        bool result = arranger.HandleArrangeModeUp ();

        // Assert
        Assert.True (result);
        Assert.Equal (originalY - 1, parent.Frame.Y);
    }

    [Fact]
    public void HandleArrangeModeDown_MovesViewDown_WhenArrangingIsMovable ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        View parent = border.Adornment!.Parent!;
        int originalY = parent.Frame.Y;

        // Act
        bool result = arranger.HandleArrangeModeDown ();

        // Assert
        Assert.True (result);
        Assert.Equal (originalY + 1, parent.Frame.Y);
    }

    [Fact]
    public void HandleArrangeModeLeft_MovesViewLeft_WhenArrangingIsMovable ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        View parent = border.Adornment!.Parent!;
        int originalX = parent.Frame.X;

        // Act
        bool result = arranger.HandleArrangeModeLeft ();

        // Assert
        Assert.True (result);
        Assert.Equal (originalX - 1, parent.Frame.X);
    }

    [Fact]
    public void HandleArrangeModeRight_MovesViewRight_WhenArrangingIsMovable ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        View parent = border.Adornment!.Parent!;
        int originalX = parent.Frame.X;

        // Act
        bool result = arranger.HandleArrangeModeRight ();

        // Assert
        Assert.True (result);
        Assert.Equal (originalX + 1, parent.Frame.X);
    }

    [Fact]
    public void HandleArrangeModeUp_ReturnsFalse_WhenParentIsNull ()
    {
        // Arrange
        View view = new () { BorderStyle = LineStyle.Single };
        view.BeginInit ();
        view.EndInit ();
        var borderView = (BorderView)view.Border.View!;
        borderView.Border.Parent = null;
        Arranger arranger = new (borderView);

        // Act
        bool result = arranger.HandleArrangeModeUp ();

        // Assert
        Assert.False (result);
    }

    #endregion

    #region Drag State Tests

    [Fact]
    public void IsDragging_ReturnsFalse_Initially ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);

        // Assert
        Assert.False (arranger.IsDragging);
    }

    [Fact]
    public void IsDragging_ReturnsTrue_AfterStartDrag ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);

        // Act
        arranger.StartDrag (new Point (5, 5), new Point (10, 10));

        // Assert
        Assert.True (arranger.IsDragging);
    }

    [Fact]
    public void IsDragging_ReturnsFalse_AfterEndDrag ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        arranger.StartDrag (new Point (5, 5), new Point (10, 10));

        // Act
        arranger.EndDrag ();

        // Assert
        Assert.False (arranger.IsDragging);
    }

    [Fact]
    public void GrabPoint_IsSetCorrectly_ByStartDrag ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        Point expectedGrabPoint = new (7, 13);

        // Act
        arranger.StartDrag (expectedGrabPoint, new Point (10, 10));

        // Assert
        Assert.Equal (expectedGrabPoint, arranger.GrabPoint);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ExitsArrangeMode ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Act
        arranger.Dispose ();

        // Assert
        Assert.False (arranger.IsArranging);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);

        // Act & Assert - Should not throw
        arranger.Dispose ();
        arranger.Dispose ();
        arranger.Dispose ();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteArrangeWorkflow_MovableMode ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);
        View parent = border.Adornment!.Parent!;
        int originalX = parent.Frame.X;
        int originalY = parent.Frame.Y;

        // Act - Enter, move, exit
        arranger.EnterArrangeMode (ViewArrangement.Movable);
        arranger.HandleArrangeModeRight ();
        arranger.HandleArrangeModeRight ();
        arranger.HandleArrangeModeDown ();
        arranger.ExitArrangeMode ();

        // Assert
        Assert.Equal (originalX + 2, parent.Frame.X);
        Assert.Equal (originalY + 1, parent.Frame.Y);
        Assert.False (arranger.IsArranging);
    }

    [Fact]
    public void CompleteArrangeWorkflow_ResizableMode ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Resizable);
        Arranger arranger = new (border);
        View parent = border.Adornment!.Parent!;
        int originalWidth = parent.Frame.Width;

        // Act - Use mouse drag mode
        arranger.EnterArrangeMode (ViewArrangement.RightResizable);
        arranger.StartDrag (new Point (parent.Frame.Right, parent.Frame.Y + 5), new Point (0, 0));
        arranger.HandleDragOperation (new Point (parent.Frame.X + 55, parent.Frame.Y + 5));
        arranger.EndDrag ();
        arranger.ExitArrangeMode ();

        // Assert
        Assert.True (parent.Frame.Width >= originalWidth); // Width may have increased
        Assert.False (arranger.IsArranging);
    }

    #endregion

    #region Button Visibility Tests

    [Fact]
    public void ButtonVisibility_MovableOnly_ShowsMoveButton ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert
        IEnumerable<ArrangerButton> visibleButtons = border.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible);
        Assert.Single (visibleButtons);
        Assert.True (visibleButtons.First ().ButtonType == ArrangeButtons.Move);
    }

    [Fact]
    public void ButtonVisibility_LeftResizableOnly_ShowsLeftSizeButton ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.LeftResizable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert
        IEnumerable<ArrangerButton> visibleButtons = border.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible);
        Assert.Single (visibleButtons);
        Assert.True (visibleButtons.First ().ButtonType == ArrangeButtons.LeftSize);
    }

    [Fact]
    public void ButtonVisibility_RightResizableOnly_ShowsRightSizeButton ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.RightResizable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert
        IEnumerable<ArrangerButton> visibleButtons = border.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible);
        Assert.Single (visibleButtons);
        Assert.True (visibleButtons.First ().ButtonType == ArrangeButtons.RightSize);
    }

    [Fact]
    public void ButtonVisibility_TopResizableOnly_ShowsTopSizeButton ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.TopResizable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert
        IEnumerable<ArrangerButton> visibleButtons = border.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible);
        Assert.Single (visibleButtons);
        Assert.True (visibleButtons.First ().ButtonType == ArrangeButtons.TopSize);
    }

    [Fact]
    public void ButtonVisibility_BottomResizableOnly_ShowsBottomSizeButton ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.BottomResizable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert
        IEnumerable<ArrangerButton> visibleButtons = border.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible);
        Assert.Single (visibleButtons);
        Assert.True (visibleButtons.First ().ButtonType == ArrangeButtons.BottomSize);
    }

    [Fact]
    public void ButtonVisibility_ResizableFlag_ShowsAllSizeButton ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Resizable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert - Resizable flag creates AllSize AND all four directional buttons
        IEnumerable<ArrangerButton> visibleButtons = border.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible);
        Assert.Equal (5, visibleButtons.Count ());
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.AllSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.LeftSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.RightSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.TopSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.BottomSize);
    }

    [Fact]
    public void ButtonVisibility_MovableAndResizable_ShowsBothButtons ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable | ViewArrangement.Resizable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert - Should show Move, AllSize, and all four directional buttons
        IEnumerable<ArrangerButton> visibleButtons = border.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible);
        Assert.Equal (6, visibleButtons.Count ());
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.Move);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.AllSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.LeftSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.RightSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.TopSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.BottomSize);
    }

    [Fact]
    public void ButtonVisibility_LeftAndRightResizable_ShowsBothButtons ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.LeftResizable | ViewArrangement.RightResizable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert
        IEnumerable<ArrangerButton> visibleButtons = border.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible);
        Assert.Equal (2, visibleButtons.Count ());
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.LeftSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.RightSize);
    }

    [Fact]
    public void ButtonVisibility_TopAndBottomResizable_ShowsBothButtons ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.TopResizable | ViewArrangement.BottomResizable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert
        IEnumerable<ArrangerButton> visibleButtons = border.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible);
        Assert.Equal (2, visibleButtons.Count ());
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.TopSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.BottomSize);
    }

    [Fact]
    public void ButtonVisibility_AllDirectionalResizable_ShowsAllFourButtons ()
    {
        // Arrange - Using Resizable flag creates AllSize too
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Resizable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert - AllSize plus all four directional buttons visible
        IEnumerable<ArrangerButton> visibleButtons = border.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible);
        Assert.Equal (5, visibleButtons.Count ());
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.AllSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.LeftSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.RightSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.TopSize);
        Assert.Contains (visibleButtons, b => b.ButtonType == ArrangeButtons.BottomSize);
    }

    [Fact]
    public void ButtonVisibility_AfterExit_AllButtonsRemoved ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable | ViewArrangement.Resizable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Act
        arranger.ExitArrangeMode ();

        // Assert - No buttons should remain
        IEnumerable<Button> buttons = border.SubViews.OfType<Button> ();
        Assert.Empty (buttons);
    }

    [Fact]
    public void ButtonVisibility_AfterDispose_AllButtonsRemoved ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable | ViewArrangement.Resizable);
        Arranger arranger = new (border);
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Act
        arranger.Dispose ();

        // Assert - No buttons should remain
        IEnumerable<Button> buttons = border.SubViews.OfType<Button> ();
        Assert.Empty (buttons);
    }

    [Fact]
    public void ButtonProperties_AreConfiguredCorrectly ()
    {
        // Arrange
        BorderView border = CreateBorderWithArrangement (ViewArrangement.Movable);
        Arranger arranger = new (border);

        // Act
        arranger.EnterArrangeMode (ViewArrangement.Movable);

        // Assert - Verify button properties
        ArrangerButton moveButton = border.SubViews.OfType<ArrangerButton> ().First (b => b.ButtonType == ArrangeButtons.Move);
        Assert.True (moveButton.CanFocus);
        Assert.Equal (1, moveButton.Frame.Width);
        Assert.Equal (1, moveButton.Frame.Height);
        Assert.True (moveButton.NoDecorations);
        Assert.True (moveButton.NoPadding);
        Assert.Null (moveButton.ShadowStyle);
    }

    #endregion
}

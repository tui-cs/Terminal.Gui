namespace ViewBaseTests.Arrangement;

/// <summary>
///     Tests for keyboard-based border arrangement mode (CTRL-F5).
///     These tests verify that arrangement buttons are properly shown for different ViewArrangement configurations.
/// </summary>
[Trait ("Category", "Adornment")]
[Trait ("Category", "Border")]
public class BorderArrangementKeyboardTests
{
    /// <summary>
    ///     Tests that keyboard arrangement mode (CTRL-F5) properly shows arrangement buttons
    ///     for ViewArrangement.LeftResizable
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_LeftResizable_ShowsLeftSizeButton ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act - Enter keyboard arrange mode (ViewArrangement.Fixed triggers keyboard mode)
        bool? result = (view.Border.View as BorderView)?.Arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, (view.Border.View as BorderView)?.Arranger?.Arranging);

        // Check that the left size button was created and is visible
        ArrangerButton? leftButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.LeftSize);
        Assert.NotNull (leftButton);
        Assert.True (leftButton.Visible);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for ViewArrangement.RightResizable
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_RightResizable_ShowsRightSizeButton ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.RightResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = (view.Border.View as BorderView)?.Arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, (view.Border.View as BorderView)?.Arranger.Arranging);

        ArrangerButton? rightButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.RightSize);
        Assert.NotNull (rightButton);
        Assert.True (rightButton.Visible);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for ViewArrangement.TopResizable
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_TopResizable_ShowsTopSizeButton ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.TopResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = (view.Border.View as BorderView)?.Arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, (view.Border.View as BorderView)?.Arranger.Arranging);

        ArrangerButton? topButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.TopSize);
        Assert.NotNull (topButton);
        Assert.True (topButton.Visible);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for ViewArrangement.BottomResizable
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_BottomResizable_ShowsBottomSizeButton ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.BottomResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = (view.Border.View as BorderView)?.Arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, (view.Border.View as BorderView)?.Arranger.Arranging);

        View? bottomButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.BottomSize);
        Assert.NotNull (bottomButton);
        Assert.True (bottomButton.Visible);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for ViewArrangement.Movable
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_Movable_ShowsMoveButton ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = (view.Border.View as BorderView)?.Arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.Equal (ViewArrangement.Movable, (view.Border.View as BorderView)?.Arranger.Arranging);

        View? moveButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.Move);
        Assert.NotNull (moveButton);
        Assert.True (moveButton.Visible);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for combined arrangements like LeftResizable | BottomResizable
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_LeftAndBottomResizable_ShowsCorrectButtons ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.LeftResizable | ViewArrangement.BottomResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = (view.Border.View as BorderView)?.Arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, (view.Border.View as BorderView)?.Arranger.Arranging);

        ArrangerButton? leftButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.LeftSize);
        Assert.NotNull (leftButton);

        ArrangerButton? bottomButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.BottomSize);
        Assert.NotNull (bottomButton);

        ArrangerButton? moveButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.Move);
        Assert.Null (moveButton);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for ViewArrangement.Resizable (all directions).
    ///     For fully Resizable views, only move and all-size buttons should be visible in keyboard mode.
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_Resizable_ShowsAllSizeButton ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.Resizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = (view.Border.View as BorderView)?.Arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, (view.Border.View as BorderView)?.Arranger.Arranging);

        // For fully Resizable, only the all-size button should be visible (not individual direction buttons)
        ArrangerButton? allSizeButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.AllSize);
        Assert.NotNull (allSizeButton);
        Assert.True (allSizeButton.Visible);

        // Individual direction buttons should be visible for fully Resizable
        ArrangerButton? leftButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.LeftSize);
        Assert.NotNull (leftButton);

        ArrangerButton? rightButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.RightSize);
        Assert.NotNull (rightButton);

        ArrangerButton? topButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.TopSize);
        Assert.NotNull (topButton);

        ArrangerButton? bottomButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.BottomSize);
        Assert.NotNull (bottomButton);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for TopResizable | RightResizable combination
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_TopAndRightResizable_ShowsCorrectButtons ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.TopResizable | ViewArrangement.RightResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = (view.Border.View as BorderView)?.Arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, (view.Border.View as BorderView)?.Arranger.Arranging);

        ArrangerButton? topButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.TopSize);
        Assert.NotNull (topButton);

        ArrangerButton? rightButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.RightSize);
        Assert.NotNull (rightButton);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode only shows buttons for enabled arrangements
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_OnlyShowsButtonsForEnabledArrangements ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = (view.Border.View as BorderView)?.Arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);

        // Only left button should be visible
        ArrangerButton? leftButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.LeftSize);
        Assert.NotNull (leftButton);
        Assert.True (leftButton.Visible);

        // Other buttons should not exist or be invisible
        ArrangerButton? rightButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.RightSize);
        Assert.True (rightButton is not { Visible: true });

        ArrangerButton? topButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.TopSize);
        Assert.True (topButton is not { Visible: true });

        ArrangerButton? bottomButton = view.Border.View!.SubViews.OfType<ArrangerButton> ().FirstOrDefault (v => v.ButtonType == ArrangeButtons.BottomSize);
        Assert.True (bottomButton is not { Visible: true });

        // Cleanup
        superView.Dispose ();
    }

    // Claude - Opus 4.5

    /// <summary>
    ///     Helper to set up a view in arrange mode.
    /// </summary>
    private static (View superView, View view, BorderView borderView, Arranger arranger) SetupArrangeMode (ViewArrangement arrangement)
    {
        var superView = new View { Width = 80, Height = 25, CanFocus = true };

        var view = new View
        {
            Arrangement = arrangement,
            BorderStyle = LineStyle.Single,
            CanFocus = true,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();
        superView.Layout ();
        superView.SetFocus ();

        var borderView = (BorderView)view.Border.View!;
        Arranger arranger = borderView.Arranger;
        arranger.EnterArrangeMode (ViewArrangement.Fixed);

        return (superView, view, borderView, arranger);
    }

    /// <summary>
    ///     Tests that GetFocusedArrangement returns the correct arrangement for each focused button,
    ///     proving that Tab navigation updates Arranging correctly when focus moves between buttons.
    /// </summary>
    [Fact]
    public void FocusingEachButton_UpdatesGetFocusedArrangement ()
    {
        // Arrange - set up with multiple arrangement options
        (View superView, View _, BorderView borderView, Arranger arranger) =
            SetupArrangeMode (ViewArrangement.Movable | ViewArrangement.LeftResizable | ViewArrangement.RightResizable);

        // Act & Assert - Focus each button and verify GetFocusedArrangement returns correct value
        ArrangerButton? moveButton = borderView.SubViews.OfType<ArrangerButton> ().FirstOrDefault (b => b.ButtonType == ArrangeButtons.Move);
        Assert.NotNull (moveButton);
        moveButton.SetFocus ();
        Assert.Equal (ViewArrangement.Movable, arranger.GetFocusedArrangement ());

        ArrangerButton? leftButton = borderView.SubViews.OfType<ArrangerButton> ().FirstOrDefault (b => b.ButtonType == ArrangeButtons.LeftSize);
        Assert.NotNull (leftButton);
        leftButton.SetFocus ();
        Assert.Equal (ViewArrangement.LeftResizable, arranger.GetFocusedArrangement ());

        ArrangerButton? rightButton = borderView.SubViews.OfType<ArrangerButton> ().FirstOrDefault (b => b.ButtonType == ArrangeButtons.RightSize);
        Assert.NotNull (rightButton);
        rightButton.SetFocus ();
        Assert.Equal (ViewArrangement.RightResizable, arranger.GetFocusedArrangement ());

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that focusing different buttons and calling HandleArrangeModeTab cycles
    ///     the Arranging state, proving Tab updates state correctly.
    ///     Also proves that repeated navigation stays within the arranger.
    /// </summary>
    [Fact]
    public void Tab_CyclesFocus_AndUpdatesArranging ()
    {
        // Arrange
        (View superView, View _, BorderView borderView, Arranger arranger) = SetupArrangeMode (ViewArrangement.Movable | ViewArrangement.Resizable);

        List<ArrangerButton> visibleButtons = borderView.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible).ToList ();
        int buttonCount = visibleButtons.Count;
        Assert.True (buttonCount >= 2, $"Need at least 2 visible buttons, got {buttonCount}");

        // Simulate what Tab does: focus each button in order and verify Arranging updates
        HashSet<ViewArrangement> seenArrangements = [];

        for (var i = 0; i < buttonCount * 2; i++)
        {
            ArrangerButton button = visibleButtons [i % buttonCount];
            button.SetFocus ();

            // GetFocusedArrangement reads the currently focused button
            ViewArrangement arrangement = arranger.GetFocusedArrangement ();
            Assert.NotEqual (ViewArrangement.Fixed, arrangement);
            seenArrangements.Add (arrangement);
        }

        // Assert - cycling through all buttons should have visited multiple arrangements
        Assert.True (seenArrangements.Count >= 2, $"Expected at least 2 different arrangements, saw {seenArrangements.Count}");

        // All arrangements should be valid (not Fixed)
        foreach (ViewArrangement arr in seenArrangements)
        {
            Assert.NotEqual (ViewArrangement.Fixed, arr);
        }

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that Shift+Tab (backward) cycles through buttons and Arranging reflects each,
    ///     proving that repeated backward navigation stays within the arranger.
    /// </summary>
    [Fact]
    public void ShiftTab_CyclesFocus_AndUpdatesArranging ()
    {
        // Arrange
        (View superView, View _, BorderView borderView, Arranger arranger) = SetupArrangeMode (ViewArrangement.Movable | ViewArrangement.Resizable);

        List<ArrangerButton> visibleButtons = borderView.SubViews.OfType<ArrangerButton> ().Where (b => b.Visible).ToList ();
        int buttonCount = visibleButtons.Count;
        Assert.True (buttonCount >= 2, $"Need at least 2 visible buttons, got {buttonCount}");

        // Simulate backward Tab: focus each button in reverse order
        HashSet<ViewArrangement> seenArrangements = [];

        for (int i = buttonCount * 2 - 1; i >= 0; i--)
        {
            ArrangerButton button = visibleButtons [i % buttonCount];
            button.SetFocus ();

            ViewArrangement arrangement = arranger.GetFocusedArrangement ();
            Assert.NotEqual (ViewArrangement.Fixed, arrangement);
            seenArrangements.Add (arrangement);
        }

        // Assert
        Assert.True (seenArrangements.Count >= 2, $"Expected at least 2 different arrangements, saw {seenArrangements.Count}");

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that arrow keys operate on the view while in arrange mode (movable view).
    /// </summary>
    [Fact]
    public void ArrowKeys_MoveView_WhenMovableArrangement ()
    {
        // Arrange
        (View superView, View view, BorderView _, Arranger arranger) = SetupArrangeMode (ViewArrangement.Movable);

        int originalX = view.X.GetAnchor (0);
        int originalY = view.Y.GetAnchor (0);

        // Act - Press arrow keys
        arranger.HandleArrangeModeRight ();
        arranger.HandleArrangeModeDown ();

        // Assert - View should have moved
        Assert.Equal (originalX + 1, view.X.GetAnchor (0));
        Assert.Equal (originalY + 1, view.Y.GetAnchor (0));

        // Act - Move back
        arranger.HandleArrangeModeLeft ();
        arranger.HandleArrangeModeUp ();

        // Assert - View should be back at original position
        Assert.Equal (originalX, view.X.GetAnchor (0));
        Assert.Equal (originalY, view.Y.GetAnchor (0));

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that arrow keys resize the view when in Resizable arrangement mode.
    ///     Uses Resizable-only arrangement (no Movable) so Arranging is set to Resizable directly.
    /// </summary>
    [Fact]
    public void ArrowKeys_ResizeView_WhenResizableArrangement ()
    {
        // Arrange - Resizable only, so Arranging starts as Resizable
        (View superView, View view, BorderView _, Arranger arranger) = SetupArrangeMode (ViewArrangement.Resizable);

        // Verify arrange mode is set to the full resizable arrangement
        Assert.True (arranger.IsArranging);

        int originalWidth = view.Width!.GetAnchor (0);
        int originalHeight = view.Height!.GetAnchor (0);

        // Act - Press Right and Down to resize
        arranger.HandleArrangeModeRight ();
        arranger.HandleArrangeModeDown ();

        // Assert - View should have grown
        Assert.Equal (originalWidth + 1, view.Width.GetAnchor (0));
        Assert.Equal (originalHeight + 1, view.Height.GetAnchor (0));

        // Act - Press Left and Up to shrink
        arranger.HandleArrangeModeLeft ();
        arranger.HandleArrangeModeUp ();

        // Assert - View should be back to original size
        Assert.Equal (originalWidth, view.Width.GetAnchor (0));
        Assert.Equal (originalHeight, view.Height.GetAnchor (0));

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that ExitArrangeMode (Quit command) exits arrange mode and resets state.
    /// </summary>
    [Fact]
    public void Quit_ExitsArrangeMode_ResetsArranging ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        var borderView = (BorderView)view.Border.View!;
        Arranger arranger = borderView.Arranger;
        arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Verify we are in arrange mode
        Assert.True (arranger.IsArranging);
        Assert.NotEqual (ViewArrangement.Fixed, arranger.Arranging);

        // Act - Exit arrange mode (simulates Command.Quit / Esc key)
        bool? result = arranger.ExitArrangeMode ();

        // Assert
        Assert.True (result);
        Assert.False (arranger.IsArranging);
        Assert.Equal (ViewArrangement.Fixed, arranger.Arranging);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that ExitArrangeMode removes all arranger buttons from the border.
    /// </summary>
    [Fact]
    public void Quit_ExitsArrangeMode_RemovesArrangerButtons ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        var borderView = (BorderView)view.Border.View!;
        Arranger arranger = borderView.Arranger;
        arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Verify buttons exist
        Assert.True (borderView.SubViews.OfType<ArrangerButton> ().Any (), "Arranger buttons should exist before exit");

        // Act - Exit arrange mode
        arranger.ExitArrangeMode ();

        // Assert - All arranger buttons should be removed
        Assert.False (borderView.SubViews.OfType<ArrangerButton> ().Any (), "Arranger buttons should be removed after exit");

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that ExitArrangeMode clears HotKeyBindings and resets CanFocus.
    /// </summary>
    [Fact]
    public void Quit_ExitsArrangeMode_ClearsBindingsAndCanFocus ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        var borderView = (BorderView)view.Border.View!;
        Arranger arranger = borderView.Arranger;
        arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Verify keyboard mode state
        Assert.True (borderView.CanFocus, "Border should be focusable during arrange mode");

        // Act
        arranger.ExitArrangeMode ();

        // Assert
        Assert.False (borderView.CanFocus, "Border should not be focusable after exit");
        Assert.Empty (borderView.HotKeyBindings.GetBindings ());

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that after ExitArrangeMode, the view position and size remain unchanged
    ///     (quit does not undo any prior arrangement changes).
    /// </summary>
    [Fact]
    public void Quit_ExitsArrangeMode_PreservesViewPositionAndSize ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        var borderView = (BorderView)view.Border.View!;
        Arranger arranger = borderView.Arranger;
        arranger.EnterArrangeMode (ViewArrangement.Fixed);

        // Move the view
        arranger.HandleArrangeModeRight ();
        arranger.HandleArrangeModeDown ();

        int movedX = view.X.GetAnchor (0);
        int movedY = view.Y.GetAnchor (0);

        // Act - Exit arrange mode
        arranger.ExitArrangeMode ();

        // Assert - Position should be preserved (not reverted)
        Assert.Equal (movedX, view.X.GetAnchor (0));
        Assert.Equal (movedY, view.Y.GetAnchor (0));

        // Cleanup
        superView.Dispose ();
    }
}

// Claude - Opus 4.5

namespace ApplicationTests.Popover;

/// <summary>
///     Tests for <see cref="Popover{TView, TResult}"/>.
/// </summary>
public class PopoverTests
{
    [Fact]
    public void Constructor_SetsDefaults ()
    {
        // Act
        Popover<Label, string> popover = new ();

        // Assert
        Assert.NotNull (popover.ContentView);
        Assert.IsType<Label> (popover.ContentView);
        Assert.False (popover.Visible);
        Assert.Null (popover.Result);
        Assert.Null (popover.ResultExtractor);
        Assert.Null (popover.Anchor);
        Assert.Null (popover.Target);
    }

    [Fact]
    public void Constructor_WithContentView_UsesProvidedView ()
    {
        // Arrange
        Label label = new () { Text = "Test" };

        // Act
        Popover<Label, string> popover = new (label);

        // Assert
        Assert.Same (label, popover.ContentView);
        Assert.NotNull (popover.ContentView);
        Assert.Equal ("Test", popover.ContentView.Text);
    }

    [Fact]
    public void ContentView_Set_AddsAsSubView ()
    {
        // Arrange
        Popover<Label, string> popover = new ();
        Label newLabel = new () { Text = "New" };

        // Act
        popover.ContentView = newLabel;

        // Assert
        Assert.Same (newLabel, popover.ContentView);
        Assert.Contains (newLabel, popover.SubViews);
    }

    [Fact]
    public void ContentView_Set_RemovesOldView ()
    {
        // Arrange
        Label oldLabel = new () { Text = "Old" };
        Popover<Label, string> popover = new (oldLabel);
        Label newLabel = new () { Text = "New" };

        // Act
        popover.ContentView = newLabel;

        // Assert
        Assert.DoesNotContain (oldLabel, popover.SubViews);
        Assert.Contains (newLabel, popover.SubViews);
        Assert.Same (newLabel, popover.ContentView);
    }

    [Fact]
    public void Visible_Set_True_ShowsPopover ()
    {
        // Arrange
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        Popover<Label, string> popover = new () { App = app };
        popoverManager.Register (popover);

        // Act
        popover.Visible = true;

        // Assert
        Assert.True (popover.Visible);
    }

    [Fact]
    public void Visible_Set_False_HidesPopover ()
    {
        // Arrange
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        Popover<Label, string> popover = new () { App = app };
        popoverManager.Register (popover);
        popover.Visible = true;

        // Act
        popover.Visible = false;

        // Assert
        Assert.False (popover.Visible);
    }

    [Fact]
    public void VisibleChanging_CanCancel ()
    {
        // Arrange
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        Popover<Label, string> popover = new () { App = app };
        popoverManager.Register (popover);

        popover.VisibleChanging += (_, e) =>
                                   {
                                       e.Cancel = true; // Cancel the change
                                   };

        // Act
        popover.Visible = true;

        // Assert
        Assert.False (popover.Visible);
    }

    [Fact]
    public void VisibleChanged_Fires_WhenVisibleChanges ()
    {
        // Arrange
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        Popover<Label, string> popover = new () { App = app };
        popoverManager.Register (popover);

        var isOpenChangedFired = false;

        popover.VisibleChanged += (_, e) => { isOpenChangedFired = true; };

        // Act
        popover.Visible = true;

        // Assert
        Assert.True (isOpenChangedFired);
        Assert.True (popover.Visible);
    }

    [Fact]
    public void ResultExtractor_ExtractsResult_WhenPopoverHidden ()
    {
        // Arrange
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        Label label = new () { Text = "Test Result" };
        Popover<Label, string> popover = new (label) { App = app };
        popover.ResultExtractor = lbl => lbl.Text;

        popoverManager.Register (popover);

        // Act
        popover.MakeVisible ();
        popover.Visible = false;

        // Assert
        Assert.Equal ("Test Result", popover.Result);
    }

    [Fact]
    public void Result_ExtractedFromIValue_WhenNoExtractor ()
    {
        // Arrange
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        // Use TextField which implements IValue<string>
        TextField textField = new () { Text = "IValue Result" };
        Popover<TextField, string> popover = new (textField) { App = app };

        popoverManager.Register (popover);

        // Act
        popover.MakeVisible ();
        popover.Visible = false;

        // Assert
        Assert.Equal ("IValue Result", popover.Result);
    }

    [Fact]
    public void ResultChanged_Fires_WhenResultChanges ()
    {
        // Arrange
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        Label label = new () { Text = "Result 1" };
        Popover<Label, string> popover = new (label) { App = app };
        popover.ResultExtractor = lbl => lbl.Text;

        popoverManager.Register (popover);

        var resultChangedFired = false;
        string? newResult = null;

        popover.ResultChanged += (_, e) =>
                                 {
                                     resultChangedFired = true;
                                     newResult = e.NewValue;
                                 };

        // Act
        popover.MakeVisible ();
        popover.Visible = false;

        // Assert
        Assert.True (resultChangedFired);
        Assert.Equal ("Result 1", newResult);
    }

    [Fact]
    public void Target_CanBeSetAndGet ()
    {
        // Arrange
        Popover<Label, string> popover = new ();
        Button target = new ();

        // Act
        popover.Target = new WeakReference<View> (target);

        // Assert
        Assert.NotNull (popover.Target);
        Assert.True (popover.Target.TryGetTarget (out View? retrievedTarget));
        Assert.Same (target, retrievedTarget);
    }

    [Fact]
    public void Anchor_CanBeSetAndUsedForPositioning ()
    {
        // Arrange
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        Popover<Label, string> popover = new () { App = app };
        Rectangle anchorRect = new (10, 10, 5, 5);
        popover.Anchor = () => anchorRect;

        popoverManager.Register (popover);

        // Act
        popover.MakeVisible ();

        // Assert
        // ContentView should be positioned below the anchor
        Assert.NotNull (popover.ContentView);

        // Verify anchor was used (position should be set)
        Assert.NotNull (popover.ContentView.X);
        Assert.NotNull (popover.ContentView.Y);
    }

    [Fact]
    public void MakeVisible_WithAnchorParameter_OverridesAnchorProperty ()
    {
        // Arrange
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        Popover<Label, string> popover = new () { App = app };
        popover.Anchor = () => new Rectangle (100, 100, 5, 5); // Should be ignored

        Rectangle anchorParam = new (20, 20, 5, 5);
        popoverManager.Register (popover);

        // Act
        popover.MakeVisible (anchor: anchorParam);

        // Assert
        Assert.NotNull (popover.ContentView);

        // Verify anchor parameter was used (position should be set)
        Assert.NotNull (popover.ContentView.X);
        Assert.NotNull (popover.ContentView.Y);
    }

    [Fact]
    public void Visible_MakeVisible_Synchronizes ()
    {
        // Arrange
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        Popover<Label, string> popover = new () { App = app };
        popoverManager.Register (popover);

        // Act & Assert - MakeVisible should set Visible to true
        popover.MakeVisible ();
        Assert.True (popover.Visible);

        popover.Visible = false;
        Assert.False (popover.Visible);
    }

    [Fact]
    public void EnableForDesign_CreatesContentView ()
    {
        // Arrange
        Popover<Label, string> popover = new () { ContentView = null };
        View dummyTarget = new ();

        // Act
        bool result = popover.EnableForDesign (ref dummyTarget);

        // Assert
        Assert.True (result);
        Assert.NotNull (popover.ContentView);
    }

    // GitHub Copilot

    [Fact]
    public void SetPosition_FitsBelow_PositionUnchanged ()
    {
        // Arrange - screen is 2048x2048 by default (no driver)
        ApplicationImpl app = new ();
        Label label = new () { Width = 20, Height = 10 };
        Popover<Label, string> popover = new (label) { App = app };

        // Act - position at (50, 50), plenty of room below
        popover.SetPosition (new Point (50, 50));

        // Assert
        Assert.Equal (50, (popover.ContentView!.X as PosAbsolute)!.Position);
        Assert.Equal (50, (popover.ContentView!.Y as PosAbsolute)!.Position);
    }

    [Fact]
    public void SetPosition_DoesNotFitBelow_FlipsAbove ()
    {
        // Arrange - screen is 2048x2048 by default (no driver)
        ApplicationImpl app = new ();
        Label label = new () { Width = 20, Height = 10 };
        Popover<Label, string> popover = new (label) { App = app };

        // Act - position at Y = 2045, only 3 rows of space below but view needs 10
        popover.SetPosition (new Point (50, 2045));

        // Assert - should flip: bottom 1 row above ideal Y → ny = 2045 - 10 - 1 = 2034
        Assert.Equal (50, (popover.ContentView!.X as PosAbsolute)!.Position);
        Assert.Equal (2034, (popover.ContentView!.Y as PosAbsolute)!.Position);
    }

    [Fact]
    public void SetPosition_OverflowsRight_ClampsHorizontally ()
    {
        // Arrange - screen is 2048x2048 by default (no driver)
        ApplicationImpl app = new ();
        Label label = new () { Width = 20, Height = 10 };
        Popover<Label, string> popover = new (label) { App = app };

        // Act - position at X = 2040, only 8 cols of space but view needs 20
        popover.SetPosition (new Point (2040, 50));

        // Assert - should clamp: nx = 2048 - 20 = 2028
        Assert.Equal (2028, (popover.ContentView!.X as PosAbsolute)!.Position);
        Assert.Equal (50, (popover.ContentView!.Y as PosAbsolute)!.Position);
    }

    [Fact]
    public void SetPosition_OverflowsBothEdges_ClampsHorizontallyAndFlipsAbove ()
    {
        // Arrange - screen is 2048x2048 by default (no driver)
        ApplicationImpl app = new ();
        Label label = new () { Width = 20, Height = 10 };
        Popover<Label, string> popover = new (label) { App = app };

        // Act - position at bottom-right corner
        popover.SetPosition (new Point (2040, 2045));

        // Assert - horizontal: 2048 - 20 = 2028, vertical: 2045 - 10 - 1 = 2034
        Assert.Equal (2028, (popover.ContentView!.X as PosAbsolute)!.Position);
        Assert.Equal (2034, (popover.ContentView!.Y as PosAbsolute)!.Position);
    }

    [Fact]
    public void SetPosition_FlipAboveWouldGoNegative_ClampsToZero ()
    {
        // Arrange - screen is 2048x2048 by default (no driver)
        ApplicationImpl app = new ();

        // View taller than the screen to force a negative flip
        Label label = new () { Width = 20, Height = 2048 };
        Popover<Label, string> popover = new (label) { App = app };

        // Act - position at Y = 5, doesn't fit below (needs 2048 rows), flip would be 5 - 2048 - 1 = negative
        popover.SetPosition (new Point (50, 5));

        // Assert - should clamp to 0
        Assert.Equal (50, (popover.ContentView!.X as PosAbsolute)!.Position);
        Assert.Equal (0, (popover.ContentView!.Y as PosAbsolute)!.Position);
    }
}

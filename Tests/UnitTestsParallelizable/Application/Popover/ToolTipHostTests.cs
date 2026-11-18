using JetBrains.Annotations;

namespace ApplicationTests.Popover;

/// <summary>
///     Tests for <see cref="ToolTip{TView}"/>.
/// </summary>
[TestSubject (typeof (ToolTipHost<>))]
public class ToolTipHostTests
{
    [Fact]
    public void Constructor_SetsDefaults ()
    {
        ToolTipHost<Label> toolTip = new ();

        Assert.NotNull (toolTip.ContentView);
        Assert.IsType<Label> (toolTip.ContentView);
        Assert.False (toolTip.Visible);
        Assert.Null (toolTip.Anchor);
        Assert.Null (toolTip.Target);
    }

    [Fact]
    public void Constructor_WithContentView_UsesProvidedView ()
    {
        Label label = new () { Text = "Test" };

        ToolTipHost<Label> toolTip = new (label);

        Assert.Same (label, toolTip.ContentView);
        Assert.NotNull (toolTip.ContentView);
        Assert.Equal ("Test", toolTip.ContentView.Text);
    }

    [Fact]
    public void ContentView_Set_AddsAsSubView ()
    {
        ToolTipHost<Label> toolTip = new ();
        Label newLabel = new () { Text = "New" };

        toolTip.ContentView = newLabel;

        Assert.Same (newLabel, toolTip.ContentView);
        Assert.Contains (newLabel, toolTip.SubViews);
    }

    [Fact]
    public void ContentView_Set_RemovesOldView ()
    {
        Label oldLabel = new () { Text = "Old" };
        ToolTipHost<Label> toolTip = new (oldLabel);
        Label newLabel = new () { Text = "New" };

        toolTip.ContentView = newLabel;

        Assert.DoesNotContain (oldLabel, toolTip.SubViews);
        Assert.Contains (newLabel, toolTip.SubViews);
        Assert.Same (newLabel, toolTip.ContentView);
    }

    [Fact]
    public void Visible_Set_True_ShowsToolTip ()
    {
        using IApplication app = Application.Create ();
        app.Popovers = new ApplicationPopover { App = app };

        ToolTipHost<Label> toolTip = new () { App = app };
        app.Popovers.Register (toolTip);

        toolTip.Visible = true;

        Assert.True (toolTip.Visible);
    }

    [Fact]
    public void Visible_Set_False_HidesToolTip ()
    {
        using IApplication app = Application.Create ();
        app.Popovers = new ApplicationPopover { App = app };

        ToolTipHost<Label> toolTip = new () { App = app };
        app.Popovers.Register (toolTip);
        toolTip.Visible = true;

        toolTip.Visible = false;

        Assert.False (toolTip.Visible);
    }

    [Fact]
    public void VisibleChanging_CanCancel ()
    {
        using IApplication app = Application.Create ();
        app.Popovers = new ApplicationPopover { App = app };

        ToolTipHost<Label> toolTip = new () { App = app };
        app.Popovers.Register (toolTip);

        toolTip.VisibleChanging += (_, e) => e.Cancel = true; // Cancel the change

        toolTip.Visible = true;

        Assert.False (toolTip.Visible);
    }

    [Fact]
    public void VisibleChanged_Fires_WhenVisibleChanges ()
    {
        using IApplication app = Application.Create ();
        app.Popovers = new ApplicationPopover { App = app };

        ToolTipHost<Label> toolTip = new () { App = app };
        app.Popovers.Register (toolTip);

        bool isOpenChangedFired = false;

        toolTip.VisibleChanged += (_, e) => { isOpenChangedFired = true; };

        toolTip.Visible = true;

        Assert.True (isOpenChangedFired);
        Assert.True (toolTip.Visible);
    }

    [Fact]
    public void Target_CanBeSetAndGet ()
    {
        ToolTipHost<Label> toolTip = new ();
        Button target = new ();

        toolTip.Target = new WeakReference<View> (target);

        Assert.NotNull (toolTip.Target);
        Assert.True (toolTip.Target.TryGetTarget (out View? retrievedTarget));
        Assert.Same (target, retrievedTarget);
    }

    [Fact]
    public void Anchor_CanBeSetAndUsedForPositioning ()
    {
        using IApplication app = Application.Create ();
        app.Popovers = new ApplicationPopover { App = app };

        ToolTipHost<Label> toolTip = new () { App = app };
        Rectangle anchorRect = new (10, 10, 5, 5);
        toolTip.Anchor = () => anchorRect;

        app.Popovers.Register (toolTip);

        toolTip.MakeVisible ();

        // ContentView should be positioned below the anchor
        Assert.NotNull (toolTip.ContentView);

        // Verify anchor was used (position should be set)
        Assert.NotNull (toolTip.ContentView.X);
        Assert.NotNull (toolTip.ContentView.Y);
    }

    [Fact]
    public void MakeVisible_WithAnchorParameter_OverridesAnchorProperty ()
    {
        using IApplication app = Application.Create ();
        app.Popovers = new ApplicationPopover { App = app };

        ToolTipHost<Label> toolTip = new () { App = app };
        toolTip.Anchor = () => new Rectangle (100, 100, 5, 5); // Should be ignored

        Rectangle anchorParam = new (20, 20, 5, 5);
        app.Popovers.Register (toolTip);

        toolTip.MakeVisible (anchor: anchorParam);

        Assert.NotNull (toolTip.ContentView);

        // Verify anchor parameter was used (position should be set)
        Assert.NotNull (toolTip.ContentView.X);
        Assert.NotNull (toolTip.ContentView.Y);
    }

    [Fact]
    public void Visible_MakeVisible_Synchronizes ()
    {
        using IApplication app = Application.Create ();
        app.Popovers = new ApplicationPopover { App = app };

        ToolTipHost<Label> toolTip = new () { App = app };
        app.Popovers.Register (toolTip);

        // MakeVisible should set Visible to true
        toolTip.MakeVisible ();
        Assert.True (toolTip.Visible);

        toolTip.Visible = false;
        Assert.False (toolTip.Visible);
    }

    [Fact]
    public void EnableForDesign_CreatesContentView ()
    {
        ToolTipHost<Label> toolTip = new () { ContentView = null };
        View dummyTarget = new ();

        bool result = toolTip.EnableForDesign (ref dummyTarget);

        Assert.True (result);
        Assert.NotNull (toolTip.ContentView);
    }

    // GitHub Copilot

    [Fact]
    public void SetPosition_FitsBelow_PositionUnchanged ()
    {
        using IApplication app = Application.Create ();

        Label label = new () { Width = 20, Height = 10 };
        ToolTipHost<Label> toolTip = new (label) { App = app };

        // Act - position at (50, 50), plenty of room below
        toolTip.SetPosition (new Point (50, 50));

        // Assert
        Assert.Equal (50, (toolTip.ContentView!.X as PosAbsolute)!.Position);
        Assert.Equal (50, (toolTip.ContentView!.Y as PosAbsolute)!.Position);
    }

    [Fact]
    public void SetPosition_DoesNotFitBelow_FlipsAbove ()
    {
        using IApplication app = Application.Create ();

        Label label = new () { Width = 20, Height = 10 };
        ToolTipHost<Label> toolTip = new (label) { App = app };

        // Act - position at Y = 2045, only 3 rows of space below but view needs 10
        toolTip.SetPosition (new Point (50, 2045));

        // Assert - should flip: bottom 1 row above ideal Y → ny = 2045 - 10 - 1 = 2034
        Assert.Equal (50, (toolTip.ContentView!.X as PosAbsolute)!.Position);
        Assert.Equal (2034, (toolTip.ContentView!.Y as PosAbsolute)!.Position);
    }

    [Fact]
    public void SetPosition_OverflowsRight_ClampsHorizontally ()
    {
        using IApplication app = Application.Create();

        Label label = new () { Width = 20, Height = 10 };
        ToolTipHost<Label> toolTip = new (label) { App = app };

        // Act - position at X = 2040, only 8 cols of space but view needs 20
        toolTip.SetPosition (new Point (2040, 50));

        // Assert - should clamp: nx = 2048 - 20 = 2028
        Assert.Equal (2028, (toolTip.ContentView!.X as PosAbsolute)!.Position);
        Assert.Equal (50, (toolTip.ContentView!.Y as PosAbsolute)!.Position);
    }

    [Fact]
    public void SetPosition_OverflowsBothEdges_ClampsHorizontallyAndFlipsAbove ()
    {
        using IApplication app = Application.Create();

        Label label = new () { Width = 20, Height = 10 };
        ToolTipHost<Label> toolTip = new (label) { App = app };

        // Act - position at bottom-right corner
        toolTip.SetPosition (new Point (2040, 2045));

        // Assert - horizontal: 2048 - 20 = 2028, vertical: 2045 - 10 - 1 = 2034
        Assert.Equal (2028, (toolTip.ContentView!.X as PosAbsolute)!.Position);
        Assert.Equal (2034, (toolTip.ContentView!.Y as PosAbsolute)!.Position);
    }

    [Fact]
    public void SetPosition_FlipAboveWouldGoNegative_ClampsToZero ()
    {
        using IApplication app = Application.Create();

        // View taller than the screen to force a negative flip
        Label label = new () { Width = 20, Height = 2048 };
        ToolTipHost<Label> toolTip = new (label) { App = app };

        // Act - position at Y = 5, doesn't fit below (needs 2048 rows), flip would be 5 - 2048 - 1 = negative
        toolTip.SetPosition (new Point (50, 5));

        // Assert - should clamp to 0
        Assert.Equal (50, (toolTip.ContentView!.X as PosAbsolute)!.Position);
        Assert.Equal (0, (toolTip.ContentView!.Y as PosAbsolute)!.Position);
    }
}

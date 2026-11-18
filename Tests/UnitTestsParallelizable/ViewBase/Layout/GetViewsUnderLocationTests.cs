#nullable enable

namespace ViewBaseTests.MouseTests;

[Trait ("Category", "Input")]
public class GetViewsUnderLocationTests
{
    [Theory]
    [InlineData (0, 0)]
    [InlineData (2, 1)]
    [InlineData (20, 20)]
    public void Returns_Null_If_No_SubViews_Coords_Outside (int testX, int testY)
    {
        // Arrange
        var view = new View
        {
            Frame = new (0, 0, 10, 10)
        };

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = view.GetViewsUnderLocation (location, ViewportSettingsFlags.None);

        // Assert
        Assert.Empty (viewsUnderMouse);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (2, 1)]
    [InlineData (20, 20)]
    public void Returns_Null_If_Start_Not_Visible (int testX, int testY)
    {
        // Arrange
        var view = new View
        {
            Frame = new (0, 0, 10, 10),
            Visible = false
        };

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = view.GetViewsUnderLocation (location, ViewportSettingsFlags.None);

        // Assert
        Assert.Empty (viewsUnderMouse);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, -1, -1, null)]
    [InlineData (0, 0, 0, 0, 0, 0, 0, typeof (View))]
    [InlineData (0, 0, 0, 0, 0, 1, 1, typeof (View))]
    [InlineData (0, 0, 0, 0, 0, 4, 4, typeof (View))]
    [InlineData (0, 0, 0, 0, 0, 9, 9, typeof (View))]
    [InlineData (0, 0, 0, 0, 0, 10, 10, null)]
    [InlineData (1, 1, 0, 0, 0, -1, -1, null)]
    [InlineData (1, 1, 0, 0, 0, 0, 0, null)]
    [InlineData (1, 1, 0, 0, 0, 1, 1, typeof (View))]
    [InlineData (1, 1, 0, 0, 0, 4, 4, typeof (View))]
    [InlineData (1, 1, 0, 0, 0, 9, 9, typeof (View))]
    [InlineData (1, 1, 0, 0, 0, 10, 10, typeof (View))]
    [InlineData (0, 0, 1, 0, 0, -1, -1, null)]
    [InlineData (0, 0, 1, 0, 0, 0, 0, typeof (Margin))]
    [InlineData (0, 0, 1, 0, 0, 1, 1, typeof (View))]
    [InlineData (0, 0, 1, 0, 0, 4, 4, typeof (View))]
    [InlineData (0, 0, 1, 0, 0, 9, 9, typeof (Margin))]
    [InlineData (0, 0, 1, 0, 0, 10, 10, null)]
    [InlineData (0, 0, 1, 1, 0, -1, -1, null)]
    [InlineData (0, 0, 1, 1, 0, 0, 0, typeof (Margin))]
    [InlineData (0, 0, 1, 1, 0, 1, 1, typeof (Border))]
    [InlineData (0, 0, 1, 1, 0, 4, 4, typeof (View))]
    [InlineData (0, 0, 1, 1, 0, 9, 9, typeof (Margin))]
    [InlineData (0, 0, 1, 1, 0, 10, 10, null)]
    [InlineData (0, 0, 1, 1, 1, -1, -1, null)]
    [InlineData (0, 0, 1, 1, 1, 0, 0, typeof (Margin))]
    [InlineData (0, 0, 1, 1, 1, 1, 1, typeof (Border))]
    [InlineData (0, 0, 1, 1, 1, 2, 2, typeof (Padding))]
    [InlineData (0, 0, 1, 1, 1, 4, 4, typeof (View))]
    [InlineData (0, 0, 1, 1, 1, 9, 9, typeof (Margin))]
    [InlineData (0, 0, 1, 1, 1, 10, 10, null)]
    [InlineData (1, 1, 1, 0, 0, -1, -1, null)]
    [InlineData (1, 1, 1, 0, 0, 0, 0, null)]
    [InlineData (1, 1, 1, 0, 0, 1, 1, typeof (Margin))]
    [InlineData (1, 1, 1, 0, 0, 4, 4, typeof (View))]
    [InlineData (1, 1, 1, 0, 0, 9, 9, typeof (View))]
    [InlineData (1, 1, 1, 0, 0, 10, 10, typeof (Margin))]
    [InlineData (1, 1, 1, 1, 0, -1, -1, null)]
    [InlineData (1, 1, 1, 1, 0, 0, 0, null)]
    [InlineData (1, 1, 1, 1, 0, 1, 1, typeof (Margin))]
    [InlineData (1, 1, 1, 1, 0, 4, 4, typeof (View))]
    [InlineData (1, 1, 1, 1, 0, 9, 9, typeof (Border))]
    [InlineData (1, 1, 1, 1, 0, 10, 10, typeof (Margin))]
    [InlineData (1, 1, 1, 1, 1, -1, -1, null)]
    [InlineData (1, 1, 1, 1, 1, 0, 0, null)]
    [InlineData (1, 1, 1, 1, 1, 1, 1, typeof (Margin))]
    [InlineData (1, 1, 1, 1, 1, 2, 2, typeof (Border))]
    [InlineData (1, 1, 1, 1, 1, 3, 3, typeof (Padding))]
    [InlineData (1, 1, 1, 1, 1, 4, 4, typeof (View))]
    [InlineData (1, 1, 1, 1, 1, 8, 8, typeof (Padding))]
    [InlineData (1, 1, 1, 1, 1, 9, 9, typeof (Border))]
    [InlineData (1, 1, 1, 1, 1, 10, 10, typeof (Margin))]
    public void Contains (
        int frameX,
        int frameY,
        int marginThickness,
        int borderThickness,
        int paddingThickness,
        int testX,
        int testY,
        Type? expectedAdornmentType
    )
    {
        var view = new View
        {
            X = frameX, Y = frameY,
            Width = 10, Height = 10
        };
        view.Margin.Thickness = new (marginThickness);
        view.Border.Thickness = new (borderThickness);
        view.Padding.Thickness = new (paddingThickness);

        Type? containedType = null;

        if (view.Contains (new (testX, testY)))
        {
            containedType = view.GetType ();
        }

        if (view.Margin.Contains (new (testX, testY)))
        {
            containedType = view.Margin.GetType ();
        }

        if (view.Border.Contains (new (testX, testY)))
        {
            containedType = view.Border.GetType ();
        }

        if (view.Padding.Contains (new (testX, testY)))
        {
            containedType = view.Padding.GetType ();
        }

        Assert.Equal (expectedAdornmentType, containedType);
    }

    [Fact]
    public void GetViewsUnderLocation_Returns_Adornment_Subview_When_Parent_Has_Subview_At_Same_Location ()
    {
        // Arrange - Reproduces the bug where:
        // - Parent has ExpanderButton in its Border
        // - A subview with X=-1 (extends outside parent's content) has a Border that overlaps ExpanderButton
        // - Bug: GetViewsUnderLocation returns subview.Border instead of ExpanderButton
        IApplication app = Application.Create ();

        Runnable<bool> runnable = new ()
        {
            Width = 50,
            Height = 50
        };
        app.Begin (runnable);

        // Create parent view
        var parent = new View
        {
            X = 0,
            Y = 0,
            Width = 30,
            Height = 10
        };
        parent.Border.Thickness = new (1);
        parent.Border.ViewportSettings = ViewportSettingsFlags.None;

        // Add ExpanderButton to parent's Border at (0, 0)  
        // Since parent.Border has thickness=1, the Border's viewport starts at (0,0) screen coords
        // And the ExpanderButton at (0,0) relative to Border viewport is at screen (0,0)
        var expanderButton = new Button
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1,
            Text = ">",
            ShadowStyle = null
        };
        parent.Border.GetOrCreateView ().Add (expanderButton);

        // Add a subview at X=-1, Y=-1 (extends outside parent's Viewport in both dimensions)
        // The subview's Border will overlap with the ExpanderButton location
        var childView = new View
        {
            X = -1, // This causes child's left edge to be at screen X=0 (parent content starts at X=1)
            Y = -1, // This causes child's top edge to be at screen Y=0 (parent content starts at Y=1)
            Width = 20,
            Height = 5
        };
        childView.Border.Thickness = new (1);
        childView.Border.ViewportSettings = ViewportSettingsFlags.None;
        parent.Add (childView);

        runnable.Add (parent);
        runnable.Layout ();

        // Get screen location of ExpanderButton
        Rectangle buttonFrame = expanderButton.FrameToScreen ();
        Point testLocation = buttonFrame.Location;

        // Verify that childView.Border also contains this location (this is the bug scenario)
        Rectangle childBorderFrame = childView.Border.FrameToScreen ();

        Assert.True (
                     childBorderFrame.Contains (testLocation),
                     $"Test setup failed: childView.Border ({childBorderFrame}) should contain testLocation ({testLocation})");

        // Act
        List<View?> viewsUnderLocation = runnable.GetViewsUnderLocation (testLocation, ViewportSettingsFlags.None);

        // Assert
        View? deepestView = viewsUnderLocation.LastOrDefault ();
        Assert.NotNull (deepestView);

        // The ExpanderButton is a subview of parent.Border, which is processed before childView
        // But childView.Border is processed AFTER ExpanderButton, causing the bug
        // The correct deepest view should be ExpanderButton, not childView.Border
        Assert.Equal (expanderButton, deepestView);

        app.Dispose ();
    }
}

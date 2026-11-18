using UnitTests;

namespace ViewBaseTests.Navigation;

// Copilot
public class PreviouslyFocusedTests : TestsAllViews
{
    [Fact]
    public void PreviouslyFocused_Is_Null_By_Default ()
    {
        // Arrange
        View view = new () { Id = "view", CanFocus = true };

        // Assert
        Assert.Null (view.PreviouslyFocused);
    }

    [Fact]
    public void PreviouslyFocused_Set_When_SubView_Loses_Focus ()
    {
        // Arrange
        View superView = new () { Id = "superView", CanFocus = true };

        View subView1 = new () { Id = "subView1", CanFocus = true };
        View subView2 = new () { Id = "subView2", CanFocus = true };
        superView.Add (subView1, subView2);

        superView.SetFocus ();
        Assert.True (subView1.HasFocus);

        // Act - Move focus to subView2
        subView2.SetFocus ();

        // Assert - superView.PreviouslyFocused should be subView1 (the one that lost focus)
        Assert.True (subView2.HasFocus);
        Assert.False (subView1.HasFocus);
        Assert.Equal (subView1, superView.PreviouslyFocused);
    }

    [Fact]
    public void PreviouslyFocused_Tracks_Last_Focused_SubView ()
    {
        // Arrange
        View superView = new () { Id = "superView", CanFocus = true };

        View subView1 = new () { Id = "subView1", CanFocus = true };
        View subView2 = new () { Id = "subView2", CanFocus = true };
        View subView3 = new () { Id = "subView3", CanFocus = true };
        superView.Add (subView1, subView2, subView3);

        superView.SetFocus ();
        Assert.True (subView1.HasFocus);

        // Act - Move focus from subView1 to subView2
        subView2.SetFocus ();
        Assert.Equal (subView1, superView.PreviouslyFocused);

        // Act - Move focus from subView2 to subView3
        subView3.SetFocus ();

        // Assert - PreviouslyFocused should now be subView2 (the last one that lost focus)
        Assert.Equal (subView2, superView.PreviouslyFocused);
    }

    [Fact]
    public void ClearFocus_Sets_PreviouslyFocused_To_Null ()
    {
        // Arrange
        View superView = new () { Id = "superView", CanFocus = true };

        View subView1 = new () { Id = "subView1", CanFocus = true };
        View subView2 = new () { Id = "subView2", CanFocus = true };
        superView.Add (subView1, subView2);

        superView.SetFocus ();
        subView2.SetFocus ();
        Assert.NotNull (superView.PreviouslyFocused);

        // Act
        superView.ClearFocus ();

        // Assert
        Assert.Null (superView.PreviouslyFocused);
    }

    [Fact]
    public void PreviouslyFocused_Cleared_When_View_Gains_Focus ()
    {
        // Arrange
        View superView = new () { Id = "superView", CanFocus = true };

        View subView1 = new () { Id = "subView1", CanFocus = true };
        View subView2 = new () { Id = "subView2", CanFocus = true };
        superView.Add (subView1, subView2);

        superView.SetFocus ();
        Assert.True (subView1.HasFocus);

        // Move focus to subView2, so superView.PreviouslyFocused = subView1
        subView2.SetFocus ();
        Assert.Equal (subView1, superView.PreviouslyFocused);

        // Act - Lose focus and regain it
        superView.HasFocus = false;
        Assert.False (superView.HasFocus);

        superView.SetFocus ();

        // Assert - PreviouslyFocused should be cleared when focus is gained
        Assert.True (superView.HasFocus);
        Assert.Null (superView.PreviouslyFocused);
    }

    [Fact]
    public void Remove_Clears_PreviouslyFocused ()
    {
        // Arrange
        View superView = new () { Id = "superView", CanFocus = true };

        View subView1 = new () { Id = "subView1", CanFocus = true };
        View subView2 = new () { Id = "subView2", CanFocus = true };
        superView.Add (subView1, subView2);

        superView.SetFocus ();
        Assert.True (subView1.HasFocus);

        // Act - Remove the focused SubView
        superView.Remove (subView1);

        // Assert - PreviouslyFocused should be null after removal
        Assert.Null (superView.PreviouslyFocused);
    }

    [Fact]
    public void PreviouslyFocused_Cleared_When_PreviouslyFocused_View_Is_Removed ()
    {
        // Arrange
        View superView = new () { Id = "superView", CanFocus = true };

        View subView1 = new () { Id = "subView1", CanFocus = true };
        View subView2 = new () { Id = "subView2", CanFocus = true };
        superView.Add (subView1, subView2);

        superView.SetFocus ();
        subView2.SetFocus ();
        Assert.Equal (subView1, superView.PreviouslyFocused);

        // Act - Remove subView1 (the PreviouslyFocused SubView)
        superView.Remove (subView1);

        // Assert - PreviouslyFocused should be null because subView1 was removed
        Assert.Null (superView.PreviouslyFocused);
    }

    [Fact]
    public void RestoreFocus_Uses_PreviouslyFocused ()
    {
        // Arrange
        View superView = new () { Id = "superView", CanFocus = true };

        View subView1 = new () { Id = "subView1", CanFocus = true };
        View subView2 = new () { Id = "subView2", CanFocus = true };
        View subView3 = new () { Id = "subView3", CanFocus = true };
        superView.Add (subView1, subView2, subView3);

        superView.SetFocus ();
        Assert.True (subView1.HasFocus);

        // Move focus to subView3
        subView3.SetFocus ();
        Assert.True (subView3.HasFocus);

        // Lose focus
        superView.HasFocus = false;
        Assert.False (superView.HasFocus);

        // Act - Restore focus
        superView.RestoreFocus ();

        // Assert - Should restore focus to subView3 (the PreviouslyFocused SubView of superView)
        Assert.True (superView.HasFocus);
        Assert.True (subView3.HasFocus);
        Assert.Equal (subView3, superView.Focused);
    }

    [Fact]
    public void PreviouslyFocused_Not_Set_For_Non_Focusable_Views ()
    {
        // Arrange
        View superView = new () { Id = "superView", CanFocus = true };

        View nonFocusableSubView = new () { Id = "nonFocusable", CanFocus = false };
        View focusableSubView = new () { Id = "focusable", CanFocus = true };
        superView.Add (nonFocusableSubView, focusableSubView);

        // Act
        superView.SetFocus ();

        // Assert - focusableSubView should get focus, PreviouslyFocused should remain null
        // because no other focusable SubView lost focus
        Assert.True (focusableSubView.HasFocus);
        Assert.Null (superView.PreviouslyFocused);
    }

    [Fact]
    public void PreviouslyFocused_Set_On_SuperView_When_Focus_Lost ()
    {
        // Arrange - PreviouslyFocused is set on the SuperView, not on the view itself
        View outerView = new () { Id = "outerView", CanFocus = true };

        View innerView = new () { Id = "innerView", CanFocus = true };
        View subView = new () { Id = "subView", CanFocus = true };
        innerView.Add (subView);
        outerView.Add (innerView);

        outerView.SetFocus ();
        Assert.True (subView.HasFocus);
        Assert.True (innerView.HasFocus);

        // Act - Turn off focus at the top
        outerView.HasFocus = false;

        // Assert - outerView.PreviouslyFocused should be innerView
        Assert.NotNull (outerView.PreviouslyFocused);
        Assert.Equal (innerView, outerView.PreviouslyFocused);
    }

    [Fact]
    public void PreviouslyFocused_Nested_Hierarchy_Each_Level_Tracks ()
    {
        // Arrange
        View outerView = new () { Id = "outerView", CanFocus = true };

        View innerView = new () { Id = "innerView", CanFocus = true };
        View subView1 = new () { Id = "subView1", CanFocus = true };
        View subView2 = new () { Id = "subView2", CanFocus = true };
        innerView.Add (subView1, subView2);
        outerView.Add (innerView);

        outerView.SetFocus ();
        Assert.True (subView1.HasFocus);

        // Move to subView2
        subView2.SetFocus ();
        Assert.Equal (subView1, innerView.PreviouslyFocused);

        // Act - Lose all focus
        outerView.HasFocus = false;

        // Assert - Each level should track its PreviouslyFocused
        Assert.Equal (innerView, outerView.PreviouslyFocused);
        Assert.Equal (subView2, innerView.PreviouslyFocused);
    }

    [Fact]
    public void ClearFocus_Does_Not_Affect_HasFocus ()
    {
        // Arrange
        View superView = new () { Id = "superView", CanFocus = true };

        View subView1 = new () { Id = "subView1", CanFocus = true };
        View subView2 = new () { Id = "subView2", CanFocus = true };
        superView.Add (subView1, subView2);

        superView.SetFocus ();
        subView2.SetFocus ();
        Assert.True (subView2.HasFocus);
        Assert.True (superView.HasFocus);

        // Act
        superView.ClearFocus ();

        // Assert - ClearFocus only clears PreviouslyFocused, not current focus
        Assert.Null (superView.PreviouslyFocused);
        Assert.True (superView.HasFocus);
        Assert.True (subView2.HasFocus);
    }

    [Fact]
    public void RestoreFocus_Returns_False_When_PreviouslyFocused_Is_Null ()
    {
        // Arrange
        View superView = new () { Id = "superView", CanFocus = true };

        View subView = new () { Id = "subView", CanFocus = true };
        superView.Add (subView);

        // Don't set focus, so PreviouslyFocused stays null

        // Act
        bool result = superView.RestoreFocus ();

        // Assert
        Assert.False (result);
        Assert.Null (superView.PreviouslyFocused);
    }

    [Fact]
    public void RestoreFocus_Returns_False_When_View_Already_Has_Focused_SubView ()
    {
        // Arrange
        View superView = new () { Id = "superView", CanFocus = true };

        View subView1 = new () { Id = "subView1", CanFocus = true };
        View subView2 = new () { Id = "subView2", CanFocus = true };
        superView.Add (subView1, subView2);

        superView.SetFocus ();
        subView2.SetFocus ();

        // PreviouslyFocused is subView1, but Focused is subView2 (Focused is not null)

        // Act
        bool result = superView.RestoreFocus ();

        // Assert - RestoreFocus returns false because Focused is not null
        Assert.False (result);
    }
}

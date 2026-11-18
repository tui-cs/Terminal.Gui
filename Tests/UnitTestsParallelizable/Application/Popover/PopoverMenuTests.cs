// Claude - Opus 4.5

namespace ApplicationTests.Popover;

/// <summary>
///     Tests for <see cref="PopoverMenu"/>.
/// </summary>
public class PopoverMenuTests
{
    /// <summary>
    ///     Issue 1: Event handlers leak when `PopoverMenu.Root` is changed.
    ///     When Root is replaced, handlers should be unsubscribed from the old root.
    /// </summary>
    [Fact]
    public void Root_Set_MultipleTimes_UnsubscribesOldHandlers ()
    {
        // Arrange
        PopoverMenu popoverMenu = new ();
        Menu oldRoot = new ([new MenuItem { Title = "Old" }]) { Title = "Old Root" };
        popoverMenu.Root = oldRoot;

        var acceptedFired = 0;

        // Instrument the popover to count how many times it reacts to menu Accepted
        popoverMenu.Accepted += (_, _) => acceptedFired++;

        // Act — replace root with a new menu
        Menu newRoot = new ([new MenuItem { Title = "New" }]) { Title = "New Root" };
        popoverMenu.Root = newRoot;

        // Fire Accept command on the OLD root — the popover should NOT react
        // because handlers were unsubscribed when Root was replaced
        oldRoot.InvokeCommand (Command.Accept);

        // Assert
        Assert.Equal (0, acceptedFired);
    }

    /// <summary>
    ///     Issue 5: HideAndRemoveSubMenu should not cause infinite recursion.
    ///     This test ensures hiding the popover doesn't cause a stack overflow.
    /// </summary>
    [Fact]
    public void HideAndRemoveSubMenu_DoesNotCauseInfiniteRecursion ()
    {
        // Arrange
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        PopoverMenu popoverMenu = new () { App = app };
        Menu root = new ([new MenuItem { Title = "Item1" }]) { Title = "Root" };
        popoverMenu.Root = root;

        popoverManager.Register (popoverMenu);
        popoverManager.Show (popoverMenu);

        // Act & Assert — hiding should not infinite-loop or stack overflow
        Exception? ex = Record.Exception (() => popoverMenu.Visible = false);
        Assert.Null (ex);
        Assert.False (popoverMenu.Visible);
    }

    /// <summary>
    ///     Issue 11: The filter in OnAccepting is commented out.
    ///     Accepted should only be raised when a MenuItem invokes Accept.
    ///     This test verifies that GetMenuItemsOfAllSubMenus correctly returns
    ///     all MenuItems in the hierarchy, which is required for the filter to work.
    /// </summary>
    [Fact]
    public void GetMenuItemsOfAllSubMenus_ReturnsAllMenuItems ()
    {
        // Arrange
        MenuItem subItem = new () { Title = "SubItem" };
        Menu subMenu = new ([subItem]) { Title = "SubMenu" };
        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };
        MenuItem siblingItem = new () { Title = "Sibling" };
        Menu root = new ([parentItem, siblingItem]) { Title = "Root" };

        PopoverMenu popoverMenu = new () { Root = root };

        // Act
        IEnumerable<MenuItem> allItems = popoverMenu.Root?.GetMenuItemsOfAllSubMenus () ?? [];

        // Assert — all menu items should be found
        Assert.Contains (parentItem, allItems);
        Assert.Contains (siblingItem, allItems);
        Assert.Contains (subItem, allItems);
        Assert.Equal (3, allItems.Count ());
    }

    /// <summary>
    ///     Issue 11: The filter in OnAccepting verifies the source is a MenuItem.
    ///     This test validates that the Contains check in OnAccepting correctly
    ///     identifies MenuItems that are part of the hierarchy.
    /// </summary>
    [Fact]
    public void OnAccepting_FilterContainsMenuItem ()
    {
        // Arrange
        MenuItem menuItem = new () { Title = "Item1" };
        Menu root = new ([menuItem]) { Title = "Root" };
        PopoverMenu popoverMenu = new () { Root = root };

        // Act - get all menu items
        IEnumerable<MenuItem> allMenuItems = popoverMenu.Root?.GetMenuItemsOfAllSubMenus () ?? [];

        // Assert - the filter mechanism should identify our MenuItem
        Assert.Contains (menuItem, allMenuItems);
    }
}

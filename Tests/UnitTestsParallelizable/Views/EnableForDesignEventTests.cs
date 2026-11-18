// Claude - Opus 4.6

namespace ViewsTests;

/// <summary>
///     Tests verifying that EnableForDesign hierarchies raise the correct events
///     when commands are invoked on their subviews.
/// </summary>
public class EnableForDesignEventTests
{
    // ──────────────────────────────────────────────────────────────────────
    // Bar (contains Shortcuts)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Bar_EnableForDesign_Shortcut_Accept_Raises_Accepting_On_Shortcut ()
    {
        Bar bar = new ();
        bar.EnableForDesign ();

        Shortcut shortcut = bar.SubViews.OfType<Shortcut> ().First ();
        bool shortcutAcceptingFired = false;
        shortcut.Accepting += (_, _) => shortcutAcceptingFired = true;

        shortcut.InvokeCommand (Command.Accept);

        Assert.True (shortcutAcceptingFired);

        bar.Dispose ();
    }

    [Fact]
    public void Bar_EnableForDesign_Shortcut_Activate_Raises_Activating_On_Shortcut ()
    {
        Bar bar = new ();
        bar.EnableForDesign ();

        Shortcut shortcut = bar.SubViews.OfType<Shortcut> ().First ();
        bool shortcutActivatingFired = false;
        shortcut.Activating += (_, _) => shortcutActivatingFired = true;

        shortcut.InvokeCommand (Command.Activate);

        Assert.True (shortcutActivatingFired);

        bar.Dispose ();
    }

    [Fact]
    public void Bar_EnableForDesign_All_Shortcuts_Accept_Raises_Accepting ()
    {
        Bar bar = new ();
        bar.EnableForDesign ();

        List<Shortcut> shortcuts = bar.SubViews.OfType<Shortcut> ().ToList ();
        Assert.True (shortcuts.Count >= 3);

        foreach (Shortcut shortcut in shortcuts)
        {
            bool acceptingFired = false;
            shortcut.Accepting += (_, _) => acceptingFired = true;

            shortcut.InvokeCommand (Command.Accept);

            Assert.True (acceptingFired, $"Accepting should fire for Shortcut '{shortcut.Title}'");
        }

        bar.Dispose ();
    }

    [Fact]
    public void Bar_EnableForDesign_CheckBox_CommandView_Direct_Activate_Changes_State ()
    {
        Bar bar = new ();
        bar.EnableForDesign ();

        // Find the Shortcut that has a CheckBox CommandView
        Shortcut checkBoxShortcut = bar.SubViews.OfType<Shortcut> ()
                                       .First (s => s.CommandView is CheckBox);
        CheckBox checkBox = (CheckBox)checkBoxShortcut.CommandView!;

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Direct Activate on CheckBox changes its state
        checkBox.InvokeCommand (Command.Activate);

        Assert.Equal (CheckState.Checked, checkBox.Value);

        bar.Dispose ();
    }

    // ──────────────────────────────────────────────────────────────────────
    // MenuBar (contains MenuBarItems → PopoverMenus → MenuItems)
    // Menu sets CommandsToBubbleUp = [Command.Accept, Command.Activate],
    // so events bubble from MenuItem to Menu.
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void MenuBar_EnableForDesign_MenuItem_Accept_Raises_Menu_Accepting ()
    {
        View targetView = new () { Id = "target" };
        MenuBar menuBar = new ();
        menuBar.EnableForDesign (ref targetView);

        // Get the first MenuBarItem's PopoverMenu Root
        MenuBarItem firstMenuBarItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Menu rootMenu = firstMenuBarItem.PopoverMenu!.Root!;

        // Get the first MenuItem (OfType<MenuItem> filters out Line objects)
        MenuItem menuItem = rootMenu.SubViews.OfType<MenuItem> ().First ();

        bool menuAcceptingFired = false;
        rootMenu.Accepting += (_, _) => menuAcceptingFired = true;

        menuItem.InvokeCommand (Command.Accept);

        Assert.True (menuAcceptingFired);

        menuBar.Dispose ();
        targetView.Dispose ();
    }

    [Fact]
    public void MenuBar_EnableForDesign_MenuItem_Activate_Raises_Menu_Activating ()
    {
        View targetView = new () { Id = "target" };
        MenuBar menuBar = new ();
        menuBar.EnableForDesign (ref targetView);

        MenuBarItem firstMenuBarItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Menu rootMenu = firstMenuBarItem.PopoverMenu!.Root!;

        MenuItem menuItem = rootMenu.SubViews.OfType<MenuItem> ().First ();

        bool menuActivatingFired = false;
        rootMenu.Activating += (_, _) => menuActivatingFired = true;

        menuItem.InvokeCommand (Command.Activate);

        Assert.True (menuActivatingFired);

        menuBar.Dispose ();
        targetView.Dispose ();
    }

    [Fact]
    public void MenuBar_EnableForDesign_SubMenu_MenuItem_Accept_Raises_Events ()
    {
        View targetView = new () { Id = "target" };
        MenuBar menuBar = new ();
        menuBar.EnableForDesign (ref targetView);

        // Find a MenuItem that has a SubMenu
        MenuBarItem firstMenuBarItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Menu rootMenu = firstMenuBarItem.PopoverMenu!.Root!;

        MenuItem menuItemWithSubMenu = rootMenu.SubViews.OfType<MenuItem> ()
                                                .First (mi => mi.SubMenu is not null);

        Menu subMenu = menuItemWithSubMenu.SubMenu!;

        // Get the first MenuItem in the SubMenu
        MenuItem subMenuItem = subMenu.SubViews.OfType<MenuItem> ().First ();

        bool subMenuAcceptingFired = false;
        subMenu.Accepting += (_, _) => subMenuAcceptingFired = true;

        subMenuItem.InvokeCommand (Command.Accept);

        Assert.True (subMenuAcceptingFired);

        menuBar.Dispose ();
        targetView.Dispose ();
    }

    [Fact]
    public void MenuBar_EnableForDesign_MenuItem_Activating_Fires_On_MenuItem ()
    {
        View targetView = new () { Id = "target" };
        MenuBar menuBar = new ();
        menuBar.EnableForDesign (ref targetView);

        MenuBarItem firstMenuBarItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Menu rootMenu = firstMenuBarItem.PopoverMenu!.Root!;

        MenuItem menuItem = rootMenu.SubViews.OfType<MenuItem> ().First ();

        bool menuItemActivatingFired = false;
        menuItem.Activating += (_, _) => menuItemActivatingFired = true;

        menuItem.InvokeCommand (Command.Activate);

        Assert.True (menuItemActivatingFired);

        menuBar.Dispose ();
        targetView.Dispose ();
    }

    // ──────────────────────────────────────────────────────────────────────
    // PopoverMenu (contains Root Menu → MenuItems)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void PopoverMenu_EnableForDesign_MenuItem_Accept_Raises_Menu_Accepting ()
    {
        View targetView = new () { Id = "target" };
        PopoverMenu popoverMenu = new ();
        popoverMenu.EnableForDesign (ref targetView);

        Menu rootMenu = popoverMenu.Root!;
        MenuItem menuItem = rootMenu.SubViews.OfType<MenuItem> ().First ();

        bool menuAcceptingFired = false;
        rootMenu.Accepting += (_, _) => menuAcceptingFired = true;

        menuItem.InvokeCommand (Command.Accept);

        Assert.True (menuAcceptingFired);

        popoverMenu.Dispose ();
        targetView.Dispose ();
    }

    [Fact]
    public void PopoverMenu_EnableForDesign_MenuItem_Activate_Raises_Menu_Activating ()
    {
        View targetView = new () { Id = "target" };
        PopoverMenu popoverMenu = new ();
        popoverMenu.EnableForDesign (ref targetView);

        Menu rootMenu = popoverMenu.Root!;
        MenuItem menuItem = rootMenu.SubViews.OfType<MenuItem> ().First ();

        bool menuActivatingFired = false;
        rootMenu.Activating += (_, _) => menuActivatingFired = true;

        menuItem.InvokeCommand (Command.Activate);

        Assert.True (menuActivatingFired);

        popoverMenu.Dispose ();
        targetView.Dispose ();
    }
}

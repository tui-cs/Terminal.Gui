// Claude - Opus 4.6

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="MenuBarItem"/> with <see cref="MenuBarItem.UsePopoverMenu"/> = <see langword="false"/>
///     (inline mode), verifying the behavior previously provided by the deleted InlineMenuBarItem class.
/// </summary>
public class MenuBarItemWithoutPopoverTests
{
    [Fact]
    public void UsePopoverMenu_Default_IsTrue ()
    {
        MenuBarItem item = new ();
        Assert.True (item.UsePopoverMenu);

        item.Dispose ();
    }

    [Fact]
    public void UsePopoverMenu_False_AllowsSubMenu ()
    {
        // When UsePopoverMenu = false and menu items are provided, EndInit converts
        // the PopoverMenu to a SubMenu.
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }, new MenuItem { Title = "Save" }]) { UsePopoverMenu = false };
        item.BeginInit ();
        item.EndInit ();

        Assert.NotNull (item.SubMenu);
        Assert.Null (item.PopoverMenu);
        Assert.Equal (2, item.SubMenu!.SubViews.Count (v => v is MenuItem));

        item.Dispose ();
    }

    [Fact]
    public void Constructor_Default_InlineMode_HasNoSubMenu ()
    {
        MenuBarItem item = new () { UsePopoverMenu = false };
        Assert.Null (item.SubMenu);
        Assert.Null (item.TargetView);

        item.Dispose ();
    }

    [Fact]
    public void Constructor_WithText_InlineMode_SetsTitle ()
    {
        MenuBarItem item = new ("_File") { UsePopoverMenu = false };
        Assert.Equal ("_File", item.Title);

        item.Dispose ();
    }

    [Fact]
    public void SubMenuGlyph_InlineMode_IsSuppressed ()
    {
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }]) { UsePopoverMenu = false };
        item.BeginInit ();
        item.EndInit ();

        // SubMenuGlyph returns default when UsePopoverMenu = false,
        // causing KeyView.Text to be empty (glyph slot removed from layout)
        Assert.Equal (string.Empty, item.KeyView.Text);

        item.Dispose ();
    }

    [Fact]
    public void MenuItem_SubMenuGlyph_IsRightArrow ()
    {
        // Verify the base class still uses RightArrow
        MenuItem item = new ("Open", null, new Menu ([new MenuItem { Title = "Sub" }]));
        Assert.Equal ($"{Glyphs.RightArrow}", item.KeyView.Text);

        item.Dispose ();
    }

    [Fact]
    public void IMenuBarEntry_IsMenuOpen_DefaultFalse ()
    {
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }]) { UsePopoverMenu = false };
        item.BeginInit ();
        item.EndInit ();
        IMenuBarEntry entry = item;

        Assert.False (entry.IsMenuOpen);

        item.Dispose ();
    }

    [Fact]
    public void IMenuBarEntry_RootMenu_ReturnsSubMenu ()
    {
        MenuBarItem item = new ("_Edit", [new MenuItem { Title = "Item" }]) { UsePopoverMenu = false };
        item.BeginInit ();
        item.EndInit ();
        IMenuBarEntry entry = item;

        Assert.NotNull (entry.RootMenu);
        Assert.Same (item.SubMenu, entry.RootMenu);

        item.Dispose ();
    }

    [Fact]
    public void IMenuBarEntry_RootMenu_NullWhenNoSubMenu ()
    {
        MenuBarItem item = new () { UsePopoverMenu = false };
        IMenuBarEntry entry = item;

        Assert.Null (entry.RootMenu);

        item.Dispose ();
    }

    [Fact]
    public void IMenuBarEntry_IsMenuOpen_TogglesSubMenuVisibility ()
    {
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }]) { UsePopoverMenu = false };
        item.BeginInit ();
        item.EndInit ();
        IMenuBarEntry entry = item;

        entry.IsMenuOpen = true;
        Assert.True (item.SubMenu!.Visible);

        entry.IsMenuOpen = false;
        Assert.False (item.SubMenu!.Visible);

        item.Dispose ();
    }

    [Fact]
    public void MenuOpenChanged_FiresOnVisibilityChange ()
    {
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }]) { UsePopoverMenu = false };
        item.BeginInit ();
        item.EndInit ();
        item.SubscribeToSubMenuVisibility ();

        var firedCount = 0;
        bool? lastNewValue = null;

        item.MenuOpenChanged += (_, args) =>
                                {
                                    firedCount++;
                                    lastNewValue = args.NewValue;
                                };

        IMenuBarEntry entry = item;
        entry.IsMenuOpen = true;
        Assert.Equal (1, firedCount);
        Assert.True (lastNewValue);

        entry.IsMenuOpen = false;
        Assert.Equal (2, firedCount);
        Assert.False (lastNewValue);

        item.Dispose ();
    }

    [Fact]
    public void Dispose_CleansUpSubMenu ()
    {
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }]) { UsePopoverMenu = false };
        item.BeginInit ();
        item.EndInit ();
        item.SubscribeToSubMenuVisibility ();
        item.Dispose ();

        // After dispose, SubMenu should be null (cleaned up by base MenuItem.Dispose)
        Assert.Null (item.SubMenu);
    }

    [Fact]
    public void MenuBarItem_Implements_IMenuBarEntry ()
    {
        MenuBarItem mbi = new ();
        Assert.IsAssignableFrom<IMenuBarEntry> (mbi);

        mbi.Dispose ();
    }

    [Fact]
    public void MenuBarItem_IMenuBarEntry_DelegatesToPopoverMenu ()
    {
        MenuBarItem mbi = new ();
        mbi.EnableForDesign ();
        IMenuBarEntry entry = mbi;

        Assert.False (entry.IsMenuOpen);
        Assert.NotNull (entry.RootMenu);

        mbi.Dispose ();
    }

    [Fact]
    public void MenuBar_Accepts_InlineMenuBarItem ()
    {
        MenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "A" }]) { UsePopoverMenu = false };
        MenuBarItem popoverItem = new ("_Popover", [new MenuItem { Title = "B" }]);

        MenuBar menuBar = new ([popoverItem, inlineItem]);
        Assert.Equal (2, menuBar.SubViews.Count (v => v is IMenuBarEntry));

        menuBar.Dispose ();
    }

    [Fact]
    public void MenuBar_IsOpen_IncludesInlineMenuBarItem ()
    {
        MenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "A" }]) { UsePopoverMenu = false };

        MenuBar menuBar = new ([inlineItem]);
        menuBar.BeginInit ();
        menuBar.EndInit ();

        Assert.False (menuBar.IsOpen ());

        IMenuBarEntry entry = inlineItem;
        entry.IsMenuOpen = true;
        Assert.True (menuBar.IsOpen ());

        entry.IsMenuOpen = false;
        Assert.False (menuBar.IsOpen ());

        menuBar.Dispose ();
    }

    [Fact]
    public void MenuBar_GetMenuItemsWith_IncludesInlineItems ()
    {
        MenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "Target" }]) { UsePopoverMenu = false };

        MenuBar menuBar = new ([inlineItem]);
        menuBar.BeginInit ();
        menuBar.EndInit ();

        IEnumerable<MenuItem> found = menuBar.GetMenuItemsWith (mi => mi.Title == "Target");
        Assert.Single (found);

        menuBar.Dispose ();
    }

    [Fact]
    public void MenuBar_GetMenuItemsWith_MixedMode ()
    {
        MenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "InlineChild" }]) { UsePopoverMenu = false };
        MenuBarItem popoverItem = new ("_Popover", [new MenuItem { Title = "PopoverChild" }]);

        MenuBar menuBar = new ([popoverItem, inlineItem]);
        menuBar.BeginInit ();
        menuBar.EndInit ();

        IEnumerable<MenuItem> all = menuBar.GetMenuItemsWith (_ => true);
        Assert.Equal (2, all.Count ());

        menuBar.Dispose ();
    }

    [Fact]
    public void OnActivating_Toggles_IsMenuOpen ()
    {
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }]) { UsePopoverMenu = false };
        item.BeginInit ();
        item.EndInit ();
        IMenuBarEntry entry = item;

        // First activation should open
        item.InvokeCommand (Command.Activate);
        Assert.True (entry.IsMenuOpen);

        // Second activation should close
        item.InvokeCommand (Command.Activate);
        Assert.False (entry.IsMenuOpen);

        item.Dispose ();
    }

    [Fact]
    public void HideItem_Works_WithIMenuBarEntry ()
    {
        MenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "A" }]) { UsePopoverMenu = false };

        MenuBar menuBar = new ([inlineItem]);
        menuBar.BeginInit ();
        menuBar.EndInit ();
        menuBar.Active = true;

        IMenuBarEntry entry = inlineItem;
        entry.IsMenuOpen = true;
        Assert.True (menuBar.IsOpen ());

        menuBar.HideItem (entry);
        Assert.False (entry.IsMenuOpen);

        menuBar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void ClickToggle_OpensAndClosesInlineMenu ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuBarItem inlineItem = new ("_Tools",
                                      [new MenuItem { Title = "_Compile", HelpText = "Build project" }, new MenuItem { Title = "_Run", HelpText = "Execute" }])
        {
            UsePopoverMenu = false
        };

        MenuBar menuBar = new ([inlineItem]) { Id = "menuBar" };
        ((View)runnable).Add (menuBar);
        app.Begin (runnable);

        IMenuBarEntry entry = inlineItem;

        // --- First activation: open the menu ---
        app.InjectKey (Key.F10);

        Assert.True (menuBar.Active, "MenuBar should be active after F10.");
        Assert.True (entry.IsMenuOpen, "InlineItem should be open after F10.");

        // --- Second activation: close the menu ---
        app.InjectKey (Key.F10);

        Assert.False (entry.IsMenuOpen, "InlineItem should be closed after second F10.");
        Assert.False (menuBar.Active, "MenuBar should be inactive after second F10.");
    }

    // Claude - Opus 4.6
    [Fact]
    public void ActivateToggle_InMenuBar ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuBarItem inlineItem = new ("_Tools", [new MenuItem { Title = "_Compile" }]) { UsePopoverMenu = false };

        MenuBar menuBar = new ([inlineItem]) { Id = "menuBar" };
        ((View)runnable).Add (menuBar);
        app.Begin (runnable);

        IMenuBarEntry entry = inlineItem;

        // --- Open via direct Activate command ---
        inlineItem.InvokeCommand (Command.Activate);

        Assert.True (entry.IsMenuOpen, "InlineItem should be open after Activate.");

        // --- Close via direct Activate command ---
        inlineItem.InvokeCommand (Command.Activate);

        Assert.False (entry.IsMenuOpen, "InlineItem should be closed after second Activate.");
    }

    // Claude - Opus 4.6
    [Fact]
    public void ArrowRight_PopoverToInline ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuBarItem popoverItem = new ("_File", [new MenuItem { Title = "_New" }, new MenuItem { Title = "_Open" }]);

        MenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "_Alpha" }, new MenuItem { Title = "_Beta" }]) { UsePopoverMenu = false };

        MenuBar menuBar = new ([popoverItem, inlineItem]) { Id = "menuBar" };
        ((View)runnable).Add (menuBar);
        app.Begin (runnable);

        IMenuBarEntry popoverEntry = popoverItem;
        IMenuBarEntry inlineEntry = inlineItem;

        // --- Open popover via F10 ---
        app.InjectKey (Key.F10);

        Assert.True (menuBar.Active, "MenuBar should be active after F10.");
        Assert.True (popoverEntry.IsMenuOpen, "Popover should be open after F10.");

        // --- Arrow right to switch to inline entry ---
        app.InjectKey (Key.CursorRight);

        Assert.True (inlineEntry.IsMenuOpen, "InlineItem should be open after Right.");
        Assert.False (popoverEntry.IsMenuOpen, "Popover should be closed after Right.");
    }

    // Claude - Opus 4.6
    [Fact]
    public void EnterOnMenuItem_InInlineSubMenu ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var actionFiredCount = 0;

        MenuBarItem inlineItem = new ("_Actions", [new MenuItem { Title = "_Do Something", Action = () => actionFiredCount++ }]) { UsePopoverMenu = false };

        MenuBar menuBar = new ([inlineItem]) { Id = "menuBar" };
        ((View)runnable).Add (menuBar);
        app.Begin (runnable);

        IMenuBarEntry entry = inlineItem;

        // --- Open via F10 ---
        app.InjectKey (Key.F10);

        Assert.True (entry.IsMenuOpen, "InlineItem should be open after F10.");

        // --- Press Enter on the focused MenuItem ---
        app.InjectKey (Key.Enter);

        Assert.Equal (1, actionFiredCount);
    }

    // ─── Cascading SubMenu Tests ───

    // Claude - Opus 4.6
    [Fact]
    public void Diag_EndInit_Conversion_Steps ()
    {
        // Trace exactly when cascading SubMenu items disappear by
        // manually simulating the EndInit conversion steps.
        Menu cascadeMenu = new ([new MenuItem { Title = "_Auto Save" }, new MenuItem { Title = "_Overwrite" }]);

        MenuItem optionsItem = new () { Title = "_Options", SubMenu = cascadeMenu };

        Assert.Equal (2, cascadeMenu.SubViews.Count (v => v is MenuItem));

        MenuBarItem item = new ("_File", [new MenuItem { Title = "_New" }, optionsItem]) { UsePopoverMenu = false };

        Assert.Equal (2, cascadeMenu.SubViews.Count (v => v is MenuItem));

        // Manually replicate what EndInit does:
        Menu? root = item.PopoverMenu!.Root;
        Assert.NotNull (root);

        // Step 1: Extract items
        List<View> menuItems = [.. root!.SubViews];
        Assert.Equal (2, menuItems.Count);
        Assert.Equal (2, cascadeMenu.SubViews.Count (v => v is MenuItem));

        // Step 2: RemoveAll from Root
        root.RemoveAll ();
        int afterRemoveAll = cascadeMenu.SubViews.Count (v => v is MenuItem);
        Assert.True (afterRemoveAll == 2, $"After RemoveAll: cascadeMenu has {afterRemoveAll} items (expected 2)");

        // Step 3: RemoveAll from PopoverMenu to detach cascading SubMenus, then dispose
        item.PopoverMenu!.RemoveAll ();
        item.PopoverMenu!.Dispose ();
        item.PopoverMenu = null;
        int afterDispose = cascadeMenu.SubViews.Count (v => v is MenuItem);
        Assert.True (afterDispose == 2, $"After PopoverMenu.RemoveAll+Dispose: cascadeMenu has {afterDispose} items (expected 2)");

        // Step 4: Create new inline Menu
        Menu inlineMenu = new (menuItems);
        int afterNewMenu = cascadeMenu.SubViews.Count (v => v is MenuItem);
        Assert.True (afterNewMenu == 2, $"After new Menu(menuItems): cascadeMenu has {afterNewMenu} items (expected 2)");

        // Step 5: BeginInit + EndInit on inline Menu
        inlineMenu.BeginInit ();
        int afterMenuBeginInit = cascadeMenu.SubViews.Count (v => v is MenuItem);
        Assert.True (afterMenuBeginInit == 2, $"After inlineMenu.BeginInit: cascadeMenu has {afterMenuBeginInit} items (expected 2)");

        inlineMenu.EndInit ();
        int afterMenuEndInit = cascadeMenu.SubViews.Count (v => v is MenuItem);
        Assert.True (afterMenuEndInit == 2, $"After inlineMenu.EndInit: cascadeMenu has {afterMenuEndInit} items (expected 2)");

        inlineMenu.Dispose ();
        item.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void CascadingSubMenu_EndInit_PreservesNestedSubMenus ()
    {
        // Verify that cascading SubMenus inside MenuItems survive the
        // PopoverMenu → inline SubMenu conversion in EndInit.
        MenuItem optionsItem = new ()
        {
            Title = "_Options", SubMenu = new Menu ([new MenuItem { Title = "_Auto Save" }, new MenuItem { Title = "_Overwrite" }])
        };

        MenuBarItem item = new ("_File", [new MenuItem { Title = "_New" }, optionsItem]) { UsePopoverMenu = false };
        item.BeginInit ();
        item.EndInit ();

        // The top-level SubMenu should exist with 2 items
        Assert.NotNull (item.SubMenu);
        Assert.Equal (2, item.SubMenu!.SubViews.Count (v => v is MenuItem));

        // The "Options" MenuItem should retain its cascading SubMenu
        Assert.NotNull (optionsItem.SubMenu);
        Assert.Equal (2, optionsItem.SubMenu!.SubViews.Count (v => v is MenuItem));

        item.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void CascadingSubMenu_GetMenuItemsWith_FindsNestedItems ()
    {
        MenuBarItem inlineItem = new ("_File",
                                      [
                                          new MenuItem { Title = "_New" },
                                          new MenuItem { Title = "_Options", SubMenu = new Menu ([new MenuItem { Title = "_Deep Item" }]) }
                                      ]) { UsePopoverMenu = false };

        MenuBar menuBar = new ([inlineItem]);
        menuBar.BeginInit ();
        menuBar.EndInit ();

        // GetMenuItemsWith should find items across all levels
        IEnumerable<MenuItem> found = menuBar.GetMenuItemsWith (mi => mi.Title == "_Deep Item");
        Assert.Single (found);

        menuBar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void CascadingSubMenu_ThreeLevels_Preserved ()
    {
        // Three levels: File > Preferences > Advanced > Debug Mode
        MenuBarItem item = new ("_File",
                                [
                                    new MenuItem
                                    {
                                        Title = "_Preferences",
                                        SubMenu = new Menu ([
                                                                new MenuItem
                                                                {
                                                                    Title = "_Advanced", SubMenu = new Menu ([new MenuItem { Title = "_Debug Mode" }])
                                                                }
                                                            ])
                                    }
                                ]) { UsePopoverMenu = false };
        item.BeginInit ();
        item.EndInit ();

        // Traverse: SubMenu > Preferences > SubMenu > Advanced > SubMenu > Debug Mode
        MenuItem? prefs = item.SubMenu?.SubViews.OfType<MenuItem> ().FirstOrDefault ();
        Assert.NotNull (prefs);
        Assert.Equal ("_Preferences", prefs!.Title);

        MenuItem? advanced = prefs.SubMenu?.SubViews.OfType<MenuItem> ().FirstOrDefault ();
        Assert.NotNull (advanced);
        Assert.Equal ("_Advanced", advanced!.Title);

        MenuItem? debug = advanced.SubMenu?.SubViews.OfType<MenuItem> ().FirstOrDefault ();
        Assert.NotNull (debug);
        Assert.Equal ("_Debug Mode", debug!.Title);

        item.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void CascadingSubMenu_OpenAndNavigate ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuBarItem inlineItem = new ("_File",
                                      [
                                          new MenuItem { Title = "_New" },
                                          new MenuItem
                                          {
                                              Title = "_Options",
                                              SubMenu = new Menu ([new MenuItem { Title = "_Setting A" }, new MenuItem { Title = "_Setting B" }])
                                          }
                                      ]) { UsePopoverMenu = false };

        MenuBar menuBar = new ([inlineItem]) { Id = "menuBar" };
        ((View)runnable).Add (menuBar);
        app.Begin (runnable);

        IMenuBarEntry entry = inlineItem;

        // Open the inline menu
        app.InjectKey (Key.F10);
        Assert.True (entry.IsMenuOpen, "InlineItem should be open after F10.");

        // Navigate down to "Options" (second item)
        app.InjectKey (Key.CursorDown);

        // Press Right to open the cascading SubMenu
        app.InjectKey (Key.CursorRight);

        // The cascading SubMenu for "Options" should now be visible
        MenuItem? optionsItem = inlineItem.SubMenu?.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Title == "_Options");
        Assert.NotNull (optionsItem?.SubMenu);
        Assert.True (optionsItem!.SubMenu!.Visible, "Cascading SubMenu should be visible.");

        // Verify the cascading SubMenu has the expected items
        List<MenuItem> cascadeItems = optionsItem.SubMenu!.SubViews.OfType<MenuItem> ().ToList ();
        Assert.Equal (2, cascadeItems.Count);
        Assert.Equal ("_Setting A", cascadeItems [0].Title);
        Assert.Equal ("_Setting B", cascadeItems [1].Title);
    }

    // Claude - Opus 4.6
    [Fact]
    public void CascadingSubMenu_EscapeClosesSubMenu ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuBarItem inlineItem =
            new ("_File", [new MenuItem { Title = "_Options", SubMenu = new Menu ([new MenuItem { Title = "_Setting A" }]) }]) { UsePopoverMenu = false };

        MenuBar menuBar = new ([inlineItem]) { Id = "menuBar" };
        ((View)runnable).Add (menuBar);
        app.Begin (runnable);

        IMenuBarEntry entry = inlineItem;

        // Open inline menu
        app.InjectKey (Key.F10);
        Assert.True (entry.IsMenuOpen);

        // Open cascading SubMenu via Right arrow
        app.InjectKey (Key.CursorRight);

        MenuItem? optionsItem = inlineItem.SubMenu?.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Title == "_Options");
        Assert.True (optionsItem?.SubMenu?.Visible, "Cascading SubMenu should be visible.");

        // Escape should close the cascading SubMenu but keep the parent open
        app.InjectKey (Key.Esc);
        Assert.False (optionsItem?.SubMenu?.Visible, "Cascading SubMenu should be hidden after Escape.");
    }

    // Claude - Opus 4.6
    [Fact]
    public void CascadingSubMenu_MixedMode_WithCascading ()
    {
        // Mixed mode: one popover with cascading, one inline with cascading
        MenuBarItem popoverItem = new ("_File",
                                       [
                                           new MenuItem { Title = "_New" },
                                           new MenuItem { Title = "_Extras", SubMenu = new Menu ([new MenuItem { Title = "_Extra1" }]) }
                                       ]);

        MenuBarItem inlineItem = new ("_Tools",
                                      [
                                          new MenuItem
                                          {
                                              Title = "_Diagnostics",
                                              SubMenu = new Menu ([new MenuItem { Title = "_Run All" }, new MenuItem { Title = "_Network" }])
                                          }
                                      ]) { UsePopoverMenu = false };

        MenuBar menuBar = new ([popoverItem, inlineItem]);
        menuBar.BeginInit ();
        menuBar.EndInit ();

        // Both items should be found
        IEnumerable<MenuItem> allItems = menuBar.GetMenuItemsWith (_ => true);

        // Popover: New, Extras, Extra1 = 3; Inline: Diagnostics, Run All, Network = 3
        Assert.Equal (6, allItems.Count ());

        menuBar.Dispose ();
    }
}

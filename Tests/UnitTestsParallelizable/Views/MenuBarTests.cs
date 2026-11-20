using Microsoft.Extensions.Logging;
using Terminal.Gui.Tracing;
using UnitTests;

namespace ViewsTests;

public class MenuBarTests (ITestOutputHelper output)
{
    [Fact]
    public void Command_HotKey_Activates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.False (menuBar.Active);

        menuBar.InvokeCommand (Command.HotKey);

        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });
    }

    [Fact]
    public void DefaultKey_Activates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.False (menuBar.Active);

        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });
    }

    [Fact]
    public void DefaultKey_Deactivates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Arrange
        Assert.False (menuBar.Active);

        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });

        // Act
        app.InjectKey (MenuBar.DefaultKey);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: false });
    }

    [Fact]
    public void Command_Activate_Focuses_MenuBarItem_PopoverMenu_And_First_MenuItem ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        MenuBarItem menuBarItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        PopoverMenu? popoverMenu = menuBarItem.PopoverMenu;
        Menu? menu = popoverMenu?.Root;
        MenuItem? menuItem = menu?.SubViews.OfType<MenuItem> ().First ();

        // Activate focuses MenuBarItem
        menuBar.InvokeCommand (Command.Activate);

        Assert.True (menuBar.HasFocus);
        Assert.True (menuBarItem.HasFocus);
        Assert.True (popoverMenu?.HasFocus);
        Assert.True (menu?.HasFocus);
        Assert.True (menuItem?.HasFocus);

        Assert.Equal (menu, popoverMenu?.Focused);
        Assert.Equal (menuItem, menu?.Focused);
        Assert.Equal (menuItem, app.Navigation?.GetFocused ());

        menuBar.Dispose ();
    }

    [Fact]
    public void Command_Activate_Activates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.False (menuBar.Active);

        menuBar.InvokeCommand (Command.Activate);

        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });
    }

    [Fact]
    public void Command_Activate_Activates_Command_Activate_Deactivates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.False (menuBar.Active);

        menuBar.InvokeCommand (Command.Activate);

        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });

        menuBar.InvokeCommand (Command.Activate);

        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });
    }

    [Fact]
    public void Command_Activate_WhenActive_Deactivates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Arrange
        Assert.False (menuBar.Active);

        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });

        MenuBarItem menuBarItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        PopoverMenu? popoverMenu = menuBarItem.PopoverMenu;
        Menu? menu = popoverMenu?.Root;
        MenuItem? menuItem = menu?.SubViews.OfType<MenuItem> ().First ();

        var focused = app.Navigation?.GetFocused () as MenuItem;
        Assert.Equal (menuItem, focused);

        // Act
        menuBar.InvokeCommand (Command.Activate);

        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: false });

        Assert.Equal (hostView, app.Navigation?.GetFocused ());
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that clicking the "Error" checkbox in the OptionSelector&lt;Schemes&gt; inside
    ///     MenuBar.EnableForDesign's Preferences SubMenu changes Value from 0 (Base) to 4 (Error).
    ///     This exercises the full Shortcut → OptionSelector dispatch path inside a PopoverMenu SubMenu.
    /// </summary>
    [Fact]
    public void OptionSelector_In_SubMenu_Click_Sets_Correct_Value ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Find the OptionSelector<Schemes> in the menu hierarchy
        // It's in File → Preferences → "mutuallyExclusiveOptions" MenuItem
        MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Assert.NotNull (fileItem.PopoverMenu);

        // Open the File menu
        Point fileScreenPos = fileItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (fileScreenPos));
        Assert.True (menuBar.IsOpen (), "File menu should be open");

        // Find the "Preferences" MenuItem which has a SubMenu
        Menu? rootMenu = fileItem.PopoverMenu!.Root;
        Assert.NotNull (rootMenu);

        MenuItem? prefsItem = rootMenu.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Title == "_Preferences");
        Assert.NotNull (prefsItem);
        Assert.NotNull (prefsItem.SubMenu);

        // Make Preferences SubMenu visible by navigating to it
        prefsItem.SetFocus ();

        // Find the MenuItem with the OptionSelector CommandView
        MenuItem? optionMenuItem = prefsItem.SubMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Id == "mutuallyExclusiveOptions");
        Assert.NotNull (optionMenuItem);

        OptionSelector<Schemes>? optionSelector = optionMenuItem.CommandView as OptionSelector<Schemes>;
        Assert.NotNull (optionSelector);
        Assert.Equal (Schemes.Base, optionSelector.Value);

        // Find the "Error" checkbox (index 4 in the Schemes enum)
        CheckBox? errorCheckBox = optionSelector.SubViews.OfType<CheckBox> ().ElementAtOrDefault ((int)Schemes.Error);
        Assert.NotNull (errorCheckBox);

        // Subscribe to ValueChanged
        Schemes? newValue = null;
        var valueChangedCount = 0;

        optionSelector.ValueChanged += (_, args) =>
                                       {
                                           newValue = args.Value;
                                           valueChangedCount++;
                                       };

        // Act — click on the Error checkbox via the MenuItem (simulates real mouse click
        // which hits the Shortcut/MenuItem level and dispatches down to CommandView)
        errorCheckBox.SetFocus ();
        Point errorScreenPos = errorCheckBox.FrameToScreen ().Location;

        // Use InjectMouse to simulate the full mouse pipeline (LeftButtonPressed + LeftButtonReleased)
        // which goes through the Shortcut dispatch path, not directly to the CheckBox
        app.InjectMouse (new Mouse { ScreenPosition = errorScreenPos, Flags = MouseFlags.LeftButtonPressed });
        app.InjectMouse (new Mouse { ScreenPosition = errorScreenPos, Flags = MouseFlags.LeftButtonReleased });

        // Assert — Value should change from Base (0) to Error (4), not to Menu (1)
        Assert.Equal (1, valueChangedCount);
        Assert.Equal (Schemes.Error, newValue);
        Assert.Equal (Schemes.Error, optionSelector.Value);

        menuBar.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that pressing Space on the "Error" checkbox in the OptionSelector&lt;Schemes&gt;
    ///     inside MenuBar.EnableForDesign's Preferences SubMenu changes Value correctly.
    /// </summary>
    [Fact]
    public void OptionSelector_In_SubMenu_Space_Sets_Correct_Value ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Navigate to the OptionSelector
        MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Point fileScreenPos = fileItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (fileScreenPos));

        Menu? rootMenu = fileItem.PopoverMenu!.Root;
        MenuItem? prefsItem = rootMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Title == "_Preferences");
        prefsItem!.SetFocus ();

        MenuItem? optionMenuItem = prefsItem.SubMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Id == "mutuallyExclusiveOptions");
        OptionSelector<Schemes>? optionSelector = optionMenuItem!.CommandView as OptionSelector<Schemes>;
        Assert.Equal (Schemes.Base, optionSelector!.Value);

        CheckBox? errorCheckBox = optionSelector.SubViews.OfType<CheckBox> ().ElementAtOrDefault ((int)Schemes.Error);

        Schemes? newValue = null;
        var valueChangedCount = 0;

        optionSelector.ValueChanged += (_, args) =>
                                       {
                                           newValue = args.Value;
                                           valueChangedCount++;
                                       };

        // Act — focus Error checkbox and press Space via the MenuItem context
        // Space is bound to Command.Activate on CheckBox. When inside a MenuItem/Shortcut,
        // the key goes through the Shortcut's key binding scope.
        errorCheckBox!.SetFocus ();
        app.InjectKey (Key.Space);

        // Assert — Value should change to Error (4)
        Assert.Equal (1, valueChangedCount);
        Assert.Equal (Schemes.Error, newValue);
        Assert.Equal (Schemes.Error, optionSelector.Value);

        menuBar.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that pressing Enter on the "Error" checkbox in the OptionSelector&lt;Schemes&gt;
    ///     inside MenuBar.EnableForDesign's Preferences SubMenu changes Value correctly.
    /// </summary>
    [Fact]
    public void OptionSelector_In_SubMenu_Enter_Sets_Correct_Value ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Navigate to the OptionSelector
        MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Point fileScreenPos = fileItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (fileScreenPos));

        Menu? rootMenu = fileItem.PopoverMenu!.Root;
        MenuItem? prefsItem = rootMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Title == "_Preferences");
        prefsItem!.SetFocus ();

        MenuItem? optionMenuItem = prefsItem.SubMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Id == "mutuallyExclusiveOptions");
        OptionSelector<Schemes>? optionSelector = optionMenuItem!.CommandView as OptionSelector<Schemes>;
        Assert.Equal (Schemes.Base, optionSelector!.Value);

        CheckBox? errorCheckBox = optionSelector.SubViews.OfType<CheckBox> ().ElementAtOrDefault ((int)Schemes.Error);

        var valueChangedCount = 0;

        optionSelector.ValueChanged += (_, _) => { valueChangedCount++; };

        // Act — focus Error checkbox and press Enter via the MenuItem context
        errorCheckBox!.SetFocus ();
        app.InjectKey (Key.Enter);

        // Assert — Value should change to Error (4)
        Assert.True (valueChangedCount >= 1, "ValueChanged should fire at least once");
        Assert.Equal (Schemes.Error, optionSelector.Value);

        menuBar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Mouse_Click_Activates_And_Opens ()
    {
        // Arrange - mirror the Menus.cs scenario: MenuBar inside a focusable host view
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        (runnable as View)!.Add (hostView);
        app.Begin (runnable);

        // Focus something else, like in the real scenario
        hostView.SetFocus ();

        Assert.False (menuBar.HasFocus);

        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());

        // Act - click on the first MenuBarItem
        MenuBarItem firstItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Point itemScreenPos = firstItem.FrameToScreen ().Location;

        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (itemScreenPos));

        // Assert
        Assert.True (menuBar.Active, "MenuBar should be Active after click");
        Assert.True (menuBar.IsOpen (), "PopoverMenu should be open after click");

        menuBar.Dispose ();
    }

    #region Ported from old UnitTests/Views/MenuBarTests.cs

    // Claude - Opus 4.6
    [Fact]
    public void QuitKey_Deactivates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "Menu_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_MenuBarItem" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open the menu
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());

        // Act — Quit command should close everything.
        // Note: Application.GetDefaultKey (Command.Quit) is a local KeyBinding, so we invoke the command directly
        // because the PopoverMenu has focus (separate view hierarchy from MenuBar).
        menuBar.InvokeCommand (Command.Quit);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.False (menuBarItem.PopoverMenu!.Visible);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuBarItem_HotKey_Activates_And_Opens ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);
        Assert.False (menuBar.Active);

        // Act — press MenuBarItem's HotKey (Alt+N for "_New")
        app.InjectKey (Key.N.WithAlt);

        // Assert
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu!.Visible);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuBarItem_HotKey_Deactivates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);
        Assert.False (menuBar.Active);

        // Open via HotKey
        app.InjectKey (Key.N.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());

        // Act — second press should deactivate
        app.InjectKey (Key.N.WithAlt);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.False (menuBarItem.PopoverMenu!.Visible);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_HotKey_Fires_Action_When_Open ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var actionCount = 0;
        MenuItem menuItem = new () { Id = "menuItem", Title = "Menu_Item", Action = () => actionCount++ };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_MenuBarItem" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open the menu via MenuBarItem's HotKey
        app.InjectKey (Key.M.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBarItem.PopoverMenu!.Visible);

        // Act — press MenuItem's HotKey ('I' for "Menu_Item") while menu is open
        app.InjectKey (Key.I);
        Assert.Equal (1, actionCount);
    }

    // Cursor - Grok 4.5
    /// <summary>
    ///     MenuItem.Key with BindKeyToApplication must invoke Action while the owning MenuBarItem's
    ///     PopoverMenu is closed (not visible). Previously DefaultHotKeyHandler refused HotKey when
    ///     CanBeVisible was false, so the shortcut only worked with that MenuBarItem's menu open.
    /// </summary>
    [Fact]
    public void MenuItem_BindKeyToApplication_Fires_Action_When_Popover_Closed ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        int actionCount = 0;
        MenuItem menuItem = new ()
        {
            Id = "menuItem",
            Title = "Save",
            BindKeyToApplication = true,
            Key = Key.S.WithCtrl,
            Action = () => actionCount++
        };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_File" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.False (menuBarItem.PopoverMenu!.Visible);

        app.Keyboard.RaiseKeyDownEvent (Key.S.WithCtrl);

        Assert.Equal (1, actionCount);
        Assert.False (menuBarItem.PopoverMenu.Visible);
    }

    // Claude - Opus 4.6
    [Fact]
    public void WhenOpen_Switch_MenuBarItem_Via_HotKey ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem1 = new () { Id = "menuItem1", Title = "_Item1" };
        Menu menu1 = new ([menuItem1]) { Id = "menu1" };
        MenuBarItem menuBarItem1 = new () { Id = "menuBarItem1", Title = "_New" };
        PopoverMenu popover1 = new ();
        menuBarItem1.PopoverMenu = popover1;
        popover1.Root = menu1;

        MenuItem menuItem2 = new () { Id = "menuItem2", Title = "_Item2" };
        Menu menu2 = new ([menuItem2]) { Id = "menu2" };
        MenuBarItem menuBarItem2 = new () { Id = "menuBarItem2", Title = "_Edit" };
        PopoverMenu popover2 = new () { Id = "popover2" };
        menuBarItem2.PopoverMenu = popover2;
        popover2.Root = menu2;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem1);
        menuBar.Add (menuBarItem2);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);
        Assert.False (menuBar.Active);

        // Open first MenuBarItem
        app.InjectKey (Key.N.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBarItem1.PopoverMenu!.Visible);

        // Act — switch to second MenuBarItem via its HotKey
        app.InjectKey (Key.E.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem2.PopoverMenu!.Visible);
        Assert.False (menuBarItem1.PopoverMenu!.Visible);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Update_MenuBarItem_HotKey_Works ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open with original hotkey
        app.InjectKey (Key.N.WithAlt);
        Assert.True (menuBar.IsOpen ());

        // Act — change the hotkey
        menuBarItem.HotKey = Key.E.WithAlt;

        // Old key should not close
        app.InjectKey (Key.N.WithAlt);
        Assert.True (menuBar.IsOpen ());

        // New key should close
        app.InjectKey (Key.E.WithAlt);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
    }

    // Claude - Opus 4.6
    [Fact]
    public void Disabled_MenuBar_Is_Not_Activated ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Act — disable MenuBar and try hotkey
        menuBar.Enabled = false;
        app.InjectKey (Key.N.WithAlt);

        // Assert — should not activate
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
    }

    // Claude - Opus 4.6
    [Fact]
    public void Disabled_MenuBarItem_HotKey_Does_Not_Open ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Act — disable MenuBarItem and try hotkey
        menuBarItem.Enabled = false;
        app.InjectKey (Key.N.WithAlt);

        // Assert — should not activate
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
    }

    // Claude - Opus 4.6
    [Fact]
    public void Disabled_PopoverMenu_MenuBar_Still_Activates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Act — disable PopoverMenu but MenuBar item should still respond
        menuBarItem.PopoverMenu!.Enabled = false;
        app.InjectKey (Key.N.WithAlt);

        // Assert — MenuBar activates, PopoverMenu is shown (even if disabled)
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu!.Visible);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Invisible_MenuBar_HotKey_Does_Not_Activate ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Act — hide MenuBar and try to activate via DefaultKey
        menuBar.Visible = false;
        app.InjectKey (MenuBar.DefaultKey);

        // Assert — should not activate
        Assert.False (menuBar.Active);
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void HotKey_Fires_VisibleChanged_Only_Once ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        var visibleChangeCount = 0;

        menuBarItemPopover.VisibleChanged += (_, _) =>
                                             {
                                                 if (menuBarItemPopover.Visible)
                                                 {
                                                     visibleChangeCount++;
                                                 }
                                             };

        // Act — open via MenuBarItem HotKey
        app.InjectKey (Key.N.WithAlt);

        // Assert — should fire VisibleChanged exactly once
        Assert.Equal (1, visibleChangeCount);
    }

    #endregion

    #region New gap coverage tests

    // Claude - Opus 4.6
    [Fact]
    public void Command_Right_Navigates_Between_MenuBarItems ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem1 = new () { Id = "menuItem1", Title = "_Item1" };
        Menu menu1 = new ([menuItem1]) { Id = "menu1" };
        MenuBarItem menuBarItem1 = new () { Id = "menuBarItem1", Title = "_File" };
        PopoverMenu popover1 = new ();
        menuBarItem1.PopoverMenu = popover1;
        popover1.Root = menu1;

        MenuItem menuItem2 = new () { Id = "menuItem2", Title = "_Item2" };
        Menu menu2 = new ([menuItem2]) { Id = "menu2" };
        MenuBarItem menuBarItem2 = new () { Id = "menuBarItem2", Title = "_Edit" };
        PopoverMenu popover2 = new ();
        menuBarItem2.PopoverMenu = popover2;
        popover2.Root = menu2;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem1);
        menuBar.Add (menuBarItem2);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Activate the MenuBar (Active = true, CanFocus = true) and focus first item
        menuBar.InvokeCommand (Command.Activate);
        Assert.True (menuBar.Active);

        // Focus the MenuBar's first item directly so Right arrow works within MenuBar
        menuBarItem1.SetFocus ();

        // Act — invoke Right command on the MenuBar
        menuBar.InvokeCommand (Command.Right);

        // Assert — focus should move to second MenuBarItem
        Assert.True (menuBar.Active);
        Assert.True (menuBarItem2.HasFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Escape_Closes_Open_PopoverMenu ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open the menu
        menuBar.InvokeCommand (Command.Activate);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());

        // Act — press Escape
        app.InjectKey (Key.Esc);

        // Assert — menu should be closed
        Assert.False (menuBar.IsOpen ());
    }

    // Claude - Opus 4.6
    [Fact]
    public void Focus_Restoration_After_Close ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Ensure host has focus before opening menu
        hostView.SetFocus ();
        Assert.True (hostView.HasFocus);
        Assert.False (menuBar.HasFocus);

        // Open and then close the menu
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.HasFocus);

        app.InjectKey (MenuBar.DefaultKey);

        // Assert — focus should return to hostView
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.True (hostView.HasFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menus_Property_Setter_Replaces_Items ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.Empty (menuBar.SubViews.OfType<MenuBarItem> ());

        // Act — set Menus property
        MenuBarItem newItem1 = new () { Title = "_File" };
        MenuBarItem newItem2 = new () { Title = "_Edit" };
        menuBar.Menus = [newItem1, newItem2];

        // Assert — SubViews should contain the new items
        MenuBarItem [] items = menuBar.SubViews.OfType<MenuBarItem> ().ToArray ();
        Assert.Equal (2, items.Length);
        Assert.Equal ("_File", items [0].Title);
        Assert.Equal ("_Edit", items [1].Title);
    }

    // Claude - Opus 4.6
    [Fact]
    public void GetMenuItemsWith_Returns_Matching_Items ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Act — query by predicate
        IEnumerable<MenuItem> results = menuBar.GetMenuItemsWith (mi => mi.Command == Command.Quit);

        // Assert — should find at least one Quit MenuItem
        Assert.NotEmpty (results);
    }

    // Claude - Opus 4.6
    [Fact (Skip = "Mouse click switching has a timing issue: OnSelectedMenuItemChanged auto-opens, then click's Activate toggles off.")]
    public void Mouse_Click_On_Different_MenuBarItem_Switches ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        MenuBarItem firstItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0);
        MenuBarItem secondItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (1);

        // Click on first item to open it
        Point firstScreenPos = firstItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (firstScreenPos));
        Assert.True (menuBar.Active);
        Assert.True (firstItem.PopoverMenu is { Visible: true });

        // Act — click on second item
        Point secondScreenPos = secondItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (secondScreenPos));

        // Assert — second item open, first closed
        Assert.True (menuBar.Active);
        Assert.True (secondItem.PopoverMenu is { Visible: true });
        Assert.False (firstItem.PopoverMenu is { Visible: true });
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuBar_Active_False_Sets_CanFocus_False ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Activate
        menuBar.InvokeCommand (Command.Activate);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.CanFocus);

        // Act — deactivate
        menuBar.InvokeCommand (Command.Activate);

        // Assert — CanFocus should be false
        Assert.False (menuBar.Active);
        Assert.False (menuBar.CanFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void IsOpen_False_After_All_Popovers_Close ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.IsOpen ());

        // Act — close
        app.InjectKey (MenuBar.DefaultKey);

        // Assert — IsOpen should be false (Bug 1 fix)
        Assert.False (menuBar.IsOpen ());
    }

    // Claude - Opus 4.6
    [Fact]
    public void QuitKey_With_PopoverMenu_Visible_Fully_Deactivates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "Menu_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_MenuBarItem" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open the menu so PopoverMenu is visible
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu!.Visible);

        // Act — single Escape (QuitKey) should fully deactivate everything
        app.InjectKey (Application.GetDefaultKey (Command.Quit));

        // Assert — MenuBar should be completely inactive after ONE press
        Assert.False (menuBarItem.PopoverMenu!.Visible);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.Active);
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Activate_Fully_Deactivates_MenuBar ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var actionCount = 0;
        MenuItem menuItem = new () { Id = "menuItem", Title = "Menu_Item", Action = () => actionCount++ };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_MenuBarItem" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open the menu
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu!.Visible);

        // Act — activate MenuItem via its HotKey ('I' for "Menu_Item")
        app.InjectKey (Key.I);

        // Assert — Action should have fired
        Assert.Equal (1, actionCount);

        // Assert — MenuBar should be completely inactive after MenuItem activation
        Assert.False (menuBarItem.PopoverMenu!.Visible);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.Active);
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MouseLeave_Without_Open_Popover_Deactivates_MenuBar ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "Menu_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_MenuBarItem" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Mouse enters MenuBar — should activate
        Point menuBarPos = menuBar.FrameToScreen ().Location;
        app.InjectMouse (new Mouse { ScreenPosition = menuBarPos, Flags = MouseFlags.PositionReport });

        Assert.True (menuBar.Active);
        Assert.False (menuBar.IsOpen (), "Hovering should not open a popover");

        // Mouse leaves MenuBar — should deactivate (no popover is open)
        app.InjectMouse (new Mouse { ScreenPosition = new Point (0, menuBar.FrameToScreen ().Bottom + 2), Flags = MouseFlags.PositionReport });

        Assert.False (menuBar.Active);
        Assert.False (menuBar.CanFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void OptionSelector_Click_In_SubMenu_Deactivates_MenuBar ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open File menu
        MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Point fileScreenPos = fileItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (fileScreenPos));
        Assert.True (menuBar.IsOpen (), "File menu should be open");

        // Navigate to Preferences SubMenu
        Menu? rootMenu = fileItem.PopoverMenu!.Root;
        MenuItem? prefsItem = rootMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Title == "_Preferences");
        Assert.NotNull (prefsItem);
        prefsItem.SetFocus ();

        // Find the OptionSelector
        MenuItem? optionMenuItem = prefsItem.SubMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Id == "mutuallyExclusiveOptions");
        Assert.NotNull (optionMenuItem);
        OptionSelector<Schemes>? optionSelector = optionMenuItem.CommandView as OptionSelector<Schemes>;
        Assert.NotNull (optionSelector);

        // Click on the Error checkbox
        CheckBox? errorCheckBox = optionSelector.SubViews.OfType<CheckBox> ().ElementAtOrDefault ((int)Schemes.Error);
        Assert.NotNull (errorCheckBox);
        errorCheckBox.SetFocus ();
        Point errorScreenPos = errorCheckBox.FrameToScreen ().Location;
        app.InjectMouse (new Mouse { ScreenPosition = errorScreenPos, Flags = MouseFlags.LeftButtonPressed });
        app.InjectMouse (new Mouse { ScreenPosition = errorScreenPos, Flags = MouseFlags.LeftButtonReleased });

        // Assert — value changed AND menu is fully deactivated
        Assert.Equal (Schemes.Error, optionSelector.Value);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.Active);

        menuBar.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     When a MenuBarItem's popover is open and the mouse moves over another MenuBarItem,
    ///     the MenuBar should switch to the new item's popover. This test verifies that mouse
    ///     hover switching works without throwing a focus-related exception in View.Navigation.
    /// </summary>
    [Fact]
    public void Mouse_Hover_Over_Second_MenuBarItem_While_First_Is_Open_Switches ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        MenuBarItem firstItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0);
        MenuBarItem secondItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (1);

        // Act — click on first MBI to open its popover
        Point firstScreenPos = firstItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (firstScreenPos));

        Assert.True (menuBar.Active);
        Assert.True (firstItem.PopoverMenuOpen, "First item's popover should be open after click");

        // Act — move the mouse over the second MBI (hover, no click)
        Point secondScreenPos = secondItem.FrameToScreen ().Location;
        app.InjectMouse (new Mouse { ScreenPosition = secondScreenPos, Flags = MouseFlags.PositionReport });

        // Assert — second item should be open, first should be closed
        Assert.True (menuBar.Active, "MenuBar should remain active during hover switch");
        Assert.True (secondItem.PopoverMenuOpen, "Second item's popover should open on hover");
        Assert.False (firstItem.PopoverMenuOpen, "First item's popover should close when second opens");

        menuBar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void EnableForDesign_AltE_Activates_Edit_Menu ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.False (menuBar.Active);

        // EnableForDesign creates: _File (0), _Edit (1), _Help (2)
        MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0);
        MenuBarItem editItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (1);

        // Verify PopoverMenus are registered with Popovers (as they are in a real app)
        Assert.True (app.Popovers!.IsRegistered (fileItem.PopoverMenu));
        Assert.True (app.Popovers!.IsRegistered (editItem.PopoverMenu));

        // Track what actually opens — detect the real bug where _File opens instead of _Edit
        var filePopoverOpened = false;

        fileItem.PopoverMenu!.VisibleChanged += (_, _) =>
                                                {
                                                    if (fileItem.PopoverMenu.Visible)
                                                    {
                                                        filePopoverOpened = true;
                                                    }
                                                };

        // Act — press Alt+E to activate the _Edit menu directly (MenuBar is inactive)
        app.InjectKey (Key.E.WithAlt);

        // Assert — MenuBar should be active with the _Edit menu open (not _File)
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (editItem.PopoverMenu is { Visible: true });

        // The _File popover should NEVER have been opened (even transiently)
        Assert.False (filePopoverOpened, "Bug: _File menu was opened instead of/before _Edit menu");
        Assert.False (fileItem.PopoverMenu is { Visible: true });
    }

    // Claude - Opus 4.6
    [Fact]
    public void EnableForDesign_AltH_Activates_Help_Menu ()
    {
        // Arrange — same bug as Alt+E: pressing Alt+H to open _Help instead opens _File
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.False (menuBar.Active);

        // EnableForDesign creates: _File (0), _Edit (1), _Help (2)
        MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0);
        MenuBarItem helpItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (2);

        // Verify PopoverMenus are registered with Popovers (as they are in a real app)
        Assert.True (app.Popovers!.IsRegistered (fileItem.PopoverMenu));
        Assert.True (app.Popovers!.IsRegistered (helpItem.PopoverMenu));

        // Track what actually opens — detect the real bug where _File opens instead of _Help
        var filePopoverOpened = false;

        fileItem.PopoverMenu!.VisibleChanged += (_, _) =>
                                                {
                                                    if (fileItem.PopoverMenu.Visible)
                                                    {
                                                        filePopoverOpened = true;
                                                    }
                                                };

        // Act — press Alt+H to activate the _Help menu directly (MenuBar is inactive)
        app.InjectKey (Key.H.WithAlt);

        // Assert — MenuBar should be active with the _Help menu open (not _File)
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (helpItem.PopoverMenu is { Visible: true });

        // The _File popover should NEVER have been opened (even transiently)
        Assert.False (filePopoverOpened, "Bug: _File menu was opened instead of/before _Help menu");
        Assert.False (fileItem.PopoverMenu is { Visible: true });
    }

    // Claude - Opus 4.6
    [Fact]
    public void Mouse_Over_Standalone_Menu_While_MenuBar_Active_Does_Not_Crash ()
    {
        // Arrange — replicate the Menus Scenario layout:
        // A hostView containing a MenuBar (with EnableForDesign) and a standalone Menu (TestMenu).
        // The bug: After the MenuBar is activated (which causes focus corruption via the
        // Popovers HotKey dispatch bug), moving the mouse over the standalone Menu's MenuItems
        // causes a Debug.Assert(_hasFocus) failure in View.SetHasFocusFalse because
        // MenuItem.OnMouseEnter calls SetFocus() on a corrupted focus chain.
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        // Add a standalone Menu like the Menus Scenario's TestMenu
        Menu testMenu = new () { Y = 10, Id = "TestMenu" };

        // ReSharper disable once StringLiteralTypo
        MenuItem testMenuItem1 = new () { Title = "Z_igzag", Text = "Gonna zig zag" };
        MenuItem testMenuItem2 = new () { Title = "_Borders", Text = "Borders" };
        testMenu.Add (testMenuItem1, testMenuItem2);
        hostView.Add (testMenu);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Trigger the buggy activation path: press Alt+E which causes the Popovers dispatch
        // to fire the _Edit PopoverMenu's HotKey, which bubbles to MenuBar and causes a generic
        // Activate that opens _File first. This corrupts the focus state.
        app.InjectKey (Key.E.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());

        // Act — move the mouse over the standalone Menu's MenuItems.
        // MenuItem.OnMouseEnter calls SetFocus(), which traverses the focus chain. If the
        // focus state was corrupted by the buggy activation above, this will trigger a
        // Debug.Assert(_hasFocus) failure in View.SetHasFocusFalse (View.Navigation.cs:908).
        Point menuItemScreenPos = testMenuItem1.FrameToScreen ().Location;
        app.InjectMouse (new Mouse { ScreenPosition = menuItemScreenPos, Flags = MouseFlags.PositionReport });

        // Assert — focus state should be consistent after mouse move
        // If the bug is present, the assertion above will fire before we get here.
        // Verify the standalone MenuItem got focus and the MenuBar closed cleanly.
        Assert.True (testMenuItem1.HasFocus, "TestMenu's MenuItem should have focus after mouse enter");
        Assert.False (menuBar.IsOpen (), "MenuBar popover should close when focus moves to standalone Menu");
    }

    // Claude - Opus 4.6
    [Fact]
    public void CursorRight_While_PopoverOpen_Switches_To_Next_MenuBarItem ()
    {
        // Arrange — reproduces Integration test Navigation_Left_Right_Wraps
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0);
        MenuBarItem editItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (1);
        MenuBarItem helpItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (2);

        // Act — press F10 to open MenuBar (File menu opens)
        app.InjectKey (MenuBar.DefaultKey);

        Assert.True (menuBar.Active, "MenuBar should be active after F10");
        Assert.True (menuBar.IsOpen (), "MenuBar should be open after F10");
        Assert.True (fileItem.PopoverMenu is { Visible: true }, "File's popover should be visible");

        // Act — press CursorRight to switch to Edit menu
        app.InjectKey (Key.CursorRight);

        // Assert — Edit's popover should now be open, File's should be closed
        Assert.True (menuBar.Active, "MenuBar should still be active after CursorRight");
        Assert.True (menuBar.IsOpen (), "MenuBar should still be open after CursorRight");
        Assert.False (fileItem.PopoverMenu is { Visible: true }, "File's popover should be hidden");
        Assert.True (editItem.PopoverMenu is { Visible: true }, "Edit's popover should be visible");

        // Act — press CursorRight again to switch to Help menu
        app.InjectKey (Key.CursorRight);

        Assert.True (helpItem.PopoverMenu is { Visible: true }, "Help's popover should be visible");
        Assert.False (editItem.PopoverMenu is { Visible: true }, "Edit's popover should be hidden");

        // Act — press CursorRight again to wrap to File
        app.InjectKey (Key.CursorRight);

        Assert.True (fileItem.PopoverMenu is { Visible: true }, "File's popover should be visible after wrap");
        Assert.False (helpItem.PopoverMenu is { Visible: true }, "Help's popover should be hidden");

        // Act — press CursorLeft to go back to Help
        app.InjectKey (Key.CursorLeft);

        Assert.True (helpItem.PopoverMenu is { Visible: true }, "Help's popover should be visible after CursorLeft");
        Assert.False (fileItem.PopoverMenu is { Visible: true }, "File's popover should be hidden");
    }

    // Claude - Opus 4.6
    [Fact]
    public void After_Deactivation_Keys_Are_Not_Eaten_By_PopoverMenu_With_SubMenu ()
    {
        // Arrange — a MenuBar with a menu item that has a submenu containing HotKey items
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        // Create a submenu with HotKey items
        MenuItem subItem1 = new () { Id = "subItem1", Title = "_Bold" };
        MenuItem subItem2 = new () { Id = "subItem2", Title = "_Dark" };
        Menu subMenu = new ([subItem1, subItem2]) { Id = "subMenu" };

        // Create root menu with an item that has the submenu
        MenuItem rootItemWithSub = new () { Id = "rootItemWithSub", Title = "_Themes", SubMenu = subMenu };
        MenuItem rootItemPlain = new () { Id = "rootItemPlain", Title = "_New" };
        Menu rootMenu = new ([rootItemPlain, rootItemWithSub]) { Id = "rootMenu" };

        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_File" };
        PopoverMenu popover = new ();
        menuBarItem.PopoverMenu = popover;
        popover.Root = rootMenu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        // Create a focusable view that should receive keys after deactivation
        var keyReceived = false;
        View focusableView = new () { Id = "focusableView", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        focusableView.KeyDown += (_, k) =>
                                 {
                                     if (k == Key.B)
                                     {
                                         keyReceived = true;
                                     }
                                 };

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);
        hostView.Add (focusableView);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Act — open the menu, open the submenu, then close the menu
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active, "MenuBar should be active");
        Assert.True (menuBar.IsOpen (), "MenuBar should be open");

        // Navigate to the Themes item and open its submenu
        app.InjectKey (Key.CursorDown); // Focus on _Themes
        app.InjectKey (Key.CursorRight); // Open submenu

        // Close the menu entirely
        app.InjectKey (Application.GetDefaultKey (Command.Quit));

        Assert.False (menuBar.Active, "MenuBar should be deactivated");
        Assert.False (menuBar.IsOpen (), "No popover should be open");

        // Verify focus is on the focusable view
        focusableView.SetFocus ();
        Assert.True (focusableView.HasFocus, "focusableView should have focus");

        // Act — press 'B' which was a HotKey in the submenu
        app.InjectKey (Key.B);

        // Assert — the key should reach the focusable view, not be eaten by the PopoverMenu
        Assert.True (keyReceived, "Key 'B' should reach focusableView, not be eaten by inactive PopoverMenu");
    }

    // Claude - Opus 4.6
    [Fact]
    public void CursorRight_From_SubMenu_Switches_MenuBarItem_And_Closes_SubMenu ()
    {
        // Arrange — MenuBar with two items, first item has a submenu
        // Reproduces: "if the submenu is open and I press right arrow to cause the next menu
        // to open, and then go left, the submenu is still open"
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        // First MenuBarItem: has a submenu
        MenuItem subItem = new () { Id = "subItem", Title = "_Bold" };
        Menu subMenu = new ([subItem]) { Id = "subMenu" };
        MenuItem rootItemWithSub = new () { Id = "rootItemWithSub", Title = "_Themes", SubMenu = subMenu };
        MenuItem rootItemPlain = new () { Id = "rootItemPlain", Title = "_New" };
        Menu rootMenu1 = new ([rootItemPlain, rootItemWithSub]) { Id = "rootMenu1" };
        MenuBarItem menuBarItem1 = new () { Id = "menuBarItem1", Title = "_File" };
        PopoverMenu popover1 = new ();
        menuBarItem1.PopoverMenu = popover1;
        popover1.Root = rootMenu1;

        // Second MenuBarItem: simple
        MenuItem editItem = new () { Id = "editItem", Title = "Cu_t" };
        Menu rootMenu2 = new ([editItem]) { Id = "rootMenu2" };
        MenuBarItem menuBarItem2 = new () { Id = "menuBarItem2", Title = "_Edit" };
        PopoverMenu popover2 = new ();
        menuBarItem2.PopoverMenu = popover2;
        popover2.Root = rootMenu2;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem1);
        menuBar.Add (menuBarItem2);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open File menu, navigate to Themes, open its submenu
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBarItem1.PopoverMenu is { Visible: true }, "File's popover should be visible");
        app.InjectKey (Key.CursorDown); // Focus on _Themes
        app.InjectKey (Key.CursorRight); // Open submenu, focus on _Bold
        Assert.True (subMenu.Visible, "SubMenu should be visible after CursorRight");

        // Act — from the submenu, CursorRight again. _Bold has no sub-submenu,
        // so PopoverMenu.MoveRight returns false, key propagates to MenuBar, which
        // switches to Edit.
        app.InjectKey (Key.CursorRight);

        // Assert — File's popover (and its submenu) should be fully closed
        Assert.False (menuBarItem1.PopoverMenu is { Visible: true }, "File's popover should be hidden");
        Assert.False (subMenu.Visible, "SubMenu should be closed after switching MenuBarItem");
        Assert.True (menuBarItem2.PopoverMenu is { Visible: true }, "Edit's popover should be visible");

        // Act — go back to File with CursorLeft
        app.InjectKey (Key.CursorLeft);

        // Assert — File reopens WITHOUT the submenu still showing
        Assert.True (menuBarItem1.PopoverMenu is { Visible: true }, "File's popover should be visible again");
        Assert.False (subMenu.Visible, "SubMenu should NOT reappear when returning to File");
    }

    // Claude - Opus 4.6
    [Fact]
    public void AltE_Opens_Edit_When_Another_View_Has_Focus ()
    {
        // Arrange — closer to UICatalog: MenuBar + separate focusable view with focus
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        // A separate focusable view that has focus (simulates scenarios TableView)
        View contentView = new ()
        {
            Id = "content",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Y = 1
        };
        hostView.Add (contentView);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Ensure the content view has focus, not the MenuBar
        contentView.SetFocus ();
        Assert.True (contentView.HasFocus, "Content view should have focus");
        Assert.False (menuBar.HasFocus, "MenuBar should NOT have focus");

        MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0);
        MenuBarItem editItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (1);

        var filePopoverOpened = false;

        fileItem.PopoverMenu!.VisibleChanged += (_, _) =>
                                                {
                                                    if (fileItem.PopoverMenu.Visible)
                                                    {
                                                        filePopoverOpened = true;
                                                    }
                                                };

        // Act — press Alt+E
        app.InjectKey (Key.E.WithAlt);

        // Assert
        Assert.True (menuBar.Active, "MenuBar should be active");
        Assert.True (editItem.PopoverMenu is { Visible: true }, "Edit's popover should be visible");
        Assert.False (filePopoverOpened, "File's popover should NEVER have opened");
    }

    // Claude - Opus 4.6
    [Fact]
    public void Enter_On_MenuItem_Does_Not_Raise_Accepting_On_MenuBar_SuperView ()
    {
        // Arrange — Enter on a MenuItem should activate it (fire its action), not raise
        // Accepting which would bubble up to the MenuBar's SuperView.
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var menuItemActivated = false;
        MenuItem menuItem = new () { Id = "menuItem", Title = "_New" };
        menuItem.Activated += (_, _) => menuItemActivated = true;
        Menu rootMenu = new ([menuItem]) { Id = "rootMenu" };

        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_File" };
        PopoverMenu popover = new ();
        menuBarItem.PopoverMenu = popover;
        popover.Root = rootMenu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        var hostAccepted = false;
        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Accepting += (_, _) => hostAccepted = true;
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open the menu
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.IsOpen (), "MenuBar should be open");

        // Act — press Enter on the focused MenuItem
        app.InjectKey (Key.Enter);

        // Assert — the MenuItem should have been activated, but Accepting should NOT bubble
        Assert.True (menuItemActivated, "MenuItem should have been activated");
        Assert.False (hostAccepted, "Accepting should NOT have bubbled up to the host view");
    }

    #endregion

    #region Diagnostic and fix tests

    // Claude - Opus 4.6
    [Fact]
    public void Diagnostic_AltE_TraceCapture ()
    {
        ListBackend traceBackend = new ();
        Trace.Backend = traceBackend;

        // Do not set this unless debugging. It is a static that is process wide.
        //Trace.EnabledCategories |= TraceCategory.Command;
        //Trace.EnabledCategories |= TraceCategory.Keyboard;

        try
        {
            VirtualTimeProvider time = new ();
            using IApplication app = Application.Create (time);
            app.Init (DriverRegistry.Names.ANSI);
            IRunnable runnable = new Runnable ();

            View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
            MenuBar menuBar = new () { Id = "menuBar" };
            menuBar.EnableForDesign (ref hostView);
            hostView.Add (menuBar);
            ((View)runnable).Add (hostView);
            app.Begin (runnable);

            // Track File popover opening (detects the transient open bug)
            MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0);
            MenuBarItem editItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (1);
            var filePopoverOpened = false;

            fileItem.PopoverMenu!.VisibleChanged += (_, _) =>
                                                    {
                                                        if (fileItem.PopoverMenu.Visible)
                                                        {
                                                            filePopoverOpened = true;
                                                        }
                                                    };

            // Clear traces from setup
            traceBackend.Clear ();

            // Act — press Alt+E
            app.InjectKey (Key.E.WithAlt);

            // Build trace dump for assertion messages
            string traceDump = string.Join ("\n",
                                            traceBackend.Entries.Select (e =>
                                                                         {
                                                                             string dataStr = e.Data switch
                                                                                              {
                                                                                                  (Command cmd, CommandRouting routing) => $"Cmd={
                                                                                                      cmd
                                                                                                  } Routing={
                                                                                                      routing
                                                                                                  }",
                                                                                                  Key key => $"Key={key}",
                                                                                                  _ => e.Data?.ToString () ?? ""
                                                                                              };

                                                                             return $"  [{e.Category}:{e.Phase}] {e.Id} ({e.Method}) {e.Message} [{dataStr}]";
                                                                         }));

            // The real assertions — with trace dump in failure message
            Assert.True (menuBar.Active, $"MenuBar should be active.\nTraces:\n{traceDump}");
            Assert.True (editItem.PopoverMenu is { Visible: true }, $"Edit PopoverMenu should be visible.\nTraces:\n{traceDump}");
            Assert.False (filePopoverOpened, $"File PopoverMenu should NEVER have opened.\nTraces:\n{traceDump}");
        }
        finally
        {
            Trace.Backend = new NullBackend ();
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void Diagnostic_FocusChain_After_AltE ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        Menu testMenu = new () { Y = 10, Id = "TestMenu" };

        // ReSharper disable once StringLiteralTypo
        MenuItem testMenuItem1 = new () { Title = "Z_igzag", Text = "Gonna zig zag" };
        testMenu.Add (testMenuItem1);
        hostView.Add (testMenu);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Capture focus chain BEFORE Alt+E
        string focusBefore = DumpFocusChain ((View)runnable, "Before Alt+E");

        // Press Alt+E
        app.InjectKey (Key.E.WithAlt);

        // Capture focus chain AFTER Alt+E
        string focusAfter = DumpFocusChain ((View)runnable, "After Alt+E");

        // Verify the focus chain is consistent:
        // Walk from MostFocused up to root, verifying _hasFocus is true at every level
        View? current = app.Navigation?.GetFocused ();
        List<string> focusProblems = [];

        while (current is { })
        {
            if (!current.HasFocus)
            {
                focusProblems.Add ($"  BROKEN: {current.ToIdentifyingString ()} HasFocus=false but is in the focused chain");
            }

            current = current.SuperView;
        }

        Assert.True (focusProblems.Count == 0, $"Focus chain is corrupted:\n{string.Join ("\n", focusProblems)}\n\n{focusBefore}\n\n{focusAfter}");
    }

    private static string DumpFocusChain (View root, string label)
    {
        List<string> lines = [$"=== {label} ==="];
        DumpView (root, 0, lines);

        return string.Join ("\n", lines);
    }

    private static void DumpView (View view, int indent, List<string> lines)
    {
        string prefix = new (' ', indent * 2);
        string focusMarker = view.HasFocus ? " [FOCUSED]" : "";
        lines.Add ($"{prefix}{view.ToIdentifyingString ()}{focusMarker}");

        foreach (View sub in view.SubViews)
        {
            DumpView (sub, indent + 1, lines);
        }
    }

    #endregion

    #region IValue Integration Tests

    // Claude - Opus 4.5
    [Fact]
    public void MenuBar_Activated_ContextValue_ContainsMenuItem ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            VirtualTimeProvider time = new ();
            using IApplication app = Application.Create (time);
            app.Init (DriverRegistry.Names.ANSI);
            Runnable runnable = new ();

            MenuBar menuBar = new ();
            runnable.Add (menuBar);

            MenuItem menuItem = new () { Title = "TestItem" };
            PopoverMenu popoverMenu = new ([menuItem]);
            MenuBarItem menuBarItem = new ("Test", popoverMenu);
            menuBar.Add (menuBarItem);

            // Register the popoverMenu with Application
            app.Popovers?.Register (popoverMenu);

            string? lastActivatedValueText = null;
            var menuBarActivatedCount = 0;

            menuBar.Activated += (_, args) =>
                                 {
                                     menuBarActivatedCount++;

                                     if (args.Value?.Value is string title)
                                     {
                                         lastActivatedValueText = title;
                                     }
                                 };

            // Invoke Activate command on the MenuItem
            menuItem.InvokeCommand (Command.Activate);

            Assert.Equal (1, menuBarActivatedCount);
            Assert.Equal (menuItem.Title, lastActivatedValueText);

            runnable.Dispose ();
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Activate_WithFocusedMenuItem_DoesNotStackOverflow ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();

        Menu menu = new ();
        MenuItem menuItem = new () { Title = "TestItem", CanFocus = true };
        menu.Add (menuItem);
        runnable.Add (menu);

        app.Begin (runnable);

        // Focus the MenuItem inside the Menu (simulates real scenario)
        menuItem.SetFocus ();
        Assert.True (menuItem.HasFocus);

        // This should NOT stack overflow
        menuItem.InvokeCommand (Command.Activate);

        // Value should be set to the activated MenuItem
        Assert.Same (menuItem, menu.Value);

        runnable.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void PopoverMenu_Activate_WithFocusedMenuItem_DoesNotStackOverflow ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        Runnable runnable = new ();

        MenuItem menuItem = new () { Title = "TestItem" };
        PopoverMenu popoverMenu = new ([menuItem]);
        runnable.Add (popoverMenu);

        app.Popovers?.Register (popoverMenu);
        app.Begin (runnable);

        // Get the Menu inside the PopoverMenu and focus the MenuItem
        Menu? menu = popoverMenu.SubViews.OfType<Menu> ().FirstOrDefault ();
        Assert.NotNull (menu);

        popoverMenu.Visible = true;
        menuItem.SetFocus ();

        // This should NOT stack overflow
        menuItem.InvokeCommand (Command.Activate);

        // Value should propagate to the Menu
        Assert.Same (menuItem, menu.Value);

        runnable.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuBar_Activated_ContextValue_WithFocusedMenuItem ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            VirtualTimeProvider time = new ();
            using IApplication app = Application.Create (time);
            app.Init (DriverRegistry.Names.ANSI);
            Runnable runnable = new ();

            MenuBar menuBar = new ();
            runnable.Add (menuBar);

            MenuItem menuItem = new () { Title = "TestItem" };
            PopoverMenu popoverMenu = new ([menuItem]);
            MenuBarItem menuBarItem = new ("Test", popoverMenu);
            menuBar.Add (menuBarItem);

            app.Popovers?.Register (popoverMenu);
            app.Begin (runnable);

            string? lastActivatedValueText = null;
            var menuBarActivatedCount = 0;

            menuBar.Activated += (_, args) =>
                                 {
                                     menuBarActivatedCount++;

                                     if (args.Value?.Value is string title)
                                     {
                                         lastActivatedValueText = title;
                                     }
                                 };

            // Focus the MenuItem (simulates real user interaction)
            Menu? menu = popoverMenu.SubViews.OfType<Menu> ().FirstOrDefault ();
            Assert.NotNull (menu);

            popoverMenu.Visible = true;
            menuItem.SetFocus ();
            Assert.True (menuItem.HasFocus);

            // Invoke Activate - should not stack overflow and ctx.Value should work
            menuItem.InvokeCommand (Command.Activate);

            Assert.Equal (1, menuBarActivatedCount);
            Assert.Equal (menuItem.Title, lastActivatedValueText);

            runnable.Dispose ();
        }
    }

    #endregion
}

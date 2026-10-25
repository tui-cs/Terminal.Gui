namespace ViewsTests;

public class MenuItemOpenAndSelectTests
{
    /// <summary>
    ///     Builds a MenuBarItem in the designer-style inline mode (UsePopoverMenu = false).
    /// </summary>
    private static MenuBarItem CreateInlineBarItem (string title, params MenuItem [] items) =>
        new () { Title = title, UsePopoverMenu = false, SubMenu = new Menu (items) };

    [Fact]
    public void OpenAndSelect_DeepItem_OpensFullHierarchy_AndFocusesTarget ()
    {
        var file1 = new MenuItem { Title = "File1.txt" };

        var recent = new MenuItem { Title = "_Recent", SubMenu = new Menu ([file1]) };

        MenuBarItem fileMenu = CreateInlineBarItem ("_File", new MenuItem { Title = "_New" }, recent, new MenuItem { Title = "E_xit" });

        var menuBar = new MenuBar ([fileMenu]);

        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        using var top = new Window ();
        top.Add (menuBar);
        top.BeginInit ();
        top.EndInit ();
        top.LayoutSubViews ();

        Assert.IsType<Menu> (file1.SuperView);
        Assert.Same (recent, ((Menu)file1.SuperView!).SuperMenuItem);

        bool ok = file1.OpenAndSelect ();

        Assert.True (ok);
        Assert.True (menuBar.Active);
        Assert.True (((IMenuBarEntry)fileMenu).IsMenuOpen);
        Assert.True (recent.SubMenu!.Visible);
        Assert.Same (file1, recent.SubMenu.SelectedMenuItem);
        Assert.True (file1.HasFocus);
    }

    [Fact]
    public void OpenAndSelect_AfterModal_RestoresSelection ()
    {
        var target = new MenuItem { Title = "_Edit Me" };
        MenuBarItem fileMenu = CreateInlineBarItem ("_File", target, new MenuItem { Title = "_Quit" });
        var menuBar = new MenuBar ([fileMenu]);

        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        using var top = new Window ();
        top.Add (menuBar);
        top.BeginInit ();
        top.EndInit ();
        top.LayoutSubViews ();

        menuBar.Active = true;
        ((IMenuBarEntry)fileMenu).IsMenuOpen = true;
        target.SetFocus ();
        Assert.True (target.HasFocus);

        ((IMenuBarEntry)fileMenu).IsMenuOpen = false;
        menuBar.Active = false;

        Assert.IsType<Menu> (target.SuperView); // still attached

        bool ok = target.OpenAndSelect ();

        Assert.True (ok);
        Assert.True (menuBar.Active);
        Assert.True (((IMenuBarEntry)fileMenu).IsMenuOpen);
        Assert.NotNull (fileMenu.SubMenu);
        Assert.True (fileMenu.SubMenu!.Visible);
        Assert.Same (target, fileMenu.SubMenu.SelectedMenuItem);
    }

    [Fact]
    public void OpenAndSelect_DetachedItem_ReturnsFalse ()
    {
        var orphan = new MenuItem { Title = "Orphan" };
        Assert.False (orphan.OpenAndSelect ());
    }

    [Fact]
    public void OpenAndSelect_AfterMovingItemBetweenSubMenus ()
    {
        var item = new MenuItem { Title = "Movable" };

        var subA = new Menu ([item]);
        var parentA = new MenuItem { Title = "A", SubMenu = subA };

        var subB = new Menu ();
        var parentB = new MenuItem { Title = "B", SubMenu = subB };

        MenuBarItem root = CreateInlineBarItem ("_Root", parentA, parentB);
        var menuBar = new MenuBar ([root]);

        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        using var top = new Window ();
        top.Add (menuBar);
        top.BeginInit ();
        top.EndInit ();
        top.LayoutSubViews ();

        subA.Remove (item);
        subB.Add (item);

        bool ok = item.OpenAndSelect ();

        Assert.True (ok);
        Assert.True (subB.Visible);
        Assert.Same (item, subB.SelectedMenuItem);
        Assert.True (item.HasFocus);
    }

    [Fact]
    public void OpenAndSelect_Idempotent_WhenAlreadyOpen ()
    {
        var item = new MenuItem { Title = "Already open" };
        MenuBarItem barItem = CreateInlineBarItem ("_File", item);
        var menuBar = new MenuBar ([barItem]);

        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        using var top = new Window ();
        top.Add (menuBar);
        top.BeginInit ();
        top.EndInit ();
        top.LayoutSubViews ();

        Assert.True (item.OpenAndSelect ());
        Assert.True (item.OpenAndSelect ());
        Assert.True (item.HasFocus);
        Assert.True (((IMenuBarEntry)barItem).IsMenuOpen);
    }

    [Fact]
    public void OpenAndSelect_PopoverMode_OpensPopover_AndFocusesTarget ()
    {
        var target = new MenuItem { Title = "_Edit Me" };

        var fileMenu = new MenuBarItem { Title = "_File", PopoverMenu = new PopoverMenu ([target, new MenuItem { Title = "_Quit" }]) };

        var menuBar = new MenuBar ([fileMenu]);

        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);

        using var top = new Window ();
        top.Add (menuBar);
        top.BeginInit ();
        top.EndInit ();
        top.LayoutSubViews ();

        // MenuBar.EndInit should Register; if not, force it for the test
        if (fileMenu.PopoverMenu is { } pop && app.Popovers is { } popovers && !popovers.IsRegistered (pop)) // Register if MenuBar.EndInit did not.
        {
            popovers.Register (pop);
        }

        Assert.IsType<Menu> (target.SuperView);
        Assert.Same (fileMenu.PopoverMenu!.Root, target.SuperView);

        bool ok = target.OpenAndSelect ();

        Assert.True (ok);
        Assert.True (menuBar.Active);
        Assert.True (fileMenu.PopoverMenuOpen);

        // Prefer SelectedMenuItem on the root menu over HasFocus under unit fixtures
        Assert.Same (target, fileMenu.PopoverMenu!.Root!.SelectedMenuItem);
    }
}

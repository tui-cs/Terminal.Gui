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

        // Modal closed the menus
        ((IMenuBarEntry)fileMenu).IsMenuOpen = false;
        menuBar.Active = false;

        bool ok = target.OpenAndSelect ();

        Assert.True (ok);
        Assert.True (menuBar.Active);
        Assert.True (((IMenuBarEntry)fileMenu).IsMenuOpen);
        Assert.True (target.HasFocus);
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
}

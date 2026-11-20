namespace ViewsTests;

public class MenuOpenAndSelectHelperTests
{
    private static MenuBarItem CreateInlineBarItem (string title, params MenuItem [] items) =>
        new () { Title = title, UsePopoverMenu = false, SubMenu = new Menu (items) };

    private static (IApplication app, Window top, MenuBar menuBar, MenuBarItem fileMenu, MenuItem target) CreateFixture (MenuItem target)
    {
        MenuBarItem fileMenu = CreateInlineBarItem ("_File", target, new MenuItem { Title = "_Quit" });

        var menuBar = new MenuBar ([fileMenu]);

        IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        var top = new Window ();
        top.Add (menuBar);
        top.BeginInit ();
        top.EndInit ();
        top.LayoutSubViews ();

        return (app, top, menuBar, fileMenu, target);
    }

    [Fact]
    public void MenuBar_OpenAndSelect_DelegatesToMenuItem_AndOpensHierarchy ()
    {
        var target = new MenuItem { Title = "_Edit Me" };
        (IApplication app, Window top, MenuBar menuBar, MenuBarItem fileMenu, _) = CreateFixture (target);

        using (app)
        using (top)
        {
            bool ok = menuBar.OpenAndSelect (target);

            Assert.True (ok);
            Assert.True (menuBar.Active);
            Assert.True (((IMenuBarEntry)fileMenu).IsMenuOpen);
            Assert.True (target.HasFocus);
        }
    }

    [Fact]
    public void MenuBar_OpenAndSelect_Null_Throws ()
    {
        var menuBar = new MenuBar ();

        Assert.Throws<ArgumentNullException> (() => menuBar.OpenAndSelect (null!));
    }

    [Fact]
    public void MenuBar_OpenAndSelect_DetachedItem_ReturnsFalse ()
    {
        var orphan = new MenuItem { Title = "Orphan" };
        var menuBar = new MenuBar ([CreateInlineBarItem ("_File", new MenuItem { Title = "Other" })]);

        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        using var top = new Window ();
        top.Add (menuBar);
        top.BeginInit ();
        top.EndInit ();
        top.LayoutSubViews ();

        Assert.False (menuBar.OpenAndSelect (orphan));
    }

    [Fact]
    public void Menu_EnsureOpenAndSelect_OpensMenu_AndFocusesDirectChild ()
    {
        var item = new MenuItem { Title = "Child" };
        var menu = new Menu ([item]);

        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        using var top = new Window ();
        top.Add (menu);
        top.BeginInit ();
        top.EndInit ();
        top.LayoutSubViews ();

        menu.Visible = false;

        menu.EnsureOpenAndSelect (item);

        Assert.True (menu.Visible);
        Assert.Same (item, menu.SelectedMenuItem);
        Assert.True (item.HasFocus);
    }

    [Fact]
    public void Menu_EnsureOpenAndSelect_NestedItem_UsesOpenAndSelect ()
    {
        var nested = new MenuItem { Title = "Nested" };
        var parent = new MenuItem { Title = "Parent", SubMenu = new Menu ([nested]) };
        var rootMenu = new Menu ([parent]);

        // Attach under inline MenuBar so OpenAndSelect can resolve hierarchy
        var barItem = new MenuBarItem { Title = "_Root", UsePopoverMenu = false, SubMenu = rootMenu };
        var menuBar = new MenuBar ([barItem]);

        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        using var top = new Window ();
        top.Add (menuBar);
        top.BeginInit ();
        top.EndInit ();
        top.LayoutSubViews ();

        rootMenu.EnsureOpenAndSelect (nested);

        Assert.True (menuBar.Active);
        Assert.True (((IMenuBarEntry)barItem).IsMenuOpen);
        Assert.True (parent.SubMenu!.Visible);
        Assert.Same (nested, parent.SubMenu.SelectedMenuItem);
        Assert.True (nested.HasFocus);
    }

    [Fact]
    public void Menu_EnsureOpenAndSelect_Null_Throws ()
    {
        var menu = new Menu ();

        Assert.Throws<ArgumentNullException> (() => menu.EnsureOpenAndSelect (null!));
    }
}

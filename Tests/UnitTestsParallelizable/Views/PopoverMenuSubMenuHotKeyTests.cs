namespace ViewsTests;

public class PopoverMenuSubMenuHotKeyTests
{
    private sealed class Fixture : IDisposable
    {
        public IApplication App { get; }
        public MenuBar MenuBar { get; }
        public MenuBarItem FileItem { get; }
        public MenuItem ExportItem { get; }
        public MenuItem ExcelItem { get; }
        public MenuItem TextItem { get; }
        public MenuItem OpenItem { get; }

        public Fixture ()
        {
            App = Application.Create (new VirtualTimeProvider ());
            App.Init (DriverRegistry.Names.ANSI);

            OpenItem = new MenuItem { Id = "open", Title = "_Open" };

            // Sub-menu: the first item deliberately shares its hotkey (E) with ExportItem
            ExcelItem = new MenuItem { Id = "excel", Title = "_Excel" };
            TextItem = new MenuItem { Id = "text", Title = "_Text" };
            ExportItem = new MenuItem { Id = "export", Title = "_Export", SubMenu = new Menu ([ExcelItem, TextItem]) { Id = "exportSubMenu" } };

            FileItem = new MenuBarItem { Id = "file", Title = "_File", PopoverMenu = new PopoverMenu ([OpenItem, ExportItem]) { Id = "filePopover" } };

            MenuBar = new MenuBar { Id = "menuBar" };
            MenuBar.Add (FileItem);

            View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
            hostView.Add (MenuBar);

            Runnable runnable = new ();
            runnable.Add (hostView);
            App.Begin (runnable);
        }

        public void Dispose () => App.Dispose ();
    }

    // Copilot
    [Fact]
    public void HotKey_On_MenuItem_With_SubMenu_Keeps_Popover_Open ()
    {
        using Fixture f = new ();

        f.App.InjectKey (Key.F.WithAlt);
        Assert.True (f.FileItem.PopoverMenu!.Visible);

        f.App.InjectKey (Key.E.WithAlt);

        Assert.True (f.MenuBar.Active);
        Assert.True (f.FileItem.PopoverMenu.Visible);
    }

    // Copilot
    [Fact]
    public void HotKey_On_MenuItem_With_SubMenu_Shows_SubMenu ()
    {
        using Fixture f = new ();

        f.App.InjectKey (Key.F.WithAlt);
        f.App.InjectKey (Key.E.WithAlt);

        Assert.True (f.ExportItem.SubMenu!.Visible);
    }

    // Copilot
    [Fact]
    public void HotKey_On_MenuItem_With_SubMenu_Focuses_First_SubMenu_Item ()
    {
        using Fixture f = new ();

        f.App.InjectKey (Key.F.WithAlt);
        f.App.InjectKey (Key.E.WithAlt);

        Assert.True (f.ExcelItem.HasFocus);
        Assert.False (f.ExportItem.HasFocus);
    }

    // Copilot
    [Fact]
    public void HotKey_On_MenuItem_With_SubMenu_Skips_Disabled_Items_When_Focusing ()
    {
        using Fixture f = new ();
        f.ExcelItem.Enabled = false;

        f.App.InjectKey (Key.F.WithAlt);
        f.App.InjectKey (Key.E.WithAlt);

        Assert.True (f.ExportItem.SubMenu!.Visible);
        Assert.True (f.TextItem.HasFocus);
    }

    // Copilot
    [Fact]
    public void HotKey_On_Leaf_MenuItem_Hides_Popover ()
    {
        using Fixture f = new ();
        var openActivated = 0;
        f.OpenItem.Activated += (_, _) => openActivated++;

        f.App.InjectKey (Key.F.WithAlt);
        Assert.True (f.FileItem.PopoverMenu!.Visible);

        f.App.InjectKey (Key.O.WithAlt);

        Assert.Equal (1, openActivated);
        Assert.False (f.FileItem.PopoverMenu.Visible);
    }

    // Copilot
    [Fact]
    public void SubMenu_HotKey_After_Opening_SubMenu_Activates_SubMenu_Item ()
    {
        using Fixture f = new ();
        var textAccepted = 0;
        f.TextItem.Activated += (_, _) => textAccepted++;

        f.App.InjectKey (Key.F.WithAlt);
        f.App.InjectKey (Key.E.WithAlt);
        Assert.True (f.FileItem.PopoverMenu!.Visible);

        f.App.InjectKey (Key.T.WithAlt);

        Assert.Equal (1, textAccepted);
        Assert.False (f.FileItem.PopoverMenu.Visible);
    }

    // Copilot
    [Fact]
    public void Same_HotKey_On_MenuItem_And_Its_SubMenu_Item_Is_Allowed ()
    {
        using Fixture f = new ();
        var excelAccepted = 0;
        f.ExcelItem.Activated += (_, _) => excelAccepted++;

        // Both ExportItem ("_Export") and ExcelItem ("_Excel") use HotKey E, each in a different SuperView
        Assert.Equal (Key.E, f.ExportItem.HotKey);
        Assert.Equal (Key.E, f.ExcelItem.HotKey);
        Assert.NotSame (f.ExportItem.SuperView, f.ExcelItem.SuperView);

        f.App.InjectKey (Key.F.WithAlt);

        // First press opens the sub-menu (resolves to the parent menu's item)
        f.App.InjectKey (Key.E.WithAlt);
        Assert.True (f.ExportItem.SubMenu!.Visible);
        Assert.Equal (0, excelAccepted);

        // Second press resolves to the sub-menu's item
        f.App.InjectKey (Key.E.WithAlt);
        Assert.Equal (1, excelAccepted);
    }
}

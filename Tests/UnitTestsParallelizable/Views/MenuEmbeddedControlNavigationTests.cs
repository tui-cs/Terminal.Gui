namespace ViewsTests;

public class MenuEmbeddedControlNavigationTests
{
    private static string FocusedId (IApplication app) => app.Navigation?.GetFocused ()?.Id ?? "<null>";

    private static (IApplication app, OptionSelector<Schemes> selector, int[] changedCount) CreateOpenedRootWithEmbeddedSelector ()
    {
        VirtualTimeProvider time = new ();
        IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        OptionSelector<Schemes> selector = new () { Title = "Scheme", CanFocus = true };
        int[] changedCount = [0];
        selector.ValueChanged += (_, _) => { changedCount [0]++; };

        MenuItem item = new () { Id = "selectorItem", HelpText = "Pick a scheme", CommandView = selector };

        Menu root = new ([item]) { Id = "root" };
        MenuBarItem bi = new () { Id = "bi", Title = "_Test" };
        PopoverMenu pm = new ();
        bi.PopoverMenu = pm;
        pm.Root = root;
        MenuBar bar = new () { Id = "bar" };
        bar.Add (bi);

        View host = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        host.Add (bar);
        ((View)runnable).Add (host);
        app.Begin (runnable);

        app.InjectKey (MenuBar.DefaultKey);

        return (app, selector, changedCount);
    }

    private static (IApplication app, Menu root, OptionSelector<Schemes> selector, int[] changedCount) CreateOpenedMenuWithEmbeddedSelector (int selectorIndex, int totalItems)
    {
        VirtualTimeProvider time = new ();
        IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        OptionSelector<Schemes> selector = new () { Title = "Scheme", CanFocus = true };
        int[] changedCount = [0];
        selector.ValueChanged += (_, _) => { changedCount [0]++; };

        List<View> items = [];

        for (var i = 0; i < totalItems; i++)
        {
            if (i == selectorIndex)
            {
                items.Add (new MenuItem { Id = "selectorItem", HelpText = "Pick a scheme", CommandView = selector });
            }
            else
            {
                items.Add (new MenuItem { Id = $"item{i}", Title = $"_Item {i}" });
            }
        }

        Menu root = new (items) { Id = "root" };
        MenuBarItem bi = new () { Id = "bi", Title = "_Test" };
        PopoverMenu pm = new ();
        bi.PopoverMenu = pm;
        pm.Root = root;
        MenuBar bar = new () { Id = "bar" };
        bar.Add (bi);

        View host = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        host.Add (bar);

        // A focusable peer outside the popover: if navigation escapes the menu, focus lands here
        // instead of staying inside the menu.
        View peer = new () { Id = "peer", CanFocus = true };
        host.Add (peer);
        ((View)runnable).Add (host);
        app.Begin (runnable);

        app.InjectKey (MenuBar.DefaultKey);

        return (app, root, selector, changedCount);
    }

    private static (IApplication app, Menu subMenu, OptionSelector<Schemes> selector, int[] changedCount) CreateOpenedSubmenuWithEmbeddedSelector (int selectorIndex, int totalItems)
    {
        VirtualTimeProvider time = new ();
        IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        OptionSelector<Schemes> selector = new () { Title = "Scheme", CanFocus = true };
        int[] changedCount = [0];
        selector.ValueChanged += (_, _) => { changedCount [0]++; };

        List<View> subItems = [];

        for (var i = 0; i < totalItems; i++)
        {
            if (i == selectorIndex)
            {
                subItems.Add (new MenuItem { Id = "selectorItem", HelpText = "Pick a scheme", CommandView = selector });
            }
            else
            {
                subItems.Add (new MenuItem { Id = $"item{i}", Title = $"_Item {i}" });
            }
        }

        Menu subMenu = new (subItems) { Id = "subMenu" };

        MenuItem r1 = new () { Id = "r1", Title = "_New" };
        MenuItem rSub = new () { Id = "rSub", Title = "_Themes", SubMenu = subMenu };
        MenuItem r3 = new () { Id = "r3", Title = "_Quit" };
        Menu root = new ([r1, rSub, r3]) { Id = "root" };
        MenuBarItem bi = new () { Id = "bi", Title = "_File" };
        PopoverMenu pm = new ();
        bi.PopoverMenu = pm;
        pm.Root = root;
        MenuBar bar = new () { Id = "bar" };
        bar.Add (bi);

        View host = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        host.Add (bar);

        // A focusable peer outside the popover: if navigation escapes the submenu, focus lands here
        // instead of staying inside the submenu.
        View peer = new () { Id = "peer", CanFocus = true };
        host.Add (peer);
        ((View)runnable).Add (host);
        app.Begin (runnable);

        app.InjectKey (MenuBar.DefaultKey);
        app.InjectKey (Key.CursorDown); // r1 -> rSub
        app.InjectKey (Key.CursorRight); // open submenu

        return (app, subMenu, selector, changedCount);
    }

    [Fact]
    public void Embedded_OptionSelector_Root_Down_Navigates_And_Space_Selects ()
    {
        var (app, selector, changedCount) = CreateOpenedRootWithEmbeddedSelector ();
        using (app)
        {
            // Navigate within the embedded selector: Base -> Menu -> Dialog -> Runnable -> Error
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);

            app.InjectKey (Key.Space);

            Assert.Equal (Schemes.Error, selector.Value);
            Assert.Equal (1, changedCount [0]);
        }
    }

    [Fact]
    public void Embedded_OptionSelector_Root_Up_Wraps_Within_Control ()
    {
        var (app, selector, changedCount) = CreateOpenedRootWithEmbeddedSelector ();
        using (app)
        {
            // Starting on Base (first), Up wraps within the selector to Error (last).
            app.InjectKey (Key.CursorUp);
            app.InjectKey (Key.Space);

            Assert.Equal (Schemes.Error, selector.Value);
            Assert.Equal (1, changedCount [0]);
        }
    }

    [Fact]
    public void Embedded_OptionSelector_Last_Item_Down_From_Last_Option_Wraps_To_First_Item ()
    {
        var (app, root, selector, _) = CreateOpenedMenuWithEmbeddedSelector (1, 2);
        using (app)
        {
            // item0 -> into the selector's first option
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Base", FocusedId (app));

            // Down through the remaining options to the last one
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Error", FocusedId (app));

            // Down past the selector's last option wraps to the first menu item; the menu stays open
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("item0", FocusedId (app));
            Assert.True (root.Visible);
            Assert.Equal (Schemes.Base, selector.Value);
        }
    }

    [Fact]
    public void Embedded_OptionSelector_First_Item_Up_From_First_Option_Wraps_To_Last_Item ()
    {
        var (app, root, selector, _) = CreateOpenedMenuWithEmbeddedSelector (0, 2);
        using (app)
        {
            // Opening the menu focuses the selector's first option
            Assert.Equal ("Base", FocusedId (app));

            // Up from the selector's first option wraps to the last menu item; the menu stays open
            app.InjectKey (Key.CursorUp);
            Assert.Equal ("item1", FocusedId (app));
            Assert.True (root.Visible);
        }
    }

    [Fact]
    public void Embedded_OptionSelector_Middle_Item_Down_Past_Last_Option_Moves_To_Next_Item ()
    {
        var (app, root, selector, _) = CreateOpenedMenuWithEmbeddedSelector (1, 3);
        using (app)
        {
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Base", FocusedId (app));

            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Error", FocusedId (app));

            // Down past the selector's last option moves to the next menu item; the menu stays open
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("item2", FocusedId (app));
            Assert.True (root.Visible);
        }
    }

    [Fact]
    public void Embedded_OptionSelector_Middle_Item_Up_From_First_Option_Moves_To_Previous_Item ()
    {
        var (app, root, selector, _) = CreateOpenedMenuWithEmbeddedSelector (1, 3);
        using (app)
        {
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Base", FocusedId (app));

            // Up from the selector's first option moves to the previous menu item; the menu stays open
            app.InjectKey (Key.CursorUp);
            Assert.Equal ("item0", FocusedId (app));
            Assert.True (root.Visible);
        }
    }

    [Fact]
    public void Embedded_OptionSelector_Each_Arrow_Moves_Exactly_One_Option ()
    {
        var (app, root, selector, changedCount) = CreateOpenedMenuWithEmbeddedSelector (1, 3);
        using (app)
        {
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Base", FocusedId (app));

            // Each Down moves exactly one option
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Accent", FocusedId (app));

            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Dialog", FocusedId (app));

            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Menu", FocusedId (app));

            // Up moves back exactly one option
            app.InjectKey (Key.CursorUp);
            Assert.Equal ("Dialog", FocusedId (app));

            Assert.True (root.Visible);

            // Navigation never changes the selector's value
            Assert.Equal (Schemes.Base, selector.Value);
            Assert.Equal (0, changedCount [0]);
        }
    }

    [Fact]
    public void Embedded_OptionSelector_In_Submenu_Down_From_Last_Option_Wraps_To_First_Item ()
    {
        var (app, subMenu, selector, _) = CreateOpenedSubmenuWithEmbeddedSelector (1, 2);
        using (app)
        {
            // sPlain -> into the selector's first option
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Base", FocusedId (app));

            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Error", FocusedId (app));

            // Down past the selector's last option wraps to the first submenu item; focus must
            // stay inside the submenu and the submenu must stay open
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("item0", FocusedId (app));
            Assert.True (subMenu.Visible);
        }
    }

    [Fact]
    public void Embedded_OptionSelector_In_Submenu_Up_From_First_Option_Wraps_To_Last_Item ()
    {
        var (app, subMenu, selector, _) = CreateOpenedSubmenuWithEmbeddedSelector (0, 2);
        using (app)
        {
            // Opening the submenu focuses the selector's first option
            Assert.Equal ("Base", FocusedId (app));

            // Up from the selector's first option wraps to the last submenu item; focus must stay
            // inside the submenu and the submenu must stay open
            app.InjectKey (Key.CursorUp);
            Assert.Equal ("item1", FocusedId (app));
            Assert.True (subMenu.Visible);
        }
    }

    [Fact]
    public void Embedded_OptionSelector_In_Submenu_Middle_Down_Past_Last_Option_Moves_To_Next_Item ()
    {
        var (app, subMenu, selector, _) = CreateOpenedSubmenuWithEmbeddedSelector (1, 3);
        using (app)
        {
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Base", FocusedId (app));

            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Error", FocusedId (app));

            // Down past the selector's last option moves to the next submenu item
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("item2", FocusedId (app));
            Assert.True (subMenu.Visible);
        }
    }

    [Fact]
    public void Embedded_OptionSelector_In_Submenu_Middle_Up_From_First_Option_Moves_To_Previous_Item ()
    {
        var (app, subMenu, selector, _) = CreateOpenedSubmenuWithEmbeddedSelector (1, 3);
        using (app)
        {
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("Base", FocusedId (app));

            // Up from the selector's first option moves to the previous submenu item
            app.InjectKey (Key.CursorUp);
            Assert.Equal ("item0", FocusedId (app));
            Assert.True (subMenu.Visible);
        }
    }
}

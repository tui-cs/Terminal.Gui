namespace ViewsTests;

public class MenuWrapAroundTests
{
    private static string Focused (IApplication app) => app.Navigation?.GetFocused ()?.Id ?? "<null>";

    private static (IApplication app, Menu root, Menu subMenu) CreateOpenedSubmenu (IEnumerable<View>? subItems = null)
    {
        VirtualTimeProvider time = new ();
        IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        Menu subMenu = new (subItems ?? [new MenuItem { Id = "s1", Title = "S_1" },
                                         new MenuItem { Id = "s2", Title = "S_2" },
                                         new MenuItem { Id = "s3", Title = "S_3" }])
        { Id = "subMenu" };

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
        ((View)runnable).Add (host);
        app.Begin (runnable);

        app.InjectKey (MenuBar.DefaultKey);
        app.InjectKey (Key.CursorDown); // r1 -> rSub
        app.InjectKey (Key.CursorRight); // open submenu

        return (app, root, subMenu);
    }

    private static (IApplication app, Menu root) CreateOpenedRoot ()
    {
        VirtualTimeProvider time = new ();
        IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem r1 = new () { Id = "r1", Title = "_New" };
        MenuItem r2 = new () { Id = "r2", Title = "_Open" };
        MenuItem r3 = new () { Id = "r3", Title = "_Quit" };
        Menu root = new ([r1, r2, r3]) { Id = "root" };
        MenuBarItem bi = new () { Id = "bi", Title = "_File" };
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

        return (app, root);
    }

    private static (IApplication app, Menu menu) CreateStandaloneMenu (IEnumerable<View>? items = null, bool withPeer = false)
    {
        VirtualTimeProvider time = new ();
        IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        Menu menu = new (items ?? [new MenuItem { Id = "m1", Title = "M_1" },
                                   new MenuItem { Id = "m2", Title = "M_2" },
                                   new MenuItem { Id = "m3", Title = "M_3" }])
        { Id = "standaloneMenu" };

        View host = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        host.Add (menu);
        if (withPeer)
        {
            View peer = new () { Id = "peer", CanFocus = true };
            host.Add (peer);
        }
        ((View)runnable).Add (host);
        app.Begin (runnable);

        menu.SetFocus ();

        return (app, menu);
    }

    [Fact]
    public void Submenu_Down_From_Last_Wraps_To_First ()
    {
        var (app, _, subMenu) = CreateOpenedSubmenu ();
        using (app)
        {
            app.InjectKey (Key.CursorDown); // s1 -> s2
            app.InjectKey (Key.CursorDown); // s2 -> s3
            Assert.Equal ("s3", Focused (app));

            app.InjectKey (Key.CursorDown);
            Assert.Equal ("s1", Focused (app));
            Assert.True (subMenu.Visible);
        }
    }

    [Fact]
    public void Submenu_Up_From_First_Wraps_To_Last ()
    {
        var (app, _, subMenu) = CreateOpenedSubmenu ();
        using (app)
        {
            Assert.Equal ("s1", Focused (app));

            app.InjectKey (Key.CursorUp);
            Assert.Equal ("s3", Focused (app));
            Assert.True (subMenu.Visible);
        }
    }

    [Fact]
    public void Root_Down_From_Last_Wraps_To_First ()
    {
        var (app, _) = CreateOpenedRoot ();
        using (app)
        {
            app.InjectKey (Key.CursorDown);
            app.InjectKey (Key.CursorDown);
            Assert.Equal ("r3", Focused (app));

            app.InjectKey (Key.CursorDown);
            Assert.Equal ("r1", Focused (app));
        }
    }

    [Fact]
    public void Root_Up_From_First_Wraps_To_Last ()
    {
        var (app, _) = CreateOpenedRoot ();
        using (app)
        {
            app.InjectKey (Key.CursorUp);
            Assert.Equal ("r3", Focused (app));
        }
    }

    [Fact]
    public void Standalone_Menu_Down_From_Last_Wraps_To_First ()
    {
        var (app, _) = CreateStandaloneMenu ();
        using (app)
        {
            Assert.Equal ("m1", Focused (app));

            app.InjectKey (Key.CursorDown); // m1 -> m2
            app.InjectKey (Key.CursorDown); // m2 -> m3
            Assert.Equal ("m3", Focused (app));

            app.InjectKey (Key.CursorDown); // wrap m3 -> m1
            Assert.Equal ("m1", Focused (app));
        }
    }

    [Fact]
    public void Standalone_Menu_Up_From_First_Wraps_To_Last ()
    {
        var (app, _) = CreateStandaloneMenu ();
        using (app)
        {
            Assert.Equal ("m1", Focused (app));

            app.InjectKey (Key.CursorUp); // wrap m1 -> m3
            Assert.Equal ("m3", Focused (app));
        }
    }

    [Fact]
    public void Standalone_Menu_With_Peer_Down_From_Last_Wraps_To_First ()
    {
        var (app, _) = CreateStandaloneMenu (withPeer: true);
        using (app)
        {
            Assert.Equal ("m1", Focused (app));

            app.InjectKey (Key.CursorDown); // m1 -> m2
            app.InjectKey (Key.CursorDown); // m2 -> m3
            Assert.Equal ("m3", Focused (app));

            app.InjectKey (Key.CursorDown); // wrap m3 -> m1, must not escape to the peer
            Assert.Equal ("m1", Focused (app));
        }
    }

    [Fact]
    public void Standalone_Menu_With_Peer_Up_From_First_Wraps_To_Last ()
    {
        var (app, _) = CreateStandaloneMenu (withPeer: true);
        using (app)
        {
            Assert.Equal ("m1", Focused (app));

            app.InjectKey (Key.CursorUp); // wrap m1 -> m3, must not escape to the peer
            Assert.Equal ("m3", Focused (app));
        }
    }

    [Fact]
    public void Submenu_Wrap_Skips_Separator_And_Disabled ()
    {
        MenuItem s1 = new () { Id = "s1", Title = "S_1" };
        MenuItem sDisabled = new () { Id = "sDisabled", Title = "S_Disabled", Enabled = false };
        Line sep = new () { Id = "sep" };
        MenuItem sLast = new () { Id = "sLast", Title = "S_Last" };

        var (app, _, subMenu) = CreateOpenedSubmenu ([s1, sDisabled, sep, sLast]);
        using (app)
        {
            Assert.Equal ("s1", Focused (app));

            app.InjectKey (Key.CursorDown);
            Assert.Equal ("sLast", Focused (app));
            Assert.True (subMenu.Visible);

            app.InjectKey (Key.CursorDown);
            Assert.Equal ("s1", Focused (app));
            Assert.True (subMenu.Visible);

            app.InjectKey (Key.CursorUp);
            Assert.Equal ("sLast", Focused (app));
            Assert.True (subMenu.Visible);
        }
    }

    [Fact]
    public void Nested_Submenu_Wrap ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem n1 = new () { Id = "n1", Title = "N_1" };
        MenuItem n2 = new () { Id = "n2", Title = "N_2" };
        Menu nested = new ([n1, n2]) { Id = "nested" };
        MenuItem s1 = new () { Id = "s1", Title = "S_1", SubMenu = nested };
        MenuItem s2 = new () { Id = "s2", Title = "S_2" };
        Menu subMenu = new ([s1, s2]) { Id = "subMenu" };
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
        ((View)runnable).Add (host);
        app.Begin (runnable);

        app.InjectKey (MenuBar.DefaultKey);
        app.InjectKey (Key.CursorDown); // r1 -> rSub
        app.InjectKey (Key.CursorRight); // open submenu
        Assert.Equal ("s1", Focused (app));

        app.InjectKey (Key.CursorRight); // open nested
        Assert.Equal ("n1", Focused (app));
        Assert.True (nested.Visible);

        app.InjectKey (Key.CursorDown); // n1 -> n2
        Assert.Equal ("n2", Focused (app));
        Assert.True (nested.Visible);

        app.InjectKey (Key.CursorDown); // wrap n2 -> n1
        Assert.Equal ("n1", Focused (app));
        Assert.True (nested.Visible);

        app.InjectKey (Key.CursorUp); // wrap n1 -> n2
        Assert.Equal ("n2", Focused (app));
        Assert.True (nested.Visible);
    }

    [Fact]
    public void Submenu_With_Single_Focusable_Item_Stays ()
    {
        var (app, _, subMenu) = CreateOpenedSubmenu ([new MenuItem { Id = "only", Title = "Only" }]);
        using (app)
        {
            Assert.Equal ("only", Focused (app));
            Assert.True (subMenu.Visible);

            app.InjectKey (Key.CursorDown);
            Assert.Equal ("only", Focused (app));
            Assert.True (subMenu.Visible);

            app.InjectKey (Key.CursorUp);
            Assert.Equal ("only", Focused (app));
            Assert.True (subMenu.Visible);
        }
    }

    [Fact]
    public void Wrap_Onto_Submenu_Item_Does_Not_Open_Submenu ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        Menu subMenu = new ([new MenuItem { Id = "s1", Title = "S_1" },
                             new MenuItem { Id = "s2", Title = "S_2" }])
        { Id = "subMenu" };

        MenuItem r1 = new () { Id = "r1", Title = "_New" };
        MenuItem r2 = new () { Id = "r2", Title = "_Open" };
        MenuItem rSub = new () { Id = "rSub", Title = "_Themes", SubMenu = subMenu };
        Menu root = new ([r1, r2, rSub]) { Id = "root" };
        MenuBarItem bi = new () { Id = "bi", Title = "_File" };
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
        Assert.Equal ("r1", Focused (app));

        app.InjectKey (Key.CursorUp); // wrap r1 -> rSub
        Assert.Equal ("rSub", Focused (app));

        // The wrap must not open (or close) any submenu
        Assert.False (subMenu.Visible);
        Assert.True (root.Visible);
    }

    [Fact]
    public void Wrap_From_Submenu_Item_Closes_Its_Open_Submenu ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        Menu subMenu = new ([new MenuItem { Id = "s1", Title = "S_1" },
                             new MenuItem { Id = "s2", Title = "S_2" }])
        { Id = "subMenu" };

        MenuItem r1 = new () { Id = "r1", Title = "_New" };
        MenuItem rSub = new () { Id = "rSub", Title = "_Themes", SubMenu = subMenu };
        Menu root = new ([r1, rSub]) { Id = "root" };
        MenuBarItem bi = new () { Id = "bi", Title = "_File" };
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
        Assert.Equal ("r1", Focused (app));

        app.InjectKey (Key.CursorDown); // r1 -> rSub; a non-wrap move opens its submenu
        Assert.Equal ("rSub", Focused (app));
        Assert.True (subMenu.Visible); // pins the base (non-wrap) auto-open behavior

        app.InjectKey (Key.CursorDown); // wrap rSub -> r1
        Assert.Equal ("r1", Focused (app));

        // The wrap must still close the submenu left open on rSub
        Assert.False (subMenu.Visible);
        Assert.True (root.Visible);
    }

    [Fact]
    public void Wrap_From_First_Submenu_Item_Up_Closes_Its_Open_Submenu ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        Menu subMenu = new ([new MenuItem { Id = "s1", Title = "S_1" },
                             new MenuItem { Id = "s2", Title = "S_2" }])
        { Id = "subMenu" };

        MenuItem rSub = new () { Id = "rSub", Title = "_Themes", SubMenu = subMenu };
        MenuItem r2 = new () { Id = "r2", Title = "_Open" };
        Menu root = new ([rSub, r2]) { Id = "root" };
        MenuBarItem bi = new () { Id = "bi", Title = "_File" };
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
        Assert.Equal ("rSub", Focused (app));

        // Opening the menu selects rSub, which opens its submenu (base behavior)
        Assert.True (subMenu.Visible);

        app.InjectKey (Key.CursorUp); // wrap rSub -> r2
        Assert.Equal ("r2", Focused (app));

        // The wrap must still close the submenu left open on rSub
        Assert.False (subMenu.Visible);
        Assert.True (root.Visible);
    }
}

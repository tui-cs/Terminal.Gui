
namespace ViewsTests;

public class MenuBarItemTests
{
    [Fact]
    public void Constructors_Defaults ()
    {
        var menuBarItem = new MenuBarItem ();
        Assert.Null (menuBarItem.PopoverMenu);
        Assert.Null (menuBarItem.TargetView);

        menuBarItem = new MenuBarItem (null, Command.NotBound, null, null);
        Assert.Null (menuBarItem.PopoverMenu);
        Assert.Null (menuBarItem.TargetView);
    }

    [Fact]
    public void Command_Activate_Executes_Action ()
    {
        MenuBarItem menuBarItem = new () { Title = "Test" };
        var actionFired = 0;
        menuBarItem.Action = () => actionFired++;

        // Accept executes Action
        menuBarItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, actionFired);

        menuBarItem.Dispose ();
    }

    [Fact]
    public void Command_Activate_Activates_PopoverMenu ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        MenuBarItem? menuBarItem = new ();
        menuBarItem.EnableForDesign ();
        hostView.Add (menuBarItem);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.False (menuBarItem.PopoverMenuOpen);

        menuBarItem.InvokeCommand (Command.Activate);

        Assert.True (menuBarItem.PopoverMenuOpen);
        Assert.True (menuBarItem.PopoverMenu?.Visible);
    }

    [Fact]
    public void Command_Accept_Does_Not_Execute_Action ()
    {
        MenuBarItem menuBarItem = new () { Title = "Test" };
        var actionFired = 0;
        menuBarItem.Action = () => actionFired++;

        menuBarItem.InvokeCommand (Command.Accept);

        Assert.Equal (0, actionFired);

        menuBarItem.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Command_HotKey_Executes_Action ()
    {
        MenuBarItem menuBarItem = new () { Title = "_Test" };
        var actionFired = 0;
        menuBarItem.Action = () => actionFired++;

        // HotKey invokes Accept
        menuBarItem.InvokeCommand (Command.HotKey);

        Assert.Equal (1, actionFired);

        menuBarItem.Dispose ();
    }


    [Fact]
    public void PopoverMenu_Is_Registered_By_Init ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        MenuBarItem? menuBarItem = new ();
        menuBarItem.EnableForDesign ();
        hostView.Add (menuBarItem);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Registration is now lazy — popover is not registered until it is opened
        Assert.False (app.Popovers?.IsRegistered (menuBarItem.PopoverMenu));

        // Open the popover → triggers auto-registration via MakeVisible
        menuBarItem.InvokeCommand (Command.Activate);
        Assert.True (app.Popovers?.IsRegistered (menuBarItem.PopoverMenu));
    }

    [Fact]
    public void PopoverMenu_Is_Registered_By_Activate ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        MenuBarItem? menuBarItem = new ();
        menuBarItem.EnableForDesign ();

        ((View)runnable).Add (hostView);
        app.Begin (runnable);
        Assert.False (app.Popovers?.IsRegistered (menuBarItem.PopoverMenu));

        hostView.Add (menuBarItem);

        menuBarItem.InvokeCommand (Command.Activate);

        Assert.True (app.Popovers?.IsRegistered (menuBarItem.PopoverMenu));
    }

    [Fact]
    public void PopoverMenu_Is_Registered_By_Set ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        MenuBarItem? menuBarItem = new ();

        ((View)runnable).Add (hostView);
        app.Begin (runnable);
        Assert.False (app.Popovers?.IsRegistered (menuBarItem.PopoverMenu));
        hostView.Add (menuBarItem);
        Assert.False (app.Popovers?.IsRegistered (menuBarItem.PopoverMenu));

        menuBarItem.EnableForDesign ();

        // Registration is now lazy — popover is not registered until it is opened
        Assert.False (app.Popovers?.IsRegistered (menuBarItem.PopoverMenu));

        // Open the popover → triggers auto-registration via MakeVisible
        menuBarItem.InvokeCommand (Command.Activate);
        Assert.True (app.Popovers?.IsRegistered (menuBarItem.PopoverMenu));
    }
}

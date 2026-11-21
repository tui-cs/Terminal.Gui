using System.ComponentModel;
using Microsoft.Extensions.Logging;
using UnitTests.Parallelizable;
using Terminal.Gui.Tracing;
using UnitTests;

namespace ViewsTests;

public class MenuItemTests (ITestOutputHelper output)
{
    /// <summary>Test view that exposes AddCommand publicly for testing.</summary>
    private class TestTargetView : View
    {
        public void RegisterCommand (Command command, Func<bool?> impl) => AddCommand (command, impl);
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var menuItem = new MenuItem ();
        Assert.Null (menuItem.TargetView);

        menuItem = new MenuItem (null, Command.NotBound);
        Assert.Null (menuItem.TargetView);
    }

    [Fact]
    public void Command_Accept_Does_Not_Execute_Action ()
    {
        MenuItem menuItem = new () { Title = "Test" };
        var actionFired = false;
        menuItem.Action = () => actionFired = true;

        // Accept executes Action
        menuItem.InvokeCommand (Command.Accept);

        Assert.False (actionFired);

        menuItem.Dispose ();
    }

    [Fact]
    public void Command_HotKey_Executes_Action ()
    {
        MenuItem menuItem = new () { Title = "_Test" };
        var actionFired = false;
        menuItem.Action = () => actionFired = true;

        // HotKey invokes Accept
        menuItem.InvokeCommand (Command.HotKey);

        Assert.True (actionFired);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Activate_Raises_Activating ()
    {
        MenuItem menuItem = new () { Title = "Test" };
        var activatingCount = 0;
        menuItem.Activating += (_, _) => activatingCount++;

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, activatingCount);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_HotKey_Raises_Activating ()
    {
        MenuItem menuItem = new () { Title = "_Test" };
        var activatingCount = 0;
        menuItem.Activating += (_, _) => activatingCount++;

        menuItem.InvokeCommand (Command.HotKey);

        Assert.True (activatingCount > 0);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_SubMenu_NotNull_Sets_RightArrow_On_KeyView ()
    {
        MenuItem menuItem = new () { Title = "Parent" };
        Menu subMenu = new ();

        menuItem.SubMenu = subMenu;

        Assert.Equal (Glyphs.RightArrow.ToString (), menuItem.KeyView.Text);

        menuItem.Dispose ();
    }

    [Fact]
    public void MenuItem_SubMenu_Null_Restores_Key_Text ()
    {
        MenuItem menuItem = new () { Title = "Parent", Key = Key.F5 };
        Menu subMenu = new ();

        menuItem.SubMenu = subMenu;

        Assert.Equal (Glyphs.RightArrow.ToString (), menuItem.KeyView.Text);

        menuItem.SubMenu = null;

        Assert.Equal ($"{Key.F5}", menuItem.KeyView.Text);

        menuItem.Dispose ();
    }

    [Fact]
    public void MenuItem_SubMenu_KeyView_SuperView_Behavior ()
    {
        MenuItem menuItem = new () { Title = "Parent", Key = Key.F5.WithCtrl };
        Menu subMenu = new ();

        Assert.Equal ($"{Key.F5.WithCtrl}", menuItem.KeyView.Text);
        Assert.Same (menuItem, menuItem.KeyView.SuperView);

        menuItem.SubMenu = subMenu;

        Assert.Equal (Glyphs.RightArrow.ToString (), menuItem.KeyView.Text);
        Assert.Same (menuItem, menuItem.KeyView.SuperView);

        menuItem.SubMenu = null;

        Assert.Equal ($"{Key.F5.WithCtrl}", menuItem.KeyView.Text);
        Assert.Same (menuItem, menuItem.KeyView.SuperView);

        menuItem.Key = Key.Empty;
        Assert.Equal (string.Empty, menuItem.KeyView.Text);
        Assert.Null (menuItem.KeyView.SuperView);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_MouseEnter_Sets_Focus ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem item1 = new () { Title = "First" };
        MenuItem item2 = new () { Title = "Second" };

        Menu menu = new ([item1, item2]);
        (runnable as View)?.Add (menu);
        app.Begin (runnable);

        // First item should have focus initially
        Assert.True (item1.HasFocus);

        // Simulate mouse entering second item (internal method visible via InternalsVisibleTo)
        item2.NewMouseEnterEvent (new CancelEventArgs ());

        Assert.True (item2.HasFocus);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Command_Property_Sets_Title_And_HelpText ()
    {
        MenuItem menuItem = new ();

        // Verify Command property round-trips correctly
        menuItem.Command = Command.Accept;
        Assert.Equal (Command.Accept, menuItem.Command);

        menuItem.Command = Command.Activate;
        Assert.Equal (Command.Activate, menuItem.Command);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Accept_Does_Not_Invoke_Command_On_TargetView ()
    {
        TestTargetView targetView = new () { Title = "Target" };
        var commandInvoked = false;

        targetView.RegisterCommand (Command.Save,
                                    () =>
                                    {
                                        commandInvoked = true;

                                        return true;
                                    });

        // Bind a key so the MenuItem constructor can find it
        targetView.HotKeyBindings.Add (Key.S.WithCtrl, Command.Save);

        MenuItem menuItem = new (targetView, Command.Save);

        menuItem.InvokeCommand (Command.Accept);

        Assert.False (commandInvoked);

        menuItem.Dispose ();
        targetView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Dispose_Disposes_SubMenu ()
    {
        MenuItem menuItem = new () { Title = "Parent" };
        Menu subMenu = new ();

        menuItem.SubMenu = subMenu;

        Assert.NotNull (menuItem.SubMenu);

        menuItem.Dispose ();

        Assert.Null (menuItem.SubMenu);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Constructors_Defaults_Extended ()
    {
        TestTargetView targetView = new () { Title = "Target" };
        targetView.RegisterCommand (Command.Save, () => true);
        targetView.HotKeyBindings.Add (Key.S.WithCtrl, Command.Save);

        Menu subMenu = new ();

        MenuItem menuItem = new (targetView, Command.Save, "Save File", "Saves the current file", subMenu);

        Assert.Equal (targetView, menuItem.TargetView);
        Assert.Equal (Command.Save, menuItem.Command);
        Assert.Equal ("Save File", menuItem.Title);
        Assert.Equal ("Saves the current file", menuItem.HelpText);
        Assert.Equal (subMenu, menuItem.SubMenu);

        menuItem.Dispose ();
        targetView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Activate_Bubbles_To_Menu ()
    {
        Menu menu = new ();
        MenuItem menuItem = new () { Title = "Test Item" };
        menu.Add (menuItem);

        var menuActivatingFired = 0;

        menu.Activating += (_, _) => { menuActivatingFired++; };

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, menuActivatingFired);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_With_CommandView_Activate_Bubbles_To_Menu ()
    {
        CheckBox checkBox = new () { Title = "_Check", CanFocus = false };
        Menu menu = new ();
        MenuItem menuItem = new () { Title = "Test", CommandView = checkBox };
        menu.Add (menuItem);

        var menuActivatingFired = 0;

        menu.Activating += (_, _) => { menuActivatingFired++; };

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, menuActivatingFired);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Accept_Handled_Does_Not_Bubble_To_Menu ()
    {
        Menu menu = new ();
        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var menuAcceptingFired = 0;

        menuItem.Accepting += (_, e) => { e.Handled = true; };
        menu.Accepting += (_, _) => { menuAcceptingFired++; };

        menuItem.InvokeCommand (Command.Accept);

        Assert.Equal (0, menuAcceptingFired);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Activate_Handled_Does_Not_Bubble_To_Menu ()
    {
        Menu menu = new ();
        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var menuActivatingFired = 0;

        menuItem.Activating += (_, e) => { e.Handled = true; };
        menu.Activating += (_, _) => { menuActivatingFired++; };

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (0, menuActivatingFired);

        menu.Dispose ();
    }

    #region SubMenu Command Propagation

    // ────────────────────────────────────────────────────────────────────
    //  Tests that commands from MenuItems inside a SubMenu propagate
    //  correctly through Menu/MenuItem:
    //
    //    rootMenu (Menu)
    //      └─ parentMenuItem (MenuItem, has SubMenu)
    //           └─ subMenu (Menu)
    //                └─ childMenuItem (MenuItem)
    //
    //  MenuItem.SubMenu is of type Menu (NOT PopoverMenu). The SubMenu
    //  is NOT in the rootMenu's containment hierarchy — it is set via
    //  a property. Menu.SuperMenuItem links back to the owning MenuItem.
    //  CommandBridge connects SubMenu → parentMenuItem across the
    //  non-containment boundary.
    //
    //  Question: when childMenuItem is activated/accepted, does the
    //  command reach (a) subMenu, (b) parentMenuItem, (c) rootMenu?
    // ────────────────────────────────────────────────────────────────────

    // Claude - Opus 4.6
    /// <summary>
    ///     Activating a child MenuItem inside a SubMenu should bubble to the SubMenu (Menu)
    ///     via CommandsToBubbleUp.
    /// </summary>
    [Fact]
    public void SubMenu_ChildActivate_Bubbles_To_SubMenu ()
    {
        MenuItem childItem = new () { Title = "Child" };
        Menu subMenu = new ([childItem]);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };
        Menu rootMenu = new ([parentItem]);

        var subMenuActivatingCount = 0;
        subMenu.Activating += (_, _) => subMenuActivatingCount++;

        childItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, subMenuActivatingCount);

        rootMenu.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Accepting a child MenuItem inside a SubMenu should bubble to the SubMenu (Menu)
    ///     via CommandsToBubbleUp.
    /// </summary>
    [Fact]
    public void SubMenu_ChildAccept_Bubbles_To_SubMenu ()
    {
        MenuItem childItem = new () { Title = "Child" };
        Menu subMenu = new ([childItem]);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };
        Menu rootMenu = new ([parentItem]);

        var subMenuAcceptingCount = 0;
        subMenu.Accepting += (_, _) => subMenuAcceptingCount++;

        childItem.InvokeCommand (Command.Accept);

        Assert.Equal (1, subMenuAcceptingCount);

        rootMenu.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Accepting a child MenuItem inside a SubMenu fires Accepted on the SubMenu exactly once,
    ///     via bubbling through CommandsToBubbleUp=[Accept].
    /// </summary>
    [Fact]
    public void SubMenu_ChildAccept_Fires_Accepted_On_SubMenu ()
    {
        MenuItem childItem = new () { Title = "Child" };
        Menu subMenu = new ([childItem]);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };
        Menu rootMenu = new ([parentItem]);

        var subMenuAcceptedCount = 0;
        subMenu.Accepted += (_, _) => subMenuAcceptedCount++;

        childItem.InvokeCommand (Command.Accept);

        // Fixed: removed redundant menuItem.Accepting += RaiseAccepted from Menu.OnSubViewAdded.
        // Now only the bubbling path fires Accepted.
        Assert.Equal (1, subMenuAcceptedCount);

        rootMenu.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Activating a child in a SubMenu bridges to parentMenuItem via CommandBridge, and then
    ///     parentMenuItem's activation bubbles to rootMenu (parentMenuItem IS a SubView of rootMenu).
    ///     The SubMenu itself is NOT in rootMenu's containment hierarchy, but the bridge crosses
    ///     that boundary by re-entering the command pipeline on parentMenuItem.
    /// </summary>
    [Fact]
    public void SubMenu_ChildActivate_Bridges_Through_ParentMenuItem_To_RootMenu ()
    {
        MenuItem childItem = new () { Title = "Child" };
        Menu subMenu = new ([childItem]);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };
        Menu rootMenu = new ([parentItem]);

        var rootActivatingCount = 0;
        rootMenu.Activating += (_, _) => rootActivatingCount++;

        childItem.InvokeCommand (Command.Activate);

        // Bridge → parentMenuItem.InvokeCommand → TryBubbleUp → rootMenu.Activating fires.
        Assert.Equal (1, rootActivatingCount);

        rootMenu.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     CommandBridge uses InvokeCommand, which re-enters the full command pipeline on the parent
    ///     MenuItem. This means Activating fires on parentMenuItem (as well as Activated).
    /// </summary>
    [Fact]
    public void SubMenu_ChildActivate_Fires_Activating_On_ParentMenuItem ()
    {
        MenuItem childItem = new () { Title = "Child" };
        Menu subMenu = new ([childItem]);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };
        Menu rootMenu = new ([parentItem]);

        var parentActivatingCount = 0;
        parentItem.Activating += (_, _) => parentActivatingCount++;

        childItem.InvokeCommand (Command.Activate);

        // Bridge calls InvokeCommand → full pipeline → Activating fires on parentMenuItem.
        Assert.Equal (1, parentActivatingCount);

        rootMenu.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     CommandBridge from SubMenu → parent MenuItem fires Activated on the parent
    ///     when a child MenuItem in the SubMenu is activated.
    /// </summary>
    [Fact]
    public void SubMenu_ChildActivate_Bridges_Activated_To_ParentMenuItem ()
    {
        MenuItem childItem = new () { Title = "Child" };
        Menu subMenu = new ([childItem]);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };
        Menu rootMenu = new ([parentItem]);

        var parentActivatedCount = 0;
        parentItem.Activated += (_, _) => parentActivatedCount++;

        childItem.InvokeCommand (Command.Activate);

        // CommandBridge relays SubMenu.Activated → parentMenuItem.RaiseActivated
        Assert.Equal (1, parentActivatedCount);

        rootMenu.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     CommandBridge from SubMenu → parent MenuItem fires Accepted on the parent
    ///     when a child MenuItem in the SubMenu is accepted.
    /// </summary>
    [Fact]
    public void SubMenu_ChildAccept_Bridges_Accepted_To_ParentMenuItem ()
    {
        MenuItem childItem = new () { Title = "Child" };
        Menu subMenu = new ([childItem]);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };
        Menu rootMenu = new ([parentItem]);

        var parentAcceptedCount = 0;
        parentItem.Accepted += (_, _) => parentAcceptedCount++;

        childItem.InvokeCommand (Command.Accept);

        // CommandBridge relays SubMenu.Accepted → parentMenuItem.RaiseAccepted
        Assert.Equal (1, parentAcceptedCount);

        rootMenu.Dispose ();
    }

    #endregion SubMenu Command Propagation

    #region IValue Implementation

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Implements_IValue ()
    {
        MenuItem menuItem = new () { Title = "TestItem" };

        Assert.IsAssignableFrom<IValue> (menuItem);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_GetValue_Returns_Title ()
    {
        MenuItem menuItem = new () { Title = "MyTitle" };

        IValue iValue = menuItem;
        object? value = iValue.GetValue ();

        Assert.Equal ("MyTitle", value);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_GetValue_Returns_Updated_Title ()
    {
        MenuItem menuItem = new () { Title = "Original" };

        IValue iValue = menuItem;
        Assert.Equal ("Original", iValue.GetValue ());

        menuItem.Title = "Updated";
        Assert.Equal ("Updated", iValue.GetValue ());

        menuItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_InvokeCommand_Activate_ContextValue_Contains_Title ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            MenuItem menuItem = new () { Title = "TestItem" };

            menuItem.BeginInit ();
            menuItem.EndInit ();

            ICommandContext? capturedContext = null;
            var activatingCount = 0;

            menuItem.Activating += (_, args) =>
                                   {
                                       activatingCount++;
                                       capturedContext = args.Context;
                                   };

            menuItem.InvokeCommand (Command.Activate);

            Assert.Equal (1, activatingCount);
            Assert.NotNull (capturedContext);
            Assert.Equal ("TestItem", capturedContext!.Value);

            menuItem.Dispose ();
        }
    }

    #endregion IValue Implementation
}

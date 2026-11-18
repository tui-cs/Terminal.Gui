using System.Globalization;
using AppTestHelpers;
using AppTestHelpers.XunitHelpers;

namespace IntegrationTests;

/// <summary>
///     Tests for the PopoverMenu class
/// </summary>
public class PopoverMenuTests : TestsAllDrivers
{
    private readonly TextWriter _out;

    public PopoverMenuTests (ITestOutputHelper outputHelper)
    {
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        _out = new TestOutputWriter (outputHelper);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void EnableForDesign_CreatesMenuItems (string d)
    {
        using AppTestHelper c = With.A<Window> (80, 25, d)
                                        .Then (app =>
                                               {
                                                   PopoverMenu popoverMenu = new ();
                                                   app.TopRunnableView!.Add (popoverMenu);

                                                   // Call EnableForDesign
                                                   View top = app.TopRunnableView;
                                                   bool result = popoverMenu.EnableForDesign (ref top);

                                                   // Should return true
                                                   Assert.True (result);

                                                   // Should have created menu items
                                                   Assert.NotNull (popoverMenu.Root);
                                                   Assert.Equal (7, popoverMenu.Root.SubViews.Count);

                                                   // Should have Cut menu item
                                                   View? cutMenuItem = popoverMenu.Root?.GetMenuItemsOfAllSubMenus ().FirstOrDefault (v => v?.Title == "Cu_t");

                                                   Assert.NotNull (cutMenuItem);
                                               });
    }

    private static readonly object o = new ();

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Activate_Sets_Application_Navigation_Correctly (string d)
    {
        lock (o)
        {
            IApplication? app = null;

            using AppTestHelper c = With.A<Window> (50, 20, d)
                                            .Then (a =>
                                                   {
                                                       app = a;
                                                       PopoverMenu popoverMenu = new () { App = app };

                                                       // Call EnableForDesign
                                                       View top = app.TopRunnableView!;
                                                       popoverMenu.EnableForDesign (ref top);

                                                       var view = new View
                                                       {
                                                           CanFocus = true,
                                                           Height = Dim.Auto (),
                                                           Width = Dim.Auto (),
                                                           Id = "focusableView",
                                                           Text = "View"
                                                       };
                                                       app.TopRunnableView!.Add (view);

                                                       // EnableForDesign sets to true; undo that
                                                       popoverMenu.Visible = false;

                                                       app?.Popovers!.Register (popoverMenu);

                                                       view.SetFocus ();
                                                   })
                                            .AssertFalse (app?.Popovers?.GetActivePopover () is PopoverMenu)
                                            .AssertIsNotType<MenuItem> (app?.Navigation!.GetFocused ())
                                            .ScreenShot ("PopoverMenu initial state", _out)
                                            .Then (_ => app?.Popovers!.Show (app?.Popovers.Popovers.First ()))
                                            .ScreenShot ("After Show", _out)
                                            .AssertTrue (app?.Popovers?.GetActivePopover () is PopoverMenu)
                                            .AssertEqual ("Cu_t", app?.Navigation!.GetFocused ()!.Title);
        }
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void QuitKey_Hides (string d)
    {
        IApplication? app = null;

        using AppTestHelper c = With.A<Window> (50, 20, d)
                                        .Then (a =>
                                               {
                                                   app = a;
                                                   PopoverMenu popoverMenu = new () { App = app };

                                                   // Call EnableForDesign
                                                   View top = app.TopRunnableView!;
                                                   bool result = popoverMenu.EnableForDesign (ref top);

                                                   var view = new View
                                                   {
                                                       CanFocus = true,
                                                       Height = Dim.Auto (),
                                                       Width = Dim.Auto (),
                                                       Id = "focusableView",
                                                       Text = "View"
                                                   };
                                                   app.TopRunnableView!.Add (view);

                                                   // EnableForDesign sets to true; undo that
                                                   popoverMenu.Visible = false;

                                                   app?.Popovers!.Register (popoverMenu);

                                                   view.SetFocus ();
                                               })
                                        .ScreenShot ("PopoverMenu initial state", _out)
                                        .AssertFalse (app?.Popovers?.GetActivePopover () is PopoverMenu)
                                        .Then (_ => app?.Popovers!.Show (app?.Popovers.Popovers.First ()))
                                        .ScreenShot ("After Show", _out)
                                        .AssertTrue (app?.Popovers?.GetActivePopover () is PopoverMenu)
                                        .KeyDown (Application.GetDefaultKey (Command.Quit))
                                        .ScreenShot ($"After {Application.GetDefaultKey (Command.Quit)}", _out)
                                        .AssertFalse (app?.Popovers!.Popovers.Cast<PopoverMenu> ().FirstOrDefault ()!.Visible)
                                        .AssertNull (app?.Popovers!.GetActivePopover ())
                                        .AssertTrue (app?.TopRunnable!.IsRunning);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void QuitKey_Restores_Focus_Correctly (string d)
    {
        IApplication? app = null;

        using AppTestHelper c = With.A<Window> (50, 20, d)
                                        .Then (a =>
                                               {
                                                   app = a;
                                                   PopoverMenu popoverMenu = new () { App = app };

                                                   // Call EnableForDesign
                                                   View top = app.TopRunnableView!;
                                                   bool result = popoverMenu.EnableForDesign (ref top);

                                                   var view = new View
                                                   {
                                                       CanFocus = true,
                                                       Height = Dim.Auto (),
                                                       Width = Dim.Auto (),
                                                       Id = "focusableView",
                                                       Text = "View"
                                                   };
                                                   app.TopRunnableView!.Add (view);

                                                   // EnableForDesign sets to true; undo that
                                                   popoverMenu.Visible = false;

                                                   app?.Popovers!.Register (popoverMenu);

                                                   view.SetFocus ();
                                               })
                                        .ScreenShot ("PopoverMenu initial state", _out)
                                        .AssertFalse (app?.Popovers?.GetActivePopover () is PopoverMenu)
                                        .AssertIsNotType<MenuItem> (app?.Navigation!.GetFocused ())
                                        .Then (_ => app?.Popovers!.Show (app?.Popovers.Popovers.First ()))
                                        .ScreenShot ("After Show", _out)
                                        .AssertTrue (app?.Popovers?.GetActivePopover () is PopoverMenu)
                                        .AssertIsType<MenuItem> (app?.Navigation!.GetFocused ())
                                        .KeyDown (Application.GetDefaultKey (Command.Quit))
                                        .ScreenShot ($"After {Application.GetDefaultKey (Command.Quit)}", _out)
                                        .AssertFalse (app?.Popovers?.GetActivePopover () is PopoverMenu)
                                        .AssertIsNotType<MenuItem> (app?.Navigation!.GetFocused ());
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void MenuBarItem_With_QuitKey_Open_QuitKey_Does_Not_Quit_App (string d)
    {
        IApplication? app = null;

        using AppTestHelper c = With.A<Window> (50, 20, d)
                                        .Then (a =>
                                               {
                                                   app = a;
                                                   PopoverMenu popoverMenu = new () { App = app };

                                                   // Call EnableForDesign
                                                   View top = app.TopRunnableView!;
                                                   bool result = popoverMenu.EnableForDesign (ref top);

                                                   var view = new View
                                                   {
                                                       CanFocus = true,
                                                       Height = Dim.Auto (),
                                                       Width = Dim.Auto (),
                                                       Id = "focusableView",
                                                       Text = "View"
                                                   };
                                                   app.TopRunnableView!.Add (view);

                                                   // EnableForDesign sets to true; undo that
                                                   popoverMenu.Visible = false;

                                                   app?.Popovers!.Register (popoverMenu);

                                                   view.SetFocus ();
                                               })
                                        .AssertIsNotType<MenuItem> (app?.Navigation!.GetFocused ())
                                        .ScreenShot ("PopoverMenu initial state", _out)
                                        .Then (_ => app?.Popovers!.Show (app?.Popovers.Popovers.First ()))
                                        .ScreenShot ("PopoverMenu after Show", _out)
                                        .AssertEqual ("Cu_t", app?.Navigation!.GetFocused ()!.Title)
                                        .AssertTrue (app?.TopRunnable!.IsRunning)
                                        .KeyDown (Application.GetDefaultKey (Command.Quit))
                                        .ScreenShot ($"After {Application.GetDefaultKey (Command.Quit)}", _out)
                                        .AssertFalse (app?.Popovers?.GetActivePopover () is PopoverMenu)
                                        .AssertTrue (app?.TopRunnable!.IsRunning);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Not_Active_DoesNotEat_Space (string d)
    {
        var spaceKeyDownCount = 0;

        var testView = new View { CanFocus = true, Id = "testView" };

        testView.KeyDown += (sender, key) =>
                            {
                                if (key == Key.Space)
                                {
                                    spaceKeyDownCount++;
                                }
                            };

        IApplication? app = null;

        using AppTestHelper c = With.A<Window> (50, 20, d)
                                        .Then (a =>
                                               {
                                                   app = a;
                                                   PopoverMenu popoverMenu = new () { App = app };
                                                   View top = app.TopRunnableView!;
                                                   popoverMenu.EnableForDesign (ref top);
                                                   app?.Popovers!.Register (popoverMenu);
                                               })
                                        .Add (testView)
                                        .Focus (testView)
                                        .KeyDown (Key.Space)
                                        .AssertEqual (1, spaceKeyDownCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Not_Active_DoesNotEat_Enter (string d)
    {
        var enterKeyDownCount = 0;

        var testView = new View { CanFocus = true, Id = "testView" };

        testView.KeyDown += (sender, key) =>
                            {
                                if (key == Key.Enter)
                                {
                                    enterKeyDownCount++;
                                }
                            };

        IApplication? app = null;

        using AppTestHelper c = With.A<Window> (50, 20, d)
                                        .Then (a =>
                                               {
                                                   app = a;
                                                   PopoverMenu popoverMenu = new () { App = app };
                                                   View top = app.TopRunnableView!;
                                                   popoverMenu.EnableForDesign (ref top);
                                                   app?.Popovers!.Register (popoverMenu);
                                               })
                                        .Add (testView)
                                        .Focus (testView)
                                        .KeyDown (Key.Enter)
                                        .AssertEqual (1, enterKeyDownCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Not_Active_DoesNotEat_QuitKey (string d)
    {
        var quitKeyDownCount = 0;

        var testView = new View { CanFocus = true, Id = "testView" };

        testView.KeyDown += (sender, key) =>
                            {
                                if (key == Application.GetDefaultKey (Command.Quit))
                                {
                                    quitKeyDownCount++;
                                }
                            };

        IApplication? app = null;

        using AppTestHelper c = With.A<Window> (50, 20, d)
                                        .Then (a =>
                                               {
                                                   app = a;
                                                   PopoverMenu popoverMenu = new () { App = app };
                                                   View top = app.TopRunnableView!;
                                                   popoverMenu.EnableForDesign (ref top);
                                                   app?.Popovers!.Register (popoverMenu);
                                               })
                                        .Add (testView)
                                        .KeyDown (Application.GetDefaultKey (Command.Quit))
                                        .AssertEqual (1, quitKeyDownCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void ContextMenu_CrashesOnRight (string d)
    {
        var clicked = false;

        MenuItem [] menuItems = [new ("_New File", string.Empty, () => { clicked = true; })];

        IApplication? app = null;

        using AppTestHelper c = With.A<Window> (40, 10, d, _out)
                                        .Then (a => app = a)
                                        .WithContextMenu (new PopoverMenu (menuItems) { App = app })
                                        .ScreenShot ("Before open menu", _out)

                                        // Click in main area inside border
                                        .RightClick (1, 1)
                                        .Then (_ =>
                                               {
                                                   // Test depends on menu having a border
                                                   IPopover? popover = app?.Popovers!.GetActivePopover ();
                                                   Assert.NotNull (popover);
                                                   var popoverMenu = popover as PopoverMenu;
                                                   popoverMenu!.Root!.BorderStyle = LineStyle.Single;
                                               })
                                        .ScreenShot ("After open menu", _out)
                                        .LeftClick (2, 2)
                                        .AssertTrue (clicked);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void ContextMenu_OpenSubmenu (string d)
    {
        var clicked = false;

        MenuItem [] menuItems =
        [
            new ("One", "", null),
            new ("Two", "", null),
            new ("Three", "", null),
            new ("Four",
                 "",
                 new Menu ([
                               new MenuItem ("SubMenu1", "", null),
                               new MenuItem ("SubMenu2", "", () => clicked = true),
                               new MenuItem ("SubMenu3", "", null),
                               new MenuItem ("SubMenu4", "", null),
                               new MenuItem ("SubMenu5", "", null),
                               new MenuItem ("SubMenu6", "", null),
                               new MenuItem ("SubMenu7", "", null)
                           ])),
            new ("Five", "", null),
            new ("Six", "", null)
        ];

        IApplication? app = null;

        using AppTestHelper c = With.A<Window> (40, 10, d)
                                        .Then (a => app = a)
                                        .WithContextMenu (new PopoverMenu (menuItems) { App = app })
                                        .ScreenShot ("Before open menu", _out)

                                        // Click in main area inside border
                                        .RightClick (1, 1)
                                        .ScreenShot ("After open menu", _out)
                                        .KeyDown (Key.CursorDown)
                                        .KeyDown (Key.CursorDown)
                                        .KeyDown (Key.CursorDown)
                                        .KeyDown (Key.CursorRight)
                                        .ScreenShot ("After open submenu", _out)
                                        .KeyDown (Key.CursorDown)
                                        .KeyDown (Key.Enter)
                                        .ScreenShot ("Menu should be closed after selecting", _out);
        Assert.True (clicked);
    }
}

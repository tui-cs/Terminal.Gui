#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Menus", "Illustrates Menu and MenuItem")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Menus")]
public class Menus : Scenario
{
    private EventLog? _eventLog;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Runnable runnable = new ();
        runnable.Title = GetQuitKeyAndName ();

        _eventLog = new EventLog
        {
            Id = "eventLog",
            X = Pos.AnchorEnd (),
            Height = Dim.Fill (),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            BorderStyle = LineStyle.Double,
            Title = "E_vents",
            Arrangement = ViewArrangement.LeftResizable
        };

        MenuDemoHost menuHostView = new ()
        {
            Id = "menuHostView",
            Title = "Menu Demo Host",
            X = 0,
            Y = 0,
            Width = Dim.Fill () - Dim.Width (_eventLog),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        runnable.Add (menuHostView);
        runnable.CommandsToBubbleUp = [Command.Activate];

        menuHostView.Activated += (_, args) =>
                                  {
                                      // If the Activate command is from the Borders menu item, toggle the border style on the MenuHostView
                                      if (args.Value?.TryGetSource (out View? source) is true && source is CheckBox { Id: "menuItemBorders" })
                                      {
                                          menuHostView.BorderStyle = args.Value?.Value as CheckState? == CheckState.Checked ? LineStyle.Double : LineStyle.None;

                                          return;
                                      }

                                      if (args.Value?.TryGetSource (out source) is not true
                                          || source?.SuperView is not OptionSelector<Schemes> { Id: "schemeOptionSelector" } schemeOptionSelector)
                                      {
                                          return;
                                      }

                                      if (schemeOptionSelector.Value is { } scheme)
                                      {
                                          menuHostView.SchemeName = scheme.ToString ();
                                      }
                                  };

        _eventLog.SetViewToLog (runnable);
        _eventLog.SetViewToLog (menuHostView);

        runnable.Initialized += (_, _) =>
                                {
                                    foreach (Menu menu in menuHostView.SubViews.OfType<Menu> ())
                                    {
                                        _eventLog.SetViewToLog (menu);

                                        foreach (MenuItem mi in menu.SubViews.OfType<MenuItem> ())
                                        {
                                            _eventLog.SetViewToLog (mi);
                                            _eventLog.SetViewToLog (mi.CommandView);
                                        }
                                    }
                                };

        runnable.Add (_eventLog);

        app.Run (runnable);
    }

    /// <summary>
    ///     A demo view class that demonstrates Menu and MenuItem as direct SubViews.
    /// </summary>
    public class MenuDemoHost : View
    {
        public MenuDemoHost ()
        {
            CanFocus = true;
            BorderStyle = LineStyle.Dashed;
            CommandsToBubbleUp = [Command.Activate, Command.Accept];
        }

        /// <inheritdoc/>
        public override void EndInit ()
        {
            base.EndInit ();

            Label lastCommandLabel = new () { Title = "_Last Command:", X = 1, Y = 0 };

            View lastCommandText = new () { X = Pos.Right (lastCommandLabel) + 1, Y = Pos.Top (lastCommandLabel), Height = Dim.Auto (), Width = Dim.Auto () };

            Add (lastCommandLabel, lastCommandText);

            AddCommand (Command.Quit,
                        _ =>
                        {
                            App?.RequestStop ();

                            return true;
                        });
            HotKeyBindings.Add (Application.GetDefaultKey (Command.Quit), Command.Quit);

            // --- Test Menu: demonstrates Menu with MenuItems ---
            Label testMenuLabel = new () { Title = "Menu with MenuItems:", X = 1, Y = Pos.Bottom (lastCommandLabel) + 1 };
            Add (testMenuLabel);

            Menu testMenu = new () { Y = Pos.Bottom (testMenuLabel), Id = "testMenu" };
            ConfigureTestMenu (testMenu);
            Add (testMenu);

            Button addAboveButton = new () { X = Pos.Right (testMenu) + 1, Y = Pos.Top (testMenu), Title = "_Add a MenuItem above selected" };

            addAboveButton.Accepting += (_, args) =>
                                        {
                                            testMenu.AddAt (testMenu.SubViews.ToArray ().IndexOf (testMenu.SelectedMenuItem),
                                                            new MenuItem { Title = "_Above", HelpText = "I was added above", Key = Key.A.WithCtrl });
                                            args.Handled = true;
                                        };
            Add (addAboveButton);

            Button addBelowButton = new () { X = Pos.Right (testMenu) + 1, Y = Pos.Bottom (addAboveButton), Title = "Add a MenuItem _below selected" };

            addBelowButton.Accepting += (_, args) =>
                                        {
                                            testMenu.AddAt (testMenu.SubViews.ToArray ().IndexOf (testMenu.SelectedMenuItem) + 1,
                                                            new MenuItem { Title = "_Below", HelpText = "I was added below", Key = Key.B.WithCtrl });
                                            args.Handled = true;
                                        };
            Add (addBelowButton);

            Button removeButton = new () { X = Pos.Right (testMenu) + 1, Y = Pos.Bottom (addBelowButton), Title = "_Remove the selected MenuItem" };

            removeButton.Accepting += (_, args) =>
                                      {
                                          testMenu.Remove (testMenu.SelectedMenuItem)?.Dispose ();
                                          args.Handled = true;
                                      };
            Add (removeButton);

            // --- SubMenu Demo: demonstrates MenuItem.SubMenu for nested menus ---
            Label subMenuLabel = new () { Title = "MenuItem with SubMenu:", X = 1, Y = Pos.Bottom (testMenu) + 1 };
            Add (subMenuLabel);

            Menu subMenuDemo = new () { Id = "subMenuDemo", Y = Pos.Bottom (subMenuLabel) };
            subMenuDemo.EnableForDesign ();
            Add (subMenuDemo);

            // Wire up scenario-specific behavior for the About item
            MenuItem? aboutItem = subMenuDemo.GetMenuItemsOfAllSubMenus (mi => mi.Title == "_About").FirstOrDefault ();

            if (aboutItem is { })
            {
                aboutItem.Activated += (_, _) => MessageBox.Query (App!, "SubMenu Demo", "Demonstrates MenuItem.SubMenu for nested menus.", Strings.btnOk);
            }
        }

        private void ConfigureTestMenu (Menu menu)
        {
            MenuItem menuItem1 = new () { Id = "menuItem1", Title = "Z_igzag", Key = Key.I.WithCtrl, Text = "Gonna zig zag" };

            menuItem1.Activated += (s, _) =>
                                   {
                                       if (s is not MenuItem mi)

                                       {
                                           return;
                                       }

                                       MessageBox.Query (App!,
                                                         "This is a MessageBox",
                                                         $"This is a message box message from {mi.Title} (Value = {mi.GetValue ()})",
                                                         Strings.btnOk);
                                   };

            Line line = new ();

            MenuItem menuItemBorders = new () { Id = "menuItemBorders", Title = "_Borders", Text = "Borders", Key = Key.D4.WithAlt };
            menuItemBorders.CommandView = new CheckBox { Id = "menuItemBorders", Title = menuItemBorders.Title, CanFocus = false };

            // Use Action to set the menu's BorderStyle to Double when the Borders menu item is checked, and None when it is unchecked
            // Note: above we set the MenuHostView to listen for Activate commands and toggle its BorderStyle when the Borders menu item is toggled,
            // this is just to demonstrate that we can also handle this directly in the MenuItem's Action as well
            menuItemBorders.Action += () =>
                                      {
                                          if (menuItemBorders.CommandView is CheckBox cb)
                                          {
                                              menu.BorderStyle = cb.Value == CheckState.Checked ? LineStyle.Double : LineStyle.None;
                                          }
                                      };

            OptionSelector<Schemes> schemeOptionSelector = new () { Id = "schemeOptionSelector", Title = "Scheme", CanFocus = true };

            MenuItem menuItemScheme = new ()
            {
                Id = "menuItemScheme",
                Title = "Scheme",
                Text = "Scheme",
                Key = Key.S.WithCtrl,
                CommandView = schemeOptionSelector
            };

            // Set the Menu's SchemeName to the selected scheme in the OptionSelector
            // Note: above we set the Scheme of the MenuHostView illustrating the commands bubble up
            schemeOptionSelector.ValueChanged += (_, args) =>
                                                 {
                                                     if (args.Value is { } scheme)
                                                     {
                                                         menu.SchemeName = scheme.ToString ();
                                                     }
                                                 };

            menu.Add (menuItem1, line, menuItemBorders, menuItemScheme);
        }
    }
}

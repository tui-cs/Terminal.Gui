using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Bars", "Illustrates Bar views (e.g. StatusBar)")]
[ScenarioCategory ("Controls")]
public class Bars : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Runnable mainWindow = new ();
        mainWindow.Id = "mainWindow";

        mainWindow.IsModalChanged += OnIsModalChanged;

        app.Run (mainWindow);
    }

    // Setting everything up in Loaded handler because we change the
    // QuitKey it only sticks if changed after init
    private void OnIsModalChanged (object sender, EventArgs e)
    {
        if (sender is not Runnable { IsRunning: true } mainWindow)
        {
            return;
        }

        EventLog eventLog = new ()
        {
            Id = "eventLog",
            X = Pos.AnchorEnd (),
            Height = Dim.Fill (),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            BorderStyle = LineStyle.Double,
            Title = "E_vents",
            Arrangement = ViewArrangement.LeftResizable
        };

        FrameView menuBarLikeExamples = new ()
        {
            Title = "MenuBar-Like Examples",
            X = 0,
            Y = 0,
            Width = Dim.Fill (eventLog),
            Height = Dim.Percent (33)
        };
        mainWindow.Add (menuBarLikeExamples);

        Label label = new () { Title = "      Bar:", X = 0, Y = 0 };
        menuBarLikeExamples.Add (label);

        Bar bar = new () { Id = "menuBar-like", X = Pos.Right (label), Y = Pos.Top (label), Width = Dim.Fill () };

        ConfigMenuBar (bar);
        menuBarLikeExamples.Add (bar);

        label = new Label { Title = "  MenuBar:", X = 0, Y = Pos.Bottom (bar) + 1 };
        menuBarLikeExamples.Add (label);

        FrameView menuLikeExamples = new ()
        {
            Title = "Menu-Like Examples",
            X = 0,
            Y = Pos.Center (),
            Width = Dim.Fill (eventLog),
            Height = Dim.Percent (33)
        };
        mainWindow.Add (menuLikeExamples);

        Label barLabel = new () { Title = "Bar:", X = 0, Y = 0 };
        menuLikeExamples.Add (barLabel);

        var menuLikeBar = new Bar
        {
            Id = "menu-like",
            X = 0,
            Y = Pos.Bottom (barLabel),

            //Width = Dim.Percent (40),
            Orientation = Orientation.Vertical
        };
        ConfigureMenu (menuLikeBar);

        menuLikeExamples.Add (menuLikeBar);

        barLabel = new Label { Title = "Menu:", X = Pos.Right (menuLikeBar) + 1, Y = Pos.Top (barLabel) };
        menuLikeExamples.Add (barLabel);

        menuLikeBar = new Bar { Id = "menu", X = Pos.Left (barLabel), Y = Pos.Bottom (barLabel) };
        ConfigureMenu (menuLikeBar);
        menuLikeBar.Arrangement = ViewArrangement.RightResizable;

        menuLikeExamples.Add (menuLikeBar);

        barLabel = new Label { Title = "PopOver Menu (Right click to show):", X = Pos.Right (menuLikeBar) + 1, Y = Pos.Top (barLabel) };
        menuLikeExamples.Add (barLabel);

        Menu popOverMenu = new () { Id = "popupMenu", X = Pos.Left (barLabel), Y = Pos.Bottom (barLabel) };
        ConfigureMenu (popOverMenu);

        popOverMenu.Arrangement = ViewArrangement.Overlapped;
        popOverMenu.Visible = false;

        Shortcut toggleShortcut = new () { Title = "Toggle Hide", Text = "App", BindKeyToApplication = true, Key = Key.F4.WithCtrl };
        popOverMenu.Add (toggleShortcut);

        popOverMenu.Accepting += PopOverMenuOnAccept;

        menuLikeExamples.Add (popOverMenu);

        menuLikeExamples.MouseEvent += MenuLikeExamplesMouseEvent;

        FrameView statusBarLikeExamples = new ()
        {
            Title = "StatusBar-Like Examples",
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (eventLog),
            Height = Dim.Percent (33)
        };
        mainWindow.Add (statusBarLikeExamples);

        Label statusBarBarLabel = new Label { Title = "      Bar:", X = 0, Y = 0 };
        statusBarLikeExamples.Add (statusBarBarLabel);

        Bar statusBarLikeBar = new Bar
        {
            Id = "statusBar-like",
            X = Pos.Right (statusBarBarLabel),
            Y = Pos.Top (statusBarBarLabel),
            Width = Dim.Fill (),
            Orientation = Orientation.Horizontal
        };
        ConfigStatusBar (statusBarLikeBar);
        statusBarLikeExamples.Add (statusBarLikeBar);

        statusBarBarLabel = new Label { Title = "StatusBar:", X = 0, Y = Pos.Bottom (statusBarLikeBar) + 1 };
        statusBarLikeExamples.Add (statusBarBarLabel);

        statusBarLikeBar = new Bar { Id = "statusBar", X = Pos.Right (statusBarBarLabel), Y = Pos.Top (statusBarBarLabel), Width = Dim.Fill () };
        ConfigStatusBar (statusBarLikeBar);
        statusBarLikeExamples.Add (statusBarLikeBar);

        mainWindow.CommandsToBubbleUp = [Command.Accept];

        eventLog.SetViewToLog (mainWindow);

        foreach (FrameView frameView in mainWindow.SubViews.OfType<FrameView> ())
        {
            frameView.CommandsToBubbleUp = [Command.Accept, Command.Activate];
            eventLog.SetViewToLog (frameView);

            foreach (Bar barView in frameView.SubViews.OfType<Bar> ())
            {
                eventLog.SetViewToLog (barView);

                foreach (Shortcut sh in barView.SubViews.OfType<Shortcut> ())
                {
                    eventLog.SetViewToLog (sh);
                    eventLog.SetViewToLog (sh.CommandView);
                }
            }
        }

        mainWindow.Add (eventLog);

        void MenuLikeExamplesMouseEvent (object _, Mouse mouse)
        {
            if (mouse.Flags.HasFlag (MouseFlags.RightButtonClicked))
            {
                popOverMenu.X = mouse.Position!.Value.X;
                popOverMenu.Y = mouse.Position!.Value.Y;
                popOverMenu.Visible = true;

                //popOverMenu.Enabled = popOverMenu.Visible;
                popOverMenu.SetFocus ();
            }
            else
            {
                popOverMenu.Visible = false;

                //popOverMenu.Enabled = popOverMenu.Visible;
            }
        }

        void PopOverMenuOnAccept (object o, CommandEventArgs args)
        {
            if (popOverMenu.Visible)
            {
                popOverMenu.Visible = false;
            }
            else
            {
                popOverMenu.Visible = true;
                popOverMenu.SetFocus ();
            }
        }
    }

    private void ConfigMenuBar (Bar bar)
    {
        Shortcut fileMenuBarItem = new () { Title = Strings.menuFile, HelpText = "File Menu", Key = Key.D0.WithAlt };

        Shortcut editMenuBarItem = new () { Title = "_Edit", HelpText = "Edit Menu", Key = Key.D1.WithAlt };

        Shortcut helpMenuBarItem = new () { Title = Strings.menuHelp, HelpText = "Halp Menu", Key = Key.D2.WithAlt };

        bar.Add (fileMenuBarItem, editMenuBarItem, helpMenuBarItem);
    }

    private void ConfigureMenu (Bar bar)
    {
        Shortcut shortcut1 = new () { Title = "Z_igzag", Key = Key.I.WithCtrl, Text = "Gonna zig zag" };

        Line line = new ();

        Shortcut shortcut4 = new () { Title = "_Borders", Text = "Borders", Key = Key.D4.WithAlt };
        shortcut4.CommandView = new CheckBox { Title = shortcut4.Title, CanFocus = false };

        shortcut4.Action += () =>
                            {
                                if (shortcut4.CommandView is CheckBox cb)
                                {
                                    bar.BorderStyle = cb.Value == CheckState.Checked ? LineStyle.Double : LineStyle.None;
                                }
                            };

        // This ensures the checkbox state toggles when the hotkey of Title is pressed.
        shortcut4.Accepting += (_, args) => args.Handled = true;

        OptionSelector<Schemes> schemeOptionSelector = new () { Title = "Scheme", CanFocus = true };
        Shortcut schemeShortcut = new () { Title = "Scheme", Text = "Scheme", Key = Key.S.WithCtrl, CommandView = schemeOptionSelector };

        schemeOptionSelector!.ValueChanged += (_, args) =>
                                              {
                                                  if (args.Value is { } scheme)
                                                  {
                                                      bar.SchemeName = scheme.ToString ();
                                                  }
                                              };

        bar.Add (shortcut1, line, shortcut4, schemeShortcut);
    }

    public void ConfigStatusBar (Bar bar)
    {
        Shortcut shortcut = new () { Text = "Quit", Title = "Q_uit", Key = Key.Z.WithCtrl };

        bar.Add (shortcut);

        shortcut = new Shortcut { Text = "Help Text", Title = "Help", Key = Key.F1 };

        bar.Add (shortcut);

        shortcut = new Shortcut { Title = "_Show/Hide", Key = Key.F10, CommandView = new CheckBox { CanFocus = false, Text = "_Show/Hide" } };

        bar.Add (shortcut);

        Button button1 = new ()
        {
            Text = "I'll Hide"

            // Visible = false
        };
        button1.Accepting += ButtonClicked;
        bar.Add (button1);

        shortcut.Accepting += (_, e) =>
                              {
                                  button1.Visible = !button1.Visible;
                                  button1.Enabled = button1.Visible;
                                  e.Handled = true;
                              };

        bar.Add (new Label { HotKeySpecifier = new Rune ('_'), Text = "Fo_cusLabel", CanFocus = true });

        Button middleButton = new () { Text = "Or me!" };
        middleButton.Accepted += (s, _) => (s as View)?.App!.RequestStop ();

        bar.Add (middleButton);

        return;

        static void ButtonClicked (object sender, EventArgs e) => MessageBox.Query ((sender as View)?.App!, "Hi", $"You clicked {sender}");
    }
}

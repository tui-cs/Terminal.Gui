#nullable enable

using System.Diagnostics;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("MenuBars", "Illustrates MenuBar, MenuBarItem, and MenuBarItem.PopoverMenu")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Menus")]
[ScenarioCategory ("Shortcuts")]
public class MenuBars : Scenario
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

        MenuBarHost menuBarHostView = new ()
        {
            Id = "menuBarHostView",
            Title = "MenuBar Host",
            X = 0,
            Y = 0,
            Width = Dim.Fill () - Dim.Width (_eventLog),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        runnable.Add (menuBarHostView);

        _eventLog.SetViewToLog (runnable);
        _eventLog.SetViewToLog (menuBarHostView);

        runnable.Initialized += (_, _) =>
                                {
                                    _eventLog.SetViewToLog (menuBarHostView.MenuBar);

                                    foreach (MenuItem menuItem in menuBarHostView.MenuBar?.GetMenuItemsWith (v => true) ?? [])
                                    {
                                        _eventLog.SetViewToLog (menuItem);
                                        _eventLog.SetViewToLog (menuItem.CommandView);
                                        menuItem.Action += () => _eventLog.Log ($"{menuItem.ToIdentifyingString ()} Action!");
                                    }

                                    _eventLog.SetViewToLog (menuBarHostView.BottomMenuBar);

                                    foreach (MenuItem menuItem in menuBarHostView.BottomMenuBar?.GetMenuItemsWith (v => true) ?? [])
                                    {
                                        _eventLog.SetViewToLog (menuItem);
                                        _eventLog.SetViewToLog (menuItem.CommandView);
                                        menuItem.Action += () => _eventLog.Log ($"{menuItem.ToIdentifyingString ()} Action!");
                                    }
                                };

        runnable.Add (_eventLog);

        app.Run (runnable);
    }

    /// <summary>
    ///     A demo view class that contains a MenuBar.
    /// </summary>
    public class MenuBarHost : View
    {
        internal MenuBar? MenuBar { get; private set; }

        internal MenuBar? BottomMenuBar { get; private set; }

        public MenuBarHost ()
        {
            CanFocus = true;
            BorderStyle = LineStyle.Dashed;
        }

        /// <inheritdoc/>
        public override void EndInit ()
        {
            base.EndInit ();

            Label lastCommandLabel = new () { Title = "_Last Command:", X = 15, Y = 10 };

            View lastCommandText = new () { X = Pos.Right (lastCommandLabel) + 1, Y = Pos.Top (lastCommandLabel), Height = Dim.Auto (), Width = Dim.Auto () };

            Add (lastCommandLabel, lastCommandText);

            AddCommand (Command.Quit,
                        _ =>
                        {
                            App?.RequestStop ();

                            return true;
                        });
            HotKeyBindings.Add (Application.GetDefaultKey (Command.Quit), Command.Quit);

            // BUGBUG: This must come before we create the MenuBar or it will not work.
            // BUGBUG: This is due to TODO's in PopoverMenu where key bindings are not
            // BUGBUG: updated after the MenuBar is created.
            App?.Keyboard.KeyBindings.Remove (Key.F5);
            App?.Keyboard.KeyBindings.AddApp (Key.F5, this, Command.Edit);

            MenuBar = new MenuBar { Title = "MenuBarHost MenuBar" };
            MenuBar.CommandsToBubbleUp = [Command.Accept, Command.Activate, Command.HotKey];
            MenuBarHost host = this;
            MenuBar?.EnableForDesign (ref host);
            Add (MenuBar);

            Label lastActivatedLabel = new () { Title = "Last Activated:", X = Pos.Left (lastCommandLabel), Y = Pos.Bottom (lastCommandLabel) };

            View lastActivatedText = new ()
            {
                X = Pos.Right (lastActivatedLabel) + 1, Y = Pos.Top (lastActivatedLabel), Height = Dim.Auto (), Width = Dim.Auto ()
            };

            Add (lastActivatedLabel, lastActivatedText);

            // Demonstrate ctx.Value containing the accepted MenuItem
            Label lastActivatedValueLabel = new ()
            {
                Title = "Last Activated (from ctx.Value):", X = Pos.Left (lastCommandLabel), Y = Pos.Bottom (lastActivatedLabel)
            };

            View lastActivatedValueText = new ()
            {
                X = Pos.Right (lastActivatedValueLabel) + 1, Y = Pos.Top (lastActivatedValueLabel), Height = Dim.Auto (), Width = Dim.Auto ()
            };

            Add (lastActivatedValueLabel, lastActivatedValueText);

            // MenuItem: AutoSave - Demos simple CommandView state tracking
            // In MenuBar.EnableForDesign, the auto save MenuItem does not specify a Command. But does
            // set a Key (F10). MenuBar adds this key as a hotkey and thus if it's pressed, it toggles the MenuItem
            // CB.
            // So that is needed is to mirror the two check boxes.
            var autoSaveMenuItemCb = MenuBar?.GetMenuItemsWith (mi => mi.Id == "AutoSave").FirstOrDefault ()?.CommandView as CheckBox;
            Debug.Assert (autoSaveMenuItemCb is { });

            CheckBox autoSaveStatusCb = new ()
            {
                Title = "AutoSave Status (MenuItem Binding to F10)", X = Pos.Left (lastActivatedValueLabel), Y = Pos.Bottom (lastActivatedValueLabel)
            };

            autoSaveStatusCb.ValueChanged += (_, _) => { autoSaveMenuItemCb.Value = autoSaveStatusCb.Value; };
            autoSaveMenuItemCb.ValueChanged += (_, _) => { autoSaveStatusCb.Value = autoSaveMenuItemCb.Value; };

            Add (autoSaveStatusCb);

            // MenuItem: Enable Overwrite - Demos View Key Binding
            // In MenuBar.EnableForDesign, to overwrite MenuItem specifies a Command (Command.EnableOverwrite).
            // Ctrl+W is bound to Command.EnableOverwrite by this View.
            // Thus, when Ctrl+W is pressed the MenuBar never sees it, but the command is invoked on this.
            // If the user clicks on the MenuItem, Accept will be raised.
            CheckBox enableOverwriteStatusCb = new ()
            {
                Title = "Enable Overwrite (View Binding to Ctrl+W)", X = Pos.Left (autoSaveStatusCb), Y = Pos.Bottom (autoSaveStatusCb)
            };

            // The source of truth is our status CB; any time it changes, update the menu item
            var enableOverwriteMenuItemCb = MenuBar?.GetMenuItemsWith (mi => mi.Id == "Overwrite").FirstOrDefault ()?.CommandView as CheckBox;

            enableOverwriteStatusCb.ValueChanged += (_, _) => { enableOverwriteMenuItemCb?.Value = enableOverwriteStatusCb.Value; };

            HotKeyBindings.Add (Key.W.WithCtrl, Command.EnableOverwrite);

            AddCommand (Command.EnableOverwrite,
                        ctx =>
                        {
                            // The command was invoked. Toggle the status Cb.
                            enableOverwriteStatusCb.AdvanceCheckState ();

                            return true;
                        });
            Add (enableOverwriteStatusCb);

            // MenuItem: EditMode - Demos App Level Key Bindings
            // In MenuBar.EnableForDesign, the edit mode MenuItem specifies a Command (Command.Edit).
            // F5 is bound to Command.EnableOverwrite as an Application-Level Key Binding
            // Thus when F5 is pressed the MenuBar never sees it, but the command is invoked on this, via
            // Application.KeyBinding.
            // If the user clicks on the MenuItem, Accept will be raised.
            CheckBox editModeStatusCb = new ()
            {
                Title = "EditMode (App Binding to F5)", X = Pos.Left (enableOverwriteStatusCb), Y = Pos.Bottom (enableOverwriteStatusCb)
            };

            // The source of truth is our status CB; any time it changes, update the menu item
            var editModeMenuItemCb = MenuBar?.GetMenuItemsWith (mi => mi.Id == "EditMode").FirstOrDefault ()?.CommandView as CheckBox;

            editModeStatusCb.ValueChanged += (_, _) => { editModeMenuItemCb?.Value = editModeStatusCb.Value; };

            MenuBar?.Activated += (_, args) =>
                                  {
                                      if (args.Value?.TryGetSource (out View? ctxSource) is true)
                                      {
                                          // Walk up the superview chain to find the MenuItem that was activated, if any, and log it.
                                          while (ctxSource is not null && ctxSource is not MenuItem)
                                          {
                                              ctxSource = ctxSource.SuperView;
                                          }
                                          lastActivatedText.Text = $"{ctxSource.ToIdentifyingString ()}";
                                      }
                                      else
                                      {
                                          lastActivatedText.Text = $"{Glyphs.Null}";
                                      }
                                      lastActivatedValueText.Text = $"{args.Value?.Value ?? Glyphs.Null.ToString ()}";
                                  };

            CommandNotBound += (sender, args) =>
                                       {
                                           lastCommandText.Text = $"{args.Context?.Command}";
                                       };

            AddCommand (Command.Edit,
                        ctx =>
                        {
                            // The command was invoked. Toggle the status Cb.
                            editModeStatusCb.AdvanceCheckState ();

                            return true;
                        });

            Add (editModeStatusCb);

            OptionSelector<Schemes>? schemeOptionSelector =
                MenuBar?.GetMenuItemsWith (mi => mi.Id == "mutuallyExclusiveOptions").FirstOrDefault ()?.CommandView as OptionSelector<Schemes>;

            schemeOptionSelector!.ValueChanged += (_, args) =>
                                                  {
                                                      if (args.Value is { } scheme)
                                                      {
                                                          MenuBar?.SchemeName = scheme.ToString ();
                                                      }
                                                  };

            // Add a button to open the MenuBar
            Button openBtn = new () { X = Pos.Center (), Y = 4, Text = "_Open Menu", IsDefault = true };

            openBtn.Accepted += (_, _) =>
                                 {
                                     MenuBar?.InvokeCommand (Command.Activate);
                                 };

            Add (openBtn);

            // --- Bottom MenuBar: demonstrates Y = Pos.AnchorEnd () ---
            BottomMenuBar = new MenuBar { Title = "Bottom MenuBar", Y = Pos.AnchorEnd () };

            MenuItem statusItem = new () { Title = "_Status", Text = "Show status info" };
            statusItem.Activated += (_, _) => MessageBox.Query (App!, "Status", "All systems operational.", Strings.btnOk);

            MenuItem toolsSettingsItem = new () { Title = "Se_ttings...", Text = "Tool settings" };
            toolsSettingsItem.Activated += (_, _) => MessageBox.Query (App!, "Settings", "This would be a settings dialog.", Strings.btnOk);

            BottomMenuBar.Add (new MenuBarItem ("_Status",
                                                [statusItem, new Line (), new MenuItem { Title = "_Refresh", Text = "Refresh status", Key = Key.R.WithCtrl }]));

            BottomMenuBar.Add (new MenuBarItem ("_Tools",
                                                [
                                                    toolsSettingsItem,
                                                    new Line (),
                                                    new MenuItem { Title = "_Console", Text = "Open console" },
                                                    new MenuItem { Title = "_Diagnostics", Text = "Run diagnostics" }
                                                ]));

            Add (BottomMenuBar);

            autoSaveStatusCb.SetFocus ();
        }
    }
}

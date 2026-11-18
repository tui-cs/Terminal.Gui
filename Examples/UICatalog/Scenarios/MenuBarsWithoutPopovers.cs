#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("MenuBarsWithoutPopovers", "Demonstrates MenuBarItem.UsePopoverMenu = false (inline, non-modal menus)")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Menus")]
public class MenuBarsWithoutPopovers : Scenario
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

        InlineMenuHost host = new ()
        {
            Id = "inlineMenuHost",
            Title = "Inline Menu Host",
            X = 0,
            Y = 0,
            Width = Dim.Fill () - Dim.Width (_eventLog),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        runnable.Add (host);

        _eventLog.SetViewToLog (runnable);
        _eventLog.SetViewToLog (host);

        runnable.Initialized += (_, _) =>
                                {
                                    _eventLog.SetViewToLog (host.InlineMenuBar);

                                    foreach (MenuItem menuItem in host.InlineMenuBar?.GetMenuItemsWith (v => true) ?? [])
                                    {
                                        _eventLog.SetViewToLog (menuItem);
                                        _eventLog.SetViewToLog (menuItem.CommandView);
                                        menuItem.Action += () => _eventLog.Log ($"{menuItem.ToIdentifyingString ()} Action!");
                                    }

                                    _eventLog.SetViewToLog (host.MixedMenuBar);

                                    foreach (MenuItem menuItem in host.MixedMenuBar?.GetMenuItemsWith (v => true) ?? [])
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
    ///     A demo view that demonstrates inline (non-modal) menus via
    ///     <see cref="MenuBarItem.UsePopoverMenu"/> = <see langword="false"/>.
    /// </summary>
    public class InlineMenuHost : View
    {
        public InlineMenuHost ()
        {
            CanFocus = true;
            BorderStyle = LineStyle.Dashed;
        }

        /// <inheritdoc/>
        public override void EndInit ()
        {
            base.EndInit ();

            // ─── Description ───
            Label descLabel = new ()
            {
                Title = "MenuBarItem.UsePopoverMenu = false uses inline, non-modal SubMenu drop-downs.",
                X = 1,
                Y = 1,
                Width = Dim.Fill (1)
            };

            Add (descLabel);

            // ─── Top MenuBar: all-inline with cascading submenus ───
            InlineMenuBar = new MenuBar { Id = "InlineMenuBar", Title = "Inline MenuBar" };

            InlineMenuBar.Add (new MenuBarItem (Strings.menuFile,
                                                [
                                                    new MenuItem { Title = "_New", HelpText = "Create new document" },
                                                    new MenuItem { Title = "_Open", HelpText = "Open existing document" },
                                                    new MenuItem { Title = "_Save", HelpText = "Save current document", Key = Key.S.WithCtrl },
                                                    new MenuItem { Title = "Save _As...", HelpText = "Save to new file" },
                                                    new Line (),
                                                    new MenuItem
                                                    {
                                                        Title = "_File Options",
                                                        SubMenu = new Menu ([
                                                                                new MenuItem { Title = "_Auto Save", HelpText = "Toggle auto save" },
                                                                                new MenuItem { Title = "Enable _Overwrite", HelpText = "Toggle overwrite mode" },
                                                                                new Line (),
                                                                                new MenuItem
                                                                                {
                                                                                    Title = "_File Settings...",
                                                                                    HelpText = "More file settings",
                                                                                    Action =
                                                                                        () => MessageBox.Query (App!,
                                                                                                                "File Settings",
                                                                                                                "This is the File Settings Dialog\n",
                                                                                                                Strings.btnOk,
                                                                                                                Strings.btnCancel)
                                                                                }
                                                                            ])
                                                        {
                                                            Id = "FileOptionsMenu",
                                                        }
                                                    },
                                                    new Line (),
                                                    new MenuItem
                                                    {
                                                        Title = "_Preferences",
                                                        SubMenu = new Menu ([
                                                                                new MenuItem { Title = "_Appearance...", HelpText = "Visual settings" },
                                                                                new MenuItem { Title = "_Language", HelpText = "Language settings" },
                                                                                new Line (),
                                                                                new MenuItem
                                                                                {
                                                                                    Title = "_Advanced",
                                                                                    SubMenu = new Menu ([
                                                                                                            new MenuItem { Title = "_Debug Mode", HelpText = "Toggle debug mode" },
                                                                                                            new MenuItem { Title = "_Verbose Logging", HelpText = "Toggle verbose logs" }
                                                                                                        ])
                                                                                }
                                                                            ])
                                                        {
                                                            Id = "PreferencesMenu"
                                                        }

                                                    },
                                                    new Line (),
                                                    new MenuItem { Title = "_Quit", Key = Application.GetDefaultKey (Command.Quit) }
                                                ])
            {
                Id = "FileMenu",
                UsePopoverMenu = false
            });

            InlineMenuBar.Add (new MenuBarItem ("_Edit",
                                                [
                                                    new MenuItem { Title = "Cu_t", HelpText = "Cut selection", Key = Key.X.WithCtrl },
                                                    new MenuItem { Title = "_Copy", HelpText = "Copy selection", Key = Key.C.WithCtrl },
                                                    new MenuItem { Title = "_Paste", HelpText = "Paste clipboard", Key = Key.V.WithCtrl },
                                                    new Line (),
                                                    new MenuItem { Title = "Select _All", Key = Key.A.WithCtrl },
                                                    new Line (),
                                                    new MenuItem
                                                    {
                                                        Title = "_Find && Replace",
                                                        SubMenu = new Menu ([
                                                                                new MenuItem { Title = "_Find...", HelpText = "Find text", Key = Key.F.WithCtrl },
                                                                                new MenuItem { Title = "_Replace...", HelpText = "Find and replace", Key = Key.H.WithCtrl },
                                                                                new Line (),
                                                                                new MenuItem { Title = "_Go to Line...", HelpText = "Jump to line number", Key = Key.G.WithCtrl }
                                                                            ])
                                                            {
                                                                Id = "FindReplaceMenu"
                                                            }
                                                    }
                                                ])
            {
                Id = "EditMenu",
                UsePopoverMenu = false
            });

            InlineMenuBar.Add (new MenuBarItem (Strings.menuHelp,
                                                [
                                                    new MenuItem
                                                    {
                                                        Title = "_Online Help...",
                                                        Action = () => MessageBox.Query (App!, "Online Help", "https://tui-cs.github.io/Terminal.Gui", Strings.btnOk)
                                                    },
                                                    new MenuItem
                                                    {
                                                        Title = "About...",
                                                        Action = () => MessageBox.Query (App!, "About", "Inline Menus Demo", Strings.btnOk)
                                                    }
                                                ])
            {
                Id = "HelpMenu",
                UsePopoverMenu = false
            });

            Add (InlineMenuBar);

            // ─── Last-activated label ───
            Label lastActivatedLabel = new () { Title = "Last Activated:", X = 1, Y = 5 };

            View lastActivatedText = new ()
            {
                X = Pos.Right (lastActivatedLabel) + 1,
                Y = Pos.Top (lastActivatedLabel),
                Height = Dim.Auto (),
                Width = Dim.Auto ()
            };

            Add (lastActivatedLabel, lastActivatedText);

            InlineMenuBar.Activated += (_, args) =>
                                       {
                                           if (args.Value?.TryGetSource (out View? src) is true)
                                           {
                                               while (src is not null && src is not MenuItem)
                                               {
                                                   src = src.SuperView;
                                               }

                                               lastActivatedText.Text = $"{src?.ToIdentifyingString ()}";
                                           }
                                           else
                                           {
                                               lastActivatedText.Text = $"{Glyphs.Null}";
                                           }
                                       };

            // ─── Bottom MenuBar: mixed popover + inline ───
            MixedMenuBar = new MenuBar { Id = "MixedMenuBar", Title = "Mixed MenuBar", Y = Pos.AnchorEnd () };

            // This MenuBarItem uses the default popover mode
            MixedMenuBar.Add (new MenuBarItem ("_Status (Popover)",
                                               [
                                                   new MenuItem { Title = "_Status", HelpText = "Show status info" },
                                                   new Line (),
                                                   new MenuItem { Title = "_Refresh", HelpText = "Refresh status" },
                                                   new MenuItem
                                                   {
                                                       Title = "_Details",
                                                       SubMenu = new Menu ([
                                                                               new MenuItem { Title = "_System Info", HelpText = "OS and runtime info" },
                                                                               new MenuItem { Title = "_Memory Usage", HelpText = "Memory stats" }
                                                                           ])
                                                       {
                                                           Id = "StatusDetailsMenu",
                                                       }
                                                   }
                                               ])
            {
                UsePopoverMenu = false,
                Id = "StatusMenu",
            });

            // This MenuBarItem uses inline mode with cascading submenus
            MixedMenuBar.Add (new MenuBarItem ("_Tools (Inline)",
                                               [
                                                   new MenuItem
                                                   {
                                                       Title = "Se_ttings...",
                                                       HelpText = "Tool settings",
                                                       Action = () => MessageBox.Query (App!, "Settings", "This would be a settings dialog.", Strings.btnOk)
                                                   },
                                                   new Line (),
                                                   new MenuItem { Title = "_Console", HelpText = "Open console" },
                                                   new MenuItem
                                                   {
                                                       Title = "_Diagnostics",
                                                       SubMenu = new Menu ([
                                                                               new MenuItem { Title = "_Run All", HelpText = "Run all diagnostics" },
                                                                               new MenuItem { Title = "_Network Test", HelpText = "Test network connectivity" },
                                                                               new MenuItem { Title = "_Disk Check", HelpText = "Verify disk health" }
                                                                           ])
                                                       {
                                                           Id = "DiagnosticsMenu",
                                                       }
                                                   }
                                               ])
            {
                UsePopoverMenu = false,
                Id = "ToolsMenu",
            });

            Add (MixedMenuBar);

            MixedMenuBar.Activated += (_, args) =>
                                      {
                                          if (args.Value?.TryGetSource (out View? src) is true)
                                          {
                                              while (src is not null && src is not MenuItem)
                                              {
                                                  src = src.SuperView;
                                              }

                                              lastActivatedText.Text = $"(Mixed) {src?.ToIdentifyingString ()}";
                                          }
                                      };
        }

        internal MenuBar? InlineMenuBar { get; private set; }

        internal MenuBar? MixedMenuBar { get; private set; }
    }
}

using System.ComponentModel;
using Terminal.Gui.Tracing;

#pragma warning disable CS0618 // Obsolete - MenuBar uses [ConfigurationProperty] for DefaultBorderStyle/DefaultKey during transition

namespace Terminal.Gui.Views;

/// <summary>
///     A horizontal <see cref="Menu"/> that contains <see cref="MenuBarItem"/> items. Each
///     <see cref="MenuBarItem"/> owns a <see cref="PopoverMenu"/> that is displayed as a drop-down when
///     the item is selected. Typically placed at the top of a window or view.
/// </summary>
/// <remarks>
/// <img src="../images/views/MenuBar.gif" alt="MenuBar demo"/>
///     <para>
///         <see cref="MenuBar"/> extends <see cref="Menu"/> with horizontal orientation and specializes it for
///         <see cref="MenuBarItem"/> items. By default, it is positioned at <c>Y = 0</c> with
///         <c>Width = <see cref="Dim"/>.<see cref="Dim.Fill()"/>()</c>, spanning the full width of its
///         <see cref="View.SuperView"/>.
///     </para>
///     <para>
///         <b>Activation:</b> The <see cref="Key"/> property (default: <see cref="Key.F10"/>, configurable via
///         <see cref="DefaultKey"/>) activates the <see cref="MenuBar"/>. When activated, the first
///         <see cref="MenuBarItem"/> with a <see cref="PopoverMenu"/> is opened. Use <see cref="Active"/> to
///         get or set whether the <see cref="MenuBar"/> is in its active state. When <see cref="Active"/> changes,
///         it drives <see cref="View.CanFocus"/> and hides any open <see cref="PopoverMenu"/>s on deactivation.
///     </para>
///     <para>
///         <b>Popover Browsing:</b> While a <see cref="PopoverMenu"/> is open, moving between
///         <see cref="MenuBarItem"/>s (via arrow keys, mouse hover, or HotKeys) automatically switches the
///         visible popover to the newly focused item. Use <see cref="IsOpen"/> to determine if any
///         <see cref="PopoverMenu"/> is currently visible.
///     </para>
///     <para>
///         <b>Command Dispatch:</b> Uses the consume-dispatch pattern (<c>ConsumeDispatch = true</c>,
///         <c>GetDispatchTarget =&gt; Focused</c>), meaning the <see cref="MenuBar"/> owns activation state
///         for its <see cref="MenuBarItem"/>s. Registers custom command handlers for
///         <see cref="Command.HotKey"/>, <c>Command.Quit</c>, <c>Command.Right</c>, and <c>Command.Left</c>.
///     </para>
///     <para>
///         <b>Navigation:</b> The <c>Left</c> and <c>Right</c> arrow keys move focus between
///         <see cref="MenuBarItem"/>s. <see cref="Application.GetDefaultKey"/> and <see cref="Key"/> close any open
///         popover and deactivate the <see cref="MenuBar"/>.
///     </para>
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/shortcut.html">Shortcut Deep Dive</see> for
///         details on the <see cref="Shortcut"/> base class and command routing patterns.
///     </para>
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/menus.html">Menus Deep Dive</see> for the
///         full menu system architecture, class hierarchy, command routing, and usage examples.
///     </para>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>F10 (configurable via <see cref="DefaultKey"/>)</term>
///             <description>Activates/deactivates the menu bar.</description>
///         </item>
///         <item>
///             <term>Left / Right</term> <description>Moves between menu bar items.</description>
///         </item>
///         <item>
///             <term>Escape, <see cref="Application.GetDefaultKey"/></term>
///             <description>Closes any open popover and deactivates the menu bar.</description>
///         </item>
///     </list>
/// </remarks>
public class MenuBar : Menu, IDesignable
{
    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="MenuBar"/>. All standard navigation bindings are
    ///     inherited from <see cref="View.DefaultKeyBindings"/>, so this dictionary is empty by default.
    ///     Dynamic bindings (activation key, quit key) are bound directly in the constructor.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ();

    /// <inheritdoc/>
    public MenuBar () : this ([]) { }

    /// <inheritdoc/>
    public MenuBar (IEnumerable<MenuItem> menuBarItems) : base (menuBarItems)
    {
        CanFocus = false;
        TabStop = TabBehavior.TabGroup;
        Y = 0;
        Width = Dim.Fill ();
        Height = Dim.Auto ();
        Orientation = Orientation.Horizontal;

        Key = DefaultKey;

        // If we're not focused, Key activates/deactivates
        HotKeyBindings.Add (Key, Command.HotKey);

        AddCommand (Command.Quit, Quit);

        AddCommand (Command.Right, MoveRight);

        AddCommand (Command.Left, MoveLeft);

        // Apply layered key bindings (base View layer + MenuBar-specific layer)
        ApplyKeyBindings (View.DefaultKeyBindings, DefaultKeyBindings);

        // Dynamic bindings that depend on instance properties
        KeyBindings.Add (Key, Command.Quit);
        KeyBindings.ReplaceCommands (Application.GetDefaultKey (Command.Quit), Command.Quit);

        // Override the default HotKey handler to correctly route bubbled-up HotKeys
        // from MenuBarItems. Without this, DefaultHotKeyHandler invokes Activate on the
        // MenuBar with Direct routing, causing FallbackToFirst to always open the first
        // item instead of the one whose HotKey was pressed.
        AddCommand (Command.HotKey, HotKeyHandler);

        BorderStyle = DefaultBorderStyle;

        ThemeChanges.ThemeChanged += OnThemeChanged;

        return;

        bool? HotKeyHandler (ICommandContext? ctx)
        {
            Trace.Command (this, ctx, "Entry");

            // When a MenuBarItem's HotKey bubbles up to the MenuBar, invoke Activate
            // on the source MenuBarItem directly. This toggles its PopoverMenuOpen and
            // then bubbles Activate to OnActivating with BubblingUp routing, which
            // correctly identifies the source and shows the right popover.
            if (ctx?.Routing != CommandRouting.BubblingUp || !ctx.TryGetSource (out View? source) || FindMenuBarEntryForSource (source) is not { } sourceEntry)
            {
                return DefaultHotKeyHandler (ctx);
            }

            var sourceView = (View)sourceEntry;
            Trace.Command (this, ctx, "BubblingUp", $"Activating {sourceView.ToIdentifyingString ()}");
            sourceView.InvokeCommand (Command.Activate, ctx.Binding);

            return true;

            // Non-bubbled HotKey (e.g. F10 pressed on MenuBar directly) — use default behavior.
        }

        bool? Quit (ICommandContext? ctx)
        {
            _popoverBrowsingMode = false;

            if (HideActiveItem ())
            {
                return true;
            }

            if (!CanFocus)
            {
                return false;
            }
            CanFocus = false;
            Active = false;

            return true;
        }

        bool? MoveLeft (ICommandContext? ctx)
        {
            // Don't set _isSwitchingItem here — OnSelectedMenuItemChanged needs to call
            // ShowItem on the newly focused MenuBarItem. ShowItem has its own internal
            // _isSwitchingItem guard for the focus transfer it does.
            bool? result = AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop);
            FocusInlineSubMenuIfNeeded ();

            return result;
        }

        bool? MoveRight (ICommandContext? ctx)
        {
            // Don't set _isSwitchingItem here — OnSelectedMenuItemChanged needs to call
            // ShowItem on the newly focused MenuBarItem. ShowItem has its own internal
            // _isSwitchingItem guard for the focus transfer it does.
            bool? result = AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
            FocusInlineSubMenuIfNeeded ();

            return result;
        }
    }

    private bool _isSwitchingItem;

    /// <summary>
    ///     Tracks "browsing mode" — set when any entry's menu opens, stays true during
    ///     item switching (bridging the brief gap when old menu closes before new one opens).
    ///     Reset only when <see cref="Active"/> goes false or <see cref="Command.Quit"/> is handled.
    /// </summary>
    private bool _popoverBrowsingMode;

    /// <inheritdoc/>
    public bool EnableForDesign<TContext> (ref TContext targetView) where TContext : notnull
    {
        // Note: This menu is used by unit tests. If you modify it, you'll likely have to update
        // unit tests.

        if (targetView is View target)
        {
            App ??= target.App;
        }

        Id = "DemoBar";

        CheckBox bordersCb = new ()
        {
            Title = "_Borders",

            // Shortcut/MenuItem override GettingAttributeForRole to ensure CommandViews with multiple selectable items (like a ListView or Selector)
            // show the selected item distinctly, but for a CommandView with only a single selectable item (like a CheckBox),
            // we want it to look focused when selected, and unfocused when not, so set CanFocus false.
            CanFocus = false,
            Value = DefaultBorderStyle == LineStyle.None ? CheckState.UnChecked : CheckState.Checked
        };

        CheckBox autoSaveCb = new ()
        {
            Title = "_Auto Save",

            // Shortcut/MenuItem override GettingAttributeForRole to ensure CommandViews with multiple selectable items (like a ListView or Selector)
            // show the selected item distinctly, but for a CommandView with only a single selectable item (like a CheckBox),
            // we want it to look focused when selected, and unfocused when not, so set CanFocus false.
            CanFocus = false
        };

        CheckBox enableOverwriteCb = new ()
        {
            Title = "Enable _Overwrite",

            // Shortcut/MenuItem override GettingAttributeForRole to ensure CommandViews with multiple selectable items (like a ListView or Selector)
            // show the selected item distinctly, but for a CommandView with only a single selectable item (like a CheckBox),
            // we want it to look focused when selected, and unfocused when not, so set CanFocus false.
            CanFocus = false
        };

        OptionSelector<Schemes> mutuallyExclusiveOptionsSelector = new () { Title = "Scheme", CanFocus = true, MouseHighlightStates = MouseState.None };

        ColorPicker menuBgColorCp = new () { Width = 30 };

        menuBgColorCp.ValueChanged += (_, args) =>
                                      {
                                          SetScheme (GetScheme () with
                                          {
                                              Normal = new Attribute (GetAttributeForRole (VisualRole.Normal).Foreground,
                                                                      args.NewValue ?? Color.Black,
                                                                      GetAttributeForRole (VisualRole.Normal).Style)
                                          });
                                      };

        Add (new MenuBarItem (Strings.menuFile,
                              [
                                  new MenuItem (targetView as View, Command.New),
                                  new MenuItem (targetView as View, Command.Open),
                                  new MenuItem (targetView as View, Command.Save),
                                  new MenuItem (targetView as View, Command.SaveAs),
                                  new Line (),
                                  new MenuItem
                                  {
                                      Title = "_File Options",
                                      SubMenu =
                                          new Menu ([
                                                        new MenuItem { Id = "AutoSave", Text = "(no Command)", Key = Key.F10, CommandView = autoSaveCb },
                                                        new MenuItem
                                                        {
                                                            Text = "Overwrite",
                                                            Id = "Overwrite",
                                                            Key = Key.W.WithCtrl,
                                                            CommandView = enableOverwriteCb,
                                                            Command = Command.EnableOverwrite,
                                                            TargetView = targetView as View
                                                        },
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
                                                    ]) { Id = "FileOptions" }
                                  },
                                  new Line (),
                                  new MenuItem
                                  {
                                      Title = "_Preferences",
                                      SubMenu = new Menu ([
                                                              new MenuItem
                                                              {
                                                                  CommandView = bordersCb, HelpText = "Toggle Menu Borders", Action = ToggleMenuBorders
                                                              },
                                                              new MenuItem
                                                              {
                                                                  Id = "mutuallyExclusiveOptions",
                                                                  HelpText = "Mutually Exclusive Options",
                                                                  CommandView = mutuallyExclusiveOptionsSelector,
                                                                  Key = Key.F7,
                                                                  MouseHighlightStates = MouseState.None
                                                              },
                                                              new Line (),
                                                              new MenuItem { HelpText = "MenuBar BG Color", CommandView = menuBgColorCp, Key = Key.F8 }
                                                          ]) { Id = "Preferences" }
                                  },
                                  new Line (),
                                  new MenuItem { TargetView = targetView as View, Command = Command.Quit }
                              ]));

        Add (new MenuBarItem ("_Edit",
                              [
                                  new MenuItem (targetView as View, Command.Cut),
                                  new MenuItem (targetView as View, Command.Copy),
                                  new MenuItem (targetView as View, Command.Paste),
                                  new Line (),
                                  new MenuItem (targetView as View, Command.SelectAll),
                                  new Line (),
                                  new MenuItem { Title = "_Details", SubMenu = new Menu (ConfigureDetailsSubMenu ()) { Id = "DetailsSubMenu" } }
                              ]));

        MenuItem onlineHelpMi = new () { Title = "_Online Help..." };

        // Demonstrate using Activating
        onlineHelpMi.Activated += (_, _) => MessageBox.Query (App!, "Online Help", "https://tui-cs.github.io/Terminal.Gui", Strings.btnOk);

        Add (new MenuBarItem (Strings.menuHelp,
                              [
                                  onlineHelpMi,
                                  new MenuItem { Title = "About...", Action = () => MessageBox.Query (App!, "About", "Something About Mary.", Strings.btnOk) }
                              ]));

        return true;

        void ToggleMenuBorders ()

        {
            foreach (MenuBarItem mbi in SubViews.OfType<MenuBarItem> ())
            {
                if (mbi is not { PopoverMenu: { } })
                {
                    continue;
                }

                foreach (Menu subMenu in mbi.PopoverMenu.Root?.GetAllSubMenus () ?? [])
                {
                    subMenu.Border.Thickness = bordersCb.Value == CheckState.Checked ? new Thickness (1) : new Thickness (0);
                    subMenu.Border.LineStyle = bordersCb.Value == CheckState.Checked ? LineStyle.Rounded : LineStyle.None;
                }
            }
        }

        MenuItem [] ConfigureDetailsSubMenu ()
        {
            MenuItem detail = new () { Title = "_Detail 1", Text = "Some detail #1" };

            MenuItem nestedSubMenu = new () { Title = "_Moar Details", SubMenu = new Menu (ConfigureMoreDetailsSubMenu ()) { Id = "MoreDetailsSubMenu" } };

            // This menu item is used to test Application Key binding. See the Menus Scenario.
            // F5 will toggle the Edit Mode checkbox, and the menu item text will update to show the Command it's bound to.
            MenuItem editMode = new ()
            {
                Text = "App Binding to Command.Edit",
                Id = "EditMode",
                Command = Command.Edit,
                CommandView = new CheckBox { Title = "E_dit Mode" },
                Key = Key.F5,
                BindKeyToApplication = true
            };

            return [detail, nestedSubMenu, null!, editMode];

            View [] ConfigureMoreDetailsSubMenu ()
            {
                MenuItem deeperDetail = new ()
                {
                    Title = "_Deeper Detail",
                    Text = "Deeper Detail",
                    Action = () => { MessageBox.Query (App!, "Deeper Detail", "Lots of details", Strings.btnOk); }
                };

                MenuItem belowLineDetail = new () { Title = "_Even more detail", Text = "Below the line" };

                // This ensures the checkbox state toggles when the hotkey of Title is pressed.
                // shortcut4.Accepting += (sender, args) => args.Cancel = true;

                return [deeperDetail, new Line (), belowLineDetail];
            }
        }
    }

    /// <summary>
    ///     Gets or sets whether the menu bar is active or not. When active, the MenuBar can focus and moving the mouse
    ///     over a MenuBarItem will switch focus to that item. Use <see cref="IsOpen"/> to determine if a PopoverMenu of
    ///     a MenuBarItem is open.
    /// </summary>
    /// <returns></returns>
    public bool Active
    {
        get;
        internal set
        {
            if (field == value)
            {
                return;
            }

            Trace.Command (this, "ActiveChanged", $"{field} -> {value}");
            field = value;

            // When deactivating, clear browsing mode BEFORE changing CanFocus.
            // CanFocus = false triggers focus leave → OnFocusedChanged → OnSelectedMenuItemChanged.
            // If _popoverBrowsingMode is still true at that point, the handler would
            // call ShowEntry and reopen the menu we just closed.
            if (!field)
            {
                _popoverBrowsingMode = false;
            }

            // Change CanFocus based on Active state before hiding Popovers; this way when focus is restored,
            // it won't be to the MenuBar
            CanFocus = value;

            if (field)
            {
                return;
            }

            HideActiveItem ();
        }
    }

    /// <summary>
    ///     Gets or sets the default Border Style for the MenuBar. The default is <see cref="LineStyle.None"/>.
    /// </summary>
    public new static LineStyle DefaultBorderStyle => MenuBarSettings.Current.DefaultBorderStyle;

    /// <summary>The default key for activating menu bars.</summary>
    public static Key DefaultKey
    {
        get => MenuBarSettings.Current.DefaultKey;
        set => MenuBarSettings.Current = MenuBarSettings.Current with { DefaultKey = value };
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        if (Border is { })
        {
            Border.Thickness = new Thickness (0);
            Border.LineStyle = LineStyle.None;
        }

        // TODO: This needs to be done whenever a menuitem in any MenuBarItem changes
        foreach (View sv in SubViews)
        {
            if (sv is not MenuBarItem mbi)
            {
                continue;
            }

            if (mbi.UsePopoverMenu)
            {
                App?.Popovers?.Register (mbi.PopoverMenu);
            }
            else
            {
                mbi.SubscribeToSubMenuVisibility ();
            }
        }
    }

    /// <summary>
    ///     Gets all <see cref="MenuItem"/>s in the menu hierarchy that match <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">A function to test each <see cref="MenuItem"/>.</param>
    /// <returns>All matching <see cref="MenuItem"/>s across all <see cref="PopoverMenu"/>s.</returns>
    public IEnumerable<MenuItem> GetMenuItemsWith (Func<MenuItem, bool> predicate)
    {
        List<MenuItem> menuItems = [];

        foreach (IMenuBarEntry entry in SubViews.OfType<IMenuBarEntry> ())
        {
            if (entry.RootMenu is { } rootMenu)
            {
                menuItems.AddRange (rootMenu.GetMenuItemsOfAllSubMenus (predicate));
            }
        }

        return menuItems;
    }

    /// <summary>
    ///     Hides the popover menu associated with the active menu bar item and updates the focus state.
    /// </summary>
    /// <returns><see langword="true"/> if the popover was hidden</returns>
    public bool HideActiveItem ()
    {
        Trace.Command (this, "Entry");

        return HideItem (GetActiveItem ());
    }

    /// <summary>
    ///     Hides the menu associated with the specified menu bar entry and updates the focus state.
    /// </summary>
    /// <param name="activeItem">The <see cref="IMenuBarEntry"/> whose menu should be hidden.</param>
    /// <returns><see langword="true"/> if the menu was hidden.</returns>
    public bool HideItem (IMenuBarEntry? activeItem)
    {
        if (activeItem is null || !activeItem.IsMenuOpen)
        {
            return false;
        }

        // IMPORTANT: Set Visible false before setting Active to false (Active changes Can/HasFocus)
        activeItem.IsMenuOpen = false;

        Active = false;
        HasFocus = false;

        return true;
    }

    /// <summary>
    ///     Gets whether any of the menu bar entries have a visible menu.
    /// </summary>
    public bool IsOpen () => SubViews.OfType<IMenuBarEntry> ().Any (e => e.IsMenuOpen);

    /// <summary>
    ///     Specifies the key that will activate the MenuBar. The default is <see cref="Key.F10"/> and
    ///     can be configured using the <see cref="DefaultKey"/> configuraiton property.
    /// </summary>
    public Key Key
    {
        get;
        set
        {
            Key oldKey = field;
            field = value;
            KeyChanged?.Invoke (this, new KeyChangedEventArgs (oldKey, field));
        }
    }

    /// <summary>Raised when <see cref="Key"/> is changed.</summary>
    public event EventHandler<KeyChangedEventArgs>? KeyChanged;

    /// <summary>
    ///     Sets the Menu Bar Items for this Menu Bar. This will replace any existing Menu Bar Items.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a convenience property to help porting from the v1 MenuBar.
    ///     </para>
    /// </remarks>
    public MenuBarItem []? Menus
    {
        set
        {
            RemoveAll ();

            if (value is null)
            {
                return;
            }

            foreach (MenuBarItem mbi in value)
            {
                Add (mbi);
            }
        }
    }

    /// <inheritdoc/>
    protected override bool ConsumeDispatch => true;

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        if (disposing)
        {
            ThemeChanges.ThemeChanged -= OnThemeChanged;
        }
    }

    /// <inheritdoc/>
    protected override View? GetDispatchTarget (ICommandContext? ctx) => Focused;

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Entry", $"Active={Active} Routing={ctx?.Routing}");
        base.OnActivated (ctx);

        if (ctx is { Routing: CommandRouting.Direct })
        {
            CanFocus = !Active;
        }
    }

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        Trace.Command (this, args.Context, "Entry", $"Visible={Visible} Enabled={Enabled} Active={Active}");

        if (!Visible || !Enabled)
        {
            Trace.Command (this, args.Context, "Blocked", "Invisible or disabled");

            return true; // Block activation when invisible or disabled
        }

        // When a menu bar entry's activation bubbles up, activate the MenuBar and show that item.
        if (args.Context?.Routing == CommandRouting.BubblingUp)
        {
            if (!args.Context.TryGetSource (out View? source) || FindMenuBarEntryForSource (source) is not { } sourceEntry)
            {
                Trace.Command (this, args.Context, "BubblingUp", "Source not found or not an IMenuBarEntry");

                return false;
            }

            var sourceView = (View)sourceEntry;
            Trace.Command (this, args.Context, "BubblingUp", $"Source={sourceView.ToIdentifyingString ()} IsMenuOpen={sourceEntry.IsMenuOpen}");

            if (sourceEntry.IsMenuOpen)
            {
                // Guard against intermediate focus traversal: setting Active=true
                // causes CanFocus=true which can focus the first entry transiently.
                // _isSwitchingItem prevents OnSelectedMenuItemChanged from auto-opening
                // that intermediate item.
                _isSwitchingItem = true;

                try
                {
                    // Entry just opened its menu — ensure MenuBar is active
                    if (!Active)
                    {
                        Active = true;
                    }

                    ShowEntry (sourceEntry);
                }
                finally
                {
                    _isSwitchingItem = false;
                }

                FocusInlineSubMenuIfNeeded ();
            }
            else
            {
                // Entry just closed its menu — deactivate MenuBar
                Active = false;
            }

            return true;

            // Non-entry SubView bubbling — let normal bubbling proceed.
        }

        if (Active)
        {
            // Already active — toggle off
            Trace.Command (this, args.Context, "ToggleOff", "Already active — deactivating");
            Active = false;

            return true;
        }

        if (SubViews.OfType<IMenuBarEntry> ().FirstOrDefault (e => e.RootMenu is { }) is not { } first)
        {
            Trace.Command (this, args.Context, "NoItems", "No IMenuBarEntry with a menu found");

            return false;
        }

        // Not yet active — activate and show the first entry with a menu.
        Trace.Command (this, args.Context, "FallbackToFirst", $"Opening first={((View)first).ToIdentifyingString ()}");
        Active = true;
        ShowEntry (first);
        FocusInlineSubMenuIfNeeded ();

        return true;
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        if (newHasFocus || _isSwitchingItem)
        {
            return;
        }

        // Don't deactivate if focus moved to the SubMenu of one of our inline MenuBarItems.
        // The SubMenu is a sibling of the MenuBar (added to SuperView), so focus leaving
        // the MenuBar doesn't mean the user left the menu system.
        if (focusedView is { } && IsInlineSubMenu (focusedView))
        {
            return;
        }

        Active = false;
    }

    /// <inheritdoc/>
    protected override bool OnMouseEnter (CancelEventArgs eventArgs)
    {
        // If the MenuBar does not have focus and the mouse enters: Enable CanFocus
        // But do NOT show a Popover unless the user clicks or presses a hotkey
        if (!HasFocus)
        {
            Active = true;
        }

        return base.OnMouseEnter (eventArgs);
    }

    /// <inheritdoc/>
    protected override void OnMouseLeave ()
    {
        if (!IsOpen ())
        {
            Active = false;
        }

        base.OnMouseLeave ();
    }

    /// <inheritdoc/>
    protected override void OnSelectedMenuItemChanged (MenuItem? selected)
    {
        Trace.Command (this, "Entry", $"Selected={selected?.ToIdentifyingString ()} BrowsingMode={_popoverBrowsingMode} IsSwitching={_isSwitchingItem}");

        // When _isSwitchingItem is true, we're already inside a ShowEntry call
        // (e.g., from a HotKey activation that bubbled up). Focus traversal may
        // transiently pass through intermediate entries. We must NOT auto-open
        // those items — only the target item should open.
        if (!_isSwitchingItem && _popoverBrowsingMode && selected is IMenuBarEntry { IsMenuOpen: false } selectedEntry)
        {
            ShowEntry (selectedEntry);
        }
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        if (view is IMenuBarEntry entry)
        {
            entry.MenuOpenChanged += OnEntryMenuOpenChanged;
        }
    }

    /// <inheritdoc/>
    protected override void OnSubViewRemoved (View view)
    {
        base.OnSubViewRemoved (view);

        if (view is IMenuBarEntry entry)
        {
            entry.MenuOpenChanged -= OnEntryMenuOpenChanged;
        }
    }

    /// <inheritdoc/>
    protected override void OnSuperViewChanged (ValueChangedEventArgs<View?> e)
    {
        if (SuperView is null)
        {
            // BUGBUG: This is a hack for avoiding a race condition when theme overlays re-apply.
            // BUGBUG: For some reason in some unit tests, when Top is disposed, MenuBar.Dispose does not get called.
            // BUGBUG: Yet, the MenuBar does get Removed from Top (and it's SuperView set to null).
            // BUGBUG: Related: https://github.com/tui-cs/Terminal.Gui/issues/4021
            ThemeChanges.ThemeChanged -= OnThemeChanged;
        }
    }

    /// <summary>
    ///     Guards against deactivation during entry switching (arrow keys, mouse hover, ShowEntry)
    ///     and against reentrant menu reopening during toggle-close.
    ///     When switching items, the old menu may close before the new one opens, which would
    ///     trigger deactivation via <see cref="OnEntryMenuOpenChanged"/>.
    ///     This flag prevents that false deactivation.
    /// </summary>
    internal bool IsSwitchingItem { get => _isSwitchingItem; set => _isSwitchingItem = value; }

    /// <summary>
    ///     Finds the <see cref="IMenuBarEntry"/> that is an ancestor (or is itself) the source view, and is a direct
    ///     SubView of this MenuBar.
    /// </summary>
    private IMenuBarEntry? FindMenuBarEntryForSource (View? source)
    {
        View? current = source;

        while (current is { })
        {
            if (current is IMenuBarEntry entry && current.SuperView == this)
            {
                return entry;
            }

            current = current.SuperView;
        }

        return null;
    }

    /// <summary>
    ///     Focuses the SubMenu of an InlineMenuBarItem after it has been opened. Called after
    ///     <see cref="ShowEntry"/> completes (outside the <see cref="_isSwitchingItem"/> guard)
    ///     so that <see cref="View.AdvanceFocus"/> can finish before focus leaves the MenuBar.
    /// </summary>
    private void FocusInlineSubMenuIfNeeded ()
    {
        IMenuBarEntry? active = GetActiveItem ();

        if (active is MenuBarItem { UsePopoverMenu: false, SubMenu: { } subMenu })
        {
            subMenu.SetFocus ();
        }
    }

    private IMenuBarEntry? GetActiveItem () => SubViews.OfType<IMenuBarEntry> ().FirstOrDefault (e => e.IsMenuOpen);

    /// <summary>
    ///     Determines whether the specified view is (or is contained within) the SubMenu
    ///     of one of this MenuBar's inline <see cref="MenuBarItem"/>s (those with
    ///     <see cref="MenuBarItem.UsePopoverMenu"/> = <see langword="false"/>).
    /// </summary>
    private bool IsInlineSubMenu (View view)
    {
        foreach (View sv in SubViews)
        {
            if (sv is not MenuBarItem { UsePopoverMenu: false, SubMenu: { } subMenu })
            {
                continue;
            }

            // Check if 'view' is the SubMenu or a descendant of it
            View? current = view;

            while (current is { })
            {
                if (current == subMenu)
                {
                    return true;
                }

                current = current.SuperView;
            }
        }

        return false;
    }

    private void OnThemeChanged (object? sender, App.EventArgs<string> e) => BorderStyle = DefaultBorderStyle;

    private void OnEntryMenuOpenChanged (object? sender, ValueChangedEventArgs<bool> e)
    {
        if (e.NewValue)
        {
            _popoverBrowsingMode = true;

            return;
        }

        // A menu just closed. If no others are open, and we're not in the middle of
        // arrow-key navigation (where the old menu closes before the new one opens),
        // deactivate the MenuBar entirely. HotKey switching (Alt+E while File is open)
        // is safe because the new menu opens BEFORE the old one closes.
        if (_isSwitchingItem || SubViews.OfType<IMenuBarEntry> ().Any (e2 => e2.IsMenuOpen))
        {
            return;
        }

        // During mouse-hover switching, the old entry's menu closes because
        // MenuItem.OnMouseEnter → SetFocus() moved focus to the new entry.  At this point
        // the new entry has _hasFocus=true but hasn't opened its menu yet (we're still
        // inside SetHasFocusTrue).  Deactivating now would set CanFocus=false and cascade
        // focus loss back to the new entry, violating SetHasFocusTrue's post-condition.
        // Skip deactivation when a *different* entry already has focus — ShowEntry will run
        // via OnSelectedMenuItemChanged once the focus transfer completes.
        bool anotherEntryHasFocus = SubViews.OfType<IMenuBarEntry> ().Any (e2 => (View)e2 != sender && ((View)e2).HasFocus);

        if (anotherEntryHasFocus)
        {
            return;
        }

        // If no entry has focus at all, focus is leaving the MenuBar entirely. Don't call
        // Active = false here — it would set CanFocus = false → HasFocus = false reentrantly,
        // violating SetHasFocusFalse's invariant (Debug.Assert(_hasFocus) at line 908).
        // MenuBar.OnHasFocusChanged will deactivate once the focus traversal completes.
        bool anyEntryHasFocus = SubViews.OfType<IMenuBarEntry> ().Any (e2 => ((View)e2).HasFocus);

        if (!anyEntryHasFocus)
        {
            return;
        }

        Active = false;
    }

    /// <summary>
    ///     Shows the menu for the specified entry, but only if the menu bar is active.
    /// </summary>
    /// <param name="entry">The <see cref="IMenuBarEntry"/> whose menu should be shown.</param>
    private void ShowEntry (IMenuBarEntry? entry)
    {
        var entryView = entry as View;
        Trace.Command (this, "Entry", $"Item={entryView?.ToIdentifyingString ()} Active={Active}");

        if (!Active || !Visible || entry is null)
        {
            return;
        }

        // Guard: when switching items, SetFocus() closes the old menu before the new one opens.
        // Without this guard, OnEntryMenuOpenChanged would deactivate the MenuBar.
        _isSwitchingItem = true;

        try
        {
            // Close any currently-open entry that is NOT the target.
            // ShowPopoverItem's SetFocus() naturally closes another popover, but when
            // switching from a popover to an InlineMenuBarItem (or vice versa), the old
            // entry's menu won't close automatically.
            IMenuBarEntry? activeItem = GetActiveItem ();

            if (activeItem is { } && activeItem != entry)
            {
                activeItem.IsMenuOpen = false;
            }

            if (entry is not MenuBarItem mbi)
            {
                return;
            }

            if (mbi.UsePopoverMenu)
            {
                ShowPopoverItem (mbi);
            }
            else
            {
                ShowInlineItem (mbi);
            }
        }
        finally
        {
            _isSwitchingItem = false;
        }
    }

    /// <summary>
    ///     Shows the inline SubMenu for a <see cref="MenuBarItem"/> with
    ///     <see cref="MenuBarItem.UsePopoverMenu"/> = <see langword="false"/>.
    /// </summary>
    private void ShowInlineItem (MenuBarItem menuBarItem)
    {
        if (menuBarItem.SubMenu is { IsInitialized: false })
        {
            menuBarItem.SubMenu.BeginInit ();
            menuBarItem.SubMenu.EndInit ();
        }

        Active = true;
        menuBarItem.SetFocus ();

        if (menuBarItem.SubMenu is { })
        {
            menuBarItem.SubMenu.SuperMenuItem = menuBarItem;
            menuBarItem.SubMenu.SchemeName = SchemeName;

            // Add SubMenu to MenuBar's SuperView so it isn't clipped by the MenuBar's viewport
            if (menuBarItem.SubMenu.SuperView is null && SuperView is { })
            {
                SuperView.Add (menuBarItem.SubMenu);
            }

            // Position below the MenuBarItem
            if (menuBarItem.SubMenu.SuperView is { })
            {
                Point screenPos = new (menuBarItem.FrameToScreen ().Left, menuBarItem.FrameToScreen ().Bottom);
                Point localPos = menuBarItem.SubMenu.SuperView.ScreenToViewport (screenPos);
                menuBarItem.SubMenu.X = localPos.X;
                menuBarItem.SubMenu.Y = localPos.Y;
            }
        }

        IMenuBarEntry entry = menuBarItem;
        entry.IsMenuOpen = true;
    }

    /// <summary>
    ///     Shows the PopoverMenu for a <see cref="MenuBarItem"/>.
    /// </summary>
    private void ShowPopoverItem (MenuBarItem menuBarItem)
    {
        if (menuBarItem.PopoverMenu is { IsInitialized: false })
        {
            menuBarItem.PopoverMenu.BeginInit ();
            menuBarItem.PopoverMenu.EndInit ();
        }

        Active = true;
        menuBarItem.SetFocus ();

        if (menuBarItem.PopoverMenu?.Root is { })
        {
            menuBarItem.PopoverMenu.Root.SuperMenuItem = menuBarItem;
            menuBarItem.PopoverMenu.Root.SchemeName = SchemeName;
        }

        menuBarItem.PopoverMenuOpen = true;
    }

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:500,F9,wait:800,CursorDown,wait:400,CursorDown,wait:400,Escape,wait:500";
}

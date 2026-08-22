using Terminal.Gui.Tracing;

namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="IPopover"/>-derived view that provides a cascading menu.
///     Can be used as a context menu or a drop-down menu as part of <see cref="MenuBar"/>.
/// </summary>
/// <remarks>
///     <para>
///         <b>IMPORTANT:</b> Must be registered with <see cref="Application.Popovers"/> via
///         <see cref="ApplicationPopover.Register"/> before calling <see cref="Popover{TView, TResult}.MakeVisible"/> or
///         <see cref="ApplicationPopover.Show"/>.
///     </para>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
///         var menu = new PopoverMenu ([
///             new MenuItem ("Cut", Command.Cut),
///             new MenuItem ("Copy", Command.Copy),
///             new MenuItem ("Paste", Command.Paste)
///         ]);
///         Application.Popover?.Register (menu);
///         menu.MakeVisible (); // or Application.Popover?.Show (menu);
///     </code>
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/popovers.html"/> for more information.
///     </para>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Right</term> <description>Opens a sub-menu or moves to the next menu bar item.</description>
///         </item>
///         <item>
///             <term>Left</term> <description>Closes a sub-menu or moves to the previous menu bar item.</description>
///         </item>
///     </list>
/// </remarks>
public class PopoverMenu : Popover<Menu, MenuItem>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverMenu"/> class.
    /// </summary>
    public PopoverMenu () : this ((Menu?)null) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverMenu"/> class. If any of the elements of
    ///     <paramref name="menuItems"/> is <see langword="null"/>, a <see cref="Line"/> will be created instead.
    /// </summary>
    /// <param name="menuItems">The views to use as menu items. Null elements become separator lines.</param>
    /// <remarks>
    ///     Remember to call <see cref="ApplicationPopover.Register"/> before calling
    ///     <see cref="Popover{TView, TResult}.MakeVisible"/>.
    /// </remarks>
    public PopoverMenu (IEnumerable<View?>? menuItems) : this (new Menu (menuItems?.Select (item => item ?? new Line ()))) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverMenu"/> class with the specified menu items.
    /// </summary>
    /// <param name="menuItems">The menu items to display in the popover.</param>
    /// <remarks>
    ///     Remember to call <see cref="ApplicationPopover.Register"/> before calling
    ///     <see cref="Popover{TView, TResult}.MakeVisible"/>.
    /// </remarks>
    public PopoverMenu (IEnumerable<MenuItem>? menuItems) : this (new Menu (menuItems)) { }

    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="PopoverMenu"/>. All standard navigation bindings are
    ///     inherited from <see cref="View.DefaultKeyBindings"/>, so this dictionary is empty by default.
    ///     Dynamic bindings (activation key) are bound directly in the constructor.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ();

    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverMenu"/> class with the specified root <see cref="Menu"/>.
    /// </summary>
    /// <param name="root">The root menu that contains the top-level menu items.</param>
    /// <remarks>
    ///     Remember to call <see cref="ApplicationPopover.Register"/> before calling
    ///     <see cref="Popover{TView, TResult}.MakeVisible"/>.
    /// </remarks>
    public PopoverMenu (Menu? root) : base (root)
    {
        Key = DefaultKey;

        AddCommand (Command.Right, MoveRight);

        //KeyBindings.Add (Key.CursorDown, Command.Down);

        AddCommand (Command.Left, MoveLeft);

        //KeyBindings.Add (Key.CursorUp, Command.Up);

        // Apply layered key bindings (base View layer + PopoverMenu-specific layer)
        ApplyKeyBindings (View.DefaultKeyBindings, DefaultKeyBindings);

        KeyBindings.Remove (Key.Space);
        KeyBindings.Remove (Key.Enter);

#if DEBUG
        if (string.IsNullOrEmpty (root?.Id))
        {
            root?.Id = $"popoverMenuRoot_{Id}";
        }
#endif

        return;

        bool? MoveLeft (ICommandContext? ctx)
        {
            if (Focused == Root)
            {
                if (!TryGetTarget (out View? targetView))
                {
                    return true;
                }

                // HACK: If our owner is a MenuBarItem, we want to allow left key to propagate to the MenuBar so it can move focus to the previous MenuBarItem.
                return targetView is not MenuBarItem;
            }

            if (MostFocused is not MenuItem { SuperView: Menu focusedMenu })
            {
                return AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop);
            }
            focusedMenu.SuperMenuItem?.SetFocus ();

            return true;
        }

        bool? MoveRight (ICommandContext? ctx)
        {
            if (MostFocused is not MenuItem { SubMenu.Visible: true } focused)
            {
                if (!TryGetTarget (out View? targetView))
                {
                    return true;
                }

                // HACK: If our owner is a MenuBarItem, we want to allow right key to propagate to the MenuBar so it can move focus to the next MenuBarItem.
                return targetView is not MenuBarItem;
            }
            focused.SubMenu.SetFocus ();

            return true;
        }
    }

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        Trace.Command (this, args.Context, "Entry", $"Routing={args.Context?.Routing} Cmd={args.Context?.Command}");

        if (base.OnActivating (args))
        {
            return true;
        }

        // Only process bridged commands from the menu hierarchy
        if (args.Context?.Routing != CommandRouting.Bridged)
        {
            return false;
        }

        // For non-HotKey activations, hide the popover (the menu item was selected)
        if (args.Context?.Command != Command.HotKey)
        {
            Trace.Command (this, args.Context, "HideOnNonHotKey", "Setting Visible=false");
            Visible = false;
        }

        // QuitKey handling
        if (args.Context?.Binding is KeyBinding { Key: { } key } && key == Application.GetDefaultKey (Command.Quit) && SuperView is { Visible: true })
        {
            args.Handled = true;
        }

        return false;
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     When a MenuItem activation completes (arrives via bridge as <see cref="CommandRouting.Bridged"/>),
    ///     this determines whether to hide or show submenus based on the source MenuItem.
    /// </remarks>
    protected override void OnActivated (ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Entry", $"Routing={ctx?.Routing}");

        base.OnActivated (ctx);

        // Only process bridged commands from the menu hierarchy
        if (ctx?.Routing != CommandRouting.Bridged)
        {
            return;
        }

        if (ctx.Source?.TryGetTarget (out View? sourceView) != true)
        {
            return;
        }

        Trace.Command (this, ctx, "SourceFound", $"Source={sourceView.ToIdentifyingString ()} HasSubMenu={sourceView is MenuItem { SubMenu: { } }}");

        if (sourceView is MenuItem { SubMenu: null })
        {
            // Leaf MenuItem — hide the entire menu
            Root?.HideMenu ();
        }
        else if (sourceView is MenuItem { SubMenu: { } } menuItemWithSubMenu)
        {
            // MenuItem with SubMenu — show the submenu
            ShowMenuItemSubMenu (menuItemWithSubMenu);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     <para>
    ///         When the popover is not visible, only hotkey commands are processed.
    ///     </para>
    ///     <para>
    ///         This method raises <see cref="View.Accepted"/> for commands that originate from menu items in the hierarchy.
    ///     </para>
    /// </remarks>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        Trace.Command (this, args.Context, "Entry", $"Visible={Visible}");

        // If we're not visible, ignore any keys that are not hotkeys

        if (!Visible && args.Context?.Binding is KeyBinding { Key: { } key })
        {
            if (Root?.GetMenuItemsOfAllSubMenus (i => i.Key == key).Any () != true)
            {
                Trace.Command (this, args.Context, "NotVisible", "No matching MenuItem key — ignoring");

                return false;
            }
        }

        bool? ret = base.OnAccepting (args);

        if (ret is true || args.Handled)
        {
            return args.Handled = true;
        }

        // Only raise Accepted if the command came from one of our MenuItems
        // if (GetMenuItemsOfAllSubMenus ().Contains (args.Context?.Source))
        RaiseAccepted (args.Context);

        // Always return false to enable accepting to continue propagating
        return false;
    }

    /// <summary>
    ///     Gets or sets the key that will activate the popover menu when it is registered but not visible.
    /// </summary>
    /// <remarks>
    ///     This key binding works as a global hotkey when the popover is registered with
    ///     <see cref="Application.Popovers"/>. The default value can be configured via the <see cref="DefaultKey"/>
    ///     configuraiton property.
    /// </remarks>
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

    /// <summary>
    ///     Raised when the <see cref="Key"/> property is changed.
    /// </summary>
    public event EventHandler<KeyChangedEventArgs>? KeyChanged;

    /// <summary>
    ///     Gets or sets the default key for activating popover menus. The default value is <see cref="Key.F10"/> with Shift.
    /// </summary>
    /// <remarks>
    ///     This is a configuration property that affects all new <see cref="PopoverMenu"/> instances.
    /// </remarks>
    public static Key DefaultKey
    {
        get => PopoverMenuSettings.Current.DefaultKey;
        set => PopoverMenuSettings.Current = PopoverMenuSettings.Current with { DefaultKey = value };
    }

    /// <summary>
    ///     The mouse flags that will cause the popover menu to be visible. The default is
    ///     <see cref="MouseFlags.RightButtonClicked"/> which is typically the right mouse button.
    /// </summary>
    public MouseFlags MouseFlags { get; set; } = MouseFlags.RightButtonClicked;

    /// <inheritdoc/>
    /// <remarks>
    ///     When becoming visible, the root menu is shown. When becoming hidden, the root menu is hidden
    ///     and the popover is hidden via <see cref="ApplicationPopover.Hide"/>.
    /// </remarks>
    protected override void OnVisibleChanged ()
    {
        Trace.Command (this, "Entry", $"Visible={Visible}");

        // IMPORTANT: ShowMenu/HideMenu must run BEFORE base.OnVisibleChanged because
        // Popover<TView, TResult>.OnVisibleChanged sets ContentView.Visible which would
        // cause ShowMenu/HideMenu to exit early (they check Visible as a guard).
        if (Visible)
        {
            Root?.ShowMenu ();
        }
        else
        {
            Root?.HideMenu ();
        }

        base.OnVisibleChanged ();
    }

    /// <summary>
    ///     Gets or sets the <see cref="Menu"/> that is the root of the popover menu hierarchy. The root menu is added
    ///     as the first Subview of the PopoverMenu.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The root menu contains the top-level menu items. Setting this property updates key bindings and
    ///         event subscriptions for all menus in the hierarchy.
    ///     </para>
    ///     <para>
    ///         When set, all submenus are configured with appropriate event handlers for selection and acceptance.
    ///     </para>
    /// </remarks>
    public Menu? Root
    {
        get => ContentView;
        set
        {
            if (ContentView == value)
            {
                return;
            }

            // Unsubscribe from old Root's events and remove/dispose submenus
            if (ContentView is { } oldRoot)
            {
                oldRoot.HideMenu ();
                oldRoot.VisibleChanged -= RootOnVisibleChanged;

                IEnumerable<Menu> oldMenus = oldRoot.GetAllSubMenus ();

                foreach (Menu menu in oldMenus)
                {
                    menu.SelectedMenuItemChanged -= MenuOnSelectedMenuItemChanged;
                    Remove (menu);
                    menu.Dispose ();
                }
            }

            // Set ContentView (which handles Add/Remove and CommandBridge)
            ContentView = value;

            // Subscribe to new Root's events
            if (ContentView is { })
            {
                // When Root is hidden (e.g. via HideMenu), hide the PopoverMenu too
                ContentView.VisibleChanged += RootOnVisibleChanged;
            }
        }
    }

    #region MenuItem & SubMenu Helpers

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if attempting to add a <see cref="Menu"/> or <see cref="MenuItem"/> directly to the popover.
    /// </exception>
    /// <remarks>
    ///     Do not add <see cref="MenuItem"/> or <see cref="Menu"/> views directly to the popover.
    ///     Use the <see cref="Root"/> property instead.
    /// </remarks>
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        if (view is not Menu addedMenu)
        {
            return;
        }

        UpdateKeyBindings (addedMenu);

        IEnumerable<Menu> allMenus = addedMenu.GetAllSubMenus ();

        foreach (Menu menu in allMenus)
        {
            menu.App = App;
            menu.Visible = false;

            // Disable so keys are ignored
            //menu.Enabled = false;

            // Activate/Activated are handled via CommandBridge (Root → PopoverMenu).
            // SubMenu activations propagate up through MenuItem SubMenu bridges to Root.
            menu.SelectedMenuItemChanged += MenuOnSelectedMenuItemChanged;

            if (menu != addedMenu)
            {
                Add (menu);
            }
        }
    }

    private void MenuOnSelectedMenuItemChanged (object? sender, MenuItem? e)
    {
        // Menu.OnSelectedMenuItemChanged already handled show/hide.
        // PopoverMenu only adjusts positioning for screen boundaries.
        if (e?.SubMenu is not { Visible: true })
        {
            return;
        }

        Point idealLocation = ScreenToViewport (new Point (e.FrameToScreen ().Right - e.SubMenu.GetAdornmentsThickness ().Left,
                                                           e.FrameToScreen ().Top - e.SubMenu.GetAdornmentsThickness ().Top));

        Point pos = GetMostVisibleLocationForSubMenu (e.SubMenu, idealLocation);
        e.SubMenu.X = pos.X;
        e.SubMenu.Y = pos.Y;
    }

    /// <summary>
    ///     When Root becomes invisible, hide the PopoverMenu too.
    /// </summary>
    private void RootOnVisibleChanged (object? sender, EventArgs e)
    {
        if (sender is Menu { Visible: false } menu && menu == Root && Visible)
        {
            Visible = false;
        }
    }

    /// <summary>
    ///     Updates the key bindings for all menu items with associated commands, assigning the appropriate key based on the
    ///     target view or application context.
    /// </summary>
    /// <remarks>
    ///     This method automatically determines and assigns key bindings for menu items with commands.
    ///     If a menu item specifies a command and a target view, the key is derived from the view's hot key bindings;
    ///     otherwise, it is obtained from the application's keyboard bindings. Existing key assignments are overridden if a
    ///     valid key is found.
    /// </remarks>
    /// <param name="start">
    ///     The root menu from which to begin updating key bindings. If <see langword="null"/>, <see cref="Root"/> will be
    ///     used.
    /// </param>
    private void UpdateKeyBindings (Menu? start = null)
    {
        IEnumerable<MenuItem> all = (start ?? Root)?.GetMenuItemsOfAllSubMenus (mi => mi.Command != Command.NotBound) ?? [];

        foreach (MenuItem menuItem in all)
        {
            Key? key;

            if (menuItem.TargetView is { })
            {
                // A TargetView implies HotKey
                key = menuItem.TargetView.HotKeyBindings.GetFirstFromCommands (menuItem.Command);
            }
            else
            {
                // No TargetView implies Application HotKey
                key = App?.Keyboard.KeyBindings.GetFirstFromCommands (menuItem.Command);
            }

            if (key is not { IsValid: true })
            {
                continue;
            }

            if (menuItem.Key.IsValid)
            {
                //Logging.Warning ("Do not specify a Key for MenuItems where a Command is specified. Key will be determined automatically.");
            }

            menuItem.Key = key;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Checks all menu items in the hierarchy for a matching key binding and invokes the
    ///     appropriate menu item if found.
    /// </remarks>
    protected override bool OnKeyDownNotHandled (Key key)
    {
        // See if any of our MenuItems have this key as Key
        IEnumerable<MenuItem> all = Root?.GetMenuItemsOfAllSubMenus (mi => key != Application.GetDefaultKey (Command.Quit) && mi.Key == key) ?? [];

        foreach (MenuItem menuItem in all)
        {
            return menuItem.NewKeyDownEvent (key);
        }

        return base.OnKeyDownNotHandled (key);
    }

    /// <summary>
    ///     Shows the submenu of the specified <see cref="MenuItem"/>, if it has one.
    /// </summary>
    /// <param name="menuItem">The menu item whose submenu should be shown.</param>
    /// <remarks>
    ///     <para>
    ///         If another submenu is currently visible at the same level, it will be hidden and disabled before showing the
    ///         new one.
    ///     </para>
    ///     <para>
    ///         The submenu is positioned to the right of the menu item, adjusted to ensure full visibility on screen.
    ///     </para>
    /// </remarks>
    internal void ShowMenuItemSubMenu (MenuItem? menuItem)
    {
        var menu = menuItem?.SuperView as Menu;

        // If there's a visible peer, hide it
        if (menu?.SubViews.FirstOrDefault (v => v is MenuItem { SubMenu.Visible: true }) is MenuItem visiblePeer)
        {
            visiblePeer.SubMenu!.HideMenu ();
        }

        if (menuItem is not { SubMenu.Visible: false })
        {
            return;
        }

        menuItem.SubMenu.ShowMenu ();

        Point idealLocation = ScreenToViewport (new Point (menuItem.FrameToScreen ().Right - menuItem.SubMenu.GetAdornmentsThickness ().Left,
                                                           menuItem.FrameToScreen ().Top - menuItem.SubMenu.GetAdornmentsThickness ().Top));

        Point pos = GetMostVisibleLocationForSubMenu (menuItem.SubMenu, idealLocation);
        menuItem.SubMenu.X = pos.X;
        menuItem.SubMenu.Y = pos.Y;
    }

    /// <summary>
    ///     Calculates the most visible screen-relative location for the specified <paramref name="menu"/>.
    /// </summary>
    /// <param name="menu">The menu to position.</param>
    /// <param name="idealLocation">The ideal screen-relative location.</param>
    /// <returns>The adjusted screen-relative position that ensures maximum visibility of the menu.</returns>
    /// <remarks>
    ///     This method adjusts the position to keep the menu fully visible on screen, considering screen boundaries.
    /// </remarks>
    internal Point GetMostVisibleLocationForSubMenu (Menu menu, Point idealLocation)
    {
        // Calculate the initial position to the right of the menu item
        GetLocationEnsuringFullVisibility (menu, idealLocation.X, idealLocation.Y, out int nx, out int ny);

        return new Point (nx, ny);
    }

    #endregion

    /// <summary>
    ///     Enables the popover menu for use in design-time scenarios.
    /// </summary>
    /// <typeparam name="TContext">The type of the target view context.</typeparam>
    /// <param name="targetView">The target view to associate with the menu commands.</param>
    /// <returns><see langword="true"/> if successfully enabled for design; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    ///     This method creates a default set of menu items (Cut, Copy, Paste, Select All, Quit) for design-time use.
    ///     It is primarily used for demonstration and testing purposes.
    /// </remarks>
    public override bool EnableForDesign<TContext> (ref TContext targetView)
    {
        // Note: This menu is used by unit tests. If you modify it, you'll likely have to update
        // unit tests.

        Root = new Menu ([
                             new MenuItem (targetView as View, Command.Cut),
                             new MenuItem (targetView as View, Command.Copy),
                             new MenuItem (targetView as View, Command.Paste),
                             new Line (),
                             new MenuItem (targetView as View, Command.SelectAll),
                             new Line (),
                             new MenuItem (targetView as View, Command.Quit)
                         ])
        { Id = "enableForDesignRoot" };

        // NOTE: This is a workaround for the fact that the PopoverMenu is not visible in the designer
        // NOTE: without being activated via App?.Popover. But we want it to be visible.
        // NOTE: If you use PopoverView.EnableForDesign for real Popover scenarios, change back to false
        // NOTE: after calling EnableForDesign.
        //Visible = true;

        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     This method unsubscribes from all menu events and disposes the root menu.
    /// </remarks>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            if (Root is { } root)
            {
                root.VisibleChanged -= RootOnVisibleChanged;
            }

            IEnumerable<Menu> allMenus = Root?.GetAllSubMenus () ?? [];

            foreach (Menu menu in allMenus)
            {
                menu.SelectedMenuItemChanged -= MenuOnSelectedMenuItemChanged;

                // No need to Remove/Dispose subviews as that's done by View
            }
        }

        base.Dispose (disposing);
    }
}

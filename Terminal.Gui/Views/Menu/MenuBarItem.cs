using Terminal.Gui.Tracing;

namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="MenuItem"/>-derived item for use in a <see cref="MenuBar"/>. Each <see cref="MenuBarItem"/>
///     holds either a <see cref="PopoverMenu"/> (modal, default) or an inline <see cref="MenuItem.SubMenu"/>
///     (non-modal) that is displayed as a drop-down menu when the item is selected. The behavior is controlled
///     by the <see cref="UsePopoverMenu"/> property.
/// </summary>
/// <remarks>
///     <para>
///         When <see cref="UsePopoverMenu"/> is <see langword="true"/> (default), <see cref="MenuBarItem"/>
///         uses a <see cref="PopoverMenu"/> — a modal, overlay-rendered drop-down registered with the
///         Application popover system.
///     </para>
///     <para>
///         When <see cref="UsePopoverMenu"/> is <see langword="false"/>, <see cref="MenuBarItem"/> uses the
///         inherited <see cref="MenuItem.SubMenu"/> mechanism — a non-modal, inline drop-down rendered as a
///         sibling view of the <see cref="MenuBar"/>. This mode is designed for scenarios that need non-modal
///         menus — for example, the Terminal.Gui Designer (TGD).
///     </para>
///     <para>
///         <b>PopoverMenu Integration:</b> When <see cref="PopoverMenu"/> is set, the popover's
///         <c>Target</c> is set to this <see cref="MenuBarItem"/> (creating a <c>CommandBridge</c>
///         that bridges <see cref="Command.Activate"/> commands from the popover back to this item), and the
///         popover's <c>Anchor</c> is set to position the drop-down below this <see cref="MenuBarItem"/>.
///     </para>
///     <para>
///         <b>PopoverMenu Visibility:</b> Use <see cref="PopoverMenuOpen"/> to get or set whether the
///         <see cref="PopoverMenu"/> is visible. The <see cref="PopoverMenuOpenChanged"/> event fires when
///         visibility changes, relayed from <see cref="View.VisibleChanged"/>.
///     </para>
///     <para>
///         <b>Activation:</b> Overrides <see cref="View.OnActivating"/> to toggle the menu open/closed.
///         Bridged commands (originating from within the menu) are ignored to avoid
///         unintended toggling. A custom <see cref="Command.HotKey"/> handler skips
///         <see cref="View.SetFocus"/> before invoking <see cref="Command.Activate"/>, preventing premature
///         menu opening when switching between <see cref="MenuBarItem"/>s via HotKey.
///     </para>
///     <para>
///         <b>Application-scoped MenuItem shortcuts:</b> <see cref="EndInit"/> assigns
///         <see cref="View.App"/> on the <see cref="PopoverMenu"/> (or inline <see cref="MenuItem.SubMenu"/>)
///         and re-registers any SubView <see cref="Shortcut"/> with
///         <see cref="Shortcut.BindKeyToApplication"/> set, so <see cref="Shortcut.Key"/> bindings created
///         before App was available still register on <see cref="IKeyboard.KeyBindings"/>.
///     </para>
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/shortcut.html">Shortcut Deep Dive</see> for
///         details on the underlying command routing and BubbleDown pattern.
///     </para>
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/menus.html">Menus Deep Dive</see> for the
///         full menu system architecture, class hierarchy, command routing, and usage examples.
///     </para>
/// </remarks>
public class MenuBarItem : MenuItem, IMenuBarEntry, IDesignable
{
    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItem"/>.
    /// </summary>
    public MenuBarItem () : base (null, Command.NotBound) => SetupCommands ();

    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItem"/>. Each MenuBarItem typically has a <see cref="PopoverMenu"/>
    ///     that is
    ///     shown when the item is selected.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="targetView">
    ///     The View that <paramref name="command"/> will be invoked on when user does something that causes the MenuBarItems's
    ///     Accept event to be raised.
    /// </param>
    /// <param name="command">
    ///     The Command to invoke on <paramref name="targetView"/>. The Key <paramref name="targetView"/>
    ///     has bound to <paramref name="command"/> will be used as <see cref="Key"/>
    /// </param>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="popoverMenu">The Popover Menu that will be displayed when this item is selected.</param>
    public MenuBarItem (View? targetView, Command command, string? commandText, PopoverMenu? popoverMenu = null) : base (targetView, command, commandText)
    {
        TargetView = targetView;
        Command = command;
        PopoverMenu = popoverMenu;
        SetupCommands ();
    }

    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItem"/> with the specified <paramref name="popoverMenu"/>. This is a
    ///     helper for the most common MenuBar use-cases.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="popoverMenu">The Popover Menu that will be displayed when this item is selected.</param>
    public MenuBarItem (string commandText, PopoverMenu? popoverMenu = null) : this (null, Command.NotBound, commandText, popoverMenu) { }

    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItem"/> with the <paramref name="menuItems"/> automatcialy added to a
    ///     <see cref="PopoverMenu"/>.
    ///     This is a helper for the most common MenuBar use-cases.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="menuItems">
    ///     The menu items that will be added to the Popover Menu that will be displayed when this item is
    ///     selected.
    /// </param>
    public MenuBarItem (string commandText, IEnumerable<View> menuItems) : this (null,
                                                                                 Command.NotBound,
                                                                                 commandText,
                                                                                 new PopoverMenu (menuItems)
                                                                                 {
#if DEBUG
                                                                                     Id = $"PopoverMenu ({commandText})"
#endif
                                                                                 })
    { }

    /// <summary>
    ///     Initializes <see cref="MenuBarItem"/>-specific command handlers.
    /// </summary>
    private void SetupCommands () =>

        // Override the default HotKey handler to skip SetFocus before InvokeCommand(Activate).
        // DefaultHotKeyHandler calls SetFocus() which triggers OnSelectedMenuItemChanged on MenuBar,
        // opening the popover BEFORE Activate can toggle it — causing it to open then immediately close
        // when switching between MenuBarItems via HotKey.
        AddCommand (Command.HotKey,
                    ctx =>
                    {
                        Trace.Command (this, ctx, "Entry", "HotKey handler");

                        if (RaiseHandlingHotKey (ctx) is true)
                        {
                            Trace.Command (this, ctx, "Handled", "RaiseHandlingHotKey returned true");

                            return false;
                        }

                        RaiseHotKeyCommand (ctx);

                        Trace.Command (this, ctx, "InvokeActivate", "Before InvokeCommand(Activate)");

                        // ShowItem/HideItem in MenuBar.OnActivating handles focus.
                        InvokeCommand (Command.Activate, ctx?.Binding);

                        return true;
                    });

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        IMenuBarEntry entry = this;
        Trace.Command (this, args.Context, "Entry", $"IsMenuOpen={entry.IsMenuOpen} Routing={args.Context?.Routing}");

        if (base.OnActivating (args))
        {
            Trace.Command (this, args.Context, "BaseHandled", "base.OnActivating returned true");

            return true;
        }

        // Bridged commands come FROM the menu (e.g., a MenuItem was activated inside it).
        // This is a notification — do not toggle the menu open/closed.
        if (args.Context?.Routing == CommandRouting.Bridged)
        {
            Trace.Command (this, args.Context, "Bridged", "Ignoring bridged command — no toggle");

            return false;
        }

        if (UsePopoverMenu)
        {
            PopoverMenuOpen = !PopoverMenuOpen;
        }
        else
        {
            // When closing the inline menu, guard against reentrant reopening:
            // HideMenu() → Visible=false → focus returns to MenuBar → OnFocusedChanged →
            // OnSelectedMenuItemChanged sees _popoverBrowsingMode=true + IsMenuOpen=false → ShowEntry.
            // Setting IsSwitchingItem prevents OnSelectedMenuItemChanged from auto-opening.
            if (entry.IsMenuOpen && SuperView is MenuBar menuBar)
            {
                menuBar.IsSwitchingItem = true;

                try
                {
                    entry.IsMenuOpen = false;
                }
                finally
                {
                    menuBar.IsSwitchingItem = false;
                }
            }
            else
            {
                entry.IsMenuOpen = !entry.IsMenuOpen;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        if (!UsePopoverMenu)
        {
            // Convert PopoverMenu → SubMenu for inline mode.
            // The constructor may have created a PopoverMenu (since UsePopoverMenu is init-only
            // and set after construction). Extract menu items from its Root and create a new
            // SubMenu. We cannot reuse the Root directly because Popover.ContentView.set disposes
            // the old content view when detached.
            if (PopoverMenu?.Root is { } rootMenu && SubMenu is null)
            {
                // Collect all SubViews from the Root before PopoverMenu disposes them
                List<View> menuItems = [.. rootMenu.SubViews];

                // Remove items from Root so they aren't disposed with it
                rootMenu.RemoveAll ();

                // PopoverMenu.OnSubViewAdded adds cascading SubMenu Menus as direct SubViews
                // of PopoverMenu for positioning (and due to reentrancy in OnSubViewAdded,
                // deep cascading menus may be added multiple times). RemoveAll detaches them
                // all before Dispose can cascade View.Dispose into those SubMenus.
                PopoverMenu.RemoveAll ();

                // Now safely dispose the PopoverMenu (no SubViews left to cascade into)
                PopoverMenu.Dispose ();
                PopoverMenu = null;

                // Create a new Menu from the extracted items
                Menu inlineMenu = new (menuItems)
                {
#if DEBUG
                    Id = $"InlineMenu ({Id})"
#endif
                };

                SubMenu = inlineMenu;
            }

            if (SubMenu is { })
            {
                SubMenu.App ??= App;
                EnsureApplicationKeyBindings (SubMenu, App);

                if (!SubMenu.IsInitialized)
                {
                    SubMenu.BeginInit ();
                    SubMenu.EndInit ();
                }
            }

            SetupSubMenuNavigation ();

            return;
        }

        if (PopoverMenu is null)
        {
            return;
        }

        PopoverMenu.App ??= App;

        // MenuItems may have set BindKeyToApplication + Key before App was available on the popover.
        EnsureApplicationKeyBindings (PopoverMenu, App);

        if (PopoverMenu.IsInitialized)
        {
            return;
        }

        PopoverMenu.BeginInit ();
        PopoverMenu.EndInit ();
    }

    /// <summary>
    ///     Gets whether this entry uses a modal <see cref="PopoverMenu"/> (<see langword="true"/>, default)
    ///     or an inline <see cref="MenuItem.SubMenu"/> (<see langword="false"/>) for its dropdown.
    ///     This property must be set at construction time and cannot be changed after initialization.
    /// </summary>
    public bool UsePopoverMenu { get; init; } = true;

    /// <inheritdoc/>
    protected override Rune SubMenuGlyph => UsePopoverMenu ? Glyphs.RightArrow : default;

    /// <summary>
    ///     The Popover Menu that will be displayed when this item is selected.
    /// </summary>
    public PopoverMenu? PopoverMenu
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            if (field is { })
            {
                field.VisibleChanged -= OnPopoverMenuVisibleChanged;
                field.Target = null;
            }

            field = value;

            if (field is null)
            {
                return;
            }

            Trace.Command (this, "PopoverMenuSet", $"PopoverMenu={field.ToIdentifyingString ()}");

            // Bridge Activate/Accept from PopoverMenu → MenuBarItem via PopoverImpl.Target.
            field.Target = new WeakReference<View> (this);

            // Set Anchor for positioning below MenuBarItem
            field.Anchor = () => FrameToScreen ();

            // Relay VisibleChanged to PopoverMenuOpenChanged for consumers (e.g. MenuBar)
            field.VisibleChanged += OnPopoverMenuVisibleChanged;
        }
    }

    /// <summary>
    ///     Gets or sets whether the PopoverMenu is open and visible or not.
    ///     Delegates to <see cref="PopoverMenu"/>.<see cref="View.Visible"/>.
    /// </summary>
    public bool PopoverMenuOpen
    {
        get => PopoverMenu?.Visible ?? false;
        set
        {
            if (PopoverMenu is null)
            {
                return;
            }

            if (value)
            {
                PopoverMenu.MakeVisible ();
            }
            else
            {
                PopoverMenu.Visible = false;
            }
        }
    }

    /// <summary>
    ///     Raised when <see cref="PopoverMenuOpen"/> has changed. Relayed from
    ///     <see cref="View.VisibleChanged"/>.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<bool>>? PopoverMenuOpenChanged;

    private void OnPopoverMenuVisibleChanged (object? sender, EventArgs e)
    {
        bool isOpen = PopoverMenu?.Visible ?? false;
        PopoverMenuOpenChanged?.Invoke (this, new ValueChangedEventArgs<bool> (!isOpen, isOpen));
        MenuOpenChanged?.Invoke (this, new ValueChangedEventArgs<bool> (!isOpen, isOpen));
    }

    #region IMenuBarEntry

    /// <inheritdoc/>
    bool IMenuBarEntry.IsMenuOpen
    {
        get => UsePopoverMenu ? PopoverMenuOpen : SubMenu?.Visible ?? false;
        set
        {
            if (UsePopoverMenu)
            {
                PopoverMenuOpen = value;
            }
            else if (SubMenu is { })
            {
                if (value)
                {
                    SubMenu.ShowMenu ();
                }
                else
                {
                    SubMenu.HideMenu ();
                }
            }
        }
    }

    /// <inheritdoc/>
    event EventHandler<ValueChangedEventArgs<bool>>? IMenuBarEntry.MenuOpenChanged
    {
        add => MenuOpenChanged += value;
        remove => MenuOpenChanged -= value;
    }

    /// <inheritdoc/>
    Menu? IMenuBarEntry.RootMenu => UsePopoverMenu ? PopoverMenu?.Root : SubMenu;

    #endregion IMenuBarEntry

    /// <summary>
    ///     Raised when the menu open state has changed. In popover mode, this relays
    ///     <see cref="PopoverMenuOpenChanged"/>. In inline mode, this relays SubMenu visibility changes.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<bool>>? MenuOpenChanged;

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key key)
    {
        bool isMenuVisible = UsePopoverMenu
                                 ? PopoverMenu is { Visible: true }
                                 : SubMenu is { Visible: true };

        Trace.Keyboard (this, key, "Entry", $"MenuVisible={isMenuVisible}");

        if (!isMenuVisible || !HotKeyBindings.TryGet (key, out _))
        {
            return false;
        }

        Trace.Keyboard (this, key, "HotKeyMatch", "Menu visible + HotKey match — hiding");

        // If the user presses the hotkey for a menu item that is already open,
        // it should close the menu item (Test: MenuBarItem_HotKey_DeActivates)
        if (SuperView is MenuBar menuBar)
        {
            menuBar.HideActiveItem ();
        }

        return true;
    }

    #region Inline SubMenu Support

    private bool _subMenuNavigationSetup;

    /// <summary>
    ///     Subscribes to the SubMenu's <see cref="View.KeyDown"/> event to forward
    ///     Left/Right arrow keys to the parent <see cref="MenuBar"/> for entry switching,
    ///     and Escape for closing the menu.
    /// </summary>
    private void SetupSubMenuNavigation ()
    {
        if (SubMenu is null || _subMenuNavigationSetup)
        {
            return;
        }

        _subMenuNavigationSetup = true;

        SubMenu.KeyDown += OnSubMenuKeyDown;
    }

    private void OnSubMenuKeyDown (object? sender, Key e)
    {
        if (SuperView is not MenuBar menuBar)
        {
            return;
        }

        if (e == Key.CursorRight)
        {
            e.Handled = true;
            menuBar.InvokeCommand (Command.Right);
        }
        else if (e == Key.CursorLeft)
        {
            e.Handled = true;
            menuBar.InvokeCommand (Command.Left);
        }
        else if (e == Application.GetDefaultKey (Command.Quit))
        {
            e.Handled = true;
            menuBar.HideActiveItem ();
        }
    }

    /// <summary>
    ///     Subscribes to SubMenu visibility changes to relay them through <see cref="MenuOpenChanged"/>.
    /// </summary>
    internal void SubscribeToSubMenuVisibility ()
    {
        if (SubMenu is { })
        {
            SubMenu.VisibleChanged += OnSubMenuVisibleChanged;
        }
    }

    /// <summary>
    ///     Unsubscribes from SubMenu visibility changes.
    /// </summary>
    internal void UnsubscribeFromSubMenuVisibility ()
    {
        if (SubMenu is { })
        {
            SubMenu.VisibleChanged -= OnSubMenuVisibleChanged;
        }
    }

    private void OnSubMenuVisibleChanged (object? sender, EventArgs e)
    {
        bool isOpen = SubMenu?.Visible ?? false;
        MenuOpenChanged?.Invoke (this, new ValueChangedEventArgs<bool> (!isOpen, isOpen));
    }

    #endregion Inline SubMenu Support

    /// <inheritdoc/>
    public new bool EnableForDesign ()
    {
        PopoverMenu = new PopoverMenu ();
        PopoverMenu.Root = new Menu ();
        PopoverMenu.Root.EnableForDesign ();

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            if (UsePopoverMenu)
            {
                PopoverMenu?.Dispose ();
                PopoverMenu = null;
            }
            else
            {
                UnsubscribeFromSubMenuVisibility ();

                if (SubMenu is { })
                {
                    SubMenu.KeyDown -= OnSubMenuKeyDown;
                    SubMenu.Dispose ();
                    SubMenu = null;
                }
            }
        }

        base.Dispose (disposing);
    }
}

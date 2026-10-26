using System.ComponentModel;

namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="Shortcut"/>-derived item for use in a <see cref="Menu"/>. Displays a command, help text, and
///     key binding and supports nested <see cref="SubMenu"/>s for cascading menu hierarchies.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="MenuItem"/> extends <see cref="Shortcut"/> to add support for hierarchical menus.
///         Like <see cref="Shortcut"/>, it displays a <see cref="Shortcut.CommandView"/> (command text),
///         <see cref="Shortcut.HelpView"/> (help text), and <see cref="Shortcut.KeyView"/> (key binding).
///         When the user activates a <see cref="MenuItem"/>, the associated <see cref="Shortcut.Action"/> is invoked.
///     </para>
///     <para>
///         <b>SubMenu Support:</b> Set the <see cref="SubMenu"/> property to a <see cref="Menu"/> to create
///         cascading (nested) menus. When a <see cref="SubMenu"/> is set, a right-arrow glyph is displayed in the
///         <see cref="Shortcut.KeyView"/> and a <c>CommandBridge</c> connects the SubMenu back to this
///         <see cref="MenuItem"/>, bridging <see cref="Command.Activate"/> and <see cref="Command.Accept"/>
///         commands across the non-containment boundary.
///     </para>
///     <para>
///         <b>Command Binding:</b> A <see cref="MenuItem"/> can be bound to a <see cref="Command"/> on a
///         <see cref="Shortcut.TargetView"/>. The key that <see cref="Shortcut.TargetView"/> has bound to the
///         command will be used as the <see cref="Shortcut.Key"/>.
///     </para>
///     <para>
///         <b>Mouse Behavior:</b> When the mouse enters a <see cref="MenuItem"/>, it automatically receives focus,
///         enabling hover-to-select behavior within menus.
///     </para>
///     <para>
///         <see cref="MenuItem"/> implements <see cref="IValue"/>, exposing <see cref="View.Title"/> as its value.
///     </para>
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/shortcut.html">Shortcut Deep Dive</see> for
///         details on command routing, the BubbleDown pattern, and how <see cref="Shortcut"/> coordinates commands
///         between itself and its <see cref="Shortcut.CommandView"/>.
///     </para>
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/menus.html">Menus Deep Dive</see> for the
///         full menu system architecture, class hierarchy, command routing, and usage examples.
///     </para>
/// </remarks>
public class MenuItem : Shortcut, IValue
{
    /// <summary>
    ///     Creates a new instance of <see cref="MenuItem"/>.
    /// </summary>
    public MenuItem () : base (Key.Empty, null, null) { }

    /// <summary>
    ///     Creates a new instance of <see cref="MenuItem"/>, binding it to <paramref name="targetView"/> and
    ///     <paramref name="command"/>. The Key <paramref name="targetView"/>
    ///     has bound to <paramref name="command"/> will be used as <see cref="Key"/>.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="targetView">
    ///     The View that <paramref name="command"/> will be invoked on when user does something that causes the Shortcut's
    ///     Accept
    ///     event to be raised.
    /// </param>
    /// <param name="command">
    ///     The Command to invoke on <paramref name="targetView"/>. The Key <paramref name="targetView"/>
    ///     has bound to <paramref name="command"/> will be used as <see cref="Key"/>
    /// </param>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="helpText">The help text to display.</param>
    /// <param name="subMenu">The submenu to display when the user selects this menu item.</param>
    public MenuItem (View? targetView, Command command, string? commandText = null, string? helpText = null, Menu? subMenu = null) :
        base (targetView?.HotKeyBindings.GetFirstFromCommands (command)!,
              string.IsNullOrEmpty (commandText) ? GlobalResources.GetString ($"cmd{command}") : commandText,
              null,
              string.IsNullOrEmpty (helpText) ? GlobalResources.GetString ($"cmd{command}_Help") : helpText)
    {
        TargetView = targetView;
        Command = command;
        SubMenu = subMenu;
    }

    /// <inheritdoc/>
    public MenuItem (string? commandText = null, string? helpText = null, Action? action = null, Key? key = null) : base (key ?? Key.Empty,
        commandText,
        action,
        helpText)
    { }

    /// <inheritdoc/>
    public MenuItem (string commandText, Key key, Action? action = null) : base (key ?? Key.Empty, commandText, action) { }

    /// <inheritdoc/>
    public MenuItem (string? commandText = null, string? helpText = null, Menu? subMenu = null) : base (Key.Empty, commandText, null, helpText) =>
        SubMenu = subMenu;

    /// <summary>
    ///     Gets the glyph displayed in <see cref="Shortcut.KeyView"/> when a <see cref="SubMenu"/> is set.
    ///     The default is <see cref="Glyphs.RightArrow"/> (►). Override to change the indicator
    ///     (e.g., <see cref="Glyphs.DownArrow"/> for a drop-down menu bar entry).
    /// </summary>
    protected virtual Rune SubMenuGlyph => Glyphs.RightArrow;

    private CommandBridge? _subMenuBridge;

    /// <summary>
    ///     The submenu to display when the user selects this menu item.
    /// </summary>
    public Menu? SubMenu
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            // Tear down old bridge
            _subMenuBridge?.Dispose ();
            _subMenuBridge = null;

            field = value;

            if (field is null)
            {
                KeyView.Text = Key == Key.Empty ? string.Empty : $"{Key}";

                return;
            }

#if DEBUG
            field.Id = $"{Id}_SubMenu";
#endif

            field!.App ??= App;
            field!.Visible = false;

            Rune glyph = SubMenuGlyph;
            KeyView.Text = glyph == default (Rune) ? string.Empty : $"{glyph}";
            field.SuperMenuItem = this;

            // Bridge Activate and Accept from SubMenu → this MenuItem across the
            // non-containment boundary. SubMenu is not a SubView of this MenuItem,
            // so commands can't bubble naturally; the bridge relays completion events.
            _subMenuBridge = CommandBridge.Connect (this, field, Command.Activate, Command.Accept);
        }
    }

    /// <summary>
    ///     Ensures this <see cref="MenuItem"/> is visible and focused by opening every
    ///     ancestor menu (including the root <see cref="MenuBar"/> / <see cref="PopoverMenu"/>
    ///     if needed) and selecting this item.
    /// </summary>
    /// <param name="ensureVisible">
    ///     If <see langword="true"/> (default), marks the containing menus for redraw so the
    ///     item is brought into view when possible.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if the item is attached under a <see cref="Menu"/> and
    ///     the path was opened (a <see cref="MenuBar"/> is activated when present);
    ///     <see langword="false"/> if the item is not attached to any menu hierarchy.
    /// </returns>
    public bool OpenAndSelect (bool ensureVisible = true)
    {
        // Leaf must live in a Menu
        if (SuperView is not Menu leafMenu)
        {
            return false;
        }

        // Chain of MenuItems from outermost parent → this
        List<(Menu menu, MenuItem item)> chain = new ();
        MenuItem? current = this;
        Menu? menu = leafMenu;

        while (current is { } && menu is { })
        {
            chain.Insert (0, (menu, current));

            MenuItem? parent = menu.SuperMenuItem;

            if (parent is null)
            {
                break;
            }

            current = parent;
            menu = parent.SuperView as Menu;
        }

        // Resolve MenuBarItem / MenuBar
        MenuBarItem? barItem = null;
        MenuBar? menuBar = null;

        // 1) Inline: outermost SuperMenuItem is MenuBarItem
        if (chain [0].menu.SuperMenuItem is MenuBarItem inlineBar)
        {
            barItem = inlineBar;
            menuBar = inlineBar.SuperView as MenuBar;
        }

        // 2) SuperView walk (PopoverMenu.Target, etc.)
        if (barItem is null)
        {
            for (View? v = chain [0].menu; v is { }; v = v.SuperView)
            {
                if (v is MenuBarItem mbi)
                {
                    barItem = mbi;
                    menuBar = mbi.SuperView as MenuBar;
                    break;
                }

                if (v is PopoverMenu pop
                    && pop.Target is { } weak
                    && weak.TryGetTarget (out View? t)
                    && t is MenuBarItem targetMbi)
                {
                    barItem = targetMbi;
                    menuBar = targetMbi.SuperView as MenuBar;
                    break;
                }

                if (v is MenuBar mb)
                {
                    menuBar = mb;
                    break;
                }
            }
        }

        // 3) Popover fallback only — Root often not under MenuBar in the SuperView chain
        if (barItem is null)
        {
            Menu rootMenu = chain [0].menu;

            foreach (MenuBar mb in FindMenuBars (this))
            {
                foreach (MenuBarItem entry in mb.SubViews.OfType<MenuBarItem> ())
                {
                    Menu? owned = entry.UsePopoverMenu
                                      ? entry.PopoverMenu?.Root
                                      : entry.SubMenu;

                    if (owned is null)
                    {
                        continue;
                    }

                    if (owned == rootMenu
                        || owned.GetMenuItemsOfAllSubMenus (mi => mi == this).Any ())
                    {
                        barItem = entry;
                        menuBar = mb;
                        break;
                    }
                }

                if (barItem is { })
                {
                    break;
                }
            }
        }

        if (menuBar is { })
        {
            menuBar.Active = true;
        }

        if (barItem is { })
        {
            if (barItem.UsePopoverMenu)
            {
                barItem.PopoverMenuOpen = true;
            }
            else
            {
                ((IMenuBarEntry)barItem).IsMenuOpen = true;
            }
        }

        foreach ((Menu levelMenu, MenuItem levelItem) in chain)
        {
            if (!levelMenu.Visible)
            {
                levelMenu.ShowMenu ();
            }

            levelItem.SetFocus ();

            if (ensureVisible)
            {
                levelMenu.SetNeedsDraw ();
            }
        }

        SetFocus ();

        // Success means the item is attached under a Menu and the path was opened.
        // A MenuBar / MenuBarItem is activated when found; context-only hierarchies
        // still return true without a bar.
        return true;
    }

    private static IEnumerable<MenuBar> FindMenuBars (View start)
    {
        for (View? v = start; v is { }; v = v.SuperView)
        {
            if (v is MenuBar mb)
            {
                yield return mb;

                yield break;
            }
        }

        View? root = start.App?.TopRunnableView;

        if (root is null)
        {
            yield break;
        }

        HashSet<View> visited = new ();
        Queue<View> queue = new ();
        queue.Enqueue (root);

        while (queue.Count > 0)
        {
            View v = queue.Dequeue ();

            if (!visited.Add (v))
            {
                continue;
            }

            if (v is MenuBar mb)
            {
                yield return mb;
            }

            foreach (View child in v.SubViews)
            {
                if (!visited.Contains (child))
                {
                    queue.Enqueue (child);
                }
            }
        }
    }

    /// <inheritdoc/>
    public object GetValue () => Title;

    /// <inheritdoc/>
    public bool TrySetValueFromString (string input)
    {
        if (input is null)
        {
            return false;
        }

        Title = input;

        return true;
    }

    /// <inheritdoc/>
    event EventHandler<ValueChangedEventArgs<object?>>? IValue.ValueChangedUntyped
    {
        add
        {
            // Forward Title changes to ValueChangedUntyped
            if (value is null)
            {
                return;
            }
            bool hadHandlers = _valueChangedUntypedHandlers is { };

            _valueChangedUntypedHandlers += value;

            // Wire up the bridge only when the first handler is added
            if (hadHandlers)
            {
                return;
            }

            // Initialize last known title so OldValue is correct on first change
            _lastTitle = Title;
            TitleChanged += OnTitleChangedForValueChanged;
        }
        remove
        {
            if (value is null)
            {
                return;
            }
            _valueChangedUntypedHandlers -= value;

            if (_valueChangedUntypedHandlers is null)
            {
                TitleChanged -= OnTitleChangedForValueChanged;
            }
        }
    }

    private EventHandler<ValueChangedEventArgs<object?>>? _valueChangedUntypedHandlers;
    private string? _lastTitle;

    private void OnTitleChangedForValueChanged (object? sender, EventArgs<string> e)
    {
        string? oldTitle = _lastTitle;
        _lastTitle = e.Value;
        _valueChangedUntypedHandlers?.Invoke (this, new ValueChangedEventArgs<object?> (oldTitle, e.Value));
    }

    /// <inheritdoc/>
    protected override bool OnMouseEnter (CancelEventArgs eventArgs)
    {
        // When the mouse enters a menuitem, we set focus to it automatically.

        if (App?.Mouse.LastMousePosition is { } screenPosition && App.TopRunnableView is { } topRunnableView)
        {
            List<View?> viewsUnderMouse = topRunnableView.GetViewsUnderLocation (screenPosition, ViewportSettingsFlags.TransparentMouse);

            // GetViewsUnderLocation returns views ordered by depth (deepest last),
            // so walk from the end to find the deepest MenuItem.
            for (int i = viewsUnderMouse.Count - 1; i >= 0; i--)
            {
                if (viewsUnderMouse [i] is MenuItem deepestMenuItem)
                {
                    if (deepestMenuItem != this)
                    {
                        return base.OnMouseEnter (eventArgs);
                    }

                    break;
                }
            }
        }

        // Logging.Trace($"OnEnter {this.ToIdentifyingString ()}");
        SetFocus ();

        return base.OnMouseEnter (eventArgs);
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            _subMenuBridge?.Dispose ();
            _subMenuBridge = null;

            SubMenu?.Dispose ();
            SubMenu = null;
        }

        base.Dispose (disposing);
    }
}

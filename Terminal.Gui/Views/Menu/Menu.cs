using Terminal.Gui.Tracing;

namespace Terminal.Gui.Views;

/// <summary>
///     A vertically-oriented <see cref="Bar"/> that contains <see cref="MenuItem"/> items, supporting cascading
///     sub-menus, selection tracking, and the <see cref="IValue{T}"/> pattern.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Menu"/> extends <see cref="Bar"/> with vertical orientation and specializes it for
///         <see cref="MenuItem"/> items. It supports <see cref="Line"/> separators between items and uses the
///         <see cref="SchemeManager.SchemesToSchemeName">Schemes.Menu</see> color scheme by default.
///     </para>
///     <para>
///         <b>Selection Tracking:</b> The <see cref="SelectedMenuItem"/> property tracks the currently focused
///         <see cref="MenuItem"/>. The <see cref="SelectedMenuItemChanged"/> event fires when the selection changes.
///         <see cref="OnSelectedMenuItemChanged"/> automatically hides peer SubMenus and shows the selected item's
///         <see cref="MenuItem.SubMenu"/> with positioning relative to the parent <see cref="Menu"/>.
///     </para>
///     <para>
///         <b>Command Propagation:</b> Sets <see cref="View.CommandsToBubbleUp"/> to
///         [<see cref="Command.Accept"/>, <see cref="Command.Activate"/>], enabling commands from
///         <see cref="MenuItem"/> items to propagate up through the menu hierarchy (e.g., through
///         <see cref="PopoverMenu"/> and back to a <see cref="MenuBarItem"/>). Overrides
///         <see cref="View.OnActivating"/> to dispatch <see cref="Command.Activate"/> to the focused
///         <see cref="MenuItem"/>.
///     </para>
///     <para>
///         <b>IValue Support:</b> Implements <see cref="IValue{T}"/> where <c>T</c> is <see cref="MenuItem"/>.
///         The <see cref="Value"/> property is automatically set when a <see cref="MenuItem"/> is activated or
///         accepted, enabling callers to determine which item triggered the command.
///     </para>
///     <para>
///         <b>Hierarchy Traversal:</b> Use <see cref="GetAllSubMenus"/> for depth-first traversal of the
///         SubMenu hierarchy, and <see cref="GetMenuItemsOfAllSubMenus"/> to collect all <see cref="MenuItem"/>s
///         across the hierarchy with an optional predicate filter.
///     </para>
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/shortcut.html">Shortcut Deep Dive</see> for
///         details on the <see cref="Shortcut"/> base class and command routing patterns.
///     </para>
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/menus.html">Menus Deep Dive</see> for the
///         full menu system architecture, class hierarchy, command routing, and usage examples.
///     </para>
/// </remarks>
public class Menu : Bar, IValue<MenuItem?>
{
    /// <summary>
    ///     Gets or sets the default Border Style for Menus. The default is <see cref="LineStyle.None"/>.
    /// </summary>
    public static LineStyle DefaultBorderStyle => MenuSettings.Current.DefaultBorderStyle;

    /// <inheritdoc/>
    public Menu () : this ([]) { }

    /// <inheritdoc/>
    public Menu (IEnumerable<MenuItem>? menuItems) : this (menuItems?.Cast<View> ()) { }

    /// <inheritdoc/>
    public Menu (IEnumerable<View>? shortcuts) : base (shortcuts)
    {
        // Do this to support debugging traces where Title gets set
        base.HotKeySpecifier = (Rune)'\xffff';

        Orientation = Orientation.Vertical;
        Width = Dim.Auto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);
        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Menu);

        Border.Settings &= ~BorderSettings.Title;

        BorderStyle = DefaultBorderStyle;

        CommandsToBubbleUp = [Command.Accept, Command.Activate];

        KeyBindings.Clear ();
        MouseBindings.Clear ();

        ThemeChanges.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged (object? sender, App.EventArgs<string> e)
    {
        if (SuperView is { })
        {
            BorderStyle = DefaultBorderStyle;
        }
    }

    /// <summary>
    ///     Gets or sets the menu item that opened this menu as a sub-menu.
    /// </summary>
    public MenuItem? SuperMenuItem { get; set; }

    /// <inheritdoc/>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        // When a MenuItem's Accept command bubbles up, capture it as our Value
        // before calling base, which will bubble it further.
        if (args.Context?.Routing == CommandRouting.BubblingUp && args.Context.Source?.TryGetTarget (out View? source) == true && source is MenuItem menuItem)
        {
            Value = menuItem;
        }

        return base.OnAccepting (args);
    }

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        Trace.Command (this, args.Context, "Entry", $"Routing={args.Context?.Routing} Cmd={args.Context?.Command}");

        if (base.OnActivating (args) || args.Handled)
        {
            return true;
        }

        // When a MenuItem's activation bubbles up, capture it as our Value and let normal bubbling proceed.
        // MenuItem now implements IValue (returning Title), so ctx.Value is already populated by the framework.
        if (args.Context?.Routing == CommandRouting.BubblingUp)
        {
            if (args.Context.Source?.TryGetTarget (out View? source) == true && source is MenuItem menuItem)
            {
                Value = menuItem;
            }

            return false;
        }

        // Dispatch Activate to the focused MenuItem. This enables callers to invoke
        // menu.InvokeCommand(Activate) and have it reach the selected MenuItem and its CommandView.
        if (Focused is not MenuItem focusedMenuItem)
        {
            return false;
        }

        KeyBinding binding = new ([Command.Activate]);
        WeakReference<View> sourceRef = new (this);
        CommandContext ctx = new (Command.Activate, sourceRef, binding);
        focusedMenuItem.InvokeCommand (Command.Activate, ctx);

        return true;
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        switch (view)
        {
            case MenuItem menuItem:
                {
                    menuItem.CanFocus = true;

                    // Accept propagation is handled by CommandsToBubbleUp=[Accept] (line 36).
                    // An explicit Accepting subscription here caused double-fire of Accepted.

                    break;
                }

            case Line line:
                // Grow line so we get auto-join line
                line.X = Pos.Func (_ => -Border.Thickness.Left);
                line.Width = Dim.Fill () + Dim.Func (_ => Border.Thickness.Right);

                break;
        }
    }

    /// <inheritdoc/>
    protected override void OnFocusedChanged (View? previousFocused, View? focused)
    {
        base.OnFocusedChanged (previousFocused, focused);
        RaiseSelectedMenuItemChanged (SelectedMenuItem);
    }

    /// <summary>
    ///     Gets the currently selected menu item. This is a helper that
    ///     tracks <see cref="View.Focused"/>.
    /// </summary>
    public MenuItem? SelectedMenuItem => (Focused ?? PreviouslyFocused) as MenuItem;

    internal void RaiseSelectedMenuItemChanged (MenuItem? selected)
    {
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({selected?.Title})");
        Trace.Command (this, "Handler", $"{selected?.ToIdentifyingString ()}");

        OnSelectedMenuItemChanged (selected);
        SelectedMenuItemChanged?.Invoke (this, selected);
    }

    /// <summary>
    ///     Called when the selected menu item has changed. Handles hiding peer SubMenus
    ///     and showing the selected item's SubMenu.
    /// </summary>
    /// <param name="selected">The newly selected <see cref="MenuItem"/>, or <see langword="null"/> if none.</param>
    protected virtual void OnSelectedMenuItemChanged (MenuItem? selected)
    {
        // Hide any visible peer SubMenus
        foreach (MenuItem mi in SubViews.OfType<MenuItem> ().Where (mi => mi != selected && mi.SubMenu is { Visible: true }))
        {
            mi.SubMenu!.HideMenu ();
        }

        if (selected?.SubMenu is not { Visible: false })
        {
            return;
        }

        // If SubMenu has no SuperView yet, add it to our SuperView
        if (selected.SubMenu.SuperView is null && SuperView is { })
        {
            SuperView.Add (selected.SubMenu);
        }

        selected.SubMenu.ShowMenu ();

        // Generic positioning: right of this Menu, at the MenuItem's Y
        if (selected.SubMenu.SuperView is null)
        {
            return;
        }
        Point screenPos = new (selected.FrameToScreen ().Right, selected.FrameToScreen ().Top);
        Point localPos = selected.SubMenu.SuperView.ScreenToViewport (screenPos);
        selected.SubMenu.X = localPos.X;
        selected.SubMenu.Y = localPos.Y;
    }

    /// <summary>
    ///     Raised when the selected menu item has changed.
    /// </summary>
    public event EventHandler<MenuItem?>? SelectedMenuItemChanged;

    /// <summary>
    ///     Gets all the submenus in this menu's hierarchy, including this menu.
    /// </summary>
    /// <returns>An enumerable collection of all <see cref="Menu"/> instances in the hierarchy.</returns>
    /// <remarks>
    ///     This method performs a depth-first traversal of the menu tree starting from <see langword="this"/>.
    /// </remarks>
    public IEnumerable<Menu> GetAllSubMenus ()
    {
        List<Menu> result = [];
        Stack<Menu> stack = new ();
        stack.Push (this);

        while (stack.Count > 0)
        {
            Menu currentMenu = stack.Pop ();
            result.Add (currentMenu);

            foreach (View subView in currentMenu.SubViews)
            {
                if (subView is MenuItem { SubMenu: { } } menuItem)
                {
                    stack.Push (menuItem.SubMenu);
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Gets menu items in this menu's hierarchy, optionally filtered by <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">
    ///     If provided, only <see cref="MenuItem"/>s matching the predicate are returned.
    ///     If <see langword="null"/>, all menu items are returned.
    /// </param>
    /// <returns>The matching <see cref="MenuItem"/> instances across all menus in the hierarchy.</returns>
    /// <remarks>
    ///     This method traverses all menus returned by <see cref="GetAllSubMenus"/> and collects their menu items.
    /// </remarks>
    public IEnumerable<MenuItem> GetMenuItemsOfAllSubMenus (Func<MenuItem, bool>? predicate = null)
    {
        List<MenuItem> result = [];

        foreach (Menu menu in GetAllSubMenus ())
        {
            foreach (View subView in menu.SubViews)
            {
                if (subView is MenuItem menuItem && (predicate is null || predicate (menuItem)))
                {
                    result.Add (menuItem);
                }
            }
        }

        return result;
    }

    #region ShowMenu / HideMenu

    /// <summary>
    ///     Shows this menu by setting <see cref="View.Visible"/> and <see cref="View.Enabled"/> to <see langword="true"/>.
    ///     If the menu has not been initialized, initialization is performed first.
    /// </summary>
    internal void ShowMenu ()
    {
        if (Visible)
        {
            return;
        }

        if (!IsInitialized)
        {
            BeginInit ();
            EndInit ();
        }

        ClearFocus ();

        // IMPORTANT: This must be done after adding the menu to the SuperView or Add will try
        // to set focus to it.
        Visible = true;
    }

    private bool _isHiding;

    /// <summary>
    ///     Hides this menu and any visible SubMenus by setting <see cref="View.Visible"/> and
    ///     <see cref="View.Enabled"/> to <see langword="false"/>.
    /// </summary>
    internal void HideMenu ()
    {
        if (!Visible || _isHiding)
        {
            return;
        }

        _isHiding = true;

        try
        {
            // If there's a visible SubMenu, hide it first (deepest first)
            if (SubViews.FirstOrDefault (v => v is MenuItem { SubMenu.Visible: true }) is MenuItem visiblePeer)
            {
                _isHiding = false;
                visiblePeer.SubMenu!.HideMenu ();
                _isHiding = true;
            }

            Visible = false;

            ClearFocus ();
        }
        finally
        {
            _isHiding = false;
        }
    }

    #endregion ShowMenu / HideMenu

    #region IValue<MenuItem?> Implementation

    private MenuItem? _value;

    /// <summary>
    ///     Gets or sets the most recently activated <see cref="MenuItem"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is automatically set when a <see cref="MenuItem"/> within this menu raises
    ///         the <see cref="View.Accepted"/> event (e.g., when the user presses Enter or double-clicks on a menu item).
    ///     </para>
    ///     <para>
    ///         The value is captured before the <see cref="Command.Accept"/> command propagates up the hierarchy,
    ///         enabling command handlers to access <see cref="ICommandContext.Value"/> to determine which
    ///         menu item triggered the command.
    ///     </para>
    ///     <para>
    ///         Setting this property programmatically will raise <see cref="ValueChanging"/> and <see cref="ValueChanged"/>
    ///         events following the Cancellable Work Pattern (CWP).
    ///     </para>
    /// </remarks>
    public MenuItem? Value { get => _value; set => ChangeValue (value); }

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<MenuItem?>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<MenuItem?>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Called when <see cref="Value"/> is changing.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<MenuItem?> args) => false;

    /// <summary>
    ///     Called when <see cref="Value"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<MenuItem?> args) { }

    /// <summary>
    ///     INTERNAL Sets Value.
    /// </summary>
    /// <param name="newValue">The new value.</param>
    /// <returns>
    ///     <see langword="true"/> if state change was canceled, <see langword="false"/> if the state changed, and
    ///     <see langword="null"/> if the state was not changed for some other reason.
    /// </returns>
    private void ChangeValue (MenuItem? newValue)
    {
        if (_value == newValue)
        {
            return;
        }

        MenuItem? oldValue = _value;

        ValueChangingEventArgs<MenuItem?> changingArgs = new (oldValue, newValue);

        if (OnValueChanging (changingArgs) || changingArgs.Handled)
        {
            return;
        }

        ValueChanging?.Invoke (this, changingArgs);

        if (changingArgs.Handled)
        {
            return;
        }

        _value = newValue;

        ValueChangedEventArgs<MenuItem?> changedArgs = new (oldValue, _value);
        OnValueChanged (changedArgs);
        ValueChanged?.Invoke (this, changedArgs);

        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, _value));
    }

    #endregion IValue<MenuItem?> Implementation

    /// <inheritdoc/>
    public override bool EnableForDesign ()
    {
        // Note: This menu is used by unit tests and the Menus scenario.
        // If you modify it, you'll likely have to update unit tests.

        Id = "enableForDesignMenu";

        MenuItem formatItem = new ()
        {
            Title = "_Format",
            Text = "Text formatting options",
            SubMenu = new Menu ([
                                    new MenuItem { Title = "_Bold", Text = "Bold text", Key = Key.B.WithCtrl },
                                    new MenuItem { Title = "_Italic", Text = "Italic text", Key = Key.I.WithAlt },
                                    new MenuItem { Title = "_Underline", Text = "Underline text", Key = Key.U.WithCtrl }
                                ])
            {
                Id = "FormatingSubMenu"
            }
        };

        MenuItem viewItem = new ()
        {
            Title = "_View",
            Text = "View options",
            SubMenu = new Menu ([
                                    new MenuItem { Title = "_Zoom In", Text = "Zoom in", Key = Key.D0.WithCtrl },
                                    new MenuItem { Title = "Zoom _Out", Text = "Zoom out", Key = Key.D9.WithCtrl },
                                    new Line (),
                                    new MenuItem
                                    {
                                        Title = "_Layout",
                                        Text = "Layout options",
                                        SubMenu = new Menu ([
                                                                new MenuItem { Title = "_Horizontal", Text = "Horizontal layout" },
                                                                new MenuItem { Title = "_Vertical", Text = "Vertical layout" }
                                                            ])
                                        {
                                            Id = "LayoutSubMenu"
                                        }
                                    }
                                ])
            {
                Id = "ViewSubMenu"
            }
        };

        MenuItem aboutItem = new () { Title = "_About", Text = "About this demo" };

        Add (formatItem, viewItem, new Line (), aboutItem);

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        if (disposing)
        {
            ThemeChanges.ThemeChanged -= OnThemeChanged;
        }
    }
}

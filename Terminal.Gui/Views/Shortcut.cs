namespace Terminal.Gui.Views;

/// <summary>
///     Displays a command, help text, and a key binding. Serves as the foundational building block for
///     <see cref="Bar"/>, <see cref="Menu"/>, <see cref="MenuBar"/>, and <see cref="StatusBar"/>.
/// </summary>
/// <remarks>
/// <img src="../images/views/Shortcut.gif" alt="Shortcut demo"/>
///     <para>
///         A <see cref="Shortcut"/> is a composite view containing three subviews:
///         <see cref="CommandView"/> (command text and hotkey, left side by default),
///         <see cref="HelpView"/> (help text, middle), and
///         <see cref="KeyView"/> (key binding display, right side).
///         From the user's perspective, a <see cref="Shortcut"/> acts as a single control—clicking anywhere
///         on it produces the same result regardless of which subview was hit.
///     </para>
///     <para>
///         <b>Commands:</b> <see cref="Shortcut"/> participates in the standard <see cref="Command"/> system:
///     </para>
///     <list type="table">
///         <listheader>
///             <term>Command</term>
///             <description>Trigger and behavior</description>
///         </listheader>
///         <item>
///             <term><see cref="Command.Activate"/></term>
///             <description>
///                 Triggered by <c>Space</c>, mouse click, or <see cref="Key"/> press. Changes state
///                 (e.g., toggles a <see cref="CheckBox"/>) and invokes <see cref="Action"/>.
///             </description>
///         </item>
///         <item>
///             <term><see cref="Command.Accept"/></term>
///             <description>
///                 Triggered by <c>Enter</c> or double-click. When initiated from a key binding,
///                 <see cref="Command.Accept"/> is converted to <see cref="Command.Activate"/> so it flows
///                 through the menu/bridge architecture correctly.
///             </description>
///         </item>
///         <item>
///             <term><see cref="Command.HotKey"/></term>
///             <description>
///                 Triggered by the HotKey letter in <see cref="CommandView"/> or by <see cref="Key"/>.
///                 Sets focus, then invokes <see cref="Command.Activate"/>.
///             </description>
///         </item>
///     </list>
///     <para>
///         <b>Relay Dispatch:</b> <see cref="Shortcut"/> uses relay dispatch
///         (<c>GetDispatchTarget =&gt; <see cref="CommandView"/></c>, <c>ConsumeDispatch = false</c>).
///         Commands dispatched to <see cref="CommandView"/> complete normally (e.g., <see cref="CheckBox"/>
///         toggles), then <see cref="Shortcut"/> is notified via a deferred callback.
///         Sets <see cref="View.CommandsToBubbleUp"/> to
///         [<see cref="Command.Activate"/>, <see cref="Command.Accept"/>], enabling commands from
///         <see cref="CommandView"/> to bubble up to the <see cref="Shortcut"/> for centralized handling.
///     </para>
///     <para>
///         <b>Replaceable CommandView:</b> The <see cref="CommandView"/> can be replaced with any
///         <see cref="View"/> (e.g., <see cref="CheckBox"/>, <see cref="Button"/>). The
///         <see cref="Shortcut"/> automatically adapts its activation behavior to the <see cref="CommandView"/>.
///     </para>
///     <para>
///         <b>Application-Wide Key:</b> If <see cref="BindKeyToApplication"/> is <see langword="true"/>,
///         <see cref="Key"/> will invoke <see cref="Command.HotKey"/> (and thus <see cref="Command.Activate"/>)
///         regardless of what <see cref="View"/> has focus, enabling an application-wide keyboard shortcut.
///     </para>
///     <para>
///         <b>Layout:</b> By default, a <see cref="Shortcut"/> displays the command text on the left,
///         help text in the middle, and key binding on the right. Set <see cref="AlignmentModes"/> to
///         <see cref="ViewBase.AlignmentModes.EndToStart"/> to reverse the order.
///         The command text can be set via <see cref="View.Title"/> or the <see cref="CommandView"/>'s
///         <see cref="View.Text"/> property. The help text can be set via <see cref="HelpText"/> or
///         <see cref="View.Text"/>. The key text is set via <see cref="Key"/>; if <see cref="Key"/> is
///         <see cref="Key.Empty"/>, the key text is not displayed.
///     </para>
///     <para>
///         <see cref="View.MouseHighlightStates"/> defaults to <see cref="MouseState.In"/>, causing the
///         <see cref="Shortcut"/> to highlight when the mouse is over it.
///     </para>
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/shortcut.html">Shortcut Deep Dive</see>
///         for detailed information on command routing, the BubbleDown pattern, dispatch flows, and
///         <see cref="CommandView"/> variant behaviors.
///     </para>
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/menus.html">Menus Deep Dive</see> for
///         how <see cref="Shortcut"/> fits into the menu system class hierarchy.
///     </para>
/// </remarks>
public class Shortcut : View, IOrientation, IDesignable
{
    /// <summary>
    ///     Creates a new instance of <see cref="Shortcut"/>.
    /// </summary>
    public Shortcut () : this (Key.Empty, null, null) { }

    /// <summary>
    ///     Creates a new instance of <see cref="Shortcut"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a helper API that mimics the V1 API for creating StatusItems.
    ///     </para>
    /// </remarks>
    /// <param name="key"></param>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="action"></param>
    /// <param name="helpText">The help text to display.</param>
    public Shortcut (Key key, string? commandText, Action? action, string? helpText = null)
    {
        MouseHighlightStates = MouseState.In;
        CanFocus = true;

        Border.Settings &= ~BorderSettings.Title;

        Width = GetWidthDimAuto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new OrientationHelper (this);
        _orientationHelper.OrientationChanging += (_, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (_, e) => OrientationChanged?.Invoke (this, e);

        CommandsToBubbleUp = [Command.Activate, Command.Accept];

        TitleChanged += Shortcut_TitleChanged; // This needs to be set before CommandView is set

        CommandView = new View
        {
#if DEBUG
            Id = "CommandView",
#endif
            Frame = new (0, 0, 2, 1),
            Width = Dim.Auto (),
            Height = Dim.Fill ()
        };
        Title = commandText ?? string.Empty;

#if DEBUG
        HelpView.Id = "_helpView";
#endif
        HelpView.Text = helpText ?? string.Empty;
        SetTextDirect (helpText ?? string.Empty);
        HelpView.GettingAttributeForRole += SubViewOnGettingAttributeForRole;

#if DEBUG
        KeyView.Id = "_keyView";
#endif
        KeyView.GettingAttributeForRole += (_, args) =>
                                           {
                                               if (args.Role != VisualRole.Normal)
                                               {
                                                   return;
                                               }

                                               args.Result = SuperView?.GetAttributeForRole (HasFocus ? VisualRole.HotFocus : VisualRole.HotNormal)
                                                             ?? Attribute.Default;
                                               args.Handled = true;
                                           };

        KeyView.ClearingViewport += (_, args) =>
                                    {
                                        // Do not clear; otherwise spaces will be printed with underlines
                                        args.Cancel = true;
                                    };
        Key = key;
        Action = action;

        ShowHide ();
    }

    // This is used to calculate the minimum width of the Shortcut when Width is NOT Dim.Auto
    // It is calculated by setting Width to DimAuto temporarily and forcing layout.
    // Once Frame.Width gets below this value, LayoutStarted makes HelpView an KeyView smaller.
    private int? _minimumNaturalWidth;

    /// <summary>
    ///     Gets or sets the <see cref="AlignmentModes"/> for this <see cref="Shortcut"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The default is <see cref="AlignmentModes.StartToEnd"/>. This means that the CommandView will be on the left,
    ///         HelpView in the middle, and KeyView on the right.
    ///     </para>
    /// </remarks>
    public AlignmentModes AlignmentModes
    {
        get;
        set
        {
            field = value;
            SetCommandViewDefaultLayout ();
            SetHelpViewDefaultLayout ();
            SetKeyViewDefaultLayout ();
        }
    } = AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast;

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();
        UpdateKeyBindings (Key.Empty);
    }

    // When layout starts, we need to adjust the layout of the HelpView and KeyView
    /// <inheritdoc/>
    protected override void OnSubViewLayout (LayoutEventArgs e)
    {
        base.OnSubViewLayout (e);

        ShowHide ();
        ForceCalculateNaturalWidth ();

        if (Width.Has<DimAuto> (out _))
        {
            return;
        }

        // Frame.Width is smaller than the natural width. Reduce width of HelpView.
        _maxHelpWidth = int.Max (0, GetContentWidth () - (CommandView?.Frame.Width ?? 0) - KeyView.Frame.Width);

        if (_maxHelpWidth < 3)
        {
            Thickness t = GetMarginThickness ();

            HelpView.Margin.Thickness = _maxHelpWidth switch
            {
                0 or 1 =>

                    // Scrunch it by removing both margins
                    new Thickness (t.Right - 1, t.Top, t.Left - 1, t.Bottom),
                2 =>

                    // Scrunch just the right margin
                    new Thickness (t.Right, t.Top, t.Left - 1, t.Bottom),
                _ => HelpView.Margin.Thickness
            };
        }
        else
        {
            // Reset to default
            HelpView.Margin.Thickness = GetMarginThickness ();

            // Margin must be transparent to mouse, so clicks pass through to Shortcut
            HelpView.Margin.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
        }
    }

    // Helper to set Width consistently
    internal Dim GetWidthDimAuto () => Dim.Auto (DimAutoStyle.Content, Dim.Func (_ => _minimumNaturalWidth ?? 0), Dim.Func (_ => _minimumNaturalWidth ?? 0));

    // When one of the subviews is "empty" we don't want to show it. So we
    // Use Add/Remove. We need to be careful to add them in the right order
    // so Pos.Align works correctly.
    internal void ShowHide ()
    {
        if (CommandView?.Visible == true)
        {
            if (CommandView.SuperView is null)
            {
                Add (CommandView);
                SetCommandViewDefaultLayout ();
            }
        }
        else
        {
            if (CommandView?.SuperView is { })
            {
                Remove (CommandView);
            }
        }

        if (HelpView.Visible && !string.IsNullOrEmpty (HelpView.Text))
        {
            if (HelpView.SuperView is null)
            {
                Add (HelpView);
                SetHelpViewDefaultLayout ();
            }
        }
        else
        {
            if (HelpView.SuperView is { })
            {
                Remove (HelpView);
            }
        }

        if (KeyView.Visible && (Key != Key.Empty || KeyView.Text != string.Empty))
        {
            if (KeyView.SuperView is null)
            {
                Add (KeyView);
                SetKeyViewDefaultLayout ();
            }
        }
        else
        {
            if (KeyView.SuperView is { })
            {
                Remove (KeyView);
            }
        }

        MoveSubViewToStart (KeyView);
        MoveSubViewToStart (HelpView);

        if (CommandView is { })
        {
            MoveSubViewToStart (CommandView);
        }
    }

    // Force Width to DimAuto to calculate natural width and then set it back
    private void ForceCalculateNaturalWidth ()
    {
        // Get the natural size of each subview
        Size screenSize = App?.Screen.Size ?? new Size (2048, 2048);
        CommandView?.SetRelativeLayout (screenSize);
        HelpView.SetRelativeLayout (screenSize);
        KeyView.SetRelativeLayout (screenSize);

        _minimumNaturalWidth = PosAlign.CalculateMinDimension (0, SubViews, Dimension.Width);

        // Reset our relative layout
        SetRelativeLayout (SuperView?.GetContentSize () ?? screenSize);
    }

    // TODO: Enable setting of the margin thickness
    private static Thickness GetMarginThickness () => new (1, 0, 1, 0);

    #region Accept/Activate/HotKey Command Handling

    /// <summary>
    ///     Shortcut dispatches all commands to <see cref="CommandView"/>. The framework handles:
    ///     <list type="bullet">
    ///         <item>Source guard (skip if source is already within CommandView)</item>
    ///         <item>Programmatic guard (skip if no binding)</item>
    ///     </list>
    /// </summary>
    protected override View? GetDispatchTarget (ICommandContext? ctx) => CommandView;

    // ConsumeDispatch defaults to false — CommandView completes its own activation
    // (e.g., CheckBox.OnActivated calls AdvanceCheckState).

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);

        Action?.Invoke ();

        // Translate the incoming command to Command via immutable context
        ICommandContext? targetCtx = ctx;

        if (Command != Command.NotBound && ctx is CommandContext cc)
        {
            targetCtx = cc.WithCommand (Command);
        }

        InvokeOnTargetOrApp (targetCtx);
    }

    private void InvokeOnTargetOrApp (ICommandContext? ctx)
    {
        View? target = TargetView ?? GetTopSuperView ();

        if (target is { })
        {
            target.InvokeCommand (Command, ctx);

            return;
        }

        if (!Key.IsValid || Command == Command.NotBound)
        {
            return;
        }

        // Is this an Application-bound command?
        App?.Keyboard.InvokeCommandsBoundToKey (Key);
    }

    /// <remarks>
    ///     Enter means "activate this item" — not
    ///     "accept/submit". When Accept arrives from a key binding (e.g. Enter), translates it
    ///     into <see cref="Command.Activate"/> so it flows through the bridge architecture
    ///     (MenuItem → Menu → CommandBridge → PopoverMenu), dismissing the menu.
    ///     Without this, Accept would bubble past the menu hierarchy to the host view,
    ///     triggering unintended exit behavior.
    /// </remarks>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        // Only convert key-binding-initiated Accept (e.g. Enter key).
        // Programmatic InvokeCommand(Accept) without a binding should flow normally.
        if (args.Context?.Binding is not KeyBinding)
        {
            return base.OnAccepting (args);
        }

        // Convert Accept → Activate. The binding is preserved so TryDispatchToTarget's
        // relay-dispatch guard (ConsumeDispatch=false && binding is null) passes and
        // the CommandView still receives Activate (e.g. CheckBox toggles).
        InvokeCommand (Command.Activate, args.Context.Binding);

        return true;
    }

    /// <summary>
    ///     Gets or sets the action to be invoked when the Shortcut is Activated.
    /// </summary>
    public Action? Action { get; set; }

    #endregion Accept/Activate/HotKey Command Handling

    #region IOrientation members

    private readonly OrientationHelper _orientationHelper;

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/> for this <see cref="Bar"/>. The default is
    ///     <see cref="Orientation.Horizontal"/>.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public Orientation Orientation { get => _orientationHelper.Orientation; set => _orientationHelper.Orientation = value; }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation"></param>
    public void OnOrientationChanged (Orientation newOrientation) => SetNeedsLayout ();

    #endregion

    #region Command

    private View? _commandView;

    /// <summary>
    ///     Gets or sets the View that displays the command text and hotkey.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    ///     <para>
    ///         By default, the <see cref="View.Title"/> of the <see cref="CommandView"/> is displayed as the Shortcut's
    ///         command text.
    ///     </para>
    ///     <para>
    ///         By default, the CommandView is a <see cref="View"/> with <see cref="View.CanFocus"/> set to
    ///         <see langword="false"/>.
    ///     </para>
    ///     <para>
    ///         Setting the <see cref="CommandView"/> will add it to the <see cref="Shortcut"/> and remove any existing
    ///         <see cref="CommandView"/>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This example illustrates how to add a <see cref="Shortcut"/> to a <see cref="StatusBar"/> that toggles the
    ///         <see cref="IDriver.Force16Colors"/> property.
    ///     </para>
    ///     <code>
    ///     var force16ColorsShortcut = new Shortcut
    ///     {
    ///         Key = Key.F6,
    ///         KeyBindingScope = KeyBindingScope.HotKey,
    ///         CommandView = new CheckBox { Text = "Force 16 Colors" }
    ///     };
    ///     var cb = force16ColorsShortcut.CommandView as CheckBox;
    ///     cb.Checked = Application.Force16Colors;
    /// 
    ///     cb.Toggled += (s, e) =>
    ///     {
    ///         var cb = s as CheckBox;
    ///         App.Driver.Force16Colors = cb!.Checked == true;
    ///         App.river.Refresh();
    ///     };
    ///     StatusBar.Add(force16ColorsShortcut);
    /// </code>
    /// </example>
    public View? CommandView
    {
        get => _commandView;
        set
        {
            // Clean up old
            _commandView?.GettingAttributeForRole -= SubViewOnGettingAttributeForRole;
            Remove (_commandView);
            _commandView?.Dispose ();

            // Set new
            _commandView = value;

#if DEBUG
            if (string.IsNullOrEmpty (_commandView?.Id))
            {
                _commandView?.Id = "_commandView";
            }
#endif
            _commandView?.GettingAttributeForRole += SubViewOnGettingAttributeForRole;

            // If the CommandView has a hotkey, we use that. Otherwise, we use '_' to indicate the hotkey is in the Title.
            if (_commandView?.HotKey != Key.Empty)
            {
                HotKeySpecifier = (Rune)'\xffff';
            }
            else
            {
                HotKeySpecifier = (Rune)'_';
            }
            Title = _commandView?.Text ?? string.Empty;

            UpdateKeyBindings (Key.Empty);
            UpdateMouseBindings ();
            ShowHide ();
        }
    }

    /// <summary>
    ///     INTERNAL: Clone the mouse bindings of CommandView to ensure Shortcut mouse activation behavior
    ///     is the same.
    /// </summary>
    private void UpdateMouseBindings ()
    {
        // BUGBUG: If CommandView changes MouseBindings after being set, this will not be updated.
        // BUGBUG: There is currently no event for us to subscribe to in order to detect this.

        MouseBindings.Clear ();

        foreach (KeyValuePair<MouseFlags, MouseBinding> mb in CommandView?.MouseBindings.GetBindings () ?? [])
        {
            MouseBindings.Add (mb.Key, mb.Value);
        }
    }

    private void SetCommandViewDefaultLayout ()
    {
        CommandView?.Margin.Thickness = GetMarginThickness ();

        // Margin must be transparent to mouse, so clicks pass through to Shortcut
        CommandView?.Margin.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

        CommandView?.X = Pos.Align (Alignment.End, AlignmentModes);

        CommandView?.VerticalTextAlignment = Alignment.Center;
        CommandView?.TextAlignment = Alignment.Start;
        CommandView?.TextFormatter.WordWrap = false;

        CommandView?.MouseHighlightStates = MouseState.None;
        CommandView?.GettingAttributeForRole += SubViewOnGettingAttributeForRole;
    }

    private void SubViewOnGettingAttributeForRole (object? sender, VisualRoleEventArgs e)
    {
        var subView = sender as View;

        if (subView is null)
        {
            return;
        }

        switch (e.Role)
        {
            case VisualRole.Normal:
                if (subView.HasFocus)
                {
                    e.Handled = true;
                    e.Result = GetAttributeForRole (VisualRole.Focus);
                }

                break;

            case VisualRole.HotNormal:
                if (subView.HasFocus)
                {
                    e.Handled = true;
                    e.Result = GetAttributeForRole (VisualRole.HotFocus);
                }

                break;

            case VisualRole.Focus:
                if (subView.HasFocus)
                {
                    e.Handled = true;
                    e.Result = GetAttributeForRole (VisualRole.Active);
                }

                break;

            case VisualRole.HotFocus:
                if (subView.HasFocus)
                {
                    e.Handled = true;
                    e.Result = GetAttributeForRole (VisualRole.HotActive);
                }

                break;
        }
    }

    private void Shortcut_TitleChanged (object? sender, EventArgs<string> e) =>

        // If the Title changes, update the CommandView Text.
        // This is a helper to make it easier to set the CommandView text.
        // CommandView is public and replaceable, but this is a convenience.
        _commandView?.Text = Title;

    /// <summary>
    ///     Gets or sets the target <see cref="View"/> that the <see cref="Command"/> will be invoked on
    ///     when the Shortcut is accepted.
    /// </summary>
    public View? TargetView { get; set; }

    /// <summary>
    ///     Gets or sets the <see cref="Command"/> that will be invoked on <see cref="TargetView"/> when the Shortcut
    ///     is accepted. If no <see cref="TargetView"/> is set, the <see cref="Key"/> will be used to invoke commands
    ///     bound at the application level.
    /// </summary>
    public Command Command
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            if (string.IsNullOrEmpty (Title))
            {
                Title = GlobalResources.GetString ($"cmd{field}") ?? string.Empty;
            }

            if (string.IsNullOrEmpty (HelpText))
            {
                HelpText = GlobalResources.GetString ($"cmd{field}_Help") ?? string.Empty;
            }
        }
    }

    #endregion Command

    #region Help

    // The maximum width of the HelpView. Calculated in OnLayoutStarted and used in HelpView.Width (Dim.Auto/Func).
    private int _maxHelpWidth;

    /// <summary>
    ///     The subview that displays the help text for the command. Internal for unit testing.
    /// </summary>
    public View HelpView { get; } = new () { Frame = new Rectangle (0, 0, 2, 1) };

    private void SetHelpViewDefaultLayout ()
    {
        HelpView.Margin.Thickness = GetMarginThickness ();

        // Margin must be transparent to mouse, so clicks pass through to Shortcut
        HelpView.Margin.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

        HelpView.X = Pos.Align (Alignment.End, AlignmentModes);
        _maxHelpWidth = HelpView.Text.GetColumns ();
        HelpView.Width = Dim.Auto (DimAutoStyle.Text, maximumContentDim: Dim.Func (_ => _maxHelpWidth));
        HelpView.Height = Dim.Fill ();

        HelpView.Visible = true;
        HelpView.VerticalTextAlignment = Alignment.Center;
        HelpView.TextAlignment = Alignment.Start;
        HelpView.TextFormatter.WordWrap = true;
        HelpView.MouseHighlightStates = MouseState.None;
    }

    /// <summary>
    ///     Gets or sets the help text displayed in the middle of the Shortcut. Identical in function to <see cref="HelpText"/>
    ///     .
    /// </summary>
    protected override void OnTextChanged ()
    {
        HelpView.Text = Text;
        ShowHide ();

        base.OnTextChanged ();
    }

    /// <summary>
    ///     Gets or sets the help text displayed in the middle of the Shortcut.
    /// </summary>
    public string HelpText
    {
        get => HelpView.Text;
        set
        {
            HelpView.Text = value;
            SetTextDirect (value);
            ShowHide ();
        }
    }

    #endregion Help

    #region Key

    /// <summary>
    ///     Gets or sets the <see cref="Key"/> that will be bound to the <see cref="Command.Activate"/> command.
    /// </summary>
    public Key Key
    {
        get => field ?? Key.Empty;
        set
        {
            Key oldKey = field ?? Key.Empty;
            field = value;

            UpdateKeyBindings (oldKey);

            KeyView.Text = Key == Key.Empty ? string.Empty : $"{Key}";
            ShowHide ();
        }
    }

    /// <summary>
    ///     Gets or sets whether <see cref="Key"/> is bound to <see cref="Command"/> via <see cref="View.HotKeyBindings"/> or
    ///     <see cref="KeyBindings"/>.
    /// </summary>
    public bool BindKeyToApplication
    {
        get;
        set
        {
            if (value == field)
            {
                return;
            }

            if (field)
            {
                App?.Keyboard.KeyBindings.Remove (Key);
            }
            else
            {
                HotKeyBindings.Remove (Key);
            }

            field = value;

            UpdateKeyBindings (Key.Empty);
        }
    }

    /// <summary>
    ///     Gets the subview that displays the key. Is drawn with Normal and HotNormal colors reversed.
    /// </summary>

    public View KeyView { get; } = new () { Frame = new Rectangle (0, 0, 2, 1) };

    /// <summary>
    ///     Gets or sets the minimum size of the key text. Useful for aligning the key text with other <see cref="Shortcut"/>s.
    /// </summary>
    public int MinimumKeyTextSize
    {
        get;
        set
        {
            if (value == field)
            {
                return;
            }

            field = value;
            SetKeyViewDefaultLayout ();
        }
    }

    private void SetKeyViewDefaultLayout ()
    {
        KeyView.Margin.Thickness = GetMarginThickness ();

        // Margin must be transparent to mouse, so clicks pass through to Shortcut
        KeyView.Margin.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

        KeyView.X = Pos.Align (Alignment.End, AlignmentModes);
        KeyView.Width = Dim.Auto (DimAutoStyle.Text, Dim.Func (_ => MinimumKeyTextSize));
        KeyView.Height = Dim.Fill ();

        KeyView.Visible = true;

        // Right align the text
        KeyView.TextAlignment = Alignment.End;
        KeyView.VerticalTextAlignment = Alignment.Center;
        KeyView.KeyBindings.Clear ();
        KeyView.MouseHighlightStates = MouseState.None;
    }

    private void UpdateKeyBindings (Key oldKey)
    {
        if (BindKeyToApplication)
        {
            if (oldKey != Key.Empty)
            {
                App?.Keyboard.KeyBindings.Remove (oldKey);
            }

            App?.Keyboard.KeyBindings.Remove (Key);

            if (Key != Key.Empty)
            {
                // Use the form of Add that provides target since this is an app-level hotkey
                App?.Keyboard.KeyBindings.AddApp (Key, this, Command.HotKey);
            }
        }
        else
        {
            if (oldKey != Key.Empty)
            {
                HotKeyBindings.Remove (oldKey);
            }

            HotKeyBindings.Remove (Key);

            if (Key != Key.Empty)
            {
                HotKeyBindings.Add (Key, Command.HotKey);
            }
        }
    }

    #endregion Key

    #region Focus

    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (!HasFocus)
        {
            return base.OnGettingAttributeForRole (role, ref currentAttribute);
        }

        switch (role)
        {
            case VisualRole.Normal:
                currentAttribute = GetAttributeForRole (VisualRole.Focus);

                return true;

            case VisualRole.HotNormal:
                currentAttribute = GetAttributeForRole (VisualRole.HotFocus);

                return true;

            default: return base.OnGettingAttributeForRole (role, ref currentAttribute);
        }
    }

    #endregion Focus

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Title = "_Shortcut";
        HelpText = "Shortcut help";
        Key = Key.F1;

        return true;
    }

    /// <inheritdoc/>
    public string? GetDemoKeyStrokes () => "wait:500,Enter,wait:1000";

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            TitleChanged -= Shortcut_TitleChanged;

            if (_commandView?.SuperView is null)
            {
                _commandView?.Dispose ();
                _commandView = null;
            }

            if (HelpView.SuperView is null)
            {
                HelpView.Dispose ();
            }

            if (KeyView.SuperView is null)
            {
                KeyView.Dispose ();
            }
        }

        base.Dispose (disposing);
    }
}

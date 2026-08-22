using Terminal.Gui.Tracing;

#pragma warning disable CS0618 // Obsolete - View.Keyboard still uses ConfigurationPropertyAttribute during transition

namespace Terminal.Gui.ViewBase;

public partial class View // Keyboard APIs
{
    /// <summary>
    ///     Helper to configure all things keyboard related for a View. Called from the View constructor.
    /// </summary>
    private void SetupKeyboard ()
    {
        KeyBindings = new KeyBindings ();
        KeyBindings.Add (Key.Space, Command.Activate);

        // QUESTION: Should subclasses be required to enable Accept?
        KeyBindings.Add (Key.Enter, Command.Accept);

        HotKeyBindings = new KeyBindings ();

        // Note, setting HotKey will bind HotKey to Command.HotKey
        HotKeySpecifier = (Rune)'_';
        TitleTextFormatter.HotKeyChanged += TitleTextFormatter_HotKeyChanged;
    }

    /// <summary>
    ///     Helper to dispose all things keyboard related for a View. Called from the View Dispose method.
    /// </summary>
    private void DisposeKeyboard () => TitleTextFormatter.HotKeyChanged -= TitleTextFormatter_HotKeyChanged;

    #region HotKey Support

    /// <summary>Raised when the <see cref="HotKey"/> is changed.</summary>
    public event EventHandler<KeyChangedEventArgs>? HotKeyChanged;

    private Key _hotKey = new ();
    private void TitleTextFormatter_HotKeyChanged (object? sender, KeyChangedEventArgs e) => HotKeyChanged?.Invoke (this, e);

    /// <summary>
    ///     Gets or sets the hot key defined for this view. Pressing the hot key on the keyboard while this view has focus will
    ///     invoke <see cref="Command.HotKey"/>. By default, the HotKey is set to the first character of <see cref="Text"/>
    ///     that is prefixed with <see cref="HotKeySpecifier"/>.
    ///     <para>
    ///         A HotKey is a keypress that causes a visible UI item to perform an action. For example, in a Dialog,
    ///         with a Button with the text of "_Text" <c>Alt+T</c> will cause the button to gain focus and to raise its
    ///         <see cref="Accepting"/> event.
    ///         Or, in a
    ///         <see cref="Menu"/> with "_File _Edit", <c>Alt+F</c> will select (show) the Strings.menuFile menu. If the
    ///         Strings.menuFile menu
    ///         has a
    ///         sub-menu of Strings.cmdNew <c>Alt+N</c> or <c>N</c> will ONLY select the Strings.cmdNew sub-menu if the
    ///         Strings.menuFile menu is already
    ///         opened.
    ///     </para>
    ///     <para>
    ///         View subclasses can use <see cref="View.AddCommand(Command,CommandImplementation)"/> to
    ///         define the
    ///         behavior of the hot key.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>See <see href="../docs/keyboard.md"/> for an overview of Terminal.Gui keyboard APIs.</para>
    ///     <para>
    ///         This is a helper API for configuring a key binding for the hot key. By default, this property is set whenever
    ///         <see cref="Text"/> changes.
    ///     </para>
    ///     <para>
    ///         By default, when the Hot Key is set, key bindings are added for both the base key (e.g.
    ///         <see cref="Key.D3"/>) and the Alt-shifted key (e.g. <see cref="Key.D3"/>.
    ///         <see cref="Key.WithAlt"/>). This behavior can be overriden by overriding
    ///         <see cref="AddKeyBindingsForHotKey"/>.
    ///     </para>
    ///     <para>
    ///         By default, when the HotKey is set to <see cref="Key.A"/> through <see cref="Key.Z"/> key bindings will
    ///         be added for both the un-shifted and shifted versions. This means if the HotKey is <see cref="Key.A"/>, key
    ///         bindings for <c>Key.A</c> and <c>Key.A.WithShift</c> will be added. This behavior can be overriden by
    ///         overriding <see cref="AddKeyBindingsForHotKey"/>.
    ///     </para>
    ///     <para>If the hot key is changed, the <see cref="HotKeyChanged"/> event is fired.</para>
    ///     <para>Set to <see cref="Key.Empty"/> to disable the hot key.</para>
    /// </remarks>
    public Key HotKey
    {
        get => _hotKey;
        set
        {
            if (value is null)
            {
                throw new ArgumentException (@"HotKey must not be null. Use Key.Empty to clear the HotKey.", nameof (value));
            }

            if (!AddKeyBindingsForHotKey (_hotKey, value))
            {
                return;
            }

            // This will cause TextFormatter_HotKeyChanged to be called, firing HotKeyChanged
            // BUGBUG: _hotkey should be set BEFORE setting TextFormatter.HotKey
            _hotKey = value;
            TitleTextFormatter.HotKey = value;
        }
    }

    /// <summary>
    ///     Adds key bindings for the specified HotKey. Useful for views that contain multiple items that each have their
    ///     own HotKey such as <see cref="OptionSelector"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Key bindings are added for both the base key (e.g. <see cref="Key.D3"/>) and the Alt-shifted key
    ///         (e.g. <c>Key.D3.WithAlt</c>).
    ///     </para>
    ///     <para>
    ///         When <paramref name="hotKey"/> is <see cref="Key.A"/> through <see cref="Key.Z"/> key bindings
    ///         will be added for both the un-shifted and shifted versions. This means if the HotKey is <see cref="Key.A"/>,
    ///         key bindings for <c>Key.A</c> and <c>Key.A.WithShift</c> will be added.
    ///     </para>
    /// </remarks>
    /// <param name="prevHotKey">The HotKey <paramref name="hotKey"/> is replacing. Key bindings for this key will be removed.</param>
    /// <param name="hotKey">The new HotKey. If <see cref="Key.Empty"/> <paramref name="prevHotKey"/> bindings will be removed.</param>
    /// <param name="data">Arbitrary data that can be associated with this key binding.</param>
    /// <returns><see langword="true"/> if the HotKey bindings were added.</returns>
    /// <exception cref="ArgumentException"></exception>
    public bool AddKeyBindingsForHotKey (Key prevHotKey, Key hotKey, object? data = null)
    {
        if (_hotKey == hotKey)
        {
            return false;
        }

        Key newKey = hotKey;

        Key baseKey = newKey.NoAlt.NoShift.NoCtrl;

        if (newKey != Key.Empty && Rune.IsControl (baseKey.AsRune))
        {
            throw new ArgumentException (@$"HotKey must be a printable key ({hotKey}).");
        }

        if (newKey != baseKey)
        {
            if (newKey.IsCtrl)
            {
                throw new ArgumentException (@$"HotKey does not support CtrlMask ({hotKey}).");
            }

            // Strip off the shift mask if it's A...Z
            if (baseKey.IsKeyCodeAtoZ)
            {
                newKey = newKey.NoShift;
            }

            // Strip off the Alt mask
            newKey = newKey.NoAlt;
        }

        // Remove base version
        if (HotKeyBindings.TryGet (prevHotKey, out _))
        {
            HotKeyBindings.Remove (prevHotKey);
        }

        // Remove the Alt version
        if (HotKeyBindings.TryGet (prevHotKey.WithAlt, out _))
        {
            HotKeyBindings.Remove (prevHotKey.WithAlt);
        }

        if (_hotKey.IsKeyCodeAtoZ)
        {
            // Remove the shift version
            if (HotKeyBindings.TryGet (prevHotKey.WithShift, out _))
            {
                HotKeyBindings.Remove (prevHotKey.WithShift);
            }

            // Remove alt | shift version
            if (HotKeyBindings.TryGet (prevHotKey.WithShift.WithAlt, out _))
            {
                HotKeyBindings.Remove (prevHotKey.WithShift.WithAlt);
            }
        }

        // Add the new
        if (newKey == Key.Empty)
        {
            return true;
        }
        KeyBinding keyBinding = new ([Command.HotKey], newKey, this, null, data);

        // Add the base and Alt key
        HotKeyBindings.Remove (newKey);
        HotKeyBindings.Add (newKey, keyBinding);
        HotKeyBindings.Remove (newKey.WithAlt);
        HotKeyBindings.Add (newKey.WithAlt, keyBinding);

        // If the Key is A-Z, add ShiftMask and AltMask | ShiftMask
        if (!newKey.IsKeyCodeAtoZ)
        {
            return true;
        }
        HotKeyBindings.Remove (newKey.WithShift);
        HotKeyBindings.Add (newKey.WithShift, keyBinding);
        HotKeyBindings.Remove (newKey.WithShift.WithAlt);
        HotKeyBindings.Add (newKey.WithShift.WithAlt, keyBinding);

        return true;
    }

    /// <summary>
    ///     Gets or sets the specifier character for the hot key (e.g. '_'). Set to '\xffff' to disable automatic hot key
    ///     setting support for this View instance. The default is '\xffff'.
    /// </summary>
    public virtual Rune HotKeySpecifier
    {
        get => TitleTextFormatter.HotKeySpecifier;
        set
        {
            TitleTextFormatter.HotKeySpecifier = TextFormatter.HotKeySpecifier = value;
            SetHotKeyFromTitle ();
        }
    }

    private void SetHotKeyFromTitle ()
    {
        if (HotKeySpecifier == new Rune ('\xFFFF'))
        {
            HotKey = Key.Empty;

            return; // throw new InvalidOperationException ("Can't set HotKey unless a TextFormatter has been created");
        }

        if (TextFormatter.FindHotKey (_title, HotKeySpecifier, out _, out Key hk))
        {
            if (_hotKey != hk)
            {
                HotKey = hk;
            }
        }
        else
        {
            HotKey = Key.Empty;
        }
    }

    #endregion HotKey Support

    #region AutoHotKey Assignment

    /// <summary>
    ///     If <see langword="true"/>, unique hotkeys will automatically be assigned to focusable subviews that have a
    ///     <see cref="Title"/> but no <see cref="HotKey"/> defined.
    ///     <para>
    ///         <see cref="UsedHotKeys"/> will be used to ensure unique keys are assigned. Set <see cref="UsedHotKeys"/>
    ///         before adding subviews with any hotkeys that may conflict with other Views.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When enabled, hotkeys are assigned to subviews when they are added via <see cref="Add(View?)"/>.
    ///         The assignment algorithm:
    ///         <list type="number">
    ///             <item>
    ///                 Checks if the subview already has a programmatically set <see cref="HotKey"/>; if so and the key is not
    ///                 already used, preserves that hotkey
    ///             </item>
    ///             <item>
    ///                 If no usable programmatic <see cref="HotKey"/> is found, checks if the subview's <see cref="Title"/>
    ///                 contains a hotkey specifier (e.g., Strings.menuFile) and preserves it if the key is not already used
    ///             </item>
    ///             <item>
    ///                 If neither a usable programmatic hotkey nor a usable title specifier is found, assigns a new hotkey
    ///                 from the first available character in the title
    ///             </item>
    ///             <item>
    ///                 Skips characters that are already in <see cref="UsedHotKeys"/>, as well as spaces and control
    ///                 characters, when determining whether a hotkey is usable or when assigning a new one
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Call <see cref="AssignHotKeysToSubViews"/> to manually trigger hotkey assignment for all subviews.
    ///     </para>
    /// </remarks>
    public bool AssignHotKeys
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            if (field)
            {
                AssignHotKeysToSubViews ();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the set of hotkeys that are already used or should not be used when
    ///     <see cref="AssignHotKeys"/> is enabled.
    ///     <para>
    ///         This property is used to ensure that automatically assigned hotkeys do not conflict with
    ///         hotkeys used elsewhere in the application. Set <see cref="UsedHotKeys"/> before adding
    ///         subviews if there are hotkeys that may conflict with other views.
    ///     </para>
    /// </summary>
    public HashSet<Key> UsedHotKeys { get; set; } = [];

    /// <summary>
    ///     Assigns unique hotkeys to all subviews that have a <see cref="Title"/> but no <see cref="HotKey"/> defined.
    ///     Called automatically when <see cref="AssignHotKeys"/> is enabled and subviews are added.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method iterates through all subviews and assigns hotkeys based on their <see cref="Title"/>.
    ///         It respects pre-existing hotkeys defined via the <see cref="HotKeySpecifier"/> in the title.
    ///     </para>
    ///     <para>
    ///         Override this method to customize the hotkey assignment behavior.
    ///     </para>
    /// </remarks>
    public void AssignHotKeysToSubViews ()
    {
        if (!AssignHotKeys)
        {
            return;
        }

        foreach (View subView in SubViews)
        {
            AssignHotKeyToView (subView);
        }
    }

    /// <summary>
    ///     Assigns a unique hotkey to a single view based on its <see cref="Title"/>.
    /// </summary>
    /// <param name="view">The view to assign a hotkey to.</param>
    /// <returns><see langword="true"/> if a hotkey was assigned; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    ///     <para>
    ///         This method checks if the view's title already contains a hotkey specifier and preserves it if available.
    ///         Otherwise, it assigns a new hotkey from the first available character in the title.
    ///     </para>
    /// </remarks>
    protected bool AssignHotKeyToView (View view)
    {
        if (string.IsNullOrEmpty (view.Title) || view.HotKeySpecifier == (Rune)'\xffff')
        {
            return false;
        }

        string label = view.Title;

        // Check if there's already a hotkey defined in the title
        if (TextFormatter.FindHotKey (label, HotKeySpecifier, out int hotKeyPos, out Key existingHotKey))
        {
            // Label already has a hotkey - preserve it if available
            if (!UsedHotKeys.Contains (existingHotKey))
            {
                view.HotKey = existingHotKey;
                UsedHotKeys.Add (existingHotKey);

                return true;
            }

            // Existing hotkey is already used, remove it and assign new one
            label = TextFormatter.RemoveHotKeySpecifier (label, hotKeyPos, HotKeySpecifier);
        }
        else if (view.HotKey != Key.Empty && UsedHotKeys.Add (view.HotKey))
        {
            // View already has a hotkey set programmatically - preserve it

            return true;
        }

        // Assign a new hotkey from available characters
        Rune [] runes = label.EnumerateRunes ().ToArray ();

        for (var i = 0; i < runes.Length; i++)
        {
            Rune lower = Rune.ToLowerInvariant (runes [i]);
            var newKey = new Key (lower.Value);

            if (UsedHotKeys.Contains (newKey))
            {
                continue;
            }

            if (!newKey.IsValid || newKey == Key.Empty || newKey == Key.Space || Rune.IsPunctuation (newKey.AsRune) || Rune.IsControl (newKey.AsRune))
            {
                continue;
            }

            view.Title = label.Insert (i, HotKeySpecifier.ToString ());
            view.HotKey = newKey;
            UsedHotKeys.Add (newKey);

            return true;
        }

        return false;
    }

    #endregion AutoHotKey Assignment

    #region Low-level Key handling

    #region Key Down Event

    /// <summary>
    ///     If the view is enabled, raises the related key down events on the view, and returns <see langword="true"/> if the
    ///     event was
    ///     handled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the view has a sub view that is focused, <see cref="NewKeyDownEvent"/> will be called on the focused view
    ///         first.
    ///     </para>
    ///     <para>
    ///         If a more focused subview does not handle the key press, this method raises <see cref="OnKeyDown"/>/
    ///         <see cref="KeyDown"/> to allow the
    ///         view to pre-process the key press. If <see cref="OnKeyDown"/>/<see cref="KeyDown"/> is not handled any commands
    ///         bound to the key will be invoked.
    ///         Then, only if no key bindings are
    ///         handled, <see cref="OnKeyDownNotHandled"/>/<see cref="KeyDownNotHandled"/> will be raised allowing the view to
    ///         process the key press.
    ///     </para>
    ///     <para>
    ///         Calling this method for a key bound to the view via an Application-scoped keybinding will have no effect.
    ///         Instead,
    ///         use <see cref="IKeyboard.RaiseKeyDownEvent"/>.
    ///     </para>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the event was handled.</returns>
    public bool NewKeyDownEvent (Key key)
    {
        Trace.Keyboard (this, key, "Entry");

        if (!Enabled)
        {
            return false;
        }

        // TODO: We really need an event before recursing into Focused. Without, there's no way for
        // TODO: SuperViews to prevent SubViews from seeing certain keys. A use-case for this:
        // TODO:    - MenuBar needs to prevent MenuItems from seeing QuitKey if the MenuItem is not visible

        // If there's a Focused subview, give it a chance (this recurses down the hierarchy)
        if (Focused?.NewKeyDownEvent (key) == true)
        {
            return true;
        }

        // Before (fire the cancellable event)
        if (RaiseKeyDown (key) || key.Handled)
        {
            return true;
        }

        // During (this is what can be cancelled)

        // TODO: NewKeyDownEvent returns bool. It should be bool? so state of InvokeCommands can be reflected up stack
        if (InvokeCommandsBoundToKey (key) is true || key.Handled)
        {
            return true;
        }

        bool? handled = InvokeCommandsBoundToHotKey (key);

        if (handled is true)
        {
            return true;
        }

        // After
        if (RaiseKeyDownNotHandled (key) || key.Handled)
        {
            return true;
        }

        return key.Handled;

        bool RaiseKeyDown (Key k)
        {
            // Before (fire the cancellable event)
            if (OnKeyDown (k) || k.Handled)
            {
                return true;
            }

            // fire event
            KeyDown?.Invoke (this, k);

            return k.Handled;
        }

        bool RaiseKeyDownNotHandled (Key k)
        {
            if (OnKeyDownNotHandled (k) || k.Handled)
            {
                return true;
            }

            KeyDownNotHandled?.Invoke (this, k);

            return false;
        }
    }

    /// <summary>
    ///     Called when the user presses a key, allowing subscribers to pre-process the key down event. Called
    ///     before key bindings are invoked and <see cref="KeyDownNotHandled"/> is raised. Set
    ///     <see cref="Key.Handled"/>
    ///     to true to
    ///     stop the key from being processed further.
    /// </summary>
    /// <param name="key">The key that produced the event.</param>
    /// <returns>
    ///     <see langword="false"/> if the key down event was not handled. <see langword="true"/> if the event was handled
    ///     and processing should stop.
    /// </returns>
    /// <remarks>
    ///     <para>Fires the <see cref="KeyDown"/> event.</para>
    /// </remarks>
    protected virtual bool OnKeyDown (Key key) => false;

    /// <summary>
    ///     Raised when the user presses a key, allowing subscribers to pre-process the key down event. Called
    ///     before key bindings are invoked and <see cref="KeyDownNotHandled"/> is raised. Set
    ///     <see cref="Key.Handled"/>
    ///     to true to
    ///     stop the key from being processed further.
    /// </summary>
    /// <remarks>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    public event EventHandler<Key>? KeyDown;

    /// <summary>
    ///     Called when the user has pressed key it wasn't handled by <see cref="KeyDown"/> and was not bound to a key binding.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="key">Contains the details about the key that produced the event.</param>
    /// <returns>
    ///     <see langword="false"/> if the key press was not handled. <see langword="true"/> if the keypress was handled
    ///     and no other view should see it.
    /// </returns>
    protected virtual bool OnKeyDownNotHandled (Key key) => key.Handled;

    /// <summary>
    ///     Raised when the user has pressed key it wasn't handled by <see cref="KeyDown"/> and was not bound to a key binding.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         SubViews can use the <see cref="KeyDownNotHandled"/> of their super view override the default behavior of when
    ///         key bindings are invoked.
    ///     </para>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    public event EventHandler<Key>? KeyDownNotHandled;

    #endregion KeyDown Event

    #region KeyUp Event

    /// <summary>
    ///     Called when a key is released. This is the view-level entry point for key-up events,
    ///     following the same pattern as <see cref="NewKeyDownEvent"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Key-up events are only raised when the driver provides key release information.
    ///         Not all drivers support key-up events.
    ///     </para>
    /// </remarks>
    /// <param name="key">The key that was released.</param>
    /// <returns><see langword="true"/> if the event was handled.</returns>
    public bool NewKeyUpEvent (Key key)
    {
        if (!Enabled)
        {
            return false;
        }

        if (Focused?.NewKeyUpEvent (key) == true)
        {
            return true;
        }

        if (OnKeyUp (key) || key.Handled)
        {
            return true;
        }

        KeyUp?.Invoke (this, key);

        return key.Handled;
    }

    /// <summary>
    ///     Called when the user releases a key, allowing subclasses to pre-process the key up event.
    ///     Set <see cref="Key.Handled"/> to <see langword="true"/> to stop the key from being processed further.
    /// </summary>
    /// <param name="key">The key that produced the event.</param>
    /// <returns>
    ///     <see langword="false"/> if the key up event was not handled. <see langword="true"/> if the event was handled
    ///     and processing should stop.
    /// </returns>
    protected virtual bool OnKeyUp (Key key) => false;

    /// <summary>
    ///     Raised when the user releases a key.
    ///     <para>
    ///         Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and to prevent
    ///         additional processing.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Key-up events are only raised when the driver provides key release information.
    ///         Not all drivers support key-up events.
    ///     </para>
    /// </remarks>
    public event EventHandler<Key>? KeyUp;

    #endregion KeyUp Event

    #endregion Low-level Key handling

    #region Key Bindings

    /// <summary>Gets the bindings for this view that will be invoked only if this view has focus.</summary>
    public KeyBindings KeyBindings { get; internal set; } = null!;

    /// <summary>Gets the bindings for this view that will be invoked regardless of whether this view has focus or not.</summary>
    public KeyBindings HotKeyBindings { get; internal set; } = null!;

    /// <summary>
    ///     Gets or sets the default key bindings shared across all views. Only commands that a view supports
    ///     (via <see cref="GetSupportedCommands"/>) are actually bound when <see cref="ApplyKeyBindings"/> is called.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings
    {
        get;
        set
        {
            field = value;
            Trace.Configuration ("DefaultKeyBindings", "View Set", $"{string.Join (", ", value?.Select (kvp => $"{kvp.Key}=[{kvp.Value}]") ?? [])}");
        }
    } = new ()
    {
        // Navigation
        [Command.Left] = Bind.All (Key.CursorLeft),
        [Command.Right] = Bind.All (Key.CursorRight),
        [Command.Up] = Bind.All (Key.CursorUp),
        [Command.Down] = Bind.All (Key.CursorDown),
        [Command.PageUp] = Bind.All (Key.PageUp),
        [Command.PageDown] = Bind.All (Key.PageDown),
        [Command.LeftStart] = Bind.All (Key.Home),
        [Command.RightEnd] = Bind.All (Key.End),
        [Command.Start] = Bind.All (Key.Home.WithCtrl),
        [Command.End] = Bind.All (Key.End.WithCtrl),

        // Selection-extend
        [Command.LeftExtend] = Bind.All (Key.CursorLeft.WithShift),
        [Command.RightExtend] = Bind.All (Key.CursorRight.WithShift),
        [Command.UpExtend] = Bind.All (Key.CursorUp.WithShift),
        [Command.DownExtend] = Bind.All (Key.CursorDown.WithShift),
        [Command.PageUpExtend] = Bind.All (Key.PageUp.WithShift),
        [Command.PageDownExtend] = Bind.All (Key.PageDown.WithShift),
        [Command.LeftStartExtend] = Bind.All (Key.Home.WithShift),
        [Command.RightEndExtend] = Bind.All (Key.End.WithShift),
        [Command.StartExtend] = Bind.All (Key.Home.WithCtrl.WithShift),
        [Command.EndExtend] = Bind.All (Key.End.WithCtrl.WithShift),

        // Clipboard
        [Command.Copy] = Bind.All (Key.C.WithCtrl),
        [Command.Cut] = Bind.All (Key.X.WithCtrl),
        [Command.Paste] = Bind.All (Key.V.WithCtrl),

        // Editing
        [Command.Undo] = Bind.AllPlus (Key.Z.WithCtrl, [new Key ('/').WithCtrl]),
        [Command.Redo] = Bind.AllPlus (Key.Y.WithCtrl, [Key.Z.WithCtrl.WithShift]),
        [Command.SelectAll] = Bind.All (Key.A.WithCtrl),
        [Command.DeleteCharLeft] = Bind.All (Key.Backspace),
        [Command.DeleteCharRight] = Bind.All (Key.Delete, Key.D.WithCtrl)
    };

    /// <summary>
    ///     Gets or sets per-view key binding overrides from configuration. The outer key is the view type name
    ///     (e.g., "TextField"), the inner dictionary maps commands to platform key bindings.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public static Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? ViewKeyBindings { get; set; }

    /// <summary>
    ///     Applies layered key bindings from the provided dictionaries. Only commands that the view supports
    ///     (via <see cref="GetSupportedCommands"/>) are bound. Keys already bound are not overwritten.
    /// </summary>
    protected void ApplyKeyBindings (params Dictionary<Command, PlatformKeyBinding>? [] layers)
    {
        HashSet<Command> supported = new (GetSupportedCommands ());

        foreach (Dictionary<Command, PlatformKeyBinding>? layer in layers)
        {
            if (layer is null)
            {
                continue;
            }

            ApplyLayer (layer, supported);
        }

        // Apply user overrides from View.ViewKeyBindings (CM/MEC bridge)
        // Strip generic arity suffix (e.g., "TreeView`1" → "TreeView") so config keys match
        string typeName = GetType ().Name;
        int backtick = typeName.IndexOf ('`');

        if (backtick >= 0)
        {
            typeName = typeName [..backtick];
        }

        if (ViewKeyBindings?.TryGetValue (typeName, out Dictionary<Command, PlatformKeyBinding>? overrides) == true)
        {
            ApplyLayer (overrides, supported);
        }
    }

    private void ApplyLayer (Dictionary<Command, PlatformKeyBinding> layer, HashSet<Command> supported)
    {
        foreach ((Command command, PlatformKeyBinding platformKeys) in layer)
        {
            if (!supported.Contains (command))
            {
                continue;
            }

            foreach (Key key in platformKeys.GetCurrentPlatformKeys ())
            {
                if (key == Key.Empty)
                {
                    continue;
                }

                if (KeyBindings.TryGet (key, out _))
                {
                    continue;
                }

                KeyBindings.Add (key, command);
            }
        }
    }

    /// <summary>
    ///     INTERNAL: Invokes the Commands bound to <paramref name="key"/>.
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </summary>
    /// <param name="key">The key event passed.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was invoked or there was no matching key binding; input processing should
    ///     continue.
    ///     <see langword="false"/> if a command was invoked and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if at least one command was invoked and handled (or
    ///     cancelled); input processing should stop.
    /// </returns>
    internal bool? InvokeCommandsBoundToKey (Key key)
    {
        if (!KeyBindings.TryGet (key, out KeyBinding binding))
        {
            return null;
        }

        if (binding is { } && (binding.Key is null || !binding.Key.IsValid))
        {
            binding.Key = key;
        }

        Trace.Keyboard (this, key, "InvokeCommands");

        return InvokeCommands (binding.Commands, binding with { Source = new WeakReference<View> (this) });
    }

    /// <summary>
    ///     INTERNAL: Invokes any commands bound to <paramref name="hotKey"/> on this view and subviews.
    /// </summary>
    /// <param name="hotKey"></param>
    /// <returns>
    ///     <see langword="null"/> if no command was invoked; input processing should continue.
    ///     <see langword="false"/> if at least one command was invoked and was not handled (or cancelled); input processing
    ///     should continue.
    ///     <see langword="true"/> if at least one command was invoked and handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    internal bool? InvokeCommandsBoundToHotKey (Key hotKey)
    {
        if (!Enabled)
        {
            return false;
        }

        if (!Visible)
        {
            return false;
        }

        bool? handled = null;

        // Process this View
        if (HotKeyBindings.TryGet (hotKey, out KeyBinding binding))
        {
            if (binding.Key is null || binding.Key == Key.Empty)
            {
                binding.Key = hotKey;
            }

            if (InvokeCommands (binding.Commands, binding with { Source = new WeakReference<View> (this) }) is true)
            {
                return true;
            }
        }

        // Now, process any HotKey bindings in the subviews
        foreach (View subview in GetSubViews (includePadding: true, includeBorder: true))
        {
            if (subview == Focused)
            {
                continue;
            }

            bool? recurse = subview.InvokeCommandsBoundToHotKey (hotKey);

            if (recurse is true || (handled is { } && (bool)handled))
            {
                return true;
            }
        }

        return false;
    }

    #endregion Key Bindings
}

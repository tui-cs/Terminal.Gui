using System.ComponentModel;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.ViewBase;

public partial class View // Mouse APIs
{
    /// <summary>
    ///     Gets the mouse bindings for this view. By default, all mouse buttons are bound to the
    ///     <see cref="Command.Activate"/> command.
    /// </summary>
    public MouseBindings MouseBindings { get; internal set; } = null!;

    /// <summary>
    ///     Gets or sets the default mouse bindings shared across all views. Only commands that a view supports
    ///     (via <see cref="GetSupportedCommands"/>) are actually bound when <see cref="ApplyMouseBindings"/> is called.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public static Dictionary<Command, PlatformMouseBinding>? DefaultMouseBindings
    {
        get;
        set
        {
            field = value;
            Trace.Configuration ("DefaultMouseBindings", "View Set", $"{string.Join (", ", value?.Select (kvp => $"{kvp.Key}=[{kvp.Value}]") ?? [])}");
        }
    } = new ()
    {
        [Command.Activate] = BindMouse.All (MouseFlags.LeftButtonReleased),
        [Command.Context] = BindMouse.All (MouseFlags.LeftButtonReleased | MouseFlags.Ctrl),
        [Command.StartSelection] = BindMouse.Platform (windows: [MouseFlags.LeftButtonPressed | MouseFlags.Alt],
                                                       linux: [MouseFlags.LeftButtonPressed | MouseFlags.Shift],
                                                       macos: [MouseFlags.LeftButtonPressed | MouseFlags.Shift]),
        [Command.StartRectangleSelection] = BindMouse.Platform (windows: [MouseFlags.LeftButtonPressed | MouseFlags.Alt | MouseFlags.Ctrl],
                                                                linux: [MouseFlags.LeftButtonPressed | MouseFlags.Shift | MouseFlags.Ctrl],
                                                                macos: [MouseFlags.LeftButtonPressed | MouseFlags.Shift | MouseFlags.Ctrl])
    };

    /// <summary>
    ///     Gets or sets per-view mouse binding overrides from configuration. The outer key is the view type name
    ///     (e.g., "TextView"), the inner dictionary maps commands to platform mouse bindings.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public static Dictionary<string, Dictionary<Command, PlatformMouseBinding>>? ViewMouseBindings { get; set; }

    private void SetupMouse ()
    {
        MouseBindings = new MouseBindings ();
        ApplyMouseBindings (DefaultMouseBindings);

        // Released bindings are added/removed dynamically when MouseHoldRepeat changes
        // See OnMouseHoldRepeatChanged
    }

    /// <summary>
    ///     Applies layered mouse bindings from the provided dictionaries. Only commands that the view supports
    ///     (via <see cref="GetSupportedCommands"/>) are bound. Mouse flags already bound are not overwritten.
    /// </summary>
    protected void ApplyMouseBindings (params Dictionary<Command, PlatformMouseBinding>? [] layers)
    {
        if (layers is null)
        {
            return;
        }

        HashSet<Command> supported = new (GetSupportedCommands ());

        foreach (Dictionary<Command, PlatformMouseBinding>? layer in layers)
        {
            if (layer is null)
            {
                continue;
            }

            ApplyMouseLayer (layer, supported);
        }

        string typeName = GetType ().Name;
        int backtick = typeName.IndexOf ('`');

        if (backtick >= 0)
        {
            typeName = typeName [..backtick];
        }

        if (ViewMouseBindings?.TryGetValue (typeName, out Dictionary<Command, PlatformMouseBinding>? overrides) == true)
        {
            ApplyMouseLayer (overrides, supported);
        }
    }

    private void ApplyMouseLayer (Dictionary<Command, PlatformMouseBinding> layer, HashSet<Command> supported)
    {
        foreach ((Command command, PlatformMouseBinding platformMouseFlags) in layer)
        {
            if (!supported.Contains (command))
            {
                continue;
            }

            foreach (MouseFlags mouseFlags in platformMouseFlags.GetCurrentPlatformMouseFlags ())
            {
                if (mouseFlags == MouseFlags.None)
                {
                    continue;
                }

                if (MouseBindings.TryGet (mouseFlags, out _))
                {
                    continue;
                }

                MouseBindings.Add (mouseFlags, command);
            }
        }
    }

    #region MouseEnterLeave

    /// <summary>
    ///     INTERNAL Called by <see cref="IMouse.RaiseMouseEvent"/> when the mouse moves over the View's
    ///     <see cref="Frame"/>.
    ///     <see cref="MouseLeave"/> will
    ///     be raised when the mouse is no longer over the <see cref="Frame"/>. If another View occludes this View, the
    ///     that View will also receive MouseEnter/Leave events.
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <returns>
    ///     <see langword="true"/> if the event was canceled, <see langword="false"/> if not, <see langword="null"/> if the
    ///     view is not visible or not enabled. Cancelling the event
    ///     prevents Views higher in the visible hierarchy from receiving Enter/Leave events.
    /// </returns>
    internal bool? NewMouseEnterEvent (CancelEventArgs eventArgs)
    {
        // Pre-conditions
        if (!CanBeVisible (this) || !Enabled)
        {
            return null;
        }

        // Cancellable event
        if (OnMouseEnter (eventArgs))
        {
            return true;
        }

        MouseEnter?.Invoke (this, eventArgs);

        if (eventArgs.Cancel)
        {
            return true;
        }

        MouseState |= MouseState.In;

        if ((MouseHighlightStates & MouseState.In) != MouseState.None)
        {
            SetNeedsDraw ();
        }

        return false;
    }

    /// <summary>
    ///     Called when the mouse moves over the View's <see cref="Frame"/> and no other non-SubView occludes it.
    ///     <see cref="MouseLeave"/> will
    ///     be raised when the mouse is no longer over the <see cref="Frame"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A view must be visible and enabled to receive Enter events (Leave events are always received).
    ///     </para>
    ///     <para>
    ///         If the event is cancelled, the mouse event will not be propagated to other views and <see cref="MouseEnter"/>
    ///         will not be raised.
    ///     </para>
    ///     <para>
    ///         Adornments receive MouseEnter/Leave events when the mouse is over the Adornment's <see cref="Thickness"/>.
    ///     </para>
    ///     <para>
    ///         See <see cref="MouseState"/> for more information.
    ///     </para>
    /// </remarks>
    /// <param name="eventArgs"></param>
    /// <returns>
    ///     <see langword="true"/> if the event was canceled, <see langword="false"/> if not. Cancelling the event
    ///     prevents Views higher in the visible hierarchy from receiving Enter/Leave events.
    /// </returns>
    protected virtual bool OnMouseEnter (CancelEventArgs eventArgs) => false;

    /// <summary>
    ///     Raised when the mouse moves over the View's <see cref="Frame"/>. <see cref="MouseLeave"/> will
    ///     be raised when the mouse is no longer over the <see cref="Frame"/>. If another View occludes this View, the
    ///     that View will also receive MouseEnter/Leave events.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A view must be visible and enabled to receive Enter events (Leave events are always received).
    ///     </para>
    ///     <para>
    ///         If the event is cancelled, the mouse event will not be propagated to other views.
    ///     </para>
    ///     <para>
    ///         Adornments receive MouseEnter/Leave events when the mouse is over the Adornment's <see cref="Thickness"/>.
    ///     </para>
    ///     <para>
    ///         Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> if the event was canceled,
    ///         <see langword="false"/> if not. Cancelling the event
    ///         prevents Views higher in the visible hierarchy from receiving Enter/Leave events.
    ///     </para>
    ///     <para>
    ///         See <see cref="MouseState"/> for more information.
    ///     </para>
    /// </remarks>
    public event EventHandler<CancelEventArgs>? MouseEnter;

    /// <summary>
    ///     INTERNAL Called by <see cref="IMouse.RaiseMouseEvent"/> when the mouse leaves <see cref="Frame"/>, or is
    ///     occluded
    ///     by another non-SubView.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method calls <see cref="OnMouseLeave"/> and raises the <see cref="MouseLeave"/> event.
    ///     </para>
    ///     <para>
    ///         Adornments receive MouseEnter/Leave events when the mouse is over the Adornment's <see cref="Thickness"/>.
    ///     </para>
    ///     <para>
    ///         See <see cref="MouseState"/> for more information.
    ///     </para>
    /// </remarks>
    internal void NewMouseLeaveEvent ()
    {
        // Non-cancellable event
        OnMouseLeave ();

        MouseLeave?.Invoke (this, EventArgs.Empty);

        MouseState &= ~MouseState.In;

        if ((MouseHighlightStates & MouseState.In) != MouseState.None)
        {
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Called when the mouse moves outside View's <see cref="Frame"/>, or is occluded by another non-SubView.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Adornments receive MouseEnter/Leave events when the mouse is over the Adornment's <see cref="Thickness"/>.
    ///     </para>
    ///     <para>
    ///         See <see cref="MouseState"/> for more information.
    ///     </para>
    /// </remarks>
    protected virtual void OnMouseLeave () { }

    /// <summary>
    ///     Raised when the mouse moves outside View's <see cref="Frame"/>, or is occluded by another non-SubView.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Adornments receive MouseEnter/Leave events when the mouse is over the Adornment's <see cref="Thickness"/>.
    ///     </para>
    ///     <para>
    ///         See <see cref="MouseState"/> for more information.
    ///     </para>
    /// </remarks>
    public event EventHandler? MouseLeave;

    #endregion MouseEnterLeave

    #region Low Level Mouse Events

    /// <summary>
    ///     Gets or sets which mouse event triggers command invocation during continuous button press.
    ///     When set to a non-null value and the user presses and holds the mouse button,
    ///     <see cref="NewMouseEvent"/> will be repeatedly called, enabling repeating button behavior.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Valid values are:
    ///         <list type="bullet">
    ///             <item><see langword="null"/> - Disabled (default)</item>
    ///             <item><see cref="MouseFlags.LeftButtonPressed"/> - Commands invoked on Press during hold</item>
    ///             <item><see cref="MouseFlags.LeftButtonReleased"/> - Commands invoked on Released during hold</item>
    ///             <item><see cref="MouseFlags.LeftButtonClicked"/> - Commands invoked on Click after hold</item>
    ///             <item>Other xxxReleased or xxxClicked flags for other mouse buttons</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         When enabled, pressing the mouse button starts a timer that periodically invokes the
    ///         <see cref="Command.Activate"/> command bound to the specified mouse event, enabling
    ///         auto-repeat behavior (e.g., scrollbar arrows, spin buttons).
    ///     </para>
    /// </remarks>
    public MouseFlags? MouseHoldRepeat
    {
        get;
        set
        {
            // Validate that only null, Pressed, or Clicked flags are allowed
            if (value.HasValue)
            {
                bool isPressed = (value.Value & (MouseFlags.LeftButtonPressed | MouseFlags.MiddleButtonPressed | MouseFlags.RightButtonPressed)) != 0;
                bool isReleased = (value.Value & (MouseFlags.LeftButtonReleased | MouseFlags.MiddleButtonReleased | MouseFlags.RightButtonReleased)) != 0;

                bool isClicked = (value.Value
                                  & (MouseFlags.LeftButtonClicked
                                     | MouseFlags.MiddleButtonClicked
                                     | MouseFlags.RightButtonClicked
                                     | MouseFlags.LeftButtonDoubleClicked
                                     | MouseFlags.MiddleButtonDoubleClicked
                                     | MouseFlags.RightButtonDoubleClicked
                                     | MouseFlags.LeftButtonTripleClicked
                                     | MouseFlags.MiddleButtonTripleClicked
                                     | MouseFlags.RightButtonTripleClicked))
                                 != 0;

                if (!isReleased && !isPressed && !isClicked)
                {
                    throw new ArgumentException (@"MouseHoldRepeat only accepts null, Pressed, Released or Clicked mouse flags.", nameof (value));
                }
            }

            CWPPropertyHelper.ChangeProperty (this,
                                              ref field,
                                              value,
                                              OnMouseHoldRepeatChanging,
                                              MouseHoldRepeatChanging,
                                              DoWork,
                                              OnMouseHoldRepeatChanged,
                                              MouseHoldRepeatChanged,
                                              out _);

            return;

            void DoWork (MouseFlags? newValue)
            {
                if (newValue.HasValue)
                {
                    // Enabled: Bind the specified mouse event to Activate command
                    MouseBindings.ReplaceCommands (newValue.Value, Command.Activate);
                }
                else
                {
                    // Disabled: Remove any hold-repeat bindings and restore default Released binding
                    MouseBindings.Remove (MouseFlags.LeftButtonReleased);
                    MouseBindings.Remove (MouseFlags.MiddleButtonReleased);
                    MouseBindings.Remove (MouseFlags.RightButtonReleased);
                    MouseBindings.Remove (MouseFlags.LeftButtonClicked);
                    MouseBindings.Remove (MouseFlags.MiddleButtonClicked);
                    MouseBindings.Remove (MouseFlags.RightButtonClicked);
                    MouseBindings.Remove (MouseFlags.LeftButtonDoubleClicked);
                    MouseBindings.Remove (MouseFlags.MiddleButtonDoubleClicked);
                    MouseBindings.Remove (MouseFlags.RightButtonDoubleClicked);
                    MouseBindings.Remove (MouseFlags.LeftButtonTripleClicked);
                    MouseBindings.Remove (MouseFlags.MiddleButtonTripleClicked);
                    MouseBindings.Remove (MouseFlags.RightButtonTripleClicked);
                    MouseBindings.ReplaceCommands (MouseFlags.LeftButtonReleased, Command.Activate);
                }

                field = newValue;
            }
        }
    }

    /// <summary>
    ///     Called before <see cref="MouseHoldRepeat"/> changes. Return <see langword="true"/> to cancel the change.
    /// </summary>
    protected virtual bool OnMouseHoldRepeatChanging (ValueChangingEventArgs<MouseFlags?> args) => false;

    /// <summary>
    ///     Raised before <see cref="MouseHoldRepeat"/> changes. Set <see cref="CancelEventArgs.Cancel"/> to
    ///     <see langword="true"/> to cancel the change.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<MouseFlags?>>? MouseHoldRepeatChanging;

    /// <summary>
    ///     Called after <see cref="MouseHoldRepeat"/> has changed.
    /// </summary>
    protected virtual void OnMouseHoldRepeatChanged (ValueChangedEventArgs<MouseFlags?> args) { }

    /// <summary>
    ///     Raised after <see cref="MouseHoldRepeat"/> has changed.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<MouseFlags?>>? MouseHoldRepeatChanged;

    /// <summary>
    ///     Gets or sets whether the <see cref="View"/> wants mouse position reports.
    /// </summary>
    /// <value><see langword="true"/> if mouse position reports are wanted; otherwise, <see langword="false"/>.</value>
    public bool MousePositionTracking { get; set; }

    /// <summary>
    ///     Gets whether auto-grab should be enabled for this view based on <see cref="MouseHighlightStates"/>
    ///     or <see cref="MouseHoldRepeat"/> being set.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When <see langword="true"/>, the view will automatically grab the mouse on button press and
    ///         ungrab on button release (clicked event), capturing all mouse events during the press-release cycle.
    ///     </para>
    ///     <para>
    ///         Auto-grab is enabled when either <see cref="MouseHighlightStates"/> is not <see cref="MouseState.None"/>
    ///         (the view wants visual feedback) or <see cref="MouseHoldRepeat"/> has a value
    ///         (the view wants continuous press events).
    ///     </para>
    /// </remarks>
    private bool ShouldAutoGrab => MouseHighlightStates != MouseState.None || MouseHoldRepeat.HasValue;

    /// <summary>
    ///     Processes a mouse event for this view. This is the main entry point for mouse input handling,
    ///     called by <see cref="IMouse.RaiseMouseEvent"/> when the mouse interacts with this view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method orchestrates the complete mouse event handling pipeline:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>
    ///                 Validates pre-conditions (view must be enabled and visible)
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Raises <see cref="MouseEvent"/> for low-level handling via <see cref="OnMouseEvent"/>
    ///                 and event subscribers
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Handles mouse grab scenarios when <see cref="MouseHighlightStates"/> or
    ///                 <see cref="MouseHoldRepeat"/> are set (press/release/click)
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Invokes commands bound to mouse clicks via <see cref="MouseBindings"/>
    ///                 (default: <see cref="Command.Activate"/> → <see cref="Activating"/> event)
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <strong>Continuous Button Press:</strong> When <see cref="MouseHoldRepeat"/> is
    ///         <see langword="true"/> and the user holds a mouse button down, this method is repeatedly called
    ///         with <see cref="MouseFlags.LeftButtonPressed"/> (or other button pressed) events, enabling repeating button
    ///         behavior (e.g., scroll buttons).
    ///     </para>
    ///     <para>
    ///         <strong>Mouse Grab:</strong> Views with <see cref="MouseHighlightStates"/> or
    ///         <see cref="MouseHoldRepeat"/> enabled automatically grab the mouse on button press,
    ///         receiving all subsequent mouse events until the button is released, even if the mouse moves
    ///         outside the view's <see cref="Viewport"/>.
    ///     </para>
    /// </remarks>
    /// <param name="mouse">
    ///     The mouse event to process. Coordinates in <see cref="Mouse.Position"/> are relative
    ///     to the view's <see cref="Viewport"/>.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if the event was handled and should not be propagated;
    ///     <see langword="false"/> if the event was not handled and should continue propagating;
    ///     <see langword="null"/> if the view declined to handle the event (e.g., disabled or not visible).
    /// </returns>
    /// <seealso cref="MouseEvent"/>
    /// <seealso cref="OnMouseEvent"/>
    /// <seealso cref="MouseBindings"/>
    /// <seealso cref="Activating"/>
    /// <seealso cref="MouseHoldRepeat"/>
    /// <seealso cref="MouseHighlightStates"/>
    public bool? NewMouseEvent (Mouse mouse)
    {
        // 1. Pre-conditions
        mouse.Position ??= mouse.ScreenPosition;

        // BUGBUG: If the mouse is disabled after it's been grabbed or while it's being held down, it will stop receiving events and won't be able to ungrab itself.
        // BUGBUG: We need tests to prove this behavior and determine if we need to ungrab the mouse when the view is disabled or becomes not visible while the mouse is grabbed.
        if (!Enabled)
        {
            // A disabled view should not eat mouse events
            return false;
        }

        // BUGBUG: If the mouse is hidden after it's been grabbed or while it's being held down, it will stop receiving events and won't be able to ungrab itself.
        // BUGBUG: We need tests to prove this behavior and determine if we need to ungrab the mouse when the view is disabled or becomes not visible while the mouse is grabbed.
        if (!CanBeVisible (this))
        {
            return false;
        }

        if (!MousePositionTracking && mouse.Flags == MouseFlags.PositionReport)
        {
            return false;
        }

        Trace.Mouse (this, mouse, "Entry");

        // 2. Setup MouseHoldRepeater if needed
        if (MouseHoldRepeater is null)
        {
            MouseHoldRepeater = new MouseHoldRepeaterImpl (this, App?.TimedEvents, App?.Mouse);
        }

        // 3. MouseHoldRepeat timer management
        if (MouseHoldRepeat != null)
        {
            if (mouse.IsPressed)
            {
                MouseHoldRepeater.MouseIsHeldDownTick += MouseHoldRepeaterOnMouseIsHeldDownTick;
                MouseHoldRepeater.Start (mouse);
            }
            else
            {
                MouseHoldRepeater.MouseIsHeldDownTick -= MouseHoldRepeaterOnMouseIsHeldDownTick;
                MouseHoldRepeater.Stop ();
            }
        }

        // 4. Low-level MouseEvent (cancellable)
        if (RaiseMouseEvent (mouse) || mouse.Handled)
        {
            Trace.Mouse (this, mouse, "Exit", $"{true}");

            return true;
        }

        // 5. Auto-grab lifecycle
        if (ShouldAutoGrab)
        {
            if (mouse.IsPressed)
            {
                if (HandleAutoGrabPress (mouse))
                {
                    Trace.Mouse (this, mouse, "Exit", $"{true}");

                    return true;
                }
            }
            else if (mouse.IsReleased)
            {
                if (HandleAutoGrabRelease (mouse))
                {
                    Trace.Mouse (this, mouse, "Exit", $"{true}");

                    return true;
                }
            }
            else if (mouse.IsSingleDoubleOrTripleClicked)
            {
                if (HandleAutoGrabClicked (mouse))
                {
                    Trace.Mouse (this, mouse, "Exit", $"{mouse.Handled}");

                    return mouse.Handled;
                }
            }
        }

        // 6. Command invocation
        // When ShouldAutoGrab: Pressed/Released invoke commands in HandleAutoGrabPress/Release, Clicked ungrabs
        // When MouseHoldRepeat: Only the configured event (Released or Clicked) invokes commands
        // Otherwise: Both Released and Clicked invoke commands (Pressed only when no bindings exist for Released)

        // For MouseHoldRepeat: Press starts timer, configured event invokes command via binding
        // Timer handler (MouseHoldRepeaterOnMouseIsHeldDownTick) invokes commands during hold
        if (MouseHoldRepeat.HasValue)
        {
            // Only invoke commands on the configured mouse event - ignore everything else
            if ((mouse.Flags & MouseHoldRepeat.Value) != 0)
            {
                bool ret = RaiseCommandsBoundToButtonFlags (mouse);

                Trace.Mouse (this, mouse, "Exit", $"{ret}");

                return ret;
            }

            // Ignore all other events when MouseHoldRepeat is set
            Trace.Mouse (this, mouse, "Exit", $"MouseHoldRepeat {false}");

            return false;
        }

        // Normal behavior: Invoke commands for clicked (when not auto-grab), or pressed (when not auto-grab)
        // Note: Released and Pressed are handled by HandleAutoGrabRelease/Press when ShouldAutoGrab
        if (mouse.IsSingleDoubleOrTripleClicked || ((mouse.IsReleased || mouse.IsPressed) && !ShouldAutoGrab))
        {
            bool ret = RaiseCommandsBoundToButtonFlags (mouse);

            Trace.Mouse (this, mouse, "Exit", $"{ret}");

            return ret;
        }

        bool result = mouse.IsWheel && RaiseCommandsBoundToWheelFlags (mouse);

        Trace.Mouse (this, mouse, "Exit", $"{result}");

        return result;
    }

    /// <summary>
    ///     INTERNAL: Manages continuous button press behavior for views that have <see cref="MouseHoldRepeat"/> set to
    ///     <see langword="true"/>.
    ///     When a mouse button is held down on such a view, this instance periodically raises events to enable auto-repeat
    ///     functionality
    ///     (e.g., scrollbars that continue scrolling while the button is held, or buttons that repeat their action).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is automatically instantiated when needed in <see cref="RaiseMouseEvent"/>. It implements an
    ///         accelerating timeout
    ///         pattern where the first event fires after 500ms, with subsequent events occurring every 50ms with a 0.5
    ///         acceleration factor.
    ///     </para>
    ///     <para>
    ///         When a button press is detected, the mouse is grabbed and periodic
    ///         <see cref="IMouseHoldRepeater.MouseIsHeldDownTick"/> events
    ///         are raised until the button is released. Each tick event triggers command execution via
    ///         <see cref="RaiseCommandsBoundToButtonFlags"/>,
    ///         enabling continuous actions like scrolling or button repetition.
    ///     </para>
    ///     <para>
    ///         This is used for UI elements that benefit from auto-repeat behavior, such as scrollbar arrows, spin buttons, or
    ///         other
    ///         controls where holding down a button should continue the action.
    ///     </para>
    /// </remarks>
    internal IMouseHoldRepeater? MouseHoldRepeater { get; set; }

    /// <summary>
    ///     Raises the <see cref="RaiseMouseEvent"/>/<see cref="MouseEvent"/> event.
    /// </summary>
    /// <param name="mouse"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    public bool RaiseMouseEvent (Mouse mouse)
    {
        if (OnMouseEvent (mouse) || mouse.Handled)
        {
            return true;
        }

        MouseEvent?.Invoke (this, mouse);

        return mouse.Handled;
    }

    private void MouseHoldRepeaterOnMouseIsHeldDownTick (object? sender, CancelEventArgs<Mouse> e)
    {
        // Use the configured MouseHoldRepeat flags, defaulting to LeftButtonPressed if not set
        e.NewValue.Flags = MouseHoldRepeat ?? MouseFlags.LeftButtonPressed;

        e.NewValue.ScreenPosition = App?.Mouse.LastMousePosition ?? e.NewValue.ScreenPosition;
        RaiseCommandsBoundToButtonFlags (e.NewValue);
    }

    /// <summary>Called when a mouse event occurs within the view's <see cref="Viewport"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    /// </remarks>
    /// <param name="mouse"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnMouseEvent (Mouse mouse) => false;

    /// <summary>Raised when a mouse event occurs.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<Mouse>? MouseEvent;

    #endregion Low Level Mouse Events

    #region Auto-Grab Lifecycle Helpers

    /// <summary>
    ///     Handles the pressed event when auto-grab is enabled. Grabs the mouse, sets focus if needed,
    ///     and updates <see cref="MouseState"/>.
    /// </summary>
    /// <param name="mouse">The mouse event.</param>
    /// <returns><see langword="true"/> if processing should stop; <see langword="false"/> otherwise.</returns>
    private bool HandleAutoGrabPress (Mouse mouse)
    {
        Trace.Mouse (this, mouse, "AutoGrab");

        if (!mouse.IsPressed)
        {
            return false;
        }

        // Don't grab if an enabled SubView at the mouse position can handle the event.
        // CachedViewsUnderMouse is already updated by RaiseMouseEnterLeaveEvents before NewMouseEvent runs.
        // Disabled views are included in the cache, so we must find the deepest enabled view.
        if (App?.Mouse.CachedViewsUnderMouse is { Count: > 0 } cached)
        {
            View? deepestEnabledView = null;

            for (int i = cached.Count - 1; i >= 0; i--)
            {
                if (cached [i] is not { Enabled: true } candidate)
                {
                    continue;
                }
                deepestEnabledView = candidate;

                break;
            }

            if (deepestEnabledView is { } && deepestEnabledView != this)
            {
                // An enabled SubView is under the cursor - let it handle its own events
                return false;
            }
        }

        // If the user has just pressed the mouse, grab the mouse and set focus
        if (App is null || !App.Mouse.IsGrabbed (this))
        {
            App?.Mouse.GrabMouse (this);

            if (!HasFocus)
            {
                if (CanFocus)
                {
                    // Set the focus, but don't invoke Accept
                    SetFocus ();
                }
                else if (SuperView is { CanFocus: true, HasFocus: false, Enabled: true } superView)
                {
                    superView.SetFocus ();
                }
            }
        }

        // Update MouseState based on position
        UpdateMouseStateOnPress (mouse.Position);

        if (MouseHoldRepeat != null)
        {
            // Allow command invocation to proceed
            return false;
        }

        // Invoke commands (if any Pressed bindings exist) and continue processing
        InvokeCommandsBoundToMouse (mouse);

        return false; // Continue to allow Released/Clicked to be processed
    }

    /// <summary>
    ///     Handles the released event when auto-grab is enabled. Updates <see cref="MouseState"/>.
    ///     Only invokes commands if the mouse is still within the viewport (allows cancellation).
    /// </summary>
    /// <param name="mouse">The mouse event.</param>
    private bool HandleAutoGrabRelease (Mouse mouse)
    {
        Trace.Mouse (this, mouse, "AutoGrab");

        if (!mouse.IsReleased)
        {
            return false;
        }

        if (App is null || !App.Mouse.IsGrabbed (this))
        {
            return false;
        }

        // Update MouseState
        UpdateMouseStateOnRelease ();

        if (MouseHoldRepeat != null)
        {
            // Allow command invocation to proceed
            return false;
        }

        // Only invoke commands if mouse is still in viewport (enables cancellation by releasing outside)
        if (mouse.Position is { } pos && !Viewport.Contains (pos))
        {
            // Released outside - don't invoke commands (cancellation)
            return false; // Continue processing (will reach Clicked handler to ungrab)
        }

        // Invoke commands and continue processing (Clicked event will ungrab)
        InvokeCommandsBoundToMouse (mouse);

        return false; // Continue to Clicked handler for ungrab
    }

    /// <summary>
    ///     Handles all click events (single, double, triple) when auto-grab is enabled. Ungrabs the mouse.
    /// </summary>
    /// <param name="mouse">The mouse event.</param>
    /// <returns>
    ///     <see langword="true"/> if the click was outside the viewport (should stop processing);
    ///     <see langword="false"/> if the click was inside (should continue to invoke commands).
    /// </returns>
    private bool HandleAutoGrabClicked (Mouse mouse)
    {
        Trace.Mouse (this, mouse, "AutoGrab");

        if (!mouse.IsSingleDoubleOrTripleClicked)
        {
            return false;
        }

        if (App is null || !App.Mouse.IsGrabbed (this))
        {
            return false;
        }

        // We're grabbed. Clicked event comes after the last Release. This is our signal to ungrab
        App.Mouse.UngrabMouse ();

        // If mouse is still in bounds, return false to indicate commands should be raised
        return !Viewport.Contains (mouse.Position!.Value);
    }

    /// <summary>
    ///     Updates <see cref="MouseState"/> when a button is pressed, setting <see cref="MouseState.Pressed"/>
    ///     or <see cref="MouseState.PressedOutside"/> as appropriate.
    /// </summary>
    /// <param name="position">The mouse position relative to the view's viewport.</param>
    private void UpdateMouseStateOnPress (Point? position)
    {
        if (position is { } pos && Viewport.Contains (pos))
        {
            // The mouse is inside the viewport
            if (MouseHighlightStates.FastHasFlags (MouseState.Pressed))
            {
                MouseState |= MouseState.Pressed;
            }

            // Always clear PressedOutside when the mouse is pressed inside the Viewport
            MouseState &= ~MouseState.PressedOutside;
        }
        else
        {
            // The mouse is outside the viewport
            // When MouseHoldRepeat is set we want to keep the mouse state as pressed (e.g., a repeating button).
            // This shows the user that the button is doing something, even if the mouse is outside the Viewport.
            if (MouseHighlightStates.FastHasFlags (MouseState.PressedOutside) && !MouseHoldRepeat.HasValue)
            {
                MouseState |= MouseState.PressedOutside;
            }
        }
    }

    /// <summary>
    ///     Updates <see cref="MouseState"/> when a button is released, clearing <see cref="MouseState.Pressed"/>
    ///     and <see cref="MouseState.PressedOutside"/> flags.
    /// </summary>
    private void UpdateMouseStateOnRelease ()
    {
        MouseState &= ~MouseState.Pressed;
        MouseState &= ~MouseState.PressedOutside;
    }

    #endregion Auto-Grab Lifecycle Helpers

    #region Command Invocation

    /// <summary>
    ///     INTERNAL API: Converts mouse click events into <see cref="Command"/>s by invoking the commands bound
    ///     to the mouse buttons via <see cref="MouseBindings"/>. By default, all mouse clicks are bound to
    ///     <see cref="Command.Activate"/> which raises the <see cref="Activating"/> event.
    /// </summary>
    /// <param name="args">The mouse event arguments containing the mouse flags and position information.</param>
    /// <returns>
    ///     <see langword="true"/> if a command was invoked and handled; <see langword="false"/> if no command was invoked
    ///     or the command was not handled. Also sets <see cref="HandledEventArgs.Handled"/> on the input
    ///     <paramref name="args"/>
    ///     .
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Invokes commands bound to mouse events via <see cref="MouseBindings"/>. By default, Pressed events
    ///         are bound to <see cref="Command.Activate"/> and Clicked events are bound to <see cref="Command.Accept"/>.
    ///     </para>
    /// </remarks>
    protected bool RaiseCommandsBoundToButtonFlags (Mouse args)
    {
        // Pre-conditions
        if (!Enabled)
        {
            return args.Handled = false;
        }

        Trace.Mouse (this, args.Flags, args.Position!.Value, "Invoking");

        // By default, this will raise Activating/OnActivating - Subclasses can override this via
        // ReplaceCommand (Command.Activate ...).
        args.Handled = InvokeCommandsBoundToMouse (args) == true;

        Trace.Mouse (this, args.Flags, args.Position!.Value, "Invoked", $"handled={args.Handled}");

        return args.Handled;
    }

    /// <summary>
    ///     INTERNAL API: Converts mouse wheel events into <see cref="Command"/>s by invoking the commands bound
    ///     to the mouse wheel via <see cref="MouseBindings"/>. By default, all mouse wheel events are not bound.
    /// </summary>
    /// <param name="args">The mouse event arguments containing the mouse flags and position information.</param>
    /// <returns>
    ///     <see langword="true"/> if a command was invoked and handled; <see langword="false"/> if no command was invoked
    ///     or the command was not handled.
    ///     .
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The converted wheel event is then passed to <see cref="InvokeCommandsBoundToMouse"/> to execute
    ///         any commands bound to the mouse flags via <see cref="MouseBindings"/>.
    ///     </para>
    /// </remarks>
    protected bool RaiseCommandsBoundToWheelFlags (Mouse args)
    {
        // Pre-conditions
        if (!Enabled)
        {
            return args.Handled = false;
        }

        Trace.Mouse (this, args.Flags, args.Position!.Value, "Invoking");

        args.Handled = InvokeCommandsBoundToMouse (args) == true;

        Trace.Mouse (this, args.Flags, args.Position!.Value, "Invoked", $"handled={args.Handled}");

        return args.Handled;
    }

    /// <summary>
    ///     INTERNAL API: Invokes the Commands bound to the MouseFlags specified by <paramref name="mouseEventArgs"/>.
    ///     <para>See <see href="../docs/mouse.md">for an overview of Terminal.Gui mouse APIs.</see></para>
    /// </summary>
    /// <param name="mouseEventArgs">The mouse event passed.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was invoked; input processing should continue.
    ///     <see langword="false"/> if at least one command was invoked and was not handled (or cancelled); input processing
    ///     should continue.
    ///     <see langword="true"/> if at least one command was invoked and handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    protected bool? InvokeCommandsBoundToMouse (Mouse mouseEventArgs)
    {
        if (!MouseBindings.TryGet (mouseEventArgs.Flags, out MouseBinding binding))
        {
            return null;
        }

        return InvokeCommands (binding.Commands, binding with { MouseEvent = mouseEventArgs, Source = new WeakReference<View> (this) });
    }

    #endregion Command Invocation

    #region MouseState Handling

    /// <summary>
    ///     Gets the state of the mouse relative to the View. When changed, the <see cref="MouseStateChanged"/>/
    ///     <see cref="OnMouseStateChanged"/>
    ///     event will be raised.
    /// </summary>
    public MouseState MouseState
    {
        get;
        internal set
        {
            if (field == value)
            {
                return;
            }

            EventArgs<MouseState> args = new (value);

            RaiseMouseStateChanged (args);

            field = value;
        }
    }

    /// <summary>
    ///     Gets or sets which <see cref="MouseState"/> changes should cause the View to change its appearance.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="MouseState.In"/> is set by default, which means the View will be highlighted when the
    ///         mouse is over it. The default behavior of <see cref="SetAttributeForRole"/>
    ///         is to use the <see cref="Drawing.VisualRole.Highlight"/> role for the highlight Attribute.
    ///     </para>
    ///     <para>
    ///         <see cref="MouseState.Pressed"/> means the View will be highlighted when the mouse is pressed over it.
    ///         <see cref="Border"/>'s default behavior is to use
    ///         the <see cref="VisualRole.Highlight"/> role when the Border is pressed for Arrangement.
    ///         <see cref="Margin"/>'s default behavior, when shadows are enabled, is to move the shadow providing
    ///         a pressed effect.
    ///     </para>
    ///     <para>
    ///         <see cref="MouseState.PressedOutside"/> means the View will be highlighted when the mouse was pressed
    ///         inside it and then moved outside of it, unless <see cref="MouseHoldRepeat"/> is set to
    ///         <see langword="true"/>, in which case the flag has no effect.
    ///     </para>
    /// </remarks>
    public MouseState MouseHighlightStates { get; set; }

    /// <summary>
    ///     INTERNAL Raises the <see cref="MouseStateChanged"/> event.
    /// </summary>
    /// <param name="args"></param>
    private void RaiseMouseStateChanged (EventArgs<MouseState> args)
    {
        OnMouseStateChanged (args);

        MouseStateChanged?.Invoke (this, args);
    }

    /// <summary>
    ///     Called when <see cref="MouseState"/> has changed, indicating the View should be highlighted or not. The
    ///     <see cref="MouseState"/> passed in the event
    ///     indicates the highlight style that will be applied.
    /// </summary>
    protected virtual void OnMouseStateChanged (EventArgs<MouseState> args) { }

    /// <summary>
    ///     Raised when <see cref="MouseState"/> has changed, indicating the View should be highlighted or not. The
    ///     <see cref="MouseState"/> passed in the event
    ///     indicates the highlight style that will be applied.
    /// </summary>
    public event EventHandler<EventArgs<MouseState>>? MouseStateChanged;

    #endregion MouseState Handling

    private void DisposeMouse ()
    {
        if (MouseHoldRepeater is { })
        {
            MouseHoldRepeater.MouseIsHeldDownTick -= MouseHoldRepeaterOnMouseIsHeldDownTick;
            MouseHoldRepeater.Dispose ();
        }

        if (App is { } && App.Mouse.IsGrabbed (this))
        {
            App.Mouse.UngrabMouse ();
        }
    }
}

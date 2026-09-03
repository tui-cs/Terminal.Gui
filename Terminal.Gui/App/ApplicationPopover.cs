using Terminal.Gui.Tracing;

namespace Terminal.Gui.App;

/// <summary>
///     Helper class for support of <see cref="IPopoverView"/> views for <see cref="IApplication"/>. Held by
///     <see cref="IApplication.Popovers"/>
/// </summary>
public sealed class ApplicationPopover : IDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationPopover"/> class.
    /// </summary>
    public ApplicationPopover () { }

    private readonly List<IPopoverView> _popovers = [];

    private IPopoverView? _activePopover;

    /// <inheritdoc/>
    public void Dispose ()
    {
        Hide ();

        foreach (IPopoverView popover in _popovers)
        {
            if (popover is IDisposable disposable)
            {
                disposable.Dispose ();
            }
        }

        _popovers.Clear ();
        _activePopover = null;
    }

    /// <summary>
    ///     The <see cref="IApplication"/> instance used by this instance.
    /// </summary>
    public IApplication? App { get; set; }

    /// <summary>
    ///     Raised when a popover is registered via <see cref="Register"/>.
    /// </summary>
    public event EventHandler<EventArgs<IPopoverView>>? PopoverRegistered;

    /// <summary>
    ///     Raised when a popover is de-registered via <see cref="DeRegister"/>.
    /// </summary>
    public event EventHandler<EventArgs<IPopoverView>>? PopoverDeRegistered;

    /// <summary>
    ///     De-registers <paramref name="popover"/> with the application. Use this to remove the popover and it's
    ///     keyboard bindings from the application.
    /// </summary>
    /// <remarks>
    ///     When a popover is registered, the View instance lifetime is managed by the application. Call
    ///     <see cref="DeRegister"/>
    ///     to manage the lifetime of the popover directly.
    ///     If <paramref name="popover"/> is the active popover, it is hidden first so the region it covered is invalidated.
    /// </remarks>
    /// <param name="popover"></param>
    /// <returns></returns>
    public bool DeRegister (IPopoverView? popover)
    {
        if (popover is null || !IsRegistered (popover))
        {
            return false;
        }

        if (GetActivePopover () == popover)
        {
            // Hide first so Visible is cleared and the covered region is invalidated.
            // Clearing _activePopover alone leaves stale cells (see #5635).
            Hide (popover);
        }

        _popovers.Remove (popover);

        PopoverDeRegistered?.Invoke (this, new EventArgs<IPopoverView> (popover));

        return true;
    }

    /// <summary>
    ///     Gets the active popover, if any.
    /// </summary>
    /// <remarks>
    ///     Note, the active pop over does not necessarily to be registered with the application.
    /// </remarks>
    /// <returns></returns>
    public IPopoverView? GetActivePopover () => _activePopover;

    /// <summary>
    ///     Causes the specified popover to be hidden.
    ///     If the popover is derived from <see cref="PopoverImpl"/>, this is the same as setting
    ///     <see cref="IPopoverView.Visible"/> to <see langword="false"/>.
    /// </summary>
    /// <param name="popover">If <see langword="null"/>, the active popover will be hidden.</param>
    public void Hide (IPopoverView? popover = null)
    {
        popover ??= GetActivePopover ();

        if (_activePopover != popover)
        {
            return;
        }

        // If there's an existing popover, hide it.
        _activePopover = null;
        popover?.Visible = false;

        // Need View cast for TopRunnableView access
        if (popover is View popoverView)
        {
            popoverView.App?.TopRunnableView?.SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Indicates whether a popover has been registered or not.
    /// </summary>
    /// <param name="popover"></param>
    /// <returns></returns>
    public bool IsRegistered (IPopoverView? popover) => popover is { } && _popovers.Contains (popover);

    /// <summary>
    ///     Gets the list of popovers registered with the application.
    /// </summary>
    public IReadOnlyCollection<IPopoverView> Popovers => _popovers.AsReadOnly ();

    /// <summary>
    ///     Registers <paramref name="popover"/> with the application.
    ///     This enables the popover to receive keyboard events even when it is not active.
    /// </summary>
    /// <remarks>
    ///     When a popover is registered, the View instance lifetime is managed by the application. Call
    ///     <see cref="DeRegister"/>
    ///     to manage the lifetime of the popover directly.
    /// </remarks>
    /// <param name="popover"></param>
    /// <returns><paramref name="popover"/>, after it has been registered.</returns>
    public IPopoverView? Register (IPopoverView? popover)
    {
        if (popover is null || IsRegistered (popover))
        {
            return popover;
        }
        popover.Owner ??= App?.TopRunnable;

        if (popover is View popoverView)
        {
            if (popoverView.App is null)
            {
                popoverView.App = App;
            }
            else if (popoverView.App != App)
            {
                throw
                    new InvalidOperationException ("Popover views must be associated with the same application instance as the one they are registered with.");
            }
        }

        _popovers.Add (popover);

        PopoverRegistered?.Invoke (this, new EventArgs<IPopoverView> (popover));

        return popover;
    }

    /// <summary>
    ///     Shows <paramref name="popover"/>. IPopoverView implementations should use OnVisibleChanaged/VisibleChanged to be
    ///     notified when the user has done something to cause the popover to be hidden.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This API calls <see cref="Register"/>. To disable the popover from processing keyboard events,
    ///         either call <see cref="DeRegister"/> to
    ///         remove the popover from the application or set <see cref="IPopoverView.Enabled"/> to <see langword="false"/>.
    ///     </para>
    /// </remarks>
    /// <param name="popover"></param>
    public void Show (IPopoverView? popover)
    {
        if (!IsRegistered (popover))
        {
            throw new InvalidOperationException (@"Popovers must be registered before being shown.");
        }

        // Prevent re-show of a popover that was just dismissed by a mouse-press-outside event.
        // The dismiss logic in ApplicationMouse.RaiseMouseEvent sets DismissedByMousePress before recursing,
        // and the guard spans the entire press → release → click cycle. This prevents views beneath
        // the popover from re-opening the same popover during the click that dismissed it.
        if (popover is { } && App?.Mouse is ApplicationMouse { DismissedByMousePress: { } dismissed } && dismissed == popover)
        {
            return;
        }

        // If there's an existing popover, hide it.
        if (_activePopover is { })
        {
            _activePopover.Visible = false;
            _activePopover = null;
        }

        if (popover is null)
        {
            return;
        }

        // Validation requires View cast (ViewportSettings, KeyBindings, BeginInit/EndInit)
        if (popover is View newPopover)
        {
            if (!newPopover.IsInitialized)
            {
                newPopover.App = App;
                newPopover.BeginInit ();
                newPopover.EndInit ();
            }

            if (!(newPopover.ViewportSettings.FastHasFlags (ViewportSettingsFlags.Transparent)
                  && newPopover.ViewportSettings.FastHasFlags (ViewportSettingsFlags.TransparentMouse)))
            {
                throw new InvalidOperationException ("Popovers must have ViewportSettings.Transparent and ViewportSettings.TransparentMouse set.");
            }

            if (newPopover.KeyBindings.GetFirstFromCommands (Command.Quit) is null)
            {
                throw new InvalidOperationException ("Popovers must have a key binding for Command.Quit.");
            }
        }

        _activePopover = popover;
        popover.Enabled = true;
        popover.Visible = true;
    }

    /// <summary>
    ///     INTERNAL: Called when the user presses a key. Dispatches the key to the active popover, if any,
    ///     otherwise to the popovers in the order they were registered. Inactive popovers only get hotkeys.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    internal bool DispatchKeyDownToActivePopover (Key key)
    {
        Trace.Keyboard ("Popovers", key, "Entry");

        // Do active first - Active gets all key down events.
        // Need View cast for keyboard dispatch - this is the app→view keyboard bridge
        IPopoverView? activePopover = GetActivePopover ();

        if (activePopover is not { Visible: true })
        {
            return false;
        }

        if (activePopover is View activePopoverView && activePopoverView.NewKeyDownEvent (key))
        {
            return true;
        }

        if (activePopover.Target is { } && activePopover.Target.TryGetTarget (out View? targetView) && targetView is MenuBarItem)
        {
            // HACK: If our owner is a MenuBarItem, we want to allow nav keys to propagate to the MenuBar so it can move focus to the previous MenuBarItem.
            return false;
        }

        // If the active popover didn't handle the key, try application-level hotkeys. This allows hotkeys to work even when a popover is open.
        bool? commandHandled = App?.Keyboard.InvokeCommandsBoundToKey (key);

        return commandHandled is true;
    }

    /// <summary>
    ///     INTERNAL: Hides a popover view if it supports the quit command and is currently visible. It checks for the
    ///     command's
    ///     support before hiding.
    /// </summary>
    /// <param name="visiblePopover">
    ///     The view that is being checked and potentially hidden based on its visibility and command
    ///     support.
    /// </param>
    internal static void HideWithQuitCommand (IPopoverView? visiblePopover)
    {
        if (visiblePopover is not View visiblePopoverView)
        {
            return;
        }

        if (visiblePopoverView.Visible
            && (!visiblePopoverView.GetSupportedCommands ().Contains (Command.Quit)
                || (visiblePopoverView.InvokeCommand (Command.Quit) is true && visiblePopoverView.Visible)))
        {
            visiblePopoverView.Visible = false;
        }
    }

    /// <summary>
    ///     INTERNAL: Dispatches <paramref name="key"/> to inactive popovers. Inactive popovers only get hotkeys.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="activePopover">The popover that was active when <see cref="DispatchKeyDownToActivePopover"/> was called.</param>
    /// <returns></returns>
    internal bool DispatchKeyDownToInactivePopovers (Key key, IPopoverView? activePopover)
    {
        // Now try inactive Popovers. They only get hotkeys.

        bool? hotKeyHandled = null;

        foreach (IPopoverView popover in App?.Popovers?.Popovers.ToList () ?? [])
        {
            // Need View cast for keyboard dispatch
            if (popover == activePopover || popover is not View popoverView || (popover.Owner is { } && popover.Owner != App?.TopRunnable))
            {
                continue;
            }

            Trace.Keyboard ("Popovers", key, "InactiveDispatch", $"Sending to {popoverView.ToIdentifyingString ()}");

            popoverView.App ??= App;

            // Inactive only get hotkeys
            hotKeyHandled = popoverView.NewKeyDownEvent (key);

            Trace.Keyboard ("Popovers", key, "InactiveResult", $"{popoverView.ToIdentifyingString ()} returned {hotKeyHandled}");

            if (hotKeyHandled is true)
            {
                return true;
            }
        }

        return hotKeyHandled is true;
    }
}

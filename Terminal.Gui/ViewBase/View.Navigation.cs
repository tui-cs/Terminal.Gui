using System.Diagnostics;

namespace Terminal.Gui.ViewBase;

public partial class View // Focus and cross-view navigation management (TabStop, TabIndex, etc...)
{
    private bool _canFocus;

    /// <summary>
    ///     Advances the focus to the next or previous view in the focus chain, based on
    ///     <paramref name="direction"/>.
    ///     itself.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If there is no next/previous view to advance to, the focus is set to the view itself.
    ///     </para>
    ///     <para>
    ///         See the View Navigation Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/navigation.html"/>
    ///     </para>
    /// </remarks>
    /// <param name="direction"></param>
    /// <param name="behavior"></param>
    /// <returns>
    ///     <see langword="true"/> if focus was changed to another subview (or stayed on this one), <see langword="false"/>
    ///     otherwise.
    /// </returns>
    public bool AdvanceFocus (NavigationDirection direction, TabBehavior? behavior)
    {
        //Logging.Trace ($"{Id} - {direction} {behavior}");
        if (!CanBeVisible (this)) // TODO: is this check needed?
        {
            return false;
        }

        if (RaiseAdvancingFocus (direction, behavior))
        {
            return true;
        }

        View? focused = Focused;

        if (focused is { } && focused.AdvanceFocus (direction, behavior))
        {
            return true;
        }

        // AdvanceFocus did not advance - do we wrap, or move up to the superview?

        View [] focusChain = GetFocusChain (direction, behavior);

        if (focusChain.Length == 0)
        {
            return false;
        }

        // Special case TabGroup
        if (behavior == TabBehavior.TabGroup)
        {
            if (direction == NavigationDirection.Forward && focused == focusChain [^1] && SuperView is null)
            {
                // We're at the top of the focus chain. Go back down the focus chain and focus the first TabGroup
                if (AdvanceFocusChain ())
                {
                    return true;
                }
            }

            if (direction == NavigationDirection.Backward && focused == focusChain [0] && SuperView is null)
            {
                // We're at the bottom of the focus chain
                if (AdvanceFocusChain ())
                {
                    return true;
                }
            }
        }

        int focusedIndex = focusChain.IndexOf (Focused); // Will return -1 if Focused can't be found or is null
        var next = 0; // Assume we wrap to start of the focus chain

        if (focusedIndex < focusChain.Length - 1)
        {
            // We're moving w/in the subviews
            next = focusedIndex + 1;
        }
        else
        {
            // Determine if focus should remain in this focus chain, or move to the superview's focus chain
            if (SuperView is { })
            {
                // If we are TabStop, and we have at least one other focusable peer, move to the SuperView's chain
                if (TabStop == TabBehavior.TabStop && SuperView is { } && ShouldBubbleUpForWrapping (SuperView, direction, behavior))
                {
                    return false;
                }

                // TabGroup is special-cased.
                if (focused?.TabStop == TabBehavior.TabGroup)
                {
                    if (SuperView?.GetFocusChain (direction, TabBehavior.TabGroup).Length > 0)
                    {
                        // Our superview has a TabGroup subview; signal we couldn't move so nav out to it
                        return false;
                    }
                }
            }
        }

        View view = focusChain [next];

        if (view.HasFocus)
        {
            // We could not advance
            if (view != this)
            {
                // Tell it to try the other way.
                return view.RaiseAdvancingFocus (direction == NavigationDirection.Forward ? NavigationDirection.Backward : NavigationDirection.Forward,
                                                 behavior);
            }

            return view == this;
        }

        // The subview does not have focus, but at least one other that can. Can this one be focused?
        (bool focusSet, bool _) = view.SetHasFocusTrue (Focused);

        return focusSet;

        bool AdvanceFocusChain ()
        {
            if (focusChain.Length <= 0)
            {
                return false;
            }

            // Get the index of the currently focused view
            int focusedTabGroupIndex = focusChain.IndexOf (Focused); // Will return -1 if Focused can't be found or is null

            if (focusedTabGroupIndex + 1 > focusChain.Length - 1)
            {
                focusedTabGroupIndex = 0;
            }
            else
            {
                focusedTabGroupIndex++;
            }

            View [] subViews = focusChain [focusedTabGroupIndex].GetFocusChain (NavigationDirection.Forward, TabBehavior.TabStop);

            if (subViews.Length <= 0)
            {
                return false;
            }

            if (focusChain [focusedTabGroupIndex].PreviouslyFocused is null || subViews.All (v => v != focusChain [focusedTabGroupIndex].PreviouslyFocused))
            {
                return subViews [0].SetFocus ();
            }

            // We have a subview that can be focused
            return focusChain [focusedTabGroupIndex].PreviouslyFocused!.SetFocus () || subViews [0].SetFocus ();
        }
    }

    /// <summary>
    ///     Determines if focus should bubble up to a SuperView when wrapping would occur.
    ///     Iteratively checks up the SuperView hierarchy to see if there are any focusable peers at any level.
    /// </summary>
    /// <param name="view">The SuperView to check.</param>
    /// <param name="direction">The navigation direction.</param>
    /// <param name="behavior">The tab behavior to filter by.</param>
    /// <returns>
    ///     <see langword="true"/> if there are focusable peers at this level or any ancestor level,
    ///     <see langword="false"/> otherwise.
    /// </returns>
    private bool ShouldBubbleUpForWrapping (View? view, NavigationDirection direction, TabBehavior? behavior)
    {
        View? currentView = view;

        while (currentView is { })
        {
            // If this parent has multiple focusable children, we should bubble up
            View [] chain = currentView.GetFocusChain (direction, behavior);

            if (chain.Length > 1)
            {
                return true;
            }

            // If parent has only 1 child but parent is also TabStop with a SuperView, continue checking up the hierarchy
            if (currentView is { TabStop: TabBehavior.TabStop, SuperView: { } })
            {
                currentView = currentView.SuperView;
            }
            else
            {
                break;
            }
        }

        return false;
    }

    private bool RaiseAdvancingFocus (NavigationDirection direction, TabBehavior? behavior)
    {
        // Call the virtual method
        if (OnAdvancingFocus (direction, behavior))
        {
            // The event was cancelled
            return true;
        }

        var args = new AdvanceFocusEventArgs (direction, behavior);
        AdvancingFocus?.Invoke (this, args);

        // The event was cancelled
        return args.Cancel;
    }

    /// <summary>
    ///     Called when <see cref="View.AdvanceFocus"/> is about to advance focus.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If a view cancels the event and the focus could not otherwise advance, the Navigation direction will be
    ///         reversed and the event will be raised again.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/>, if the focus advance is to be cancelled, <see langword="false"/>
    ///     otherwise.
    /// </returns>
    protected virtual bool OnAdvancingFocus (NavigationDirection direction, TabBehavior? behavior) => false;

    /// <summary>
    ///     Raised when <see cref="View.AdvanceFocus"/> is about to advance focus.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Cancel the event to prevent the focus from advancing.
    ///     </para>
    ///     <para>
    ///         If a view cancels the event and the focus could not otherwise advance, the Navigation direction will be
    ///         reversed and the event will be raised again.
    ///     </para>
    /// </remarks>
    public event EventHandler<AdvanceFocusEventArgs>? AdvancingFocus;

    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> can be focused.</summary>
    /// <remarks>
    ///     <para>
    ///         See the View Navigation Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/navigation.html"/>
    ///     </para>
    ///     <para>
    ///         <see cref="SuperView"/> must also have <see cref="CanFocus"/> set to <see langword="true"/>.
    ///     </para>
    ///     <para>
    ///         When set to <see langword="false"/>, if an attempt is made to make this view focused, the focus will be set to
    ///         the next focusable view.
    ///     </para>
    ///     <para>
    ///         When set to <see langword="false"/>, the value of <see cref="CanFocus"/> for all
    ///         subviews will be cached so that when <see cref="CanFocus"/> is set back to <see langword="true"/>, the subviews
    ///         will be restored to their previous values.
    ///     </para>
    ///     <para>
    ///         Changing this property to <see langword="true"/> will cause <see cref="TabStop"/> to be set to
    ///         <see cref="TabBehavior.TabStop"/>" as a convenience. Changing this property to
    ///         <see langword="false"/> will have no effect on <see cref="TabStop"/>.
    ///     </para>
    /// </remarks>
    public bool CanFocus
    {
        get => _canFocus;
        set
        {
            if (_canFocus == value)
            {
                return;
            }

            _canFocus = value;

            if (TabStop is null && _canFocus)
            {
                TabStop = TabBehavior.TabStop;
            }

            if (!_canFocus && HasFocus)
            {
                // If CanFocus is set to false and this view has focus, make it leave focus
                // Set transversing down so we don't go back up the hierarchy...
                SetHasFocusFalse (null);
            }

            if (_canFocus && !HasFocus && Visible && SuperView is { Focused: null })
            {
                // If CanFocus is set to true and this view does not have focus, make it enter focus
                SetFocus ();
            }

            OnCanFocusChanged ();
        }
    }

    /// <summary>Raised when <see cref="CanFocus"/> has been changed.</summary>
    /// <remarks>
    ///     Raised by the <see cref="OnCanFocusChanged"/> virtual method.
    /// </remarks>
    public event EventHandler? CanFocusChanged;

    /// <summary>
    ///     Focuses the deepest focusable SubView if one exists. If there are no focusable SubViews then the focus is set to
    ///     the view itself.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="behavior"></param>
    /// <returns><see langword="true"/> if a subview other than this was focused.</returns>
    public bool FocusDeepest (NavigationDirection direction, TabBehavior? behavior)
    {
        View? deepest = FindDeepestFocusableView (direction, behavior);

        if (deepest is { })
        {
            return deepest.SetFocus ();
        }

        return SetFocus ();
    }

    /// <summary>Gets the currently focused SubView or Adornment of this view, or <see langword="null"/> if nothing is focused.</summary>
    public View? Focused
    {
        get
        {
            View? focused = GetSubViews ( /*includeBorder: true,*/ includePadding: true).FirstOrDefault (v => v.HasFocus);

            if (focused is { })
            {
                return focused;
            }

            // How about in Adornments?
            if (Margin.View is { HasFocus: true })
            {
                return Margin.View;
            }

            if (Border.View is { HasFocus: true })
            {
                return Border.View;
            }

            if (Padding.View is { HasFocus: true })
            {
                return Padding.View;
            }

            return null;
        }
    }

    internal void RaiseFocusedChanged (View? previousFocused, View? focused)
    {
        //Logging.Trace($"RaiseFocusedChanged: {focused.Title}");
        OnFocusedChanged (previousFocused, focused);
        FocusedChanged?.Invoke (this, new HasFocusEventArgs (true, true, previousFocused, focused));
    }

    /// <summary>
    ///     Called when the focused view has changed.
    /// </summary>
    /// <param name="previousFocused"></param>
    /// <param name="focused"></param>
    protected virtual void OnFocusedChanged (View? previousFocused, View? focused) { }

    /// <summary>
    ///     Raised when the focused view has changed.
    /// </summary>
    public event EventHandler<HasFocusEventArgs>? FocusedChanged;

    /// <summary>Returns a value indicating if this View is currently on Top (Active)</summary>
    public bool IsCurrentTop => App?.TopRunnableView == this;

    /// <summary>
    ///     Returns the most focused SubView down the subview-hierarchy, or this view if it has focus and no subview has focus.
    /// </summary>
    /// <value>
    ///     The most focused SubView, this view if it has focus and no subview has focus, or <see langword="null"/> if this
    ///     view does not have focus.
    /// </value>
    public View? MostFocused
    {
        get
        {
            // TODO: Remove this API. It's duplicative of Application.Navigation.GetFocused.
            if (!HasFocus)
            {
                return null;
            }

            if (Focused is null)
            {
                return this;
            }

            View? most = Focused.MostFocused;

            if (most is { })
            {
                return most;
            }

            return Focused;
        }
    }

    /// <summary>Invoked when the <see cref="CanFocus"/> property from a view is changed.</summary>
    /// <remarks>
    ///     Raises the <see cref="CanFocusChanged"/> event.
    /// </remarks>
    public virtual void OnCanFocusChanged () => CanFocusChanged?.Invoke (this, EventArgs.Empty);

    /// <summary>
    ///     INTERNAL API to restore focus to the subview that had focus before this view lost focus.
    /// </summary>
    /// <returns>
    ///     Returns true if focus was restored to a subview, false otherwise.
    /// </returns>
    internal bool RestoreFocus ()
    {
        // Ignore TabStop
        View [] indices = GetFocusChain (NavigationDirection.Forward, null);

        if (Focused is { } || PreviouslyFocused is null || !indices.Contains (PreviouslyFocused))
        {
            return false;
        }

        if (PreviouslyFocused.SetFocus ())
        {
            return true;
        }

        PreviouslyFocused = null;

        return false;
    }

    /// <summary>
    ///     Clears any focus state (e.g. the previously focused subview) from this view.
    /// </summary>
    public void ClearFocus () => PreviouslyFocused = null;

    private View? FindDeepestFocusableView (NavigationDirection direction, TabBehavior? behavior)
    {
        View [] indices = GetFocusChain (direction, behavior);

        foreach (View v in indices)
        {
            return v.FindDeepestFocusableView (direction, behavior);
        }

        return null;
    }

    #region HasFocus

    // Backs `HasFocus` and is the ultimate source of truth whether a View has focus or not.
    private bool _hasFocus;

    /// <summary>
    ///     Gets or sets whether this view has focus.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Navigation Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/navigation.html"/>
    ///     </para>
    ///     <para>
    ///         Only Views that are visible, enabled, and have <see cref="CanFocus"/> set to <see langword="true"/> are
    ///         focusable. If
    ///         these conditions are not met when this property is set to <see langword="true"/> <see cref="HasFocus"/> will
    ///         not change.
    ///     </para>
    ///     <para>
    ///         Setting this property causes the <see cref="OnHasFocusChanging"/> and <see cref="OnHasFocusChanged"/> virtual
    ///         methods (and <see cref="HasFocusChanging"/> and
    ///         <see cref="HasFocusChanged"/> events to be raised). If the event is cancelled, <see cref="HasFocus"/> will not
    ///         be changed.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see langword="true"/> will recursively set <see cref="HasFocus"/> to
    ///         <see langword="true"/> for all SuperViews up the hierarchy.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see langword="true"/> will cause the subview furthest down the hierarchy that is
    ///         focusable to also gain focus (as long as <see cref="TabStop"/> is set to <see langword="true"/>).
    ///     </para>
    ///     <para>
    ///         Setting this property to <see langword="false"/> will cause <see cref="AdvanceFocus"/> to set
    ///         the focus on the next view to be focused.
    ///     </para>
    /// </remarks>
    public bool HasFocus
    {
        set
        {
            if (HasFocus == value)
            {
                return;
            }

            if (value)
            {
                // NOTE: If Application.Navigation is null, we pass null to FocusChanging. For unit tests.
                (bool focusSet, bool _) = SetHasFocusTrue (App?.Navigation?.GetFocused ());

                if (focusSet)
                {
                    // The change happened
                    // HasFocus is now true
                }
            }
            else
            {
                SetHasFocusFalse (null);

                Debug.Assert (!_hasFocus);

                if (_hasFocus)
                {
                    // force it.
                    _hasFocus = false;
                }
            }
        }
        get => _hasFocus;
    }

    /// <summary>
    ///     Causes this view to be focused. Calling this method has the same effect as setting <see cref="HasFocus"/> to
    ///     <see langword="true"/> but with the added benefit of returning a value indicating whether the focus was set.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Navigation Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/navigation.html"/>
    ///     </para>
    /// </remarks>
    /// <returns><see langword="true"/> if the focus changed; <see langword="true"/> false otherwise.</returns>
    public bool SetFocus ()
    {
        (bool focusSet, bool _) = SetHasFocusTrue (App?.Navigation?.GetFocused ());

        return focusSet;
    }

    /// <summary>
    ///     When this view loses focus, this property is set to the subview that had focus before the view lost focus. This
    ///     allows
    ///     focus to be restored to the same subview when focus returns to this view. This is set to <see langword="null"/>
    ///     when
    ///     the view gains focus.
    /// </summary>
    public View? PreviouslyFocused { get; private set; }

    /// <summary>
    ///     INTERNAL: Called when focus is going to change to this view. This method is called by <see cref="SetFocus"/> and
    ///     other methods that
    ///     set or remove focus from a view.
    /// </summary>
    /// <param name="currentFocusedView">
    ///     The currently focused view. If <see langword="null"/> there is no previously focused
    ///     view.
    /// </param>
    /// <param name="traversingUp"></param>
    /// <returns><see langword="true"/> if <see cref="HasFocus"/> was changed to <see langword="true"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private (bool focusSet, bool cancelled) SetHasFocusTrue (View? currentFocusedView, bool traversingUp = false)
    {
        Debug.Assert (SuperView is null || IsInHierarchy (SuperView, this));

        //Logging.Trace ($"{Id} - {currentFocusedView?.Id} -> {Id}");

        // Pre-conditions
        if (_hasFocus)
        {
            //// See https://github.com/tui-cs/Terminal.Gui/pull/4013#issuecomment-2823934197
            //if (Application.Navigation is { } && (Application.Navigation.GetFocused () == this || Application.Navigation.GetFocused () == MostFocused))
            //{
            //    throw new InvalidOperationException (@"Do not SetFocus on a view that is already MostFocused.");
            //}

            return (false, false);
        }

        if (currentFocusedView is { HasFocus: false })
        {
            throw new ArgumentException ("SetHasFocusTrue: currentFocusedView must HasFocus.");
        }

        var thisAsAdornment = this as AdornmentView;
        View? superViewOrParent = thisAsAdornment?.Adornment?.Parent ?? SuperView;

        if ((CanFocus && superViewOrParent is { CanFocus: false }) || !CanBeVisible (this) || !Enabled || !CanFocus)
        {
            return (false, false);
        }

        bool previousValue = HasFocus;

        bool cancelled = RaiseFocusChanging (false, true, currentFocusedView, this);

        if (cancelled)
        {
            return (false, true);
        }

        // Make sure superviews up the superview hierarchy have focus.
        // Any of them may cancel gaining focus. In which case we need to back out.
        if (superViewOrParent is { HasFocus: false })
        {
            (bool focusSet, bool svCancelled) = superViewOrParent.SetHasFocusTrue (currentFocusedView, true);

            if (!focusSet)
            {
                return (false, svCancelled);
            }
        }

        if (_hasFocus)
        {
            // Something else beat us to the change (likely a FocusChanged handler).
            return (true, false);
        }

        // By setting _hasFocus to true we definitively change HasFocus for this view.

        // Get whatever peer has focus, if any
        View? focusedPeer = superViewOrParent?.Focused;

        _hasFocus = true;

        // Ensure that the peer loses focus
        focusedPeer?.SetHasFocusFalse (this, true);

        if (!traversingUp)
        {
            // Restore focus to the previously focused subview, if any
            if (!RestoreFocus ())
            {
                // Couldn't restore focus, so use Advance to navigate to the next focusable subview, if any
                if (AdvanceFocus (NavigationDirection.Forward, null))
                {
                    // Focus advanced to a subview; prevent the exception below
                    previousValue = !HasFocus;
                }
            }
        }

        // Now make sure the old focused view loses focus
        if (currentFocusedView is { HasFocus: true } && GetFocusChain (NavigationDirection.Forward, TabStop).Contains (currentFocusedView))
        {
            currentFocusedView.SetHasFocusFalse (this);
        }

        if (PreviouslyFocused is { })
        {
            PreviouslyFocused = null;
        }

        if (Arrangement.FastHasFlags (ViewArrangement.Overlapped))
        {
            SuperView?.MoveSubViewToEnd (this);
        }

        // Focus work is done. Notify.
        RaiseFocusChanged (HasFocus, currentFocusedView, this);

        SetNeedsDraw ();

        // Post-conditions - prove correctness
        if (HasFocus == previousValue)
        {
            throw new InvalidOperationException ("FocusChanging was not cancelled and the HasFocus value did not change.");
        }

        return (true, false);
    }

    // TODO: CWP: FocusChanging should use an event arg type derived from ResultEventArgs<bool> so that its more obvious
    // TODO: the result can be changed.
    private bool RaiseFocusChanging (bool currentHasFocus, bool newHasFocus, View? currentFocused, View? newFocused)
    {
        Debug.Assert (currentFocused is null or { HasFocus: true });
        Debug.Assert (newFocused is null or { CanFocus: true });

        // Call the virtual method
        if (OnHasFocusChanging (currentHasFocus, newHasFocus, currentFocused, newFocused))
        {
            // The event was cancelled
            return true;
        }

        var args = new HasFocusEventArgs (currentHasFocus, newHasFocus, currentFocused, newFocused);
        HasFocusChanging?.Invoke (this, args);

        if (args.Cancel)
        {
            // The event was cancelled
            return true;
        }

        View? appFocused = App?.Navigation?.GetFocused ();

        if (appFocused != currentFocused)
        {
            return false;
        }

        App?.Navigation?.SetFocused (newFocused is { HasFocus: true } ? newFocused : null);

        return false;
    }

    /// <summary>
    ///     Invoked when <see cref="View.HasFocus"/> is about to change. This method is called before the
    ///     <see cref="HasFocusChanging"/> event is raised.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use <see cref="OnHasFocusChanged"/> to be notified after the focus has changed.
    ///     </para>
    /// </remarks>
    /// <param name="currentHasFocus">The current value of <see cref="View.HasFocus"/>.</param>
    /// <param name="newHasFocus">The value <see cref="View.HasFocus"/> will have if the focus change happens.</param>
    /// <param name="currentFocused">The view that is currently Focused. May be <see langword="null"/>.</param>
    /// <param name="newFocused">The view that will be focused. May be <see langword="null"/>.</param>
    /// <returns>
    ///     <see langword="true"/>, if the change to <see cref="View.HasFocus"/> is to be cancelled, <see langword="false"/>
    ///     otherwise.
    /// </returns>
    protected virtual bool OnHasFocusChanging (bool currentHasFocus, bool newHasFocus, View? currentFocused, View? newFocused) => false;

    /// <summary>
    ///     Raised when <see cref="View.HasFocus"/> is about to change.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Cancel the event to prevent the focus from changing.
    ///     </para>
    ///     <para>
    ///         Use <see cref="HasFocusChanged"/> to be notified after the focus has changed.
    ///     </para>
    /// </remarks>
    public event EventHandler<HasFocusEventArgs>? HasFocusChanging;

    /// <summary>
    ///     Called when this view should stop being focused.
    /// </summary>
    /// <param name="newFocusedView">
    ///     The new focused view. If <see langword="null"/> it is not known which view will be
    ///     focused.
    /// </param>
    /// <param name="traversingDown">
    ///     Set to true to traverse down the focus
    ///     chain only. If false, the method will attempt to AdvanceFocus on the superview or restore focus on
    ///     Application.Navigation.GetFocused().
    /// </param>
    /// <exception cref="InvalidOperationException"></exception>
    private void SetHasFocusFalse (View? newFocusedView, bool traversingDown = false)
    {
        // Pre-conditions
        if (!_hasFocus)
        {
            throw new InvalidOperationException ("SetHasFocusFalse should not be called if the view does not have focus.");
        }

        if (newFocusedView is { HasFocus: false })
        {
            throw new InvalidOperationException ("SetHasFocusFalse new focused view does not have focus.");
        }

        var thisAsAdornment = this as AdornmentView;
        View? superViewOrParent = thisAsAdornment?.Adornment?.Parent ?? SuperView;

        // If newFocusedVew is null, we need to find the view that should get focus, and SetFocus on it.
        if (!traversingDown && newFocusedView is null)
        {
            // Restore focus?
            if (superViewOrParent?.PreviouslyFocused is { CanFocus: true })
            {
                // TODO: Why don't we call RestoreFocus here?
                if (superViewOrParent.PreviouslyFocused != this && superViewOrParent.PreviouslyFocused.SetFocus ())
                {
                    // The above will cause SetHasFocusFalse, so we can return
                    Debug.Assert (!_hasFocus);

                    return;
                }
            }

            // AdvanceFocus?
            if (superViewOrParent is { CanFocus: true })
            {
                if (superViewOrParent.AdvanceFocus (NavigationDirection.Forward, TabStop))
                {
                    // The above might have SetHasFocusFalse, so we can return
                    if (!_hasFocus)
                    {
                        return;
                    }
                }

                if (superViewOrParent is { HasFocus: true, CanFocus: true })
                {
                    newFocusedView = superViewOrParent;
                }
            }

            // Application.Navigation.GetFocused?
            View? applicationFocused = App?.Navigation?.GetFocused ();

            if (newFocusedView is null && applicationFocused != this && applicationFocused is { CanFocus: true })
            {
                // Temporarily ensure this view can't get focus
                bool prevCanFocus = _canFocus;
                _canFocus = false;
                bool restoredFocus = applicationFocused.RestoreFocus ();
                _canFocus = prevCanFocus;

                if (restoredFocus)
                {
                    // The above caused SetHasFocusFalse, so we can return
                    Debug.Assert (!_hasFocus);

                    return;
                }
            }

            // Application.TopRunnable?
            if (newFocusedView is null && App?.TopRunnableView is { CanFocus: true, HasFocus: false })
            {
                // Temporarily ensure this view can't get focus
                bool prevCanFocus = _canFocus;
                _canFocus = false;
                bool restoredFocus = App?.TopRunnableView.RestoreFocus () ?? false;
                _canFocus = prevCanFocus;

                if (App?.TopRunnableView is { CanFocus: true, HasFocus: true })
                {
                    newFocusedView = App?.TopRunnableView;
                }
                else if (restoredFocus)
                {
                    // The above caused SetHasFocusFalse, so we can return
                    Debug.Assert (!_hasFocus);

                    return;
                }
            }

            // No other focusable view to be found. Just "leave" us...
        }

        Debug.Assert (_hasFocus);

        // Before we can leave focus, we need to make sure that all views down the subview-hierarchy have left focus.
        View? mostFocused = MostFocused;

        if (mostFocused is { } && (newFocusedView is null || mostFocused != newFocusedView))
        {
            // Start at the bottom and work our way up to us
            View? bottom = mostFocused;

            while (bottom is { } && bottom != this)
            {
                if (bottom.HasFocus)
                {
                    bottom.SetHasFocusFalse (newFocusedView, true);

                    Debug.Assert (_hasFocus);
                }

                bottom = bottom.SuperView;
            }

            Debug.Assert (_hasFocus);
        }

        superViewOrParent?.PreviouslyFocused = this;

        bool previousValue = HasFocus;

        Debug.Assert (_hasFocus);

        // Note, can't be cancelled.
        RaiseFocusChanging (HasFocus, !HasFocus, this, newFocusedView);

        // Even though the change can't be cancelled, some listener may have changed the focus to another view.
        if (!_hasFocus)
        {
            // Notify caused HasFocus to change to false.
            return;
        }

        // Set HasFocus false
        _hasFocus = false;

        RaiseFocusChanged (HasFocus, this, newFocusedView);

        if (_hasFocus)
        {
            // Notify caused HasFocus to change to true.
            return;
        }

        // Post-conditions - prove correctness
        if (HasFocus == previousValue)
        {
            throw new InvalidOperationException ("SetHasFocusFalse and the HasFocus value did not change.");
        }

        //if (SuperView is AdornmentView { HasFocus: true } superAsAdornment)
        //{
        //    superAsAdornment.SetHasFocusFalse (newFocusedView, true);
        //}

        SetNeedsDraw ();
    }

    // TODO: CWP: FocusChanged should not be using event args derived from CancelEventArgs, as it is not cancellable.
    private void RaiseFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        // If we are the most focused view, we need to set the focused view in Application.Navigation
        if (newHasFocus && focusedView?.Focused is null)
        {
            App?.Navigation?.SetFocused (focusedView);
        }

        // Call the virtual method
        OnHasFocusChanged (newHasFocus, previousFocusedView, focusedView);

        // Raise the event
        var args = new HasFocusEventArgs (newHasFocus, newHasFocus, previousFocusedView, focusedView);
        HasFocusChanged?.Invoke (this, args);

        if (newHasFocus || focusedView is null)
        {
            SuperView?.RaiseFocusedChanged (previousFocusedView, focusedView);
        }
    }

    /// <summary>
    ///     Invoked after <see cref="HasFocus"/> has changed. This method is called before the <see cref="HasFocusChanged"/>
    ///     event is raised.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event cannot be cancelled.
    ///     </para>
    /// </remarks>
    /// <param name="newHasFocus">The new value of <see cref="View.HasFocus"/>.</param>
    /// <param name="previousFocusedView"></param>
    /// <param name="focusedView">The view that is now focused. May be <see langword="null"/></param>
    protected virtual void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView) { }

    /// <summary>Raised after <see cref="HasFocus"/> has changed.</summary>
    /// <remarks>
    ///     <para>
    ///         This event cannot be cancelled.
    ///     </para>
    /// </remarks>
#pragma warning disable CS0067 // Event is never used
    public event EventHandler<HasFocusEventArgs>? HasFocusChanged;
#pragma warning restore CS0067 // Event is never used

    #endregion HasFocus

    #region Tab/Focus Handling

    /// <summary>
    ///     Gets the subviews and Adornments of this view that are scoped to the specified behavior and direction. If behavior
    ///     is null, all focusable subviews and
    ///     Adornments are returned.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="behavior"></param>
    /// <returns></returns>
    internal View [] GetFocusChain (NavigationDirection direction, TabBehavior? behavior)
    {
        IEnumerable<View>? filteredSubViews;

        if (behavior.HasValue)
        {
            filteredSubViews = GetSubViews (includeBorder: true, includePadding: true).Where (v => v.TabStop == behavior && v is { CanFocus: true, Visible: true, Enabled: true });
        }
        else
        {
            filteredSubViews = GetSubViews (includeBorder: true, includePadding: true).Where (v => v is { CanFocus: true, Visible: true, Enabled: true });
        }

        if (this is not IAdornmentView)
        {

            if (Padding.View is { CanFocus: true, Visible: true, Enabled: true } && (Padding.View?.TabStop == null || Padding.View?.TabStop == behavior) && Padding.Thickness != Thickness.Empty)
            {
                if (Padding.View is { })
                {
                    filteredSubViews = filteredSubViews.Append (Padding.View);
                }
            }
            //else if (Border.View is { CanFocus: true, Visible: true, Enabled: true } && (Border.View?.TabStop == null || Border.View?.TabStop == behavior) && Border.Thickness != Thickness.Empty)
            //{
            //    if (Border.View is { })
            //    {
            //        filteredSubViews = filteredSubViews.Append (Border.View);
            //    }
            //}
        }

        // Border and Margin do not participate in focus chain navigation.

        if (direction == NavigationDirection.Backward)
        {
            filteredSubViews = filteredSubViews.Reverse ();
        }

        return filteredSubViews.ToArray ();
    }

    /// <summary>
    ///     Gets or sets the behavior of <see cref="AdvanceFocus"/> for keyboard navigation.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Navigation Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/navigation.html"/>
    ///     </para>
    ///     <para>
    ///         If <see langword="null"/> the tab stop has not been set and setting <see cref="CanFocus"/> to true will set it
    ///         to
    ///         <see cref="TabBehavior.TabStop"/>.
    ///     </para>
    ///     <para>
    ///         TabStop is independent of <see cref="CanFocus"/>. If <see cref="CanFocus"/> is <see langword="false"/>, the
    ///         view will not gain
    ///         focus even if this property is set and vice versa.
    ///     </para>
    ///     <para>
    ///         The default <see cref="TabBehavior.TabStop"/> keys are <c>Key.Tab</c>
    ///         and <c>Key.Tab.WithShift</c>.
    ///     </para>
    ///     <para>
    ///         The default <see cref="TabBehavior.TabGroup"/> keys are <c>Key.F6</c>
    ///         and <c>Key.F6.WithShift</c>.
    ///     </para>
    /// </remarks>
    public TabBehavior? TabStop
    {
        get;
        set
        {
            if (field is { } && field == value)
            {
                return;
            }

            field = value;
        }
    }

    #endregion Tab/Focus Handling
}

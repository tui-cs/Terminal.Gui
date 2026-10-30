using System.Diagnostics;

namespace Terminal.Gui.ViewBase;

public partial class View // SuperView/SubView hierarchy management (SuperView, SubViews, Add, Remove, etc.)
{
    private static readonly IReadOnlyCollection<View> _empty = [];

    private readonly List<View>? _subviews = [];

    /// <summary>
    ///     INTERNAL: Gets the list of SubViews precisely as they were added/ordered.
    /// </summary>
    internal IList<View> InternalSubViews => _subviews ?? [];

    /// <summary>Gets the list of SubViews as they were added/ordered.</summary>
    /// <remarks>
    ///     Use <see cref="Add(View?)"/> and <see cref="Remove(View?)"/> to add or remove subviews.
    /// </remarks>
    /// <seealso cref="GetSubViews"/>
    public IReadOnlyCollection<View> SubViews => InternalSubViews?.AsReadOnly () ?? _empty;

    /// <summary>
    ///     Gets all SubViews of this View, optionally including SubViews of the View's Adornments
    ///     (Margin, Border, and Padding). This method is used for navigation and focus management and can be overridden by
    ///     subclasses to include additional SubViews as needed
    ///     or to change the order in which SubViews are returned. By default, this method returns only the direct SubViews of
    ///     this View.
    /// </summary>
    /// <param name="includeMargin">
    ///     If <see langword="true"/>, includes SubViews from <see cref="Margin"/>. If <see langword="false"/> (default),
    ///     returns only the direct SubViews
    ///     of this View.
    /// </param>
    /// <param name="includeBorder">
    ///     If <see langword="true"/>, includes SubViews from <see cref="Border"/>. If <see langword="false"/> (default),
    ///     returns only the direct SubViews
    ///     of this View.
    /// </param>
    /// <param name="includePadding">
    ///     If <see langword="true"/>, includes SubViews from <see cref="Padding"/>. If <see langword="false"/> (default),
    ///     returns only the direct SubViews
    ///     of this View.
    /// </param>
    /// <returns>
    ///     A read-only collection containing all SubViews. If <paramref name="includeMargin"/> is
    ///     <see langword="true"/>, the collection includes SubViews from this View's direct SubViews as well
    ///     as SubViews from the Margin, Border, and Padding adornments.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method returns a snapshot of the SubViews at the time of the call. The collection is
    ///         safe to iterate even if SubViews are added or removed during iteration.
    ///     </para>
    ///     <para>
    ///         The order of SubViews in the returned collection is:
    ///         <list type="number">
    ///             <item>Direct SubViews of this View</item>
    ///             <item>SubViews of Margin (if <paramref name="includeMargin"/> is <see langword="true"/>)</item>
    ///             <item>SubViews of Border (if <paramref name="includeBorder"/> is <see langword="true"/>)</item>
    ///             <item>SubViews of Padding (if <paramref name="includePadding"/> is <see langword="true"/>)</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public virtual IReadOnlyCollection<View> GetSubViews (bool includeMargin = false, bool includeBorder = false, bool includePadding = false)
    {
        List<View> result = [];

        // Add direct SubViews
        result.AddRange (InternalSubViews);

        if (includeMargin && Margin.View is { SubViews.Count: > 0 } && Margin.Thickness != Thickness.Empty)
        {
            // Add Margin SubViews
            result.AddRange (Margin.View.SubViews);
        }

        if (includeBorder && Border.View is { SubViews.Count: > 0 } && Border.Thickness != Thickness.Empty)
        {
            // Add Border SubViews
            result.AddRange (Border.View.SubViews);
        }

        if (includePadding && Padding.View is { SubViews.Count: > 0 } && Padding.Thickness != Thickness.Empty)
        {
            // Add Padding SubViews
            result.AddRange (Padding.View.SubViews);
        }

        return result.AsReadOnly ();
    }

    private View? _superView;

    /// <summary>
    ///     Gets this Views SuperView (the View's container), or <see langword="null"/> if this view has not been added as a
    ///     SubView.
    /// </summary>
    /// <seealso cref="OnSuperViewChanging"/>
    /// <seealso cref="SuperViewChanging"/>
    /// <seealso cref="OnSuperViewChanged"/>
    /// <seealso cref="SuperViewChanged"/>
    public View? SuperView => _superView;

    /// <summary>
    ///     INTERNAL: Sets the SuperView of this View.
    /// </summary>
    /// <param name="value"></param>
    /// <returns><see langword="true"/> if the SuperView was changed; otherwise, <see langword="false"/>.</returns>
    private bool SetSuperView (View? value)
    {
        VerifyAssociationAccess (value?.App, nameof (SuperView));

        if (_superView == value)
        {
            return true;
        }

        return CWPPropertyHelper.ChangeProperty (this,
                                                 ref _superView,
                                                 value,
                                                 OnSuperViewChanging,
                                                 SuperViewChanging,
                                                 newValue =>
                                                 {
                                                     _superView?.AdjustExplicitApplicationCount (-ExplicitApplicationCount);
                                                     _superView = newValue;
                                                     _superView?.AdjustExplicitApplicationCount (ExplicitApplicationCount);
                                                     RefreshThreadAccessApplication ();
                                                 },
                                                 OnSuperViewChanged,
                                                 SuperViewChanged,
                                                 out View? _);
    }

    /// <summary>
    ///     Called when the SuperView of this View is about to be changed. This is called before the SuperView property
    ///     is updated, allowing access to the current SuperView and its resources (such as <see cref="App"/>) for
    ///     cleanup purposes.
    /// </summary>
    /// <param name="args">Hold the new SuperView that will be set, or <see langword="null"/> if being removed.</param>
    /// <returns><see langword="true"/> to cancel the change; <see langword="false"/> to allow it.</returns>
    protected virtual bool OnSuperViewChanging (ValueChangingEventArgs<View?> args) => false;

    /// <summary>
    ///     Raised when the SuperView of this View is about to be changed. This is raised before the SuperView property
    ///     is updated, allowing access to the current SuperView and its resources (such as <see cref="App"/>) for
    ///     cleanup purposes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event follows the Cancellable Work Pattern (CWP). Set <see cref="ValueChangingEventArgs{T}.Handled"/>
    ///         to <see langword="true"/> in the event args to cancel the change.
    ///     </para>
    /// </remarks>
    public event EventHandler<ValueChangingEventArgs<View?>>? SuperViewChanging;

    /// <summary>
    ///     Called when the SuperView of this View has changed.
    /// </summary>
    protected virtual void OnSuperViewChanged (ValueChangedEventArgs<View?> args) { }

    /// <summary>Raised when the SuperView of this View has changed.</summary>
    public event EventHandler<ValueChangedEventArgs<View?>>? SuperViewChanged;

    #region AddRemove

    /// <summary>
    ///     Adds a SubView (child) to this view at the specified index in the <see cref="SubViews"/> list.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Views that have been added to this view can be retrieved via the <see cref="SubViews"/> property.
    ///     </para>
    ///     <para>
    ///         To check if a View has been added to this View, compare it's <see cref="SuperView"/> property to this View.
    ///     </para>
    ///     <para>
    ///         SubViews will be disposed when this View is disposed. In other-words, calling this method causes
    ///         the lifecycle of the subviews to be transferred to this View.
    ///     </para>
    ///     <para>
    ///         Calls/Raises the <see cref="OnSubViewAdding"/>/<see cref="SubViewAdding"/> event before adding, which can
    ///         cancel the addition. Calls/Raises the <see cref="OnSubViewAdded"/>/<see cref="SubViewAdded"/> event after adding.
    ///     </para>
    ///     <para>
    ///         The <see cref="OnSuperViewChanged"/>/<see cref="SuperViewChanged"/> event will be raised on the added View.
    ///     </para>
    /// </remarks>
    /// <param name="index">The index at which to insert the view.</param>
    /// <param name="view">The view to add.</param>
    /// <returns>The view that was added.</returns>
    /// <seealso cref="Remove(View)"/>
    /// <seealso cref="RemoveAll"/>
    /// <seealso cref="OnSubViewAdding"/>
    /// <seealso cref="SubViewAdding"/>
    /// <seealso cref="OnSubViewAdded"/>
    /// <seealso cref="SubViewAdded"/>
    /// <seealso cref="OnSuperViewChanging"/>
    /// <seealso cref="SuperViewChanging"/>
    /// <seealso cref="OnSuperViewChanged"/>
    /// <seealso cref="SuperViewChanged"/>
    public View? AddAt (int index, View? view)
    {
        VerifyAccessIfEnabled ();
        view?.VerifyAssociationAccess (App, nameof (AddAt));

        if (view is null)
        {
            return null;
        }

        ArgumentOutOfRangeException.ThrowIfNegative (index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan (index, InternalSubViews.Count);

        //Debug.Assert (view.SuperView is null, $"{view} already has a SuperView: {view.SuperView}.");
        if (view.SuperView is { })
        {
            Logging.Warning ($"{view} already has a SuperView: {view.SuperView}.");
        }

        //Debug.Assert (!InternalSubViews.Contains (view), $"{view} has already been Added to {this}.");
        if (InternalSubViews.Contains (view))
        {
            Logging.Warning ($"{view} has already been Added to {this}.");
        }

        if (this is MarginView)
        {
            if (view is not ShadowView)
            {
                throw new InvalidOperationException ("SubViews of Margin are not supported.");
            }
        }

        if (!RaiseSubViewAdding (view))
        {
            return null;
        }

        // TODO: Make this thread safe
        InternalSubViews.Insert (index, view);

        // Try to set the SuperView - this may be cancelled
        if (!view.SetSuperView (this))
        {
            InternalSubViews.RemoveAt (index);

            // The change was cancelled
            return null;
        }

        // Ensure views don't have focus when being added
        view.HasFocus = false;

        if (view is { Enabled: true, Visible: true, CanFocus: true })
        {
            // Add will cause the newly added subview to gain focus if it's focusable
            if (HasFocus)
            {
                view.SetFocus ();
            }
        }

        if (view.Enabled && !Enabled)
        {
            view.Enabled = false;
        }

        // Raise event indicating a subview has been added
        // We do this before Init.
        RaiseSubViewAdded (view);

        if (IsInitialized && !view.IsInitialized)
        {
            view.BeginInit ();
            view.EndInit ();
        }

        SetNeedsDraw ();
        SetNeedsLayout ();

        return view;
    }

    /// <summary>Adds a SubView (child) to this view as the last view in the <see cref="SubViews"/> list.</summary>
    /// <remarks>
    ///     <para>
    ///         The Views that have been added to this view can be retrieved via the <see cref="SubViews"/> property.
    ///     </para>
    ///     <para>
    ///         To check if a View has been added to this View, compare it's <see cref="SuperView"/> property to this View.
    ///     </para>
    ///     <para>
    ///         SubViews will be disposed when this View is disposed. In other-words, calling this method causes
    ///         the lifecycle of the subviews to be transferred to this View.
    ///     </para>
    ///     <para>
    ///         Calls/Raises the <see cref="OnSubViewAdding"/>/<see cref="SubViewAdding"/> event before adding, which can
    ///         cancel the addition. Calls/Raises the <see cref="OnSubViewAdded"/>/<see cref="SubViewAdded"/> event after adding.
    ///     </para>
    ///     <para>
    ///         The <see cref="OnSuperViewChanged"/>/<see cref="SuperViewChanged"/> event will be raised on the added View.
    ///     </para>
    /// </remarks>
    /// <param name="view">The view to add.</param>
    /// <returns>The view that was added.</returns>
    /// <seealso cref="Remove(View)"/>
    /// <seealso cref="RemoveAll"/>
    /// <seealso cref="OnSubViewAdding"/>
    /// <seealso cref="SubViewAdding"/>
    /// <seealso cref="OnSubViewAdded"/>
    /// <seealso cref="SubViewAdded"/>
    /// <seealso cref="OnSuperViewChanging"/>
    /// <seealso cref="SuperViewChanging"/>
    /// <seealso cref="OnSuperViewChanged"/>
    /// <seealso cref="SuperViewChanged"/>
    public View? Add (View? view) => AddAt (InternalSubViews.Count, view);

    /// <summary>Adds the specified SubView (children) to the view.</summary>
    /// <param name="views">Array of one or more views (can be optional parameter).</param>
    /// <remarks>
    ///     <para>
    ///         The Views that have been added to this view can be retrieved via the <see cref="SubViews"/> property. See also
    ///         <seealso cref="Remove(View)"/> and <seealso cref="RemoveAll"/>.
    ///     </para>
    ///     <para>
    ///         SubViews will be disposed when this View is disposed. In other-words, calling this method causes
    ///         the lifecycle of the subviews to be transferred to this View.
    ///     </para>
    /// </remarks>
    public void Add (params View []? views)
    {
        if (views is null)
        {
            return;
        }

        foreach (View view in views)
        {
            Add (view);
        }
    }

    internal bool RaiseSubViewAdding (View view)
    {
        EventArgs<View> args = new (view);

        if (OnSubViewAdding (args))
        {
            return false;
        }

        SubViewAdding?.Invoke (this, args);

        return true;
    }

    /// <summary>
    ///     Called before a SubView is added to this View. Return <see langword="true"/> to cancel the addition.
    /// </summary>
    /// <param name="args">Arguments describing the pending addition. <see cref="EventArgs{T}.Value"/> is the SubView being added.</param>
    /// <returns><see langword="true"/> to cancel the addition; <see langword="false"/> to allow it.</returns>
    protected virtual bool OnSubViewAdding (EventArgs<View> args) => false;

    /// <summary>Raised before a SubView is added to this View.</summary>
    /// <remarks>
    ///     <para>
    ///         This event follows the Cancellable Work Pattern (CWP). Override <see cref="OnSubViewAdding"/> and return
    ///         <see langword="true"/> to cancel the addition.
    ///     </para>
    /// </remarks>
    public event EventHandler<EventArgs<View>>? SubViewAdding;

    internal void RaiseSubViewAdded (View view)
    {
        // If auto-hotkey assignment is enabled, assign a hotkey to the new subview
        // This must happen BEFORE the CWP events so observers see the assigned hotkey
        if (AssignHotKeys)
        {
            AssignHotKeyToView (view);
        }

        OnSubViewAdded (view);
        SubViewAdded?.Invoke (this, new SuperViewChangedEventArgs (this, view));
    }

    /// <summary>
    ///     Called when a SubView has been added to this View.
    /// </summary>
    /// <remarks>
    ///     If the SubView has not been initialized, this happens before BeginInit/EndInit is called.
    /// </remarks>
    /// <param name="view"></param>
    protected virtual void OnSubViewAdded (View view) { }

    /// <summary>Raised when a SubView has been added to this View.</summary>
    /// <remarks>
    ///     If the SubView has not been initialized, this happens before BeginInit/EndInit is called.
    /// </remarks>
    public event EventHandler<SuperViewChangedEventArgs>? SubViewAdded;

    /// <summary>Removes a SubView added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.</summary>
    /// <remarks>
    ///     <para>
    ///         Normally SubViews will be disposed when this View is disposed. Removing a SubView causes ownership of the
    ///         SubView's
    ///         lifecycle to be transferred to the caller; the caller must call <see cref="Dispose()"/>.
    ///     </para>
    ///     <para>
    ///         Calls/Raises the <see cref="OnSubViewRemoved"/>/<see cref="SubViewRemoved"/> event.
    ///     </para>
    ///     <para>
    ///         The <see cref="OnSuperViewChanged"/>/<see cref="SuperViewChanged"/> event will be raised on the removed View.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     The removed View. <see langword="null"/> if the View could not be removed.
    /// </returns>
    /// <seealso cref="Add(View)"/>
    /// <seealso cref="RemoveAll"/>
    /// <seealso cref="OnSubViewAdded"/>
    /// <seealso cref="OnSubViewRemoved"/>
    /// <seealso cref="SubViewRemoved"/>
    /// <seealso cref="OnSuperViewChanging"/>
    /// <seealso cref="SuperViewChanging"/>
    /// <seealso cref="OnSuperViewChanged"/>
    /// <seealso cref="SuperViewChanged"/>
    public View? Remove (View? view)
    {
        VerifyAccessIfEnabled ();
        view?.VerifyAssociationAccess (App, nameof (Remove));

        if (view is null)
        {
            return null;
        }

        if (InternalSubViews.Count == 0)
        {
            return view;
        }

        if (view.SuperView is null)
        {
            Logging.Warning ($"{view.ToIdentifyingString ()} cannot be Removed. SuperView is null.");
        }
        else if (view.SuperView != this)
        {
            Logging.Warning ($"{view.ToIdentifyingString ()} cannot be Removed. SuperView is not this ({view.SuperView.ToIdentifyingString ()}).");
        }

        if (!InternalSubViews.Contains (view))
        {
            Logging.Warning ($"{view.ToIdentifyingString ()} cannot be Removed. It has not been added to {this.ToIdentifyingString ()}.");
        }

        if (App is { } && App.Mouse.IsGrabbed (view))
        {
            App.Mouse.UngrabMouse ();
        }

        Rectangle touched = view.Frame;

        bool hadFocus = view.HasFocus;
        bool couldFocus = view.CanFocus;

        if (hadFocus)
        {
            view.CanFocus = false; // If view had focus, this will ensure it doesn't and it stays that way
        }

        Debug.Assert (!view.HasFocus);

        View? previousSuperView = view.SuperView;

        // Try to clear the SuperView - this may be cancelled
        if (!view.SetSuperView (null))
        {
            // The change was cancelled, restore state and return null
            view.CanFocus = couldFocus;

            return null;
        }

        Debug.Assert (view.SuperView is null);
        InternalSubViews.Remove (view);

        // Clean up focus stuff
        PreviouslyFocused = null;

        if (previousSuperView is { } && previousSuperView.PreviouslyFocused == this)
        {
            previousSuperView.PreviouslyFocused = null;
        }

        SetNeedsLayout ();
        SetNeedsDraw ();

        foreach (View v in InternalSubViews)
        {
            if (v.Frame.IntersectsWith (touched))
            {
                view.SetNeedsDraw ();
            }
        }

        view.CanFocus = couldFocus; // Restore to previous value

        if (PreviouslyFocused == view)
        {
            PreviouslyFocused = null;
        }

        RaiseSubViewRemoved (view);

        return view;
    }

    internal void RaiseSubViewRemoved (View view)
    {
        // If auto-hotkey assignment is enabled, remove the subview's hotkey from UsedHotKeys
        if (AssignHotKeys && view.HotKey != Key.Empty)
        {
            UsedHotKeys.Remove (view.HotKey);
        }

        OnSubViewRemoved (view);
        SubViewRemoved?.Invoke (this, new SuperViewChangedEventArgs (this, view));
    }

    /// <summary>
    ///     Called when a SubView has been removed from this View.
    /// </summary>
    /// <param name="view"></param>
    protected virtual void OnSubViewRemoved (View view) { }

    /// <summary>Raised when a SubView has been added to this View.</summary>
    public event EventHandler<SuperViewChangedEventArgs>? SubViewRemoved;

    // TODO: Make this non-virtual once WizardStep is refactored to use events
    /// <summary>
    ///     Removes all SubViews added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Normally SubViews will be disposed when this View is disposed. Removing a SubView causes ownership of the
    ///         SubView's
    ///         lifecycle to be transferred to the caller; the caller must call <see cref="Dispose()"/> on any Views that were
    ///         added.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     A list of removed Views.
    /// </returns>
    /// <seealso cref="Add(View)"/>
    /// <seealso cref="Remove(View)"/>
    /// <seealso cref="OnSubViewAdded"/>
    /// <seealso cref="OnSubViewRemoved"/>
    /// <seealso cref="SubViewRemoved"/>
    /// <seealso cref="OnSuperViewChanging"/>
    /// <seealso cref="SuperViewChanging"/>
    /// <seealso cref="OnSuperViewChanged"/>
    /// <seealso cref="SuperViewChanged"/>
    public virtual IReadOnlyCollection<View> RemoveAll ()
    {
        VerifyAccessIfEnabled ();

        List<View> removedList = new ();

        while (InternalSubViews.Count > 0)
        {
            View? removed = Remove (InternalSubViews [0]);

            if (removed is { })
            {
                removedList.Add (removed);
            }
        }

        return removedList.AsReadOnly ();
    }

    /// <summary>
    ///     Removes all SubViews of a type added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Normally SubViews will be disposed when this View is disposed. Removing a SubView causes ownership of the
    ///         SubView's
    ///         lifecycle to be transferred to the caller; the caller must call <see cref="Dispose()"/> on any Views that were
    ///         added.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     A list of removed Views.
    /// </returns>
    public IReadOnlyCollection<TView> RemoveAll<TView> () where TView : View
    {
        VerifyAccessIfEnabled ();

        List<TView> removedList = new ();

        foreach (TView view in InternalSubViews.OfType<TView> ().ToList ())
        {
            Remove (view);
            removedList.Add (view);
        }

        return removedList.AsReadOnly ();
    }

#pragma warning disable CS0067 // The event is never used
    /// <summary>Raised when a SubView has been removed from this View.</summary>
    public event EventHandler<SuperViewChangedEventArgs>? Removed;
#pragma warning restore CS0067 // The event is never used

    #endregion AddRemove

    /// <summary>
    ///     Walks up the <see cref="SuperView"/> chain and returns the topmost ancestor of
    ///     <paramref name="from"/> (or of this View if <paramref name="from"/> is <see langword="null"/>).
    /// </summary>
    /// <param name="from">
    ///     The View whose ancestor chain to walk. If <see langword="null"/>, starts from this View's <see cref="SuperView"/>.
    /// </param>
    /// <param name="to">
    ///     If specified, traversal stops when this View is reached. Defaults to <see cref="IApplication.TopRunnableView"/>.
    /// </param>
    /// <returns>
    ///     The topmost ancestor found, <paramref name="to"/> if encountered, or <see langword="null"/> if no
    ///     ancestors exist.
    /// </returns>
    /// <seealso cref="SuperView"/>
    /// <seealso cref="IsInHierarchy"/>
    public View? GetTopSuperView (View? from = null, View? to = null)
    {
        View? top = to; // ?? App?.TopRunnableView;

        for (View? v = from?.SuperView ?? SuperView; v != null; v = v.SuperView)
        {
            top = v;

            if (top == to)
            {
                break;
            }
        }

        return top;
    }

    /// <summary>
    ///     Gets whether <paramref name="view"/> is in the View hierarchy of <paramref name="start"/>.
    /// </summary>
    /// <param name="start">The View at the start of the hierarchy.</param>
    /// <param name="view">The View to test.</param>
    /// <param name="includeAdornments">Will include all <see cref="IAdornment"/>s in addition to Subviews if true.</param>
    /// <returns></returns>
    public static bool IsInHierarchy (View? start, View? view, bool includeAdornments = false)
    {
        if (view is null || start is null)
        {
            return false;
        }

        if (view == start)
        {
            return true;
        }

        foreach (View subView in start.InternalSubViews)
        {
            if (view == subView)
            {
                return true;
            }

            bool found = IsInHierarchy (subView, view, includeAdornments);

            if (found)
            {
                return found;
            }
        }

        if (!includeAdornments)
        {
            return false;
        }

        bool inHierarchy = IsInHierarchy (start.Padding.View, view, includeAdornments);

        if (inHierarchy)
        {
            return inHierarchy;
        }

        inHierarchy = IsInHierarchy (start.Border.View, view, includeAdornments);

        if (inHierarchy)
        {
            return inHierarchy;
        }

        inHierarchy = IsInHierarchy (start.Margin.View, view, includeAdornments);

        if (inHierarchy)
        {
            return inHierarchy;
        }

        return false;
    }

    #region SubViewOrdering

    /// <summary>
    ///     Moves <paramref name="subview"/> one position towards the end of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview">The subview to move.</param>
    public void MoveSubViewTowardsEnd (View subview) =>
        PerformActionForSubView (subview,
                                 x =>
                                 {
                                     int idx = InternalSubViews.IndexOf (x);

                                     if (idx + 1 >= InternalSubViews.Count)
                                     {
                                         return;
                                     }
                                     InternalSubViews.Remove (x);
                                     InternalSubViews.Insert (idx + 1, x);
                                 });

    /// <summary>
    ///     Moves <paramref name="subview"/> to the end of the <see cref="SubViews"/> list.
    ///     If the <see cref="Arrangement"/> is <see cref="ViewArrangement.Overlapped"/>, keeps the original sorting.
    /// </summary>
    /// <param name="subview">The subview to move.</param>
    public void MoveSubViewToEnd (View subview)
    {
        if (Arrangement.FastHasFlags (ViewArrangement.Overlapped))
        {
            PerformActionForSubView (subview,
                                     x =>
                                     {
                                         while (InternalSubViews.IndexOf (x) != InternalSubViews.Count - 1)
                                         {
                                             View v = InternalSubViews [0];
                                             InternalSubViews.Remove (v);
                                             InternalSubViews.Add (v);
                                         }
                                     });

            return;
        }

        PerformActionForSubView (subview,
                                 x =>
                                 {
                                     InternalSubViews.Remove (x);
                                     InternalSubViews.Add (x);
                                 });
    }

    /// <summary>
    ///     Moves <paramref name="subview"/> one position towards the start of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview">The subview to move.</param>
    public void MoveSubViewTowardsStart (View subview) =>
        PerformActionForSubView (subview,
                                 x =>
                                 {
                                     int idx = InternalSubViews.IndexOf (x);

                                     if (idx <= 0)
                                     {
                                         return;
                                     }
                                     InternalSubViews.Remove (x);
                                     InternalSubViews.Insert (idx - 1, x);
                                 });

    /// <summary>
    ///     Moves <paramref name="subview"/> to the start of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview">The subview to move.</param>
    public void MoveSubViewToStart (View subview) =>
        PerformActionForSubView (subview,
                                 x =>
                                 {
                                     InternalSubViews.Remove (x);
                                     InternalSubViews.Insert (0, subview);
                                 });

    /// <summary>
    ///     Internal API that runs <paramref name="action"/> on a subview if it is part of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview"></param>
    /// <param name="action"></param>
    private void PerformActionForSubView (View subview, Action<View> action)
    {
        VerifyAccessIfEnabled ();
        subview.VerifyAccessIfEnabled ();

        if (InternalSubViews.Contains (subview))
        {
            action (subview);
        }

        // BUGBUG: this is odd. Why is this needed?
        SetNeedsDraw ();
        subview.SetNeedsDraw ();
    }

    #endregion SubViewOrdering
}

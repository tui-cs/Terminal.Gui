namespace Terminal.Gui.Views;

/// <summary>
///     Base implementation of <see cref="IRunnable"/> for views that can be run as blocking sessions without returning a
///     result.
/// </summary>
/// <remarks>
///     <para>
///         Views that don't need to return a result can derive from this class instead of <see cref="Runnable{TResult}"/>.
///     </para>
///     <para>
///         This class provides default implementations of the <see cref="IRunnable"/> interface
///         following the Terminal.Gui Cancellable Work Pattern (CWP).
///     </para>
///     <para>
///         For views that need to return a result, use <see cref="Runnable{TResult}"/> instead.
///     </para>
/// </remarks>
public class Runnable : View, IRunnable
{
    // Cached state - eliminates race conditions from stack queries

    /// <summary>
    ///     Constructs a new instance of the <see cref="Runnable"/> class.
    /// </summary>
    public Runnable ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;
        Arrangement = ViewArrangement.Overlapped;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);
        Border.Settings |= BorderSettings.TerminalTitle;
    }

    /// <inheritdoc/>
    public object? Result { get; set; }

    #region IRunnable Implementation - IsRunning (from base interface)

    /// <inheritdoc/>
    public void SetApp (IApplication app) => App = app;

    /// <inheritdoc/>
    public bool IsRunning { get; private set; }

    /// <inheritdoc/>
    public void SetIsRunning (bool value) => IsRunning = value;

    /// <inheritdoc/>
    public virtual void RequestStop () =>

        // Use the IRunnable-specific RequestStop if the App supports it
        App?.RequestStop (this);

    /// <inheritdoc/>
    public bool RaiseIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    {
        // Clear previous result when starting (for non-generic Runnable)
        // Derived Runnable<TResult> will clear its typed Result in OnIsRunningChanging override
        if (newIsRunning)
        {
            Result = null;
        }

        // CWP Phase 1: Virtual method (pre-notification)
        if (OnIsRunningChanging (oldIsRunning, newIsRunning))
        {
            return true; // Canceled
        }

        // CWP Phase 2: Event notification
        bool newValue = newIsRunning;
        CancelEventArgs<bool> args = new (in oldIsRunning, ref newValue);
        IsRunningChanging?.Invoke (this, args);

        return args.Cancel;
    }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<bool>>? IsRunningChanging;

    /// <inheritdoc/>
    public void RaiseIsRunningChangedEvent (bool newIsRunning)
    {
        // Initialize if needed when starting
        if (newIsRunning && !IsInitialized)
        {
            BeginInit ();
            EndInit ();

            // Initialized event is raised by View.EndInit()
        }

        // CWP Phase 3: Post-notification (work already done by Application.Begin/End)
        OnIsRunningChanged (newIsRunning);

        EventArgs<bool> args = new (newIsRunning);
        IsRunningChanged?.Invoke (this, args);
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<bool>>? IsRunningChanged;

    /// <summary>
    ///     Called before <see cref="IsRunningChanging"/> event. Override to cancel state change or perform cleanup.
    /// </summary>
    /// <param name="oldIsRunning">The current value of <see cref="IsRunning"/>.</param>
    /// <param name="newIsRunning">The new value of <see cref="IsRunning"/> (true = starting, false = stopping).</param>
    /// <returns><see langword="true"/> to cancel; <see langword="false"/> to proceed.</returns>
    /// <remarks>
    ///     <para>
    ///         Default implementation returns <see langword="false"/> (allow change).
    ///     </para>
    ///     <para>
    ///         <b>IMPORTANT</b>: When <paramref name="newIsRunning"/> is <see langword="false"/> (stopping), this is the ideal
    ///         place to perform cleanup or validation before the runnable is removed from the stack.
    ///     </para>
    ///     <example>
    ///         <code>
    /// protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    /// {
    ///     if (!newIsRunning)  // Stopping
    ///     {
    ///         // Check if user wants to save first
    ///         if (HasUnsavedChanges ())
    ///         {
    ///             int result = MessageBox.Query (App, "Save?", "Save changes?", Strings.btnNo, Strings.btnYes);
    ///             if (result == 0) return true;  // Cancel stopping
    ///             if (result == 1) Save ();
    ///         }
    ///     }
    /// 
    ///     return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
    /// }
    /// </code>
    ///     </example>
    /// </remarks>
    protected virtual bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning) => false;

    /// <summary>
    ///     Called after <see cref="IsRunning"/> has changed. Override for post-state-change logic.
    /// </summary>
    /// <param name="newIsRunning">The new value of <see cref="IsRunning"/> (true = started, false = stopped).</param>
    /// <remarks>
    ///     Default implementation does nothing. Overrides should call base to ensure extensibility.
    /// </remarks>
    protected virtual void OnIsRunningChanged (bool newIsRunning)
    {
        // Default: no-op
    }

    #endregion

    #region IRunnable Implementation - IsModal (from base interface)

    /// <inheritdoc/>
    public bool IsModal { get; private set; }

    /// <inheritdoc/>
    public void SetIsModal (bool value) => IsModal = value;

    /// <inheritdoc/>
    public bool StopRequested { get; set; }

    /// <inheritdoc/>
    public void RaiseIsModalChangedEvent (bool newIsModal)
    {
        if (newIsModal)
        {
            if (App?.AppModel == AppModel.Inline && Height.Has (out DimFill _))
            {
                // If starting inline and height is Dim.Fill, change to Dim.Auto to avoid full screen
                Height = Dim.Auto ();
            }

            // Set focus to self if becoming modal
            SetFocus ();
            App?.Navigation?.SetFocused (MostFocused);
        }
        else
        {
            App?.Popovers?.Hide ();
        }

        // CWP Phase 3: Post-notification (work already done by Application)
        OnIsModalChanged (newIsModal);

        EventArgs<bool> args = new (newIsModal);
        IsModalChanged?.Invoke (this, args);

        // Layout may need to change when modal state changes
        SetNeedsLayout ();
        SetNeedsDraw ();
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<bool>>? IsModalChanged;

    /// <summary>
    ///     Called after <see cref="IsModal"/> has changed. Override for post-activation logic.
    /// </summary>
    /// <param name="newIsModal">The new value of <see cref="IsModal"/> (true = became modal, false = no longer modal).</param>
    /// <remarks>
    ///     <para>
    ///         Default implementation does nothing. Overrides should call base to ensure extensibility.
    ///     </para>
    ///     <para>
    ///         Common uses: setting focus when becoming modal, updating UI state.
    ///     </para>
    /// </remarks>
    protected virtual void OnIsModalChanged (bool newIsModal)
    {
        // Default: no-op
    }

    #endregion
}

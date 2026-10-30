using System.Runtime.CompilerServices;

namespace Terminal.Gui.ViewBase;

public partial class View
{
    // Cache the association, not the switch: changing the application's diagnostics setting takes effect immediately.
    // Unassociated trees have no application, so their setters do not walk to the root when diagnostics are disabled.
    private IApplication? _threadAccessApplication;
    private int _explicitApplicationCount;
    internal int ExplicitApplicationCount => _explicitApplicationCount;

    private bool _hasCustomApplication;

    /// <summary>Verifies UI-thread access for this view's initialized application.</summary>
    /// <param name="operation">The mutation being verified. Defaults to the calling member's name.</param>
    /// <remarks>
    ///     This explicit check is independent of automatic diagnostics. Unassociated views and views whose application
    ///     is not initialized are permitted. This does not verify disposal or request/session ownership.
    ///     To protect a custom mutation, call this method before changing state or raising events.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The view is associated with another thread's initialized application.</exception>
    public void VerifyAccess ([CallerMemberName] string operation = "") => VerifyAccess (App, operation);

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    internal void VerifyAccessIfEnabled ([CallerMemberName] string operation = "")
    {
        IApplication? app = _threadAccessApplication;

        if (app is not IApplicationThreadAccess { ThreadAccessChecksEnabled: true })
        {
            return;
        }

        VerifyAccess (app, operation);
    }

    private void VerifyAccess (IApplication? app, string operation)
    {
        if (app is not { Initialized: true, MainThreadId: { } expectedThreadId } || expectedThreadId == Environment.CurrentManagedThreadId)
        {
            return;
        }

        ThrowThreadAccessException (app, operation, expectedThreadId);
    }

    [MethodImpl (MethodImplOptions.NoInlining)]
    private void ThrowThreadAccessException (IApplication app, string operation, int expectedThreadId)
    {
        // Resolve session information only on failure. Avoid virtual formatting methods and user event handlers.
        View? runnable = null;
        SessionToken? session = null;
        SessionToken [] sessions = app.SessionStack?.ToArray () ?? [];

        for (View? view = this; view is not null; view = view.SuperView ?? (view as AdornmentView)?.Adornment?.Parent)
        {
            session = sessions.FirstOrDefault (token => ReferenceEquals (token.Runnable, view));

            if (session is not null)
            {
                runnable = view;

                break;
            }

            if (runnable is null && view is IRunnable)
            {
                runnable = view;
            }
        }

        string owner = runnable is null ? "unavailable" : $"{runnable.GetType ().Name} (Id='{runnable.Id}')";
        string sessionId = session is null ? "unavailable" : session.Id.ToString ();

        throw new InvalidOperationException ($"UI-thread access violation in {operation} for {GetType ().Name} (Id='{Id}'): expected thread {expectedThreadId}, current thread {Environment.CurrentManagedThreadId}; runnable {owner}; session {sessionId}. Use app.InvokeAsync with the owning session to update the UI.");
    }

    // Association changes must validate both the old tree and its destination before the first side effect.
    internal void VerifyAssociationAccess (IApplication? destination, string operation)
    {
        VerifyAccessIfEnabled (operation);

        if (destination is IApplicationThreadAccess { ThreadAccessChecksEnabled: true })
        {
            VerifyAccess (destination, operation);
        }

        if (_explicitApplicationCount == (_app is null ? 0 : 1) + (_hasCustomApplication ? 1 : 0))
        {
            return;
        }

        foreach (View subView in InternalSubViews)
        {
            if (subView._explicitApplicationCount == 0)
            {
                continue;
            }

            subView.VerifyAssociationAccess (destination, operation);
        }

        VerifyAdornment (Margin.View);
        VerifyAdornment (Border.View);
        VerifyAdornment (Padding.View);

        return;

        void VerifyAdornment (AdornmentView? view)
        {
            if (view is null || view._explicitApplicationCount == 0)
            {
                return;
            }

            view.VerifyAssociationAccess (destination, operation);
        }
    }

    internal void RefreshThreadAccessApplication ()
    {
        IApplication? app = App;
        bool hasCustomApplication = this is not AdornmentView && _app is null && !ReferenceEquals (app, SuperView?._threadAccessApplication);

        if (hasCustomApplication != _hasCustomApplication)
        {
            AdjustExplicitApplicationCount (hasCustomApplication ? 1 : -1);
            _hasCustomApplication = hasCustomApplication;
        }

        if (ReferenceEquals (_threadAccessApplication, app))
        {
            return;
        }

        _threadAccessApplication = app;

        foreach (View subView in InternalSubViews)
        {
            subView.RefreshThreadAccessApplication ();
        }

        Margin.View?.RefreshThreadAccessApplication ();
        Border.View?.RefreshThreadAccessApplication ();
        Padding.View?.RefreshThreadAccessApplication ();
    }

    /// <summary>Refreshes cached thread-access association after a custom <see cref="GetApp"/> owner changes.</summary>
    /// <remarks>
    ///     Standard App, SuperView, and adornment ownership changes refresh automatically. To implement another ownership
    ///     relationship, call this method after establishing the initial owner in the constructor. For later changes,
    ///     call <see cref="VerifyAccess(string)"/> before changing the owner, then call this method afterwards.
    ///     The destination application is also checked before updating the diagnostic cache.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Enabled diagnostics reject the old or new application's thread.</exception>
    protected void RefreshApplicationAssociation ()
    {
        VerifyAccessIfEnabled ();
        IApplication? destination = App;

        if (destination is IApplicationThreadAccess { ThreadAccessChecksEnabled: true })
        {
            VerifyAccess (destination, nameof (RefreshApplicationAssociation));
        }

        RefreshThreadAccessApplication ();
    }

    // Counts let association validation skip branches without explicit applications. Inherited association is checked
    // once at the mutation's entry, rather than walking every unassociated SubView on every Add/Remove.
    internal void AdjustExplicitApplicationCount (int delta)
    {
        if (delta == 0)
        {
            return;
        }

        for (View? view = this; view is not null; view = view.SuperView ?? (view as AdornmentView)?.Adornment?.Parent)
        {
            view._explicitApplicationCount += delta;
        }
    }

    private void SetApplication (IApplication? app)
    {
        VerifyAssociationAccess (app ?? SuperView?.App, nameof (App));
        int previousCount = _app is null ? 0 : 1;
        _app = app;
        AdjustExplicitApplicationCount ((_app is null ? 0 : 1) - previousCount);
        RefreshThreadAccessApplication ();
    }
}

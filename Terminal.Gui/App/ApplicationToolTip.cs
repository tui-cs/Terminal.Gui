using System.ComponentModel;

namespace Terminal.Gui.App;

/// <summary>
///     Manages tooltip behavior for a given <see cref="Window"/>.
/// </summary>
/// <remarks>
///     <para>
///         This manager provides a shared tooltip instance and associates tooltip content with multiple views.
///         It centralizes hover handling (MouseEnter / MouseLeave) and ensures that only one tooltip is visible at a time.
///     </para>
///     <para>
///         ToolTip content is defined per <see cref="View"/> using a factory (<see cref="Func{View}"/>),
///         allowing dynamic content creation on each display.
///     </para>
///     <para>
///         The manager avoids adding state directly to <see cref="View"/> by maintaining an external registry.
///     </para>
/// </remarks>
public sealed class ApplicationToolTip : IDisposable
{
    /// <summary>
    ///     The <see cref="IApplication"/> instance used by this instance.
    /// </summary>
    public IApplication? App { get; set; }

    // Stores tooltip registrations for each target view
    internal readonly Dictionary<View, ToolTipRegistration> _registrations = new ();

    // Shared tooltip instance reused across all views
    private ToolTipHost<View>? _sharedToolTip;

    // Currently active target view (if any)
    private View? _currentTarget;

    /// <summary>
    ///     Sets the tooltip text for the specified view.
    /// </summary>
    /// <param name="target">The view to associate with the tooltip. Cannot be null.</param>
    /// <param name="text">The text to display in the tooltip. Cannot be null.</param>
    public void SetToolTip (View target, string text)
    {
        ArgumentNullException.ThrowIfNull (target);
        ArgumentNullException.ThrowIfNull (text);

        SetToolTip (target, new ToolTipProvider (text));
    }

    /// <summary>
    ///     Associates a tooltip with the specified view, using a delegate to dynamically provide the tooltip text.
    /// </summary>
    /// <remarks>
    ///     Use this method when the tooltip text may change over time or depends on runtime conditions.
    ///     The tooltip text is evaluated on demand by calling the provided delegate.
    /// </remarks>
    /// <param name="target">The view to which the tooltip will be attached. Cannot be null.</param>
    /// <param name="textFactory">
    ///     A delegate that returns the tooltip text to display. Cannot be null. The delegate is invoked each time the
    ///     tooltip is shown.
    /// </param>
    public void SetToolTip (View target, Func<string> textFactory)
    {
        ArgumentNullException.ThrowIfNull (target);
        ArgumentNullException.ThrowIfNull (textFactory);

        SetToolTip (target, new ToolTipProvider (textFactory));
    }

    /// <summary>
    ///     Associates a tooltip with the specified target view using a factory function to generate the tooltip content.
    /// </summary>
    /// <remarks>
    ///     The tooltip content is created on demand by invoking the provided factory function each time
    ///     the tooltip is displayed. This allows for dynamic or context-sensitive tooltip content.
    /// </remarks>
    /// <param name="target">The view to which the tooltip will be attached. Cannot be null.</param>
    /// <param name="contentFactory">A function that returns the view to be used as the tooltip content. Cannot be null.</param>
    public void SetToolTip (View target, Func<View> contentFactory)
    {
        ArgumentNullException.ThrowIfNull (target);
        ArgumentNullException.ThrowIfNull (contentFactory);

        SetToolTip (target, new ToolTipProvider (contentFactory));
    }

    /// <summary>
    ///     Registers a tooltip provider for the specified view, enabling tooltips to be displayed when the user hovers over
    ///     the view.
    /// </summary>
    /// <remarks>
    ///     If a tooltip provider is already registered for the specified view, it will be replaced by
    ///     the new provider. ToolTips are shown when the mouse enters the view and hidden when the mouse leaves. To remove
    ///     a tooltip, use the appropriate removal method.
    /// </remarks>
    /// <param name="target">The view for which the tooltip should be displayed. Cannot be null.</param>
    /// <param name="provider">The provider that supplies tooltip content for the target view. Cannot be null.</param>
    internal void SetToolTip (View target, ToolTipProvider provider)
    {
        ArgumentNullException.ThrowIfNull (target);
        ArgumentNullException.ThrowIfNull (provider);

        RemoveToolTip (target);

        target.MouseEnter += OnMouseEnter;
        target.MouseLeave += OnMouseLeave;
        target.Disposing += OnDisposing;

        _registrations [target] = new ToolTipRegistration (OnMouseEnter, OnMouseLeave, OnDisposing);

        return;

        void OnMouseLeave (object? sender, EventArgs e)
        {
            if (_currentTarget == target)
            {
                Hide ();
            }
        }

        void OnMouseEnter (object? sender, CancelEventArgs e) => ShowFor (target, provider);
    }

    private void OnDisposing (object? sender, EventArgs e)
    {
        if (sender is View target)
        {
            target.App?.ToolTips?.RemoveToolTip (target);
        }
    }

    /// <summary>
    ///     Removes the tooltip associated with a target view.
    /// </summary>
    /// <param name="target">The target view.</param>
    /// <remarks>
    ///     This unsubscribes from events and removes any stored tooltip content.
    /// </remarks>
    public void RemoveToolTip (View target)
    {
        ArgumentNullException.ThrowIfNull (target);

        if (_registrations.TryGetValue (target, out ToolTipRegistration? registration))
        {
            target.MouseEnter -= registration.MouseEnter;
            target.MouseLeave -= registration.MouseLeave;
            target.Disposing -= registration.Disposing;

            _registrations.Remove (target);
        }

        if (_currentTarget == target)
        {
            Hide ();
        }
    }

    /// <summary>
    ///     Displays a tooltip for the specified target view using the provided tooltip content provider.
    /// </summary>
    /// <remarks>
    ///     If a tooltip is already visible for another view, it will be updated to display content for
    ///     the new target. The tooltip is anchored relative to the target view's position on the screen.
    /// </remarks>
    /// <param name="target">The view for which the tooltip will be shown. Cannot be null.</param>
    /// <param name="provider">The provider that supplies the tooltip content for the target view. Cannot be null.</param>
    public void ShowFor (View target, ToolTipProvider provider)
    {
        ArgumentNullException.ThrowIfNull (target);
        ArgumentNullException.ThrowIfNull (provider);

        _sharedToolTip ??= new ToolTipHost<View> ();
        _sharedToolTip.App ??= target.App;
        _sharedToolTip.Anchor = () => target.FrameToScreen ();
        _sharedToolTip.SetContent (provider.GetContent);

        _currentTarget = target;
        _sharedToolTip.MakeVisible ();
    }

    /// <summary>
    ///     Hides the currently visible tooltip.
    /// </summary>
    public void Hide ()
    {
        _sharedToolTip?.Visible = false;
        _currentTarget = null;
    }

    /// <summary>
    ///     Releases all resources used by the manager.
    /// </summary>
    /// <remarks>
    ///     This unsubscribes all event handlers and disposes the shared tooltip.
    /// </remarks>
    public void Dispose ()
    {
        foreach ((View target, ToolTipRegistration registration) in _registrations)
        {
            target.MouseEnter -= registration.MouseEnter;
            target.MouseLeave -= registration.MouseLeave;
            target.Disposing -= registration.Disposing;
        }

        _registrations.Clear ();
        _sharedToolTip?.Dispose ();
        _sharedToolTip = null;
        _currentTarget = null;
    }

    /// <summary>
    ///     Stores event handlers and content factory for a registered view.
    /// </summary>
    internal sealed record ToolTipRegistration (EventHandler<CancelEventArgs> MouseEnter, EventHandler MouseLeave, EventHandler Disposing);
}

namespace Terminal.Gui.App;

/// <summary>
///     Helper class for <see cref="Application"/> navigation and cursor handling. Held by
///     <see cref="Application.Navigation"/>
/// </summary>
public class ApplicationNavigation
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationNavigation"/> class.
    /// </summary>
    public ApplicationNavigation ()
    {
        // TODO: Move navigation key bindings here from ApplicationKeyboard
    }

    /// <summary>
    ///     The <see cref="IApplication"/> instance used by this instance.
    /// </summary>
    public IApplication? App { get; set; }

    private View? _focused;

    /// <summary>
    ///     Raised when the most focused <see cref="View"/> in the application has changed.
    /// </summary>
    public event EventHandler<EventArgs>? FocusedChanged;

    /// <summary>
    ///     Gets the most focused <see cref="View"/> in the application, if there is one.
    /// </summary>
    public View? GetFocused () => _focused;

    // QUESTION: This only gets Subviews and ignores Adornments. Should it use View.IsInHierarchy?
    // QUESTION: Related, see View.GetSubViews(), which does support Adornments.
    /// <summary>
    ///     Gets whether <paramref name="view"/> is in the SubView hierarchy of <paramref name="start"/>.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    public static bool IsInHierarchy (View? start, View? view)
    {
        if (view is null)
        {
            return false;
        }

        if (view == start || start is null)
        {
            return true;
        }

        foreach (View subView in start.SubViews)
        {
            if (view == subView)
            {
                return true;
            }

            bool found = IsInHierarchy (subView, view);

            if (found)
            {
                return found;
            }
        }

        return false;
    }

    /// <summary>
    ///     INTERNAL method to record the most focused <see cref="View"/> in the application.
    /// </summary>
    /// <remarks>
    ///     Raises <see cref="FocusedChanged"/>.
    /// </remarks>
    internal void SetFocused (View? value)
    {
        App.VerifyAccessIfEnabled ();

        if (_focused == value)
        {
            return;
        }

        if (_focused is { } && App?.Mouse.IsGrabbed (_focused) == true)
        {
            App.Mouse.UngrabMouse ();
        }

        //Debug.Assert (value is null or { CanFocus: true, HasFocus: true });

        _focused = value;

        // Cursor needs update when focus changes
        App?.Driver?.SetCursorNeedsUpdate (true);

        FocusedChanged?.Invoke (this, EventArgs.Empty);
    }

    /// <summary>
    ///     Advances the focus to the next or previous view in the focus chain, based on
    ///     <paramref name="direction"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If there is no next/previous view, the focus remains on the current view.
    ///     </para>
    /// </remarks>
    /// <param name="direction">The direction to advance.</param>
    /// <param name="behavior">The tab behavior.</param>
    /// <returns>
    ///     <see langword="true"/> if focus was changed to another subview (or stayed on this one), <see langword="false"/>
    ///     otherwise.
    /// </returns>
    public bool AdvanceFocus (NavigationDirection direction, TabBehavior? behavior)
    {
        App.VerifyAccessIfEnabled ();

        if (App?.Popovers?.GetActivePopover () is { Visible: true } visiblePopover)
        {
            return visiblePopover.AdvanceFocus (direction, behavior);
        }

        return App?.TopRunnableView is { } && App.TopRunnableView.AdvanceFocus (direction, behavior);
    }

    /// <summary>
    ///     Updates the terminal cursor based on the currently focused view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is called once per main loop iteration by <see cref="IApplicationMainLoop{T}"/>.
    ///     </para>
    /// </remarks>
    public void UpdateCursor ()
    {
        if (App?.Driver?.GetCursorNeedsUpdate () == false)
        {
            return;
        }

        View? mostFocused = App?.TopRunnableView?.MostFocused;

        if (mostFocused is null || !mostFocused.Cursor.IsVisible)
        {
            App?.Driver?.SetCursor (new Cursor ()); // Hide cursor

            return;
        }

        // Get cursor in content area coordinates
        Cursor mostFocusedCursor = mostFocused.Cursor;

        if (mostFocusedCursor.Position.HasValue)
        {
            // Check if position is within all ancestor viewports
            var withinViewports = true;
            View? current = mostFocused;

            while (current is { })
            {
                Rectangle viewportBounds = current.ViewportToScreen (new Rectangle (Point.Empty, current.Viewport.Size));

                if (!viewportBounds.Contains (mostFocusedCursor.Position.Value))
                {
                    withinViewports = false;

                    break;
                }

                current = current.SuperView;
            }

            if (withinViewports)
            {
                App?.Driver?.SetCursor (mostFocusedCursor);
            }
            else
            {
                App?.Driver?.SetCursor (new Cursor ()); // Hide cursor
            }
        }
        else
        {
            App?.Driver?.SetCursor (new Cursor ()); // Hide cursor
        }
    }
}

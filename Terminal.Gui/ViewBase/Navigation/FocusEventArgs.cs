namespace Terminal.Gui.ViewBase;

// TODO: CWP: FocusChanging should use an event arg type derived from ResultEventArgs<bool> so that its more obvious
// TODO: the result can be changed.
/// <summary>The event arguments for <see cref="View.HasFocus"/> events.</summary>
public class HasFocusEventArgs : CancelEventArgs<bool>
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="currentHasFocus">The current value of <see cref="View.HasFocus"/>.</param>
    /// <param name="newHasFocus">The value <see cref="View.HasFocus"/> will have if the event is not cancelled.</param>
    /// <param name="currentFocused">The view that is losing focus.</param>
    /// <param name="newFocused">The view that is gaining focus.</param>
    public HasFocusEventArgs (bool currentHasFocus, bool newHasFocus, View? currentFocused, View? newFocused) : base (ref currentHasFocus, ref newHasFocus)
    {
        CurrentFocused = currentFocused;
        NewFocused = newFocused;
    }

    /// <summary>Gets or sets the view that is losing focus.</summary>
    public View? CurrentFocused { get; set; }

    /// <summary>Gets or sets the view that is gaining focus.</summary>
    public View? NewFocused { get; set; }
}

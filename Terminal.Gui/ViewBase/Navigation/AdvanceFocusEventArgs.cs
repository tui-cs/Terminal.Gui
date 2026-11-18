namespace Terminal.Gui.ViewBase;

/// <summary>The event arguments for <see cref="View.AdvanceFocus"/> events.</summary>
public class AdvanceFocusEventArgs : CancelEventArgs<bool>
{
    /// <summary>Initializes a new instance.</summary>
    public AdvanceFocusEventArgs (NavigationDirection direction, TabBehavior? behavior) : base (false, false)
    {
        Direction = direction;
        Behavior = behavior;
    }

    /// <summary>Gets or sets the view that is losing focus.</summary>
    public NavigationDirection Direction { get; set; }

    /// <summary>Gets or sets the view that is gaining focus.</summary>
    public TabBehavior? Behavior { get; set; }
}

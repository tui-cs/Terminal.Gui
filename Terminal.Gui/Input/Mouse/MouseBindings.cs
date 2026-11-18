namespace Terminal.Gui.Input;

/// <summary>
///     Provides a collection of <see cref="MouseBinding"/> objects bound to a combination of <see cref="MouseFlags"/>.
/// </summary>
/// <seealso cref="View.MouseBindings"/>
/// <seealso cref="Command"/>
public class MouseBindings : CommandBindingsBase<MouseFlags, MouseBinding>
{
    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    public MouseBindings () : base ((commands, flags, source) => new MouseBinding (commands, flags, source), EqualityComparer<MouseFlags>.Default)
    { }

    /// <inheritdoc/>
    public override bool IsValid (MouseFlags eventArgs) => eventArgs != MouseFlags.None;
}

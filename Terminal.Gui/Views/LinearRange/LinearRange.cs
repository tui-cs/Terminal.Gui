namespace Terminal.Gui.Views;

/// <summary>
///     Convenience non-generic <see cref="LinearRange{T}"/> closed over <see cref="string"/>. Allows
///     designer scenarios (e.g. <c>AllViewsTester</c>) and reflection-based instantiation to discover
///     and create the view without supplying a type argument.
/// </summary>
/// <remarks>
///     <img src="../images/views/LinearRange.gif" alt="LinearRange demo"/>
///     <para>
///         To work with non-string option types, use <see cref="LinearRange{T}"/> directly.
///     </para>
/// </remarks>
public class LinearRange : LinearRange<string>
{
    /// <summary>Initializes a new instance of <see cref="LinearRange"/>.</summary>
    public LinearRange () { }

    /// <summary>Initializes a new instance of <see cref="LinearRange"/>.</summary>
    /// <param name="options">Initial options.</param>
    /// <param name="orientation">Initial orientation.</param>
    public LinearRange (List<string>? options, Orientation orientation = Orientation.Horizontal)
        : base (options, orientation)
    { }
}

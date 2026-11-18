namespace Terminal.Gui.Views;

/// <summary>
///     Convenience non-generic <see cref="LinearSelector{T}"/> closed over <see cref="string"/>. Allows
///     designer scenarios (e.g. <c>AllViewsTester</c>) and reflection-based instantiation to discover
///     and create the view without supplying a type argument.
/// </summary>
/// <remarks>
///     <para>
///         To work with non-string option types, use <see cref="LinearSelector{T}"/> directly.
///     </para>
/// </remarks>
public class LinearSelector : LinearSelector<string>
{
    /// <summary>Initializes a new instance of <see cref="LinearSelector"/>.</summary>
    public LinearSelector () { }

    /// <summary>Initializes a new instance of <see cref="LinearSelector"/>.</summary>
    /// <param name="options">Initial options.</param>
    /// <param name="orientation">Initial orientation.</param>
    public LinearSelector (List<string>? options, Orientation orientation = Orientation.Horizontal)
        : base (options, orientation)
    { }
}

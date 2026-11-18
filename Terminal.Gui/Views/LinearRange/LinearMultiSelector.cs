namespace Terminal.Gui.Views;

/// <summary>
///     Convenience non-generic <see cref="LinearMultiSelector{T}"/> closed over <see cref="string"/>.
///     Allows designer scenarios (e.g. <c>AllViewsTester</c>) and reflection-based instantiation to
///     discover and create the view without supplying a type argument.
/// </summary>
/// <remarks>
///     <para>
///         To work with non-string option types, use <see cref="LinearMultiSelector{T}"/> directly.
///     </para>
/// </remarks>
public class LinearMultiSelector : LinearMultiSelector<string>
{
    /// <summary>Initializes a new instance of <see cref="LinearMultiSelector"/>.</summary>
    public LinearMultiSelector () { }

    /// <summary>Initializes a new instance of <see cref="LinearMultiSelector"/>.</summary>
    /// <param name="options">Initial options.</param>
    /// <param name="orientation">Initial orientation.</param>
    public LinearMultiSelector (List<string>? options, Orientation orientation = Orientation.Horizontal)
        : base (options, orientation)
    { }
}

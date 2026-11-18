namespace Terminal.Gui.Drivers;

/// <summary>
///     Describes a sequence of chars (and optionally T metadata) accumulated
///     by an <see cref="IAnsiResponseParser"/>
/// </summary>
internal interface IHeld
{
    /// <summary>
    ///     Clears all held objects
    /// </summary>
    void ClearHeld ();

    /// <summary>
    ///     Returns string representation of the held objects
    /// </summary>
    /// <returns></returns>
    string HeldToString ();

    /// <summary>
    ///     Returns the collection objects directly e.g. <see langword="char"/>
    ///     or <see cref="Tuple"/> <see langword="char"/> + metadata T
    /// </summary>
    /// <returns></returns>
    IEnumerable<object> HeldToObjects ();

    /// <summary>
    ///     Adds the given object to the collection.
    /// </summary>
    /// <param name="o"></param>
    void AddToHeld (object o);

    int Length { get; }
}

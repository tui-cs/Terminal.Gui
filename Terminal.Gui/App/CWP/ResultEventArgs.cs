namespace Terminal.Gui.App;

using System;

#pragma warning disable CS1711

/// <summary>
///     Provides data for events that produce a result in a cancellable workflow in the Cancellable Work Pattern (CWP).
/// </summary>
/// <remarks>
///     Used for workflows where a result (e.g., <see cref="Command"/> outcome, <see cref="Attribute"/> resolution) is
///     being produced or cancelled, such as for methods like <see cref="View.GetAttributeForRole"/>.
/// </remarks>
/// <typeparam name="T">The type of the result.</typeparam>
/// <seealso cref="CancelEventArgs{T}"/>
/// <seealso cref="ValueChangingEventArgs{T}"/>
public class ResultEventArgs<T>
{
    /// <summary>
    ///     Gets or sets the result of the operation, which may be null if no result is provided.
    /// </summary>
    public T? Result { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the operation has been handled.
    ///     If true, the operation is considered handled and may use the provided result.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResultEventArgs{T}"/> class with no initial result.
    /// </summary>
    public ResultEventArgs () { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResultEventArgs{T}"/> class with an initial result.
    /// </summary>
    /// <param name="result">The initial result, which may be null for optional outcomes.</param>
    public ResultEventArgs (T? result)
    {
        Result = result;
    }
}
#pragma warning restore CS1711

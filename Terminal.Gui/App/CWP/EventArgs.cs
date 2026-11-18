namespace Terminal.Gui.App;

#pragma warning disable CS1711
/// <summary>
///    Provides data for events that convey the current value of a property or other value in a cancellable workflow (CWP).
/// </summary>
/// <remarks>
///     Used for workflows where the current value of a property or value is being conveyed, such as
///     when a property has been changed.
/// </remarks>
/// <typeparam name="T">The type of the value.</typeparam>
public class EventArgs<T> : EventArgs /*where T : notnull*/
{
    /// <summary>Initializes a new instance of the <see cref="EventArgs{T}"/> class.</summary>
    /// <param name="currentValue">The current value of the property.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    public EventArgs (in T currentValue) { Value = currentValue; }

    /// <summary>The current value of the property.</summary>
    public T Value { get; }
}

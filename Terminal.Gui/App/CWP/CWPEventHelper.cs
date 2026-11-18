namespace Terminal.Gui.App;

using System;


/// <summary>
///     Provides helper methods for executing event-driven workflows in the Cancellable Work Pattern (CWP).
/// </summary>
/// <remarks>
///     <para>
///         Used for workflows where an event is raised to allow cancellation or customization of a result,
///         such as in <see cref="IKeyboard.RaiseKeyDownEvent"/>. The <see cref="Execute{T}"/> method invokes an
///         event handler and returns whether the operation was handled, supporting result production
///         scenarios with <see cref="ResultEventArgs{T}"/>.
///     </para>
/// </remarks>
/// <seealso cref="ResultEventArgs{T}"/>
public static class CWPEventHelper
{
    /// <summary>
    ///     Executes an event-driven CWP workflow by raising an event.
    /// </summary>
    /// <typeparam name="T">The type of the result in the event arguments.</typeparam>
    /// <param name="eventHandler">The event handler to invoke, or null if no handler is subscribed.</param>
    /// <param name="args">The event arguments, containing a result and handled status.</param>
    /// <returns>True if the event was handled, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="args"/> is null.</exception>
    /// <example>
    ///     <code>
    ///         EventHandler&lt;ResultEventArgs&lt;Key&gt;&gt;? keyDownHandler = (sender, args) =>
    ///         {
    ///             if (args.Result?.KeyCode == KeyCode.Q | KeyCode.CtrlMask)
    ///             {
    ///                 args.Handled = true;
    ///             }
    ///         };
    ///         ResultEventArgs&lt;Key&gt; args = new(new Key(KeyCode.Q | KeyCode.CtrlMask));
    ///         bool handled = CWPEventHelper.Execute(keyDownHandler, args);
    ///     </code>
    /// </example>
    public static bool Execute<T> (
        EventHandler<ResultEventArgs<T>>? eventHandler,
        ResultEventArgs<T> args)
    {
        ArgumentNullException.ThrowIfNull (args);

        if (eventHandler == null)
        {
            return false;
        }

        // BUGBUG: This should pass this not null; need to test
        eventHandler.Invoke (null, args);
        return args.Handled;
    }
}

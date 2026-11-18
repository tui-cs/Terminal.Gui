namespace Terminal.Gui.App;

/// <summary>
///     Provides helper methods for executing single-phase and result-producing workflows in the Cancellable Work Pattern
///     (CWP).
/// </summary>
/// <remarks>
///     <para>
///         Used for workflows that allow customization or cancellation, such as command execution
///         (e.g., <see cref="View.RaiseAccepting"/>) or scheme resolution (e.g., <see cref="View.GetScheme"/>).
///         The <see cref="Execute{T}"/> method handles workflows without results, while
///         <see cref="ExecuteWithResult{TResult}"/> handles workflows producing results.
///     </para>
/// </remarks>
/// <seealso cref="ResultEventArgs{T}"/>
public static class CWPWorkflowHelper
{
    /// <summary>
    ///     Executes a single-phase CWP workflow with a virtual method, event, and optional default action.
    /// </summary>
    /// <typeparam name="T">The type of the result in the event arguments.</typeparam>
    /// <param name="onMethod">The virtual method invoked first, returning true to mark the workflow as handled.</param>
    /// <param name="eventHandler">The event handler to invoke, or null if no handlers are subscribed.</param>
    /// <param name="args">The event arguments containing a result and handled status.</param>
    /// <param name="defaultAction">The default action to execute if the workflow is not handled, or null if none.</param>
    /// <returns>True if the workflow was handled, false if not, or null if no event handlers are subscribed.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="onMethod"/> or <paramref name="args"/> is null.
    /// </exception>
    /// <example>
    ///     <code>
    ///         ResultEventArgs&lt;bool&gt; args = new();
    ///         Func&lt;ResultEventArgs&lt;bool&gt;, bool&gt; onAccepting = _ =&gt; false;
    ///         EventHandler&lt;ResultEventArgs&lt;bool&gt;&gt;? acceptingHandler = null;
    ///         Action? defaultAction = () =&gt; args.Result = true;
    ///         bool? handled = CWPWorkflowHelper.Execute(onAccepting, acceptingHandler, args, defaultAction);
    ///     </code>
    /// </example>
    public static bool? Execute<T> (Func<ResultEventArgs<T>, bool> onMethod,
                                    EventHandler<ResultEventArgs<T>>? eventHandler,
                                    ResultEventArgs<T> args,
                                    Action? defaultAction = null)
    {
        ArgumentNullException.ThrowIfNull (onMethod);
        ArgumentNullException.ThrowIfNull (args);

        bool handled = onMethod (args) || args.Handled;

        if (handled)
        {
            return true;
        }

        // BUGBUG: This should pass this not null; need to test
        eventHandler?.Invoke (null, args);

        if (args.Handled)
        {
            return true;
        }

        if (defaultAction is { })
        {
            defaultAction ();

            return true;
        }

        return eventHandler is null ? null : false;
    }

    /// <summary>
    ///     Executes a CWP workflow that produces a result, suitable for methods like <see cref="View.GetScheme"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the result, which may be a nullable reference type (e.g., <see cref="Scheme"/>?).</typeparam>
    /// <param name="onMethod">The virtual method invoked first, returning true to mark the workflow as handled.</param>
    /// <param name="eventHandler">The event handler to invoke, or null if no handlers are subscribed.</param>
    /// <param name="args">The event arguments containing a result and handled status.</param>
    /// <param name="defaultAction">The default action that produces the result if the workflow is not handled.</param>
    /// <param name="this">The view instance associated with the workflow, or null if not applicable.</param>
    /// <returns>The result from the event arguments or the default action.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="onMethod"/>, <paramref name="args"/>, or <paramref name="defaultAction"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if <see cref="ResultEventArgs{T}.Result"/> is null for non-nullable reference types when
    ///     <see cref="ResultEventArgs{T}.Handled"/> is true.
    /// </exception>
    /// <example>
    ///     <code>
    ///         ResultEventArgs&lt;Scheme?&gt; args = new();
    ///         Func&lt;ResultEventArgs&lt;Scheme?&gt;, bool&gt; onGettingScheme = _ =&gt; false;
    ///         EventHandler&lt;ResultEventArgs&lt;Scheme?&gt;&gt;? gettingSchemeHandler = null;
    ///         Func&lt;Scheme&gt; defaultAction = () =&gt; SchemeManager.GetScheme("Base");
    ///         Scheme scheme = CWPWorkflowHelper.ExecuteWithResult(onGettingScheme, gettingSchemeHandler, args, defaultAction);
    ///     </code>
    /// </example>
    public static TResult ExecuteWithResult<TResult> (Func<ResultEventArgs<TResult>, bool> onMethod,
                                                      EventHandler<ResultEventArgs<TResult>>? eventHandler,
                                                      ResultEventArgs<TResult> args,
                                                      Func<TResult> defaultAction,
                                                      View? @this = null)
    {
        ArgumentNullException.ThrowIfNull (onMethod);
        ArgumentNullException.ThrowIfNull (args);
        ArgumentNullException.ThrowIfNull (defaultAction);

        bool handled = onMethod (args) || args.Handled;

        if (handled)
        {
            if (args.Result is null && !typeof (TResult).IsValueType && !Nullable.GetUnderlyingType (typeof (TResult))?.IsValueType == true)
            {
                throw new InvalidOperationException ("Result cannot be null for non-nullable reference types when Handled is true.");
            }

            return args.Result!;
        }

        // BUGBUG: This should pass this not null; need to test
        eventHandler?.Invoke (@this, args);

        if (!args.Handled)
        {
            return defaultAction ();
        }

        if (args.Result is null && !typeof (TResult).IsValueType && !Nullable.GetUnderlyingType (typeof (TResult))?.IsValueType == true)
        {
            throw new InvalidOperationException ("Result cannot be null for non-nullable reference types when Handled is true.");
        }

        return args.Result!;
    }
}

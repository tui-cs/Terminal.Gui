namespace Terminal.Gui.Views;

/// <summary>
///     Wraps any <see cref="View"/> to make it runnable with a typed result without adding dialog buttons.
/// </summary>
/// <typeparam name="TView">The type of view being wrapped. Must have a parameterless constructor.</typeparam>
/// <typeparam name="TResult">
///     The type of result data returned when the session completes.
///     <para>
///         <strong>Important:</strong> Use nullable types (e.g., <c>DateTime?</c>, <c>Color?</c>)
///         so that <see langword="null"/> can indicate cancellation.
///     </para>
/// </typeparam>
/// <remarks>
///     <para>
///         Unlike <see cref="Prompt{TView, TResult}"/>, this class does not add Ok/Cancel buttons.
///         The wrapped view is responsible for setting <see cref="IRunnable{TResult}.Result"/> and
///         calling <see cref="Runnable.RequestStop"/> when the user accepts.
///     </para>
///     <para>
///         Use <see cref="ResultExtractor"/> to automatically extract the result when the view
///         raises <see cref="Command.Accept"/>.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // Wrap a DatePicker for inline use
///     RunnableWrapper&lt;DatePicker, DateTime&gt; wrapper = new ()
///     {
///         Title = "Select a Date",
///         ResultExtractor = dp =&gt; dp.Value
///     };
///     app.Run (wrapper);
///     if (wrapper.Result is { } date) Console.WriteLine (date);
///     </code>
/// </example>
public class RunnableWrapper<TView, TResult> : Runnable<TResult> where TView : View, new()
{
    private readonly TView _wrappedView;

    /// <summary>
    ///     Initializes a new instance of <see cref="RunnableWrapper{TView, TResult}"/> with a new instance of the view.
    /// </summary>
    public RunnableWrapper ()
    {
        KeyBindings.Clear ();
        MouseBindings.Clear ();
        _wrappedView = new TView ();
        Width = Dim.Fill ();
        Height = Dim.Auto ();
        Add (_wrappedView);
        CommandsToBubbleUp = [Command.Accept];
    }

    /// <summary>
    ///     Initializes a new instance of <see cref="RunnableWrapper{TView, TResult}"/> with a specified view instance.
    /// </summary>
    /// <param name="wrappedView">The view to wrap.</param>
    public RunnableWrapper (TView wrappedView)
    {
        KeyBindings.Clear ();
        MouseBindings.Clear ();
        _wrappedView = wrappedView;
        Width = Dim.Fill ();
        Height = Dim.Auto ();
        Add (_wrappedView);
        CommandsToBubbleUp = [Command.Accept];
    }

    /// <summary>
    ///     Gets the wrapped view.
    /// </summary>
    public TView GetWrappedView () => _wrappedView;

    /// <summary>
    ///     Gets or sets the function that extracts the result from the wrapped view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Called when <see cref="Command.Accept"/> is received. The return value is stored
    ///         in <see cref="IRunnable{TResult}.Result"/> and <see cref="Runnable.RequestStop"/> is called.
    ///     </para>
    ///     <para>
    ///         If <see langword="null"/> and <typeparamref name="TView"/> implements <see cref="IValue{T}"/>,
    ///         the value is extracted automatically.
    ///     </para>
    /// </remarks>
    public Func<TView, TResult?>? ResultExtractor { get; set; }

    /// <inheritdoc/>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        if (base.OnAccepting (args))
        {
            return true;
        }

        if (ResultExtractor is { })
        {
            Result = ResultExtractor (_wrappedView);
        }
        else if (_wrappedView is IValue<TResult> iValue)
        {
            Result = iValue.Value;
        }

        RequestStop ();

        return true;
    }
}

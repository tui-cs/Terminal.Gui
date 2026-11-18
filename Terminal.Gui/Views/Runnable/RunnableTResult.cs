namespace Terminal.Gui.Views;

/// <summary>
///     Base implementation of <see cref="IRunnable{TResult}"/> for views that can be run as blocking sessions.
/// </summary>
/// <typeparam name="TResult">
///     The type of result data returned when the session completes.
///     <para>
///         <strong>Important:</strong> Use nullable types (e.g., <c>Color?</c>, <c>int?</c>, <c>string?</c>)
///         so that <see langword="null"/> can indicate cancellation. Using non-nullable value types
///         (e.g., <c>Color</c>, <c>int</c>) will return their default values on cancellation, making
///         it impossible to distinguish cancellation from a valid result.
///     </para>
/// </typeparam>
/// <remarks>
///     <para>
///         Views can derive from this class or implement <see cref="IRunnable{TResult}"/> directly.
///     </para>
///     <para>
///         This class provides default implementations of the <see cref="IRunnable{TResult}"/> interface
///         following the Terminal.Gui Cancellable Work Pattern (CWP).
///     </para>
///     <para>
///         For views that don't need to return a result, use <see cref="Runnable"/> instead.
///     </para>
///     <para>
///         This class inherits from <see cref="Runnable"/> to avoid code duplication and ensure consistent behavior.
///     </para>
/// </remarks>
public class Runnable<TResult> : Runnable, IRunnable<TResult>
{
    /// <summary>
    ///     Constructs a new instance of the <see cref="Runnable{TResult}"/> class.
    /// </summary>
    public Runnable ()
    {
        // Base constructor handles common initialization
    }

    /// <inheritdoc/>
    public new TResult? Result
    {
        get => base.Result is null ? default (TResult?) : (TResult)base.Result;
        set => base.Result = value;
    }

    /// <summary>
    ///     Override to handle state changes when starting or stopping.
    ///     Called by base <see cref="Runnable.RaiseIsRunningChanging"/> before events are raised.
    /// </summary>
    /// <remarks>
    ///     The base class <see cref="Runnable.RaiseIsRunningChanging"/> already clears <c>Result</c>
    ///     to <see langword="null"/> when starting. This allows callers to detect cancellation by
    ///     checking if <c>Result</c> is <see langword="null"/> after the session ends.
    /// </remarks>
    protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    {
        // NOTE: Do NOT set Result = default here. The base class already sets base.Result = null
        // when starting (newIsRunning = true). For value types, default(T) is a valid value
        // (e.g., 0 for int), which would prevent callers from detecting cancellation.

        // Call base implementation to allow further customization
        return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
    }
}

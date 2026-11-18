using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Interface for reading console input in a perpetual loop on a dedicated input thread.
/// </summary>
/// <remarks>
///     <para>
///         Implementations run on a separate thread (started by
///         <see cref="MainLoopCoordinator{TInputRecord}.StartInputTaskAsync"/>)
///         and continuously read platform-specific input from the console, placing it into a thread-safe queue
///         for processing by <see cref="IInputProcessor"/> on the main UI thread.
///     </para>
///     <para>
///         <b>Architecture:</b>
///     </para>
///     <code>
///         Input Thread:                    Main UI Thread:
///         ┌─────────────────┐             ┌──────────────────────┐
///         │ IInput.Run()    │             │ IInputProcessor      │
///         │  ├─ Peek()      │             │  ├─ ProcessQueue()   │
///         │  ├─ Read()      │──Enqueue──→ │  ├─ Process()        │
///         │  └─ Enqueue     │             │  ├─ ToKey()          │
///         └─────────────────┘             │  └─ Raise Events     │
///                                         └──────────────────────┘
///     </code>
///     <para>
///         <b>Lifecycle:</b>
///     </para>
///     <list type="number">
///         <item><see cref="Initialize"/> - Set the shared input queue</item>
///         <item><see cref="Run"/> - Start the perpetual read loop (blocks until cancelled)</item>
///         <item>
///             Loop calls <see cref="InputImpl{TInputRecord}.Peek"/> and <see cref="InputImpl{TInputRecord}.Read"/>
///         </item>
///         <item>Cancellation via `runCancellationToken` or <see cref="ExternalCancellationTokenSource"/></item>
///     </list>
///     <para>
///         <b>Implementations:</b>
///     </para>
///     <list type="bullet">
///         <item><see cref="WindowsInput"/> - Uses Windows Console API (<c>ReadConsoleInput</c>)</item>
///         <item><see cref="NetInput"/> - Uses .NET <see cref="System.Console"/> API</item>
///         <item><see cref="AnsiInput"/> - For testing, implements <see cref="ITestableInput{TInputRecord}"/></item>
///     </list>
///     <para>
///         <b>Testing Support:</b> See <see cref="ITestableInput{TInputRecord}"/> for programmatic input injection
///         in test scenarios.
///     </para>
/// </remarks>
/// <typeparam name="TInputRecord">
///     The platform-specific input record type:
///     <list type="bullet">
///         <item><see cref="ConsoleKeyInfo"/> - for .NET and ANSI drivers</item>
///         <item><see cref="WindowsConsole.InputRecord"/> - for Windows driver</item>
///         <item><see cref="char"/> - for Unix driver</item>
///     </list>
/// </typeparam>
public interface IInput<TInputRecord> : IDisposable
{
    /// <summary>
    ///     Gets or sets an external cancellation token source that can stop the <see cref="Run"/> loop
    ///     in addition to the `runCancellationToken` passed to <see cref="Run"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property allows external code (e.g., test harnesses like <c>AppTestHelper</c>) to
    ///         provide additional cancellation signals such as timeouts or hard-stop conditions.
    ///     </para>
    ///     <para>
    ///         <b>Ownership:</b> The setter does NOT transfer ownership of the <see cref="CancellationTokenSource"/>.
    ///         The creator is responsible for disposal. <see cref="IInput{TInputRecord}"/> implementations
    ///         should NOT dispose this token source.
    ///     </para>
    ///     <para>
    ///         <b>How it works:</b> <see cref="InputImpl{TInputRecord}.Run"/> creates a linked token that
    ///         responds to BOTH the `runCancellationToken` AND this external token:
    ///     </para>
    ///     <code>
    ///         var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
    ///             runCancellationToken,
    ///             ExternalCancellationTokenSource.Token);
    ///     </code>
    /// </remarks>
    /// <example>
    ///     Test scenario with timeout:
    ///     <code>
    ///         var input = new ANSIInput();
    ///         input.ExternalCancellationTokenSource = new CancellationTokenSource(
    ///             TimeSpan.FromSeconds(30)); // 30-second timeout
    ///         
    ///         // Run will stop if either:
    ///         // 1. runCancellationToken is cancelled (normal shutdown)
    ///         // 2. 30 seconds elapse (timeout)
    ///         input.Run(normalCancellationToken);
    ///     </code>
    /// </example>
    CancellationTokenSource? ExternalCancellationTokenSource { get; set; }

    /// <summary>
    ///     Initializes the input reader with the thread-safe queue where read input will be stored.
    /// </summary>
    /// <param name="inputQueue">
    ///     The shared <see cref="ConcurrentQueue{T}"/> that both <see cref="Run"/> (producer)
    ///     and <see cref="IInputProcessor"/> (consumer) use for passing input records between threads.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This queue is created by <see cref="Terminal.Gui.App.MainLoopCoordinator{TInputRecord}"/>
    ///         and shared between the input thread and main UI thread.
    ///     </para>
    ///     <para>
    ///         <b>Must be called before <see cref="Run"/>.</b> Calling <see cref="Run"/> without
    ///         initialization will throw an exception.
    ///     </para>
    /// </remarks>
    void Initialize (ConcurrentQueue<TInputRecord> inputQueue);

    /// <summary>
    ///     Runs the input loop, continuously reading input and placing it into the queue
    ///     provided by <see cref="Initialize"/>.
    /// </summary>
    /// <param name="runCancellationToken">
    ///     The primary cancellation token that stops the input loop. Provided by
    ///     <see cref="Terminal.Gui.App.MainLoopCoordinator{TInputRecord}"/> and triggered
    ///     during application shutdown.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         <b>Threading:</b> This method runs on a dedicated input thread created by
    ///         <see cref="MainLoopCoordinator{TInputRecord}.StartInputTaskAsync"/>. and blocks until
    ///         cancellation is requested. It should never be called from the main UI thread.
    ///     </para>
    ///     <para>
    ///         <b>Cancellation:</b> The loop stops when either <paramref name="runCancellationToken"/>
    ///         or <see cref="ExternalCancellationTokenSource"/> (if set) is cancelled.
    ///     </para>
    ///     <para>
    ///         <b>Base Implementation:</b> <see cref="InputImpl{TInputRecord}.Run"/> provides the
    ///         standard loop logic:
    ///     </para>
    ///     <code>
    ///         while (!cancelled)
    ///         {
    ///             while (Peek())  // Check for available input
    ///             {
    ///                 foreach (var input in Read())  // Read all available
    ///                 {
    ///                     inputQueue.Enqueue(input);  // Store for processing
    ///                 }
    ///             }
    ///             Task.Delay(20ms);  // Throttle to ~50 polls/second
    ///         }
    ///     </code>
    ///     <para>
    ///         <b>Testing:</b> For <see cref="ITestableInput{TInputRecord}"/> implementations,
    ///         test input injected via <see cref="ITestableInput{TInputRecord}.InjectInput"/>
    ///         flows through the same <c>Peek/Read</c> pipeline.
    ///     </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    ///     Thrown when <paramref name="runCancellationToken"/> or <see cref="ExternalCancellationTokenSource"/>
    ///     is cancelled. This is the normal/expected means of exiting the input loop.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if <see cref="Initialize"/> was not called before <see cref="Run"/>.
    /// </exception>
    void Run (CancellationToken runCancellationToken);
}

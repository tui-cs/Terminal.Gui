using System.ComponentModel;

namespace Terminal.Gui.Drivers;

/// <summary>
///     INTERNAL: Processes raw mouse events from drivers and generates synthetic click events (single, double, triple
///     clicks)
///     based on button state tracking and timing analysis.
/// </summary>
/// <remarks>
///     <para>
///         This class acts as a middleware between the driver layer and the application layer, transforming low-level
///         pressed/released events into higher-level click events. It maintains state for all four mouse buttons
///         independently using <see cref="MouseButtonClickTracker"/> instances.
///     </para>
///     <para>
///         For each incoming <see cref="Mouse"/>, the interpreter:
///     </para>
///     <list type="number">
///         <item>
///             <description>Yields the original event unchanged (for low-level handling)</description>
///         </item>
///         <item>
///             <description>Updates the state of all four button trackers</description>
///         </item>
///         <item>
///             <description>Immediately emits click events (single, double, triple) when detected</description>
///         </item>
///     </list>
///     <para>
///         Click detection follows standard UI conventions: clicks are counted on button **release**, not press,
///         and consecutive clicks must occur within <see cref="RepeatedClickThreshold"/> milliseconds at the same
///         position to be counted as multi-clicks. ALL clicks are emitted immediately, providing instant user feedback
///         while still correctly detecting multi-click sequences.
///     </para>
///     <para>
///         <strong>Design Philosophy:</strong> Unlike previous implementations that deferred single clicks to detect
///         double-clicks (causing 500ms delay), this implementation emits all clicks immediately. Applications that need
///         to distinguish between single and double-click intentions can track timing themselves (see ListView example
///         in mouse.md).
///     </para>
/// </remarks>
internal class MouseInterpreter
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MouseInterpreter"/> class.
    /// </summary>
    /// <param name="timeProvider">
    ///     Optional time provider for getting the current time. If <see langword="null"/>, defaults to
    ///     <see cref="SystemTimeProvider"/>.
    ///     Useful for unit tests to inject controlled time values via <see cref="VirtualTimeProvider"/>.
    /// </param>
    /// <param name="doubleClickThreshold">
    ///     Optional threshold for multi-click detection. If <see langword="null"/>, defaults to 500 milliseconds.
    ///     This value determines how quickly consecutive clicks must occur to be counted together.
    /// </param>
    /// <remarks>
    ///     Creates four <see cref="MouseButtonClickTracker"/> instances, one for each supported mouse button
    ///     (LeftButton/Left, MiddleButton/Middle, RightButton/Right, Button4), all using the same time provider and threshold.
    /// </remarks>
    public MouseInterpreter (ITimeProvider? timeProvider = null, TimeSpan? doubleClickThreshold = null)
    {
        TimeProvider = timeProvider ?? new SystemTimeProvider ();
        RepeatedClickThreshold = doubleClickThreshold ?? TimeSpan.FromMilliseconds (500);

        _mouseButtonClickTracker =
        [
            new MouseButtonClickTracker (TimeProvider, RepeatedClickThreshold, 0),
            new MouseButtonClickTracker (TimeProvider, RepeatedClickThreshold, 1),
            new MouseButtonClickTracker (TimeProvider, RepeatedClickThreshold, 2),
            new MouseButtonClickTracker (TimeProvider, RepeatedClickThreshold, 3)
        ];
    }

    /// <summary>
    ///     Gets or sets the time provider for returning the current time.
    /// </summary>
    /// <value>An <see cref="ITimeProvider"/> instance that provides the current time.</value>
    /// <remarks>
    ///     This property enables time injection for unit tests, ensuring repeatable and deterministic test behavior.
    ///     In production, this defaults to <see cref="SystemTimeProvider"/> which uses <c>DateTime.Now</c>.
    ///     For testing, use <see cref="VirtualTimeProvider"/> to control time explicitly.
    /// </remarks>
    public ITimeProvider TimeProvider { get; set; }

    /// <summary>
    ///     Gets or sets the maximum time allowed between consecutive clicks for them to be counted as a multi-click
    ///     (double-click, triple-click).
    /// </summary>
    /// <value>
    ///     A <see cref="TimeSpan"/> representing the click threshold. Defaults to 500 milliseconds.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         If the time between button releases exceeds this threshold, the click counter resets to 1.
    ///         This value applies to all mouse buttons tracked by this interpreter.
    ///     </para>
    ///     <para>
    ///         Standard double-click thresholds typically range from 300-500ms depending on the operating system.
    ///     </para>
    /// </remarks>
    public TimeSpan RepeatedClickThreshold { get; set; }

    private readonly MouseButtonClickTracker [] _mouseButtonClickTracker;

    /// <summary>
    ///     Processes a raw mouse event and generates both the original event and any synthetic click events.
    /// </summary>
    /// <param name="mouse">The mouse event to process, typically from a driver layer.</param>
    /// <returns>
    ///     An enumerable sequence of <see cref="Mouse"/>:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>The original input event (always yielded first)</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Synthesized click events (LeftButtonClicked, LeftButtonDoubleClicked, etc.) immediately when
    ///                 button released
    ///             </description>
    ///         </item>
    ///     </list>
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method uses a generator pattern (yield return) to produce events from input.
    ///         The original event is always yielded first to allow low-level handling. Click events
    ///         are yielded immediately when a button is released.
    ///     </para>
    ///     <para>
    ///         <strong>New Behavior:</strong> Clicks are emitted immediately (no deferral). This provides
    ///         instant user feedback for single clicks while still correctly detecting multi-clicks.
    ///     </para>
    ///     <para>
    ///         Example sequence for a double click:
    ///     </para>
    ///     <code>
    ///         Input: LeftButtonPressed  → Yields: LeftButtonPressed
    ///         Input: LeftButtonReleased → Yields: LeftButtonReleased, LeftButtonClicked (immediate!)
    ///         Input: LeftButtonPressed  → Yields: LeftButtonPressed
    ///         Input: LeftButtonReleased → Yields: LeftButtonReleased, LeftButtonDoubleClicked (immediate!)
    ///     </code>
    ///     <para>
    ///         Applications receive both LeftButtonClicked and LeftButtonDoubleClicked events. Applications
    ///         that need to distinguish single vs double-click can track timing (see ListView in mouse.md).
    ///     </para>
    /// </remarks>
    public IEnumerable<Mouse> Process (Mouse mouse)
    {
        yield return mouse;

        // For each mouse button
        for (var i = 0; i < 4; i++)
        {
            _mouseButtonClickTracker [i].UpdateState (mouse, out int? numClicks);

            if (numClicks.HasValue)
            {
                yield return CreateClickEvent (i, numClicks.Value, mouse);
            }
        }
    }

    /// <summary>
    ///     Checks all button trackers for expired pending clicks and returns them as synthetic click events.
    /// </summary>
    /// <returns>
    ///     An empty enumerable - clicks are now emitted immediately via <see cref="Process"/>, not deferred.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         <strong>DEPRECATED:</strong> This method is kept for backwards compatibility but no longer emits events.
    ///         Clicks are now emitted immediately in <see cref="Process"/> instead of being deferred.
    ///     </para>
    ///     <para>
    ///         In the previous implementation, this method was called periodically to retrieve deferred single-click
    ///         events after the double-click threshold expired. This caused a 500ms delay for all single clicks,
    ///         which was unacceptable UX.
    ///     </para>
    /// </remarks>
    public IEnumerable<Mouse> CheckForExpiredClicks ()
    {
        // Clicks are now emitted immediately via Process() - nothing to do here
        yield break;
    }

    /// <summary>
    ///     Creates a synthetic click event based on button index and click count.
    /// </summary>
    /// <param name="button">
    ///     The zero-based button index (0=LeftButton/Left, 1=MiddleButton/Middle, 2=RightButton/Right,
    ///     3=Button4).
    /// </param>
    /// <param name="numberOfClicks">The number of consecutive clicks detected (1=single, 2=double, 3+=triple).</param>
    /// <param name="mouseEventArgs">The original mouse event to copy screen position and view information from.</param>
    /// <returns>
    ///     A new <see cref="Mouse"/> with the appropriate click flag (LeftButtonClicked, LeftButtonDoubleClicked,
    ///     LeftButtonTripleClicked, etc.) and screen position/view copied from the input event.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The returned event has <see cref="HandledEventArgs.Handled"/> set to <see langword="false"/> to allow
    ///         propagation through the event system. Logs a trace message when raising the click event.
    ///     </para>
    /// </remarks>
    private Mouse CreateClickEvent (int button, int numberOfClicks, Mouse mouseEventArgs)
    {
        var newClick = new Mouse
        {
            Timestamp = mouseEventArgs.Timestamp ?? TimeProvider.Now,
            Handled = false,
            Flags = ToClicks (button, numberOfClicks) | (mouseEventArgs.Flags & (MouseFlags.Shift | MouseFlags.Ctrl | MouseFlags.Alt)),
            ScreenPosition = mouseEventArgs.ScreenPosition

            // View is intentionally NOT copied - it's View-relative and set by ApplicationMouse/View.Mouse
            // Position is intentionally NOT copied - it's View-relative and set by ApplicationMouse/View.Mouse
        };

        //Logging.Trace ($"Raising click event:{newClick.Flags} at screen {newClick.ScreenPosition}");

        return newClick;
    }

    /// <summary>
    ///     Converts a button index and click count into the appropriate <see cref="MouseFlags"/> click flag.
    /// </summary>
    /// <param name="buttonIdx">
    ///     The zero-based button index (0=LeftButton/Left, 1=MiddleButton/Middle, 2=RightButton/Right,
    ///     3=Button4).
    /// </param>
    /// <param name="numberOfClicks">
    ///     The number of consecutive clicks detected:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>1 - Single click (Button*Clicked)</description>
    ///         </item>
    ///         <item>
    ///             <description>2 - Double click (Button*DoubleClicked)</description>
    ///         </item>
    ///         <item>
    ///             <description>3+ - Triple click (Button*TripleClicked)</description>
    ///         </item>
    ///     </list>
    /// </param>
    /// <returns>The corresponding <see cref="MouseFlags"/> value for the button and click count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="numberOfClicks"/> is zero or <paramref name="buttonIdx"/> is not 0-3.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This method maps button indices and click counts to specific <see cref="MouseFlags"/> values.
    ///         Four or more consecutive clicks are reported as triple-clicks.
    ///     </para>
    ///     <para>
    ///         Button index mapping:
    ///     </para>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Index</term>
    ///             <description>Button / Flags</description>
    ///         </listheader>
    ///         <item>
    ///             <term>0</term>
    ///             <description>LeftButton (Left) → LeftButtonClicked, LeftButtonDoubleClicked, LeftButtonTripleClicked</description>
    ///         </item>
    ///         <item>
    ///             <term>1</term>
    ///             <description>
    ///                 MiddleButton (Middle) → MiddleButtonClicked, MiddleButtonDoubleClicked,
    ///                 MiddleButtonTripleClicked
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>2</term>
    ///             <description>RightButton (Right) → RightButtonClicked, RightButtonDoubleClicked, RightButtonTripleClicked</description>
    ///         </item>
    ///         <item>
    ///             <term>3</term>
    ///             <description>Button4 → Button4Clicked, Button4DoubleClicked, Button4TripleClicked</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private MouseFlags ToClicks (int buttonIdx, int numberOfClicks)
    {
        if (numberOfClicks == 0)
        {
            throw new ArgumentOutOfRangeException (nameof (numberOfClicks), @"Zero clicks are not valid.");
        }

        return buttonIdx switch
        {
            0 => numberOfClicks switch
            {
                1 => MouseFlags.LeftButtonClicked,
                2 => MouseFlags.LeftButtonDoubleClicked,
                _ => MouseFlags.LeftButtonTripleClicked
            },
            1 => numberOfClicks switch
            {
                1 => MouseFlags.MiddleButtonClicked,
                2 => MouseFlags.MiddleButtonDoubleClicked,
                _ => MouseFlags.MiddleButtonTripleClicked
            },
            2 => numberOfClicks switch
            {
                1 => MouseFlags.RightButtonClicked,
                2 => MouseFlags.RightButtonDoubleClicked,
                _ => MouseFlags.RightButtonTripleClicked
            },
            3 => numberOfClicks switch
            {
                1 => MouseFlags.Button4Clicked,
                2 => MouseFlags.Button4DoubleClicked,
                _ => MouseFlags.Button4TripleClicked
            },
            _ => throw new ArgumentOutOfRangeException (nameof (buttonIdx), @"Unsupported button index")
        };
    }
}

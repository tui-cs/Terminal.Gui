namespace Terminal.Gui.Drivers;

/// <summary>
///     INTERNAL: Tracks the state of a single mouse button to detect multi-click patterns (single, double, triple clicks).
///     Manages button press/release state, timing, and position tracking to determine when consecutive clicks should be
///     counted together.
/// </summary>
/// <remarks>
///     <para>
///         This class is used by <see cref="MouseInterpreter"/> to implement click detection for each mouse button
///         independently. It uses event timestamps and position matching to determine whether consecutive button
///         presses should be counted as multi-clicks (e.g., double-click, triple-click).
///     </para>
///     <para>
///         Click detection uses an **immediate emission** approach: when a button is released, the click event
///         is emitted immediately (count=1 for first release, count=2 for second release within threshold, etc.).
///         This provides instant user feedback for single clicks while still detecting multi-clicks correctly.
///         Applications that need to distinguish between single and double-click intentions can track timing
///         themselves (see ListView example in mouse.md).
///     </para>
///     <para>
///         Not to be confused with <c>NetEvents.MouseButtonState</c>.
///     </para>
/// </remarks>
/// <param name="_timeProvider">Time provider for getting the current time, allowing for time injection in tests.</param>
/// <param name="_repeatClickThreshold">Maximum time between clicks to count as consecutive (e.g., double-click timeout).</param>
/// <param name="_buttonIdx">
///     Zero-based index of the button being tracked (0=LeftButton/Left, 1=MiddleButton/Middle,
///     2=RightButton/Right, 3=Button4).
/// </param>

// ReSharper disable InconsistentNaming
internal class MouseButtonClickTracker (ITimeProvider _timeProvider, TimeSpan _repeatClickThreshold, int _buttonIdx)
{
    // ReSharper enable InconsistentNaming
    private int _consecutiveClicks;
    private Point _lastPosition;


    /// <summary>
    ///     Gets or sets the timestamp when the button last changed state (pressed or released).
    /// </summary>
    /// <value>The <see cref="DateTime"/> when the button entered its current state.</value>
    public DateTime At { get; set; }

    /// <summary>
    ///     Gets or sets whether the button is currently in a pressed state.
    /// </summary>
    /// <value><see langword="true"/> if the button is currently down; <see langword="false"/> if released.</value>
    public bool Pressed { get; set; }

    /// <summary>
    ///     Updates the button state based on a new mouse event and determines if a multi-click occurred.
    /// </summary>
    /// <param name="mouse">The mouse event arguments containing button flags, position, and timestamp.</param>
    /// <param name="numClicks">
    ///     Output parameter indicating the number of consecutive clicks detected. Returns:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see langword="null"/> - No click event (button state unchanged)</description>
    ///         </item>
    ///         <item>
    ///             <description>1 - Single click (first release, emitted immediately)</description>
    ///         </item>
    ///         <item>
    ///             <description>2 - Double click (second consecutive release within threshold)</description>
    ///         </item>
    ///         <item>
    ///             <description>3 - Triple click (third consecutive release within threshold)</description>
    ///         </item>
    ///         <item>
    ///             <description>4+ - Additional consecutive clicks</description>
    ///         </item>
    ///     </list>
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method implements timestamp-based multi-click detection with immediate single-click emission:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>
    ///                 Check if time since last state change exceeds threshold or position changed.
    ///                 If so, reset consecutive click counter.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If button state hasn't changed (still pressed or still released), return null.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 On button press: cancel any pending click state (start of potential multi-click sequence).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 On button release: emit click immediately (single click on first release, multi-click on subsequent).
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <strong>Design:</strong> Single clicks are emitted immediately for instant user feedback. Multi-clicks
    ///         (double, triple) are also emitted immediately when detected. Applications that need to distinguish
    ///         between single and double-click can track timing themselves (see ListView example in mouse.md).
    ///     </para>
    /// </remarks>
    public void UpdateState (Mouse mouse, out int? numClicks)
    {
        bool isPressedNow = IsPressed (_buttonIdx, mouse.Flags);
        bool isSamePosition = _lastPosition == mouse.ScreenPosition;

        // Use mouse.Timestamp if available, otherwise use _timeProvider.Now for current time
        DateTime currentTime = mouse.Timestamp ?? _timeProvider.Now;
        TimeSpan elapsed = currentTime - At;

        numClicks = null; // Default to no click

        // Check if threshold exceeded or position changed
        if (elapsed >= _repeatClickThreshold || !isSamePosition)
        {
            // Reset consecutive click counter
            _consecutiveClicks = 0;
        }

        // Check if button state changed
        if (isPressedNow == Pressed)
        {
            // No state change - do nothing
            return;
        }

        // State changed - update tracking
        if (Pressed)
        {
            // Button was pressed, now released - this is a click!
            ++_consecutiveClicks;

            // EMIT CLICKS IMMEDIATELY - no deferral
            // Applications handle timing if they need to distinguish single vs double-click
            numClicks = _consecutiveClicks;
        }

        // Record new state
        OverwriteState (mouse);
    }

    /// <summary>
    ///     Checks if there's a pending click that has exceeded the threshold and should be yielded.
    /// </summary>
    /// <param name="numClicks">
    ///     Output parameter - always returns <see langword="null"/> in the current implementation
    ///     (clicks are emitted immediately, not deferred).
    /// </param>
    /// <param name="position">
    ///     Output parameter - returns <see cref="Point.Empty"/> (no expired clicks to emit).
    /// </param>
    /// <returns>
    ///     Always returns <see langword="false"/> in the current implementation (clicks are emitted immediately).
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         <strong>DEPRECATED:</strong> This method is kept for backwards compatibility but is no longer used.
    ///         Clicks are now emitted immediately via <see cref="UpdateState"/> instead of being deferred.
    ///     </para>
    ///     <para>
    ///         In the previous implementation, single-clicks were deferred to allow double-click detection.
    ///         This caused a 500ms delay for all single clicks, which was unacceptable UX. The new implementation
    ///         emits clicks immediately while still correctly detecting multi-clicks.
    ///     </para>
    /// </remarks>
    public bool CheckForExpiredClicks (out int? numClicks, out Point position)
    {
        // Clicks are now emitted immediately - no deferred clicks to check
        numClicks = null;
        position = Point.Empty;

        return false;
    }

    /// <summary>
    ///     Overwrites the current state with values from a new mouse event.
    /// </summary>
    /// <param name="e">The mouse event containing the new state to record.</param>
    /// <remarks>
    ///     Updates <see cref="Pressed"/>, <see cref="At"/>, and the last known position to match the current event.
    ///     Uses the mouse event's <see cref="Mouse.Timestamp"/> when available, otherwise falls back to the now function.
    /// </remarks>
    private void OverwriteState (Mouse e)
    {
        Pressed = IsPressed (_buttonIdx, e.Flags);
        At = e.Timestamp ?? _timeProvider.Now;
        _lastPosition = e.ScreenPosition;
    }

    /// <summary>
    ///     Determines if a specific button is pressed based on the mouse flags.
    /// </summary>
    /// <param name="btn">
    ///     The zero-based button index (0=LeftButton/Left, 1=MiddleButton/Middle, 2=RightButton/Right,
    ///     3=Button4).
    /// </param>
    /// <param name="eFlags">The mouse flags to check for the pressed state.</param>
    /// <returns><see langword="true"/> if the specified button is pressed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="btn"/> is not 0-3.</exception>
    /// <remarks>
    ///     Maps button indices to their corresponding <see cref="MouseFlags"/> pressed flags:
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Index</term>
    ///             <description>Button / Flag</description>
    ///         </listheader>
    ///         <item>
    ///             <term>0</term>
    ///             <description>LeftButton (Left) / <see cref="MouseFlags.LeftButtonPressed"/></description>
    ///         </item>
    ///         <item>
    ///             <term>1</term>
    ///             <description>MiddleButton (Middle) / <see cref="MouseFlags.MiddleButtonPressed"/></description>
    ///         </item>
    ///         <item>
    ///             <term>2</term>
    ///             <description>RightButton (Right) / <see cref="MouseFlags.RightButtonPressed"/></description>
    ///         </item>
    ///         <item>
    ///             <term>3</term>
    ///             <description>Button4 / <see cref="MouseFlags.Button4Pressed"/></description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private bool IsPressed (int btn, MouseFlags eFlags) =>
        btn switch
        {
            0 => eFlags.FastHasFlags (MouseFlags.LeftButtonPressed),
            1 => eFlags.FastHasFlags (MouseFlags.MiddleButtonPressed),
            2 => eFlags.FastHasFlags (MouseFlags.RightButtonPressed),
            3 => eFlags.FastHasFlags (MouseFlags.Button4Pressed),
            _ => throw new ArgumentOutOfRangeException (nameof (btn))
        };
}

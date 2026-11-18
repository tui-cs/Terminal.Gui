namespace Terminal.Gui.App;

/// <summary>
///     Defines a contract for tracking which <see cref="View"/> (if any) has 'grabbed' the mouse,
///     giving it exclusive priority for mouse events such as movement, button presses, and release.
/// </summary>
/// <remarks>
///     <para>
///         <strong>When to Use Mouse Grab:</strong>
///     </para>
///     <para>
///         Mouse grab is essential for scenarios where a view must receive all mouse events during an operation,
///         even when the mouse moves outside the view's bounds. Common use cases include:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Dragging operations (e.g., moving windows, resizing borders, dragging scrollbar thumbs)</description>
///         </item>
///         <item>
///             <description>Button hold-to-repeat behavior (e.g., scrollbar arrows, spin buttons)</description>
///         </item>
///         <item>
///             <description>Selection operations (e.g., drag-selecting text or items)</description>
///         </item>
///         <item>
///             <description>Custom drag-and-drop interactions</description>
///         </item>
///     </list>
///     <para>
///         <strong>Grab Lifecycle:</strong>
///     </para>
///     <list type="number">
///         <item>
///             <description>
///                 <strong>Press:</strong> View calls <see cref="GrabMouse"/> (typically on <c>Button1Pressed</c>).
///                 The <see cref="GrabbingMouse"/> event fires first (can be cancelled), then <see cref="GrabbedMouse"/>.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>During Grab:</strong> All mouse events (movement, additional presses) are routed exclusively
///                 to the grabbed view with coordinates converted to the view's viewport. <see cref="HandleMouseGrab"/>
///                 intercepts and redirects events.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Release:</strong> View calls <see cref="UngrabMouse"/> (typically on <c>Button1Released</c> or
///                 <c>Button1Clicked</c>). The <see cref="UnGrabbingMouse"/> event fires first (can be cancelled),
///                 then <see cref="UnGrabbedMouse"/>. Normal mouse routing resumes.
///             </description>
///         </item>
///     </list>
///     <para>
///         <strong>Auto-Grab Feature:</strong>
///     </para>
///     <para>
///         Views with <see cref="View.MouseHighlightStates"/> or <see cref="View.MouseHoldRepeat"/> enabled
///         automatically grab the mouse on button press. See <see cref="View.NewMouseEvent"/> for details.
///     </para>
///     <para>
///         <strong>Implementation Notes:</strong>
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 The grabbed view is stored using a <see cref="WeakReference{T}"/>, so disposed views
///                 are automatically released without causing memory leaks.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Only one view can hold the mouse grab at a time. Calling <see cref="GrabMouse"/> with
///                 a new view implicitly releases the previous grab.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Grab events (<see cref="GrabbingMouse"/>, <see cref="UnGrabbingMouse"/>) follow the
///                 Cancellable Work Pattern (CWP) - handlers can set <c>Cancel = true</c> to prevent the operation.
///             </description>
///         </item>
///     </list>
///     <para>
///         <strong>Example Usage (Manual Grab):</strong>
///     </para>
///     <code>
///     protected override bool OnMouseEvent (Mouse mouse)
///     {
///         if (mouse.Flags.HasFlag (MouseFlags.Button1Pressed))
///         {
///             App?.Mouse.GrabMouse (this);
///             _isDragging = true;
///             return true;
///         }
/// 
///         if (_isDragging &amp;&amp; mouse.Flags.HasFlag (MouseFlags.Button1Released))
///         {
///             App?.Mouse.UngrabMouse ();
///             _isDragging = false;
///             return true;
///         }
/// 
///         if (_isDragging)
///         {
///             // Handle drag - mouse.Position is relative to this view's viewport
///             UpdateDragPosition (mouse.Position);
///             return true;
///         }
/// 
///         return false;
///     }
///     </code>
/// </remarks>
/// <seealso cref="View.MouseHighlightStates"/>
/// <seealso cref="View.MouseHoldRepeat"/>
/// <seealso cref="View.NewMouseEvent"/>
/// <seealso cref="IMouse"/>
public interface IMouseGrabHandler
{
    /// <summary>
    ///     Raised after a view has successfully grabbed the mouse.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event fires after <see cref="GrabbingMouse"/> (if not cancelled) and indicates that the specified view
    ///         now has exclusive mouse event routing. All subsequent mouse events will be directed to this view until
    ///         <see cref="UngrabMouse"/> is called.
    ///     </para>
    ///     <para>
    ///         The <see cref="ViewEventArgs.View"/> property contains the view that grabbed the mouse.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GrabbingMouse"/>
    /// <seealso cref="GrabMouse"/>
    public event EventHandler<ViewEventArgs>? GrabbedMouse;

    /// <summary>
    ///     Raised when a view requests to grab the mouse; can be cancelled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event fires before the grab is established. Handlers can set <see cref="GrabMouseEventArgs.Cancel"/>
    ///         to <see langword="true"/> to prevent the grab from occurring.
    ///     </para>
    ///     <para>
    ///         Use this event to implement grab arbitration logic, such as preventing one view from stealing
    ///         the mouse grab from another view during an active drag operation.
    ///     </para>
    ///     <para>
    ///         The <see cref="GrabMouseEventArgs.View"/> property contains the view attempting to grab the mouse.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     // Prevent other views from grabbing during an active drag
    ///     App.Mouse.GrabbingMouse += (sender, e) =&gt;
    ///     {
    ///         if (_isDragging &amp;&amp; !ReferenceEquals(e.View, this))
    ///         {
    ///             e.Cancel = true;
    ///         }
    ///     };
    ///     </code>
    /// </example>
    /// <seealso cref="GrabbedMouse"/>
    /// <seealso cref="GrabMouse"/>
    public event EventHandler<GrabMouseEventArgs>? GrabbingMouse;

    /// <summary>
    ///     Grabs the mouse, forcing all mouse events to be routed exclusively to the specified view until
    ///     <see cref="UngrabMouse"/> is called.
    /// </summary>
    /// <param name="view">
    ///     The <see cref="View"/> that will receive all mouse events until <see cref="UngrabMouse"/> is invoked.
    ///     If <see langword="null"/>, equivalent to calling <see cref="UngrabMouse"/>.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         <strong>Event Sequence:</strong> <see cref="GrabbingMouse"/> → (if not cancelled) → <see cref="GrabbedMouse"/>
    ///     </para>
    ///     <para>
    ///         <strong>Behavior:</strong>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>If <paramref name="view"/> is <see langword="null"/>, the current grab is released.</description>
    ///         </item>
    ///         <item>
    ///             <description>If <see cref="GrabbingMouse"/> is cancelled, the grab does not occur.</description>
    ///         </item>
    ///         <item>
    ///             <description>While grabbed, all mouse events have coordinates converted to the view's viewport.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The view is stored via <see cref="WeakReference{T}"/>, preventing memory leaks if the view is
    ///                 disposed.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <strong>Typical Call Sites:</strong> In <see cref="View.OnMouseEvent"/> when handling
    ///         <see cref="MouseFlags.LeftButtonPressed"/> (or other button pressed events) to begin a drag operation.
    ///     </para>
    /// </remarks>
    /// <seealso cref="UngrabMouse"/>
    /// <seealso cref="IsGrabbed(View)"/>
    /// <seealso cref="IsGrabbed()"/>
    /// <seealso cref="GrabbingMouse"/>
    /// <seealso cref="GrabbedMouse"/>
    public void GrabMouse (View? view);

    /// <summary>
    ///     Determines whether the specified view currently has the mouse grabbed.
    /// </summary>
    /// <param name="view">The view to check. If <see langword="null"/>, returns <see langword="false"/>.</param>
    /// <returns>
    ///     <see langword="true"/> if the specified view currently has the mouse grabbed;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method uses reference equality (<see cref="object.ReferenceEquals"/>) to check if the given view
    ///         is the one that currently has exclusive mouse event routing.
    ///     </para>
    ///     <para>
    ///         <strong>Automatic Cleanup:</strong> If the grabbed view has been disposed or garbage collected,
    ///         this method returns <see langword="false"/> because the internal <see cref="WeakReference{T}"/> will
    ///         no longer resolve to a live object.
    ///     </para>
    ///     <para>
    ///         <strong>Use Cases:</strong>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Check if this view owns the grab before processing mouse events.</description>
    ///         </item>
    ///         <item>
    ///             <description>Implement grab arbitration in <see cref="GrabbingMouse"/> handlers.</description>
    ///         </item>
    ///         <item>
    ///             <description>Conditional logic based on whether the view is actively dragging.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     if (App.Mouse.IsGrabbed(this))
    ///     {
    ///         // Process drag event
    ///         UpdatePosition(mouse.Position);
    ///     }
    ///     </code>
    /// </example>
    /// <seealso cref="GrabMouse"/>
    /// <seealso cref="UngrabMouse"/>
    public bool IsGrabbed (View? view);

    /// <summary>
    ///     Determines whether any view currently has the mouse grabbed.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> if any view currently has the mouse grabbed;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Use this method to check if the mouse grab is active without knowing which specific view holds it.
    ///     </para>
    ///     <para>
    ///         <strong>Automatic Cleanup:</strong> If the grabbed view has been disposed or garbage collected,
    ///         this method returns <see langword="false"/> because the internal <see cref="WeakReference{T}"/> will
    ///         no longer resolve to a live object.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     if (!App.Mouse.IsGrabbed())
    ///     {
    ///         // No view has the mouse grabbed - safe to proceed
    ///         App.Mouse.GrabMouse(this);
    ///     }
    ///     </code>
    /// </example>
    /// <seealso cref="IsGrabbed(View?)"/>
    /// <seealso cref="GrabMouse"/>
    /// <seealso cref="UngrabMouse"/>
    public bool IsGrabbed ();

    /// <summary>
    ///     Raised after a view has released the mouse grab.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event fires after <see cref="UnGrabbingMouse"/> (if not cancelled) and indicates that normal
    ///         mouse routing has resumed. Mouse events will now be delivered to the view under the mouse pointer.
    ///     </para>
    ///     <para>
    ///         The <see cref="ViewEventArgs.View"/> property contains the view that released the mouse grab.
    ///     </para>
    ///     <para>
    ///         <strong>Post-Ungrab Behavior:</strong> After this event, the system immediately updates
    ///         <see cref="View.MouseEnter"/>/<see cref="View.MouseLeave"/> states for views under the current
    ///         mouse position, ensuring proper visual feedback.
    ///     </para>
    /// </remarks>
    /// <seealso cref="UnGrabbingMouse"/>
    /// <seealso cref="UngrabMouse"/>
    public event EventHandler<ViewEventArgs>? UnGrabbedMouse;

    /// <summary>
    ///     Raised when a view requests to release the mouse grab; can be cancelled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event fires before the grab is released. Handlers can set <see cref="GrabMouseEventArgs.Cancel"/>
    ///         to <see langword="true"/> to prevent the ungrab from occurring.
    ///     </para>
    ///     <para>
    ///         <strong>Use Cases for Cancellation:</strong>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Preventing ungrab during a critical operation that must complete.</description>
    ///         </item>
    ///         <item>
    ///             <description>Implementing confirmation dialogs before releasing a drag.</description>
    ///         </item>
    ///         <item>
    ///             <description>Enforcing constraints on when a grab can be released.</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         The <see cref="GrabMouseEventArgs.View"/> property contains the view attempting to release the grab.
    ///     </para>
    /// </remarks>
    /// <seealso cref="UnGrabbedMouse"/>
    /// <seealso cref="UngrabMouse"/>
    public event EventHandler<GrabMouseEventArgs>? UnGrabbingMouse;

    /// <summary>
    ///     Releases the mouse grab, restoring normal mouse event routing to the view under the mouse pointer.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <strong>Event Sequence:</strong> <see cref="UnGrabbingMouse"/> → (if not cancelled) →
    ///         <see cref="UnGrabbedMouse"/>
    ///     </para>
    ///     <para>
    ///         <strong>Behavior:</strong>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>If no view currently has the grab, this method does nothing.</description>
    ///         </item>
    ///         <item>
    ///             <description>If <see cref="UnGrabbingMouse"/> is cancelled, the grab remains active.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 After ungrab, <see cref="View.MouseEnter"/>/<see cref="View.MouseLeave"/> events are updated
    ///                 for views under the mouse.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <strong>Typical Call Sites:</strong> In <see cref="View.OnMouseEvent"/> when handling
    ///         <see cref="MouseFlags.LeftButtonReleased"/> or click events to end a drag operation.
    ///     </para>
    ///     <para>
    ///         <strong>Auto-Ungrab:</strong> For views with <see cref="View.MouseHighlightStates"/> or
    ///         <see cref="View.MouseHoldRepeat"/> enabled, ungrab is called automatically on click events.
    ///         See <see cref="View.NewMouseEvent"/> for details.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GrabMouse"/>
    /// <seealso cref="IsGrabbed(View)"/>
    /// <seealso cref="IsGrabbed()"/>
    /// <seealso cref="UnGrabbingMouse"/>
    /// <seealso cref="UnGrabbedMouse"/>
    public void UngrabMouse ();

    /// <summary>
    ///     INTERNAL: Handles mouse grab logic for a mouse event, routing events to the grabbed view if one exists.
    /// </summary>
    /// <param name="deepestViewUnderMouse">
    ///     The deepest view under the mouse according to normal hit-testing. This parameter is not used when
    ///     a view has the mouse grabbed, but is included for context.
    /// </param>
    /// <param name="mouse">
    ///     The mouse event to handle. If a view has the grab, coordinates are converted to the grabbed view's
    ///     viewport and the event is delivered to that view via <see cref="View.NewMouseEvent"/>.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if a view had the mouse grabbed and the event was routed to it (regardless of
    ///     whether the grabbed view marked it as handled); <see langword="false"/> if no view has the grab and
    ///     normal mouse routing should proceed.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is called by <see cref="IMouse.RaiseMouseEvent"/> early in the mouse event processing
    ///         pipeline. When a view has grabbed the mouse:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>The event coordinates are converted to the grabbed view's viewport coordinates.</description>
    ///         </item>
    ///         <item>
    ///             <description>The event is delivered directly to the grabbed view via <see cref="View.NewMouseEvent"/>.</description>
    ///         </item>
    ///         <item>
    ///             <description><see langword="true"/> is returned to prevent further event propagation.</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         This ensures that during drag operations, only the grabbing view receives mouse events,
    ///         preventing other views from interfering with the drag (e.g., receiving Enter/Leave events).
    ///     </para>
    /// </remarks>
    bool HandleMouseGrab (View? deepestViewUnderMouse, Mouse mouse);
}

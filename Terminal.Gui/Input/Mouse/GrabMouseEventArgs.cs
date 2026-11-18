namespace Terminal.Gui.Input;

/// <summary>
///     Provides data for mouse grab-related events (<see cref="IMouseGrabHandler.GrabbingMouse"/> and
///     <see cref="IMouseGrabHandler.UnGrabbingMouse"/>).
/// </summary>
/// <remarks>
///     <para>
///         This class is used with the Cancellable Work Pattern (CWP). Handlers can set <see cref="Cancel"/> to
///         <see langword="true"/> to prevent the grab or ungrab operation from proceeding.
///     </para>
///     <para>
///         <strong>Use Cases for Cancellation:</strong>
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <strong>Prevent Grab Theft:</strong> Cancel <see cref="IMouseGrabHandler.GrabbingMouse"/> when another
///                 view tries to grab the mouse during an active drag operation.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Prevent Premature Release:</strong> Cancel <see cref="IMouseGrabHandler.UnGrabbingMouse"/> when
///                 a drag operation must complete before the grab can be released.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="IMouseGrabHandler"/>
/// <seealso cref="IMouseGrabHandler.GrabbingMouse"/>
/// <seealso cref="IMouseGrabHandler.UnGrabbingMouse"/>
public class GrabMouseEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GrabMouseEventArgs"/> class.
    /// </summary>
    /// <param name="view">The view that is requesting or releasing the mouse grab.</param>
    public GrabMouseEventArgs (View view) => View = view;

    /// <summary>
    ///     Gets or sets a value indicating whether the grab or ungrab operation should be cancelled.
    /// </summary>
    /// <value>
    ///     <see langword="true"/> to cancel the operation and prevent the grab state from changing;
    ///     <see langword="false"/> (default) to allow the operation to proceed.
    /// </value>
    /// <remarks>
    ///     When set to <see langword="true"/> in a <see cref="IMouseGrabHandler.GrabbingMouse"/> handler,
    ///     the view will not grab the mouse. When set to <see langword="true"/> in a
    ///     <see cref="IMouseGrabHandler.UnGrabbingMouse"/> handler, the view will retain the mouse grab.
    /// </remarks>
    public bool Cancel { get; set; }

    /// <summary>
    ///     Gets the view that is requesting or releasing the mouse grab.
    /// </summary>
    /// <value>
    ///     The <see cref="View"/> instance involved in the grab operation.
    /// </value>
    public View View { get; }
}

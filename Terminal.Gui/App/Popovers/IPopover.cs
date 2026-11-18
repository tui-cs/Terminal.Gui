
namespace Terminal.Gui.App;

/// <summary>
///     Defines the contract for a popover view in Terminal.Gui.
/// </summary>
/// <remarks>
///     <para>
///         A popover is a transient UI element that appears above other content to display contextual information or UI,
///         such as menus, tooltips, or dialogs.
///     </para>
///     <para>
///         <b>IMPORTANT:</b> Popovers must be registered with <see cref="Application.Popovers"/> using
///         <see cref="ApplicationPopover.Register"/> before they can be shown with <see cref="ApplicationPopover.Show"/>.
///     </para>
///     <para>
///         <b>Lifecycle:</b><br/>
///         When registered, the popover's lifetime is managed by the application. Registered popovers are
///         automatically disposed when <see cref="Application.Shutdown"/> is called. Call
///         <see cref="ApplicationPopover.DeRegister"/> to manage the lifetime directly.
///     </para>
///     <para>
///         <b>Visibility and Hiding:</b><br/>
///         Popovers are automatically hidden when:
///     </para>
///     <list type="bullet">
///         <item>The user clicks outside the popover (unless clicking on a subview).</item>
///         <item>The user presses <see cref="Application.GetDefaultKey"/> (typically <c>Esc</c>).</item>
///         <item>Another popover is shown.</item>
///         <item><see cref="View.Visible"/> is set to <see langword="false"/>.</item>
///     </list>
///     <para>
///         <b>Focus and Input:</b><br/>
///         Popovers are not modal but do receive focus and input events while visible.
///         Registered popovers receive keyboard events even when not visible, enabling global hotkey support.
///     </para>
///     <para>
///         <b>Layout:</b><br/>
///         When becoming visible, popovers are automatically laid out to fill the screen by default.
///         Override <see cref="View.Width"/> and <see cref="View.Height"/> to customize size.
///     </para>
///     <para>
///         <b>Mouse Events:</b><br/>
///         Popovers use <see cref="ViewportSettingsFlags.TransparentMouse"/>, meaning mouse events
///         outside subviews are not captured.
///     </para>
///     <para>
///         <b>Creating Custom Popovers:</b><br/>
///         Inherit from <see cref="PopoverImpl"/> and add your own content and logic.
///         For a more complete interface that exposes View-level operations without requiring casts,
///         see <see cref="IPopoverView"/>.
///     </para>
/// </remarks>
/// <seealso cref="IPopoverView"/>
public interface IPopover
{
    /// <summary>
    ///     Gets or sets the <see cref="IRunnable"/> that this popover is associated with.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see langword="null"/>, the popover is not associated with any runnable and will receive all keyboard
    ///         events from the application.
    ///     </para>
    ///     <para>
    ///         If set, the popover will only receive keyboard events when the associated runnable is active.
    ///     </para>
    ///     <para>
    ///         When <see cref="ApplicationPopover.Register"/> is called, this property is automatically set to
    ///         <see cref="IApplication.TopRunnableView"/> if not already set.
    ///     </para>
    /// </remarks>
    IRunnable? Owner { get; set; }
}

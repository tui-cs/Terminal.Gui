using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

/// <summary>
///     Abstract base class for popover views in Terminal.Gui. Implements <see cref="IPopoverView"/>.
/// </summary>
/// <remarks>
///     <para>
///         <b>IMPORTANT:</b> Popovers must be registered with <see cref="Application.Popovers"/> using
///         <see cref="ApplicationPopover.Register"/> before they can be shown.
///     </para>
///     <para>
///         <b>Requirements:</b><br/>
///         Derived classes must:
///     </para>
///     <list type="bullet">
///         <item>
///             Set <see cref="View.ViewportSettings"/> to include <see cref="ViewportSettingsFlags.Transparent"/> and
///             <see cref="ViewportSettingsFlags.TransparentMouse"/>.
///         </item>
///         <item>Add a key binding for <see cref="Command.Quit"/> (typically bound to <see cref="Application.GetDefaultKey"/>).</item>
///     </list>
///     <para>
///         <b>Default Behavior:</b><br/>
///         This base class provides:
///     </para>
///     <list type="bullet">
///         <item>
///             Fills the screen by default (<see cref="View.Width"/> = <see cref="Dim.Fill()"/>, <see cref="View.Height"/>
///             = <see cref="Dim.Fill()"/>).
///         </item>
///         <item>Transparent viewport settings for proper mouse event handling.</item>
///         <item>Automatic layout when becoming visible.</item>
///         <item>Focus restoration when hidden.</item>
///         <item>Default <see cref="Command.Quit"/> implementation that hides the popover.</item>
///     </list>
///     <para>
///         <b>Lifecycle:</b><br/>
///         Use <see cref="ApplicationPopover.Show"/> to display and <see cref="ApplicationPopover.Hide"/> or
///         set <see cref="View.Visible"/> to <see langword="false"/> to hide.
///     </para>
/// </remarks>
public abstract class PopoverImpl : View, IPopoverView
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverImpl"/> class.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Sets up default popover behavior:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             Fills the screen (<see cref="View.Width"/> = <see cref="Dim.Fill()"/>, <see cref="View.Height"/> =
    ///             <see cref="Dim.Fill()"/>).
    ///         </item>
    ///         <item>Sets <see cref="View.CanFocus"/> to <see langword="true"/>.</item>
    ///         <item>
    ///             Configures <see cref="View.ViewportSettings"/> with <see cref="ViewportSettingsFlags.Transparent"/> and
    ///             <see cref="ViewportSettingsFlags.TransparentMouse"/>.
    ///         </item>
    ///         <item>
    ///             Adds <see cref="Command.Quit"/> bound to <see cref="Application.GetDefaultKey"/> which hides the popover when
    ///             invoked.
    ///         </item>
    ///     </list>
    /// </remarks>
    protected PopoverImpl ()
    {
#if DEBUG
        Id = "popoverImpl";
#endif
        CanFocus = true;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse;

        // TODO: Add a diagnostic setting for this?
        //TextFormatter.VerticalAlignment = Alignment.End;
        //TextFormatter.Alignment = Alignment.End;
        //base.Text = "popover";

        AddCommand (Command.Quit, Quit);
        KeyBindings.Add (Application.GetDefaultKey (Command.Quit), Command.Quit);
        KeyBindings.Remove (Key.Enter);

        // Clear all mouse bindings so there's no conflict with subviews
        MouseBindings.Clear ();

        return;

        bool? Quit (ICommandContext? ctx)
        {
            if (!Visible)
            {
                return false;
            }

            Visible = false;

            return true;
        }
    }

    /// <inheritdoc/>
    public IRunnable? Owner
    {
        get;
        set
        {
            field = value;
            App ??= (field as View)?.App;
        }
    }

    private CommandBridge? _targetBridge;

    /// <summary>
    ///     Gets or sets the target <see cref="View"/> of this popover as a weak reference. This is typically the view that
    ///     triggered the popover to be shown.
    ///     Commands that bubble from Views within the Popover will be bridged to this target view, allowing them to be handled
    ///     as if they originated from the target.
    ///     This is useful for scenarios where the View that triggered the popover is not part of the popover's view hierarchy,
    ///     but still wants commands to be handled in the context of that view.
    /// </summary>
    public WeakReference<View>? Target
    {
        get;
        set
        {
            // Tear down old bridge
            _targetBridge?.Dispose ();
            _targetBridge = null;

            field = value;

            // Create bridge: when this popover fires Activated/Accepted, bridge to Target
            if (value?.TryGetTarget (out View? targetView) == true)
            {
                _targetBridge = CommandBridge.Connect (targetView, this, Command.Activate, Command.Accept);
            }
        }
    }

    /// <summary>
    ///     Gets or sets the anchor positioning function. When the popover is shown, this function
    ///     is called to determine the anchor rectangle for positioning.
    /// </summary>
    public Func<Rectangle?>? Anchor { get; set; }

    /// <summary>
    ///     Makes the popover visible. Base implementation performs layout and delegates to <see cref="ApplicationPopover.Show"/>.
    ///     Derived classes typically override to add positioning logic before calling base.
    /// </summary>
    /// <param name="idealScreenPosition">
    ///     The ideal screen position for the popover. If <see langword="null"/>, uses the current mouse position.
    /// </param>
    /// <param name="anchor">
    ///     The anchor rectangle to position relative to. If <see langword="null"/>, uses the <see cref="Anchor"/> property.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         Base implementation:
    ///     </para>
    ///     <list type="number">
    ///         <item>Returns if already <see cref="View.Visible"/></item>
    ///         <item>Calls <see cref="View.Layout()"/></item>
    ///         <item>Calls <see cref="ApplicationPopover.Show"/> to make visible</item>
    ///     </list>
    ///     <para>
    ///         Derived classes should override to insert positioning logic between steps 2 and 3.
    ///     </para>
    /// </remarks>
    public virtual void MakeVisible (Point? idealScreenPosition = null, Rectangle? anchor = null)
    {
        if (Visible)
        {
            return;
        }

        Layout ();
        App?.Popovers?.Show (this);
    }

    /// <summary>
    ///     Attempts to retrieve the <see cref="Target"/> view. Returns <see langword="false"/> if the target has been
    ///     collected or was never set.
    /// </summary>
    /// <param name="target">
    ///     When this method returns <see langword="true"/>, contains the target <see cref="View"/>; otherwise,
    ///     <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if the target view is still alive; otherwise, <see langword="false"/>.</returns>
    public bool TryGetTarget ([NotNullWhen (true)] out View? target)
    {
        if (Target?.TryGetTarget (out target) == true)
        {
            return true;
        }

        target = null;

        return false;
    }

    /// <summary>
    ///     Called when the <see cref="View.Visible"/> property is changing. Handles layout and focus management.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> to cancel the visibility change; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         <b>When becoming visible:</b> Lays out the popover to fit the screen.
    ///     </para>
    ///     <para>
    ///         <b>When becoming hidden:</b> Restores focus to the previously focused view in the view hierarchy.
    ///     </para>
    /// </remarks>
    protected override bool OnVisibleChanging ()
    {
        bool ret = base.OnVisibleChanging ();

        if (ret)
        {
            return ret;
        }

        if (!Visible)
        {
            // Whenever visible is changing to true, we need to resize;
            // it's our only chance because we don't get laid out until we're visible
            if (App is { })
            {
                Layout (App.Screen.Size);
            }
        }
        else
        {
            // Whenever visible is changing to false, we need to reset the focus
            if (ApplicationNavigation.IsInHierarchy (this, App?.Navigation?.GetFocused ()))
            {
                App?.Navigation?.SetFocused (App?.TopRunnableView?.MostFocused);
            }
        }

        return ret;
    }

    /// <summary>
    ///     Called when the <see cref="View.Visible"/> property has changed. Hides the popover via <see cref="ApplicationPopover"/>
    ///     when becoming invisible.
    /// </summary>
    protected override void OnVisibleChanged ()
    {
        base.OnVisibleChanged ();

        // When becoming invisible, notify ApplicationPopover
        if (!Visible)
        {
            App?.Popovers?.Hide (this);
        }
    }

    /// <summary>
    ///     Returns a string representation of the popover, including its type, visibility, owner, and target.
    /// </summary>
    /// <returns>A formatted string with popover state information.</returns>
    public override string ToString ()
    {
        string owner = Owner is { } ? $"Owner={Owner}" : "Owner=null";
        string target = TryGetTarget (out View? t) ? $", Target={t.ToIdentifyingString ()}" : "";
        string visible = Visible ? "Visible" : "Hidden";

        return $"{GetType ().Name}({Id}) {visible} ({owner}{target})";
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            _targetBridge?.Dispose ();
            _targetBridge = null;
        }

        base.Dispose (disposing);
    }
}

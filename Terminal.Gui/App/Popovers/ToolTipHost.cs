namespace Terminal.Gui.App;

/// <summary>
///     A generic tooltip view that hosts a view and can be shown by a <c>ToolTipManager</c>.
/// </summary>
/// <typeparam name="TView">
///     The type of view being hosted. Must derive from <see cref="View"/> and have a parameterless constructor.
/// </typeparam>
/// <remarks>
///     <para>
///         This class is inspired by <see cref="Popover{TView, TResult}"/> but simplified for tooltip scenarios
///         where no result extraction is needed.
///     </para>
///     <para>
///         This class is responsible only for hosting, positioning and displaying tooltip content.
///         Hover handling and target registration should be managed externally by a tooltip manager.
///     </para>
/// </remarks>
public class ToolTipHost<TView> : PopoverImpl, IDesignable where TView : View, new()
{
    private CommandBridge? _contentCommandBridge;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ToolTipHost{TView}"/> class.
    /// </summary>
    public ToolTipHost () : this (null) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ToolTipHost{TView}"/> class with the specified content view.
    /// </summary>
    /// <param name="contentView">
    ///     The view to host in the tooltip. If <see langword="null"/>, a new instance will be created.
    /// </param>
    public ToolTipHost (TView? contentView)
    {
        // Do this to support debugging traces where Title gets set
        // Unicode Character 'REPLACEMENT CHARACTER' (U+FFFF) is used to indicate an invalid HotKeySpecifier
        base.HotKeySpecifier = (Rune)'\xffff';

        Border.Settings &= ~BorderSettings.Title;

        base.Visible = false;

#if DEBUG
        if (string.IsNullOrEmpty (contentView?.Id))
        {
            contentView?.Id = $"tooltipContentView_{Id}";
        }
#endif

        ContentView = contentView;
    }

    /// <summary>
    ///     Gets or sets the content view hosted by this tooltip.
    /// </summary>
    public TView? ContentView
    {
        get;
        set
        {
            if (field is null && value is null)
            {
                value = new TView ();
            }
            else if (ReferenceEquals (field, value))
            {
                return;
            }

#if DEBUG
            Id = $"{value?.Id}ToolTip";
#endif

            if (field is { })
            {
                field.VisibleChanged -= ContentViewOnVisibleChanged;
                Remove (field);
                field.Dispose ();
            }

            field = value ?? new TView ();

            field.App = App;
            Add (field);

            field.VisibleChanged += ContentViewOnVisibleChanged;

            _contentCommandBridge?.Dispose ();
            _contentCommandBridge = CommandBridge.Connect (this, field, Command.Activate, Command.Accept);
        }
    }

    /// <summary>
    ///     Makes the tooltip visible and positions it based on <see cref="IPopoverView.Anchor"/> or
    ///     <paramref name="idealScreenPosition"/>.
    /// </summary>
    /// <param name="idealScreenPosition">
    ///     The ideal screen-relative position for the tooltip. If <see langword="null"/>, positioning is determined
    ///     by <see cref="IPopoverView.Anchor"/> or the current mouse position.
    /// </param>
    /// <param name="anchor">
    ///     Optional anchor rectangle to override <see cref="IPopoverView.Anchor"/> property for this call.
    ///     If <see langword="null"/>, uses the <see cref="IPopoverView.Anchor"/> property.
    /// </param>
    public override void MakeVisible (Point? idealScreenPosition = null, Rectangle? anchor = null)
    {
        Layout ();
        SetPosition (idealScreenPosition, anchor);

        if (App?.Popovers is not { } popovers)
        {
            return;
        }

        if (!popovers.IsRegistered (this))
        {
            popovers.Register (this);
        }

        popovers.Show (this);
    }

    /// <summary>
    ///     Sets the position of the tooltip based on <paramref name="idealScreenPosition"/> or <paramref name="anchor"/>.
    /// </summary>
    public void SetPosition (Point? idealScreenPosition = null, Rectangle? anchor = null)
    {
        Rectangle? effectiveAnchor = anchor ?? Anchor?.Invoke ();

        if (effectiveAnchor is { })
        {
            idealScreenPosition = new Point (effectiveAnchor.Value.X, effectiveAnchor.Value.Y + effectiveAnchor.Value.Height);
        }

        idealScreenPosition ??= App?.Mouse.LastMousePosition;

        if (idealScreenPosition is null || ContentView is null)
        {
            return;
        }

        Point pos = idealScreenPosition.Value;

        if (!ContentView.IsInitialized)
        {
            ContentView.App ??= App;
            ContentView.BeginInit ();
            ContentView.EndInit ();
        }

        pos = GetAdjustedPosition (ContentView, pos);

        ContentView.X = pos.X;
        ContentView.Y = pos.Y;
    }

    /// <summary>
    ///     Calculates an adjusted screen-relative position for the content view to ensure full visibility.
    /// </summary>
    protected virtual Point GetAdjustedPosition (View view, Point idealLocation)
    {
        int screenWidth = App?.Screen.Width ?? 0;
        int screenHeight = App?.Screen.Height ?? 0;

        int viewWidth = view.Frame.Width;
        int viewHeight = view.Frame.Height;

        int nx = idealLocation.X;

        if (nx + viewWidth > screenWidth)
        {
            nx = Math.Max (screenWidth - viewWidth, 0);
        }

        nx = Math.Max (nx, 0);

        int ny = idealLocation.Y;

        if (ny + viewHeight > screenHeight)
        {
            ny = idealLocation.Y - viewHeight - 1;
        }

        ny = Math.Max (ny, 0);

        return new Point (nx, ny);
    }

    /// <inheritdoc/>
    protected override void OnVisibleChanged ()
    {
        if (Visible)
        {
            ContentView?.Visible = true;
        }
        else
        {
            ContentView?.Visible = false;
        }

        base.OnVisibleChanged ();
    }

    /// <summary>
    ///     Called when the ContentView becomes invisible, to hide the ToolTip.
    /// </summary>
    private void ContentViewOnVisibleChanged (object? sender, EventArgs e)
    {
        if (sender is View { Visible: false } view && view == ContentView && Visible)
        {
            Visible = false;
        }
    }

    /// <summary>
    ///     Replaces the current content with a newly created view.
    /// </summary>
    public void SetContent (Func<TView> contentFactory)
    {
        ArgumentNullException.ThrowIfNull (contentFactory);
        ContentView = contentFactory ();
    }

    /// <summary>
    ///     Enables the tooltip for use in design-time scenarios.
    /// </summary>
    public virtual bool EnableForDesign<TContext> (ref TContext targetView) where TContext : notnull
    {
        ContentView ??= new TView ();

        if (ContentView is IDesignable designable)
        {
            return designable.EnableForDesign (ref targetView);
        }

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            ContentView?.VisibleChanged -= ContentViewOnVisibleChanged;
            ContentView?.Dispose ();
            ContentView = null;

            _contentCommandBridge?.Dispose ();
            _contentCommandBridge = null;
        }

        base.Dispose (disposing);
    }
}
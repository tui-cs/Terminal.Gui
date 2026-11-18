namespace Terminal.Gui.ViewBase;

/// <summary>
///     The <see cref="View"/>-backed rendering layer for an adornment (<see cref="Margin"/>, <see cref="Border"/>,
///     or <see cref="Padding"/>).
///     Implements <see cref="IAdornmentView"/> — i.e., it knows its <see cref="IAdornment.Parent"/> <see cref="View"/>
///     and its <see cref="Adornment"/> settings owner.
///     Created lazily by <see cref="AdornmentImpl.GetOrCreateView"/> when <see cref="View"/>-level functionality is
///     needed.
/// </summary>
/// <remarks>
///     <para>
///         This class is structurally a copy of <see cref="Adornment"/>. It extends <see cref="View"/> directly
///         (not <see cref="Adornment"/>) and accesses <see cref="IAdornment.Thickness"/> via the <see cref="IAdornment"/>
///         back-reference, making <see cref="AdornmentImpl"/> the single authoritative owner of Thickness.
///     </para>
///     <para>
///         <see cref="MarginView"/>, <see cref="BorderView"/>, and <see cref="PaddingView"/> all extend
///         <see cref="AdornmentView"/>. <see cref="AdornmentImpl"/> is the authoritative owner of
///         <see cref="IAdornment.Thickness"/>; the corresponding <see cref="AdornmentView"/> hosts the
///         render-layer behavior.
///     </para>
/// </remarks>
public class AdornmentView : View, IAdornmentView, IDesignable
{
    /// <summary>Parameter-less constructor required to support all views unit tests (e.g., AllViewsTester).</summary>
    public AdornmentView ()
    {
        /* Do nothing. */
    }

    /// <summary>Constructs a rendering layer for the specified <paramref name="adornment"/>.</summary>
    public AdornmentView (IAdornment adornment)
    {
        // Set Adornment FIRST so subclass constructors can reference it safely.
        Adornment = adornment;

        // By default, Adornments can't get focus; has to be enabled specifically.
        CanFocus = false;
        TabStop = TabBehavior.NoStop;

        // By default, Adornments have no key bindings.
        KeyBindings.Clear ();
    }

    /// <inheritdoc/>
    public virtual void OnParentFrameChanged (Rectangle newParentFrame) => throw new NotImplementedException ();

    /// <inheritdoc cref="IAdornmentView.Adornment"/>
    public IAdornment? Adornment { get; set; }

    #region View Overrides

    /// <summary>
    ///     Gets or sets whether the Adornment will draw diagnostic information.
    /// </summary>
    public new ViewDiagnosticFlags Diagnostics { get; set; } = View.Diagnostics;

    /// <inheritdoc/>
    public override string ToDebugString () =>
        $"{this.ToIdentifyingString ()} Parent={(Adornment?.Parent is { } ? Adornment.Parent.ToDebugString () : "null")}";

    /// <inheritdoc/>
    protected override IApplication? GetApp () => Adornment?.Parent?.App;

    /// <inheritdoc/>
    protected override IDriver? GetDriver () => Adornment?.Parent?.Driver ?? base.GetDriver ();

    private Scheme? _scheme;

    /// <inheritdoc/>
    protected override bool OnGettingScheme (out Scheme? scheme)
    {
        scheme = _scheme ?? Adornment?.Parent?.GetScheme () ?? SchemeManager.GetScheme (Schemes.Base);

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnSettingScheme (ValueChangingEventArgs<Scheme?> args)
    {
        Adornment?.Parent?.SetNeedsDraw ();
        _scheme = args.NewValue;

        return false;
    }

    /// <inheritdoc/>
    public override Rectangle Viewport
    {
        get => base.Viewport;
        set => throw new InvalidOperationException (@"The Viewport of an AdornmentView cannot be modified.");
    }

    /// <inheritdoc/>
    public override Rectangle FrameToScreen ()
    {
        if (Adornment?.Parent is null)
        {
            // Support AllViewsTester where AdornmentView may be a SubView.
            if (SuperView is null)
            {
                return Frame;
            }

            Point super = SuperView.ViewportToScreen (Frame.Location);

            return new Rectangle (super, Frame.Size);
        }

        // AdornmentViews are *Children* of a View, not SubViews. Use Parent.FrameToScreen()
        // to get the parent's screen origin, then offset by our Frame.
        Rectangle parentScreen = Adornment.Parent.FrameToScreen ();

        return new Rectangle (new Point (parentScreen.X + Frame.X, parentScreen.Y + Frame.Y), Frame.Size);
    }

    /// <inheritdoc/>
    public override Point ScreenToFrame (in Point location)
    {
        View? parentOrSuperView = Adornment?.Parent;

        if (parentOrSuperView is { })
        {
            return parentOrSuperView.ScreenToFrame (new Point (location.X - Frame.X, location.Y - Frame.Y));
        }

        // Support AllViewsTester where AdornmentView may be a SubView.
        parentOrSuperView = SuperView;

        if (parentOrSuperView is null)
        {
            return Point.Empty;
        }

        return parentOrSuperView.ScreenToFrame (new Point (location.X - Frame.X, location.Y - Frame.Y));
    }

    /// <summary>
    ///     Called when the <see cref="Thickness"/> of the Adornment is to be cleared.
    /// </summary>
    /// <returns><see langword="true"/> to stop further clearing.</returns>
    protected override bool OnClearingViewport ()
    {
        if (Adornment is null || Adornment.Thickness == Thickness.Empty)
        {
            return true;
        }

        if (Driver is { })
        {
            Adornment.Thickness.Draw (Driver, ViewportToScreen (Viewport), Diagnostics, ToString ());
        }

        // Do NOT call SetNeedsDraw () here. The thickness has been drawn; calling
        // SetNeedsDraw cascades to the parent's SubViewNeedsDraw mid-pass and adds
        // churn that competes with the needsDrawSelf gate added for issue #5358.
        return true;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingText () => Adornment is null || Adornment.Thickness == Thickness.Empty;

    /// <inheritdoc/>
    protected override bool OnDrawingSubViews () => Adornment is null || Adornment.Thickness == Thickness.Empty;

    /// <summary>Does nothing for AdornmentView.</summary>
    protected override bool OnRenderingLineCanvas () => true;

    /// <summary>
    ///     AdornmentViews only render to their <see cref="IAdornment.Parent"/>'s or Parent's SuperView's LineCanvas.
    /// </summary>
    public override bool SuperViewRendersLineCanvas
    {
        get => false;
        set => throw new InvalidOperationException (@"AdornmentView can only render to their Parent or Parent's Superview.");
    }

    /// <summary>
    ///     Indicates whether the specified Parent's SuperView-relative coordinates are within the Adornment's
    ///     <see cref="Thickness"/>.
    /// </summary>
    public override bool Contains (in Point location)
    {
        View? parentOrSuperView = Adornment?.Parent;

        if (parentOrSuperView is null)
        {
            parentOrSuperView = SuperView;

            if (parentOrSuperView is null)
            {
                return false;
            }
        }

        if (Adornment is null)
        {
            return false;
        }

        Rectangle outside = Frame;
        outside.Offset (parentOrSuperView.Frame.Location);

        return Adornment?.Thickness.Contains (outside, location) ?? false;
    }

    #endregion View Overrides

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Adornment?.Thickness = new Thickness (3);

        Frame = new Rectangle (0, 0, 10, 10);
        Diagnostics = ViewDiagnosticFlags.Thickness;

        return true;
    }
}

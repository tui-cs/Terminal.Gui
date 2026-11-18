namespace Terminal.Gui.ViewBase;

/// <summary>
///     Lightweight base class for adornment settings.
///     Holds <see cref="Thickness"/> and optional <see cref="Parent"/> reference.
///     The full <see cref="AdornmentView"/> is created lazily via <see cref="GetOrCreateView"/>
///     only when needed.
/// </summary>
public abstract class AdornmentImpl : IAdornment
{
    /// <summary>
    ///     The <see cref="View"/> this adornment surrounds. Not on <see cref="IAdornment"/> — callers that need
    ///     the parent already have the <see cref="View"/> reference they used to access this adornment.
    ///     Set by <see cref="View.SetupAdornments"/> and used for geometry math and
    ///     <see cref="GetOrCreateView"/> creation.
    /// </summary>
    public View? Parent
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field?.FrameChanged -= ParentOnFrameChanged;
            field = value;
            field?.FrameChanged += ParentOnFrameChanged;

            if (Parent is { })
            {
                View?.OnParentFrameChanged (Parent.Frame);
            }
        }
    }

    #region Thickness

    /// <inheritdoc/>
    public Thickness Thickness
    {
        get;
        set
        {
            if (value == field)
            {
                return;
            }

            field = value;

            OnThicknessChanged ();
            ThicknessChanged?.Invoke (this, EventArgs.Empty);
        }
    } = Thickness.Empty;

    /// <inheritdoc/>
    public event EventHandler? ThicknessChanged;

    /// <summary>Called when <see cref="Thickness"/> changes. Override in subclasses to react; base is empty.</summary>
    protected virtual void OnThicknessChanged () { }

    #endregion Thickness

    #region View

    /// <inheritdoc/>
    public abstract Rectangle GetFrame ();

    /// <summary>
    ///     The <see cref="IAdornmentView"/> backing this adornment, cast to the adornment type for convenience.
    ///     <see langword="null"/> until the adornment actually needs <see cref="View"/>-level functionality
    ///     (rendering, SubViews, mouse, arrangement, shadow).
    /// </summary>
    public AdornmentView? View { get; internal set; }

    /// <summary>
    ///     Explicit <see cref="IAdornment"/> implementation — exposes <see cref="View"/> as
    ///     <see cref="IAdornmentView"/> so the interface contract does not reference the concrete class.
    /// </summary>
    IAdornmentView? IAdornment.View => View;

    /// <summary>
    ///     Returns the existing <see cref="AdornmentView"/>, creating it if not yet allocated.
    ///     Calls <see cref="View.BeginInit"/> and/or <see cref="View.EndInit"/> on the new view
    ///     to match the parent's current initialization state.
    /// </summary>
    /// <remarks>Must be called on the UI thread. Internal to prevent eager allocation by consumers.</remarks>
    public AdornmentView GetOrCreateView ()
    {
        if (View is { })
        {
            return View;
        }

        // Capture field-backed ViewportSettings before creating View, since the getter
        // switches to reading from View once View is non-null.
        ViewportSettingsFlags savedViewportSettings = ViewportSettings;

        View = CreateView ();

        // Synchronize ViewportSettings that were set before the View existed.
        View.ViewportSettings = savedViewportSettings;

        // Synchronize frame from parent's current state (we may have missed FrameChanged events).
        if (Parent is { })
        {
            View.OnParentFrameChanged (Parent.Frame);
        }

        // Synchronize init state with the parent.
        if (Parent?.IsInitialized != true)
        {
            return View;
        }
        View.BeginInit ();
        View.EndInit ();

        return View;
    }

    private void ParentOnFrameChanged (object? sender, EventArgs<Rectangle> e) => View?.OnParentFrameChanged (e.Value);

    /// <summary>Factory method — subclasses return their specific <see cref="AdornmentView"/> subclass.</summary>
    protected abstract AdornmentView CreateView ();

    #endregion View

    #region Coordinator methods

    /// <summary>Returns the screen-relative rectangle for this adornment.</summary>
    public Rectangle FrameToScreen () => View is { } v ? v.FrameToScreen () : ComputeFrameToScreen ();

    private Rectangle ComputeFrameToScreen ()
    {
        Rectangle parentScreen = Parent?.FrameToScreen () ?? Rectangle.Empty;

        return new Rectangle (new Point (parentScreen.X + GetFrame ().X, parentScreen.Y + GetFrame ().Y), GetFrame ().Size);
    }

    #endregion Coordinator methods

    /// <summary>Gets or sets diagnostic flags. Stored locally and forwarded to the backing <see cref="View"/> when it exists.</summary>
    public ViewDiagnosticFlags Diagnostics
    {
        get => View?.Diagnostics ?? field;
        set
        {
            field = value;

            if (View is { } v)
            {
                v.Diagnostics = value;
            }
        }
    } = ViewDiagnosticFlags.Off;

    /// <summary>
    ///     Gets the cached drawn region from the last draw pass. Populated during
    ///     <see cref="View.Draw(DrawContext?)"/> for adornments with <see cref="ViewportSettingsFlags.TransparentMouse"/> set.
    ///     Used by mouse hit-testing to determine which cells should receive mouse events.
    ///     Returns <see langword="null"/> if not drawn yet or TransparentMouse not set.
    ///     Invalidated by <see cref="View.SetNeedsDraw()"/>.
    /// </summary>
    internal Region? CachedDrawnRegion { get; set; }

    /// <summary>
    ///     Gets the drawn region from this adornment's last Draw pass.
    ///     Populated by <see cref="View.DrawAdornments"/> using a per-adornment <see cref="DrawContext"/>.
    ///     Used by <see cref="View.DoDrawComplete"/> for both visual transparency clip exclusion
    ///     and <see cref="CachedDrawnRegion"/> computation — uniformly for all adornment types.
    /// </summary>
    internal Region? LastDrawnRegion { get; set; }

    /// <summary>Gets or sets the viewport settings flags on the backing <see cref="View"/>.</summary>
    public ViewportSettingsFlags ViewportSettings
    {
        get => View?.ViewportSettings ?? field;
        set
        {
            field = value;

            if (View is { } v)
            {
                v.ViewportSettings = value;
            }
        }
    }

    /// <summary>
    ///     Updates <see cref="CachedDrawnRegion"/> (and the backing <see cref="View"/>'s
    ///     <see cref="View.CachedDrawnRegion"/>) from <see cref="LastDrawnRegion"/> and an
    ///     optional line-canvas region. Only acts when
    ///     <see cref="ViewportSettingsFlags.TransparentMouse"/> is set.
    /// </summary>
    /// <param name="lineCanvasRegion">
    ///     The parent view's rendered <see cref="LineCanvas"/> region, or <see langword="null"/>.
    ///     When non-null, the portion within this adornment's frame is included in the cached region.
    /// </param>
    internal void UpdateCachedDrawnRegion (Region? lineCanvasRegion)
    {
        if (!ViewportSettings.FastHasFlags (ViewportSettingsFlags.TransparentMouse))
        {
            return;
        }

        Region adornmentDrawnRegion = new ();

        if (LastDrawnRegion is { })
        {
            adornmentDrawnRegion.Combine (LastDrawnRegion, RegionOp.Union);
        }

        // The parent's LineCanvas includes border lines rendered in DoRenderLineCanvas.
        // Intersect with this adornment's frame to get only the lines within it.
        if (lineCanvasRegion is { })
        {
            Region lineRegion = lineCanvasRegion.Clone ();
            lineRegion.Intersect (FrameToScreen ());
            adornmentDrawnRegion.Combine (lineRegion, RegionOp.Union);
        }

        CachedDrawnRegion = adornmentDrawnRegion;

        if (View is { } adornmentView)
        {
            adornmentView.CachedDrawnRegion = adornmentDrawnRegion;
        }
    }

    /// <summary>
    ///     Adds this adornment's drawn region (from <see cref="LastDrawnRegion"/> and an optional
    ///     line-canvas region) to the provided <paramref name="exclusion"/> region.
    ///     Used during transparent-layer clip exclusion in <c>DoDrawComplete</c>.
    /// </summary>
    /// <param name="exclusion">The exclusion region to add drawn cells to.</param>
    /// <param name="lineCanvasRegion">
    ///     The parent view's rendered <see cref="LineCanvas"/> region, or <see langword="null"/>.
    /// </param>
    internal void AddDrawnRegionTo (Region exclusion, Region? lineCanvasRegion)
    {
        if (LastDrawnRegion is { })
        {
            Region clipped = LastDrawnRegion.Clone ();
            clipped.Intersect (FrameToScreen ());
            exclusion.Combine (clipped, RegionOp.Union);
        }

        // The parent's LineCanvas includes border lines rendered in DoRenderLineCanvas.
        if (lineCanvasRegion is null)
        {
            return;
        }

        Region lineRegion = lineCanvasRegion.Clone ();
        lineRegion.Intersect (FrameToScreen ());
        exclusion.Combine (lineRegion, RegionOp.Union);
    }

    /// <summary>
    ///     Indicates whether the specified SuperView-relative coordinates are within this adornment's
    ///     <see cref="Thickness"/>. Works even when no <see cref="View"/> has been created.
    /// </summary>
    public bool Contains (in Point location)
    {
        if (View is { } v)
        {
            return v.Contains (location);
        }

        if (Parent is null)
        {
            return false;
        }

        Rectangle outside = GetFrame ();
        outside.Offset (Parent.Frame.Location);

        return Thickness.Contains (outside, location);
    }
}

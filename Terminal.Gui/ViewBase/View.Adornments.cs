namespace Terminal.Gui.ViewBase;

public partial class View // Adornments
{
    /// <summary>
    ///     Initializes the Adornments of the View. Called by the constructor.
    /// </summary>
    private void SetupAdornments ()
    {
        if (this is AdornmentView)
        {
            return;
        }

        Thickness previousMarginThickness = Margin.Thickness;
        Thickness previousBorderThickness = Border.Thickness;
        Thickness previousPaddingThickness = Padding.Thickness;

        Margin.Parent = this;
        Border.Parent = this;
        Padding.Parent = this;

        // When any adornment's thickness changes, recompute frames and request layout + redraw.
        Margin.ThicknessChanged += (_, _) =>
                                   {
                                       previousMarginThickness = HandleAdornmentThicknessChanged (Margin, previousMarginThickness);
                                   };

        Border.ThicknessChanged += (_, _) =>
                                   {
                                       previousBorderThickness = HandleAdornmentThicknessChanged (Border, previousBorderThickness);
                                   };

        Padding.ThicknessChanged += (_, _) =>
                                    {
                                        previousPaddingThickness = HandleAdornmentThicknessChanged (Padding, previousPaddingThickness);
                                    };

        Thickness HandleAdornmentThicknessChanged (IAdornment adornment, Thickness previousThickness)
        {
            Rectangle oldViewport = GetViewportForAdornmentThickness (adornment, previousThickness);
            Rectangle oldViewportFrame = GetViewportFrameForAdornmentThickness (adornment, previousThickness);
            Thickness currentThickness = adornment.Thickness;

            (adornment.View as View)?.SetNeedsLayout ();
            SetAdornmentFrames ();
            SetNeedsLayout ();
            SetNeedsDraw ();

            if (oldViewportFrame != GetViewportFrameForAdornmentThickness (adornment, currentThickness))
            {
                InvalidateAdornmentThicknessChange ();
            }

            if (oldViewport != Viewport)
            {
                RaiseViewportChangedEvent (oldViewport);
            }

            return currentThickness;
        }
    }

    private void BeginInitAdornments ()
    {
        Margin.View?.BeginInit ();
        Border.View?.BeginInit ();
        Padding.View?.BeginInit ();
    }

    private void EndInitAdornments ()
    {
        Margin.View?.EndInit ();
        Border.View?.EndInit ();
        Padding.View?.EndInit ();
    }

    private void DisposeAdornments ()
    {
        Margin.View?.Dispose ();
        Margin.View = null;
        Margin.Parent = null;
        Border.View?.Dispose ();
        Border.View = null;
        Border.Parent = null;
        Padding.View?.Dispose ();
        Padding.View = null;
        Padding.Parent = null;
    }

    /// <summary>
    ///     The <see cref="IAdornment"/> that enables separation of a View from other SubViews of the same
    ///     SuperView. The margin offsets the <see cref="Viewport"/> from the <see cref="Frame"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The margin is typically transparent. This can be overriden by explicitly setting <see cref="Scheme"/>.
    ///     </para>
    ///     <para>
    ///         Enabling <see cref="ShadowStyle"/> will change the Thickness of the Margin to include the shadow.
    ///     </para>
    ///     <para>
    ///         The adornments (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the
    ///         View's content and are not clipped by the View's Clip Area.
    ///     </para>
    ///     <para>
    ///         Changing the size of an adornment (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>) will
    ///         change the size of <see cref="Frame"/> which will call <see cref="SetNeedsLayout"/> to update the layout of the
    ///         <see cref="SuperView"/> and its <see cref="SubViews"/>.
    ///     </para>
    /// </remarks>
    public Margin Margin { get; } = new ();

    /// <summary>
    ///     Gets or sets the shadow effect that will be drawn on the right and bottom sides of the
    ///     Margin.
    /// </summary>
    /// <remarks>
    ///     <see langword="null"/> will disable the shadow. All other values will add a shadow to the right and bottom sides of
    ///     the Margin.
    ///     The View 's <see cref="Frame"/> will be expanded to include the shadow.
    /// </remarks>
    public virtual ShadowStyles? ShadowStyle
    {
        get => Margin.ShadowStyle ?? null;
        set
        {
            SetShadowStyle (value);
            OnShadowStyleChanged ();
            ShadowStyleChanged?.Invoke (this, EventArgs.Empty);
        }
    }

    /// <summary>
    ///     Called when the <see cref="ShadowStyle"/> has changed.
    /// </summary>
    protected virtual bool OnShadowStyleChanged () => false;

    /// <summary>
    ///     Fired when the <see cref="ShadowStyle"/> has changed.
    /// </summary>
    public event EventHandler<EventArgs>? ShadowStyleChanged;

    /// <summary>
    ///     Sets the <see cref="ShadowStyle"/> of the view to the specified value.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="ShadowStyle"/> is a helper for manipulating the view's <see cref="Margin"/>. Setting this property
    ///         to any value other
    ///         than <see cref="ShadowStyles.None"/> is equivalent to setting <see cref="Margin"/>'s
    ///         <see cref="IAdornment.Thickness"/> and <see cref="ShadowStyle"/> to the value.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see cref="ShadowStyles.None"/> is equivalent to setting <see cref="Margin"/>'s
    ///         <see cref="IAdornment.Thickness"/> to `0` and <see cref="ShadowStyle"/> to <see cref="ShadowStyles.None"/>.
    ///     </para>
    ///     <para>For more advanced customization of the view's margin, manipulate see <see cref="Margin"/> directly.</para>
    /// </remarks>
    /// <param name="style"></param>
    internal void SetShadowStyle (ShadowStyles? style)
    {
        Margin.ShadowStyle = style;

        SetNeedsLayout ();
    }

    /// <summary>
    ///     Gets the <see cref="ViewBase.Border"/> adornment that draws the visual frame, title, and optional tab header
    ///     between the <see cref="Margin"/> and <see cref="Padding"/> layers.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Border provides the space for a visual border (drawn using line-drawing glyphs) and the
    ///         <see cref="Title"/>. When <see cref="Border.Settings"/> includes <see cref="BorderSettings.Tab"/>,
    ///         the border renders a tab header on the side specified by <see cref="BorderView.TabSide"/>.
    ///     </para>
    ///     <para>
    ///         The Border expands inward: if <c>Border.Thickness.Top == 2</c>, the border and title occupy the
    ///         first two rows, reducing the <see cref="Viewport"/>.
    ///     </para>
    ///     <para>
    ///         The Border also provides the UI for mouse and keyboard arrangement of the View.
    ///         See <see cref="Arrangement"/> and the
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/arrangement.html">Arrangement Deep Dive</see>.
    ///     </para>
    ///     <para>
    ///         <see cref="BorderStyle"/> is a convenience helper that sets <see cref="Border.LineStyle"/> and
    ///         <see cref="IAdornment.Thickness"/> atomically. Use <see cref="Border"/> directly for advanced
    ///         configuration (tab mode, gradient, custom thickness per side).
    ///     </para>
    ///     <para>
    ///         The adornments (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the
    ///         View's content and are not clipped by the View's Clip Area.
    ///     </para>
    ///     <para>
    ///         Changing the size of an adornment will change the size of <see cref="Frame"/> which will call
    ///         <see cref="SetNeedsLayout"/> to update the layout of the <see cref="SuperView"/> and its
    ///         <see cref="SubViews"/>.
    ///     </para>
    ///     <example>
    ///         Simple border:
    ///         <code>
    ///         view.BorderStyle = LineStyle.Single;
    ///         // Result:
    ///         // ┌┤Title├──┐
    ///         // │         │
    ///         // └─────────┘
    ///         </code>
    ///         Tab-style border:
    ///         <code>
    ///         view.BorderStyle = LineStyle.Rounded;
    ///         view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
    ///         view.Border.TabSide = Side.Top;
    ///         view.Border.Thickness = new Thickness (1, 3, 1, 1);
    ///         // Result (focused):
    ///         // ╭───╮
    ///         // │Tab│
    ///         // │   ╰───╮
    ///         // │content│
    ///         // ╰───────╯
    ///         </code>
    ///     </example>
    /// </remarks>
    public Border Border { get; } = new ();

    /// <summary>
    ///     Gets or sets the <see cref="LineStyle"/> used to draw a one-row/column-thick border around the view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a convenience helper for manipulating <see cref="Border"/>. Setting this property to any value
    ///         other than <see cref="LineStyle.None"/> sets <see cref="Border"/>.<see cref="IAdornment.Thickness"/>
    ///         to <c>1</c> (if currently zero) and <see cref="Border"/>.<see cref="Border.LineStyle"/> to the value.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see cref="LineStyle.None"/> (or <see langword="null"/>) sets
    ///         <see cref="Border"/>.<see cref="IAdornment.Thickness"/> to <c>0</c>.
    ///     </para>
    ///     <para>
    ///         Raises <see cref="OnBorderStyleChanged"/> and <see cref="BorderStyleChanged"/>.
    ///     </para>
    ///     <para>
    ///         For tab-style headers, gradient borders, or per-side thickness, configure <see cref="Border"/> directly.
    ///     </para>
    ///     <example>
    ///         <code>
    ///         // Single-line border: ┌┤Title├──┐
    ///         view.BorderStyle = LineStyle.Single;
    ///
    ///         // Rounded border:     ╭┤Title├──╮
    ///         view.BorderStyle = LineStyle.Rounded;
    ///
    ///         // Double-line border: ╔═Title═══╗
    ///         view.BorderStyle = LineStyle.Double;
    ///
    ///         // Remove border:
    ///         view.BorderStyle = LineStyle.None;
    ///         </code>
    ///     </example>
    /// </remarks>
    public LineStyle? BorderStyle
    {
        get => Border.LineStyle ?? null;
        set
        {
            SetBorderStyle (value);
            OnBorderStyleChanged ();
            BorderStyleChanged?.Invoke (this, EventArgs.Empty);
        }
    }

    /// <summary>
    ///     Called when the <see cref="BorderStyle"/> has changed.
    /// </summary>
    protected virtual bool OnBorderStyleChanged () => false;

    /// <summary>
    ///     Fired when the <see cref="BorderStyle"/> has changed.
    /// </summary>
    public event EventHandler<EventArgs>? BorderStyleChanged;

    /// <summary>
    ///     Sets the <see cref="BorderStyle"/> of the view to the specified value.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="BorderStyle"/> is a helper for manipulating the view's <see cref="Border"/>. Setting this property
    ///         to any value other
    ///         than <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s
    ///         <see cref="IAdornment.Thickness"/> to `1` and <see cref="BorderStyle"/> to the value.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s
    ///         <see cref="IAdornment.Thickness"/> to `0` and <see cref="BorderStyle"/> to <see cref="LineStyle.None"/>.
    ///     </para>
    ///     <para>For more advanced customization of the view's border, manipulate see <see cref="Border"/> directly.</para>
    /// </remarks>
    /// <param name="style"></param>
    internal void SetBorderStyle (LineStyle? style)
    {
        if (style is null or LineStyle.None)
        {
            Border.Thickness = new Thickness (0);
        }
        else
        {
            if (Border.Thickness == Thickness.Empty)
            {
                Border.Thickness = new Thickness (1);
            }
        }

        Border.LineStyle = style;

        SetNeedsLayout ();
    }

    /// <summary>
    ///     The <see cref="IAdornment"/> inside of the view that offsets the <see cref="Viewport"/>
    ///     from the <see cref="Border"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The adornments (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the
    ///         View's content and are not clipped by the View's Clip Area.
    ///     </para>
    ///     <para>
    ///         Changing the size of an adornment (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>) will
    ///         change the size of <see cref="Frame"/> which will call <see cref="SetNeedsLayout"/> to update the layout of the
    ///         <see cref="SuperView"/> and its <see cref="SubViews"/>.
    ///     </para>
    /// </remarks>
    public Padding Padding { get; } = new ();

    /// <summary>
    ///     <para>Gets the thickness describing the sum of the Adornments' thicknesses.</para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="Viewport"/> is offset from the <see cref="Frame"/> by the thickness returned by this method.
    ///     </para>
    /// </remarks>
    /// <returns>A thickness that describes the sum of the Adornments' thicknesses.</returns>
    public Thickness GetAdornmentsThickness ()
    {
        var result = Thickness.Empty;

        result += Margin.Thickness;
        result += Border.Thickness;
        result += Padding.Thickness;

        return result;
    }

    /// <summary>Sets the Frame's of the Margin, Border, and Padding.</summary>
    internal void SetAdornmentFrames ()
    {
        if (this is AdornmentView)
        {
            // AdornmentViews do not have Adornments
            return;
        }

        // Border and Padding dynamically update based on Margin's View's Frame changing
        Margin.View?.Frame = Frame with { Location = Point.Empty };
    }

    private Rectangle GetViewportForAdornmentThickness (IAdornment changedAdornment, Thickness changedThickness)
    {
        Thickness thickness = GetAdornmentsThicknessForAdornment (changedAdornment, changedThickness);

        return new Rectangle (_viewportLocation, GetViewportSizeForThickness (thickness));
    }

    private Rectangle GetViewportFrameForAdornmentThickness (IAdornment changedAdornment, Thickness changedThickness)
    {
        Thickness thickness = GetAdornmentsThicknessForAdornment (changedAdornment, changedThickness);

        return new Rectangle (new Point (thickness.Left, thickness.Top), GetViewportSizeForThickness (thickness));
    }

    private Size GetViewportSizeForThickness (Thickness thickness) =>
        new (Math.Max (0, Frame.Width - thickness.Horizontal), Math.Max (0, Frame.Height - thickness.Vertical));

    private Thickness GetAdornmentsThicknessForAdornment (IAdornment changedAdornment, Thickness changedThickness)
    {
        Thickness thickness = Thickness.Empty;

        thickness += ReferenceEquals (changedAdornment, Margin) ? changedThickness : Margin.Thickness;
        thickness += ReferenceEquals (changedAdornment, Border) ? changedThickness : Border.Thickness;
        thickness += ReferenceEquals (changedAdornment, Padding) ? changedThickness : Padding.Thickness;

        return thickness;
    }

    private void InvalidateAdornmentThicknessChange ()
    {
        if (SuperView is null)
        {
            NeedsClearScreenNextIteration ();

            return;
        }

        Rectangle invalidationViewport = Frame;
        Point superScroll = SuperView.Viewport.Location;
        invalidationViewport.Offset (-superScroll.X, -superScroll.Y);
        SuperView.SetNeedsDraw (invalidationViewport);
    }
}

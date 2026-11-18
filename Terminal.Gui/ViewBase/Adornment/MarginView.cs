namespace Terminal.Gui.ViewBase;

/// <summary>
///     The View-backed rendering layer for the Margin adornment.
///     Created lazily by <see cref="Margin"/> (via <see cref="AdornmentImpl.GetOrCreateView"/>)
///     when shadow sub-views or other View-level functionality is needed.
/// </summary>
/// <remarks>
///     <para>
///         The Margin is transparent by default. This can be overridden by explicitly setting <see cref="Scheme"/>.
///     </para>
///     <para>
///         Margins are drawn after all other Views in the application View hierarchy are drawn.
///     </para>
///     <para>
///         Margins have <see cref="ViewportSettingsFlags.Transparent"/> and
///         <see cref="ViewportSettingsFlags.TransparentMouse"/> enabled by default and are thus
///         transparent to the mouse. This can be overridden by explicitly setting <see cref="ViewportSettingsFlags"/>.
///     </para>
///     <para>See the <see cref="AdornmentView"/> class.</para>
/// </remarks>
public class MarginView : AdornmentView
{
    private const int PRESS_MOVE_HORIZONTAL = 1;
    private const int PRESS_MOVE_VERTICAL = 0;

    /// <inheritdoc/>
    public MarginView ()
    { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <inheritdoc/>
    public MarginView (Margin margin) : base (margin)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (margin == null)
        {
            // Supports AllViews_Tests_All_Constructors which uses reflection
            return;
        }
        SubViewLayout += MarginView_LayoutStarted;
        Adornment?.ThicknessChanged += OnThicknessChanged;

        // Margin should not be focusable
        CanFocus = false;

        // Margins are transparent by default
        ViewportSettings |= ViewportSettingsFlags.Transparent;

        // Margins are transparent to mouse by default
        ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

        if (margin.Parent is { })
        {
            Frame = margin.Parent.Frame with { Location = Point.Empty };
        }
        margin.ThicknessChanged += OnThicknessChanged;
    }

    /// <inheritdoc/>
    public override void OnParentFrameChanged (Rectangle newParentFrame) => Frame = newParentFrame with { Location = Point.Empty };

    private void OnThicknessChanged (object? sender, EventArgs e)
    {
        if (_isThicknessChanging || Adornment is null)
        {
            return;
        }
        _originalThickness = new Thickness (Adornment.Thickness.Left, Adornment.Thickness.Top, Adornment.Thickness.Right, Adornment.Thickness.Bottom);

        if (ShadowStyle is { })
        {
            SetShadow (ShadowStyle.Value);
        }
    }

    // When the Parent is drawn, we cache the clip region so we can draw the Margin after all other Views
    private Region? _cachedClip;

    internal Region? GetCachedClip () => _cachedClip;

    internal void ClearCachedClip () => _cachedClip = null;

    internal void CacheClip ()
    {
        if (Adornment?.Thickness != Thickness.Empty && ShadowStyle != ShadowStyles.None)
        {
            _cachedClip = GetClip ()?.Clone ();
        }
    }

    /// <summary>
    ///     INTERNAL API - Draws the transparent margins for the specified views. This is called from
    ///     <see cref="View.Draw(DrawContext)"/> on each
    ///     iteration of the main loop after all Views have been drawn.
    /// </summary>
    /// <remarks>
    ///     Non-transparent margins are drawn as-normal in <see cref="View.DrawAdornments"/>.
    /// </remarks>
    /// <param name="views"></param>
    /// <returns>
    ///     <see langword="true"/>
    /// </returns>
    internal static bool DrawMargins (IEnumerable<View> views)
    {
        Stack<View> stack = new (views);

        while (stack.Count > 0)
        {
            View view = stack.Pop ();

            if (view.Margin.View is MarginView marginView
                && view.Margin.Thickness != Thickness.Empty
                && marginView.ViewportSettings.FastHasFlags (ViewportSettingsFlags.Transparent)
                && marginView.GetCachedClip () != null)
            {
                marginView.SetNeedsDraw ();
                Region? saved = view.GetClip ();
                view.SetClip (marginView.GetCachedClip ());
                marginView.Draw ();
                view.SetClip (saved);
                marginView.ClearCachedClip ();
            }

            // Do not include Margin views of subviews; not supported
            foreach (View subview in view.GetSubViews (includePadding: true, includeBorder: true).OrderBy (v => v.ShadowStyle != ShadowStyles.None).Reverse ())
            {
                stack.Push (subview);
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override void BeginInit ()
    {
        base.BeginInit ();

        if (Adornment?.Parent is null)
        {
            return;
        }

        Adornment.Parent.MouseStateChanged += OnParentOnMouseStateChanged;
    }

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        if (Adornment is null || Adornment.Thickness == Thickness.Empty)
        {
            return true;
        }

        Rectangle screen = ViewportToScreen (Viewport);

        if (Diagnostics.HasFlag (ViewDiagnosticFlags.Thickness) || HasScheme)
        {
            // This just draws/clears the thickness, not the insides.
            Adornment!.Thickness.Draw (Driver, screen, Diagnostics, ToString ());
        }

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingText () => ViewportSettings.FastHasFlags (ViewportSettingsFlags.Transparent);

    #region Shadow

    private ShadowView? _bottomShadow;
    private ShadowView? _rightShadow;
    private bool _isThicknessChanging;
    private Thickness? _originalThickness;

    /// <summary>
    ///     Sets whether the Margin includes a shadow effect. The shadow is drawn on the right and bottom sides of the
    ///     Margin.
    /// </summary>
    public ShadowStyles? SetShadow (ShadowStyles? style)
    {
        bool hadShadow = _rightShadow is { } || _bottomShadow is { };

        if (_rightShadow is { })
        {
            Remove (_rightShadow);
            _rightShadow.Dispose ();
            _rightShadow = null;
        }

        if (_bottomShadow is { })
        {
            Remove (_bottomShadow);
            _bottomShadow.Dispose ();
            _bottomShadow = null;
        }

        _originalThickness ??= Adornment!.Thickness;

        if (hadShadow)
        {
            // Recover the original (pre-shadow) thickness by subtracting the old shadow size
            _originalThickness = new Thickness (Adornment!.Thickness.Left,
                                                Adornment!.Thickness.Top,
                                                Math.Max (Adornment!.Thickness.Right - ShadowSize.Width, 0),
                                                Math.Max (Adornment!.Thickness.Bottom - ShadowSize.Height, 0));
        }

        if (style is { })
        {
            // Turn on shadow
            _isThicknessChanging = true;

            Adornment!.Thickness = new Thickness (_originalThickness.Value.Left,
                                                  _originalThickness.Value.Top,
                                                  _originalThickness.Value.Right + ShadowSize.Width,
                                                  _originalThickness.Value.Bottom + ShadowSize.Height);
            _isThicknessChanging = false;
        }

        if (style is { })
        {
            _rightShadow = new ShadowView
            {
                X = Pos.AnchorEnd (ShadowSize.Width),
                Y = 0,
                Width = ShadowSize.Width,
                Height = Dim.Fill (),
                ShadowStyle = style,
                Orientation = Orientation.Vertical
            };

            _bottomShadow = new ShadowView
            {
                X = 0,
                Y = Pos.AnchorEnd (ShadowSize.Height),
                Width = Dim.Fill (),
                Height = ShadowSize.Height,
                ShadowStyle = style,
                Orientation = Orientation.Horizontal
            };
            Add (_rightShadow, _bottomShadow);
        }
        else if (Adornment!.Thickness != _originalThickness)
        {
            _isThicknessChanging = true;

            Adornment!.Thickness = new Thickness (_originalThickness.Value.Left,
                                                  _originalThickness.Value.Top,
                                                  _originalThickness.Value.Right,
                                                  _originalThickness.Value.Bottom);
            _isThicknessChanging = false;
        }

        return style;
    }

    /// <inheritdoc/>
    public override ShadowStyles? ShadowStyle
    {
        get => (Adornment as Margin)?.ShadowStyle ?? null;
        set => throw new InvalidOperationException ("The ShadowStyle of MarginView cannot be set");
    }

    /// <summary>
    ///     Gets or sets the size of the shadow effect.
    /// </summary>
    public Size ShadowSize
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            if (TryValidateShadowSize (field, value, out Size result))
            {
                field = value;
            }
            else
            {
                field = result;
            }

            if (ShadowStyle is { })
            {
                SetShadow (ShadowStyle);
            }
        }
    }

    private bool TryValidateShadowSize (Size originalValue, in Size newValue, out Size result)
    {
        result = newValue;

        var wasValid = true;

        if (newValue.Width < 0)
        {
            result = ShadowStyle is ShadowStyles.Opaque or ShadowStyles.Transparent ? result with { Width = 1 } : originalValue;

            wasValid = false;
        }

        if (newValue.Height < 0)
        {
            result = ShadowStyle is ShadowStyles.Opaque or ShadowStyles.Transparent ? result with { Height = 1 } : originalValue;

            wasValid = false;
        }

        if (!wasValid)
        {
            return false;
        }

        var wasUpdated = false;

        if ((ShadowStyle == ShadowStyles.Opaque && newValue.Width != 1) || (ShadowStyle == ShadowStyles.Transparent && newValue.Width < 1))
        {
            result = result with { Width = 1 };

            wasUpdated = true;
        }

        if ((ShadowStyle == ShadowStyles.Opaque && newValue.Height != 1) || (ShadowStyle == ShadowStyles.Transparent && newValue.Height < 1))
        {
            result = result with { Height = 1 };

            wasUpdated = true;
        }

        return !wasUpdated;
    }

    private void OnParentOnMouseStateChanged (object? sender, EventArgs<MouseState> args)
    {
        if (sender is not View parent || Adornment is null || Adornment.Thickness == Thickness.Empty || ShadowStyle == ShadowStyles.None)
        {
            return;
        }

        bool pressed = args.Value.FastHasFlags (MouseState.Pressed) && parent.MouseHighlightStates.FastHasFlags (MouseState.Pressed);
        bool pressedOutside = args.Value.FastHasFlags (MouseState.PressedOutside) && parent.MouseHighlightStates.FastHasFlags (MouseState.PressedOutside);

        if (pressedOutside)
        {
            pressed = false;
        }

        if (MouseState.FastHasFlags (MouseState.Pressed) && !pressed)
        {
            // If the view is pressed and the highlight is being removed, move the shadow back.
            // Note, for visual effects reasons, we only move horizontally.
            _isThicknessChanging = true;

            Adornment.Thickness = new Thickness (Adornment.Thickness.Left - PRESS_MOVE_HORIZONTAL,
                                                  Adornment.Thickness.Top - PRESS_MOVE_VERTICAL,
                                                  Adornment.Thickness.Right + PRESS_MOVE_HORIZONTAL,
                                                  Adornment.Thickness.Bottom + PRESS_MOVE_VERTICAL);
            _isThicknessChanging = false;

            _rightShadow?.Visible = true;

            _bottomShadow?.Visible = true;

            MouseState &= ~MouseState.Pressed;

            return;
        }

        if (MouseState.FastHasFlags (MouseState.Pressed) || !pressed)
        {
            return;
        }

        // If the view is not pressed, and we want highlight move the shadow
        // Note, for visual effects reasons, we only move horizontally.
        _isThicknessChanging = true;

        Adornment.Thickness = new Thickness (Adornment.Thickness.Left + PRESS_MOVE_HORIZONTAL,
                                              Adornment.Thickness.Top + PRESS_MOVE_VERTICAL,
                                              Adornment.Thickness.Right - PRESS_MOVE_HORIZONTAL,
                                              Adornment.Thickness.Bottom - PRESS_MOVE_VERTICAL);
        _isThicknessChanging = false;

        MouseState |= MouseState.Pressed;

        _rightShadow?.Visible = false;

        _bottomShadow?.Visible = false;
    }

    private void MarginView_LayoutStarted (object? sender, LayoutEventArgs e)
    {
        // Adjust the shadow such that it is drawn aligned with the Border
        if (_rightShadow is null || _bottomShadow is null || Adornment is null)
        {
            return;
        }

        switch (ShadowStyle)
        {
            case ShadowStyles.Transparent:
                _rightShadow.Y = Adornment.Parent?.Border.Thickness.Top > 0 ? ScreenToViewport (Adornment.Parent.Border.FrameToScreen ().Location).Y + 1 : 0;

                break;

            case ShadowStyles.Opaque:
                _rightShadow.Y = Adornment.Parent?.Border.Thickness.Top > 0 ? ScreenToViewport (Adornment.Parent.Border.FrameToScreen ().Location).Y + 1 : 0;
                _bottomShadow.X = Adornment.Parent?.Border.Thickness.Left > 0 ? ScreenToViewport (Adornment.Parent.Border.FrameToScreen ().Location).X + 1 : 0;

                break;

            case ShadowStyles.None:
            default:
                _rightShadow.Y = 0;
                _bottomShadow.X = 0;

                break;
        }
    }

    #endregion Shadow
}

using TitleViewType = Terminal.Gui.ViewBase.TitleView;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     The View-backed rendering, navigation, and arrangement layer for the <see cref="Border"/> adornment.
///     Created lazily by <see cref="Border"/> (via <see cref="AdornmentImpl.GetOrCreateView"/>)
///     when rendering, arrangement, or other View-level functionality is needed.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="BorderView"/> has two rendering code paths selected by <see cref="BorderSettings"/>:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>Legacy mode</b> (<see cref="BorderSettings.Tab"/> not set): Draws the border frame and
///                 inline title using <see cref="LineCanvas"/>. Title position is determined by
///                 <see cref="IAdornment.Thickness"/> on the title side (1 = inline, 2 = cap line, 3+ = enclosed
///                 rectangle).
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Tab mode</b> (<see cref="BorderSettings.Tab"/> set): Draws a content border frame and a separate
///                 tab header via a <see cref="TitleView"/> SubView. The TitleView has
///                 <see cref="View.SuperViewRendersLineCanvas"/> = <see langword="true"/>, so its border lines
///                 auto-join with the content border via <see cref="View.LineCanvas"/>.
///             </description>
///         </item>
///     </list>
///     <para>
///         Mouse and Keyboard-driven move/resize is handled by the <see cref="Arranger"/> (see
///         <see cref="View.Arrangement"/>
///         and the <see href="https://tui-cs.github.io/Terminal.Gui/docs/arrangement.html">Arrangement Deep Dive</see>).
///     </para>
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/borders.html"/> for the full deep dive.
///     </para>
/// </remarks>
/// <seealso cref="Border"/>
/// <seealso cref="BorderSettings"/>
/// <seealso cref="View.Arrangement"/>
public partial class BorderView : AdornmentView
{
    /// <inheritdoc/>
    public BorderView ()
    {
        SetupArrangeCommands ();
    }

    /// <inheritdoc/>
    public BorderView (Border border) : base (border)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (border == null)
        {
            // Supports AllViews_Tests_All_Constructors which uses reflection
            SetupArrangeCommands ();

            return;
        }

        SetupArrangeCommands ();

        CanFocus = false;
        TabStop = TabBehavior.TabStop;

        if (border.Parent is { })
        {
            Frame = border.Parent.Margin.Thickness.GetInside (border.Parent.Margin.GetFrame ());

            if (border.Parent is Runnable runnable)
            {
                runnable.TitleChanged += HandleParentTitleChanged;
                runnable.IsModalChanged += RunnableOnIsModalChanged;
            }
        }
        border.ThicknessChanged += OnThicknessChanged;
        border.Parent?.Margin.ThicknessChanged += OnThicknessChanged;
    }

    private void RunnableOnIsModalChanged (object? sender, EventArgs<bool> e) => TryUpdateTerminalTitle ();

    private void HandleParentTitleChanged (object? sender, EventArgs<string> e) => TryUpdateTerminalTitle ();


    /// <inheritdoc/>
    public override void BeginInit ()
    {
        base.BeginInit ();

        if (Adornment?.Parent is null)
        {
            return;
        }

        ShowHideDrawIndicator ();
        ConfigureForTabMode ();

        MouseHighlightStates |= Adornment.Parent.Arrangement != ViewArrangement.Fixed ? MouseState.Pressed : MouseState.None;
    }

    /// <inheritdoc/>
    public override void OnParentFrameChanged (Rectangle newParentFrame)
    {
        if (Adornment?.Parent is { })
        {
            Frame = Adornment.Parent.Margin.Thickness.GetInside (Adornment.Parent.Margin.GetFrame ());
        }
    }

    // TODO: Move DrawIndicator out of Border and into View
    private void OnThicknessChanged (object? sender, EventArgs e)
    {
        if (Adornment is { Parent: { } })
        {
            OnParentFrameChanged (Adornment.Parent.Frame);
        }

        if (IsInitialized)
        {
            ShowHideDrawIndicator ();
        }

        ConfigureForTabMode ();
    }

    /// <summary>
    ///     Called by <see cref="Border.Settings"/> setter when settings change.
    ///     Reconfigures tab mode state.
    /// </summary>
    internal void OnSettingsChanged ()
    {
        ConfigureForTabMode ();
        TryUpdateTerminalTitle ();
    }

    private void DisposeBorderTitleHooks ()
    {
        if (Adornment is not Border border)
        {
            return;
        }

        border.ThicknessChanged -= OnThicknessChanged;

        if (border.Parent is not { } parent)
        {
            return;
        }
        parent.Margin.ThicknessChanged -= OnThicknessChanged;

        if (parent is not Runnable runnable)
        {
            return;
        }
        runnable.TitleChanged -= HandleParentTitleChanged;
        runnable.IsModalChanged -= RunnableOnIsModalChanged;
    }

    /// <summary>
    ///     Emits an OSC terminal-title update when <see cref="BorderSettings.TerminalTitle"/> is enabled and the
    ///     parent Runnable becomes Modal.
    /// </summary>
    private void TryUpdateTerminalTitle ()
    {
        if (Adornment is not Border { Parent: { } parent } border)
        {
            return;
        }

        IDriver? driver = Driver;

        if (driver is null || !border.Settings.FastHasFlags (BorderSettings.TerminalTitle) || parent is not Runnable { IsModal: true } runnable)
        {
            return;
        }

        string strippedTitle = runnable.Title;

        if (runnable.TitleTextFormatter.HotKeyPos >= 0)
        {
            strippedTitle = runnable.Title.Remove (runnable.TitleTextFormatter.HotKeyPos, 1);
        }

        driver.SetTerminalTitle (strippedTitle);
    }

    private Rectangle GetBorderBounds ()
    {
        if (Adornment is null)
        {
            return ViewportToScreen (Viewport);
        }

        Rectangle screenRect = ViewportToScreen (Viewport);

        return new Rectangle (screenRect.X + Math.Max (0, Adornment.Thickness.Left - 1),
                              screenRect.Y + Math.Max (0, Adornment.Thickness.Top - 1),
                              Math.Max (0,
                                        screenRect.Width
                                        - Math.Max (0, Math.Max (0, Adornment.Thickness.Left - 1) + Math.Max (0, Adornment.Thickness.Right - 1))),
                              Math.Max (0,
                                        screenRect.Height
                                        - Math.Max (0, Math.Max (0, Adornment.Thickness.Top - 1) + Math.Max (0, Adornment.Thickness.Bottom - 1))));
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (Adornment is null || Adornment.Thickness == Thickness.Empty)
        {
            return true;
        }

        if (Adornment is not Border border)
        {
            throw new InvalidOperationException ("Adornment must be of type Border");
        }

        // Tab mode: completely separate codepath with edge-based border positioning
        if (border.Settings.FastHasFlags (BorderSettings.Tab))
        {
            return DrawTabBorder (border);
        }

        return DrawLegacyBorder (context, border);
    }

    /// <summary>
    ///     INTERNAL: Draws the border and title when <see cref="BorderSettings.Tab"/> is not set.
    ///     Retained for backward compatibility and for use when tab mode is not desired.
    /// </summary>
    private bool DrawLegacyBorder (DrawContext? context, Border border)
    {
        Rectangle screenBounds = ViewportToScreen (Viewport);

        Rectangle borderBounds = GetBorderBounds ();
        int topTitleLineY = borderBounds.Y;
        int titleY = borderBounds.Y;
        var titleBarsLength = 0;

        int maxTitleWidth = Math.Max (0,
                                      Math.Min (Adornment?.Parent?.TitleTextFormatter.FormatAndGetSize ().Width ?? 0,
                                                Math.Min (screenBounds.Width - 4, borderBounds.Width - 4)));

        Adornment?.Parent?.TitleTextFormatter.ConstrainToSize = new Size (maxTitleWidth, 1);

        int sideLineLength = borderBounds.Height;
        bool canDrawBorder = borderBounds is { Width: > 0, Height: > 0 };
        bool hasTitle = border.Settings.FastHasFlags (BorderSettings.Title);

        if (hasTitle)
        {
            switch (Adornment?.Thickness.Top)
            {
                case 2:
                    topTitleLineY = borderBounds.Y - 1;
                    titleY = topTitleLineY + 1;
                    titleBarsLength = 2;

                    break;

                case 3:
                    topTitleLineY = borderBounds.Y - (Adornment.Thickness.Top - 1);
                    titleY = topTitleLineY + 1;
                    titleBarsLength = 3;
                    sideLineLength++;

                    break;

                case > 3:
                    topTitleLineY = borderBounds.Y - 2;
                    titleY = topTitleLineY + 1;
                    titleBarsLength = 3;
                    sideLineLength++;

                    break;
            }
        }

        // Draw title text
        if (Driver is { }
            && Adornment?.Parent is { }
            && canDrawBorder
            && maxTitleWidth > 0
            && hasTitle
            && !string.IsNullOrEmpty (Adornment.Parent?.Title)
            && Adornment.Thickness.Top > 0)
        {
            Rectangle titleRect = new (borderBounds.X + 2, titleY, maxTitleWidth, 1);

            bool hasFocus = Adornment.Parent.HasFocus;

            if (Adornment.Parent.SuperView is null)
            {
                hasFocus = false;
            }

            Adornment.Parent.TitleTextFormatter.Draw (Driver,
                                                      titleRect,
                                                      GetAttributeForRole (hasFocus ? VisualRole.Focus : VisualRole.Normal),
                                                      GetAttributeForRole (hasFocus ? VisualRole.HotFocus : VisualRole.HotNormal));

            context?.AddDrawnRectangle (titleRect);
            Adornment.Parent?.LineCanvas.Exclude (new Region (titleRect));
        }

        if (!canDrawBorder || (Adornment as Border)?.LineStyle is null || (Adornment as Border)?.LineStyle == LineStyle.None)
        {
            return true;
        }

        LineCanvas? lc = Adornment.Parent?.LineCanvas;

        bool drawTop = Adornment.Thickness.Top > 0 && Frame is { Width: > 1, Height: >= 1 };
        bool drawLeft = Adornment.Thickness.Left > 0 && (Frame.Height > 1 || Adornment.Thickness.Top == 0);
        bool drawBottom = Adornment.Thickness.Bottom > 0 && Frame is { Width: > 1, Height: > 1 };
        bool drawRight = Adornment.Thickness.Right > 0 && (Frame.Height > 1 || Adornment.Thickness.Top == 0);

        Attribute normalAttribute = GetAttributeForRole (VisualRole.Normal);

#if TAB_COLOR_PROTOTYPE
        if (Adornment.Parent is TitleView titleView && Adornment.Parent is { })
        {
            normalAttribute = Adornment.Parent.GetAttributeForRole (VisualRole.Normal);
        }

        if (MouseState.FastHasFlags (MouseState.Pressed))
        {
            normalAttribute = GetAttributeForRole (VisualRole.Highlight);
        }
#endif

        SetAttribute (normalAttribute);

        if (drawTop)
        {
            if (borderBounds.Width < 4 || !hasTitle || string.IsNullOrEmpty (Adornment.Parent?.Title))
            {
                if (border.LineStyle is { })
                {
                    lc?.AddLine (borderBounds.Location with { Y = borderBounds.Y },
                                 borderBounds.Width,
                                 Orientation.Horizontal,
                                 border.LineStyle.Value,
                                 normalAttribute);
                }
            }
            else
            {
                // Title bar decoration
                if (Adornment.Thickness.Top == 2)
                {
                    lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY),
                                 Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 border.LineStyle ?? LineStyle.None,
                                 normalAttribute);
                }

                if (borderBounds.Width >= 4 && Adornment.Thickness.Top > 2)
                {
                    lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY),
                                 Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 border.LineStyle ?? LineStyle.None,
                                 normalAttribute);

                    lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY + 2),
                                 Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 border.LineStyle ?? LineStyle.None,
                                 normalAttribute);
                }

                lc?.AddLine (borderBounds.Location with { Y = titleY }, 2, Orientation.Horizontal, border.LineStyle ?? LineStyle.None, normalAttribute);

                lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY), titleBarsLength, Orientation.Vertical, LineStyle.Single, normalAttribute);

                lc?.AddLine (new Point (borderBounds.X + 1 + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2) - 1, topTitleLineY),
                             titleBarsLength,
                             Orientation.Vertical,
                             LineStyle.Single,
                             normalAttribute);

                lc?.AddLine (new Point (borderBounds.X + 1 + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2) - 1, titleY),
                             borderBounds.Width - Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                             Orientation.Horizontal,
                             border.LineStyle ?? LineStyle.None,
                             normalAttribute);
            }
        }

        if (drawLeft)
        {
            lc?.AddLine (borderBounds.Location with { Y = titleY }, sideLineLength, Orientation.Vertical, border.LineStyle ?? LineStyle.None, normalAttribute);
        }

        if (drawBottom)
        {
            lc?.AddLine (new Point (borderBounds.X, borderBounds.Y + borderBounds.Height - 1),
                         borderBounds.Width,
                         Orientation.Horizontal,
                         border.LineStyle ?? LineStyle.None,
                         normalAttribute);
        }

        if (drawRight)
        {
            lc?.AddLine (new Point (borderBounds.X + borderBounds.Width - 1, titleY),
                         sideLineLength,
                         Orientation.Vertical,
                         border.LineStyle ?? LineStyle.None,
                         normalAttribute);
        }

        // TODO: This should be moved to LineCanvas as a new BorderStyle.Ruler
        if (Diagnostics.HasFlag (ViewDiagnosticFlags.Ruler))
        {
            var hRuler = new Ruler { Length = screenBounds.Width, Orientation = Orientation.Horizontal };

            if (drawTop)
            {
                hRuler.Draw (Driver, new Point (screenBounds.X, screenBounds.Y));
            }

            if (drawTop && maxTitleWidth > 0 && border.Settings.FastHasFlags (BorderSettings.Title))
            {
                Adornment.Parent?.TitleTextFormatter.Draw (Driver,
                                                           new Rectangle (borderBounds.X + 2, titleY, maxTitleWidth, 1),
                                                           Adornment.Parent.HasFocus
                                                               ? Adornment.Parent.GetAttributeForRole (VisualRole.Focus)
                                                               : Adornment.Parent.GetAttributeForRole (VisualRole.Normal),
                                                           Adornment.Parent.HasFocus
                                                               ? Adornment.Parent.GetAttributeForRole (VisualRole.Focus)
                                                               : Adornment.Parent.GetAttributeForRole (VisualRole.Normal));
            }

            var vRuler = new Ruler { Length = screenBounds.Height - 2, Orientation = Orientation.Vertical };

            if (drawLeft)
            {
                vRuler.Draw (Driver, new Point (screenBounds.X, screenBounds.Y + 1), 1);
            }

            if (drawBottom)
            {
                hRuler.Draw (Driver, new Point (screenBounds.X, screenBounds.Y + screenBounds.Height - 1));
            }

            if (drawRight)
            {
                vRuler.Draw (Driver, new Point (screenBounds.X + screenBounds.Width - 1, screenBounds.Y + 1), 1);
            }
        }

        if (lc is { } && border.Settings.FastHasFlags (BorderSettings.Gradient))
        {
            if (_cachedGradientFill is null || _cachedGradientRect != screenBounds)
            {
                SetupGradientLineCanvas (lc, screenBounds);
            }
            else
            {
                lc.Fill = _cachedGradientFill;
            }
        }
        else
        {
            lc?.Fill = null;
            _cachedGradientFill = null;
        }

        return true;
    }

    #region Border.Settings.Tab Support

    /// <summary>
    ///     Gets or sets which side the tab header protrudes from. Defaults to <see cref="Side.Top"/>.
    ///     Only meaningful when <see cref="Border.Settings"/> includes <see cref="BorderSettings.Tab"/>.
    /// </summary>
    /// <remarks>
    ///     For <see cref="Side.Left"/> and <see cref="Side.Right"/>, the title text renders vertically.
    /// </remarks>
    public Side TabSide
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            Adornment?.Parent?.SetNeedsLayout ();
        }
    } = Side.Top;

    /// <summary>
    ///     Gets or sets the offset along the border edge where the tab header starts (columns for
    ///     <see cref="Side.Top"/>/<see cref="Side.Bottom"/>, rows for <see cref="Side.Left"/>/<see cref="Side.Right"/>).
    ///     Only meaningful when <see cref="Border.Settings"/> includes <see cref="BorderSettings.Tab"/>.
    /// </summary>
    /// <remarks>
    ///     Can be positive (shifted right/down), zero (at the start), or negative (shifted left/up,
    ///     partially off-screen). The <see cref="TitleView"/> is clipped automatically by the View system's
    ///     natural viewport clipping.
    /// </remarks>
    public int TabOffset
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            Adornment?.Parent?.SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     Gets or sets the total length of the tab header parallel to the border edge (including border cells).
    ///     If <see langword="null"/>, the length is auto-computed from the <see cref="View.Title"/> width plus the
    ///     <see cref="TitleView"/>'s border cells. Only meaningful when <see cref="Border.Settings"/> includes
    ///     <see cref="BorderSettings.Tab"/>.
    /// </summary>
    public int? TabLength
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            Adornment?.Parent?.SetNeedsLayout ();
        }
    }

    private bool _tabModeSetTransparent;

    /// <summary>
    ///     Configures persistent state for tab mode. Called when <see cref="Border.Settings"/> or
    ///     <see cref="IAdornment.Thickness"/> changes. Sets <see cref="View.ViewportSettings"/> and
    ///     ensures the <see cref="TitleView"/> SubView exists with the correct static properties.
    /// </summary>
    private void ConfigureForTabMode ()
    {
        if (Adornment is not Border border)
        {
            return;
        }

        if (border.Settings.FastHasFlags (BorderSettings.Tab))
        {
            ViewportSettings |= ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse;
            _tabModeSetTransparent = true;

            EnsureTitleView ();

            if (TitleView is not ITitleView itv)
            {
                return;
            }
            itv.TabSide = TabSide;
            itv.TabDepth = GetTabDepth ();
        }
        else
        {
            // Only clear flags if we set them for tab mode
            if (_tabModeSetTransparent)
            {
                ViewportSettings &= ~(ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse);
                _tabModeSetTransparent = false;
            }

            TitleView?.Visible = false;
        }
    }

#if TAB_COLOR_PROTOTYPE
    /// <inheritdoc />
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (base.OnGettingAttributeForRole (in role, ref currentAttribute))
        {
            return true;
        }

        if (Adornment is not Border border || !border.Settings.FastHasFlags (BorderSettings.Tab))
        {
            return false;
        }

        return false;
    }
#endif

    /// <inheritdoc/>
    protected override void OnSubViewLayout (LayoutEventArgs args) => UpdateTitleViewLayout ();

    /// <summary>
    ///     Delegates layout of the <see cref="TitleView"/> to the <see cref="ITitleView"/> implementation.
    ///     Called via <see cref="View.SubViewLayout"/>.
    /// </summary>
    private void UpdateTitleViewLayout ()
    {
        if (Adornment is not Border border || !border.Settings.FastHasFlags (BorderSettings.Tab))
        {
            return;
        }

        if (_titleView is not ITitleView itv)
        {
            return;
        }

        // Ensure stored state is current before layout
        itv.TabSide = TabSide;
        itv.TabDepth = GetTabDepth ();

        itv.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = GetTabBorderBounds (),
            TabOffset = TabOffset,
            TabLengthOverride = TabLength,
            HasFocus = IsFocusedOrLastTab (),
            LineStyle = border.LineStyle,
            Title = Adornment.Parent?.Title ?? string.Empty,
            ScreenOrigin = ViewportToScreen (Point.Empty)
        });
    }

    /// <summary>
    ///     Computes the content border rectangle in screen coordinates when <see cref="BorderSettings.Tab"/> is set.
    ///     Non-title sides use the outer edge of the thickness. The title side uses <c>thickness - 1</c>
    ///     from the outer edge, leaving a tab header region between the outer edge and the content border line.
    /// </summary>
    private Rectangle GetTabBorderBounds ()
    {
        Rectangle screenRect = ViewportToScreen (Viewport);

        if (Adornment is null)
        {
            return screenRect;
        }

        int left = screenRect.X;
        int top = screenRect.Y;
        int right = screenRect.Right;
        int bottom = screenRect.Bottom;

        // Title side: content border at thickness - 1 from outer edge
        switch (TabSide)
        {
            case Side.Top:
                top += Math.Max (0, Adornment.Thickness.Top - 1);

                break;

            case Side.Bottom:
                bottom -= Math.Max (0, Adornment.Thickness.Bottom - 1);

                break;

            case Side.Left:
                left += Math.Max (0, Adornment.Thickness.Left - 1);

                break;

            case Side.Right:
                right -= Math.Max (0, Adornment.Thickness.Right - 1);

                break;
        }

        return new Rectangle (left, top, Math.Max (0, right - left), Math.Max (0, bottom - top));
    }

    private TitleView? _titleView;

    /// <summary>
    ///     Gets the tab header <see cref="ViewBase.TitleView"/> SubView, or <see langword="null"/> if
    ///     <see cref="BorderSettings.Tab"/> is not set or the view has not yet been created.
    /// </summary>
    /// <remarks>
    ///     The <see cref="ViewBase.TitleView"/> is created lazily by <see cref="EnsureTitleView"/> when
    ///     <see cref="BorderSettings.Tab"/> is first set. It can be used to hook mouse events for
    ///     custom behaviors such as drag-to-slide tab reordering.
    /// </remarks>
    public View? TitleView => _titleView;

    /// <summary>
    ///     Gets the effective tab length — either the explicit <see cref="TabLength"/> or
    ///     the <see cref="ITitleView.MeasuredTabLength"/> from the laid-out <see cref="TitleView"/>.
    ///     Returns 0 if no <see cref="TitleView"/> exists yet.
    /// </summary>
    public int EffectiveTabLength
    {
        get
        {
            if (Adornment is not Border _)
            {
                return 0;
            }

            if (TabLength is { } explicitLength)
            {
                return explicitLength;
            }

            if (TitleView is not (ITitleView itv and View tv))
            {
                return 0;
            }

            if (itv.MeasuredTabLength > 0)
            {
                return itv.MeasuredTabLength;
            }

            // TitleView hasn't been laid out yet — set text and orientation, then measure.
            tv.Text = Adornment?.Parent?.Title ?? string.Empty;
            itv.Orientation = TabSide is Side.Left or Side.Right ? Orientation.Vertical : Orientation.Horizontal;

            int measured = TabSide is Side.Top or Side.Bottom ? tv.GetAutoWidth () : tv.GetAutoHeight ();
            itv.MeasuredTabLength = measured;

            return measured;
        }
    }

    /// <summary>
    ///     Gets or lazily creates the <see cref="TitleView"/> SubView used to render the tab header.
    ///     The view has its own border with <see cref="View.SuperViewRendersLineCanvas"/> = true,
    ///     so its border lines auto-join with the View's content border via <see cref="View.LineCanvas"/>.
    /// </summary>
    private void EnsureTitleView ()
    {
        if (TitleView is { })
        {
            return;
        }

        _titleView = new TitleView
        {
#if DEBUG
            Id = "TitleView",
#endif
        };
        Add (TitleView);
    }

    private int GetTabDepth ()
    {
        if (Adornment is null)
        {
            return 0;
        }

        return TabSide switch
        {
            Side.Top => Adornment.Thickness.Top,
            Side.Bottom => Adornment.Thickness.Bottom,
            Side.Left => Adornment.Thickness.Left,
            Side.Right => Adornment.Thickness.Right,
            _ => 3
        };
    }

    /// <summary>
    ///     Draws the border and tab header when <see cref="BorderSettings.Tab"/> is set.
    ///     Uses a <see cref="TitleView"/> SubView with its own border and
    ///     <see cref="View.SuperViewRendersLineCanvas"/> = true for the tab header.
    ///     The TitleView's border lines auto-join with the content border via <see cref="View.LineCanvas"/>.
    /// </summary>
    private bool DrawTabBorder (Border border)
    {
        if (Adornment?.Parent is null || Driver is null)
        {
            return true;
        }

        Rectangle screenBounds = ViewportToScreen (Viewport);
        Rectangle borderBounds = GetTabBorderBounds ();

        if (borderBounds is not { Width: > 0, Height: > 0 })
        {
            return true;
        }

        if (border.LineStyle is null or LineStyle.None)
        {
            return true;
        }

        Attribute normalAttribute = GetAttributeForRole (VisualRole.Normal);

        if (MouseState.FastHasFlags (MouseState.Pressed))
        {
            normalAttribute = GetAttributeForRole (VisualRole.Highlight);
        }

        SetAttribute (normalAttribute);

        LineCanvas? lc = Adornment.Parent?.LineCanvas;

        if (lc is null)
        {
            return true;
        }

        int tabDepth = GetTabDepth ();

        int effectiveTabLength = EffectiveTabLength;

        if (effectiveTabLength > 0)
        {
            LineStyle lineStyle = border.LineStyle.Value;
            bool hasFocus = IsFocusedOrLastTab ();

            // Compute tab header geometry
            Rectangle headerRect = TitleViewType.ComputeHeaderRect (borderBounds, TabSide, TabOffset, effectiveTabLength, tabDepth);
            Rectangle viewBounds = TitleViewType.ComputeViewBounds (borderBounds, TabSide, tabDepth);
            Rectangle clipped = Rectangle.Intersect (headerRect, viewBounds);
            bool tabVisible = !clipped.IsEmpty;

            // Draw the 3 non-tab-side content border lines (always drawn).
            // The tab-side line is handled below — conditionally, based on whether the tab is visible.
            if (Adornment.Thickness.Top > 0 && (TabSide != Side.Top || !tabVisible))
            {
                lc.AddLine (new Point (borderBounds.X, borderBounds.Y), borderBounds.Width, Orientation.Horizontal, lineStyle, normalAttribute);
            }

            if (Adornment.Thickness.Bottom > 0 && (TabSide != Side.Bottom || !tabVisible))
            {
                lc.AddLine (new Point (borderBounds.X, borderBounds.Bottom - 1), borderBounds.Width, Orientation.Horizontal, lineStyle, normalAttribute);
            }

            if (Adornment.Thickness.Left > 0 && (TabSide != Side.Left || !tabVisible))
            {
                lc.AddLine (new Point (borderBounds.X, borderBounds.Y), borderBounds.Height, Orientation.Vertical, lineStyle, normalAttribute);
            }

            if (Adornment.Thickness.Right > 0 && (TabSide != Side.Right || !tabVisible))
            {
                lc.AddLine (new Point (borderBounds.Right - 1, borderBounds.Y), borderBounds.Height, Orientation.Vertical, lineStyle, normalAttribute);
            }

            // Draw the tab-side content border (gap segments around the tab)
            if (tabVisible)
            {
                AddTabSideContentBorder (lc,
                                         clipped,
                                         headerRect,
                                         borderBounds,
                                         TabSide,
                                         hasFocus,
                                         tabDepth,
                                         lineStyle,
                                         normalAttribute);
            }
        }

        // Gradient support
        if (border.Settings.FastHasFlags (BorderSettings.Gradient))
        {
            if (_cachedGradientFill is null || _cachedGradientRect != screenBounds)
            {
                SetupGradientLineCanvas (lc, screenBounds);
            }
            else
            {
                lc.Fill = _cachedGradientFill;
            }
        }
        else
        {
            lc.Fill = null;
            _cachedGradientFill = null;
        }

        return true;
    }

    private bool IsFocusedOrLastTab ()
    {
        if (Adornment is not Border border || !border.Settings.FastHasFlags (BorderSettings.Tab))
        {
            return false;
        }

        // If the Parent is in a Tabs container, and it is the last subview of its SuperView, treat it as though it is focused
        if (border.Parent is { SuperView: Tabs } tab && tab.SuperView?.SubViews.LastOrDefault () == tab)
        {
            return true;
        }

        return border.Parent?.HasFocus ?? false;
    }

    /// <summary>
    ///     When in tab mode, if a command is not handled by the TitleView, bubble it to the SuperView (e.g. Tabs);
    ///     this enables keyboard navigation commands to be handled by the Tabs container when the TitleView has focus.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    protected override bool OnCommandNotBound (CommandEventArgs args)
    {
        if (base.OnCommandNotBound (args))
        {
            return true;
        }

        if (Adornment is not Border border || !border.Settings.FastHasFlags (BorderSettings.Tab))
        {
            return false;
        }

        if (args.Context.TryGetSource (out View? view) && view is TitleView && args.Context is { })
        {
            return border.Parent?.SuperView?.InvokeCommand (args.Context.Command, args.Context) is true;
        }

        return false;
    }

    /// <summary>
    ///     Draws the tab-side content border line. For focused depth ≥ 3, draws split segments
    ///     around the gap. For unfocused depth ≥ 3, draws the full line (auto-join creates junctions).
    ///     For depth &lt; 3, draws the full line.
    /// </summary>
    private static void AddTabSideContentBorder (LineCanvas lc,
                                                 Rectangle clipped,
                                                 Rectangle headerRect,
                                                 Rectangle contentBorderRect,
                                                 Side side,
                                                 bool hasFocus,
                                                 int depth,
                                                 LineStyle lineStyle,
                                                 Attribute? attribute)
    {
        // Open gap when: focused at depth ≥ 3 (no content-side border on tab), or
        // depth < 3 (content border coincides with tab title row — must not overwrite it).
        bool openGap = (hasFocus && depth >= 3) || depth < 3;

        switch (side)
        {
            case Side.Top:
                {
                    int borderY = contentBorderRect.Y;

                    if (!openGap)
                    {
                        lc.AddLine (new Point (contentBorderRect.X, borderY), contentBorderRect.Width, Orientation.Horizontal, lineStyle, attribute);
                    }
                    else
                    {
                        // Reserve the gap cells so overlapped compositing suppresses
                        // lower-Z views' content border lines at these positions.
                        int gapStart = clipped.X + 1;
                        int gapEnd = clipped.Right - 1;

                        if (gapEnd > gapStart)
                        {
                            lc.Reserve (new Rectangle (gapStart, borderY, gapEnd - gapStart, 1));
                        }

                        if (clipped.X > contentBorderRect.X)
                        {
                            lc.AddLine (new Point (contentBorderRect.X, borderY),
                                        clipped.X - contentBorderRect.X + 1,
                                        Orientation.Horizontal,
                                        lineStyle,
                                        attribute);
                        }

                        if (clipped.Right - 1 < contentBorderRect.Right - 1)
                        {
                            lc.AddLine (new Point (clipped.Right - 1, borderY),
                                        contentBorderRect.Right - (clipped.Right - 1),
                                        Orientation.Horizontal,
                                        lineStyle,
                                        attribute);
                        }
                    }

                    break;
                }

            case Side.Bottom:
                {
                    int borderY = contentBorderRect.Bottom - 1;

                    if (!openGap)
                    {
                        lc.AddLine (new Point (contentBorderRect.X, borderY), contentBorderRect.Width, Orientation.Horizontal, lineStyle, attribute);
                    }
                    else
                    {
                        int gapStart = clipped.X + 1;
                        int gapEnd = clipped.Right - 1;

                        if (gapEnd > gapStart)
                        {
                            lc.Reserve (new Rectangle (gapStart, borderY, gapEnd - gapStart, 1));
                        }

                        if (clipped.X > contentBorderRect.X)
                        {
                            lc.AddLine (new Point (contentBorderRect.X, borderY),
                                        clipped.X - contentBorderRect.X + 1,
                                        Orientation.Horizontal,
                                        lineStyle,
                                        attribute);
                        }

                        if (clipped.Right - 1 < contentBorderRect.Right - 1)
                        {
                            lc.AddLine (new Point (clipped.Right - 1, borderY),
                                        contentBorderRect.Right - (clipped.Right - 1),
                                        Orientation.Horizontal,
                                        lineStyle,
                                        attribute);
                        }
                    }

                    break;
                }

            case Side.Left:
                {
                    int borderX = contentBorderRect.X;

                    if (!openGap)
                    {
                        lc.AddLine (new Point (borderX, contentBorderRect.Y), contentBorderRect.Height, Orientation.Vertical, lineStyle, attribute);
                    }
                    else
                    {
                        int gapStart = clipped.Y + 1;
                        int gapEnd = clipped.Bottom - 1;

                        if (gapEnd > gapStart)
                        {
                            lc.Reserve (new Rectangle (borderX, gapStart, 1, gapEnd - gapStart));
                        }

                        if (clipped.Y > contentBorderRect.Y)
                        {
                            lc.AddLine (new Point (borderX, contentBorderRect.Y), clipped.Y - contentBorderRect.Y + 1, Orientation.Vertical, lineStyle, attribute);
                        }
                        else if (clipped.Y > headerRect.Y)
                        {
                            // Header clipped at top (overflow) — suppress corner glyph
                            lc.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Y, 1, 1)));
                        }

                        if (clipped.Bottom - 1 < contentBorderRect.Bottom - 1)
                        {
                            lc.AddLine (new Point (borderX, clipped.Bottom - 1),
                                        contentBorderRect.Bottom - (clipped.Bottom - 1),
                                        Orientation.Vertical,
                                        lineStyle,
                                        attribute);
                        }
                        else if (clipped.Bottom < headerRect.Bottom)
                        {
                            // Header clipped at bottom (overflow) — suppress corner glyph
                            lc.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Bottom - 1, 1, 1)));
                        }
                    }

                    break;
                }

            case Side.Right:
                {
                    int borderX = contentBorderRect.Right - 1;

                    if (!openGap)
                    {
                        lc.AddLine (new Point (borderX, contentBorderRect.Y), contentBorderRect.Height, Orientation.Vertical, lineStyle, attribute);
                    }
                    else
                    {
                        int gapStart = clipped.Y + 1;
                        int gapEnd = clipped.Bottom - 1;

                        if (gapEnd > gapStart)
                        {
                            lc.Reserve (new Rectangle (borderX, gapStart, 1, gapEnd - gapStart));
                        }

                        if (clipped.Y > contentBorderRect.Y)
                        {
                            lc.AddLine (new Point (borderX, contentBorderRect.Y), clipped.Y - contentBorderRect.Y + 1, Orientation.Vertical, lineStyle, attribute);
                        }
                        else if (clipped.Y > headerRect.Y)
                        {
                            // Header clipped at top (overflow) — suppress corner glyph
                            lc.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Y, 1, 1)));
                        }

                        if (clipped.Bottom - 1 < contentBorderRect.Bottom - 1)
                        {
                            lc.AddLine (new Point (borderX, clipped.Bottom - 1),
                                        contentBorderRect.Bottom - (clipped.Bottom - 1),
                                        Orientation.Vertical,
                                        lineStyle,
                                        attribute);
                        }
                        else if (clipped.Bottom < headerRect.Bottom)
                        {
                            // Header clipped at bottom (overflow) — suppress corner glyph
                            lc.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Bottom - 1, 1, 1)));
                        }
                    }

                    break;
                }

            default: throw new ArgumentOutOfRangeException (nameof (side), side, null);
        }
    }

    #endregion Border.Settings.Tab Support

    #region DrawIndicator Support

    /// <summary>
    ///     Gets the <see cref="SpinnerView"/> SubView used to render <see cref="ViewDiagnosticFlags.DrawIndicator"/>,
    ///     or <see langword="null"/> if the diagnostic flag is not set or the border has zero thickness.
    /// </summary>
    public SpinnerView? DrawIndicator { get; private set; }

    private void ShowHideDrawIndicator ()
    {
        if (View.Diagnostics.HasFlag (ViewDiagnosticFlags.DrawIndicator) && Adornment?.Thickness != Thickness.Empty)
        {
            if (DrawIndicator is { })
            {
                return;
            }

            DrawIndicator = new SpinnerView
            {
#if DEBUG
                Id = "DrawIndicator",
#endif
                X = 1,
                Style = new SpinnerStyle.Dots2 (),
                SpinDelay = 0,
                Visible = false
            };
            Add (DrawIndicator);
        }
        else if (DrawIndicator is { })
        {
            Remove (DrawIndicator);
            DrawIndicator?.Dispose ();
            DrawIndicator = null;
        }
    }

    internal void AdvanceDrawIndicator ()
    {
        if (!View.Diagnostics.HasFlag (ViewDiagnosticFlags.DrawIndicator) || DrawIndicator is null)
        {
            return;
        }
        DrawIndicator.AdvanceAnimation (false);
        DrawIndicator.Render ();
    }

    #endregion DrawIndicator Support

    #region Gradient Support

    private FillPair? _cachedGradientFill;
    private Rectangle _cachedGradientRect;

    private void SetupGradientLineCanvas (LineCanvas lc, Rectangle rect)
    {
        GetAppealingGradientColors (out List<Color> stops, out List<int> steps);

        Gradient g = new (stops, steps);

        GradientFill fore = new (rect, g, GradientDirection.Diagonal);
        SolidFill back = new (GetAttributeForRole (VisualRole.Normal).Background);

        _cachedGradientFill = new FillPair (fore, back);
        _cachedGradientRect = rect;
        lc.Fill = _cachedGradientFill;
    }

    private static void GetAppealingGradientColors (out List<Color> stops, out List<int> steps)
    {
        stops =
        [
            new Color (0, 128, 255), // Bright Blue
            new Color (0, 255, 128), // Bright Green
            new Color (255, 255), // Bright Yellow
            new Color (255, 128), // Bright Orange
            new Color (255, 0, 128)
        ];

        steps = [15];
    }

    #endregion Gradient Support


    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            DisposeBorderTitleHooks ();
            DisposeArranger ();
        }

        base.Dispose (disposing);
    }

}

namespace Terminal.Gui.ViewBase;

/// <summary>
///     The lightweight Margin settings for a <see cref="View"/>. Accessed via <see cref="View.Margin"/>.
///     The underlying <see cref="MarginView"/> is created lazily when shadows, sub-views, or other
///     View-level functionality is needed.
/// </summary>
/// <remarks>
///     <para>
///         The Margin is transparent by default. This can be overridden by setting a custom
///         <see cref="Scheme"/> on the <see cref="AdornmentImpl.View"/> via <see cref="AdornmentImpl.GetOrCreateView"/>.
///     </para>
///     <para>
///         Margins are drawn after all other Views in the application View hierarchy are drawn.
///     </para>
///     <para>
///         Margins have <see cref="ViewportSettingsFlags.Transparent"/> and
///         <see cref="ViewportSettingsFlags.TransparentMouse"/> enabled by default and are thus
///         transparent to the mouse. This can be overridden by explicitly setting <see cref="ViewportSettingsFlags"/>.
///     </para>
/// </remarks>
public class Margin : AdornmentImpl
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Margin"/> class with default settings. By default, Margins are
    ///     transparent and transparent to mouse events (i.e., they don't block mouse events from reaching underlying views).
    ///     This can be overridden by explicitly setting <see cref="ViewportSettingsFlags"/>.
    /// </summary>
    public Margin () => ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse;

    /// <inheritdoc/>
    protected override AdornmentView CreateView ()
    {
        MarginView mv = new (this);

        return mv;
    }

    /// <inheritdoc/>
    public override Rectangle GetFrame () => Parent is { } ? Parent.Frame with { Location = Point.Empty } : Rectangle.Empty;

    /// <inheritdoc/>
    protected override void OnThicknessChanged ()
    {
        base.OnThicknessChanged ();

        if (Thickness == Thickness.Empty)
        { }

        //GetOrCreateView ();
    }

    /// <summary>
    ///     Shadow effect. Setting to anything other than <see cref="ShadowStyles.None"/> forces a <see cref="MarginView"/>
    ///     to be created so the shadow sub-views can be hosted.
    /// </summary>
    public ShadowStyles? ShadowStyle
    {
        get => field;
        set
        {
            if (field == value)
            {
                return;
            }
            field = value;

            if (field is null)
            {
                // null means no shadow and no thickness
                if (View is MarginView mv)
                {
                    mv.SetShadow (null);
                    mv.ShadowSize = Size.Empty;
                }

                return;
            }

            if (GetOrCreateView () is not MarginView marginView)
            {
                return;
            }

            switch (field)
            {
                case ShadowStyles.Opaque:
                case ShadowStyles.Transparent when marginView.ShadowSize.Width == 0 || marginView.ShadowSize.Height == 0:
                    {
                        if (marginView.ShadowSize.Width != 1)
                        {
                            marginView.ShadowSize = marginView.ShadowSize with { Width = 1 };
                        }

                        if (marginView.ShadowSize.Height != 1)
                        {
                            marginView.ShadowSize = marginView.ShadowSize with { Height = 1 };
                        }

                        break;
                    }
            }

            // Always call SetShadow to update thickness and shadow views
            marginView.SetShadow (field);
        }
    }

}

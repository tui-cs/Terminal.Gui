namespace Terminal.Gui.ViewBase;

/// <summary>
///     Draws a shadow on the right or bottom of the view. Used internally by <see cref="Margin"/>.
/// </summary>
internal class ShadowView : View
{
    /// <inheritdoc/>
    protected override bool OnDrawingText () => true;

    /// <inheritdoc/>
    protected override bool OnClearingViewport () =>

        // Prevent clearing (so we can have transparency)
        true;

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        switch (ShadowStyle)
        {
            case ShadowStyles.Opaque:
                if (Orientation == Orientation.Vertical)
                {
                    DrawVerticalShadowOpaque (Viewport);
                }
                else
                {
                    DrawHorizontalShadowOpaque (Viewport);
                }

                break;

            case ShadowStyles.Transparent:
                if (Orientation == Orientation.Vertical)
                {
                    DrawVerticalShadowTransparent (Viewport);
                }
                else
                {
                    DrawHorizontalShadowTransparent ();
                }

                break;
        }

        // Report the drawn region so that Margin's CachedDrawnRegion includes shadow cells.
        context?.AddDrawnRectangle (ViewportToScreen (Viewport));

        return true;
    }

    /// <summary>
    ///     Gets or sets the orientation of the shadow.
    /// </summary>
    public Orientation Orientation { get; set; }

    public override ShadowStyles? ShadowStyle
    {
        get;
        set
        {
            Visible = value is not null;
            field = value;

            ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
        }
    }

    private void DrawHorizontalShadowOpaque (Rectangle rectangle)
    {
        // Draw the start glyph
        SetAttribute (GetAttributeUnderLocation (ViewportToScreen (new Point (0, 0))));
        AddRune (0, 0, Glyphs.ShadowHorizontalStart);

        // Fill the rest of the rectangle with the glyph - note we skip the last since vertical will draw it
        for (var i = 1; i < rectangle.Width - 1; i++)
        {
            SetAttribute (GetAttributeUnderLocation (ViewportToScreen (new Point (i, 0))));
            AddRune (i, 0, Glyphs.ShadowHorizontal);
        }

        // Last is special
        SetAttribute (GetAttributeUnderLocation (ViewportToScreen (new Point (rectangle.Width - 1, 0))));
        AddRune (rectangle.Width - 1, 0, Glyphs.ShadowHorizontalEnd);
    }

    private void DrawHorizontalShadowTransparent ()
    {
        Rectangle screen = ViewportToScreen (Viewport);

        for (int r = Math.Max (0, screen.Y); r < screen.Y + screen.Height; r++)
        {
            for (int c = Math.Max (0, screen.X + 1); c < screen.X + screen.Width; c++)
            {
                Driver?.Move (c, r);
                SetAttribute (GetAttributeUnderLocation (new Point (c, r)));

                if (!(c < ScreenContents?.GetLength (1)) || !(r < ScreenContents?.GetLength (0)))
                {
                    continue;
                }
                string grapheme = ScreenContents [r, c].Grapheme;
                AddStr (grapheme);

                if (grapheme.GetColumns () > 1)
                {
                    c++;
                }
            }
        }
    }

    private void DrawVerticalShadowOpaque (Rectangle viewport)
    {
        // Draw the start glyph
        SetAttribute (GetAttributeUnderLocation (ViewportToScreen (new Point (0, 0))));
        AddRune (0, 0, Glyphs.ShadowVerticalStart);

        // Fill the rest of the rectangle with the glyph
        for (var i = 1; i < viewport.Height - 1; i++)
        {
            SetAttribute (GetAttributeUnderLocation (ViewportToScreen (new Point (0, i))));
            AddRune (0, i, Glyphs.ShadowVertical);
        }
    }

    private void DrawVerticalShadowTransparent (Rectangle viewport)
    {
        Rectangle screen = ViewportToScreen (Viewport);

        // Fill in the rest of the rectangle
        for (int r = Math.Max (0, screen.Y); r < screen.Y + viewport.Height; r++)
        {
            for (int c = Math.Max (0, screen.X); c < screen.X + screen.Width; c++)
            {
                Driver?.Move (c, r);
                SetAttribute (GetAttributeUnderLocation (new Point (c, r)));

                if (ScreenContents is null
                    || screen.X >= ScreenContents.GetLength (1)
                    || r >= ScreenContents.GetLength (0)
                    || c >= ScreenContents.GetLength (1)
                    || r >= ScreenContents.GetLength (0))
                {
                    continue;
                }
                string grapheme = ScreenContents [r, c].Grapheme;
                AddStr (grapheme);

                if (grapheme.GetColumns () > 1)
                {
                    c++;
                }
            }
        }
    }

    // BUGBUG: This will never really work completely right by looking at an underlying cell and trying
    // BUGBUG: to do transparency by adjusting colors. Instead, it might be possible to use the A in argb for this.
    // BUGBUG: See https://github.com/tui-cs/Terminal.Gui/issues/4491
    private Attribute GetAttributeUnderLocation (Point location)
    {
        if (SuperView is not AdornmentView
            || location.X < 0
            || location.X >= App?.Screen.Width
            || location.Y < 0
            || location.Y >= App?.Screen.Height
            || ScreenContents == null
            || location.Y < 0
            || location.Y >= ScreenContents.GetLength (0)
            || location.X < 0
            || location.X >= ScreenContents.GetLength (1))
        {
            return Attribute.Default;
        }

        Attribute? attribute = ScreenContents [location.Y, location.X].Attribute;

        if (attribute is null)
        {
            return Attribute.Default;
        }
        Attribute attr = attribute.Value;

        var newAttribute = new Attribute (ShadowStyle == ShadowStyles.Opaque ? Color.Black : attr.Foreground.GetDimmerColor (),
                                          ShadowStyle == ShadowStyles.Opaque ? attr.Background : attr.Background.GetDimmerColor (0.9),
                                          attr.Style);

        // If the BG is DarkGray, GetDimmerColor gave up. Instead of using the attribute in the Driver under the shadow,
        // use the Normal attribute from the View under the shadow.
        if (newAttribute.Background != Color.DarkGray)
        {
            return newAttribute;
        }
        List<View?> currentViewsUnderMouse = GetViewsUnderLocation (location, ViewportSettingsFlags.Transparent);
        View? underView = currentViewsUnderMouse.LastOrDefault ();
        attr = underView?.GetAttributeForRole (VisualRole.Normal) ?? Attribute.Default;

        newAttribute = new Attribute (ShadowStyle == ShadowStyles.Opaque ? Color.Black : attr.Background.GetDimmerColor (),
                                      ShadowStyle == ShadowStyles.Opaque ? attr.Background : attr.Foreground.GetDimmerColor (0.9),
                                      attr.Style);

        return newAttribute;
    }
}

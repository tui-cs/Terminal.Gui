namespace Terminal.Gui.ViewBase;

public partial class View
{
    /// <summary>Moves the drawing cursor to the specified <see cref="Viewport"/>-relative location in the view.</summary>
    /// <remarks>
    ///     <para>
    ///         The top-left corner of the visible content area is <c>ViewPort.Location</c>.
    ///     </para>
    /// </remarks>
    /// <param name="col">Column (viewport-relative).</param>
    /// <param name="row">Row (viewport-relative).</param>
    public bool Move (int col, int row)
    {
        if (Driver is null || Driver?.Rows == 0)
        {
            return false;
        }

        Point screen = ViewportToScreen (new Point (col, row));
        Driver?.Move (screen.X, screen.Y);

        return true;
    }

    /// <summary>Draws the specified character at the current draw position.</summary>
    /// <param name="rune">The Rune.</param>
    public void AddRune (Rune rune) => Driver?.AddRune (rune);

    /// <summary>
    ///     Adds the specified <see langword="char"/> to the display at the current cursor position. This method is a
    ///     convenience method that calls <see cref="AddRune(Rune)"/> with the <see cref="Rune"/> constructor.
    /// </summary>
    /// <param name="c">Character to add.</param>
    public void AddRune (char c) => AddRune (new Rune (c));

    /// <summary>Draws the specified character in the specified viewport-relative column and row of the View.</summary>
    /// <para>
    ///     If the provided coordinates are outside the visible content area, this method does nothing.
    /// </para>
    /// <remarks>
    ///     The top-left corner of the visible content area is <c>ViewPort.Location</c>.
    /// </remarks>
    /// <param name="col">Column (viewport-relative).</param>
    /// <param name="row">Row (viewport-relative).</param>
    /// <param name="rune">The Rune.</param>
    public void AddRune (int col, int row, Rune rune)
    {
        if (Move (col, row))
        {
            Driver?.AddRune (rune);
        }
    }

    /// <summary>Adds the <paramref name="str"/> to the display at the current draw position.</summary>
    /// <remarks>
    ///     <para>
    ///         When the method returns, the draw position will be incremented by the number of columns
    ///         <paramref name="str"/> required, unless the new column value is outside the <see cref="GetClip"/> or
    ///         <see cref="Application.Screen"/>.
    ///     </para>
    ///     <para>If <paramref name="str"/> requires more columns than are available, the output will be clipped.</para>
    /// </remarks>
    /// <param name="str">String.</param>
    public void AddStr (string str) => Driver?.AddStr (str);

    /// <summary>Draws the specified <paramref name="str"/> in the specified viewport-relative column and row of the View.</summary>
    /// <para>
    ///     If the provided coordinates are outside the visible content area, this method does nothing.
    /// </para>
    /// <remarks>
    ///     The top-left corner of the visible content area is <c>ViewPort.Location</c>.
    /// </remarks>
    /// <param name="col">Column (viewport-relative).</param>
    /// <param name="row">Row (viewport-relative).</param>
    /// <param name="str">The Text.</param>
    public void AddStr (int col, int row, string str)
    {
        if (Move (col, row))
        {
            Driver?.AddStr (str);
        }
    }

    /// <summary>Utility function to draw strings that contain a hotkey.</summary>
    /// <param name="text">String to display, the hotkey specifier before a letter flags the next letter as the hotkey.</param>
    /// <param name="hotColor">Hot color.</param>
    /// <param name="normalColor">Normal color.</param>
    /// <remarks>
    ///     <para>
    ///         The hotkey is any character following the hotkey specifier, which is the underscore ('_') character by
    ///         default.
    ///     </para>
    ///     <para>The hotkey specifier can be changed via <see cref="HotKeySpecifier"/></para>
    /// </remarks>
    public void DrawHotString (string text, Attribute hotColor, Attribute normalColor)
    {
        Rune hotkeySpec = HotKeySpecifier == (Rune)0xffff ? (Rune)'_' : HotKeySpecifier;
        SetAttribute (normalColor);

        foreach (string grapheme in GraphemeHelper.GetGraphemes (text))
        {
            // The hotkey specifier is always a single simple character (e.g., '_')
            if (grapheme.Length == 1 && new Rune (grapheme [0]) == new Rune (hotkeySpec.Value))
            {
                SetAttribute (hotColor);

                continue;
            }

            AddStr (grapheme);
            SetAttribute (normalColor);
        }
    }

    /// <summary>
    ///     Utility function to draw strings that contains a hotkey using a <see cref="Scheme"/> and the "focused"
    ///     state.
    /// </summary>
    /// <param name="text">String to display, the underscore before a letter flags the next letter as the hotkey.</param>
    /// <param name="focused">
    ///     If set to <see langword="true"/> this uses the focused colors from the scheme, otherwise
    ///     the regular ones.
    /// </param>
    public void DrawHotString (string text, bool focused)
    {
        if (focused)
        {
            DrawHotString (text, GetAttributeForRole (VisualRole.HotFocus), GetAttributeForRole (VisualRole.Focus));
        }
        else
        {
            DrawHotString (text,
                           Enabled ? GetAttributeForRole (VisualRole.HotNormal) : GetScheme ().Disabled,
                           Enabled ? GetAttributeForRole (VisualRole.Normal) : GetScheme ().Disabled);
        }
    }

    /// <summary>Fills the specified <see cref="Viewport"/>-relative rectangle with the specified color.</summary>
    /// <param name="rect">The Viewport-relative rectangle to clear.</param>
    /// <param name="color">The color to use to fill the rectangle. If not provided, the Normal background color will be used.</param>
    public void FillRect (Rectangle rect, Color? color = null)
    {
        if (Driver is null)
        {
            return;
        }

        Region? prevClip = AddViewportToClip ();
        Rectangle toClear = ViewportToScreen (rect);
        Attribute prev = SetAttribute (new Attribute (color ?? GetAttributeForRole (VisualRole.Normal).Background));
        Driver.FillRect (toClear);
        SetAttribute (prev);
        SetClip (prevClip);
    }

    /// <summary>Fills the specified <see cref="Viewport"/>-relative rectangle.</summary>
    /// <param name="rect">The Viewport-relative rectangle to clear.</param>
    /// <param name="rune">The Rune to fill with.</param>
    public void FillRect (Rectangle rect, Rune rune)
    {
        if (Driver is null)
        {
            return;
        }

        Region? prevClip = AddViewportToClip ();
        Rectangle toClear = ViewportToScreen (rect);
        Driver.FillRect (toClear, rune);
        SetClip (prevClip);
    }
}

using System.Numerics;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Drawing;

/// <summary>
///     Describes the thickness of a frame around a rectangle. Four <see cref="int"/> values describe the
///     <see cref="Left"/>, <see cref="Top"/>, <see cref="Right"/>, and <see cref="Bottom"/> sides of the rectangle,
///     respectively.
/// </summary>
/// <remarks>
///     <para>
///         Use the helper API (<see cref="GetInside(Rectangle)"/> to get the rectangle describing the insides of the
///         frame,
///         with the thickness widths subtracted.
///     </para>
///     <para>
///         Thickness uses <see langword="float"/> internally. As a result, there is a potential precision loss for very
///         large numbers. This is typically not an issue for UI dimensions but could be relevant in other contexts.
///     </para>
/// </remarks>
public record struct Thickness
{
    /// <summary>Initializes a new instance of the <see cref="Thickness"/> class with all widths set to 0.</summary>
    public Thickness () => _sides = Vector4.Zero;

    /// <summary>Initializes a new instance of the <see cref="Thickness"/> class with a uniform width to each side.</summary>
    /// <param name="width"></param>
    public Thickness (int width) : this (width, width, width, width) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Thickness"/> class that has specific widths applied to each side
    ///     of the rectangle.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="right"></param>
    /// <param name="bottom"></param>
    public Thickness (int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    private Vector4 _sides;

    /// <summary>
    ///     Adds the thickness widths of another <see cref="Thickness"/> to the current <see cref="Thickness"/>, returning a
    ///     new <see cref="Thickness"/>.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public readonly Thickness Add (Thickness other) => new (Left + other.Left, Top + other.Top, Right + other.Right, Bottom + other.Bottom);

    /// <summary>Gets or sets the width of the lower side of the rectangle.</summary>
    [JsonInclude]
    public int Bottom { readonly get => (int)_sides.W; set => _sides.W = value; }

    /// <summary>
    ///     Gets whether the specified coordinates lie within the thickness (inside the bounding rectangle but outside
    ///     the rectangle described by <see cref="GetInside(Rectangle)"/>.
    /// </summary>
    /// <param name="outside">Describes the location and size of the rectangle that contains the thickness.</param>
    /// <param name="location">The coordinate to check.</param>
    /// <returns><see langword="true"/> if the specified coordinate is within the thickness; <see langword="false"/> otherwise.</returns>
    public bool Contains (in Rectangle outside, in Point location)
    {
        Rectangle inside = GetInside (outside);

        return outside.Contains (location) && !inside.Contains (location);
    }

    /// <summary>Draws the <see cref="Thickness"/> rectangle with an optional diagnostics label.</summary>
    /// <remarks>
    ///     If <see cref="ViewDiagnosticFlags"/> is set to
    ///     <see cref="ViewDiagnosticFlags.Thickness"/> then 'T', 'L', 'R', and 'B' glyphs will be used instead of
    ///     space. If <see cref="ViewDiagnosticFlags"/> is set to
    ///     <see cref="ViewDiagnosticFlags.Ruler"/> then a ruler will be drawn on the outer edge of the
    ///     Thickness.
    /// </remarks>
    /// <param name="rect">The location and size of the rectangle that bounds the thickness rectangle, in screen coordinates.</param>
    /// <param name="diagnosticFlags"></param>
    /// <param name="label">The diagnostics label to draw on the bottom of the <see cref="Bottom"/>.</param>
    /// <param name="driver">Optional driver. If not specified, <see cref="Application.Driver"/> will be used.</param>
    /// <returns>The inner rectangle remaining to be drawn.</returns>
    public Rectangle Draw (IDriver? driver, Rectangle rect, ViewDiagnosticFlags diagnosticFlags = ViewDiagnosticFlags.Off, string? label = null)
    {
        ArgumentNullException.ThrowIfNull (driver);

        if (rect.Size.Width < 1 || rect.Size.Height < 1)
        {
            return Rectangle.Empty;
        }

        var clearChar = (Rune)' ';
        Rune leftChar = clearChar;
        Rune rightChar = clearChar;
        Rune topChar = clearChar;
        Rune bottomChar = clearChar;

        if (diagnosticFlags.HasFlag (ViewDiagnosticFlags.Thickness))
        {
            leftChar = (Rune)'L';
            rightChar = (Rune)'R';
            topChar = (Rune)'T';
            bottomChar = (Rune)'B';

            if (!string.IsNullOrEmpty (label))
            {
                leftChar = rightChar = bottomChar = topChar = (Rune)label [0];
            }
        }

        // Draw the Top side
        if (Top > 0)
        {
            driver.FillRect (rect with { Height = Math.Min (rect.Height, Top) }, topChar);
        }

        // Draw the Left side
        if (Left > 0)
        {
            driver.FillRect (rect with { Width = Math.Min (rect.Width, Left) }, leftChar);
        }

        // Draw the Right side
        if (Right > 0)
        {
            driver.FillRect (rect with { X = Math.Max (0, rect.X + rect.Width - Right), Width = Math.Min (rect.Width, Right) }, rightChar);
        }

        // Draw the Bottom side
        if (Bottom > 0)
        {
            driver.FillRect (rect with { Y = rect.Y + Math.Max (0, rect.Height - Bottom), Height = Bottom }, bottomChar);
        }

        if (diagnosticFlags.HasFlag (ViewDiagnosticFlags.Ruler))
        {
            // PERF: This can almost certainly be simplified down to a single point offset and fewer calls to Draw
            // Top
            Ruler hRuler = new () { Length = rect.Width, Orientation = Orientation.Horizontal };

            if (Top > 0)
            {
                hRuler.Draw (driver, rect.Location);
            }

            //Left
            Ruler vRuler = new () { Length = rect.Height - 2, Orientation = Orientation.Vertical };

            if (Left > 0)
            {
                vRuler.Draw (driver, rect.Location with { Y = rect.Y + 1 }, 1);
            }

            // Bottom
            if (Bottom > 0)
            {
                hRuler.Draw (driver, rect.Location with { Y = rect.Y + rect.Height - 1 });
            }

            // Right
            if (Right > 0)
            {
                vRuler.Draw (driver, new Point (rect.X + rect.Width - 1, rect.Y + 1), 1);
            }
        }

        if (!diagnosticFlags.HasFlag (ViewDiagnosticFlags.Thickness))
        {
            return GetInside (rect);
        }

        // Draw the diagnostics label on the bottom
        string text = label is null ? string.Empty : $"{label} {this}";

        TextFormatter tf = new ()
        {
            Text = text,
            Alignment = Alignment.Center,
            VerticalAlignment = Alignment.End,
            ConstrainToWidth = text.GetColumns (),
            ConstrainToHeight = 1
        };

        tf.Draw (driver, rect, driver.CurrentAttribute, driver.CurrentAttribute, rect);

        return GetInside (rect);
    }

    /// <summary>Gets an empty thickness.</summary>
    public static Thickness Empty => new (0);

    /// <summary>
    ///     Returns a rectangle describing the location and size of the inside area of <paramref name="rect"/> with the
    ///     thickness widths subtracted. The height and width of the returned rectangle will never be less than 0.
    /// </summary>
    /// <remarks>
    ///     If a thickness width is negative, the inside rectangle will be larger than <paramref name="rect"/>. e.g. a
    ///     <c>
    ///         Thickness (-1, -1, -1, -1) will result in a rectangle skewed -1 in the X and Y directions and with a Size
    ///         increased by 1.
    ///     </c>
    /// </remarks>
    /// <param name="rect">The source rectangle</param>
    /// <returns></returns>
    public Rectangle GetInside (Rectangle rect)
    {
        int x = rect.X + Left;
        int y = rect.Y + Top;
        int width = Math.Max (0, rect.Size.Width - Horizontal);
        int height = Math.Max (0, rect.Size.Height - Vertical);

        return new Rectangle (x, y, width, height);
    }

    /// <summary>
    ///     Returns a region describing the thickness.
    /// </summary>
    /// <param name="rect">The source rectangle</param>
    /// <returns></returns>
    public Region AsRegion (Rectangle rect)
    {
        var region = new Region (rect);
        region.Exclude (GetInside (rect));

        return region;
    }

    /// <summary>
    ///     Gets the total width of the left and right sides of the rectangle. Sets the width of the left and right sides
    ///     of the rectangle to half the specified value.
    /// </summary>
    public int Horizontal { get => Left + Right; set => Left = Right = value / 2; }

    /// <summary>Gets or sets the width of the left side of the rectangle.</summary>
    [JsonInclude]
    public int Left { readonly get => (int)_sides.X; set => _sides.X = value; }

    /// <summary>
    ///     Adds the thickness widths of another <see cref="Thickness"/> to another <see cref="Thickness"/>.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Thickness operator + (Thickness a, Thickness b) => a.Add (b);

    /// <summary>Gets or sets the width of the right side of the rectangle.</summary>
    [JsonInclude]
    public int Right { readonly get => (int)_sides.Z; set => _sides.Z = value; }

    /// <summary>Gets or sets the width of the upper side of the rectangle.</summary>
    [JsonInclude]
    public int Top { readonly get => (int)_sides.Y; set => _sides.Y = value; }

    /// <summary>Returns the thickness widths of the Thickness formatted as a string.</summary>
    /// <returns>The thickness widths as a string.</returns>
    public override string ToString () => $"(Left={Left},Top={Top},Right={Right},Bottom={Bottom})";

    /// <summary>
    ///     Gets the total height of the top and bottom sides of the rectangle. Sets the height of the top and bottom
    ///     sides of the rectangle to half the specified value.
    /// </summary>
    public int Vertical { get => Top + Bottom; set => Top = Bottom = value / 2; }
}

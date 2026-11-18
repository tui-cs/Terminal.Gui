
namespace Terminal.Gui.Drawing;

/// <summary>Draws a ruler on the screen.</summary>
/// <remarks>
///     <para></para>
/// </remarks>
internal class Ruler
{
    /// <summary>Gets or sets the foreground and background color to use.</summary>
    public Attribute Attribute { get; set; } = new ();

    /// <summary>Gets or sets the length of the ruler. The default is 0.</summary>
    public int Length { get; set; }

    /// <summary>Gets or sets whether the ruler is drawn horizontally or vertically. The default is horizontally.</summary>
    public Orientation Orientation { get; set; }

    private string _hTemplate { get; } = "|123456789";
    private string _vTemplate { get; } = "-123456789";

    /// <summary>Draws the <see cref="Ruler"/>.</summary>
    /// <param name="driver">Optional Driver. If not provided, driver will be used.</param>
    /// <param name="location">The location to start drawing the ruler, in screen-relative coordinates.</param>
    /// <param name="start">The start value of the ruler.</param>
    public void Draw (IDriver? driver, Point location, int start = 0)
    {
        ArgumentNullException.ThrowIfNull (driver);

        if (start < 0)
        {
            throw new ArgumentException ("start must be greater than or equal to 0");
        }

        if (Length < 1)
        {
            return;
        }

        if (Orientation == Orientation.Horizontal)
        {
            string hrule =
                _hTemplate.Repeat ((int)Math.Ceiling (Length + 2 / (double)_hTemplate.Length))! [start..(Length + start)];

            // Top
            driver?.Move (location.X, location.Y);
            driver?.AddStr (hrule);
        }
        else
        {
            string vrule =
                _vTemplate.Repeat ((int)Math.Ceiling ((Length + 2) / (double)_vTemplate.Length))!
                    [start..(Length + start)];

            for (int r = location.Y; r < location.Y + Length; r++)
            {
                driver?.Move (location.X, r);
                driver?.AddRune ((Rune)vrule [r - location.Y]);
            }
        }
    }
}

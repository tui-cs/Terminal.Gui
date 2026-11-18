
namespace Terminal.Gui.Drawing;

// TODO: Add events that notify when StraightLine changes to enable dynamic layout
/// <summary>A line between two points on a horizontal or vertical <see cref="Orientation"/> and a given style/color.</summary>
public class StraightLine
{
    /// <summary>Creates a new instance of the <see cref="StraightLine"/> class.</summary>
    /// <param name="start">The start location.</param>
    /// <param name="length">The length of the line.</param>
    /// <param name="orientation">The orientation of the line.</param>
    /// <param name="style">The line style.</param>
    /// <param name="attribute">The attribute to be used for rendering the line.</param>
    public StraightLine (
        Point start,
        int length,
        Orientation orientation,
        LineStyle style,
        Attribute? attribute = null
    )
    {
        Start = start;
        Length = length;
        Orientation = orientation;
        Style = style;
        Attribute = attribute;
    }

    /// <summary>Gets or sets the color of the line.</summary>
    public Attribute? Attribute { get; set; }

    /// <summary>Gets or sets the length of the line.</summary>
    public int Length { get; set; }

    /// <summary>Gets or sets the orientation (horizontal or vertical) of the line.</summary>
    public Orientation Orientation { get; set; }

    /// <summary>Gets or sets where the line begins.</summary>
    public Point Start { get; set; }

    /// <summary>Gets or sets the line style of the line (e.g. dotted, double).</summary>
    public LineStyle Style { get; set; }

    /// <summary>
    ///     Gets the rectangle that describes the bounds of the canvas. Location is the coordinates of the line that is
    ///     the furthest left/top and Size is defined by the line that extends the furthest right/bottom.
    /// </summary>

    // PERF: Probably better to store the rectangle rather than make a new one on every single access to Bounds.
    internal Rectangle Bounds
    {
        get
        {
            // 0 and 1/-1 Length means a size (width or height) of 1
            int size = Math.Max (1, Math.Abs (Length));

            // How much to offset x or y to get the start of the line
            int offset = Math.Abs (Length < 0 ? Length + 1 : 0);
            int x = Start.X - (Orientation == Orientation.Horizontal ? offset : 0);
            int y = Start.Y - (Orientation == Orientation.Vertical ? offset : 0);
            int width = Orientation == Orientation.Horizontal ? size : 1;
            int height = Orientation == Orientation.Vertical ? size : 1;

            return new (x, y, width, height);
        }
    }

    /// <summary>Formats the Line as a string in (Start.X,Start.Y,Length,Orientation) notation.</summary>
    public override string ToString () { return $"({Start.X},{Start.Y},{Length},{Orientation})"; }

    internal IntersectionDefinition? Intersects (int x, int y)
    {
        switch (Orientation)
        {
            case Orientation.Horizontal: return IntersectsHorizontally (x, y);
            case Orientation.Vertical: return IntersectsVertically (x, y);
            default: throw new ArgumentOutOfRangeException (nameof (Orientation));
        }
    }

    private bool EndsAt (int x, int y)
    {
        int sub = Length == 0 ? 0 :
                  Length > 0 ? 1 : -1;

        if (Orientation == Orientation.Horizontal)
        {
            return Start.X + Length - sub == x && Start.Y == y;
        }

        return Start.X == x && Start.Y + Length - sub == y;
    }

    private IntersectionType GetTypeByLength (
        IntersectionType typeWhenNegative,
        IntersectionType typeWhenZero,
        IntersectionType typeWhenPositive
    )
    {
        if (Length == 0)
        {
            return typeWhenZero;
        }

        return Length < 0 ? typeWhenNegative : typeWhenPositive;
    }

    private IntersectionDefinition? IntersectsHorizontally (int x, int y)
    {
        if (Start.Y != y)
        {
            return null;
        }

        var p = new Point (x, y);

        if (StartsAt (x, y))
        {
            return new (
                        p,
                        GetTypeByLength (
                                         IntersectionType.StartLeft,
                                         IntersectionType.PassOverHorizontal,
                                         IntersectionType.StartRight
                                        ),
                        this
                       );
        }

        if (EndsAt (x, y))
        {
            return new (
                        p,
                        Length < 0 ? IntersectionType.StartRight : IntersectionType.StartLeft,
                        this
                       );
        }

        int xmin = Math.Min (Start.X, Start.X + Length);
        int xmax = Math.Max (Start.X, Start.X + Length);

        if (xmin < x && xmax > x)
        {
            return new (
                        p,
                        IntersectionType.PassOverHorizontal,
                        this
                       );
        }

        return null;
    }

    private IntersectionDefinition? IntersectsVertically (int x, int y)
    {
        if (Start.X != x)
        {
            return null;
        }

        var p = new Point (x, y);

        if (StartsAt (x, y))
        {
            return new (
                        p,
                        GetTypeByLength (
                                         IntersectionType.StartUp,
                                         IntersectionType.PassOverVertical,
                                         IntersectionType.StartDown
                                        ),
                        this
                       );
        }

        if (EndsAt (x, y))
        {
            return new (
                        p,
                        Length < 0 ? IntersectionType.StartDown : IntersectionType.StartUp,
                        this
                       );
        }

        int ymin = Math.Min (Start.Y, Start.Y + Length);
        int ymax = Math.Max (Start.Y, Start.Y + Length);

        if (ymin < y && ymax > y)
        {
            return new (
                        p,
                        IntersectionType.PassOverVertical,
                        this
                       );
        }

        return null;
    }

    private bool StartsAt (int x, int y) { return Start.X == x && Start.Y == y; }
}

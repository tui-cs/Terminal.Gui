namespace Terminal.Gui.Drawing;

internal class IntersectionDefinition
{
    internal IntersectionDefinition (Point point, IntersectionType type, StraightLine line)
    {
        Point = point;
        Type = type;
        Line = line;
    }

    /// <summary>The line that intersects <see cref="Point"/></summary>
    internal StraightLine Line { get; }

    /// <summary>The point at which the intersection happens</summary>
    internal Point Point { get; }

    /// <summary>Defines how <see cref="Line"/> position relates to <see cref="Point"/>.</summary>
    internal IntersectionType Type { get; }
}

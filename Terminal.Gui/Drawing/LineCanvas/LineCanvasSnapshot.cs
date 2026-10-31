namespace Terminal.Gui.Drawing;

/// <summary>Retains line geometry without allocating a canvas or subscribing to theme changes.</summary>
internal sealed class LineCanvasSnapshot (StraightLine [] lines, Region? exclusions, HashSet<Point>? reserved)
{
    internal HashSet<Point>? Reserved { get; } = reserved;

    internal void MergeInto (LineCanvas target, bool includeReserved = false)
    {
        foreach (StraightLine line in lines)
        {
            target.AddLine (line);
        }

        if (includeReserved && Reserved is { })
        {
            foreach (Point point in Reserved)
            {
                target.Reserve (new Rectangle (point, new Size (1, 1)));
            }
        }

        if (exclusions is null)
        {
            return;
        }

        target.Exclude (exclusions);
    }
}

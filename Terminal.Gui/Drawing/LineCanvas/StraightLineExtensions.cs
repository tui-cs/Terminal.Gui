
namespace Terminal.Gui.Drawing;

/// <summary>Extension methods for <see cref="StraightLine"/> (including collections).</summary>
public static class StraightLineExtensions
{
    /// <summary>
    ///     Splits or removes all lines in the <paramref name="collection"/> such that none cover the given exclusion
    ///     area.
    /// </summary>
    /// <param name="collection">Lines to adjust</param>
    /// <param name="start">First point to remove from collection</param>
    /// <param name="length">The number of sequential points to exclude</param>
    /// <param name="orientation">Orientation of the exclusion line</param>
    /// <returns></returns>
    public static IEnumerable<StraightLine> Exclude (
        this IEnumerable<StraightLine> collection,
        Point start,
        int length,
        Orientation orientation
    )
    {
        List<StraightLine> toReturn = new ();

        if (length == 0)
        {
            return collection;
        }

        foreach (StraightLine l in collection)
        {
            if (l.Length == 0)
            {
                toReturn.Add (l);

                continue;
            }

            // lines are parallel.  For any straight line one axis (x or y) is constant
            // e.g. Horizontal lines have constant y
            int econstPoint = orientation == Orientation.Horizontal ? start.Y : start.X;
            int lconstPoint = l.Orientation == Orientation.Horizontal ? l.Start.Y : l.Start.X;

            // For the varying axis what is the max/mins
            // i.e. points on horizontal lines vary by x, vertical lines vary by y
            int eDiffMin = GetLineStartOnDiffAxis (start, length, orientation);
            int eDiffMax = GetLineEndOnDiffAxis (start, length, orientation);
            int lDiffMin = GetLineStartOnDiffAxis (l.Start, l.Length, l.Orientation);
            int lDiffMax = GetLineEndOnDiffAxis (l.Start, l.Length, l.Orientation);

            // line is parallel to exclusion
            if (l.Orientation == orientation)
            {
                // Do the parallel lines share constant plane
                if (econstPoint != lconstPoint)
                {
                    // No, so no way they overlap
                    toReturn.Add (l);
                }
                else
                {
                    if (lDiffMax < eDiffMin)
                    {
                        // Line ends before exclusion starts
                        toReturn.Add (l);
                    }
                    else if (lDiffMin > eDiffMax)
                    {
                        // Line starts after exclusion ends
                        toReturn.Add (l);
                    }
                    else
                    {
                        //lines overlap!

                        // Is there a bit we can keep on the left?
                        if (lDiffMin < eDiffMin)
                        {
                            // Create line up to exclusion point
                            int from = lDiffMin;
                            int len = eDiffMin - lDiffMin;

                            if (len > 0)
                            {
                                toReturn.Add (CreateLineFromDiff (l, from, len));
                            }
                        }

                        // Is there a bit we can keep on the right?
                        if (lDiffMax > eDiffMax)
                        {
                            // Create line up to exclusion point
                            int from = eDiffMax + 1;
                            int len = lDiffMax - eDiffMax;

                            if (len > 0)
                            {
                                // A single line with length 1 and -1 are the same (fills only the single cell)
                                // They differ only in how they join to other lines (i.e. to create corners)
                                // Using negative for the later half of the line ensures line joins in a way
                                // consistent with its pre-snipped state.
                                if (len == 1)
                                {
                                    len = -1;
                                }

                                toReturn.Add (CreateLineFromDiff (l, from, len));
                            }
                        }
                    }
                }
            }
            else
            {
                // line is perpendicular to exclusion

                // Does the constant plane of the exclusion appear within the differing plane of the line?
                if (econstPoint >= lDiffMin && econstPoint <= lDiffMax)
                {
                    // Yes, e.g. Vertical exclusion's x is within xmin/xmax of the horizontal line

                    // Vice versa must also be true
                    // for example there is no intersection if the vertical exclusion line does not
                    // stretch down far enough to reach the line
                    if (lconstPoint >= eDiffMin && lconstPoint <= eDiffMax)
                    {
                        // Perpendicular intersection occurs here
                        Point intersection = l.Orientation == Orientation.Horizontal
                                                 ? new Point (econstPoint, lconstPoint)
                                                 : new Point (lconstPoint, econstPoint);

                        // To snip out this single point we will use a recursive call
                        // snipping 1 length along the orientation of l (i.e. parallel)
                        toReturn.AddRange (new [] { l }.Exclude (intersection, 1, l.Orientation));
                    }
                    else
                    {
                        // No intersection
                        toReturn.Add (l);
                    }
                }
                else
                {
                    // Lines do not intersect
                    toReturn.Add (l);
                }
            }
        }

        return toReturn;
    }

    /// <summary>
    ///     Creates a new line which is part of <paramref name="l"/> from the point on the varying axis
    ///     <paramref name="from"/> to <paramref name="length"/>.  Horizontal lines have points that vary by x while vertical
    ///     lines have points that vary by y
    /// </summary>
    /// <param name="l">Line to create sub part from</param>
    /// <param name="from">Point on varying axis to start at</param>
    /// <param name="length">Length of line to return</param>
    /// <returns>The new line</returns>
    private static StraightLine CreateLineFromDiff (StraightLine l, int from, int length)
    {
        var start = new Point (
                               l.Orientation == Orientation.Horizontal ? from : l.Start.X,
                               l.Orientation == Orientation.Horizontal ? l.Start.Y : from
                              );

        return new StraightLine (start, length, l.Orientation, l.Style, l.Attribute);
    }

    /// <summary>
    ///     <para>
    ///         Calculates the single digit point where a line ends on the differing axis i.e. the maximum (controlling for
    ///         negative lengths).
    ///     </para>
    ///     <para>
    ///         For lines with <see cref="Orientation.Horizontal"/> this is an x coordinate. For lines that are
    ///         <see cref="Orientation.Vertical"/> this is a y coordinate.
    ///     </para>
    /// </summary>
    /// <param name="start">Where the line starts</param>
    /// <param name="length">Length of the line</param>
    /// <param name="orientation">Orientation of the line</param>
    /// <returns>The maximum x or y (whichever is differing) point on the line, controlling for negative lengths. </returns>
    private static int GetLineEndOnDiffAxis (Point start, int length, Orientation orientation)
    {
        if (length == 0)
        {
            throw new ArgumentException (@"0 length lines are not supported", nameof (length));
        }

        int sub = length > 0 ? 1 : -1;

        if (orientation == Orientation.Vertical)
        {
            // Points on line differ by y
            return Math.Max (start.Y + length - sub, start.Y);
        }

        // Points on line differ by x
        return Math.Max (start.X + length - sub, start.X);
    }

    /// <summary>
    ///     <para>
    ///         Calculates the single digit point where a line starts on the differing axis i.e. the minimum (controlling for
    ///         negative lengths).
    ///     </para>
    ///     <para>
    ///         For lines with <see cref="Orientation.Horizontal"/> this is an x coordinate. For lines that are
    ///         <see cref="Orientation.Vertical"/> this is a y coordinate.
    ///     </para>
    /// </summary>
    /// <param name="start">Where the line starts</param>
    /// <param name="length">Length of the line</param>
    /// <param name="orientation">Orientation of the line</param>
    /// <returns>The minimum x or y (whichever is differing) point on the line, controlling for negative lengths. </returns>
    private static int GetLineStartOnDiffAxis (Point start, int length, Orientation orientation)
    {
        if (length == 0)
        {
            throw new ArgumentException (@"0 length lines are not supported", nameof (length));
        }

        int sub = length > 0 ? 1 : -1;

        if (orientation == Orientation.Vertical)
        {
            // Points on line differ by y
            return Math.Min (start.Y + length - sub, start.Y);
        }

        // Points on line differ by x
        return Math.Min (start.X + length - sub, start.X);
    }
}

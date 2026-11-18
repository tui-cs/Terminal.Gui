namespace Terminal.Gui.Drawing;

/// <summary>
///     Represents a region composed of one or more rectangles, providing methods for geometric set operations such as
///     union,
///     intersection, exclusion, and complement. This class is designed for use in graphical or terminal-based user
///     interfaces
///     where regions need to be manipulated to manage screen areas, clipping, or drawing boundaries.
/// </summary>
/// <remarks>
///     <para>
///         This class is thread-safe. All operations are synchronized to ensure consistent state when accessed
///         concurrently.
///     </para>
///     <para>
///         The <see cref="Region"/> class adopts a philosophy of efficiency and flexibility, balancing performance with
///         usability for GUI applications. It maintains a list of <see cref="Rectangle"/> objects, representing disjoint
///         (non-overlapping) rectangular areas, and supports operations inspired by set theory. These operations allow
///         combining regions in various ways, such as merging areas (<see cref="RegionOp.Union"/> or
///         <see cref="RegionOp.MinimalUnion"/>),
///         finding common areas (<see cref="RegionOp.Intersect"/>), or removing portions (
///         <see cref="RegionOp.Difference"/> or
///         <see cref="Exclude(Rectangle)"/>).
///     </para>
///     <para>
///         To achieve high performance, the class employs a sweep-line algorithm for merging rectangles, which efficiently
///         processes large sets of rectangles in O(n log n) time by scanning along the x-axis and tracking active vertical
///         intervals. This approach ensures scalability for typical GUI scenarios with moderate numbers of rectangles. For
///         operations like <see cref="RegionOp.Union"/> and <see cref="RegionOp.MinimalUnion"/>, an optional minimization
///         step (
///         <see
///             cref="MinimizeRectangles"/>
///         ) is used to reduce the number of rectangles to a minimal set, producing the smallest
///         possible collection of non-overlapping rectangles that cover the same area. This minimization, while O(n²) in
///         worst-case complexity, is optimized for small-to-medium collections and provides a compact representation ideal
///         for drawing or logical operations.
///     </para>
///     <para>
///         The class is immutable in its operations (returning new regions or modifying in-place via methods like
///         <see cref="Combine(Rectangle,RegionOp)"/>), supports nullability for robustness, and implements
///         <see cref="IDisposable"/> to manage
///         resources by clearing internal state. Developers can choose between granular (detailed) or minimal (compact)
///         outputs for union operations via <see cref="RegionOp.Union"/> and <see cref="RegionOp.MinimalUnion"/>, catering
///         to diverse use cases such as rendering optimization, event handling, or visualization.
///     </para>
/// </remarks>
public class Region
{
    private readonly List<Rectangle> _rectangles = [];

    // Add a single reusable list for temp operations
    private readonly List<Rectangle> _tempRectangles = new ();

    // Object used for synchronization
    private readonly object _syncLock = new ();

    /// <summary>
    ///     Initializes a new instance of the <see cref="Region"/> class.
    /// </summary>
    public Region () { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Region"/> class with the specified rectangle.
    /// </summary>
    /// <param name="rectangle">The initial rectangle for the region.</param>
    public Region (Rectangle rectangle)
    {
        lock (_syncLock)
        {
            _rectangles.Add (rectangle);
        }
    }

    /// <summary>
    ///     Creates an exact copy of the region.
    /// </summary>
    /// <returns>A new <see cref="Region"/> that is a copy of this instance.</returns>
    public Region Clone ()
    {
        lock (_syncLock)
        {
            Region clone = new ();
            clone._rectangles.Capacity = _rectangles.Count; // Pre-allocate capacity
            clone._rectangles.AddRange (_rectangles);

            return clone;
        }
    }

    /// <summary>
    ///     Combines <paramref name="rectangle"/> with the region using the specified operation.
    /// </summary>
    /// <param name="rectangle">The rectangle to combine.</param>
    /// <param name="operation">The operation to perform.</param>
    public void Combine (Rectangle rectangle, RegionOp operation)
    {
        lock (_syncLock)
        {
            if (rectangle.IsEmpty && operation != RegionOp.Replace)
            {
                if (operation == RegionOp.Intersect)
                {
                    _rectangles.Clear ();
                }

                return;
            }

            Combine (new Region (rectangle), operation);
        }
    }

    /// <summary>
    ///     Combines <paramref name="region"/> with the region using the specified operation.
    /// </summary>
    /// <param name="region">The region to combine.</param>
    /// <param name="operation">The operation to perform.</param>
    public void Combine (Region? region, RegionOp operation)
    {
        lock (_syncLock)
        {
            CombineInternal (region, operation);
        }
    }

    // Private method to implement the combine logic within a lock
    private void CombineInternal (Region? region, RegionOp operation)
    {
        if (region is null || region._rectangles.Count == 0)
        {
            if (operation is RegionOp.Intersect or RegionOp.Replace)
            {
                _rectangles.Clear ();
            }

            return;
        }

        switch (operation)
        {
            case RegionOp.Difference:

                // region is regionB
                // We'll chain the difference: (regionA - rect1) - rect2 - rect3 ...
                List<Rectangle> newRectangles = new (_rectangles);

                foreach (Rectangle rect in region._rectangles)
                {
                    List<Rectangle> temp = [];

                    foreach (Rectangle r in newRectangles)
                    {
                        temp.AddRange (SubtractRectangle (r, rect));
                    }

                    newRectangles = temp;
                }

                _rectangles.Clear ();
                _rectangles.AddRange (newRectangles);

                break;

            case RegionOp.Intersect:
                List<Rectangle> intersections = new (_rectangles.Count); // Pre-allocate

                foreach (Rectangle rect1 in _rectangles)
                {
                    foreach (Rectangle rect2 in region._rectangles)
                    {
                        Rectangle intersected = Rectangle.Intersect (rect1, rect2);

                        if (intersected.Width > 0 && intersected.Height > 0)
                        {
                            intersections.Add (intersected);
                        }
                    }
                }

                _rectangles.Clear ();
                _rectangles.AddRange (intersections);

                break;

            case RegionOp.Union:
                // Avoid collection initialization with spread operator
                _tempRectangles.Clear ();
                _tempRectangles.AddRange (_rectangles);

                // Get the region's rectangles safely
                lock (region._syncLock)
                {
                    _tempRectangles.AddRange (region._rectangles);
                }
                List<Rectangle> mergedUnion = MergeRectangles (_tempRectangles, false);
                _rectangles.Clear ();
                _rectangles.AddRange (mergedUnion);

                break;

            case RegionOp.MinimalUnion:
                // Avoid collection initialization with spread operator
                _tempRectangles.Clear ();
                _tempRectangles.AddRange (_rectangles);

                // Get the region's rectangles safely
                lock (region._syncLock)
                {
                    _tempRectangles.AddRange (region._rectangles);
                }
                List<Rectangle> mergedMinimalUnion = MergeRectangles (_tempRectangles, true);
                _rectangles.Clear ();
                _rectangles.AddRange (mergedMinimalUnion);

                break;

            case RegionOp.XOR:

                // Snapshot both operands before mutating either, so the symmetric
                // difference is computed as (thisOriginal \ regionOriginal) ∪
                // (regionOriginal \ thisOriginal) rather than against a partially
                // mutated `this`.
                Region thisOriginal = Clone ();
                Region regionOriginal = region.Clone ();

                // Reuse `thisOriginal` to hold (thisOriginal \ regionOriginal).
                thisOriginal.Combine (regionOriginal, RegionOp.Difference);

                // Reuse a fresh clone of the unmodified `this` for (regionOriginal \ thisOriginal).
                Region regionMinusThis = region.Clone ();
                regionMinusThis.Combine (Clone (), RegionOp.Difference);

                _rectangles.Clear ();
                _rectangles.AddRange (thisOriginal._rectangles);
                _rectangles.AddRange (regionMinusThis._rectangles);

                break;

            case RegionOp.ReverseDifference:
                region.Combine (this, RegionOp.Difference);
                _rectangles.Clear ();
                _rectangles.AddRange (region._rectangles);

                break;

            case RegionOp.Replace:
                _rectangles.Clear ();
                _rectangles.Capacity = region._rectangles.Count; // Pre-allocate
                _rectangles.AddRange (region._rectangles);

                break;

            default: throw new ArgumentOutOfRangeException (nameof (operation), operation, null);
        }
    }

    /// <summary>
    ///     Updates the region to be the complement of itself within the specified bounds.
    /// </summary>
    /// <param name="bounds">The bounding rectangle to use for complementing the region.</param>
    public void Complement (Rectangle bounds)
    {
        if (bounds.IsEmpty || _rectangles.Count == 0)
        {
            _rectangles.Clear ();

            return;
        }

        List<Rectangle> complementRectangles = new (4) { bounds }; // Typical max initial capacity

        foreach (Rectangle rect in _rectangles)
        {
            complementRectangles = complementRectangles.SelectMany (r => SubtractRectangle (r, rect)).ToList ();
        }

        _rectangles.Clear ();
        _rectangles.AddRange (complementRectangles);
    }

    /// <summary>
    ///     Determines whether the specified point is contained within the region.
    /// </summary>
    /// <param name="x">The x-coordinate of the point.</param>
    /// <param name="y">The y-coordinate of the point.</param>
    /// <returns><c>true</c> if the point is contained within the region; otherwise, <c>false</c>.</returns>
    public bool Contains (int x, int y)
    {
        lock (_syncLock)
        {
            foreach (Rectangle r in _rectangles)
            {
                if (r.Contains (x, y))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    ///     Determines whether the specified rectangle is contained within the region.
    /// </summary>
    /// <param name="rectangle">The rectangle to check for containment.</param>
    /// <returns><c>true</c> if the rectangle is contained within the region; otherwise, <c>false</c>.</returns>
    public bool Contains (Rectangle rectangle)
    {
        lock (_syncLock)
        {
            foreach (Rectangle r in _rectangles)
            {
                if (r.Contains (rectangle))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    ///     Determines whether the specified object is equal to this region.
    /// </summary>
    /// <param name="obj">The object to compare with this region.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    public override bool Equals (object? obj) => obj is Region other && Equals (other);

    private static bool IsRegionEmpty (List<Rectangle> rectangles)
    {
        if (rectangles.Count == 0)
        {
            return true;
        }

        foreach (Rectangle r in rectangles)
        {
            if (r is { IsEmpty: false, Width: > 0, Height: > 0 })
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Determines whether the specified region is equal to this region.
    /// </summary>
    /// <param name="other">The region to compare with this region.</param>
    /// <returns><c>true</c> if the regions are equal; otherwise, <c>false</c>.</returns>
    public bool Equals (Region? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals (this, other))
        {
            return true;
        }

        // Check if either region is empty
        bool thisEmpty = IsRegionEmpty (_rectangles);
        bool otherEmpty = IsRegionEmpty (other._rectangles);

        // If either is empty, they're equal only if both are empty
        if (thisEmpty || otherEmpty)
        {
            return thisEmpty == otherEmpty;
        }

        // For non-empty regions, compare rectangle counts
        if (_rectangles.Count != other._rectangles.Count)
        {
            return false;
        }

        // Compare all rectangles - order matters since we maintain canonical form
        for (var i = 0; i < _rectangles.Count; i++)
        {
            if (!_rectangles [i].Equals (other._rectangles [i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Removes the specified rectangle from the region.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a helper method that is equivalent to calling <see cref="Combine(Rectangle,RegionOp)"/> with
    ///         <see cref="RegionOp.Difference"/>.
    ///     </para>
    /// </remarks>
    /// <param name="rectangle">The rectangle to exclude from the region.</param>
    public void Exclude (Rectangle rectangle) => Combine (rectangle, RegionOp.Difference);

    /// <summary>
    ///     Removes the portion of the specified region from this region.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a helper method that is equivalent to calling <see cref="Combine(Region,RegionOp)"/> with
    ///         <see cref="RegionOp.Difference"/>.
    ///     </para>
    /// </remarks>
    /// <param name="region">The region to exclude from this region.</param>
    public void Exclude (Region? region) => Combine (region, RegionOp.Difference);

    /// <summary>
    ///     Gets a bounding rectangle for the entire region.
    /// </summary>
    /// <returns>A <see cref="Rectangle"/> that bounds the region.</returns>
    public Rectangle GetBounds ()
    {
        if (_rectangles.Count == 0)
        {
            return Rectangle.Empty;
        }

        Rectangle first = _rectangles [0];
        int left = first.Left;
        int top = first.Top;
        int right = first.Right;
        int bottom = first.Bottom;

        for (var i = 1; i < _rectangles.Count; i++)
        {
            Rectangle r = _rectangles [i];
            left = Math.Min (left, r.Left);
            top = Math.Min (top, r.Top);
            right = Math.Max (right, r.Right);
            bottom = Math.Max (bottom, r.Bottom);
        }

        return new Rectangle (left, top, right - left, bottom - top);
    }

    /// <summary>
    ///     Returns a hash code for this region.
    /// </summary>
    /// <returns>A hash code for this region.</returns>
    public override int GetHashCode ()
    {
        var hash = new HashCode ();

        foreach (Rectangle rect in _rectangles)
        {
            hash.Add (rect);
        }

        return hash.ToHashCode ();
    }

    /// <summary>
    ///     Returns an array of rectangles that represent the region.
    /// </summary>
    /// <returns>An array of <see cref="Rectangle"/> objects that make up the region.</returns>
    public Rectangle [] GetRectangles () => _rectangles.ToArray ();

    /// <summary>
    ///     Updates the region to be the intersection of itself with the specified rectangle.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a helper method that is equivalent to calling <see cref="Combine(Rectangle,RegionOp)"/> with
    ///         <see cref="RegionOp.Intersect"/>.
    ///     </para>
    /// </remarks>
    /// <param name="rectangle">The rectangle to intersect with the region.</param>
    public void Intersect (Rectangle rectangle) => Combine (rectangle, RegionOp.Intersect);

    /// <summary>
    ///     Updates the region to be the intersection of itself with the specified region.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a helper method that is equivalent to calling <see cref="Combine(Region,RegionOp)"/> with
    ///         <see cref="RegionOp.Intersect"/>.
    ///     </para>
    /// </remarks>
    /// <param name="region">The region to intersect with this region.</param>
    public void Intersect (Region? region) => Combine (region, RegionOp.Intersect);

    /// <summary>
    ///     Determines whether the region is empty.
    /// </summary>
    /// <returns><c>true</c> if the region is empty; otherwise, <c>false</c>.</returns>
    public bool IsEmpty ()
    {
        if (_rectangles.Count == 0)
        {
            return true;
        }

        foreach (Rectangle r in _rectangles)
        {
            if (r is { IsEmpty: false, Width: > 0, Height: > 0 })
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Translates all rectangles in the region by the specified offsets.
    /// </summary>
    /// <param name="offsetX">The amount to offset along the x-axis.</param>
    /// <param name="offsetY">The amount to offset along the y-axis.</param>
    public void Translate (int offsetX, int offsetY)
    {
        if (offsetX == 0 && offsetY == 0)
        {
            return;
        }

        for (var i = 0; i < _rectangles.Count; i++)
        {
            Rectangle rect = _rectangles [i];
            _rectangles [i] = rect with { X = rect.Left + offsetX, Y = rect.Top + offsetY };
        }
    }

    /// <summary>
    ///     Adds the specified rectangle to the region. Merges all rectangles into a minimal or granular bounding shape.
    /// </summary>
    /// <param name="rectangle">The rectangle to add to the region.</param>
    public void Union (Rectangle rectangle) => Combine (rectangle, RegionOp.Union);

    /// <summary>
    ///     Adds the specified region to this region. Merges all rectangles into a minimal or granular bounding shape.
    /// </summary>
    /// <param name="region">The region to add to this region.</param>
    public void Union (Region? region) => Combine (region, RegionOp.Union);

    /// <summary>
    ///     Adds the specified rectangle to the region. Merges all rectangles into the smallest possible bounding shape.
    /// </summary>
    /// <param name="rectangle">The rectangle to add to the region.</param>
    public void MinimalUnion (Rectangle rectangle) => Combine (rectangle, RegionOp.MinimalUnion);

    /// <summary>
    ///     Adds the specified region to this region. Merges all rectangles into the smallest possible bounding shape.
    /// </summary>
    /// <param name="region">The region to add to this region.</param>
    public void MinimalUnion (Region? region) => Combine (region, RegionOp.MinimalUnion);

    /// <summary>
    ///     Merges overlapping rectangles into a minimal or granular set of non-overlapping rectangles with a minimal bounding
    ///     shape.
    /// </summary>
    /// <param name="rectangles">The list of rectangles to merge.</param>
    /// <param name="minimize">
    ///     If <c>true</c>, minimizes the set to the smallest possible number of rectangles; otherwise,
    ///     returns a granular set.
    /// </param>
    /// <returns>A list of merged rectangles.</returns>
    internal static List<Rectangle> MergeRectangles (List<Rectangle> rectangles, bool minimize)
    {
        if (rectangles.Count <= 1)
        {
            return rectangles.ToList ();
        }

        // Generate events
        List<(int x, bool isStart, int yTop, int yBottom)> events = new (rectangles.Count * 2);

        foreach (Rectangle r in rectangles)
        {
            if (r.Width <= 0 || r.Height <= 0)
            {
                continue;
            }

            events.Add ((r.Left, true, r.Top, r.Bottom));
            events.Add ((r.Right, false, r.Top, r.Bottom));
        }

        if (events.Count == 0)
        {
            return [];
        }

        // Sort events:
        // 1. Primarily by x-coordinate.
        // 2. Secondary: End events before Start events at the same x.
        // 3. Tertiary: By yTop coordinate as a tie-breaker.
        // 4. Quaternary: By yBottom coordinate as a final tie-breaker.
        events.Sort ((a, b) =>
                     {
                         // 1. Sort by X
                         int cmp = a.x.CompareTo (b.x);

                         if (cmp != 0)
                         {
                             return cmp;
                         }

                         // 2. Sort End events before Start events
                         bool aIsEnd = !a.isStart;
                         bool bIsEnd = !b.isStart;
                         cmp = aIsEnd.CompareTo (bIsEnd); // True (End) comes after False (Start)

                         if (cmp != 0)
                         {
                             return -cmp; // Reverse: End (true) should come before Start (false)
                         }

                         // 3. Tie-breaker: Sort by yTop
                         cmp = a.yTop.CompareTo (b.yTop);

                         if (cmp != 0)
                         {
                             return cmp;
                         }

                         // 4. Final Tie-breaker: Sort by yBottom
                         return a.yBottom.CompareTo (b.yBottom);
                     });

        List<Rectangle> merged = [];

        // Use a dictionary to track active intervals and their overlap counts
        Dictionary<(int yTop, int yBottom), int> activeCounts = new ();

        // Comparer for sorting intervals when needed
        Comparer<(int yTop, int yBottom)> intervalComparer = Comparer<(int yTop, int yBottom)>.Create ((a, b) =>
                                                                                                       {
                                                                                                           int cmp = a.yTop.CompareTo (b.yTop);

                                                                                                           return cmp != 0
                                                                                                               ? cmp
                                                                                                               : a.yBottom.CompareTo (b.yBottom);
                                                                                                       });

        // Group events by x-coordinate to process all events at a given x together
        IOrderedEnumerable<IGrouping<int, (int x, bool isStart, int yTop, int yBottom)>> groupedEvents = events.GroupBy (e => e.x).OrderBy (g => g.Key);
        int lastX = groupedEvents.First ().Key; // Initialize with the first event's x

        foreach (IGrouping<int, (int x, bool isStart, int yTop, int yBottom)> group in groupedEvents)
        {
            int currentX = group.Key;

            // Get active intervals based on state *before* processing events at currentX
            SortedSet<(int yTop, int yBottom)> currentActiveIntervals = GetActiveIntervals ();

            // 1. Output rectangles for the segment ending *before* this x coordinate
            if (currentX > lastX && currentActiveIntervals.Count > 0)
            {
                merged.AddRange (MergeVerticalIntervals (currentActiveIntervals, lastX, currentX));
            }

            // 2. Process all events *at* this x coordinate to update counts
            foreach ((int x, bool isStart, int yTop, int yBottom) evt in group)
            {
                (int yTop, int yBottom) interval = (evt.yTop, evt.yBottom);

                if (evt.isStart)
                {
                    activeCounts.TryGetValue (interval, out int count);
                    activeCounts [interval] = count + 1;
                }
                else
                {
                    // Only decrement/remove if the interval exists
                    if (!activeCounts.TryGetValue (interval, out int count))
                    {
                        continue;
                    }

                    if (count - 1 <= 0)
                    {
                        activeCounts.Remove (interval);
                    }
                    else
                    {
                        activeCounts [interval] = count - 1;
                    }
                }
            }

            // 3. Update lastX for the next segment
            lastX = currentX;
        }

        return minimize ? MinimizeRectangles (merged) : merged;

        // Helper to get the current active intervals (where count > 0) as a SortedSet
        SortedSet<(int yTop, int yBottom)> GetActiveIntervals ()
        {
            SortedSet<(int yTop, int yBottom)> set = new (intervalComparer);

            foreach (KeyValuePair<(int yTop, int yBottom), int> kvp in activeCounts)
            {
                if (kvp.Value > 0)
                {
                    set.Add (kvp.Key);
                }
            }

            return set;
        }
    }

    /// <summary>
    ///     Merges overlapping vertical intervals into a minimal set of non-overlapping rectangles.
    /// </summary>
    /// <param name="active">The set of active vertical intervals.</param>
    /// <param name="startX">The starting x-coordinate for the rectangles.</param>
    /// <param name="endX">The ending x-coordinate for the rectangles.</param>
    /// <returns>A list of merged rectangles.</returns>
    internal static List<Rectangle> MergeVerticalIntervals (SortedSet<(int yTop, int yBottom)> active, int startX, int endX)
    {
        if (active.Count == 0)
        {
            return [];
        }

        List<Rectangle> result = new (active.Count); // Pre-allocate
        int? currentTop = null;
        int? currentBottom = null;

        foreach ((int yTop, int yBottom) in active)
        {
            if (currentTop is null)
            {
                currentTop = yTop;
                currentBottom = yBottom;
            }
            else if (yTop <= currentBottom)
            {
                currentBottom = Math.Max (currentBottom.Value, yBottom);
            }
            else
            {
                if (currentBottom is { })
                {
                    result.Add (new Rectangle (startX, currentTop.Value, endX - startX, currentBottom.Value - currentTop.Value));
                    currentTop = yTop;
                }
                currentBottom = yBottom;
            }
        }

        if (currentTop is null)
        {
            return result;
        }

        if (currentBottom is { })
        {
            result.Add (new Rectangle (startX, currentTop.Value, endX - startX, currentBottom.Value - currentTop.Value));
        }

        return result;
    }

    /// <summary>
    ///     Minimizes a list of rectangles into the smallest possible set of non-overlapping rectangles
    ///     by merging adjacent rectangles where possible.
    /// </summary>
    /// <param name="rectangles">The list of rectangles to minimize.</param>
    /// <returns>A list of minimized rectangles.</returns>
    internal static List<Rectangle> MinimizeRectangles (List<Rectangle> rectangles)
    {
        if (rectangles.Count <= 1)
        {
            return rectangles.ToList ();
        }

        List<Rectangle> minimized = new (rectangles.Count); // Pre-allocate
        List<Rectangle> current = new (rectangles); // Work with a copy

        bool changed;

        do
        {
            changed = false;
            minimized.Clear ();

            // Sort by Y then X for consistent processing
            current.Sort ((a, b) =>
                          {
                              int cmp = a.Top.CompareTo (b.Top);

                              return cmp != 0 ? cmp : a.Left.CompareTo (b.Left);
                          });

            var i = 0;

            while (i < current.Count)
            {
                Rectangle r = current [i];
                int j = i + 1;

                while (j < current.Count)
                {
                    Rectangle next = current [j];

                    // Check if rectangles can be merged horizontally (same Y range, adjacent X)
                    if (r.Top == next.Top && r.Bottom == next.Bottom && (r.Right == next.Left || next.Right == r.Left || r.IntersectsWith (next)))
                    {
                        r = new Rectangle (Math.Min (r.Left, next.Left), r.Top, Math.Max (r.Right, next.Right) - Math.Min (r.Left, next.Left), r.Height);
                        current.RemoveAt (j);
                        changed = true;
                    }

                    // Check if rectangles can be merged vertically (same X range, adjacent Y)
                    else if (r.Left == next.Left && r.Right == next.Right && (r.Bottom == next.Top || next.Bottom == r.Top || r.IntersectsWith (next)))
                    {
                        r = new Rectangle (r.Left, Math.Min (r.Top, next.Top), r.Width, Math.Max (r.Bottom, next.Bottom) - Math.Min (r.Top, next.Top));
                        current.RemoveAt (j);
                        changed = true;
                    }
                    else
                    {
                        j++;
                    }
                }

                minimized.Add (r);
                i++;
            }

            current = minimized.ToList (); // Prepare for next iteration
        }
        while (changed);

        return minimized;
    }

    /// <summary>
    ///     Subtracts the specified rectangle from the original rectangle, returning the resulting rectangles.
    /// </summary>
    /// <param name="original">The original rectangle.</param>
    /// <param name="subtract">The rectangle to subtract from the original.</param>
    /// <returns>An enumerable collection of resulting rectangles after subtraction.</returns>
    internal static IEnumerable<Rectangle> SubtractRectangle (Rectangle original, Rectangle subtract)
    {
        // Handle empty or invalid rectangles
        if (original.IsEmpty || original.Width <= 0 || original.Height <= 0)
        {
            yield break; // Return empty enumeration for empty or invalid original
        }

        if (subtract.IsEmpty || subtract.Width <= 0 || subtract.Height <= 0)
        {
            yield return original;

            yield break;
        }

        // Check for complete overlap (subtract fully contains or equals original)
        if (subtract.Left <= original.Left && subtract.Top <= original.Top && subtract.Right >= original.Right && subtract.Bottom >= original.Bottom)
        {
            yield break; // Return empty if subtract completely overlaps original
        }

        // Check for no overlap
        if (!original.IntersectsWith (subtract))
        {
            yield return original;

            yield break;
        }

        // Fragment the original rectangle into segments excluding the subtract rectangle

        // Top segment (above subtract)
        if (original.Top < subtract.Top)
        {
            yield return new Rectangle (original.Left, original.Top, original.Width, subtract.Top - original.Top);
        }

        // Bottom segment (below subtract)
        if (original.Bottom > subtract.Bottom)
        {
            yield return new Rectangle (original.Left, subtract.Bottom, original.Width, original.Bottom - subtract.Bottom);
        }

        // Left segment (to the left of subtract)
        if (original.Left < subtract.Left)
        {
            int top = Math.Max (original.Top, subtract.Top);
            int bottom = Math.Min (original.Bottom, subtract.Bottom);

            if (bottom > top)
            {
                yield return new Rectangle (original.Left, top, subtract.Left - original.Left, bottom - top);
            }
        }

        // Right segment (to the right of subtract)
        if (original.Right <= subtract.Right)
        {
            yield break;
        }

        {
            int top = Math.Max (original.Top, subtract.Top);
            int bottom = Math.Min (original.Bottom, subtract.Bottom);

            if (bottom > top)
            {
                yield return new Rectangle (subtract.Right, top, original.Right - subtract.Right, bottom - top);
            }
        }
    }

    /// <summary>
    ///     Fills the interior of all rectangles in the region with the specified attribute and fill rune.
    /// </summary>
    /// <param name="driver"></param>
    /// <param name="attribute">The attribute (color/style) to use.</param>
    /// <param name="fillRune">
    ///     The rune to fill the interior of the rectangles with. If <cref langword="null"/> space will be
    ///     used.
    /// </param>
    public void FillRectangles (IDriver? driver, Attribute? attribute, Rune? fillRune = null)
    {
        ArgumentNullException.ThrowIfNull (driver);

        if (_rectangles.Count == 0 || attribute is null)
        {
            return;
        }

        foreach (Rectangle rect in _rectangles)
        {
            if (rect.IsEmpty || rect.Width <= 0 || rect.Height <= 0)
            {
                continue;
            }

            driver?.SetAttribute (attribute.Value);

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    driver?.Move (x, y);
                    driver?.AddRune (fillRune ?? (Rune)' ');
                }
            }
        }
    }

    /// <summary>
    ///     Draws the boundaries of all rectangles in the region using the specified attributes, only if the rectangle is big
    ///     enough.
    /// </summary>
    /// <param name="canvas">The canvas to draw on.</param>
    /// <param name="style">The line style to use for drawing.</param>
    /// <param name="attribute">The attribute (color/style) to use for the lines. If <c>null</c>.</param>
    public void DrawBoundaries (LineCanvas canvas, LineStyle style, Attribute? attribute = null)
    {
        if (_rectangles.Count == 0)
        {
            return;
        }

        foreach (Rectangle rect in _rectangles)
        {
            if (rect.IsEmpty || rect.Width <= 0 || rect.Height <= 0)
            {
                continue;
            }

            // Only draw boundaries if the rectangle is "big enough" (e.g., width and height > 1)
            //if (rect.Width > 2 && rect.Height > 2)
            {
                if (rect.Width > 1)
                {
                    // Add horizontal lines
                    canvas.AddLine (new Point (rect.Left, rect.Top), rect.Width, Orientation.Horizontal, style, attribute);
                    canvas.AddLine (new Point (rect.Left, rect.Bottom - 1), rect.Width, Orientation.Horizontal, style, attribute);
                }

                if (rect.Height <= 1)
                {
                    continue;
                }

                // Add vertical lines
                canvas.AddLine (new Point (rect.Left, rect.Top), rect.Height, Orientation.Vertical, style, attribute);
                canvas.AddLine (new Point (rect.Right - 1, rect.Top), rect.Height, Orientation.Vertical, style, attribute);
            }
        }
    }
}

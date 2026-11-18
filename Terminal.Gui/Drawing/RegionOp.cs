
namespace Terminal.Gui.Drawing;

/// <summary>
///     Specifies the operation to perform when combining two regions or a <see cref="Region"/> with a
///     <see cref="Rectangle"/>>, defining how their
///     rectangular areas are merged, intersected, or subtracted.
/// </summary>
/// <remarks>
///     <para>
///         Each operation modifies the first region's set of rectangles based on the second (op) region or rectangle,
///         producing a new set of non-overlapping rectangles. The operations align with set theory, enabling flexible
///         manipulation for TUI layout, clipping, or drawing. Developers can choose between granular outputs (e.g.,
///         <see cref="Union"/>) that preserve detailed rectangles or minimal outputs (e.g., <see cref="MinimalUnion"/>)
///         that reduce the rectangle count for compactness.
///     </para>
/// </remarks>
public enum RegionOp
{
    /// <summary>
    ///     Subtracts the second (op) region or rectangle from the first region, removing any areas where the op overlaps
    ///     the first region. The result includes only the portions of the first region that do not intersect with the op.
    ///     <para>
    ///         For example, if the first region contains rectangle A = (0,0,10,10) and the op is B = (5,5,5,5), the result
    ///         would include rectangles covering A minus the overlapping part of B, such as (0,0,10,5), (0,5,5,5), and
    ///         (5,10,5,5).
    ///     </para>
    ///     <para>
    ///         If the op region is empty or null, the operation has no effect unless the first region is also empty, in
    ///         which case it clears the first region.
    ///     </para>
    /// </summary>
    Difference = 0,

    /// <summary>
    ///     Intersects the first region with the second (op) region or rectangle, retaining only the areas where both
    ///     regions overlap. The result includes rectangles covering the common areas, excluding any parts unique to either
    ///     region.
    ///     <para>
    ///         For example, if the first region contains A = (0,0,10,10) and the op is B = (5,5,5,5), the result would be
    ///         a single rectangle (5,5,5,5), representing the intersection.
    ///     </para>
    ///     <para>
    ///         If either region is empty or null, the result clears the first region, as there’s no intersection possible.
    ///     </para>
    /// </summary>
    Intersect = 1,

    /// <summary>
    ///     Performs a union (inclusive-or) of the first region and the second (op) region or rectangle, combining all
    ///     areas covered by either region into a single contiguous region without holes (unless explicitly subtracted).
    ///     <para>
    ///         The formal union (∪) includes all points in at least one rectangle, producing a granular set of
    ///         non-overlapping rectangles that cover the combined area. For example, if the first region contains A =
    ///         (0,0,5,5) and the op is B = (5,0,5,5), the result might include (0,0,5,5) and (5,0,5,5) unless minimized.
    ///     </para>
    ///     <para>
    ///         This operation uses granular output (preserving detailed rectangles). To minimize the result use
    ///         <see cref="MinimalUnion"/> instead.
    ///     </para>
    ///     <para>
    ///         If the op region is empty or null, the first region remains unchanged.
    ///     </para>
    /// </summary>
    Union = 2,

    /// <summary>
    ///     Performs a minimal union (inclusive-or) of the first region and the second (op) region or rectangle, merging adjacent or
    ///     overlapping rectangles into the smallest possible set of non-overlapping rectangles that cover the combined
    ///     area.
    ///     <para>
    ///         This operation minimizes the number of rectangles, producing a more compact representation compared to
    ///         <see cref="Union"/>. For example, if the first region contains A = (0,0,5,5) and the op is B = (5,0,5,5),
    ///         the result would be a single rectangle (0,0,10,5), reducing redundancy.
    ///     </para>
    ///     <para>
    ///         This operation always minimizes the output and has lower performance than <see cref="Union"/>.
    ///     </para>
    ///     <para>
    ///         If the op region is empty or null, the first region remains unchanged.
    ///     </para>
    /// </summary>
    MinimalUnion = 3,

    /// <summary>
    ///     Performs an exclusive-or (XOR) of the first region and the second (op) region or rectangle, retaining only the
    ///     areas that are unique to each region—i.e., areas present in one region but not both.
    ///     <para>
    ///         For example, if the first region contains A = (0,0,10,10) and the op is B = (5,5,5,5), the result would
    ///         include rectangles covering (0,0,10,5), (0,5,5,5), (5,10,5,5), and (5,5,5,5), excluding the intersection
    ///         (5,5,5,5).
    ///     </para>
    ///     <para>
    ///         If the op region is empty or null, this operation excludes the first region from itself (clearing it) or
    ///         adds the first region to the op (if op is empty), depending on the logic.
    ///     </para>
    /// </summary>
    XOR = 4,

    /// <summary>
    ///     Subtracts the first region from the second (op) region or rectangle, retaining only the areas of the op that do
    ///     not overlap with the first region. The result replaces the first region with these areas.
    ///     <para>
    ///         For example, if the first region contains A = (5,5,5,5) and the op is B = (0,0,10,10), the result would
    ///         include rectangles covering B minus A, such as (0,0,10,5), (0,5,5,5), and (5,10,5,5).
    ///     </para>
    ///     <para>
    ///         If the first region is empty or null, the op region replaces the first region. If the op region is empty,
    ///         the first region is cleared.
    ///     </para>
    /// </summary>
    ReverseDifference = 5,

    /// <summary>
    ///     Replaces the first region entirely with the second (op) region or rectangle, discarding the first region's
    ///     current rectangles and adopting the op's rectangles.
    ///     <para>
    ///         For example, if the first region contains (0,0,5,5) and the op is (10,10,5,5), the first region will be
    ///         cleared and replaced with (10,10,5,5).
    ///     </para>
    ///     <para>
    ///         If the op region is empty or null, the first region is cleared. This operation is useful for resetting or
    ///         overwriting region state.
    ///     </para>
    /// </summary>
    Replace = 6
}

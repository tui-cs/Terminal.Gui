using System.Text.Json.Serialization;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Determines alignment modes for <see cref="Alignment"/>.
/// </summary>
[JsonConverter (typeof (JsonStringEnumConverter<AlignmentModes>))]
[Flags]
public enum AlignmentModes
{
    /// <summary>
    ///     The items will be arranged from start (left/top) to end (right/bottom).
    /// </summary>
    StartToEnd = 0,

    /// <summary>
    ///     The items will be arranged from end (right/bottom) to start (left/top).
    /// </summary>
    EndToStart = 1,

    /// <summary>
    ///     At least one space will be added between items. Useful for justifying text where at least one space is needed.
    /// </summary>
    /// <remarks>
    ///     If the total size of the items is greater than the container size, the space between items will be ignored
    ///     starting from the end.
    /// </remarks>
    AddSpaceBetweenItems = 2,

    /// <summary>
    ///     When aligning via <see cref="Alignment.Start"/> or <see cref="Alignment.End"/>, the item opposite to the alignment
    ///     (the first or last item) will be ignored.
    /// </summary>
    /// <remarks>
    ///     If the container is smaller than the total size of the items, the end items will be clipped (their locations
    ///     will be greater than the container size).
    /// </remarks>
    /// <example>
    ///     <c>
    ///         Start: |111 2222     33333|
    ///         End:   |111     2222 33333|
    ///     </c>
    /// </example>
    IgnoreFirstOrLast = 4
}

using System.Text.Json.Serialization;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Determines the position of items when arranged in a container.
/// </summary>
[JsonConverter (typeof (JsonStringEnumConverter<Alignment>))]
public enum Alignment
{
    /// <summary>
    ///     The items will be aligned to the start (left or top) of the container.
    ///     <remarks>
    ///         <para>
    ///             If the container is smaller than the total size of the items, the end items will be clipped (their
    ///             locations
    ///             will be greater than the container size).
    ///         </para>
    ///         <para>
    ///             The <see cref="AlignmentModes"/> enumeration provides additional options for aligning items in a container.
    ///         </para>
    ///     </remarks>
    ///     <example>
    ///         <c>
    ///             |111 2222 33333    |
    ///         </c>
    ///     </example>
    /// </summary>
    Start = 0,

    /// <summary>
    ///     The items will be aligned to the end (right or bottom) of the container.
    ///     <remarks>
    ///         <para>
    ///             If the container is smaller than the total size of the items, the start items will be clipped (their
    ///             locations
    ///             will be negative).
    ///         </para>
    ///         <para>
    ///             The <see cref="AlignmentModes"/> enumeration provides additional options for aligning items in a container.
    ///         </para>
    ///     </remarks>
    ///     <example>
    ///         <c>
    ///             |    111 2222 33333|
    ///         </c>
    ///     </example>
    /// </summary>
    End,

    /// <summary>
    ///     Center in the available space.
    ///     <remarks>
    ///         <para>
    ///             If centering is not possible, the group will be left-aligned.
    ///         </para>
    ///         <para>
    ///             Extra space will be distributed between the items, biased towards the left.
    ///         </para>
    ///     </remarks>
    ///     <example>
    ///         <c>
    ///             |  111 2222 33333  |
    ///         </c>
    ///     </example>
    /// </summary>
    Center,

    /// <summary>
    ///     The items will fill the available space.
    ///     <remarks>
    ///         <para>
    ///             Extra space will be distributed between the items, biased towards the end.
    ///         </para>
    ///     </remarks>
    ///     <example>
    ///         <c>
    ///             |111  2222    33333|
    ///         </c>
    ///     </example>
    /// </summary>
    Fill
}

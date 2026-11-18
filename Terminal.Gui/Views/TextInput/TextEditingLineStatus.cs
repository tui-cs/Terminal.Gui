/// <summary>
///     Represents the status of a line during text editing operations in a <see cref="TextView"/>.
/// </summary>
/// <remarks>
///     This enum is used to track changes to text lines in the <see cref="HistoryText"/> class, enabling undo/redo
///     functionality
///     and maintaining the state of text modifications. Each value describes a specific type of change or state for a
///     line.
/// </remarks>

// ReSharper disable once CheckNamespace
public enum TextEditingLineStatus
{
    /// <summary>
    ///     Indicates that the line is in its original, unmodified state.
    /// </summary>
    Original,

    /// <summary>
    ///     Indicates that the line has been replaced with new content.
    /// </summary>
    Replaced,

    /// <summary>
    ///     Indicates that the line has been removed.
    /// </summary>
    Removed,

    /// <summary>
    ///     Indicates that a new line has been added.
    /// </summary>
    Added,

    /// <summary>
    ///     Indicates that the line's attributes (e.g., formatting or styling) have been modified.
    /// </summary>
    Attribute
}

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Parameters passed from <see cref="BorderView"/> to an <see cref="ITitleView"/> during layout.
///     Captures the border/tab state that the title view cannot derive from its own stored properties.
/// </summary>
public readonly record struct TabLayoutContext
{
    /// <summary>Gets the content border rectangle in screen coordinates.</summary>
    public required Rectangle BorderBounds { get; init; }

    /// <summary>Gets the tab offset along the tab side (in cells from the content border origin).</summary>
    public required int TabOffset { get; init; }

    /// <summary>
    ///     Gets an explicit tab length override, or <see langword="null"/> to auto-size from the title text.
    ///     When set, the title view uses this length instead of auto-sizing.
    /// </summary>
    public int? TabLengthOverride { get; init; }

    /// <summary>Gets whether this tab is focused or is the last tab (open-gap state).</summary>
    public required bool HasFocus { get; init; }

    /// <summary>Gets the <see cref="LineStyle"/> to apply to the title view's border.</summary>
    public required LineStyle? LineStyle { get; init; }

    /// <summary>Gets the title text to display in the header.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the screen origin of the owning <see cref="BorderView"/>'s viewport, used for coordinate conversion.</summary>
    public required Point ScreenOrigin { get; init; }
}

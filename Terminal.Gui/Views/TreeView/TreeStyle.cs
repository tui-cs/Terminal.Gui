namespace Terminal.Gui.Views;

/// <summary>Defines rendering options that affect how the tree is displayed.</summary>
public class TreeStyle
{
    /// <summary>
    ///     Symbol to use for branch nodes that can be collapsed (are currently expanded). Defaults to '-'. Set to null to
    ///     hide.
    /// </summary>
    public Rune? CollapseableSymbol { get; set; } = Glyphs.Collapse;

    /// <summary>
    ///     Set to <see langword="true"/> to highlight expand/collapse symbols using the
    ///     <see cref="VisualRole.Highlight"/> color.
    /// </summary>
    public bool ColorExpandSymbol { get; set; }

    /// <summary>
    ///     Symbol to use for branch nodes that can be expanded to indicate this to the user. Defaults to '+'. Set to null
    ///     to hide.
    /// </summary>
    public Rune? ExpandableSymbol { get; set; } = Glyphs.Expand;

    /// <summary>
    ///     Set to <see langword="true"/> to cause the selected item to be rendered with only the
    ///     model text highlighted. If <see langword="false"/> (the default), the entire row will
    ///     be highlighted.
    /// </summary>
    public bool HighlightModelTextOnly { get; set; }

    /// <summary>Invert foreground and background colors used to render the expand symbol.</summary>
    public bool InvertExpandSymbolColors { get; set; }

    /// <summary>
    ///     <see langword="true"/> to render vertical lines under expanded nodes to show which node belongs to which
    ///     parent. <see langword="false"/> to use only whitespace.
    /// </summary>
    public bool ShowBranchLines { get; set; } = true;
}

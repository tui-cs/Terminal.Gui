namespace Terminal.Gui.Views;

/// <summary><see cref="LinearRange{T}"/> Style</summary>
public class LinearRangeStyle
{
    /// <summary>Constructs a new instance.</summary>
    public LinearRangeStyle () { LegendAttributes = new (); }

    /// <summary>The glyph and the attribute to indicate mouse dragging.</summary>
    public Cell DragChar { get; set; }

    /// <summary>The glyph and the attribute used for empty spaces on the linear range.</summary>
    public Cell EmptyChar { get; set; }

    /// <summary>The glyph and the attribute used for the end of ranges on the linear range.</summary>
    public Cell EndRangeChar { get; set; }

    /// <summary>Legend attributes</summary>
    public LinearRangeAttributes LegendAttributes { get; set; }

    /// <summary>The glyph and the attribute used for each option (tick) on the linear range.</summary>
    public Cell OptionChar { get; set; }

    /// <summary>The glyph and the attribute used for filling in ranges on the linear range.</summary>
    public Cell RangeChar { get; set; }

    /// <summary>The glyph and the attribute used for options (ticks) that are set on the linear range.</summary>
    public Cell SetChar { get; set; }

    /// <summary>The glyph and the attribute used for spaces between options (ticks) on the linear range.</summary>
    public Cell SpaceChar { get; set; }

    /// <summary>The glyph and the attribute used for the start of ranges on the linear range.</summary>
    public Cell StartRangeChar { get; set; }
}

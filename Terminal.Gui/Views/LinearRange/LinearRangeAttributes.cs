namespace Terminal.Gui.Views;

/// <summary><see cref="LinearRange{T}"/> Legend Style</summary>
public class LinearRangeAttributes
{
    /// <summary>Attribute for the Legends Container.</summary>
    public Attribute? EmptyAttribute { get; set; }

    /// <summary>Attribute for when the respective Option is NOT Set.</summary>
    public Attribute? NormalAttribute { get; set; }

    /// <summary>Attribute for when the respective Option is Set.</summary>
    public Attribute? SetAttribute { get; set; }
}

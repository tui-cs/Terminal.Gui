namespace Terminal.Gui.Views;

/// <summary>Represents an option in a <see cref="LinearRange{T}"/> .</summary>
/// <typeparam name="T">Data type of the option.</typeparam>
public class LinearRangeOption<T>
{
    /// <summary>Creates a new empty instance of the <see cref="LinearRangeOption{T}"/> class.</summary>
    public LinearRangeOption () { }

    /// <summary>Creates a new instance of the <see cref="LinearRangeOption{T}"/> class with values for each property.</summary>
    public LinearRangeOption (string legend, Rune legendAbbr, T data)
    {
        Legend = legend;
        LegendAbbr = legendAbbr;
        Data = data;
    }

    /// <summary>Event fired when an option has changed.</summary>
    public event EventHandler<LinearRangeOptionEventArgs>? Changed;

    /// <summary>Custom data of the option.</summary>
    public T? Data { get; set; }

    /// <summary>Legend of the option.</summary>
    public string? Legend { get; set; }

    /// <summary>
    ///     Abbreviation of the Legend. Used when the inner spacing is too small to fit
    ///     <see cref="Legend"/>.
    /// </summary>
    public Rune LegendAbbr { get; set; }

    /// <summary>Event Raised when this option is set.</summary>
    public event EventHandler<LinearRangeOptionEventArgs>? Set;

    /// <summary>Creates a human-readable string that represents this <see cref="LinearRangeOption{T}"/>.</summary>
    public override string ToString () => "{Legend=" + Legend + ", LegendAbbr=" + LegendAbbr + ", Data=" + Data + "}";

    /// <summary>Event Raised when this option is unset.</summary>
    public event EventHandler<LinearRangeOptionEventArgs>? UnSet;

    /// <summary>To Raise the <see cref="Changed"/> event from the LinearRange.</summary>
    internal void OnChanged (bool isSet) { Changed?.Invoke (this, new (isSet)); }

    /// <summary>To Raise the <see cref="Set"/> event from the LinearRange.</summary>
    internal void OnSet () { Set?.Invoke (this, new (true)); }

    /// <summary>To Raise the <see cref="UnSet"/> event from the LinearRange.</summary>
    internal void OnUnSet () { UnSet?.Invoke (this, new (false)); }
}

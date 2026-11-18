namespace Terminal.Gui.Views;

/// <summary><see cref="EventArgs"/> for <see cref="LinearRange{T}"/> <see cref="LinearRangeOption{T}"/> events.</summary>
public class LinearRangeOptionEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="LinearRangeOptionEventArgs"/></summary>
    /// <param name="isSet"> indicates whether the option is set</param>
    public LinearRangeOptionEventArgs (bool isSet) { IsSet = isSet; }

    /// <summary>Gets whether the option is set or not.</summary>
    public bool IsSet { get; }
}

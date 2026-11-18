namespace Terminal.Gui.Views;

/// <summary><see cref="EventArgs"/> for <see cref="LinearRange{T}"/> events.</summary>
public class LinearRangeEventArgs<T> : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="LinearRangeEventArgs{T}"/></summary>
    /// <param name="options">The current options.</param>
    /// <param name="focused">Index of the option that is focused. -1 if no option has the focus.</param>
    public LinearRangeEventArgs (Dictionary<int, LinearRangeOption<T>> options, int focused = -1)
    {
        Options = options;
        Focused = focused;
        Cancel = false;
    }

    /// <summary>If set to true, the focus operation will be canceled, if applicable.</summary>
    public bool Cancel { get; set; }

    /// <summary>Gets or sets the index of the option that is focused.</summary>
    public int Focused { get; set; }

    /// <summary>Gets/sets whether the option is set or not.</summary>
    public Dictionary<int, LinearRangeOption<T>> Options { get; set; }
}

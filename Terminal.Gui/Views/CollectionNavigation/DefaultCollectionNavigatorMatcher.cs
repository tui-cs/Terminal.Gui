namespace Terminal.Gui.Views;

/// <summary>
///     Default implementation of <see cref="ICollectionNavigatorMatcher"/>, performs
///     case-insensitive (see <see cref="Comparer"/>) matching of items based on
///     <see cref="object.ToString()"/>.
/// </summary>
internal class DefaultCollectionNavigatorMatcher : ICollectionNavigatorMatcher
{
    /// <summary>The comparer function to use when searching the collection.</summary>
    public StringComparison Comparer { get; set; } = StringComparison.InvariantCultureIgnoreCase;

    /// <inheritdoc/>
    public virtual bool IsMatch (string search, object? value)
    {
        return value?.ToString ()?.StartsWith (search, Comparer) ?? false;
    }

    /// <summary>
    ///     Returns true if <paramref name="key"/> is key searchable key (e.g. letters, numbers, etc.) that are valid to pass
    ///     to this class for search filtering.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool IsCompatibleKey (Key key)
    {
        Rune rune = key.AsRune;

        return rune != default (Rune) && !Rune.IsControl (rune) && key is { IsAlt: false, IsCtrl: false };
    }
}

namespace Terminal.Gui.Configuration;

/// <summary>
///     Abstracts color scheme management. Provides access to named schemes.
/// </summary>
public interface ISchemeManager
{
    /// <summary>Gets the names of all registered schemes.</summary>
    IReadOnlyList<string> SchemeNames { get; }

    /// <summary>Gets a scheme by name.</summary>
    /// <param name="name">The scheme name (case-insensitive).</param>
    /// <returns>The scheme, or <see langword="null"/> if not found.</returns>
    Scheme? GetScheme (string name);

    /// <summary>Adds or updates a named scheme.</summary>
    /// <param name="name">The scheme name.</param>
    /// <param name="scheme">The scheme to add/update.</param>
    void AddScheme (string name, Scheme scheme);
}

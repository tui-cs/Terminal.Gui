namespace Terminal.Gui.Configuration;

/// <summary>
///     MEC-backed implementation of <see cref="ISchemeManager"/>. Delegates to the static
///     <see cref="SchemeManager"/> facade. Distinct from that facade because instance
///     <see cref="ISchemeManager.GetScheme"/> cannot share a name with the static method.
/// </summary>
public class MecSchemeManager : ISchemeManager
{
    /// <inheritdoc/>
    public IReadOnlyList<string> SchemeNames
    {
        get
        {
            try
            {
                return SchemeManager.GetSchemeNames ().ToList ();
            }
            catch
            {
                return [];
            }
        }
    }

    /// <inheritdoc/>
    public Scheme? GetScheme (string name)
    {
        try
        {
            return SchemeManager.GetScheme (name);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public void AddScheme (string name, Scheme scheme)
    {
        SchemeManager.AddScheme (name, scheme);
    }
}

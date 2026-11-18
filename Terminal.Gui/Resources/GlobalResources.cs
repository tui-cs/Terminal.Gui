
using System.Collections;
using System.Globalization;
using System.Resources;

namespace Terminal.Gui.App;

/// <summary>
///     Provide static access to the ResourceManagerWrapper
/// </summary>
public static class GlobalResources
{
    private static readonly ResourceManagerWrapper _resourceManagerWrapper;

    static GlobalResources ()
    {
        // Initialize the ResourceManagerWrapper once
        var resourceManager = new ResourceManager (typeof (Strings));
        _resourceManagerWrapper = new (resourceManager);
    }

    /// <summary>
    ///     Looks up a resource value for a particular name.  Looks in the specified CultureInfo, and if not found, all parent
    ///     CultureInfos.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="culture"></param>
    /// <returns>Null if the resource was not found in the current culture or the invariant culture.</returns>
    public static object GetObject (string name, CultureInfo culture = null!) { return _resourceManagerWrapper.GetObject (name, culture); }

    /// <summary>
    ///     Looks up a set of resources for a particular CultureInfo. This is not useful for most users of the ResourceManager
    ///     - call GetString() or GetObject() instead. The parameters let you control whether the ResourceSet is created if it
    ///     hasn't yet been loaded and if parent CultureInfos should be loaded as well for resource inheritance.
    /// </summary>
    /// <param name="culture"></param>
    /// <param name="createIfNotExists"></param>
    /// <param name="tryParents"></param>
    /// <returns></returns>
    public static ResourceSet? GetResourceSet (CultureInfo culture, bool createIfNotExists, bool tryParents)
    {
        return _resourceManagerWrapper.GetResourceSet (culture, createIfNotExists, tryParents)!;
    }

    /// <summary>
    ///     Looks up a set of resources for a particular CultureInfo. This is not useful for most users of the ResourceManager
    ///     - call GetString() or GetObject() instead. The parameters let you control whether the ResourceSet is created if it
    ///     hasn't yet been loaded and if parent CultureInfos should be loaded as well for resource inheritance. Allows
    ///     filtering of resources.
    /// </summary>
    /// <param name="culture"></param>
    /// <param name="createIfNotExists"></param>
    /// <param name="tryParents"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static ResourceSet? GetResourceSet (CultureInfo culture, bool createIfNotExists, bool tryParents, Func<DictionaryEntry, bool>? filter)
    {
        return _resourceManagerWrapper.GetResourceSet (culture, createIfNotExists, tryParents, filter)!;
    }

    /// <summary>
    ///     Looks up a resource value for a particular name. Looks in the specified CultureInfo, and if not found, all parent
    ///     CultureInfos.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="culture"></param>
    /// <returns>Null if the resource was not found in the current culture or the invariant culture.</returns>
    public static string? GetString (string name, CultureInfo? culture = null!) { return _resourceManagerWrapper.GetString (name, culture); }
}

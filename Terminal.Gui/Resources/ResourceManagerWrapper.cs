
using System.Collections;
using System.Globalization;
using System.Resources;

namespace Terminal.Gui.App;

internal class ResourceManagerWrapper (ResourceManager resourceManager)
{
    private readonly ResourceManager _resourceManager = resourceManager ?? throw new ArgumentNullException (nameof (resourceManager));

    // Optionally, expose other ResourceManager methods as needed
    public object GetObject (string name, CultureInfo culture = null!)
    {
        object value = _resourceManager.GetObject (name, culture)!;

        if (Equals (culture, CultureInfo.InvariantCulture))
        {
            return value;
        }

        if (value is null)
        {
            value = _resourceManager.GetObject (name, CultureInfo.InvariantCulture)!;
        }

        return value;
    }

    public ResourceSet? GetResourceSet (CultureInfo culture, bool createIfNotExists, bool tryParents)
    {
        ResourceSet value = _resourceManager.GetResourceSet (culture, createIfNotExists, tryParents)!;

        if (Equals (culture, CultureInfo.InvariantCulture))
        {
            return value;
        }

        if (value!.Cast<DictionaryEntry> ().Any ())
        {
            value = _resourceManager.GetResourceSet (CultureInfo.InvariantCulture, createIfNotExists, tryParents)!;
        }

        return value;
    }

    public ResourceSet? GetResourceSet (CultureInfo culture, bool createIfNotExists, bool tryParents, Func<DictionaryEntry, bool>? filter)
    {
        ResourceSet value = _resourceManager.GetResourceSet (culture, createIfNotExists, tryParents)!;

        IEnumerable<DictionaryEntry> filteredEntries = value.Cast<DictionaryEntry> ().Where (filter ?? (_ => true));

        ResourceSet? filteredValue = ConvertToResourceSet (filteredEntries);

        if (Equals (culture, CultureInfo.InvariantCulture))
        {
            return filteredValue;
        }

        if (!filteredValue!.Cast<DictionaryEntry> ().Any ())
        {
            filteredValue = GetResourceSet (CultureInfo.InvariantCulture, createIfNotExists, tryParents, filter)!;
        }

        return filteredValue;
    }

    public string? GetString (string name, CultureInfo? culture = null!)
    {
        // Attempt to get the string for the specified culture
        string? value = _resourceManager.GetString (name, culture)!;

        // If it's already using the invariant culture return
        if (Equals (culture, CultureInfo.InvariantCulture))
        {
            return value;
        }

        // If the string is empty or null, fall back to the invariant culture
        if (string.IsNullOrEmpty (value))
        {
            value = _resourceManager.GetString (name, CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static ResourceSet? ConvertToResourceSet (IEnumerable<DictionaryEntry> entries)
    {
        using var memoryStream = new MemoryStream ();

        using var resourceWriter = new ResourceWriter (memoryStream);

        // Add each DictionaryEntry to the ResourceWriter
        foreach (DictionaryEntry entry in entries)
        {
            resourceWriter.AddResource ((string)entry.Key, entry.Value);
        }

        // Finish writing to the stream
        resourceWriter.Generate ();

        // Reset the stream position to the beginning
        memoryStream.Position = 0;

        // Create a ResourceSet from the MemoryStream
        var resourceSet = new ResourceSet (memoryStream);

        return resourceSet;
    }
}

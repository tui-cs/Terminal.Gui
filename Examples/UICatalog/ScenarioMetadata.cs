#nullable enable
using System;
using System.Linq;

namespace UICatalog;

/// <summary>Defines the metadata (Name and Description) for a <see cref="Scenario"/></summary>
[AttributeUsage (AttributeTargets.Class)]
public class ScenarioMetadata (string name, string description) : System.Attribute
{
    /// <summary><see cref="Scenario"/> Description</summary>
    public string Description { get; set; } = description;

    /// <summary>Static helper function to get the <see cref="Scenario"/> Description given a Type</summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static string GetDescription (Type t)
    {
        if (GetCustomAttributes (t).FirstOrDefault (a => a is ScenarioMetadata) is ScenarioMetadata { } metadata)
        {
            return metadata.Description;
        }

        return string.Empty;
    }

    /// <summary>Static helper function to get the <see cref="Scenario"/> Name given a Type</summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static string GetName (Type t)
    {
        if (GetCustomAttributes (t).FirstOrDefault (a => a is ScenarioMetadata) is ScenarioMetadata { } metadata)
        {
            return metadata.Name;
        }

        return string.Empty;
    }

    /// <summary><see cref="Scenario"/> Name</summary>
    public string Name { get; set; } = name;
}

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace UICatalog;

/// <summary>Defines the category names used to categorize a <see cref="Scenario"/></summary>
[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
public class ScenarioCategory (string name) : System.Attribute
{
    /// <summary>Static helper function to get the <see cref="Scenario"/> Categories given a Type</summary>
    /// <param name="t"></param>
    /// <returns>list of category names</returns>
    public static List<string> GetCategories (Type t)
    {
        return GetCustomAttributes (t)
               .ToList ()
               .Where (a => a is ScenarioCategory)
               .Select<System.Attribute, string> (a => ((ScenarioCategory)a).Name)
               .ToList ();
    }

    /// <summary>Static helper function to get the <see cref="Scenario"/> Name given a Type</summary>
    /// <param name="t"></param>
    /// <returns>Name of the category</returns>
    public static string GetName (Type t)
    {
        if (GetCustomAttributes (t).FirstOrDefault (a => a is ScenarioMetadata) is ScenarioMetadata { } metadata)
        {
            return metadata.Name;
        }

        return string.Empty;
    }

    /// <summary>Category Name</summary>
    public string Name { get; set; } = name;
}

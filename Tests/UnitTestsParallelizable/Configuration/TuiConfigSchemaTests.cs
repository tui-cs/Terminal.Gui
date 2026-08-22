// Grok - grok-4.6
using System.Text.Json;

namespace ConfigurationTests;

/// <summary>
///     Locks the shipped <c>docfx/schemas/tui-config-schema.json</c> to the nested MEC shape.
/// </summary>
public class TuiConfigSchemaTests
{
    private static readonly JsonDocumentOptions JsonOptions = new ()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    [Fact]
    public void Schema_UsesNestedMecShape ()
    {
        using JsonDocument doc = JsonDocument.Parse (File.ReadAllText (FindSchemaPath ()), JsonOptions);
        JsonElement properties = doc.RootElement.GetProperty ("properties");

        Assert.True (properties.TryGetProperty ("Application", out _));
        Assert.True (properties.TryGetProperty ("View", out _));
        Assert.True (properties.TryGetProperty ("Button", out _));
        Assert.True (properties.TryGetProperty ("Glyphs", out _));
        Assert.True (properties.TryGetProperty ("PopoverMenu", out _));
        Assert.True (properties.TryGetProperty ("Themes", out JsonElement themes));
        Assert.Equal ("object", themes.GetProperty ("type").GetString ());
        Assert.False (themes.TryGetProperty ("items", out _));

        Assert.False (properties.TryGetProperty ("ConfigurationManager.ThrowOnJsonErrors", out _));
        Assert.False (properties.TryGetProperty ("Button.DefaultShadow", out _));
        Assert.False (properties.TryGetProperty ("Key.Separator", out _));

        JsonElement definitions = doc.RootElement.GetProperty ("definitions");
        JsonElement applicationSettings = definitions.GetProperty ("applicationSettings").GetProperty ("properties");
        Assert.True (applicationSettings.TryGetProperty ("DefaultKeyBindings", out _));
        JsonElement viewSettings = definitions.GetProperty ("viewSettings").GetProperty ("properties");
        Assert.True (viewSettings.TryGetProperty ("DefaultKeyBindings", out _));
        Assert.True (viewSettings.TryGetProperty ("ViewKeyBindings", out _));
    }

    [Fact]
    public void LibraryConfigJson_PointsAtHostedSchema_AndUsesNestedThemes ()
    {
        using JsonDocument doc = JsonDocument.Parse (File.ReadAllText (FindLibraryConfigPath ()), JsonOptions);
        JsonElement root = doc.RootElement;

        Assert.Equal (
                     "https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
                     root.GetProperty ("$schema").GetString ());
        Assert.Equal (JsonValueKind.Object, root.GetProperty ("Themes").ValueKind);
        Assert.Equal (JsonValueKind.Object, root.GetProperty ("Application").ValueKind);
        Assert.False (root.TryGetProperty ("ConfigurationManager.ThrowOnJsonErrors", out _));
    }

    private static string FindSchemaPath () =>
        FindRepoFile (Path.Combine ("docfx", "schemas", "tui-config-schema.json"));

    private static string FindLibraryConfigPath () =>
        FindRepoFile (Path.Combine ("Terminal.Gui", "Resources", "config.json"));

    private static string FindRepoFile (string relativePath)
    {
        DirectoryInfo? dir = new (AppContext.BaseDirectory);

        while (dir is not null)
        {
            string candidate = Path.Combine (dir.FullName, relativePath);

            if (File.Exists (candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException (relativePath);
    }
}

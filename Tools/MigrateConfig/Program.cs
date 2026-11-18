// Migrates a pre-MEC Terminal.Gui config.json (flat-key, array-themes shape)
// to the nested MEC-native shape consumed by TuiConfigurationBuilder.
//
// Transforms applied (recursively):
//   1. Any object property name containing '.' is split into nested objects.
//      "Button.DefaultShadow": "Opaque"   ->   "Button": { "DefaultShadow": "Opaque" }
//   2. "Themes" as an array of single-key objects becomes a dictionary.
//      "Themes": [ { "Dark": { ... } } ]  ->   "Themes": { "Dark": { ... } }
//   3. "Schemes" inside a theme follows the same array -> dictionary collapse.
//
// Usage:
//   migrate-tui-config <input.json> [output.json]
//
// If output.json is omitted, the migrated JSON is written to stdout.
// Exit codes: 0 success, 1 usage error, 2 I/O or JSON parse error.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Terminal.Gui.Tools.MigrateConfig;

internal static class Program
{
    private static int Main (string [] args)
    {
        if (args.Length is < 1 or > 2)
        {
            Console.Error.WriteLine ("Usage: migrate-tui-config <input.json> [output.json]");
            Console.Error.WriteLine ("If output.json is omitted, the result is written to stdout.");

            return 1;
        }

        string inputPath = args [0];
        string? outputPath = args.Length == 2 ? args [1] : null;

        string text;

        try
        {
            text = File.ReadAllText (inputPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine ($"Could not read \"{inputPath}\": {ex.Message}");

            return 2;
        }

        JsonNodeOptions nodeOpts = new () { PropertyNameCaseInsensitive = false };

        JsonDocumentOptions docOpts = new ()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        JsonNode? root;

        try
        {
            root = JsonNode.Parse (text, nodeOpts, docOpts);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine ($"Could not parse \"{inputPath}\" as JSON: {ex.Message}");

            return 2;
        }

        if (root is not JsonObject rootObj)
        {
            Console.Error.WriteLine ("Top-level JSON value is not an object; cannot migrate.");

            return 2;
        }

        JsonObject migrated = ConfigJsonMigrator.MigrateObject (rootObj);

        JsonSerializerOptions writeOpts = new ()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string output = migrated.ToJsonString (writeOpts);

        if (outputPath is null)
        {
            Console.WriteLine (output);
        }
        else
        {
            try
            {
                File.WriteAllText (outputPath, output);
                Console.Error.WriteLine ($"Migrated \"{inputPath}\" -> \"{outputPath}\".");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine ($"Could not write \"{outputPath}\": {ex.Message}");

                return 2;
            }
        }

        return 0;
    }

}

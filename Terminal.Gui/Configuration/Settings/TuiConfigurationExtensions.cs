using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Terminal.Gui.App;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Extension methods for <see cref="IConfigurationBuilder"/> that add Terminal.Gui configuration sources
///     in the correct precedence order (matching MEC provider order).
/// </summary>
public static class TuiConfigurationExtensions
{
    /// <summary>The name of the TUI configuration folder.</summary>
    public const string TUI_CONFIG_FOLDER = ".tui";

    /// <summary>The name of the TUI configuration environment variable.</summary>
    public const string TUI_CONFIG_ENV = "TUI_CONFIG";

    /// <summary>The default config filename.</summary>
    public const string CONFIG_FILENAME = "config.json";

    /// <summary>
    ///     Adds the Terminal.Gui library's embedded <c>config.json</c> as a configuration source.
    ///     This is the lowest-priority file-based source (above hard-coded POCO defaults).
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IConfigurationBuilder AddTuiLibraryDefaults (this IConfigurationBuilder builder) => AddTuiLibraryDefaults (builder, null);

    internal static IConfigurationBuilder AddTuiLibraryDefaults (IConfigurationBuilder builder, List<JsonObject>? jsonSources)
    {
        Assembly libraryAssembly = typeof (TuiConfigurationExtensions).Assembly;
        string resourceName = $"Terminal.Gui.Resources.{CONFIG_FILENAME}";

        Stream? stream = libraryAssembly.GetManifestResourceStream (resourceName);

        if (stream is null)
        {
            return builder;
        }

        using StreamReader reader = new (stream);
        string json = reader.ReadToEnd ();

        return AddTuiInlineJson (builder, json, $"Embedded resource \"{resourceName}\"", jsonSources);
    }

    /// <summary>
    ///     Adds the entry assembly's embedded <c>config.json</c> as a configuration source.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="appName">The application name (used for app-specific config files).</param>
    /// <returns>The builder for chaining.</returns>
    public static IConfigurationBuilder AddTuiAppDefaults (this IConfigurationBuilder builder, string? appName = null) =>
        AddTuiAppDefaults (builder, appName, null);

    internal static IConfigurationBuilder AddTuiAppDefaults (IConfigurationBuilder builder, string? appName, List<JsonObject>? jsonSources)
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly ();

        if (entryAssembly is null)
        {
            return builder;
        }

        string? resourceName = entryAssembly
                               .GetManifestResourceNames ()
                               .FirstOrDefault (x => x.EndsWith (CONFIG_FILENAME, StringComparison.Ordinal));

        if (string.IsNullOrEmpty (resourceName))
        {
            return builder;
        }

        Stream? stream = entryAssembly.GetManifestResourceStream (resourceName);

        if (stream is null)
        {
            return builder;
        }

        using StreamReader reader = new (stream);
        string json = reader.ReadToEnd ();

        return AddTuiInlineJson (builder, json, $"Embedded resource \"{resourceName}\"", jsonSources);
    }

    /// <summary>
    ///     Adds user-level configuration files from the standard locations:
    ///     <list type="bullet">
    ///         <item><c>~/.tui/config.json</c> (GlobalHome)</item>
    ///         <item><c>./.tui/config.json</c> (GlobalCurrent)</item>
    ///         <item><c>~/.tui/{appName}.config.json</c> (AppHome)</item>
    ///         <item><c>./.tui/{appName}.config.json</c> (AppCurrent)</item>
    ///     </list>
    ///     Files are optional — missing files are silently skipped. Later files override earlier ones.
    ///     Files that fail to read or parse are collected in <see cref="TuiJsonErrors"/> and skipped;
    ///     legacy-shaped files are warned about and skipped.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="appName">The application name for app-specific config files. If null, app-specific files are skipped.</param>
    /// <param name="currentDirectory">
    ///     The directory the <c>./.tui/</c> paths are resolved against. If null, uses
    ///     <see cref="Environment.CurrentDirectory"/> (never the app's install directory).
    /// </param>
    /// <returns>The builder for chaining.</returns>
    public static IConfigurationBuilder AddTuiUserFiles (this IConfigurationBuilder builder, string? appName = null, string? currentDirectory = null) =>
        AddTuiUserFiles (builder, appName, currentDirectory, null);

    internal static IConfigurationBuilder AddTuiUserFiles (IConfigurationBuilder builder, string? appName, string? currentDirectory, List<JsonObject>? jsonSources)
    {
        string homeDir = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
        string currentDir = Path.GetFullPath (currentDirectory ?? Environment.CurrentDirectory);

        AddUserFile (builder, Path.Combine (homeDir, TUI_CONFIG_FOLDER, CONFIG_FILENAME), jsonSources);
        AddUserFile (builder, Path.Combine (currentDir, TUI_CONFIG_FOLDER, CONFIG_FILENAME), jsonSources);

        if (!string.IsNullOrEmpty (appName))
        {
            AddUserFile (builder, Path.Combine (homeDir, TUI_CONFIG_FOLDER, $"{appName}.{CONFIG_FILENAME}"), jsonSources);
            AddUserFile (builder, Path.Combine (currentDir, TUI_CONFIG_FOLDER, $"{appName}.{CONFIG_FILENAME}"), jsonSources);
        }

        return builder;
    }

    /// <summary>
    ///     Adds the <c>TUI_CONFIG</c> environment variable as a JSON configuration source.
    ///     The environment variable value is treated as inline JSON content.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IConfigurationBuilder AddTuiEnvironmentVariable (this IConfigurationBuilder builder) => AddTuiEnvironmentVariable (builder, null);

    internal static IConfigurationBuilder AddTuiEnvironmentVariable (IConfigurationBuilder builder, List<JsonObject>? jsonSources)
    {
        string? envConfig = Environment.GetEnvironmentVariable (TUI_CONFIG_ENV);

        if (string.IsNullOrEmpty (envConfig))
        {
            return builder;
        }

        return AddTuiInlineJson (builder, envConfig, $"{TUI_CONFIG_ENV} environment variable", jsonSources);
    }

    /// <summary>
    ///     Adds an in-memory JSON string as the highest-priority configuration source.
    ///     Highest-priority in-memory JSON overlay (UICatalog, tests, and app startup).
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="json">A JSON string containing configuration overrides.</param>
    /// <returns>The builder for chaining.</returns>
    public static IConfigurationBuilder AddTuiRuntimeConfig (this IConfigurationBuilder builder, string? json) =>
        AddTuiRuntimeConfig (builder, json, null);

    internal static IConfigurationBuilder AddTuiRuntimeConfig (IConfigurationBuilder builder, string? json, List<JsonObject>? jsonSources)
    {
        if (string.IsNullOrEmpty (json))
        {
            return builder;
        }

        return AddTuiInlineJson (builder, json, "RuntimeConfig", jsonSources);
    }

    /// <summary>
    ///     Adds inline JSON as a stream source after validating it (parsed exactly once). Malformed JSON and
    ///     non-object roots are collected in <see cref="TuiJsonErrors"/> and skipped; legacy-shaped JSON is
    ///     warned about and skipped. A source must never crash the build — it runs from a module initializer.
    ///     When <paramref name="jsonSources"/> is provided, the parsed root object is appended to it so callers
    ///     can build a raw-JSON merged view of all sources (atomic array overrides).
    /// </summary>
    internal static IConfigurationBuilder AddTuiInlineJson (IConfigurationBuilder builder, string json, string sourceName, List<JsonObject>? jsonSources = null)
    {
        JsonObject obj;

        try
        {
            JsonNode? node = JsonNode.Parse (json, documentOptions: new () { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            if (node is not JsonObject parsed)
            {
                TuiJsonErrors.Add ($"{sourceName}: The top-level JSON element must be an object.");

                return builder;
            }

            if (IsLegacyShape (parsed))
            {
                Logging.Warning (
                                 $"{sourceName} uses a pre-MEC (flat-key or array-Themes) shape and is not applied. Convert it with Tools/MigrateConfig. See docfx/docs/migrate-cm-to-mec.md.");

                return builder;
            }

            obj = parsed;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException)
        {
            // ArgumentException/InvalidOperationException: JsonObject materialization rejects duplicate
            // keys (which MEC's own parser would also reject — but inside builder.Build(), past every
            // per-source handler). Skip the source here so one bad source cannot discard the rest.
            TuiJsonErrors.Add ($"{sourceName}: {ex.Message}");

            return builder;
        }

        byte [] bytes = System.Text.Encoding.UTF8.GetBytes (json);
        MemoryStream stream = new (bytes);
        builder.AddJsonStream (stream);
        jsonSources?.Add (obj);

        return builder;
    }

    private static void AddUserFile (IConfigurationBuilder builder, string path, List<JsonObject>? jsonSources)
    {
        if (!File.Exists (path))
        {
            return;
        }

        string text;

        try
        {
            text = File.ReadAllText (path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // An unreadable file must not crash the build (it runs from a module initializer);
            // the error is printed at shutdown.
            TuiJsonErrors.Add ($"Configuration file \"{path}\": {ex.Message}");

            return;
        }

        AddTuiInlineJson (builder, text, $"Configuration file \"{path}\"", jsonSources);
    }

    /// <summary>
    ///     Returns <see langword="true"/> when <paramref name="json"/> uses a pre-MEC shape:
    ///     top-level dotted keys (e.g. <c>Button.DefaultShadow</c>) or <c>Themes</c>/<c>Schemes</c>
    ///     as an array of single-key objects.
    /// </summary>
    public static bool IsLegacyConfigShape (string json)
    {
        if (string.IsNullOrWhiteSpace (json))
        {
            return false;
        }

        JsonNode? node;

        try
        {
            node = JsonNode.Parse (json, documentOptions: new () { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (JsonException)
        {
            return false;
        }

        if (node is not JsonObject obj)
        {
            return false;
        }

        return IsLegacyShape (obj);
    }

    private static bool IsLegacyShape (JsonObject obj)
    {
        foreach (KeyValuePair<string, JsonNode?> pair in obj)
        {
            if (pair.Key.Contains ('.'))
            {
                return true;
            }

            if (pair.Key is "Themes" or "Schemes" && pair.Value is JsonArray)
            {
                return true;
            }
        }

        return false;
    }
}

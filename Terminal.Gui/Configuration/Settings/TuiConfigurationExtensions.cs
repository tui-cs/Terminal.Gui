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
    public static IConfigurationBuilder AddTuiLibraryDefaults (this IConfigurationBuilder builder)
    {
        Assembly libraryAssembly = typeof (TuiConfigurationExtensions).Assembly;
        string resourceName = $"Terminal.Gui.Resources.{CONFIG_FILENAME}";

        Stream? stream = libraryAssembly.GetManifestResourceStream (resourceName);

        if (stream is not null)
        {
            builder.AddJsonStream (stream);
        }

        return builder;
    }

    /// <summary>
    ///     Adds the entry assembly's embedded <c>config.json</c> as a configuration source.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="appName">The application name (used for app-specific config files).</param>
    /// <returns>The builder for chaining.</returns>
    public static IConfigurationBuilder AddTuiAppDefaults (this IConfigurationBuilder builder, string? appName = null)
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

        return AddTuiInlineJson (builder, json, $"Embedded resource \"{resourceName}\"");
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
    ///     Files that fail to load are collected in <see cref="TuiJsonErrors"/> and skipped; legacy-shaped
    ///     files are warned about and skipped.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="appName">The application name for app-specific config files. If null, app-specific files are skipped.</param>
    /// <param name="currentDirectory">
    ///     The directory the <c>./.tui/</c> paths are resolved against. If null, uses
    ///     <see cref="Environment.CurrentDirectory"/> (never the app's install directory).
    /// </param>
    /// <returns>The builder for chaining.</returns>
    public static IConfigurationBuilder AddTuiUserFiles (this IConfigurationBuilder builder, string? appName = null, string? currentDirectory = null)
    {
        string homeDir = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
        string currentDir = Path.GetFullPath (currentDirectory ?? Environment.CurrentDirectory);

        builder.SetFileLoadExceptionHandler (HandleFileLoadException);

        AddUserFile (builder, Path.Combine (homeDir, TUI_CONFIG_FOLDER, CONFIG_FILENAME));
        AddUserFile (builder, Path.Combine (currentDir, TUI_CONFIG_FOLDER, CONFIG_FILENAME));

        if (!string.IsNullOrEmpty (appName))
        {
            AddUserFile (builder, Path.Combine (homeDir, TUI_CONFIG_FOLDER, $"{appName}.{CONFIG_FILENAME}"));
            AddUserFile (builder, Path.Combine (currentDir, TUI_CONFIG_FOLDER, $"{appName}.{CONFIG_FILENAME}"));
        }

        return builder;
    }

    /// <summary>
    ///     Adds the <c>TUI_CONFIG</c> environment variable as a JSON configuration source.
    ///     The environment variable value is treated as inline JSON content.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IConfigurationBuilder AddTuiEnvironmentVariable (this IConfigurationBuilder builder)
    {
        string? envConfig = Environment.GetEnvironmentVariable (TUI_CONFIG_ENV);

        if (string.IsNullOrEmpty (envConfig))
        {
            return builder;
        }

        return AddTuiInlineJson (builder, envConfig, $"{TUI_CONFIG_ENV} environment variable");
    }

    /// <summary>
    ///     Adds an in-memory JSON string as the highest-priority configuration source.
    ///     Highest-priority in-memory JSON overlay (UICatalog, tests, and app startup).
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="json">A JSON string containing configuration overrides.</param>
    /// <returns>The builder for chaining.</returns>
    public static IConfigurationBuilder AddTuiRuntimeConfig (this IConfigurationBuilder builder, string? json)
    {
        if (string.IsNullOrEmpty (json))
        {
            return builder;
        }

        return AddTuiInlineJson (builder, json, "RuntimeConfig");
    }

    /// <summary>
    ///     Adds inline JSON as a stream source after validating it. Legacy-shaped JSON is warned about
    ///     and skipped; malformed JSON is collected in <see cref="TuiJsonErrors"/> and skipped. A source
    ///     must never crash the build — it runs from a module initializer.
    /// </summary>
    internal static IConfigurationBuilder AddTuiInlineJson (IConfigurationBuilder builder, string json, string sourceName)
    {
        if (IsLegacyConfigShape (json))
        {
            Logging.Warning (
                             $"{sourceName} uses a pre-MEC (flat-key or array-Themes) shape and is not applied. Convert it with Tools/MigrateConfig. See docfx/docs/migrate-cm-to-mec.md.");

            return builder;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse (
                                                              json,
                                                              new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (JsonException ex)
        {
            TuiJsonErrors.Add ($"{sourceName}: {ex.Message}");

            return builder;
        }

        byte [] bytes = System.Text.Encoding.UTF8.GetBytes (json);
        MemoryStream stream = new (bytes);
        builder.AddJsonStream (stream);

        return builder;
    }

    private static void AddUserFile (IConfigurationBuilder builder, string path)
    {
        if (IsLegacyConfigFile (path))
        {
            return;
        }

        builder.AddJsonFile (path, optional: true, reloadOnChange: false);
    }

    private static void HandleFileLoadException (FileLoadExceptionContext context)
    {
        TuiJsonErrors.Add ($"Configuration file \"{context.Provider.Source.Path}\": {context.Exception.Message}");
        context.Ignore = true;
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

    private static bool IsLegacyConfigFile (string path)
    {
        if (!File.Exists (path))
        {
            return false;
        }

        string text;

        try
        {
            text = File.ReadAllText (path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Cannot inspect the shape here. If MEC cannot read it either, the load-exception
            // handler collects the error; the build must not crash (it runs from a module initializer).
            return false;
        }

        if (!IsLegacyConfigShape (text))
        {
            return false;
        }

        Logging.Warning (
                         $"Configuration file \"{path}\" uses a pre-MEC (flat-key or array-Themes) shape and is not applied. Convert it with Tools/MigrateConfig. See docfx/docs/migrate-cm-to-mec.md.");

        return true;
    }
}

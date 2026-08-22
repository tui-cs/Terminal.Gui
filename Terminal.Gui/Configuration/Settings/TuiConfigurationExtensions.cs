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

        if (stream is not null)
        {
            builder.AddJsonStream (stream);
        }

        return builder;
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
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="appName">The application name for app-specific config files. If null, app-specific files are skipped.</param>
    /// <returns>The builder for chaining.</returns>
    public static IConfigurationBuilder AddTuiUserFiles (this IConfigurationBuilder builder, string? appName = null)
    {
        string homeDir = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
        string globalHomePath = Path.Combine (homeDir, TUI_CONFIG_FOLDER, CONFIG_FILENAME);
        string globalCurrentPath = Path.Combine (".", TUI_CONFIG_FOLDER, CONFIG_FILENAME);

        WarnIfLegacyConfigFile (globalHomePath);
        builder.AddJsonFile (globalHomePath, optional: true, reloadOnChange: false);
        WarnIfLegacyConfigFile (globalCurrentPath);
        builder.AddJsonFile (globalCurrentPath, optional: true, reloadOnChange: false);

        if (!string.IsNullOrEmpty (appName))
        {
            string appHomePath = Path.Combine (homeDir, TUI_CONFIG_FOLDER, $"{appName}.{CONFIG_FILENAME}");
            string appCurrentPath = Path.Combine (".", TUI_CONFIG_FOLDER, $"{appName}.{CONFIG_FILENAME}");

            WarnIfLegacyConfigFile (appHomePath);
            builder.AddJsonFile (appHomePath, optional: true, reloadOnChange: false);
            WarnIfLegacyConfigFile (appCurrentPath);
            builder.AddJsonFile (appCurrentPath, optional: true, reloadOnChange: false);
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

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes (envConfig);
        MemoryStream stream = new (bytes);
        builder.AddJsonStream (stream);

        return builder;
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

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes (json);
        MemoryStream stream = new (bytes);
        builder.AddJsonStream (stream);

        return builder;
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

    private static void WarnIfLegacyConfigFile (string path)
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
        catch (IOException)
        {
            return;
        }

        if (!IsLegacyConfigShape (text))
        {
            return;
        }

        Logging.Warning (
                         $"Configuration file \"{path}\" uses a pre-MEC (flat-key or array-Themes) shape and is not applied. Convert it with Tools/MigrateConfig. See docfx/docs/migrate-cm-to-mec.md.");
    }
}

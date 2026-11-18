// Claude - Fable 5
using Microsoft.Extensions.Configuration;
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

/// <summary>
///     Tests for user-file configuration sources: <c>./.tui/config.json</c> resolves against the
///     current directory (not the app's install directory), malformed files are collected as errors
///     instead of crashing at assembly load, legacy-shaped files are skipped entirely, and
///     <see cref="TuiConfigurationBuilder.Reload"/> picks up file changes.
/// </summary>
[Collection ("StaticSettingsTests")]
public class UserFileConfigTests
{
    private static string CreateTuiDirWithConfig (string json)
    {
        string root = Directory.CreateTempSubdirectory ("tui-test-").FullName;
        string tuiDir = Path.Combine (root, TuiConfigurationExtensions.TUI_CONFIG_FOLDER);
        Directory.CreateDirectory (tuiDir);
        File.WriteAllText (Path.Combine (tuiDir, TuiConfigurationExtensions.CONFIG_FILENAME), json);

        return root;
    }

    [Fact]
    public void Build_LoadsCurrentDirConfig_FromGivenCurrentDirectory ()
    {
        string root = CreateTuiDirWithConfig ("""{ "TestMarker": { "Value": "42" } }""");

        try
        {
            TuiConfigurationBuilder tuiBuilder = new (appName: null, currentDirectory: root);

            Assert.Equal ("42", tuiBuilder.Configuration ["TestMarker:Value"]);
        }
        finally
        {
            Directory.Delete (root, recursive: true);
        }
    }

    [Fact]
    public void Reload_PicksUpChangedUserFile ()
    {
        string root = CreateTuiDirWithConfig ("""{ "TestMarker": { "Value": "1" } }""");
        string configPath = Path.Combine (root, TuiConfigurationExtensions.TUI_CONFIG_FOLDER, TuiConfigurationExtensions.CONFIG_FILENAME);

        try
        {
            TuiConfigurationBuilder tuiBuilder = new (appName: null, currentDirectory: root);
            Assert.Equal ("1", tuiBuilder.Configuration ["TestMarker:Value"]);

            File.WriteAllText (configPath, """{ "TestMarker": { "Value": "2" } }""");

            // The configuration is cached until invalidated.
            Assert.Equal ("1", tuiBuilder.Configuration ["TestMarker:Value"]);

            tuiBuilder.Reload ();

            Assert.Equal ("2", tuiBuilder.Configuration ["TestMarker:Value"]);
        }
        finally
        {
            Directory.Delete (root, recursive: true);
        }
    }

    [Fact]
    public void Build_MalformedUserFile_DoesNotThrow_AndCollectsError ()
    {
        string root = CreateTuiDirWithConfig ("{ this is not json");
        TuiJsonErrors.Print ();

        try
        {
            TuiConfigurationBuilder tuiBuilder = new (appName: null, currentDirectory: root);
            IConfiguration config = tuiBuilder.Configuration;

            Assert.NotNull (config);
            Assert.Contains (TuiJsonErrors.GetErrors (), e => e.Contains (TuiConfigurationExtensions.CONFIG_FILENAME));
        }
        finally
        {
            Directory.Delete (root, recursive: true);
        }
    }

    [Fact]
    public void Build_LegacyShapedUserFile_IsSkippedEntirely ()
    {
        string root = CreateTuiDirWithConfig (
                                              """
                                              {
                                                "TestMarker.Value": "42",
                                                "TestSection": { "X": "1" }
                                              }
                                              """);

        try
        {
            TuiConfigurationBuilder tuiBuilder = new (appName: null, currentDirectory: root);

            // The whole file is skipped — neither the flat dotted key nor the nested section leaks in.
            Assert.Null (tuiBuilder.Configuration ["TestMarker.Value"]);
            Assert.Null (tuiBuilder.Configuration ["TestSection:X"]);
        }
        finally
        {
            Directory.Delete (root, recursive: true);
        }
    }
}

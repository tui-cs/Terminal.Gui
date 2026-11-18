using BenchmarkDotNet.Attributes;
using Terminal.Gui.Configuration;

namespace Terminal.Gui.Benchmarks.Configuration;

/// <summary>
///     Measures the cost of switching the active theme via
///     <c>ThemeManager.Theme = "X"</c>.
///     Parametric over every built-in theme name shipped in the embedded <c>config.json</c>.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter '*ThemeSwitch*'</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Configuration", "Theme")]
public class ThemeSwitchBenchmark
{
    /// <summary>The built-in theme to switch to during each benchmark invocation.</summary>
    [ParamsSource (nameof (ThemeNames))]
    public string ThemeName { get; set; } = ThemeManager.DEFAULT_THEME_NAME;

    /// <summary>Returns the set of built-in theme names available after loading library resources.</summary>
    public static IEnumerable<string> ThemeNames
    {
        get
        {

            IEnumerable<string> names = ThemeManager.GetThemeNames ();

            return names;
        }
    }

    /// <summary>Loads the embedded configuration so all built-in themes are available.</summary>
    [GlobalSetup]
    public void Setup ()
    {
    }

    /// <summary>
    ///     Switches the active theme and applies the change.
    ///     This is the user-facing hot path when cycling themes via a <see cref="Views.Shortcut"/>.
    ///     Resets to <see cref="ThemeManager.DEFAULT_THEME_NAME"/> before each switch so every
    ///     invocation performs a real theme change (not a redundant reapply).
    /// </summary>
    [Benchmark]
    public void SwitchTheme ()
    {
        ThemeManager.Theme = ThemeManager.DEFAULT_THEME_NAME;

        ThemeManager.Theme = ThemeName;
    }

    /// <summary>No cleanup required; facade state is process-wide and restored by the next setup.</summary>
    [GlobalCleanup]
    public void Cleanup ()
    {
    }
}

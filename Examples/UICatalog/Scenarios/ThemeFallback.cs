#nullable enable

namespace UICatalog.Scenarios;

/// <summary>
///     Demonstrates the SchemeName fallback chain introduced in v2.
///     When a view's <see cref="View.SchemeName"/> is not found in the active theme, the view no longer
///     throws a <see cref="KeyNotFoundException"/>. Instead, it walks the fallback chain:
///     <list type="number">
///         <item>
///             <description>Named scheme (if found in current theme)</description>
///         </item>
///         <item>
///             <description>SuperView's scheme (recursive)</description>
///         </item>
///         <item>
///             <description>"Base" scheme from the current theme</description>
///         </item>
///         <item>
///             <description>Hard-coded "Base" scheme (always present)</description>
///         </item>
///     </list>
/// </summary>
[ScenarioMetadata ("Theme Fallback", "Demonstrates graceful SchemeName fallback when a named scheme is missing from the active theme.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Configuration")]
public sealed class ThemeFallback : Scenario
{
    private const string CUSTOM_SCHEME_NAME = "CustomHighlight";
    private const string MISSING_SCHEME_NAME = "NonExistentScheme";

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        // Extend the Default theme with a custom scheme that has TextStyle.Blink
        // so it stands out visually.  Other built-in themes do NOT contain this scheme,
        // so switching themes lets you watch the fallback chain activate in real time.
        SchemeManager.AddScheme (CUSTOM_SCHEME_NAME, new () { Normal = new Attribute (Color.BrightYellow, Color.Blue, TextStyle.Blink) });

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();

        // --- Theme selector ---
        string [] themeLabels = ThemeManager.GetThemeNames ().Select (n => "_" + n).ToArray ();

        OptionSelector themeSelector = new ()
        {
            Title = "_Theme",
            BorderStyle = LineStyle.Rounded,
            X = 1,
            Y = 1,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Labels = themeLabels,
            Value = ThemeManager.GetThemeNames ().IndexOf (ThemeManager.Theme)
        };

        themeSelector.ValueChanged += (sender, args) =>
                                      {
                                          if (sender is not OptionSelector sel)
                                          {
                                              return;
                                          }

                                          string rawLabel = sel.Labels! [(int)args.NewValue!];

                                          // Strip the leading underscore added for keyboard shortcut.
                                          ThemeManager.Theme = rawLabel [1..];

                                          // Re-add the custom scheme to the newly-active theme so the
                                          // "Default" theme always demonstrates the found case.
                                          if (ThemeManager.Theme == ThemeManager.DEFAULT_THEME_NAME)
                                          {
                                              SchemeManager.AddScheme (CUSTOM_SCHEME_NAME,
                                                                       new () { Normal = new Attribute (Color.BrightYellow, Color.Blue, TextStyle.Blink) });
                                          }
                                      };

        // --- Explanation ---
        Label intro = new ()
        {
            X = Pos.Right (themeSelector) + 1,
            Y = 1,
            Width = Dim.Fill (1),
            Text = $"Switch to a non-Default theme to see the fallback activate.\n"
                   + $"  • \"{CUSTOM_SCHEME_NAME}\" is only in the Default theme.\n"
                   + $"  • \"{MISSING_SCHEME_NAME}\" is never in any theme.\n"
                   + $"In both missing cases the view falls back gracefully instead of throwing."
        };

        // --- View 1: scheme FOUND in the active theme ---
        FrameView foundFrame = new ()
        {
            Title = $"SchemeName = \"{CUSTOM_SCHEME_NAME}\"",
            X = Pos.Right (themeSelector) + 1,
            Y = Pos.Bottom (intro) + 1,
            Width = Dim.Fill (1),
            Height = 5,
            SchemeName = CUSTOM_SCHEME_NAME
        };

        Label foundLabel = new ()
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill (2),
            Text = $"On the Default theme this scheme exists (BrightYellow/Blue + Blink).\n"
                   + $"On any other theme the scheme is missing → fallback chain activates."
        };
        foundFrame.Add (foundLabel);

        // --- View 2: scheme NEVER found — fallback always activates ---
        FrameView missingFrame = new ()
        {
            Title = $"SchemeName = \"{MISSING_SCHEME_NAME}\"",
            X = Pos.Right (themeSelector) + 1,
            Y = Pos.Bottom (foundFrame) + 1,
            Width = Dim.Fill (1),
            Height = 5,
            SchemeName = MISSING_SCHEME_NAME
        };

        Label missingLabel = new ()
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill (2),
            Text = $"This scheme does not exist in any theme.\n"
                   + $"The view silently falls back to its SuperView's scheme (no exception).\n"
                   + $"A warning is written to the debug log."
        };
        missingFrame.Add (missingLabel);

        appWindow.Add (themeSelector, intro, foundFrame, missingFrame);

        app.Run (appWindow);
    }
}

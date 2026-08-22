# Configuration Deep Dive

Terminal.Gui loads themes, glyphs, key bindings, and view defaults from JSON using [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/core/extensions/configuration) via <xref:Terminal.Gui.Configuration.TuiConfigurationBuilder>.

The legacy `ConfigurationManager` type was removed in 2.5.0. To convert a pre-2.5.0 `config.json`, see [Migrating ConfigurationManager to TuiConfigurationBuilder](migrate-cm-to-mec.md).

## Quick start

Library defaults are applied when `Terminal.Gui.dll` loads. To overlay runtime JSON or switch themes:

```csharp
TuiConfigurationBuilder builder = new ("MyApp");
builder.RuntimeConfig = """{ "Theme": "Dark" }""";
builder.ApplyToStaticFacades ();

builder.ThemeManager.SwitchTheme ("Dark");
```

Views read static facades such as `Button.DefaultShadow` (backed by `ButtonSettings.Current`).

## Sources and precedence

`TuiConfigurationBuilder.Build` loads sources lowest-to-highest:

1. Hard-coded Settings POCO `init` defaults
2. Library embedded `Terminal.Gui.Resources.config.json`
3. App embedded `config.json`
4. `~/.tui/config.json` and `./.tui/config.json`
5. `~/.tui/{appName}.config.json` and `./.tui/{appName}.config.json`
6. `TUI_CONFIG` environment variable (inline JSON)
7. `TuiConfigurationBuilder.RuntimeConfig`

Later sources override earlier ones property-by-property.

## JSON shape

Settings are **nested objects**, not dotted keys. The JSON Schema is [`docfx/schemas/tui-config-schema.json`](../schemas/tui-config-schema.json), hosted at `https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json` after docs publish.

```json
{
  "Theme": "Dark",
  "Application": {
    "IsMouseDisabled": false
  },
  "Button": {
    "DefaultShadow": "Opaque"
  },
  "Glyphs": {
    "CheckStateChecked": "☑"
  },
  "Themes": {
    "Dark": {
      "Button": { "DefaultShadow": "None" },
      "Glyphs": { "LeftBracket": "[", "RightBracket": "]" }
    }
  }
}
```

`Themes` is a dictionary of theme name → overlay. Each overlay uses the same nested section names as the root (`Button`, `Glyphs`, `Dialog`, …). Properties omitted from an overlay keep the root value.

A pre-MEC file with `"Button.DefaultShadow": "None"` or `"Themes": [ { "Dark": { } } ]` is still applied, but a warning is logged. Convert it with `Tools/MigrateConfig`.

## Settings POCOs

| Scope | Types | Storage |
|-------|--------|---------|
| ThemeScope | `ButtonSettings`, `DialogSettings`, `GlyphSettings`, `MenuSettings`, … | Immutable `record` with `Current` swapped atomically |
| SettingsScope | `ApplicationSettings`, `DriverSettings`, `KeySettings`, `ThemeSettings`, … | Mutable `Defaults` instance |

Theme overlays apply only to ThemeScope POCOs. The selected theme name is `ThemeSettings.Defaults.Theme` (JSON scalar `"Theme"`).

To change a theme-scoped default in code:

```csharp
ButtonSettings.Current = ButtonSettings.Current with { DefaultShadow = ShadowStyles.None };
```

## Themes and schemes

- <xref:Terminal.Gui.Configuration.IThemeManager> (`TuiConfigurationBuilder.ThemeManager`) lists theme names and calls `SwitchTheme`.
- <xref:Terminal.Gui.Configuration.ThemeChanges>.`ThemeChanged` is the process-wide observer for theme switches.
- Schemes live inside each theme's `Schemes` dictionary and are applied through <xref:Terminal.Gui.Drawing.SchemeManager>.

```csharp
builder.ThemeManager.ThemeChanged += (_, args) =>
{
    // args.Value is the new theme name
};

ThemeChanges.ThemeChanged += (_, _) => view.SetNeedsDraw ();
```

## App settings

To bind an application-specific POCO:

```csharp
public class MyAppSettings
{
    public string Title { get; set; } = "My App";
    public static MyAppSettings Defaults { get; set; } = new ();
}

TuiConfigurationBuilder builder = new ("MyApp");
builder.BindAppSettings<MyAppSettings> ("MyApp", s => MyAppSettings.Defaults = s);
builder.ApplyToStaticFacades ();
```

Corresponding JSON:

```json
{ "MyApp": { "Title": "Demo" } }
```

## Custom sources

```csharp
IConfiguration config = new ConfigurationBuilder ()
    .AddTuiLibraryDefaults ()
    .AddTuiUserFiles ("MyApp")
    .AddJsonFile ("custom-settings.json", optional: true)
    .Build ();
```

## See also

- [Migrating ConfigurationManager to TuiConfigurationBuilder](migrate-cm-to-mec.md)
- [Configuration JSON Schema](../schemas/tui-config-schema.json)
- [Scheme Deep Dive](scheme.md)
- [Menus](menus.md)

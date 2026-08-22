---
uid: Terminal.Gui.Configuration
summary: Configuration management, themes, and persistent settings.
---

The `Configuration` namespace loads Terminal.Gui settings from JSON using Microsoft.Extensions.Configuration.

## Key Types

- **TuiConfigurationBuilder** - Builds the source chain and applies values to Settings POCOs
- **TuiConfigurationExtensions** - `AddTuiLibraryDefaults`, `AddTuiUserFiles`, `AddTuiRuntimeConfig`
- **IThemeManager** / **MecThemeManager** - Theme names and `SwitchTheme`
- **ISchemeManager** / **SchemeManager** - Named schemes
- **ThemeChanges** - Process-wide `ThemeChanged` observer
- **\*Settings** records (`ButtonSettings`, `GlyphSettings`, …) - Bind targets; ThemeScope types expose `Current`

## Example

```csharp
TuiConfigurationBuilder builder = new ("MyApp");
builder.RuntimeConfig = """{ "Theme": "Dark" }""";
builder.ApplyToStaticFacades ();

builder.ThemeManager.SwitchTheme ("Dark");
```

## See Also

- [Configuration Deep Dive](~/docs/config.md)
- [Migrating ConfigurationManager](~/docs/migrate-cm-to-mec.md)

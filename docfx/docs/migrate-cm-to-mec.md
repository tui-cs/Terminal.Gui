# Migrating ConfigurationManager to TuiConfigurationBuilder

Terminal.Gui 2.5.0 removes the legacy `ConfigurationManager` API that 2.4.17 marked `[Obsolete]`. Configuration is now Microsoft.Extensions.Configuration (MEC) via `TuiConfigurationBuilder`.

To convert a pre-2.5.0 `config.json`, run:

```bash
dotnet run --project Tools/MigrateConfig -- ./.tui/config.json ./.tui/config.json
```

The tool:

- Splits top-level dotted keys (`Button.DefaultShadow`) into nested objects (`Button: { DefaultShadow }`)
- Splits `Application.DefaultKeyBindings`, `View.DefaultKeyBindings`, and `View.ViewKeyBindings` into nested `Application` / `View` sections (still applied in 2.5 — overlay by command; unmentioned commands keep hard-coded defaults)
- Collapses `Themes` / `Schemes` arrays of single-key objects into dictionaries
- Treats empty `Themes` / `Schemes` arrays as empty dictionaries (`{}`)

The library does **not** apply the legacy shape. A `WARN` log names the file and points here so you can convert it. Nested JSON is the supported contract. Point `$schema` at `https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json` (nested MEC, 2.5+).

## Code

| 2.4.x (removed) | 2.5.0 |
|---|---|
| `ConfigurationManager.Enable (ConfigLocations.All)` | `new TuiConfigurationBuilder ().ApplyToStaticFacades ()` (already runs at assembly load) |
| `ConfigurationManager.RuntimeConfig = json` | `builder.RuntimeConfig = json; builder.ApplyToStaticFacades ()` (nested JSON only) |
| `ConfigurationManager.Applied += ...` | `ThemeChanges.ThemeChanged += ...` |
| `ThemeManager.Theme = "Dark"` | `builder.ThemeManager.SwitchTheme ("Dark")` |
| `Button.DefaultShadow = ShadowStyles.None` | `ButtonSettings.Current = ButtonSettings.Current with { DefaultShadow = ShadowStyles.None }` |
| `[ConfigurationProperty]` | Settings POCO property + `config.json` section |

See [Configuration](config.md) for the full contract.

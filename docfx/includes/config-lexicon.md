| Term | Meaning |
|:-----|:--------|
| **TuiConfigurationBuilder** | Builds the Microsoft.Extensions.Configuration source chain and applies settings to static facades. |
| **Settings POCO** | Bind target for a nested JSON section (`ButtonSettings`, `GlyphSettings`, `ApplicationSettings`, …). |
| **Theme-scoped settings** | Immutable `*Settings` records with `Current` swapped on theme switch (`ButtonSettings`, `GlyphSettings`, …). |
| **Process-wide settings** | Mutable `*Settings.Defaults` bound once at apply (`ApplicationSettings`, `DriverSettings`, `KeySettings`, `TraceSettings`, …). |
| **Theme** | Named overlay under `Themes:{name}` that property-level-merges onto root sections. |
| **ThemeChanges** | Process-wide `ThemeChanged` observer for views that cannot take `IThemeManager`. |
| **Sources** | MEC provider chain: hard-coded POCO defaults, library `config.json`, app `config.json`, `~/.tui` / `./.tui` files, `TUI_CONFIG`, `RuntimeConfig`. |
| **RuntimeConfig** | Highest-priority in-memory JSON string on `TuiConfigurationBuilder`. Nested MEC shape only. |

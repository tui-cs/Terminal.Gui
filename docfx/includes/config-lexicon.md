| Term | Meaning |
|:-----|:--------|
| **AppSettings** | Application-specific settings stored in the application's resources. |
| **Apply** | Apply the configuration to the application; copies settings from configuration properties to corresponding `static` `[ConfigProperty]` properties. |
| **ConfigProperty** | A property decorated with `[ConfigProperty]` that can be configured via the configuration system. |
| **Configuration** | A collection of settings defining application behavior and appearance. |
| **TuiConfigurationBuilder** | Builds the Microsoft.Extensions.Configuration source chain and applies settings to static facades. |
| **Settings POCO** | Bind target for a JSON section (`ButtonSettings`, `GlyphSettings`, …). |
| **Load** | Load configuration from given location(s), updating with new values. Loading doesn't apply settings automatically. |
| **Location** | Storage location for configuration (e.g., user's home directory, application directory). |
| **Reset** | Reset configuration to current values or hard-coded defaults. Does not load configuration. |
| **Scope** | Defines the context where configuration applies (Settings, Theme, or AppSettings). |
| **Settings** | Runtime options including both system settings and application-specific settings. |
| **Sources** | Set of locations where configuration can be stored (@Terminal.Gui.ConfigLocations enum). |
| **Theme** | Named instance containing specific appearance settings. |
| **ThemeInheritance** | Mechanism where themes can inherit and override settings from other themes. |
| **Themes** | Collection of named Theme definitions bundling visual and layout settings. |

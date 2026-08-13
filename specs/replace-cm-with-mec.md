# Spec: Replace ConfigurationManager (CM) with Microsoft.Extensions.Configuration (MEC)

> **Status:** In Progress — MEC migration is implemented; current focus is finishing complex-type migration in PR #5411.
> **Tracking Issue:** [#4943](https://github.com/gui-cs/Terminal.Gui/issues/4943)
> **Related analysis:** See [@tig's AOT binary-size comment](https://github.com/gui-cs/Terminal.Gui/issues/4943#issuecomment-XXXXXX)

---

## Table of Contents

1. [Background and Motivation](#1-background-and-motivation)
2. [Constitution Alignment](#2-constitution-alignment)
3. [Current CM — Complete Use Case Inventory](#3-current-cm--complete-use-case-inventory)
4. [Current CM — Architecture Summary](#4-current-cm--architecture-summary)
5. [Proposed MEC-Based Architecture](#5-proposed-mec-based-architecture)
6. [Use Case → MEC Mapping](#6-use-case--mec-mapping)
7. [Functionality Changes Requiring Explicit Debate](#7-functionality-changes-requiring-explicit-debate)
8. [Implementation Phases](#8-implementation-phases)
9. [Open Questions](#9-open-questions)
10. [Out of Scope](#10-out-of-scope)

---

## 1. Background and Motivation

### 1.1 What Is ConfigurationManager (CM)?

`ConfigurationManager` is a static, process-wide configuration subsystem introduced in Terminal.Gui v1 and carried forward into v2. It allows Terminal.Gui library components and application developers to:

- Define configurable properties via the `[ConfigurationProperty]` attribute on static properties.
- Load layered JSON configuration from up to 9 sources (hard-coded defaults → embedded resources → home-directory files → current-directory files → environment variable → in-memory runtime string).
- Apply loaded configuration back to those static properties.
- Define and switch named **Themes**, each containing **Schemes** (color/attribute sets) and per-component visual defaults.
- Subscribe to `Updated` and `Applied` events to react to live configuration changes.

### 1.2 Why Replace CM?

Three concrete drivers, ordered by urgency:

#### Driver 1 — AOT Binary Size (Highest Urgency)

Analysis of the NativeAot example (PR #5243) shows that a simple login-form app produces an **11.8 MB** native binary. CM is the primary culprit:

| Anti-trimming pattern | Location | Impact |
|---|---|---|
| `[DynamicDependency]` on 29 types | `ConfigPropertyHostTypes.cs:66-94` | Forces linker to preserve all public properties of all 29 types, even those unused by the app |
| `Assembly.GetTypes()` reflection scan | `ConfigProperty.cs` | Prevents any type from being trimmed; worst-case AOT pattern |
| `Activator.CreateInstance` | `DeepCloner.cs` | Prevents trimming of cloned types |
| `MakeGenericType` | `DeepCloner.cs`, `ScopeJsonConverter.cs` | Prevents trimming of generic instantiations |
| ~188 `[ConfigurationProperty]` attributes | Throughout codebase (144 on `Glyphs` alone) | Each roots a property |

Disabling CM at runtime (`ConfigurationManager.IsEnabled == false`) has **zero impact on binary size** because the rooting annotations are compile-time, not runtime.

#### Driver 2 — Test Parallelization (High Urgency)

CM uses static state (`_settings`, `_enabled`, `ThemeManager.Theme`, `SchemeManager.Schemes`, etc.). Tests that touch CM cannot run in parallel. This limits `Tests/UnitTestsParallelizable` coverage significantly. Every component whose behavior is controlled by a `[ConfigurationProperty]` static property is a hidden parallelization barrier.

#### Driver 3 — Modern .NET Alignment (Moderate Urgency)

The .NET ecosystem has converged on `Microsoft.Extensions.Configuration` + `Microsoft.Extensions.Options` (`IOptions<T>`, `IOptionsMonitor<T>`) as the standard configuration pattern since .NET Core 2.0. Terminal.Gui's bespoke CM is unfamiliar to modern .NET developers and duplicates a mature system already in the ecosystem.

---

## 2. Constitution Alignment

This proposal must be evaluated against the Terminal.Gui [constitution](./constitution.md). Key tenet checks:

### ✅ Testability First

> *Views must be testable in isolation without global state.*

**Current CM violates this tenet.** Static `[ConfigurationProperty]` properties make all 29 host types (and all views they affect) globally stateful. Replacing CM with `IOptions<T>` / `IOptionsMonitor<T>` passed via constructor or property injection allows each test to create an isolated configuration snapshot.

### ✅ Performance Is a Feature

> *We never accept regressions in the hot path.*

CM adds reflection overhead at module-init time (`Assembly.GetTypes()`, deep-clone of all properties). MEC configuration is loaded once into POCOs and accessed via lightweight interface calls. The hot rendering path reads a simple `Rune` field or `ShadowStyle` enum — this **must not change**.

### ✅ Users Have Final Control

> *Everything configurable must be configurable.*

MEC retains full configurability. The same JSON files remain valid. The precedence model survives. No configuration capability is removed (see §7 for the one debatable exception).

### ✅ Separation of Concerns

> *Layout, focus, input, and drawing are cleanly decoupled.*

Currently, View subclasses have static `[ConfigurationProperty]` properties entangled with CM lifecycle. MEC decouples them: views hold a value and receive updates; configuration loading and persistence is someone else's job.

### ✅ Respect What Came Before

> *Appreciate existing systems; learn from the past.*

CM served Terminal.Gui well for multiple years. This spec proposes preserving the **external JSON format** unchanged and the **same 9-level precedence model** so that users' existing config files continue to work without modification.

### 🔴 Documentation Is the Spec

> *API documentation is the contract. When docs and code conflict, the code is wrong.*

This spec **is** the new contract. `docfx/docs/config.md` must be updated to match before any implementation ships.

---

## 3. Current CM — Complete Use Case Inventory

This section is the authoritative catalog of everything CM does. Every use case listed here must be supported by the MEC replacement — either equivalently or with an explicit decision (documented in §7) to change or remove it.

---

### UC-01: Library Developer Defines a SettingsScope Property

**What:** A Terminal.Gui library developer marks a static property with `[ConfigurationProperty(Scope = typeof(SettingsScope))]` to make it configurable via `config.json`.

**Who does this:** Library developers only (not app developers).

**Current API:**
```csharp
// In Application.cs
[ConfigurationProperty(Scope = typeof(SettingsScope))]
public static string ForceDriver { get; set; } = string.Empty;

// In Driver.cs
[ConfigurationProperty(Scope = typeof(SettingsScope))]
public static bool Force16Colors { get; set; } = false;

// In View.Keyboard.cs
[ConfigurationProperty(Scope = typeof(SettingsScope))]
public static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; }
```

**JSON representation:**
```json
{
  "Application.ForceDriver": "ansi",
  "Driver.Force16Colors": true
}
```

**Current behavior:** CM discovers these at module-init via `ConfigProperty.Initialize()`, stores their current values as hard-coded defaults, and applies loaded JSON values back to the static properties during `Apply()`.

**Full list of SettingsScope properties (as of writing):**
- `Application.AppModel`
- `Application.ForceDriver`
- `Application.DefaultKeyBindings`
- `Application.IsMouseDisabled`
- `Color.Colors16` *(OmitClassName — maps ColorName16 to string)*
- `ConfigurationManager.ThrowOnJsonErrors`
- `Driver.Force16Colors`
- `Driver.SizeDetection`
- `Key.Separator`
- `MenuBar.DefaultKey` *(F10)*
- `PopoverMenu.DefaultKey`
- `FileDialog.DefaultOpenMode`
- `FileDialogStyle.DefaultSearchMatcher`
- `FileDialogStyle.PreferResultFromOpenMode`
- `Trace.EnabledCategories`
- `View.DefaultKeyBindings` *(keyboard bindings for all Views)*
- `View.ViewKeyBindings` *(per-View-type keyboard bindings)*

---

### UC-02: Library Developer Defines a ThemeScope Property

**What:** A Terminal.Gui library developer marks a static property with `[ConfigurationProperty(Scope = typeof(ThemeScope))]` to make it part of the theme system — its value can vary per named theme.

**Who does this:** Library developers only (not app developers).

**Current API:**
```csharp
// In Button.cs
[ConfigurationProperty(Scope = typeof(ThemeScope))]
public static ShadowStyles DefaultShadow { get; set; } = ShadowStyles.Opaque;

[ConfigurationProperty(Scope = typeof(ThemeScope))]
public static MouseState DefaultMouseHighlightStates { get; set; } = MouseState.In | MouseState.Pressed | MouseState.PressedOutside;

// In Glyphs.cs (144 of these)
[ConfigurationProperty(Scope = typeof(ThemeScope))]
public static Rune CheckStateChecked { get; set; } = (Rune)'☑';
```

**JSON representation (inside a theme):**
```json
{
  "Themes": [
    {
      "Default": {
        "Button.DefaultShadow": "Opaque",
        "Glyphs.CheckStateChecked": "☑"
      }
    }
  ]
}
```

**Full list of ThemeScope property host types (as of writing):**
- `Glyphs` (~130+ glyph Runes: CheckStateChecked, CheckStateUnChecked, CheckStateNone, all border/line drawing runes, all scrollbar runes, all arrow glyphs, FileOpen, HorizontalEllipsis, etc.)
- `Button.DefaultShadow`, `Button.DefaultMouseHighlightStates`
- `CheckBox.DefaultMouseHighlightStates`
- `Dialog.DefaultShadow`, `Dialog.DefaultBorderStyle`, `Dialog.DefaultButtonAlignment`, `Dialog.DefaultButtonAlignmentModes`
- `FrameView.DefaultBorderStyle`
- `HexView.DefaultBorderStyle`
- `Menu.DefaultBorderStyle`
- `MenuBar.DefaultBorderStyle`
- `MessageBox.DefaultBorderStyle`, `MessageBox.DefaultButtonAlignment`
- `NerdFonts.Enable`
- `SelectorBase.DefaultBorderStyle`
- `StatusBar.DefaultBorderStyle`
- `TextField.DefaultBorderStyle`
- `TextView.DefaultBorderStyle`
- `Window.DefaultBorderStyle`, `Window.DefaultShadow`
- `CharMap.DefaultBorderStyle`
- `LinearRangeDefaults.*`
- `SchemeManager.Schemes` *(the color/attribute dictionary for the current theme)*

---

### UC-03: App Developer Defines an AppSettingsScope Property

**What:** An application developer marks a static property with `[ConfigurationProperty]` (defaulting to `AppSettingsScope`) so that users can configure it via `AppName.config.json`.

**Who does this:** App developers.

**Current API:**
```csharp
// In a UICatalog class
[ConfigurationProperty] // implies AppSettingsScope
public static bool ShowStatusBar { get; set; } = true;
```

**JSON representation:**
```json
{
  "AppSettings": {
    "UICatalog.ShowStatusBar": true
  }
}
```

**Constraint:** OmitClassName is **not** allowed for `AppSettingsScope` to ensure globally unique keys. The property name in JSON is always `ClassName.PropertyName`.

---

### UC-04: Enable Configuration Loading

**What:** CM is disabled by default. Hard-coded defaults are always in effect. An app must explicitly call `Enable()` to load from any source.

**Current API:**
```csharp
// Typical usage (before Application.Init):
ConfigurationManager.Enable(ConfigLocations.All);

// Enable without loading any sources (just enables the system):
ConfigurationManager.Enable(ConfigLocations.None);

// Enable with only hard-coded defaults (same as hard-coded):
ConfigurationManager.Enable(ConfigLocations.HardCoded);
```

**Notes:**
- `IsEnabled` returns `false` until `Enable()` is called.
- Most framework behaviors are available even when CM is disabled (via hard-coded defaults).
- `Apply()` and `Load()` throw `ConfigurationManagerNotEnabledException` if CM is not enabled.

---

### UC-05: Load from 9-Level Precedence Stack

**What:** Load configuration from all standard sources in a defined precedence order. Higher-precedence sources override lower ones.

**Current API:**
```csharp
ConfigurationManager.Load(ConfigLocations.All); // Load all 9 levels
ConfigurationManager.Load(ConfigLocations.GlobalHome | ConfigLocations.AppCurrent); // Selective
```

**Precedence order (lowest to highest):**

| Level | `ConfigLocations` flag | Source | Example path |
|-------|------------------------|--------|--------------|
| 1 | `HardCoded` | Static property initializers in code | n/a |
| 2 | `LibraryResources` | Embedded in `Terminal.Gui.dll` | `Terminal.Gui.Resources.config.json` |
| 3 | `AppResources` | Embedded in the app assembly | `MyApp.Resources.config.json` |
| 4 | `GlobalHome` | Global file in user home | `~/.tui/config.json` |
| 5 | `GlobalCurrent` | Global file in current dir | `./.tui/config.json` |
| 6 | `AppHome` | App file in user home | `~/.tui/MyApp.config.json` |
| 7 | `AppCurrent` | App file in current dir | `./.tui/MyApp.config.json` |
| 8 | `Env` | `TUI_CONFIG` environment variable (JSON string) | `export TUI_CONFIG='{"Key.Separator":"+"}'` |
| 9 | `Runtime` | `ConfigurationManager.RuntimeConfig` in-memory string | Set programmatically |

**Notes:**
- Sources at the same precedence level that don't exist (missing file) are silently skipped.
- JSON parsing errors are collected in `_jsonErrors` by default (not thrown) unless `ThrowOnJsonErrors` is set.
- `AppName` (defaults to entry assembly name) is used to construct per-app file names.
- The `TUI_CONFIG` environment variable value is a raw JSON fragment (same format as a config file).

---

### UC-06: Apply Configuration to Static Properties

**What:** After loading, apply the merged configuration to the static `[ConfigurationProperty]` properties.

**Current API:**
```csharp
ConfigurationManager.Apply(); // throws if not enabled
```

**Notes:**
- `Apply()` copies the loaded values back to the static properties.
- Fires `Applied` event when any value changed.
- Calls `ThemeManager.Themes[Theme].Apply()` and `AppSettings.Apply()` internally.

---

### UC-07: Reset to Hard-Coded Defaults

**What:** Restore all `[ConfigurationProperty]` static properties to their initial hard-coded values (as they were when the module loaded).

**Current API:**
```csharp
ConfigurationManager.Disable(resetToHardCodedDefaults: true);
// OR:
ConfigurationManager.Enable(ConfigLocations.HardCoded);
```

**Notes:**
- Hard-coded defaults are captured once at module initialization via `ConfigProperty.GetAllConfigProperties()`.
- A deep clone of each property's initial value is preserved in `_hardCodedConfigPropertyCache`.
- Resetting clears `RuntimeConfig` and `SourcesManager.Sources`.

---

### UC-08: Runtime / Programmatic Configuration Override

**What:** Override configuration at runtime via an in-memory JSON string, without touching any files. This is the highest-precedence source (level 9).

**Current API:**
```csharp
// Override a setting programmatically (used in UICatalog's Runner for force-driver):
ConfigurationManager.RuntimeConfig = """
  {
    "Application.ForceDriver": "ansi",
    "Driver.Force16Colors": true
  }
  """;

ConfigurationManager.Load(ConfigLocations.All);
ConfigurationManager.Apply();
```

**Use cases in the codebase:**
- `UICatalog/Runner.cs`: Sets `ForceDriver` and `Force16Colors` from command-line arguments.
- Tests: Used to configure specific settings without requiring files on disk.

---

### UC-09: Define and Apply a Theme

**What:** A theme is a named collection of `ThemeScope` properties (visual settings + color schemes). Terminal.Gui ships several built-in themes in its embedded `config.json`.

**Current API:**
```csharp
// Get current theme
ThemeScope currentTheme = ThemeManager.GetCurrentTheme();

// Get all theme names
ImmutableList<string> names = ThemeManager.GetThemeNames();

// Switch themes (apply immediately after)
ThemeManager.Theme = "Dark";
ConfigurationManager.Apply();

// Access themes dictionary
ConcurrentDictionary<string, ThemeScope> allThemes = ThemeManager.Themes!;
```

**Built-in themes (from `Terminal.Gui/Resources/config.json`):**
- Default
- Dark
- Light
- TurboPascal 5
- Anders
- Green Phosphor
- Amber Phosphor

**JSON format:**
```json
{
  "Theme": "Dark",
  "Themes": [
    {
      "Dark": {
        "Button.DefaultShadow": "None",
        "Window.DefaultBorderStyle": "Heavy",
        "Schemes": [
          { "Base": { "Normal": { "Foreground": "White", "Background": "Black" } } }
        ]
      }
    }
  ]
}
```

---

### UC-10: Subscribe to Theme Change Events

**What:** Components and applications can subscribe to events that fire when the theme changes or when configuration is applied.

**Current API:**
```csharp
// Fires when a new theme has been selected
ThemeManager.ThemeChanged += (sender, e) => {
    // e.Value is the new theme name
    RefreshUI();
};

// Fires when configuration has been loaded (not yet applied)
ConfigurationManager.Updated += (sender, e) => {
    LogMessage("Config updated");
};

// Fires when configuration has been applied to static properties
ConfigurationManager.Applied += (sender, e) => {
    // In UICatalogRunnable: re-read all static properties and refresh
    RefreshUI();
};
```

---

### UC-11: Define Color Schemes

**What:** A Scheme maps `VisualRole`s (Normal, Focus, HotNormal, HotFocus, Disabled, Code) to `Attribute`s (Foreground + Background + Style). Schemes are part of the current theme.

**Current API:**
```csharp
// Access built-in scheme
Scheme baseScheme = SchemeManager.GetScheme(Schemes.Base);
Scheme menuScheme = SchemeManager.GetScheme("Menu");

// Try-get (safe, no exceptions)
if (SchemeManager.TryGetScheme("MyCustomScheme", out Scheme? scheme)) { }

// Get all scheme names
ImmutableList<string> names = SchemeManager.GetSchemeNames();

// Get all schemes for current theme
Dictionary<string, Scheme?> all = SchemeManager.GetSchemesForCurrentTheme();

// Add custom scheme
SchemeManager.AddScheme("MyScheme", new Scheme {
    Normal = new Attribute(Color.White, Color.Black),
    Focus = new Attribute(Color.Black, Color.White),
    // ...
});

// Remove custom scheme (built-ins cannot be removed)
SchemeManager.RemoveScheme("MyScheme");
```

**Built-in scheme names (from `Schemes` enum):**
- Base, Dialog, Menu, Error, Accent, Toplevel, View, Highlight

**JSON representation:**
```json
{
  "Schemes": [
    {
      "Base": {
        "Normal":    { "Foreground": "White",  "Background": "Black"  },
        "Focus":     { "Foreground": "Black",  "Background": "White"  },
        "HotNormal": { "Foreground": "Yellow", "Background": "Black"  },
        "HotFocus":  { "Foreground": "Yellow", "Background": "White"  },
        "Disabled":  { "Foreground": "Gray",   "Background": "Black"  },
        "Code":      { "Foreground": "Green",  "Background": "Black"  }
      }
    }
  ]
}
```

---

### UC-12: Assign Scheme to a View

**What:** Each View has a `SchemeName` property. When `SchemeName` is set, the View uses the named Scheme for rendering.

**Current API:**
```csharp
// Assign scheme by name
myDialog.SchemeName = SchemeManager.SchemesToSchemeName(Schemes.Dialog);

// Assign via enum (resolves to "Dialog")
myMenuBar.SchemeName = SchemeManager.SchemesToSchemeName(Schemes.Menu);

// Views inherit scheme from SuperView if SchemeName is null
```

---

### UC-13: Access Hard-Coded Configuration

**What:** Get the hard-coded default config values as a JSON string, or get an empty config template.

**Current API:**
```csharp
// Get empty config (just the schema URL)
string emptyJson = ConfigurationManager.GetEmptyConfig();

// Get hard-coded defaults as JSON
string hardCodedJson = ConfigurationManager.GetHardCodedConfig();
```

**Notes:**
- `GetHardCodedConfig()` is used by UICatalog's "Save Defaults" test to regenerate `config.json`.
- Used in tooling to scaffold configuration files.

---

### UC-14: Embedded Library Resource Configuration

**What:** Terminal.Gui ships its own `Terminal.Gui.Resources.config.json` embedded in the DLL. This file defines the built-in themes and sets the source-of-truth defaults for all `[ConfigurationProperty]` static properties.

**Notes:**
- This file is **the canonical definition** of built-in themes like "Dark", "Light", "TurboPascal 5", etc.
- It is 1,501 lines of JSON and contains all scheme definitions for all built-in themes.
- Loading this file is `ConfigLocations.LibraryResources` (precedence level 2).
- Changes to this file must be reflected in `config.json` and vice versa — the `SaveDefaults` test enforces this.

---

### UC-15: Embedded App Resource Configuration

**What:** Application developers can embed a `config.json` (or `Resources/config.json`) in their app assembly to provide app-specific defaults. This is `ConfigLocations.AppResources` (precedence level 3).

**Notes:**
- Discovered by scanning the entry assembly's manifest resource names for anything ending in `config.json`.
- Loaded before any user-level files, so user preferences can still override app defaults.

---

### UC-16: Global User Configuration Files

**What:** Users place global configuration in `~/.tui/config.json` (`GlobalHome`) or `./.tui/config.json` (`GlobalCurrent`). These affect all Terminal.Gui apps the user runs.

**Current behavior:**
- `SourcesManager` expands `~` to `Environment.GetFolderPath(SpecialFolder.UserProfile)`.
- Missing files are silently skipped.
- Cross-platform: Works on Windows, macOS, Linux.

---

### UC-17: Per-App User Configuration Files

**What:** Users can place app-specific configuration in `~/.tui/AppName.config.json` (`AppHome`) or `./.tui/AppName.config.json` (`AppCurrent`). `AppName` defaults to the entry assembly's simple name.

**Notes:**
- `ConfigurationManager.AppName` can be set before `Enable()` to override the default.
- Useful for multi-runnable apps (e.g. UICatalog hosting sub-scenarios with distinct names).

---

### UC-18: Environment Variable Configuration

**What:** The `TUI_CONFIG` environment variable can contain a JSON fragment that overrides configuration. Useful for CI/CD, containers, and test harnesses.

**Current behavior:**
```bash
export TUI_CONFIG='{"Application.ForceDriver": "NetDriver", "Driver.Force16Colors": true}'
```

- The environment variable value is treated as a raw JSON config document.
- It is at precedence level 8 (overrides all file-based sources, but can be overridden by `RuntimeConfig`).

---

### UC-19: JSON Error Handling

**What:** By default, JSON parsing errors are collected (not thrown) and can be reported after the fact. In test/debug mode, errors can be set to throw exceptions.

**Current API:**
```csharp
// Default: collect errors, print them when Application shuts down
ConfigurationManager.PrintJsonErrors();

// Test/debug: throw on first error
ConfigurationManager.ThrowOnJsonErrors = true;

// Check if any errors occurred
bool hasErrors = ConfigurationManager._jsonErrors.Length > 0;
```

---

### UC-20: Custom JSON Converters for Terminal.Gui Types

**What:** CM uses custom `System.Text.Json` converters for Terminal.Gui types that don't map directly to JSON primitives.

**Converters:**
| Converter | Handles | Example JSON |
|---|---|---|
| `RuneJsonConverter` | `Rune` (Unicode code point) | `"☑"`, `"U+2611"`, `97` |
| `KeyJsonConverter` | `Key` (key binding) | `"Ctrl+Q"`, `"F1"` |
| `ColorJsonConverter` | `Color` | `"White"`, `"#RRGGBB"`, `42` |
| `AttributeJsonConverter` | `Attribute` (fg+bg pair) | `{ "Foreground": "White", "Background": "Black" }` |
| `SchemeJsonConverter` | `Scheme` | Nested scheme object |
| `DictionaryJsonConverter<T>` | Dictionaries | Theme scheme arrays |
| `ConcurrentDictionaryJsonConverter<T>` | Themes dict | Theme array → dict |
| `ScopeJsonConverter<T>` | All Scope types | Flat key-value to typed properties |
| `KeyArrayJsonConverter` | `Key[]` | Array of key strings |
| `KeyCodeJsonConverter` | `KeyCode` | Numeric key code |
| `TraceCategoryJsonConverter` | `TraceCategory` | Trace category flags |

---

### UC-21: AOT / Trim Compatibility

**What:** CM must work in `PublishTrimmed=true` and `PublishAot=true` builds, which is Terminal.Gui's current requirement.

**Current mechanism:**
- `ConfigPropertyHostTypes.GetTypes()` enumerates all 29 host types.
- Each type is decorated with `[DynamicDependency(PublicProperties, typeof(...))]` to prevent trimming.
- `SourceGenerationContext` provides STJ source-generation for all known types.
- `DeepCloner` is the residual reflection hotspot (multiple `[UnconditionalSuppressMessage]` annotations are required).

---

### UC-22: Disable CM Without Resetting

**What:** CM can be disabled without resetting to hard-coded defaults (e.g., to prevent further loads while preserving the currently applied state).

**Current API:**
```csharp
ConfigurationManager.Disable(); // disable, keep current static property values
ConfigurationManager.Disable(resetToHardCodedDefaults: true); // disable and reset
```

---

### UC-23: Get/Set `AppName`

**What:** `ConfigurationManager.AppName` is the name used for per-app config files. Defaults to entry assembly name. Can be overridden before `Enable()`.

**Current API:**
```csharp
ConfigurationManager.AppName = "MyCustomAppName";
ConfigurationManager.Enable(ConfigLocations.All);
```

---

### UC-24: App Developer `Updated` / `Applied` Event Subscriptions

**What:** Application developers subscribe to CM events to react to configuration changes (e.g., after theme switch, re-read all theme-affected views).

**Current API:**
```csharp
// UICatalogRunnable:
ConfigurationManager.Applied += ConfigAppliedHandler;
// ...
ConfigurationManager.Applied -= ConfigAppliedHandler;
```

---

### UC-25: Reload Configuration (Live Reload)

**What:** After `Enable()`, an app can call `Load()` again to reload from sources and then `Apply()` to push changes to the application. Used in UICatalog's theme switcher.

**Current API:**
```csharp
// After user selects "Dark" theme:
ThemeManager.Theme = "Dark";
ConfigurationManager.Load(ConfigLocations.All);
ConfigurationManager.Apply();
```

---

### UC-26: Custom Schemes (App-Developer Defined)

**What:** App developers can add custom named Schemes beyond the built-in ones.

**Current API:**
```csharp
SchemeManager.AddScheme("MyCustomScheme", new Scheme {
    Normal = new Attribute(Color.BrightWhite, Color.DarkBlue),
    Focus = new Attribute(Color.Black, Color.BrightYellow),
    HotNormal = new Attribute(Color.BrightYellow, Color.DarkBlue),
    HotFocus = new Attribute(Color.BrightYellow, Color.BrightYellow),
    Disabled = new Attribute(Color.Gray, Color.DarkBlue)
});

// Assign to view
myView.SchemeName = "MyCustomScheme";
```

---

### UC-27: Access `SourcesManager.Sources` (Diagnostics)

**What:** `SourcesManager.Sources` is a `ConcurrentDictionary<ConfigLocations, string>` showing which files/resources were actually loaded. Used for diagnostics and "config editor" scenarios.

**Current API:**
```csharp
foreach (var (location, path) in ConfigurationManager.SourcesManager!.Sources)
{
    Console.WriteLine($"{location}: {path}");
}
```

---

### UC-28: Serialization / Export Configuration

**What:** The current configuration (or a specific scope) can be serialized to JSON for export, editing, or display.

**Current API:**
```csharp
// Serialize the current settings scope to JSON
string json = ConfigurationManager.SourcesManager!.ToJson(ConfigurationManager.Settings);

// Get hard-coded defaults as JSON
string defaults = ConfigurationManager.GetHardCodedConfig();
```

---

## 4. Current CM — Architecture Summary

```
Module Init (once, process-wide)
  └─ ConfigProperty.Initialize()
       └─ Scan all types in ConfigPropertyHostTypes.GetTypes()
       └─ Reflect on all [ConfigurationProperty] static properties
       └─ Cache hard-coded values (deep-cloned)

Enable(locations)
  └─ SourcesManager.LoadFromLocations(Settings, locations)
       └─ Load LibraryResources  ──┐
       └─ Load AppResources       │  Each is JSON → SettingsScope merge
       └─ Load GlobalHome         │  Higher precedence overwrites lower
       └─ Load GlobalCurrent      │
       └─ Load AppHome            │
       └─ Load AppCurrent         │
       └─ Load Env                │
       └─ Load Runtime            ─┘
  └─ InternalApply()
       └─ Settings.Apply()         → writes to all [CP(SettingsScope)] static props
       └─ ThemeManager.Themes[Theme].Apply() → writes to all [CP(ThemeScope)] static props
       └─ AppSettings.Apply()      → writes to all [CP(AppSettingsScope)] static props
       └─ OnApplied()              → fires Applied event
```

**Key structural characteristics:**
1. **Process-wide global state** — all state is in static fields.
2. **Attribute-based discovery** — `[ConfigurationProperty]` decorates static properties; CM finds them all via reflection at init.
3. **Push model** — loaded values are "pushed" to static properties during `Apply()`.
4. **Scope model** — three orthogonal scopes: SettingsScope, ThemeScope, AppSettingsScope.
5. **Theme layering** — ThemeScope properties are per-theme; switching themes means applying a different `ThemeScope` instance to the same static properties.

---

## 5. Proposed MEC-Based Architecture

This section describes the target architecture. It is a **proposal requiring review**, not a final decision.

### 5.1 Core Concepts

#### A. Replace Static Properties with POCOs + Options

Each group of related configuration properties becomes a POCO ("Settings class"):

```csharp
// Terminal.Gui/Configuration/Settings/ButtonSettings.cs
public class ButtonSettings
{
    public ShadowStyles DefaultShadow { get; set; } = ShadowStyles.Opaque;
    public MouseState DefaultMouseHighlightStates { get; set; } =
        MouseState.In | MouseState.Pressed | MouseState.PressedOutside;
}

// Terminal.Gui/Configuration/Settings/GlyphsSettings.cs
public class GlyphsSettings
{
    public Rune CheckStateChecked { get; set; } = (Rune)'☑';
    public Rune CheckStateUnChecked { get; set; } = (Rune)'☐';
    // ... all 144 glyph properties
}

// Terminal.Gui/Configuration/Settings/ApplicationSettings.cs
public class ApplicationSettings
{
    public AppModel AppModel { get; set; } = AppModel.FullScreen;
    public string ForceDriver { get; set; } = string.Empty;
    public bool IsMouseDisabled { get; set; } = false;
    // ...
}
```

#### B. MEC Replaces SourcesManager

`Microsoft.Extensions.Configuration` (`IConfiguration`) provides the same 9-level precedence stack through composable configuration providers:

| Current `ConfigLocations` | MEC Provider / Extension |
|---|---|
| `HardCoded` | POCO default property values (no provider needed) |
| `LibraryResources` | Custom `EmbeddedResourceConfigurationProvider` (Terminal.Gui.dll resource) |
| `AppResources` | Custom `EmbeddedResourceConfigurationProvider` (entry assembly resource) |
| `GlobalHome` | `JsonConfigurationExtensions.AddJsonFile("~/.tui/config.json", optional: true)` |
| `GlobalCurrent` | `JsonConfigurationExtensions.AddJsonFile("./.tui/config.json", optional: true)` |
| `AppHome` | `AddJsonFile("~/.tui/AppName.config.json", optional: true)` |
| `AppCurrent` | `AddJsonFile("./.tui/AppName.config.json", optional: true)` |
| `Env` | Custom `EnvironmentVariableChunkConfigurationProvider` for `TUI_CONFIG` |
| `Runtime` | `MemoryConfigurationProvider` or `AddInMemoryCollection(...)` |

#### C. `IOptions<T>` and `IOptionsMonitor<T>` for Consumption

Components access configuration through:
- `IOptions<T>` — a snapshot that does not change after the app starts.
- `IOptionsMonitor<T>` — subscribes to change notifications (live-update support, equivalent to the current `Applied` event).

#### D. Theme System Stays; InstanceSettings POCO Becomes the Vehicle

The theme system (named themes, scheme dictionaries) does not disappear. It becomes an `IOptionsMonitor<ThemeSettings>` where `ThemeSettings` includes:
- `string ActiveTheme` — the currently selected theme name.
- `Dictionary<string, ThemeDefinition> Themes` — all defined themes.
- `Dictionary<string, Scheme> Schemes` — schemes for the active theme (computed by `ThemeManager`).

#### E. Static Properties Remain as a Compatibility Facade (During Transition)

Static properties decorated with `[ConfigurationProperty]` do not disappear overnight. The transition plan (§8) keeps them but changes how they are populated: `IOptionsMonitor<T>.OnChange(...)` updates the static property rather than CM reflection.

#### F. `ThemeManager` and `SchemeManager` Become Services

```csharp
public interface IThemeManager
{
    string ActiveTheme { get; set; }
    ImmutableList<string> ThemeNames { get; }
    ThemeDefinition GetTheme(string name);
    void SwitchTheme(string themeName);
    event EventHandler<string> ThemeChanged;
}

public interface ISchemeManager
{
    void AddScheme(string name, Scheme scheme);
    void RemoveScheme(string name);
    Scheme GetScheme(string name);
    bool TryGetScheme(string name, out Scheme? scheme);
    ImmutableList<string> SchemeNames { get; }
}
```

#### G. Application-Level DI Registration Extension

```csharp
// Application startup
IConfigurationBuilder builder = new ConfigurationBuilder()
    .AddTuiLibraryDefaults()          // Loads Terminal.Gui.Resources.config.json
    .AddTuiAppDefaults("MyApp")       // Loads MyApp.Resources.config.json
    .AddTuiUserFiles("MyApp")         // Loads ~/.tui/*.json and ./.tui/*.json
    .AddTuiEnvironmentVariable()      // Loads TUI_CONFIG env var
    .AddTuiRuntimeConfig(myJsonStr);  // Loads in-memory override

IConfiguration config = builder.Build();

// Bind settings sections to POCOs
services.Configure<ApplicationSettings>(config.GetSection("Application"));
services.Configure<ButtonSettings>(config.GetSection("Button"));
services.Configure<GlyphsSettings>(config.GetSection("Glyphs"));
services.Configure<ThemeSettings>(config.GetSection("Themes"));
// ...
```

#### H. View Property Access Pattern

The static properties are replaced with instance fields, populated during construction:

```csharp
public class Button : View
{
    // Instance value beats the configured default
    private ShadowStyles? _shadowStyle;

    public ShadowStyles ShadowStyle
    {
        get => _shadowStyle ?? _settings.DefaultShadow;
        set
        {
            _shadowStyle = value;
            SetNeedsDisplay();
        }
    }

    // Construction via DI host
    public Button(IOptionsMonitor<ButtonSettings> settings)
    {
        _settings = settings;
        _settingsSubscription = settings.OnChange(OnSettingsChanged);
    }

    // OR: Construction without DI (for tests, direct use)
    public Button() : this(new StaticOptionsMonitor<ButtonSettings>(new ButtonSettings())) { }
}
```

> **Note on parameterless constructors:** See §7, Debate Item D-01. Requiring constructor injection for all views is a significant breaking change.

---

## 6. Use Case → MEC Mapping

| UC | Current API | MEC Equivalent | Notes |
|----|-------------|----------------|-------|
| UC-01 SettingsScope property | `[ConfigurationProperty(Scope = typeof(SettingsScope))]` on static prop | `services.Configure<ApplicationSettings>(config.GetSection("Application"))` | POCO replaces attribute |
| UC-02 ThemeScope property | `[ConfigurationProperty(Scope = typeof(ThemeScope))]` on static prop | `services.Configure<ButtonSettings>(...)`, `IOptionsMonitor<ButtonSettings>` | Per-component Settings POCOs |
| UC-03 AppSettingsScope property | `[ConfigurationProperty]` on app static prop | App-developer registers own `Configure<MyAppSettings>(...)` | Same pattern, more explicit |
| UC-04 Enable loading | `ConfigurationManager.Enable(ConfigLocations.All)` | `IConfigurationBuilder.AddTuiUserFiles(...)` | Explicit builder, no opt-in flag |
| UC-05 9-level precedence | `ConfigLocations` flags | MEC provider chain ordering | See §5.1B for mapping |
| UC-06 Apply | `ConfigurationManager.Apply()` | `IOptionsMonitor<T>.OnChange(...)` auto-push | Push replaced by pull-on-change |
| UC-07 Reset to defaults | `ConfigurationManager.Disable(resetToHardCodedDefaults: true)` | Rebuild config without user files; or set POCO defaults | **Debate item D-06** |
| UC-08 Runtime override | `ConfigurationManager.RuntimeConfig = json` | `services.Configure<T>` or `AddInMemoryCollection(...)` | Still supported; different API |
| UC-09 Themes | `ThemeManager.Theme = "Dark"; ConfigurationManager.Apply()` | `IThemeManager.SwitchTheme("Dark")` | `IThemeManager` service |
| UC-10 Theme change events | `ThemeManager.ThemeChanged`, `ConfigurationManager.Applied` | `IThemeManager.ThemeChanged`, `IOptionsMonitor<T>.OnChange` | Equivalent |
| UC-11 Color schemes | `SchemeManager.GetScheme(Schemes.Base)` | `ISchemeManager.GetScheme("Base")` | Equivalent |
| UC-12 Assign scheme to View | `view.SchemeName = "Dialog"` | No change — `SchemeName` property retained | No change needed |
| UC-13 Hard-coded config JSON | `ConfigurationManager.GetHardCodedConfig()` | Serialize default POCO values to JSON | **Debate item D-07** |
| UC-14 Library resource config | `ConfigLocations.LibraryResources` | `AddTuiLibraryDefaults()` | Equivalent |
| UC-15 App resource config | `ConfigLocations.AppResources` | `AddTuiAppDefaults(appName)` | Equivalent |
| UC-16 Global user files | `ConfigLocations.GlobalHome/Current` | `AddTuiUserFiles(appName)` | Equivalent |
| UC-17 Per-app user files | `ConfigLocations.AppHome/Current` | Part of `AddTuiUserFiles(appName)` | Equivalent |
| UC-18 Env variable | `ConfigLocations.Env` | `AddTuiEnvironmentVariable()` | Equivalent |
| UC-19 JSON error handling | `ThrowOnJsonErrors`, `PrintJsonErrors()` | MEC built-in error handling + `ILogger` | Improved |
| UC-20 Custom JSON converters | Bespoke `JsonConverter` classes | Same converters, registered with STJ via `JsonSerializerOptions` in MEC JSON provider | Reuse existing converters |
| UC-21 AOT/Trim compat | `[DynamicDependency]` on 29 types | POCOs use source-generated STJ; no reflection discovery | **The primary improvement** |
| UC-22 Disable CM | `ConfigurationManager.Disable()` | N/A — MEC is always "enabled"; omit providers to restrict | **Debate item D-08** |
| UC-23 AppName | `ConfigurationManager.AppName` | Parameter to `AddTuiUserFiles(appName)` | Equivalent |
| UC-24 Applied event | `ConfigurationManager.Applied += handler` | `IOptionsMonitor<T>.OnChange(handler)` | More granular (per-type) |
| UC-25 Live reload | `ConfigurationManager.Load(All); Apply()` | `IOptionsMonitor<T>` auto-notifies | Handled automatically |
| UC-26 Custom schemes | `SchemeManager.AddScheme(...)` | `ISchemeManager.AddScheme(...)` | No change in capability |
| UC-27 Sources diagnostics | `SourcesManager.Sources` | MEC does not expose loaded sources by default | **Debate item D-09** |
| UC-28 Serialize current config | `SourcesManager.ToJson(settings)` | Enumerate `IOptions<T>` values and serialize | Different API, same result |

---

## 7. Functionality Changes Requiring Explicit Debate

> **IMPORTANT:** These are not decisions. Each item below is a **debate question**. No implementation work may begin on any item marked here until the team has discussed it and recorded a resolution in this section.

---

### D-01: Constructor Injection vs. Parameterless Construction

**The question:** Terminal.Gui Views are currently created with `new Button()`. DI injection requires `new Button(IOptionsMonitor<ButtonSettings>)`. These two forms cannot coexist without providing a parameterless overload that uses a fallback (e.g., default POCOs, or a static `ApplicationServices` resolver).

**Options:**
1. **Dual constructors** — Parameterless constructor creates a `StaticOptionsMonitor<ButtonSettings>` backed by default values. DI constructor injects the real monitor. This preserves backward compatibility at the cost of complexity.
2. **Static facade for defaults** — `ButtonSettings.Defaults` is a static singleton POCO. The parameterless constructor reads from it. The DI path injects a live monitor. The facade is updated by the MEC binding.
3. **Ambient DI resolution** — Use `Application.Services.GetService<IOptionsMonitor<ButtonSettings>>()` internally when no explicit injection is provided. Familiar to ASP.NET Core developers; an antipattern in library code.

**Decision: Option 2 (Static facade).**

**Rationale:** Views are independent objects that can be constructed before `Application.Create()` and without any DI container. Option 1 breaks this contract for all app developers. Option 3 also breaks it — if no `Application` exists yet, there is no service provider to resolve from, so it degrades to a static fallback anyway. Option 2 preserves the existing `new Button()` contract, is AOT-safe, and requires no ceremony in tests. Future multi-instance support (#4366) can be addressed by having each `IApplication.Init()` swap the static facade to its own values.

---

### D-02: JSON Schema Compatibility

**The question:** The current config.json format uses flat keys like `"Button.DefaultShadow": "Opaque"` for ThemeScope properties. MEC's standard JSON provider uses nested sections like `"Button": { "DefaultShadow": "Opaque" }`.

**Options:**
1. **Preserve flat keys** — Write a custom MEC JSON provider that translates flat CM-style keys to MEC-style nested keys.
2. **Adopt nested format** — Break backward compatibility with existing user config files; provide a migration tool.
3. **Support both** — Custom provider accepts both formats; flat keys are deprecated.

**Decision: Option 3 (Support both; flat keys deprecated).**

**Rationale:** Existing user config files continue to work without modification. The custom provider accepts both flat (`"Button.DefaultShadow"`) and nested (`"Button": { "DefaultShadow": ... }`) formats. Flat keys emit a deprecation warning at load time. New documentation and generated defaults use the nested MEC-native format. This avoids a breaking change while guiding users toward the standard format.

---

### D-03: ThemeScope Remains vs. Merges Into App-Level Themes

**The question:** ThemeScope is currently a global per-process concept. Every Button reads `Button.DefaultShadow` from the one active theme. With MEC and instance-based config, could different application "sessions" (if Terminal.Gui ever supports multiple concurrent `IApplication` instances) have different active themes?

**Options:**
1. **Scoped themes per IApplication** — `IThemeManager` is scoped to the `IApplication` instance. Two concurrent apps could have different themes.
2. **Process-wide themes** — Theme remains a process-wide concept (simplest, matches current behavior).

**Recommendation:** Defer to the instance-based application proposal (issue #4366). For the initial CM→MEC replacement, keep themes process-wide (option 2). Revisit when multi-instance becomes a concrete requirement.

---

### D-04: AppSettingsScope — Breaking Change for App Developers

**The question:** App developers currently use `[ConfigurationProperty]` on their own static properties, and CM discovers them via `ConfigPropertyHostTypes`. In MEC, app developers would instead register their own `services.Configure<MySettings>(config.GetSection("MySettings"))`.

**User impact:** Every app that currently has custom `[ConfigurationProperty]` properties must be updated. This is a **breaking change for all app developers**.

**Mitigation:** Provide a compatibility shim period (one major version) where both approaches work. Document the migration path clearly.

---

### D-05: `ThrowOnJsonErrors` / `PrintJsonErrors` Behavior

**The question:** CM currently collects JSON parsing errors and either throws or logs them lazily. MEC throws immediately on parse failure by default.

**Options:**
1. **Adopt MEC behavior** — Errors throw at `Build()` time. This is safer and more predictable.
2. **Wrap MEC with try/catch** — Maintain the "collect errors, report at shutdown" behavior via a wrapper.

**Recommendation:** Adopt MEC behavior (option 1) with an `ILogger` integration. Remove `PrintJsonErrors()` which currently writes to `Console.WriteLine`.

---

### D-06: "Disable" Concept

**The question:** CM has `Enable()` and `Disable()`. MEC does not have an on/off switch; it is always active. Omitting providers is equivalent to "disabled."

**Options:**
1. **Remove the Enable/Disable API** — Apps that want no configuration simply don't call `AddTuiUserFiles()`. Library defaults are always in POCOs.
2. **Keep a compatibility wrapper** — A `TuiConfigurationManager` class wraps the MEC builder and provides `Enable(locations)`-style API.

**Recommendation:** Option 1. The "disabled by default" design of CM was necessary because CM's reflection scan is expensive and breaks AOT. With MEC + POCOs, the cost of "configuration" is near zero. There is no reason to opt in.

---

### D-07: `GetHardCodedConfig()` and `GetEmptyConfig()`

**The question:** These methods generate JSON from the hard-coded defaults. They are used by:
1. The `SaveDefaults` unit test to regenerate `config.json`.
2. Potentially by a "config editor" scenario in UICatalog.

With POCOs, "hard-coded defaults" are simply the default property values of each POCO. Serializing them to JSON is straightforward (`JsonSerializer.Serialize(new ButtonSettings())`).

**Resolution needed:** Decide whether a `TuiConfigurationSerializer` utility class is needed for tooling, or whether each POCO's defaults are sufficient.

---

### D-08: Sources Diagnostics (`SourcesManager.Sources`)

**The question:** `SourcesManager.Sources` shows exactly which files were loaded and from which location. MEC does not expose this natively. It is used in diagnostic scenarios (the UICatalog "Config" menu shows which config files are loaded).

**Options:**
1. **Drop the diagnostics API** — UICatalog's config editor is simplified or removed.
2. **Implement a `ITuiConfigurationSources` service** — Records which providers loaded successfully during `Build()`.

**Recommendation:** Option 2 is preferable for transparency to end users. It is not required for the initial replacement but should be in scope for Phase 2.

---

### D-09: `RuntimeConfig` String API

**The question:** `ConfigurationManager.RuntimeConfig` is a raw JSON string property that can be set at any time before `Load()`. This is used in UICatalog and tests. MEC's equivalent is `AddInMemoryCollection()` or re-building the configuration. Re-building requires that all subscribers (IOptionsMonitor) be notified.

**Options:**
1. **MEC in-memory provider** — `AddInMemoryCollection(dictionary)` is equivalent but requires the key-value format rather than JSON.
2. **Custom JSON in-memory provider** — Write an `InMemoryJsonConfigurationProvider` that accepts a JSON string and can be updated after `Build()`. MEC supports reloading via `IConfigurationSource.ReloadOnChange`.

**Recommendation:** Option 2. Existing tests and UICatalog code that sets `RuntimeConfig` to a JSON string should continue to work.

---

### D-10: Rune / Key / Color Custom JSON Converters

**The question:** The existing custom converters for `Rune`, `Key`, `Color`, `Attribute`, etc. are STJ `JsonConverter<T>` classes. They must be preserved because:
1. The JSON config files use the Rune-as-character format (`"☑"`) not the .NET default.
2. Keys are `"Ctrl+Q"` strings, not numeric codes.

**Options:**
1. **Reuse existing converters as-is** — Register them with `JsonSerializerOptions` in the MEC JSON configuration provider.
2. **Migrate to STJ source generation** — Generate converters at compile time.

**Recommendation:** Option 1 initially (reuse), then option 2 as a follow-on AOT improvement.

---

## 8. Implementation Phases

> **Status note:** Implementation is already underway. This section now serves as the execution plan and remaining-work tracker.

### Phase 0 — Spec Review and Debate Resolution (Current Phase)

- [ ] Community review of this spec document.
- [ ] Resolution of all debate items in §7 (D-01 through D-10).
- [ ] Update this spec with decisions.
- [ ] Create a formal backwards-compatibility migration guide outline.

### Phase 1 — Foundation: POCOs and MEC Provider Chain

**Goal:** Create the MEC infrastructure without changing any View APIs or removing CM.

1. Add `Microsoft.Extensions.Configuration.Json` and `Microsoft.Extensions.Options` NuGet dependencies.
2. Create all settings POCOs (`ButtonSettings`, `GlyphsSettings`, `ApplicationSettings`, `ThemeSettings`, etc.).
3. Implement `TuiConfigurationBuilder` (the MEC-based equivalent of `SourcesManager`).
4. Implement custom MEC providers for embedded resources, `TUI_CONFIG` env var, and in-memory JSON.
5. Implement `IThemeManager` and `ISchemeManager` as services backed by `IOptionsMonitor<ThemeSettings>`.
6. Implement `TuiConfigurationExtensions` with `AddTuiLibraryDefaults()`, `AddTuiUserFiles()`, etc.
7. **Tests:** Write unit tests for POCOs and the builder. All tests in `Tests/UnitTestsParallelizable`.

### Phase 2 — Wire Views to IOptionsMonitor

**Goal:** Views read from POCOs rather than static properties; existing static properties become a facade.

1. Add `IOptionsMonitor<T>` fields to each View that has `[ConfigurationProperty(Scope = typeof(ThemeScope))]` properties.
2. Create parameterless constructor overloads backed by default POCOs (resolve D-01).
3. Static `[ConfigurationProperty]` properties remain as setters that forward to the POCO.
4. The `IOptionsMonitor<T>.OnChange(...)` callback updates the static properties (to maintain compatibility) and calls `SetNeedsDisplay()`.
5. **Tests:** Verify that a View created with default constructor has the correct default values. Verify that changing an `IOptionsMonitor` fires redraw.

### Phase 3 — Theme and Scheme Refactor

**Goal:** Themes and schemes are managed by `IThemeManager` / `ISchemeManager`; the ThemeScope/SettingsScope POCO infrastructure is the source of truth.

1. Migrate `ThemeManager` to `IThemeManager`.
2. Migrate `SchemeManager` to `ISchemeManager`.
3. Theme switching becomes `IThemeManager.SwitchTheme(name)`, which updates `IOptionsMonitor<ThemeSettings>`, which triggers `OnChange` on all registered View subscriptions.
4. Preserve JSON schema compatibility for built-in `config.json` (resolve D-02).
5. **Tests:** Verify theme switching triggers `SetNeedsDisplay()` on affected views.

### Phase 3A — Finish Complex-Type Migration (Current #5411 focus)

**Goal:** Complete the functional CM→MEC migration for complex CM-owned types so effective runtime behavior is MEC-based before CM removal.

1. **Theme graph migration**
   - Ensure `"Theme"` scalar binding and reset semantics are correct (no stale state across rebuilds).
   - Ensure `"Themes"` definitions bind correctly for active theme selection and fallback behavior.
2. **Scheme dictionary migration**
   - Ensure current-theme schemes are sourced from MEC-bound data.
   - Preserve existing built-in scheme names and lookup behavior.
3. **Key-binding migration**
   - Ensure `Application.DefaultKeyBindings`, `View.DefaultKeyBindings`, and `View.ViewKeyBindings` load from MEC config.
   - Preserve command/key parsing behavior with existing key converters.
4. **Color dictionary migration**
   - Ensure `Color.Colors16` is loaded from MEC with existing key/value semantics.
5. **Mixed-format compatibility**
   - Support both nested MEC sections and legacy flat dotted keys during migration.
   - In mixed configs, preserve provider precedence (higher-precedence keys must win).
6. **Tests and acceptance criteria**
   - Add focused tests for themes, schemes, key-binding dictionaries, and `Colors16`.
   - Add regression tests for mixed nested+dotted config overlays and scalar reset behavior.
   - Keep full UnitTestsParallelizable and CI green.

**Definition of done for PR #5411:** complex-type behavior is fully MEC-driven (with compatibility shims still present), and all migration tests pass.

#### Phase 3A.x — Internal subscriber rewiring (landed in #5411)

The following items were pulled forward from the planned post-#5411 removal work because they make the MEC story coherent without requiring CM deletion:

- **A1 — `IThemeManager.ThemeChanged` event.** Added an `EventHandler<EventArgs<string>>` to `IThemeManager`. `MecThemeManager` subscribes to legacy `ThemeManager.ThemeChanged` in its constructor and forwards. The runtime theme/scheme dictionary still lives in legacy `ConfigurationManager.Settings["Themes"]`; Mec presents the API + event surface. Full Mec ownership of theme/scheme data (Phase A2) is deferred to #5416, where deletion of `ScopeJsonConverter` and the array-of-single-key-object JSON shape can be addressed together.
- **B — `ThemeChanges` static facade.** New `Terminal.Gui.Configuration.ThemeChanges` static class exposes a single `ThemeChanged` event that bridges both `ConfigurationManager.Applied` and `ThemeManager.ThemeChanged`. The four internal view subscribers — `Menu`, `MenuBar`, `StatusBar`, `LineCanvas` — now subscribe to `ThemeChanges.ThemeChanged` instead of `ConfigurationManager.Applied`. `ConfigurationManager.Applied` remains public and `[Obsolete]` for external consumers; #5416 can delete it once external migration windows close.
- **C — `TuiSerializerContext` extraction.** The configured `SourceGenerationContext` instance (with `RuneJsonConverter`, `KeyJsonConverter`, `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, comment-skip, trailing commas, etc.) now lives in a non-obsolete `internal static class TuiSerializerContext` with a single `Instance` field. The obsolete `ConfigurationManager.SerializerContext` is preserved as a thin delegator (`= TuiSerializerContext.Instance`) for back-compat. All internal JSON-converter consumers and the four affected test classes (`Key`/`Rune`/`Scheme`/`ScopeJsonConverterTests`) reference `TuiSerializerContext.Instance` directly, eliminating the `#pragma warning disable CS0618` blocks in `AttributeJsonConverter`, `SchemeJsonConverter`, `DictionaryJsonConverter`, `ConcurrentDictionaryJsonConverter`, and `DeepCloner`. The remaining two pragmas (`ScopeJsonConverter`, `SourcesManager`) stay narrowed for *other* obsolete CM uses unrelated to the serializer context; they are removed wholesale in #5416 step D.

The following items were considered for #5411 but deferred to #5416 with documented reasons:

- **A2 — Mec actually owns theme/scheme runtime data.** Requires either D-02 (config.json nested-shape migration) or a MEC-compatible parser for the array-of-single-key-object format. Out of scope for #5411 because it would force a JSON schema decision; that decision belongs in the deletion PR where `ScopeJsonConverter` is also being removed.
- **Phase D — `config.json` flat → nested migration.** CM's read path still exists in #5411 and would break on a nested-format resource file. Must be paired with `ScopeJsonConverter` deletion in #5416.
- **Phase E — Delete CM types.** The whole point of marking them `[Obsolete]` in #5411 is to provide a one-release shim window.
- **`PrintJsonErrors()` removal / `OnLoadException` wiring.** Behavior-preserving replacement is possible via `JsonConfigurationSource.OnLoadException` (sets `ctx.Ignored = true` to swallow parse errors and aggregate them for deferred display). Out of scope for #5411 to avoid expanding the diff further; folded into #5416's scope with the explicit note that fail-fast vs. deferred-error parity is preservable.

### Phase 4 — Remove [ConfigurationProperty] Static Statics

**Goal:** Remove the reflection-based CM machinery. Static properties become plain statics that are only updated by MEC.

1. Remove `[ConfigurationProperty]` attribute from all library properties.
2. Remove `ConfigProperty`, `ConfigPropertyHostTypes`, `Scope<T>`, `ScopeJsonConverter<T>`, `DeepCloner`.
3. Remove `Assembly.GetTypes()` scan from `ConfigProperty.Initialize()`.
4. Remove `[DynamicDependency]` annotations from `ConfigPropertyHostTypes`.
5. Retain `SourcesManager` as a compatibility class if needed (resolve D-09).
6. **Tests:** Verify AOT build size reduces. Verify tests still pass.

### Phase 5 — App Developer Migration (AppSettingsScope)

**Goal:** Migrate the AppSettingsScope pattern to MEC `services.Configure<T>(...)`.

1. Document the migration guide for app developers.
2. Remove or deprecate `AppSettingsScope` and `[ConfigurationProperty]` for app use.
3. Update UICatalog to use the new API.
4. **Tests:** Write tests that demonstrate the app-developer workflow.

### Phase 6 — Backward Compatibility Cleanup

**Goal:** Remove the CM shim layer; CM is fully replaced.

1. Mark `ConfigurationManager` class as `[Obsolete]`.
2. Remove in the next major version.
3. Final binary size measurement on the NativeAot example.

**Planned follow-up:** This cleanup/removal work is tracked in stacked PR #5416 so #5411 can complete functional migration first.

---

## 9. Open Questions

These questions are not debates about functionality changes — they are unresolved design questions that need answers before Phase 1 work can begin.

### Q-01: NuGet Dependency Footprint

Adding `Microsoft.Extensions.Configuration.Json` and `Microsoft.Extensions.Options` to Terminal.Gui's package dependencies affects all consumers. The packages are small and ubiquitous in .NET, but:
- Does this conflict with any consumer's version constraints?
- Do the new packages themselves have AOT/trim issues?

**Action:** Check [NuGet.org advisory database](https://advisories.nuget.org) and NativeAOT compatibility notes for each dependency before Phase 1.

### Q-02: Generic Host vs. Just MEC

The issue proposal describes using `IHostBuilder` / `Host.CreateDefaultBuilder(args)`. However, adopting the Generic Host is a much larger change than just adopting MEC.

**Recommendation:** Phase 1 through 5 use only `IConfigurationBuilder` + `IOptions<T>` (no Generic Host). The Generic Host integration (if desired) is a separate, later proposal.

### Q-03: JSON Schema URL

The current `config.json` includes `"$schema": "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json"`. The schema must be updated if the JSON format changes (D-02). Who maintains the hosted schema?

### Q-04: `Glyphs` — 144 Properties

`Glyphs` has 144 `[ConfigurationProperty(Scope = typeof(ThemeScope))]` properties. Migrating each to a `GlyphsSettings` POCO property is mechanical but voluminous. Is there a better structure for the POCO (e.g., a `Dictionary<string, Rune>` for extensibility)?

### Q-05: Custom Converter AOT

The `RuneJsonConverter`, `KeyJsonConverter`, etc. must be verified to work with STJ source generation. They use polymorphism (`JsonConverter<T>`) which *is* compatible, but they must be registered with `SourceGenerationContext`. This needs a proof-of-concept before Phase 1.

### Q-06: `IOptionsMonitor` for AOT

`IOptionsMonitor<T>` is in `Microsoft.Extensions.Options`. It uses generics but not dynamic code. It *should* be AOT-safe. This needs verification with the `PublishAot=true` build before Phase 1 is committed.

---

## 10. Out of Scope

The following are explicitly **not** part of this proposal:

1. **Generic Host / IHostBuilder adoption** — See Q-02. This is a separate, larger proposal.
2. **Constructor injection mandate for all Views** — Depends on D-01 resolution. If DI injection is not made mandatory, Views do not require DI at all.
3. **View-level DI** — Individual views resolving services from `IServiceProvider` is not proposed here.
4. **Multiple concurrent application instances** — Follows from the separate instance-based `IApplication` proposal (#4366).
5. **TextMate/Markdig conditional compilation** — Mentioned in the AOT analysis; separate issue.
6. **Lazy driver registration** — Also mentioned in the AOT analysis; separate issue.
7. **Assembly splitting** — Also mentioned in the AOT analysis; will be more impactful after this work is complete.

---

## Appendix A: Current [ConfigurationProperty] Property Full Inventory

### SettingsScope (process-wide)

| Class | Property | Type | Hard-coded Default |
|---|---|---|---|
| `Application` | `AppModel` | `AppModel` | `FullScreen` |
| `Application` | `ForceDriver` | `string` | `""` |
| `Application` | `DefaultKeyBindings` | `Dictionary<Command, PlatformKeyBinding>?` | Platform-specific |
| `Application` | `IsMouseDisabled` | `bool` | `false` |
| `ConfigurationManager` | `ThrowOnJsonErrors` | `bool?` | `false` |
| `Driver` | `Force16Colors` | `bool` | `false` |
| `Driver` | `SizeDetection` | `SizeDetectionMode` | `Polling` |
| `Key` | `Separator` | `char` | `'+'` |
| `MenuBar` | `DefaultKey` | `Key` | `F10` |
| `PopoverMenu` | `DefaultKey` | `Key` | `Shift+F10` |
| `FileDialog` | `DefaultOpenMode` | `OpenMode` | `Mixed` |
| `FileDialogStyle` | `DefaultSearchMatcher` | `ISearchMatcher?` | `DefaultSearchMatcher` |
| `FileDialogStyle` | `PreferResultFromOpenMode` | `bool` | `true` |
| `Trace` | `EnabledCategories` | `TraceCategory` | `None` |
| `View` | `DefaultKeyBindings` | `Dictionary<...>?` | Platform-specific |
| `View` | `ViewKeyBindings` | `Dictionary<string, Dictionary<...>>?` | `null` |
| `Color` | `Colors16` *(OmitClassName)* | `Dictionary<ColorName16, string>` | *(platform defaults)* |

### ThemeScope (per-theme)

| Class | Property | Type | Hard-coded Default |
|---|---|---|---|
| `Button` | `DefaultShadow` | `ShadowStyles` | `Opaque` |
| `Button` | `DefaultMouseHighlightStates` | `MouseState` | `In\|Pressed\|PressedOutside` |
| `CheckBox` | `DefaultMouseHighlightStates` | `MouseState` | `PressedOutside\|Pressed\|In` |
| `Dialog` | `DefaultShadow` | `ShadowStyles` | `Transparent` |
| `Dialog` | `DefaultBorderStyle` | `LineStyle` | `Heavy` |
| `Dialog` | `DefaultButtonAlignment` | `Alignment` | `End` |
| `Dialog` | `DefaultButtonAlignmentModes` | `AlignmentModes` | `StartToEnd\|AddSpaceBetweenItems` |
| `FrameView` | `DefaultBorderStyle` | `LineStyle` | `Single` |
| `HexView` | `DefaultBorderStyle` | `LineStyle` | `Single` |
| `Menu` | `DefaultBorderStyle` | `LineStyle` | `Single` |
| `MenuBar` | `DefaultBorderStyle` | `LineStyle` | `None` |
| `MessageBox` | `DefaultBorderStyle` | `LineStyle` | `Heavy` |
| `MessageBox` | `DefaultButtonAlignment` | `Alignment` | `Center` |
| `NerdFonts` | `Enable` | `bool` | `false` |
| `SelectorBase` | `DefaultBorderStyle` | `LineStyle` | `Single` |
| `StatusBar` | `DefaultBorderStyle` | `LineStyle` | `None` |
| `TextField` | `DefaultBorderStyle` | `LineStyle` | `Single` |
| `TextView` | `DefaultBorderStyle` | `LineStyle` | `Single` |
| `Window` | `DefaultBorderStyle` | `LineStyle` | `Single` |
| `Window` | `DefaultShadow` | `ShadowStyles` | `None` |
| `CharMap` | `DefaultBorderStyle` | `LineStyle` | `Single` |
| `LinearRangeDefaults` | *(multiple)* | *(various)* | *(see source)* |
| `NerdFonts` | `Enable` | `bool` | `false` |
| `SchemeManager` | `Schemes` | `Dictionary<string, Scheme?>?` | Built-in schemes |
| `Glyphs` | `WideGlyphReplacement` | `Rune` | `(Rune)' '` |
| `Glyphs` | `File` | `Rune` | `(Rune)'☰'` |
| `Glyphs` | `Folder` | `Rune` | `(Rune)'꤉'` |
| `Glyphs` | *(~141 more)* | `Rune` | *(various)* |

### AppSettingsScope (app-specific)

Defined by app developers. Terminal.Gui itself does not own any AppSettingsScope properties (as of writing).

---

## Appendix B: External Config Files

### Terminal.Gui Library Resource: `config.json`

Located at `Terminal.Gui/Resources/config.json`. Embedded in `Terminal.Gui.dll`. 1,501 lines. Defines:
- All built-in themes (Default, Dark, Light, TurboPascal 5, Anders, Green Phosphor, Amber Phosphor)
- Each theme defines its Schemes (color/attribute pairs for Base, Dialog, Menu, Error, Accent, etc.)
- Each theme optionally overrides ThemeScope properties (glyph sets, border styles, shadow styles)

**This file's format must be preserved** unless the team decides otherwise in D-02.

### App Config File Format

```json
{
  "$schema": "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
  "Theme": "Dark",
  "Application.ForceDriver": "NetDriver",
  "Driver.Force16Colors": false,
  "AppSettings": {
    "MyApp.ShowStatusBar": true
  },
  "Themes": [
    {
      "Default": {
        "Button.DefaultShadow": "None",
        "Schemes": [
          {
            "Base": {
              "Normal": { "Foreground": "Yellow", "Background": "DarkBlue" }
            }
          }
        ]
      }
    }
  ]
}
```

---

*Last updated: 2026-05-25. Authors: @copilot (specification), reviewed by team.*

# Spec: Remove Legacy ConfigurationManager (Phase 4–6 of CM → MEC)

> **Status:** Implemented in PR #5416 (`tig/remove-cm-followup`).
> **Tracking Issue:** [#4943](https://github.com/gui-cs/Terminal.Gui/issues/4943)
> **PR:** [#5416 — `tig/remove-cm-followup`](https://github.com/gui-cs/Terminal.Gui/pull/5416) (stacked on `copilot/replace-cm-with-mec`)
> **Predecessor Spec:** [`specs/replace-cm-with-mec.md`](./replace-cm-with-mec.md)

---

## 1. Purpose

PR #5411 completed the **functional** CM → MEC migration plus the cross-cutting cleanup that did not require deleting CM itself:

- Per‑component Settings POCOs with static `Defaults` facades.
- `TuiConfigurationBuilder` + `TuiConfigurationExtensions` providing the MEC-based source chain.
- `IThemeManager` / `ISchemeManager` services. `IThemeManager.ThemeChanged` exists and is wired (A1 done).
- `Terminal.Gui.Configuration.ThemeChanges` static facade — bridges both `ConfigurationManager.Applied` (because `CM.Apply()` writes `ConfigProperty` values directly, bypassing C# setters) and `IThemeManager.ThemeChanged` into a single observer-friendly event. All four internal view subscribers (`Menu`, `MenuBar`, `StatusBar`, `LineCanvas`) have migrated off `ConfigurationManager.Applied`.
- `Terminal.Gui.Configuration.TuiSerializerContext` — internal, non-obsolete holder of the configured `SourceGenerationContext` instance with all custom converters and JSON options. All 7 internal JSON converter consumers reference `TuiSerializerContext.Instance` directly; their `#pragma warning disable CS0618` blocks are gone. `ConfigurationManager.SerializerContext` is now a one-line delegator.
- All `[ConfigurationProperty(...)]` attributes removed from production view/option properties (only the attribute *class* still exists).
- `ConfigurationManager`, `SourcesManager`, `ConfigProperty`, `Scope<T>`, `DeepCloner`, `ScopeJsonConverter` marked `[Obsolete]` and only referenced internally during the transition.

This PR finishes the migration by **deleting** every legacy CM type, **migrating** the embedded `Resources/config.json` to a MEC-compatible shape, **promoting** `MecThemeManager`/`MecSchemeManager` from event-bridging wrappers to data owners (A2), and **removing** all CM-specific tests. The result is the AOT size reduction and parallel-test unlock that motivated #4943.

This corresponds to **Phases 4, 5, and 6** of the predecessor spec, executed as a single stacked PR. See the `Phase 3A.x — Internal subscriber rewiring (landed in #5411)` subsection of [`specs/replace-cm-with-mec.md`](./replace-cm-with-mec.md) for the authoritative record of what shipped in the parent PR.

---

## 2. Goals

1. **Complete Phase A2** — Migrate runtime theme/scheme data ownership from `ConfigurationManager.Settings["Themes"]` (parsed by `ScopeJsonConverter`) into `IOptionsMonitor<ThemeSettings>`. This is the gating prerequisite for deleting `ScopeJsonConverter` and is tightly coupled to the D-02 resource-shape decision (see §5.4).
2. **Delete every legacy CM type and attribute** (no `[Obsolete]` shim period — they were already shimmed in #5411).
3. **Eliminate CM-related anti-trim patterns**: `ConfigPropertyHostTypes.GetTypes()`, `[DynamicDependency]` on 29 host types, `Assembly.GetTypes()` reflection scan, `DeepCloner` reflection, `MakeGenericType` usage.
4. **Migrate the embedded `Terminal.Gui/Resources/config.json`** from the legacy flat-key `Themes: [ { Name: { "Class.Prop": ... } } ]` layout to a MEC-readable shape. Either nested-only with a one-release migration helper, **or** a custom MEC source that parses the legacy shape directly — see D-02 in §5.4.
5. **Delete the four-line `ConfigurationManager.Applied` bridge** inside `ThemeChanges` once `ConfigurationManager` is gone. Keep `ThemeChanges` itself as the supported public observer facade.
6. **Delete the `ConfigurationManager.SerializerContext` delegator** (one field). All real consumers already reference `TuiSerializerContext.Instance`.
7. **Delete all CM-only tests** (`ConfigurationMangerTests`, `ConfigPropertyAssemblyScanTests`, `ConfigPropertyTests`, `ConfigPropertyHostTypesTests`, `DeepClonerTests`, `ScopeJsonConverterTests`, `ScopeTests`, `SettingsScopeTests`, `ThemeScopeTests`, `ConfigurationPropertyAttributeTests`, legacy `SourcesManagerTests`). Keep MEC equivalents.
8. **Measure and record the NativeAOT binary-size delta** in PR #5416 so the stated motivation in #4943 is verifiable.
9. **Update `docfx/docs/config.md`** so the documented contract matches the shipped contract (Constitution: *Documentation Is the Spec*).

### Non‑goals

- Generic Host / `IHostBuilder` adoption (deferred, Q‑02).
- View constructor injection of `IOptionsMonitor<T>` (deferred — static `Defaults` facade remains per D‑01).
- Multi-instance `IApplication` scoping of themes (D‑03).
- Lazy driver registration / assembly splitting / TextMate conditional compilation (out of scope per #10 of predecessor spec).

---

## 3. Constitution Alignment

| Tenet | How this PR aligns |
|-------|--------------------|
| ✅ **Testability First** | Removing CM static state moves every theme/scheme test into `UnitTestsParallelizable`. |
| ✅ **Performance Is a Feature** | Removes the `Assembly.GetTypes()` scan + DeepCloner reflection at module init. Hot path (`Rune` read on a glyph, `ShadowStyles` read on Button) becomes a direct field read on a POCO. |
| ✅ **Users Have Final Control** | All configurable surfaces stay configurable. Only the *internal mechanism* changes; the documented public configuration model is MEC. |
| ✅ **Separation of Concerns** | Views no longer reach into a process-wide static (`ConfigurationManager.Settings["Foo"]`). They observe `IOptionsMonitor<T>` (via the static `Defaults` facade for now). |
| ✅ **Respect What Came Before** | The 9‑level precedence semantics are preserved through the MEC provider chain assembled in `TuiConfigurationExtensions`. The user-facing JSON keeps the same *shape* aside from the flat→nested key migration. |
| 🔴 **Documentation Is the Spec** | `docfx/docs/config.md` is rewritten in this PR; no out-of-date references to `ConfigurationManager.Enable`, `RuntimeConfig`, `Applied` event, etc. |

---

## 4. Current Residual Surface (post‑#5411)

The exhaustive inventory of what still has to go. Discovered by `grep`ping the worktree against `copilot/replace-cm-with-mec` HEAD.

### 4.1 Types to **delete** entirely (`Terminal.Gui/Configuration/`)

| File | Notes |
|------|-------|
| `ConfigurationManager.cs` (~33 KB) | Static facade; obsolete |
| `ConfigurationManagerEventArgs.cs` | `Applied`/`Updated` event args |
| `ConfigurationManagerNotEnabledException.cs` | No "enabled" concept in MEC |
| `ConfigurationPropertyAttribute.cs` | Attribute already unused on production properties |
| `ConfigProperty.cs` (~28 KB) | Reflection-discovered config property descriptor |
| `ConfigPropertyHostTypes.cs` | `[DynamicDependency]` rooting of 29 host types |
| `ConfigLocations.cs` | Replaced by MEC provider ordering in `TuiConfigurationExtensions` |
| `DeepCloner.cs` (~19 KB) | Reflection-based deep clone; not needed once POCOs replace `Scope<T>` |
| `Scope.cs`, `SettingsScope.cs`, `AppSettingsScope.cs`, `ThemeScope.cs` | Scope abstractions superseded by typed `IOptions<T>` |
| `ScopeJsonConverter.cs` | Flat-key scope deserializer |
| `DictionaryJsonConverter.cs`, `ConcurrentDictionaryJsonConverter.cs` | Only needed by the flat-key scope format |
| `KeyArrayJsonConverter.cs`, `KeyCodeJsonConverter.cs`, `MouseFlagsArrayJsonConverter.cs` | Audit — only delete if unused after migration; otherwise keep and register on the MEC-side `JsonSerializerOptions` |

### 4.2 Types to **keep** (already MEC-shaped) and harden

| File | Action |
|------|--------|
| `TuiSerializerContext.cs` | **Already exists post-#5411.** No change. This is the canonical JSON context for the MEC era. |
| `SourceGenerationContext.cs` | Keep. Re-audit `[JsonSerializable]` entries — remove any that reference `SettingsScope` / `ThemeScope` / `AppSettingsScope`. Add `ThemeSettings` and the per-component settings POCOs. |
| `ThemeChanges.cs` | **Already exists post-#5411.** Keep as the supported observer facade. In this PR: remove the `ConfigurationManager.Applied` bridge inside it (one branch of an `OR`) once `ConfigurationManager` is deleted. |
| `AttributeJsonConverter.cs`, `SchemeJsonConverter.cs`, `RuneJsonConverter.cs`, `KeyJsonConverter.cs`, `ColorJsonConverter.cs`, `TraceCategoryJsonConverter.cs`, `DictionaryJsonConverter.cs`, `ConcurrentDictionaryJsonConverter.cs` | Keep. **Already point at `TuiSerializerContext.Instance` post-#5411.** No further work. |
| `Settings/*Settings.cs` (all 30+) | Keep. These are the POCOs that became the source of truth in #5411. **Pattern divergence (A2.1+):** ThemeScope POCOs (the 17/18 bind targets of `BindThemeScope<T>`) are immutable `sealed record` + `Default`/`Current` with `Volatile`-swapped atomic publish; SettingsScope POCOs remain mutable `Defaults`. Rationale: only ThemeScope participates in theme overlay merge; SettingsScope is bound once at app start and never per-theme. |
| `Settings/TuiConfigurationBuilder.cs` | Keep. Becomes the **only** configuration entry point. |
| `Settings/TuiConfigurationExtensions.cs` | Keep. |
| `Settings/IThemeManager.cs`, `Settings/ISchemeManager.cs`, `Settings/MecThemeManager.cs`, `Settings/MecSchemeManager.cs` | Keep. **A1 (event plumbing) is done post-#5411.** A2 (data ownership) is this PR's work — see §5.2. |

### 4.3 Types to **rename** (drop the `Mec` prefix once the legacy peers are gone)

| Old (post‑#5411) | New | Outcome |
|------------------|-----|---------|
| `MecSchemeManager` | `SchemeManager` | **Kept.** Static `SchemeManager` was retained as a MEC-backed facade (`GetScheme`, `AddScheme`). Instance `ISchemeManager.GetScheme` cannot share that name. |
| `MecThemeManager` | `ThemeManager` | **Kept.** Static `ThemeManager` was retained as a MEC-backed facade (`ThemeManager.Theme =`). A static `ThemeChanged` event cannot coexist with the instance event on `IThemeManager`. |
| `Tests/UnitTestsParallelizable/Configuration/MecSettingsTests.cs` | `SettingsTests.cs` | Renamed |
| `Tests/UnitTestsParallelizable/Configuration/MecThemeTests.cs` | `ThemeTests.cs` | Renamed |
| `Tests/UnitTestsParallelizable/Configuration/MecAppSettingsTests.cs` | `AppSettingsTests.cs` | Renamed |
| `Tests/UnitTestsParallelizable/Configuration/MecDottedKeyTests.cs` | `DottedKeyTests.cs` | Renamed |

### 4.4 Callers to **rewire** (production source outside `Configuration/`)

| File | Current dependency | Replacement |
|------|--------------------|-------------|
| `ModuleInitializers.cs` | Calls `ConfigurationManager.Initialize ()` then `mecBuilder.ApplyToStaticFacades ()` | Single call to `TuiConfigurationBuilder.ApplyToStaticFacades ()`. Delete the dual-init suppression. Gated by A2 completion. |
| `App/ApplicationImpl.Lifecycle.cs` | `ConfigurationManager.PrintJsonErrors ()` | Replace with an equivalent aggregated-error printer fed by `JsonConfigurationSource.OnLoadException`. See §5.4 and §6 Phase D. |
| `App/Application.cs` | Several `ConfigurationManager.*` references in xmldoc/method bodies | Replace with `TuiConfigurationBuilder` references or delete. |
| `App/Tracing/Trace.cs` | One CM reference | Replace with `TraceSettings.Defaults.EnabledCategories`. |
| `Drawing/Scheme.cs` | xmldoc references CM in a comment | Update wording. |
| `Drawing/Glyphs.cs` | (None — already POCO-backed via `GlyphSettings.Defaults`.) | No change. Confirm via grep after removal. |
| `Input/CommandBindingsBase.cs` | Comment refers to `ConfigurationManager.Apply` / `DeepMemberWiseCopy` | Update comment; the dictionary‑copy workaround stays because the underlying race is now in `IOptionsMonitor` callbacks. |
| Stale `using Terminal.Gui.Configuration;` / xmldoc references in: `Views/Menu/PopoverMenu.cs`, `Views/Selectors/SelectorBase.cs`, `Views/Window.cs`, `Views/Dialog.cs`, `Views/FrameView.cs`, `Views/MessageBox.cs`, `Views/HexView.cs`, `Views/CharMap/CharMap.cs`, `Views/CheckBox.cs`, `Views/Button.cs`, `Views/FileDialogs/FileDialog.cs`, `Views/FileDialogs/FileDialogStyle.cs`, `Views/LinearRange/LinearRangeDefaults.cs`, `Views/LinearRange/LinearRangeViewBase.cs`, `Views/TextInput/TextField/TextField.cs`, `Views/TextInput/TextView/TextView.cs`, `Views/TreeView/TreeViewT.cs`, `ViewBase/View.Keyboard.cs`, `ViewBase/Mouse/View.Mouse.cs`, `ViewBase/Adornment/BorderView.Arrangement.cs`, `Text/NerdFonts.cs`, `FileServices/FileSystemIconProvider.cs`, `Drawing/Color/Color.cs`, `Input/Keyboard/Key.cs`, `Drivers/Driver.cs`, `App/Legacy/Application.Mouse.cs` | No live code dependency (verified — `[ConfigurationProperty(` grep returns only the attribute file itself). | Mechanical comment sweep. |

**Already rewired in #5411 (no action in this PR):** `Views/Menu/Menu.cs`, `Views/Menu/MenuBar.cs`, `Views/StatusBar.cs`, `Drawing/LineCanvas/LineCanvas.cs` all subscribe to `ThemeChanges.ThemeChanged` instead of `ConfigurationManager.Applied`.

### 4.5 Resource files

| File | Action |
|------|--------|
| `Terminal.Gui/Resources/config.json` (52 KB, ~1,500 lines) | **Rewrite** in nested MEC format. Keys like `"Button.DefaultShadow"` become `Button: { DefaultShadow: ... }`. The `"Themes": [ { "Default": { ... } } ]` array-of-single-key-objects becomes `Themes: { Default: { ... }, Dark: { ... } }`. Schemes stay as a nested map. The `$schema` URL stays; the hosted schema must be updated in tandem (Q‑03 — file a tracking issue). |
| `Examples/Config/example_config.json` | Migrate alongside library `config.json`. |
| `Tests/.../*config.json` (if any) | Audit and migrate. |

### 4.6 Tests to **delete** (`Tests/`)

| File | Reason |
|------|--------|
| `UnitTests.NonParallelizable/Configuration/ConfigurationMangerTests.cs` | Tests deleted facade. |
| `UnitTests.NonParallelizable/Configuration/ConfigPropertyAssemblyScanTests.cs` | Tests deleted reflection scan. |
| `UnitTests.NonParallelizable/Configuration/SourcesManagerLoadNullJsonTests.cs` | Replaced by `SourcesManagerTests` in `UnitTestsParallelizable` (which already test the MEC builder). |
| `UnitTests.NonParallelizable/Configuration/GlyphTests.cs` | Move to parallelizable if it doesn't depend on static state; otherwise replace with MEC equivalent. |
| `UnitTestsParallelizable/Configuration/ConfigPropertyTests.cs` | Tests deleted type. |
| `UnitTestsParallelizable/Configuration/ConfigPropertyHostTypesTests.cs` | Tests deleted type. |
| `UnitTestsParallelizable/Configuration/ConfigurationPropertyAttributeTests.cs` | Tests deleted attribute. |
| `UnitTestsParallelizable/Configuration/DeepClonerTests.cs` | Tests deleted utility. |
| `UnitTestsParallelizable/Configuration/ScopeJsonConverterTests.cs` | Tests deleted converter. |
| `UnitTestsParallelizable/Configuration/ScopeTests.cs` | Tests deleted base class. |
| `UnitTestsParallelizable/Configuration/SettingsScopeTests.cs` | Tests deleted scope. |
| `UnitTestsParallelizable/Configuration/ThemeScopeTests.cs` | Tests deleted scope. |
| `Benchmarks/Configuration/ConfigurationManagerLoadBenchmark.cs` | Replace with `TuiConfigurationBuilderBuildBenchmark`. |
| `Benchmarks/Configuration/ThemeSwitchBenchmark.cs` | Rewrite against `IThemeManager.SwitchTheme`. |

### 4.7 Tests to **keep** (and audit)

- `UnitTestsParallelizable/Configuration/MecAppSettingsTests.cs`
- `UnitTestsParallelizable/Configuration/MecSettingsTests.cs`
- `UnitTestsParallelizable/Configuration/MecThemeTests.cs`
- `UnitTestsParallelizable/Configuration/SourcesManagerTests.cs` (MEC version — confirm not the legacy one)
- `UnitTestsParallelizable/Configuration/KeyJsonConverterTests.cs`
- `UnitTestsParallelizable/Configuration/RuneJsonConverterTests.cs`
- `UnitTestsParallelizable/Configuration/SchemeJsonConverterTests.cs`
- All `*DefaultKeyBindingsTests.cs` (these read `View.DefaultKeyBindings`; after CM removal they read it via the bound POCO)

### 4.8 Examples to **rewrite**

| File | Action |
|------|--------|
| `Examples/UICatalog/Runner.cs` | Replace `ConfigurationManager.RuntimeConfig = "..."` with `TuiConfigurationBuilder.Runtime(...)` |
| `Examples/UICatalog/UICatalog.cs` and `UICatalogRunnable.cs` | Replace `ConfigurationManager.Enable/Apply/Applied` with the new builder + `IOptionsMonitor<ThemeSettings>.OnChange` |
| `Examples/UICatalog/Scenarios/ConfigurationEditor.cs` | Heavy CM user. Rewrite to enumerate the `Settings/*Settings.cs` POCOs and serialize via `SourceGenerationContext`. |
| `Examples/UICatalog/Scenarios/Themes.cs`, `ThemeFallback.cs` | Use `IThemeManager.ThemeChanged` and `IThemeManager.SwitchTheme`. |
| `Examples/NativeAot/Program.cs`, `Examples/SelfContained/Program.cs`, `Examples/ReactiveExample/Program.cs`, `Examples/CommunityToolkitExample/Program.cs`, `Examples/ScenarioRunner/Program.cs`, `Examples/ShortcutTest/ShortcutTest.cs`, `Examples/PromptExample/Program.cs`, `Examples/Example/Example.cs` | Drop `ConfigurationManager.Enable (...)`; replace with `new TuiConfigurationBuilder ().ApplyToStaticFacades ()` (or remove entirely if defaults are sufficient). |
| `Examples/Config/README.md`, `Examples/NativeAot/README.md`, `Examples/SelfContained/README.md`, `Examples/ReactiveExample/README.md`, `Examples/CommunityToolkitExample/README.md` | Update documentation to reference MEC. |
| All `Examples/UICatalog/Scenarios/*.cs` files listed in §4 of the discovery output | Most contain only stale `using Terminal.Gui.Configuration;` plus an occasional `ConfigurationManager.Applied` subscription. Sweep with a single mechanical pass. |

---

## 5. Detailed Design

### 5.1 New `IOptionsMonitor<ThemeSettings>` flow

```text
TuiConfigurationBuilder.Build()
  └─ IConfigurationBuilder
       ├─ AddTuiLibraryDefaults()       (embedded Terminal.Gui.Resources.config.json)
       ├─ AddTuiAppDefaults(appName)    (entry assembly config.json)
       ├─ AddTuiUserFiles(appName)      (~/.tui/*.json, ./.tui/*.json)
       ├─ AddTuiEnvironmentVariable()   (TUI_CONFIG)
       └─ AddTuiRuntimeConfig(json)     (in-memory string)
  └─ IConfiguration root
  └─ services.Configure<ThemeSettings>(root.GetSection("Themes"))
  └─ services.Configure<ApplicationSettings>(root.GetSection("Application"))
  └─ services.Configure<ButtonSettings>(root.GetSection("Button"))
  └─ ... one Configure<T> per POCO

TuiConfigurationBuilder.ApplyToStaticFacades()
  └─ ButtonSettings.Current  = bound ButtonSettings (root + Themes:{active}:Button overlay)
  └─ DialogSettings.Current  = bound DialogSettings
  └─ GlyphSettings.Current   = bound GlyphSettings
  └─ ... per ThemeScope POCO
  └─ ApplicationSettings.Defaults / DriverSettings.Defaults / ... for SettingsScope POCOs
```

`ButtonSettings.Current` (and peers) are the **static facade** used by all views per D‑01. The compile-time `Default` snapshot is immutable; `Current` is swapped atomically. The only writer is `TuiConfigurationBuilder.ApplyToStaticFacades` — never `ConfigurationManager.Apply`.

### 5.2 ThemeManager / SchemeManager: from event-bridging wrapper to data owner (A2)

Post-#5411 status (**A1 — done**): `IThemeManager.ThemeChanged` exists; `MecThemeManager` subscribes to the legacy static `ThemeManager.ThemeChanged` in its constructor and forwards. `ThemeChanges.ThemeChanged` is the public observer facade used by internal views; it bridges both `ConfigurationManager.Applied` *and* `IThemeManager.ThemeChanged` because `CM.Apply()` writes `ConfigProperty` values directly and bypasses the C# setter, so `ThemeChanged` alone would miss most theme switches today.

This PR's work (**A2**): make `MecThemeManager` / `MecSchemeManager` the **owners** of theme and scheme runtime data, so the `ConfigurationManager.Applied` half of the `ThemeChanges` bridge can be deleted along with `ConfigurationManager` itself.

Today the runtime theme/scheme dictionary lives in `ConfigurationManager.Settings["Themes"]`, parsed by `ScopeJsonConverter`. The target shape:

```csharp
public sealed class ThemeManager : IThemeManager
{
    private readonly IOptionsMonitor<ThemeSettings> _monitor;
    private IDisposable? _subscription;

    public ThemeManager (IOptionsMonitor<ThemeSettings> monitor)
    {
        _monitor = monitor;
        _subscription = _monitor.OnChange (HandleChanged);
    }

    public string ActiveTheme
    {
        get => _monitor.CurrentValue.ActiveTheme;
        set => SwitchTheme (value);
    }

    public IReadOnlyList<string> ThemeNames => _monitor.CurrentValue.Themes.Keys.ToImmutableList ();

    public event EventHandler<string>? ThemeChanged;

    public void SwitchTheme (string name) { /* mutate in-memory provider + reload */ }

    private void HandleChanged (ThemeSettings settings, string? name)
        => ThemeChanged?.Invoke (this, settings.ActiveTheme);
}
```

A2 is the **gating prerequisite** for deleting `ScopeJsonConverter` and is tied to the D-02 resource-shape decision: the MEC binder cannot bind `ThemeSettings` from the legacy flat-key `Themes: [ { Name: { "Class.Prop": ... } } ]` shape without a custom source. Either:

- **Option α (recommended):** Rewrite `Terminal.Gui/Resources/config.json` to the MEC-native nested shape, ship `TuiConfigMigrator` for user files. `ScopeJsonConverter` deletes cleanly.
- **Option β:** Keep the legacy shape and write a `LegacyTuiConfigurationSource : IConfigurationSource` that re-emits flat keys as nested MEC keys at load time. Smaller user blast radius; larger ongoing maintenance.

§5.4 carries the analysis.

`SchemeManager` becomes an instance class fronting `ThemeSettings.Schemes`. The static `SchemeManager.GetScheme(string)` API used pervasively in views is kept as a **static convenience facade** that forwards to the registered service (via a static `Application.Services` accessor) or to a process-wide fallback when no app has been created — same shape as the `XxxSettings.Defaults` pattern adopted in #5411.

### 5.3 `ThemeChanges` bridge cleanup

`ThemeChanges` currently raises its event in response to **either**:

1. `ConfigurationManager.Applied` (legacy CM apply path), or
2. `IThemeManager.ThemeChanged` (MEC path, post‑A1).

After A2 lands, (1) is dead — every theme switch goes through `MecThemeManager.SwitchTheme` ⇒ `IOptionsMonitor<ThemeSettings>.OnChange` ⇒ `IThemeManager.ThemeChanged` ⇒ `ThemeChanges.ThemeChanged`. The `ConfigurationManager.Applied` subscription inside `ThemeChanges` deletes alongside `ConfigurationManager`.

No view-side changes are required — `ThemeChanges` is the public surface and stays.

### 5.4 Library `config.json`: resolving D-02

**Resolution: α-lite (detect + warn, no library-side migration).**

The MEC read path is pure-nested. `TuiConfigurationBuilder.AddTuiJsonFile` (and the equivalent extension overloads) peek the JSON before binding and, if they detect pre-MEC shapes — top-level keys containing `.`, or `Themes` as a JSON array — emit a single `WARN`-level log identifying the file path and pointing at the migration documentation URL. The legacy shape is **not** parsed; affected settings fall through to defaults.

**Rationale.** v2 is GA, so silent breakage (γ) is inappropriate. But hand-edited `config.json` usage is rare and concentrated in app authors who control their own upgrade timing, so a perpetual translation layer (β `LegacyTuiConfigurationSource`) or a full bidirectional migrator (α `TuiConfigMigrator`) buys little and costs permanent library surface. α-lite is the minimal GA-appropriate response: ~20 LOC of detection + one log line — no parsing of the legacy shape, no auto-migration, no round-trip tests, no deprecation cycle to engineer.

**Library resource rewrite (one-time, in this PR).** `Terminal.Gui/Resources/config.json` is regenerated in the nested MEC-native shape:

Before (flat):
```json
{
  "Themes": [
    {
      "Default": {
        "Button.DefaultShadow": "Opaque",
        "Glyphs.CheckStateChecked": "☑",
        "Schemes": [ { "Base": { "Normal": { "Foreground": "White", "Background": "Black" } } } ]
      }
    }
  ]
}
```

After (nested, MEC-native):
```json
{
  "Themes": {
    "Default": {
      "Button":  { "DefaultShadow": "Opaque" },
      "Glyphs":  { "CheckStateChecked": "☑" },
      "Schemes": { "Base": { "Normal": { "Foreground": "White", "Background": "Black" } } }
    }
  }
}
```

`Examples/Config/example_config.json` and every UICatalog/example config file gets the same treatment.

**Detection heuristic (implementation note).** Single `JsonDocument` walk, no schema knowledge required:

- *Flat key* = any top-level property name in the root object whose name contains `.`.
- *Array themes* = the `Themes` property exists and its `ValueKind == JsonValueKind.Array`.

Both checks together: < 20 LOC, no allocation beyond the `JsonDocument`. The warn message names the file and links to `docfx/docs/migrate-cm-to-mec.md` (and the migration tool below).

**Migration aid.** A standalone `Tools/MigrateConfig/` console app (~50 LOC) lives in the repo but is **not** shipped in `Terminal.Gui.dll` and **not** added to any solution that ships. The detection warning references it. The tool is deletable any time without a deprecation cycle.

**Cleanup horizon.** The detection-and-warn code itself can be deleted in a future minor when issue volume indicates no remaining users hit it. It is small enough to keep indefinitely if preferred — the decision is non-coupled to anything in this PR.

**Explicitly rejected alternatives** (recorded so reviewers don't re-litigate):

- ❌ *"Keep both shapes forever, just document both."* Combines β's permanent maintenance cost with α's user confusion cost. No upside.
- ❌ *"Silently translate legacy shapes."* Opaque failure mode if translation is wrong; GA-inappropriate.
- ❌ *"Throw on legacy shape."* Too aggressive for a non-malformed JSON file. Warn-and-ignore is the right contract — the file is still valid JSON, it just no longer matches our schema.

### 5.5 Source generation context cleanup

`TuiSerializerContext` (created in #5411) already configures all custom converters and `JsonSerializerOptions`. Remaining work in this PR:

- Audit `SourceGenerationContext`'s `[JsonSerializable]` entries. Remove anything that references `SettingsScope` / `ThemeScope` / `AppSettingsScope` (those types are being deleted).
- Ensure every Settings POCO actively bound by `TuiConfigurationBuilder` is registered:

```csharp
[JsonSerializable (typeof (ThemeSettings))]
[JsonSerializable (typeof (ApplicationSettings))]
[JsonSerializable (typeof (ButtonSettings))]
[JsonSerializable (typeof (DialogSettings))]
[JsonSerializable (typeof (GlyphSettings))]
// ... one entry per Settings/*Settings.cs POCO
[JsonSerializable (typeof (Scheme))]
[JsonSerializable (typeof (Attribute))]
[JsonSerializable (typeof (Color))]
[JsonSerializable (typeof (Dictionary<string, Scheme>))]
[JsonSerializable (typeof (Dictionary<string, ThemeDefinition>))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
```

The converter registration is owned by `TuiSerializerContext` and does not move.

### 5.6 ModuleInitializer simplification

After A2 + the legacy CM deletion:

```csharp
[ModuleInitializer]
internal static void InitializeTuiConfiguration ()
{
    TuiConfigurationBuilder builder = new ();
    builder.ApplyToStaticFacades ();
}
```

Single call. No `ConfigurationManager.Initialize ()`, no `#pragma warning disable CS0618`.

### 5.7 `PrintJsonErrors` replacement (behavior-preserving)

The v2 contract is "don't fail-fast on bad config.json; collect errors and print at shutdown." MEC supports this via `JsonConfigurationSource.OnLoadException`:

```csharp
builder.AddJsonStream (stream, source =>
{
    source.OnLoadException = ctx =>
    {
        TuiJsonErrors.Add ($"{ctx.Source}: {ctx.Exception.Message}");
        ctx.Ignored = true;
    };
});
```

Wire the same hook on every JSON source registered by `TuiConfigurationExtensions`. `ApplicationImpl.Lifecycle.Shutdown` calls `TuiJsonErrors.Print ()` (or routes through `Logging`) in place of the current `ConfigurationManager.PrintJsonErrors ()` call.

Caveat: `OnLoadException` covers file/parse errors only. Bind/POCO validation errors (`OptionsValidationException`) don't have an equivalent hook in MEC and will throw on first `IOptions<T>.Value` access. This is acceptable — bind errors are programmer-level (POCO shape mismatch), not user-level (typo in JSON).

---

## 6. Implementation Phases (within this PR)

Each phase is one or more commits. Tests must build and pass at every commit. **Phases A1, B, and the JSON-converter half of C landed in #5411 and are not repeated here.**

### Phase D — Library `config.json` rewrite + α-lite detection *(blocks Phase A2)*
Scope (per §5.4 resolution):
1. Rewrite `Terminal.Gui/Resources/config.json` in the nested MEC-native shape.
2. Add the ~20 LOC peek-and-warn to `TuiConfigurationBuilder.AddTuiJsonFile` (and any sibling overloads). Detection heuristic: flat top-level keys containing `.`, or `Themes` as a JSON array.
3. Delete `Terminal.Gui/Configuration/ScopeJsonConverter.cs` (with the legacy parser gone, nothing reads the legacy shape inside `Terminal.Gui.dll`).
4. Rewrite `Examples/Config/example_config.json` and every UICatalog / example config to nested.
5. Add `Tools/MigrateConfig/` — standalone console app (~50 LOC), separate csproj, not in any shipping solution.
6. Add CHANGELOG entry pointing at the tool and the migration guide URL.
7. Wire `JsonConfigurationSource.OnLoadException` per §5.7 so v2 deferred-error behavior is preserved.
8. File a follow-up issue to update the hosted JSON schema (Q-03).

Tests for Phase D: **two** parallelizable tests — one asserts the warning fires on a flat-key sample, one asserts it fires on an array-themes sample. No translation logic to test, so no round-trip tests are required.

### Phase A2 — Mec managers own runtime theme/scheme data
- Have `MecThemeManager` / `MecSchemeManager` read theme and scheme dictionaries from `IOptionsMonitor<ThemeSettings>.CurrentValue` instead of delegating to the legacy static `ThemeManager` / `SchemeManager`.
- `SwitchTheme` mutates the underlying in-memory MEC source and triggers `IOptionsMonitor.OnChange`.
- Confirm `ThemeChanges.ThemeChanged` is now raised exclusively through the `IThemeManager.ThemeChanged` path; verify no `ConfigurationManager.Applied` round-trip happens at runtime.
- Add tests in `MecThemeTests` for switch / add / remove scheme paths that previously delegated to the legacy code.

### Phase C-finish — `SourceGenerationContext` POCO audit
- Remove `SettingsScope` / `ThemeScope` / `AppSettingsScope` from `[JsonSerializable]` list.
- Add per-component POCOs and `Dictionary<string, Scheme>` / `Dictionary<string, ThemeDefinition>` per §5.5.
- Delete the `ConfigurationManager.SerializerContext` one-line delegator field (kept post-#5411 purely to avoid an obsolete-attr breaking change in the parent PR).

### Phase E — Delete legacy CM types + tests
- Delete every file listed in §4.1 and every test listed in §4.6.
- Delete `ModuleInitializers.cs`'s `ConfigurationManager.Initialize ()` call (§5.6).
- Replace `ApplicationImpl.Lifecycle`'s `ConfigurationManager.PrintJsonErrors ()` call with `TuiJsonErrors.Print ()` per §5.7.
- Delete the `ConfigurationManager.Applied` branch inside `ThemeChanges` per §5.3.
- Sweep stale doc-comment references (§4.4 last row).

### Phase F — Rename `MecThemeManager`/`MecSchemeManager` → `ThemeManager`/`SchemeManager`
- Test files renamed (`SettingsTests`, `ThemeTests`, `AppSettingsTests`, `DottedKeyTests`).
- Production `MecThemeManager` / `MecSchemeManager` **keep the `Mec` prefix**: static `ThemeManager` / `SchemeManager` facades were retained for `ThemeManager.Theme =` and `SchemeManager.GetScheme` callers. C# cannot host both the static facade members and the `IThemeManager` / `ISchemeManager` instance members on one type (`ThemeChanged` event and `GetScheme` collide). The instance types stay the `IThemeManager` / `ISchemeManager` implementations returned by `TuiConfigurationBuilder`.

### Phase G — Update examples (Runner, UICatalog, NativeAot, etc.)
- §4.8 sweep.

### Phase H — Update `docfx/docs/config.md` + migration guide
- Rewrite `config.md` to reflect the MEC contract.
- Remove every reference to `ConfigurationManager.Enable`, `ConfigLocations`, `ConfigurationProperty`, `SettingsScope`, `ThemeScope`, `AppSettingsScope`.
- Document `TuiConfigurationBuilder`, `IThemeManager`, `ISchemeManager`, the nested JSON schema, the migration helper, `ThemeChanges`, `TuiSerializerContext`.
- Add `docfx/docs/migrate-cm-to-mec.md` cheatsheet.

### Phase I — Verification
- Run full test matrix (`UnitTestsParallelizable`, `UnitTests.NonParallelizable`, `IntegrationTests`).
- Confirm zero new warnings (the `CS0618` suppressions are gone — if anything tries to use a deleted obsolete API the build fails by design).
- Build `Tests/NativeAotSmoke` with `PublishAot=true` and record `before` / `after` binary size in the PR description.
- Run `Benchmarks/Configuration/*` and record delta.

#### Phase I results

| Check | Result |
|-------|--------|
| `Tests/NativeAotSmoke` `PublishAot=true` `win-arm64` | develop `24,640,512` (23.50 MB) → this PR `24,201,728` (23.08 MB), **−438,784 (−1.78%)** |
| Dotted RuntimeConfig overlay | `BindFlatDottedKeys` always runs after nested `Bind`, so later dotted keys win over library nested sections |
| Test matrix | CI on PR #5416 (parallel, non-parallel, integration) |
| Benchmarks | `TuiConfigurationBuilderBuildBenchmark` replaces `ConfigurationManagerLoadBenchmark`; `ThemeSwitchBenchmark` uses `ThemeManager.Theme =` |

---

## 7. Breaking Changes (Public API)

The following public surface is **removed**. Source-incompatible for any consumer that touched these (the predecessor #5411 marked them all `[Obsolete]`).

- `Terminal.Gui.Configuration.ConfigurationManager` (class)
- `Terminal.Gui.Configuration.ConfigurationManagerNotEnabledException`
- `Terminal.Gui.Configuration.ConfigurationPropertyAttribute`
- `Terminal.Gui.Configuration.ConfigLocations`
- `Terminal.Gui.Configuration.SettingsScope`, `ThemeScope`, `AppSettingsScope`, `Scope<T>`
- `Terminal.Gui.Configuration.ConfigProperty`
- `Terminal.Gui.Configuration.ConfigPropertyHostTypes`
- `Terminal.Gui.Configuration.SourcesManager` (replaced by `TuiConfigurationBuilder`)
- `Terminal.Gui.Configuration.DeepCloner`
- `Terminal.Gui.Configuration.ScopeJsonConverter<T>`
- The `Terminal.Gui.Configuration.ConfigurationManager.Applied` / `Updated` events

Static `ThemeManager` and `SchemeManager` remain as MEC-backed facades (`ThemeManager.Theme =`, `SchemeManager.GetScheme`). Instance APIs are `IThemeManager` / `ISchemeManager` (`MecThemeManager` / `MecSchemeManager`).

### JSON file breaking change

The library `Resources/config.json` is nested MEC. User files and `RuntimeConfig` in the legacy flat-key format (`"Button.DefaultShadow": "..."`) or array-themes format still bind (dotted keys overlay nested library sections) but log a `WARN` pointing at the migration documentation and `Tools/MigrateConfig/`. No library-side auto-migration of on-disk files ships. The hosted JSON schema update is follow-up [#5631](https://github.com/tui-cs/Terminal.Gui/issues/5631).

### Migration guide

A separate `docfx/docs/migrate-cm-to-mec.md` is added in Phase H with a side-by-side cheatsheet. The PR description links to it.

---

## 8. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Hidden third-party consumers still call obsolete CM API | They received an `[Obsolete]` warning in #5411 with the message pointing at `TuiConfigurationBuilder`. One release window has elapsed by the time this PR ships. |
| Nested JSON breaks existing user config files | α-lite (§5.4): detect-and-warn at load time, fall through to defaults rather than fail. Standalone `Tools/MigrateConfig/` console app + `migrate-cm-to-mec.md` guide give users a one-shot fix path. If issue volume after release indicates more help is needed, β (`LegacyTuiConfigurationSource`) remains a viable hotfix. |
| Renaming `MecThemeManager` → `ThemeManager` clashes with the deleted legacy `ThemeManager` if a partial revert lands | Phase F runs only after Phase E commits; if reverted, the rename is reverted with it. |
| AOT size delta is smaller than predicted | Acceptable. The parallel-test and decoupling wins still justify the change. Record actual delta in the PR. |
| `IOptionsMonitor<T>.OnChange` is not invoked in NativeAOT due to missing trim roots | Verified in #5411. Re-verify with a smoke test in `Examples/NativeAot` as part of Phase I. |
| Bind-time (`OptionsValidationException`) errors are not aggregated like file-parse errors | Accepted. Bind errors are programmer-level (POCO shape mismatch). The user-level "typo in JSON" path is fully preserved via `JsonConfigurationSource.OnLoadException` (§5.7). |

---

## 9. Out of Scope

Carried forward from the predecessor spec (§10), unchanged:

1. Generic Host / `IHostBuilder` adoption.
2. Constructor injection mandate for all Views.
3. View-level DI.
4. Multiple concurrent `IApplication` instances.
5. TextMate / Markdig conditional compilation.
6. Lazy driver registration.
7. Assembly splitting.

---

*Authors: @copilot. Companion to [`specs/replace-cm-with-mec.md`](./replace-cm-with-mec.md). Tracking PR: #5416.*

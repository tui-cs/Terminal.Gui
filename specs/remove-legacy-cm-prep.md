# CM Removal — Pre-Work Inventories & Baselines

> **Status:** Historical inventory used while implementing #5416. Not the live configuration contract.

Companion artifact for [`remove-legacy-cm.md`](./remove-legacy-cm.md).
Captures the empirical "before" state used by phases **G** (examples sweep),
**E** (test deletion), and **I** (AOT measurement) so the eventual delta
claim is reproducible.

Generated against base SHA `83ded73aa` (`origin/copilot/replace-cm-with-mec`,
post Phase A1/B/C of #5411). My branch is rebased onto this commit; spec
commit is `f122ebe6a`.

---

## 1. AOT Size Baseline (Phase I)

Command: `dotnet publish Examples/NativeAot/NativeAot.csproj -c Release -r win-x64`
Platform: Windows x64, .NET 10 SDK, clean `bin/obj` before publish.

| Artifact                       | Bytes        | MB     |
|--------------------------------|--------------|--------|
| `NativeAot.exe` (AOT, single)  | 23,873,536   | 22.77  |
| `Terminal.Gui.dll` (Release)   |  1,854,464   |  1.77  |
| Publish dir total              | 96,319,509   | 91.86  |

### Reflection-rooting metric

`Terminal.Gui/Configuration/ConfigPropertyHostTypes.cs` currently roots
**31** types via `[DynamicDependency]`. Deleting this file in Phase E
should drop those 31 root anchors and let the AOT trimmer collect their
unused members. This is the headline number for the "AOT size reduction"
claim in #5416's PR description.

### Reproducing this number after Phase E

```powershell
Remove-Item Examples/NativeAot/bin, Examples/NativeAot/obj -Recurse -Force -EA 0
Remove-Item Terminal.Gui/bin, Terminal.Gui/obj -Recurse -Force -EA 0
dotnet publish Examples/NativeAot/NativeAot.csproj -c Release -r win-x64
Get-Item Examples/NativeAot/bin/Release/net10.0/win-x64/publish/NativeAot.exe |
  Select-Object Name, Length
```

Record the new `NativeAot.exe` byte count and the delta vs. 23,873,536.
A meaningful win should be ≥ 1 MB given the 31 rooted types include
view types with large transitive surfaces (`MenuBar`, `StatusBar`,
`TableView`, `TreeView`, `Dialog`, etc.).

---

## 2. Examples Inventory (Phase G)

Total `Examples/` files touching CM: **~115 source/doc files**, but
**99% are the same one-line incantation** — the actual rewrite surface
is small.

### 2.1 Dominant pattern (~105 occurrences, mostly UICatalog scenarios)

Single call in scenario constructor / `Main`:

```csharp
ConfigurationManager.Enable (ConfigLocations.All);
```

**Replacement:** Replace with MEC equivalent (likely
`Application.Create ().UseTuiConfiguration ().Init ()` or whatever the
final builder shape settles on — see spec §5.6).

Files using only this pattern (mechanical sed, no review needed):
all `Examples/UICatalog/Scenarios/*.cs` except the four below.

### 2.2 Non-trivial scenarios needing review

| File | Lines | What it does | Replacement notes |
|------|-------|--------------|-------------------|
| `Examples/UICatalog/UICatalogRunnable.cs` | 30, 131, 203, 715 | Subscribes to `ConfigurationManager.Applied`; reads `IsEnabled`; uses `[ConfigurationProperty (Scope = typeof (AppSettingsScope), OmitClassName = true)]` on a settings field | Subscribe to `ThemeChanges.ThemeChanged` (already exists post-#5411); move `[ConfigurationProperty]` to a POCO bound via `IOptionsMonitor<UICatalogAppSettings>` |
| `Examples/UICatalog/Runner.cs` | 14, 39, 373, 378, 379 | Sets `RuntimeConfig` JSON string for driver overrides; explicit `Load + Apply`; bridges `ThemeManager.ThemeChanged → CM.Apply` | Build an in-memory `IConfigurationSource` for the driver overrides; drop the bridge (MEC reload handles it) |
| `Examples/UICatalog/Scenarios/ConfigurationEditor.cs` | 23, 51, 75, 222, 227, 232, 247 | The CM editor scenario itself — enumerates `SourcesManager.Sources`, edits `RuntimeConfig`, calls `GetHardCodedConfig`/`GetEmptyConfig` | Rewrite to walk `IConfigurationRoot.Providers` and edit a designated `IConfigurationSource`. **This scenario is the heaviest item in Phase G.** |
| `Examples/UICatalog/Scenarios/Themes.cs` | 89 | `ConfigurationManager.Apply ()` after a theme switch | Drop the call (MEC reload-on-change) |
| `Examples/UICatalog/Scenarios/ThemeFallback.cs` | 73 | Same pattern | Drop the call |
| `Examples/UICatalog/Scenarios/CodeViewDemo.cs` | 153 | Same pattern | Drop the call |
| `Examples/UICatalog/Scenarios/TextInputControls.cs` | 384 | `ConfigurationManager.Applied += ...` | Subscribe to `ThemeChanges.ThemeChanged` |
| `Examples/UICatalog/Scenarios/Shortcuts.cs` | 345 | Reads `ConfigurationManager.IsEnabled` | Drop the check (MEC is always on or behind a builder opt-in) |
| `Examples/UICatalog/UICatalog.cs` | 296 | `Enable` call at app startup | Replace with builder call |
| `Examples/UICatalog/Scenarios/CharacterMap/CharacterMap.cs` | 57 | `Enable` call | Replace |

### 2.3 Standalone examples (each has one `Enable` call)

`Example.cs`, `SelfContained/Program.cs`, `CommunityToolkitExample/Program.cs`,
`NativeAot/Program.cs`, `ShortcutTest.cs`, `ReactiveExample/Program.cs`,
`PromptExample/Program.cs`, `ScenarioRunner/Program.cs` (2 calls).

`Examples/Example/Example.cs:13` additionally sets `RuntimeConfig`
to a JSON literal — needs the same in-memory-source treatment as
`Runner.cs`.

### 2.4 Docs / READMEs to update (Phase H)

`Examples/Config/README.md`, `Examples/CommunityToolkitExample/README.md`,
`Examples/SelfContained/README.md`, `Examples/ReactiveExample/README.md`,
`Examples/NativeAot/README.md` — all reference the legacy `Enable`
incantation.

`Examples/Config/example_config.json` — uses the legacy flat
`"ConfigurationManager.ThrowOnJsonErrors"` key. Migrate when D-02
lands.

### 2.5 AOT-blocker note

`Examples/NativeAot/README.md` explicitly documents `ConfigurationManager.Initialize`,
`DeepCloner`, `SourceGenerationContext` as the reasons AOT works the way
it does today. After CM removal, this README needs a full rewrite — the
trim warnings should largely disappear.

---

## 3. Test Inventory (Phase E)

Total Configuration tests across all test projects: **25 files**.

### 3.1 DELETE — pure CM/scope/Initialize tests (8 files)

| File | Project | CM refs |
|------|---------|---------|
| `ConfigurationMangerTests.cs` | NonParallelizable | 49 |
| `ConfigPropertyAssemblyScanTests.cs` | NonParallelizable | 9 |
| `SourcesManagerLoadNullJsonTests.cs` | NonParallelizable | 5 |
| `GlyphTests.cs` | NonParallelizable | uses `static CM` import, `RuntimeConfig`, `ThemeManager.GetCurrentTheme()` |
| `SourcesManagerTests.cs` | Parallelizable | 69 |
| `ScopeTests.cs` | Parallelizable | 18 |
| `ScopeJsonConverterTests.cs` | Parallelizable | 5 |
| `SettingsScopeTests.cs` / `ThemeScopeTests.cs` | Parallelizable | 2 / 4 |
| `ConfigPropertyTests.cs` / `ConfigPropertyHostTypesTests.cs` / `ConfigurationPropertyAttributeTests.cs` | Parallelizable | 10 / 10 / 2 |
| `DeepClonerTests.cs` | Parallelizable | 7 |

(That's 13 files counted, not 8 — the 8/13 split depends on whether
glyph behavior gets re-tested against MEC; see §3.3 *port* row.)

### 3.2 KEEP — JSON converter tests still valid against `TuiSerializerContext` (8 files)

These test converter behavior, not CM machinery. The converters
themselves are kept (per spec §4.2) and now wire through
`TuiSerializerContext`.

- `AttributeJsonConverterTests.cs`
- `ColorJsonConverterTests.cs`
- `ConcurrentDictionaryJsonConverterTests.cs`
- `KeyJsonConverterTests.cs`
- `KeyCodeJsonConverterTests.cs`
- `RuneJsonConverterTests.cs`
- `SchemeJsonConverterTests.cs` *(verify it doesn't depend on `ThemeScope` resolution)*
- `MemorySizeEstimator.cs` *(helper, not a test)*

### 3.3 KEEP — already MEC-based (3 files)

- `MecThemeTests.cs`
- `MecSettingsTests.cs`
- `MecAppSettingsTests.cs`
- `SchemeManagerTests.cs` *(verify which manager — likely MEC since legacy `SchemeManager` is `[Obsolete]`)*

### 3.4 PORT — behaviors that should survive as MEC tests

The `GlyphTests` "Apply over defaults" behavior is a real
user-observable contract: when you override a glyph in `config.json`,
`Glyphs.LeftBracket` reflects it. After deletion, add an equivalent
test against the MEC pipeline (load JSON → assert `Glyphs.*`).

Same applies to any `ConfigurationMangerTests` case that asserts
end-to-end behavior (file-load → apply → property-reflects) that isn't
already covered by `MecSettingsTests` / `MecThemeTests`. A line-by-line
audit is required in Phase E — not in this prep doc.

### 3.5 Benchmarks (Phase E)

| File | Action |
|------|--------|
| `Tests/Benchmarks/Configuration/ConfigurationManagerLoadBenchmark.cs` | **Port** to measure `Application.Create().UseTuiConfiguration().Init()` cold-start, or delete if MEC startup is well-characterized elsewhere |
| `Tests/Benchmarks/Configuration/ThemeSwitchBenchmark.cs` | **Port** to measure `IThemeManager.Theme = "X"` (which triggers `ThemeChanges.ThemeChanged`) — this is still the same user-facing operation |

These benchmarks are the only quantitative basis for any "startup is
faster after CM removal" claim, so porting (not deleting) is preferred.

### 3.6 Cross-test-project sanity check

One outlier hit in
`Tests/UnitTestsParallelizable/Input/Keyboard/CommandInsertCaretKeyBindingTests.cs`
matched the broad regex but contains no actual CM references — false
positive (likely `Settings` substring). No action.

---

## 4. What This Prep Did NOT Touch

- D-02 (`config.json` shape decision) — still gated on source-session input.
- `TuiConfigMigrator` design — blocked on D-02.
- Migration guide draft — deferred until D-02 resolves so the user-facing
  examples are accurate.
- Phase A2 (Mec managers own theme/scheme data) — owned by the source
  session per the last cross-session sync.

---

## 5. Next Executable Step (now unblocked)

D-02 resolved to **α-lite** (detect + warn; no library-side migration).
See `remove-legacy-cm.md` §5.4 and Phase D for the full scope. Concretely:

1. Rewrite `Terminal.Gui/Resources/config.json` once (flat → nested,
   array-themes → dict-themes).
2. Add ~20 LOC peek-and-warn to `TuiConfigurationBuilder.AddTuiJsonFile`
   (detect top-level keys containing `.` or `Themes` as a JSON array;
   emit one `WARN` log per offending file).
3. Delete `Terminal.Gui/Configuration/ScopeJsonConverter.cs` — nothing
   left in the library reads the legacy shape.
4. Wire `JsonConfigurationSource.OnLoadException → TuiJsonErrors`
   aggregator in `TuiConfigurationExtensions` (spec §5.7).
5. Rewrite every `Examples/.../config.json` to nested.
6. Add `Tools/MigrateConfig/` console app (~50 LOC, separate csproj,
   not in any shipping solution).
7. Two parallelizable tests: one asserts the warning fires on a
   flat-key sample, one on an array-themes sample. **No translation
   logic to test.**

Once Phase D lands, Phase A2 (Mec managers consume `IOptionsMonitor<ThemeSettings>.CurrentValue` directly) unblocks.

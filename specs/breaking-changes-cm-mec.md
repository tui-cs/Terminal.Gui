# Breaking-Change Analysis: CM → MEC Migration (#5411 vs #5416)

> **Status:** Historical. Companion to [replace-cm-with-mec.md](./replace-cm-with-mec.md). Live contract: [config.md](../docfx/docs/config.md).
> Tracks the umbrella issue [#4943](https://github.com/gui-cs/Terminal.Gui/issues/4943).

This document records what is **actually** breaking in
[#5411](https://github.com/gui-cs/Terminal.Gui/pull/5411)
("Refactors `ConfigurationManager` to be based on `MEC`") versus what is deferred
to its stacked follow-on
[#5416](https://github.com/gui-cs/Terminal.Gui/pull/5416)
("Remove legacy `ConfigurationManager` after MEC migration").

The two PRs are intentionally split so the **functional migration** (#5411) and the
**legacy removal** (#5416) carry different risk profiles.

## TL;DR

| | #5411 | #5416 |
|---|---|---|
| Public types removed | **None** | Yes (CM machinery) |
| Public members removed / renamed / re-signatured | **None** | Yes |
| Public types newly `[Obsolete]` | **6** (see below) | n/a (they get deleted) |
| New transitive package dependencies | **4** (Microsoft.Extensions.*) | — |
| Behavioral change to runtime config | Intended **none** (shims preserved) | Yes (CM-backed paths removed) |
| Net source-break for an external consumer | Only if they treat **CS0618** as an error | Hard compile breaks |

**Bottom line:** Despite the `BREAKING CHANGE` label, #5411 is **source-compatible**.
It removes nothing public and changes no public signature. Its only consumer-facing
surface is **`[Obsolete]` (CS0618) deprecation warnings** plus four new package
dependencies. All hard, compile-breaking removals live in **#5416**.

---

## What #5411 actually does (verified against the merge with `develop`)

### 1. No public API removed, renamed, or re-signatured

Verified by diffing #5411's net contribution over `develop`:

- **0 files deleted** (`--diff-filter=D`).
- **0 files renamed** (`--diff-filter=R`).
- **0 public/protected methods removed.**
- The `-public ...` lines in the diff are all **auto-properties converted to
  delegating properties** — e.g. `Glyphs`, `Button.DefaultShadow`,
  `Driver.SizeDetection`. The signature (`public static Rune File { get; set; }`)
  is preserved; only the backing implementation moves to a Settings POCO
  (`GlyphSettings.Defaults.File`, etc.). Get/set behavior is unchanged.

So nothing a consumer *calls* disappears or changes shape in #5411.

### 2. Six public types marked `[Obsolete]`

These remain present and functional, but emit **CS0618** when referenced:

| Type | Replacement guidance in the attribute |
|---|---|
| `ConfigurationManager` | Use `TuiConfigurationBuilder` |
| `ConfigurationPropertyAttribute` | Use Settings POCOs with `TuiConfigurationBuilder` |
| `ConfigProperty` | Being replaced by MEC |
| `SettingsScope` | Being replaced by MEC |
| `ThemeScope` | Being replaced by MEC |
| `AppSettingsScope` | Use MEC with `TuiConfigurationBuilder` |

`ConfigPropertyHostTypes` is also `[Obsolete]` but is **`internal`** → no consumer impact.

**Still public and NOT obsolete** (the common theming/scheme entry points keep working
without warnings):

- `ThemeManager` (incl. `Theme`, `Themes`, `ThemeChanged`)
- `SchemeManager`

#### Important: the in-repo build hides these warnings; external consumers will see them

`.editorconfig` (line ~86) sets `dotnet_diagnostic.cs0618.severity = none`
**repo-wide** (added Oct 2025 in #4362, *not* by #5411). That is why building
`Terminal.Gui`, `UICatalog`, and the test projects produces **zero** CS0618 warnings
even though `UICatalog`/`Runner.cs` calls `ConfigurationManager.Enable/Load/Apply`
directly.

That suppression only applies to projects **inside this repository**. A downstream
app consuming the published NuGet package is **not** covered by it and **will** see
CS0618 deprecation warnings for any use of the six types above. For consumers that
build with `TreatWarningsAsErrors`, that is effectively a **source break** — the only
one #5411 can produce. It is easily worked around (suppress CS0618, or migrate to
`TuiConfigurationBuilder`).

### 3. Four new transitive package dependencies

Added to `Directory.Packages.props` and referenced by `Terminal.Gui`:

- `Microsoft.Extensions.Configuration` (10.0.7)
- `Microsoft.Extensions.Configuration.Binder` (10.0.7)
- `Microsoft.Extensions.Configuration.Json` (10.0.7)
- `Microsoft.Extensions.Options` (10.0.7)

This is not a source break, but it **does** change the dependency closure of anyone
referencing Terminal.Gui (relevant for size-sensitive / NativeAOT consumers; #5416
targets AOT-size reduction as follow-up). The MEC binder is reflection-based, so
#5411 adds `IL2026`/`IL3050` to `NoWarn` and `[UnconditionalSuppressMessage]` on the
binder paths to keep AOT/trim builds quiet.

### 4. Behavior: intended to be preserved

#5411 keeps the legacy CM paths alive as shims and routes effective configuration
through MEC. Notable behavior-neutral plumbing:

- `ConfigurationManager.SerializerContext` now delegates to the new non-obsolete
  `TuiSerializerContext.Instance` (literally the same readonly instance).
- A new `Terminal.Gui.Configuration.ThemeChanges` facade bridges
  `ConfigurationManager.Applied` and `ThemeManager.ThemeChanged` into one event;
  `Menu`, `MenuBar`, `StatusBar`, and `LineCanvas` subscribe to it instead of
  `ConfigurationManager.Applied` (which stays public + obsolete for external use).
- `IThemeManager`/`ISchemeManager` interfaces + `Mec*` implementations are added
  (additive).

Validated locally: full solution build clean; the `Configuration` (230) and
`CheckBox` (36) parallelizable suites pass, including develop's
`Default_CheckState_Glyphs_Are_Distinct`.

> ⚠️ One thing that *looks* like a #5411 behavior change but is **not**: the default
> checkbox glyphs changed `☒→☑` and `□→⬛`. That came from `develop` commit
> `bbd16ad1e` ("Update default checkbox state glyphs"), surfaced here only because the
> merge had to reconcile it with #5411's POCO delegation. See the merge notes below.

---

## What is deferred to #5416 (the real removals)

#5416 is stacked on #5411's branch and is where the hard breaks land:

- **Deletes / strips the legacy CM machinery** — e.g. `Glyphs.cs` loses ~1260 lines
  (the `[ConfigurationProperty]` attribute surface), `ConfigPropertyHostTypes` is
  removed, and `[ConfigurationProperty]` attributes are stripped from the Settings
  POCOs and view defaults.
- Reworks theme/scheme **data ownership** into MEC (`ThemeDefinition`, theme overlay
  merge), which requires the `config.json` shape decision that #5411 deliberately did
  not make.
- Ships a `Tools/MigrateConfig` utility to convert existing `config.json` files —
  acknowledgement that #5416 is where the **on-disk config format** can break.
- Targets NativeAOT size reduction (#4367).

In short: **#5416 removes the obsolete types and the `[ConfigurationProperty]`
contract** that #5411 only deprecated. Code that merely produced CS0618 warnings under
#5411 will **fail to compile** under #5416, and custom `config.json` files may need the
migration tool.

---

## Recommended messaging for the #5411 changelog

- "Marks `ConfigurationManager` and its scope/attribute types `[Obsolete]`; introduces
  `TuiConfigurationBuilder` and Settings POCOs as the replacement. **No public APIs are
  removed in this release** — existing code keeps compiling (you may see CS0618
  deprecation warnings). Adds `Microsoft.Extensions.Configuration` dependencies.
  Legacy `ConfigurationManager` removal and `config.json` format changes follow in a
  later release (#5416)."

---

## Appendix: merge-conflict resolution with `develop`

Merging `develop` (101 commits ahead) into #5411 produced three conflicts, all
resolved in favor of #5411's architecture while preserving develop's intent:

1. **`App/Application.cs`** — kept develop's reordered global usings; re-wrapped the
   now-obsolete `global using CM = ConfigurationManager` alias in
   `#pragma warning disable/restore CS0618`.
2. **`Configuration/ConcurrentDictionaryJsonConverter.cs`** — kept #5411's
   `TuiSerializerContext.Instance` (Phase C extraction) with develop's correct spacing
   (dropped the bot-introduced no-space formatting).
3. **`Drawing/Glyphs.cs`** — kept #5411's POCO-delegation for `CheckStateChecked` /
   `CheckStateNone`, and **moved develop's new glyph values (`☑`, `⬛`) into
   `GlyphSettings.cs`** so the static defaults — and develop's
   `Default_CheckState_Glyphs_Are_Distinct` test — produce the new glyphs.
   `config.json` (the runtime source of truth) already carried `☑`/`⬛` from develop.

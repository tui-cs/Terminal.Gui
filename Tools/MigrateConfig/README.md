# migrate-tui-config

Migrates a pre-MEC Terminal.Gui `config.json` (flat-key, array-themes
shape) to the nested shape consumed by `TuiConfigurationBuilder`.

## When you need this

Terminal.Gui v2.x originally used a flat-key `config.json` format:

```json
{
  "Button.DefaultShadow": "Opaque",
  "Themes": [
    { "Dark": { "Glyphs.CheckStateChecked": "☑" } }
  ]
}
```

Starting in the release that ships PR #5416, the library only reads
the nested MEC-native shape:

```json
{
  "Button": { "DefaultShadow": "Opaque" },
  "Themes": {
    "Dark": { "Glyphs": { "CheckStateChecked": "☑" } }
  }
}
```

Files in the old shape are detected at load time and ignored with a
`WARN` log; their settings fall through to defaults. This tool produces
a migrated copy you can drop in place.

## Usage

```bash
# Write to a new file
dotnet run --project Tools/MigrateConfig -- ./.tui/config.json ./.tui/config.migrated.json

# Or pipe to stdout
dotnet run --project Tools/MigrateConfig -- ./.tui/config.json
```

Exit codes:

- `0` — success
- `1` — usage error (wrong number of arguments)
- `2` — I/O or JSON parse error

## What it does

Three transforms applied recursively:

1. Property names containing `.` are split into nested objects.
   `"Button.DefaultShadow": "Opaque"` becomes
   `"Button": { "DefaultShadow": "Opaque" }`.
2. `"Themes"` arrays of single-key objects become dictionaries.
   `"Themes": [{"Dark": {...}}]` becomes `"Themes": {"Dark": {...}}`.
3. `"Schemes"` arrays inside a theme get the same collapse.

JSON comments and trailing commas in the input are tolerated but
**not** preserved in the output — re-add hand-edited comments after
migration.

## What it doesn't do

- It does not validate that the resulting JSON binds cleanly to the
  v2.x Settings POCOs. Run your app afterwards and watch for any
  `OptionsValidationException`.
- It does not preserve property order beyond what `System.Text.Json`
  guarantees for a `JsonObject`.

## Lifecycle

This tool is not part of the shipping `Terminal.Gui` library and is
not included in `Terminal.slnx`. It exists to ease the one-time
migration in the release that drops the legacy shape. It will be
removed in a future release once the upgrade window is past.

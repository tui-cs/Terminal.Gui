# Terminal.Gui Benchmarks

This project contains performance benchmarks and memory profilers for Terminal.Gui.

## Memory Profilers

Fast, in-process memory measurement using `GC.GetAllocatedBytesForCurrentThread()`. Results are printed as markdown tables.

### View Memory (`memory`)

Measures memory allocated when instantiating each concrete `View` subclass. Discovers all types via reflection (same technique as `TestsAllViews`). Tracks per-view footprint for [#4696](https://github.com/tui-cs/Terminal.Gui/issues/4696).

```bash
# Print to console (note the -- separator before the command)
dotnet run --project Tests/Benchmarks -c Release -- memory

# Export for comparison
dotnet run --project Tests/Benchmarks -c Release -- memory > after.md
```

### Scenario Memory (`scenarios`)

Runs each UICatalog `Scenario` for one main-loop iteration and reports total memory allocated. Uses `StopAfterFirstIteration` for fast exit plus `Scenario.StartBenchmark()` timeout as a safety net.

```bash
# Print to console (note the -- separator before the command)
dotnet run --project Tests/Benchmarks -c Release -- scenarios

# Export for comparison
dotnet run --project Tests/Benchmarks -c Release -- scenarios > scenarios.md
```

## BenchmarkDotNet Benchmarks

Formal performance benchmarks using [BenchmarkDotNet](https://benchmarkdotnet.org/).

### Run All Benchmarks

```bash
cd Tests/Benchmarks
dotnet run -c Release
```

### Run Specific Benchmark Category

```bash
# Run only DimAuto benchmarks
dotnet run -c Release -- --filter '*DimAuto*'

# Run only Scrolling benchmarks
dotnet run -c Release -- --filter '*Scroll*'

# Run only TextFormatter benchmarks
dotnet run -c Release -- --filter '*TextFormatter*'
```

### Run Specific Benchmark Method

```bash
# Run only the ComplexLayout benchmark
dotnet run -c Release -- --filter '*DimAutoBenchmark.ComplexLayout*'

# Run only TextView scrolling benchmarks
dotnet run -c Release -- --filter '*TextViewScroll*'
```

### Quick Run (Shorter but Less Accurate)

For faster iteration during development:

```bash
dotnet run -c Release -- --filter '*Scroll*' -j short
```

### List Available Benchmarks

```bash
dotnet run -c Release -- --list flat
```

## DimAuto Benchmarks

The `DimAutoBenchmark` class tests layout performance with `Dim.Auto()` in various scenarios:

- **SimpleLayout**: Baseline with 3 subviews using basic positioning
- **ComplexLayout**: 20 subviews with mixed Pos/Dim types (tests iteration overhead)
- **DeeplyNestedLayout**: 5 levels of nested views with DimAuto (tests recursive performance)

## Scrolling Benchmarks

The `Scrolling/` directory contains end-to-end scrolling benchmarks that cover the full input → layout → draw pipeline.

### BaselineScrollBenchmark

Minimal `View` subclass with a large `ContentSize` and no rendering logic. Isolates framework scrolling overhead from any view-specific work.

- **ViewportScroll_Down / Up**: Direct viewport manipulation (no key injection). Measures pure framework overhead.
- **ViewportScroll_PageDown**: Viewport-sized jump.
- Parameterized by `ContentHeight` = [1 000, 10 000]

### TextViewScrollBenchmark

`TextView` with read-only content of 1 000 / 5 000 lines of ~80-char text.

- **ScrollDown_OneStep / ScrollUp_OneStep**: Single `Key.CursorDown` / `Key.CursorUp` injection. With the caret at the viewport boundary, every keystroke triggers a viewport scroll.
- **PageDown_OneStep**: Single `Key.PageDown` injection.
- Parameterized by `Lines` = [1 000, 5 000]

### ListViewScrollBenchmark

`ListView` with 1 000 / 10 000 string items.

- **ScrollDown_OneStep / ScrollUp_OneStep / PageDown_OneStep**
- Parameterized by `Items` = [1 000, 10 000]

### TableViewScrollBenchmark

`TableView` with 100 / 1 000 rows × 10 columns.

- **ScrollDown_OneStep / ScrollUp_OneStep / PageDown_OneStep / ScrollRight_OneStep**
- Parameterized by `Rows` = [100, 1 000]

### Run all scrolling benchmarks

```bash
dotnet run --project Tests/Benchmarks -c Release -- --filter '*Scroll*'
```

## Configuration Benchmarks

The `Configuration/` directory contains benchmarks for the configuration, theming, and scheme subsystems.

### ConfigurationManagerLoadBenchmark

Measures the cold-start cost of `ConfigurationManager.Disable(true)` → `Enable(ConfigLocations.LibraryResources)` → `Apply()`. This is the app-startup hot path covering embedded-config load, deserialization, and apply.

### ThemeSwitchBenchmark

Measures `ThemeManager.Theme = "X"; ConfigurationManager.Apply()` against the embedded configuration. Parametric over all built-in theme names (`Default`, `Dark`, `Light`, `TurboPascal 5`, `Anders`, `Green Phosphor`, `Amber Phosphor`).

### SchemeAttributeBenchmark

Measures `Scheme.GetAttributeForRole(VisualRole)` for roles at different depths of the derivation chain:
- **GetNormal**: Explicitly-set role — the fastest path
- **GetHotFocus**: Derived from `Focus` (which itself derives from `Normal`)
- **GetCode**: Deepest derivation path (`Code` → `Editable` → `Normal`)

No `ConfigurationManager` required; operates on a standalone `Scheme` instance.

### SchemeSerializationBenchmark

Measures serialize/deserialize of a representative `Base` `Scheme` via `JsonSerializer` + `SchemeJsonConverter`. Catches regressions in JSON code paths when future PRs add fields to `Scheme`.

### Run all configuration benchmarks

```bash
dotnet run --project Tests/Benchmarks -c Release -- --filter '*Config*' '*Scheme*' '*Theme*'
```

## Adding New Benchmarks

1. Create a new class in an appropriate subdirectory (e.g., `Layout/`, `Text/`, `ViewBase/`, `Scrolling/`)
2. For BenchmarkDotNet: add `[MemoryDiagnoser]`, `[BenchmarkCategory]`, `[Benchmark(Baseline = true)]`
3. For memory profilers: add a `public static void Run()` method and route it from `Program.cs`
4. Use `[GlobalSetup]`/`[GlobalCleanup]` for application init/dispose

## Best Practices

- Always run benchmarks in **Release** configuration
- Use the `memory` / `scenarios` commands for quick allocation checks
- Use BenchmarkDotNet for formal timing benchmarks with statistical rigor
- Document what each benchmark measures

## Output Flush Benchmarks

`OutputWriteBenchmark` compares a full 270x72 frame with a sparse frame whose first and last cells
are dirty on every row. The sparse case reproduces the shape reported in issue #5627.

```powershell
dotnet run --project Tests/Benchmarks -c Release -- --filter "*OutputWriteBenchmark*" --job short --exporters json
```

To measure the real Windows output path, run the benchmark through Windows Terminal or `tuirec` and
provide a JSON result path:

```powershell
dotnet run --project Tests/Benchmarks -c Release -- output-conpty "$env:TEMP\tg-output-conpty.json"
```

The ConPTY benchmark requires Windows with output attached to a terminal. It warms three sparse
frames, measures twenty frames, and records mean, median, p95, minimum, and maximum frame latency.

## Continuous Integration

### Layer 1: Performance Smoke Tests

Stopwatch-based xUnit tests in `Tests/UnitTestsParallelizable/Views/ScrollingPerformanceTests.cs` run on every CI build via the standard unit test workflow. They use generous thresholds (50–100× typical) to catch catastrophic O(n²) regressions without flaking on slow runners.

Each test:
- Creates a large document (10 000 rows / 100 000 items)
- Measures the cost of a **single viewport draw** after scrolling to the mid-point
- Asserts completion under a generous threshold (e.g., < 200 ms for TableView)

This detects if a draw function accidentally iterates the entire document instead of just the visible viewport.

### Layer 2: Baseline Comparison

The `.github/workflows/perf-gate.yml` workflow runs on every push to `main` / `develop` (not PRs) and:

1. Runs the `*Scroll*`, `*Config*`, `*Scheme*`, and `*Theme*` benchmarks with `--job short` (~30–60 s total)
2. Compares results to `Tests/Benchmarks/baseline.json`
3. **Fails** if any benchmark exceeds **3×** the baseline
4. **Celebrates** 🎉 if any benchmark drops below **0.8×** the baseline
5. Posts a markdown comparison table to the GitHub step summary

### Updating the Baseline

After a deliberate performance change, re-run the focused scrolling benchmarks, then update `baseline.json`:

```bash
# Run ShortRun and export JSON results
dotnet run --project Tests/Benchmarks -c Release -- --filter '*Scroll*' '*Config*' '*Scheme*' '*Theme*' -j short --exporters json

# Inspect the JSON output in BenchmarkDotNet.Artifacts/ and update baseline.json
```

## Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Issue #4696 — View memory footprint](https://github.com/tui-cs/Terminal.Gui/issues/4696)

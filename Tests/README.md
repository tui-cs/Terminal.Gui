# Terminal.Gui Tests

This folder contains the tests for Terminal.Gui.

## Test Projects

### ./UnitTestsParallelizable

The primary home for new tests. Tests here run in parallel (`parallelizeAssembly: true`, `maxParallelThreads: 12`)
and **must not** touch any process-wide static state.

### ./UnitTests.NonParallelizable

Tests that either explicitly test process-wide static state, or must set process-wide statics, or otherwise
cannot be run concurrently with other tests. These tests run with `parallelizeAssembly: false`.

### ./UnitTests.Legacy

Tests that have not yet been ported to `UnitTestsParallelizable` or `UnitTests.NonParallelizable`.
These tests are candidates for rewrite (if the case is not covered elsewhere) or deletion (if already covered).
Do not add new tests here.

### ./IntegrationTests

Integration tests for Terminal.Gui.

### ./StressTests

Stress tests for Terminal.Gui.

### ./NativeAotSmoke

Native AOT smoke-test application used by CI to validate trimming and AOT compatibility in-repo.

---

## Static State in Terminal.Gui

The following classes and properties use process-wide static state. Tests that read or write these **must not**
go in `UnitTestsParallelizable` — they belong in `UnitTests.NonParallelizable`.

### `Application` (legacy static façade — `Terminal.Gui.App/Legacy/`)

| Member | Notes |
|--------|-------|
| `Application.Instance` | Returns `ApplicationImpl.Instance` (singleton) |
| `Application.Init(driverName)` | Initializes the singleton; sets `Initialized`, `Driver`, `MainThreadId`, `SynchronizationContext` |
| `Application.Shutdown()` | Disposes the singleton; resets all static fields |
| `Application.Initialized` | Global bool; true between `Init` and `Shutdown` |
| `Application.MainThreadId` | Process-wide main thread ID set by `Init` |
| `Application.Driver` | The active `IDriver` singleton |
| `Application.DefaultKeyBindings` | Process-wide default key-binding dictionary (mutable `IDictionary`) |
| `Application.ForceDriver` | Persists across tests unless reset |
| `Application.Keyboard` | Delegates to `ApplicationImpl.Instance.Keyboard` |
| `Application.Navigation` | Delegates to `ApplicationImpl.Instance.Navigation` |
| `Application.Popovers` | Delegates to `ApplicationImpl.Instance.Popovers` |
| `Application.Screen` | Delegates to `ApplicationImpl.Instance.Screen` |
| `Application.ResetState()` | Resets all static backing fields to defaults |
| `SynchronizationContext.Current` | Set by `Application.Init`; process-wide |

### `ApplicationImpl` (singleton — `Terminal.Gui.App/ApplicationImpl.cs`)

| Member | Notes |
|--------|-------|
| `ApplicationImpl.Instance` | Process-wide singleton; set by `SetInstance()` |
| `ApplicationImpl.SetInstance(impl)` | Replaces the singleton (used in tests) |
| `ApplicationImpl.ResetModelUsageTracking()` | Resets the static "which model was used" flag |

### Settings facades (process-wide — `Terminal.Gui.Configuration/`)

| Member | Notes |
|--------|-------|
| `TuiConfigurationBuilder.Shared` | Process-wide builder applied at assembly load |
| `*Settings.Current` / `*Settings.Defaults` | Static facades mutated by `ApplyToStaticFacades` |

### `View.Diagnostics` (static field — `Terminal.Gui.ViewBase/View.cs`)

| Member | Notes |
|--------|-------|
| `View.Diagnostics` | `ViewDiagnosticFlags`; process-wide debug flags |

---

## Which Project Does My Test Belong In?

| Test characteristic | Project |
|---------------------|---------|
| No static state; no `Application.Init`/`Shutdown`; no process-wide settings-facade mutations | `UnitTestsParallelizable` |
| Calls `Application.Init`/`Shutdown` directly, or mutates `Application.DefaultKeyBindings`, `Application.ForceDriver`, or `ApplicationImpl.ResetModelUsageTracking()` | `UnitTests.NonParallelizable` |
| Uses `[AutoInitShutdown]` or `[SetupFakeApplication]` (fake driver, does not call real `Application.Init`) | Currently in `UnitTests.Legacy`; migrate to `UnitTestsParallelizable` or `UnitTests.NonParallelizable` after review |
| Not yet reviewed / ported | `UnitTests.Legacy` |

---

See the [Testing wiki](https://github.com/tui-cs/Terminal.Gui/wiki/Testing) for details on how to add more tests.

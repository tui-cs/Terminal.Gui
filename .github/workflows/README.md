## CI/CD Workflows

The repository uses multiple GitHub Actions workflows. What runs and when:

> **Note:** Tests use xUnit v3 with Microsoft Testing Platform (MTP). The test runner is
> configured in `global.json` via `"test": { "runner": "Microsoft.Testing.Platform" }`.
> MTP runs test projects as standalone executables, so each OS must build its own binaries.

### 1) Build Validation (`.github/workflows/build-validation.yml`)

- **Triggers**: push and pull_request to `main`, `develop` (ignores `**.md`)
- **Runner/timeout**: `ubuntu-latest`, 10 minutes
- **Steps**:
- Checkout and setup .NET 10.x GA
- `dotnet restore`
- Build Debug: `dotnet build --configuration Debug --no-restore -property:NoWarn=0618%3B0612`
- Build Release (library): `dotnet build Terminal.Gui/Terminal.Gui.csproj --configuration Release --no-incremental --force -property:NoWarn=0618%3B0612`
- Pack Release packages: `dotnet pack Terminal.Gui/Terminal.Gui.csproj --configuration Release --output ./local_packages -property:NoWarn=0618%3B0612` and `dotnet pack Terminal.Gui.Interop.Spectre/Terminal.Gui.Interop.Spectre.csproj --configuration Release --output ./local_packages -property:NoWarn=0618%3B0612`
- Publish `Tests/NativeAotSmoke` with AOT and run `--smoke-test`
- Build Release solution

### 2) Build & Run Unit Tests (`.github/workflows/unit-tests.yml`)

- **Triggers**: push and pull_request to `main`, `develop` (ignores `**.md`)
- **Matrix**: Ubuntu/Windows/macOS
- **Timeout**: 15 minutes (non-parallel), 60 minutes (parallel)
- **Process**:
1. Each OS checks out code, restores, and builds locally
2. **Performance optimizations**:
   - Disables Windows Defender on Windows runners (significant speedup)
3. Runs three test jobs:
   - **Non-parallel UnitTests.Legacy**: `Tests/UnitTests.Legacy` with diagnostic output
   - **Non-parallel UnitTests.NonParallelizable**: `Tests/UnitTests.NonParallelizable` with diagnostic output
   - **Parallel UnitTestsParallelizable**: `Tests/UnitTestsParallelizable` with diagnostic output
4. Uploads test logs and diagnostic data from all runners

**Test results**: All tests output to unified `TestResults/` directory at repository root

### 3) Build & Run Integration Tests (`.github/workflows/integration-tests.yml`)

- **Triggers**: push and pull_request to `main`, `develop` (ignores `**.md`)
- **Matrix**: Ubuntu/Windows/macOS
- **Timeout**: 15 minutes
- **Process**:
1. Each OS checks out code, restores, and builds locally
2. **Performance optimizations**:
   - Disables Windows Defender on Windows runners
3. Runs IntegrationTests with diagnostic output
4. Uploads logs per-OS

### 4) Publish to NuGet (`.github/workflows/publish.yml`)

- **Triggers**: push to `develop` and tags `v*` (ignores `**.md`)
- Uses GitVersion to compute SemVer, builds Release, packs Terminal.Gui and Terminal.Gui.Interop.Spectre with symbols, and pushes both to NuGet.org using `NUGET_API_KEY`
- **Automatically triggered** when a `v*` tag is pushed — typically by the **Finalize Release** workflow after a release PR (from **Prepare Release**) is merged into `main`
- **Additional actions on tag push** (`v*`):
  - Triggers Terminal.Gui.templates repository update via repository_dispatch (requires `TEMPLATE_REPO_TOKEN` secret)

### 5) Build and publish API docs (`.github/workflows/api-docs.yml`)

- **Triggers**: push to `develop`, and `workflow_dispatch`
- Builds DocFX site on Windows and deploys to GitHub Pages


### Replicating CI Locally

```bash
# Full CI sequence:
dotnet restore
dotnet build --configuration Debug --no-restore
dotnet test --project Tests/UnitTests.Legacy --no-build --verbosity normal
dotnet test --project Tests/UnitTests.NonParallelizable --no-build --verbosity normal
dotnet test --project Tests/UnitTestsParallelizable --no-build --verbosity normal
dotnet build --configuration Release --no-restore
```

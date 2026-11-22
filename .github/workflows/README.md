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

### 4) Release and Release Lock Gate

- [Release](release.yml) is triggered by manual dispatch to prepare a stable, beta, or RC release and by pull requests targeting `main` to verify and finalize a release.
- [Release Lock Gate](release-lock-gate.yml) handles `pull_request` and `merge_group` events targeting `develop` so routine PRs do not show skipped release orchestration jobs. It checks out the trusted base revision before running the lock script.
- The prepare job waits for the `develop` merge queue to drain, refreshes the source snapshot, and calculates the version from GitVersion and full history. A `release-snapshot-gate` check blocks the promotion PR if its head no longer matches the recorded snapshot.
- The workflow opens a direct `develop` → `main` PR for maintainer review. Merge the promotion with a merge commit; auto-merge is intentionally disabled.
- After the PR is merged, finalization validates the snapshot, configures a Git identity, and creates or verifies the tag and GitHub Release. Pre-release tags set the GitHub Release prerelease flag.
- Finalization then creates or reuses the `backmerge/<tag>-<short-merge-sha>` PR. Conflicts remain open for manual resolution. Merge the back-merge with a merge commit through the `develop` merge queue; rerunning finalization resumes safely from a merged back-merge PR.
- Require `release-lock-gate` for both `develop` pull requests and merge-group runs, and require `release-snapshot-gate` for release PRs targeting `main`. Configure the `develop` merge queue to use **MERGE**, set its maximum group size to one, and require all applicable checks for both pull requests and merge groups. Checks required for the queue must not use path filters on pull-request events.
- The trusted-base checkout prevents a PR from changing the lock script and executing its modified copy in that PR's check. CODEOWNERS protects the release workflows and lock scripts, but a reviewer must still treat edits to the gate workflow itself as security-sensitive because pull-request workflow logic comes from the candidate commit.
- To unlock after a back-merge PR is closed without merging, reopen that PR and merge it with a merge commit. Do not change the branch name or release marker.

### 5) Publish to NuGet (`.github/workflows/publish.yml`)

- **Triggers**: push to `develop` and tags `v*` (ignores `**.md`)
- Uses GitVersion to compute SemVer, builds Release, packs Terminal.Gui and Terminal.Gui.Interop.Spectre with symbols, and pushes both to NuGet.org. It prefers Trusted Publishing when `vars.NUGET_TRUSTED_PUBLISHING_USER` and a matching NuGet.org policy are configured; otherwise it falls back to the `NUGET_API_KEY` secret.
- **Automatically triggered** when a stable `v*` tag is pushed by the **Release** workflow after the `develop` → `main` promotion PR is merged
- **Additional actions on tag push** (`v*`):
  - Triggers Terminal.Gui.templates repository update via repository_dispatch (requires `TEMPLATE_REPO_TOKEN` secret)

### 6) Build and publish API docs (`.github/workflows/api-docs.yml`)

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

# Terminal.Gui Library — Maintainer Guide

This directory contains the core **Terminal.Gui** library source code. This README documents how to maintain and release the library. For contribution guidelines, see [CONTRIBUTING.md](../CONTRIBUTING.md). For building apps with Terminal.Gui, see [the documentation](https://tui-cs.github.io/Terminal.Gui).

## Versioning

Versions are computed automatically by [GitVersion 6.x](https://gitversion.net) using the [GitFlow](https://gitversion.net/docs/learn/branching-strategies/gitflow/) branching strategy. Configuration is in [`GitVersion.yml`](../GitVersion.yml).

The [GitVersion.MsBuild](https://www.nuget.org/packages/GitVersion.MsBuild) NuGet package is included in `Terminal.Gui.csproj` and automatically sets `Version`, `AssemblyVersion`, `FileVersion`, and `InformationalVersion` from git history at build time. No manual version management is needed.

### How Versions Are Computed

| Branch | Example Version | Increment | Notes |
|--------|----------------|-----------|-------|
| `main` (stable) | `2.0.0` | Patch | Label set in `GitVersion.yml` (`label: ''`) |
| `main` (pre-release) | `2.0.1-rc.1` | Patch | Label set in `GitVersion.yml` (e.g., `label: rc`) |
| `develop` | `2.1.0-develop.42` | Minor | Always carries `-develop` pre-release label |
| `feature/*`, `fix/*`, etc. | `2.1.0-my-feature.1` | Inherit | Inherits from `develop`; branch name becomes label |
| `pull-request/*` | `2.0.0-pr.123.1` | Inherit | PR number in label |

### Checking Versions Locally

```powershell
# Install the CLI tool (one-time)
dotnet tool install --global GitVersion.Tool

# Show what version would be computed for the current branch
dotnet-gitversion
```

The version is also embedded in every build. For example, `UICatalog --version` displays the terse SemVer (build metadata after `+` is stripped).

### Pre-Release Label Progression on `main`

To change the pre-release stage, edit the `label` field under `main` in `GitVersion.yml`:

| Stage | `label` value | Example Output |
|-------|--------------|----------------|
| Beta | `beta` | `2.0.0-beta.1` |
| Release Candidate | `rc` | `2.0.0-rc.1` |
| Stable Release | `''` (empty) | `2.0.0` |

## Publishing a Release

Releases follow [Semantic Versioning](https://semver.org/): **MAJOR** for breaking changes, **MINOR** for new features, **PATCH** for bug fixes.

### Automated Release Workflow (Preferred)

Two workflows handle the release lifecycle:

**Step 1 — [Prepare Release](../.github/workflows/prepare-release.yml)** (manual trigger):
1. Go to **Actions → Prepare Release → Run workflow**.
2. Pick the release type (`beta`, `rc`, `stable`) and optionally override the version.
3. The workflow creates a `release/<tag>` branch from `develop` — where `<tag>` is the full version tag, e.g. `release/v2.0.0` or `release/v2.0.0-beta.1` — updates the `GitVersion.yml` label, and opens a PR into `main`.
4. Review the PR — CI runs, branch protections apply.

**Step 2 — [Finalize Release](../.github/workflows/finalize-release.yml)** (automatic on PR merge):
When the release PR is merged into `main`:
1. Creates an annotated tag (`vX.Y.Z` or `vX.Y.Z-beta`)
2. Creates a GitHub Release with auto-generated notes
3. [Publish](../.github/workflows/publish.yml) fires automatically → NuGet package published
4. Opens a back-merge PR (`main` → `develop`) to keep branches in sync

### Manual Release Steps

If you need to release manually:

1. **Ensure `develop` is ready**: all changes committed, CI passing.

2. **Merge `develop` into `main`**:
   ```powershell
   git checkout main
   git pull upstream main
   git checkout develop
   git pull upstream develop
   git checkout main
   git merge develop
   # Fix any merge conflicts
   ```

3. **Update the pre-release label** in `GitVersion.yml` if changing stages (e.g., `beta` → `rc` → `''`).

4. **Tag the release on `main`**:
   ```powershell
   git tag vX.Y.Z -a -m "Release vX.Y.Z"
   ```

5. **Push atomically**:
   ```powershell
   git push --atomic upstream main vX.Y.Z
   ```

6. **Monitor CI**: the [Publish workflow](https://github.com/tui-cs/Terminal.Gui/actions) builds and pushes to [NuGet](https://www.nuget.org/packages/Terminal.Gui). It also triggers an update to [Terminal.Gui.templates](https://github.com/tui-cs/Terminal.Gui.templates) for stable releases.

7. **Create a GitHub Release** at [Releases](https://github.com/tui-cs/Terminal.Gui/releases) with auto-generated release notes.

8. **Merge `main` back into `develop`**:
   ```powershell
   git checkout develop
   git pull upstream develop
   git merge main
   git push upstream develop
   ```

## CI/CD Workflows

All workflows are in [`.github/workflows/`](../.github/workflows/):

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **[prepare-release.yml](../.github/workflows/prepare-release.yml)** | Manual dispatch | Creates a release PR from `develop` → `main` with label updates |
| **[finalize-release.yml](../.github/workflows/finalize-release.yml)** | Release PR merged to `main` | Creates tag, GitHub Release, back-merge PR to `develop` |
| **[publish.yml](../.github/workflows/publish.yml)** | Push to `develop`, or `v*` tags | Builds Release config, packs, and publishes to NuGet.org |
| **[build-validation.yml](../.github/workflows/build-validation.yml)** | Push/PR to `main` or `develop` | Builds all configurations to validate compilation |
| **[unit-tests.yml](../.github/workflows/unit-tests.yml)** | Push/PR to `main` or `develop` | Runs all unit tests |
| **[api-docs.yml](../.github/workflows/api-docs.yml)** | Push to `develop` | Builds and deploys API docs to GitHub Pages |
| **[codeql-analysis.yml](../.github/workflows/codeql-analysis.yml)** | Push/PR to `main` or `develop` | CodeQL security analysis |
| **[integration-tests.yml](../.github/workflows/integration-tests.yml)** | Push/PR to `main` or `develop` | Integration tests |
| **[stress-tests.yml](../.github/workflows/stress-tests.yml)** | Push/PR to `main` or `develop` | Stress tests |

## V1 Legacy Branches

Terminal.Gui V1 (latest: `v1.19.0`) is maintained on separate branches:

| Branch | Purpose |
|--------|---------|
| `v1_release` | V1 stable releases (equivalent of `main` for V1) |
| `v1_develop` | V1 development (equivalent of `develop` for V1) |

V1 follows the same GitFlow model as V2 but is in maintenance-only mode. The V1 NuGet package is `Terminal.Gui` 1.x. V1 API docs are published from `v1_release` to a separate GitHub Pages path.

These branches are **not** configured in `GitVersion.yml` (the config was removed to avoid interference with V2 versioning). V1 releases are tagged manually (e.g., `v1.19.0`).

## NuGet Package

- **Package**: [nuget.org/packages/Terminal.Gui](https://www.nuget.org/packages/Terminal.Gui)
- **Auto-published** on every push to `develop` (pre-release versions) and on `v*` tag pushes (stable/pre-release versions tagged on `main`)
- Pre-release versions (e.g., `2.1.0-develop.42`) are marked as pre-release on NuGet

### Local Package Development

When building in `Release` configuration, the `.csproj` automatically:
1. Copies `.nupkg` and `.snupkg` to `../local_packages/`
2. Pushes the package to the local NuGet cache

Use the `local_packages` folder as a local NuGet source to test packages before publishing:
```powershell
dotnet build Terminal.Gui/Terminal.Gui.csproj -c Release
# Package is now in local_packages/ and your global NuGet cache
```

## Documentation

- **Live docs**: [tui-cs.github.io/Terminal.Gui](https://tui-cs.github.io/Terminal.Gui)
- **DocFX source**: [`docfx/`](../docfx/) — see [`docfx/README.md`](../docfx/README.md) for local generation
- **API docs** are auto-deployed to GitHub Pages on every push to `develop`

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for build/test instructions, coding conventions, and PR guidelines.

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
| `develop` | `2.5.0-develop.55` | Patch | Always carries `-develop` pre-release label |
| `feature/*`, `fix/*`, etc. | `2.5.0-my-feature.1` | Inherit | Inherits from `develop`; branch name becomes label |
| `pull-request/*` | `2.0.0-pr.123.1` | Inherit | PR number in label |

`develop` increments **Patch** (`GitVersion.yml` `increment: Patch`). To bump Minor, include `+semver: minor` in a commit message. Do not pin `next-version`.

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

The release lifecycle uses one release workflow; publishing starts automatically after it creates a stable tag. PRs use Squash and merge; the `develop` merge queue validates the queued commit before merging:

**Step 1 — [Release](../.github/workflows/release.yml)** (manual trigger):
1. Go to **Actions → Release → Run workflow** on the `develop` branch.
2. Optionally enter a stable version override (e.g., `2.6.0`); otherwise GitVersion computes it from full history and commit-message increments.
3. The workflow opens a direct PR from `develop` to `main` and enables squash auto-merge.

**Step 2 — Finalize after the PR merges** (automatic):
When the release PR is merged into `main`:
1. Verifies the source snapshot and opens a back-merge PR (`main` → `develop`) to keep branches in sync.
2. If the source/version checks pass and the back-merge has no conflicts, creates a stable tag (`vX.Y.Z`) and GitHub Release.
3. [Publish](../.github/workflows/publish.yml) fires automatically from the tag → NuGet package published.

Repository administrators must enable the `develop` merge queue, set its maximum group size to one, and require `release-lock-gate` and all applicable CI checks for merge-group runs as well as pull requests. After the back-merge completes, re-run blocked develop PR checks.

### Manual Release Recovery

If the automation halts, inspect the failed run and resolve the issue through normal pull requests and branch protection. Do not push directly or force-push to `main` or `develop`; conflicted back-merge PRs remain open for manual resolution. After resolving and merging a conflicted back-merge PR, rerun the failed release job so it can create the tag and GitHub Release.

## CI/CD Workflows

All workflows are in [`.github/workflows/`](../.github/workflows/):

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **[release.yml](../.github/workflows/release.yml)** | Manual dispatch and merged release PR | Promotes `develop` to `main`, creates a stable release, and opens a back-merge PR |
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
- Pre-release versions (e.g., `2.5.0-develop.55`) are marked as pre-release on NuGet

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

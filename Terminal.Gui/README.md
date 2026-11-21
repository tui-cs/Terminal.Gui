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

The release lifecycle uses a release workflow and a separate lock-gate workflow. Publishing starts automatically after the workflow creates the release tag. Promotion and back-merge PRs use merge commits so `main` and `develop` retain ancestry:

**Step 1 — [Release](../.github/workflows/release.yml)** (manual trigger):
1. Go to **Actions → Release → Run workflow** on the `develop` branch.
2. Choose `beta`, `rc`, or `stable`. Optionally enter a version override (e.g., `2.6.0`); otherwise GitVersion computes it from full history and commit-message increments.
3. The workflow waits for the `develop` merge queue to drain, records the refreshed source SHA, and opens a direct PR from `develop` to `main` for maintainer review.
4. Verify that `release-snapshot-gate` passes, then merge the promotion PR with a merge commit.

**Step 2 — Finalize after the PR merges** (automatic):
When the release PR is merged into `main`:
1. Verifies the source snapshot, then creates or verifies the annotated tag and GitHub Release. Beta and RC releases are marked as GitHub prereleases.
2. Opens or reuses the back-merge PR (`main` → `develop`) to keep branch ancestry intact.
3. The maintainer resolves any conflicts and merges the back-merge with a merge commit through the `develop` merge queue.
4. [Publish](../.github/workflows/publish.yml) fires automatically from the tag → NuGet package published.

Repository administrators must enable the `develop` merge queue with the **MERGE** method, set its maximum group size to one, and require `release-lock-gate` and all applicable CI checks for merge-group runs as well as pull requests. Require `release-snapshot-gate` for promotion PRs targeting `main`. Checks required for the queue must not use pull-request path filters. After the back-merge completes, re-run blocked develop PR checks.

### Manual Release Recovery

To recover from a conflicted back-merge, resolve and merge the PR, then rerun the failed finalization job. A rerun recognizes the merged PR even after GitHub deletes its branch. To unlock after a back-merge PR is closed without merging, reopen and merge it; do not replace its branch or release marker.

For break-glass recovery when automation is unavailable, prepare a clean clone and preserve merge ancestry:

1. Fetch and fast-forward both `main` and `develop`.
2. On `main`, run `git merge --no-ff develop`, create the annotated release tag, and then push the commit and tag atomically with `git push --atomic origin main vX.Y.Z`.
3. Create the GitHub Release and publish packages through the normal `publish.yml` path.
4. On `develop`, run `git merge --no-ff main`, resolve any conflicts, and push through a protected back-merge PR rather than directly to `develop`.

Do not use force pushes or squash merges during break-glass recovery.

## CI/CD Workflows

All workflows are in [`.github/workflows/`](../.github/workflows/):

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **[release.yml](../.github/workflows/release.yml)** | Manual dispatch and pull requests targeting `main` | Promotes `develop` to `main`, creates a stable or prerelease, and opens a back-merge PR |
| **[release-lock-gate.yml](../.github/workflows/release-lock-gate.yml)** | PR and merge-group events targeting `develop` | Blocks unrelated `develop` merges while a release is in progress |
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

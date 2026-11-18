# Pull Request Workflow

**Source:** [CONTRIBUTING.md - Pull Request Guidelines](../../CONTRIBUTING.md#pull-request-guidelines)

## PR Requirements Checklist

Before submitting a PR, ensure:

- [ ] **Title** follows format: `"Fixes #issue. Terse description"`
  - If multiple issues: `"Fixes #123, #456. Terse description"`

- [ ] **Description** includes:
  - `- Fixes #issue` for each issue near the top
  - Instructions for pulling down locally (see template below)

- [ ] **Tests** added for new functionality (see [Testing Patterns](../rules/testing-patterns.md))

- [ ] **Coverage** maintained or increased (70%+ for new code)

- [ ] **Scenarios** updated in UICatalog when adding features

- [ ] **Warnings** - No new warnings introduced
  - Any file modified in PR that currently generates warnings **MUST** be fixed
  - Exception: Warnings caused by `[Obsolete]` attributes can remain

- [ ] **Code formatting** applied (see [Code Layout](../rules/code-layout.md))

- [ ] **Build passes** locally: `dotnet build --no-restore`

- [ ] **Tests pass** locally: `dotnet test --project Tests/UnitTestsParallelizable --no-build && dotnet test --project Tests/UnitTests.NonParallelizable --no-build`

## PR Description Template

```markdown
## Summary

[Brief description of changes]

- Fixes #issue_number

## Changes

- [List key changes]
- [Include rationale for non-obvious decisions]

## Testing

- [Describe how you tested the changes]
- [List any new test cases added]

## To pull down this PR locally:

```bash
git remote add copilot <your-fork-url>
git fetch copilot <branch-name>
git checkout copilot/<branch-name>
```

## Pre-Submission Workflow

### 1. Verify Build

```bash
dotnet restore
dotnet build --configuration Debug --no-restore
```

**Expected:** 0 errors, no new warnings

### 2. Run Tests

```bash
dotnet test --project Tests/UnitTestsParallelizable --no-build --verbosity normal && dotnet test --project Tests/UnitTests.NonParallelizable --no-build --verbosity normal
```

**Expected:** All tests pass

### 3. Check Coverage

Coverage collection is temporarily disabled in CI during the xUnit v3 / MTP migration.

### 4. Format Code

**For ReSharper/Rider users:**
- Run "Full Cleanup" profile (`Ctrl-E-C`)
- Only format files you modified

**For Visual Studio users:**
- Format document (`Ctrl-K-D`)
- Only format files you modified

### 5. Review Changed Files

```bash
git status
git diff
```

**Verify:**
- No unintended changes
- No debugging code left behind
- No commented-out code (unless necessary with explanation)

## What NOT to Include in PRs

- ❌ Don't modify unrelated code
- ❌ Don't remove/edit unrelated tests
- ❌ Don't break existing functionality
- ❌ Don't add tests to `UnitTests.NonParallelizable` if they can be parallelizable; never add tests to `UnitTests.Legacy`
- ❌ Don't decrease code coverage
- ❌ Don't introduce new warnings
- ❌ Don't include commented-out code without explanation
- ❌ Don't add new linters/formatters

## After PR Submission

1. **Monitor CI/CD pipelines** - Ensure all checks pass
2. **Address review feedback** promptly
3. **Keep PR updated** with `develop` if needed:
   ```bash
   git fetch origin
   git rebase origin/develop
   git push --force-with-lease
   ```

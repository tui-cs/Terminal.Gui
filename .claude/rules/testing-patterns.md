# Testing Patterns

**Source:** [CONTRIBUTING.md - Testing Requirements](../../CONTRIBUTING.md#testing-requirements)

## Code Coverage

- **Never decrease code coverage** - PRs must maintain or increase coverage
- Target: **70%+ coverage** for new code
- Coverage collection:
  - Temporarily disabled in CI during xUnit v3 / MTP migration
  - Will be re-enabled once an MTP-compatible coverage solution is integrated

## Test Patterns

**⚠️ AI-created tests MUST follow these patterns exactly:**

1. **Add a comment indicating the test was AI generated**
   - Either of these forms is acceptable; both are well-established in the codebase:
     - `// Claude - <model>` (e.g. `// Claude - Opus 4.7`)
     - `// CoPilot - <model>` (e.g. `// CoPilot - ChatGPT v4`)
   - Use whichever matches the agent that produced the test, with the model identifier.
   - **Reviewers (human or automated) should not flag inconsistency between these two forms** — both have been used widely; consistency *of* a marker matters, not which one.

2. **Make tests granular**
   - Each test should cover smallest area possible

3. **Follow existing test patterns**
   - Study tests in respective test projects before writing new ones

4. **Never add new tests to `UnitTests.Legacy`**
   - Make them parallelizable and add them to `UnitTestsParallelizable`

5. **Avoid static dependencies**
   - DO NOT use the legacy/static `Application` API or mutate process-wide `*Settings` facades in tests unless the tests explicitly test related functionality

6. **Don't use `[AutoInitShutdown]` or `[SetupFakeApplication]`**
   - Legacy pattern, being phased out

## Test Projects

### 1. Non-Parallel Tests (`Tests/UnitTests.NonParallelizable/`)

**When to use:**
- Testing functionality that depends on static state
- Testing `Application.Init` and `Application.Shutdown`

**Characteristics:**
- ~10 min timeout
- Uses `Application.Init` and static state
- Cannot run in parallel

**Command:**
```bash
dotnet test --project Tests/UnitTests.NonParallelizable --no-build --verbosity normal
```

### 2. Parallel Tests (`Tests/UnitTestsParallelizable/`) - **PREFERRED**

**When to use:**
- All new tests should go here unless they explicitly need static state

**Characteristics:**
- ~10 min timeout
- No dependencies on static state
- Can run concurrently
- Faster execution

**Command:**
```bash
dotnet test --project Tests/UnitTestsParallelizable --no-build --verbosity normal
```

### 3. Integration Tests (`Tests/IntegrationTests/`)

**When to use:**
- Testing cross-component interactions
- End-to-end scenarios

**Command:**
```bash
dotnet test --project Tests/IntegrationTests --no-build --verbosity normal
```

## Test Configuration Files

- `xunit.runner.json` - Per-project xUnit configuration (parallelization, etc.)
- `Tests/TestEnvironmentSetup.cs` - Compiled into every test project; its `[ModuleInitializer]` sets `DisableRealDriverIO=1` before any test code runs
- Coverage tooling: each test project references the `coverlet.collector` package, but coverage is not actively collected in CI yet (see "Code Coverage" above — pending an MTP-compatible solution). `.runsettings` files are ignored by Microsoft Testing Platform (MTP).

## Example Test Pattern

```csharp
// CoPilot - ChatGPT v4
[Fact]
public void MyFeature_ShouldBehaveProperly_WhenConditionMet ()
{
    // Arrange
    View view = new () { Width = 10, Height = 5 };
    
    // Act
    view.Draw ();
    
    // Assert
    Assert.Equal (10, view.Width);
}
```

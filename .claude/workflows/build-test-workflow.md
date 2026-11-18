# Build and Test Workflow

**Source:** [CONTRIBUTING.md - Building and Testing](../../CONTRIBUTING.md#building-and-testing)

## Required Tools

- **.NET SDK**: 10.0.100 (see `global.json`)
- **Runtime**: .NET 10.x (latest GA)
- **Optional**: ReSharper/Rider for code formatting (honor `.editorconfig` and `Terminal.sln.DotSettings`)

## Build Commands

**⚠️ ALWAYS run these commands from the repository root**

### 1. Restore Packages (Required First)

**Time:** ~15-20 seconds

```bash
dotnet restore
```

**Must run before building.** Downloads all NuGet dependencies.

### 2. Build Solution (Debug)

**Time:** ~50 seconds

```bash
dotnet build --configuration Debug --no-restore
```

**Expected output:**
- ~326 warnings (nullable reference warnings, unused variables, etc.) - **these are normal**
- **0 errors expected**

### 3. Build Release (For Packaging)

```bash
dotnet build --configuration Release --no-restore
```

## Test Commands

### Run Non-Parallel Tests

**Time:** ~10 min timeout

```bash
dotnet test --project Tests/UnitTests.NonParallelizable --no-build --verbosity normal
```

- Uses `Application.Init` and static state
- Cannot run in parallel
- Includes `--diagnostic` flag for logging

### Run Parallel Tests (Preferred)

**Time:** ~10 min timeout

```bash
dotnet test --project Tests/UnitTestsParallelizable --no-build --verbosity normal
```

- No dependencies on static state
- **Preferred for new tests**
- Faster execution

### Run Integration Tests

```bash
dotnet test --project Tests/IntegrationTests --no-build --verbosity normal
```

### Run All Tests

```bash
dotnet test --project Tests/UnitTestsParallelizable --no-build --verbosity normal && dotnet test --project Tests/UnitTests.NonParallelizable --no-build --verbosity normal
```

## Common Build Issues

### Issue: AOT validation failures

**Solution:** Build and publish the in-repo AOT smoke app:

```bash
dotnet publish ./Tests/NativeAotSmoke/NativeAotSmoke.csproj --configuration Release --output ./aot-publish
./aot-publish/NativeAotSmoke --smoke-test
```

## Build Order Best Practice

**For clean builds, always run in this order:**

```bash
dotnet restore && dotnet build --no-restore && dotnet test --project Tests/UnitTestsParallelizable --no-build && dotnet test --project Tests/UnitTests.NonParallelizable --no-build
```

This ensures:
1. Packages are downloaded first
2. Build uses restored packages
3. Tests run against built assemblies
4. Minimal rebuild overhead

## Warning Management

**⚠️ CRITICAL - PRs must not introduce any new warnings**

- Any file modified in a PR that currently generates warnings **MUST** be fixed to remove those warnings
- **Exception:** Warnings caused by `[Obsolete]` attributes can remain
- **Action:** Before submitting a PR, verify your changes don't add new warnings and fix any warnings in files you modify

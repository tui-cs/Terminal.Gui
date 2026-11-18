# Code Cleanup Agent

## Purpose
Automated code cleanup, modernization, and refactoring for Terminal.Gui C# files.

## Components

1. **PartialSplitter** - Roslyn tool that splits large files (>1000 lines) into semantic partials
2. **BackingFieldReorderer** - Roslyn tool that fixes ReSharper bug RSRP-484963 (backing fields before properties)

## Usage

Users should be able to invoke the code cleanup agent by telling Claude Code "cleanup these files" and the agent will run the necessary steps to clean up the specified files.

Claude performs these steps manually (calling individual tools) and uses its intelligence to handle errors, rollbacks, and reporting.

The steps the agent will perform are:

1. Build the solution and run the Unit tests to ensure a clean starting state.
2. For each specified file:
   - If the file is >1000 lines, run PartialSplitter to create partial class files.
   - Build and test to ensure the split didn't break anything or add any new build warnings.
   - For each split file (or the original if not split):
       - Add `#nullable enable` directive if appropriate (based on project/global settings).
       - Run BackingFieldReorderer to fix backing field ordering.
       - Run ReSharper Command Line Tools to apply the "Full Cleanup" profile to the file.
       - Run ReSharper InspectCode and collect warnings.
       - Refactor to fix easy to fix InspectCode warnings and report harder ones, explaining why they were deemed hard.
       - Detect CWP patterns and add `// TODO: Refactor to use CWP` comments in the source code.
       - Run ReSharper Command Line Tools to apply the "Full Cleanup" profile to the file again.
       - Build and test to ensure no new build warnings or test failures.

## Acceptance Criteria

Successfully cleaned files meet:
- ✓ No new build warnings
- ✓ `#nullable enable` directive (if appropriate) - all code should support nullable.
- ✓ Splits >1000 lines into logical partials
- ✓ Backing fields immediately before properties
- ✓ Near-zero ReSharper InspectCode warnings
- ✓ CWP candidates identified with `// TODO: Refactor to use CWP` comments added in source code
- ✓ All tests pass

## Prerequisites

- **ReSharper Command Line Tools**:
  ```powershell
  dotnet tool install -g JetBrains.ReSharper.GlobalTools
  ```
- **.NET 8 SDK** (already required for Terminal.Gui)
- **Git** (for rollback functionality)

## Step Details

### 1. File Splitting (>1000 lines)
- Analyzes class members by naming patterns
- Groups related functionality:
  - `*Mouse*` → ClassName.Mouse.cs
  - `*Keyboard*` → ClassName.Keyboard.cs
  - `*Draw*` → ClassName.Drawing.cs
  - `*Layout*` → ClassName.Layout.cs
  - etc.
- **Preserves**:
  - Base classes and interfaces (in Core file)
  - Namespace-level delegates and types (in Core file)
  - Backing field-property relationships
- **Minimum**: 100 lines per partial (avoids over-splitting)

### 2. ReSharper Full Cleanup
- Applies "Full Cleanup" profile from `Terminal.sln.DotSettings`
- Uses `--include` parameter to target specific files
- Respects all project code style settings
- Command: `jb cleanupcode Terminal.slnx --profile="Full Cleanup" --include="path/to/file.cs" --no-build`

### 3. Backing Field Reordering
- Places backing fields immediately before their properties
- Fixes ReSharper bug RSRP-484963

### 4. Nullable Directive Management
- Adds `#nullable enable` if:
  - File doesn't have it
  - Project doesn't have it globally
  - File doesn't have `#nullable disable`

### 5. CWP Pattern Detection
- Identifies virtual `On*` methods without `Raise*` counterparts
- Identifies events without `Raise*` methods
- **MUST add `// TODO: Refactor to use CWP` comments** in the source code at each location where CWP pattern should be applied:
  - Above events that lack proper `Raise*` methods
  - Above `On*` methods that lack corresponding `Raise*` methods
  - Above direct event invocations (e.g., `Event?.Invoke(...)`) that should use CWP pattern
- Use the Edit tool to add these TODO comments directly to the files
- Format: `// TODO: Refactor to use CWP` (exact format, no variations)

### 6. Warning Verification
- **Build Warnings (HARD RULE)**:
  - Counts compiler warnings before and after cleanup
  - **Attempts to fix** any new build warnings introduced
  - Re-runs cleanup after fixes
  - Rolls back only if unable to fix after multiple attempts
- **InspectCode Warnings (SOFT GOAL)**:
  - Counts ReSharper code quality warnings before and after
  - Reports changes but doesn't fail on increase
  - Goal is to drastically reduce warnings
- Uses `dotnet build` for build warnings
- Uses `jb inspectcode` for ReSharper warnings

### 7. Testing & Validation
- Rebuilds solution
- Runs UnitTestsParallelizable test suite
- Rolls back on failure (except for partial splits)

## ReSharper Command Line Usage

### cleanupcode - Apply Code Formatting

The `--include` parameter **WORKS CORRECTLY** for single-file cleanup:

```powershell
# Cleanup a single file
jb cleanupcode Terminal.slnx `
    --profile="Full Cleanup" `
    --include="Terminal.Gui/Views/TableView/TableView.cs" `
    --no-build `
    --verbosity=WARN
```

**Key Parameters:**
- `--profile="Full Cleanup"` - Uses the profile defined in Terminal.sln.DotSettings
- `--include="path/to/file.cs"` - Relative path from solution root (forward slashes recommended)
- `--no-build` - Skips building before cleanup (recommended, saves time)
- `--verbosity=WARN` - Recommended (ERROR=minimal, WARN=moderate, VERBOSE=detailed, use VERBOSE for full debugging)

**Verified Behavior (Tested 2026-01-13):**
- ✅ Only the specified file is modified (--include works correctly)
- ⚠️ May display exceptions in output, but cleanup completes successfully

### inspectcode - Identify Code Issues

```powershell
# Inspect a single file for warnings
jb inspectcode Terminal.slnx `
    --output="inspect-report.xml" `
    --include="Terminal.Gui/Views/TableView/TableView.cs" `
    --severity=WARNING `
    --no-build `
    --format=Xml
```

**Key Parameters:**
- `--output="file.xml"` - Path to output XML report
- `--include="path/to/file.cs"` - Filter to specific file (same format as cleanupcode)
- `--severity=WARNING` - Minimum severity level (INFO, HINT, SUGGESTION, WARNING, ERROR)
- `--format=Xml` - Output format (Xml, Html, Text, Sarif)

### Known Limitations

- **ReSharper exceptions**: May display exceptions during execution but completes successfully (exit code 0). Verify changes with `git diff`.
- **Performance**: First run takes 30-60 seconds (caching), subsequent runs 15-30 seconds.
- **Partial splitting**: Heuristic-based grouping may not always be perfect. Review generated partials before committing.

## Troubleshooting

### "Build failed after cleanup"
- ReSharper may have introduced issues
- Review changes with `git diff` and fix manually
- Roll back with `git checkout -- <file>`

### "Tests failed"
- Review test failures to determine cause
- May need test updates for structural changes
- Roll back changes if unable to fix

### "Hundreds of files modified"
- ReSharper CLI processed entire solution (forgot `--include`)
- Use `git checkout -- .` to revert all changes
- Re-run with correct `--include` parameter

### "Partial split created confusing files"
- Adjust grouping heuristics in PartialSplitter/Program.cs
- Delete partials and restore from `.backup` file
- Skip splitting for files that don't split well semantically

## Best Practices

1. **Test on small files first** - Verify behavior before processing large files
2. **Review all changes** - Use `git diff` before committing
3. **Work on branches** - Create `cleanup/batch-1` branches
4. **Build and test frequently** - Verify after each major step
5. **Commit incrementally** - Commit after each successful file cleanup
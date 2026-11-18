# Code Cleanup Agent for Terminal.Gui
# WARNING: ReSharper cleanupcode may process entire solution regardless of --include parameter
# Recommend running ReSharper cleanup manually in IDE or running on whole solution once

param (
    [Parameter(Mandatory = $true)]
    [string[]]$Files,

    [Parameter(Mandatory = $false)]
    [switch]$SkipReSharper,

    [Parameter(Mandatory = $false)]
    [switch]$SkipTests,

    [Parameter(Mandatory = $false)]
    [switch]$SkipPartialSplit
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$StartTime = Get-Date

Write-Host @"
===============================================
  Code Cleanup Agent for Terminal.Gui
===============================================
  Files to process: $($Files.Count)
  Skip ReSharper: $SkipReSharper
  Skip tests: $SkipTests
  Skip partial split: $SkipPartialSplit
===============================================
"@ -ForegroundColor Cyan

# Statistics
$Stats = @{
    TotalFiles = $Files.Count
    SuccessFiles = @()
    FailedFiles = @()
    FilesSplit = @()
    NullableAdded = 0
    CWPTodosAdded = 0
    BuildWarningsBefore = 0
    BuildWarningsAfter = 0
    InspectWarningsBefore = 0
    InspectWarningsAfter = 0
}

function Write-Status {
    param([string]$Message, [string]$Color = "Cyan")
    Write-Host "  [$((Get-Date).ToString('HH:mm:ss'))] $Message" -ForegroundColor $Color
}

function Test-NeedsPartialSplit {
    param([string]$FilePath)

    if ($SkipPartialSplit) { return $false }

    $lineCount = (Get-Content $FilePath).Count
    return $lineCount -gt 1000
}

function Invoke-PartialSplit {
    param([string]$FilePath)

    $lineCount = (Get-Content $FilePath).Count
    Write-Status "File has $lineCount lines, splitting into partials..." "Yellow"

    & "$RepoRoot\Scripts\PartialSplitter\bin\Debug\net8.0\PartialSplitter.exe" $FilePath 2>&1 | Out-Host

    if ($LASTEXITCODE -ne 0) {
        Write-Status "Partial split failed" "Red"
        return $false
    }

    return $true
}

function Invoke-ReSharperCleanup {
    param([string]$FilePath)

    if ($SkipReSharper) {
        Write-Status "Skipping ReSharper (--SkipReSharper flag)" "Yellow"
        return $true
    }

    # Convert to relative path for ReSharper
    $relativePath = (Resolve-Path -Relative $FilePath).TrimStart(".\").Replace("\", "/")
    Write-Status "Running ReSharper Full Cleanup on: $relativePath" "Yellow"

    # Run cleanup on specific file using --include parameter
    # Note: May show exceptions in output, but cleanup still completes successfully
    jb cleanupcode "$RepoRoot\Terminal.slnx" `
        --profile="Full Cleanup" `
        --include="$relativePath" `
        --no-build `
        --verbosity=WARN 2>&1 | Out-Host

    if ($LASTEXITCODE -ne 0) {
        Write-Status "ReSharper cleanup failed" "Red"
        return $false
    }

    return $true
}

function Get-ReSharperWarningCount {
    param([string[]]$FilePaths)

    $tempXml = Join-Path $env:TEMP "inspect-$(New-Guid).xml"

    # Build include list (semicolon-separated relative paths)
    $includeList = ($FilePaths | ForEach-Object {
        (Resolve-Path -Relative $_).TrimStart(".\").Replace("\", "/")
    }) -join ";"

    Write-Status "Running ReSharper InspectCode to count warnings..."

    jb inspectcode "$RepoRoot\Terminal.slnx" `
        --output="$tempXml" `
        --include="$includeList" `
        --severity=WARNING `
        --no-build `
        --format=Xml `
        --verbosity=ERROR 2>&1 | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Status "InspectCode failed" "Red"
        return -1
    }

    # Parse XML and count warnings
    [xml]$report = Get-Content $tempXml -Raw -ErrorAction SilentlyContinue
    if (-not $report) {
        Write-Status "Failed to parse InspectCode report" "Red"
        return -1
    }

    $warningCount = 0
    if ($report.Report.Issues.Project.Issue) {
        $warningCount = @($report.Report.Issues.Project.Issue).Count
    }

    Remove-Item $tempXml -ErrorAction SilentlyContinue
    return $warningCount
}

function Get-BuildWarnings {
    param([switch]$CountOnly)

    Write-Status "Analyzing build warnings..."

    $buildOutput = dotnet build "$RepoRoot\Terminal.slnx" --no-restore --configuration Debug --verbosity normal 2>&1 | Out-String

    if ($LASTEXITCODE -ne 0) {
        Write-Status "Build failed" "Red"
        return @{ Count = -1; Warnings = @(); Output = $buildOutput }
    }

    # Extract warning lines (full warning messages)
    $warningLines = $buildOutput -split "`r?`n" | Where-Object { $_ -match 'warning CS\d+' }
    $warningCount = $warningLines.Count

    Write-Status "Build warnings: $warningCount" $(if ($warningCount -eq 0) { "Green" } else { "Yellow" })

    if ($CountOnly) {
        return @{ Count = $warningCount; Warnings = @(); Output = "" }
    }

    return @{ Count = $warningCount; Warnings = $warningLines; Output = $buildOutput }
}

function Get-BuildWarningCount {
    $result = Get-BuildWarnings -CountOnly
    return $result.Count
}

function Invoke-BackingFieldReorder {
    param([string]$FilePath)

    Write-Status "Reordering backing fields..."

    & "$RepoRoot\Scripts\BackingFieldReorderer\bin\Debug\net8.0\BackingFieldReorderer.exe" $FilePath 2>&1 | Out-Null

    return $LASTEXITCODE -eq 0
}

function Invoke-EnableNullable {
    param([string]$FilePath)

    $content = Get-Content $FilePath -Raw

    # Check if already has nullable enable
    if ($content -match '#nullable\s+enable') {
        return $false
    }

    # Check if project has nullable enabled globally
    $projectFile = Get-Item "$RepoRoot\Terminal.Gui\Terminal.Gui.csproj" -ErrorAction SilentlyContinue
    if ($projectFile) {
        $projectContent = Get-Content $projectFile.FullName -Raw
        if ($projectContent -match '<Nullable>enable</Nullable>') {
            # Project has nullable enabled globally - directive not needed
            return $false
        }
    }

    # Check if file has nullable disable (don't override it)
    if ($content -match '#nullable\s+disable') {
        return $false
    }

    Write-Status "Adding #nullable enable directive..."

    $lines = $content -split "`r?`n"
    $insertIndex = 0

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^using\s+') {
            $insertIndex = $i + 1
        }
        if ($lines[$i] -match '^namespace\s+') {
            $insertIndex = $i + 1
            break
        }
    }

    $newLines = @()
    $newLines += $lines[0..$insertIndex]
    $newLines += ""
    $newLines += "#nullable enable"
    if ($insertIndex + 1 -lt $lines.Count) {
        $newLines += $lines[($insertIndex + 1)..($lines.Count - 1)]
    }

    $newContent = $newLines -join "`r`n"
    Set-Content $FilePath -Value $newContent -NoNewline

    return $true
}

function Add-CWPTodoComments {
    param([string]$FilePath)

    $content = Get-Content $FilePath -Raw
    $lines = $content -split "`r?`n"
    $modified = $false
    $todosAdded = 0

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Detect virtual methods that could use CWP
        if ($line -match '^\s*(public|protected|internal)\s+virtual\s+\w+\s+On\w+\s*\(') {
            # Check if TODO comment already exists in previous lines
            $hasTodo = $false
            for ($j = [Math]::Max(0, $i - 3); $j -lt $i; $j++) {
                if ($lines[$j] -match 'TODO.*CWP') {
                    $hasTodo = $true
                    break
                }
            }

            if (-not $hasTodo) {
                $indent = ($line -replace '^(\s*).*', '$1')
                $lines[$i] = "$indent// TODO: Consider refactoring to use Cancellable Work Pattern (CWP)`r`n$line"
                $modified = $true
                $todosAdded++
            }
        }

        # Detect event declarations that might need CWP
        if ($line -match '^\s*public\s+event\s+\w+') {
            $eventName = if ($line -match 'event\s+\w+\s+(\w+)') { $Matches[1] } else { $null }
            if ($eventName) {
                $raiseMethod = "Raise$eventName"
                $hasRaiseMethod = $content -match "function\s+$raiseMethod\s*\(|void\s+$raiseMethod\s*\("

                if (-not $hasRaiseMethod) {
                    $hasTodo = $false
                    for ($j = [Math]::Max(0, $i - 3); $j -lt $i; $j++) {
                        if ($lines[$j] -match 'TODO.*CWP') {
                            $hasTodo = $true
                            break
                        }
                    }

                    if (-not $hasTodo) {
                        $indent = ($line -replace '^(\s*).*', '$1')
                        $lines[$i] = "$indent// TODO: Consider adding Raise$eventName method using CWP pattern`r`n$line"
                        $modified = $true
                        $todosAdded++
                    }
                }
            }
        }
    }

    if ($modified) {
        Write-Status "Added $todosAdded CWP TODO comments" "Green"
        $newContent = $lines -join "`r`n"
        Set-Content $FilePath -Value $newContent -NoNewline
        return $todosAdded
    }

    return 0
}

function Invoke-RebuildSolution {
    Write-Status "Rebuilding solution..."

    dotnet build "$RepoRoot\Terminal.slnx" --no-restore --configuration Debug --verbosity quiet 2>&1 | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Status "Build failed" "Red"
        return $false
    }

    return $true
}

function Invoke-RunTests {
    if ($SkipTests) {
        Write-Status "Skipping tests (--SkipTests flag)" "Yellow"
        return $true
    }

    Write-Status "Running tests (UnitTestsParallelizable)..."

    dotnet test "$RepoRoot\Tests\UnitTestsParallelizable" `
        --no-build `
        --verbosity minimal `
        --logger "console;verbosity=minimal" 2>&1 | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Status "Tests failed" "Red"
        return $false
    }

    Write-Status "Tests passed" "Green"
    return $true
}

function Invoke-RollbackFile {
    param([string]$FilePath)

    Write-Status "Rolling back changes..." "Yellow"
    git checkout HEAD -- $FilePath 2>&1 | Out-Null

    return $LASTEXITCODE -eq 0
}

function Process-CleanupFile {
    param([string]$FilePath)

    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Processing: $FilePath" -ForegroundColor White
    Write-Host "========================================" -ForegroundColor Cyan

    $wasSplit = $false
    $filesToProcess = @($FilePath)

    try {
        # Step 1: Check file length & split if needed
        if (Test-NeedsPartialSplit -FilePath $FilePath) {
            $wasSplit = Invoke-PartialSplit -FilePath $FilePath

            if ($wasSplit) {
                $Stats.FilesSplit += $FilePath
                Write-Status "File split successfully." "Green"

                # Get all partial files created
                $directory = Split-Path $FilePath -Parent
                $baseName = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
                $partialFiles = Get-ChildItem "$directory\$baseName*.cs" -Exclude "*.backup"
                $filesToProcess = $partialFiles | ForEach-Object { $_.FullName }

                Write-Status "Created $($partialFiles.Count) partial files. Processing each through cleanup..." "Cyan"
            }
        }

        # Capture baseline warning counts
        Write-Host "`n--- Baseline Warning Counts ---" -ForegroundColor Cyan
        $buildWarningsBefore = Get-BuildWarningCount
        $inspectWarningsBefore = Get-ReSharperWarningCount -FilePaths $filesToProcess
        Write-Status "Build warnings (before): $buildWarningsBefore" "Cyan"
        Write-Status "InspectCode warnings (before): $inspectWarningsBefore" "Cyan"

        if ($wasSplit) {
            # Process each partial file through cleanup steps 2-5
                foreach ($partial in $partialFiles) {
                    Write-Status "Processing partial: $($partial.Name)" "Cyan"

                    # Step 2: Run ReSharper cleanup
                    if (-not (Invoke-ReSharperCleanup -FilePath $partial.FullName)) {
                        Write-Status "ReSharper cleanup failed for $($partial.Name)" "Red"
                        return $false
                    }

                    # Step 3: Fix backing field placement
                    if (-not (Invoke-BackingFieldReorder -FilePath $partial.FullName)) {
                        Write-Status "Backing field reordering failed for $($partial.Name)" "Red"
                        return $false
                    }

                    # Step 4: Ensure #nullable enable (if needed)
                    $nullableAdded = Invoke-EnableNullable -FilePath $partial.FullName
                    if ($nullableAdded) {
                        $Stats.NullableAdded++
                    }

                    # Step 5: Add CWP TODO comments
                    $cwpTodos = Add-CWPTodoComments -FilePath $partial.FullName
                    $Stats.CWPTodosAdded += $cwpTodos
                }

            Write-Status "Completed cleanup of all $($partialFiles.Count) partial files" "Green"

            # Continue to rebuild and test (Steps 6-7)
        }
        else {
            # No split needed - process single file through cleanup steps 2-5

            # Step 2: Run ReSharper cleanup on specific file
            if (-not (Invoke-ReSharperCleanup -FilePath $FilePath)) {
                return $false
            }

            # Step 3: Fix backing field placement
            if (-not (Invoke-BackingFieldReorder -FilePath $FilePath)) {
                Write-Status "Backing field reordering failed" "Red"
                return $false
            }

            # Step 4: Ensure #nullable enable (if needed)
            $nullableAdded = Invoke-EnableNullable -FilePath $FilePath
            if ($nullableAdded) {
                $Stats.NullableAdded++
            }

            # Step 5: Add CWP TODO comments
            $cwpTodos = Add-CWPTodoComments -FilePath $FilePath
            $Stats.CWPTodosAdded += $cwpTodos
        }

        # Verify warning counts after cleanup
        Write-Host "`n--- Post-Cleanup Warning Counts ---" -ForegroundColor Cyan
        $buildResult = Get-BuildWarnings
        $buildWarningsAfter = $buildResult.Count
        $inspectWarningsAfter = Get-ReSharperWarningCount -FilePaths $filesToProcess
        Write-Status "Build warnings (after): $buildWarningsAfter" "Cyan"
        Write-Status "InspectCode warnings (after): $inspectWarningsAfter" "Cyan"

        # HARD RULE: No new build warnings - attempt to fix if found
        $buildWarningsDelta = $buildWarningsAfter - $buildWarningsBefore
        $fixAttempts = 0
        $maxFixAttempts = 2

        while ($buildWarningsDelta -gt 0 -and $fixAttempts -lt $maxFixAttempts) {
            $fixAttempts++
            Write-Status "Cleanup introduced $buildWarningsDelta NEW BUILD WARNINGS - attempting fix ($fixAttempts/$maxFixAttempts)..." "Yellow"

            # Output the warnings for diagnosis
            Write-Host "`n--- New Build Warnings ---" -ForegroundColor Yellow
            foreach ($warning in $buildResult.Warnings) {
                Write-Host "  $warning" -ForegroundColor Yellow
            }
            Write-Host ""

            # Attempt fix: Re-run ReSharper cleanup (it sometimes fixes its own issues)
            Write-Status "Re-running ReSharper cleanup to fix warnings..."
            foreach ($file in $filesToProcess) {
                Invoke-ReSharperCleanup -FilePath $file | Out-Null
            }

            # Re-check warnings
            $buildResult = Get-BuildWarnings
            $buildWarningsAfter = $buildResult.Count
            $buildWarningsDelta = $buildWarningsAfter - $buildWarningsBefore
        }

        # Track statistics
        $Stats.BuildWarningsBefore += $buildWarningsBefore
        $Stats.BuildWarningsAfter += $buildWarningsAfter
        $Stats.InspectWarningsBefore += $inspectWarningsBefore
        $Stats.InspectWarningsAfter += $inspectWarningsAfter

        # Final check on build warnings
        if ($buildWarningsDelta -gt 0) {
            Write-Status "FAILED: Unable to fix $buildWarningsDelta NEW BUILD WARNINGS after $maxFixAttempts attempts" "Red"
            Write-Host "`n--- Unresolved Build Warnings ---" -ForegroundColor Red
            foreach ($warning in $buildResult.Warnings) {
                Write-Host "  $warning" -ForegroundColor Red
            }
            Write-Host ""
            Invoke-RollbackFile -FilePath $FilePath
            return $false
        }
        if ($buildWarningsDelta -lt 0) {
            Write-Status "✓ Fixed $([Math]::Abs($buildWarningsDelta)) build warnings" "Green"
        }

        # SOFT GOAL: Report InspectCode warning changes
        $inspectWarningsDelta = $inspectWarningsAfter - $inspectWarningsBefore
        if ($inspectWarningsDelta -gt 0) {
            Write-Status "⚠ InspectCode warnings increased by $inspectWarningsDelta (review recommended)" "Yellow"
        } elseif ($inspectWarningsDelta -lt 0) {
            Write-Status "✓ Fixed $([Math]::Abs($inspectWarningsDelta)) InspectCode warnings" "Green"
        } else {
            Write-Status "InspectCode warnings unchanged" "Cyan"
        }

        # Step 6: Rebuild solution
        if (-not (Invoke-RebuildSolution)) {
            Invoke-RollbackFile -FilePath $FilePath
            return $false
        }

        # Step 7: Run tests
        if (-not (Invoke-RunTests)) {
            if (-not $wasSplit) {
                Invoke-RollbackFile -FilePath $FilePath
                return $false
            } else {
                Write-Status "Tests failed after split, keeping partials for manual review" "Yellow"
            }
        }

        Write-Status "✓ Success!" "Green"
        return $true
    }
    catch {
        Write-Host "ERROR: $_" -ForegroundColor Red
        Write-Host $_.ScriptStackTrace -ForegroundColor Red
        Invoke-RollbackFile -FilePath $FilePath
        return $false
    }
}

# Main Execution
foreach ($file in $Files) {
    $filePath = if ([System.IO.Path]::IsPathRooted($file)) {
        $file
    } else {
        Join-Path $RepoRoot $file
    }

    if (-not (Test-Path $filePath)) {
        Write-Host "File not found: $filePath" -ForegroundColor Red
        $Stats.FailedFiles += @{ Path = $filePath; Reason = "File not found" }
        continue
    }

    $success = Process-CleanupFile -FilePath $filePath

    if ($success) {
        $Stats.SuccessFiles += $filePath
    } else {
        $Stats.FailedFiles += @{ Path = $filePath; Reason = "Cleanup failed" }
    }
}

# Summary Report
$endTime = Get-Date
$duration = $endTime - $StartTime

Write-Host @"

========== CODE CLEANUP REPORT ==========
Session Start: $($StartTime.ToString('yyyy-MM-dd HH:mm:ss'))
Session End:   $($endTime.ToString('yyyy-MM-dd HH:mm:ss'))
Total Duration: $($duration.ToString('hh\:mm\:ss'))

FILES PROCESSED: $($Stats.TotalFiles)
  ✓ Success: $($Stats.SuccessFiles.Count)
  ✗ Failed:  $($Stats.FailedFiles.Count)

FILES SPLIT INTO PARTIALS: $($Stats.FilesSplit.Count)

NULLABLE DIRECTIVES ADDED: $($Stats.NullableAdded)
CWP TODO COMMENTS ADDED: $($Stats.CWPTodosAdded)

WARNINGS:
  Build Warnings (Before):    $($Stats.BuildWarningsBefore)
  Build Warnings (After):     $($Stats.BuildWarningsAfter)
  Build Warnings (Delta):     $(if ($Stats.BuildWarningsAfter -lt $Stats.BuildWarningsBefore) { "✓ Fixed $($Stats.BuildWarningsBefore - $Stats.BuildWarningsAfter)" } elseif ($Stats.BuildWarningsAfter -gt $Stats.BuildWarningsBefore) { "✗ Added $($Stats.BuildWarningsAfter - $Stats.BuildWarningsBefore)" } else { "Unchanged" })

  InspectCode Warnings (Before): $($Stats.InspectWarningsBefore)
  InspectCode Warnings (After):  $($Stats.InspectWarningsAfter)
  InspectCode Warnings (Delta):  $(if ($Stats.InspectWarningsAfter -lt $Stats.InspectWarningsBefore) { "✓ Fixed $($Stats.InspectWarningsBefore - $Stats.InspectWarningsAfter)" } elseif ($Stats.InspectWarningsAfter -gt $Stats.InspectWarningsBefore) { "⚠ Added $($Stats.InspectWarningsAfter - $Stats.InspectWarningsBefore)" } else { "Unchanged" })

FAILED FILES:
$(if ($Stats.FailedFiles.Count -gt 0) { $Stats.FailedFiles | ForEach-Object { "  - $($_.Path): $($_.Reason)" } } else { "  None" })

=========================================
"@ -ForegroundColor Cyan

if ($Stats.FailedFiles.Count -gt 0) { exit 1 } else { exit 0 }

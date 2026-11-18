<#
.SYNOPSIS
    Lints the AI agent instruction files for staleness and contradictions.

.DESCRIPTION
    The agent instruction files (CLAUDE.md, AGENTS.md, .cursorrules, etc.) are
    maintained in parallel and drift from the repo state and from each other.
    This script fails CI when known rot patterns reappear:

      1. References to test projects that no longer exist (bare Tests/UnitTests)
      2. Machine-local absolute paths (D:\..., C:\Users\..., /home/...)
      3. camelCase local-function guidance (the .editorconfig rule is PascalCase)
      4. Deprecated xUnit v2 test filter syntax (--filter "FullyQualifiedName~")
      5. .NET SDK version claims that do not match global.json

.EXAMPLE
    pwsh -File Scripts/lint-agent-docs.ps1
#>

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

# The set of files that instruct AI agents. Keep in sync with the files listed
# at the top of CLAUDE.md and AGENTS.md.
$agentDocPatterns = @(
    'CLAUDE.md'
    'AGENTS.md'
    'CONTRIBUTING.md'
    'ai-v2-primer.md'
    'llms.txt'
    '.cursorrules'
    '.windsurfrules'
    '.aider.md'
    '.github/copilot-instructions.md'
)

$files = [System.Collections.Generic.List[string]]::new()

foreach ($pattern in $agentDocPatterns)
{
    $path = Join-Path $repoRoot $pattern

    if (Test-Path $path)
    {
        $files.Add($path)
    }
}

# All markdown under .claude/ (rules, tasks, workflows, cookbook).
Get-ChildItem -Path (Join-Path $repoRoot '.claude') -Filter '*.md' -Recurse -File |
    ForEach-Object { $files.Add($_.FullName) }

$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure
{
    param ([string] $File, [int] $Line, [string] $Rule, [string] $Text)

    $relative = [System.IO.Path]::GetRelativePath($repoRoot, $File)
    $failures.Add("${relative}:${Line}: [$Rule] $Text")
}

# Rule 5 needs the pinned SDK version.
$globalJson = Get-Content (Join-Path $repoRoot 'global.json') -Raw | ConvertFrom-Json
$sdkVersion = $globalJson.sdk.version
$sdkMajor = $sdkVersion.Split('.')[0]

foreach ($file in $files)
{
    $lines = Get-Content $file
    $lineNumber = 0

    foreach ($line in $lines)
    {
        $lineNumber++

        # Rule 1: stale test project names. Valid projects: UnitTestsParallelizable,
        # UnitTests.NonParallelizable, UnitTests.Legacy.
        if ($line -match 'Tests/UnitTests(?!Parallelizable|\.NonParallelizable|\.Legacy)')
        {
            Add-Failure $file $lineNumber 'stale-test-project' "bare 'Tests/UnitTests' no longer exists; use Tests/UnitTestsParallelizable or Tests/UnitTests.NonParallelizable"
        }

        # Rule 2: machine-local absolute paths.
        if ($line -match '[A-Za-z]:\\' -or $line -match '(?<![\w.])/(home|Users)/')
        {
            Add-Failure $file $lineNumber 'local-path' "machine-local absolute path: $($line.Trim())"
        }

        # Rule 3: camelCase local-function guidance contradicts .editorconfig.
        if ($line -match '(?i)local\s+functions?' -and $line -match '(?i)camelCase')
        {
            Add-Failure $file $lineNumber 'local-function-casing' "local functions are PascalCase (see .editorconfig local_functions_rule)"
        }

        # Rule 4: deprecated filter syntax. Allowed only on lines that warn it
        # does not work (AGENTS.md documents the migration).
        if ($line -match 'FullyQualifiedName~' -and $line -notmatch '(?i)not\W*work')
        {
            Add-Failure $file $lineNumber 'deprecated-filter' "use xUnit v3 MTP syntax: --filter-method / --filter-class"
        }

        # Rule 5: SDK version claims must match global.json.
        if ($line -match '(?i)\.NET SDK\D*(\d+\.\d+(\.\d+)?)')
        {
            $claimed = $Matches[1]

            if (-not $claimed.StartsWith($sdkMajor))
            {
                Add-Failure $file $lineNumber 'sdk-version' "claims .NET SDK $claimed but global.json pins $sdkVersion"
            }
        }

        # Rule 6: target framework moniker must match global.json's SDK major.
        # Catches stale `net8.0`-style TFMs left behind when the project moved on.
        if ($line -match 'net(\d+)\.0(?![\d.])')
        {
            $tfmMajor = $Matches[1]

            if ($tfmMajor -ne $sdkMajor)
            {
                Add-Failure $file $lineNumber 'stale-tfm' "references net$tfmMajor.0 but global.json pins .NET $sdkMajor (expected net$sdkMajor.0)"
            }
        }
    }
}

if ($failures.Count -gt 0)
{
    Write-Host "Agent doc lint FAILED ($($failures.Count) issue(s)):" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host ''
    Write-Host 'These files instruct AI agents; stale content causes agents to run broken commands.'
    Write-Host 'Fix the flagged lines (see Scripts/lint-agent-docs.ps1 for rule details).'

    exit 1
}

Write-Host "Agent doc lint passed ($($files.Count) files checked)." -ForegroundColor Green
exit 0

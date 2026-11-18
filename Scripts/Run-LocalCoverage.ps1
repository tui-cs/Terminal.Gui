# ------------------------------------------------------------
# Run-LocalCoverage.ps1
# Local-only: Unit + Parallel + Integration tests
# Quiet, merged coverage in /Tests
# ------------------------------------------------------------

# 1. Define paths
$testDir    = Join-Path $PWD "Tests"
$covDir     = Join-Path $testDir "coverage"
$reportDir  = Join-Path $testDir "report"
$resultsDir = Join-Path $testDir "TestResults"
$mergedFile = Join-Path $covDir "coverage.merged.cobertura.xml"

# 2. Clean old results - INCLUDING TestResults directory
Write-Host "Cleaning old coverage files and test results..."
Remove-Item -Recurse -Force $covDir, $reportDir, $resultsDir -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $covDir, $reportDir -Force | Out-Null

dotnet build --configuration Debug --no-restore

# ------------------------------------------------------------
# 3. Run UNIT TESTS (non-parallel)
# ------------------------------------------------------------
Write-Host "`nRunning UnitTests (quiet)..."
dotnet test Tests/UnitTests `
  --no-build `
  --verbosity minimal `
  --collect:"XPlat Code Coverage" `
  --settings Tests/UnitTests/runsettings.coverage.xml

# ------------------------------------------------------------
# 4. Run UNIT TESTS (parallel)
# ------------------------------------------------------------
Write-Host "`nRunning UnitTestsParallelizable (quiet)..."
dotnet test Tests/UnitTestsParallelizable `
  --no-build `
  --verbosity minimal `
  --collect:"XPlat Code Coverage" `
  --settings Tests/UnitTestsParallelizable/runsettings.coverage.xml

# ------------------------------------------------------------
# 5. Run INTEGRATION TESTS
# ------------------------------------------------------------
Write-Host "`nRunning IntegrationTests (quiet)..."
dotnet test Tests/IntegrationTests `
  --no-build `
  --verbosity minimal `
  --collect:"XPlat Code Coverage" `
  --settings Tests/IntegrationTests/runsettings.coverage.xml

# ------------------------------------------------------------
# 6. Find ALL coverage files (from all 3 projects) - NOW SCOPED TO Tests/TestResults
# ------------------------------------------------------------
Write-Host "`nCollecting coverage files..."
$covFiles = Get-ChildItem -Path $resultsDir -Recurse -Filter coverage.cobertura.xml -File -ErrorAction SilentlyContinue

if (-not $covFiles) {
    Write-Error "No coverage files found in $resultsDir. Did all tests run successfully?"
    exit 1
}

# ------------------------------------------------------------
# 7. Move to Tests/coverage
# ------------------------------------------------------------
Write-Host "Moving $($covFiles.Count) coverage file(s) to $covDir..."
$fileIndex = 1
foreach ($f in $covFiles) {
    $destFile = Join-Path $covDir "coverage.$fileIndex.cobertura.xml"
    Copy-Item $f.FullName -Destination $destFile -Force
    $fileIndex++
}

# ------------------------------------------------------------
# 8. Merge into one file
# ------------------------------------------------------------
Write-Host "Merging coverage from all test projects..."
dotnet-coverage merge `
    -o $mergedFile `
    -f cobertura `
    "$covDir\*.cobertura.xml"

# ------------------------------------------------------------
# 9. Generate HTML + text report
# ------------------------------------------------------------
Write-Host "Generating final HTML report..."
reportgenerator `
    -reports:$mergedFile `
    -targetdir:$reportDir `
    -reporttypes:"Html;TextSummary"

# ------------------------------------------------------------
# 10. Show summary + open report
# ------------------------------------------------------------
Write-Host "`n=== Final Coverage Summary (Unit + Integration) ==="
Get-Content "$reportDir\Summary.txt"

$indexHtml = Join-Path $reportDir "index.html"
if (Test-Path $indexHtml) {
    Write-Host "Opening report in browser..."
    Start-Process $indexHtml
} else {
    Write-Warning "HTML report not found at $indexHtml"
}

Write-Host "`nCoverage artifacts:"
Write-Host "  - Merged coverage: $mergedFile"
Write-Host "  - HTML report:     $reportDir\index.html"
Write-Host "  - Test results:    $resultsDir"
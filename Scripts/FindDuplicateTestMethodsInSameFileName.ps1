# FindDuplicateTestMethodsInSameFileName.ps1
param (
    [string]$solutionPath = ".\Tests"
)

# Set the base path for relative paths (current directory when script is run)
$basePath = Get-Location

# Define projects to ignore (add your project names or path patterns here)
$ignoreProjects = @(
    "StressTests"
    # Add more as needed, e.g., "Tests/SubFolder/OldProject"
)

# Function to extract method names from a C# file
function Get-TestMethodNames {
    param ($filePath)
    $content = Get-Content -Path $filePath -Raw
    $testMethods = @()

    # Match test attributes and capture method names with flexible spacing/comments
    $methodPattern = '(?s)(\[TestMethod\]|\[Test\]|\[Fact\]|\[Theory\])\s*[\s\S]*?public\s+(?:void|Task)\s+(\w+)\s*\('
    $methods = [regex]::Matches($content, $methodPattern)

    foreach ($match in $methods) {
        $methodName = $match.Groups[2].Value  # Group 2 is the method name
        if ($methodName) {  # Ensure we only add non-empty method names
            $testMethods += $methodName
        }
    }
    return $testMethods
}

# Collect all test files
$testFiles = Get-ChildItem -Path $solutionPath -Recurse -Include *.cs | 
             Where-Object { $_.FullName -match "Tests" -or $_.FullName -match "Test" }

# Group files by filename
$fileGroups = $testFiles | Group-Object -Property Name

# Dictionary to track method names and their locations, scoped to same filenames
$duplicates = @{}

foreach ($group in $fileGroups) {
    if ($group.Count -gt 1) { # Only process files that exist in multiple locations
        $fileName = $group.Name
        $methodMap = @{} # Track methods for this specific filename

        foreach ($file in $group.Group) {
            # Skip files in ignored projects
            $skipFile = $false
            foreach ($ignore in $ignoreProjects) {
                if ($file.FullName -like "*$ignore*") {
                    $skipFile = $true
                    break
                }
            }
            if ($skipFile) { continue }

            $methods = Get-TestMethodNames -filePath $file.FullName
            foreach ($method in $methods) {
                if ($methodMap.ContainsKey($method)) {
                    # Duplicate found for this method in the same filename
                    if (-not $duplicates.ContainsKey($method)) {
                        $duplicates[$method] = @($methodMap[$method])
                    }
                    $duplicates[$method] += $file.FullName
                } else {
                    $methodMap[$method] = $file.FullName
                }
            }
        }
    }
}

# Output results with relative paths
if ($duplicates.Count -eq 0) {
    Write-Host "No duplicate test method names found in files with the same name across projects." -ForegroundColor Green
} else {
    Write-Host "Duplicate test method names found in files with the same name across projects:" -ForegroundColor Yellow
    foreach ($dup in $duplicates.Keys) {
        Write-Host "Method: $dup" -ForegroundColor Cyan
        foreach ($fullPath in $duplicates[$dup]) {
            $relativePath = Resolve-Path -Path $fullPath -Relative -RelativeBasePath $basePath
            Write-Host "  - $relativePath" -ForegroundColor White
        }
    }
    # Display total number of duplicate methods
    Write-Host "Total number of duplicate methods: $($duplicates.Count)" -ForegroundColor Magenta
    # Fail the pipeline by setting a non-zero exit code
    exit 1
}
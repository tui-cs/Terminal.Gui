# Define the root directory containing test projects
$testsDir = "./Tests"

# Get all subfolders in the ./Tests directory
$subfolders = Get-ChildItem -Directory $testsDir

# Initialize a hashtable to track method names and their associated subfolders
$methodMap = @{}

# Iterate through each subfolder
foreach ($subfolder in $subfolders) {
    $subfolderName = $subfolder.Name
    
    # Run dotnet test --list-tests to get the list of tests in the subfolder
    $output = dotnet test $subfolder.FullName --list-tests | Out-String
    
    # Split the output into lines and filter for lines containing a dot (indicative of test names)
    $testLines = $output -split "`n" | Where-Object { $_ -match "\." }
    
    # Process each test line to extract the method name
    foreach ($testLine in $testLines) {
        $trimmed = $testLine.Trim()
        $parts = $trimmed -split "\."
        $lastPart = $parts[-1]
        
        # Handle parameterized tests by extracting the method name before any parentheses
        if ($lastPart -match "\(") {
            $methodName = $lastPart.Substring(0, $lastPart.IndexOf("("))
        } else {
            $methodName = $lastPart
        }
        
        # Update the hashtable with the method name and subfolder
        if ($methodMap.ContainsKey($methodName)) {
            # Add the subfolder only if it’s not already listed for this method name
            if (-not ($methodMap[$methodName] -contains $subfolderName)) {
                $methodMap[$methodName] += $subfolderName
            }
        } else {
            $methodMap[$methodName] = @($subfolderName)
        }
    }
}

# Identify and display duplicated test method names
foreach ($entry in $methodMap.GetEnumerator()) {
    if ($entry.Value.Count -gt 1) {
        Write-Output "Duplicated test: $($entry.Key) in folders: $($entry.Value -join ', ')"
    }
}

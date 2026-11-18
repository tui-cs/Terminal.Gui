#!/bin/bash

# This script runs the tests in a loop until they all pass.
# It will exit if any test run fails.

dotnet build -c Debug

iterationCount=1

while true; do
    echo "Starting iteration $iterationCount..."

    dotnet test Tests/UnitTests --no-build --diag:TestResults/UnitTests.log -- xunit.stopOnFail=true
    if [ $? -ne 0 ]; then
        echo "UnitTests run failed on iteration $iterationCount. Exiting."
        exit 1
    fi

    dotnet test Tests/UnitTestsParallelizable --no-build --diag:TestResults/UnitTestsParallelizable.log -- xunit.stopOnFail=true
    if [ $? -ne 0 ]; then
        echo "UnitTestsParallelizable run failed on iteration $iterationCount. Exiting."
        exit 1
    fi

    # Clean up the log files
    rm log*

    # Increment the iteration counter
    ((iterationCount++))
done
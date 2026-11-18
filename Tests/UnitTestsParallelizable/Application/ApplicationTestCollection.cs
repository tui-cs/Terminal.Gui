using Xunit;

namespace ApplicationTests;

/// <summary>
///     Collection definition for Application tests.
///     Tests in this collection run sequentially to avoid conflicts with Application lifecycle and state.
/// </summary>
[CollectionDefinition("Application Tests")]
public class ApplicationTestCollection
{
    // This class is never instantiated - it's just a marker for xUnit
}

/// <summary>
///     Collection definition for Application Timer Related tests.
///     Tests in this collection run sequentially to avoid conflicts with Application lifecycle and state.
/// </summary>
[CollectionDefinition ("Application Timer Tests")]
public class ApplicationTimerTestCollection
{
    // This class is never instantiated - it's just a marker for xUnit
}

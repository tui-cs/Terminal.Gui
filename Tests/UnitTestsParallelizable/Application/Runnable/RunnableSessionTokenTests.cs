namespace ApplicationTests.RunnableTests;

/// <summary>
///     Tests for RunnableSessionToken class.
/// </summary>
[Collection("Application Tests")]
public class RunnableSessionTokenTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void RunnableSessionToken_Constructor_SetsRunnable ()
    {
        // Arrange
        Runnable<int> runnable = new ();

        // Act
        SessionToken token = new (runnable);

        // Assert
        Assert.NotNull (token.Runnable);
        Assert.Same (runnable, token.Runnable);
    }

    [Fact]
    public void RunnableSessionToken_Runnable_CanBeSetToNull ()
    {
        // Arrange
        Runnable<int> runnable = new ();
        SessionToken token = new (runnable);

        // Act
        token.Runnable = null;

        // Assert
        Assert.Null (token.Runnable);
    }
}

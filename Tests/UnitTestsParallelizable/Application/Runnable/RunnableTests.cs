namespace ApplicationTests.RunnableTests;

/// <summary>
///     Tests for IRunnable interface and Runnable base class.
/// </summary>
[Collection("Application Tests")]
public class RunnableTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Runnable_Implements_IRunnable ()
    {
        // Arrange & Act
        Runnable<int> runnable = new ();

        // Assert
        Assert.IsAssignableFrom<IRunnable> (runnable);
        Assert.IsAssignableFrom<IRunnable<int>> (runnable);
    }

    [Fact]
    public void Runnable_Result_DefaultsToDefault ()
    {
        // Arrange & Act
        Runnable<int> runnable = new ();

        // Assert
        Assert.Equal (0, runnable.Result);
    }

    [Fact]
    public void Runnable_Result_CanBeSet ()
    {
        // Arrange
        Runnable<int> runnable = new ();

        // Act
        runnable.Result = 42;

        // Assert
        Assert.Equal (42, runnable.Result);
    }

    [Fact]
    public void Runnable_Result_CanBeSetToNull ()
    {
        // Arrange
        Runnable<string> runnable = new ();

        // Act
        runnable.Result = null;

        // Assert
        Assert.Null (runnable.Result);
    }

    [Fact]
    public void Runnable_IsRunning_ReturnsFalse_WhenNotRunning ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ();
        Runnable<int> runnable = new ();

        // Act & Assert
        Assert.False (runnable.IsRunning);

        // Cleanup
        app.Dispose ();
    }

    [Fact]
    public void Runnable_IsModal_ReturnsFalse_WhenNotRunning ()
    {
        // Arrange
        Runnable<int> runnable = new ();

        // Act & Assert
        // IsModal should be false when the runnable has no app or is not TopRunnable
        Assert.False (runnable.IsModal);
    }

    [Fact]
    public void RaiseIsRunningChanging_ClearsResult_WhenStarting ()
    {
        // Arrange
        Runnable<int> runnable = new () { Result = 42 };

        // Act
        bool canceled = runnable.RaiseIsRunningChanging (false, true);

        // Assert
        Assert.False (canceled);
        Assert.Equal (0, runnable.Result); // Result should be cleared
    }

    [Fact]
    public void RaiseIsRunningChanging_CanBeCanceled_ByVirtualMethod ()
    {
        // Arrange
        CancelableRunnable runnable = new ();

        // Act
        bool canceled = runnable.RaiseIsRunningChanging (false, true);

        // Assert
        Assert.True (canceled);
    }

    [Fact]
    public void RaiseIsRunningChanging_CanBeCanceled_ByEvent ()
    {
        // Arrange
        Runnable<int> runnable = new ();
        var eventRaised = false;

        runnable.IsRunningChanging += (s, e) =>
                                      {
                                          eventRaised = true;
                                          e.Cancel = true;
                                      };

        // Act
        bool canceled = runnable.RaiseIsRunningChanging (false, true);

        // Assert
        Assert.True (eventRaised);
        Assert.True (canceled);
    }

    [Fact]
    public void RaiseIsRunningChanged_RaisesEvent ()
    {
        // Arrange
        Runnable<int> runnable = new ();
        var eventRaised = false;
        bool? receivedValue = null;

        runnable.IsRunningChanged += (s, e) =>
                                     {
                                         eventRaised = true;
                                         receivedValue = e.Value;
                                     };

        // Act
        runnable.RaiseIsRunningChangedEvent (true);

        // Assert
        Assert.True (eventRaised);
        Assert.True (receivedValue);
    }

    [Fact]
    public void RaiseIsModalChanged_RaisesEvent ()
    {
        // Arrange
        Runnable<int> runnable = new ();
        var eventRaised = false;
        bool? receivedValue = null;

        runnable.IsModalChanged += (s, e) =>
                                   {
                                       eventRaised = true;
                                       receivedValue = e.Value;
                                   };

        // Act
        runnable.RaiseIsModalChangedEvent (true);

        // Assert
        Assert.True (eventRaised);
        Assert.True (receivedValue);
    }

    /// <summary>
    ///     Test runnable that can cancel lifecycle changes.
    /// </summary>
    private class CancelableRunnable : Runnable<int>
    {
        protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning) => true; // Always cancel
    }
}

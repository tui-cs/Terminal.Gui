#nullable enable

namespace ViewsTests;

/// <summary>
///     Tests for IRunnable lifecycle behavior.
/// </summary>
[Collection("Application Tests")]
public class RunnableLifecycleTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Runnable_OnIsRunningChanging_CanExtractResult ()
    {
        // Arrange
        ResultExtractingRunnable runnable = new ();
        runnable.TestValue = "extracted";

        // Act
        bool canceled = runnable.RaiseIsRunningChanging (true, false); // Stopping

        // Assert
        Assert.False (canceled);
        Assert.Equal ("extracted", runnable.Result);
    }

    [Fact]
    public void Runnable_OnIsRunningChanging_ClearsResultWhenStarting ()
    {
        // Arrange
        ResultExtractingRunnable runnable = new () { Result = "previous" };

        // Act
        bool canceled = runnable.RaiseIsRunningChanging (false, true); // Starting

        // Assert
        Assert.False (canceled);
        Assert.Null (runnable.Result); // Result should be cleared
    }

    [Fact]
    public void Runnable_CanCancelStoppingWithUnsavedChanges ()
    {
        // Arrange
        UnsavedChangesRunnable runnable = new () { HasUnsavedChanges = true };

        // Act
        bool canceled = runnable.RaiseIsRunningChanging (true, false); // Stopping

        // Assert
        Assert.True (canceled); // Should be canceled
    }

    [Fact]
    public void Runnable_AllowsStoppingWithoutUnsavedChanges ()
    {
        // Arrange
        UnsavedChangesRunnable runnable = new () { HasUnsavedChanges = false };

        // Act
        bool canceled = runnable.RaiseIsRunningChanging (true, false); // Stopping

        // Assert
        Assert.False (canceled); // Should not be canceled
    }

    [Fact]
    public void Runnable_OnIsRunningChanged_CalledAfterStateChange ()
    {
        // Arrange
        TrackedRunnable runnable = new ();

        // Act
        runnable.RaiseIsRunningChangedEvent (true);

        // Assert
        Assert.True (runnable.OnIsRunningChangedCalled);
        Assert.True (runnable.LastIsRunningValue);
    }

    [Fact]
    public void Runnable_OnIsModalChanged_CalledAfterStateChange ()
    {
        // Arrange
        TrackedRunnable runnable = new ();

        // Act
        runnable.RaiseIsModalChangedEvent (true);

        // Assert
        Assert.True (runnable.OnIsModalChangedCalled);
        Assert.True (runnable.LastIsModalValue);
    }

    /// <summary>
    ///     Test runnable that extracts result in OnIsRunningChanging.
    /// </summary>
    private class ResultExtractingRunnable : Runnable<string>
    {
        public string? TestValue { get; set; }

        protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
        {
            if (!newIsRunning) // Stopping
            {
                // Extract result before removal from stack
                Result = TestValue;
            }

            return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
        }
    }

    /// <summary>
    ///     Test runnable that can prevent stopping with unsaved changes.
    /// </summary>
    private class UnsavedChangesRunnable : Runnable<int>
    {
        public bool HasUnsavedChanges { get; set; }

        protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
        {
            if (!newIsRunning && HasUnsavedChanges) // Stopping with unsaved changes
            {
                return true; // Cancel stopping
            }

            return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
        }
    }

    /// <summary>
    ///     Test runnable that tracks lifecycle method calls.
    /// </summary>
    private class TrackedRunnable : Runnable<int>
    {
        public bool OnIsRunningChangedCalled { get; private set; }
        public bool LastIsRunningValue { get; private set; }
        public bool OnIsModalChangedCalled { get; private set; }
        public bool LastIsModalValue { get; private set; }

        protected override void OnIsRunningChanged (bool newIsRunning)
        {
            OnIsRunningChangedCalled = true;
            LastIsRunningValue = newIsRunning;
            base.OnIsRunningChanged (newIsRunning);
        }

        protected override void OnIsModalChanged (bool newIsModal)
        {
            OnIsModalChangedCalled = true;
            LastIsModalValue = newIsModal;
            base.OnIsModalChanged (newIsModal);
        }
    }
}

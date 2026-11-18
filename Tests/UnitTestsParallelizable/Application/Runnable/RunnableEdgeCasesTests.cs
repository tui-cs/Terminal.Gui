namespace ApplicationTests.RunnableTests;

/// <summary>
///     Tests for edge cases and error conditions in IRunnable implementation.
/// </summary>
[Collection("Application Tests")]
public class RunnableEdgeCasesTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Runnable_MultipleEventSubscribers_AllInvoked ()
    {
        // Arrange
        Runnable<int> runnable = new ();
        var subscriber1Called = false;
        var subscriber2Called = false;
        var subscriber3Called = false;

        runnable.IsRunningChanging += (s, e) => subscriber1Called = true;
        runnable.IsRunningChanging += (s, e) => subscriber2Called = true;
        runnable.IsRunningChanging += (s, e) => subscriber3Called = true;

        // Act
        runnable.RaiseIsRunningChanging (false, true);

        // Assert
        Assert.True (subscriber1Called);
        Assert.True (subscriber2Called);
        Assert.True (subscriber3Called);
    }

    [Fact]
    public void Runnable_EventSubscriber_CanCancelAfterOthers ()
    {
        // Arrange
        Runnable<int> runnable = new ();
        var subscriber1Called = false;
        var subscriber2Called = false;

        runnable.IsRunningChanging += (s, e) => subscriber1Called = true;

        runnable.IsRunningChanging += (s, e) =>
                                      {
                                          subscriber2Called = true;
                                          e.Cancel = true; // Second subscriber cancels
                                      };

        // Act
        bool canceled = runnable.RaiseIsRunningChanging (false, true);

        // Assert
        Assert.True (subscriber1Called);
        Assert.True (subscriber2Called);
        Assert.True (canceled);
    }

    [Fact]
    public void Runnable_Result_CanBeSetMultipleTimes ()
    {
        // Arrange
        Runnable<int> runnable = new ();

        // Act
        runnable.Result = 1;
        runnable.Result = 2;
        runnable.Result = 3;

        // Assert
        Assert.Equal (3, runnable.Result);
    }

    [Fact]
    public void Runnable_Result_ClearedOnMultipleStarts ()
    {
        // Arrange
        Runnable<int> runnable = new () { Result = 42 };

        // Act & Assert - First start
        runnable.RaiseIsRunningChanging (false, true);
        Assert.Equal (0, runnable.Result);

        // Set result again
        runnable.Result = 99;
        Assert.Equal (99, runnable.Result);

        // Second start should clear again
        runnable.RaiseIsRunningChanging (false, true);
        Assert.Equal (0, runnable.Result);
    }

    [Fact]
    public void Runnable_NullableResult_DefaultsToNull ()
    {
        // Arrange & Act
        Runnable<string> runnable = new ();

        // Assert
        Assert.Null (runnable.Result);
    }

    [Fact]
    public void Runnable_NullableResult_CanBeExplicitlyNull ()
    {
        // Arrange
        Runnable<string> runnable = new () { Result = "test" };

        // Act
        runnable.Result = null;

        // Assert
        Assert.Null (runnable.Result);
    }

    [Fact]
    public void Runnable_ComplexType_Result ()
    {
        // Arrange
        Runnable<ComplexResult> runnable = new ();
        ComplexResult result = new () { Value = 42, Text = "test" };

        // Act
        runnable.Result = result;

        // Assert
        Assert.NotNull (runnable.Result);
        Assert.Equal (42, runnable.Result.Value);
        Assert.Equal ("test", runnable.Result.Text);
    }

    [Fact]
    public void Runnable_IsRunning_WithNoApp ()
    {
        // Arrange
        Runnable<int> runnable = new ();

        // Don't set App property

        // Act & Assert
        Assert.False (runnable.IsRunning);
    }

    [Fact]
    public void Runnable_IsModal_WithNoApp ()
    {
        // Arrange
        Runnable<int> runnable = new ();

        // Don't set App property

        // Act & Assert
        Assert.False (runnable.IsModal);
    }

    [Fact]
    public void Runnable_VirtualMethods_CanBeOverridden ()
    {
        // Arrange
        OverriddenRunnable runnable = new ();

        // Act
        bool canceledRunning = runnable.RaiseIsRunningChanging (false, true);
        runnable.RaiseIsRunningChangedEvent (true);
        runnable.RaiseIsModalChangedEvent (true);

        // Assert
        Assert.True (runnable.OnIsRunningChangingCalled);
        Assert.True (runnable.OnIsRunningChangedCalled);
        Assert.True (runnable.OnIsModalChangedCalled);
    }

    [Fact]
    public void Runnable_RequestStop_WithNoApp ()
    {
        // Arrange
        Runnable<int> runnable = new ();

        // Don't set App property

        // Act & Assert - Should not throw
        runnable.RequestStop ();
    }

    [Fact]
    public void RunnableSessionToken_Constructor_RequiresRunnable ()
    {
        // This is implicitly tested by the constructor signature,
        // but let's verify it creates with non-null runnable

        // Arrange
        Runnable<int> runnable = new ();

        // Act
        SessionToken token = new (runnable);

        // Assert
        Assert.NotNull (token.Runnable);
    }

    [Fact]
    public void Runnable_EventArgs_PreservesValues ()
    {
        // Arrange
        Runnable<int> runnable = new ();
        bool? capturedOldValue = null;
        bool? capturedNewValue = null;

        runnable.IsRunningChanging += (s, e) =>
                                      {
                                          capturedOldValue = e.CurrentValue;
                                          capturedNewValue = e.NewValue;
                                      };

        // Act
        runnable.RaiseIsRunningChanging (false, true);

        // Assert
        Assert.NotNull (capturedOldValue);
        Assert.NotNull (capturedNewValue);
        Assert.False (capturedOldValue.Value);
        Assert.True (capturedNewValue.Value);
    }

    [Fact]
    public void Runnable_IsModalChanged_EventArgs_PreservesValue ()
    {
        // Arrange
        Runnable<int> runnable = new ();
        bool? capturedValue = null;

        runnable.IsModalChanged += (s, e) => { capturedValue = e.Value; };

        // Act
        runnable.RaiseIsModalChangedEvent (true);

        // Assert
        Assert.NotNull (capturedValue);
        Assert.True (capturedValue.Value);
    }

    [Fact]
    public void Runnable_DifferentGenericTypes_Independent ()
    {
        // Arrange & Act
        Runnable<int> intRunnable = new () { Result = 42 };
        Runnable<string> stringRunnable = new () { Result = "test" };
        Runnable<bool> boolRunnable = new () { Result = true };

        // Assert
        Assert.Equal (42, intRunnable.Result);
        Assert.Equal ("test", stringRunnable.Result);
        Assert.True (boolRunnable.Result);
    }

    /// <summary>
    ///     Complex result type for testing.
    /// </summary>
    private class ComplexResult
    {
        public int Value { get; set; }
        public string? Text { get; set; }
    }

    /// <summary>
    ///     Runnable that tracks virtual method calls.
    /// </summary>
    private class OverriddenRunnable : Runnable<int>
    {
        public bool OnIsRunningChangingCalled { get; private set; }
        public bool OnIsRunningChangedCalled { get; private set; }
        public bool OnIsModalChangedCalled { get; private set; }

        protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
        {
            OnIsRunningChangingCalled = true;

            return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
        }

        protected override void OnIsRunningChanged (bool newIsRunning)
        {
            OnIsRunningChangedCalled = true;
            base.OnIsRunningChanged (newIsRunning);
        }

        protected override void OnIsModalChanged (bool newIsModal)
        {
            OnIsModalChangedCalled = true;
            base.OnIsModalChanged (newIsModal);
        }
    }
}

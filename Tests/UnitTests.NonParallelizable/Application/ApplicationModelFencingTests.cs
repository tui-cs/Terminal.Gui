namespace UnitTests.NonParallelizable.ApplicationTests;

/// <summary>
///     Tests to ensure that mixing legacy static Application and modern instance-based models
///     throws appropriate exceptions.
/// </summary>
public class ApplicationModelFencingTests
{
    [Fact]
    public void Create_ThenInstanceAccess_ThrowsInvalidOperationException ()
    {
        // Reset the model usage tracking before each test
        ApplicationImpl.ResetModelUsageTracking ();

        // Create a modern instance-based application
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Attempting to initialize using the legacy static model should throw
        var ex = Assert.Throws<InvalidOperationException> (() => { ApplicationImpl.Instance.Init (DriverRegistry.Names.ANSI); });

        Assert.Contains ("Cannot use legacy static Application model", ex.Message);
        Assert.Contains ("after using modern instance-based model", ex.Message);

        // Clean up
        app.Dispose ();
        ApplicationImpl.ResetModelUsageTracking ();
    }

    [Fact]
    public void InstanceAccess_ThenCreate_ThrowsInvalidOperationException ()
    {
        // Reset the model usage tracking before each test
        ApplicationImpl.ResetModelUsageTracking ();

        // Initialize using the legacy static model
        IApplication staticInstance = ApplicationImpl.Instance;
        staticInstance.Init (DriverRegistry.Names.ANSI);

        // Attempting to create and initialize with modern instance-based model should throw
        var ex = Assert.Throws<InvalidOperationException> (() =>
                                                           {
                                                               IApplication app = Application.Create ();
                                                               app.Init (DriverRegistry.Names.ANSI);
                                                           });

        Assert.Contains ("Cannot use modern instance-based model", ex.Message);
        Assert.Contains ("after using legacy static Application model", ex.Message);

        // Clean up
        staticInstance.Dispose ();
        ApplicationImpl.ResetModelUsageTracking ();
    }

    [Fact]
    public void Init_ThenCreate_ThrowsInvalidOperationException ()
    {
        // Reset the model usage tracking before each test
        ApplicationImpl.ResetModelUsageTracking ();

        // Initialize using legacy static API
        IApplication staticInstance = ApplicationImpl.Instance;
        staticInstance.Init (DriverRegistry.Names.ANSI);

        // Attempting to create a modern instance-based application should throw
        var ex = Assert.Throws<InvalidOperationException> (() =>
                                                           {
                                                               IApplication _ = Application.Create ();
                                                           });

        Assert.Contains ("Cannot use modern instance-based model", ex.Message);
        Assert.Contains ("after using legacy static Application model", ex.Message);

        // Clean up
        staticInstance.Dispose ();
        ApplicationImpl.ResetModelUsageTracking ();
    }

    [Fact]
    public void Create_ThenInit_ThrowsInvalidOperationException ()
    {
        // Reset the model usage tracking before each test
        ApplicationImpl.ResetModelUsageTracking ();

        // Create a modern instance-based application
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Attempting to initialize using the legacy static model should throw
        var ex = Assert.Throws<InvalidOperationException> (() => { ApplicationImpl.Instance.Init (DriverRegistry.Names.ANSI); });

        Assert.Contains ("Cannot use legacy static Application model", ex.Message);
        Assert.Contains ("after using modern instance-based model", ex.Message);

        // Clean up
        app.Dispose ();
        ApplicationImpl.ResetModelUsageTracking ();
    }

    [Fact]
    public void MultipleCreate_Calls_DoNotThrow ()
    {
        // Reset the model usage tracking before each test
        ApplicationImpl.ResetModelUsageTracking ();

        // Multiple calls to Create should not throw
        IApplication app1 = Application.Create ();
        IApplication app2 = Application.Create ();
        IApplication app3 = Application.Create ();

        Assert.NotNull (app1);
        Assert.NotNull (app2);
        Assert.NotNull (app3);

        // Clean up
        app1.Dispose ();
        app2.Dispose ();
        app3.Dispose ();
        ApplicationImpl.ResetModelUsageTracking ();
    }

    [Fact]
    public void MultipleInstanceAccess_DoesNotThrow ()
    {
        // Reset the model usage tracking before each test
        ApplicationImpl.ResetModelUsageTracking ();

        // Multiple accesses to Instance should not throw (it's a singleton)
        IApplication instance1 = ApplicationImpl.Instance;
        IApplication instance2 = ApplicationImpl.Instance;
        IApplication instance3 = ApplicationImpl.Instance;

        Assert.NotNull (instance1);
        Assert.Same (instance1, instance2);
        Assert.Same (instance2, instance3);

        // Clean up
        instance1.Dispose ();
        ApplicationImpl.ResetModelUsageTracking ();
    }

    [Fact]
    public void ResetModelUsageTracking_AllowsSwitchingModels ()
    {
        // Reset the model usage tracking before each test
        ApplicationImpl.ResetModelUsageTracking ();

        // Use modern model
        IApplication app1 = Application.Create ();
        app1.Dispose ();

        // Reset the tracking
        ApplicationImpl.ResetModelUsageTracking ();

        // Should now be able to use legacy model
        IApplication staticInstance = ApplicationImpl.Instance;
        Assert.NotNull (staticInstance);
        staticInstance.Dispose ();

        // Reset again
        ApplicationImpl.ResetModelUsageTracking ();

        // Should be able to use modern model again
        IApplication app2 = Application.Create ();
        Assert.NotNull (app2);
        app2.Dispose ();
        ApplicationImpl.ResetModelUsageTracking ();
    }
}

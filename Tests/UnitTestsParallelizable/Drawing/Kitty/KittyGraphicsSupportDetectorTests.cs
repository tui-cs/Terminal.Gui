// Copilot - Claude Sonnet 4.6

using System.Reflection;

namespace DrawingTests;

[Collection ("Environment Variable Tests")]
public class KittyGraphicsSupportDetectorTests
{
    [Fact]
    public void Constructor_DriverParameter_IsNonNullableContract ()
    {
        NullabilityInfoContext context = new ();

        ConstructorInfo constructor = typeof (KittyGraphicsSupportDetector).GetConstructor ([typeof (IDriver)])
                                      ?? throw new InvalidOperationException ("Driver constructor was not found.");

        ParameterInfo parameter = Assert.Single (constructor.GetParameters ());

        Assert.Equal (NullabilityState.NotNull, context.Create (parameter).ReadState);
        Assert.Throws<ArgumentNullException> (() => new KittyGraphicsSupportDetector (null!));
    }

    [Fact]
    public void Detect_WhenKittyWindowIdSet_ReturnsSupported ()
    {
        string? previous = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", "1");
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", null);

        try
        {
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.True (result.IsSupported);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previous);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }

    [Fact]
    public void Detect_WhenTermProgramIsKitty_ReturnsSupported ()
    {
        string? previous = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", null);
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", "kitty");

        try
        {
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.True (result.IsSupported);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previous);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }

    [Fact]
    public void Detect_WhenTermProgramIsGhostty_ReturnsSupported ()
    {
        string? previous = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", null);
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", "ghostty");

        try
        {
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.True (result.IsSupported);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previous);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }

    [Fact]
    public void Detect_WhenTermProgramIsGhostty_CaseInsensitive ()
    {
        string? previous = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", null);
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", "Ghostty");

        try
        {
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.True (result.IsSupported);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previous);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }

    [Fact]
    public void Detect_WhenNoEnvVarsSet_ReturnsNotSupported ()
    {
        string? previousWindowId = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", null);
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", "xterm");

        try
        {
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.False (result.IsSupported);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previousWindowId);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }

    [Fact]
    public void Detect_NotSupported_HasDefaultResolution ()
    {
        string? previousWindowId = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", null);
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", null);

        try
        {
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.Equal (new Size (10, 20), result.Resolution);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previousWindowId);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }

    [Fact]
    public void Detect_Supported_NoDriver_HasDefaultResolution ()
    {
        string? previousWindowId = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", "1");
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", null);

        try
        {
            // Detector without a driver cannot query for resolution — defaults to 10×20
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.True (result.IsSupported);
            Assert.Equal (new Size (10, 20), result.Resolution);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previousWindowId);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }
}

[CollectionDefinition ("Environment Variable Tests", DisableParallelization = true)]
public class EnvironmentVariableTestCollection
{ }

public class KittyGraphicsSupportDetectorCollectionTests
{
    [Fact]
    public void DetectorTests_RunInNonParallelEnvironmentCollection ()
    {
        object? collectionAttribute = typeof (KittyGraphicsSupportDetectorTests).GetCustomAttributes (false)
                                                                                .SingleOrDefault (attribute => attribute.GetType ().Name
                                                                                                      == "CollectionAttribute");

        Assert.NotNull (collectionAttribute);

        var collectionName = collectionAttribute.GetType ().GetProperty ("Name")?.GetValue (collectionAttribute) as string;

        Assert.Equal ("Environment Variable Tests", collectionName);

        Type? collectionDefinitionType = typeof (KittyGraphicsSupportDetectorTests).Assembly.GetType ("DrawingTests.EnvironmentVariableTestCollection");

        Assert.NotNull (collectionDefinitionType);

        object? collectionDefinitionAttribute = collectionDefinitionType.GetCustomAttributes (false)
                                                                         .SingleOrDefault (attribute => attribute.GetType ().Name
                                                                                                        == "CollectionDefinitionAttribute");

        Assert.NotNull (collectionDefinitionAttribute);

        var disableParallelization = (bool)(collectionDefinitionAttribute
                                            .GetType ()
                                            .GetProperty ("DisableParallelization")
                                            ?.GetValue (collectionDefinitionAttribute)
                                            ?? false);

        Assert.True (disableParallelization);
    }
}

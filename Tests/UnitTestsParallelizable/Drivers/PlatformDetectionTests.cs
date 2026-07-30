using System.Runtime.InteropServices;

namespace DriverTests;

[Collection ("Driver Tests")]
public class PlatformDetectionTests (ITestOutputHelper output)
{
    [Fact]
    public void DetectPlatform_BasedOnOSDescription ()
    {
        bool isWSLExpected = PlatformDetection.IsWSL ();

        if (OperatingSystem.IsWindows ())
        {
            Assert.False (isWSLExpected);
        }
        else if (OperatingSystem.IsLinux ())
        {
            Assert.Equal (isWSLExpected, PlatformDetection.IsWSL ());
        }
        else if (OperatingSystem.IsMacOS ())
        {
            Assert.False (isWSLExpected);
        }
        else
        {
            // Fallback for other Unix-like or unknown systems
            Assert.False (isWSLExpected);
            output.WriteLine ($"Unknown OS Description: {RuntimeInformation.OSDescription}");
        }
    }

    // Claude - Opus 5
    [Fact]
    public void GetPlatformName_ReturnsNameMatchingCurrentPlatform ()
    {
        string name = PlatformDetection.GetPlatformName ();

        string expected = OperatingSystem.IsWindows () ? "WINDOWS"
                          : OperatingSystem.IsAndroid () ? "ANDROID"
                          : OperatingSystem.IsLinux () ? "LINUX"
                          : OperatingSystem.IsMacOS () ? "OSX"
                          : OperatingSystem.IsFreeBSD () ? "FREEBSD"
                          : name; // Platform not covered by OperatingSystem.Is* helpers; asserted below instead.

        Assert.Equal (expected, name);
        Assert.True (name.Length == 0 || PlatformDetection.KnownPlatformNames.Contains (name));
    }

    // Claude - Opus 5
    // ANDROID must be listed separately from LINUX: OperatingSystem.IsLinux () is false on Android, so any
    // platform table keyed off "is it Linux?" silently misses it.
    [Theory]
    [InlineData ("WINDOWS")]
    [InlineData ("LINUX")]
    [InlineData ("ANDROID")]
    [InlineData ("OSX")]
    [InlineData ("FREEBSD")]
    [InlineData ("NETBSD")]
    [InlineData ("OPENBSD")]
    [InlineData ("SOLARIS")]
    [InlineData ("ILLUMOS")]
    [InlineData ("HAIKU")]
    public void KnownPlatformNames_Contains (string platformName) => Assert.Contains (platformName, PlatformDetection.KnownPlatformNames);

    // Claude - Opus 5
    [Fact]
    public void KnownPlatformNames_AreUpperCaseAndUnique ()
    {
        Assert.Equal (PlatformDetection.KnownPlatformNames.Length, PlatformDetection.KnownPlatformNames.Distinct ().Count ());
        Assert.All (PlatformDetection.KnownPlatformNames, n => Assert.Equal (n.ToUpperInvariant (), n));
    }
}

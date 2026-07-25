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
}

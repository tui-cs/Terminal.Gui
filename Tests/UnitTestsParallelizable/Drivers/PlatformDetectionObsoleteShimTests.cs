using System.Reflection;

namespace DriverTests;

/// <summary>
///     Pins the <see cref="PlatformDetection"/> members that shipped public in v2.4.17 and were removed in #5600.
///     They are restored as <see cref="ObsoleteAttribute"/> shims so upgrading consumers do not hit
///     <see cref="MissingMethodException"/>. Each assertion below encodes the v2.4.17 behaviour, not an improved one.
/// </summary>
public class PlatformDetectionObsoleteShimTests
{
    // Claude - Opus 5
    // These members are deliberately obsolete; calling them here is the point of the test.
#pragma warning disable CS0618

    [Fact]
    public void IsWindows_MatchesOperatingSystem () => Assert.Equal (OperatingSystem.IsWindows (), PlatformDetection.IsWindows ());

    // Claude - Opus 5
    [Fact]
    public void IsLinux_MatchesOperatingSystem () => Assert.Equal (OperatingSystem.IsLinux (), PlatformDetection.IsLinux ());

    // Claude - Opus 5
    [Fact]
    public void IsMac_MatchesOperatingSystem () => Assert.Equal (OperatingSystem.IsMacOS (), PlatformDetection.IsMac ());

    // Claude - Opus 5
    [Fact]
    public void IsUnixLike_CoversLinuxMacAndFreeBsdOnly ()
    {
        // v2.4.17 semantics: Linux || OSX || FreeBSD. Deliberately excludes NetBSD/OpenBSD/Solaris/illumos/Haiku.
        bool expected = OperatingSystem.IsLinux () || OperatingSystem.IsMacOS () || OperatingSystem.IsFreeBSD ();

        Assert.Equal (expected, PlatformDetection.IsUnixLike ());
    }

    // Claude - Opus 5
    [Fact]
    public void IsUnixLike_IsFalseOnWindows ()
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        Assert.False (PlatformDetection.IsUnixLike ());
    }

    // Claude - Opus 5
    [Fact]
    public void GetCurrentPlatform_ReturnsPlatformForCurrentOS ()
    {
        TuiPlatform expected = OperatingSystem.IsWindows () ? TuiPlatform.Windows
                               : OperatingSystem.IsMacOS () ? TuiPlatform.Macos
                               : TuiPlatform.Linux;

        Assert.Equal (expected, PlatformDetection.GetCurrentPlatform ());
    }

    // Claude - Opus 5
    [Fact]
    public void GetCurrentPlatform_ReturnsDefinedvalue () => Assert.True (Enum.IsDefined (PlatformDetection.GetCurrentPlatform ()));

    // Claude - Opus 5
    // Pins the public surface that #5600 removed. A full PublicAPI baseline for the library would catch this
    // class of break everywhere, but this guards the specific type that regressed without gating every future PR.
    [Fact]
    public void PublicSurface_MatchesV2_4_17 ()
    {
        string [] expected =
        [
            "GetCurrentPlatform",
            "IsLinux",
            "IsMac",
            "IsUnixLike",
            "IsWSL",
            "IsWindows"
        ];

        string [] actual =
        [
            .. typeof (PlatformDetection).GetMethods (BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                        .Select (m => m.Name)
                                        .OrderBy (n => n, StringComparer.Ordinal)
        ];

        Assert.Equal (expected, actual);
    }

    // Claude - Opus 5
    [Fact]
    public void PublicSurface_IsParameterlessAndReturnsExpectedTypes ()
    {
        MethodInfo [] methods = typeof (PlatformDetection).GetMethods (BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.All (methods, m => Assert.Empty (m.GetParameters ()));
        Assert.Equal (typeof (TuiPlatform), typeof (PlatformDetection).GetMethod (nameof (PlatformDetection.GetCurrentPlatform))!.ReturnType);
    }

    // Claude - Opus 5
    [Fact]
    public void ExactlyOneOfWindowsLinuxMac_IsTrue_OnSupportedPlatforms ()
    {
        if (!OperatingSystem.IsWindows () && !OperatingSystem.IsLinux () && !OperatingSystem.IsMacOS ())
        {
            return; // Some other Unix; none of the three is expected to be true.
        }

        bool [] flags = [PlatformDetection.IsWindows (), PlatformDetection.IsLinux (), PlatformDetection.IsMac ()];

        Assert.Single (flags, f => f);
    }
#pragma warning restore CS0618
}

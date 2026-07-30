using System.Runtime.InteropServices;

namespace DriverTests;

/// <summary>
///     Pins <see cref="UnixIOHelper.MapTiocgwinsz"/> against each platform's ioctl numbering. These values are
///     unverifiable on CI (which only runs Windows, Linux, and macOS), so the table itself is the contract.
/// </summary>
public class UnixIOHelperTiocgwinszTests
{
    // Claude - Opus 5
    [Theory]

    // asm-generic ioctl numbering. Android needs its own entry because OperatingSystem.IsLinux () is false there,
    // even though Android uses the same numbering as Linux.
    [InlineData ("LINUX", 0x5413u)]
    [InlineData ("ANDROID", 0x5413u)]

    // BSD _IOR ('t', 104, struct winsize) == 0x40000000 | (8 << 16) | ('t' << 8) | 104.
    [InlineData ("OSX", 0x40087468u)]
    [InlineData ("FREEBSD", 0x40087468u)]
    [InlineData ("NETBSD", 0x40087468u)]
    [InlineData ("OPENBSD", 0x40087468u)]

    // Solaris/illumos: TIOC|104 where TIOC == ('T' << 8).
    [InlineData ("SOLARIS", 0x5468u)]
    [InlineData ("ILLUMOS", 0x5468u)]

    // Haiku: (TCGETA + 12) where TCGETA == 0x8000. Notably NOT the BSD value.
    [InlineData ("HAIKU", 0x800Cu)]

    // Unknown platforms fall back to BSD-style encoding, the most common convention.
    [InlineData ("", 0x40087468u)]
    public void MapTiocgwinsz_ReturnsRequestCodeForPlatform (string platformName, uint expected) =>
        Assert.Equal (expected, UnixIOHelper.MapTiocgwinsz (Architecture.X64, platformName));

    // Claude - Opus 5
    // Linux ioctl numbering is architecture-dependent: ppc64le uses _IOR ('t', 104, struct winsize) per its UAPI,
    // every other architecture .NET targets uses asm-generic 0x5413.
    [Theory]
    [InlineData (Architecture.Ppc64le, 0x40087468u)]
    [InlineData (Architecture.X64, 0x5413u)]
    [InlineData (Architecture.X86, 0x5413u)]
    [InlineData (Architecture.Arm, 0x5413u)]
    [InlineData (Architecture.Arm64, 0x5413u)]
    [InlineData (Architecture.RiscV64, 0x5413u)]
    [InlineData (Architecture.S390x, 0x5413u)]
    [InlineData (Architecture.LoongArch64, 0x5413u)]
    public void MapTiocgwinsz_Linux_IsArchitectureDependent (Architecture arch, uint expected) =>
        Assert.Equal (expected, UnixIOHelper.MapTiocgwinsz (arch, "LINUX"));

    // Claude - Opus 5
    [Fact]
    public void MapTiocgwinsz_NonLinux_IsArchitectureIndependent ()
    {
        string [] nonLinux = ["OSX", "FREEBSD", "NETBSD", "OPENBSD", "SOLARIS", "ILLUMOS", "HAIKU", ""];
        Architecture [] arches = [Architecture.X64, Architecture.Arm64, Architecture.Ppc64le, Architecture.S390x];

        foreach (string platform in nonLinux)
        {
            uint baseline = UnixIOHelper.MapTiocgwinsz (Architecture.X64, platform);

            Assert.All (arches, a => Assert.Equal (baseline, UnixIOHelper.MapTiocgwinsz (a, platform)));
        }
    }

    // Claude - Opus 5
    [Fact]
    public void MapTiocgwinsz_BsdConstant_MatchesIorEncoding ()
    {
        // _IOR ('t', 104, struct winsize): IOC_OUT | ((sizeof (winsize) & IOCPARM_MASK) << 16) | ('t' << 8) | 104
        const uint IOC_OUT = 0x40000000u;
        const uint WINSIZE_SIZE = 8u; // four ushorts
        uint expected = IOC_OUT | (WINSIZE_SIZE << 16) | ((uint)'t' << 8) | 104u;

        Assert.Equal (expected, UnixIOHelper.MapTiocgwinsz (Architecture.X64, "OSX"));
    }

    // Claude - Opus 5
    [Fact]
    public void MapTiocgwinsz_SolarisConstant_MatchesTiocEncoding ()
    {
        // Solaris: TIOCGWINSZ == (TIOC|104) where TIOC == ('T' << 8)
        uint expected = ((uint)'T' << 8) | 104u;

        Assert.Equal (expected, UnixIOHelper.MapTiocgwinsz (Architecture.X64, "SOLARIS"));
    }

    // Claude - Opus 5
    [Fact]
    public void MapTiocgwinsz_HaikuConstant_MatchesTcgetaOffset ()
    {
        // Haiku: TIOCGWINSZ == (TCGETA + 12), TCGETA == 0x8000. Haiku does not use BSD _IOR encoding, so the
        // BSD fallback is wrong for it.
        const uint TCGETA = 0x8000u;
        uint expected = TCGETA + 12u;

        Assert.Equal (expected, UnixIOHelper.MapTiocgwinsz (Architecture.X64, "HAIKU"));
        Assert.NotEqual (0x40087468u, UnixIOHelper.MapTiocgwinsz (Architecture.X64, "HAIKU"));
    }

    // Claude - Opus 5
    [Fact]
    public void TIOCGWINSZ_MatchesMappingForCurrentPlatform () =>
        Assert.Equal (UnixIOHelper.MapTiocgwinsz (RuntimeInformation.ProcessArchitecture, PlatformDetection.GetPlatformName ()),
                      UnixIOHelper.TIOCGWINSZ);

    // Claude - Opus 5
    // The Apple ARM64 variadic ABI applies to every Apple platform on ARM64, and to nothing else.
    [Theory]
    [InlineData (Architecture.Arm64, "OSX", true)]
    [InlineData (Architecture.Arm64, "MACCATALYST", true)]
    [InlineData (Architecture.Arm64, "IOS", true)]
    [InlineData (Architecture.Arm64, "TVOS", true)]

    // Non-Apple ARM64 uses standard AAPCS64, where variadic args go in registers.
    [InlineData (Architecture.Arm64, "LINUX", false)]
    [InlineData (Architecture.Arm64, "ANDROID", false)]
    [InlineData (Architecture.Arm64, "FREEBSD", false)]
    [InlineData (Architecture.Arm64, "NETBSD", false)]

    // Non-ARM64 never uses the placeholder signature, Apple or not.
    [InlineData (Architecture.X64, "OSX", false)]
    [InlineData (Architecture.X64, "LINUX", false)]
    [InlineData (Architecture.Arm, "OSX", false)]
    [InlineData (Architecture.X86, "OSX", false)]
    public void UseArm64VariadicIoctl_OnlyForAppleArm64 (Architecture arch, string platformName, bool expected) =>
        Assert.Equal (expected, UnixIOHelper.UseArm64VariadicIoctl (arch, platformName));

    // Claude - Opus 5
    [Fact]
    public void UseArm64VariadicIoctl_RosettaX64Process_UsesPlainIoctl ()
    {
        // .NET reports OSArchitecture == Arm64 for an x64 process translated by Rosetta (it checks
        // sysctl.proc_translated). That process executes x64 code and passes variadic args in registers, so keying
        // off OSArchitecture would wrongly select the stack-passing path. ProcessArchitecture is X64 there.
        Assert.False (UnixIOHelper.UseArm64VariadicIoctl (Architecture.X64, "OSX"));
        Assert.True (UnixIOHelper.UseArm64VariadicIoctl (Architecture.Arm64, "OSX"));
    }

    // Claude - Opus 5
    [Fact]
    public void UseArm64VariadicIoctl_MatchesProcessArchitectureForCurrentPlatform ()
    {
        bool expected = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                        && PlatformDetection.IsApplePlatformName (PlatformDetection.GetPlatformName ());

        Assert.Equal (expected, UnixIOHelper.UseArm64VariadicIoctl (RuntimeInformation.ProcessArchitecture, PlatformDetection.GetPlatformName ()));
    }
}

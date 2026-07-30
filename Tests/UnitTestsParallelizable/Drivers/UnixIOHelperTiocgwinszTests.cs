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

    // Unknown platforms fall back to BSD-style encoding, the most common convention.
    [InlineData ("HAIKU", 0x40087468u)]
    [InlineData ("", 0x40087468u)]
    public void MapTiocgwinsz_ReturnsRequestCodeForPlatform (string platformName, uint expected) =>
        Assert.Equal (expected, UnixIOHelper.MapTiocgwinsz (platformName));

    // Claude - Opus 5
    [Fact]
    public void MapTiocgwinsz_BsdConstant_MatchesIorEncoding ()
    {
        // _IOR ('t', 104, struct winsize): IOC_OUT | ((sizeof (winsize) & IOCPARM_MASK) << 16) | ('t' << 8) | 104
        const uint IOC_OUT = 0x40000000u;
        const uint WINSIZE_SIZE = 8u; // four ushorts
        uint expected = IOC_OUT | (WINSIZE_SIZE << 16) | ((uint)'t' << 8) | 104u;

        Assert.Equal (expected, UnixIOHelper.MapTiocgwinsz ("OSX"));
    }

    // Claude - Opus 5
    [Fact]
    public void MapTiocgwinsz_SolarisConstant_MatchesTiocEncoding ()
    {
        // Solaris: TIOCGWINSZ == (TIOC|104) where TIOC == ('T' << 8)
        uint expected = ((uint)'T' << 8) | 104u;

        Assert.Equal (expected, UnixIOHelper.MapTiocgwinsz ("SOLARIS"));
    }

    // Claude - Opus 5
    [Fact]
    public void TIOCGWINSZ_MatchesMappingForCurrentPlatform () =>
        Assert.Equal (UnixIOHelper.MapTiocgwinsz (PlatformDetection.GetPlatformName ()), UnixIOHelper.TIOCGWINSZ);
}

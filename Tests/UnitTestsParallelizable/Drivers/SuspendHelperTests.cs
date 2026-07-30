namespace DriverTests;

/// <summary>
///     Pins <see cref="SuspendHelper.MapSuspendSignal"/> against each platform's <c>signal.h</c>. These values are
///     unverifiable on CI (which only runs Windows, Linux, and macOS), so the table itself is the contract.
/// </summary>
public class SuspendHelperTests
{
    // Claude - Opus 5
    [Theory]

    // Darwin and the BSDs.
    [InlineData ("OSX", 18)]
    [InlineData ("FREEBSD", 18)]
    [InlineData ("NETBSD", 18)]
    [InlineData ("OPENBSD", 18)]

    // Linux, and Android — which needs its own entry because OperatingSystem.IsLinux () is false there.
    [InlineData ("LINUX", 20)]
    [InlineData ("ANDROID", 20)]

    // Solaris/illumos.
    [InlineData ("SOLARIS", 24)]
    [InlineData ("ILLUMOS", 24)]

    // Haiku: SIGTSTP is 13. 21 is SIGKILLTHR there, which would kill threads instead of suspending.
    [InlineData ("HAIKU", 13)]

    // Platforms with no suspend concept, and unknown names.
    [InlineData ("WINDOWS", -1)]
    [InlineData ("BROWSER", -1)]
    [InlineData ("", -1)]
    public void MapSuspendSignal_ReturnsSigtstpForPlatform (string platformName, int expected) =>
        Assert.Equal (expected, SuspendHelper.MapSuspendSignal (platformName));

    // Claude - Opus 5
    [Fact]
    public void MapSuspendSignal_NeverReturnsHaikuSigkillthr ()
    {
        // Regression guard: 21 is SIGKILLTHR on Haiku and SIGURG/SIGTTOU elsewhere — never a suspend signal.
        Assert.All (PlatformDetection.KnownPlatformNames, n => Assert.NotEqual (21, SuspendHelper.MapSuspendSignal (n)));
    }

    // Claude - Opus 5
    [Fact]
    public void MapSuspendSignal_EveryUnixPlatformHasASignal ()
    {
        string [] unixPlatforms = [.. PlatformDetection.KnownPlatformNames.Where (n => n != "WINDOWS")];

        Assert.All (unixPlatforms, n => Assert.NotEqual (-1, SuspendHelper.MapSuspendSignal (n)));
    }

    // Claude - Opus 5
    [Fact]
    public void MapSuspendSignal_UnknownPlatform_ReturnsMinusOne () => Assert.Equal (-1, SuspendHelper.MapSuspendSignal ("NOT_A_PLATFORM"));

    // Claude - Opus 5
    [Fact]
    public void Suspend_KillpgSucceeds_ReturnsTrue () => Assert.True (SuspendHelper.Suspend (20, (_, _) => 0));

    // Claude - Opus 5
    // killpg failing means the process was never stopped, so Suspend () must not claim success — callers use this to
    // decide whether they resumed, and previously it always returned true.
    [Theory]
    [InlineData (-1)]
    [InlineData (1)]
    public void Suspend_KillpgFails_ReturnsFalse (int killpgResult) => Assert.False (SuspendHelper.Suspend (20, (_, _) => killpgResult));

    // Claude - Opus 5
    [Fact]
    public void Suspend_NoSuspendSignal_ReturnsFalse_AndDoesNotSignal ()
    {
        var invoked = false;

        bool result = SuspendHelper.Suspend (-1,
                                             (_, _) =>
                                             {
                                                 invoked = true;

                                                 return 0;
                                             });

        Assert.False (result);
        Assert.False (invoked);
    }

    // Claude - Opus 5
    [Fact]
    public void Suspend_SignalsOwnProcessGroup_WithMappedSignal ()
    {
        int observedGroup = int.MinValue;
        int observedSignal = int.MinValue;

        SuspendHelper.Suspend (20,
                               (pgrp, sig) =>
                               {
                                   observedGroup = pgrp;
                                   observedSignal = sig;

                                   return 0;
                               });

        Assert.Equal (0, observedGroup); // 0 == caller's own process group
        Assert.Equal (20, observedSignal);
    }
}

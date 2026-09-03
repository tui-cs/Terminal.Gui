using System.Runtime.InteropServices;
using Terminal.Gui.Drivers;

namespace DriverTests;

/// <summary>
///     Tests for <see cref="UnixIOHelper"/> polling semantics on Unix-like platforms.
/// </summary>
[Collection ("Driver Tests")]
public class UnixIOHelperTests
{
    [DllImport ("libc", SetLastError = true)]
    private static extern int pipe (int [] pipefd);

    [DllImport ("libc", SetLastError = true)]
    private static extern int close (int fd);

    [Fact]
    public void TryWriteAll_ReturnsFalse_WhenWriteReturnsZero ()
    {
        var calls = 0;
        byte [] payload = [0x41, 0x42];

        bool result = UnixIOHelper.TryWriteAll (1,
                                                payload,
                                                (_, _, _) =>
                                                {
                                                    calls++;

                                                    return 0;
                                                });

        Assert.False (result);
        Assert.Equal (1, calls);
    }

    [Fact]
    public void TryWriteAll_RetriesUntilAllBytesWritten ()
    {
        List<int> counts = [];
        List<byte> firstBytes = [];
        byte [] payload = [0x41, 0x42, 0x43];

        bool result = UnixIOHelper.TryWriteAll (1,
                                                payload,
                                                (_, buffer, count) =>
                                                {
                                                    counts.Add (count);
                                                    firstBytes.Add (buffer [0]);

                                                    return 1;
                                                });

        Assert.True (result);
        Assert.Equal ([3, 2, 1], counts);
        Assert.Equal ([0x41, 0x42, 0x43], firstBytes);
    }

    [Fact]
    // Copilot
    public void IsInputAvailable_ReturnsTrue_WhenPollReportsReadableData ()
    {
        if (OperatingSystem.IsWindows ())
        {
            return;
        }

        int [] pipeFds = new int [2];
        Assert.Equal (0, pipe (pipeFds));

        try
        {
            byte [] payload = [0x41];
            Assert.Equal (1, UnixIOHelper.write (pipeFds [1], payload, payload.Length));

            UnixIOHelper.Pollfd [] pollMap =
            [
                new ()
                {
                    fd = pipeFds [0],
                    events = (short)UnixIOHelper.Condition.PollIn
                }
            ];

            Assert.True (UnixIOHelper.IsInputAvailable (pollMap, 0));
        }
        finally
        {
            close (pipeFds [0]);
            close (pipeFds [1]);
        }
    }

    [Fact]
    // Copilot
    public void IsInputAvailable_ReturnsFalse_WhenPollReportsNonReadableEvent ()
    {
        if (OperatingSystem.IsWindows ())
        {
            return;
        }

        int [] pipeFds = new int [2];
        Assert.Equal (0, pipe (pipeFds));

        try
        {
            Assert.Equal (0, close (pipeFds [0]));

            UnixIOHelper.Pollfd [] pollMap =
            [
                new ()
                {
                    fd = pipeFds [0],
                    events = (short)UnixIOHelper.Condition.PollIn
                }
            ];

            Assert.False (UnixIOHelper.IsInputAvailable (pollMap, 0));
        }
        finally
        {
            close (pipeFds [1]);
        }
    }
}

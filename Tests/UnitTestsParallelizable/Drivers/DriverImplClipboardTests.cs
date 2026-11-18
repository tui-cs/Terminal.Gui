namespace UnitTestsParallelizable.Drivers;

public class DriverImplClipboardTests
{
    [Fact]
    public void UnixClipboard_Set_Then_Get_RoundTrips_Through_Xclip_Runner ()
    {
        UnixClipboardProcessRunner processRunner = new ();
        UnixClipboard clipboard = new (processRunner);

        Assert.True (clipboard.IsSupported);

        clipboard.SetClipboardData ("hello");

        Assert.Equal ("hello", clipboard.GetClipboardData ());
        Assert.Contains ("-selection clipboard -i", processRunner.LastSetCommandLine);
        Assert.Contains ("-selection clipboard -o", processRunner.LastGetCommandLine);
    }

    [Fact]
    public void CreateClipboard_Linux_AttachedToTerminal_And_UnixSupported_Uses_UnixClipboard ()
    {
        FakeClipboard fallbackClipboard = new ();
        FakeClipboard unixClipboard = new ();

        IClipboard clipboard = DriverImpl.CreateClipboard (isWindows: () => false,
                                                           isMac: () => false,
                                                           isWsl: () => false,
                                                           isLinux: () => true,
                                                           createWindowsClipboard: () => throw new InvalidOperationException (),
                                                           createMacClipboard: () => throw new InvalidOperationException (),
                                                           createWslClipboard: () => throw new InvalidOperationException (),
                                                           createUnixClipboard: () => unixClipboard,
                                                           fallbackClipboard: fallbackClipboard);

        Assert.Same (unixClipboard, clipboard);
    }

    [Fact]
    public void CreateClipboard_NotLinux_Uses_FallbackClipboard ()
    {
        FakeClipboard fallbackClipboard = new ();

        IClipboard clipboard = DriverImpl.CreateClipboard (isWindows: () => false,
                                                           isMac: () => false,
                                                           isWsl: () => false,
                                                           isLinux: () => false,
                                                           createWindowsClipboard: () => throw new InvalidOperationException (),
                                                           createMacClipboard: () => throw new InvalidOperationException (),
                                                           createWslClipboard: () => throw new InvalidOperationException (),
                                                           createUnixClipboard: () => throw new InvalidOperationException (),
                                                           fallbackClipboard: fallbackClipboard);

        Assert.Same (fallbackClipboard, clipboard);
    }

    [Fact]
    public void CreateClipboard_Linux_AttachedToTerminal_But_UnixUnsupported_Uses_FallbackClipboard ()
    {
        FakeClipboard fallbackClipboard = new ();
        FakeClipboard unsupportedUnixClipboard = new (isSupportedAlwaysFalse: true);

        IClipboard clipboard = DriverImpl.CreateClipboard (isWindows: () => false,
                                                           isMac: () => false,
                                                           isWsl: () => false,
                                                           isLinux: () => true,
                                                           createWindowsClipboard: () => throw new InvalidOperationException (),
                                                           createMacClipboard: () => throw new InvalidOperationException (),
                                                           createWslClipboard: () => throw new InvalidOperationException (),
                                                           createUnixClipboard: () => unsupportedUnixClipboard,
                                                           fallbackClipboard: fallbackClipboard);

        Assert.Same (fallbackClipboard, clipboard);
    }

    private sealed class UnixClipboardProcessRunner : IClipboardProcessRunner
    {
        private string _clipboardText = string.Empty;

        public string LastGetCommandLine { get; private set; } = string.Empty;
        public string LastSetCommandLine { get; private set; } = string.Empty;

        public (int exitCode, string result) Bash (string commandLine, string inputText = "", bool waitForOutput = false)
        {
            if (commandLine == "which xclip")
            {
                return (0, "/usr/bin/xclip");
            }

            if (commandLine.Contains ("-selection clipboard -i", StringComparison.Ordinal))
            {
                LastSetCommandLine = commandLine;
                _clipboardText = inputText;

                return (0, string.Empty);
            }

            if (!commandLine.Contains ("-selection clipboard -o", StringComparison.Ordinal))
            {
                return (1, string.Empty);
            }
            LastGetCommandLine = commandLine;
            int redirectIndex = commandLine.IndexOf (" > ", StringComparison.Ordinal);
            string tempFileName = commandLine [(redirectIndex + 3)..];

            File.WriteAllText (tempFileName, _clipboardText);

            return (0, string.Empty);
        }

        public (int exitCode, string result) Process (string cmd, string arguments, string? input = null, bool waitForOutput = true)
        {
            throw new NotSupportedException ();
        }
    }
}

using System.Text;

namespace UnitTestsParallelizable.Drivers;

public class WSLClipboardTests
{
    [Fact]
    public void TextField_Copy_Then_Paste_RoundTrips_Cjk_Text_Through_WslClipboard ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        WslClipboardProcessRunner processRunner = new ();
        app.Driver!.Clipboard = new WSLClipboard (processRunner, "pwsh.exe");

        using Runnable runnable = new ();
        TextField textField = new () { Width = 10 };
        runnable.Add (textField);
        app.Begin (runnable);

        processRunner.SetExternalClipboardText ("腌");

        Assert.True (textField.Paste ());
        Assert.Equal ("腌", textField.Text);

        Assert.True (textField.SelectAll ());
        Assert.True (textField.Copy ());

        Assert.True (textField.MoveEnd ());
        Assert.True (textField.Paste ());

        Assert.Equal (0, processRunner.BashCallCount);
        Assert.DoesNotContain ("$input", processRunner.LastSetArguments);
        Assert.True (processRunner.LastSetInputWasAscii);
        Assert.Equal ("腌腌", textField.Text);
    }

    [Fact]
    public void SetClipboardData_Sends_Ascii_Base64_Through_Windows_PowerShell_Stdin ()
    {
        WslClipboardProcessRunner processRunner = new ();
        WSLClipboard clipboard = new (processRunner, "pwsh.exe");

        clipboard.SetClipboardData ("腌");

        Assert.Equal (0, processRunner.BashCallCount);
        Assert.DoesNotContain ("$input", processRunner.LastSetArguments);
        Assert.True (processRunner.LastSetInputWasAscii);
        Assert.Equal ("腌", clipboard.GetClipboardData ());
    }

    private sealed class WslClipboardProcessRunner : IClipboardProcessRunner
    {
        private string _clipboardText = string.Empty;

        public string LastSetArguments { get; private set; } = string.Empty;
        public bool LastSetInputWasAscii { get; private set; }
        public int BashCallCount { get; private set; }

        public void SetExternalClipboardText (string text)
        {
            _clipboardText = text;
        }

        public (int exitCode, string result) Bash (string commandLine, string inputText = "", bool waitForOutput = false)
        {
            BashCallCount++;

            return (0, "pwsh.exe");
        }

        public (int exitCode, string result) Process (string cmd, string arguments, string? input = null, bool waitForOutput = true)
        {
            if (!arguments.Contains ("Set-Clipboard"))
            {
                return (0, _clipboardText);
            }
            LastSetArguments = arguments;
            LastSetInputWasAscii = IsAscii (input ?? string.Empty);

            if (arguments.Contains ("FromBase64String"))
            {
                byte [] bytes = Convert.FromBase64String (input ?? string.Empty);
                _clipboardText = Encoding.Unicode.GetString (bytes);
            }
            else
            {
                _clipboardText = "Þàî";
            }

            return (0, string.Empty);
        }

        private static bool IsAscii (string text)
        {
            foreach (char ch in text)
            {
                if (ch > 127)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

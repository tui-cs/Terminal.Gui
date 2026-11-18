namespace Terminal.Gui.App;

internal sealed class ClipboardProcessRunnerImpl : IClipboardProcessRunner
{
    public (int exitCode, string result) Bash (string commandLine, string inputText = "", bool waitForOutput = false)
    {
        return ClipboardProcessRunner.Bash (commandLine, inputText, waitForOutput);
    }

    public (int exitCode, string result) Process (string cmd, string arguments, string? input = null, bool waitForOutput = true)
    {
        return ClipboardProcessRunner.Process (cmd, arguments, input, waitForOutput);
    }
}

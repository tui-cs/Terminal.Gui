namespace Terminal.Gui.App;

internal interface IClipboardProcessRunner
{
    (int exitCode, string result) Bash (string commandLine, string inputText = "", bool waitForOutput = false);
    (int exitCode, string result) Process (string cmd, string arguments, string? input = null, bool waitForOutput = true);
}

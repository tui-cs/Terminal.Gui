using System.Diagnostics;

namespace Terminal.Gui.App;

/// <summary>
///     Helper class for console drivers to invoke shell commands to interact with the clipboard. Used primarily by
///     UnixDriver, but also used in Unit tests which is why it is in IDriver.cs.
/// </summary>
internal static class ClipboardProcessRunner
{
    public static (int exitCode, string result) Bash (
        string commandLine,
        string inputText = "",
        bool waitForOutput = false
    )
    {
        var arguments = $"-c \"{commandLine}\"";
        (int exitCode, string result) = Process ("bash", arguments, inputText, waitForOutput);

        return (exitCode, result.TrimEnd ());
    }

    public static bool FileExists (this string value) { return !string.IsNullOrEmpty (value) && !value.Contains ("not found"); }

    public static (int exitCode, string result) Process (
        string cmd,
        string arguments,
        string? input = null,
        bool waitForOutput = true
    )
    {
        var output = string.Empty;

        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return (-1, output);
        }

        using var process = new Process ();

        process.StartInfo = new ()
        {
            FileName = cmd,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start ();

        if (!string.IsNullOrEmpty (input))
        {
            process.StandardInput.Write (input);
            process.StandardInput.Close ();
        }

        if (!process.WaitForExit (5000))
        {
            var timeoutError =
                $@"Process timed out. Command line: {process.StartInfo.FileName} {process.StartInfo.Arguments}.";

            throw new TimeoutException (timeoutError);
        }

        if (waitForOutput && process.StandardOutput.Peek () != -1)
        {
            output = process.StandardOutput.ReadToEnd ();
        }

        if (process.ExitCode > 0)
        {
            output = $@"Process failed to run. Command line: {cmd} {arguments}.
										Output: {output}
										Error: {process.StandardError.ReadToEnd ()}";
        }

        return (process.ExitCode, output);
    }
}

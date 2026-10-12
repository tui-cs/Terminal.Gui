using System.Runtime.CompilerServices;
using System.Text;

namespace AppTestHelpers;

public partial class AppTestHelper
{
    /// <summary>
    ///     Asserts the current screen against a golden <b>ANSI</b> snapshot file.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Captures the screen via <c>IDriver.ToAnsi ()</c> — the exact escape-sequence stream
    ///         the driver would write to recreate it (truecolor, bold, reverse, blink, layout),
    ///         excluding the terminal cursor (a separate, non-deterministic <c>SetCursor</c>, so
    ///         snapshots stay stable). Row separators are normalized to <c>\n</c> so the same
    ///         golden compares on every platform. The recorded <c>.ans</c> file <i>is</i> the look:
    ///         <c>cat &lt;file&gt;.ans</c> in a truecolor terminal reproduces the screen exactly.
    ///     </para>
    ///     <para>
    ///         Complements <see cref="AnsiScreenShot" /> (which only dumps to a writer): this
    ///         records on first run (or when the <c>UPDATE_SNAPSHOTS</c> environment variable is
    ///         <c>1</c>/<c>true</c>) and otherwise compares byte-for-byte. On mismatch it writes a
    ///         sibling <c>.ans.actual</c> and throws with the plain-text render inline plus the
    ///         <c>cat</c> commands — enough to verify the look without an interactive run. Set
    ///         <c>SNAPSHOT_DIR</c> to override the golden root (default: <c>__snapshots__/</c>
    ///         beside the calling test source).
    ///     </para>
    /// </remarks>
    /// <param name="name">Snapshot name, unique within the test (becomes <c>&lt;name&gt;.ans</c>).</param>
    /// <param name="callerFile">Compiler-supplied; locates <c>__snapshots__/</c> beside the test.</param>
    /// <returns>This <see cref="AppTestHelper" /> (fluent).</returns>
    public AppTestHelper AssertAnsiSnapshot (string name, [CallerFilePath] string callerFile = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace (name);

        string? ansi = null;
        string? plain = null;

        WaitIteration (app =>
                       {
                           ansi = app.Driver?.ToAnsi ();
                           plain = app.Driver?.ToString ();
                       });

        ansi = NormalizeAnsiLineEndings (ansi ?? string.Empty);

        string dir = SnapshotDirectory (callerFile);
        Directory.CreateDirectory (dir);
        string path = Path.Combine (dir, name + ".ans");

        bool update = Environment.GetEnvironmentVariable ("UPDATE_SNAPSHOTS") is "1" or "true";

        if (update || !File.Exists (path))
        {
            // Byte-exact, UTF-8 without BOM, no newline translation: the file must remain a
            // faithful `cat`-able reproduction of the terminal stream. Mark *.ans `binary` in
            // .gitattributes so core.autocrlf cannot corrupt it.
            File.WriteAllText (path, ansi, new UTF8Encoding (false));

            return this;
        }

        string expected = NormalizeAnsiLineEndings (File.ReadAllText (path));

        if (string.Equals (expected, ansi, StringComparison.Ordinal))
        {
            return this;
        }

        string actualPath = path + ".actual";
        File.WriteAllText (actualPath, ansi, new UTF8Encoding (false));

        AnsiSnapshotException exception = new (
            $"""
             ANSI snapshot '{name}' did not match {path}.

             Plain-text render of the actual screen (glyphs only — colors/styles omitted):
             ----------------------------------------------------------------------
             {plain}
             ----------------------------------------------------------------------

             Exact look (with colors/styles): cat '{actualPath}'
             Expected look:                   cat '{path}'

             If this change is intended, accept it by re-running with UPDATE_SNAPSHOTS=1
             (or copy the .actual over the .ans).
             """);

        Stop ();

        throw exception;
    }

    private static string SnapshotDirectory (string callerFile)
    {
        string? overrideDir = Environment.GetEnvironmentVariable ("SNAPSHOT_DIR");

        if (!string.IsNullOrWhiteSpace (overrideDir))
        {
            return overrideDir;
        }

        string callerPath = callerFile;

        // On Linux, Visual Studio WSL testing can supply a Windows-style caller path.
        // Windows accepts both '\\' and '/' in drive-qualified paths, so keep both.
        if (OperatingSystem.IsLinux () && IsWindowsStylePath (callerFile))
        {
            callerPath = ToWslPath (callerFile);
        }

        string? sourceDir = Path.GetDirectoryName (callerPath);

        if (string.IsNullOrEmpty (sourceDir))
        {
            throw new InvalidOperationException (
                "Could not resolve the snapshot directory from the caller path. Set SNAPSHOT_DIR.");
        }

        return Path.Combine (sourceDir, "__snapshots__");
    }

    internal static bool IsWindowsStylePath (string path)
    {
        return path.Length >= 3
               && char.IsAsciiLetter (path [0])
               && path [1] == ':'
               && (path [2] == '\\' || path [2] == '/');
    }

    internal static string ToWslPath (string windowsPath)
    {
        return $"/mnt/{char.ToLowerInvariant (windowsPath [0])}/{windowsPath [3..].Replace ('\\', '/')}";
    }

    private static string NormalizeAnsiLineEndings (string ansi) => ansi.Replace ("\r\n", "\n").Replace ("\r", "\n");
}

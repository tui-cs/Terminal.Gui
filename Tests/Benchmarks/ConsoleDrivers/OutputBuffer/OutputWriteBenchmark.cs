using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Terminal.Gui.Drivers;

namespace Terminal.Gui.Benchmarks.ConsoleDrivers.OutputBuffer;

/// <summary>
///     Measures output flushing for full and sparse 270x72 frames.
/// </summary>
/// <remarks>
///     The sparse frame matches issue #5627: the first and last cells of every row are dirty,
///     with a large clean gap between them.
///     <para>
///         Run the BenchmarkDotNet cases with:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter "*OutputWriteBenchmark*"</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Output", "Latency")]
public class OutputWriteBenchmark
{
    private const int COLS = 270;
    private const int ROWS = 72;
    private const int CONPTY_WARMUP_FRAMES = 3;
    private const int CONPTY_MEASURED_FRAMES = 20;

    private OutputBufferImpl _fullBuffer = null!;
    private MeasuringOutput _output = null!;
    private OutputBufferImpl _sparseBuffer = null!;

    /// <summary>
    ///     Creates and warms the output buffers before benchmarking.
    /// </summary>
    [GlobalSetup]
    public void Setup ()
    {
        _fullBuffer = CreateBuffer ();
        _sparseBuffer = CreateBuffer ();
        _output = new ();

        _output.Write (_fullBuffer);
        _output.Write (_sparseBuffer);
    }

    /// <summary>
    ///     Marks every cell dirty before measuring a full-frame flush.
    /// </summary>
    [IterationSetup (Target = nameof (FullFrame))]
    public void PrepareFullFrame ()
    {
        MarkFullFrame (_fullBuffer);
        _output.ResetCounters ();
    }

    /// <summary>
    ///     Marks only the first and last cells of every row dirty before measuring a sparse flush.
    /// </summary>
    [IterationSetup (Target = nameof (SparseBorderFrame))]
    public void PrepareSparseBorderFrame ()
    {
        MarkSparseBorderFrame (_sparseBuffer);
        _output.ResetCounters ();
    }

    /// <summary>
    ///     Flushes a frame in which every cell is dirty.
    /// </summary>
    [Benchmark (Baseline = true)]
    public long FullFrame ()
    {
        _output.Write (_fullBuffer);

        return _output.CharactersWritten;
    }

    /// <summary>
    ///     Flushes a frame with dirty border cells separated by clean interior cells.
    /// </summary>
    [Benchmark]
    public long SparseBorderFrame ()
    {
        _output.Write (_sparseBuffer);

        return _output.CharactersWritten;
    }

    /// <summary>
    ///     Measures sparse-frame output through <see cref="WindowsOutput"/> under Windows Terminal or ConPTY
    ///     and writes the timing summary to <paramref name="resultPath"/>.
    /// </summary>
    /// <param name="resultPath">Path for the JSON timing summary.</param>
    public static void RunConPty (string resultPath)
    {
        if (!OperatingSystem.IsWindows ())
        {
            throw new PlatformNotSupportedException ("The ConPTY output benchmark requires Windows.");
        }

        if (Console.IsOutputRedirected)
        {
            throw new InvalidOperationException ("Run the ConPTY output benchmark from Windows Terminal or tuirec.");
        }

        if (string.IsNullOrWhiteSpace (resultPath))
        {
            throw new ArgumentException ("A result path is required.", nameof (resultPath));
        }

        OutputBufferImpl buffer = CreateBuffer ();
        double [] samples = new double [CONPTY_MEASURED_FRAMES];

        using (WindowsOutput output = new ())
        {
            output.Write (buffer);

            for (var frame = 0; frame < CONPTY_WARMUP_FRAMES; frame++)
            {
                MarkSparseBorderFrame (buffer);
                output.Write (buffer);
            }

            for (var frame = 0; frame < CONPTY_MEASURED_FRAMES; frame++)
            {
                MarkSparseBorderFrame (buffer);
                long started = Stopwatch.GetTimestamp ();
                output.Write (buffer);
                samples [frame] = Stopwatch.GetElapsedTime (started).TotalMilliseconds;
            }
        }

        Array.Sort (samples);
        int p95Index = (int)Math.Ceiling (samples.Length * 0.95) - 1;
        double median = (samples [samples.Length / 2 - 1] + samples [samples.Length / 2]) / 2;
        string fullResultPath = Path.GetFullPath (resultPath);
        string? resultDirectory = Path.GetDirectoryName (fullResultPath);

        if (!string.IsNullOrEmpty (resultDirectory))
        {
            Directory.CreateDirectory (resultDirectory);
        }

        string json = JsonSerializer.Serialize (new
        {
            Columns = COLS,
            Rows = ROWS,
            WarmupFrames = CONPTY_WARMUP_FRAMES,
            MeasuredFrames = CONPTY_MEASURED_FRAMES,
            MeanMilliseconds = samples.Average (),
            MedianMilliseconds = median,
            P95Milliseconds = samples [p95Index],
            MinMilliseconds = samples [0],
            MaxMilliseconds = samples [^1]
        }, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText (fullResultPath, json);
    }

    private static OutputBufferImpl CreateBuffer ()
    {
        OutputBufferImpl buffer = new ();
        buffer.SetSize (COLS, ROWS);

        for (var row = 0; row < ROWS; row++)
        {
            buffer.Move (0, row);
            buffer.AddRune ('|');
            buffer.Move (COLS - 1, row);
            buffer.AddRune ('|');
        }

        return buffer;
    }

    private static void MarkFullFrame (OutputBufferImpl buffer)
    {
        for (var row = 0; row < ROWS; row++)
        {
            for (var col = 0; col < COLS; col++)
            {
                buffer.Contents! [row, col].IsDirty = true;
            }

            buffer.DirtyLines [row] = true;
        }
    }

    private static void MarkSparseBorderFrame (OutputBufferImpl buffer)
    {
        for (var row = 0; row < ROWS; row++)
        {
            buffer.Contents! [row, 0].IsDirty = true;
            buffer.Contents [row, COLS - 1].IsDirty = true;
            buffer.DirtyLines [row] = true;
        }
    }

    private sealed class MeasuringOutput : OutputBase
    {
        public long CharactersWritten { get; private set; }

        public int CursorMoves { get; private set; }

        public int Writes { get; private set; }

        public void ResetCounters ()
        {
            CharactersWritten = 0;
            CursorMoves = 0;
            Writes = 0;
        }

        protected override bool SetCursorPositionImpl (int screenPositionX, int screenPositionY)
        {
            CursorMoves++;

            StringBuilder sequence = new ();
            EscSeqUtils.CSI_AppendCursorPosition (sequence, screenPositionY + 1, screenPositionX + 1);
            string cursorSequence = sequence.ToString ();
            CharactersWritten += cursorSequence.Length;

            return true;
        }

        protected override void Write (StringBuilder output)
        {
            Writes++;
            CharactersWritten += output.Length;
        }
    }
}

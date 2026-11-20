using BenchmarkDotNet.Attributes;
using Terminal.Gui.Drivers;
using Terminal.Gui.Drawing;

namespace Terminal.Gui.Benchmarks.ConsoleDrivers.OutputBuffer;

/// <summary>Measures the complete UnixRaw frame writer without terminal I/O.</summary>
[MemoryDiagnoser]
[BenchmarkCategory ("Output", "Latency")]
public class UnixRawOutputBenchmark
{
    private OutputBufferImpl _small = null!;
    private OutputBufferImpl _large = null!;
    private OutputBufferImpl _colored = null!;
    private AnsiOutput _output = null!;
    private long _bytes;
    private int _writes;

    /// <summary>Builds and warms the small (80×25), large (240×70), and per-cell colored (400×110) frames.</summary>
    [GlobalSetup]
    public void Setup ()
    {
        _small = CreateBuffer (80, 25);
        _large = CreateBuffer (240, 70);
        _colored = CreateBuffer (400, 110);

        for (int row = 0; row < _colored.Rows; row++)
        {
            for (int col = 0; col < _colored.Cols; col++)
            {
                _colored.Contents! [row, col].Attribute = new (new Color (col % 256, row % 256, (col + row) % 256), Color.Black);
            }
        }
        _output = new (bytes =>
        {
            _writes++;
            _bytes += bytes.Length;
            return true;
        }) { CaptureOutput = false };

        _output.Write (_small);

        if (_writes == 0 || _bytes == 0)
        {
            throw new InvalidOperationException ("The UnixRaw benchmark did not write the warmup frame.");
        }

        _output.Write (_large);
        _output.Write (_colored);
    }

    /// <summary>Marks the 80×25 frame dirty before each measurement.</summary>
    public void PrepareSmall ()
    {
        MarkDirty (_small);
        _writes = 0;
        _bytes = 0;
    }

    /// <summary>Marks the 240×70 frame dirty before each measurement.</summary>
    public void PrepareLarge ()
    {
        MarkDirty (_large);
        _writes = 0;
        _bytes = 0;
    }

    /// <summary>Flushes a fully dirty 80×25 frame.</summary>
    [Benchmark]
    public long SmallFrame ()
    {
        PrepareSmall ();
        _output.Write (_small);

        if (_writes != 1)
        {
            throw new InvalidOperationException ($"Expected one UnixRaw write, got {_writes}.");
        }

        return _bytes;
    }

    /// <summary>Flushes a fully dirty 240×70 frame.</summary>
    [Benchmark]
    public long LargeFrame ()
    {
        PrepareLarge ();
        _output.Write (_large);

        if (_writes != 1)
        {
            throw new InvalidOperationException ($"Expected one UnixRaw write, got {_writes}.");
        }

        return _bytes;
    }

    /// <summary>Flushes a 400×110 frame with a truecolor change at every cell.</summary>
    [Benchmark]
    public long PerCellColorFrame ()
    {
        MarkDirty (_colored);
        _writes = 0;
        _bytes = 0;
        _output.Write (_colored);

        if (_writes < 2 || _bytes <= 1024 * 1024)
        {
            throw new InvalidOperationException ("The colored frame did not exercise cumulative staging.");
        }

        return _bytes;
    }

    private static OutputBufferImpl CreateBuffer (int cols, int rows)
    {
        OutputBufferImpl buffer = new ();
        buffer.SetSize (cols, rows);
        string text = new ('A', cols);

        for (int row = 0; row < rows; row++)
        {
            buffer.Move (0, row);
            buffer.AddStr (text);
        }

        return buffer;
    }

    private static void MarkDirty (OutputBufferImpl buffer)
    {
        for (int row = 0; row < buffer.Rows; row++)
        {
            buffer.DirtyLines [row] = true;

            for (int col = 0; col < buffer.Cols; col++)
            {
                buffer.Contents! [row, col].IsDirty = true;
            }
        }
    }
}

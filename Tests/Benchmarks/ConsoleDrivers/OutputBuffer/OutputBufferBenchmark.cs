using System.Drawing;
using System.Text;
using BenchmarkDotNet.Attributes;
using Terminal.Gui.Drivers;
using Attribute = Terminal.Gui.Drawing.Attribute;
using Color = Terminal.Gui.Drawing.Color;

namespace Terminal.Gui.Benchmarks.ConsoleDrivers.OutputBuffer;

/// <summary>
///     Benchmarks for <see cref="OutputBufferImpl"/> operations.
///     Measures the performance of AddStr, FillRect, ClearContents, and SetSize
///     to establish a baseline before and after the _contentsLock fix (#5130).
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory ("OutputBuffer")]
public class OutputBufferBenchmark
{
    private OutputBufferImpl _buffer = null!;

    [Params (80, 200)]
    public int Cols { get; set; }

    [Params (25, 50)]
    public int Rows { get; set; }

    [GlobalSetup]
    public void Setup ()
    {
        _buffer = new ();
        _buffer.SetSize (Cols, Rows);
        _buffer.CurrentAttribute = new Attribute (Color.White, Color.Black);
    }

    /// <summary>
    ///     Baseline: Write a full row of text using AddStr.
    /// </summary>
    [Benchmark (Baseline = true)]
    public void AddStr_FullRow ()
    {
        _buffer.Move (0, 0);
        _buffer.AddStr (new string ('A', Cols));
    }

    /// <summary>
    ///     Write text to every row in the buffer.
    /// </summary>
    [Benchmark]
    public void AddStr_AllRows ()
    {
        string line = new ('A', Cols);

        for (var row = 0; row < Rows; row++)
        {
            _buffer.Move (0, row);
            _buffer.AddStr (line);
        }
    }

    /// <summary>
    ///     Fill the entire screen using FillRect(Rectangle, Rune).
    /// </summary>
    [Benchmark]
    public void FillRect_FullScreen ()
    {
        _buffer.FillRect (new Rectangle (0, 0, Cols, Rows), new Rune (' '));
    }

    /// <summary>
    ///     ClearContents resets the entire buffer.
    /// </summary>
    [Benchmark]
    public void ClearContents ()
    {
        _buffer.ClearContents ();
    }

    /// <summary>
    ///     SetSize triggers ClearContents internally.
    /// </summary>
    [Benchmark]
    public void SetSize ()
    {
        _buffer.SetSize (Cols, Rows);
    }

    /// <summary>
    ///     Simulates a typical draw cycle: clear, then fill every row.
    /// </summary>
    [Benchmark]
    public void TypicalDrawCycle ()
    {
        _buffer.ClearContents ();
        string line = new ('X', Cols);

        for (var row = 0; row < Rows; row++)
        {
            _buffer.Move (0, row);
            _buffer.AddStr (line);
        }
    }
}

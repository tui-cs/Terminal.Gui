using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui.Text;

namespace Terminal.Gui.Benchmarks.Text.RuneExtensions;

/// <summary>
/// Benchmarks for <see cref="Tui.RuneExtensions.IsSurrogatePair"/> performance fine-tuning.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.RuneExtensions))]
public class IsSurrogatePair
{
    /// <summary>
    /// Benchmark for previous implementation.
    /// </summary>
    /// <param name="rune"></param>
    [Benchmark]
    [ArgumentsSource (nameof (DataSource))]
    public bool Previous (Rune rune)
    {
        return WithToString (rune);
    }

    /// <summary>
    /// Benchmark for current implementation.
    /// 
    /// Avoids intermediate heap allocations by using stack allocated buffer.
    /// </summary>
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public bool Current (Rune rune)
    {
        return Tui.RuneExtensions.IsSurrogatePair (rune);
    }

    /// <summary>
    /// Previous implementation with intermediate string allocation.
    /// </summary>
    private static bool WithToString (Rune rune)
    {
        return char.IsSurrogatePair (rune.ToString (), 0);
    }

    public static IEnumerable<object> DataSource ()
    {
        yield return new Rune ('a');
        yield return "𝔹".EnumerateRunes ().Single ();
    }
}

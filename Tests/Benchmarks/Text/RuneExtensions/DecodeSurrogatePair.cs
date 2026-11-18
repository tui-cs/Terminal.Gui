using System.Text;
using BenchmarkDotNet.Attributes;
using Terminal.Gui.Text;
using Tui = Terminal.Gui.Text;

namespace Terminal.Gui.Benchmarks.Text.RuneExtensions;

/// <summary>
/// Benchmarks for <see cref="DecodeSurrogatePair"/> performance fine-tuning.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.RuneExtensions))]
public class DecodeSurrogatePair
{
    /// <summary>
    /// Benchmark for previous implementation.
    /// </summary>
    /// <param name="rune"></param>
    /// <returns></returns>
    [Benchmark]
    [ArgumentsSource (nameof (DataSource))]
    public char []? Previous (Rune rune)
    {
        _ = RuneToStringToCharArray (rune, out char []? chars);
        return chars;
    }

    /// <summary>
    /// Benchmark for current implementation.
    /// 
    /// Utilizes Rune methods that take Span argument avoiding intermediate heap array allocation when combined with stack allocated intermediate buffer.
    /// When rune is not surrogate pair there will be no heap allocation.
    /// 
    /// Final surrogate pair array allocation cannot be avoided due to the current method signature design.
    /// Changing the method signature, or providing an alternative method, to take a destination Span would allow further optimizations by allowing caller to reuse buffer for consecutive calls.
    /// </summary>
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public char []? Current (Rune rune)
    {
        _ = Tui.RuneExtensions.DecodeSurrogatePair (rune, out char []? chars);
        return chars;
    }

    /// <summary>
    /// Previous implementation with intermediate string allocation.
    /// 
    /// The IsSurrogatePair implementation at the time had hidden extra string allocation so there were intermediate heap allocations even if rune is not surrogate pair.
    /// </summary>
    private static bool RuneToStringToCharArray (Rune rune, out char []? chars)
    {
        if (rune.IsSurrogatePair ())
        {
            chars = rune.ToString ().ToCharArray ();
            return true;
        }

        chars = null;
        return false;
    }

    public static IEnumerable<object> DataSource ()
    {
        yield return new Rune ('a');
        yield return "𝔹".EnumerateRunes ().Single ();
    }
}

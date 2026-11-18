using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui.Text;

namespace Terminal.Gui.Benchmarks.Text.RuneExtensions;

/// <summary>
/// Benchmarks for <see cref="Tui.RuneExtensions.Encode"/> performance fine-tuning.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.RuneExtensions))]
public class Encode
{
    /// <summary>
    /// Benchmark for previous implementation.
    /// </summary>
    [Benchmark]
    [ArgumentsSource (nameof (DataSource))]
    public byte [] Previous (Rune rune, byte [] destination, int start, int count)
    {
        _ = StringEncodingGetBytes (rune, destination, start, count);
        return destination;
    }

    /// <summary>
    /// Benchmark for current implementation.
    /// 
    /// Avoids intermediate heap allocations with stack allocated intermediate buffer.
    /// </summary>
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public byte [] Current (Rune rune, byte [] destination, int start, int count)
    {
        _ = Tui.RuneExtensions.Encode (rune, destination, start, count);
        return destination;
    }

    /// <summary>
    /// Previous implementation with intermediate byte array and string allocation.
    /// </summary>
    private static int StringEncodingGetBytes (Rune rune, byte [] dest, int start = 0, int count = -1)
    {
        byte [] bytes = Encoding.UTF8.GetBytes (rune.ToString ());
        var length = 0;

        for (var i = 0; i < (count == -1 ? bytes.Length : count); i++)
        {
            if (bytes [i] == 0)
            {
                break;
            }

            dest [start + i] = bytes [i];
            length++;
        }

        return length;
    }

    public static IEnumerable<object []> DataSource ()
    {
        Rune[] runes = [ new Rune ('a'),"𝔞".EnumerateRunes().Single() ];

        foreach (var rune in runes)
        {
            yield return new object [] { rune, new byte [16], 0, -1 };
            yield return new object [] { rune, new byte [16], 8, -1 };
            // Does not work in original implementation
            //yield return new object [] { rune, new byte [16], 8, 8 };
        }
    }
}

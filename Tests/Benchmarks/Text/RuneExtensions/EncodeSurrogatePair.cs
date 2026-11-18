using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui.Text;

namespace Terminal.Gui.Benchmarks.Text.RuneExtensions;

/// <summary>
/// Benchmarks for <see cref="Tui.RuneExtensions.EncodeSurrogatePair"/> performance fine-tuning.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.RuneExtensions))]
public class EncodeSurrogatePair
{
    /// <summary>
    /// Benchmark for current implementation.
    /// </summary>
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public Rune Current (char highSurrogate, char lowSurrogate)
    {
        _ = Tui.RuneExtensions.EncodeSurrogatePair (highSurrogate, lowSurrogate, out Rune rune);
        return rune;
    }

    public static IEnumerable<object []> DataSource ()
    {
        string[] runeStrings = ["🍕", "🧠", "🌹"];
        foreach (string symbol in runeStrings)
        {
            if (symbol is [char high, char low])
            {
                yield return [high, low];
            }
        }
    }
}

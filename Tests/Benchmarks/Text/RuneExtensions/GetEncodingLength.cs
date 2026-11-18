using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui.Text;

namespace Terminal.Gui.Benchmarks.Text.RuneExtensions;

/// <summary>
/// Benchmarks for <see cref="Tui.RuneExtensions.GetEncodingLength"/> performance fine-tuning.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.RuneExtensions))]
public class GetEncodingLength
{
    /// <summary>
    /// Benchmark for previous implementation.
    /// </summary>
    [Benchmark]
    [ArgumentsSource (nameof (DataSource))]
    public int Previous (Rune rune, PrettyPrintedEncoding encoding)
    {
        return WithEncodingGetBytesArray (rune, encoding);
    }

    /// <summary>
    /// Benchmark for current implementation.
    /// </summary>
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public int Current (Rune rune, PrettyPrintedEncoding encoding)
    {
        return Tui.RuneExtensions.GetEncodingLength (rune, encoding);
    }

    /// <summary>
    /// Previous implementation with intermediate byte array, string, and char array allocation.
    /// </summary>
    private static int WithEncodingGetBytesArray (Rune rune, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        byte [] bytes = encoding.GetBytes (rune.ToString ().ToCharArray ());
        var offset = 0;

        if (bytes [^1] == 0)
        {
            offset++;
        }

        return bytes.Length - offset;
    }

    public static IEnumerable<object []> DataSource ()
    {
        PrettyPrintedEncoding[] encodings = [ new(Encoding.UTF8), new(Encoding.Unicode), new(Encoding.UTF32) ];
        Rune[] runes = [ new Rune ('a'), "𝔹".EnumerateRunes ().Single () ];

        foreach (var encoding in encodings)
        {
            foreach (Rune rune in runes)
            {
                yield return [rune, encoding];
            }
        }
    }

    /// <summary>
    /// <see cref="System.Text.Encoding"/> wrapper to display proper encoding name in benchmark results.
    /// </summary>
    public record PrettyPrintedEncoding (Encoding Encoding)
    {
        public static implicit operator Encoding (PrettyPrintedEncoding ppe) => ppe.Encoding;

        public override string ToString () => Encoding.HeaderName;
    }
}

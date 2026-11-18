using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui.Text;

namespace Terminal.Gui.Benchmarks.Text.StringExtensions;

/// <summary>
/// Benchmarks for <see cref="Tui.StringExtensions.ToString(IEnumerable{Rune})"/> performance fine-tuning.
/// </summary>
[MemoryDiagnoser]
public class ToStringEnumerable
{

    /// <summary>
    /// Benchmark for previous implementation.
    /// </summary>
    [Benchmark]
    [ArgumentsSource (nameof (DataSource))]
    public string Previous (IEnumerable<Rune> runes, int len)
    {
        return StringConcatInLoop (runes);
    }

    /// <summary>
    /// Benchmark for current implementation with char buffer and
    /// fallback to rune chars appending to StringBuilder.
    /// </summary>
    /// <param name="runes"></param>
    /// <returns></returns>
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public string Current (IEnumerable<Rune> runes, int len)
    {
        return Tui.StringExtensions.ToString (runes);
    }

    /// <summary>
    /// Previous implementation with string concatenation in a loop.
    /// </summary>
    private static string StringConcatInLoop (IEnumerable<Rune> runes)
    {
        var str = string.Empty;

        foreach (Rune rune in runes)
        {
            str += rune.ToString ();
        }

        return str;
    }

    public IEnumerable<object []> DataSource ()
    {
        // Extra length argument as workaround for the summary grouping
        // different length collections to same baseline making comparison difficult.
        foreach (string text in GetTextData ())
        {
            Rune [] runes = [..text.EnumerateRunes ()];
            yield return [runes, runes.Length];
        }
    }

    private IEnumerable<string> GetTextData ()
    {
        string textSource =
            """
            Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ.
            Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé.
            Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.
            Óŕćí v́áŕíúś ńát́όq́úé ṕéńát́íb́úś ét́ ḿáǵńíś d́íś ṕáŕt́úŕíéńt́ ḿόńt́éś, ńáśćét́úŕ ŕíd́íćúĺúś ḿúś. F́úśćé át́ éx́ b́ĺáńd́ít́, ćόńv́áĺĺíś q́úáḿ ét́, v́úĺṕút́át́é ĺáćúś.
            Śúśṕéńd́íśśé śít́ áḿét́ áŕćú út́ áŕćú f́áúćíb́úś v́áŕíúś. V́ív́áḿúś śít́ áḿét́ ḿáx́íḿúś d́íáḿ. Ńáḿ éx́ ĺéό, ṕh́áŕét́ŕá éú ĺόb́όŕt́íś át́, t́ŕíśt́íq́úé út́ f́éĺíś.
            """;

        int[] lengths = [1, 10, 100, textSource.Length / 2, textSource.Length];

        foreach (int length in lengths)
        {
            yield return textSource [..length];
        }

        string textLongerThanStackallocThreshold = string.Concat(Enumerable.Repeat(textSource, 10));
        yield return textLongerThanStackallocThreshold;
    }
}

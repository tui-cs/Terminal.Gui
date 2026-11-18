using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui.Text;

namespace Terminal.Gui.Benchmarks.Text.TextFormatter;

/// <summary>
/// Benchmarks for <see cref="Tui.TextFormatter.RemoveHotKeySpecifier"/> performance fine-tuning.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory (nameof(Tui.TextFormatter))]
public class RemoveHotKeySpecifier
{
    // Omit from summary table.
    private static readonly Rune HotkeySpecifier = (Rune)'_';

    /// <summary>
    /// Benchmark for previous implementation.
    /// </summary>
    [Benchmark]
    [ArgumentsSource (nameof (DataSource))]
    public string Previous (string text, int hotPos)
    {
        return StringConcatLoop (text, hotPos, HotkeySpecifier);
    }

    /// <summary>
    /// Benchmark for current implementation with stackalloc char buffer and fallback to rented array.
    /// </summary>
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public string Current (string text, int hotPos)
    {
        return Tui.TextFormatter.RemoveHotKeySpecifier (text, hotPos, HotkeySpecifier);
    }

    /// <summary>
    /// Previous implementation with string concatenation in a loop.
    /// </summary>
    public static string StringConcatLoop (string text, int hotPos, Rune hotKeySpecifier)
    {
        if (string.IsNullOrEmpty (text))
        {
            return text;
        }

        // Scan 
        var start = string.Empty;
        var i = 0;

        foreach (Rune c in text.EnumerateRunes ())
        {
            if (c == hotKeySpecifier && i == hotPos)
            {
                i++;

                continue;
            }

            start += c;
            i++;
        }

        return start;
    }

    public IEnumerable<object []> DataSource ()
    {
        string[] texts = [
            "",
			// Typical scenario.
			"_Save file (Ctrl+S)",
			// Medium text, hotkey specifier somewhere in the middle.
			"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla sed euismod metus. _Phasellus lectus metus, ultricies a commodo quis, facilisis vitae nulla.",
			// Long text, hotkey specifier almost at the beginning.
			"Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. _Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ. " +
            "Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé. " +
            "Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.",
			// Long text, hotkey specifier almost at the end.
			"Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ. " +
            "Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé. " +
            "Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. _Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.",
        ];

        foreach (string text in texts)
        {
            int hotPos = text.EnumerateRunes()
                .Select((r, i) => r == HotkeySpecifier ? i : -1)
                .FirstOrDefault(i => i > -1, -1);

            yield return [text, hotPos];
        }

        // Typical scenario but without hotkey and with misleading position.
        yield return ["Save file (Ctrl+S)", 3];
    }
}

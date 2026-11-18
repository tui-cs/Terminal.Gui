using System.Text;
using BenchmarkDotNet.Attributes;
using Terminal.Gui.Text;
using Tui = Terminal.Gui.Text;

namespace Terminal.Gui.Benchmarks.Text.TextFormatter;

/// <summary>
/// Benchmarks for <see cref="Tui.TextFormatter.ReplaceCRLFWithSpace"/> performance fine-tuning.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.TextFormatter))]
public class ReplaceCRLFWithSpace
{

    /// <summary>
    /// Benchmark for previous implementation.
    /// </summary>
    [Benchmark]
    [ArgumentsSource (nameof (DataSource))]
    public string Previous (string str)
    {
        return ToRuneListReplaceImplementation (str);
    }

    /// <summary>
    /// Benchmark for current implementation.
    /// </summary>
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (DataSource))]
    public string Current (string str)
    {
        return Tui.TextFormatter.ReplaceCRLFWithSpace (str);
    }

    /// <summary>
    /// Previous implementation with intermediate rune list.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private static string ToRuneListReplaceImplementation (string str)
    {
        var runes = str.ToRuneList ();
        for (int i = 0; i < runes.Count; i++)
        {
            switch (runes [i].Value)
            {
                case '\n':
                    runes [i] = (Rune)' ';
                    break;

                case '\r':
                    if ((i + 1) < runes.Count && runes [i + 1].Value == '\n')
                    {
                        runes [i] = (Rune)' ';
                        runes.RemoveAt (i + 1);
                        i++;
                    }
                    else
                    {
                        runes [i] = (Rune)' ';
                    }
                    break;
            }
        }
        return Tui.StringExtensions.ToString (runes);
    }

    public IEnumerable<object> DataSource ()
    {
        // Extreme newline scenario
        yield return "E\r\nx\r\nt\r\nr\r\ne\r\nm\r\ne\r\nn\r\ne\r\nw\r\nl\r\ni\r\nn\r\ne\r\ns\r\nc\r\ne\r\nn\r\na\r\nr\r\ni\r\no\r\n";
        // Long text with few line endings
        yield return
            """
			Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ.
			Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé.
			Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.
			Óŕćí v́áŕíúś ńát́όq́úé ṕéńát́íb́úś ét́ ḿáǵńíś d́íś ṕáŕt́úŕíéńt́ ḿόńt́éś, ńáśćét́úŕ ŕíd́íćúĺúś ḿúś. F́úśćé át́ éx́ b́ĺáńd́ít́, ćόńv́áĺĺíś q́úáḿ ét́, v́úĺṕút́át́é ĺáćúś.
			Śúśṕéńd́íśśé śít́ áḿét́ áŕćú út́ áŕćú f́áúćíb́úś v́áŕíúś. V́ív́áḿúś śít́ áḿét́ ḿáx́íḿúś d́íáḿ. Ńáḿ éx́ ĺéό, ṕh́áŕét́ŕá éú ĺόb́όŕt́íś át́, t́ŕíśt́íq́úé út́ f́éĺíś.
			"""
            // Consistent line endings between systems for more consistent performance evaluation.
            .ReplaceLineEndings ("\r\n");
        // Long text without line endings
        yield return
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla sed euismod metus. Phasellus lectus metus, ultricies a commodo quis, facilisis vitae nulla. " +
            "Curabitur mollis ex nisl, vitae mattis nisl consequat at. Aliquam dolor lectus, tincidunt ac nunc eu, elementum molestie lectus. Donec lacinia eget dolor a scelerisque. " +
            "Aenean elementum molestie rhoncus. Duis id ornare lorem. Nam eget porta sapien. Etiam rhoncus dignissim leo, ac suscipit magna finibus eu. Curabitur hendrerit elit erat, sit amet suscipit felis condimentum ut. " +
            "Nullam semper tempor mi, nec semper quam fringilla eu. Aenean sit amet pretium augue, in posuere ante. Aenean convallis porttitor purus, et posuere velit dictum eu.";
    }
}

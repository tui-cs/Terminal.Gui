using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui.Drivers;

namespace Terminal.Gui.Benchmarks.Drivers.EscSeqUtils;

[MemoryDiagnoser]
// Hide useless column from results.
[HideColumns ("writer")]
public class CSI_SetVsWrite
{
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (TextWriterSource))]
    public TextWriter Set (TextWriter writer)
    {
        writer.Write (Tui.EscSeqUtils.CSI_SetCursorPosition (1, 1));
        return writer;
    }

    [Benchmark]
    [ArgumentsSource (nameof (TextWriterSource))]
    public TextWriter Write (TextWriter writer)
    {
        Tui.EscSeqUtils.CSI_WriteCursorPosition (writer, 1, 1);
        return writer;
    }

    public static IEnumerable<object> TextWriterSource ()
    {
        return [StringWriter.Null];
    }
}

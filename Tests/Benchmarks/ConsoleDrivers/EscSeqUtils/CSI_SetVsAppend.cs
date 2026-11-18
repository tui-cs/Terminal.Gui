using System.Text;
using BenchmarkDotNet.Attributes;
using Tui = Terminal.Gui.Drivers;

namespace Terminal.Gui.Benchmarks.Drivers.EscSeqUtils;

/// <summary>
/// Compares the Set and Append implementations in combination.
/// </summary>
/// <remarks>
/// A bit misleading because *CursorPosition is called very seldom compared to the other operations
/// but they are very similar in performance because they do very similar things.
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory (nameof (Tui.EscSeqUtils))]
// Hide useless empty column from results.
[HideColumns ("stringBuilder")]
public class CSI_SetVsAppend
{
    [Benchmark (Baseline = true)]
    [ArgumentsSource (nameof (StringBuilderSource))]
    public StringBuilder Set (StringBuilder stringBuilder)
    {
        stringBuilder.Append (Tui.EscSeqUtils.CSI_SetBackgroundColorRGB (1, 2, 3));
        stringBuilder.Append (Tui.EscSeqUtils.CSI_SetForegroundColorRGB (3, 2, 1));
        stringBuilder.Append (Tui.EscSeqUtils.CSI_SetCursorPosition (4, 2));
        // Clear to prevent out of memory exception from consecutive iterations.
        stringBuilder.Clear ();
        return stringBuilder;
    }

    [Benchmark]
    [ArgumentsSource (nameof (StringBuilderSource))]
    public StringBuilder Append (StringBuilder stringBuilder)
    {
        Tui.EscSeqUtils.CSI_AppendBackgroundColorRGB (stringBuilder, 1, 2, 3);
        Tui.EscSeqUtils.CSI_AppendForegroundColorRGB (stringBuilder, 3, 2, 1);
        Tui.EscSeqUtils.CSI_AppendCursorPosition (stringBuilder, 4, 2);
        // Clear to prevent out of memory exception from consecutive iterations.
        stringBuilder.Clear ();
        return stringBuilder;
    }

    public static IEnumerable<object> StringBuilderSource ()
    {
        return [new StringBuilder ()];
    }
}

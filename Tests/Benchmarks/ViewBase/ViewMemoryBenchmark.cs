using Terminal.Gui.App;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.Benchmarks.ViewBase;

/// <summary>
///     Measures memory allocated when instantiating each concrete <see cref="View"/> subclass.
///     Discovers all View types via reflection (same technique as TestsAllViews).
///     Tracks the per-view footprint reduction described in
///     <see href="https://github.com/tui-cs/Terminal.Gui/issues/4696"/>.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- memory</code>
///     </para>
///     <para>
///         Export to file for comparison:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- memory &gt; after.md</code>
///     </para>
/// </remarks>
public static class ViewMemoryBenchmark
{
    /// <summary>
    ///     Runs the memory profiler and prints a markdown table to stdout.
    ///     Uses <c>GC.GetAllocatedBytesForCurrentThread()</c> for accurate per-type measurement.
    /// </summary>
    public static void Run ()
    {
        using IApplication app = Application.Create ().Init ("ANSI");

        List<(string Name, long Bytes)> results = [];

        foreach (Type type in discoverViewTypes ())
        {
            // Warm up: create one instance to JIT constructors, then discard
            try
            {
                Activator.CreateInstance (type);
            }
            catch
            {
                continue;
            }

            GC.Collect ();
            GC.WaitForPendingFinalizers ();
            GC.Collect ();

            long before = GC.GetAllocatedBytesForCurrentThread ();
            object? instance = Activator.CreateInstance (type);
            long after = GC.GetAllocatedBytesForCurrentThread ();

            GC.KeepAlive (instance);

            results.Add ((type.Name, after - before));
        }

        // Print markdown table sorted by allocation size descending
        results.Sort ((a, b) => b.Bytes.CompareTo (a.Bytes));

        Console.WriteLine ("| Type | Allocated (bytes) |");
        Console.WriteLine ("|------|------------------:|");

        long total = 0;

        foreach ((string name, long bytes) in results)
        {
            Console.WriteLine ($"| {name,-40} | {bytes,17:N0} |");
            total += bytes;
        }

        Console.WriteLine ($"| {"**Total**",-40} | {total,17:N0} |");
        Console.WriteLine ($"| {"**Average**",-40} | {total / results.Count,17:N0} |");
        Console.WriteLine ($"| {"**View count**",-40} | {results.Count,17:N0} |");
    }

    private static List<Type> discoverViewTypes ()
    {
        List<Type> types = [];

        foreach (Type type in typeof (View).Assembly.GetTypes ()
                                           .Where (t => t is { IsClass: true, IsAbstract: false }
                                                        && (t == typeof (View) || t.IsSubclassOf (typeof (View))))
                                           .OrderBy (t => t.Name))
        {
            if (type is { IsGenericType: true, IsTypeDefinition: true })
            {
                Type? closed = CloseGenericType (type);

                if (closed is { })
                {
                    types.Add (closed);
                }
            }
            else
            {
                types.Add (type);
            }
        }

        return types;

        static Type? CloseGenericType (Type openType)
        {
            List<Type> typeArgs = [];

            foreach (Type arg in openType.GetGenericArguments ())
            {
                Type [] constraints = arg.GetGenericParameterConstraints ();

                if (constraints.Any (c => c == typeof (View) || c.IsSubclassOf (typeof (View))))
                {
                    typeArgs.Add (typeof (View));
                }
                else
                {
                    typeArgs.Add (typeof (object));
                }
            }

            try
            {
                Type closed = openType.MakeGenericType (typeArgs.ToArray ());

                return closed.ContainsGenericParameters ? null : closed;
            }
            catch
            {
                return null;
            }
        }
    }
}

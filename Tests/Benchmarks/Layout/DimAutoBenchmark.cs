using BenchmarkDotNet.Attributes;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Benchmarks.Layout;

/// <summary>
/// Benchmarks for DimAuto performance testing.
/// Tests various scenarios to measure iteration overhead, allocation pressure, and overall execution time.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory ("DimAuto")]
public class DimAutoBenchmark
{
    private View _simpleView = null!;
    private View _complexView = null!;
    private View _deeplyNestedView = null!;

    [GlobalSetup]
    public void Setup ()
    {
        // Initialize application context with ANSI driver for benchmarking
        Application.Init (driverName: "ANSI");

        // Simple scenario: Few subviews with basic positioning
        _simpleView = CreateSimpleView ();

        // Complex scenario: Many subviews with mixed Pos/Dim types
        _complexView = CreateComplexView ();

        // Deeply nested scenario: Nested views with DimAuto
        _deeplyNestedView = CreateDeeplyNestedView ();
    }

    [GlobalCleanup]
    public void Cleanup ()
    {
        Application.Shutdown ();
    }

    /// <summary>
    /// Benchmark for simple layout with 3 subviews using basic positioning.
    /// </summary>
    [Benchmark (Baseline = true)]
    public void SimpleLayout ()
    {
        _simpleView.SetNeedsLayout ();
        _simpleView.Layout ();
    }

    /// <summary>
    /// Benchmark for complex layout with 20 subviews using mixed Pos/Dim types.
    /// Tests iteration overhead and categorization performance.
    /// </summary>
    [Benchmark]
    public void ComplexLayout ()
    {
        _complexView.SetNeedsLayout ();
        _complexView.Layout ();
    }

    /// <summary>
    /// Benchmark for deeply nested layout with DimAuto at multiple levels.
    /// Tests recursive layout performance.
    /// </summary>
    [Benchmark]
    public void DeeplyNestedLayout ()
    {
        _deeplyNestedView.SetNeedsLayout ();
        _deeplyNestedView.Layout ();
    }

    private View CreateSimpleView ()
    {
        var parent = new View
        {
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };

        parent.Add (
                    new Label { X = 0, Y = 0, Text = "Label 1" },
                    new Label { X = 0, Y = 1, Text = "Label 2" },
                    new Button { X = 0, Y = 2, Text = "Button" }
                   );

        return parent;
    }

    private View CreateComplexView ()
    {
        var parent = new View
        {
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };

        // Mix of different positioning types
        parent.Add (
                    // Absolute positioning
                    new Label { X = 0, Y = 0, Width = 20, Height = 1, Text = "Absolute" },

                    // DimAuto
                    new View
                    {
                        X = 0, Y = 1, Width = Dim.Auto (), Height = Dim.Auto ()
                    },

                    // PosCenter
                    new Label { X = Pos.Center (), Y = 2, Width = 15, Height = 1, Text = "Centered" },

                    // PosPercent
                    new Label { X = Pos.Percent (25), Y = 3, Width = 15, Height = 1, Text = "25%" },

                    // DimFill
                    new View { X = 0, Y = 4, Width = Dim.Fill (), Height = 3 },

                    // PosAnchorEnd
                    new Label { X = Pos.AnchorEnd (10), Y = 5, Width = 8, Height = 1, Text = "Anchored" },

                    // PosAlign
                    new Label { X = Pos.Align (Alignment.End), Y = 6, Width = 10, Height = 1, Text = "Aligned" },

                    // Multiple views with DimFunc
                    new Label { X = 0, Y = 7, Width = Dim.Func ((Func<View?, int>)(_ => 20)), Height = 1, Text = "Func 1" },
                    new Label { X = 0, Y = 8, Width = Dim.Func ((Func<View?, int>)(_ => 25)), Height = 1, Text = "Func 2" },
                    new Label { X = 0, Y = 9, Width = Dim.Func ((Func<View?, int>)(_ => 30)), Height = 1, Text = "Func 3" },

                    // Multiple views with DimPercent
                    new View { X = 0, Y = 10, Width = Dim.Percent (50), Height = 1 },
                    new View { X = 0, Y = 11, Width = Dim.Percent (75), Height = 1 },

                    // More absolute views
                    new Label { X = 0, Y = 14, Width = 18, Height = 1, Text = "Absolute 2" },
                    new Label { X = 0, Y = 15, Width = 22, Height = 1, Text = "Absolute 3" },
                    new Label { X = 0, Y = 16, Width = 16, Height = 1, Text = "Absolute 4" },

                    // DimFill with To
                    new View
                    {
                        X = 0, Y = 17,
                        Width = Dim.Fill (), Height = 1
                    }
                   );

        // Add nested view after creation to avoid Subviews indexing issues
        var nestedView = (View)parent.InternalSubViews [1];
        nestedView.Add (new Label { X = 0, Y = 0, Text = "Nested Auto" });

        return parent;
    }

    private View CreateDeeplyNestedView ()
    {
        var root = new View
        {
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };

        View currentParent = root;

        // Create 5 levels of nesting
        for (var level = 0; level < 5; level++)
        {
            var container = new View
            {
                X = 0,
                Y = level,
                Width = Dim.Auto (),
                Height = Dim.Auto ()
            };

            // Add some content at each level
            container.Add (
                           new Label { X = 0, Y = 0, Text = $"Level {level} - Item 1" },
                           new Label { X = 0, Y = 1, Text = $"Level {level} - Item 2" },
                           new Button { X = 0, Y = 2, Text = $"Level {level} Button" }
                          );

            currentParent.Add (container);
            currentParent = container;
        }

        return root;
    }
}

using UnitTests;

namespace ViewsTests;

public class LinearRangeRangeTests : TestDriverBase
{
    [Fact]
    public void Constructor_Default ()
    {
        LinearRange<int> r = new ();

        Assert.NotNull (r);
        Assert.Equal (LinearRangeSpanKind.Closed, r.RangeKind);
        Assert.Equal (LinearRangeSpanKind.None, r.Value.Kind);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Closed_Selects_Both_Bounds ()
    {
        LinearRange<int> r = new ([10, 20, 30, 40]);

        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 20, 40, 1, 3);

        Assert.Equal (LinearRangeSpanKind.Closed, r.Value.Kind);
        Assert.Equal (20, r.Value.Start);
        Assert.Equal (40, r.Value.End);
        Assert.Contains (1, r.SelectedIndices);
        Assert.Contains (3, r.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Resolves_Indices_From_Data_When_NotProvided ()
    {
        LinearRange<int> r = new ([10, 20, 30, 40]);

        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 20, 40, -1, -1);

        // The setter should resolve indices via IndexOfData.
        Assert.Contains (1, r.SelectedIndices);
        Assert.Contains (3, r.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Value_Setter_LeftBounded_Selects_End_Only ()
    {
        LinearRange<int> r = new ([10, 20, 30]) { RangeKind = LinearRangeSpanKind.LeftBounded };

        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.LeftBounded, default, 20, -1, 1);

        Assert.Single (r.SelectedIndices);
        Assert.Equal (1, r.SelectedIndices [0]);
        Assert.Equal (20, r.Value.End);
    }

    // Copilot
    [Fact]
    public void Value_Setter_RightBounded_Selects_Start_Only ()
    {
        LinearRange<int> r = new ([10, 20, 30]) { RangeKind = LinearRangeSpanKind.RightBounded };

        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.RightBounded, 20, default, 1, -1);

        Assert.Single (r.SelectedIndices);
        Assert.Equal (1, r.SelectedIndices [0]);
        Assert.Equal (20, r.Value.Start);
    }

    // Copilot
    [Fact]
    public void Value_Setter_None_Clears ()
    {
        LinearRange<int> r = new ([10, 20, 30]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 10, 30, 0, 2);

        r.Value = LinearRangeSpan<int>.Empty;

        Assert.Equal (LinearRangeSpanKind.None, r.Value.Kind);
        Assert.Empty (r.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Same_Value_Does_Not_Raise_Events ()
    {
        LinearRange<int> r = new ([10, 20, 30]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 10, 30, 0, 2);
        var changedCount = 0;
        r.ValueChanged += (_, _) => changedCount++;

        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 10, 30, 0, 2);

        Assert.Equal (0, changedCount);
    }

    // Copilot
    [Fact]
    public void Value_Setter_ValueChanging_Cancellation_Reverts ()
    {
        LinearRange<int> r = new ([10, 20, 30]);
        r.ValueChanging += (_, args) => args.Handled = true;

        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 10, 30, 0, 2);

        Assert.Equal (LinearRangeSpanKind.None, r.Value.Kind);
        Assert.Empty (r.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void RangeKind_Closed_To_LeftBounded_Drops_Start ()
    {
        LinearRange<int> r = new ([10, 20, 30, 40]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 20, 40, 1, 3);

        r.RangeKind = LinearRangeSpanKind.LeftBounded;

        Assert.Equal (LinearRangeSpanKind.LeftBounded, r.Value.Kind);
        Assert.Equal (40, r.Value.End);
        Assert.Equal (3, r.Value.EndIndex);
        Assert.Equal (default, r.Value.Start);
        Assert.Equal (-1, r.Value.StartIndex);
    }

    // Copilot
    [Fact]
    public void RangeKind_Closed_To_RightBounded_Drops_End ()
    {
        LinearRange<int> r = new ([10, 20, 30, 40]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 20, 40, 1, 3);

        r.RangeKind = LinearRangeSpanKind.RightBounded;

        Assert.Equal (LinearRangeSpanKind.RightBounded, r.Value.Kind);
        Assert.Equal (20, r.Value.Start);
        Assert.Equal (1, r.Value.StartIndex);
        Assert.Equal (default, r.Value.End);
        Assert.Equal (-1, r.Value.EndIndex);
    }

    // Copilot
    [Fact]
    public void RangeKind_LeftBounded_To_RightBounded_Promotes_End_To_Start ()
    {
        LinearRange<int> r = new ([10, 20, 30]) { RangeKind = LinearRangeSpanKind.LeftBounded };
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.LeftBounded, default, 20, -1, 1);

        r.RangeKind = LinearRangeSpanKind.RightBounded;

        Assert.Equal (LinearRangeSpanKind.RightBounded, r.Value.Kind);
        Assert.Equal (20, r.Value.Start);
        Assert.Equal (1, r.Value.StartIndex);
    }

    // Copilot
    [Fact]
    public void RangeKind_LeftBounded_To_Closed_Collapses_End_To_Both ()
    {
        LinearRange<int> r = new ([10, 20, 30]) { RangeKind = LinearRangeSpanKind.LeftBounded };
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.LeftBounded, default, 30, -1, 2);

        r.RangeKind = LinearRangeSpanKind.Closed;

        Assert.Equal (LinearRangeSpanKind.Closed, r.Value.Kind);
        Assert.Equal (30, r.Value.Start);
        Assert.Equal (30, r.Value.End);
    }

    // Copilot
    [Fact]
    public void RangeKind_To_None_Clears ()
    {
        LinearRange<int> r = new ([10, 20]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 10, 20, 0, 1);

        r.RangeKind = LinearRangeSpanKind.None;

        Assert.Equal (LinearRangeSpanKind.None, r.Value.Kind);
        Assert.Empty (r.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void LinearRangeSpan_Equality_Is_ByValue ()
    {
        LinearRangeSpan<int> a = new (LinearRangeSpanKind.Closed, 10, 20, 0, 1);
        LinearRangeSpan<int> b = new (LinearRangeSpanKind.Closed, 10, 20, 0, 1);

        Assert.Equal (a, b);
        Assert.True (a == b);
        Assert.Equal (a.GetHashCode (), b.GetHashCode ());
    }

    // Copilot
    [Fact]
    public void LinearRangeSpan_Empty_Has_None_Kind ()
    {
        LinearRangeSpan<int> empty = LinearRangeSpan<int>.Empty;

        Assert.Equal (LinearRangeSpanKind.None, empty.Kind);
        Assert.Equal (-1, empty.StartIndex);
        Assert.Equal (-1, empty.EndIndex);
    }

    // Copilot
    [Fact]
    public void IValue_GetValue_Returns_Boxed_Span ()
    {
        LinearRange<int> r = new ([10, 20, 30]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 10, 30, 0, 2);
        IValue ivalue = r;

        object? boxed = ivalue.GetValue ();

        Assert.IsType<LinearRangeSpan<int>> (boxed);
        Assert.Equal (r.Value, (LinearRangeSpan<int>)boxed!);
    }

    [Fact]
    public void EnableForDesign_String_Populates_WorkHours_Closed_Range ()
    {
        // Copilot
        LinearRange<string> r = new ();

        bool ok = r.EnableForDesign ();

        Assert.True (ok);
        Assert.Equal (11, r.Options.Count);
        Assert.Equal (LinearRangeSpanKind.Closed, r.RangeKind);
        Assert.Equal (LinearRangeSpanKind.Closed, r.Value.Kind);
        Assert.Equal ("9 AM", r.Value.Start);
        Assert.Equal ("5 PM", r.Value.End);
        Assert.Equal (1, r.Value.StartIndex);
        Assert.Equal (9, r.Value.EndIndex);
    }

    [Fact]
    public void EnableForDesign_NonString_Returns_False_And_Leaves_Options_Empty ()
    {
        // Copilot
        LinearRange<int> r = new ();

        bool ok = r.EnableForDesign ();

        Assert.False (ok);
        Assert.Empty (r.Options);
    }

    // Copilot
    [Fact]
    public void NonGeneric_LinearRange_Activator_CreateInstance_And_EnableForDesign_Populates ()
    {
        Type type = typeof (LinearRange);
        Assert.False (type.ContainsGenericParameters);

        View view = (View)Activator.CreateInstance (type)!;
        Assert.IsType<LinearRange> (view);

        var demoText = "demo";
        bool ok = ((IDesignable)view).EnableForDesign (ref demoText);

        Assert.True (ok);
        LinearRange r = (LinearRange)view;
        Assert.Equal (11, r.Options.Count);
        Assert.Equal (LinearRangeSpanKind.Closed, r.Value.Kind);
        Assert.Equal ("9 AM", r.Value.Start);
        Assert.Equal ("5 PM", r.Value.End);
    }
}

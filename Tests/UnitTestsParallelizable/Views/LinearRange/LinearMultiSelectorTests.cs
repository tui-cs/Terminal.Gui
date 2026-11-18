using UnitTests;

namespace ViewsTests;

public class LinearMultiSelectorTests : TestDriverBase
{
    [Fact]
    public void Constructor_Default ()
    {
        LinearMultiSelector<string> ms = new ();

        Assert.NotNull (ms);
        Assert.NotNull (ms.Value);
        Assert.Empty (ms.Value!);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Selects_Matching_Options ()
    {
        LinearMultiSelector<string> ms = new (["A", "B", "C"]);

        ms.Value = ["A", "C"];

        Assert.Equal (2, ms.Value!.Count);
        Assert.Equal (2, ms.SelectedIndices.Count);
        Assert.Contains (0, ms.SelectedIndices);
        Assert.Contains (2, ms.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Null_Treated_As_Empty ()
    {
        LinearMultiSelector<string> ms = new (["A", "B"]) { Value = ["A"] };

        ms.Value = null;

        Assert.NotNull (ms.Value);
        Assert.Empty (ms.Value!);
        Assert.Empty (ms.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Value_Setter_SequenceEqual_Does_Not_Raise_Events ()
    {
        LinearMultiSelector<string> ms = new (["A", "B"]) { Value = ["A", "B"] };
        var changedCount = 0;
        ms.ValueChanged += (_, _) => changedCount++;

        // Different list instance, same elements/order.
        ms.Value = ["A", "B"];

        Assert.Equal (0, changedCount);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Defensive_Copy_Of_Input ()
    {
        LinearMultiSelector<string> ms = new (["A", "B", "C"]);
        List<string> mutable = ["A"];

        ms.Value = mutable;

        // Mutate caller's list after assignment.
        mutable.Add ("B");

        Assert.Single (ms.Value!);
        Assert.Equal ("A", ms.Value! [0]);
    }

    // Copilot
    [Fact]
    public void Value_Getter_Never_Returns_Null ()
    {
        LinearMultiSelector<string> ms = new (["A"]);

        Assert.NotNull (ms.Value);
    }

    // Copilot
    [Fact]
    public void Value_Setter_ValueChanging_Cancellation_Reverts ()
    {
        LinearMultiSelector<string> ms = new (["A", "B"]);
        ms.ValueChanging += (_, args) => args.Handled = true;

        ms.Value = ["A"];

        Assert.Empty (ms.Value!);
        Assert.Empty (ms.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Internal_Multi_Selection_Builds_Sorted_Value ()
    {
        LinearMultiSelector<string> ms = new (["A", "B", "C"]) { AllowEmpty = true };

        // Activate index 2 then index 0.
        ms.FocusedOption = 2;
        ms.InvokeCommand (Command.Activate);

        ms.FocusedOption = 0;
        ms.InvokeCommand (Command.Activate);

        // Value is built in option-order, not selection-order.
        Assert.Equal (2, ms.Value!.Count);
        Assert.Equal ("A", ms.Value! [0]);
        Assert.Equal ("C", ms.Value! [1]);
    }

    // Copilot
    [Fact]
    public void IValue_GetValue_Returns_Boxed_List ()
    {
        LinearMultiSelector<string> ms = new (["A", "B"]) { Value = ["A"] };
        IValue ivalue = ms;

        object? boxed = ivalue.GetValue ();

        IReadOnlyList<string>? list = boxed as IReadOnlyList<string>;
        Assert.NotNull (list);
        Assert.Single (list!);
        Assert.Equal ("A", list! [0]);
    }

    [Fact]
    public void EnableForDesign_String_Populates_Days_With_Weekdays_Selected ()
    {
        // Copilot
        LinearMultiSelector<string> ms = new ();

        bool ok = ms.EnableForDesign ();

        Assert.True (ok);
        Assert.Equal (7, ms.Options.Count);
        Assert.Equal (["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"], ms.Options.Select (o => o.Legend));
        Assert.NotNull (ms.Value);
        Assert.Equal (["Mon", "Tue", "Wed", "Thu", "Fri"], ms.Value!);
    }

    [Fact]
    public void EnableForDesign_NonString_Returns_False_And_Leaves_Options_Empty ()
    {
        // Copilot
        LinearMultiSelector<int> ms = new ();

        bool ok = ms.EnableForDesign ();

        Assert.False (ok);
        Assert.Empty (ms.Options);
    }

    // Copilot
    [Fact]
    public void NonGeneric_LinearMultiSelector_Activator_CreateInstance_And_EnableForDesign_Populates ()
    {
        Type type = typeof (LinearMultiSelector);
        Assert.False (type.ContainsGenericParameters);

        View view = (View)Activator.CreateInstance (type)!;
        Assert.IsType<LinearMultiSelector> (view);

        var demoText = "demo";
        bool ok = ((IDesignable)view).EnableForDesign (ref demoText);

        Assert.True (ok);
        LinearMultiSelector ms = (LinearMultiSelector)view;
        Assert.Equal (7, ms.Options.Count);
        Assert.Equal (["Mon", "Tue", "Wed", "Thu", "Fri"], ms.Value!);
    }
}

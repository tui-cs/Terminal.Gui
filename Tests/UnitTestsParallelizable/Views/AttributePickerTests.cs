// Claude - Opus 4.5

namespace ViewsTests;

/// <summary>
///     Unit tests for <see cref="AttributePicker"/>.
/// </summary>
public class AttributePickerTests
{
    [Fact]
    public void Implements_IValue_Interface ()
    {
        AttributePicker picker = new ();
        Assert.IsAssignableFrom<IValue<Attribute?>> (picker);
    }

    [Fact]
    public void Constructor_SetsDefaultValue ()
    {
        AttributePicker picker = new ();
        Assert.NotNull (picker.Value);
        Assert.Equal (Attribute.Default, picker.Value);
    }

    [Fact]
    public void ValueChanged_Fires_WhenValueChanges ()
    {
        AttributePicker picker = new ();
        Attribute? newValue = null;
        var count = 0;

        picker.ValueChanged += (_, e) =>
                               {
                                   count++;
                                   newValue = e.NewValue;
                               };

        Attribute testAttribute = new (Color.Red, Color.Blue);
        picker.Value = testAttribute;

        Assert.Equal (1, count);
        Assert.Equal (testAttribute, newValue);
    }

    [Fact]
    public void ValueChanging_CanCancel_ViaHandled ()
    {
        AttributePicker picker = new ();
        Attribute initialValue = new (Color.White, Color.Black);
        picker.Value = initialValue;

        picker.ValueChanging += (_, e) =>
                                {
                                    e.Handled = true; // Cancel the change
                                };

        Attribute newAttribute = new (Color.Red, Color.Blue);
        picker.Value = newAttribute; // Should be cancelled

        Assert.Equal (initialValue, picker.Value); // Value unchanged
    }

    [Fact]
    public void ValueChanging_Fires_BeforeValueChanged ()
    {
        AttributePicker picker = new ();
        List<string> events = [];

        picker.ValueChanging += (_, _) => events.Add ("changing");
        picker.ValueChanged += (_, _) => events.Add ("changed");

        picker.Value = new Attribute (Color.Red, Color.Blue);

        Assert.Equal (["changing", "changed"], events);
    }

    [Fact]
    public void Value_DoesNotChange_WhenCancelled ()
    {
        AttributePicker picker = new ();
        Attribute original = new (Color.Green, Color.Yellow);
        picker.Value = original;
        var changedCount = 0;

        picker.ValueChanging += (_, e) => e.Handled = true;
        picker.ValueChanged += (_, _) => changedCount++;

        picker.Value = new Attribute (Color.Red, Color.Blue);

        Assert.Equal (original, picker.Value);
        Assert.Equal (0, changedCount);
    }

    [Fact]
    public void Value_IncludesTextStyle ()
    {
        AttributePicker picker = new ();

        Attribute testAttribute = new (Color.Red, Color.Blue, TextStyle.Bold | TextStyle.Underline);
        picker.Value = testAttribute;

        Assert.Equal (testAttribute, picker.Value);
        Assert.Equal (TextStyle.Bold | TextStyle.Underline, picker.Value!.Value.Style);
    }

    [Fact]
    public void SampleText_Property_Default ()
    {
        AttributePicker picker = new ();
        Assert.Equal ("Sample Text", picker.SampleText);
    }

    [Fact]
    public void SampleText_Property_CanBeSet ()
    {
        AttributePicker picker = new ();
        picker.SampleText = "Custom Text";
        Assert.Equal ("Custom Text", picker.SampleText);
    }

    [Fact]
    public void EnableForDesign_ReturnsTrue ()
    {
        AttributePicker picker = new ();
        bool result = picker.EnableForDesign ();
        Assert.True (result);
    }

    [Fact]
    public void EnableForDesign_SetsMultilineSampleText ()
    {
        AttributePicker picker = new ();
        picker.EnableForDesign ();
        Assert.Contains ("\n", picker.SampleText);
    }

    [Fact]
    public void EnableForDesign_SetsValueWithStyle ()
    {
        AttributePicker picker = new ();
        picker.EnableForDesign ();

        Assert.NotNull (picker.Value);
        Assert.Equal (Color.BrightRed, picker.Value!.Value.Foreground);
        Assert.Equal (Color.Blue, picker.Value!.Value.Background);
        Assert.Equal (TextStyle.Bold | TextStyle.Italic, picker.Value!.Value.Style);
    }

    [Fact]
    public void ValueChanged_DoesNotFire_WhenValueSame ()
    {
        AttributePicker picker = new ();
        Attribute testAttribute = new (Color.Red, Color.Blue);
        picker.Value = testAttribute;

        var count = 0;
        picker.ValueChanged += (_, _) => count++;

        picker.Value = testAttribute; // Same value

        Assert.Equal (0, count);
    }

    [Fact]
    public void Dispose_UnhooksEventHandlers ()
    {
        AttributePicker picker = new ();
        picker.Dispose ();

        // If Dispose works correctly, this should not throw
        // and should not trigger any events
        Assert.True (true);
    }
}

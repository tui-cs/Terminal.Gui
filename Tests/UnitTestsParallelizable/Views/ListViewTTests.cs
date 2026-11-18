using System.Collections.ObjectModel;

// Copilot

namespace ViewsTests;

public class ListViewTTests
{
    [Fact]
    public void Value_ReturnsSelectedObject ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView<string> listView = new ();
        listView.SetSource (source);
        listView.Index = 1;

        Assert.Equal ("two", listView.Value);
    }

    [Fact]
    public void Value_IsNull_WhenNoSelection ()
    {
        ObservableCollection<string> source = ["one", "two"];
        ListView<string> listView = new ();
        listView.SetSource (source);

        Assert.Null (listView.Value);
    }

    [Fact]
    public void Value_IsNull_WhenSourceIsNull ()
    {
        ListView<string> listView = new ();

        Assert.Null (listView.Value);
    }

    [Fact]
    public void Value_Setter_SelectsCorrectIndex ()
    {
        ObservableCollection<string> source = ["alpha", "beta", "gamma"];
        ListView<string> listView = new ();
        listView.SetSource (source);
        listView.Index = 0;

        listView.Value = "beta";

        Assert.Equal (1, listView.Index);
        Assert.Equal ("beta", listView.Value);
    }

    [Fact]
    public void Value_Setter_Null_ClearsSelection ()
    {
        ObservableCollection<string> source = ["alpha", "beta"];
        ListView<string> listView = new ();
        listView.SetSource (source);
        listView.Index = 0;

        listView.Value = null;

        Assert.Null (listView.Index);
        Assert.Null (listView.Value);
    }

    [Fact]
    public void Value_Setter_DoesNothing_WhenObjectNotInCollection ()
    {
        ObservableCollection<string> source = ["one", "two"];
        ListView<string> listView = new ();
        listView.SetSource (source);
        listView.Index = 0;

        listView.Value = "three";

        Assert.Equal (0, listView.Index);
        Assert.Equal ("one", listView.Value);
    }

    [Fact]
    public void Value_Setter_DoesNothing_WhenSourceIsNull ()
    {
        ListView<string> listView = new ();

        // Should not throw
        listView.Value = "anything";

        Assert.Null (listView.Index);
    }

    [Fact]
    public void ValueChanged_FiresWithTypedObject ()
    {
        ObservableCollection<string> source = ["a", "b", "c"];
        ListView<string> listView = new ();
        listView.SetSource (source);
        listView.Index = 0;

        ValueChangedEventArgs<string?>? receivedArgs = null;
        listView.ValueChanged += (_, args) => receivedArgs = args;

        listView.Index = 2;

        Assert.NotNull (receivedArgs);
        Assert.Equal ("a", receivedArgs!.OldValue);
        Assert.Equal ("c", receivedArgs.NewValue);
    }

    [Fact]
    public void ValueChanging_FiresWithTypedObject ()
    {
        ObservableCollection<string> source = ["x", "y", "z"];
        ListView<string> listView = new ();
        listView.SetSource (source);
        listView.Index = 0;

        ValueChangingEventArgs<string?>? receivedArgs = null;
        listView.ValueChanging += (_, args) => receivedArgs = args;

        listView.Index = 1;

        Assert.NotNull (receivedArgs);
        Assert.Equal ("x", receivedArgs!.CurrentValue);
        Assert.Equal ("y", receivedArgs.NewValue);
    }

    [Fact]
    public void ValueChanging_CanCancel ()
    {
        ObservableCollection<string> source = ["p", "q"];
        ListView<string> listView = new ();
        listView.SetSource (source);
        listView.Index = 0;

        listView.ValueChanging += (_, args) => args.Handled = true;

        listView.Index = 1;

        Assert.Equal (0, listView.Index);
        Assert.Equal ("p", listView.Value);
    }

    [Fact]
    public void ValueChangedUntyped_FiresWithObjectNotIndex ()
    {
        ObservableCollection<string> source = ["first", "second"];
        ListView<string> listView = new ();
        listView.SetSource (source);
        listView.Index = 0;

        ValueChangedEventArgs<object?>? receivedArgs = null;
        listView.ValueChangedUntyped += (_, args) => receivedArgs = args;

        listView.Index = 1;

        Assert.NotNull (receivedArgs);
        Assert.Equal ("first", receivedArgs!.OldValue);
        Assert.Equal ("second", receivedArgs.NewValue);
    }

    [Fact]
    public void GetValue_ReturnsTypedObject ()
    {
        ObservableCollection<string> source = ["item0", "item1"];
        ListView<string> listView = new ();
        listView.SetSource (source);
        listView.Index = 1;

        object? result = ((IValue)listView).GetValue ();

        Assert.Equal ("item1", result);
    }

    [Fact]
    public void SetSource_Null_DoesNotThrow ()
    {
        ListView<string> listView = new ();
        listView.SetSource (["a", "b"]);

        // Should not throw
        listView.SetSource (null);
    }

    [Fact]
    public void SetSource_Null_ValueIsNull ()
    {
        ListView<string> listView = new ();
        listView.SetSource (["a", "b"]);
        listView.SetSource (null);

        Assert.Null (listView.Value);
    }

    [Fact]
    public void Index_ReturnsSelectedItemIndex ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView<string> listView = new ();
        listView.SetSource (source);
        listView.Index = 2;

        Assert.Equal (2, listView.Index);
    }

    [Fact]
    public void Index_IsNull_WhenNoSelection ()
    {
        ObservableCollection<string> source = ["one", "two"];
        ListView<string> listView = new ();
        listView.SetSource (source);

        Assert.Null (listView.Index);
    }

    [Fact]
    public void Index_UpdatesWhenValueSetterChangesSelection ()
    {
        ObservableCollection<string> source = ["a", "b", "c"];
        ListView<string> listView = new ();
        listView.SetSource (source);

        listView.Value = "c";

        Assert.Equal (2, listView.Index);
    }

    [Fact]
    public void Index_Setter_SelectsCorrectItem ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView<string> listView = new ();
        listView.SetSource (source);

        listView.Index = 2;

        Assert.Equal (2, listView.Index);
        Assert.Equal ("three", listView.Value);
    }

    [Fact]
    public void Value_UsesObjectEquality_ForValueSetter ()
    {
        ObservableCollection<string> source = ["cat", "dog", "bird"];
        ListView<string> listView = new ();
        listView.SetSource (source);
        listView.Index = 0;

        listView.Value = "bird";

        Assert.Equal (2, listView.Index);
    }

    [Fact]
    public void SelectedItem_ReturnsSelectedObject ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView<string> listView = new ();
        listView.SetSource (source);
        listView.Index = 1;

        Assert.Equal ("two", listView.SelectedItem);
    }

    [Fact]
    public void SelectedItem_IsNull_WhenNoSelection ()
    {
        ObservableCollection<string> source = ["one", "two"];
        ListView<string> listView = new ();
        listView.SetSource (source);

        Assert.Null (listView.SelectedItem);
    }

    [Fact]
    public void SelectedItem_Setter_SelectsCorrectItem ()
    {
        ObservableCollection<string> source = ["alpha", "beta", "gamma"];
        ListView<string> listView = new ();
        listView.SetSource (source);

        listView.SelectedItem = "beta";

        Assert.Equal (1, listView.Index);
        Assert.Equal ("beta", listView.Value);
    }
}

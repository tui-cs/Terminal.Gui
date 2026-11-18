namespace ApplicationTests;

[Collection("Application Tests")]
public class ResultEventArgsTests
{
    [Fact]
    public void DefaultConstructor_InitializesProperties ()
    {
        ResultEventArgs<string> args = new ();

        Assert.Null (args.Result);
        Assert.False (args.Handled);
    }

    [Fact]
    public void Constructor_WithResult_SetsResult ()
    {
        ResultEventArgs<int> args = new (42);

        Assert.Equal (42, args.Result);
        Assert.False (args.Handled);
    }

    [Fact]
    public void Constructor_WithNullResult_AllowsNull ()
    {
        ResultEventArgs<string?> args = new (null);

        Assert.Null (args.Result);
        Assert.False (args.Handled);
    }

    [Fact]
    public void Result_CanBeSetAndRetrieved ()
    {
        ResultEventArgs<string> args = new ();
        args.Result = "foo";

        Assert.Equal ("foo", args.Result);

        args.Result = null;
        Assert.Null (args.Result);
    }

    [Fact]
    public void Handled_CanBeSetAndRetrieved ()
    {
        ResultEventArgs<object> args = new ();
        Assert.False (args.Handled);

        args.Handled = true;
        Assert.True (args.Handled);

        args.Handled = false;
        Assert.False (args.Handled);
    }

    [Fact]
    public void WorksWithValueTypes ()
    {
        ResultEventArgs<int> args = new ();
        Assert.Equal (0, args.Result); // default(int) is 0

        args.Result = 123;
        Assert.Equal (123, args.Result);
    }

    [Fact]
    public void WorksWithReferenceTypes ()
    {
        var obj = new object ();
        ResultEventArgs<object> args = new (obj);

        Assert.Same (obj, args.Result);

        args.Result = null;
        Assert.Null (args.Result);
    }

    // Simulate an event pattern
    public event EventHandler<ResultEventArgs<string>>? StringResultEvent;

    [Fact]
    public void EventHandler_CanChangeResult_AndCallerSeesChange ()
    {
        // Arrange
        ResultEventArgs<string> args = new ("initial");

        StringResultEvent += (sender, e) =>
                             {
                                 // Handler changes the result
                                 e.Result = "changed by handler";
                             };

        // Act
        StringResultEvent?.Invoke (this, args);

        // Assert
        Assert.Equal ("changed by handler", args.Result);
    }

    [Fact]
    public void EventHandler_CanSetResultToNull ()
    {
        // Arrange
        ResultEventArgs<string> args = new ("not null");
        StringResultEvent += (sender, e) => { e.Result = null; };

        // Act
        StringResultEvent?.Invoke (this, args);

        // Assert
        Assert.Null (args.Result);
    }

    [Fact]
    public void MultipleHandlers_LastHandlerWins ()
    {
        // Arrange
        ResultEventArgs<int> args = new (1);
        EventHandler<ResultEventArgs<int>>? intEvent = null;
        intEvent += (s, e) => e.Result = 2;
        intEvent += (s, e) => e.Result = 3;

        // Act
        intEvent?.Invoke (this, args);

        // Assert
        Assert.Equal (3, args.Result);
    }

    // Value type: int
    [Fact]
    public void EventHandler_CanChangeResult_Int ()
    {
        EventHandler<ResultEventArgs<int>> handler = (s, e) => e.Result = 99;
        ResultEventArgs<int> args = new (1);
        handler.Invoke (this, args);
        Assert.Equal (99, args.Result);
    }

    // Value type: double
    [Fact]
    public void EventHandler_CanChangeResult_Double ()
    {
        EventHandler<ResultEventArgs<double>> handler = (s, e) => e.Result = 2.718;
        ResultEventArgs<double> args = new (3.14);
        handler.Invoke (this, args);
        Assert.Equal (2.718, args.Result);
    }

    // Value type: bool
    [Fact]
    public void EventHandler_CanChangeResult_Bool ()
    {
        EventHandler<ResultEventArgs<bool>> handler = (s, e) => e.Result = false;
        ResultEventArgs<bool> args = new (true);
        handler.Invoke (this, args);
        Assert.False (args.Result);
    }

    // Enum
    private enum MyEnum
    {
        A,
        B,
        C
    }

    [Fact]
    public void EventHandler_CanChangeResult_Enum ()
    {
        EventHandler<ResultEventArgs<MyEnum>> handler = (s, e) => e.Result = MyEnum.C;
        ResultEventArgs<MyEnum> args = new (MyEnum.A);
        handler.Invoke (this, args);
        Assert.Equal (MyEnum.C, args.Result);
    }

    // Struct
    private struct MyStruct
    {
        public int X;
    }

    [Fact]
    public void EventHandler_CanChangeResult_Struct ()
    {
        EventHandler<ResultEventArgs<MyStruct>> handler = (s, e) => e.Result = new() { X = 42 };
        ResultEventArgs<MyStruct> args = new (new() { X = 1 });
        handler.Invoke (this, args);
        Assert.Equal (42, args.Result.X);
    }

    // Reference type: string
    [Fact]
    public void EventHandler_CanChangeResult_String ()
    {
        EventHandler<ResultEventArgs<string>> handler = (s, e) => e.Result = "changed";
        ResultEventArgs<string> args = new ("original");
        handler.Invoke (this, args);
        Assert.Equal ("changed", args.Result);
    }

    // Reference type: object
    [Fact]
    public void EventHandler_CanChangeResult_Object ()
    {
        var newObj = new object ();
        EventHandler<ResultEventArgs<object>> handler = (s, e) => e.Result = newObj;
        ResultEventArgs<object> args = new (new ());
        handler.Invoke (this, args);
        Assert.Same (newObj, args.Result);
    }

    // Nullable value type
    [Fact]
    public void EventHandler_CanChangeResult_NullableInt ()
    {
        EventHandler<ResultEventArgs<int?>> handler = (s, e) => e.Result = null;
        ResultEventArgs<int?> args = new (42);
        handler.Invoke (this, args);
        Assert.Null (args.Result);
    }

    // Array
    [Fact]
    public void EventHandler_CanChangeResult_Array ()
    {
        var newArr = new [] { "x", "y" };
        EventHandler<ResultEventArgs<string []>> handler = (s, e) => e.Result = newArr;
        ResultEventArgs<string []> args = new (new [] { "a", "b" });
        handler.Invoke (this, args);
        Assert.Equal (newArr, args.Result);
    }

    // List<T>
    [Fact]
    public void EventHandler_CanChangeResult_List ()
    {
        List<int> newList = new() { 1, 2, 3 };
        EventHandler<ResultEventArgs<List<int>>> handler = (s, e) => e.Result = newList;
        ResultEventArgs<List<int>> args = new (new() { 9 });
        handler.Invoke (this, args);
        Assert.Equal (newList, args.Result);
    }

    // Dictionary<K,V>
    [Fact]
    public void EventHandler_CanChangeResult_Dictionary ()
    {
        Dictionary<string, int> newDict = new() { ["a"] = 1 };
        EventHandler<ResultEventArgs<Dictionary<string, int>>> handler = (s, e) => e.Result = newDict;
        ResultEventArgs<Dictionary<string, int>> args = new (new ());
        handler.Invoke (this, args);
        Assert.Equal (newDict, args.Result);
    }

    // Record
    public record MyRecord (int Id, string Name);

    [Fact]
    public void EventHandler_CanChangeResult_Record ()
    {
        var rec = new MyRecord (1, "foo");
        EventHandler<ResultEventArgs<MyRecord>> handler = (s, e) => e.Result = rec;
        ResultEventArgs<MyRecord> args = new (null);
        handler.Invoke (this, args);
        Assert.Equal (rec, args.Result);
    }

    // Nullable int
    [Fact]
    public void EventHandler_CanChangeResult_NullableInt_ToValue_AndNull ()
    {
        EventHandler<ResultEventArgs<int?>> handler = (s, e) => e.Result = 123;
        ResultEventArgs<int?> args = new (null);
        handler.Invoke (this, args);
        Assert.Equal (123, args.Result);

        handler = (s, e) => e.Result = null;
        args = new (456);
        handler.Invoke (this, args);
        Assert.Null (args.Result);
    }

    // Nullable double
    [Fact]
    public void EventHandler_CanChangeResult_NullableDouble_ToValue_AndNull ()
    {
        EventHandler<ResultEventArgs<double?>> handler = (s, e) => e.Result = 3.14;
        ResultEventArgs<double?> args = new (null);
        handler.Invoke (this, args);
        Assert.Equal (3.14, args.Result);

        handler = (s, e) => e.Result = null;
        args = new (2.71);
        handler.Invoke (this, args);
        Assert.Null (args.Result);
    }

    // Nullable custom struct
    [Fact]
    public void EventHandler_CanChangeResult_NullableStruct_ToValue_AndNull ()
    {
        EventHandler<ResultEventArgs<MyStruct?>> handler = (s, e) => e.Result = new MyStruct { X = 7 };
        ResultEventArgs<MyStruct?> args = new (null);
        handler.Invoke (this, args);
        Assert.Equal (7, args.Result?.X);

        handler = (s, e) => e.Result = null;
        args = new (new MyStruct { X = 8 });
        handler.Invoke (this, args);
        Assert.Null (args.Result);
    }

    // Nullable string (reference type)
    [Fact]
    public void EventHandler_CanChangeResult_NullableString_ToValue_AndNull ()
    {
        EventHandler<ResultEventArgs<string?>> handler = (s, e) => e.Result = "hello";
        ResultEventArgs<string?> args = new (null);
        handler.Invoke (this, args);
        Assert.Equal ("hello", args.Result);

        handler = (s, e) => e.Result = null;
        args = new ("world");
        handler.Invoke (this, args);
        Assert.Null (args.Result);
    }

    // Nullable custom class
    private class MyClass
    {
        public int Y { get; set; }
    }

    [Fact]
    public void EventHandler_CanChangeResult_NullableClass_ToValue_AndNull ()
    {
        EventHandler<ResultEventArgs<MyClass?>> handler = (s, e) => e.Result = new() { Y = 42 };
        ResultEventArgs<MyClass?> args = new (null);
        handler.Invoke (this, args);
        Assert.NotNull (args.Result);
        Assert.Equal (42, args.Result?.Y);

        handler = (s, e) => e.Result = null;
        args = new (new() { Y = 99 });
        handler.Invoke (this, args);
        Assert.Null (args.Result);
    }
}

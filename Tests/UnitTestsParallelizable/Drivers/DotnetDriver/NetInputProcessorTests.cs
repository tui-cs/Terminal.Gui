using System.Collections.Concurrent;
using System.Text;

namespace DriverTests.Dotnet;

[Collection ("Driver Tests")]
public class NetInputProcessorTests
{
    public static IEnumerable<object []> GetConsoleKeyInfoToKeyTestCases_Rune ()
    {
        yield return [new ConsoleKeyInfo ('C', ConsoleKey.None, false, false, false), new Rune ('C')];
        yield return [new ConsoleKeyInfo ('\\', ConsoleKey.Oem5, false, false, false), new Rune ('\\')];
        yield return [new ConsoleKeyInfo ('+', ConsoleKey.OemPlus, true, false, false), new Rune ('+')];
        yield return [new ConsoleKeyInfo ('=', ConsoleKey.OemPlus, false, false, false), new Rune ('=')];
        yield return new object [] { new ConsoleKeyInfo ('_', ConsoleKey.OemMinus, true, false, false), new Rune ('_') };
        yield return new object [] { new ConsoleKeyInfo ('-', ConsoleKey.OemMinus, false, false, false), new Rune ('-') };
        yield return new object [] { new ConsoleKeyInfo (')', ConsoleKey.None, false, false, false), new Rune (')') };
        yield return new object [] { new ConsoleKeyInfo ('0', ConsoleKey.None, false, false, false), new Rune ('0') };
        yield return new object [] { new ConsoleKeyInfo ('(', ConsoleKey.None, false, false, false), new Rune ('(') };
        yield return new object [] { new ConsoleKeyInfo ('9', ConsoleKey.None, false, false, false), new Rune ('9') };
        yield return new object [] { new ConsoleKeyInfo ('*', ConsoleKey.None, false, false, false), new Rune ('*') };
        yield return new object [] { new ConsoleKeyInfo ('8', ConsoleKey.None, false, false, false), new Rune ('8') };
        yield return new object [] { new ConsoleKeyInfo ('&', ConsoleKey.None, false, false, false), new Rune ('&') };
        yield return new object [] { new ConsoleKeyInfo ('7', ConsoleKey.None, false, false, false), new Rune ('7') };
        yield return new object [] { new ConsoleKeyInfo ('^', ConsoleKey.None, false, false, false), new Rune ('^') };
        yield return new object [] { new ConsoleKeyInfo ('6', ConsoleKey.None, false, false, false), new Rune ('6') };
        yield return new object [] { new ConsoleKeyInfo ('%', ConsoleKey.None, false, false, false), new Rune ('%') };
        yield return new object [] { new ConsoleKeyInfo ('5', ConsoleKey.None, false, false, false), new Rune ('5') };
        yield return new object [] { new ConsoleKeyInfo ('$', ConsoleKey.None, false, false, false), new Rune ('$') };
        yield return new object [] { new ConsoleKeyInfo ('4', ConsoleKey.None, false, false, false), new Rune ('4') };
        yield return new object [] { new ConsoleKeyInfo ('#', ConsoleKey.None, false, false, false), new Rune ('#') };
        yield return new object [] { new ConsoleKeyInfo ('@', ConsoleKey.None, false, false, false), new Rune ('@') };
        yield return new object [] { new ConsoleKeyInfo ('2', ConsoleKey.None, false, false, false), new Rune ('2') };
        yield return new object [] { new ConsoleKeyInfo ('!', ConsoleKey.None, false, false, false), new Rune ('!') };
        yield return new object [] { new ConsoleKeyInfo ('1', ConsoleKey.None, false, false, false), new Rune ('1') };
        yield return new object [] { new ConsoleKeyInfo ('\t', ConsoleKey.None, false, false, false), new Rune ('\t') };
        yield return new object [] { new ConsoleKeyInfo ('}', ConsoleKey.Oem6, true, false, false), new Rune ('}') };
        yield return new object [] { new ConsoleKeyInfo (']', ConsoleKey.Oem6, false, false, false), new Rune (']') };
        yield return new object [] { new ConsoleKeyInfo ('{', ConsoleKey.Oem4, true, false, false), new Rune ('{') };
        yield return new object [] { new ConsoleKeyInfo ('[', ConsoleKey.Oem4, false, false, false), new Rune ('[') };
        yield return new object [] { new ConsoleKeyInfo ('\"', ConsoleKey.Oem7, true, false, false), new Rune ('\"') };
        yield return new object [] { new ConsoleKeyInfo ('\'', ConsoleKey.Oem7, false, false, false), new Rune ('\'') };
        yield return new object [] { new ConsoleKeyInfo (':', ConsoleKey.Oem1, true, false, false), new Rune (':') };
        yield return new object [] { new ConsoleKeyInfo (';', ConsoleKey.Oem1, false, false, false), new Rune (';') };
        yield return new object [] { new ConsoleKeyInfo ('?', ConsoleKey.Oem2, true, false, false), new Rune ('?') };
        yield return new object [] { new ConsoleKeyInfo ('/', ConsoleKey.Oem2, false, false, false), new Rune ('/') };
        yield return new object [] { new ConsoleKeyInfo ('>', ConsoleKey.OemPeriod, true, false, false), new Rune ('>') };
        yield return new object [] { new ConsoleKeyInfo ('.', ConsoleKey.OemPeriod, false, false, false), new Rune ('.') };
        yield return new object [] { new ConsoleKeyInfo ('<', ConsoleKey.OemComma, true, false, false), new Rune ('<') };
        yield return new object [] { new ConsoleKeyInfo (',', ConsoleKey.OemComma, false, false, false), new Rune (',') };
        yield return new object [] { new ConsoleKeyInfo ('w', ConsoleKey.None, false, false, false), new Rune ('w') };
        yield return new object [] { new ConsoleKeyInfo ('e', ConsoleKey.None, false, false, false), new Rune ('e') };
        yield return new object [] { new ConsoleKeyInfo ('a', ConsoleKey.None, false, false, false), new Rune ('a') };
        yield return new object [] { new ConsoleKeyInfo ('s', ConsoleKey.None, false, false, false), new Rune ('s') };
    }

    [Theory]
    [MemberData (nameof (GetConsoleKeyInfoToKeyTestCases_Rune))]
    public void ConsoleKeyInfoToKey_ValidInput_AsRune (ConsoleKeyInfo input, Rune expected)
    {
        var converter = new NetKeyConverter ();

        // Act
        var result = converter.ToKey (input);

        // Assert
        Assert.Equal (expected, result.AsRune);
    }

    public static IEnumerable<object []> GetConsoleKeyInfoToKeyTestCases_Key ()
    {
        yield return new object [] { new ConsoleKeyInfo ('\t', ConsoleKey.None, false, false, false), Key.Tab };
        yield return new object [] { new ConsoleKeyInfo ('\u001B', ConsoleKey.None, false, false, false), Key.Esc };
        yield return new object [] { new ConsoleKeyInfo ('\u007f', ConsoleKey.None, false, false, false), Key.Backspace };

        // TODO: Terminal.Gui does not have a Key for this mapped
        // TODO: null and default(Key) are both not same as Null.  Why user has to do (Key)0 to get a null key?!
        yield return new object [] { new ConsoleKeyInfo ('\0', ConsoleKey.LeftWindows, false, false, false), (Key)0 };
    }

    [Theory]
    [MemberData (nameof (GetConsoleKeyInfoToKeyTestCases_Key))]
    public void ConsoleKeyInfoToKey_ValidInput_AsKey (ConsoleKeyInfo input, Key expected)
    {
        var converter = new NetKeyConverter ();

        // Act
        var result = converter.ToKey (input);

        // Assert
        Assert.Equal (expected, result);
    }

    [Fact]
    public void Test_ProcessQueue_CapitalHLowerE ()
    {
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();

        queue.Enqueue (new ('H', ConsoleKey.None, true, false, false));
        queue.Enqueue (new ('e', ConsoleKey.None, false, false, false));

        var processor = new NetInputProcessor (queue, null);

        List<Key> downs = new ();

        processor.KeyDown += (s, e) => { downs.Add (e); };

        Assert.Empty (downs);

        processor.ProcessQueue ();

        Assert.Equal (Key.H.WithShift, downs [0]);
        Assert.Equal (Key.E, downs [1]);
    }
}

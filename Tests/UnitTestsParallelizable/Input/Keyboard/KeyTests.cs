using System.Text;

namespace InputTests;

public class KeyTests
{
    private readonly ITestOutputHelper _output;
    public KeyTests (ITestOutputHelper output) { _output = output; }

    // Via https://andrewlock.net/creating-parameterised-tests-in-xunit-with-inlinedata-classdata-and-memberdata/
    public static TheoryData<string, Key> ValidStrings =>
        new ()
        {
            { "a", Key.A },
            { "Ctrl+A", Key.A.WithCtrl },
            { "Alt+A", Key.A.WithAlt },
            { "Shift+A", Key.A.WithShift },
            { "A", Key.A.WithShift },
            { "â", new ((KeyCode)'â') },
            { "Shift+â", new ((KeyCode)'â' | KeyCode.ShiftMask) },
            { "Shift+Â", new ((KeyCode)'Â' | KeyCode.ShiftMask) },
            { "Ctrl+Shift+CursorUp", Key.CursorUp.WithShift.WithCtrl },
            { "Ctrl+Alt+Shift+CursorUp", Key.CursorUp.WithShift.WithCtrl.WithAlt },
            { "ctrl+alt+shift+cursorup", Key.CursorUp.WithShift.WithCtrl.WithAlt },
            { "CTRL+ALT+SHIFT+CURSORUP", Key.CursorUp.WithShift.WithCtrl.WithAlt },
            { "Ctrl+Alt+Shift+Delete", Key.Delete.WithCtrl.WithAlt.WithShift },
            { "Ctrl+Alt+Shift+Enter", Key.Enter.WithCtrl.WithAlt.WithShift },
            { "Tab", Key.Tab },
            { "Shift+Tab", Key.Tab.WithShift },
            { "Ctrl+Tab", Key.Tab.WithCtrl },
            { "Alt+Tab", Key.Tab.WithAlt },
            { "Ctrl+Shift+Tab", Key.Tab.WithShift.WithCtrl },
            { "Ctrl+Alt+Tab", Key.Tab.WithAlt.WithCtrl },
            { "", Key.Empty },
            { " ", Key.Space },
            { "Space", Key.Space },
            { "Shift+Space", Key.Space.WithShift },
            { "Ctrl+Space", Key.Space.WithCtrl },
            { "Alt+Space", Key.Space.WithAlt },
            { "Shift+ ", Key.Space.WithShift },
            { "Ctrl+ ", Key.Space.WithCtrl },
            { "Alt+ ", Key.Space.WithAlt },
            { "F1", Key.F1 },
            { "0", Key.D0 },
            { "9", Key.D9 },
            { "D0", Key.D0 },
            { "65", Key.A.WithShift },
            { "97", Key.A },
            { "Shift", KeyCode.ShiftMask },
            { "Ctrl", KeyCode.CtrlMask },
            { "Ctrl-A", Key.A.WithCtrl },
            { "Alt-A", Key.A.WithAlt },
            { "A-Ctrl", Key.A.WithCtrl },
            { "Alt-A-Ctrl", Key.A.WithCtrl.WithAlt },
            { "📄", (KeyCode)0x1F4C4 }
        };

    [Theory]
    [InlineData ((KeyCode)'❿', '❿')]
    [InlineData ((KeyCode)'☑', '☑')]
    [InlineData ((KeyCode)'英', '英')]
    [InlineData ((KeyCode)'{', '{')]
    [InlineData ((KeyCode)'\'', '\'')]
    [InlineData ((KeyCode)'ó', 'ó')]
    [InlineData ((KeyCode)'ó' | KeyCode.ShiftMask, 'ó')]
    [InlineData ((KeyCode)'Ó', 'Ó')]
    [InlineData ((KeyCode)'ç' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '\0')]
    [InlineData (KeyCode.A, 97)] // 65 or equivalent to Key.A, but A-Z are mapped to lower case by drivers
    //[InlineData (Key.A, 97)] // 65 equivalent to (Key)'A', but A-Z are mapped to lower case by drivers
    [InlineData (KeyCode.ShiftMask | KeyCode.A, 65)]
    [InlineData (KeyCode.CtrlMask | KeyCode.A, '\0')]
    [InlineData (KeyCode.AltMask | KeyCode.A, '\0')]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.A, '\0')]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.A, '\0')]
    [InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.A, '\0')]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.A, '\0')]
    [InlineData (KeyCode.Z, 'z')]
    [InlineData (KeyCode.ShiftMask | KeyCode.Z, 'Z')]
    [InlineData ((KeyCode)'1', '1')]
    [InlineData (KeyCode.ShiftMask | KeyCode.D1, '1')]
    [InlineData (KeyCode.CtrlMask | KeyCode.D1, '\0')]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.D1, '\0')]
    [InlineData (KeyCode.F1, '\0')]
    [InlineData (KeyCode.ShiftMask | KeyCode.F1, '\0')]
    [InlineData (KeyCode.CtrlMask | KeyCode.F1, '\0')]
#pragma warning disable xUnit1025 // InlineData should be unique within the Theory it belongs to
    [InlineData (KeyCode.Enter, '\r')]
#pragma warning restore xUnit1025 // InlineData should be unique within the Theory it belongs to
    [InlineData (KeyCode.Tab, '\t')]
    [InlineData (KeyCode.Esc, 0x1b)]
    [InlineData (KeyCode.Space, ' ')]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Enter, '\0')]
    [InlineData (KeyCode.Null, '\0')]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Null, '\0')]
    [InlineData (KeyCode.CharMask, '\0')]
    [InlineData (KeyCode.SpecialMask, '\0')]
    public void AsRune_ShouldReturnCorrectIntValue (KeyCode key, uint expected)
    {
        var eventArgs = new Key (key);
        Assert.Equal ((Rune)expected, eventArgs.AsRune);
    }

    [Theory]
    [InlineData ('a', KeyCode.A)]
    [InlineData ('A', KeyCode.A | KeyCode.ShiftMask)]
    [InlineData ('z', KeyCode.Z)]
    [InlineData ('Z', KeyCode.Z | KeyCode.ShiftMask)]
    [InlineData (' ', KeyCode.Space)]
    [InlineData ('1', KeyCode.D1)]
    [InlineData ('!', (KeyCode)'!')]
    [InlineData ('\r', KeyCode.Enter)]
    [InlineData ('\t', KeyCode.Tab)]
    [InlineData ('\n', (KeyCode)10)]
    [InlineData ('ó', (KeyCode)'ó')]
    [InlineData ('Ó', (KeyCode)'Ó')]
    [InlineData ('❿', (KeyCode)'❿')]
    [InlineData ('☑', (KeyCode)'☑')]
    [InlineData ('英', (KeyCode)'英')]
    [InlineData ('{', (KeyCode)'{')]
    [InlineData ('\'', (KeyCode)'\'')]
    [InlineData ('\xFFFF', (KeyCode)0xFFFF)]
    [InlineData ('\x0', (KeyCode)0x0)]
    public void Cast_Char_Int_To_Key (char ch, KeyCode expectedKeyCode)
    {
        var key = (Key)ch;
        Assert.Equal (expectedKeyCode, key.KeyCode);

        key = (int)ch;
        Assert.Equal (expectedKeyCode, key.KeyCode);
    }

    [Fact]
    public void Cast_Key_ToString ()
    {
        var str = (string)Key.Q.WithCtrl;
        Assert.Equal ("Ctrl+Q", str);
    }

    [Theory]
    [InlineData (KeyCode.Enter, KeyCode.Enter)]
    [InlineData (KeyCode.Esc, KeyCode.Esc)]
    [InlineData (KeyCode.A, KeyCode.A)]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, KeyCode.A | KeyCode.ShiftMask)]
    [InlineData (KeyCode.Z, KeyCode.Z)]
    [InlineData (KeyCode.Space, KeyCode.Space)]
    public void Cast_KeyCode_Int_To_Key (KeyCode cdk, KeyCode expected)
    {
        // KeyCode
        var key = (Key)cdk;
        Assert.Equal (((Key)expected).ToString (), key.ToString ());

        // Int
        key = key.AsRune.Value;
        Assert.Equal (((Key)expected).ToString (), key.ToString ());
    }

    // string cast operators
    [Theory]
    [InlineData ("Ctrl+Q", KeyCode.Q | KeyCode.CtrlMask)]
    [InlineData ("📄", (KeyCode)0x1F4C4)]
    public void Cast_String_To_Key (string str, KeyCode expectedKeyCode)
    {
        var key = (Key)str;
        Assert.Equal (expectedKeyCode, key.KeyCode);
    }

    [Theory]
    [InlineData (KeyCode.A, KeyCode.A)]
    [InlineData (KeyCode.F1, KeyCode.F1)]
    public void Casting_Between_Key_And_KeyCode (KeyCode keyCode, KeyCode key)
    {
        Assert.Equal (keyCode, (Key)key);
        Assert.NotEqual (keyCode, ((Key)key).WithShift);
        Assert.Equal ((uint)keyCode, (uint)(Key)key);
        Assert.NotEqual ((uint)keyCode, (uint)((Key)key).WithShift);
    }

    [Theory]
    [InlineData ('a', KeyCode.A)]
    [InlineData ('A', KeyCode.A | KeyCode.ShiftMask)]
    [InlineData ('z', KeyCode.Z)]
    [InlineData ('Z', KeyCode.Z | KeyCode.ShiftMask)]
    [InlineData (' ', KeyCode.Space)]
    [InlineData ('1', KeyCode.D1)]
    [InlineData ('!', (KeyCode)'!')]
    [InlineData ('\r', KeyCode.Enter)]
    [InlineData ('\t', KeyCode.Tab)]
    [InlineData ('\n', (KeyCode)10)]
    [InlineData ('ó', (KeyCode)'ó')]
    [InlineData ('Ó', (KeyCode)'Ó')]
    [InlineData ('❿', (KeyCode)'❿')]
    [InlineData ('☑', (KeyCode)'☑')]
    [InlineData ('英', (KeyCode)'英')]
    [InlineData ('{', (KeyCode)'{')]
    [InlineData ('\'', (KeyCode)'\'')]
    [InlineData ('\xFFFF', (KeyCode)0xFFFF)]
    [InlineData ('\x0', (KeyCode)0x0)]
    public void Constructor_Char_Int (char ch, KeyCode expectedKeyCode)
    {
        var key = new Key (ch);
        Assert.Equal (expectedKeyCode, key.KeyCode);

        key = new ((int)ch);
        Assert.Equal (expectedKeyCode, key.KeyCode);
    }

    [Theory]
    [InlineData (0x1F4C4, (KeyCode)0x1F4C4, "📄")]
    [InlineData (0x1F64B, (KeyCode)0x1F64B, "🙋")]
    [InlineData (0x1F46A, (KeyCode)0x1F46A, "👪")]
    public void Constructor_Int_Non_Bmp (int value, KeyCode expectedKeyCode, string expectedString)
    {
        var key = new Key (value);
        Assert.Equal (expectedKeyCode, key.KeyCode);
        Assert.Equal (expectedString, key.AsRune.ToString ());
        Assert.Equal (expectedString, key.ToString ());
    }

    [Theory]
    [InlineData (-1)]
    [InlineData (0x11FFFF)]
    public void Constructor_Int_Invalid_Throws (int keyInt) { Assert.Throws<ArgumentOutOfRangeException> (() => new Key (keyInt)); }

    [Theory]
    [InlineData ('\ud83d')]
    [InlineData ('\udcc4')]
    public void Constructor_Int_Surrogate_Throws (int keyInt) { Assert.Throws<ArgumentException> (() => new Key (keyInt)); }

    [Fact]
    public void Constructor_Default_ShouldSetKeyToNull ()
    {
        var eventArgs = new Key ();
        Assert.Equal (KeyCode.Null, eventArgs.KeyCode);
    }

    [Fact]
    public void GetPrintableText_Prefers_AssociatedText ()
    {
        Key key = new (Key.D1.WithShift) { AssociatedText = "!" };

        Assert.Equal ("!", key.GetPrintableText ());
        Assert.True (key.TryGetPrintableRune (out Rune rune));
        Assert.Equal (new Rune ('!'), rune);
    }

    [Fact]
    public void GetPrintableText_Falls_Back_To_ShiftedKeyCode ()
    {
        Key key = new (Key.D1.WithShift) { ShiftedKeyCode = (KeyCode)'!' };

        Assert.Equal ("!", key.GetPrintableText ());
        Assert.True (key.TryGetPrintableRune (out Rune rune));
        Assert.Equal (new Rune ('!'), rune);
    }

    [Fact]
    public void GetPrintableText_Falls_Back_To_AsRune ()
    {
        Key key = new (Key.D1.WithShift);

        Assert.Equal ("1", key.GetPrintableText ());
        Assert.True (key.TryGetPrintableRune (out Rune rune));
        Assert.Equal (new Rune ('1'), rune);
    }

    [Fact]
    public void TryGetPrintableRune_Returns_False_For_MultiRune_AssociatedText ()
    {
        Key key = new (Key.A) { AssociatedText = "ab" };

        Assert.False (key.TryGetPrintableRune (out Rune rune));
        Assert.Equal (default (Rune), rune);
    }

    [Fact]
    public void AsGrapheme_Returns_SingleAssociatedGrapheme ()
    {
        Key key = new () { AssociatedText = "👨‍👩‍👧‍👦" };

        Assert.Equal ("👨‍👩‍👧‍👦", key.AsGrapheme);
        Assert.Equal (default (Rune), key.AsRune);
    }

    [Fact]
    public void AsGrapheme_Returns_CombiningSequence_AsSingleGrapheme ()
    {
        Key key = new () { AssociatedText = "a\u0301" };

        Assert.Equal ("a\u0301", key.AsGrapheme);
        Assert.Equal (default (Rune), key.AsRune);
    }

    [Fact]
    public void AsGrapheme_And_AsRune_Suppress_ModifiedPrintableFallback_WithoutAssociatedText ()
    {
        Key key = new (Key.T.WithShift.WithAlt) { ShiftedKeyCode = (KeyCode)'T' };

        Assert.Equal (string.Empty, key.AsGrapheme);
        Assert.Equal (default (Rune), key.AsRune);
        Assert.Equal (string.Empty, key.GetPrintableText ());
        Assert.False (key.TryGetPrintableRune (out Rune rune));
        Assert.Equal (default (Rune), rune);
    }

    [Theory]
    [InlineData ("Barf")]
    public void Constructor_String_Invalid_Throws (string keyString) { Assert.Throws<ArgumentException> (() => new Key (keyString)); }

    [Theory]
    [MemberData (nameof (ValidStrings))]
    public void Constructor_String_Valid (string keyString, Key expected)
    {
        var key = new Key (keyString);
        Assert.Equal (expected.ToString (), key.ToString ());
    }

    [Theory]
    [InlineData (KeyCode.Enter)]
    [InlineData (KeyCode.Esc)]
    [InlineData (KeyCode.A)]
    public void Constructor_WithKey_ShouldSetCorrectKey (KeyCode key)
    {
        var eventArgs = new Key (key);
        Assert.Equal (key, eventArgs.KeyCode);
    }

    [Fact]
    public void HandledProperty_ShouldBeFalseByDefault ()
    {
        var eventArgs = new Key ();
        Assert.False (eventArgs.Handled);
    }

    [Theory]
    [InlineData (KeyCode.AltMask, true)]
    [InlineData (KeyCode.A, false)]
    public void IsAlt_ShouldReturnCorrectValue (KeyCode key, bool expected)
    {
        var eventArgs = new Key (key);
        Assert.Equal (expected, eventArgs.IsAlt);
    }

    [Theory]
    [InlineData (KeyCode.A, true)]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, true)]
    [InlineData (KeyCode.F, true)]
    [InlineData (KeyCode.F | KeyCode.ShiftMask, true)]

    // these have alt or ctrl modifiers or are not a..z
    [InlineData (KeyCode.A | KeyCode.CtrlMask, false)]
    [InlineData (KeyCode.A | KeyCode.AltMask, false)]
    [InlineData (KeyCode.D0, false)]
    [InlineData (KeyCode.Esc, false)]
    [InlineData (KeyCode.Tab, false)]
    public void IsKeyCodeAtoZ (KeyCode key, bool expected)
    {
        var eventArgs = new Key (key);
        Assert.Equal (expected, eventArgs.IsKeyCodeAtoZ);
    }

    [Theory]
    [InlineData (KeyCode.ShiftMask, true, false, false)]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask, true, true, false)]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, true, true, true)]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask, true, false, true)]
    [InlineData (KeyCode.AltMask, false, true, false)]
    [InlineData (KeyCode.AltMask | KeyCode.CtrlMask, false, true, true)]
    [InlineData (KeyCode.CtrlMask, false, false, true)]
    public void IsShift_IsAlt_IsCtrl (KeyCode keyCode, bool isShift, bool isAlt, bool isCtrl)
    {
        Assert.Equal (((Key)keyCode).IsShift, isShift);
        Assert.Equal (((Key)keyCode).IsAlt, isAlt);
        Assert.Equal (((Key)keyCode).IsCtrl, isCtrl);
    }

    // IsValid
    [Theory]
    [InlineData (KeyCode.A, true)]
    [InlineData (KeyCode.B, true)]
    [InlineData (KeyCode.F1 | KeyCode.ShiftMask, true)]
    [InlineData (KeyCode.Null, false)]
    [InlineData (KeyCode.ShiftMask, false)]
    [InlineData (KeyCode.CtrlMask, false)]
    [InlineData (KeyCode.AltMask, false)]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask, false)]
    public void IsValid (KeyCode key, bool expected) { Assert.Equal (expected, ((Key)key).IsValid); }

    [Fact]
    public void NoShift_ShouldReturnCorrectValue ()
    {
        Key cad = Key.Delete.WithCtrl.WithAlt;
        Assert.Equal (KeyCode.Delete | KeyCode.CtrlMask | KeyCode.AltMask, cad);

        Assert.Equal (KeyCode.Delete | KeyCode.AltMask, cad.NoCtrl);

        Key a = Key.A.WithCtrl.WithAlt.WithShift;
        Assert.Equal (KeyCode.A, a.NoCtrl.NoShift.NoAlt);
        Assert.Equal (KeyCode.A, a.NoAlt.NoShift.NoCtrl);
        Assert.Equal (KeyCode.A, a.NoAlt.NoShift.NoCtrl.NoCtrl.NoAlt.NoShift);

        Assert.Equal (Key.Delete, Key.Delete.WithCtrl.NoCtrl);

        Assert.Equal ((KeyCode)Key.Delete.WithCtrl, Key.Delete.NoCtrl.WithCtrl);
    }

    [Fact]
    public void Standard_Keys_Always_New ()
    {
        // Make two local keys, and grab Key.A, which is a reference to a singleton.
        Key aKey1 = Key.A;
        Key aKey2 = Key.A;

        // Assert the starting state that is expected
        Assert.False (aKey1.Handled);
        Assert.False (aKey2.Handled);
        Assert.False (Key.A.Handled);

        // Now set Handled true on one of our local keys
        aKey1.Handled = true;

        // Assert the newly-expected case
        // The last two assertions will fail, because we have actually modified a singleton
        Assert.True (aKey1.Handled);
        Assert.False (aKey2.Handled);
        Assert.False (Key.A.Handled);
    }

    [Fact]
    public void Standard_Keys_Should_Equal_KeyCode ()
    {
        Assert.Equal (KeyCode.A, Key.A);
        Assert.Equal (KeyCode.Delete, Key.Delete);
    }

    [Theory]
    [InlineData ((KeyCode)'☑', "☑")]
    [InlineData ((KeyCode)'英', "英")]
    [InlineData ((KeyCode)'{', "{")]
    [InlineData ((KeyCode)'\'', "\'")]
    [InlineData ((KeyCode)'ó', "ó")]
    [InlineData ((KeyCode)'Ó' | KeyCode.ShiftMask, "Shift+Ó")]
    [InlineData ((KeyCode)'ó' | KeyCode.ShiftMask, "Shift+ó")]
    [InlineData ((KeyCode)'Ó', "Ó")]
    [InlineData ((KeyCode)'ç' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, "Ctrl+Alt+Shift+ç")]
    [InlineData ((KeyCode)'a', "a")] // 97 or Key.Space | Key.A
    [InlineData ((KeyCode)'A', "a")] // 65 or equivalent to Key.A, but A-Z are mapped to lower case by drivers
    [InlineData (KeyCode.ShiftMask | KeyCode.A, "A")]
    [InlineData ((KeyCode)'a' | KeyCode.ShiftMask, "A")]
    [InlineData (KeyCode.CtrlMask | KeyCode.A, "Ctrl+A")]
    [InlineData (KeyCode.AltMask | KeyCode.A, "Alt+A")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.A, "Ctrl+Shift+A")]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.A, "Alt+Shift+A")]
    [InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.A, "Ctrl+Alt+A")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.A, "Ctrl+Alt+Shift+A")]
    [InlineData (KeyCode.ShiftMask | KeyCode.Z, "Z")]
    [InlineData (KeyCode.CtrlMask | KeyCode.Z, "Ctrl+Z")]
    [InlineData (KeyCode.AltMask | KeyCode.Z, "Alt+Z")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.Z, "Ctrl+Shift+Z")]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.Z, "Alt+Shift+Z")]
    [InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.Z, "Ctrl+Alt+Z")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Z, "Ctrl+Alt+Shift+Z")]
    [InlineData ((KeyCode)'1', "1")]
    [InlineData (KeyCode.ShiftMask | KeyCode.D1, "Shift+1")]
    [InlineData (KeyCode.CtrlMask | KeyCode.D1, "Ctrl+1")]
    [InlineData (KeyCode.AltMask | KeyCode.D1, "Alt+1")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.D1, "Ctrl+Shift+1")]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.D1, "Alt+Shift+1")]
    [InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.D1, "Ctrl+Alt+1")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.D1, "Ctrl+Alt+Shift+1")]
    [InlineData (KeyCode.F1, "F1")]
    [InlineData (KeyCode.ShiftMask | KeyCode.F1, "Shift+F1")]
    [InlineData (KeyCode.CtrlMask | KeyCode.F1, "Ctrl+F1")]
    [InlineData (KeyCode.AltMask | KeyCode.F1, "Alt+F1")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.F1, "Ctrl+Shift+F1")]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.F1, "Alt+Shift+F1")]
    [InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.F1, "Ctrl+Alt+F1")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.F1, "Ctrl+Alt+Shift+F1")]
    [InlineData (KeyCode.Enter, "Enter")]
    [InlineData (KeyCode.ShiftMask | KeyCode.Enter, "Shift+Enter")]
    [InlineData (KeyCode.CtrlMask | KeyCode.Enter, "Ctrl+Enter")]
    [InlineData (KeyCode.AltMask | KeyCode.Enter, "Alt+Enter")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.Enter, "Ctrl+Shift+Enter")]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.Enter, "Alt+Shift+Enter")]
    [InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.Enter, "Ctrl+Alt+Enter")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Enter, "Ctrl+Alt+Shift+Enter")]
    [InlineData (KeyCode.Delete, "Delete")]
    [InlineData (KeyCode.ShiftMask | KeyCode.Delete, "Shift+Delete")]
    [InlineData (KeyCode.CtrlMask | KeyCode.Delete, "Ctrl+Delete")]
    [InlineData (KeyCode.AltMask | KeyCode.Delete, "Alt+Delete")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.Delete, "Ctrl+Shift+Delete")]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.Delete, "Alt+Shift+Delete")]
    [InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.Delete, "Ctrl+Alt+Delete")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Delete, "Ctrl+Alt+Shift+Delete")]
    [InlineData (KeyCode.CursorUp, "CursorUp")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CursorUp, "Shift+CursorUp")]
    [InlineData (KeyCode.CtrlMask | KeyCode.CursorUp, "Ctrl+CursorUp")]
    [InlineData (KeyCode.AltMask | KeyCode.CursorUp, "Alt+CursorUp")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.CursorUp, "Ctrl+Shift+CursorUp")]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CursorUp, "Alt+Shift+CursorUp")]
    [InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.CursorUp, "Ctrl+Alt+CursorUp")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.CursorUp, "Ctrl+Alt+Shift+CursorUp")]
    [InlineData (KeyCode.Space, "Space")]
    [InlineData (KeyCode.Null, "Null")]
    [InlineData (KeyCode.ShiftMask | KeyCode.Null, "Shift")]
    [InlineData (KeyCode.CtrlMask | KeyCode.Null, "Ctrl")]
    [InlineData (KeyCode.AltMask | KeyCode.Null, "Alt")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.Null, "Ctrl+Shift")]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.Null, "Alt+Shift")]
    [InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.Null, "Ctrl+Alt")]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Null, "Ctrl+Alt+Shift")]
    [InlineData (KeyCode.CharMask, "CharMask")]
    [InlineData (KeyCode.SpecialMask, "Ctrl+Alt+Shift")]
    [InlineData ((KeyCode)'+', "+")]
    [InlineData ((KeyCode)'+' | KeyCode.ShiftMask, "Shift++")]
    [InlineData ((KeyCode)'+' | KeyCode.CtrlMask, "Ctrl++")]
    [InlineData ((KeyCode)'+' | KeyCode.ShiftMask | KeyCode.CtrlMask, "Ctrl+Shift++")]
    public void ToString_ShouldReturnFormattedString (KeyCode key, string expected) =>
        Assert.Equal (expected, Key.ToString (key));

    // TODO: Create equality operator for KeyCode
    //Assert.Equal (KeyCode.DeleteChar, Key.Delete);

    // Similar tests for IsShift and IsCtrl
    [Fact]
    public void ToString_ShouldReturnReadableString ()
    {
        Key eventArgs = Key.A.WithCtrl;
        Assert.Equal ("Ctrl+A", eventArgs.ToString ());
    }

    [Theory]
    [InlineData (KeyCode.CtrlMask | KeyCode.A, '+', "Ctrl+A")]
    [InlineData (KeyCode.AltMask | KeyCode.B, '-', "Alt-B")]
    public void ToStringWithSeparator_ShouldReturnFormattedString (KeyCode key, char separator, string expected)
    {
        Assert.Equal (expected, Key.ToString (key, (Rune)separator));
    }

    [Theory]
    [InlineData ("aa")]
    [InlineData ("-1")]
    [InlineData ("Crtl")]
    [InlineData ("99a")]
    [InlineData ("a99")]
    [InlineData ("#99")]
    [InlineData ("x99")]
    [InlineData ("0x99")]
    [InlineData ("Ctrl-Ctrl")]
    public void TryParse_ShouldReturnFalse_On_InvalidKey (string keyString) { Assert.False (Key.TryParse (keyString, out Key _)); }

    // TryParse
    [Theory]
    [InlineData ("a", KeyCode.A)]
    [InlineData ("Ctrl+A", KeyCode.A | KeyCode.CtrlMask)]
    [InlineData ("Alt+A", KeyCode.A | KeyCode.AltMask)]
    [InlineData ("Shift+A", KeyCode.A | KeyCode.ShiftMask)]
    [InlineData ("A", KeyCode.A | KeyCode.ShiftMask)]
    [InlineData ("â", (KeyCode)'â')]
    [InlineData ("Shift+â", (KeyCode)'â' | KeyCode.ShiftMask)]
    [InlineData ("Shift+Â", (KeyCode)'Â' | KeyCode.ShiftMask)]
    [InlineData ("Ctrl+Shift+CursorUp", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.CursorUp)]
    [InlineData ("Ctrl+Alt+Shift+CursorUp", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.CursorUp)]
    [InlineData ("ctrl+alt+shift+cursorup", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.CursorUp)]
    [InlineData ("CTRL+ALT+SHIFT+CURSORUP", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.CursorUp)]
    [InlineData ("Ctrl+Alt+Shift+Delete", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Delete)]
    [InlineData ("Ctrl+Alt+Shift+Enter", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Enter)]
    [InlineData ("Tab", KeyCode.Tab)]
    [InlineData ("Shift+Tab", KeyCode.Tab | KeyCode.ShiftMask)]
    [InlineData ("Ctrl+Tab", KeyCode.Tab | KeyCode.CtrlMask)]
    [InlineData ("Alt+Tab", KeyCode.Tab | KeyCode.AltMask)]
    [InlineData ("Ctrl+Shift+Tab", KeyCode.Tab | KeyCode.ShiftMask | KeyCode.CtrlMask)]
    [InlineData ("Ctrl+Alt+Tab", KeyCode.Tab | KeyCode.AltMask | KeyCode.CtrlMask)]
    [InlineData ("", KeyCode.Null)]
    [InlineData (" ", KeyCode.Space)]
    [InlineData ("Space", KeyCode.Space)]
    [InlineData ("Shift+Space", KeyCode.Space | KeyCode.ShiftMask)]
    [InlineData ("Ctrl+Space", KeyCode.Space | KeyCode.CtrlMask)]
    [InlineData ("Alt+Space", KeyCode.Space | KeyCode.AltMask)]
    [InlineData ("Shift+ ", KeyCode.Space | KeyCode.ShiftMask)]
    [InlineData ("Ctrl+ ", KeyCode.Space | KeyCode.CtrlMask)]
    [InlineData ("Alt+ ", KeyCode.Space | KeyCode.AltMask)]
    [InlineData ("F1", KeyCode.F1)]
    [InlineData ("0", KeyCode.D0)]
    [InlineData ("9", KeyCode.D9)]
    [InlineData ("D0", KeyCode.D0)]
    [InlineData ("65", KeyCode.A | KeyCode.ShiftMask)]
    [InlineData ("97", KeyCode.A)]
    [InlineData ("Shift", KeyCode.ShiftMask)]
    [InlineData ("Ctrl", KeyCode.CtrlMask)]
    [InlineData ("Ctrl-A", KeyCode.A | KeyCode.CtrlMask)]
    [InlineData ("Alt-A", KeyCode.A | KeyCode.AltMask)]
    [InlineData ("A-Ctrl", KeyCode.A | KeyCode.CtrlMask)]
    [InlineData ("Alt-A-Ctrl", KeyCode.A | KeyCode.CtrlMask | KeyCode.AltMask)]
    [InlineData ("120", KeyCode.X)]
    [InlineData ("Ctrl@A", KeyCode.A | KeyCode.CtrlMask)]
    public void TryParse_ShouldReturnTrue_WhenValidKey (string keyString, KeyCode expected)
    {
        Assert.True (Key.TryParse (keyString, out Key key));
        Key expectedKey = expected;
        Assert.Equal (expectedKey.ToString (), key.ToString ());
    }

    [Fact]
    public void WithShift_ShouldReturnCorrectValue ()
    {
        Key a = Key.A;
        Assert.Equal (KeyCode.A | KeyCode.ShiftMask, a.WithShift);

        Key cad = Key.Delete.WithCtrl.WithAlt;
        Assert.Equal (KeyCode.Delete | KeyCode.CtrlMask | KeyCode.AltMask, cad);
    }

    // Test Equals
    [Fact]
    public void Equals_ShouldReturnTrue_WhenEqual ()
    {
        Key a = Key.A;
        Key b = Key.A;
        Assert.True (a.Equals (b));

        // Handled is per-event state and not part of Key identity (see issue #5170).
        b.Handled = true;
        Assert.True (a.Equals (b));
    }

    [Fact]
    public void Equals_Handled_Changed_ShouldReturnTrue_WhenEqual ()
    {
        Key a = Key.A;
        a.Handled = true;
        Key b = Key.A;
        b.Handled = true;
        Assert.True (a.Equals (b));
    }

    [Fact]
    public void Equals_Handled_Changed_ShouldReturnTrue_WhenHandledDiffers ()
    {
        // Handled is per-event state and is intentionally excluded from Key identity (see issue #5170).
        Key a = Key.A;
        a.Handled = true;
        Key b = Key.A;
        Assert.True (a.Equals (b));
    }


    [Fact]
    public void KeyPressed_Handled_True_Cancels_KeyPress ()
    {
        var r = new View ();
        var args = new Key { KeyCode = KeyCode.Null };

        Assert.False (r.NewKeyDownEvent (args));
        Assert.False (args.Handled);

        r.KeyDown += (s, a) => a.Handled = true;
        Assert.True (r.NewKeyDownEvent (args));
        Assert.True (args.Handled);

        r.Dispose ();
    }

    [Fact]
    public void Set_Key_Separator_With_Rune_Default_Ensure_Using_The_Default_Plus ()
    {
        Key key = new (Key.A.WithCtrl);
        Assert.Equal ((Rune)'+', Key.Separator);
        Assert.Equal ("Ctrl+A", key.ToString ());

        // NOTE: This means this test can't be parallelized
        Key.Separator = new ('-');
        Assert.Equal ((Rune)'-', Key.Separator);
        Assert.Equal ("Ctrl-A", key.ToString ());

        Key.Separator = new ();
        Assert.Equal ((Rune)'+', Key.Separator);
        Assert.Equal ("Ctrl+A", key.ToString ());
    }
}

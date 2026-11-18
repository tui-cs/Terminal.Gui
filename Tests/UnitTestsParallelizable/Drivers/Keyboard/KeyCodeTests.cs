namespace DriverTests.Keyboard;

[Collection ("Driver Tests")]
public class KeyCodeTests
{
    private readonly ITestOutputHelper _output;
    public KeyCodeTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    public void Key_Enum_Ambiguity_Check ()
    {
        KeyCode key = KeyCode.Y | KeyCode.CtrlMask;

        // This will not be well compared.
        Assert.True (key.HasFlag (KeyCode.Q | KeyCode.CtrlMask));
        Assert.True ((key & (KeyCode.Q | KeyCode.CtrlMask)) != 0);
        Assert.Equal (KeyCode.Y | KeyCode.CtrlMask, key);
        Assert.Equal ("Y, CtrlMask", key.ToString ());

        // This will be well compared, because the Key.CtrlMask have a high value.
        Assert.False (key == Application.GetDefaultKey (Command.Quit));

        switch (key)
        {
            case KeyCode.Q | KeyCode.CtrlMask:
                // Never goes here.
                break;
            case KeyCode.Y | KeyCode.CtrlMask:
                Assert.True (key == (KeyCode.Y | KeyCode.CtrlMask));

                break;
        }
    }

    [Fact]
    public void Key_ToString ()
    {
        KeyCode k = KeyCode.Y | KeyCode.CtrlMask;
        Assert.Equal ("Y, CtrlMask", k.ToString ());

        k = KeyCode.CtrlMask | KeyCode.Y;
        Assert.Equal ("Y, CtrlMask", k.ToString ());

        k = KeyCode.Space;
        Assert.Equal ("Space", k.ToString ());

        k = KeyCode.D;
        Assert.Equal ("D", k.ToString ());

        k = (KeyCode)'d';
        Assert.Equal ("d", ((char)k).ToString ());

        k = KeyCode.D;
        Assert.Equal ("D", k.ToString ());

        // In a console this will always returns Key.D
        k = KeyCode.D | KeyCode.ShiftMask;
        Assert.Equal ("D, ShiftMask", k.ToString ());
    }

    [Fact]
    public void KeyEnum_ShouldHaveCorrectValues ()
    {
        Assert.Equal (0, (int)KeyCode.Null);
        Assert.Equal (8, (int)KeyCode.Backspace);
        Assert.Equal (9, (int)KeyCode.Tab);

        // Continue for other keys...
    }

    [Fact]
    public void SimpleEnum_And_FlagedEnum ()
    {
        SimpleEnum simple = SimpleEnum.Three | SimpleEnum.Five;

        // Nothing will not be well compared here.
        Assert.True (simple.HasFlag (SimpleEnum.Zero | SimpleEnum.Five));
        Assert.True (simple.HasFlag (SimpleEnum.One | SimpleEnum.Five));
        Assert.True (simple.HasFlag (SimpleEnum.Two | SimpleEnum.Five));
        Assert.True (simple.HasFlag (SimpleEnum.Three | SimpleEnum.Five));
        Assert.True (simple.HasFlag (SimpleEnum.Four | SimpleEnum.Five));
        Assert.True ((simple & (SimpleEnum.Zero | SimpleEnum.Five)) != 0);
        Assert.True ((simple & (SimpleEnum.One | SimpleEnum.Five)) != 0);
        Assert.True ((simple & (SimpleEnum.Two | SimpleEnum.Five)) != 0);
        Assert.True ((simple & (SimpleEnum.Three | SimpleEnum.Five)) != 0);
        Assert.True ((simple & (SimpleEnum.Four | SimpleEnum.Five)) != 0);
        Assert.Equal (7, (int)simple); // As it is not flagged only shows as number.
        Assert.Equal ("7", simple.ToString ());
        Assert.False (simple == (SimpleEnum.Zero | SimpleEnum.Five));
        Assert.False (simple == (SimpleEnum.One | SimpleEnum.Five));
        Assert.True (simple == (SimpleEnum.Two | SimpleEnum.Five));
        Assert.True (simple == (SimpleEnum.Three | SimpleEnum.Five));
        Assert.False (simple == (SimpleEnum.Four | SimpleEnum.Five));

        FlaggedEnum flagged = FlaggedEnum.Three | FlaggedEnum.Five;

        // Nothing will not be well compared here.
        Assert.True (flagged.HasFlag (FlaggedEnum.Zero | FlaggedEnum.Five));
        Assert.True (flagged.HasFlag (FlaggedEnum.One | FlaggedEnum.Five));
        Assert.True (flagged.HasFlag (FlaggedEnum.Two | FlaggedEnum.Five));
        Assert.True (flagged.HasFlag (FlaggedEnum.Three | FlaggedEnum.Five));
        Assert.True (flagged.HasFlag (FlaggedEnum.Four | FlaggedEnum.Five));
        Assert.True ((flagged & (FlaggedEnum.Zero | FlaggedEnum.Five)) != 0);
        Assert.True ((flagged & (FlaggedEnum.One | FlaggedEnum.Five)) != 0);
        Assert.True ((flagged & (FlaggedEnum.Two | FlaggedEnum.Five)) != 0);
        Assert.True ((flagged & (FlaggedEnum.Three | FlaggedEnum.Five)) != 0);
        Assert.True ((flagged & (FlaggedEnum.Four | FlaggedEnum.Five)) != 0);
        Assert.Equal (FlaggedEnum.Two | FlaggedEnum.Five, flagged); // As it is flagged shows as bitwise.
        Assert.Equal ("Two, Five", flagged.ToString ());
        Assert.False (flagged == (FlaggedEnum.Zero | FlaggedEnum.Five));
        Assert.False (flagged == (FlaggedEnum.One | FlaggedEnum.Five));
        Assert.True (flagged == (FlaggedEnum.Two | FlaggedEnum.Five));
        Assert.True (flagged == (FlaggedEnum.Three | FlaggedEnum.Five));
        Assert.False (flagged == (FlaggedEnum.Four | FlaggedEnum.Five));
    }

    [Fact]
    public void SimpleHighValueEnum_And_FlaggedHighValueEnum ()
    {
        SimpleHighValueEnum simple = SimpleHighValueEnum.Three | SimpleHighValueEnum.Last;

        // This will not be well compared.
        Assert.True (simple.HasFlag (SimpleHighValueEnum.Zero | SimpleHighValueEnum.Last));
        Assert.True (simple.HasFlag (SimpleHighValueEnum.One | SimpleHighValueEnum.Last));
        Assert.True (simple.HasFlag (SimpleHighValueEnum.Two | SimpleHighValueEnum.Last));
        Assert.True (simple.HasFlag (SimpleHighValueEnum.Three | SimpleHighValueEnum.Last));
        Assert.False (simple.HasFlag (SimpleHighValueEnum.Four | SimpleHighValueEnum.Last));
        Assert.True ((simple & (SimpleHighValueEnum.Zero | SimpleHighValueEnum.Last)) != 0);
        Assert.True ((simple & (SimpleHighValueEnum.One | SimpleHighValueEnum.Last)) != 0);
        Assert.True ((simple & (SimpleHighValueEnum.Two | SimpleHighValueEnum.Last)) != 0);
        Assert.True ((simple & (SimpleHighValueEnum.Three | SimpleHighValueEnum.Last)) != 0);
        Assert.True ((simple & (SimpleHighValueEnum.Four | SimpleHighValueEnum.Last)) != 0);

        // This will be well compared, because the SimpleHighValueEnum.Last have a high value.
        Assert.Equal (1073741827, (int)simple); // As it is not flagged only shows as number.
        Assert.Equal ("1073741827", simple.ToString ()); // As it is not flagged only shows as number.
        Assert.False (simple == (SimpleHighValueEnum.Zero | SimpleHighValueEnum.Last));
        Assert.False (simple == (SimpleHighValueEnum.One | SimpleHighValueEnum.Last));
        Assert.False (simple == (SimpleHighValueEnum.Two | SimpleHighValueEnum.Last));
        Assert.True (simple == (SimpleHighValueEnum.Three | SimpleHighValueEnum.Last));
        Assert.False (simple == (SimpleHighValueEnum.Four | SimpleHighValueEnum.Last));

        FlaggedHighValueEnum flagged = FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last;

        // This will not be well compared.
        Assert.True (flagged.HasFlag (FlaggedHighValueEnum.Zero | FlaggedHighValueEnum.Last));
        Assert.True (flagged.HasFlag (FlaggedHighValueEnum.One | FlaggedHighValueEnum.Last));
        Assert.True (flagged.HasFlag (FlaggedHighValueEnum.Two | FlaggedHighValueEnum.Last));
        Assert.True (flagged.HasFlag (FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last));
        Assert.False (flagged.HasFlag (FlaggedHighValueEnum.Four | FlaggedHighValueEnum.Last));
        Assert.True ((flagged & (FlaggedHighValueEnum.Zero | FlaggedHighValueEnum.Last)) != 0);
        Assert.True ((flagged & (FlaggedHighValueEnum.One | FlaggedHighValueEnum.Last)) != 0);
        Assert.True ((flagged & (FlaggedHighValueEnum.Two | FlaggedHighValueEnum.Last)) != 0);
        Assert.True ((flagged & (FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last)) != 0);
        Assert.True ((flagged & (FlaggedHighValueEnum.Four | FlaggedHighValueEnum.Last)) != 0);

        // This will be well compared, because the SimpleHighValueEnum.Last have a high value.
        Assert.Equal (
                      FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last,
                      flagged
                     ); // As it is flagged shows as bitwise.
        Assert.Equal ("Three, Last", flagged.ToString ()); // As it is flagged shows as bitwise.
        Assert.False (flagged == (FlaggedHighValueEnum.Zero | FlaggedHighValueEnum.Last));
        Assert.False (flagged == (FlaggedHighValueEnum.One | FlaggedHighValueEnum.Last));
        Assert.False (flagged == (FlaggedHighValueEnum.Two | FlaggedHighValueEnum.Last));
        Assert.True (flagged == (FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last));
        Assert.False (flagged == (FlaggedHighValueEnum.Four | FlaggedHighValueEnum.Last));
    }

    [Flags]
    private enum FlaggedEnum
    {
        Zero,
        One,
        Two,
        Three,
        Four,
        Five
    }

    [Flags]
    private enum FlaggedHighValueEnum
    {
        Zero,
        One,
        Two,
        Three,
        Four,
        Last = 0x40000000
    }

    private enum SimpleEnum
    {
        Zero,
        One,
        Two,
        Three,
        Four,
        Five
    }

    private enum SimpleHighValueEnum
    {
        Zero,
        One,
        Two,
        Three,
        Four,
        Last = 0x40000000
    }
}

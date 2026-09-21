namespace Terminal.Gui.KeySequences.Tests;

public class KeySequenceBindingsTests
{
    [Fact]
    public void ProcessKey_Matches_Count_Motion ()
    {
        KeySequenceBindings bindings = new ();
        int moveCount = 0;

        bindings.Add ("; m <count> k", context =>
        {
            moveCount = context.Count;
            return true;
        });

        View target = new ();

        Assert.Equal (KeySequenceResult.Started, bindings.ProcessKey (target, ';'));
        Assert.Equal (KeySequenceResult.Pending, bindings.ProcessKey (target, 'm'));
        Assert.Equal (KeySequenceResult.Pending, bindings.ProcessKey (target, '4'));
        Assert.Equal (KeySequenceResult.Matched, bindings.ProcessKey (target, 'k'));
        Assert.Equal (4, moveCount);
        Assert.False (bindings.IsCapturing);
    }

    [Fact]
    public void ProcessKey_Matches_Operator_Count_Motion ()
    {
        KeySequenceBindings bindings = new ();
        int deleteCount = 0;
        Key? operatorKey = null;
        Key? motionKey = null;

        bindings.Add ("; d <count> d", context =>
        {
            deleteCount = context.Count;
            operatorKey = context.OperatorKey;
            motionKey = context.MotionKey;
            return true;
        });

        View target = new ();

        bindings.ProcessKey (target, ';');
        bindings.ProcessKey (target, 'd');
        bindings.ProcessKey (target, '2');

        Assert.Equal (KeySequenceResult.Matched, bindings.ProcessKey (target, 'd'));
        Assert.Equal (2, deleteCount);
        Assert.Equal (Key.D, operatorKey);
        Assert.Equal (Key.D, motionKey);
    }

    [Fact]
    public void ProcessKey_Uses_Default_Count_When_Count_Is_Omitted ()
    {
        KeySequenceBindings bindings = new ();
        int moveCount = 0;

        bindings.Add ("; m <count> k", context =>
        {
            moveCount = context.Count;
            return true;
        });

        View target = new ();

        bindings.ProcessKey (target, ';');
        bindings.ProcessKey (target, 'm');

        Assert.Equal (KeySequenceResult.Matched, bindings.ProcessKey (target, 'k'));
        Assert.Equal (1, moveCount);
    }

    [Fact]
    public void ProcessKey_Cancels_With_Escape ()
    {
        KeySequenceBindings bindings = new ();
        bindings.Add ("; m <count> k", _ => true);

        View target = new ();

        bindings.ProcessKey (target, ';');

        Assert.Equal (KeySequenceResult.Canceled, bindings.ProcessKey (target, Key.Esc));
        Assert.False (bindings.IsCapturing);
    }

    [Fact]
    public void Add_Rejects_Duplicate_Pattern ()
    {
        KeySequenceBindings bindings = new ();
        bindings.Add ("; m <count> k", _ => true);

        Assert.Throws<InvalidOperationException> (() => bindings.Add ("; m <count> k", _ => true));
    }

    [Fact]
    public void PersistentMode_Enters_Matches_And_Stays_In_Command_Mode ()
    {
        KeySequenceBindings bindings = new ()
        {
            Mode = KeySequenceMode.Persistent,
            EnterModeKey = Key.Esc,
            ExitModeKey = 'i'
        };
        int moveCount = 0;

        bindings.AddMode ("<count> k", context =>
        {
            moveCount = context.Count;
            Assert.True (context.IsCommandMode);
            Assert.Null (context.LeaderKey);
            return true;
        });

        View target = new ();

        Assert.Equal (KeySequenceResult.ModeEntered, bindings.ProcessKey (target, Key.Esc));
        Assert.True (bindings.IsCommandMode);
        Assert.Equal (KeySequenceResult.Pending, bindings.ProcessKey (target, '4'));
        Assert.Equal (KeySequenceResult.Matched, bindings.ProcessKey (target, 'k'));
        Assert.Equal (4, moveCount);
        Assert.True (bindings.IsCommandMode);
    }

    [Fact]
    public void PersistentMode_Exits_With_ExitModeKey ()
    {
        KeySequenceBindings bindings = new ()
        {
            Mode = KeySequenceMode.Persistent,
            EnterModeKey = Key.Esc,
            ExitModeKey = 'i'
        };

        View target = new ();

        bindings.ProcessKey (target, Key.Esc);

        Assert.Equal (KeySequenceResult.ModeExited, bindings.ProcessKey (target, 'i'));
        Assert.False (bindings.IsCommandMode);
    }

    [Fact]
    public void PersistentMode_Rejects_Invalid_Sequence_And_Stays_In_Command_Mode ()
    {
        KeySequenceBindings bindings = new ()
        {
            Mode = KeySequenceMode.Persistent,
            EnterModeKey = Key.Esc,
            ExitModeKey = 'i'
        };
        bindings.AddMode ("k", _ => true);

        View target = new ();

        bindings.ProcessKey (target, Key.Esc);

        Assert.Equal (KeySequenceResult.Rejected, bindings.ProcessKey (target, 'x'));
        Assert.True (bindings.IsCommandMode);
    }
}

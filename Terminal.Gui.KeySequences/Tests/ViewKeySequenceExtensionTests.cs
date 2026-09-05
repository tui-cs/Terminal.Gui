namespace Terminal.Gui.KeySequences.Tests;

public class ViewKeySequenceExtensionTests
{
    [Fact]
    public void UseKeySequences_Starts_After_Unhandled_Leader_Then_Captures_Sequence ()
    {
        View view = new ();
        int moveCount = 0;

        using IDisposable registration = view.UseKeySequences (bindings =>
        {
            bindings.Add ("; m <count> k", context =>
            {
                moveCount = context.Count;
                return true;
            });
        });

        Assert.True (view.NewKeyDownEvent (';'));
        Assert.True (view.NewKeyDownEvent ('m'));
        Assert.True (view.NewKeyDownEvent ('4'));
        Assert.True (view.NewKeyDownEvent ('k'));
        Assert.Equal (4, moveCount);
    }

    [Fact]
    public void UseKeySequences_Does_Not_Handle_NonLeader_Key ()
    {
        View view = new ();

        using IDisposable registration = view.UseKeySequences (bindings =>
        {
            bindings.Add ("; m <count> k", _ => true);
        });

        Assert.False (view.NewKeyDownEvent ('x'));
    }

    [Fact]
    public void UseKeySequences_Stops_Handling_After_Dispose ()
    {
        View view = new ();
        IDisposable registration = view.UseKeySequences (bindings =>
        {
            bindings.Add ("; m <count> k", _ => true);
        });

        registration.Dispose ();

        Assert.False (view.NewKeyDownEvent (';'));
    }

    [Fact]
    public void UseKeySequences_Captures_Keys_While_Command_Mode_Is_Active ()
    {
        View view = new ();
        int moveCount = 0;

        using IDisposable registration = view.UseKeySequences (
            bindings =>
            {
                bindings.Mode = KeySequenceMode.Persistent;
                bindings.EnterModeKey = Key.Esc;
                bindings.ExitModeKey = 'i';
                bindings.AddMode ("<count> k", context =>
                {
                    moveCount = context.Count;
                    return true;
                });
            },
            KeySequenceInterceptionMode.Preemptive);

        Assert.True (view.NewKeyDownEvent (Key.Esc));
        Assert.True (view.NewKeyDownEvent ('3'));
        Assert.True (view.NewKeyDownEvent ('k'));
        Assert.Equal (3, moveCount);
        Assert.True (view.NewKeyDownEvent ('i'));
    }
}

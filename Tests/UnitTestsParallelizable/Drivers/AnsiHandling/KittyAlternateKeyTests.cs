// Copilot - Opus 4.6

using System.Collections.Concurrent;

namespace DriverTests.AnsiHandling;

/// <summary>
///     Tests for kitty keyboard protocol alternate key reporting (flag 4: ReportAlternateKeys).
///     Covers <c>Key.ShiftedKeyCode</c>, <c>Key.BaseLayoutKeyCode</c>, copy constructor preservation,
///     equality semantics, and end-to-end propagation from raw ANSI sequences to <c>View.KeyDown</c>.
/// </summary>
public class KittyAlternateKeyTests
{
    #region Key Object Behavior

    // Copilot - Opus 4.6
    [Fact]
    public void AlternateKeys_PreservedByCopyConstructor ()
    {
        Key original = new (Key.A) { ShiftedKeyCode = (KeyCode)64, BaseLayoutKeyCode = (KeyCode)50 };

        Key copy = new (original);

        Assert.Equal ((KeyCode)64, copy.ShiftedKeyCode);
        Assert.Equal ((KeyCode)50, copy.BaseLayoutKeyCode);
        Assert.Equal (original.KeyCode, copy.KeyCode);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void AlternateKeys_DoNotAffectEquality ()
    {
        Key withAlternates = new (Key.A) { ShiftedKeyCode = (KeyCode)64, BaseLayoutKeyCode = (KeyCode)50 };

        Key withoutAlternates = new (Key.A);

        Assert.Equal (withoutAlternates, withAlternates);
        Assert.Equal (withoutAlternates.GetHashCode (), withAlternates.GetHashCode ());
    }

    #endregion

    #region End-to-End: Raw ANSI → View.KeyDown

    /// <summary>
    ///     Injects raw ANSI sequences through the full pipeline and captures keys at both
    ///     <c>app.Keyboard.KeyDown</c> and <c>View.KeyDown</c>, proving the fields survive
    ///     from parser → driver → application → view.
    /// </summary>
    private static (List<Key> AppKeyDown, List<Key> ViewKeyDown, List<Key> ViewKeyUp) InjectRawSequenceToView (params string [] sequences)
    {
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> appKeyDown = [];
        List<Key> viewKeyDown = [];
        List<Key> viewKeyUp = [];

        app.Keyboard.KeyDown += (_, key) => appKeyDown.Add (key);

        View view = new () { CanFocus = true, App = app };
        view.KeyDown += (_, key) => viewKeyDown.Add (key);
        view.KeyUp += (_, key) => viewKeyUp.Add (key);

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);
        view.SetFocus ();

        IInputProcessor processor = app.Driver?.GetInputProcessor ()!;
        ConcurrentQueue<char> queue = ((AnsiInputProcessor)processor).InputQueue;

        foreach (string seq in sequences)
        {
            foreach (char ch in seq)
            {
                queue.Enqueue (ch);
            }

            processor.ProcessQueue ();
        }

        app.End (token!);
        top.Dispose ();

        return (appKeyDown, viewKeyDown, viewKeyUp);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void ViewKeyDown_ShiftedAndBase_Preserved ()
    {
        // Previous fixture ESC[97:64:50;2u was misleading because 'a', '@', and US-base '2' do not describe
        // a coherent US-keyboard case.
        // ESC[50:64;2;64u = '2' with shifted key '@' (64), Shift modifier (2),
        // and associated text '@' (64). The redundant base-layout field is omitted because
        // it would duplicate the primary key code on a US keyboard.
        (List<Key> appDown, List<Key> viewDown, _) = InjectRawSequenceToView ("\x1b[50:64;2;64u");

        // App level
        Assert.Single (appDown);
        Assert.Equal ((KeyCode)64, appDown [0].ShiftedKeyCode);
        Assert.Equal (KeyCode.Null, appDown [0].BaseLayoutKeyCode);
        Assert.Equal ("@", appDown [0].AssociatedText);

        // View level — same alternate key fields must arrive
        Assert.Single (viewDown);
        Assert.Equal (new Key ('@').WithShift, viewDown [0]);
        Assert.Equal ((KeyCode)64, viewDown [0].ShiftedKeyCode);
        Assert.Equal (KeyCode.Null, viewDown [0].BaseLayoutKeyCode);
        Assert.Equal ("@", viewDown [0].AssociatedText);
    }

    [Fact]
    public void ViewKeyDown_AssociatedText_Preserved ()
    {
        // ESC[49;2;33u = physical '1' key with Shift modifier and associated text '!'
        (List<Key> appDown, List<Key> viewDown, _) = InjectRawSequenceToView ("\x1b[49;2;33u");

        Assert.Single (appDown);
        Assert.Equal ("!", appDown [0].AssociatedText);
        Assert.Equal ("!", appDown [0].GetPrintableText ());

        Assert.Single (viewDown);
        Assert.Equal (new Key ('!').WithShift, viewDown [0]);
        Assert.Equal ("!", viewDown [0].AssociatedText);
        Assert.Equal ("!", viewDown [0].GetPrintableText ());
    }

    // Copilot - Opus 4.6
    [Fact]
    public void ViewKeyDown_ShiftedOnly_Preserved ()
    {
        // Previous fixture ESC[97:64u was misleading because 'a' -> '@' is not a sensible US-keyboard example.
        // ESC[50:64u = '2' with shifted '@' (64), no base layout, no modifier field
        (_, List<Key> viewDown, _) = InjectRawSequenceToView ("\x1b[50:64u");

        Assert.Single (viewDown);
        Assert.Equal (Key.D2, viewDown [0]);
        Assert.Equal ((KeyCode)64, viewDown [0].ShiftedKeyCode);
        Assert.Equal (KeyCode.Null, viewDown [0].BaseLayoutKeyCode);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void ViewKeyDown_BaseLayoutOnly_Preserved ()
    {
        // ESC[50:0:50u = '2' with no shifted (0), base layout '2' (50)
        (_, List<Key> viewDown, _) = InjectRawSequenceToView ("\x1b[50:0:50u");

        Assert.Single (viewDown);
        Assert.Equal (Key.D2, viewDown [0]);
        Assert.Equal (KeyCode.Null, viewDown [0].ShiftedKeyCode);
        Assert.Equal ((KeyCode)50, viewDown [0].BaseLayoutKeyCode);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void ViewKeyDown_NoAlternateKeys_DefaultsToNull ()
    {
        // ESC[97u = plain 'a' — no alternate key fields at all
        (_, List<Key> viewDown, _) = InjectRawSequenceToView ("\x1b[97u");

        Assert.Single (viewDown);
        Assert.Equal (Key.A, viewDown [0]);
        Assert.Equal (KeyCode.Null, viewDown [0].ShiftedKeyCode);
        Assert.Equal (KeyCode.Null, viewDown [0].BaseLayoutKeyCode);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void ViewKeyDown_WithModifiersAndEventType_Preserved ()
    {
        // ESC[50:64;6:1u = coherent US-keyboard example: '2', shifted '@', Ctrl+Shift, press
        (_, List<Key> viewDown, _) = InjectRawSequenceToView ("\x1b[50:64;6:1u");

        Assert.Single (viewDown);
        Assert.Equal (new Key ('@').WithCtrl.WithShift, viewDown [0]);
        Assert.Equal ((KeyCode)64, viewDown [0].ShiftedKeyCode);
        Assert.Equal (KeyCode.Null, viewDown [0].BaseLayoutKeyCode);
        Assert.Equal (KeyEventType.Press, viewDown [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void ViewKeyUp_AlternateKeys_Preserved ()
    {
        // ESC[50:64;2:3u = coherent US-keyboard example, Shift, release
        (_, List<Key> viewDown, List<Key> viewUp) = InjectRawSequenceToView ("\x1b[50:64;2:3u");

        // Release events go to KeyUp, not KeyDown
        Assert.Empty (viewDown);
        Assert.Single (viewUp);
        Assert.Equal (new Key ('@').WithShift, viewUp [0]);
        Assert.Equal ((KeyCode)64, viewUp [0].ShiftedKeyCode);
        Assert.Equal (KeyCode.Null, viewUp [0].BaseLayoutKeyCode);
        Assert.Equal (KeyEventType.Release, viewUp [0].EventType);
        Assert.Equal (string.Empty, viewUp [0].AssociatedText);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void ViewKeyDown_FullCycle_PressRepeatRelease ()
    {
        // Simulates holding Shift+'2': press → repeat → release
        // All three should carry the same alternate key fields
        (_, List<Key> viewDown, List<Key> viewUp) = InjectRawSequenceToView ("\x1b[50:64;2;64u", // press
                                                                             "\x1b[50:64;2:2u", // repeat
                                                                             "\x1b[50:64;2:3u" // release
                                                                            );

        Assert.Equal (2, viewDown.Count);
        Assert.Equal (KeyEventType.Press, viewDown [0].EventType);
        Assert.Equal (KeyEventType.Repeat, viewDown [1].EventType);

        Assert.Single (viewUp);
        Assert.Equal (KeyEventType.Release, viewUp [0].EventType);

        // ALL events must carry the alternate key fields
        List<Key> all = [.. viewDown, .. viewUp];

        foreach (Key k in all)
        {
            Assert.Equal ((KeyCode)64, k.ShiftedKeyCode);
            Assert.Equal (KeyCode.Null, k.BaseLayoutKeyCode);
        }

        Assert.Equal ("@", viewDown [0].AssociatedText);
        Assert.Equal (string.Empty, viewDown [1].AssociatedText);
        Assert.Equal (string.Empty, viewUp [0].AssociatedText);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void ViewKeyDown_InternationalLayout_PhysicalPosition ()
    {
        // Simulates a French AZERTY keyboard pressing the physical 'A' key:
        // The key produces 'q' (113) on AZERTY, but the base layout is 'a' (97) in US.
        // ESC[113:0:97u = primary 'q', no shifted, base layout 'a'
        (_, List<Key> viewDown, _) = InjectRawSequenceToView ("\x1b[113:0:97u");

        Assert.Single (viewDown);

        // The primary key code is 'q' — Key(113) maps lowercase to KeyCode.Q (81)
        Assert.Equal (KeyCode.Q, viewDown [0].KeyCode);

        // BaseLayoutKeyCode reveals the US-layout physical position is 'a' (97)
        Assert.Equal ((KeyCode)97, viewDown [0].BaseLayoutKeyCode);

        // ShiftedKeyCode is not reported (0 → Null)
        Assert.Equal (KeyCode.Null, viewDown [0].ShiftedKeyCode);
    }

    #endregion
}

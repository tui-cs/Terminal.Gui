// Claude - Opus 4.7
using System.Collections.Concurrent;

namespace InputTests;

public class KeyEqualityTests
{
    [Fact]
    public void Equals_ReturnsTrue_WhenHandledDiffers ()
    {
        Key a = new (KeyCode.F1) { Handled = false };
        Key b = new (KeyCode.F1) { Handled = true };

        Assert.True (a.Equals (b));
    }

    [Fact]
    public void GetHashCode_IsEqual_WhenHandledDiffers ()
    {
        Key a = new (KeyCode.F1) { Handled = false };
        Key b = new (KeyCode.F1) { Handled = true };

        Assert.Equal (a.GetHashCode (), b.GetHashCode ());
    }

    [Fact]
    public void ConcurrentDictionary_Lookup_Succeeds_WhenHandledDiffers ()
    {
        // Regression test for issue #5170: KeyBindings is a ConcurrentDictionary<Key, KeyBinding>
        // and lookup must succeed when the lookup Key has a different Handled value than the stored Key.
        ConcurrentDictionary<Key, string> bindings = new ();
        Key a = new (KeyCode.F1) { Handled = false };
        Key b = new (KeyCode.F1) { Handled = true };
        bindings [a] = "binding-A";

        Assert.True (bindings.TryGetValue (b, out string? found));
        Assert.Equal ("binding-A", found);
    }
}

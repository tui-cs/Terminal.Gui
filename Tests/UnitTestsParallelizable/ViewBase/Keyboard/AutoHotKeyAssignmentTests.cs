using System.Globalization;

namespace ViewBaseTests.Keyboard;

/// <summary>
///     Tests for the automatic hotkey assignment feature in <see cref="View"/>.
///     This feature was added to address Issue #4145.
/// </summary>

// Claude - Opus 4.5
public class AutoHotKeyAssignmentTests
{
    #region Default Values

    [Fact]
    public void AssignHotKeys_Defaults_To_False ()
    {
        View view = new ();
        Assert.False (view.AssignHotKeys);
    }

    [Fact]
    public void UsedHotKeys_Defaults_To_Empty ()
    {
        View view = new ();
        Assert.Empty (view.UsedHotKeys);
    }

    #endregion

    #region AssignHotKeys Property Tests

    [Fact]
    public void AssignHotKeys_True_AssignsHotKeysToSubViews ()
    {
        View superView = new () { AssignHotKeys = true };

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };

        superView.Add (button1, button2);

        Assert.NotEqual (Key.Empty, button1.HotKey);
        Assert.NotEqual (Key.Empty, button2.HotKey);
    }

    [Fact]
    public void AssignHotKeys_True_AddsHotKeySpecifierToTitles ()
    {
        View superView = new () { AssignHotKeys = true };

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };

        superView.Add (button1, button2);

        Assert.Contains ("_", button1.Title);
        Assert.Contains ("_", button2.Title);
    }

    [Fact]
    public void AssignHotKeys_True_AssignsUniqueHotKeys ()
    {
        View superView = new () { AssignHotKeys = true };

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Send" };
        View button3 = new () { Title = "Submit" };

        superView.Add (button1, button2, button3);

        // All should have unique hotkeys
        HashSet<Key> hotKeys = [button1.HotKey, button2.HotKey, button3.HotKey];
        Assert.Equal (3, hotKeys.Count);
    }

    [Fact]
    public void AssignHotKeys_False_DoesNotAssignHotKeys ()
    {
        View superView = new () { AssignHotKeys = false };

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };

        superView.Add (button1, button2);

        Assert.Equal (Key.Empty, button1.HotKey);
        Assert.Equal (Key.Empty, button2.HotKey);
    }

    [Fact]
    public void AssignHotKeys_PreservesExistingHotKeysInTitle ()
    {
        View superView = new () { AssignHotKeys = true };

        View button = new () { Title = "_Alt Option" };

        superView.Add (button);

        // Should use 'A' from "_Alt"
        Assert.Equal (Key.A, button.HotKey);
    }

    [Fact]
    public void AssignHotKeys_PreservesProgrammaticHotKey ()
    {
        View superView = new () { AssignHotKeys = true };

        View button = new () { Title = "Option", HotKey = Key.X };

        superView.Add (button);

        // Should preserve the programmatically set hotkey
        Assert.Equal (Key.X, button.HotKey);
        Assert.Contains (Key.X, superView.UsedHotKeys);
    }

    [Fact]
    public void AssignHotKeys_SetTrue_AssignsToExistingSubViews ()
    {
        View superView = new ();

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };

        superView.Add (button1, button2);

        // Initially no hotkeys
        Assert.Equal (Key.Empty, button1.HotKey);
        Assert.Equal (Key.Empty, button2.HotKey);

        // Enable auto-assignment
        superView.AssignHotKeys = true;

        // Now hotkeys should be assigned
        Assert.NotEqual (Key.Empty, button1.HotKey);
        Assert.NotEqual (Key.Empty, button2.HotKey);
    }

    #endregion

    #region UsedHotKeys Tests

    [Fact]
    public void UsedHotKeys_SkipsMarkedKeys ()
    {
        View superView = new () { AssignHotKeys = true };
        superView.UsedHotKeys.Add (Key.S); // Mark 'S' as used

        View button = new () { Title = "Save" };

        superView.Add (button);

        // Should skip 'S' and use next available character
        Assert.NotEqual (Key.S, button.HotKey);
        Assert.Equal (Key.A, button.HotKey); // 'a' from "Save"
    }

    [Fact]
    public void UsedHotKeys_PopulatedWhenHotKeysAssigned ()
    {
        View superView = new () { AssignHotKeys = true };

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };

        superView.Add (button1, button2);

        // UsedHotKeys should contain the assigned hotkeys
        Assert.NotEmpty (superView.UsedHotKeys);
        Assert.Contains (button1.HotKey, superView.UsedHotKeys);
        Assert.Contains (button2.HotKey, superView.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_ClearedWhenSubViewRemoved ()
    {
        View superView = new () { AssignHotKeys = true };

        View button = new () { Title = "Save" };
        superView.Add (button);

        Key assignedKey = button.HotKey;
        Assert.Contains (assignedKey, superView.UsedHotKeys);

        superView.Remove (button);

        Assert.DoesNotContain (assignedKey, superView.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_ClearedWhenRemoveAllCalled ()
    {
        View superView = new () { AssignHotKeys = true };

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };
        superView.Add (button1, button2);

        Assert.NotEmpty (superView.UsedHotKeys);

        foreach (View removed in superView.RemoveAll ())
        {
            removed.Dispose ();
        }

        Assert.Empty (superView.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_ClearedWhenTitleChangesAndIsInitialized ()
    {
        View superView = new () { AssignHotKeys = true };

        View button = new () { Title = "Save" };
        superView.Add (button);
        superView.BeginInit ();
        superView.EndInit ();

        button.Title = "Open";

        // HotKey should be reassigned after title changes
        Assert.NotEqual (Key.S, button.HotKey);
        Assert.DoesNotContain (Key.S, superView.UsedHotKeys);
        Assert.Equal (Key.O, button.HotKey); // 'o' from "Open"
        Assert.Contains (button.HotKey, superView.UsedHotKeys);
    }

    // CoPilot - ChatGPT
    [Fact]
    public void UsedHotKeys_UpdatedWhenInitializedTitleChangesToExplicitHotKey ()
    {
        View superView = new () { AssignHotKeys = true };

        View button = new () { Title = "Save" };
        superView.Add (button);
        superView.BeginInit ();
        superView.EndInit ();

        button.Title = "O_pen";

        Assert.Equal (Key.P, button.HotKey);
        Assert.DoesNotContain (Key.S, superView.UsedHotKeys);
        Assert.Contains (Key.P, superView.UsedHotKeys);
    }

    // CoPilot - ChatGPT
    [Fact]
    public void UsedHotKeys_ClearedWhenInitializedTitleChangesAfterProgrammaticHotKey ()
    {
        View superView = new () { AssignHotKeys = true };

        View button = new () { Title = "Option", HotKey = Key.X };
        superView.Add (button);
        superView.BeginInit ();
        superView.EndInit ();

        button.Title = "Open";

        Assert.Equal (Key.O, button.HotKey);
        Assert.DoesNotContain (Key.X, superView.UsedHotKeys);
        Assert.Contains (Key.O, superView.UsedHotKeys);
    }

    #endregion

    #region AssignHotKeysToSubViews Tests

    [Fact]
    public void AssignHotKeysToSubViews_ManualCall_AssignsHotKeys ()
    {
        View superView = new ();

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };
        superView.Add (button1, button2);

        // Initially no hotkeys
        Assert.Equal (Key.Empty, button1.HotKey);
        Assert.Equal (Key.Empty, button2.HotKey);

        // Enable and manually call
        superView.AssignHotKeys = true;
        superView.AssignHotKeysToSubViews ();

        Assert.NotEqual (Key.Empty, button1.HotKey);
        Assert.NotEqual (Key.Empty, button2.HotKey);
    }

    [Fact]
    public void AssignHotKeysToSubViews_SkipsViewsWithEmptyTitle ()
    {
        View superView = new () { AssignHotKeys = true };

        View button1 = new () { Title = "" };
        View button2 = new () { Title = "Cancel" };

        superView.Add (button1, button2);

        Assert.Equal (Key.Empty, button1.HotKey);
        Assert.NotEqual (Key.Empty, button2.HotKey);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AssignHotKeys_HandlesConflictingExistingHotKey ()
    {
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture; // Ensure consistent test results

        View superView = new () { AssignHotKeys = true };

        View button1 = new () { Title = Strings.cmdSave }; // Uses 'S'
        superView.Add (button1);

        View button2 = new () { Title = "_Save Again" }; // Also uses 'S'
        superView.Add (button2);

        // First button should keep 'S', second should get reassigned
        Assert.Equal (Key.S, button1.HotKey);
        Assert.NotEqual (Key.S, button2.HotKey);
        Assert.NotEqual (Key.Empty, button2.HotKey);
    }

    [Fact]
    public void AssignHotKeys_SkipsSpaceAndControlCharacters ()
    {
        View superView = new () { AssignHotKeys = true };

        View button = new () { Title = "  A B C  " }; // Spaces before valid characters

        superView.Add (button);

        // Should skip spaces and use first valid character
        Assert.Equal (Key.A, button.HotKey);
    }

    [Fact]
    public void AssignHotKeys_WorksWithUnicodeCharacters ()
    {
        View superView = new () { AssignHotKeys = true };

        View button = new () { Title = "Ökologie" }; // German umlaut

        superView.Add (button);

        // Should assign a hotkey from the title
        Assert.NotEqual (Key.Empty, button.HotKey);
    }

    [Fact]
    public void AssignHotKeys_NoHotKeyWhenAllCharactersUsed ()
    {
        View superView = new () { AssignHotKeys = true };

        // Pre-mark all letters in "AB" as used
        superView.UsedHotKeys.Add (Key.A);
        superView.UsedHotKeys.Add (Key.B);

        View button = new () { Title = "AB" };
        superView.Add (button);

        // No hotkey can be assigned
        Assert.Equal (Key.Empty, button.HotKey);
    }

    #endregion
}

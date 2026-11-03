using System.Globalization;
using System.Text;

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
    public void UsedHotKeys_ReassignedWhenInitializedTitleChanges ()
    {
        View superView = new () { AssignHotKeys = true };

        View button = new () { Title = "Save" };
        superView.Add (button);
        superView.BeginInit ();
        superView.EndInit ();

        int hotKeyChangedCount = 0;
        bool hotKeyChangedBeforeTitle = false;
        button.HotKeyChanged += (_, _) =>
                                {
                                    hotKeyChangedCount++;
                                    hotKeyChangedBeforeTitle |= button.Title == "_Save";
                                };
        button.Title = "Open";

        // HotKey should be reassigned after title changes
        Assert.False (hotKeyChangedBeforeTitle);
        Assert.Equal (1, hotKeyChangedCount);
        Assert.NotEqual (Key.S, button.HotKey);
        Assert.DoesNotContain (Key.S, superView.UsedHotKeys);
        Assert.Equal (Key.O, button.HotKey); // 'o' from "Open"
        Assert.Contains (button.HotKey, superView.UsedHotKeys);
    }

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

    [Fact]
    public void UsedHotKeys_ProgrammaticHotKeySetBeforeInitializationIsPreservedWhenTitleChanges ()
    {
        View superView = new () { AssignHotKeys = true };

        View button = new () { Title = "Option", HotKey = Key.X };
        superView.Add (button);
        superView.BeginInit ();
        superView.EndInit ();

        button.Title = "Open";

        Assert.Equal (Key.X, button.HotKey);
        Assert.DoesNotContain (Key.S, superView.UsedHotKeys);
        Assert.Contains (Key.X, superView.UsedHotKeys);
        Assert.Single (superView.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_TitleChangeDoesNotReleaseAnotherSubViewsReservation ()
    {
        View superView = new () { AssignHotKeys = true };
        View firstButton = new () { Title = "_Save" };
        View secondButton = new () { Title = "_Save" };
        superView.Add (firstButton, secondButton);
        superView.BeginInit ();
        superView.EndInit ();

        Key firstHotKey = firstButton.HotKey;
        Key secondHotKey = secondButton.HotKey;
        secondButton.Title = "Open";

        Assert.Equal (Key.S, firstHotKey);
        Assert.NotEqual (firstHotKey, secondHotKey);
        Assert.Contains (firstHotKey, superView.UsedHotKeys);
        Assert.DoesNotContain (secondHotKey, superView.UsedHotKeys);
        Assert.Equal (Key.O, secondButton.HotKey);
    }

    [Fact]
    public void UsedHotKeys_TitleChangeUsesFallbackWhenPreferredKeyIsReserved ()
    {
        View superView = new () { AssignHotKeys = true };
        View firstButton = new () { Title = "_Open" };
        View secondButton = new () { Title = "Save" };
        superView.Add (firstButton, secondButton);
        superView.BeginInit ();
        superView.EndInit ();

        Key previousHotKey = secondButton.HotKey;
        secondButton.Title = "_Open";

        Assert.Equal (Key.O, firstButton.HotKey);
        Assert.NotEqual (firstButton.HotKey, secondButton.HotKey);
        Assert.DoesNotContain (previousHotKey, superView.UsedHotKeys);
        Assert.Contains (secondButton.HotKey, superView.UsedHotKeys);
    }

    [Theory]
    [InlineData ("")]
    [InlineData (null)]
    public void UsedHotKeys_TitleChangeToEmptyTitleReleasesReservation (string? title)
    {
        View superView = new () { AssignHotKeys = true };
        View button = new () { Title = "Save" };
        superView.Add (button);
        superView.BeginInit ();
        superView.EndInit ();

        Key previousHotKey = button.HotKey;
        button.Title = title!;

        Assert.Equal (Key.Empty, button.HotKey);
        Assert.DoesNotContain (previousHotKey, superView.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_TitleChangePreservesUnreservedProgrammaticHotKey ()
    {
        View superView = new () { AssignHotKeys = true };
        View button = new () { Title = "Save" };
        superView.Add (button);
        superView.BeginInit ();
        superView.EndInit ();

        button.HotKey = Key.X;
        button.Title = "Open";

        Assert.Equal (Key.X, button.HotKey);
        Assert.DoesNotContain (Key.S, superView.UsedHotKeys);
        Assert.Contains (Key.X, superView.UsedHotKeys);
    }

    [Fact]
    public void AssignHotKeysToSubViews_CalledRepeatedlyPreservesProgrammaticHotKey ()
    {
        View superView = new () { AssignHotKeys = true };
        View button = new () { Title = "Option", HotKey = Key.X };
        superView.Add (button);

        superView.AssignHotKeysToSubViews ();
        superView.AssignHotKeysToSubViews ();

        Assert.Equal (Key.X, button.HotKey);
        Assert.Contains (Key.X, superView.UsedHotKeys);
        Assert.Single (superView.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_TitleChangeDoesNotAutoAssignWhenDisabled ()
    {
        View superView = new () { AssignHotKeys = false };
        View button = new () { Title = "_Save" };
        superView.Add (button);
        superView.BeginInit ();
        superView.EndInit ();

        superView.UsedHotKeys.Add (Key.X);
        button.Title = "_Open";

        Assert.Equal (Key.O, button.HotKey);
        Assert.Single (superView.UsedHotKeys);
        Assert.Contains (Key.X, superView.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_CancelledTitleChangeKeepsReservation ()
    {
        View superView = new () { AssignHotKeys = true };
        View button = new () { Title = "Save" };
        superView.Add (button);
        superView.BeginInit ();
        superView.EndInit ();

        Key hotKey = button.HotKey;
        HashSet<Key> usedHotKeys = [.. superView.UsedHotKeys];
        button.TitleChanging += (_, args) => args.Cancel = true;
        button.Title = "Open";

        Assert.Equal ("_Save", button.Title);
        Assert.Equal (hotKey, button.HotKey);
        Assert.Equal (usedHotKeys, superView.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_TitleChangeBeforeInitializationReleasesAndReassignsReservation ()
    {
        View superView = new () { AssignHotKeys = true };
        View button = new () { Title = "Save" };
        superView.Add (button);

        Key previousHotKey = button.HotKey;
        button.Title = "_Open";

        Assert.DoesNotContain (previousHotKey, superView.UsedHotKeys);
        Assert.Equal (Key.O, button.HotKey);
        Assert.Contains (button.HotKey, superView.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_TitleChangeAfterRemovalDoesNotUseSuperView ()
    {
        View superView = new () { AssignHotKeys = true };
        View button = new () { Title = "Save" };
        superView.Add (button);
        superView.BeginInit ();
        superView.EndInit ();
        superView.Remove (button);

        button.Title = "_Open";

        Assert.Null (button.SuperView);
        Assert.Equal (Key.O, button.HotKey);
        Assert.Empty (superView.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_TitleChangeWithNoAvailableCharacterClearsHotKey ()
    {
        View superView = new () { AssignHotKeys = true };
        View button = new () { Title = "AB" };
        superView.Add (button);
        superView.BeginInit ();
        superView.EndInit ();
        superView.UsedHotKeys.Add (Key.C);
        superView.UsedHotKeys.Add (Key.D);

        button.Title = "CD";

        Assert.Equal (Key.Empty, button.HotKey);
        Assert.DoesNotContain (Key.A, superView.UsedHotKeys);
        Assert.Contains (Key.C, superView.UsedHotKeys);
        Assert.Contains (Key.D, superView.UsedHotKeys);
    }

    [Fact]
    public void AssignHotKeys_TogglingOffAndOnKeepsExistingReservations ()
    {
        View superView = new () { AssignHotKeys = true };
        View button = new () { Title = "Save" };
        superView.Add (button);

        Key hotKey = button.HotKey;
        superView.AssignHotKeys = false;
        superView.AssignHotKeys = true;

        Assert.Equal (hotKey, button.HotKey);
        Assert.Contains (hotKey, superView.UsedHotKeys);
        Assert.Single (superView.UsedHotKeys);
    }

    [Fact]
    public void HotKeySpecifier_ChangedOnInitializedViewReassignsReservation ()
    {
        View superView = new () { AssignHotKeys = true };
        View button = new () { Title = "Save" };
        superView.Add (button);
        superView.BeginInit ();
        superView.EndInit ();

        button.HotKeySpecifier = (Rune)'&';

        Assert.Equal (Key.S, button.HotKey);
        Assert.Single (superView.UsedHotKeys);
        Assert.Contains (Key.S, superView.UsedHotKeys);
        Assert.Contains (button.HotKey, superView.UsedHotKeys);
        Assert.Contains ("&", button.Title);
    }

    [Fact]
    public void UsedHotKeys_RemovingSubviewReleasesOnlyItsReservationAndAllowsReadd ()
    {
        View originalSuperView = new () { AssignHotKeys = true };
        View firstButton = new () { Title = "_Save" };
        View secondButton = new () { Title = "_Save" };
        originalSuperView.Add (firstButton, secondButton);
        Key firstHotKey = firstButton.HotKey;
        Key secondHotKey = secondButton.HotKey;

        originalSuperView.Remove (secondButton);
        secondButton.Title = "Cancel";

        View newSuperView = new () { AssignHotKeys = true };
        newSuperView.Add (secondButton);

        Assert.Contains (firstHotKey, originalSuperView.UsedHotKeys);
        Assert.DoesNotContain (secondHotKey, originalSuperView.UsedHotKeys);
        Assert.Contains (secondButton.HotKey, newSuperView.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_RemovingSubviewKeepsManuallyAddedReservation ()
    {
        View superView = new () { AssignHotKeys = true };
        View button = new () { Title = "Save" };
        superView.Add (button);
        superView.UsedHotKeys.Add (Key.X);
        button.HotKey = Key.X;

        superView.Remove (button);

        Assert.Contains (Key.X, superView.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_ReassignmentAfterClearDoesNotReleaseNewOwnersReservation ()
    {
        View superView = new () { AssignHotKeys = true };
        View originalOwner = new () { Title = "Save" };
        superView.Add (originalOwner);
        superView.UsedHotKeys.Clear ();

        View newOwner = new () { Title = "Cancel" };
        superView.Add (newOwner);
        newOwner.HotKey = Key.S;
        superView.UsedHotKeys.Add (Key.S);
        originalOwner.Title = "Open";

        Assert.Equal (Key.S, newOwner.HotKey);
        Assert.Contains (Key.S, superView.UsedHotKeys);
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

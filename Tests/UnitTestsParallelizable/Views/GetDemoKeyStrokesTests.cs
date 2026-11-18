// Copilot - Opus 4.6
using System.Reflection;
using UnitTests;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="IDesignable.GetDemoKeyStrokes"/> implementations.
/// </summary>
public class GetDemoKeyStrokesTests : TestsAllViews
{
    /// <summary>
    ///     Verifies that all views implementing IDesignable that explicitly override GetDemoKeyStrokes
    ///     return either null (static view) or a non-empty, well-formed tuirec keystroke string.
    /// </summary>
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllDesignableViews_GetDemoKeyStrokes_Returns_Valid_Or_Null (Type viewType)
    {
        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view is null)
        {
            return;
        }

        if (view is not IDesignable designable)
        {
            view.Dispose ();

            return;
        }

        string? keystrokes = designable.GetDemoKeyStrokes ();

        if (keystrokes is not null)
        {
            // Must not be empty or whitespace
            Assert.False (string.IsNullOrWhiteSpace (keystrokes), $"{viewType.Name}.GetDemoKeyStrokes() returned empty/whitespace string. Should return null for no interaction.");

            // Must be comma-separated tokens
            string [] tokens = keystrokes.Split (',');
            Assert.True (tokens.Length > 0, $"{viewType.Name}.GetDemoKeyStrokes() returned no tokens.");

            // Each token must be a valid tuirec token format
            foreach (string token in tokens)
            {
                string trimmed = token.Trim ();
                Assert.False (string.IsNullOrWhiteSpace (trimmed), $"{viewType.Name}.GetDemoKeyStrokes() has empty token in: {keystrokes}");

                // Valid token types: wait:<ms>, click:col:row, move:col:row, `literal text`, or a Key name
                bool isValid = trimmed.StartsWith ("wait:", StringComparison.Ordinal)
                               || trimmed.StartsWith ("click:", StringComparison.Ordinal)
                               || trimmed.StartsWith ("move:", StringComparison.Ordinal)
                               || (trimmed.StartsWith ('`') && trimmed.EndsWith ('`'))
                               || IsValidKeyName (trimmed);

                Assert.True (isValid, $"{viewType.Name}.GetDemoKeyStrokes() has invalid token '{trimmed}' in: {keystrokes}");
            }
        }

        view.Dispose ();
    }

    /// <summary>
    ///     Verifies that views that do NOT explicitly implement GetDemoKeyStrokes
    ///     use the default interface implementation (returns null).
    ///     This catches the ProgressBar anti-pattern of explicitly returning null.
    /// </summary>
    [Fact]
    public void DefaultInterfaceImplementation_Returns_Null ()
    {
        // Views that don't implement GetDemoKeyStrokes should return null via the
        // default interface implementation. Use a view that genuinely has no demo.
        ImageView iv = new ();
        IDesignable designable = iv;
        Assert.Null (designable.GetDemoKeyStrokes ());
        iv.Dispose ();
    }

    /// <summary>
    ///     Verifies specific views return non-null keystrokes (they have interactive demos).
    /// </summary>
    [Theory]
    [InlineData (typeof (Button))]
    [InlineData (typeof (TextField))]
    [InlineData (typeof (ListView))]
    [InlineData (typeof (OptionSelector))]
    [InlineData (typeof (FlagSelector))]
    [InlineData (typeof (DropDownList))]
    [InlineData (typeof (Tabs))]
    [InlineData (typeof (SpinnerView))]
    [InlineData (typeof (TreeView))]
    [InlineData (typeof (TableView))]
    [InlineData (typeof (DateEditor))]
    [InlineData (typeof (TimeEditor))]
    [InlineData (typeof (HexView))]
    [InlineData (typeof (ColorPicker))]
    [InlineData (typeof (TextView))]
    [InlineData (typeof (Terminal.Gui.Views.Markdown))]
    [InlineData (typeof (ProgressBar))]
    public void InteractiveViews_Return_NonNull_KeyStrokes (Type viewType)
    {
        View? view = Activator.CreateInstance (viewType) as View;
        Assert.NotNull (view);

        IDesignable? designable = view as IDesignable;
        Assert.NotNull (designable);

        string? keystrokes = designable.GetDemoKeyStrokes ();
        Assert.NotNull (keystrokes);
        Assert.False (string.IsNullOrWhiteSpace (keystrokes), $"{viewType.Name} should return non-null keystrokes");

        view.Dispose ();
    }

    private static bool IsValidKeyName (string token)
    {
        // Valid key names: single chars, named keys (CursorDown, Enter, Escape, etc.),
        // modifier combos (Ctrl+C, Shift+Tab), or F-keys (F1-F24)
        // Also valid: Space, Home, End, Tab, etc.
        // Simple heuristic: starts with uppercase letter or is a single char
        if (token.Length == 1)
        {
            return true;
        }

        // Modifier combos
        if (token.Contains ('+') || token.Contains ('-'))
        {
            return true;
        }

        // Named keys start with uppercase
        return char.IsUpper (token [0]);
    }
}

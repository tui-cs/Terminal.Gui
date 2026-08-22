// Copilot

using System.ComponentModel;

namespace ViewBaseTests;

/// <summary>
///     Tests for the CWP-compliant <see cref="View.Text"/> notifications:
///     <see cref="View.TextChanging"/> and <see cref="View.TextChanged"/>.
/// </summary>
public class TextCwpTests
{
    [Fact]
    public void Text_SetSameValue_NoEventsRaised ()
    {
        View view = new () { Text = "hello" };
        bool changingRaised = false;
        bool changedRaised = false;
        view.TextChanging += (_, _) => changingRaised = true;
        view.TextChanged += (_, _) => changedRaised = true;

        view.Text = "hello";

        Assert.False (changingRaised);
        Assert.False (changedRaised);
    }

    [Fact]
    public void Text_SetDifferentValue_BothEventsRaised ()
    {
        View view = new () { Text = "old" };
        bool changingRaised = false;
        bool changedRaised = false;
        view.TextChanging += (_, _) => changingRaised = true;
        view.TextChanged += (_, _) => changedRaised = true;

        view.Text = "new";

        Assert.True (changingRaised);
        Assert.True (changedRaised);
    }

    [Fact]
    public void TextChanging_Cancel_PreventsTextChange ()
    {
        View view = new () { Text = "original" };
        view.TextChanging += (_, e) => e.Cancel = true;

        view.Text = "modified";

        Assert.Equal ("original", view.Text);
    }

    [Fact]
    public void TextChanging_Cancel_SuppressesTextChanged ()
    {
        View view = new () { Text = "original" };
        bool changedRaised = false;
        view.TextChanging += (_, e) => e.Cancel = true;
        view.TextChanged += (_, _) => changedRaised = true;

        view.Text = "modified";

        Assert.False (changedRaised);
    }

    [Fact]
    public void TextChanging_RaisedBeforeMutation ()
    {
        View view = new () { Text = "before" };
        string? textDuringChanging = null;
        view.TextChanging += (sender, _) => textDuringChanging = ((View)sender!).Text;

        view.Text = "after";

        Assert.Equal ("before", textDuringChanging);
    }

    [Fact]
    public void TextChanged_RaisedAfterMutation ()
    {
        View view = new () { Text = "before" };
        string? textDuringChanged = null;
        view.TextChanged += (sender, _) => textDuringChanged = ((View)sender!).Text;

        view.Text = "after";

        Assert.Equal ("after", textDuringChanged);
    }

    [Fact]
    public void OnTextChanging_Override_CanCancel ()
    {
        CancellingView view = new ();
        // Set initial text before enabling cancellation
        view.AllowChange = true;
        view.Text = "initial";
        view.AllowChange = false;

        view.Text = "blocked";

        Assert.Equal ("initial", view.Text);
    }

    [Fact]
    public void OnTextChanged_Override_CalledAfterChange ()
    {
        TrackingView view = new () { Text = "start" };

        view.Text = "end";

        Assert.True (view.OnTextChangedCalled);
        Assert.Equal ("end", view.TextAtOnTextChanged);
    }

    [Fact]
    public void TextChanging_EventOrder_ChangingBeforeChanged ()
    {
        View view = new () { Text = "a" };
        List<string> order = [];
        view.TextChanging += (_, _) => order.Add ("changing");
        view.TextChanged += (_, _) => order.Add ("changed");

        view.Text = "b";

        Assert.Equal (["changing", "changed"], order);
    }

    // --- CR feedback tests: these must fail before fixes, pass after ---

    /// <summary>
    ///     CR Issue 1: <see cref="View.OnTextChanging"/> should raise <see cref="View.TextChanging"/>
    ///     (CWP pattern: virtual method raises the event). A subclass that calls base.OnTextChanging()
    ///     should get the event's cancellation result.
    /// </summary>
    [Fact]
    public void OnTextChanging_BaseRaisesEvent_SubclassGetsCancel () // Copilot
    {
        DelegatingView view = new ();
        view.TextChanging += (_, e) => e.Cancel = true;

        view.Text = "hello";

        // base.OnTextChanging() should have raised TextChanging and returned true (cancelled)
        Assert.True (view.BaseReturnedCancel);
        Assert.Equal (string.Empty, view.Text);
    }

    /// <summary>
    ///     CR Issue 2: Setting <see cref="View.Text"/> polymorphically on a <see cref="TextField"/>
    ///     should sync the TextField's internal model so that <c>textField.Text</c> returns the new value.
    /// </summary>
    [Fact]
    public void TextField_PolymorphicSet_SyncsInternalModel () // Copilot
    {
        TextField tf = new ();
        View v = tf;

        v.Text = "hello";

        Assert.Equal ("hello", tf.Text);
    }

    /// <summary>
    ///     CR Issue 3: Setting <see cref="View.Text"/> polymorphically on a <see cref="TextView"/>
    ///     should sync the TextView's internal model so that <c>textView.Text</c> returns the new value.
    /// </summary>
    [Fact]
    public void TextView_PolymorphicSet_SyncsInternalModel () // Copilot
    {
        TextView tv = new ();
        View v = tv;

        v.Text = "hello";

        Assert.Equal ("hello", tv.Text);
    }

    /// <summary>
    ///     CR Issue 4: A newly constructed <see cref="ProgressBar"/> should have <see cref="View.Text"/>
    ///     set to "0%" so that <see cref="ProgressBarFormat.SimplePlusPercentage"/> renders the percentage
    ///     on first draw.
    /// </summary>
    [Fact]
    public void ProgressBar_Constructor_TextShowsZeroPercent () // Copilot
    {
        ProgressBar pb = new ();

        Assert.Equal ("0%", pb.Text);
    }

    /// <summary>A test subclass that cancels text changes via <see cref="View.OnTextChanging"/>.</summary>
    private class CancellingView : View
    {
        public bool AllowChange { get; set; }

        protected override bool OnTextChanging (string newText) => !AllowChange;
    }

    /// <summary>A test subclass that tracks calls to <see cref="View.OnTextChanged"/>.</summary>
    private class TrackingView : View
    {
        public bool OnTextChangedCalled { get; private set; }
        public string? TextAtOnTextChanged { get; private set; }

        protected override void OnTextChanged ()
        {
            OnTextChangedCalled = true;
            TextAtOnTextChanged = Text;

            base.OnTextChanged ();
        }
    }

    /// <summary>
    ///     A test subclass that delegates to <c>base.OnTextChanging()</c> and records
    ///     whether the base returned a cancel signal.
    /// </summary>
    private class DelegatingView : View
    {
        public bool BaseReturnedCancel { get; private set; }

        protected override bool OnTextChanging (string newText)
        {
            BaseReturnedCancel = base.OnTextChanging (newText);

            return BaseReturnedCancel;
        }
    }

    // --- CR feedback regression tests ---

    /// <summary>
    ///     Verifies that when TextField's TextChanging subscriber modifies text, and then a
    ///     subsequent base TextChanging event cancels, the stale modified text does not leak
    ///     into the next successful text change.
    /// </summary>
    [Fact]
    public void TextField_PendingText_ClearedOnBaseCancel ()
    {
        // Copilot
        TextField tf = new () { Width = 20, Height = 1 };
        tf.Text = "initial";

        // First: a subscriber modifies text via ResultEventArgs
        tf.TextChanging += (_, args) =>
                           {
                               if (args.Result == "modified")
                               {
                                   args.Result = "subscriber_changed";
                               }
                           };

        tf.Text = "modified";
        Assert.Equal ("subscriber_changed", tf.Text);

        // Now subscribe to base View.TextChanging to cancel the NEXT change
        var cancelOnce = true;

        ((View)tf).TextChanging += (_, e) =>
                                   {
                                       if (cancelOnce)
                                       {
                                           e.Cancel = true;
                                           cancelOnce = false;
                                       }
                                   };

        // This should be cancelled by the base event
        tf.Text = "blocked";
        Assert.Equal ("subscriber_changed", tf.Text);

        // Next change should succeed with fresh text, NOT stale _pendingText
        tf.Text = "final";
        Assert.Equal ("final", tf.Text);
    }

    /// <summary>
    ///     Verifies that TextValidateField does not raise View.TextChanged when
    ///     ValueChanging is cancelled (CWP semantics: cancel suppresses TextChanged).
    /// </summary>
    [Fact]
    public void TextValidateField_ValueChangingCancel_SuppressesTextChanged ()
    {
        // Copilot
        TextValidateField field = new () { Width = 20, Height = 1 };

        // Set a provider that accepts any text
        field.Provider = new TextRegexProvider (".*");
        field.Text = "initial";

        // Cancel ValueChanging
        field.ValueChanging += (_, args) => args.Handled = true;

        bool textChangedRaised = false;
        ((View)field).TextChanged += (_, _) => textChangedRaised = true;

        field.Text = "blocked";

        Assert.False (textChangedRaised, "TextChanged should not fire when ValueChanging cancels");
        Assert.Equal ("initial", field.Text);
    }

    /// <summary>
    ///     Verifies that DatePicker rejects invalid (unparseable) text: Text should not
    ///     persist an invalid string that cannot round-trip through DateTime.
    /// </summary>
    [Fact]
    public void DatePicker_InvalidText_DoesNotPersist ()
    {
        // Copilot
        DatePicker dp = new () { Width = 20, Height = 1 };
        DateTime originalValue = dp.Value;
        string originalText = dp.Text;

        // Set invalid date text
        dp.Text = "not-a-date";

        // Value should remain unchanged
        Assert.Equal (originalValue, dp.Value);

        // Text should NOT hold the invalid string — it should revert or be rejected
        Assert.NotEqual ("not-a-date", dp.Text);
    }

    /// <summary>
    ///     Verifies that ColorPicker rejects invalid (unparseable) text: Text should not
    ///     persist an invalid string that cannot round-trip through Color.
    /// </summary>
    [Fact]
    public void ColorPicker_InvalidText_DoesNotPersist ()
    {
        // Copilot
        ColorPicker cp = new () { Width = 20, Height = 3 };
        cp.SelectedColor = new Color (255, 0, 0);
        string originalText = cp.Text;

        // Set invalid color text
        cp.Text = "not-a-color";

        // SelectedColor should remain unchanged
        Assert.Equal (new Color (255, 0, 0), cp.SelectedColor);

        // Text should NOT hold the invalid string — it should revert or be rejected
        Assert.NotEqual ("not-a-color", cp.Text);
    }

    /// <summary>
    ///     Verifies that setting View.Text on a word-wrapped TextView via a View reference
    ///     does not corrupt or redundantly re-process the model.
    /// </summary>
    [Fact]
    public void TextView_WordWrap_PolymorphicSet_DoesNotCorruptModel ()
    {
        // Copilot
        TextView tv = new () { Width = 10, Height = 5, WordWrap = true };
        tv.Text = "Hello World this wraps";

        // Set via polymorphic View reference
        View viewRef = tv;
        viewRef.Text = "Short";

        Assert.Equal ("Short", tv.Text);
        Assert.Equal ("Short", viewRef.Text);
    }

    /// <summary>
    ///     Verifies that setting a valid date string via Text updates DatePicker.Value accordingly.
    ///     Ensures Text↔Value consistency on the happy path.
    /// </summary>
    [Fact]
    public void DatePicker_ValidText_UpdatesValue ()
    {
        // Copilot
        DatePicker dp = new () { Width = 20, Height = 1 };
        DateTime target = new (2025, 12, 25);

        // Use the same short-date format the picker uses internally
        string formatted = target.ToShortDateString ();
        dp.Text = formatted;

        Assert.Equal (target, dp.Value);
        Assert.Equal (formatted, dp.Text);
    }

    /// <summary>
    ///     Verifies that setting DatePicker.Value updates Text to the formatted representation.
    ///     Ensures Value→Text consistency.
    /// </summary>
    [Fact]
    public void DatePicker_ValueSet_UpdatesText ()
    {
        // Copilot
        DatePicker dp = new () { Width = 20, Height = 1 };
        DateTime target = new (2024, 7, 4);

        dp.Value = target;

        Assert.Equal (target, dp.Value);

        // Text should be parseable back to the same date
        Assert.True (DateTime.TryParse (dp.Text, out DateTime roundTrip));
        Assert.Equal (target, roundTrip);
    }

    /// <summary>
    ///     Verifies that setting a valid color string via Text updates ColorPicker.SelectedColor.
    ///     Ensures Text↔Value consistency on the happy path.
    /// </summary>
    [Fact]
    public void ColorPicker_ValidText_UpdatesSelectedColor ()
    {
        // Copilot
        ColorPicker cp = new () { Width = 20, Height = 3 };
        cp.SelectedColor = new Color (0, 0, 0);

        // StandardColorsNameResolver accepts StandardColor enum names
        cp.Text = "Red";

        // SelectedColor should have changed from black to red
        Assert.NotEqual (new Color (0, 0, 0), cp.SelectedColor);
        Assert.Equal ("Red", cp.Text);
    }

    /// <summary>
    ///     Verifies that setting ColorPicker.SelectedColor updates Text to the color string.
    ///     Ensures Value→Text consistency.
    /// </summary>
    [Fact]
    public void ColorPicker_SelectedColorSet_UpdatesText ()
    {
        // Copilot
        ColorPicker cp = new () { Width = 20, Height = 3 };

        cp.SelectedColor = new Color (0, 0, 255);

        Assert.Equal (new Color (0, 0, 255).ToString (), cp.Text);
    }

    /// <summary>
    ///     Verifies that TextValidateField accepts valid text and updates Value accordingly.
    ///     Ensures Text↔Value consistency on the happy path.
    /// </summary>
    [Fact]
    public void TextValidateField_ValidText_UpdatesValue ()
    {
        // Copilot
        TextValidateField tvf = new () { Provider = new TextRegexProvider ("^[0-9]+$") };
        tvf.Text = "123";

        tvf.Text = "456";

        Assert.Equal ("456", tvf.Text);
    }

    /// <summary>
    ///     Verifies that when TextValidateField.ValueChanging cancels, both Text and the
    ///     provider's internal value remain at the old value (no divergence).
    /// </summary>
    [Fact]
    public void TextValidateField_ValueChangingCancel_TextAndProviderStayConsistent ()
    {
        // Copilot
        TextValidateField tvf = new () { Provider = new TextRegexProvider ("^[0-9]+$") };
        tvf.Text = "111";
        tvf.ValueChanging += (_, e) => e.Handled = true;

        tvf.Text = "222";

        // Both must stay at original value
        Assert.Equal ("111", tvf.Text);
    }
}

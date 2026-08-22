#nullable enable
using System.Text;
using System.Text.RegularExpressions;
using Terminal.Gui.Editor;
// ReSharper disable AccessToDisposedClosure

// ReSharper disable AccessToModifiedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Text Input Controls", "Tests all text input controls")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Mouse and Keyboard")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("DateTime")]
public class TextInputControls : Scenario
{
    private Label? _labelMirroringTimeEditor;
    private TimeEditor? _timeEditor;

    public override void Main ()
    {

        // Init
        using IApplication app = Application.Create ();
        app.Init ();

        // Setup - Create a top-level application window and configure it.
        using Window win = new ();
        win.Title = GetQuitKeyAndName ();

        // TextField is a simple, single-line text input control
        Label label = new () { Text = " _TextField:" };
        win.Add (label);

        TextField textField = new ()
        {
            X = Pos.Right (label) + 1, Y = 0, Width = Dim.Percent (50) - 1, Text = "TextField with test text. Unicode shouldn't ??AR??!"
        };

        SingleWordSuggestionGenerator textFieldWordGenerator = new ();
        textField.Autocomplete?.SuggestionGenerator = textFieldWordGenerator;
        textField.TextChanging += TextFieldTextChanging;

        win.Add (textField);

        View labelMirroringTextField = new ()
        {
            X = Pos.Right (textField) + 1,
            Y = Pos.Top (textField),
            Width = Dim.Fill (1) - 1,
            Height = 1,
            Text = textField.Text,
            Enabled = false
        };
        win.Add (labelMirroringTextField);
        textField.TextChanged += (sender, _) => { labelMirroringTextField.Text = ((TextField)sender!).Text; };

        label = new Label { Text = "Te_xtField2:", X = 0, Y = Pos.Bottom (textField) + 1 };
        win.Add (label);

        TextField textField2 = new ()
        {
            X = Pos.Right (label) + 1, Y = Pos.Top (label), Width = Dim.Percent (50) - 1, Title = "TextField with caption and AppendAutoComplete"
        };
        textField2.Autocomplete = new AppendAutocomplete (textField2);
        SingleWordSuggestionGenerator textField2WordGenerator = new ();
        textField2.Autocomplete.SuggestionGenerator = textField2WordGenerator;
        textField2.TextChanging += AppendTextFieldTextChanging;

        win.Add (textField2);

        // Editor is a rope-backed multi-line text editor from Terminal.Gui.Editor
        label = new Label { Text = "    E_ditor:", Y = Pos.Bottom (label) + 1 };
        win.Add (label);

        Editor editor = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Percent (50) - 1,
            Height = Dim.Percent (10),
            ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar | ViewportSettingsFlags.HasHorizontalScrollBar
        };

        // TODO: Editor does not yet expose an autocomplete API. Add when available.

        win.Add (editor);

        Label labelMirroringEditorView = new ()
        {
            X = Pos.Right (editor) + 1,
            Y = Pos.Top (editor),
            Width = Dim.Fill (1) - 1,
            Height = Dim.Height (editor) - 1,
            Enabled = false
        };
        win.Add (labelMirroringEditorView);

        // Use DocumentChanged to detect if the user has typed something in Editor.
        editor.Document!.Changed += (_, _) => { labelMirroringEditorView.Text = editor.Text; };
        editor.Text = "Editor with some more test text. Unicode shouldn't ??AR??!";

        CheckBox chxReadOnly = new ()
        {
            X = Pos.Left (editor), Y = Pos.Bottom (editor), Value = editor.ReadOnly ? CheckState.Checked : CheckState.UnChecked, Text = "Read_Only"
        };
        chxReadOnly.ValueChanging += (_, args) => editor.ReadOnly = args.NewValue == CheckState.Checked;
        win.Add (chxReadOnly);

        CheckBox chxWordWrap = new ()
        {
            X = Pos.Right (chxReadOnly) + 2,
            Y = Pos.Top (chxReadOnly),
            Value = editor.WordWrap ? CheckState.Checked : CheckState.UnChecked,
            Text = "_Word Wrap"
        };
        chxWordWrap.ValueChanging += (_, e) => editor.WordWrap = e.NewValue == CheckState.Checked;
        win.Add (chxWordWrap);

        CheckBox scrollBars = new ()
        {
            X = Pos.Right (chxWordWrap) + 2,
            Y = Pos.Top (chxWordWrap),
            Title = "_ScrollBars",
            Value = CheckState.Checked
        };

        scrollBars.ValueChanged += (_, _) =>
                                   {
                                       if (scrollBars.Value == CheckState.Checked)
                                       {
                                           editor.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar | ViewportSettingsFlags.HasHorizontalScrollBar;
                                       }
                                       else
                                       {
                                           editor.ViewportSettings &= ~(ViewportSettingsFlags.HasVerticalScrollBar | ViewportSettingsFlags.HasHorizontalScrollBar);
                                       }
                                   };

        win.Add (scrollBars);

        // Hex editor
        label = new Label { Text = "   _HexView:", Y = Pos.Bottom (scrollBars) + 1 };
        win.Add (label);

        HexView hexEditor = new (new MemoryStream (Encoding.UTF8.GetBytes ("HexEditor Unicode that shouldn't ??AR??!")))
        {
            X = Pos.Right (label) + 1, Y = Pos.Top (label), Width = Dim.Percent (50) - 1, Height = Dim.Percent (30)
        };
        win.Add (hexEditor);

        Label labelMirroringHexEditor = new ()
        {
            X = Pos.Right (hexEditor) + 1, Y = Pos.Top (hexEditor), Width = Dim.Fill (1) - 1, Height = Dim.Height (hexEditor) - 1
        };
        byte [] array = ((MemoryStream)hexEditor.Source!).ToArray ();
        labelMirroringHexEditor.Text = Encoding.UTF8.GetString (array, 0, array.Length);

        hexEditor.Edited += (_, _) =>
                            {
                                hexEditor.ApplyEdits ();
                                byte [] arr = ((MemoryStream)hexEditor.Source!).ToArray ();
                                labelMirroringHexEditor.Text = Encoding.UTF8.GetString (arr, 0, arr.Length);
                            };
        win.Add (labelMirroringHexEditor);

        // DateEditor
        label = new Label { Text = "_DateEditor:", Y = Pos.Bottom (hexEditor) + 1 };
        win.Add (label);

        DateEditor dateEditor = new () { X = Pos.Right (label) + 1, Y = Pos.Bottom (hexEditor) + 1, Value = DateTime.Today };
        win.Add (dateEditor);

        Label labelMirroringDateEditor = new ()
        {
            X = Pos.Right (dateEditor) + 1,
            Y = Pos.Top (dateEditor),
            Width = Dim.Width (dateEditor),
            Height = Dim.Height (dateEditor),
            Text = dateEditor.Text
        };
        win.Add (labelMirroringDateEditor);

        dateEditor.ValueChanged += (_, _) => { labelMirroringDateEditor.Text = dateEditor.Text; };

        // TimeEditor
        label = new Label { Text = "T_imeEditor:", Y = Pos.Top (dateEditor), X = Pos.Right (labelMirroringDateEditor) + 5 };
        win.Add (label);

        _timeEditor = new TimeEditor { X = Pos.Right (label) + 1, Y = Pos.Top (dateEditor), Value = DateTime.Now.TimeOfDay };
        win.Add (_timeEditor);

        _labelMirroringTimeEditor = new Label
        {
            X = Pos.Right (_timeEditor) + 1,
            Y = Pos.Top (_timeEditor),
            Width = Dim.Width (_timeEditor),
            Height = Dim.Height (_timeEditor),
            Text = _timeEditor.Text
        };
        win.Add (_labelMirroringTimeEditor);

        _timeEditor.ValueChanged += TimeChanged;

        // MaskedTextProvider - uses .NET MaskedTextProvider
        NetMaskedTextProvider netProvider = new ("+99 (000) 000-0000");
        Label netProviderLabel = new () { X = Pos.Left (dateEditor), Y = Pos.Bottom (dateEditor) + 1, Text = $"_NetMaskedTextProvider ({netProvider.Mask}):" };
        win.Add (netProviderLabel);

        TextValidateField netProviderField = new () { X = Pos.Right (netProviderLabel) + 1, Y = Pos.Y (netProviderLabel), Provider = netProvider };
        win.Add (netProviderField);

        Label labelMirroringNetProviderField = new ()
        {
            X = Pos.Right (netProviderField) + 1,
            Y = Pos.Top (netProviderField),
            Width = Dim.Width (netProviderField),
            Height = Dim.Height (netProviderField),
            Text = netProviderField.Text
        };
        win.Add (labelMirroringNetProviderField);

        netProviderField.Provider.TextChanged += (_, _) => { labelMirroringNetProviderField.Text = netProviderField.Text; };

        // TextRegexProvider - Regex provider implemented by Terminal.Gui
        TextRegexProvider provider2 = new ("^([0-9]?[0-9]?[0-9]|1000)$") { ValidateOnInput = false };

        Label regexProviderLabel = new ()
        {
            X = Pos.Left (netProviderLabel), Y = Pos.Bottom (netProviderLabel) + 1, Text = $"Text_RegexProvider ({provider2.Pattern}):"
        };
        win.Add (regexProviderLabel);

        TextValidateField regexProviderField = new ()
        {
            X = Pos.Right (regexProviderLabel) + 1,
            Y = Pos.Y (regexProviderLabel),
            Width = 30,
            TextAlignment = Alignment.Center,
            Provider = provider2
        };
        win.Add (regexProviderField);

        Label labelMirroringRegexProviderField = new ()
        {
            X = Pos.Right (regexProviderField) + 1,
            Y = Pos.Top (regexProviderField),
            Width = Dim.Width (regexProviderField),
            Height = Dim.Height (regexProviderField),
            Text = regexProviderField.Text
        };
        win.Add (labelMirroringRegexProviderField);

        regexProviderField.Provider.TextChanged += (_, _) => { labelMirroringRegexProviderField.Text = regexProviderField.Text; };

        Label labelAppendAutocomplete = new () { Y = Pos.Y (regexProviderLabel) + 2, X = 1, Text = "_Append Autocomplete:" };

        TextField appendAutocompleteTextField = new ()
        {
            X = Pos.Right (labelAppendAutocomplete) + 1, Y = Pos.Top (labelAppendAutocomplete), Width = Dim.Fill ()
        };
        appendAutocompleteTextField.Autocomplete = new AppendAutocomplete (appendAutocompleteTextField);

        appendAutocompleteTextField.Autocomplete.SuggestionGenerator = new SingleWordSuggestionGenerator
        {
            AllSuggestions =
            [
                "fish",
                "flipper",
                "fin",
                "fun",
                "the",
                "at",
                "there",
                "some",
                "my",
                "of",
                "be",
                "use",
                "her",
                "than",
                "and",
                "this",
                "an",
                "would",
                "first",
                "have",
                "each",
                "make",
                "water",
                "to",
                "from",
                "which",
                "like",
                "been",
                "in",
                "or",
                "she",
                "him",
                "call",
                "is",
                "one",
                "do",
                "into",
                "who",
                "you",
                "had",
                "how",
                "time",
                "oil",
                "that",
                "by",
                "their",
                "has",
                "its",
                "it",
                "word",
                "if",
                "look",
                "now",
                "he",
                "but",
                "will",
                "two",
                "find",
                "was",
                "not",
                "up",
                "more",
                "long",
                "for",
                "what",
                "other",
                "write",
                "down",
                "on",
                "all",
                "about",
                "go",
                "day",
                "are",
                "were",
                "out",
                "see",
                "did",
                "as",
                "we",
                "many",
                "number",
                "get",
                "with",
                "when",
                "then",
                "no",
                "come",
                "his",
                "your",
                "them",
                "way",
                "made",
                "they",
                "can",
                "these",
                "could",
                "may",
                "said",
                "so",
                "people",
                "part"
            ]
        };

        win.Add (labelAppendAutocomplete);
        win.Add (appendAutocompleteTextField);

        Label acceptView = new () { X = Pos.Center (), Y = Pos.AnchorEnd () };

        win.Add (acceptView);

        win.Accepting += WinOnAccept;

        ThemeChanges.ThemeChanged += ConfigurationManagerOnApplied;

        app.Run (win);

        return;

        void TextFieldTextChanging (object? sender, ResultEventArgs<string> e) =>
            textFieldWordGenerator.AllSuggestions = Regex.Matches (e.Result!, "\\w+").Select (s => s.Value).Distinct ().ToList ();

        void AppendTextFieldTextChanging (object? sender, ResultEventArgs<string> e) =>
            textField2WordGenerator.AllSuggestions = Regex.Matches (e.Result!, "\\w+").Select (s => s.Value).Distinct ().ToList ();

        void WinOnAccept (object? sender, CommandEventArgs e)
        {
            e.Handled = true; // Don't let it close

            acceptView.Text = $"Accept was Invoked via {win.Focused?.GetType ().Name}";

            // Start a task that will set acceptView.Text to an empty string after 1 second
            Task.Run (async () =>
                      {
                          await Task.Delay (1000);
                          app.Invoke (() => acceptView.Text = "");
                      });
        }

        void ConfigurationManagerOnApplied (object? sender, EventArgs<string> e) => win.SetNeedsDraw ();
    }

    private void TimeChanged (object? sender, ValueChangedEventArgs<TimeSpan> e)
    {
        if (_labelMirroringTimeEditor is { } && _timeEditor is { })
        {
            _labelMirroringTimeEditor.Text = _timeEditor.Text;
        }
    }
}

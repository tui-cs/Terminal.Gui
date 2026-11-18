using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Keys", "Shows keyboard input handling.")]
[ScenarioCategory ("Mouse and Keyboard")]
public class Keys : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();
        ObservableCollection<string> keyDownList = [];
        ObservableCollection<string> keyDownNotHandledList = [];
        ObservableCollection<string> swallowedList = [];

        using Window win = new ();
        win.Title = GetQuitKeyAndName ();
        win.BorderStyle = LineStyle.None;

        Shortcut quitShortcut = new ()
        {
            // ReSharper disable once AccessToDisposedClosure
            Title = "Quit", Key = Application.GetDefaultKey (Command.Quit), BindKeyToApplication = true, Action = app.RequestStop
        };

        StatusBar statusBar = new ([quitShortcut, new Shortcut { Title = "Disable QuitKey", Action = () => { quitShortcut.Key = Key.Empty; } }]);

        app.AddTimeout (TimeSpan.FromMilliseconds (100),
                        () =>
                        {
                            // When the App is initialized, kitty detection is async, so we
                            // create the shortcut in a timeout.
                            statusBar.Add (new Shortcut
                            {
                                CommandView = new CheckBox
                                {
                                    Title = "Kitty Keyboard Protocol Enabled",

                                    // ReSharper disable once AccessToDisposedClosure
                                    Value = app.Driver?.KittyKeyboardCapabilities?.IsSupported is true ? CheckState.Checked : CheckState.UnChecked
                                },
                                Enabled = false
                            });

                            return false;
                        });

        Label label = new () { X = 0, Y = 0, Text = "_Type text here:" };
        win.Add (label);

        TextField edit = new () { X = Pos.Right (label) + 1, Y = Pos.Top (label), Width = Dim.Fill (2), Height = 1 };
        win.Add (edit);

        label = new Label { X = 0, Y = Pos.Bottom (label), Text = "Last _app.Keyboard.KeyDown:" };
        win.Add (label);

        Label labelAppKeypress = new () { X = Pos.Right (label) + 1, Y = Pos.Top (label), Width = Dim.Fill (2) };
        win.Add (labelAppKeypress);

        app.Keyboard.KeyDown += (_, e) => labelAppKeypress.Text = FormatKeyEvent (e);

        label = new Label { X = 0, Y = Pos.Bottom (label), Text = "Last app.Keyboard._KeyUp:" };
        win.Add (label);

        Label labelAppKeyUp = new () { X = Pos.Right (label) + 1, Y = Pos.Top (label), Width = Dim.Fill (2) };
        win.Add (labelAppKeyUp);

        app.Keyboard.KeyUp += (_, e) => labelAppKeyUp.Text = FormatKeyEvent (e);

        label = new Label { X = 0, Y = Pos.Bottom (label), Text = "_Last TextField.KeyDown:" };
        win.Add (label);

        Label lastTextFieldKeyDownLabel = new () { X = Pos.Right (label) + 1, Y = Pos.Top (label), Height = 1, Width = Dim.Fill (2) };
        win.Add (lastTextFieldKeyDownLabel);

        edit.KeyDown += (_, e) => lastTextFieldKeyDownLabel.Text = FormatKeyEvent (e);

        // Application key event log:
        label = new Label { X = 0, Y = Pos.Bottom (label) + 1, Text = "Application Key Events:" };
        win.Add (label);
        int maxKeyString = Key.CursorRight.WithAlt.WithCtrl.WithShift.ToString ().Length;
        int colWidth = maxKeyString + 12; // room for event type + modifier info

        ObservableCollection<string> keyList = [];

        ListView appKeyListView = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = colWidth,
            Height = Dim.Fill (statusBar),
            Source = new ListWrapper<string> (keyList)
        };
        appKeyListView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent);
        win.Add (appKeyListView);

        // View key events...
        edit.KeyDown += (_, a) => { keyDownList.Add (a.ToString ()); };

        edit.KeyDownNotHandled += (_, a) => { keyDownNotHandledList.Add ($"{a}"); };

        // KeyDown
        label = new Label { X = Pos.Right (appKeyListView) + 1, Y = Pos.Top (label), Text = "Editor Key Down:" };
        win.Add (label);

        ListView onKeyDownListView = new ()
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = maxKeyString,
            Height = Dim.Fill (statusBar),
            Source = new ListWrapper<string> (keyDownList)
        };
        appKeyListView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent);
        win.Add (onKeyDownListView);

        // KeyDownNotHandled
        label = new Label { X = Pos.Right (onKeyDownListView) + 1, Y = Pos.Top (label), Text = "Editor KeyDownNotHandled:" };
        win.Add (label);

        ListView onKeyDownNotHandledListView = new ()
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = maxKeyString,
            Height = Dim.Fill (statusBar),
            Source = new ListWrapper<string> (keyDownNotHandledList)
        };
        appKeyListView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent);
        win.Add (onKeyDownNotHandledListView);

        // Swallowed
        label = new Label { X = Pos.Right (onKeyDownNotHandledListView) + 1, Y = Pos.Top (label), Text = "Swallowed:" };
        win.Add (label);

        ListView onSwallowedListView = new ()
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = maxKeyString,
            Height = Dim.Fill (statusBar),
            Source = new ListWrapper<string> (swallowedList)
        };
        appKeyListView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent);
        win.Add (onSwallowedListView);

        app.Driver!.GetInputProcessor ().AnsiSequenceSwallowed += (_, e) => { swallowedList.Add (e.Replace ("\x1b", "Esc")); };

        app.Keyboard.KeyDown += (_, a) => KeyDownPressUp (a, "Down");
        app.Keyboard.KeyUp += (_, a) => KeyDownPressUp (a, "Up");

        win.Add (statusBar);
        app.Run (win);

        return;

        void KeyDownPressUp (Key args, string upDown)
        {
            var msg = $"Key{upDown,-7}: {FormatKeyEvent (args)}";
            keyList.Add (msg);
            appKeyListView.MoveDown ();
            onKeyDownNotHandledListView.MoveDown ();
        }
    }

    /// <summary>
    ///     Formats a <see cref="Key"/> event with Phase 2 metadata: event type and modifier key identity.
    /// </summary>
    private static string FormatKeyEvent (Key key)
    {
        string eventType = key.EventType switch
                           {
                               KeyEventType.Press => "press",
                               KeyEventType.Repeat => "repeat",
                               KeyEventType.Release => "release",
                               _ => "?"
                           };

        var text = $"{key} ({eventType})";

        if (key.IsModifierOnly)
        {
            text += $" [{key.ModifierKey}]";
        }

        return text;
    }
}

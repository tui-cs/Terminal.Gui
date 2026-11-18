#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("HotKeys", "Demonstrates how HotKeys work.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Mouse and Keyboard")]
public class HotKeys : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window mainWindow = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        Label editorLabel = new ()
        {
            Text = "_Editor:",
            X = 0,
            Y = 0
        };
        mainWindow.Add (editorLabel);

        TextField textField = new ()
        {
            X = Pos.Right (editorLabel) + 1,
            Y = 0,
            Width = 10
        };
        mainWindow.Add (textField);

        Label viewLabel = new ()
        {
            Text = "_View:",
            X = 0,
            Y = Pos.Bottom (textField) + 1
        };
        mainWindow.Add (viewLabel);

        View focusableView = new ()
        {
            Title = "View (_focusable)",
            Text = "Text renders _Underscore",
            CanFocus = true,
            X = Pos.Right (viewLabel) + 1,
            Y = Pos.Top (viewLabel),
            Width = 30,
            Height = 3,
            BorderStyle = LineStyle.Dashed
        };
        mainWindow.Add (focusableView);

        viewLabel = new ()
        {
            Text = "Vi_ew:",
            X = 0,
            Y = Pos.Bottom (focusableView) + 1
        };
        mainWindow.Add (viewLabel);

        View nonFocusableView = new ()
        {
            Title = "View (n_ot focusable)",
            Text = "Text renders _Underscore",
            X = Pos.Right (viewLabel) + 1,
            Y = Pos.Top (viewLabel),
            Width = 30,
            Height = 3,
            BorderStyle = LineStyle.Dashed
        };
        mainWindow.Add (nonFocusableView);

        Label labelWithFrameLabel = new ()
        {
            Text = "_Label with Frame:",
            X = 0,
            Y = Pos.Bottom (nonFocusableView) + 1
        };
        mainWindow.Add (labelWithFrameLabel);

        Label labelWithFrameFocusable = new ()
        {
            Title = "Label _with Frame (focusable)",
            CanFocus = true,
            X = Pos.Right (labelWithFrameLabel) + 1,
            Y = Pos.Top (labelWithFrameLabel),
            Width = 40,
            Height = 3,
            BorderStyle = LineStyle.Dashed
        };
        mainWindow.Add (labelWithFrameFocusable);

        labelWithFrameLabel = new ()
        {
            Text = "L_abel with Frame:",
            X = 0,
            Y = Pos.Bottom (labelWithFrameFocusable) + 1
        };
        mainWindow.Add (labelWithFrameLabel);

        Label labelWithFrame = new ()
        {
            Title = "Label with Frame (_not focusable)",
            X = Pos.Right (labelWithFrameLabel) + 1,
            Y = Pos.Top (labelWithFrameLabel),
            Width = 40,
            Height = 3,
            BorderStyle = LineStyle.Dashed
        };
        mainWindow.Add (labelWithFrame);

        Label buttonWithFrameLabel = new ()
        {
            Text = "_Button with Frame:",
            X = 0,
            Y = Pos.Bottom (labelWithFrame) + 1
        };
        mainWindow.Add (buttonWithFrameLabel);

        Button buttonWithFrameFocusable = new ()
        {
            Title = "B_utton with Frame (focusable)",
            CanFocus = true,
            X = Pos.Right (buttonWithFrameLabel) + 1,
            Y = Pos.Top (buttonWithFrameLabel),
            Width = 40,
            BorderStyle = LineStyle.Dashed
        };
        mainWindow.Add (buttonWithFrameFocusable);

        buttonWithFrameLabel = new ()
        {
            Text = "Butt_on with Frame:",
            X = 0,
            Y = Pos.Bottom (buttonWithFrameFocusable) + 1
        };
        mainWindow.Add (buttonWithFrameLabel);

        Button buttonWithFrame = new ()
        {
            Title = "Button with Frame (not focusab_le)",
            X = Pos.Right (buttonWithFrameLabel) + 1,
            Y = Pos.Top (buttonWithFrameLabel),
            Width = 40,
            CanFocus = false,
            BorderStyle = LineStyle.Dashed
        };
        mainWindow.Add (buttonWithFrame);

        Label checkboxWithFrameLabel = new ()
        {
            Text = "_Checkbox with Frame:",
            X = 0,
            Y = Pos.Bottom (buttonWithFrame) + 1
        };
        mainWindow.Add (checkboxWithFrameLabel);

        CheckBox checkboxWithFrameFocusable = new ()
        {
            Title = "C_heckbox with Frame (focusable)",
            CanFocus = true,
            X = Pos.Right (checkboxWithFrameLabel) + 1,
            Y = Pos.Top (checkboxWithFrameLabel),
            Width = 40,
            Height = 3,
            BorderStyle = LineStyle.Dashed
        };
        mainWindow.Add (checkboxWithFrameFocusable);

        checkboxWithFrameLabel = new ()
        {
            Text = "Checkb_ox with Frame:",
            X = 0,
            Y = Pos.Bottom (checkboxWithFrameFocusable) + 1
        };
        mainWindow.Add (checkboxWithFrameLabel);

        CheckBox checkboxWithFrame = new ()
        {
            Title = "Checkbox with Frame (not focusable)",
            X = Pos.Right (checkboxWithFrameLabel) + 1,
            Y = Pos.Top (checkboxWithFrameLabel),
            Width = 40,
            Height = 3,
            CanFocus = false,
            BorderStyle = LineStyle.Dashed
        };
        mainWindow.Add (checkboxWithFrame);

        // Demonstrate automatic hotkey assignment (Issue #4145)
        FrameView autoHotKeyFrame = new ()
        {
            Title = "Auto HotKey Assignment",
            X = Pos.Right (checkboxWithFrame) + 2,
            Y = 0,
            Width = 35,
            Height = 12,
            AssignHotKeys = true
        };

        // These buttons have no manual hotkey specifiers - they will be assigned automatically
        Button saveButton = new () { Title = "Save", X = 1, Y = 0 };
        Button sendButton = new () { Title = "Send", X = 1, Y = 1 };
        Button submitButton = new () { Title = "Submit", X = 1, Y = 2 };
        Button cancelButton = new () { Title = "Cancel", X = 1, Y = 3 };
        Button closeButton = new () { Title = "Close", X = 1, Y = 4 };

        autoHotKeyFrame.Add (saveButton, sendButton, submitButton, cancelButton, closeButton);

        // This one has a manual hotkey that will be preserved
        Button quitButton = new () { Title = "_Quit (manual)", X = 1, Y = 6 };
        autoHotKeyFrame.Add (quitButton);

        // Show the UsedHotKeys
        Label usedKeysLabel = new ()
        {
            X = 1,
            Y = 8,
            Text = $"Used: {string.Join (", ", autoHotKeyFrame.UsedHotKeys.Select (k => k.ToString ()))}"
        };
        autoHotKeyFrame.Add (usedKeysLabel);

        mainWindow.Add (autoHotKeyFrame);

        Button pressButton = new ()
        {
            X = Pos.Center (),
            Y = Pos.AnchorEnd (),
            Text = "_Press me!"
        };
        mainWindow.Add (pressButton);

        app.Run (mainWindow);
    }
}

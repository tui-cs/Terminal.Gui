using Terminal.Gui.Editor;
// ReSharper disable AccessToDisposedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("MessageBoxes", "Demonstrates how to use the MessageBox class.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
public class MessageBoxes : Scenario
{
    public override List<Key> GetDemoKeyStrokes (IApplication app)
    {
        List<Key> keys = [];

        keys.Add (Key.S.WithAlt);
        keys.Add (Key.Esc);

        keys.Add (Key.E.WithAlt);
        keys.Add (Key.S.WithAlt);
        keys.Add (Key.Esc);

        keys.Add (Key.N.WithAlt);
        keys.Add (Key.D5);
        keys.Add (Key.S.WithAlt);
        keys.Add (Key.Enter);

        keys.Add (Key.T.WithAlt);
        keys.Add (Key.T.WithCtrl);
        keys.AddRange (GetKeysFromText ("This is a really long title"));
        keys.Add (Key.M.WithAlt);
        keys.Add (Key.T.WithCtrl);
        keys.AddRange (GetKeysFromText ("This is a long,\nmulti-line message.\nThis is a test of the emergency\nbroadcast\nsystem."));
        keys.Add (Key.S.WithAlt);

        for (var i = 0; i < 10; i++)
        {
            keys.Add (Key.Tab);
        }

        keys.Add (Key.Enter);

        keys.Add (Key.W.WithAlt);
        keys.Add (Key.S.WithAlt);

        for (var i = 0; i < 10; i++)
        {
            keys.Add (Key.Tab);
        }

        keys.Add (Key.Enter);

        return keys;
    }

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new ();
        window.Title = GetQuitKeyAndName ();

        FrameView frame = new ()
        {
            X = Pos.Center (),
            Y = 1,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Title = "MessageBox Options"
        };
        window.Add (frame);

        Label label = new ()
        {
            X = 0,
            Y = 0,
            Height = 1,
            Width = 15,
            TextAlignment = Alignment.End,
            Text = "_Title:"
        };
        frame.Add (label);

        TextField titleEdit = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (0, 50),
            Height = 1,
            Text = "The title"
        };
        frame.Add (titleEdit);

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Message:"
        };
        frame.Add (label);

        Editor messageEdit = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (0, 50),
            Height = 5,
            WordWrap = true
        };
        messageEdit.Text = "Message line 1.\nMessage line two. This is a really long line to force wordwrap. It needs to be long for it to work.";
        frame.Add (messageEdit);

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (messageEdit),
            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Num Buttons:"
        };
        frame.Add (label);

        TextField numButtonsEdit = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "3"
        };
        frame.Add (numButtonsEdit);

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "St_yle:"
        };
        frame.Add (label);

        OptionSelector styleOptionSelector = new () { X = Pos.Right (label) + 1, Y = Pos.Top (label), Labels = ["_Query", "_Error"], Title = "Sty_le" };
        frame.Add (styleOptionSelector);

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (styleOptionSelector),
            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "Wra_p:"
        };

        CheckBox ckbWrapMessage = new ()
        {
            X = Pos.Right (label) + 1, Y = Pos.Bottom (styleOptionSelector), Value = CheckState.Checked, Text = "_Wrap Message"
        };
        frame.Add (label, ckbWrapMessage);

        frame.ValidatePosDim = true;

        label = new Label { X = Pos.Center (), Y = Pos.Bottom (frame) + 2, TextAlignment = Alignment.End, Text = "Button Pressed:" };
        window.Add (label);

        Label buttonPressedLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (label) + 1,
            SchemeName = "Error",
            TextAlignment = Alignment.Center,
            Text = " "
        };

        Button showMessageBoxButton = new () { X = Pos.Center (), Y = Pos.Bottom (frame) + 2, IsDefault = true, Text = "_Show MessageBox" };

        window.CommandsToBubbleUp = [Command.Accept];
        frame.CommandsToBubbleUp = [Command.Accept];

        window.Accepting += (_, e) =>
                            {
                                try
                                {
                                    int numButtons = int.Parse (numButtonsEdit.Text);

                                    List<string> messageBoxButtons = [];

                                    for (var i = 0; i < numButtons; i++)
                                    {
                                        messageBoxButtons.Add ($"_{NumberToWords.Convert (i)}");
                                    }

                                    if (styleOptionSelector.Value == 0)
                                    {
                                        buttonPressedLabel.Text =
                                            $"{
                                                MessageBox.Query (app,
                                                                  titleEdit.Text,
                                                                  messageEdit.Text,
                                                                  ckbWrapMessage.Value == CheckState.Checked,
                                                                  messageBoxButtons.ToArray ())
                                            }";
                                    }
                                    else
                                    {
                                        buttonPressedLabel.Text =
                                            $"{
                                                MessageBox.ErrorQuery (app,
                                                                       titleEdit.Text,
                                                                       messageEdit.Text,
                                                                       ckbWrapMessage.Value == CheckState.Checked,
                                                                       messageBoxButtons.ToArray ())
                                            }";
                                    }
                                }
                                catch (FormatException)
                                {
                                    buttonPressedLabel.Text = "Invalid Options";
                                }

                                e.Handled = true;
                            };
        window.Add (showMessageBoxButton);

        window.Add (buttonPressedLabel);

        app.Run (window);
    }

    private List<Key> GetKeysFromText (string text)
    {
        List<Key> keys = [];

        foreach (char r in text)
        {
            keys.Add (r);
        }

        return keys;
    }
}

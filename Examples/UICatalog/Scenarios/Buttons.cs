#nullable enable
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Buttons", "Demonstrates all sorts of Buttons.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Layout")]
public class Buttons : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window main = new ();
        main.Title = GetQuitKeyAndName ();

        // Add a label & text field so we can demo IsDefault
        Label editLabel = new () { X = 0, Y = 0, Text = "TextField (to demo IsDefault):" };
        main.Add (editLabel);

        // Add a TextField using Absolute layout.
        TextField edit = new () { X = 31, Width = 15, HotKey = Key.Y.WithAlt };
        main.Add (edit);

        // This is the default button (IsDefault = true); if user presses ENTER in the TextField
        // the scenario will quit
        Button defaultButton = new () { X = Pos.Center (), Y = Pos.AnchorEnd (), IsDefault = true, Text = Strings.cmdQuit };

        main.Add (defaultButton);

        // Note we handle Accept on main, not defaultButton
        main.Accepting += (s, _) => (s as View)?.App!.RequestStop ();

        Button swapButton = new ()
        {
            X = 50,
            Width = 45,
            Height = 3,
            Text = "S_wap Default (Size = 45, 3)",
            SchemeName = "Error"
        };

        swapButton.Accepting += (_, e) =>
                                {
                                    e.Handled = !swapButton.IsDefault;
                                    defaultButton.IsDefault = !defaultButton.IsDefault;
                                    swapButton.IsDefault = !swapButton.IsDefault;
                                };

        defaultButton.Accepting += (s, e) =>
                                   {
                                       e.Handled = !defaultButton.IsDefault;

                                       if (e.Handled)
                                       {
                                           MessageBox.ErrorQuery (
                                                                  (s as View)?.App!,
                                                                  "Error",
                                                                  "This button is no longer the Quit button; the Swap Default button is.",
                                                                  Strings.btnOk);
                                       }
                                   };
        main.Add (swapButton);

        Label colorButtonsLabel = new () { X = 0, Y = Pos.Bottom (swapButton) + 1, Text = "Color Buttons: " };
        main.Add (colorButtonsLabel);

        View prev = colorButtonsLabel;

        foreach (KeyValuePair<string, Scheme?> scheme in SchemeManager.GetSchemesForCurrentTheme ())
        {
            Button colorButton = new ()
            {
                X = Pos.Right (prev),
                Y = Pos.Y (colorButtonsLabel),
                Text = $"_{scheme.Key}",
                SchemeName = scheme.Key
            };
            DoMessage (colorButton, colorButton.Text);
            main.Add (colorButton);
            prev = colorButton;
        }

        Button button;

        main.Add (
                  button = new ()
                  {
                      X = 2,
                      Y = Pos.Bottom (colorButtonsLabel) + 1,
                      Text =
                          "A super l_öng Button that will probably expose a bug in clipping or wrapping of text. Will it?"
                  }
                 );
        DoMessage (button, button.Text);

        // Note the 'N' in 'Newline' will be the hotkey
        main.Add (
                  button = new () { X = 2, Y = Pos.Bottom (button), Height = 2, Text = "a Newline\nin the button" }
                 );

        button.Accepting += (s, e) =>
                            {
                                MessageBox.Query ((s as View)?.App!, "Message", "Is There A Question?", Strings.btnNo, Strings.btnYes);
                                e.Handled = true;
                            };

        var textChanger = new Button { X = 2, Y = Pos.Bottom (button), Text = "Te_xt Changer" };
        main.Add (textChanger);

        textChanger.Accepting += (_, e) =>
                                 {
                                     textChanger.Text += "!";
                                     e.Handled = true;
                                 };

        main.Add (
                  button = new ()
                  {
                      X = Pos.Right (textChanger) + 2,
                      Y = Pos.Y (textChanger),
                      Text = """This will move as "Text Changer" grows"""
                  }
                 );

        button.Accepting += (_, args) => { args.Handled = true; };

        Button removeButton = new ()
        {
            X = 2, Y = Pos.Bottom (button),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Error),
            Title = "Press to remove this button"
        };
        main.Add (removeButton);

        // This in interesting test case because `moveBtn` and below are laid out relative to this one!
        removeButton.Accepting += (_, e) =>
                                  {
                                      removeButton.Visible = false;
                                      e.Handled = true;
                                  };

        FrameView computedFrame = new ()
        {
            X = 0,
            Y = Pos.Bottom (removeButton),
            Width = Dim.Percent (50),
            Height = 6,
            Title = "Frame (Width = 50%)"
        };
        main.Add (computedFrame);

        // Demonstrates how changing the View.Frame property can move Views
        Button moveBtn = new ()
        {
            X = 0,
            Y = Pos.Center () - 1,
            Width = 30,
            SchemeName = "Error",
            Text = "Move This \u263b Button v_ia Pos"
        };

        moveBtn.Accepting += (_, e) =>
                             {
                                 moveBtn.X = moveBtn.Frame.X + 5;
                                 e.Handled = true;
                             };
        computedFrame.Add (moveBtn);

        // Demonstrates how changing the View.Frame property can SIZE Views (#583)
        Button sizeBtn = new ()
        {
            Y = Pos.Center () + 1,
            X = 0,
            Width = 30,
            Text = "Grow This \u263a Button _via Pos",
            SchemeName = "Error"
        };

        sizeBtn.Accepting += (_, e) =>
                             {
                                 sizeBtn.Width = sizeBtn.Frame.Width + 5;
                                 e.Handled = true;
                             };
        computedFrame.Add (sizeBtn);

        FrameView absoluteFrame = new ()
        {
            X = Pos.Right (computedFrame),
            Y = Pos.Top (computedFrame),
            Width = Dim.Fill (),
            Height = Dim.Height (computedFrame),
            Title = "Frame (Width = Fill)"
        };
        main.Add (absoluteFrame);

        // Demonstrates how changing the View.Frame property can move Views
        Button moveBtnA = new () { SchemeName = "Error", Text = "Move This Button via Frame" };

        moveBtnA.Accepting += (_, e) =>
                              {
                                  moveBtnA.Frame = moveBtnA.Frame with { X = moveBtnA.Frame.X + 5 };
                                  e.Handled = true;
                              };
        absoluteFrame.Add (moveBtnA);

        // Demonstrates how changing the View.Frame property can SIZE Views (#583)
        Button sizeBtnA = new ()
        {
            Y = 2, SchemeName = "Error", Text = " ~  s  gui.cs   main ↑_10 = Сохранить"
        };

        sizeBtnA.Accepting += (_, e) =>
                              {
                                  sizeBtnA.Frame = sizeBtnA.Frame with { Width = sizeBtnA.Frame.Width + 5 };
                                  e.Handled = true;
                              };
        absoluteFrame.Add (sizeBtnA);

        Label label = new ()
        {
            X = 2, Y = Pos.Bottom (computedFrame),

            // ReSharper disable once StringLiteralTypo
            Text = "Text Ali_gnment (changes all buttons): "
        };
        main.Add (label);

        OptionSelector<Alignment> osAlignment = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = 20,
            Value = Alignment.Center,
            AssignHotKeys = true,
            Title = "_9 OptionSelector",
            BorderStyle = LineStyle.Dotted

            // CanFocus = false
        };
        main.Add (osAlignment);

        // Demo changing hotkey
        string MoveHotkey (string txt)
        {
            // Remove the '_'
            List<Rune> runes = txt.ToRuneList ();

            int i = runes.IndexOf ((Rune)'_');
            var start = "";

            if (i > -1)
            {
                start = StringExtensions.ToString (runes.GetRange (0, i));
            }

            txt = start + StringExtensions.ToString (runes.GetRange (i + 1, runes.Count - (i + 1)));

            runes = txt.ToRuneList ();

            // Move over one or go to start
            i++;

            if (i >= runes.Count)
            {
                i = 0;
            }

            // Slip in the '_'
            start = StringExtensions.ToString (runes.GetRange (0, i));

            return start + '_' + StringExtensions.ToString (runes.GetRange (i, runes.Count - i));
        }

        Button moveHotKeyBtn = new ()
        {
            X = 2,
            Y = Pos.Bottom (osAlignment),
            Width = Dim.Width (computedFrame) - 2,
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            Text = "Click to Change th_is Button's Hotkey"
        };

        moveHotKeyBtn.Accepting += (_, e) =>
                                   {
                                       moveHotKeyBtn.Text = MoveHotkey (moveHotKeyBtn.Text);
                                       e.Handled = true;
                                   };
        main.Add (moveHotKeyBtn);

        Button moveUnicodeHotKeyBtn = new ()
        {
            X = Pos.Left (absoluteFrame) + 1,
            Y = Pos.Bottom (osAlignment),
            Width = Dim.Width (absoluteFrame) - 2,
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            Text = " ~  s  gui.cs   main ↑10 = Сохранить"
        };

        moveUnicodeHotKeyBtn.Accepting += (_, e) =>
                                          {
                                              moveUnicodeHotKeyBtn.Text = MoveHotkey (moveUnicodeHotKeyBtn.Text);
                                              e.Handled = true;
                                          };
        main.Add (moveUnicodeHotKeyBtn);

        osAlignment.ValueChanged += (_, args) =>
                                    {
                                        if (args.Value is null)
                                        {
                                            return;
                                        }

                                        // ReSharper disable once AccessToDisposedClosure
                                        SetTextAlignmentForAllButtons (main, args.Value.Value);
                                    };

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (moveUnicodeHotKeyBtn) + 1,
            Title = "Numeric Up/Down (press-and-_hold):"
        };

        NumericUpDown<int> numericUpDown = new ()
        {
            Value = 69,
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label)
        };
        numericUpDown.ValueChanged += NumericUpDownValueChanged;

        void NumericUpDownValueChanged (object? sender, ValueChangedEventArgs<int> e) { }

        main.Add (label, numericUpDown);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (numericUpDown),

            // ReSharper disable once StringLiteralTypo
            Title = "No Repea_t:"
        };
        var noRepeatAcceptCount = 0;

        Button noRepeatButton = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Title = $"Accepting Count: {noRepeatAcceptCount}",
            MouseHoldRepeat = null
        };

        noRepeatButton.Accepting += (_, e) =>
                                    {
                                        noRepeatButton.Title = $"Accepting Count: {++noRepeatAcceptCount}";
                                        Logging.Trace ("noRepeatButton Button Pressed");
                                        e.Handled = true;
                                    };
        main.Add (label, noRepeatButton);

        label = new ()
        {
            X = Pos.Right (noRepeatButton) + 1,
            Y = Pos.Top (label),
            Title = "N_o Repeat (no highlight):"
        };
        var noRepeatNoHighlightAcceptCount = 0;

        Button noRepeatNoHighlight = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Title = $"Accepting Count: {noRepeatNoHighlightAcceptCount}",
            MouseHoldRepeat = null,
            MouseHighlightStates = MouseState.None
        };

        noRepeatNoHighlight.Accepting += (_, e) =>
                                         {
                                             noRepeatNoHighlight.Title = $"Accepting Count: {++noRepeatNoHighlightAcceptCount}";
                                             Logging.Trace ("noRepeatNoHighlight Button Pressed");
                                             e.Handled = true;
                                         };
        main.Add (label, noRepeatNoHighlight);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (noRepeatNoHighlight),
            Title = "Repeat (_press-and-hold):"
        };

        var repeatButtonAcceptingCount = 0;

        Button repeatButton = new ()
        {
            Id = "repeatButton",
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Title = $"Accepting Co_unt: {repeatButtonAcceptingCount}",
            MouseHoldRepeat = MouseFlags.LeftButtonReleased
        };

        repeatButton.Accepting += (_, e) =>
                                  {
                                      repeatButton.Title = $"Accepting Co_unt: {++repeatButtonAcceptingCount}";
                                      e.Handled = true;
                                  };

        CheckBox enableCb = new ()
        {
            X = Pos.Right (repeatButton) + 1,
            Y = Pos.Top (repeatButton),
            Title = "Enabled",
            Value = CheckState.Checked
        };
        enableCb.ValueChanging += (_, _) => { repeatButton.Enabled = !repeatButton.Enabled; };
        main.Add (label, repeatButton, enableCb);

        NumericUpDown<int> decNumericUpDown = new ()
        {
            // ReSharper disable once StringLiteralTypo
            Title = "Hexadecima_l",
            Value = 911,
            Increment = 1,
            Format = "{0:X}",
            X = 0,
            Y = Pos.Bottom (repeatButton),
            BorderStyle = LineStyle.Single,
            Width = 15
        };

        main.Add (decNumericUpDown);

        app.Run (main);

        return;

        static void DoMessage (Button button, string txt)
        {
            button.Accepting += (s, e) =>
                                {
                                    MessageBox.Query ((s as View)?.App!, "Message", $"Did you click {txt}?", Strings.btnNo, Strings.btnYes);
                                    e.Handled = true;
                                };
        }
    }

    private static void SetTextAlignmentForAllButtons (View root, Alignment alignment)
    {
        foreach (Button button in GetAllSubViewsOfType<Button> (root))
        {
            button.TextAlignment = alignment;
        }
    }

    /// <summary>
    ///     Recursively finds all subviews of a specified type in the view hierarchy.
    /// </summary>
    /// <typeparam name="T">The type of views to find.</typeparam>
    /// <param name="view">The root view to start searching from.</param>
    /// <returns>An all matching subviews.</returns>
    private static IEnumerable<T> GetAllSubViewsOfType<T> (View view) where T : View
    {
        foreach (View subview in view.SubViews)
        {
            // If this subview is of the requested type, yield it
            if (subview is T matchingView)
            {
                yield return matchingView;
            }

            // Recursively search this subview's children
            foreach (T child in GetAllSubViewsOfType<T> (subview))
            {
                yield return child;
            }
        }
    }
}

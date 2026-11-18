#nullable enable
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Dialogs", "Demonstrates how to use the Dialog and Dialog<TResult> classes")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Runnable")]
public class Dialogs : Scenario
{
    private const int WIDE_CODE_POINT = '你';

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window mainWindow = new ();
        mainWindow.Title = GetQuitKeyAndName ();

        FrameView frame = new ()
        {
            TabStop = TabBehavior.TabStop, // FrameView normally sets to TabGroup
            X = Pos.Center (),
            Y = 1,
            Width = Dim.Percent (75),
            Height = Dim.Auto (DimAutoStyle.Content),
            Title = "Dialog Options",
            Arrangement = ViewArrangement.Resizable,
            AssignHotKeys = true
        };

        Label numButtonsLabel = new () { X = 0, TextAlignment = Alignment.End, Text = "Number of Buttons:" };

        Label label = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Width (numButtonsLabel),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "Width:"
        };
        frame.Add (label);

        TextField widthEdit = new ()
        {
            X = Pos.Right (numButtonsLabel) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "0"
        };
        frame.Add (widthEdit);

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = Dim.Width (numButtonsLabel),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "Height:"
        };
        frame.Add (label);

        TextField heightEdit = new ()
        {
            X = Pos.Right (numButtonsLabel) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "0"
        };
        frame.Add (heightEdit);

        frame.Add (new Label { X = Pos.Right (widthEdit) + 2, Y = Pos.Top (widthEdit), Text = "If width is 0, the dimension will sized to fit the content." });

        frame.Add (new Label
        {
            X = Pos.Right (heightEdit) + 2, Y = Pos.Top (heightEdit), Text = "If height is 0, the dimension will sized to fit the content."
        });

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = Dim.Width (numButtonsLabel),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "Title:"
        };
        frame.Add (label);

        TextField titleEdit = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Height = 1,
            Text = "Dialog Title"
        };
        frame.Add (titleEdit);

        numButtonsLabel.Y = Pos.Bottom (label);
        frame.Add (numButtonsLabel);

        TextField numButtonsEdit = new ()
        {
            X = Pos.Right (numButtonsLabel) + 1,
            Y = Pos.Top (numButtonsLabel),
            Width = 5,
            Height = 1,
            Text = "3"
        };
        frame.Add (numButtonsEdit);

        CheckBox glyphsNotWords = new ()
        {
            X = Pos.Right (numButtonsLabel) + 1,
            Y = Pos.Bottom (numButtonsLabel),
            TextAlignment = Alignment.End,
            Text = $"Add {char.ConvertFromUtf32 (WIDE_CODE_POINT)} to button text to stress wide char support",
            Value = CheckState.UnChecked
        };
        frame.Add (glyphsNotWords);

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (glyphsNotWords),
            Width = Dim.Width (numButtonsLabel),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "Button Alignment:"
        };
        frame.Add (label);

        OptionSelector<Alignment> alignmentOptionSelector = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Title = "Align",
            UsedHotKeys = frame.UsedHotKeys,
            AssignHotKeys = true
        };
        frame.Add (alignmentOptionSelector);
        alignmentOptionSelector.Value = Dialog.DefaultButtonAlignment;

        frame.ValidatePosDim = true;

        mainWindow.Add (frame);

        label = new Label
        {
            X = Pos.Center (),
            Y = Pos.Bottom (frame) + 4,
            TextAlignment = Alignment.End,
            HotKeySpecifier = (Rune)'\xffff',
            Text = "Button Pressed:"
        };
        mainWindow.Add (label);

        View buttonPressedLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (frame) + 5,
            SchemeName = "Error",
            Text = " ",
            Height = Dim.Auto (),
            Width = Dim.Auto ()
        };

        Button showDialogButton = new () { X = Pos.Center (), Y = Pos.Bottom (frame) + 2, IsDefault = true, Text = "Show Dialog" };

        showDialogButton.Accepting += (s, e) =>
                                      {
                                          using Dialog? dlg = CreateDemoDialog (widthEdit,
                                                                                heightEdit,
                                                                                titleEdit,
                                                                                numButtonsEdit,
                                                                                glyphsNotWords,
                                                                                alignmentOptionSelector);

                                          if (dlg is null)
                                          {
                                              MessageBox.ErrorQuery ((s as View)!.App!, "Error", "Could not create Dialog. Invalid options.", Strings.btnOk);
                                          }
                                          else
                                          {
                                              if (app.Run (dlg) is int result)
                                              {
                                                  buttonPressedLabel.Text = $"Button {(int?)result} pressed.";
                                              }
                                              else
                                              {
                                                  buttonPressedLabel.Text = "Dialog canceled.";
                                              }

                                              e.Handled = true;
                                          }
                                      };

        mainWindow.Add (showDialogButton, buttonPressedLabel);

        // --- Dialog<TResult> Demo ---
        // Demonstrates using Dialog<Color> to return a typed result instead of a button index

        Button showColorDialogButton = new () { X = Pos.Center (), Y = Pos.Bottom (buttonPressedLabel) + 2, Text = "Show Color Dialog<Color>" };
        mainWindow.Add (showColorDialogButton);

        View colorLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (showColorDialogButton),
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Text = "Dialog<T> Demo - Selected Color:"
        };
        mainWindow.Add (colorLabel);

        View selectedColorLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (colorLabel),
            Height = 1,
            Width = 15,
            TextAlignment = Alignment.Center,
            SchemeName = "Error"
        };
        mainWindow.Add (selectedColorLabel);
        selectedColorLabel.SetScheme (new Scheme { Normal = new Attribute (StandardColor.White, StandardColor.OrangeRed) });
        selectedColorLabel.Text = selectedColorLabel.GetScheme ().Normal.Background.ToString ();

        showColorDialogButton.Accepting += (_, e) =>
                                           {
                                               using ColorPickerDialog colorDialog = new (selectedColorLabel.GetScheme ().Normal.Background);
                                               colorDialog.ButtonAlignment = alignmentOptionSelector.Value.Value;

                                               // Run the dialog and get the typed result

                                               if (app.Run (colorDialog) is Color result)
                                               {
                                                   selectedColorLabel.Text = result.ToString ();

                                                   selectedColorLabel.SetScheme (new Scheme
                                                   {
                                                       Normal = new Attribute (selectedColorLabel.GetScheme ()
                                                                                                 .Normal.Foreground,
                                                                               result)
                                                   });
                                               }
                                               else
                                               {
                                                   selectedColorLabel.Text = "Canceled";
                                               }

                                               e.Handled = true;
                                           };

        // --- Prompt Demo ---
        // Demonstrates using Prompt<TView, TResult> with extension methods
        // This is a simpler alternative to creating custom Dialog<TResult> subclasses

        Button showPromptDialogButton = new () { X = Pos.Center (), Y = Pos.Bottom (selectedColorLabel) + 2, Text = "Prompt<AttributePicker, Attribute>" };
        mainWindow.Add (showPromptDialogButton);

        View promptAttributeLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (showPromptDialogButton),
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Text = "Prompt Demo - Selected Attribute:"
        };
        mainWindow.Add (promptAttributeLabel);

        View promptSelectedAttributeLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (promptAttributeLabel),
            Height = 1,
            Width = Dim.Auto (),
            TextAlignment = Alignment.Center,
            SchemeName = "Error"
        };
        mainWindow.Add (promptSelectedAttributeLabel);

        promptSelectedAttributeLabel.SetScheme (new Scheme { Normal = new Attribute (StandardColor.White, StandardColor.Cyan) });
        promptSelectedAttributeLabel.Text = promptSelectedAttributeLabel.GetScheme ().Normal.Background.ToString ();

        void OnShowPromptDialogButtonOnAccepting (object? _, CommandEventArgs e)
        {
            // Use the Prompt extension method - much simpler than custom Dialog<T>!
            // mainWindow is an IRunnable so we can call Prompt on it
            Attribute? result = mainWindow.Prompt<AttributePicker, Attribute?> (input: promptSelectedAttributeLabel.GetScheme ().Normal,
                                                                                beginInitHandler: prompt =>
                                                                                                  {
                                                                                                      // Customize the Prompt dialog
                                                                                                      prompt.Title = "Pick an Attribute";
                                                                                                  });

            if (result is { } attribute)
            {
                promptSelectedAttributeLabel.Text = attribute.ToString ();
                Scheme updatedScheme = promptAttributeLabel.GetScheme () with { Normal = attribute };
                promptSelectedAttributeLabel.SetScheme (updatedScheme);
            }
            else
            {
                promptSelectedAttributeLabel.Text = "Canceled";
            }

            e.Handled = true;
        }

        showPromptDialogButton.Accepting += OnShowPromptDialogButtonOnAccepting;

        mainWindow.UsedHotKeys = frame.UsedHotKeys;
        mainWindow.AssignHotKeys = true;
        mainWindow.CommandsToBubbleUp = [Command.Accept];
        frame.CommandsToBubbleUp = [Command.Accept];

        app.Run (mainWindow);
    }

    private static Dialog? CreateDemoDialog (TextField widthEdit,
                                             TextField heightEdit,
                                             TextField titleEdit,
                                             TextField numButtonsEdit,
                                             CheckBox glyphsNotWords,
                                             OptionSelector alignmentGroup)
    {
        if (!int.TryParse (widthEdit.Text, out int width)
            || !int.TryParse (heightEdit.Text, out int height)
            || !int.TryParse (numButtonsEdit.Text, out int numButtons))
        {
            return null;
        }

        // Add the buttons that go on the bottom of the dialog
        List<Button> dlgButtons = [];

        for (var i = 0; i < numButtons; i++)
        {
            int buttonId = i;
            Button button;

            if (glyphsNotWords.Value == CheckState.Checked)
            {
                buttonId = i;

                button = new Button { Text = "_" + NumberToWords.Convert (buttonId) + " " + char.ConvertFromUtf32 (buttonId + WIDE_CODE_POINT) };
            }
            else
            {
                button = new Button { Text = "_" + NumberToWords.Convert (buttonId) };
            }

            dlgButtons.Add (button);
        }

        // This tests dynamically adding buttons; ensuring the dialog resizes if needed and
        // the buttons are laid out correctly
        Dialog dialog = new ()
        {
            Title = titleEdit.Text,
            ButtonAlignment = (Alignment)Enum.Parse (typeof (Alignment), alignmentGroup.Labels! [alignmentGroup.Value!.Value] [..]),
            Buttons = dlgButtons.ToArray ()
        };

        Label label = new () { Title = "_Enter text:" };
        dialog.Add (label);

        TextField textField = new () { Y = Pos.Bottom (label), Width = Dim.Fill (0, 60), Text = new string ("0123456789").Repeat (6)! };
        dialog.Add (textField);

        CheckBox checkBox = new () { Title = "_Check Me", Y = Pos.Bottom (textField), Value = CheckState.UnChecked };
        dialog.Add (checkBox);

        OptionSelector<Schemes> optionSelector = new () { Y = Pos.Bottom (checkBox), Value = Schemes.Error, AssignHotKeys = true };
        dialog.Add (optionSelector);

        FrameView frame = new ()
        {
            Title = "Frame (Fill)",
            X = Pos.Right (checkBox) + 1,
            Y = Pos.Bottom (textField),
            Width = Dim.Fill (),
            Height = Dim.Fill (0, 1)
        };
        dialog.Add (frame);

        Button addButtonButton = new () { Title = "_Add Button", X = Pos.AnchorEnd (), Y = Pos.AnchorEnd () };
        dialog.Add (addButtonButton);

        addButtonButton.Accepting += (_, e) =>
                                     {
                                         int newButtonId = dialog.Buttons.Length;

                                         Button newButton = new () { Text = "_" + NumberToWords.Convert (newButtonId) };
                                         dialog.AddButton (newButton);
                                         e.Handled = true;
                                     };

        if (width != 0)
        {
            dialog.Width = width;
        }

        if (height != 0)
        {
            dialog.Height = height;
        }

        return dialog;
    }

    /// <summary>
    ///     Example of a custom Dialog that returns a <see cref="Color"/> instead of a button index.
    ///     This demonstrates how to use <see cref="Dialog{TResult}"/> to create type-safe dialogs.
    /// </summary>
    private sealed class ColorPickerDialog : Dialog<Color>
    {
        private readonly ColorPicker _colorPicker;

        public ColorPickerDialog (Color initialColor)
        {
            Title = "Pick a Color";

            _colorPicker = new ColorPicker
            {
                Value = initialColor,
                Style = new ColorPickerStyle { ShowColorName = true, ShowTextFields = true },
                Width = Dim.Fill (0, 48),
                AssignHotKeys = true
            };
            Add (_colorPicker);
            _colorPicker.ApplyStyleChanges ();

            // Add Cancel and OK buttons
            AddButton (new Button { Text = "_Cancel" });
            AddButton (new Button { Text = "_OK" });
        }

        /// <inheritdoc/>
        protected override void OnAccepted (ICommandContext? ctx)
        {
            base.OnAccepted (ctx);
            Result = _colorPicker.Value!.Value;
        }
    }

    public override List<Key> GetDemoKeyStrokes (IApplication? app)
    {
        List<Key> keys = [Key.D6, Key.D5, Key.Tab, Key.D2, Key.D0, Key.Enter];

        for (var i = 0; i < 5; i++)
        {
            keys.Add (Key.A);
        }

        keys.Add (Key.Enter);

        keys.Add (Key.S.WithAlt);
        keys.Add (Key.Enter);

        for (var i = 0; i < 5; i++)
        {
            keys.Add (Key.A);
        }

        keys.Add (Key.Enter);

        keys.Add (Key.E.WithAlt);
        keys.Add (Key.Enter);

        for (var i = 0; i < 5; i++)
        {
            keys.Add (Key.A);
        }

        keys.Add (Key.Enter);

        keys.Add (Key.C.WithAlt);
        keys.Add (Key.Enter);

        for (var i = 0; i < 5; i++)
        {
            keys.Add (Key.A);
        }

        keys.Add (Key.Enter);

        keys.Add (Key.F.WithAlt);
        keys.Add (Key.Enter);

        for (var i = 0; i < 5; i++)
        {
            keys.Add (Key.A);
        }

        keys.Add (Key.Enter);

        return keys;
    }
}

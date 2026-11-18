using System.Text;
using Terminal.Gui.Editor;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Text Alignment and Direction", "Demos horizontal and vertical text alignment and direction.")]
[ScenarioCategory ("Text and Formatting")]
public class TextAlignmentAndDirection : Scenario
{
    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new () { Title = GetQuitKeyAndName () };

        var txt = $"Hello World{Environment.NewLine}HELLO WORLD{Environment.NewLine}🦮 👨‍👩‍👧";

        SchemeManager.AddScheme ("TextAlignmentAndDirection1", new Scheme { Normal = new Attribute (Color.Black, Color.Gray) });
        SchemeManager.AddScheme ("TextAlignmentAndDirection2", new Scheme { Normal = new Attribute (Color.Black, Color.DarkGray) });

        List<View> singleLineLabels = []; // single line
        List<View> multiLineLabels = []; // multi line

        // Horizontal Single-Line

        Label labelHL = new ()
        {
            X = 0,
            Y = 0,
            Width = 6,
            Height = 1,
            TextAlignment = Alignment.End,
            SchemeName = "Dialog",
            Text = "Start"
        };

        Label labelHC = new ()
        {
            X = 0,
            Y = 1,
            Width = 6,
            Height = 1,
            TextAlignment = Alignment.End,
            SchemeName = "Dialog",
            Text = "Center"
        };

        Label labelHR = new ()
        {
            X = 0,
            Y = 2,
            Width = 6,
            Height = 1,
            TextAlignment = Alignment.End,
            SchemeName = "Dialog",
            Text = "End"
        };

        Label labelHJ = new ()
        {
            X = 0,
            Y = 3,
            Width = 6,
            Height = 1,
            TextAlignment = Alignment.End,
            SchemeName = "Dialog",
            Text = "Fill"
        };

        View txtLabelHL = new ()
        {
            X = Pos.Right (labelHL) + 1,
            Y = Pos.Y (labelHL),
            Width = Dim.Fill (9),
            Height = 1,
            SchemeName = "TextAlignmentAndDirection1",
            TextAlignment = Alignment.Start,
            Text = txt,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };

        View txtLabelHC = new ()
        {
            X = Pos.Right (labelHC) + 1,
            Y = Pos.Y (labelHC),
            Width = Dim.Fill (9),
            Height = 1,
            SchemeName = "TextAlignmentAndDirection2",
            TextAlignment = Alignment.Center,
            Text = txt,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };

        View txtLabelHR = new ()
        {
            X = Pos.Right (labelHR) + 1,
            Y = Pos.Y (labelHR),
            Width = Dim.Fill (9),
            Height = 1,
            SchemeName = "TextAlignmentAndDirection1",
            TextAlignment = Alignment.End,
            Text = txt,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };

        View txtLabelHJ = new ()
        {
            X = Pos.Right (labelHJ) + 1,
            Y = Pos.Y (labelHJ),
            Width = Dim.Fill (9),
            Height = 1,
            SchemeName = "TextAlignmentAndDirection2",
            TextAlignment = Alignment.Fill,
            Text = txt,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };

        singleLineLabels.Add (txtLabelHL);
        singleLineLabels.Add (txtLabelHC);
        singleLineLabels.Add (txtLabelHR);
        singleLineLabels.Add (txtLabelHJ);

        window.Add (labelHL);
        window.Add (txtLabelHL);
        window.Add (labelHC);
        window.Add (txtLabelHC);
        window.Add (labelHR);
        window.Add (txtLabelHR);
        window.Add (labelHJ);
        window.Add (txtLabelHJ);

        // Vertical Single-Line

        Label labelVT = new ()
        {
            X = Pos.AnchorEnd () - 6,
            Y = 0,
            Width = 2,
            Height = 6,
            SchemeName = "TextAlignmentAndDirection1",
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.End,
            Text = "Start"
        };
        labelVT.TextFormatter.WordWrap = false;

        Label labelVM = new ()
        {
            X = Pos.AnchorEnd () - 4,
            Y = 0,
            Width = 2,
            Height = 6,
            SchemeName = "TextAlignmentAndDirection1",
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.End,
            Text = "Center"
        };
        labelVM.TextFormatter.WordWrap = false;

        Label labelVB = new ()
        {
            X = Pos.AnchorEnd () - 2,
            Y = 0,
            Width = 2,
            Height = 6,
            SchemeName = "TextAlignmentAndDirection1",
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.End,
            Text = "End"
        };
        labelVB.TextFormatter.WordWrap = false;

        Label labelVJ = new ()
        {
            X = Pos.AnchorEnd (),
            Y = 0,
            Width = 2,
            Height = 6,
            SchemeName = "TextAlignmentAndDirection1",
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.End,
            Text = "Fill"
        };
        labelVJ.TextFormatter.WordWrap = false;

        View txtLabelVT = new ()
        {
            X = Pos.X (labelVT),
            Y = Pos.Bottom (labelVT) + 1,
            Width = 2,
            Height = Dim.Fill (),
            SchemeName = "TextAlignmentAndDirection1",
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Start,
            Text = txt,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };
        txtLabelVT.TextFormatter.WordWrap = false;

        View txtLabelVM = new ()
        {
            X = Pos.X (labelVM),
            Y = Pos.Bottom (labelVM) + 1,
            Width = 2,
            Height = Dim.Fill (),
            SchemeName = "TextAlignmentAndDirection2",
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Center,
            Text = txt,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };
        txtLabelVM.TextFormatter.WordWrap = false;

        View txtLabelVB = new ()
        {
            X = Pos.X (labelVB),
            Y = Pos.Bottom (labelVB) + 1,
            Width = 2,
            Height = Dim.Fill (),
            SchemeName = "TextAlignmentAndDirection1",
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.End,
            Text = txt,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };
        txtLabelVB.TextFormatter.WordWrap = false;

        View txtLabelVJ = new ()
        {
            X = Pos.X (labelVJ),
            Y = Pos.Bottom (labelVJ) + 1,
            Width = 2,
            Height = Dim.Fill (),
            SchemeName = "TextAlignmentAndDirection2",
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Fill,
            Text = txt,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };
        txtLabelVJ.TextFormatter.WordWrap = false;

        singleLineLabels.Add (txtLabelVT);
        singleLineLabels.Add (txtLabelVM);
        singleLineLabels.Add (txtLabelVB);
        singleLineLabels.Add (txtLabelVJ);

        window.Add (labelVT);
        window.Add (txtLabelVT);
        window.Add (labelVM);
        window.Add (txtLabelVM);
        window.Add (labelVB);
        window.Add (txtLabelVB);
        window.Add (labelVJ);
        window.Add (txtLabelVJ);

        // Multi-Line

        View container = new ()
        {
            X = 0, Y = Pos.Bottom (txtLabelHJ), Width = Dim.Fill (31), Height = Dim.Fill (4)

            // SchemeName = "TextAlignmentAndDirection2"
        };

        AlignmentAndDirectionView txtLabelTL = new ()
        {
            X = 0,
            Y = 1,
            Width = Dim.Percent (100 / 3),
            Height = Dim.Percent (100 / 3),
            TextAlignment = Alignment.Start,
            VerticalTextAlignment = Alignment.Start,
            SchemeName = "TextAlignmentAndDirection1",
            Text = txt
        };
        txtLabelTL.TextFormatter.MultiLine = true;

        AlignmentAndDirectionView txtLabelTC = new ()
        {
            X = Pos.Right (txtLabelTL),
            Y = 1,
            Width = Dim.Percent (100 / 3),
            Height = Dim.Percent (100 / 3),
            TextAlignment = Alignment.Center,
            VerticalTextAlignment = Alignment.Start,
            SchemeName = "TextAlignmentAndDirection1",
            Text = txt
        };
        txtLabelTC.TextFormatter.MultiLine = true;

        AlignmentAndDirectionView txtLabelTR = new ()
        {
            X = Pos.Right (txtLabelTC),
            Y = 1,
            Width = Dim.Percent (100, DimPercentMode.Position),
            Height = Dim.Percent (100 / 3),
            TextAlignment = Alignment.End,
            VerticalTextAlignment = Alignment.Start,
            SchemeName = "TextAlignmentAndDirection1",
            Text = txt
        };
        txtLabelTR.TextFormatter.MultiLine = true;

        AlignmentAndDirectionView txtLabelML = new ()
        {
            X = Pos.X (txtLabelTL),
            Y = Pos.Bottom (txtLabelTL),
            Width = Dim.Width (txtLabelTL),
            Height = Dim.Percent (100 / 3),
            TextAlignment = Alignment.Start,
            VerticalTextAlignment = Alignment.Center,
            SchemeName = "TextAlignmentAndDirection1",
            Text = txt
        };
        txtLabelML.TextFormatter.MultiLine = true;

        AlignmentAndDirectionView txtLabelMC = new ()
        {
            X = Pos.X (txtLabelTC),
            Y = Pos.Bottom (txtLabelTC),
            Width = Dim.Width (txtLabelTC),
            Height = Dim.Percent (100 / 3),
            TextAlignment = Alignment.Center,
            VerticalTextAlignment = Alignment.Center,
            SchemeName = "TextAlignmentAndDirection1",
            Text = txt
        };
        txtLabelMC.TextFormatter.MultiLine = true;

        AlignmentAndDirectionView txtLabelMR = new ()
        {
            X = Pos.X (txtLabelTR),
            Y = Pos.Bottom (txtLabelTR),
            Width = Dim.Percent (100, DimPercentMode.Position),
            Height = Dim.Percent (100 / 3),
            TextAlignment = Alignment.End,
            VerticalTextAlignment = Alignment.Center,
            SchemeName = "TextAlignmentAndDirection1",
            Text = txt
        };
        txtLabelMR.TextFormatter.MultiLine = true;

        AlignmentAndDirectionView txtLabelBL = new ()
        {
            X = Pos.X (txtLabelML),
            Y = Pos.Bottom (txtLabelML),
            Width = Dim.Width (txtLabelML),
            Height = Dim.Percent (100, DimPercentMode.Position),
            TextAlignment = Alignment.Start,
            VerticalTextAlignment = Alignment.End,
            SchemeName = "TextAlignmentAndDirection1",
            Text = txt
        };
        txtLabelBL.TextFormatter.MultiLine = true;

        AlignmentAndDirectionView txtLabelBC = new ()
        {
            X = Pos.X (txtLabelMC),
            Y = Pos.Bottom (txtLabelMC),
            Width = Dim.Width (txtLabelMC),
            Height = Dim.Percent (100, DimPercentMode.Position),
            TextAlignment = Alignment.Center,
            VerticalTextAlignment = Alignment.End,
            SchemeName = "TextAlignmentAndDirection1",
            Text = txt
        };
        txtLabelBC.TextFormatter.MultiLine = true;

        AlignmentAndDirectionView txtLabelBR = new ()
        {
            X = Pos.X (txtLabelMR),
            Y = Pos.Bottom (txtLabelMR),
            Width = Dim.Percent (100, DimPercentMode.Position),
            Height = Dim.Percent (100, DimPercentMode.Position),
            TextAlignment = Alignment.End,
            VerticalTextAlignment = Alignment.End,
            SchemeName = "TextAlignmentAndDirection1",
            Text = txt
        };
        txtLabelBR.TextFormatter.MultiLine = true;

        multiLineLabels.Add (txtLabelTL);
        multiLineLabels.Add (txtLabelTC);
        multiLineLabels.Add (txtLabelTR);
        multiLineLabels.Add (txtLabelML);
        multiLineLabels.Add (txtLabelMC);
        multiLineLabels.Add (txtLabelMR);
        multiLineLabels.Add (txtLabelBL);
        multiLineLabels.Add (txtLabelBC);
        multiLineLabels.Add (txtLabelBR);

        // Save Alignment in Data
        foreach (View t in multiLineLabels)
        {
            t.Data = new TextAlignmentData (t.TextAlignment, t.VerticalTextAlignment);
        }

        container.Add (txtLabelTL);
        container.Add (txtLabelTC);
        container.Add (txtLabelTR);

        container.Add (txtLabelML);
        container.Add (txtLabelMC);
        container.Add (txtLabelMR);

        container.Add (txtLabelBL);
        container.Add (txtLabelBC);
        container.Add (txtLabelBR);

        window.Add (container);

        // Edit Text

        Label label = new ()
        {
            X = 1,
            Y = Pos.Bottom (container) + 1,
            Width = 10,
            Height = 1,
            Text = "Edit Text:"
        };

        Editor editText = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (31),
            Height = 3,
            Text = txt
        };

        editText.Document?.Changed += (_, _) =>
                                      {
                                          foreach (View v in singleLineLabels)
                                          {
                                              v.Text = editText.Text;
                                          }

                                          foreach (View v in multiLineLabels)
                                          {
                                              v.Text = editText.Text;
                                          }
                                      };

        editText.SetFocus ();

        window.Add (label, editText);

        // JUSTIFY CHECKBOX

        CheckBox justifyCheckbox = new ()
        {
            X = Pos.Right (container) + 1,
            Y = Pos.Y (container) + 1,
            Width = Dim.Fill (10),
            Height = 1,
            Text = "Fill"
        };

        window.Add (justifyCheckbox);

        // JUSTIFY OPTIONS

        OptionSelector justifyOptions = new ()
        {
            X = Pos.Left (justifyCheckbox) + 1,
            Y = Pos.Y (justifyCheckbox) + 1,
            Width = Dim.Fill (9),
            Labels = ["Current direction", "Opposite direction", "FIll Both"],
            Enabled = false
        };

        justifyCheckbox.ValueChanging += (_, e) => ToggleJustify (e.NewValue != CheckState.Checked);

        justifyOptions.ValueChanged += (_, _) => { ToggleJustify (false, true); };

        window.Add (justifyOptions);

        // WRAP CHECKBOX

        CheckBox wrapCheckbox = new ()
        {
            X = Pos.Right (container) + 1,
            Y = Pos.Bottom (justifyOptions),
            Width = Dim.Fill (10),
            Height = 1,
            Text = "Word Wrap"
        };
        wrapCheckbox.Value = wrapCheckbox.TextFormatter.WordWrap ? CheckState.Checked : CheckState.UnChecked;

        wrapCheckbox.ValueChanging += (s, e) =>
                                      {
                                          if (e.NewValue == CheckState.Checked)
                                          {
                                              foreach (View t in multiLineLabels)
                                              {
                                                  t.TextFormatter.WordWrap = false;
                                              }
                                          }
                                          else
                                          {
                                              foreach (View t in multiLineLabels)
                                              {
                                                  t.TextFormatter.WordWrap = true;
                                              }
                                          }
                                      };

        window.Add (wrapCheckbox);

        List<TextDirection> directionsEnum = Enum.GetValues (typeof (TextDirection)).Cast<TextDirection> ().ToList ();

        OptionSelector directionOptions = new ()
        {
            X = Pos.Right (container) + 1,
            Y = Pos.Bottom (wrapCheckbox) + 1,
            Width = Dim.Fill (10),
            Height = Dim.Fill (1),
            HotKeySpecifier = (Rune)'\xffff',
            Labels = directionsEnum.Select (e => e.ToString ()).ToArray ()
        };

        directionOptions.ValueChanged += (s, ev) =>
                                         {
                                             bool justChecked = justifyCheckbox.Value == CheckState.Checked;

                                             if (justChecked)
                                             {
                                                 ToggleJustify (true);
                                             }

                                             foreach (View v in multiLineLabels.Where (v => ev.NewValue is { }))
                                             {
                                                 v.TextDirection = (TextDirection)ev.NewValue!.Value;
                                             }

                                             if (justChecked)
                                             {
                                                 ToggleJustify (false);
                                             }
                                         };

        window.Add (directionOptions);

        app.Run (window);

        // Be a good citizen and remove the schemes we added
        SchemeManager.RemoveScheme ("TextAlignmentAndDirection1");
        SchemeManager.RemoveScheme ("TextAlignmentAndDirection2");

        return;

        void ToggleJustify (bool oldValue, bool wasJustOptions = false)
        {
            if (oldValue)
            {
                if (!wasJustOptions)
                {
                    justifyOptions.Enabled = false;
                }

                foreach (View t in multiLineLabels)
                {
                    var data = (TextAlignmentData)t.Data;
                    t.TextAlignment = data!.h;
                    t.VerticalTextAlignment = data.v;
                }
            }
            else
            {
                foreach (View t in multiLineLabels)
                {
                    if (!wasJustOptions)
                    {
                        justifyOptions.Enabled = true;
                    }

                    var data = (TextAlignmentData)t.Data;

                    if (TextFormatter.IsVerticalDirection (t.TextDirection))
                    {
                        switch (justifyOptions.Value)
                        {
                            case 0:
                                t.VerticalTextAlignment = Alignment.Fill;
                                t.TextAlignment = data!.h;

                                break;

                            case 1:
                                t.VerticalTextAlignment = data!.v;
                                t.TextAlignment = Alignment.Fill;

                                break;

                            case 2:
                                t.VerticalTextAlignment = Alignment.Fill;
                                t.TextAlignment = Alignment.Fill;

                                break;
                        }
                    }
                    else
                    {
                        switch (justifyOptions.Value)
                        {
                            case 0:
                                t.TextAlignment = Alignment.Fill;
                                t.VerticalTextAlignment = data!.v;

                                break;

                            case 1:
                                t.TextAlignment = data!.h;
                                t.VerticalTextAlignment = Alignment.Fill;

                                break;

                            case 2:
                                t.TextAlignment = Alignment.Fill;
                                t.VerticalTextAlignment = Alignment.Fill;

                                break;
                        }
                    }
                }
            }
        }
    }

    internal class AlignmentAndDirectionView : View
    {
        public AlignmentAndDirectionView ()
        {
            ViewportSettings = ViewportSettingsFlags.Transparent;
            BorderStyle = LineStyle.Dotted;
        }
    }

    private class TextAlignmentData (Alignment h, Alignment v)
    {
        public Alignment h { get; } = h;
        public Alignment v { get; } = v;
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("SpinnerView Styles", "Shows the SpinnerView Styles.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Progress")]
public class SpinnerViewStyles : Scenario
{
    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        const int DEFAULT_DELAY = 130;
        const string DEFAULT_CUSTOM = @"-\|/";
        Dictionary<int, KeyValuePair<string, Type>> styleDict = new ();
        var i = 0;

        foreach (Type style in typeof (SpinnerStyle).GetNestedTypes ())
        {
            styleDict.Add (i, new (style.Name, style));
            i++;
        }

        var preview = new View
        {
            X = Pos.Center (),
            Y = 0,
            Width = 22,
            Height = 3,

            //Title = "Preview",
            BorderStyle = LineStyle.Single
        };
        win.Add (preview);

        var spinner = new SpinnerView { X = Pos.Center (), Y = 0 };
        preview.Add (spinner);
        spinner.AutoSpin = true;

        var ckbAscii = new CheckBox
        {
            X = Pos.Center () - 7,
            Y = Pos.Bottom (preview),
            Enabled = false,
            Value = CheckState.Checked,
            Text = "Ascii Only"
        };
        win.Add (ckbAscii);

        var ckbNoSpecial = new CheckBox
        {
            X = Pos.Center () + 7,
            Y = Pos.Bottom (preview),
            Enabled = false,
            Value = CheckState.Checked,
            Text = "No Special"
        };
        win.Add (ckbNoSpecial);

        var ckbReverse = new CheckBox
        {
            X = Pos.Center () - 22, Y = Pos.Bottom (preview) + 1, Value = CheckState.UnChecked, Text = "Reverse"
        };
        win.Add (ckbReverse);

        var ckbBounce = new CheckBox
        {
            X = Pos.Right (ckbReverse) + 2, Y = Pos.Bottom (preview) + 1, Value = CheckState.UnChecked, Text = "Bounce"
        };
        win.Add (ckbBounce);

        var delayLabel = new Label { X = Pos.Right (ckbBounce) + 2, Y = Pos.Bottom (preview) + 1, Text = "Delay:" };
        win.Add (delayLabel);

        var delayField = new TextField
        {
            X = Pos.Right (delayLabel), Y = Pos.Bottom (preview) + 1, Width = 5, Text = DEFAULT_DELAY.ToString ()
        };
        win.Add (delayField);

        delayField.TextChanged += (s, e) =>
                                  {
                                      if (ushort.TryParse (delayField.Text, out ushort i))
                                      {
                                          spinner.SpinDelay = i;
                                      }
                                  };

        var customLabel = new Label { X = Pos.Right (delayField) + 2, Y = Pos.Bottom (preview) + 1, Text = "Custom:" };
        win.Add (customLabel);

        var customField = new TextField
        {
            X = Pos.Right (customLabel), Y = Pos.Bottom (preview) + 1, Width = 12, Text = DEFAULT_CUSTOM
        };
        win.Add (customField);

        string [] styleArray = styleDict.Select (e => e.Value.Key).ToArray ();

        if (styleArray.Length < 1)
        {
            return;
        }

        var styles = new ListView
        {
            X = Pos.Center (), Y = Pos.Bottom (preview) + 2, Height = Dim.Fill (), Width = Dim.Fill (1)
        };
        styles.SetSource (new ObservableCollection<string> (styleArray));
        styles.SelectedItem = 0; // SpinnerStyle.Custom;
        win.Add (styles);
        SetCustom ();

        customField.TextChanged += (s, e) =>
                                   {
                                       if (customField.Text.Length > 0)
                                       {
                                           if (styles.SelectedItem != 0)
                                           {
                                               styles.SelectedItem = 0; // SpinnerStyle.Custom
                                           }

                                           SetCustom ();
                                       }
                                   };

        styles.ValueChanged += (_, e) =>
                               {
                                   if (e.NewValue == 0)
                                   {
                                       // SpinnerStyle.Custom
                                       if (customField.Text.Length < 1)
                                       {
                                           customField.Text = DEFAULT_CUSTOM;
                                       }

                                       if (delayField.Text.Length < 1)
                                       {
                                           delayField.Text = DEFAULT_DELAY.ToString ();
                                       }

                                       SetCustom ();
                                   }
                                   else
                                   {
                                       spinner.Visible = true;
                                       spinner.Style = (SpinnerStyle)Activator.CreateInstance (styleDict [e.NewValue!.Value].Value)!;
                                       delayField.Text = spinner.SpinDelay.ToString ();
                                       ckbBounce.Value = spinner.SpinBounce ? CheckState.Checked : CheckState.UnChecked;
                                       ckbNoSpecial.Value = !spinner.HasSpecialCharacters ? CheckState.Checked : CheckState.UnChecked;
                                       ckbAscii.Value = spinner.IsAsciiOnly ? CheckState.Checked : CheckState.UnChecked;
                                       ckbReverse.Value = CheckState.UnChecked;
                                   }
                               };

        ckbReverse.ValueChanging += (s, e) => { spinner.SpinReverse = e.NewValue == CheckState.Checked; };

        ckbBounce.ValueChanging += (s, e) => { spinner.SpinBounce = e.NewValue == CheckState.Checked; };

        win.IsRunningChanged += WinIsRunningChanged;

        void SetCustom ()
        {
            if (customField.Text.Length > 0)
            {
                spinner.Visible = true;

                if (ushort.TryParse (delayField.Text, out ushort d))
                {
                    spinner.SpinDelay = d;
                }
                else
                {
                    delayField.Text = DEFAULT_DELAY.ToString ();
                    spinner.SpinDelay = DEFAULT_DELAY;
                }

                List<string> str = new ();

                foreach (char c in customField.Text)
                {
                    str.Add (c.ToString ());
                }

                spinner.Sequence = str.ToArray ();
            }
            else
            {
                spinner.Visible = false;
            }
        }

        void WinIsRunningChanged (object sender, EventArgs<bool> args)
        {
            if (!args.Value && spinner is {})
            {
                spinner.Dispose ();
                spinner = null;
            }
        }

        app.Run (win);
        win.IsRunningChanged -= WinIsRunningChanged;
        if (spinner is not null)
        {
            spinner.Dispose ();
            spinner = null;
        }
    }
}

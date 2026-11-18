namespace UICatalog.Scenarios;

[ScenarioMetadata ("Spinner Demo", "Minimal SpinnerView demo with auto-spin.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Progress")]
public class MinimalSpinnerDemo : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window main = new () { Title = GetQuitKeyAndName () };

        SpinnerView spinner = new () { X = Pos.Center (), Y = Pos.Center (), Visible = true, AutoSpin = true };

        CheckBox chkVisible = new ()
        {
            Text = "Visible", X = Pos.Center (), Y = Pos.Bottom (spinner) + 1, Value = spinner.Visible ? CheckState.Checked : CheckState.UnChecked
        };
        chkVisible.ValueChanged += (_, e) => { spinner.Visible = e.NewValue == CheckState.Checked; };

        CheckBox chkAutoSpin = new ()
        {
            Text = "AutoSpin", X = Pos.Center (), Y = Pos.Bottom (chkVisible) + 1, Value = spinner.AutoSpin ? CheckState.Checked : CheckState.UnChecked
        };
        chkAutoSpin.ValueChanged += (_, e) => { spinner.AutoSpin = e.NewValue == CheckState.Checked; };

        CheckBox chkSyncWithTerminal = new () { Text = "SyncWithTerminal", X = Pos.Center (), Y = Pos.Bottom (chkAutoSpin) + 1, Value = CheckState.UnChecked };
        chkSyncWithTerminal.ValueChanged += (_, e) => spinner.SyncWithTerminal = e.NewValue == CheckState.Checked;

        Label lblSequence = new () { Text = "Sequence (comma-separated):", X = Pos.Center (), Y = Pos.Bottom (chkSyncWithTerminal) + 1 };

        TextField tfSequence = new () { Text = string.Join (",", spinner.Sequence), X = Pos.Center (), Y = Pos.Bottom (lblSequence), Width = 30 };

        tfSequence.Accepting += (_, _) =>
                                {
                                    string [] frames = tfSequence.Text.Split (',', StringSplitOptions.RemoveEmptyEntries);

                                    if (frames.Length > 0)
                                    {
                                        spinner.Sequence = frames;
                                    }
                                };

        Button btnAdvance = new () { Text = "Advance", X = Pos.Center (), Y = Pos.Bottom (tfSequence) + 1 };
        btnAdvance.Accepting += (_, _) => spinner.AdvanceAnimation ();

        main.AssignHotKeys = true;

        main.Add (spinner, chkVisible, chkAutoSpin, chkSyncWithTerminal, lblSequence, tfSequence, btnAdvance);

        app.Run (main);
    }
}

#nullable enable

// ReSharper disable AccessToDisposedClosure
namespace UICatalog.Scenarios;

[ScenarioMetadata ("Wizards", "Demonstrates the Wizard class")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Wizards")]
[ScenarioCategory ("Runnable")]
public class Wizards : Scenario
{
    private Wizard? _wizard;
    private View? _actionLabel;
    private TextField? _titleEdit;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new ();
        win.Title = GetQuitKeyAndName ();

        FrameView settingsFrame = new ()
        {
            X = Pos.Center (),
            Y = 0,
            Width = Dim.Percent (75),
            Height = Dim.Auto (),
            Title = "Wizard Options"
        };
        win.Add (settingsFrame);

        Label label = new () { X = 0, TextAlignment = Alignment.End, Text = "_Title:" };
        settingsFrame.Add (label);

        _titleEdit = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Height = 1,
            Text = "Gandolf"
        };
        settingsFrame.Add (_titleEdit);

        CheckBox cbRun = new () { Title = "_Run Wizard as a modal", X = 0, Y = Pos.Bottom (label), Value = CheckState.Checked };
        settingsFrame.Add (cbRun);

        Button showWizardButton = new () { X = Pos.Center (), Y = Pos.Bottom (cbRun), IsDefault = true, Text = "_Show Wizard" };

        settingsFrame.Add (showWizardButton);

        label = new Label { X = Pos.Center (), Y = Pos.AnchorEnd (1), TextAlignment = Alignment.End, Text = "Action:" };
        win.Add (label);

        _actionLabel = new View
        {
            X = Pos.Right (label),
            Y = Pos.AnchorEnd (1),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Error),
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };
        win.Add (_actionLabel);

        if (cbRun.Value != CheckState.Checked)
        {
            showWizardButton.Enabled = false;
            _wizard = CreateWizard ();
            win.Add (_wizard);
        }

        cbRun.ValueChanged += (_, a) =>
                              {
                                  if (a.NewValue == CheckState.Checked)
                                  {
                                      showWizardButton.Enabled = true;
                                      _wizard!.X = Pos.Center ();
                                      _wizard.Y = Pos.Center ();

                                      win.Remove (_wizard);
                                      _wizard.Dispose ();
                                      _wizard = null;
                                  }
                                  else
                                  {
                                      showWizardButton.Enabled = false;
                                      _wizard = CreateWizard ();
                                      _wizard.Y = Pos.Bottom (settingsFrame) + 1;
                                      win.Add (_wizard);
                                  }
                              };

        showWizardButton.Accepting += (_, _) =>
                                      {
                                          _wizard = CreateWizard ();
                                          app.Run (_wizard);
                                          _wizard.Dispose ();
                                      };

        app.Run (win);
    }

    private Wizard CreateWizard ()
    {
        Wizard wizard = new ();

        if (_titleEdit is { })
        {
            wizard.Title = _titleEdit.Text;
        }

        wizard.MovingBack += (_, args) =>
                             {
                                 // Set Cancel to true to prevent moving back
                                 args.Cancel = false;
                                 _actionLabel!.Text = "Moving Back";
                             };

        wizard.MovingNext += (_, args) =>
                             {
                                 // Set Cancel to true to prevent moving next
                                 args.Cancel = false;
                                 _actionLabel!.Text = "Moving Next";
                             };

        wizard.Accepting += (s, args) =>
                            {
                                _actionLabel!.Text = "Finished";
                                MessageBox.Query ((s as View)?.App!, "Wizard", "The Wizard has been completed and accepted!", Strings.btnOk);

                                if (wizard.IsRunning)
                                {
                                    // Don't set args.Handled to true to allow the wizard to close
                                    args.Handled = false;
                                }
                                else
                                {
                                    wizard.App!.RequestStop ();
                                    args.Handled = true;
                                }
                            };

        //wizard.Cancelled += (s, args) =>
        //                    {
        //                        _actionLabel!.Text = "Cancelled";

        //                        int? btn = MessageBox.Query ((s as View)?.App!, "Wizard", "Are you sure you want to cancel?", Strings.btnNo, Strings.btnYes);
        //                        args.Cancel = btn is not 1;
        //                    };

        ((IDesignable)wizard).EnableForDesign ();

        return wizard;
    }
}

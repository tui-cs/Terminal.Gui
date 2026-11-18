using System.Collections.ObjectModel;
// ReSharper disable AccessToDisposedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ProgressBar Styles", "Shows the ProgressBar Styles.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Progress")]
[ScenarioCategory ("Threading")]

// TODO: Add enable/disable to show that that is working
// TODO: Clean up how FramesEditor works
// TODO: Better align rpPBFormat
public class ProgressBarStyles : Scenario
{
    private const uint TIMER_TICK = 20;
    private Timer _fractionTimer;
    private Timer _pulseTimer;
    private ListView _pbList;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new ();
        win.Title = GetQuitKeyAndName ();
        win.BorderStyle = LineStyle.Single;

        var editor = new AdornmentsEditor { AutoSelectViewToEdit = false, ShowViewIdentifier = true };
        win.Add (editor);

        View container = new () { X = Pos.Right (editor), Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (container);

        const float FRACTION_STEP = 0.01F;

        _pbList = new ListView
        {
            Title = "Focused ProgressBar",
            Y = Pos.Align (Alignment.Start),
            X = Pos.Center (),
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            BorderStyle = LineStyle.Single
        };
        container.Add (_pbList);

        #region ColorPicker

        var fgColorPickerBtn = new Button { Text = "Foreground HotNormal Color", X = Pos.Center (), Y = Pos.Align (Alignment.Start) };
        container.Add (fgColorPickerBtn);

        fgColorPickerBtn.Accepting += (_, _) =>
                                      {
                                          Color? result =
                                              app.TopRunnable?.Prompt<ColorPicker, Color?> (input: editor.ViewToEdit!.GetAttributeForRole (VisualRole.Normal)
                                                                                                         .Foreground,
                                                                                            beginInitHandler: prompt => { prompt.Title = "Foreground Color"; });

                                          if (result is not { } selectedColor)
                                          {
                                              return;
                                          }

                                          Scheme cs = new (editor.ViewToEdit.GetScheme ())
                                          {
                                              Active = new Attribute (selectedColor,
                                                                      editor.ViewToEdit.GetAttributeForRole (VisualRole.Active).Background,
                                                                      editor.ViewToEdit.GetAttributeForRole (VisualRole.Active).Style)
                                          };
                                          editor.ViewToEdit.SetScheme (cs);
                                      };

        var bgColorPickerBtn = new Button { X = Pos.Center (), Y = Pos.Align (Alignment.Start), Text = "Background HotNormal Color" };
        container.Add (bgColorPickerBtn);

        bgColorPickerBtn.Accepting += (_, _) =>
                                      {
                                          Color? result =
                                              app.TopRunnable?.Prompt<ColorPicker, Color?> (input: editor.ViewToEdit!.GetAttributeForRole (VisualRole.Normal)
                                                                                                         .Background,
                                                                                            beginInitHandler: prompt => { prompt.Title = "Background Color"; });

                                          if (result is not { } selectedColor)
                                          {
                                              return;
                                          }

                                          var cs = new Scheme (editor.ViewToEdit.GetScheme ())
                                          {
                                              Active = new Attribute (editor.ViewToEdit.GetAttributeForRole (VisualRole.Active).Foreground,
                                                                      selectedColor,
                                                                      editor.ViewToEdit.GetAttributeForRole (VisualRole.Active).Style)
                                          };
                                          editor.ViewToEdit.SetScheme (cs);
                                      };

        #endregion

        List<ProgressBarFormat> pbFormatEnum = Enum.GetValues (typeof (ProgressBarFormat)).Cast<ProgressBarFormat> ().ToList ();

        OptionSelector<ProgressBarFormat> osPbFormat = new ()
        {
            BorderStyle = LineStyle.Single,
            Title = "ProgressBarFormat",
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            AssignHotKeys = true
        };
        container.Add (osPbFormat);

        var button = new Button { X = Pos.Center (), Y = Pos.Align (Alignment.Start), Text = "Start timer" };
        container.Add (button);

        CheckBox ckbSyncWithTerminal = new ()
        {
            X = Pos.Center (), Y = Pos.Align (Alignment.Start), Text = "Sync with terminal progress indicator"
        };
        container.Add (ckbSyncWithTerminal);

        CheckBox ckbHideSelectedProgressBar = new () { X = Pos.Center (), Y = Pos.Align (Alignment.Start), Text = "Hide selected ProgressBar" };
        container.Add (ckbHideSelectedProgressBar);

        ProgressBar blocksPb = new ()
        {
            Title = "Blocks",
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            Width = Dim.Percent (50),
            BorderStyle = LineStyle.Single,
            CanFocus = true,
            SyncWithTerminal = true
        };
        container.Add (blocksPb);

        osPbFormat.Value = blocksPb.ProgressBarFormat;

        ProgressBar continuousPb = new ()
        {
            Title = "Continuous",
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            Width = Dim.Percent (50),
            ProgressBarStyle = ProgressBarStyle.Continuous,
            BorderStyle = LineStyle.Single,
            CanFocus = true
        };
        container.Add (continuousPb);

        button.Accepting += (_, _) =>
                            {
                                if (_fractionTimer != null)
                                {
                                    return;
                                }

                                //blocksPB.Enabled = false;
                                blocksPb.Fraction = 0;
                                continuousPb.Fraction = 0;
                                float fractionSum = 0;

                                _fractionTimer = new Timer (_ =>
                                                            {
                                                                fractionSum += FRACTION_STEP;
                                                                blocksPb.Fraction = fractionSum;
                                                                continuousPb.Fraction = fractionSum;

                                                                if (!(fractionSum > 1))
                                                                {
                                                                    return;
                                                                }
                                                                _fractionTimer?.Dispose ();
                                                                _fractionTimer = null;
                                                                button.Enabled = true;
                                                            },
                                                            null,
                                                            0,
                                                            TIMER_TICK);
                            };

        var ckbBidirectional = new CheckBox { X = Pos.Center (), Y = Pos.Bottom (continuousPb), Text = "BidirectionalMarquee", Value = CheckState.Checked };
        container.Add (ckbBidirectional);

        ProgressBar marqueesBlocksPb = new ()
        {
            Title = "Marquee Blocks",
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            Width = Dim.Percent (50),
            ProgressBarStyle = ProgressBarStyle.MarqueeBlocks,
            BorderStyle = LineStyle.Single,
            CanFocus = true
        };
        container.Add (marqueesBlocksPb);

        ProgressBar marqueesContinuousPb = new ()
        {
            Title = "Marquee Continuous",
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            Width = Dim.Percent (50),
            ProgressBarStyle = ProgressBarStyle.MarqueeContinuous,
            BorderStyle = LineStyle.Single,
            CanFocus = true
        };
        container.Add (marqueesContinuousPb);

        _pbList.SetSource (new ObservableCollection<string> (container.SubViews.Where (v => v.GetType () == typeof (ProgressBar))
                                                                      .Select (v => v.Title)
                                                                      .ToList ()));

        _pbList.ValueChanged += (_, e) =>
                                {
                                    if (e.NewValue is null)
                                    {
                                        return;
                                    }
                                    var title = (string)_pbList.Source!.ToList () [e.NewValue.Value]!;
                                    var progressBar = (ProgressBar)container.SubViews.First (v => v.GetType () == typeof (ProgressBar) && v.Title == title);
                                    editor.ViewToEdit = progressBar;
                                     ckbSyncWithTerminal.Value = progressBar.SyncWithTerminal ? CheckState.Checked : CheckState.UnChecked;
                                    ckbHideSelectedProgressBar.Value = progressBar.Visible ? CheckState.UnChecked : CheckState.Checked;
                                };

        osPbFormat.ValueChanged += (_, e) =>
                                   {
                                       if (e.Value is null)
                                       {
                                           return;
                                       }

                                       blocksPb.ProgressBarFormat = e.Value.Value;
                                       continuousPb.ProgressBarFormat = e.Value.Value;
                                       marqueesBlocksPb.ProgressBarFormat = e.Value.Value;
                                       marqueesContinuousPb.ProgressBarFormat = e.Value.Value;
                                   };

        ckbSyncWithTerminal.ValueChanging += (_, e) =>
                                             {
                                                 if (editor.ViewToEdit is not ProgressBar progressBar)
                                                 {
                                                     return;
                                                 }

                                                 progressBar.SyncWithTerminal = e.NewValue == CheckState.Checked;
                                             };

        ckbHideSelectedProgressBar.ValueChanging += (_, e) =>
                                                    {
                                                        if (editor.ViewToEdit is not ProgressBar progressBar)
                                                        {
                                                            return;
                                                        }

                                                        progressBar.Visible = e.NewValue != CheckState.Checked;
                                                    };

        ckbBidirectional.ValueChanging += (_, e) =>
                                          {
                                              marqueesBlocksPb.BidirectionalMarquee =
                                                  marqueesContinuousPb.BidirectionalMarquee = e.NewValue == CheckState.Checked;
                                          };

        win.Initialized += Win_Initialized;
        win.IsRunningChanged += WinIsRunningChanged;

        _pulseTimer = new Timer (_ =>
                                 {
                                     marqueesBlocksPb.Text = marqueesContinuousPb.Text = DateTime.Now.TimeOfDay.ToString ();
                                     marqueesBlocksPb.Pulse ();
                                     marqueesContinuousPb.Pulse ();
                                 },
                                 null,
                                 0,
                                 300);
        app.Run (win);

        return;

        void WinIsRunningChanged (object sender, EventArgs<bool> args)
        {
            if (args.Value)
            {
                return;
            }

            if (_fractionTimer != null)
            {
                _fractionTimer.Dispose ();
                _fractionTimer = null;
            }

            if (_pulseTimer != null)
            {
                _pulseTimer.Dispose ();
                _pulseTimer = null;
            }

            win.IsRunningChanged -= WinIsRunningChanged;
        }
    }

    private void Win_Initialized (object sender, EventArgs e) => _pbList.SelectedItem = 0;
}

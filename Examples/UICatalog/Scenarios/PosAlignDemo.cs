namespace UICatalog.Scenarios;

[ScenarioMetadata ("Pos.Align", "Demonstrates Pos.Align")]
[ScenarioCategory ("Layout")]
public sealed class PosAlignDemo : Scenario
{
    private readonly Aligner _horizAligner = new () { AlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems };
    private int _leftMargin;
    private readonly Aligner _vertAligner = new () { AlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems };
    private int _topMargin;

    public override void Main ()
    {

        // Init
        using IApplication app = Application.Create ();
        app.Init ();

        // Setup - Create a top-level application window and configure it.
        using Window appWindow = new ();
        appWindow.Title = $"{Application.GetDefaultKey (Command.Quit)} to Quit - Scenario: {GetName ()} - {GetDescription ()}";

        SetupControls (appWindow, Dimension.Width, Schemes.Accent);

        SetupControls (appWindow, Dimension.Height, Schemes.Error);

        Setup3By3Grid (appWindow);

        // Run - Start the application.
        app.Run (appWindow);
    }

    private void SetupControls (Window appWindow, Dimension dimension, Schemes scheme)
    {
        OptionSelector<Alignment> alignOptionSelector = new () { SchemeName = SchemeManager.SchemesToSchemeName (scheme) };

        if (dimension == Dimension.Width)
        {
            alignOptionSelector.X = Pos.Align (_horizAligner.Alignment);
            alignOptionSelector.Y = Pos.Center ();
        }
        else
        {
            alignOptionSelector.X = Pos.Center ();
            alignOptionSelector.Y = Pos.Align (_vertAligner.Alignment);
        }

        alignOptionSelector.ValueChanged += (_, _) =>
                                            {
                                                if (alignOptionSelector.Value is null)
                                                {
                                                    return;
                                                }

                                                if (dimension == Dimension.Width)
                                                {
                                                    _horizAligner.Alignment = alignOptionSelector.Value.Value;

                                                    UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                                                }
                                                else
                                                {
                                                    _vertAligner.Alignment = alignOptionSelector.Value.Value;
                                                    UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                                                }
                                            };
        appWindow.Add (alignOptionSelector);

        CheckBox endToStartCheckBox = new () { SchemeName = SchemeManager.SchemesToSchemeName (scheme), Text = "EndToStart" };

        if (dimension == Dimension.Width)
        {
            endToStartCheckBox.Value = _horizAligner.AlignmentModes.HasFlag (AlignmentModes.EndToStart) ? CheckState.Checked : CheckState.UnChecked;
            endToStartCheckBox.X = Pos.Align (_horizAligner.Alignment);
            endToStartCheckBox.Y = Pos.Top (alignOptionSelector);
        }
        else
        {
            endToStartCheckBox.Value = _vertAligner.AlignmentModes.HasFlag (AlignmentModes.EndToStart) ? CheckState.Checked : CheckState.UnChecked;
            endToStartCheckBox.X = Pos.Left (alignOptionSelector);
            endToStartCheckBox.Y = Pos.Align (_vertAligner.Alignment);
        }

        endToStartCheckBox.ValueChanging += (_, e) =>
                                            {
                                                if (dimension == Dimension.Width)
                                                {
                                                    _horizAligner.AlignmentModes = e.NewValue == CheckState.Checked
                                                                                       ? _horizAligner.AlignmentModes | AlignmentModes.EndToStart
                                                                                       : _horizAligner.AlignmentModes & ~AlignmentModes.EndToStart;
                                                    UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                                                }
                                                else
                                                {
                                                    _vertAligner.AlignmentModes = e.NewValue == CheckState.Checked
                                                                                      ? _vertAligner.AlignmentModes | AlignmentModes.EndToStart
                                                                                      : _vertAligner.AlignmentModes & ~AlignmentModes.EndToStart;
                                                    UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                                                }
                                            };
        appWindow.Add (endToStartCheckBox);

        CheckBox ignoreFirstOrLast = new () { SchemeName = SchemeManager.SchemesToSchemeName (scheme), Text = "IgnoreFirstOrLast" };

        if (dimension == Dimension.Width)
        {
            ignoreFirstOrLast.Value = _horizAligner.AlignmentModes.HasFlag (AlignmentModes.IgnoreFirstOrLast) ? CheckState.Checked : CheckState.UnChecked;
            ignoreFirstOrLast.X = Pos.Align (_horizAligner.Alignment);
            ignoreFirstOrLast.Y = Pos.Top (alignOptionSelector);
        }
        else
        {
            ignoreFirstOrLast.Value = _vertAligner.AlignmentModes.HasFlag (AlignmentModes.IgnoreFirstOrLast) ? CheckState.Checked : CheckState.UnChecked;
            ignoreFirstOrLast.X = Pos.Left (alignOptionSelector);
            ignoreFirstOrLast.Y = Pos.Align (_vertAligner.Alignment);
        }

        ignoreFirstOrLast.ValueChanging += (_, e) =>
                                           {
                                               if (dimension == Dimension.Width)
                                               {
                                                   _horizAligner.AlignmentModes = e.NewValue == CheckState.Checked
                                                                                      ? _horizAligner.AlignmentModes | AlignmentModes.IgnoreFirstOrLast
                                                                                      : _horizAligner.AlignmentModes & ~AlignmentModes.IgnoreFirstOrLast;
                                                   UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                                               }
                                               else
                                               {
                                                   _vertAligner.AlignmentModes = e.NewValue == CheckState.Checked
                                                                                     ? _vertAligner.AlignmentModes | AlignmentModes.IgnoreFirstOrLast
                                                                                     : _vertAligner.AlignmentModes & ~AlignmentModes.IgnoreFirstOrLast;
                                                   UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                                               }
                                           };
        appWindow.Add (ignoreFirstOrLast);

        CheckBox addSpacesBetweenItems = new () { SchemeName = SchemeManager.SchemesToSchemeName (scheme), Text = "AddSpaceBetweenItems" };

        if (dimension == Dimension.Width)
        {
            addSpacesBetweenItems.Value =
                _horizAligner.AlignmentModes.HasFlag (AlignmentModes.AddSpaceBetweenItems) ? CheckState.Checked : CheckState.UnChecked;
            addSpacesBetweenItems.X = Pos.Align (_horizAligner.Alignment);
            addSpacesBetweenItems.Y = Pos.Top (alignOptionSelector);
        }
        else
        {
            addSpacesBetweenItems.Value = _vertAligner.AlignmentModes.HasFlag (AlignmentModes.AddSpaceBetweenItems) ? CheckState.Checked : CheckState.UnChecked;
            addSpacesBetweenItems.X = Pos.Left (alignOptionSelector);
            addSpacesBetweenItems.Y = Pos.Align (_vertAligner.Alignment);
        }

        addSpacesBetweenItems.ValueChanging += (_, e) =>
                                               {
                                                   if (dimension == Dimension.Width)
                                                   {
                                                       _horizAligner.AlignmentModes = e.NewValue == CheckState.Checked
                                                                                          ? _horizAligner.AlignmentModes | AlignmentModes.AddSpaceBetweenItems
                                                                                          : _horizAligner.AlignmentModes & ~AlignmentModes.AddSpaceBetweenItems;
                                                       UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                                                   }
                                                   else
                                                   {
                                                       _vertAligner.AlignmentModes = e.NewValue == CheckState.Checked
                                                                                         ? _vertAligner.AlignmentModes | AlignmentModes.AddSpaceBetweenItems
                                                                                         : _vertAligner.AlignmentModes & ~AlignmentModes.AddSpaceBetweenItems;
                                                       UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                                                   }
                                               };

        appWindow.Add (addSpacesBetweenItems);

        CheckBox margin = new () { SchemeName = SchemeManager.SchemesToSchemeName (scheme), Text = "Margin" };

        if (dimension == Dimension.Width)
        {
            margin.X = Pos.Align (_horizAligner.Alignment);
            margin.Y = Pos.Top (alignOptionSelector);
        }
        else
        {
            margin.X = Pos.Left (addSpacesBetweenItems);
            margin.Y = Pos.Align (_vertAligner.Alignment);
        }

        margin.ValueChanging += (_, e) =>
                                {
                                    if (dimension == Dimension.Width)
                                    {
                                        _leftMargin = e.NewValue == CheckState.Checked ? 1 : 0;
                                        UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                                    }
                                    else
                                    {
                                        _topMargin = e.NewValue == CheckState.Checked ? 1 : 0;
                                        UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                                    }
                                };
        appWindow.Add (margin);

        List<Button> addedViews =
        [
            new ()
            {
                X = dimension == Dimension.Width ? Pos.Align (_horizAligner.Alignment) : Pos.Left (alignOptionSelector),
                Y = dimension == Dimension.Width ? Pos.Top (alignOptionSelector) : Pos.Align (_vertAligner.Alignment),
                Text = NumberToWords.Convert (0)
            }
        ];

        NumericUpDown<int> addedViewsUpDown = new ()
        {
            Width = 9,
            Title = "Added",
            SchemeName = SchemeManager.SchemesToSchemeName (scheme),
            BorderStyle = LineStyle.None,
            Value = addedViews.Count
        };

        if (dimension == Dimension.Width)
        {
            addedViewsUpDown.X = Pos.Align (_horizAligner.Alignment);
            addedViewsUpDown.Y = Pos.Top (alignOptionSelector);
            addedViewsUpDown.Border.Thickness = new Thickness (0, 1, 0, 0);
        }
        else
        {
            addedViewsUpDown.X = Pos.Left (alignOptionSelector);
            addedViewsUpDown.Y = Pos.Align (_vertAligner.Alignment);
            addedViewsUpDown.Border.Thickness = new Thickness (1, 0, 0, 0);
        }

        addedViewsUpDown.ValueChanging += (_, e) =>
                                          {
                                              if (e.NewValue < 0)
                                              {
                                                  e.Handled = true;

                                                  return;
                                              }

                                              int newValue = e.NewValue;
                                              int currentValue = e.CurrentValue;

                                              // Add or remove buttons
                                              if (newValue < currentValue)
                                              {
                                                  // Remove buttons
                                                  for (int i = currentValue - 1; i >= newValue; i--)
                                                  {
                                                      Button button = addedViews [i];
                                                      appWindow.Remove (button);
                                                      addedViews.RemoveAt (i);
                                                      button.Dispose ();
                                                  }
                                              }

                                              if (newValue > currentValue)
                                              {
                                                  // Add buttons
                                                  for (int i = currentValue; i < newValue; i++)
                                                  {
                                                      Button button = new ()
                                                      {
                                                          X = dimension == Dimension.Width
                                                                  ? Pos.Align (_horizAligner.Alignment)
                                                                  : Pos.Left (alignOptionSelector),
                                                          Y = dimension == Dimension.Width
                                                                  ? Pos.Top (alignOptionSelector)
                                                                  : Pos.Align (_vertAligner.Alignment),
                                                          Text = NumberToWords.Convert (i + 1)
                                                      };
                                                      appWindow.Add (button);
                                                      addedViews.Add (button);
                                                  }
                                              }
                                          };
        appWindow.Add (addedViewsUpDown);

        appWindow.Add (addedViews [0]);
    }

    private void UpdatePosAlignObjects (View superView, Dimension dimension, Aligner aligner)
    {
        foreach (View view in superView.SubViews.Where (v => dimension == Dimension.Width ? v.X is PosAlign : v.Y is PosAlign))
        {
            if (dimension == Dimension.Width ? view.X is not PosAlign : view.Y is not PosAlign)
            {
                continue;
            }

            // BUGBUG: Create and assign a new Pos object because we currently have no way for X to be notified
            // BUGBUG: of changes in the Pos object. See https://github.com/tui-cs/Terminal.Gui/issues/3485
            if (dimension == Dimension.Width)
            {
                var posAlign = view.X as PosAlign;

                view.X = Pos.Align (aligner.Alignment, aligner.AlignmentModes, posAlign!.GroupId);
                view.Margin.Thickness = new Thickness (_leftMargin, view.Margin.Thickness.Top, view.Margin.Thickness.Right, view.Margin.Thickness.Bottom);
            }
            else
            {
                var posAlign = view.Y as PosAlign;

                view.Y = Pos.Align (aligner.Alignment, aligner.AlignmentModes, posAlign!.GroupId);

                view.Margin.Thickness = new Thickness (view.Margin.Thickness.Left, _topMargin, view.Margin.Thickness.Right, view.Margin.Thickness.Bottom);
            }
        }
    }

    /// <summary>
    ///     Creates a 3x3 grid of views with two GroupIds: One for aligning X and one for aligning Y.
    ///     Demonstrates using PosAlign to create a grid of views that flow.
    /// </summary>
    /// <param name="appWindow"></param>
    private void Setup3By3Grid (View appWindow)
    {
        var container = new FrameView
        {
            Title = "3 by 3",
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Width = Dim.Percent (40),
            Height = Dim.Percent (40)
        };
        container.Padding.Thickness = new Thickness (8, 1, 0, 0);
        container.Padding.GetOrCreateView ().SchemeName = "Error";

        Aligner widthAligner = new () { AlignmentModes = AlignmentModes.StartToEnd };

        OptionSelector widthAlignOptionSelector = new () { Labels = Enum.GetNames<Alignment> (), Orientation = Orientation.Horizontal, X = Pos.Center () };
        container.Padding.GetOrCreateView ().Add (widthAlignOptionSelector);

        widthAlignOptionSelector.ValueChanged += (_, _) =>
                                                 {
                                                     widthAligner.Alignment =
                                                         (Alignment)Enum.Parse (typeof (Alignment),
                                                                                widthAlignOptionSelector.Labels [widthAlignOptionSelector.Value!.Value]);
                                                     UpdatePosAlignObjects (container, Dimension.Width, widthAligner);
                                                 };

        Aligner heightAligner = new () { AlignmentModes = AlignmentModes.StartToEnd };

        OptionSelector heightAlignOptionSelector = new () { Labels = Enum.GetNames<Alignment> (), Orientation = Orientation.Vertical, Y = Pos.Center () };
        container.Padding.GetOrCreateView ().Add (heightAlignOptionSelector);

        heightAlignOptionSelector.ValueChanged += (_, _) =>
                                                  {
                                                      heightAligner.Alignment =
                                                          (Alignment)Enum.Parse (typeof (Alignment),
                                                                                 heightAlignOptionSelector.Labels [heightAlignOptionSelector.Value!.Value]);
                                                      UpdatePosAlignObjects (container, Dimension.Height, heightAligner);
                                                  };

        for (var i = 0; i < 9; i++)

        {
            var v = new View
            {
                Title = $"{i}",
                Text = $"{i}",
                BorderStyle = LineStyle.Dashed,
                Height = Dim.Auto (),
                Width = Dim.Auto () + 2
            };

            v.X = Pos.Align (widthAligner.Alignment, widthAligner.AlignmentModes, i / 3);
            v.Y = Pos.Align (heightAligner.Alignment, heightAligner.AlignmentModes, i % 3 + 10);

            container.Add (v);
        }

        appWindow.Add (container);
    }
}

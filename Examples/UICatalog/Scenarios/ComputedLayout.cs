using Terminal.Gui.Editor;

namespace UICatalog.Scenarios;

/// <summary>
///     This Scenario demonstrates how to use Terminal.Gui's Dim and Pos Layout System.
/// </summary>
[ScenarioMetadata ("Computed Layout", "Demonstrates the Computed (Dim and Pos) Layout System.")]
[ScenarioCategory ("Layout")]
public class ComputedLayout : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new () { Title = GetQuitKeyAndName () };

        // Demonstrate using Dim to create a horizontal ruler that always measures the parent window's width
        const string rule = "|123456789";

        Label horizontalRuler = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = 1,
            SchemeName = "Error",
            Text = rule
        };

        window.Add (horizontalRuler);

        // Demonstrate using Dim to create a vertical ruler that always measures the parent window's height
        const string vrule = "|\n1\n2\n3\n4\n5\n6\n7\n8\n9\n";

        Label verticalRuler = new ()
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = Dim.Fill (),
            SchemeName = "Error",
            Text = vrule
        };

        window.SubViewsLaidOut += (_, _) =>
                                  {
                                      if (horizontalRuler.Viewport.Width == 0 || horizontalRuler.Viewport.Height == 0)
                                      {
                                          return;
                                      }

                                      horizontalRuler.Text =
                                          rule.Repeat ((int)Math.Ceiling (horizontalRuler.Viewport.Width / (double)rule.Length))
                                              [..horizontalRuler.Viewport.Width];

                                      verticalRuler.Text =
                                          vrule.Repeat ((int)Math.Ceiling (verticalRuler.Viewport.Height * 2 / (double)rule.Length))
                                              [..(verticalRuler.Viewport.Height * 2)];
                                  };

        window.Add (verticalRuler);

        // Demonstrate At - Using Pos.At to locate a view in an absolute location
        var atButton = new Button { Text = "Absolute(2,1)", X = Pos.Absolute (2), Y = Pos.Absolute (1) };
        window.Add (atButton);

        // Throw in a literal absolute - Should function identically to above
        var absoluteButton = new Button { Text = "X = 30, Y = 1", X = 30, Y = 1 };
        window.Add (absoluteButton);

        // Demonstrate using Dim to create a window that fills the parent with a margin
        var margin = 10;

        Window subWin = new ()
        {
            X = Pos.Center (),
            Y = 2,
            Width = Dim.Fill (margin),
            Height = 7,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped
        };

        subWin.Initialized += (_, _) =>
                              {
                                  subWin.Title = $"{subWin.GetType ().Name} {{X={subWin.X},Y={subWin.Y},Width={subWin.Width},Height={subWin.Height}}}";
                              };
        window.Add (subWin);

        var i = 1;
        var txt = "Resize the terminal to see computed layout in action.";
        List<Label> labelList = new ();
        labelList.Add (new Label { Text = "The lines below show different alignment" });

        labelList.Add (new Label
        {
            TextAlignment = Alignment.Start,
            Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (labelList.LastOrDefault ()),
            SchemeName = "Dialog",
            Text = $"{i++}-{txt}"
        });

        labelList.Add (new Label
        {
            TextAlignment = Alignment.End,
            Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (labelList.LastOrDefault ()),
            SchemeName = "Dialog",
            Text = $"{i++}-{txt}"
        });

        labelList.Add (new Label
        {
            TextAlignment = Alignment.Center,
            Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (labelList.LastOrDefault ()),
            SchemeName = "Dialog",
            Text = $"{i++}-{txt}"
        });

        labelList.Add (new Label
        {
            TextAlignment = Alignment.Fill,
            Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (labelList.LastOrDefault ()),
            SchemeName = "Dialog",
            Text = $"{i++}-{txt}"
        });
        subWin.Add (labelList.ToArray ());

        FrameView frameView = new () { X = 2, Y = Pos.Bottom (subWin), Width = 30, Height = 7 };

        frameView.Initialized += (sender, _) =>
                                 {
                                     var fv = (FrameView)sender!;

                                     fv.Title = $"{frameView.GetType ().Name} {{X={fv.X},Y={fv.Y},Width={fv.Width},Height={fv.Height}}}";
                                 };
        i = 1;
        labelList = new List<Label> ();
        labelList.Add (new Label { Text = "The lines below show different alignment" });

        labelList.Add (new Label
        {
            TextAlignment = Alignment.Start,
            Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (labelList.LastOrDefault ()),
            SchemeName = "Dialog",
            Text = $"{i++}-{txt}"
        });

        labelList.Add (new Label
        {
            TextAlignment = Alignment.End,
            Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (labelList.LastOrDefault ()),
            SchemeName = "Dialog",
            Text = $"{i++}-{txt}"
        });

        labelList.Add (new Label
        {
            TextAlignment = Alignment.Center,
            Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (labelList.LastOrDefault ()),
            SchemeName = "Dialog",
            Text = $"{i++}-{txt}"
        });

        labelList.Add (new Label
        {
            TextAlignment = Alignment.Fill,
            Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (labelList.LastOrDefault ()),
            SchemeName = "Dialog",
            Text = $"{i++}-{txt}"
        });
        frameView.Add (labelList.ToArray ());
        window.Add (frameView);

        frameView = new FrameView { X = Pos.Right (frameView), Y = Pos.Top (frameView), Width = Dim.Fill (), Height = 7 };

        frameView.Initialized += (sender, _) =>
                                 {
                                     var fv = (FrameView)sender!;

                                     fv.Title = $"{frameView.GetType ().Name} {{X={fv.X},Y={fv.Y},Width={fv.Width},Height={fv.Height}}}";
                                 };

        labelList = new List<Label> ();
        labelList.Add (new Label { Text = "The lines below show different alignment" });

        labelList.Add (new Label
        {
            TextAlignment = Alignment.Start,
            Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (labelList.LastOrDefault ()),
            SchemeName = "Dialog",
            Text = $"{i++}-{txt}"
        });

        labelList.Add (new Label
        {
            TextAlignment = Alignment.End,
            Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (labelList.LastOrDefault ()),
            SchemeName = "Dialog",
            Text = $"{i++}-{txt}"
        });

        labelList.Add (new Label
        {
            TextAlignment = Alignment.Center,
            Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (labelList.LastOrDefault ()),
            SchemeName = "Dialog",
            Text = $"{i++}-{txt}"
        });

        labelList.Add (new Label
        {
            TextAlignment = Alignment.Fill,
            Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (labelList.LastOrDefault ()),
            SchemeName = "Dialog",
            Text = $"{i++}-{txt}"
        });
        frameView.Add (labelList.ToArray ());
        window.Add (frameView);

        // Demonstrate Dim & Pos using percentages - a TextField that is 30% height and 80% wide
        Editor editor = new ()
        {
            X = Pos.Center (),
            Y = Pos.Percent (50),
            Width = Dim.Percent (80),
            Height = Dim.Percent (10),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent)
        };

        editor.Text = "This Editor should horizontally & vertically centered and \n10% of the screen height, and 80% of its width.";
        window.Add (editor);

        var oddballButton = new Button { Text = "These buttons demo convoluted PosCombine scenarios", X = Pos.Center (), Y = Pos.Bottom (editor) + 1 };
        window.Add (oddballButton);

        #region Issue2358

        // Demonstrate odd-ball Combine scenarios
        // Until https://github.com/tui-cs/Terminal.Gui/issues/2358 is fixed these won't work right

        oddballButton = new Button { Text = "Center + 0", X = Pos.Center () + 0, Y = Pos.Bottom (oddballButton) };
        window.Add (oddballButton);

        oddballButton = new Button { Text = "Center + 1", X = Pos.Center () + 1, Y = Pos.Bottom (oddballButton) };
        window.Add (oddballButton);

        oddballButton = new Button { Text = "0 + Center", X = 0 + Pos.Center (), Y = Pos.Bottom (oddballButton) };
        window.Add (oddballButton);

        oddballButton = new Button { Text = "1 + Center", X = 1 + Pos.Center (), Y = Pos.Bottom (oddballButton) };
        window.Add (oddballButton);

        oddballButton = new Button { Text = "Center - 1", X = Pos.Center () - 1, Y = Pos.Bottom (oddballButton) };
        window.Add (oddballButton);

        // This demonstrates nonsense: it the same as using Pos.AnchorEnd (100/2=50 + 100/2=50 = 100 - 50)
        // The `- Pos.Percent(5)` is there so at least something is visible
        oddballButton = new Button
        {
            Text = "Center + Center - Percent(50)", X = Pos.Center () + Pos.Center () - Pos.Percent (50), Y = Pos.Bottom (oddballButton)
        };
        window.Add (oddballButton);

        // This demonstrates nonsense: it the same as using Pos.AnchorEnd (100/2=50 + 100/2=50 = 100 - 50)
        // The `- Pos.Percent(5)` is there so at least something is visible
        oddballButton = new Button
        {
            Text = "Percent(50) + Center - Percent(50)", X = Pos.Percent (50) + Pos.Center () - Pos.Percent (50), Y = Pos.Bottom (oddballButton)
        };
        window.Add (oddballButton);

        // This demonstrates nonsense: it the same as using Pos.AnchorEnd (100/2=50 + 100/2=50 = 100 - 50)
        // The `- Pos.Percent(5)` is there so at least something is visible
        oddballButton = new Button
        {
            Text = "Center + Percent(50) - Percent(50)", X = Pos.Center () + Pos.Percent (50) - Pos.Percent (50), Y = Pos.Bottom (oddballButton)
        };
        window.Add (oddballButton);

        #endregion

        // This demonstrates nonsense: Same as At(0)
        oddballButton = new Button
        {
            Text = "Center - Center - Percent(50)", X = Pos.Center () + Pos.Center () - Pos.Percent (50), Y = Pos.Bottom (oddballButton)
        };
        window.Add (oddballButton);

        // This demonstrates combining Percents)
        oddballButton = new Button { Text = "Percent(40) + Percent(10)", X = Pos.Percent (40) + Pos.Percent (10), Y = Pos.Bottom (oddballButton) };
        window.Add (oddballButton);

        // Demonstrate AnchorEnd - Button is anchored to bottom/right
        var anchorButton = new Button { Text = "Button using AnchorEnd", Y = Pos.AnchorEnd () };
        anchorButton.X = Pos.AnchorEnd ();

        anchorButton.Accepting += (_, _) =>
                                  {
                                      // This demonstrates how to have a dynamically sized button
                                      // Each time the button is clicked the button's text gets longer
                                      // The call to app.LayoutSubViews causes the Computed layout to
                                      // get updated.
                                      anchorButton.Text += "!";
                                  };
        window.Add (anchorButton);

        // Demonstrate AnchorEnd(n) 
        // This is intentionally convoluted to illustrate potential bugs.
        var anchorEndLabel1 = new Label
        {
            Text = "This Label should be the 3rd to last line (AnchorEnd (3)).",
            TextAlignment = Alignment.Center,
            SchemeName = "Menu",
            Width = Dim.Fill (5),
            X = 5,
            Y = Pos.AnchorEnd (3)
        };
        window.Add (anchorEndLabel1);

        // Demonstrate DimCombine (via AnchorEnd(n) - 1)
        // This is intentionally convoluted to illustrate potential bugs.
        var anchorEndLabel2 = new TextField
        {
            Text = "This TextField should be the 4th to last line (AnchorEnd (3) - 1).",
            TextAlignment = Alignment.Start,
            SchemeName = "Menu",
            Width = Dim.Fill (5),
            X = 5,
            Y = Pos.AnchorEnd (3) - 1 // Pos.Combine
        };
        window.Add (anchorEndLabel2);

        // Demonstrate AnchorEnd() in combination with Pos.Align to align a set of buttons centered across the
        // bottom - 1
        // This is intentionally convoluted to illustrate potential bugs.
        var leftButton = new Button { Text = "Left", X = Pos.Align (Alignment.Center), Y = Pos.AnchorEnd () - 1 };

        leftButton.Accepting += (_, _) =>
                                {
                                    // This demonstrates how to have a dynamically sized button
                                    // Each time the button is clicked the button's text gets longer
                                    leftButton.Text += "!";
                                };

        // show positioning vertically using Pos.AnchorEnd
        var centerButton = new Button { Text = "Center", X = Pos.Align (Alignment.Center), Y = Pos.AnchorEnd (2) };

        centerButton.Accepting += (_, _) =>
                                  {
                                      // This demonstrates how to have a dynamically sized button
                                      // Each time the button is clicked the button's text gets longer
                                      centerButton.Text += "!";
                                  };

        // show positioning vertically using another window and Pos.Bottom
        var rightButton = new Button { Text = "Right", X = Pos.Align (Alignment.Center), Y = Pos.Y (centerButton) };

        rightButton.Accepting += (_, _) =>
                                 {
                                     // This demonstrates how to have a dynamically sized button
                                     // Each time the button is clicked the button's text gets longer
                                     rightButton.Text += "!";
                                 };

        View [] buttons = { leftButton, centerButton, rightButton };
        window.Add (leftButton);
        window.Add (centerButton);
        window.Add (rightButton);

        app.Run (window);
    }
}

#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Text Styles", "Shows Attribute.TextStyles including bold, italic, etc...")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Colors")]
public sealed class TextStyles : Scenario
{
    private CheckBox? _drawDirectly;

    public override void Main ()
    {

        // Init
        using IApplication app = Application.Create ();
        app.Init ();

        // Setup - Create a top-level application window and configure it.
        using Window appWindow = new ();
        appWindow.Id = "appWindow";
        appWindow.Title = GetQuitKeyAndName ();

        //appWindow.ContentSizeTracksViewport = false;
        appWindow.ViewportSettings |= ViewportSettingsFlags.HasScrollBars;

        appWindow.SubViewsLaidOut += (sender, _) =>
                                     {
                                         if (sender is View sendingView)
                                         {
                                             sendingView.SetContentHeight (sendingView.GetHeightRequiredForSubViews ());
                                         }
                                     };

        appWindow.DrawingContent += OnAppWindowOnDrawingContent;
        appWindow.DrawingSubViews += OnAppWindowOnDrawingSubviews;

        _drawDirectly = new () { Title = "_Draw styled text directly using DrawingContent vs. Buttons", Value = CheckState.UnChecked };

        appWindow.Add (_drawDirectly);
        AddButtons (appWindow);

        // Run - Start the application.
        app.Run (appWindow);
    }

    private void AddButtons (Window appWindow)
    {
        var y = 1;

        TextStyle [] allStyles = Enum.GetValues (typeof (TextStyle)).Cast<TextStyle> ().Where (style => style != TextStyle.None).ToArray ();

        // Add individual flags as labels
        foreach (TextStyle style in allStyles)
        {
            y++;

            var button = new Button
            {
                X = 0, Y = y, Title = $"{Enum.GetName (typeof (TextStyle), style)}", Visible = _drawDirectly!.Value != CheckState.Checked
            };

            button.GettingAttributeForRole += (sender, args) =>
                                              {
                                                  if (sender is not Button)
                                                  {
                                                      return;
                                                  }

                                                  if (args.Result is { })
                                                  {
                                                      args.Result = args.Result.Value with { Style = style };
                                                  }

                                                  args.Handled = true;
                                              };

            appWindow.Add (button);
        }

        // Add a blank line
        y += 1;

        // Generate all combinations of TextStyle (excluding individual flags)
        int totalCombinations = 1 << allStyles.Length; // 2^n combinations

        for (var i = 1; i < totalCombinations; i++) // Start from 1 to skip "None"
        {
            var combination = (TextStyle)0;
            List<string> styleNames = [];

            for (var bit = 0; bit < allStyles.Length; bit++)
            {
                if ((i & (1 << bit)) == 0)
                {
                    continue;
                }
                combination |= allStyles [bit];
                styleNames.Add (Enum.GetName (typeof (TextStyle), allStyles [bit])!);
            }

            // Skip individual flags
            if (styleNames.Count == 1)
            {
                continue;
            }

            y++;

            var button = new Button { X = 0, Y = y, Text = $"[{string.Join (" | ", styleNames)}]", Visible = _drawDirectly!.Value != CheckState.Checked };

            button.GettingAttributeForRole += (_, args) =>
                                              {
                                                  if (args.Result is { })
                                                  {
                                                      args.Result = args.Result.Value with { Style = combination };
                                                  }

                                                  args.Handled = true;
                                              };
            appWindow.Add (button);
        }
    }

    private void OnAppWindowOnDrawingSubviews (object? sender, DrawEventArgs e)
    {
        if (sender is not View sendingView)
        {
            return;
        }

        foreach (Button view in sendingView.SubViews.OfType<Button> ())
        {
            view.Visible = _drawDirectly!.Value != CheckState.Checked;
        }

        e.Cancel = false;
    }

    private void OnAppWindowOnDrawingContent (object? sender, DrawEventArgs args)
    {
        if (sender is not View { } sendingView || _drawDirectly!.Value != CheckState.Checked)
        {
            return;
        }
        int y = 2 - args.NewViewport.Y; // Start drawing below the checkbox

        TextStyle [] allStyles = Enum.GetValues (typeof (TextStyle)).Cast<TextStyle> ().Where (style => style != TextStyle.None).ToArray ();

        // Draw individual flags, one per line
        foreach (TextStyle style in allStyles)
        {
            string text = Enum.GetName (typeof (TextStyle), style)!;

            sendingView.Move (0, y);

            var attr = new Attribute (sendingView.GetAttributeForRole (VisualRole.Normal)) { Style = style };
            sendingView.SetAttribute (attr);
            sendingView.AddStr (text);

            y++; // Move to the next line
        }

        // Add a blank line
        y++;

        // Generate all combinations of TextStyle (excluding individual flags)
        int totalCombinations = 1 << allStyles.Length; // 2^n combinations

        for (var i = 1; i < totalCombinations; i++) // Start from 1 to skip "None"
        {
            var combination = (TextStyle)0;
            List<string> styleNames = [];

            for (var bit = 0; bit < allStyles.Length; bit++)
            {
                if ((i & (1 << bit)) == 0)
                {
                    continue;
                }
                combination |= allStyles [bit];
                styleNames.Add (Enum.GetName (typeof (TextStyle), allStyles [bit])!);
            }

            // Skip individual flags
            if (styleNames.Count == 1)
            {
                continue;
            }

            var text = $"[{string.Join (" | ", styleNames)}]";

            sendingView.Move (00, y);

            var attr = new Attribute (sendingView.GetAttributeForRole (VisualRole.Normal)) { Style = combination };
            sendingView.SetAttribute (attr);
            sendingView.AddStr (text);

            y++; // Move to the next line
        }

        args.Cancel = true;
    }
}

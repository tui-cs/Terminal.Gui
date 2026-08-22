#nullable enable
namespace UICatalog.Scenarios;

[ScenarioMetadata ("ViewportSettings", "Demonstrates manipulating Viewport, ViewportSettings, and ContentSize to scroll content")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Scrolling")]
[ScenarioCategory ("Adornments")]
public class ViewportSettings : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window mainWindow = new ();
        mainWindow.Title = GetQuitKeyAndName (); // Use a different colorscheme so ViewSettings.ClearContentOnly is obvious
        mainWindow.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent);
        mainWindow.BorderStyle = LineStyle.None;

        AdornmentsEditor adornmentsEditor = new ()
        {
            BorderStyle = LineStyle.Single, X = Pos.AnchorEnd (), AutoSelectViewToEdit = true, ShowViewIdentifier = true
        };
        mainWindow.Add (adornmentsEditor);

        ViewportSettingsEditor viewportSettingsEditor = new () { Y = Pos.AnchorEnd () };
        mainWindow.Add (viewportSettingsEditor);

        ViewportSettingsDemoView view = new ()
        {
            Title = "ViewportSettings Demo View", Width = Dim.Fill (adornmentsEditor), Height = Dim.Fill (viewportSettingsEditor)
        };

        mainWindow.Add (view);

        // Add demo views to show that things work correctly
        TextField textField = new () { X = 20, Y = 7, Width = 15, Text = "Test Te_xtField" };

        ColorPicker16 colorPicker = new ()
        {
            Title = "_BG",
            BoxHeight = 1,
            BoxWidth = 1,
            X = Pos.AnchorEnd (),
            Y = 10
        };
        colorPicker.BorderStyle = LineStyle.RoundedDotted;

        colorPicker.ValueChanged += (_, e) =>
                                    {
                                        colorPicker.SuperView!.SetScheme (new Scheme (colorPicker.SuperView.GetScheme ())
                                        {
                                            Normal = new Attribute (colorPicker.SuperView
                                                                               .GetAttributeForRole (VisualRole.Normal)
                                                                               .Foreground,
                                                                    e.NewValue)
                                        });
                                    };

        CharMap charMap = new ()
        {
            X = Pos.Center (),
            Y = 17,
            Width = Dim.Auto (DimAutoStyle.Content, maximumContentDim: Dim.Func (_ => view.GetContentWidth ())),
            Height = Dim.Auto (DimAutoStyle.Content, maximumContentDim: Dim.Percent (20))
        };

        charMap.Accepted += (s, _) => MessageBox.Query ((s as View)?.App!, 20, 7, "Hi", $"Am I a {view.GetType ().Name}?", Strings.btnNo, Strings.btnYes);

        Button buttonAnchored = new () { X = Pos.AnchorEnd (), Y = Pos.AnchorEnd (), Text = "Bottom Rig_ht" };
        buttonAnchored.Accepted += (sender, _) => MessageBox.Query ((sender as View)?.App!, "Hi", $"You pressed {((Button)sender!).Text}", Strings.btnOk);

        view.Margin.Thickness = new Thickness (0);
        view.Border.Thickness = new Thickness (3);

        view.Add (buttonAnchored, textField, colorPicker, charMap);

        Label longLabel = new ()
        {
            Id = "label2",
            X = 0,
            Y = 30,
            Text =
                "This label is long. It should clip to the ContentArea if ClipContentOnly is set. This is a virtual scrolling demo. Use the arrow keys and/or mouse wheel to scroll the content."
        };
        longLabel.TextFormatter.WordWrap = true;
        view.Add (longLabel);

        List<object> options = ["Option 1", "Option 2", "Option 3"];

        LinearMultiSelector<object> linearRange = new (options)
        {
            X = 0,
            Y = Pos.Bottom (textField) + 1,
            Orientation = Orientation.Vertical,
            AllowEmpty = false,
            BorderStyle = LineStyle.Double,
            Title = "_LinearRange"
        };
        view.Add (linearRange);

        adornmentsEditor.Initialized += (_, _) => { adornmentsEditor.ViewToEdit = view; };

        adornmentsEditor.AutoSelectViewToEdit = true;
        adornmentsEditor.AutoSelectSuperView = view;
        adornmentsEditor.AutoSelectAdornments = false;

        view.Initialized += (_, _) =>
                            {
                                viewportSettingsEditor.ViewToEdit = view;
                                adornmentsEditor.ViewToEdit = view;
                            };
        view.SetFocus ();
        app.Run (mainWindow);
    }

    public override List<Key> GetDemoKeyStrokes (IApplication? app)
    {
        List<Key> keys = new ();

        for (var i = 0; i < 50; i++)
        {
            keys.Add (Key.CursorRight);
        }

        for (var i = 0; i < 25; i++)
        {
            keys.Add (Key.CursorLeft);
        }

        for (var i = 0; i < 50; i++)
        {
            keys.Add (Key.CursorDown);
        }

        for (var i = 0; i < 25; i++)
        {
            keys.Add (Key.CursorUp);
        }

        return keys;
    }
}

internal class ViewportSettingsDemoView : FrameView
{
    public ViewportSettingsDemoView ()
    {
        Id = "ViewportSettingsDemoView";
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        SchemeName = "base";

        base.Text =
            "Text (ViewportSettingsDemoView.Text). This is long text.\nThe second line.\n3\n4\n5th line\nLine 6. This is a longer line that should wrap automatically.";
        CanFocus = true;
        BorderStyle = LineStyle.Rounded;
        Arrangement = ViewArrangement.Resizable;

        SetContentSize (new Size (60, 40));
        ViewportSettings |= ViewportSettingsFlags.ClearContentOnly;
        ViewportSettings |= ViewportSettingsFlags.ClipContentOnly;
        ViewportSettings |= ViewportSettingsFlags.HasScrollBars;

        // Things this view knows how to do
        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));

        AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));
        AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));

        // Default keybindings for all ListViews
        KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
        KeyBindings.Add (Key.CursorDown, Command.ScrollDown);
        KeyBindings.Add (Key.CursorLeft, Command.ScrollLeft);
        KeyBindings.Add (Key.CursorRight, Command.ScrollRight);

        // Add a status label to the border that shows Viewport and ContentSize values. Bit of a hack.
        // TODO: Move to Padding with controls
        Border.GetOrCreateView ().Add (new Label { X = 20 });

        ViewportChanged += VirtualDemoView_LayoutComplete;

        MouseEvent += VirtualDemoView_MouseEvent;
    }

    // TODO: Use MouseBindings
    private void VirtualDemoView_MouseEvent (object? sender, Mouse e)
    {
        switch (e.Flags)
        {
            case MouseFlags.WheeledDown:
                ScrollVertical (1);

                return;

            case MouseFlags.WheeledUp:
                ScrollVertical (-1);

                return;

            case MouseFlags.WheeledRight:
                ScrollHorizontal (1);

                return;

            case MouseFlags.WheeledLeft:
                ScrollHorizontal (-1);

                break;
        }
    }

    private void VirtualDemoView_LayoutComplete (object? sender, DrawEventArgs drawEventArgs)
    {
        Label? frameLabel = Padding.View?.SubViews.OfType<Label> ().FirstOrDefault ();

        if (frameLabel is { })
        {
            frameLabel.Text = $"Viewport: {Viewport}\nFrame: {Frame}";
        }
    }
}

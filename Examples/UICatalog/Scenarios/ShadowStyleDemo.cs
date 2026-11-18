namespace UICatalog.Scenarios;

[ScenarioMetadata ("ShadowStyle Demo", "Demonstrates ShadowStyle Effects.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Adornments")]
public class ShadowStyleDemo : Scenario
{
    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new ();
        window.Id = "app";
        window.Title = GetQuitKeyAndName ();
        window.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent);

        AdornmentsEditor editor = new ()
        {
            BorderStyle = LineStyle.Single,
            AutoSelectViewToEdit = true,

            // This is for giggles, to show that the editor can be moved around.
            Arrangement = ViewArrangement.Movable,
            Id = "editor"
        };
        editor.Border.Thickness = new Thickness (1, 2, 1, 1);

        Window shadowWindow = new ()
        {
            Id = "shadowWindow",
            X = Pos.Center (),
            Y = 0,
            Width = Dim.Percent (30),
            Height = Dim.Percent (30),
            Title = "Shadow Window",
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped,
            BorderStyle = LineStyle.Double,
            ShadowStyle = ShadowStyles.Transparent
        };

        window.DrawingContent += (_, e) =>
                                 {
                                     window!.FillRect (window!.Viewport, Glyphs.Dot);
                                     e.Cancel = true;
                                 };

        Button buttonInWin = new ()
        {
            Id = "buttonInWin",
            X = Pos.Center (),
            Y = Pos.Center (),
            Text = "Button in Window",
            ShadowStyle = ShadowStyles.Opaque
        };
        shadowWindow.Add (buttonInWin);

        Window shadowWindow2 = new ()
        {
            Id = "shadowWindow2",
            X = Pos.Right (editor) + 10,
            Y = 10,
            Width = Dim.Percent (30),
            Height = Dim.Percent (30),
            Title = "Shadow Window #2",
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Error),
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped,
            BorderStyle = LineStyle.Double,
            ShadowStyle = ShadowStyles.Transparent
        };

        Button button = new ()
        {
            Id = "button",
            X = Pos.Right (editor) + 10,
            Y = Pos.Center (),
            Text = "Button",
            ShadowStyle = ShadowStyles.Opaque
        };
        button.Accepting += ButtonOnAccepting;

        ColorPicker colorPicker = new ()
        {
            Title = "ColorPicker to illustrate highlight (currently broken)",
            BorderStyle = LineStyle.Dotted,
            Id = "colorPicker",
            X = Pos.Center (),
            Y = Pos.AnchorEnd (),
            Width = Dim.Percent (80)
        };

        colorPicker.ValueChanged += (_, args) =>
                                    {
                                        Attribute normal = window.GetScheme ().Normal;

                                        window.SetScheme (window.GetScheme () with
                                        {
                                            Normal = new Attribute (normal.Foreground, args.NewValue ?? Color.Black)
                                        });
                                    };
        window.Add (button, colorPicker, editor, shadowWindow, shadowWindow2);

        editor.ShowViewIdentifier = true;
        editor.AutoSelectViewToEdit = true;
        editor.AutoSelectSuperView = window;
        editor.AutoSelectAdornments = true;

        app.Run (window);
    }

    private void ButtonOnAccepting (object sender, CommandEventArgs e)
    {
        MessageBox.Query ((sender as View)?.App!, "Hello", "You pushed the button!");
        e.Handled = true;
    }
}

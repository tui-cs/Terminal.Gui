
// ReSharper disable AccessToModifiedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Navigation", "Navigation Tester")]
[ScenarioCategory ("Mouse and Keyboard")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Navigation")]
public class Navigation : Scenario
{
    private int _hotkeyCount;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new ();
        window.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);
        window.Title = GetQuitKeyAndName ();
        window.TabStop = TabBehavior.TabGroup;

        AdornmentsEditor adornmentsEditor = new ()
        {
            X = 0,
            Y = 0,
            AutoSelectViewToEdit = true,
            ShowViewIdentifier = true,
            TabStop = TabBehavior.NoStop
        };
        window.Add (adornmentsEditor);

        ArrangementEditor arrangementEditor = new ()
        {
            X = Pos.Right (adornmentsEditor),
            Y = 0,

            //Height = Dim.Fill(),
            AutoSelectViewToEdit = true,
            TabStop = TabBehavior.NoStop
        };
        window.Add (arrangementEditor);

        FrameView testFrame = new ()
        {
            Title = "_1 Test Frame",
            X = Pos.Right (arrangementEditor),
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        window.Add (testFrame);

        Button button = new () { X = 0, Y = 0, Title = $"TopButton _{GetNextHotKey ()}" };
        button.Accepted += (_, _) => MessageBox.Query (app, "hi", button.Title, Strings.btnOk);

        testFrame.Add (button);

        View tiledView1 = CreateTiledView (0, 2, 2);
        View tiledView2 = CreateTiledView (1, Pos.Right (tiledView1), Pos.Top (tiledView1));

        testFrame.Add (tiledView1);
        testFrame.Add (tiledView2);

        View tiledView3 = CreateTiledView (1, Pos.Right (tiledView2), Pos.Top (tiledView2));
        tiledView3.TabStop = TabBehavior.TabGroup;
        tiledView3.BorderStyle = LineStyle.Double;
        testFrame.Add (tiledView3);

        View overlappedView1 = CreateOverlappedView (2, 10, Pos.Center ());
        View tiledSubView = CreateTiledView (4, 0, 2);
        overlappedView1.Add (tiledSubView);

        ProgressBar progressBar = new ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (),
            Id = "progressBar",
            BorderStyle = LineStyle.Rounded
        };
        overlappedView1.Add (progressBar);

        //Timer timer = new (1)
        //{
        //    AutoReset = true
        //};

        //timer.Elapsed += (o, args) =>
        //                 {
        //                     if (progressBar.Fraction == 1.0)
        //                     {
        //                         progressBar.Fraction = 0;
        //                     }

        //                     progressBar.Fraction += 0.01f;

        //                     Application.Invoke (() => progressBar.SetNeedsDraw ());
        //                    ;
        //                 };
        //timer.Start ();

        app.Iteration += OnApplicationIteration;

        View overlappedView2 = CreateOverlappedView (3, 8, 10);

        View overlappedInOverlapped1 = CreateOverlappedView (4, 1, 4);
        overlappedView2.Add (overlappedInOverlapped1);

        View overlappedInOverlapped2 = CreateOverlappedView (5, 10, 7);
        overlappedView2.Add (overlappedInOverlapped2);

        StatusBar statusBar = new ();

        statusBar.Add (new Shortcut
        {
            Title = "Hide",
            Text = "Hotkey",
            Key = Key.F4,
            Action = () =>
                     {
                         // TODO: move this logic into `View.ShowHide()` or similar
                         overlappedView2.Visible = false;
                         overlappedView2.Enabled = overlappedView2.Visible;
                     }
        });

        statusBar.Add (new Shortcut
        {
            Title = "Toggle Hide",
            Text = "App",
            BindKeyToApplication = true,
            Key = Key.F4.WithCtrl,
            Action = () =>
                     {
                         // TODO: move this logic into `View.ShowHide()` or similar
                         overlappedView2.Visible = !overlappedView2.Visible;
                         overlappedView2.Enabled = overlappedView2.Visible;

                         if (overlappedView2.Visible)
                         {
                             overlappedView2.SetFocus ();
                         }
                     }
        });
        overlappedView2.Add (statusBar);

        ColorPicker colorPicker = new () { Y = 12, Width = Dim.Fill (), Id = "colorPicker", Style = new () { ShowTextFields = true, ShowColorName = true } };
        colorPicker.ApplyStyleChanges ();

        colorPicker.Value = testFrame.GetAttributeForRole (VisualRole.Normal).Background;
        colorPicker.ValueChanged += ColorPickerColorChanged;
        overlappedView2.Add (colorPicker);
        overlappedView2.Width = 50;

        testFrame.Add (overlappedView1);
        testFrame.Add (overlappedView2);

        DatePicker datePicker = new ()
        {
            X = 1,
            Y = 7,
            Id = "datePicker",
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            ShadowStyle = ShadowStyles.Transparent,
            BorderStyle = LineStyle.Double,
            CanFocus = true, // Can't drag without this? BUGBUG
            TabStop = TabBehavior.TabGroup,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped
        };
        testFrame.Add (datePicker);

        button = new () { X = Pos.AnchorEnd (), Y = Pos.AnchorEnd (), Title = $"TopButton _{GetNextHotKey ()}" };

        testFrame.Add (button);

        adornmentsEditor.AutoSelectSuperView = testFrame;
        arrangementEditor.AutoSelectSuperView = testFrame;

        testFrame.SetFocus ();
        app.Run (window);
        app.Iteration -= OnApplicationIteration;

        // timer.Close ();

        return;

        void OnApplicationIteration (object sender, EventArgs<IApplication> args)
        {
            if (progressBar.Fraction == 1.0)
            {
                progressBar.Fraction = 0;
            }

            progressBar.Fraction += 0.01f;

            app.Invoke (_ => { });
        }

        void ColorPickerColorChanged (object sender, ValueChangedEventArgs<Color?> e) =>
            testFrame.SetScheme (testFrame.GetScheme () with
            {
                Normal = new (testFrame.GetAttributeForRole (VisualRole.Normal).Foreground, e.NewValue ?? Color.Black)
            });
    }

    private View CreateOverlappedView (int id, Pos x, Pos y)
    {
        View overlapped = new ()
        {
            X = x,
            Y = y,
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Title = $"Overlapped{id} _{GetNextHotKey ()}",
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            Id = $"Overlapped{id}",
            ShadowStyle = ShadowStyles.Transparent,
            BorderStyle = LineStyle.Double,
            CanFocus = true, // At least the container needs to be focusable to drag, even if the first button inside isn't
            TabStop = TabBehavior.TabGroup,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped | ViewArrangement.Resizable
        };

        // Set the first button to CanFocus = false to test that the container can still be focused
        // and dragged even if it contains non-focusable views
        Button button = new () { Title = $"Button{id} _{GetNextHotKey ()}", CanFocus = false };
        overlapped.Add (button);

        button = new () { Y = Pos.Bottom (button), Title = $"Button{id} _{GetNextHotKey ()}" };
        overlapped.Add (button);

        return overlapped;
    }

    private View CreateTiledView (int id, Pos x, Pos y)
    {
        View overlapped = new ()
        {
            X = x,
            Y = y,
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Title = $"Tiled{id} _{GetNextHotKey ()}",
            Id = $"Tiled{id}",
            BorderStyle = LineStyle.Single,
            CanFocus = true, // Can't drag without this? BUGBUG
            TabStop = TabBehavior.TabStop,
            Arrangement = ViewArrangement.Fixed
        };

        Button button = new () { Title = $"Tiled Button{id} _{GetNextHotKey ()}", Y = 1 };
        overlapped.Add (button);

        button = new () { Y = Pos.Bottom (button), Title = $"Tiled Button{id} _{GetNextHotKey ()}" };
        overlapped.Add (button);

        return overlapped;
    }

    private char GetNextHotKey () => (char)('A' + _hotkeyCount++);
}

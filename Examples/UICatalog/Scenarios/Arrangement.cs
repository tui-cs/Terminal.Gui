#nullable enable
using Timer = System.Timers.Timer;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Arrangement", "Arrangement Tester")]
[ScenarioCategory ("Mouse and Keyboard")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Arrangement")]
public class Arrangement : Scenario
{
    private int _hotkeyCount;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window mainWindow = new ();
        mainWindow.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);
        mainWindow.Title = GetQuitKeyAndName ();
        mainWindow.TabStop = TabBehavior.TabGroup;
        mainWindow.ShadowStyle = null;

        AdornmentsEditor adornmentsEditor = new () { AutoSelectViewToEdit = true, TabStop = TabBehavior.NoStop, ShowViewIdentifier = true };

        adornmentsEditor.ExpanderButton!.Orientation = Orientation.Horizontal;

        ArrangementEditor arrangementEditor = new () { Y = Pos.Bottom (adornmentsEditor) + 1, AutoSelectViewToEdit = true, TabStop = TabBehavior.NoStop };
        mainWindow.Add (adornmentsEditor, arrangementEditor);

        FrameView testFrame = new ()
        {
            Title = "_1 Test Frame",
            Text = "This is the text of the Test Frame.\nLine 2.\nLine 3.",
            X = Pos.Right (adornmentsEditor),
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        testFrame.TextAlignment = Alignment.Center;
        testFrame.VerticalTextAlignment = Alignment.Center;

        mainWindow.Add (testFrame);

        FrameView tiledFrame = new () { Title = "Frame for Tiled Demo", Width = Dim.Fill (), Height = Dim.Auto () };
        View tiledView1 = CreateTiledView (0, 2, 1);
        View tiledView2 = CreateTiledView (1, Pos.Right (tiledView1) - 1, Pos.Top (tiledView1));
        tiledView2.Height = Dim.Height (tiledView1);
        View tiledView3 = CreateTiledView (2, Pos.Right (tiledView2) - 1, Pos.Top (tiledView2));
        tiledView3.Height = Dim.Height (tiledView1);
        View tiledView4 = CreateTiledView (3, Pos.Left (tiledView1), Pos.Bottom (tiledView1) - 1);
        tiledView4.Width = Dim.Func (_ => tiledView3.Frame.Width + tiledView2.Frame.Width + tiledView1.Frame.Width - 2);
        tiledView1.SuperViewRendersLineCanvas = true;
        tiledView2.SuperViewRendersLineCanvas = true;
        tiledView3.SuperViewRendersLineCanvas = true;
        tiledView4.SuperViewRendersLineCanvas = true;
        tiledFrame.Add (tiledView4, tiledView3, tiledView2, tiledView1);

        testFrame.Add (tiledFrame);

        View movableSizeableWithProgress = CreateOverlappedView (2, 2, 10);
        movableSizeableWithProgress.Title = "Movable _& Sizable";
        View tiledSubView = CreateTiledView (4, 0, 2);
        tiledSubView.Arrangement = ViewArrangement.Fixed;
        movableSizeableWithProgress.Add (tiledSubView);
        tiledSubView = CreateTiledView (5, Pos.Right (tiledSubView), Pos.Top (tiledSubView));
        tiledSubView.Arrangement = ViewArrangement.Fixed;
        movableSizeableWithProgress.Add (tiledSubView);

        ProgressBar progressBar = new () { Y = Pos.AnchorEnd (), Width = Dim.Fill (), Id = "progressBar" };
        movableSizeableWithProgress.Add (progressBar);

        Timer timer = new (10) { AutoReset = true };

        timer.Elapsed += (_, _) =>
                         {
                             if (Math.Abs (progressBar.Fraction - 1f) < 0.001)
                             {
                                 progressBar.Fraction = 0;
                             }

                             progressBar.Fraction += 0.01f;
                         };
        timer.Start ();

        View overlappedView2 = CreateOverlappedView (3, 10, 12);
        overlappedView2.Title = "_Not Movable";
        overlappedView2.Arrangement = ViewArrangement.Overlapped | ViewArrangement.Resizable;
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

        ColorPicker colorPicker = new ()
        {
            Y = 12, Width = Dim.Fill (), Id = "colorPicker", Style = new ColorPickerStyle { ShowTextFields = true, ShowColorName = true }
        };
        colorPicker.ApplyStyleChanges ();

        colorPicker.Value = testFrame.GetAttributeForRole (VisualRole.Normal).Background;
        colorPicker.ValueChanged += ColorPickerColorChanged;
        overlappedView2.Add (colorPicker);
        overlappedView2.Width = 50;

        DatePicker datePicker = new ()
        {
            X = 1,
            Y = 15,
            Id = "datePicker",
            Title = "Not _Sizeable",
            ShadowStyle = ShadowStyles.Transparent,
            BorderStyle = LineStyle.Double,
            TabStop = TabBehavior.TabGroup,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped,
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Error)
        };

        TransparentView transparentView = new ()
        {
            Title = "Transparent",
            ViewportSettings = ViewportSettingsFlags.Transparent,
            X = 50,
            Y = Pos.Bottom (tiledFrame),
            Width = 35,
            Height = 15
        };

        testFrame.Add (overlappedView2);
        testFrame.Add (datePicker);
        testFrame.Add (movableSizeableWithProgress);
        testFrame.Add (transparentView);

        testFrame.Add (new TransparentView
        {
            X = 50,
            Y = 25,
            Width = 35,
            Height = 15,
            Title = "Transparent|TransparentMouse",
            ViewportSettings = ViewportSettingsFlags.TransparentMouse | ViewportSettingsFlags.Transparent
        });

        mainWindow.Initialized += OnMainWindowInitialized;

        app.Run (mainWindow);
        timer.Close ();

        return;

        void OnMainWindowInitialized (object? sender, EventArgs e)
        {
            adornmentsEditor.AutoSelectSuperView = testFrame;
            arrangementEditor.AutoSelectSuperView = testFrame;

            testFrame.MoveSubViewToStart (movableSizeableWithProgress);
            movableSizeableWithProgress.SetFocus ();
        }

        void ColorPickerColorChanged (object? sender, ValueChangedEventArgs<Color?> e) =>
            testFrame.SetScheme (testFrame.GetScheme () with
            {
                Normal = new Attribute (testFrame.GetAttributeForRole (VisualRole.Normal).Foreground, e.NewValue ?? Color.Black)
            });
    }

    private View CreateOverlappedView (int id, Pos x, Pos y)
    {
        View overlapped = new ()
        {
            X = x,
            Y = y,
            Width = Dim.Auto (minimumContentDim: 20),
            Height = Dim.Auto (minimumContentDim: 3),
            Title = $"Overlapped{id} _{GetNextHotKey ()}",
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            Id = $"Overlapped{id}",
            ShadowStyle = ShadowStyles.Transparent,
            BorderStyle = LineStyle.Double,
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped | ViewArrangement.Resizable
        };

        return overlapped;
    }

    private View CreateTiledView (int id, Pos x, Pos y)
    {
        View tiled = new ()
        {
            X = x,
            Y = y,
            Width = Dim.Auto (minimumContentDim: 15),
            Height = Dim.Auto (minimumContentDim: 2),
            Title = $"Tiled{id} _{GetNextHotKey ()}",
            Id = $"Tiled{id}",
            BorderStyle = LineStyle.Single,
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            Arrangement = ViewArrangement.Resizable
        };

        return tiled;
    }

    private char GetNextHotKey () => (char)('A' + _hotkeyCount++);

    public override List<Key> GetDemoKeyStrokes (IApplication? app)
    {
        List<Key> keys = ['&', Application.GetDefaultKey (Command.Arrange)];

        // Select view with progress bar

        for (var i = 0; i < 8; i++)
        {
            keys.Add (Key.CursorUp);
        }

        for (var i = 0; i < 25; i++)
        {
            keys.Add (Key.CursorRight);
        }

        keys.Add (Application.GetDefaultKey (Command.Arrange));

        keys.Add (Key.S);

        keys.Add (Application.GetDefaultKey (Command.Arrange));

        for (var i = 0; i < 10; i++)
        {
            keys.Add (Key.CursorUp);
        }

        for (var i = 0; i < 25; i++)
        {
            keys.Add (Key.CursorLeft);
        }

        keys.Add (Application.GetDefaultKey (Command.Arrange));

        // Select view with progress bar
        keys.Add ('&');

        keys.Add (Application.GetDefaultKey (Command.Arrange));

        keys.Add (Key.Tab);

        for (var i = 0; i < 10; i++)
        {
            keys.Add (Key.CursorRight);
            keys.Add (Key.CursorDown);
        }

        return keys;
    }

    public sealed class TransparentView : FrameView
    {
        public TransparentView ()
        {
            Title = "Transparent";
            Text = "TransparentView Text";
            X = 0;
            Y = 0;
            Width = 30;
            Height = 10;
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Resizable | ViewArrangement.Movable;
            ViewportSettings |= ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse;

            Padding.Thickness = new Thickness (1);

            Add (new Button { Title = "_Hi", X = Pos.Center (), Y = Pos.Center () });
        }
    }
}

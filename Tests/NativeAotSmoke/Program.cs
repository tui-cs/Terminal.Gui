// Native AOT smoke test application for Terminal.Gui.
//
// This app is an AOT-safe equivalent of UICatalog's AllViewsTester. It statically instantiates
// every IDesignable view and calls EnableForDesign () to populate demo data.
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Terminal.Gui.Configuration;
using Terminal.Gui.Editor;
using UICatalog.Scenarios;

namespace NativeAotSmoke;

public static class Program
{
    private static async Task Main (string [] args)
    {
#pragma warning disable IL2026, IL3050
        await RunAsync (args);
#pragma warning restore IL2026, IL3050
    }

    [RequiresUnreferencedCode ("Calls Terminal.Gui.Application.Init(IDriver, String)")]
    [RequiresDynamicCode ("Calls Terminal.Gui.Application.Init(IDriver, String)")]
    private static async Task RunAsync (string [] args)
    {
        bool smokeTest = args.Length > 0 && args [0] == "--smoke-test";

        using IApplication app = Application.Create ().Init ();

        #region Localization sanity check

        if (Equals (Thread.CurrentThread.CurrentUICulture, CultureInfo.InvariantCulture) && Application.SupportedCultures!.Count == 0)
        {
            Debug.Assert (Application.SupportedCultures.Count == 0);
        }
        else
        {
            Debug.Assert (Application.SupportedCultures!.Count > 0);
            Debug.Assert (Equals (CultureInfo.CurrentCulture, Thread.CurrentThread.CurrentUICulture));
        }

        #endregion

        if (smokeTest)
        {
            ExerciseDictionaryDeepCloning ();

            using CancellationTokenSource cts = new (TimeSpan.FromSeconds (5));
            await app.RunAsync<AotAllViewsWindow> (cts.Token);
            Console.WriteLine ("AOT smoke test passed: full app lifecycle completed successfully.");

            return;
        }

        app.Run<AotAllViewsWindow> ();
    }

    private static void ExerciseDictionaryDeepCloning ()
    {
        _ = Color.Colors16;
        _ = Application.DefaultKeyBindings;
        _ = View.DefaultKeyBindings;
        _ = View.ViewKeyBindings;
        _ = ThemeManager.GetThemeNames ();
        _ = SchemeManager.Schemes;
    }
}

public sealed class AotAllViewsWindow : Runnable
{
    private const int MAX_HEIGHT = 12;

    public AotAllViewsWindow ()
    {
        Title = $"AOT All Views ({Application.GetDefaultKey (Command.Quit)} to quit)";

        MenuBar menuBar = new ()
        {
            Menus =
            [
                new MenuBarItem ("_File", [new MenuItem ("_Quit", "", () => App?.RequestStop ())]),
                new MenuBarItem ("_Help", [new MenuItem ("_About", "", () => MessageBox.Query (App!, "About", "AOT All Views Tester", "OK"))])
            ]
        };

        StatusBar statusBar = new ();

        ViewPropertiesEditor propsEditor = new ()
        {
            Title = "Properties",
            Width = Dim.Percent (30),
            Height = Dim.Fill (statusBar),
            X = Pos.AnchorEnd (),
            AutoSelectViewToEdit = true,
            AutoSelectAdornments = false,
            BorderStyle = LineStyle.Dotted
        };

        View container = new () { Y = Pos.Bottom (menuBar), Width = Dim.Fill (propsEditor), Height = Dim.Fill (statusBar), CanFocus = true };

        container.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

        container.ViewportChanged += (sender, _) =>
                                     {
                                         if (sender is View sendingView)
                                         {
                                             sendingView.SetContentHeight (sendingView.GetHeightRequiredForSubViews ());
                                         }
                                     };

        propsEditor.AutoSelectSuperView = container;

        View? previous = null;

        foreach ((string name, View view) in CreateAllViews ())
        {
            FrameView frame = new ()
            {
                CanFocus = true,
                Title = name,
                Y = previous is { } ? Pos.Bottom (previous) : 0,
                Width = Dim.Fill (),
                Height = Dim.Auto (DimAutoStyle.Content, maximumContentDim: MAX_HEIGHT)
            };

            if (view.Width == Dim.Absolute (0))
            {
                view.Width = Dim.Fill ();
            }

            if (view.Height == Dim.Absolute (0))
            {
                view.Height = MAX_HEIGHT - 2;
            }

            frame.Add (view);
            container.Add (frame);
            previous = frame;
        }

        Shortcut quitShortcut = new () { Text = "Quit", Key = Application.GetDefaultKey (Command.Quit), Action = RequestStop };

        statusBar.Add (quitShortcut);

        Add (menuBar, container, propsEditor, statusBar);
    }

    private static IEnumerable<(string Name, View View)> CreateAllViews ()
    {
        yield return Design (new Button ());
        yield return Design (new ColorPicker ());
        yield return Design (new DropDownList ());
        yield return Design (new FlagSelector ());
        yield return Design (new GraphView ());
        yield return Design (new HexView ());
        yield return Design (new Link { Text = "Terminal.Gui on GitHub", Url = "https://github.com/tui-cs/Terminal.Gui" });
        yield return Design (new ListView ());
        yield return Design (new OptionSelector ());
        yield return Design (new ProgressBar ());
        yield return Design (new ScrollBar { Orientation = Orientation.Horizontal });
        yield return Design (new Shortcut ());
        yield return Design (new SpinnerView ());
        yield return Design (new Tabs ());
        yield return Design (new TextField ());
        yield return Design (new TextValidateField ());
        yield return Design (new Editor ());
        yield return Design (new TreeView ());

        yield return (nameof (CharMap), new CharMap ());
        yield return (nameof (CheckBox), new CheckBox { Text = "Check me" });
        yield return (nameof (DatePicker), new DatePicker ());
        yield return (nameof (FrameView), new FrameView { Title = "FrameView", Text = "Content" });
        yield return (nameof (Label), new Label { Text = "This is a Label." });
        yield return (nameof (Line), new Line ());
        yield return (nameof (NumericUpDown), new NumericUpDown ());
    }

    private static (string Name, View View) Design<T> (T view) where T : View, IDesignable
    {
        view.EnableForDesign ();

        return (typeof (T).Name, view);
    }
}

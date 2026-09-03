#nullable enable
using System.Text;
using Terminal.Gui.Editor;
// ReSharper disable AccessToDisposedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Unicode", "Tries to test Unicode in all controls (#204)")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Unicode")]
public class UnicodeInMenu : Scenario
{
    public override void Main ()
    {
        const string UNICODE =
            "Τὴ γλῶσσα μοῦ ἔδωσαν ἑλληνικὴ\nτὸ σπίτι φτωχικὸ στὶς ἀμμουδιὲς τοῦ Ὁμήρου.\nΜονάχη ἔγνοια ἡ γλῶσσα μου στὶς ἀμμουδιὲς τοῦ Ὁμήρου.";

        var gitString = $"gui.cs 糊 (hú) {Glyphs.IdenticalTo} {Glyphs.DownArrow}18 {Glyphs.UpArrow}10 {Glyphs.VerticalFourDots}1 {Glyphs.HorizontalEllipsis}";

        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.BorderStyle = LineStyle.None;

        // MenuBar
        MenuBar menu = new ();

        menu.Add (new MenuBarItem ("_Файл",
                                   [
                                       new MenuItem { Title = "_Создать", HelpText = "Creates new file" },
                                       new MenuItem { Title = "_Открыть" },
                                       new MenuItem { Title = "Со_хранить" },
                                       new MenuItem { Title = "_Выход", Action = appWindow.RequestStop }
                                   ]));

        menu.Add (new MenuBarItem ("_Edit",
                                   [
                                       new MenuItem { Title = Strings.cmdCopy },
                                       new MenuItem { Title = Strings.cmdCut },
                                       new MenuItem { Title = "_糊", HelpText = "hú (Paste)" }
                                   ]));
        appWindow.Add (menu);

        Label label = new () { X = 0, Y = Pos.Bottom (menu), Text = "Label:" };
        appWindow.Add (label);

        Label testLabel = new () { X = 20, Y = Pos.Y (label), Width = Dim.Percent (50), Text = gitString };
        appWindow.Add (testLabel);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (label) + 1, Text = "Label (CanFocus):" };
        appWindow.Add (label);

        StringBuilder sb = new ();
        sb.Append ('e');
        sb.Append ('\u0301');
        sb.Append ('\u0301');

        testLabel = new Label
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (50),
            CanFocus = true,
            HotKeySpecifier = new Rune ('&'),
            Text = $"Should be [e with two accents, but isn't due to #2616]: [{sb}]"
        };
        appWindow.Add (testLabel);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (label) + 1, Text = "Button:" };
        appWindow.Add (label);

        Button button = new () { X = 20, Y = Pos.Y (label), Text = "A123456789????JQK" };
        appWindow.Add (button);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (label) + 1, Text = "CheckBox:" };
        appWindow.Add (label);

        CheckBox checkBox = new ()
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (50),
            Height = 1,
            Text = gitString
        };
        appWindow.Add (checkBox);

        CheckBox checkBoxRight = new ()
        {
            X = 20,
            Y = Pos.Bottom (checkBox),
            Width = Dim.Percent (50),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = $"End - {gitString}"
        };
        appWindow.Add (checkBoxRight);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (checkBoxRight) + 2, Text = "HexView:" };
        appWindow.Add (label);

        HexView hexView = new (new MemoryStream (Encoding.ASCII.GetBytes (gitString + " ??_???????")))
        {
            X = 20, Y = Pos.Y (label), Width = Dim.Percent (60), Height = 5
        };
        appWindow.Add (hexView);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (hexView) + 1, Text = "ListView:" };
        appWindow.Add (label);

        ListView listView = new ()
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (60),
            Height = 3,
            Source = new ListWrapper<string> (["item #1", gitString, "??_???????", UNICODE])
        };
        appWindow.Add (listView);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (listView) + 1, Text = "OptionSelector:" };
        appWindow.Add (label);

        OptionSelector optionSelector = new ()
        {
            X = 20, Y = Pos.Y (label), Width = Dim.Percent (60), Labels = ["item #1", gitString, "??_???????", "??????????R"]
        };
        appWindow.Add (optionSelector);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (optionSelector) + 1, Text = "TextField:" };
        appWindow.Add (label);

        TextField textField = new () { X = 20, Y = Pos.Y (label), Width = Dim.Percent (60), Text = gitString + " = ??_???????" };
        appWindow.Add (textField);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (textField) + 1, Text = "Editor:" };
        appWindow.Add (label);

        Editor editor = new ()
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (60),
            Height = 5,
            Text = UNICODE
        };
        appWindow.Add (editor);

        // StatusBar
        StatusBar statusBar = new ([
                                       new Shortcut (Application.GetDefaultKey (Command.Quit), "?????", appWindow.RequestStop),
                                       new Shortcut (Key.F2, "???????", null),
                                       new Shortcut (Key.F3, "??_???????", null)
                                   ]);
        appWindow.Add (statusBar);

        app.Run (appWindow);
    }
}

#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Links", "Demonstrates how Links work.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Mouse and Keyboard")]
public class Links : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetName ();
        appWindow.BorderStyle = LineStyle.None;

        Label titleLabel = new () { Text = "_Title:", X = 1, Y = 1 };
        appWindow.Add (titleLabel);

        TextField titleTextField = new () { X = Pos.Right (titleLabel) + 1, Y = Pos.Top (titleLabel), Width = Dim.Fill () };
        appWindow.Add (titleTextField);

        Label textLabel = new () { Text = " Te_xt:", X = Pos.Left (titleLabel), Y = Pos.Bottom (titleLabel) };
        appWindow.Add (textLabel);

        TextField textTextField = new () { X = Pos.Right (textLabel) + 1, Y = Pos.Top (textLabel), Width = Dim.Fill () };
        appWindow.Add (textTextField);

        Label urlLabel = new () { Text = "  _Url:", X = 1, Y = Pos.Bottom (titleTextField) + 1 };
        appWindow.Add (urlLabel);

        TextField urlTextField = new () { X = Pos.Right (urlLabel) + 1, Y = Pos.Bottom (titleTextField) + 1, Width = Dim.Fill () };
        appWindow.Add (urlTextField);

        Label simpleUrlLabel = new () { X = 1, Y = Pos.Bottom (urlTextField) + 2 };
        appWindow.Add (simpleUrlLabel);

        FrameView linkFrame = new ()
        {
            Title = "_Link Demo",
            X = 0,
            Y = Pos.Bottom (simpleUrlLabel) + 2,
            Width = Dim.Fill (),
            Height = Dim.Auto (),
            AssignHotKeys = true,
            TabStop = TabBehavior.TabStop,
            Arrangement = ViewArrangement.Resizable
        };

        Link linkWithBorder = new () { BorderStyle = LineStyle.Dotted };
        app.ToolTips!.SetToolTip (linkWithBorder, () => linkWithBorder.Url);

        linkWithBorder.TextChanged +=
            (_, _) => simpleUrlLabel.Text = $"This is just a Label with a URL in Text (WT automatically enables URLs) - {linkWithBorder.Text}";
        titleTextField.ValueChanged += (_, e) => linkWithBorder.Title = e.NewValue ?? string.Empty;
        textTextField.ValueChanged += (_, e) => linkWithBorder.Text = e.NewValue ?? string.Empty;
        urlTextField.ValueChanged += (_, e) => linkWithBorder.Url = e.NewValue ?? string.Empty;
        linkFrame.Add (linkWithBorder);

        titleTextField.Text = "Title";
        textTextField.Text = "GitHub repo";
        urlTextField.Text = "https://github.com/tui-cs/Terminal.Gui";

        Button copyButton = new () { Title = "_Copy", X = Pos.Right (linkWithBorder) + 1, Y = Pos.Top (linkWithBorder) + 1 };
        copyButton.Accepting += (_, _) => linkWithBorder.Copy ();

        linkFrame.Add (copyButton);

        Label label = new () { Y = Pos.Bottom (linkFrame), Title = "_Link to API Docs:" };

        Link link = new ()
        {
            X = Pos.Right (label) + 1, Y = Pos.Top (label), Text = "Terminal.Gui.Views.Link", Url = "https://tui-cs.github.io/Terminal.Gui/api/Terminal.Gui.Views.Link.html"
        };
        appWindow.Add (label, link);
        app.ToolTips!.SetToolTip (link, () => link.Url);

        appWindow.Add (linkFrame);

        // StatusBar
        Shortcut urlIndicator = new (Key.Empty, "", null);

        StatusBar statusBar = new ([new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", () => appWindow.RequestStop ()), urlIndicator]);

        // Demonstrate dynamically showing URL in the status bar when hovering over the link.
        // Note that we use a Shortcut here to show how they can be used in a StatusBar, but you could use any View.
        linkWithBorder.MouseEnter += (_, _) => urlIndicator.Title = linkWithBorder.Url;
        linkWithBorder.MouseLeave += (_, _) => urlIndicator.Title = "";

        appWindow.Add (statusBar);

        app.Run (appWindow);
    }
}

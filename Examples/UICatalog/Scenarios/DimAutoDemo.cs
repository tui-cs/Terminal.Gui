#nullable enable
using Terminal.Gui.Editor;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("DimAuto", "Demonstrates Dim.Auto")]
[ScenarioCategory ("Layout")]
public class DimAutoDemo : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        // Setup - Create a top-level application window and configure it.
        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();

        // For diagnostics
        appWindow.Padding.Thickness = new Thickness (1);

        FrameView dimAutoFrameView = CreateDimAutoContentFrameView (app);

        appWindow.Add (dimAutoFrameView /*, sliderFrameView dlgButton*/);

        // Run - Start the application.
        app.Run (appWindow);
    }

    private static FrameView CreateDimAutoContentFrameView (IApplication app)
    {
        FrameView dimAutoFrameView = new ()
        {
            Title = "Type to make View grow",

            // X = Pos.Center (),
            // Y = Pos.Center (),
            Width = Dim.Auto (DimAutoStyle.Content, Dim.Percent (25)),
            Height = Dim.Auto (DimAutoStyle.Content, 10),
            Arrangement = ViewArrangement.Resizable
        };
        dimAutoFrameView.Margin.Thickness = new Thickness (1);
        dimAutoFrameView.ValidatePosDim = true;

        Editor textEdit = new () { X = 0, Y = 0, Width = 20, Height = 4 };
        dimAutoFrameView.Add (textEdit);

        Label vlabel = new ()
        {
            Text = textEdit.Text,
            X = Pos.Left (textEdit),
            Y = Pos.Bottom (textEdit) + 1,
            Width = Dim.Auto (DimAutoStyle.Text, 1),
            Height = Dim.Auto (DimAutoStyle.Text, 8),
            SchemeName = "Error",
            TextDirection = TextDirection.TopBottom_LeftRight
        };
        vlabel.Id = "vlabel";
        dimAutoFrameView.Add (vlabel);

        Label hlabel = new ()
        {
            Text = textEdit.Text,
            X = Pos.Right (vlabel) + 1,
            Y = Pos.Bottom (textEdit),
            Width = Dim.Auto (DimAutoStyle.Text, 20),
            Height = Dim.Auto (DimAutoStyle.Text, 1),
            SchemeName = "Error"
        };
        hlabel.Id = "hlabel";
        dimAutoFrameView.Add (hlabel);

        View heightAuto = new ()
        {
            X = Pos.Right (vlabel) + 1,
            Y = Pos.Bottom (hlabel) + 1,
            Width = 20,
            Height = Dim.Auto (),
            SchemeName = "Error",
            Title = "W: 20, H: Auto",
            BorderStyle = LineStyle.Rounded
        };
        heightAuto.Id = "heightAuto";
        dimAutoFrameView.Add (heightAuto);

        View widthAuto = new ()
        {
            X = Pos.Right (heightAuto) + 1,
            Y = Pos.Bottom (hlabel) + 1,
            Width = Dim.Auto (),
            Height = 5,
            SchemeName = "Error",
            Title = "W: Auto, H: 5",
            BorderStyle = LineStyle.Rounded
        };
        widthAuto.Id = "widthAuto";
        dimAutoFrameView.Add (widthAuto);

        View bothAuto = new ()
        {
            X = Pos.Right (widthAuto) + 1,
            Y = Pos.Bottom (hlabel) + 1,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            SchemeName = "Error",
            Title = "W: Auto, H: Auto",
            BorderStyle = LineStyle.Rounded
        };
        bothAuto.Id = "bothAuto";
        dimAutoFrameView.Add (bothAuto);

        textEdit.Document!.Changed += (s, e) =>
                                      {
                                          hlabel.Text = textEdit.Text;
                                          vlabel.Text = textEdit.Text;
                                          heightAuto.Text = textEdit.Text;
                                          widthAuto.Text = textEdit.Text;
                                          bothAuto.Text = textEdit.Text;
                                      };

        Button movingButton = new () { Text = "_Move", X = Pos.Right (vlabel), Y = Pos.Bottom (vlabel) };
        movingButton.Accepted += (_, _) => { movingButton.Y = movingButton.Frame.Y + 1; };
        dimAutoFrameView.Add (movingButton);

        Button resetButton = new () { Text = "_Reset Button (AnchorEnd)", X = Pos.AnchorEnd (), Y = Pos.AnchorEnd () };

        resetButton.Accepted += (_, _) =>
                                 {
                                     movingButton.Y = Pos.Bottom (hlabel);
                                     movingButton.X = 0;
                                 };
        dimAutoFrameView.Add (resetButton);

        OptionSelector optionSelector = new ()
        {
            Labels = ["One", "Two", "Three"],
            X = 0,
            Y = Pos.AnchorEnd (),
            Title = "Options",
            BorderStyle = LineStyle.Dotted
        };
        dimAutoFrameView.Add (optionSelector);

        FrameView fillFrame = new ()
        {
            Title = "_Fill View",
            X = Pos.Right (optionSelector),
            Y = Pos.Bottom (vlabel),
            Width = Dim.Fill (Dim.Width (resetButton)),
            Height = Dim.Fill ()
        };
        dimAutoFrameView.Add (fillFrame);

        return dimAutoFrameView;
    }

    private static FrameView CreateSliderFrameView ()
    {
        FrameView sliderFrameView = new () { Title = "LinearRange - Example of a DimAuto View" };

        List<object> options = ["One", "Two", "Three", "Four"];

        LinearMultiSelector<object> linearRange = new (options)
        {
            X = 0,
            Y = 0,
            AllowEmpty = false,
            BorderStyle = LineStyle.Double,
            Title = "_LinearRange"
        };
        sliderFrameView.Add (linearRange);

        return sliderFrameView;
    }
}

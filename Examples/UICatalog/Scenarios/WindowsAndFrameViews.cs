using System.Collections.Generic;
using System.Linq;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Windows & FrameViews", "Stress Tests Windows, sub-Windows, and FrameViews.")]
[ScenarioCategory ("Layout")]
public class WindowsAndFrameViews : Scenario
{
    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        const int MARGIN = 2;
        const int PADDING = 1;
        const int CONTENT_HEIGHT = 7;

        // list of Windows we create
        List<View> listWin = [];

        Window win = new ()
        {
            Title = $"{listWin.Count} - Scenario: {GetName ()}",
            X = Pos.Center (),
            Y = 1,
            Width = Dim.Fill (15),
            Height = 10,
            SchemeName = "Dialog",
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable | ViewArrangement.Resizable
        };
        win.Padding.Thickness = new (PADDING);
        win.Margin.Thickness = new (MARGIN);

        Button paddingButton = new ()
        {
            X = Pos.Center (),
            Y = 0,
            SchemeName = "Error",
            Text = $"Padding of container is {PADDING}"
        };
        paddingButton.Accepted += (_, _) => MessageBox.Query (app,
                                                              "About UI Catalog",
                                                              "UI Catalog is a comprehensive sample library for Terminal.Gui",
                                                              Strings.btnOk
                                                             );
        win.Add (paddingButton);

        win.Add (
                 new Button
                 {
                     X = Pos.Center (),
                     Y = Pos.AnchorEnd (),
                     SchemeName = "Error",
                     Text = "Press ME! (Y = Pos.AnchorEnd(1))"
                 }
                );
        window.Add (win);

        // add it to our list
        listWin.Add (win);

        for (int pad = 0; pad < 3; pad++)
        {
            Window loopWin = new ()
            {
                Title = $"{listWin.Count} - Window Loop - padding = {pad}",
                X = MARGIN,
                Y = Pos.Bottom (listWin.Last ()) + MARGIN,
                Width = Dim.Fill (MARGIN),
                Height = CONTENT_HEIGHT + pad * 2 + 2,
                Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable | ViewArrangement.Resizable
            };
            loopWin.Padding.Thickness = new (pad);

            loopWin.SchemeName = "Dialog";

            Button pressMeButton = new ()
            {
                X = Pos.Center (), Y = 0, SchemeName = "Error", Text = "Press me! (Y = 0)",
            };

            pressMeButton.Accepted += (s, _) =>
                                       MessageBox.ErrorQuery ((s as View)?.App!, loopWin.Title, "Neat?", Strings.btnNo, Strings.btnYes);
            loopWin.Add (pressMeButton);

            Window subWin = new ()
            {
                Title = "Sub Window",
                X = Pos.Percent (0),
                Y = 1,
                Width = Dim.Percent (50),
                Height = 5,
                SchemeName = "Base",
                Text = "The Text in the Window",
                Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable | ViewArrangement.Resizable

            };

            subWin.Add (
                        new TextField { Y = 1, SchemeName = "Error", Text = "Edit me! " + loopWin.Title }
                       );
            loopWin.Add (subWin);

            FrameView frameView = new ()
            {
                X = Pos.Percent (50),
                Y = 1,
                Width = Dim.Percent (100, DimPercentMode.Position), // Or Dim.Percent (50)
                Height = 5,
                SchemeName = "Base",
                Text = "The Text in the FrameView",
                Title = "This is a Sub-FrameView"
            };

            frameView.Add (
                           new TextField { Y = 1, Text = "Edit Me!" }
                          );
            loopWin.Add (frameView);

            window.Add (loopWin);
            listWin.Add (loopWin);
        }

        FrameView frame = new ()
        {
            X = MARGIN,
            Y = Pos.Bottom (listWin.Last ()) + MARGIN / 2,
            Width = Dim.Fill (MARGIN),
            Height = CONTENT_HEIGHT + 2, // 2 for default padding
            Title = "This is a FrameView"
        };
        frame.SchemeName = "Dialog";

        frame.Add (
                   new Label
                   {
                       X = Pos.Center (), Y = 0, SchemeName = "Error", Text = "This is a Label! (Y = 0)"
                   }
                  );

        Window subWinOfFrameView = new ()
        {
            Title = "This is a Sub-Window",
            X = Pos.Percent (0),
            Y = 1,
            Width = Dim.Percent (50),
            Height = Dim.Fill () - 1,
            SchemeName = "Base",
            Text = "The Text in the Window",
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable | ViewArrangement.Resizable

        };

        subWinOfFrameView.Add (
                        new TextField { SchemeName = "Error", Text = "Edit Me" }
                       );

        subWinOfFrameView.Add (new CheckBox { Y = 1, Text = "Check me" });
        subWinOfFrameView.Add (new CheckBox { Y = 2, Text = "Or, Check me" });

        frame.Add (subWinOfFrameView);

        FrameView subFrameViewOfFrameView = new ()
        {
            X = Pos.Percent (50),
            Y = 1,
            Width = Dim.Percent (100),
            Height = Dim.Fill () - 1,
            SchemeName = "Base",
            Text = "The Text in the FrameView",
            Title = "this is a Sub-FrameView"
        };
        subFrameViewOfFrameView.Add (new TextField { Width = 15, Text = "Edit Me" });

        subFrameViewOfFrameView.Add (new CheckBox { Y = 1, Text = "Check me" });

        subFrameViewOfFrameView.Add (new CheckBox { Y = 2, Text = "Or, Check me" });

        frame.Add (
                   new CheckBox { X = 0, Y = Pos.AnchorEnd (), Text = "Btn1 (Y = Pos.AnchorEnd ())" }
                  );
        CheckBox c = new () { X = Pos.AnchorEnd (), Y = Pos.AnchorEnd (), Text = "Btn2 (Y = Pos.AnchorEnd ())" };
        frame.Add (c);

        frame.Add (subFrameViewOfFrameView);

        window.Add (frame);
        listWin.Add (frame);

        window.SchemeName = "Base";

        app.Run (window);
    }
}

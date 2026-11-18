
namespace UICatalog.Scenarios;

[ScenarioMetadata ("ChineseUI", "Chinese UI")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Unicode")]
public class ChineseUI : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new ()
        {
            Title = GetQuitKeyAndName (),
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        FrameView buttonPanel = new ()
        {
            Title = "Command",
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = 5
        };
        win.Add (buttonPanel);

        Button btn = new () { X = 1, Y = 1, Text = "\u4f60" }; // v1: A (Chinese: you)

        btn.Accepting += (s, _) =>
                      {
                           int? result = MessageBox.Query (
                                                           (s as View)?.App!,
                                                           "Confirm",
                                                           "Are you sure you want to quit ui?",
                                                           "No",
                                                           "Yes"
                                                         );

                          if (result == 1)
                          {
                              win.RequestStop ();
                          }
                      };

        buttonPanel.Add (
                         btn,
                         new Button { X = 12, Y = 1, Text = "\u597d" }, // v1: B (Chinese: good)
                         new Button { X = 22, Y = 1, Text = "\u5440" } // v1: C (Chinese: yeah)
                        );

        app.Run (win);
    }
}

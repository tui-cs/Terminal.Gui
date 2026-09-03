#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Generic", "Generic sample - A template for creating new Scenarios")]
[ScenarioCategory ("Controls")]
public sealed class Generic : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        // Setup - Create an IRunnable Window and configure it.
        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.AssignHotKeys = true;

        Button button = new ()
        {
            X = Pos.Center (),
            Y = 1,
            Text = "Button"
        };

        button.Accepting += (s, e) =>
                            {
                                // When Accepting is handled, set e.Handled to true to prevent further processing.
                                e.Handled = true;
                                MessageBox.Query ((s as View)!.App!, "Nice Job", "You pressed the button!", Strings.btnOk);
                            };

        appWindow.Add (button);

        // Run - Start the application.
        app.Run (appWindow);
    }
}

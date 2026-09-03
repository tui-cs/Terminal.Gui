
#nullable enable
namespace UICatalog.Scenarios;

[ScenarioMetadata ("Date Picker", "Demonstrates how to use DatePicker class")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("DateTime")]
public class DatePickers : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new ();
        window.Title = GetQuitKeyAndName ();

        var datePicker = new DatePicker { Y = Pos.Center (), X = Pos.Center () };

        window.Add (datePicker);

        app.Run (window);
    }
}

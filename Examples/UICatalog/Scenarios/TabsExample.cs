#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Tabs Example", "Demonstrates the Tabs View (a TabView replacement).")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Tabs")]

public sealed class TabsExample : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.BorderStyle = LineStyle.None;

        // ── Main Tabs control ──
        Tabs tabs = new ()
        {
            X = 2,
            Y = 3,
            Title = "_Tabs",
            Width = Dim.Percent (70),
            Height = Dim.Percent (70)
        };

        tabs.EnableForDesign ();

        // ── Editors ──
        AdornmentsEditor adornmentsEditor = new ()
        {
            BorderStyle = LineStyle.Single, AutoSelectViewToEdit = true, Arrangement = ViewArrangement.Movable, X = Pos.AnchorEnd ()
        };
        adornmentsEditor.Border.Thickness = new Thickness (1, 2, 1, 1);
        adornmentsEditor.AutoSelectSuperView = appWindow;
        adornmentsEditor.AutoSelectAdornments = true;
        adornmentsEditor.ShowViewIdentifier = true;

        ViewportSettingsEditor viewportSettingsEditor = new ()
        {
            BorderStyle = LineStyle.Single, AutoSelectViewToEdit = true, Arrangement = ViewArrangement.Movable, Y = Pos.AnchorEnd ()
        };
        viewportSettingsEditor.Border.Thickness = new Thickness (1, 2, 1, 1);
        viewportSettingsEditor.AutoSelectSuperView = appWindow;
        viewportSettingsEditor.AutoSelectAdornments = true;
        viewportSettingsEditor.ShowViewIdentifier = true;

        appWindow.Add (tabs, adornmentsEditor, viewportSettingsEditor);

        app.Run (appWindow);
    }
}

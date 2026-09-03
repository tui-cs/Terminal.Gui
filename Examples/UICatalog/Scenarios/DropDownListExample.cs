#nullable enable
using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("DropDownList", "Demonstrates the DropDownList control.")]
[ScenarioCategory ("Controls")]
public class DropDownListExample : Scenario
{
    private enum DayOfWeekShort
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    }

    private IApplication? _app;
    private EventLog? _eventLog;

    public override void Main ()
    {
        _app = Application.Create ();
        _app.Init ();

        using Window window = new ();
        window.Title = GetQuitKeyAndName ();
        window.BorderStyle = LineStyle.None;

        // Event log on the right
        _eventLog = new EventLog
        {
            Title = "_Event Log",
            X = Pos.AnchorEnd (),
            Width = Dim.Auto (maximumContentDim: 50),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Double,
            Arrangement = ViewArrangement.LeftResizable
        };

        // Main content area
        FrameView contentFrame = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (_eventLog!),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None,
            Title = "DropDownList Examples - press F4 to open focused dropdown"
        };

        // Sample data
        ObservableCollection<string> countries =
        [
            "United States",
            "Canada",
            "Mexico",
            "Brazil",
            "Argentina",
            "United Kingdom",
            "Germany",
            "France",
            "Italy",
            "Spain",
            "Russia",
            "China",
            "Japan",
            "India",
            "Australia",
            "South Africa",
            "Egypt",
            "Nigeria",
            "Kenya",
            "Morocco"
        ];

        ObservableCollection<string> colors = new (ColorStrings.GetStandardColorNames ());

        ObservableCollection<string> longGermanWords =
        [
            "Donaudampfschifffahrtsgesellschaftskapitän",
            "Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz",
            "Rechtsschutzversicherungsgesellschaft",
            "Grundstücksverkehrsgenehmigungszuständigkeitsübertragungsverordnung"
        ];

        // ReadOnly Mode Example
        Label readOnlyLabel = new () { X = 1, Y = 1, Text = "ReadOnly Mode (default):" };

        DropDownList readOnlyDropDown = new ()
        {
            X = Pos.Right (readOnlyLabel) + 1,
            Y = Pos.Top (readOnlyLabel),
            Title = "Select Color",
            Source = new ListWrapper<string> (colors),
            ReadOnly = true,
            Value = colors [0]
        };

        readOnlyDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"ReadOnly: ValueChanged - OldValue: '{e.OldValue}', NewValue: '{e.NewValue}'"); };

        readOnlyDropDown.ValueChanging += (s, e) =>
                                          {
                                              _eventLog?.Log ($"ReadOnly: ValueChanging - CurrentValue: '{e.CurrentValue}', NewValue: '{e.NewValue}'");
                                          };

        // Editable Mode Example
        Label editableLabel = new () { X = 1, Y = Pos.Bottom (readOnlyDropDown) + 1, Text = "Editable Mode:" };

        DropDownList editableDropDown = new ()
        {
            X = Pos.Right (editableLabel) + 1,
            Y = Pos.Top (editableLabel),
            Title = "Select or Type Color",
            Source = new ListWrapper<string> (colors),
            ReadOnly = false,
            Text = "Blue"
        };

        editableDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"Editable: ValueChanged - OldValue: '{e.OldValue}', NewValue: '{e.NewValue}'"); };

        editableDropDown.ValueChanging += (s, e) =>
                                          {
                                              _eventLog?.Log ($"Editable: ValueChanging - CurrentValue: '{e.CurrentValue}', NewValue: '{e.NewValue}'");
                                          };

        // Enum-typed dropdown (DropDownList<TEnum>) example
        Label enumLabel = new () { X = 1, Y = Pos.Bottom (editableDropDown) + 1, Text = "Enum Mode (DropDownList<TEnum>):" };

        DropDownList<DayOfWeekShort> enumDropDown = new ()
        {
            X = Pos.Right (enumLabel) + 1, Y = Pos.Top (enumLabel), Title = "Select Day", Value = DayOfWeekShort.Monday
        };

        enumDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"Enum: ValueChanged - NewValue: '{e.Value}'"); };

        // Different positions example
        Label positionLabel = new () { X = 1, Y = Pos.Bottom (enumDropDown) + 1, Text = "Different Positions (with Borders and Resizable right sides):" };

        DropDownList topLeftDropDown = new ()
        {
            X = 1,
            Y = Pos.Bottom (positionLabel),
            Title = "Top-Left",
            Source = new ListWrapper<string> (countries),
            ReadOnly = true,
            Text = "Argentina",
            BorderStyle = LineStyle.Dotted,
            Arrangement = ViewArrangement.RightResizable
        };

        topLeftDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"TopLeft: Selected '{e.NewValue}'"); };

        DropDownList topRightDropDown = new ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.Top (topLeftDropDown),
            Title = "Top-Right",
            Source = new ListWrapper<string> (longGermanWords),
            ReadOnly = true,
            Text = "Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz",
            BorderStyle = LineStyle.Dotted,
            Arrangement = ViewArrangement.RightResizable
        };

        topRightDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"TopRight: Selected '{e.NewValue}'"); };

        DropDownList bottomLeftDropDown = new ()
        {
            X = 1,
            Y = Pos.AnchorEnd (),
            Title = "Bottom-Left",
            Source = new ListWrapper<string> (countries),
            ReadOnly = true,
            Text = "Japan",
            BorderStyle = LineStyle.Dotted,
            Arrangement = ViewArrangement.RightResizable
        };

        bottomLeftDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"BottomLeft: Selected '{e.NewValue}'"); };

        DropDownList bottomRightDropDown = new ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.Top (bottomLeftDropDown),
            Title = "Bottom-Right",
            Source = new ListWrapper<string> (longGermanWords),
            ReadOnly = true,
            Text = "Grundstücksverkehrsgenehmigungszuständigkeitsübertragungsverordnung",
            BorderStyle = LineStyle.Dotted,
            Arrangement = ViewArrangement.RightResizable
        };

        bottomRightDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"BottomRight: Selected '{e.NewValue}'"); };

        contentFrame.AssignHotKeys = true;
        contentFrame.AssignHotKeysToSubViews ();

        contentFrame.Add (readOnlyLabel,
                          readOnlyDropDown,
                          editableLabel,
                          editableDropDown,
                          enumLabel,
                          enumDropDown,
                          positionLabel,
                          topLeftDropDown,
                          topRightDropDown,
                          bottomLeftDropDown,
                          bottomRightDropDown);

        _eventLog.SetViewToLog (window);

        window.Add (contentFrame, _eventLog);

        _app.Run (window);

        _app?.Dispose ();
        _app = null;
    }
}

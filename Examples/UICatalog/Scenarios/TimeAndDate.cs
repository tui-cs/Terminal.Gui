#nullable enable
using System;
using System.Globalization;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Time And Date", "Illustrates TimeEditor, DateEditor, DatePicker, and Prompt<DatePicker>")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("DateTime")]
public class TimeAndDate : Scenario
{
    private Label? _lblDateEditorValue;
    private Label? _lblTimeEditorValue;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new () { Title = GetQuitKeyAndName () };

        // ── TimeEditor examples ──────────────────────────────────────
        Label teLabel = new ()
        {
            X = 0,
            Y = 0,
            Text = "TimeEditor (based on TextValidateField):"
        };
        win.Add (teLabel);

        // Default culture time editor
        TimeEditor defaultTimeEditor = new ()
        {
            X = 0,
            Y = Pos.Bottom (teLabel),
            Value = DateTime.Now.TimeOfDay
        };
        defaultTimeEditor.ValueChanged += TimeEditorChanged;
        win.Add (defaultTimeEditor);

        Label defaultPatternLabel = new ()
        {
            X = Pos.Right (defaultTimeEditor) + 1,
            Y = Pos.Top (defaultTimeEditor),
            Text = $"Pattern: {CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern}"
        };
        win.Add (defaultPatternLabel);

        // 24-hour format time editor
        DateTimeFormatInfo format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();

        TimeEditor time24Editor = new ()
        {
            X = 0,
            Y = Pos.Bottom (defaultTimeEditor) + 1,
            Value = DateTime.Now.TimeOfDay,
            Format = format24h
        };
        time24Editor.ValueChanged += TimeEditorChanged;
        win.Add (time24Editor);

        Label time24PatternLabel = new ()
        {
            X = Pos.Right (time24Editor) + 1,
            Y = Pos.Top (time24Editor),
            Text = $"Pattern: {format24h.LongTimePattern}"
        };
        win.Add (time24PatternLabel);

        // Short time format time editor
        DateTimeFormatInfo shortFormat = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone ();
        shortFormat.LongTimePattern = shortFormat.ShortTimePattern;

        TimeEditor shortTimeEditor = new ()
        {
            X = 0,
            Y = Pos.Bottom (time24Editor) + 1,
            Value = DateTime.Now.TimeOfDay,
            Format = shortFormat
        };
        shortTimeEditor.ValueChanged += TimeEditorChanged;
        win.Add (shortTimeEditor);

        Label shortPatternLabel = new ()
        {
            X = Pos.Right (shortTimeEditor) + 1,
            Y = Pos.Top (shortTimeEditor),
            Text = $"Pattern: {shortFormat.LongTimePattern}"
        };
        win.Add (shortPatternLabel);

        _lblTimeEditorValue = new ()
        {
            X = 0,
            Y = Pos.Bottom (shortTimeEditor) + 1,
            Text = "TimeEditor Value: "
        };
        win.Add (_lblTimeEditorValue);

        // ── DateEditor examples ──────────────────────────────────────
        Label deLabel = new ()
        {
            X = 0,
            Y = Pos.Bottom (_lblTimeEditorValue) + 1,
            Text = "DateEditor (based on TextValidateField):"
        };
        win.Add (deLabel);

        // Default culture date editor
        DateEditor defaultDateEditor = new ()
        {
            X = 0,
            Y = Pos.Bottom (deLabel),
            Value = DateTime.Today
        };
        defaultDateEditor.ValueChanged += DateEditorChanged;
        win.Add (defaultDateEditor);

        Label defaultDatePatternLabel = new ()
        {
            X = Pos.Right (defaultDateEditor) + 1,
            Y = Pos.Top (defaultDateEditor),
            Text = $"Pattern: {CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern}"
        };
        win.Add (defaultDatePatternLabel);

        // US format date editor
        DateTimeFormatInfo usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();

        DateEditor usDateEditor = new ()
        {
            X = 0,
            Y = Pos.Bottom (defaultDateEditor) + 1,
            Value = DateTime.Today,
            Format = usFormat
        };
        usDateEditor.ValueChanged += DateEditorChanged;
        win.Add (usDateEditor);

        Label usDatePatternLabel = new ()
        {
            X = Pos.Right (usDateEditor) + 1,
            Y = Pos.Top (usDateEditor),
            Text = $"Pattern: {usFormat.ShortDatePattern}"
        };
        win.Add (usDatePatternLabel);

        // German format date editor
        DateTimeFormatInfo deFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("de-DE").DateTimeFormat.Clone ();

        DateEditor germanDateEditor = new ()
        {
            X = 0,
            Y = Pos.Bottom (usDateEditor) + 1,
            Value = DateTime.Today,
            Format = deFormat
        };
        germanDateEditor.ValueChanged += DateEditorChanged;
        win.Add (germanDateEditor);

        Label germanDatePatternLabel = new ()
        {
            X = Pos.Right (germanDateEditor) + 1,
            Y = Pos.Top (germanDateEditor),
            Text = $"Pattern: {deFormat.ShortDatePattern}"
        };
        win.Add (germanDatePatternLabel);

        _lblDateEditorValue = new ()
        {
            X = 0,
            Y = Pos.Bottom (germanDateEditor) + 1,
            Text = "DateEditor Value: "
        };
        win.Add (_lblDateEditorValue);

        // ── Inline DatePicker synced to default DateEditor ───────────
        Label dpLabel = new ()
        {
            X = Pos.Percent (50),
            Y = Pos.Top (deLabel),
            Text = "DatePicker (synced with default DateEditor):"
        };
        win.Add (dpLabel);

        DatePicker inlineDatePicker = new (defaultDateEditor.Value)
        {
            X = Pos.Percent (50),
            Y = Pos.Bottom (dpLabel)
        };
        win.Add (inlineDatePicker);

        // Sync DateEditor → DatePicker
        defaultDateEditor.ValueChanged += (_, e) =>
                                          {
                                              inlineDatePicker.Value = e.NewValue;
                                          };

        // Sync DatePicker → DateEditor
        inlineDatePicker.ValueChanged += (_, e) => defaultDateEditor.Value = e.NewValue;

        // ── Prompt<DatePicker> button ────────────────────────────────
        Button promptDatePickerButton = new ()
        {
            X = Pos.Percent (50),
            Y = Pos.Bottom (inlineDatePicker) + 1,
            Text = "Prompt<DatePicker>..."
        };
        win.Add (promptDatePickerButton);

        Label promptResultLabel = new ()
        {
            X = Pos.Percent (50),
            Y = Pos.Bottom (promptDatePickerButton),
            Text = "Prompt result: (none)"
        };
        win.Add (promptResultLabel);

        promptDatePickerButton.Accepting += (_, _) =>
                                            {
                                                DateTime? result = win.Prompt<DatePicker, DateTime?> (
                                                                                                      view: new DatePicker (defaultDateEditor.Value),
                                                                                                      resultExtractor: dp => dp.Value,
                                                                                                      beginInitHandler: prompt =>
                                                                                                                        {
                                                                                                                            prompt.Title = "Pick a Date";
                                                                                                                        });

                                                if (result is { } selectedDate)
                                                {
                                                    promptResultLabel.Text = $"Prompt result: {selectedDate:d}";
                                                    defaultDateEditor.Value = selectedDate;
                                                }
                                                else
                                                {
                                                    promptResultLabel.Text = "Prompt result: (cancelled)";
                                                }
                                            };

        app.Run (win);
    }

    private void DateEditorChanged (object? sender, ValueChangedEventArgs<DateTime> e)
    {
        _lblDateEditorValue!.Text = $"DateEditor Value: {e.NewValue:d}";
    }

    private void TimeEditorChanged (object? sender, ValueChangedEventArgs<TimeSpan> e)
    {
        _lblTimeEditorValue!.Text = $"TimeEditor Value: {e.NewValue}";
    }
}

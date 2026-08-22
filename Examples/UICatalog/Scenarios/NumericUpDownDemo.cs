#nullable enable
namespace UICatalog.Scenarios;

[ScenarioMetadata ("NumericUpDown", "Demonstrates the NumericUpDown View")]
[ScenarioCategory ("Controls")]
public class NumericUpDownDemo : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window mainWindow = new ();
        mainWindow.Title = GetQuitKeyAndName ();
        mainWindow.BorderStyle = LineStyle.None;

        NumericUpDownEditor<int> intEditor = new ()
        {
            X = 0,
            Y = 0,
            Title = "_int"
        };
        mainWindow.Add (intEditor);

        NumericUpDownEditor<float> floatEditor = new ()
        {
            X = Pos.Right (intEditor),
            Y = 0,
            Title = "_float"
        };
        mainWindow.Add (floatEditor);
        floatEditor.NumericUpDown!.Increment = 0.1F;
        floatEditor.NumericUpDown!.Format = "{0:0.0}";

        NumericUpDownEditor<int> hexEditor = new ()
        {
            X = Pos.Right (floatEditor),
            Y = 0,
            Title = "_hex"
        };
        mainWindow.Add (floatEditor);
        hexEditor.NumericUpDown!.Increment = 1;
        hexEditor.NumericUpDown!.Format = "{0:X}";
        mainWindow.Add (hexEditor);

        app.Run (mainWindow);
    }
}

internal sealed class NumericUpDownEditor<T> : View where T : notnull
{
    private NumericUpDown<T>? _numericUpDown;

    internal NumericUpDown<T>? NumericUpDown
    {
        get => _numericUpDown;
        set
        {
            if (value == _numericUpDown)
            {
                return;
            }

            _numericUpDown = value;

            if (_numericUpDown is not null && _value is not null)
            {
                _value.Text = _numericUpDown.Text;
            }
        }
    }

    private readonly TextField? _value;
    private readonly TextField? _format;
    private readonly TextField? _increment;

    internal NumericUpDownEditor ()
    {
        _numericUpDown = null;
        Title = "NumericUpDownEditor";
        BorderStyle = LineStyle.Single;
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
        TabStop = TabBehavior.TabGroup;
        CanFocus = true;

        Label label = new ()
        {
            Title = "_Value: ",
            Width = 12
        };
        label.TextFormatter.Alignment = Alignment.End;

        _value = new ()
        {
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Width = 8,
            Title = "Value"
        };
        _value.Accepting += ValuedOnAccept;
        Add (label, _value);

        label = new ()
        {
            Y = Pos.Bottom (_value),
            Width = 12,
            Title = "_Format: "
        };
        label.TextFormatter.Alignment = Alignment.End;

        _format = new ()
        {
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Title = "Format",
            Width = Dim.Width (_value)
        };
        _format.Accepting += FormatOnAccept;
        Add (label, _format);

        label = new ()
        {
            Y = Pos.Bottom (_format),
            Width = 12,
            Title = "_Increment: "
        };
        label.TextFormatter.Alignment = Alignment.End;

        _increment = new ()
        {
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Title = "Increment",
            Width = Dim.Width (_value)
        };

        _increment.Accepting += IncrementOnAccept;
        Add (label, _increment);

        _numericUpDown = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (_increment) + 1,
            Increment = NumericUpDown<int>.TryConvert (1, out T? increment) ? increment : default (T?)
        };
        _numericUpDown.ValueChanged += NumericUpDownOnValueChanged;
        _numericUpDown.IncrementChanged += NumericUpDownOnIncrementChanged;
        Add (_numericUpDown);

        _value.Text = _numericUpDown.Text;
        _format.Text = _numericUpDown.Format;
        _increment.Text = _numericUpDown!.Increment?.ToString ()!;

        return;

        void NumericUpDownOnIncrementChanged (object? o, EventArgs<T> eventArgs) { _increment.Text = _numericUpDown!.Increment?.ToString ()!; }

        void NumericUpDownOnValueChanged (object? o, ValueChangedEventArgs<T?> eventArgs) { _value.Text = _numericUpDown.Text; }

        void FormatOnAccept (object? o, EventArgs eventArgs)
        {
            if (_numericUpDown is null)
            {
                return;
            }

            try
            {
                // Test format to ensure it's valid
                _ = string.Format (_format.Text, _value);
                _numericUpDown.Format = _format.Text;

                _format.SetScheme (SuperView!.GetScheme ());
            }
            catch (FormatException)
            {
                _format.SchemeName = "Error";
            }
            catch (InvalidCastException)
            {
                _format.SchemeName = "Error";
            }
        }

        void IncrementOnAccept (object? o, EventArgs eventArgs)
        {
            if (_numericUpDown is null)
            {
                return;
            }

            try
            {
                if (string.IsNullOrEmpty (_value.Text))
                {
                    // Handle empty or null text if needed
                    _numericUpDown.Increment = default (T?)!;
                }
                else
                {
                    // Parse _value.Text and then convert to type T
                    _numericUpDown.Increment = (T)Convert.ChangeType (_increment.Text, typeof (T));
                }

                _increment.SetScheme (SuperView!.GetScheme ());
            }
            catch (FormatException)
            {
                _increment.SchemeName = "Error";
            }
            catch (InvalidCastException)
            {
                _increment.SchemeName = "Error";
            }
        }

        void ValuedOnAccept (object? sender, EventArgs e)
        {
            if (_numericUpDown is null)
            {
                return;
            }

            try
            {
                if (string.IsNullOrEmpty (_value.Text))
                {
                    // Handle empty or null text if needed
                    _numericUpDown.Value = default (T)!;
                }
                else
                {
                    // Parse _value.Text and then convert to type T
                    _numericUpDown.Value = (T)Convert.ChangeType (_value.Text, typeof (T));
                }

                _value.SetScheme (SuperView!.GetScheme ());
            }
            catch (FormatException)
            {
                _value.SchemeName = "Error";
            }
            catch (InvalidCastException)
            {
                _value.SchemeName = "Error";
            }
        }
    }
}

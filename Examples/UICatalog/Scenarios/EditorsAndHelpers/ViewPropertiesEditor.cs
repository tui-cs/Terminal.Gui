#nullable enable
using Terminal.Gui.Editor;

namespace UICatalog.Scenarios;

public class ViewPropertiesEditor : EditorBase
{
    private CheckBox? _canFocusCheckBox;
    private CheckBox? _enabledCheckBox;
    private OptionSelector<Orientation>? _orientationOptionSelector;
    private Editor? _text;

    public string DemoText
    {
        get => _text?.Text ?? string.Empty;
        set
        {
            if (_text is { } t)
            {
                t.Text = value;
            }
        }
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        _canFocusCheckBox = new CheckBox
        {
            Title = "CanFocus",
            X = 0,
            Y = 0,
            Value = ViewToEdit is { } ? ViewToEdit.CanFocus ? CheckState.Checked : CheckState.UnChecked : CheckState.UnChecked
        };

        _canFocusCheckBox.ValueChanged += (_, _) => { ViewToEdit?.CanFocus = _canFocusCheckBox.Value == CheckState.Checked; };
        Add (_canFocusCheckBox);

        _enabledCheckBox = new CheckBox
        {
            Title = "Enabled",
            X = Pos.Right (_canFocusCheckBox) + 1,
            Y = Pos.Top (_canFocusCheckBox),
            Value = ViewToEdit is { } ? ViewToEdit.Enabled ? CheckState.Checked : CheckState.UnChecked : CheckState.UnChecked
        };

        _enabledCheckBox.ValueChanged += (_, _) => { ViewToEdit?.Enabled = _enabledCheckBox.Value == CheckState.Checked; };
        Add (_enabledCheckBox);

        Label label = new () { X = Pos.Right (_enabledCheckBox) + 1, Y = Pos.Top (_enabledCheckBox), Text = "Orientation:" };

        _orientationOptionSelector = new OptionSelector<Orientation> { X = Pos.Right (label) + 1, Y = Pos.Top (label), Orientation = Orientation.Horizontal };

        _orientationOptionSelector.ValueChanged += (_, _) =>
                                                   {
                                                       if (ViewToEdit is IOrientation orientatedView)
                                                       {
                                                           orientatedView.Orientation = _orientationOptionSelector.Value!.Value;
                                                       }
                                                   };
        Add (label, _orientationOptionSelector);

        label = new Label { X = 0, Y = Pos.Bottom (_orientationOptionSelector), Text = "Text:" };

        _text = new Editor { X = Pos.Right (label) + 1, Y = Pos.Top (label), Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 2) };

        _text.Text = "This is demo text";
        _text.Document!.Changed += (_, _) => { ViewToEdit?.Text = _text.Text; };

        Add (label, _text);

        base.EndInit ();
    }

    protected override void OnViewToEditChanged ()
    {
        Enabled = ViewToEdit is { } and not AdornmentView;

        if (ViewToEdit is null or AdornmentView)
        {
            return;
        }
        _canFocusCheckBox!.Value = ViewToEdit.CanFocus ? CheckState.Checked : CheckState.UnChecked;
        _enabledCheckBox!.Value = ViewToEdit.Enabled ? CheckState.Checked : CheckState.UnChecked;

        if (ViewToEdit is IOrientation orientatedView)
        {
            _orientationOptionSelector!.Value = orientatedView.Orientation;
            _orientationOptionSelector.Enabled = true;
        }
        else
        {
            _orientationOptionSelector!.Enabled = false;
        }
        base.OnViewToEditChanged ();
    }
}

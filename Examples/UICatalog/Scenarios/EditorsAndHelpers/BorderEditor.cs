#nullable enable
using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

public class BorderEditor : AdornmentEditor
{
    private DropDownList? _osBorderStyle;
    private FlagSelector<BorderSettings>? _osBorderSettings;
    private OptionSelector<Side>? _osTabSide;
    private NumericUpDown<int>? _nudTabOffset;

    public BorderEditor ()
    {
        Title = "_Border";
        Initialized += BorderEditor_Initialized;
        AdornmentChanged += BorderEditor_AdornmentChanged;
    }

    private void BorderEditor_AdornmentChanged (object? sender, EventArgs e)
    {
        if (AdornmentToEdit is null)
        {
            return;
        }
        _osBorderStyle?.Value = ((Border)AdornmentToEdit).LineStyle.ToString ();
        _osBorderSettings?.Value = ((Border)AdornmentToEdit).Settings;

        if (AdornmentToEdit.View is null)
        {
            return;
        }
        _osTabSide?.Value = (AdornmentToEdit.View as BorderView)?.TabSide;

        if (AdornmentToEdit.View is BorderView bv)
        {
            _nudTabOffset?.Value = bv.TabOffset;
        }
    }

    private void BorderEditor_Initialized (object? sender, EventArgs e)
    {
        _osBorderStyle = new DropDownList
        {
            Y = Pos.Bottom (SubViews.ToArray () [^1]),
            Width = 10,
            Source = new ListWrapper<string> (new ObservableCollection<string> (Enum.GetNames<LineStyle> ())),
            Value = $"{(AdornmentToEdit as Border)?.LineStyle ?? LineStyle.None}",
            BorderStyle = LineStyle.Single,
            Title = "St_yle"
        };
        Add (_osBorderStyle);

        _osBorderStyle.ValueChanged += OnRbBorderStyleOnValueChanged;

        _osBorderSettings = new FlagSelector<BorderSettings>
        {
            Y = Pos.Bottom (_osBorderStyle),
            Value = (AdornmentToEdit as Border)?.Settings ?? BorderSettings.Default,
            Width = Dim.Auto (),
            BorderStyle = LineStyle.Single,
            Title = "S_ettings"
        };

        Add (_osBorderSettings);

        _osBorderSettings.ValueChanged += OnRbBorderSettingsOnValueChanged;

        _osTabSide = new OptionSelector<Side>
        {
            Y = Pos.Bottom (_osBorderSettings),
            Width = Dim.Width (_osBorderStyle),
            Value = (AdornmentToEdit?.View as BorderView)?.TabSide ?? Side.Top,
            BorderStyle = LineStyle.Single,
            Title = "_Side",
            Enabled = (AdornmentToEdit as Border)?.Settings.HasFlag (BorderSettings.Tab) ?? false
        };

        _osTabSide.ValueChanged += OnHeaderSideChanged;
        Add (_osTabSide);

        Label labelOffset = new () { Title = "Tab _Offset:", Y = Pos.Bottom (_osTabSide) };

        _nudTabOffset = new NumericUpDown<int>
        {
            X = Pos.Right (labelOffset) + 1,
            Y = Pos.Top (labelOffset),
            Value = (AdornmentToEdit?.View as BorderView)?.TabOffset ?? 0,
            Enabled = (AdornmentToEdit as Border)?.Settings.HasFlag (BorderSettings.Tab) ?? false
        };

        _nudTabOffset.ValueChanged += OnHeaderOffsetChanged;
        Add (labelOffset, _nudTabOffset);

        return;

        void OnRbBorderStyleOnValueChanged (object? s, ValueChangedEventArgs<string?> args)
        {
            if (AdornmentToEdit is not Border border)
            {
                return;
            }

            if (args.NewValue is { })
            {
                if (Enum.TryParse (args.NewValue, out LineStyle style))
                {
                    border.LineStyle = style;
                }
            }

            border.View?.SetNeedsDraw ();
            SetNeedsLayout ();
        }

        void OnRbBorderSettingsOnValueChanged (object? s, EventArgs<BorderSettings?> args)
        {
            if (AdornmentToEdit is not Border border)
            {
                return;
            }

            if (args.Value is { })
            {
                border.Settings = (BorderSettings)args.Value;
            }

            _nudTabOffset!.Enabled = border.Settings.HasFlag (BorderSettings.Tab);
            _osTabSide!.Enabled = border.Settings.HasFlag (BorderSettings.Tab);

            border.View?.SetNeedsDraw ();
            SetNeedsLayout ();
        }

        void OnHeaderSideChanged (object? _, EventArgs<Side?> args)
        {
            if (AdornmentToEdit is not Border border || args.Value is null)
            {
                return;
            }

            ((BorderView)border.View!).TabSide = args.Value.Value;
            border.View?.SetNeedsLayout ();
        }

        void OnHeaderOffsetChanged (object? _, ValueChangedEventArgs<int> args)
        {
            if (AdornmentToEdit is not Border border)
            {
                return;
            }

            ((BorderView)border.View!).TabOffset = args.NewValue;
            border.View?.SetNeedsLayout ();
        }
    }
}

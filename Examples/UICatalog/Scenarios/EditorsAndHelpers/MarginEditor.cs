#nullable enable
namespace UICatalog.Scenarios;

public class MarginEditor : AdornmentEditor
{
    public MarginEditor ()
    {
        Title = "_Margin";
        Initialized += MarginEditor_Initialized;
        AdornmentChanged += MarginEditor_AdornmentChanged;
    }

    // Sentinel value representing ShadowStyles? = null (no shadow, no thickness)
    private const int SHADOW_NULL_SENTINEL = -1;

    private OptionSelector? _optionsShadow;

    private FlagSelector? _flagSelectorTransparent;

    private static int ShadowStyleToInt (ShadowStyles? style) => style.HasValue ? (int)style.Value : SHADOW_NULL_SENTINEL;
    private static ShadowStyles? IntToShadowStyle (int? value) => value is SHADOW_NULL_SENTINEL or null ? null : (ShadowStyles)value.Value;

    private void MarginEditor_AdornmentChanged (object? sender, EventArgs e)
    {
        if (AdornmentToEdit is { })
        {
            _optionsShadow?.Value = ShadowStyleToInt (((Margin)AdornmentToEdit).ShadowStyle);
        }

        if (AdornmentToEdit is { })
        {
            _flagSelectorTransparent?.Value = (int)((Margin)AdornmentToEdit).ViewportSettings;
        }
    }

    private void MarginEditor_Initialized (object? sender, EventArgs e)
    {
        ExpanderButton?.Collapsed = false;

        _optionsShadow = new OptionSelector
        {
            X = 0,
            Y = Pos.Bottom (SubViews.ElementAt (SubViews.Count - 1)),
            Title = "_Shadow",
            BorderStyle = LineStyle.Single,
            AssignHotKeys = true,
            Values = [SHADOW_NULL_SENTINEL, (int)ShadowStyles.None, (int)ShadowStyles.Opaque, (int)ShadowStyles.Transparent],
            Labels = ["Disabled", "None", "Opaque", "Transparent"]
        };

        if (AdornmentToEdit is { })
        {
            _optionsShadow.Value = ShadowStyleToInt (((Margin)AdornmentToEdit).ShadowStyle);
        }

        _optionsShadow.ValueChanged += (_, args) => ((Margin)AdornmentToEdit!).ShadowStyle = IntToShadowStyle (args.NewValue);

        Add (_optionsShadow);

        _flagSelectorTransparent = new FlagSelector<ViewportSettingsFlags>
        {
            X = 0, Y = Pos.Bottom (_optionsShadow), Title = "_ViewportSettings", BorderStyle = LineStyle.Single
        };
        _flagSelectorTransparent.Values = [(int)ViewportSettingsFlags.Transparent, (int)ViewportSettingsFlags.TransparentMouse];
        _flagSelectorTransparent.Labels = ["Transparent", "TransparentMouse"];
        _flagSelectorTransparent.AssignHotKeys = true;

        Add (_flagSelectorTransparent);

        if (AdornmentToEdit is { })
        {
            _flagSelectorTransparent.Value = (int)((Margin)AdornmentToEdit).ViewportSettings;
        }

        _flagSelectorTransparent.ValueChanged += (_, args) => { ((Margin)AdornmentToEdit!).ViewportSettings = (ViewportSettingsFlags)args.NewValue!; };
    }
}

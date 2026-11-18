#nullable enable
namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public sealed class ArrangementEditor : EditorBase
{
    public ArrangementEditor ()
    {
        Title = "ArrangementEditor";
        TabStop = TabBehavior.TabGroup;

        Initialized += ArrangementEditor_Initialized;

        Add (_arrangementSelector);
    }

    private readonly FlagSelector<ViewArrangement> _arrangementSelector = new () { Orientation = Orientation.Vertical };

    protected override void OnViewToEditChanged ()
    {
        _arrangementSelector.Enabled = ViewToEdit is { } and not AdornmentView;

        _arrangementSelector.ValueChanged -= ArrangementFlagsOnValueChanged;

        // Set the appropriate options in the slider based on _viewToEdit.Arrangement
        if (ViewToEdit is { })
        {
            _arrangementSelector.Value = ViewToEdit.Arrangement;
        }

        _arrangementSelector.ValueChanged += ArrangementFlagsOnValueChanged;

        base.OnViewToEditChanged ();
    }

    private void ArrangementFlagsOnValueChanged (object? sender, EventArgs<ViewArrangement?> e)
    {
        if (ViewToEdit is null || e.Value is null)
        {
            return;
        }
        ViewToEdit.Arrangement = (ViewArrangement)e.Value;

        if (ViewToEdit.Arrangement.HasFlag (ViewArrangement.Overlapped))
        {
            ViewToEdit.ShadowStyle = ShadowStyles.Transparent;
            ViewToEdit.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent);
        }
        else
        {
            ViewToEdit.ShadowStyle = null;
            ViewToEdit.SchemeName = ViewToEdit!.SuperView!.SchemeName;
        }

        ViewToEdit.BorderStyle = ViewToEdit.Arrangement.HasFlag (ViewArrangement.Movable) ? LineStyle.Double : LineStyle.Single;
    }

    private void ArrangementEditor_Initialized (object? sender, EventArgs e) => _arrangementSelector.ValueChanged += ArrangementFlagsOnValueChanged;
}

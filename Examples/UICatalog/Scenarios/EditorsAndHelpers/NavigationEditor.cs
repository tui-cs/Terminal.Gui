#nullable enable
namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for TabStop and related Navigation settings.
/// </summary>
public sealed class NavigationEditor : EditorBase
{
    public NavigationEditor ()
    {
        Title = "NavigationEditor";
        TabStop = TabBehavior.TabGroup;

        Add (_tabBehaviorSelector);
    }

    private readonly OptionSelector<TabBehavior> _tabBehaviorSelector = new () { Orientation = Orientation.Vertical };

    protected override void OnViewToEditChanged ()
    {
        _tabBehaviorSelector.Enabled = ViewToEdit is { } and not AdornmentView;

        _tabBehaviorSelector.ValueChanged -= TabStopOnValueChanged;

        if (ViewToEdit is { })
        {
            _tabBehaviorSelector.Value = ViewToEdit.TabStop;
        }

        _tabBehaviorSelector.ValueChanged += TabStopOnValueChanged;

        base.OnViewToEditChanged ();
    }

    private void TabStopOnValueChanged (object? sender, EventArgs<TabBehavior?> e)
    {
        if (ViewToEdit is null || e.Value is null)
        {
            return;
        }
        ViewToEdit.TabStop = (TabBehavior)e.Value;
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();
        _tabBehaviorSelector.ValueChanged += TabStopOnValueChanged;
    }
}

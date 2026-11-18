#nullable enable
namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public class AdornmentsEditor : EditorBase
{
    public AdornmentsEditor ()
    {
        Title = "AdornmentsEditor";

        TabStop = TabBehavior.TabGroup;

        Initialized += AdornmentsEditor_Initialized;

        SchemeName = "Dialog";

        Add (_tabs);

        Height = Dim.Auto ();
        Width = Dim.Auto ();
    }

    private Tabs _tabs = new Tabs ()
    {
        TabSide = Side.Left,
    };

    public MarginEditor? MarginEditor { get; set; }
    public BorderEditor? BorderEditor { get; private set; }
    public PaddingEditor? PaddingEditor { get; private set; }

    /// <inheritdoc/>
    protected override void OnViewToEditChanged ()
    {
        MarginEditor?.AdornmentToEdit = ViewToEdit?.Margin ?? null;

        BorderEditor?.AdornmentToEdit = ViewToEdit?.Border ?? null;

        PaddingEditor?.AdornmentToEdit = ViewToEdit?.Padding ?? null;
        base.OnViewToEditChanged ();
    }

    private void AdornmentsEditor_Initialized (object? sender, EventArgs e)
    {
        ExpanderButton?.Orientation = Orientation.Horizontal;

        MarginEditor = new MarginEditor { };
        _tabs.Add (MarginEditor);

        BorderEditor = new BorderEditor { };
        _tabs.Add (BorderEditor);

        PaddingEditor = new PaddingEditor { };
        _tabs.Add (PaddingEditor);

        // Set all tabs to Dim.Auto 
        MarginEditor.Width = Dim.Auto ();
        BorderEditor.Width = Dim.Auto ();
        PaddingEditor.Width = Dim.Auto ();
        MarginEditor.Height = Dim.Auto ();
        BorderEditor.Height = Dim.Auto ();
        PaddingEditor.Height = Dim.Auto ();
        Layout ();

        // Get the largest 
        int max = new [] { MarginEditor.Frame.Width, BorderEditor.Frame.Width, PaddingEditor.Frame.Width }.Max ();
        _tabs.Width = Dim.Auto (minimumContentDim: max);

        max = new [] { MarginEditor.Frame.Height, BorderEditor.Frame.Height, PaddingEditor.Frame.Height }.Max ();
        _tabs.Height = Dim.Auto (minimumContentDim: max);

        MarginEditor.Width = Dim.Fill ();
        BorderEditor.Width = Dim.Fill ();
        PaddingEditor.Width = Dim.Fill ();
        MarginEditor.Height = Dim.Fill ();
        BorderEditor.Height = Dim.Fill ();
        PaddingEditor.Height = Dim.Fill ();

        MarginEditor.AdornmentToEdit = ViewToEdit?.Margin ?? null;
        BorderEditor.AdornmentToEdit = ViewToEdit?.Border ?? null;
        PaddingEditor.AdornmentToEdit = ViewToEdit?.Padding ?? null;
    }
}

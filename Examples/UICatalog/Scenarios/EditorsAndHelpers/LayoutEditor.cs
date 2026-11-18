#nullable enable
namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public class LayoutEditor : EditorBase
{
    public LayoutEditor ()
    {
        Title = "_LayoutEditor";
        CanFocus = true;

        Initialized += LayoutEditor_Initialized;
    }

    private PosEditor? _xEditor;
    private PosEditor? _yEditor;

    private DimEditor? _widthEditor;
    private DimEditor? _heightEditor;

    protected override void OnViewToEditChanged ()
    {
        Enabled = ViewToEdit is { } and not AdornmentView;

        _xEditor?.ViewToEdit = ViewToEdit;

        _yEditor?.ViewToEdit = ViewToEdit;

        _widthEditor?.ViewToEdit = ViewToEdit;

        _heightEditor?.ViewToEdit = ViewToEdit;

        base.OnViewToEditChanged ();
    }

    private void LayoutEditor_Initialized (object? sender, EventArgs e)
    {
        _xEditor = new PosEditor { Title = "_X", BorderStyle = LineStyle.None, Dimension = Dimension.Width };

        _yEditor = new PosEditor { Title = "_Y", BorderStyle = LineStyle.None, Dimension = Dimension.Height, X = Pos.Right (_xEditor) + 1 };

        _widthEditor = new DimEditor { Title = "_Width", BorderStyle = LineStyle.None, Dimension = Dimension.Width, X = Pos.Right (_yEditor) + 1 };

        _heightEditor = new DimEditor { Title = "_Height", BorderStyle = LineStyle.None, Dimension = Dimension.Height, X = Pos.Right (_widthEditor) + 1 };

        Add (_xEditor, _yEditor, _widthEditor, _heightEditor);
    }
}

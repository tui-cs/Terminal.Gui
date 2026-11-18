#nullable enable

namespace UICatalog.Scenarios;

public class VisualRoleViewer : View
{
    public VisualRoleViewer ()
    {
        CanFocus = false;
        Height = Dim.Auto (DimAutoStyle.Text);
        Width = Dim.Auto (DimAutoStyle.Text);
    }

    private VisualRole? _role;

    public VisualRole? Role
    {
        get => _role;
        set
        {
            _role = value;
            Text = $"{Role?.ToString ()?.PadRight (10)} {SchemeName}";
        }
    }

    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (role != Role)
        {
            currentAttribute = GetAttributeForRole (Role!.Value);

            return true;
        }

        return base.OnGettingAttributeForRole (in role, ref currentAttribute);
    }
}

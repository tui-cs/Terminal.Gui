namespace Terminal.Gui.Views;

/// <summary><see cref="IAllowedType"/> that allows selection of any types (*.*).</summary>
public class AllowedTypeAny : IAllowedType
{
    /// <inheritdoc/>
    public bool IsAllowed (string path) => true;

    /// <summary>Returns a string representation of this <see cref="AllowedTypeAny"/>.</summary>
    /// <returns></returns>
    public override string ToString () => Strings.fdAnyFiles + " (*.*)";
}

namespace Terminal.Gui.Views;

/// <summary>Provides filtering for a <see cref="TreeView{T}"/>.</summary>
public interface ITreeViewFilter<in T> where T : class
{
    /// <summary>Return <see langword="true"/> if the <paramref name="model"/> should be included in the tree.</summary>
    bool IsMatch (T model);
}

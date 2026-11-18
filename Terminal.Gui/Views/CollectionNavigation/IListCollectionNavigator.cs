#nullable disable
using System.Collections;

namespace Terminal.Gui.Views;

/// <summary>
///     <see cref="ICollectionNavigator"/> sub-interface for <see cref="ListView"/> and <see cref="TreeView"/>. See also
///     <see cref="ListView"/> / <see cref="TreeView"/>
/// </summary>
public interface IListCollectionNavigator : ICollectionNavigator
{
    /// <summary>The collection of objects to search. <see cref="object.ToString()"/> is used to search the collection.</summary>
    IList Collection { get; set; }
}

#nullable disable
using System.Collections;
using System.Collections.Specialized;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides data and rendering for <see cref="ListView"/>. Implement this interface to provide custom rendering
///     or to wrap custom data sources.
/// </summary>
/// <remarks>
///     <para>
///         The default implementation is <see cref="ListWrapper{T}"/> which renders items using
///         <see cref="object.ToString()"/>.
///     </para>
///     <para>
///         Implementors must manage their own marking state and raise <see cref="CollectionChanged"/> when the
///         underlying data changes.
///     </para>
/// </remarks>
public interface IListDataSource : IDisposable
{
    /// <summary>
    ///     Raised when items are added, removed, moved, or the entire collection is refreshed.
    /// </summary>
    /// <remarks>
    ///     <see cref="ListView"/> subscribes to this event to update its display and content size when the data
    ///     changes. Implementations should raise this event whenever the underlying collection changes, unless
    ///     <see cref="SuspendCollectionChangedEvent"/> is <see langword="true"/>.
    /// </remarks>
    event NotifyCollectionChangedEventHandler CollectionChanged;

    /// <summary>Gets the number of items in the data source.</summary>
    int Count { get; }

    /// <summary>Determines whether the specified item is marked.</summary>
    /// <param name="item">The zero-based index of the item.</param>
    /// <returns><see langword="true"/> if the item is marked; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    ///     <see cref="ListView"/> calls this method to determine whether to render the item with a mark indicator when
    ///     <see cref="ListView.ShowMarks"/> is <see langword="true"/>.
    /// </remarks>
    bool IsMarked (int item);

    /// <summary>Gets the width in columns of the widest item in the data source.</summary>
    /// <remarks>
    ///     <see cref="ListView"/> uses this value to set its horizontal content size for scrolling.
    /// </remarks>
    int MaxItemLength { get; }

    /// <summary>Renders the specified item to the <see cref="ListView"/>.</summary>
    /// <param name="listView">The <see cref="ListView"/> to render to.</param>
    /// <param name="selected">
    ///     <see langword="true"/> if the item is currently selected; otherwise <see langword="false"/>.
    /// </param>
    /// <param name="item">The zero-based index of the item to render.</param>
    /// <param name="col">The column in <paramref name="listView"/> Viewport where rendering starts.</param>
    /// <param name="row">The row in <paramref name="listView"/> Viewport where rendering starts.</param>
    /// <param name="width">The width available for rendering.</param>
    /// <param name="viewportX">The horizontal scroll offset.</param>
    /// <remarks>
    ///     <para>
    ///         <see cref="ListView"/> calls this method for each visible item during rendering. The color scheme will be
    ///         set based on selection state before this method is called.
    ///     </para>
    ///     <para>
    ///         Implementations must fill the entire <paramref name="width"/> to avoid rendering artifacts.
    ///     </para>
    /// </remarks>
    void Render (ListView listView, bool selected, int item, int col, int row, int width, int viewportX = 0);

    /// <summary>Sets the marked state of the specified item.</summary>
    /// <param name="item">The zero-based index of the item.</param>
    /// <param name="value"><see langword="true"/> to mark the item; <see langword="false"/> to unmark it.</param>
    /// <remarks>
    ///     <see cref="ListView"/> calls this method when the user toggles marking (e.g., via the SPACE key) if
    ///     <see cref="ListView.ShowMarks"/> is <see langword="true"/>.
    /// </remarks>
    void SetMark (int item, bool value);

    /// <summary>
    ///     Gets or sets whether the <see cref="CollectionChanged"/> event should be suppressed.
    /// </summary>
    /// <remarks>
    ///     Set to <see langword="true"/> to prevent <see cref="CollectionChanged"/> from being raised during bulk
    ///     operations. Set back to <see langword="false"/> to resume event notifications.
    /// </remarks>
    bool SuspendCollectionChangedEvent { get; set; }

    /// <summary>Returns the underlying data source as an <see cref="IList"/>.</summary>
    /// <returns>The data source as an <see cref="IList"/>.</returns>
    /// <remarks>
    ///     <see cref="ListView"/> uses this method to access individual items for events like
    ///     <see cref="ListView.ValueChanged"/> and to enable keyboard search via
    ///     <see cref="ListView.KeystrokeNavigator"/>.
    /// </remarks>
    IList ToList ();

    /// <summary>Renders the mark indicator for an item. Override to customize mark rendering.</summary>
    /// <param name="listView">The <see cref="ListView"/> rendering to.</param>
    /// <param name="item">The zero-based index of the item.</param>
    /// <param name="row">The row in the viewport where the item is being rendered.</param>
    /// <param name="isMarked">Whether the item is currently marked.</param>
    /// <param name="markMultiple">Whether multiple marking is enabled (checkbox vs radio button style).</param>
    /// <returns>
    ///     <see langword="true"/> if custom rendering was performed; <see langword="false"/> to use default rendering.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The default implementation returns <see langword="false"/>, causing <see cref="ListView"/> to use its
    ///         default mark rendering (checkbox glyphs when <paramref name="markMultiple"/> is <see langword="true"/>,
    ///         radio button glyphs when <see langword="false"/>).
    ///     </para>
    ///     <para>
    ///         Override and return <see langword="true"/> to provide custom mark glyphs, positioning, or attributes.
    ///         When this returns <see langword="true"/>, you must render marks yourself (if desired) and
    ///         <see cref="Render"/> will be called starting at column 0.
    ///     </para>
    /// </remarks>
    bool RenderMark (ListView listView, int item, int row, bool isMarked, bool markMultiple) => false;
}

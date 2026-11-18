// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls
// by phillip.piper@gmail.com). Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

namespace Terminal.Gui.Views;

/// <summary>Event args for the <see cref="TreeView{T}.DrawLine"/> event</summary>
/// <typeparam name="T"></typeparam>
public class DrawTreeViewLineEventArgs<T> where T : class
{
    /// <summary>Set to true to cancel drawing (e.g. if you have already manually drawn content).</summary>
    public bool Handled { get; set; }

    /// <summary>
    ///     If line contains a branch that can be expanded/collapsed then this is the index in <see cref="Cells"/> at
    ///     which the symbol is (or null for leaf elements).
    /// </summary>
    public int? IndexOfExpandCollapseSymbol { get; init; }

    /// <summary>
    ///     The notional index in <see cref="Cells"/> which contains the first character of the
    ///     <see cref="TreeView{T}.AspectGetter"/> text (i.e. after all branch lines and expansion/collapse symbols).
    /// </summary>
    /// <remarks>May be negative or outside of bounds of <see cref="Cells"/> if the view has been scrolled horizontally.</remarks>
    public int IndexOfModelText { get; init; }

    /// <summary>The object at this line in the tree</summary>
    public T? Model { get; init; }

    /// <summary>
    ///     The content and color of each cell that will be rendered. You can modify these to change what is rendered.
    /// </summary>
    /// <remarks>Changing the length of this collection may result in corrupt rendering.</remarks>
    public List<Cell>? Cells { get; init; }

    /// <summary>The <see cref="TreeView{T}"/> that is performing the rendering.</summary>
    public TreeView<T>? Tree { get; init; }

    /// <summary>The line within tree view bounds that is being rendered</summary>
    public int Y { get; init; }
}

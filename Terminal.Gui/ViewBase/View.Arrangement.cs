namespace Terminal.Gui.ViewBase;

public partial class View
{
    /// <summary>
    ///     Gets or sets the user actions that are enabled for the arranging this view within it's <see cref="SuperView"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Arrangement Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/arrangement.html"/>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This example demonstrates how to create a resizable splitter between two views using
    ///         <see cref="ViewArrangement.LeftResizable"/>:
    ///     </para>
    ///     <code>
    /// // Create left pane that fills remaining space
    /// View leftPane = new ()
    /// {
    ///     X = 0,
    ///     Y = 0,
    ///     Width = Dim.Fill (Dim.Func (_ => rightPane.Frame.Width)),
    ///     Height = Dim.Fill (),
    ///     CanFocus = true
    /// };
    /// 
    /// // Create right pane with resizable left border (acts as splitter)
    /// View rightPane = new ()
    /// {
    ///     X = Pos.Right (leftPane) - 1,
    ///     Y = 0,
    ///     Width = Dim.Fill (),
    ///     Height = Dim.Fill (),
    ///     Arrangement = ViewArrangement.LeftResizable,
    ///     BorderStyle = LineStyle.Single,
    ///     SuperViewRendersLineCanvas = true,
    ///     CanFocus = true
    /// };
    /// rightPane.Border.Thickness = new (1, 0, 0, 0); // Only left border
    /// 
    /// container.Add (leftPane, rightPane);
    /// </code>
    ///     <para>
    ///         The right pane's left border acts as a draggable splitter. The left pane's width automatically adjusts
    ///         to fill the remaining space using <c>Dim.Fill</c> with a function that subtracts the right pane's width.
    ///     </para>
    /// </example>
    public ViewArrangement Arrangement { get; set; }
}

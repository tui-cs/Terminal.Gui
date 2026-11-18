namespace Terminal.Gui.Views;

/// <summary>
///     <para>Describes an overlay element that is rendered either before or after a series.</para>
///     <para>
///         Annotations can be positioned either in screen space (e.g. a legend) or in graph space (e.g. a line showing
///         high point)
///     </para>
///     <para>Unlike <see cref="ISeries"/>, annotations are allowed to draw into graph margins</para>
/// </summary>
public interface IAnnotation
{
    /// <summary>
    ///     True if annotation should be drawn before <see cref="ISeries"/>.  This allows Series and later annotations to
    ///     potentially draw over the top of this annotation.
    /// </summary>
    bool BeforeSeries { get; }

    /// <summary>
    ///     Called once after series have been rendered (or before if <see cref="BeforeSeries"/> is true). Use
    ///     methods like <see cref="View.AddStr(string)"/> and <see cref="View.AddRune(Rune)"/> to draw. Use
    ///     <see cref="View.Viewport"/> to avoid drawing outside of graph.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="drawContext"></param>
    void Render (GraphView graph, DrawContext? drawContext);
}

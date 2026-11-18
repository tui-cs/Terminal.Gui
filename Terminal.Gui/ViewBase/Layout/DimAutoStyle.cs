namespace Terminal.Gui.ViewBase;

/// <summary>
///     Specifies how <see cref="Dim.Auto"/> will compute the dimension.
///     See the <a href="../docs/dimauto.md">Dim.Auto Deep Dive</a> for comprehensive documentation.
/// </summary>
[Flags]
public enum DimAutoStyle
{
    /// <summary>
    ///     The dimensions will be computed based on the View's <see cref="View.GetContentSize ()"/> and/or
    ///     <see cref="View.SubViews"/>.
    ///     <para>
    ///         If <see cref="View.ContentSizeTracksViewport"/> is <see langword="true"/>, <see cref="View.GetContentSize ()"/>
    ///         will be used to determine the dimension.
    ///     </para>
    ///     <para>
    ///         Otherwise, the SubView in <see cref="View.SubViews"/> with the largest corresponding position plus dimension
    ///         will determine the dimension.
    ///     </para>
    ///     <para>
    ///         The corresponding dimension of the view's <see cref="View.Text"/> will be ignored.
    ///     </para>
    /// </summary>
    Content = 1,

    /// <summary>
    ///     <para>
    ///         The corresponding dimension of the view's <see cref="View.Text"/>, formatted using the
    ///         <see cref="View.TextFormatter"/> settings,
    ///         will be used to determine the dimension.
    ///     </para>
    ///     <para>
    ///         The corresponding dimensions of <see cref="View.GetContentSize ()"/> and/or <see cref="View.SubViews"/> will be
    ///         ignored.
    ///     </para>
    ///     <para>
    ///         If <see cref="DimAuto.MaximumContentDim"/> is set, the dimension will be the maximum of the formatted text and
    ///         the
    ///         dimension provided by <see cref="DimAuto.MaximumContentDim"/>. Otherwise, the dimension will be that of the
    ///         formatted text.
    ///     </para>
    /// </summary>
    Text = 2,

    /// <summary>
    ///     The dimension will be computed using the largest of the view's <see cref="View.Text"/>,
    ///     <see cref="View.GetContentSize ()"/>, and
    ///     <see cref="View.SubViews"/> corresponding dimension
    /// </summary>
    Auto = Content | Text
}

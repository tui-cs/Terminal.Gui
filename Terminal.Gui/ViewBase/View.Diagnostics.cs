namespace Terminal.Gui.ViewBase;

public partial class View
{
    // TODO: Make this a configuration property
    /// <summary>
    ///     Gets or sets whether diagnostic information will be drawn. This is a bit-field of <see cref="ViewDiagnosticFlags"/>
    ///     .e <see cref="View"/> diagnostics.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="AdornmentView.Diagnostics"/> gets set to this property by default, enabling
    ///         <see cref="ViewDiagnosticFlags.Ruler"/> and <see cref="ViewDiagnosticFlags.Thickness"/>.
    ///     </para>
    /// </remarks>
    public static ViewDiagnosticFlags Diagnostics { get; set; }
}

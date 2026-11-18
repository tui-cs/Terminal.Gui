namespace Terminal.Gui.ViewBase;

/// <summary>
///     The lightweight Padding settings for a <see cref="View"/>. Accessed via <see cref="View.Padding"/>.
///     A <see cref="PaddingView"/> is created lazily when SubViews are added.
/// </summary>
/// <remarks>
///     <para>See the <see cref="AdornmentImpl"/> class.</para>
/// </remarks>
public class Padding : AdornmentImpl
{
    /// <inheritdoc />
    public override Rectangle GetFrame ()
    {
        if (Parent is { })
        {
            return Parent.Border.Thickness.GetInside (Parent!.Border.GetFrame ());
        }
        else
        {
            return Rectangle.Empty;
        }
    }

    /// <inheritdoc/>
    protected override AdornmentView CreateView () => new PaddingView (this);
}

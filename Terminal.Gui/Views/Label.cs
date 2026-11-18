namespace Terminal.Gui.Views;

/// <summary>
///     Displays text that describes the View next in the <see cref="View.SubViews"/>. When
///     the user presses a hotkey that matches the <see cref="View.HotKey"/> of the Label, the next <see cref="View"/> in
///     <see cref="View.SubViews"/> will be activated.
/// </summary>
/// <remarks>
/// <img src="../images/views/Label.gif" alt="Label demo"/>
///     <para>
///         Title and Text are the same property. When Title is set Text s also set. When Text is set Title is also set.
///     </para>
///     <para>
///         If <see cref="View.CanFocus"/> is <see langword="false"/> and the use clicks on the Label,
///         the <see cref="Command.HotKey"/> will be invoked on the next <see cref="View"/> in
///         <see cref="View.SubViews"/>.
///     </para>
/// </remarks>
public class Label : View, IDesignable
{
    /// <inheritdoc/>
    public Label ()
    {
        Height = Dim.Auto (DimAutoStyle.Text);
        Width = Dim.Auto (DimAutoStyle.Text);

        TitleChanged += Label_TitleChanged;
    }

    private void Label_TitleChanged (object? sender, EventArgs<string> e)
    {
        SetTextDirect (e.Value);
        TextFormatter.HotKeySpecifier = HotKeySpecifier;
    }

    /// <inheritdoc/>
    protected override void OnTextChanged ()
    {
        Title = Text;

        base.OnTextChanged ();
    }

    /// <inheritdoc/>
    public override Rune HotKeySpecifier { get => base.HotKeySpecifier; set => TextFormatter.HotKeySpecifier = base.HotKeySpecifier = value; }

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        // If Label can't focus, forward HotKey to the next peer in the SubView list
        if (CanFocus || !HotKey.IsValid)
        {
            return base.OnActivating (args);
        }
        int me = SuperView?.SubViews.IndexOf (this) ?? -1;

        if (me == -1 || !(me < SuperView?.SubViews.Count - 1))
        {
            return base.OnActivating (args);
        }
        bool handled = SuperView?.SubViews.ElementAt (me + 1).InvokeCommand (Command.HotKey) == true;

        if (!handled)
        {
            return base.OnActivating (args);
        }
        args.Handled = true;

        return true;
    }

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Text = "_Label";

        return true;
    }

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:2000";
}

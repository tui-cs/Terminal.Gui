#nullable enable

namespace UICatalog.Scenarios;

public class SchemeViewer : FrameView
{
    public SchemeViewer ()
    {
        TabStop = TabBehavior.TabStop;
        CanFocus = true;
        Height = Dim.Auto ();
        Width = Dim.Auto ();

        VisualRoleViewer? prevRoleViewer = null;

        foreach (VisualRole role in Enum.GetValues<VisualRole> ())
        {
            var roleViewer = new VisualRoleViewer
            {
                Role = role
            };

            if (prevRoleViewer is not null)
            {
                roleViewer.Y = Pos.Bottom (prevRoleViewer);
            }

            base.Add (roleViewer);

            prevRoleViewer = roleViewer;
        }
    }

    /// <inheritdoc/>
    protected override bool OnSchemeNameChanging (ValueChangingEventArgs<string?> args)
    {
        Title = args.NewValue ?? "null";

        foreach (VisualRoleViewer v in SubViews.OfType<VisualRoleViewer> ())
        {
            v.SchemeName = args.NewValue;
        }

        return base.OnSchemeNameChanging (args);
    }
}

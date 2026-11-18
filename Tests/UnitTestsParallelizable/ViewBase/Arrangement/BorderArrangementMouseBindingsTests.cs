namespace ViewBaseTests.Arrangement;

// Copilot
public class BorderArrangementMouseBindingsTests
{
    [Fact]
    public void BorderView_DefaultMouseBinding_BindsArrangeToLeftButtonPressed ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable root = new () { Width = 40, Height = 20 };
        View view = CreateArrangeableView ();
        root.Add (view);
        app.Begin (root);

        BorderView borderView = (BorderView)view.Border.View!;

        Assert.True (borderView.MouseBindings.TryGet (MouseFlags.LeftButtonPressed, out MouseBinding binding));
        Assert.Contains (Command.Arrange, binding.Commands);
    }

    [Fact]
    public void BorderView_MouseBinding_DeterminesWhichMouseFlagsStartArrangeDrag ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable root = new () { Width = 40, Height = 20 };
        View view = CreateArrangeableView ();
        root.Add (view);
        app.Begin (root);

        BorderView borderView = (BorderView)view.Border.View!;
        Arranger arranger = borderView.Arranger;

        borderView.MouseBindings.Remove (MouseFlags.LeftButtonPressed);
        borderView.MouseBindings.Add (MouseFlags.LeftButtonPressed | MouseFlags.Alt, Command.Arrange);

        borderView.NewMouseEvent (CreateBorderPress (MouseFlags.LeftButtonPressed));

        Assert.False (arranger.IsDragging);

        if (app.Mouse.IsGrabbed (borderView))
        {
            app.Mouse.UngrabMouse ();
        }

        borderView.NewMouseEvent (CreateBorderPress (MouseFlags.LeftButtonPressed | MouseFlags.Alt));

        Assert.True (arranger.IsDragging);
        Assert.Equal (ViewArrangement.Movable, arranger.Arranging);
    }

    [Fact]
    public void BorderView_CustomMiddleButtonBinding_DragsWithMiddleButtonPositionReports ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable root = new () { Width = 40, Height = 20 };
        View view = CreateArrangeableView ();
        root.Add (view);
        app.Begin (root);

        BorderView borderView = (BorderView)view.Border.View!;
        borderView.MouseBindings.Remove (MouseFlags.LeftButtonPressed);
        borderView.MouseBindings.Add (MouseFlags.MiddleButtonPressed | MouseFlags.Shift, Command.Arrange);

        borderView.NewMouseEvent (CreateBorderPress (MouseFlags.MiddleButtonPressed | MouseFlags.Shift));
        borderView.NewMouseEvent (new Mouse
        {
            Position = new Point (2, 0),
            ScreenPosition = new Point (7, 5),
            Flags = MouseFlags.MiddleButtonPressed | MouseFlags.Shift | MouseFlags.PositionReport
        });

        Assert.Equal (7, view.Frame.X);
    }

    private static View CreateArrangeableView () =>
        new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 5,
            BorderStyle = LineStyle.Single,
            Arrangement = ViewArrangement.Movable,
            CanFocus = true
        };

    private static Mouse CreateBorderPress (MouseFlags flags) =>
        new ()
        {
            Position = new Point (0, 0),
            ScreenPosition = new Point (5, 5),
            Flags = flags
        };
}

namespace ApplicationTests.Navigation;

public class ApplicationNavigationTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData (TabBehavior.NoStop)]
    [InlineData (TabBehavior.TabStop)]
    [InlineData (TabBehavior.TabGroup)]
    public void Begin_SetsFocus_On_Deepest_Focusable_View (TabBehavior behavior)
    {
        using IApplication? application = Application.Create ();

        Runnable<bool> runnable = new()
        {
            TabStop = behavior,
            CanFocus = true
        };

        View subView = new ()
        {
            CanFocus = true,
            TabStop = behavior
        };
        runnable.Add (subView);

        View subSubView = new ()
        {
            CanFocus = true,
            TabStop = TabBehavior.NoStop
        };
        subView.Add (subSubView);

        SessionToken? rs = application.Begin (runnable);
        Assert.True (runnable.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.True (subSubView.HasFocus);

        runnable.Dispose ();
    }

    [Fact]
    public void Begin_SetsFocus_On_Top ()
    {
        using IApplication? application = Application.Create ();

        Runnable<bool> runnable = new () { CanFocus = true };
        Assert.False (runnable.HasFocus);

        application.Begin (runnable);
        Assert.True (runnable.HasFocus);

        runnable.Dispose ();
    }

    [Fact]
    public void Focused_Change_Raises_FocusedChanged ()
    {
        using IApplication? application = Application.Create ();

        var raised = false;

        application.Navigation!.FocusedChanged += ApplicationNavigationOnFocusedChanged;

        application.Navigation.SetFocused (new () { CanFocus = true, HasFocus = true });

        Assert.True (raised);

        application.Navigation.FocusedChanged -= ApplicationNavigationOnFocusedChanged;

        return;

        void ApplicationNavigationOnFocusedChanged (object? sender, EventArgs e) { raised = true; }
    }

    [Fact]
    public void GetFocused_Returns_Focused_View ()
    {
        using IApplication app = Application.Create ();

        app.Begin (
                   new Runnable<bool>
                   {
                       Id = "top",
                       CanFocus = true,
                       App = app
                   });

        var subView1 = new View
        {
            Id = "subView1",
            CanFocus = true
        };

        var subView2 = new View
        {
            Id = "subView2",
            CanFocus = true
        };

        app.TopRunnableView?.Add (subView1, subView2);
        subView1.SetFocus ();

        //app.TopRunnableView?.SetFocus ();
        Assert.True (subView1.HasFocus);
        Assert.Equal (subView1, app.Navigation?.GetFocused ());

        app.Navigation?.AdvanceFocus (NavigationDirection.Forward, null);
        Assert.Equal (subView2, app.Navigation?.GetFocused ());
    }

    [Fact]
    public void GetFocused_Returns_Null_If_No_Focused_View ()
    {
        using IApplication app = Application.Create ();

        app.Begin (
                   new Runnable<bool>
                   {
                       Id = "top",
                       CanFocus = true,
                       App = app
                   });

        var subView1 = new View
        {
            Id = "subView1",
            CanFocus = true
        };

        app.TopRunnableView!.Add (subView1);

        app.TopRunnableView.SetFocus ();
        Assert.True (subView1.HasFocus);
        Assert.Equal (subView1, app.Navigation!.GetFocused ());

        subView1.HasFocus = false;
        Assert.False (subView1.HasFocus);
        Assert.True (app.TopRunnableView.HasFocus);
        Assert.Equal (app.TopRunnableView, app.Navigation.GetFocused ());

        app.TopRunnableView.HasFocus = false;
        Assert.False (app.TopRunnableView.HasFocus);
        Assert.Null (app.Navigation.GetFocused ());
    }
}

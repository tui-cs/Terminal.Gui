using System.Reflection;
namespace ViewsTests;

[CollectionDefinition ("Reviewer Menu Navigation", DisableParallelization = true)]
public class ReviewerMenuNavigationCollection;

[Collection ("Reviewer Menu Navigation")]
public class ReviewerNavigationRegressionTests (ITestOutputHelper output) : IDisposable
{
    private readonly UICatalog.UICatalogCommandLineOptions _originalOptions = UICatalog.UICatalog.Options;

    public void Dispose () => UICatalog.UICatalog.Options = _originalOptions;

    [Theory]
    [InlineData ("right-after-wrap")]
    [InlineData ("down-wrap-single-selector")]
    [InlineData ("up-wrap-single-selector")]
    [InlineData ("left-from-first-option")]
    [InlineData ("left-from-middle-option")]
    [InlineData ("left-from-last-option")]
    public void Real_Catalog_Path (string path)
    {
        UICatalog.UICatalog.Options = new () { DebugLogLevel = "Warning" };
        using IApplication app = Application.Create (new VirtualTimeProvider ());
        app.Init (DriverRegistry.Names.ANSI);
        using UICatalog.UICatalogRunnable catalog = new ();
        MenuBar bar = (MenuBar)typeof (UICatalog.UICatalogRunnable).GetMethod ("CreateMenuBar", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke (catalog, null)!;
        Runnable host = new ();
        host.Add (bar);
        app.Begin (host);
        MenuBarItem theme = bar.SubViews.OfType<MenuBarItem> ().ElementAt (1);
        Menu root = theme.PopoverMenu!.Root!;
        MenuItem scheme = root.SubViews.OfType<MenuItem> ().Last ();
        Menu sub = scheme.SubMenu!;
        OptionSelector options = (OptionSelector)sub.SubViews.OfType<MenuItem> ().Single ().CommandView!;
        View [] choices = options.SubViews.ToArray ();
        void Describe () { View? f = app.Navigation!.GetFocused (); output.WriteLine ($"{path}: focus={f?.GetType().Name}/{f?.Id}/{f?.Title}; bar={bar.Focused?.Title}; submenu={sub.Visible}"); }
        app.InjectKey (MenuBar.DefaultKey);
        app.InjectKey (Key.CursorRight);
        Assert.Same (theme, bar.Focused);
        if (path == "right-after-wrap")
        {
            app.InjectKey (Key.CursorUp);
            Assert.Same (scheme, app.Navigation!.GetFocused ());
            app.InjectKey (Key.CursorRight);
            Describe ();
            Assert.Same (theme, bar.Focused);
            Assert.True (sub.HasFocus);
            return;
        }
        scheme.SetFocus ();
        app.InjectKey (Key.CursorRight);
        Assert.True (sub.HasFocus);
        int? valueBefore = options.Value;
        if (path == "down-wrap-single-selector")
        {
            choices.Last ().SetFocus (); Assert.Same (choices.Last (), app.Navigation!.GetFocused ());
            app.InjectKey (Key.CursorDown);
            Describe ();
            Assert.Same (choices.First (), app.Navigation!.GetFocused ());
            Assert.True (sub.Visible);
            Assert.Equal (valueBefore, options.Value);
        }
        else if (path == "up-wrap-single-selector")
        {
            choices.First ().SetFocus (); Assert.Same (choices.First (), app.Navigation!.GetFocused ());
            app.InjectKey (Key.CursorUp);
            Describe ();
            Assert.Same (choices.Last (), app.Navigation!.GetFocused ());
            Assert.True (sub.Visible);
            Assert.Equal (valueBefore, options.Value);
        }
        else
        {
            View start = path == "left-from-first-option" ? choices.First () : path == "left-from-last-option" ? choices.Last () : choices [2];
            start.SetFocus (); Assert.Same (start, app.Navigation!.GetFocused ());
            app.InjectKey (Key.CursorLeft);
            Describe ();
            Assert.Same (scheme, app.Navigation!.GetFocused ());
        }
    }
    [Theory]
    [InlineData ("left")]
    [InlineData ("down")]
    [InlineData ("up")]
    public void Standalone_Selector_Keeps_Existing_Navigation (string direction)
    {
        using IApplication app = Application.Create (new VirtualTimeProvider ());
        app.Init (DriverRegistry.Names.ANSI);
        OptionSelector selector = new () { Labels = ["One", "Two", "Three"], Value = 1 };
        Runnable host = new ();
        host.Add (selector);
        app.Begin (host);
        View [] choices = selector.SubViews.ToArray ();
        choices [1].SetFocus ();
        app.InjectKey (direction == "left" ? Key.CursorLeft : direction == "down" ? Key.CursorDown : Key.CursorUp);
        Assert.Same (choices [direction == "down" ? 2 : 0], app.Navigation!.GetFocused ());
        Assert.Equal (1, selector.Value);
    }

}

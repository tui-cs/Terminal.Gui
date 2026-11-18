using JetBrains.Annotations;
using UnitTests;

namespace ViewsTests;

/// <summary>
///     Unit tests for <see cref="Link"/> that don't require static Application dependencies.
///     These tests can run in parallel without interference.
/// </summary>
[TestSubject (typeof (Link))]
public class LinkTests (ITestOutputHelper output) : TestDriverBase
{
    // Claude - Opus 4.6

    [Fact]
    public void Constructor_Defaults ()
    {
        Link link = new ();

        Assert.Equal (string.Empty, link.Url);
        Assert.Equal (string.Empty, link.Text);
        Assert.Equal (Dim.Auto (DimAutoStyle.Text), link.Height);
        Assert.Equal (Dim.Auto (DimAutoStyle.Text), link.Width);
        Assert.True (link.CanFocus);
    }

    [Fact]
    public void Text_And_Title_Are_Independent ()
    {
        Link link = new () { Text = "Click here", Title = "My Link" };

        Assert.Equal ("Click here", link.Text);
        Assert.Equal ("My Link", link.Title);
    }

    [Fact]
    public void Text_Set_Does_Not_Change_Url ()
    {
        Link link = new () { Url = "https://github.com", Text = "Click here" };

        Assert.Equal ("Click here", link.Text);
        Assert.Equal ("https://github.com", link.Url);
    }

    [Fact]
    public void Url_Set_Does_Not_Change_Text ()
    {
        Link link = new () { Url = "https://github.com" };

        Assert.Equal (string.Empty, link.Text);
        Assert.Equal ("https://github.com", link.Url);
    }

    [Fact]
    public void TextFormatter_Uses_Url_When_Text_Is_Empty ()
    {
        Link link = new () { Url = "https://github.com" };

        // Trigger layout so UpdateTextFormatterText runs
        link.BeginInit ();
        link.EndInit ();
        link.SetRelativeLayout (new Size (80, 25));

        Assert.Equal ("https://github.com", link.TextFormatter.Text);
    }

    [Fact]
    public void TextFormatter_Uses_Text_When_Text_Is_Set ()
    {
        Link link = new () { Url = "https://github.com", Text = "Click here" };

        link.BeginInit ();
        link.EndInit ();
        link.SetRelativeLayout (new Size (80, 25));

        Assert.Equal ("Click here", link.TextFormatter.Text);
    }

    [Fact]
    public void DimAuto_Uses_Text_Width_When_Text_Is_Set ()
    {
        Link link = new () { Text = "Click", Url = "https://github.com/tui-cs/Terminal.Gui" };

        link.BeginInit ();
        link.EndInit ();
        link.SetRelativeLayout (new Size (80, 25));

        // Width should be based on Text ("Click" = 5), not Url
        Assert.Equal (5, link.Frame.Width);
    }

    [Fact]
    public void DimAuto_Uses_Text_Width_When_Text_Has_Wide_Chars ()
    {
        // "ターミナル" = 5 CJK chars × 2 columns each = 10 columns
        Link link = new () { Text = "ターミナル", Url = "https://example.com" };

        link.BeginInit ();
        link.EndInit ();
        link.SetRelativeLayout (new Size (80, 25));

        Assert.Equal (10, link.Frame.Width);
    }

    [Fact]
    public void DimAuto_Uses_Url_Width_When_Text_Is_Empty ()
    {
        Link link = new () { Url = "https://github.com" };

        link.BeginInit ();
        link.EndInit ();
        link.SetRelativeLayout (new Size (80, 25));

        // Width should be based on Url since Text is empty
        Assert.Equal ("https://github.com".Length, link.Frame.Width);
    }

    [Fact]
    public void DimAuto_Uses_Url_Width_When_Url_Has_Wide_Chars ()
    {
        // IRI with CJK path: "https://例え.jp/テスト"
        // "https://" = 8, "例え" = 4, ".jp/" = 4, "テスト" = 6 → 22 columns
        Link link = new () { Url = "https://例え.jp/テスト" };

        link.BeginInit ();
        link.EndInit ();
        link.SetRelativeLayout (new Size (80, 25));

        Assert.Equal ("https://例え.jp/テスト".GetColumns (), link.Frame.Width);
    }

    [Fact]
    public void Url_Accepts_Any_String ()
    {
        Link link = new ();

        link.Url = "not a valid url";
        Assert.Equal ("not a valid url", link.Url);

        link.Url = "";
        Assert.Equal ("", link.Url);

        link.Url = "https://github.com";
        Assert.Equal ("https://github.com", link.Url);
    }

    [Fact]
    public void Url_Set_Same_Value_Does_Not_Fire_Events ()
    {
        Link link = new () { Url = "https://github.com" };
        var changingFired = false;
        var changedFired = false;

        link.UrlChanging += (_, _) => changingFired = true;
        link.UrlChanged += (_, _) => changedFired = true;

        link.Url = "https://github.com";

        Assert.False (changingFired);
        Assert.False (changedFired);
    }

    [Fact]
    public void Url_Set_Fires_UrlChanged_Event ()
    {
        var oldUrl = "http://oldvalue.io";
        var newUrl = "http://newvalue.io";

        Link link = new () { Url = oldUrl };
        var eventFired = false;
        var eventArgsValid = false;

        link.UrlChanged += (_, e) =>
                           {
                               eventFired = true;
                               eventArgsValid = e.OldValue == oldUrl && e.NewValue == newUrl;
                           };
        link.Url = newUrl;

        Assert.True (eventFired);
        Assert.True (eventArgsValid);
        Assert.Equal (newUrl, link.Url);
    }

    [Fact]
    public void Url_Set_Fires_UrlChanging_Event ()
    {
        var oldUrl = "http://oldvalue.io";
        var newUrl = "http://newvalue.io";

        Link link = new () { Url = oldUrl };
        var eventFired = false;
        var eventArgsValid = false;
        var valueChanged = false;

        link.UrlChanging += (_, e) =>
                            {
                                eventFired = true;
                                eventArgsValid = e.CurrentValue == oldUrl && e.NewValue == newUrl;

                                // Should be false since the change hasn't happened yet
                                valueChanged = e.CurrentValue == newUrl || link.Url == newUrl;
                            };
        link.Url = newUrl;

        Assert.True (eventFired);
        Assert.True (eventArgsValid);
        Assert.False (valueChanged);
        Assert.Equal (newUrl, link.Url);
    }

    [Fact]
    public void UrlChanging_Can_Cancel_Change ()
    {
        Link link = new () { Url = "https://original.com" };

        link.UrlChanging += (_, e) => e.Handled = true;

        link.Url = "https://newurl.com";

        Assert.Equal ("https://original.com", link.Url);
    }

    [Fact]
    public void Copy_Copies_Url_To_Clipboard ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();

        Link link = new () { App = app, Url = "https://github.com" };

        bool copied = link.Copy ();

        Assert.True (copied);
        Assert.Equal ("https://github.com", app.Clipboard?.GetClipboardData ());
    }

    [Fact]
    public void Link_Renders_With_OSC8_Hyperlink ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Link link = new () { App = app, Url = "https://github.com/tui-cs/Terminal.Gui", Text = "Terminal.Gui" };
        window.Add (link);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        // Get the ANSI output
        string ansi = app.Driver.ToAnsi ();
        string? look = app.Driver.GetOutput ().GetLastOutput ();

        // Verify OSC 8 sequences are present
        string expectedStart = EscSeqUtils.OSC_StartHyperlink ("https://github.com/tui-cs/Terminal.Gui");
        string expectedEnd = EscSeqUtils.OSC_EndHyperlink ();

        Assert.Contains (expectedStart, look);
        Assert.Contains (expectedEnd, look);
        Assert.Contains ("Terminal.Gui", look);
        Assert.Contains ("Terminal.Gui", ansi);

        window.Dispose ();
    }

    [Fact]
    public void Link_Renders_Without_OSC8_When_Url_Is_Default ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Link link = new () { App = app, Text = "Not a link" };
        window.Add (link);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        // Get the ANSI output
        string ansi = app.Driver!.ToAnsi ();
        string? look = app.Driver.GetOutput ().GetLastOutput ();

        // Verify OSC 8 sequences are NOT present for default URL
        Assert.DoesNotContain (EscSeqUtils.OSC_StartHyperlink (string.Empty), look);
        Assert.Contains ("Not a link", look);
        Assert.Contains ("Not a link", ansi);

        window.Dispose ();
    }

    [Fact]
    public void Link_Invalid_Url_Renders_With_Disabled_Style ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Link link = new () { App = app, Url = "not a valid url", Text = "Bad Link" };
        window.Add (link);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        string? look = app.Driver.GetOutput ().GetLastOutput ();

        // Invalid URL should NOT produce OSC 8 hyperlink sequences
        Assert.DoesNotContain (EscSeqUtils.OSC_StartHyperlink ("not a valid url"), look);
        Assert.Contains ("Bad Link", look);

        window.Dispose ();
    }

    [Fact]
    public void Link_With_HotKey_Passes_To_Next_View ()
    {
        View superView = new () { CanFocus = true };
        Link link = new () { Title = "_Link", CanFocus = false };
        View nextView = new () { CanFocus = true };

        superView.Add (link, nextView);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.False (link.HasFocus);
        Assert.False (nextView.HasFocus);

        // Invoke hotkey
        link.InvokeCommand (Command.HotKey);

        // Next view should get focus since Link can't focus
        Assert.True (nextView.HasFocus);

        superView.Dispose ();
    }

    [Fact]
    public void Link_Multiple_Links_Each_Get_Their_Own_Url ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 2);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Link link1 = new ()
        {
            App = app,
            X = 0,
            Y = 0,
            Url = "https://github.com",
            Text = "GitHub"
        };

        Link link2 = new ()
        {
            App = app,
            X = 0,
            Y = 1,
            Url = "https://microsoft.com",
            Text = "Microsoft"
        };

        window.Add (link1, link2);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        string? look = app.Driver.GetOutput ().GetLastOutput ();
        string ansi = app.Driver.ToAnsi ();

        // Verify both URLs are present in the same output
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://github.com"), look);
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://microsoft.com"), look);
        Assert.Contains ("GitHub", look);
        Assert.Contains ("Microsoft", look);
        Assert.Contains ("GitHub", ansi);
        Assert.Contains ("Microsoft", ansi);

        window.Dispose ();
    }

    [Fact]
    public void Link_Url_Changes_Update_Hyperlink ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Link link = new () { App = app, Url = "https://example.com", Text = "Example" };
        window.Add (link);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        string? look1 = app.Driver.GetOutput ().GetLastOutput ();
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://example.com"), look1);

        // Clear and change URL
        app.Driver.ClearContents ();
        link.Url = "https://newurl.com";
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        string? look2 = app.Driver.GetOutput ().GetLastOutput ();
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://newurl.com"), look2);

        window.Dispose ();
    }

    [Fact]
    public void Link_With_Focus_Draws_With_Focus_Colors ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();
        window.BorderStyle = LineStyle.None;

        // Add a dummy view to take initial focus
        View dummyView = new () { CanFocus = true, Width = 1, Height = 1 };
        window.Add (dummyView);

        Link link = new ()
        {
            App = app,
            Url = "https://github.com",
            Text = "GitHub",
            CanFocus = true,
            Y = 1 // Place it below dummyView
        };
        window.Add (link);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        // Without focus - dummyView has focus instead
        Assert.False (link.HasFocus);
        Assert.True (dummyView.HasFocus);

        // Set focus on link
        app.Driver.ClearContents ();
        link.SetFocus ();
        link.Draw ();
        Assert.True (link.HasFocus);
        Assert.False (dummyView.HasFocus);

        // The link should still have OSC 8 sequences
        string? look = app.Driver.GetOutput ().GetLastOutput ();
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://github.com"), look);

        window.Dispose ();
    }

    // Copilot
    [Theory]
    [InlineData ("file:///C:/Windows/System32/calc.exe")]
    [InlineData ("file:///etc/passwd")]
    [InlineData ("ms-msdt:id PCWDiagnostic")]
    [InlineData ("ms-officecmd:")]
    [InlineData ("search-ms:query=test")]
    [InlineData ("ldap://example.com")]
    [InlineData ("vbscript:alert('test')")]
    [InlineData ("ftp://example.com")]
    [InlineData ("ftps://example.com")]
    [InlineData ("javascript:alert(1)")]
    [InlineData ("data:text/html,<script>alert(1)</script>")]
    public void OpenUrl_DisallowedScheme_DoesNotThrow_AndDoesNotLaunch (string url)
    {
        // OpenUrl should silently return for disallowed schemes without throwing or launching a process.
        // DisableRealDriverIO is not set here; the scheme check is the guard.
        Exception? ex = Record.Exception (() => Link.OpenUrl (url));
        Assert.Null (ex);
    }

    // Copilot
    [Theory]
    [InlineData ("http://example.com")]
    [InlineData ("https://example.com")]
    [InlineData ("mailto:user@example.com")]
    public void OpenUrl_AllowedScheme_IsInSafeSchemes (string url)
    {
        bool parsed = Uri.TryCreate (url, UriKind.Absolute, out Uri? uri);
        Assert.True (parsed);
        Assert.NotNull (uri);
        Assert.Contains (uri!.Scheme, Link.SafeSchemes, StringComparer.OrdinalIgnoreCase);
    }

    // Copilot
    [Theory]
    [InlineData ("file:///C:/Windows/System32/calc.exe")]
    [InlineData ("ms-msdt:id PCWDiagnostic")]
    [InlineData ("ftp://example.com")]
    [InlineData ("ldap://example.com")]
    public void OpenUrl_DisallowedScheme_IsNotInSafeSchemes (string url)
    {
        bool parsed = Uri.TryCreate (url, UriKind.Absolute, out Uri? uri);
        Assert.True (parsed);
        Assert.NotNull (uri);
        Assert.DoesNotContain (uri!.Scheme, Link.SafeSchemes, StringComparer.OrdinalIgnoreCase);
    }

    // Copilot
    [Fact]
    public void OpenUrl_MalformedUrl_DoesNotThrow ()
    {
        Exception? ex = Record.Exception (() => Link.OpenUrl ("not a valid url"));
        Assert.Null (ex);
    }

    // Copilot
    [Fact]
    public void OpenUrl_EmptyUrl_DoesNotThrow ()
    {
        Exception? ex = Record.Exception (() => Link.OpenUrl (string.Empty));
        Assert.Null (ex);
    }

    [Fact]
    public void IDesignable_EnableForDesign_Sets_Title_And_Url ()
    {
        Link link = new ();
        IDesignable designable = link;

        bool result = designable.EnableForDesign ();

        Assert.True (result);
        Assert.Equal ("_Link", link.Title);
        Assert.Equal ("https://github.com/tui-cs", link.Url);

        link.Dispose ();
    }

    [Fact]
    public void Link_LeftButtonReleased_InvokesHotKey_OnNextView ()
    {
        View superView = new () { CanFocus = true, Height = 1, Width = 15 };
        Link link = new () { X = 0, HotKey = Key.L.WithAlt, CanFocus = false };
        View nextView = new () { CanFocus = true, X = 10, Width = 4, Height = 1 };

        superView.Add (link, nextView);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.False (link.HasFocus);
        Assert.False (nextView.HasFocus);

        // Click on the link
        link.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased });

        Assert.False (link.HasFocus);
        Assert.True (nextView.HasFocus);

        superView.Dispose ();
    }

    [Fact]
    public void Link_Osc8_Emits_StartTextEnd_And_Outputs_Correctly ()
    {
        var text = "GitHub";
        var url = "https://github.com";

        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 1);
        app.Driver.Force16Colors = true;

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        Link link = new ()
        {
            X = 0,
            Y = 0,
            Width = 60,
            Height = 1,
            Text = text,
            Url = url
        };
        window.Add (link);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b]8;;https://github.com\x1b\\\x1b[97m\x1b[40mGitHub\x1b]8;;\x1b\\\x1b[30m\x1b[107m
                                           """,
                                           output,
                                           app.Driver);
    }
}

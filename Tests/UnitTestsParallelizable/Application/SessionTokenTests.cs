#nullable enable
namespace ApplicationTests;

/// <summary>These tests focus on Application.SessionToken and the various ways it can be changed.</summary>
[Collection("Application Tests")]
public class SessionTokenTests
{
    [Fact]
    public void Begin_Throws_On_Null ()
    {
        IApplication? app = Application.Create ();
        // Test null Runnable
        Assert.Throws<ArgumentNullException> (() => app.Begin (null!));
    }

    [Fact]
    public void Begin_End_Cleans_Up_SessionToken ()
    {
        IApplication? app = Application.Create ();

        Runnable<bool> top = new Runnable<bool> ();
        SessionToken? sessionToken = app.Begin (top);
        Assert.NotNull (sessionToken);
        app.End (sessionToken);

        Assert.Null (app.TopRunnableView);

        Assert.DoesNotContain(sessionToken, app.SessionStack!);

        top.Dispose ();

    }

    [Fact]
    public void New_Creates_SessionToken ()
    {
        var rs = new SessionToken (null!);
        Assert.Null (rs.Runnable);

        var top = new Runnable ();
        rs = new (top);
        Assert.Equal (top, rs.Runnable);
    }
}

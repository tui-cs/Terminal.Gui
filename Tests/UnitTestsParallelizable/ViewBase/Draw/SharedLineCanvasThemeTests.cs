using System.Reflection;
using UnitTests;

namespace ViewBaseTests.Draw;

// CoPilot - gpt-6.1-sol
[Collection ("StaticSettingsTests")]
public class SharedLineCanvasThemeTests : TestDriverBase
{
    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void ThemeNotification_RebuildsCleanSharedContribution (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (20, 8);
        using View root = new () { Driver = driver, Width = 20, Height = 8 };
        View shared = new ()
        {
            Width = 12, Height = 6, BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true,
            Arrangement = overlapped ? ViewArrangement.Overlapped : ViewArrangement.Fixed
        };
        Button button = new () { X = 14, Text = "B" };
        root.Add (shared, button);
        root.Layout ();
        root.Draw ();
        var borderDraws = 0;
        shared.Border.View!.DrawComplete += (_, _) => borderDraws++;

        // Notify through the same facade used after applying theme glyph/configuration
        // overlays. No global theme settings are changed by this test.
        ThemeManager.RaiseThemeChanged (ThemeManager.Theme);
        button.SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();

        Assert.Equal (1, borderDraws);
        string partial = driver.ToString ();
        root.SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        Assert.Equal (partial, driver.ToString ());
    }

    [Fact]
    public void CacheCreation_DoesNotSubscribeToStaticThemeEvent ()
    {
        using IDriver driver = CreateTestDriver (20, 8);
        using View root = new () { Driver = driver, Width = 20, Height = 8 };
        View shared = new () { Width = 12, Height = 6, BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true };
        root.Add (shared);
        root.Layout ();
        int before = CountHandlers ();
        root.Draw ();
        Assert.Equal (before, CountHandlers ());

        root.Remove (shared);
        shared.BorderStyle = LineStyle.Double;
        root.Add (shared);
        root.Layout ();
        root.SetClipToScreen ();
        root.Draw ();
        Assert.Equal (before, CountHandlers ());

        static int CountHandlers () =>
            ((Delegate?)typeof (ThemeChanges).GetField (nameof (ThemeChanges.ThemeChanged), BindingFlags.NonPublic | BindingFlags.Static)!
                                            .GetValue (null))?.GetInvocationList ().Length ?? 0;
    }
}

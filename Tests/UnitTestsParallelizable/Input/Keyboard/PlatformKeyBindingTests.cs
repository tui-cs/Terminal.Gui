using System.Text.Json;
using Terminal.Gui.Configuration;

namespace InputTests;

// Copilot - Opus 4.6
public class PlatformKeyBindingTests
{
    [Fact]
    public void PlatformKeyBinding_Default_AllPropertiesNull ()
    {
        PlatformKeyBinding pkb = new ();

        Assert.Null (pkb.All);
        Assert.Null (pkb.Windows);
        Assert.Null (pkb.Linux);
        Assert.Null (pkb.Macos);
    }

    [Fact]
    public void PlatformKeyBinding_All_SetsCorrectly ()
    {
        PlatformKeyBinding pkb = new () { All = [Key.CursorLeft, Key.Home] };

        Assert.NotNull (pkb.All);
        Assert.Equal (2, pkb.All!.Length);
        Assert.Equal (Key.CursorLeft, pkb.All [0]);
        Assert.Equal (Key.Home, pkb.All [1]);
    }

    [Fact]
    public void PlatformKeyBinding_Windows_SetsCorrectly ()
    {
        PlatformKeyBinding pkb = new () { Windows = [Key.Delete] };

        Assert.Null (pkb.All);
        Assert.NotNull (pkb.Windows);
        Assert.Single (pkb.Windows!);
        Assert.Equal (Key.Delete, pkb.Windows [0]);
    }

    [Fact]
    public void PlatformKeyBinding_Linux_SetsCorrectly ()
    {
        PlatformKeyBinding pkb = new () { Linux = [Key.D.WithCtrl] };

        Assert.Null (pkb.All);
        Assert.NotNull (pkb.Linux);
        Assert.Single (pkb.Linux!);
        Assert.Equal (Key.D.WithCtrl, pkb.Linux [0]);
    }

    [Fact]
    public void PlatformKeyBinding_Macos_SetsCorrectly ()
    {
        PlatformKeyBinding pkb = new () { Macos = [Key.Backspace] };

        Assert.Null (pkb.All);
        Assert.NotNull (pkb.Macos);
        Assert.Single (pkb.Macos!);
        Assert.Equal (Key.Backspace, pkb.Macos [0]);
    }

    [Fact]
    public void GetCurrentPlatformKeys_AllOnly_ReturnsAllKeys ()
    {
        PlatformKeyBinding pkb = new () { All = [Key.CursorLeft, Key.Home] };

        List<Key> keys = pkb.GetCurrentPlatformKeys ().ToList ();

        Assert.Contains (Key.CursorLeft, keys);
        Assert.Contains (Key.Home, keys);
    }

    [Fact]
    public void GetCurrentPlatformKeys_PlatformOnly_ReturnsCurrentPlatformKeys ()
    {
        PlatformKeyBinding pkb = OperatingSystem.IsWindows () ? new () { Windows = [Key.F1] } :
            OperatingSystem.IsMacOS () ? new () { Macos = [Key.F1] } :
            new () { Linux = [Key.F1] };

        List<Key> keys = pkb.GetCurrentPlatformKeys ().ToList ();

        Assert.Single (keys);
        Assert.Equal (Key.F1, keys [0]);
    }

    [Fact]
    public void GetCurrentPlatformKeys_AllPlusPlatform_Additive ()
    {
        PlatformKeyBinding pkb = OperatingSystem.IsWindows () ? new () { All = [Key.Esc], Windows = [Key.Q.WithCtrl] } :
            OperatingSystem.IsMacOS () ? new () { All = [Key.Esc], Macos = [Key.Q.WithCtrl] } :
            new () { All = [Key.Esc], Linux = [Key.Q.WithCtrl] };

        List<Key> keys = pkb.GetCurrentPlatformKeys ().ToList ();

        Assert.Equal (2, keys.Count);
        Assert.Equal (Key.Esc, keys [0]);
        Assert.Equal (Key.Q.WithCtrl, keys [1]);
    }

    [Fact]
    public void GetCurrentPlatformKeys_OtherPlatformOnly_ReturnsEmpty ()
    {
        PlatformKeyBinding pkb = OperatingSystem.IsWindows () ? new () { Linux = [Key.F1] } :
            OperatingSystem.IsMacOS () ? new () { Windows = [Key.F1] } :
            new () { Windows = [Key.F1] };

        List<Key> keys = pkb.GetCurrentPlatformKeys ().ToList ();

        Assert.Empty (keys);
    }

    [Fact]
    public void GetCurrentPlatformKeys_AllNull_ReturnsEmpty ()
    {
        PlatformKeyBinding pkb = new ();

        List<Key> keys = pkb.GetCurrentPlatformKeys ().ToList ();

        Assert.Empty (keys);
    }

    [Fact]
    public void ToString_ShowsAllPlatforms ()
    {
        PlatformKeyBinding pkb = new ()
        {
            All = [Key.Esc],
            Windows = [Key.Q.WithCtrl],
            Linux = [Key.Q.WithCtrl],
            Macos = [Key.Q.WithCtrl]
        };

        string result = pkb.ToString ();

        Assert.Contains ("All=", result);
        Assert.Contains ("Win=", result);
        Assert.Contains ("Linux=", result);
        Assert.Contains ("Mac=", result);
    }

    [Fact]
    public void ToString_NullProperties_Omitted ()
    {
        PlatformKeyBinding pkb = new () { All = [Key.Esc] };

        string result = pkb.ToString ();

        Assert.Contains ("All=", result);
        Assert.DoesNotContain ("Win=", result);
        Assert.DoesNotContain ("Linux=", result);
        Assert.DoesNotContain ("Mac=", result);
    }

    [Fact]
    public void ToString_Empty_ReturnsNone ()
    {
        PlatformKeyBinding pkb = new ();

        string result = pkb.ToString ();

        Assert.Equal ("(none)", result);
    }

    [Fact]
    public void PlatformKeyBinding_RoundTrips_ThroughJson ()
    {
        PlatformKeyBinding original = new ()
        {
            All = [Key.Esc, Key.Q.WithCtrl],
            Windows = [Key.F4.WithAlt],
            Macos = [Key.Q.WithCtrl]
        };

        JsonSerializerOptions options = new ()
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize (original, SourceGenerationContext.Default.PlatformKeyBinding);
        PlatformKeyBinding? deserialized = JsonSerializer.Deserialize (json, SourceGenerationContext.Default.PlatformKeyBinding);

        Assert.NotNull (deserialized);

        // Verify All keys
        Assert.NotNull (deserialized!.All);
        Assert.Equal (2, deserialized.All!.Length);
        Assert.Equal (Key.Esc, deserialized.All [0]);
        Assert.Equal (Key.Q.WithCtrl, deserialized.All [1]);

        // Verify Windows keys
        Assert.NotNull (deserialized.Windows);
        Assert.Single (deserialized.Windows!);
        Assert.Equal (Key.F4.WithAlt, deserialized.Windows [0]);

        // Verify Linux null
        Assert.Null (deserialized.Linux);

        // Verify Macos keys
        Assert.NotNull (deserialized.Macos);
        Assert.Single (deserialized.Macos!);
        Assert.Equal (Key.Q.WithCtrl, deserialized.Macos [0]);
    }
}

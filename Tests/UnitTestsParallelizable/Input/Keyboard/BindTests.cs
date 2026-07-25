namespace InputTests;

// Claude - Opus 4.6
public class BindTests
{
    [Fact]
    public void Bind_All_SingleKey_ReturnsPlatformKeyBinding ()
    {
        PlatformKeyBinding result = Bind.All (Key.CursorLeft);

        Assert.Equal ((Key [])[Key.CursorLeft], result.All!.AsEnumerable ());
        Assert.Null (result.Windows);
        Assert.Null (result.Linux);
        Assert.Null (result.Macos);
    }

    [Fact]
    public void Bind_All_MultipleKeys ()
    {
        PlatformKeyBinding result = Bind.All (Key.Home, Key.Home.WithCtrl);

        Assert.Equal ((Key [])[Key.Home, Key.Home.WithCtrl], result.All!.AsEnumerable ());
    }

    [Fact]
    public void Bind_AllPlus_NonWindowsKeys ()
    {
        PlatformKeyBinding result = Bind.AllPlus (Key.Delete, [Key.D.WithCtrl]);

        Assert.Equal ((Key [])[Key.Delete], result.All!.AsEnumerable ());
        Assert.Null (result.Windows);
        Assert.Equal ((Key [])[Key.D.WithCtrl], result.Linux!.AsEnumerable ());
        Assert.Equal ((Key [])[Key.D.WithCtrl], result.Macos!.AsEnumerable ());
    }

    [Fact]
    public void Bind_AllPlus_WindowsKeys ()
    {
        PlatformKeyBinding result = Bind.AllPlus (Key.X, windows: [Key.X.WithCtrl]);

        Assert.Equal ((Key [])[Key.X], result.All!.AsEnumerable ());
        Assert.Equal ((Key [])[Key.X.WithCtrl], result.Windows!.AsEnumerable ());
        Assert.Null (result.Linux);
        Assert.Null (result.Macos);
    }

    [Fact]
    public void Bind_AllPlus_NullPlatforms_LeavesNull ()
    {
        PlatformKeyBinding result = Bind.AllPlus (Key.Delete);

        Assert.Equal ((Key [])[Key.Delete], result.All!.AsEnumerable ());
        Assert.Null (result.Windows);
        Assert.Null (result.Linux);
        Assert.Null (result.Macos);
    }

    [Fact]
    public void Bind_NonWindows_SetsLinuxAndMacos ()
    {
        PlatformKeyBinding result = Bind.NonWindows (Key.Z.WithCtrl);

        Assert.Null (result.All);
        Assert.Null (result.Windows);
        Assert.Equal ((Key [])[Key.Z.WithCtrl], result.Linux!.AsEnumerable ());
        Assert.Equal ((Key [])[Key.Z.WithCtrl], result.Macos!.AsEnumerable ());
    }

    [Fact]
    public void Bind_Platform_LinuxOnly ()
    {
        PlatformKeyBinding result = Bind.Platform (linux: [Key.Z.WithCtrl]);

        Assert.Null (result.All);
        Assert.Null (result.Windows);
        Assert.Equal ((Key [])[Key.Z.WithCtrl], result.Linux!.AsEnumerable ());
        Assert.Null (result.Macos);
    }

    [Fact]
    public void Bind_Platform_WindowsAndMacos ()
    {
        PlatformKeyBinding result = Bind.Platform ([Key.Z.WithCtrl], macos: [Key.Z.WithAlt]);

        Assert.Null (result.All);
        Assert.Equal ((Key [])[Key.Z.WithCtrl], result.Windows!.AsEnumerable ());
        Assert.Null (result.Linux);
        Assert.Equal ((Key [])[Key.Z.WithAlt], result.Macos!.AsEnumerable ());
    }

    [Fact]
    public void Bind_Platform_AllNulls_ReturnsEmpty ()
    {
        PlatformKeyBinding result = Bind.Platform ();

        Assert.Null (result.All);
        Assert.Null (result.Windows);
        Assert.Null (result.Linux);
        Assert.Null (result.Macos);
    }
}

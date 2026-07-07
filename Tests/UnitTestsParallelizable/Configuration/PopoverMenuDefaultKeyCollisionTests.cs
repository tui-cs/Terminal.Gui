// Claude - Opus 4.8

using System.IO.Abstractions.TestingHelpers;
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

/// <summary>
///     Verifies that views binding <see cref="PopoverMenu.DefaultKey"/> to <see cref="Command.Context"/>
///     do not throw when the configured key collides with a key the view already binds
///     (e.g. Ctrl+P, which <see cref="TableView"/> binds to <see cref="Command.Up"/> by default).
/// </summary>
[Collection ("StaticSettingsTests")]
public class PopoverMenuDefaultKeyCollisionTests
{
    [Fact]
    public void FileDialog_Ctor_DoesNotThrow_WhenDefaultKeyCollidesWithTableViewBinding ()
    {
        PopoverMenuSettings original = PopoverMenuSettings.Defaults;

        try
        {
            // TableView binds Ctrl+P -> Command.Up by default; the context-menu key must win without throwing.
            PopoverMenuSettings.Defaults = new () { DefaultKey = Key.P.WithCtrl };

            MockFileSystem fs = new ();
            fs.AddDirectory ("/testdir");

            using FileDialog fd = new (fs);

            Assert.NotNull (fd);
        }
        finally
        {
            PopoverMenuSettings.Defaults = original;
        }
    }

    [Fact]
    public void CharMap_Ctor_DoesNotThrow_WhenDefaultKeyCollidesWithExistingBinding ()
    {
        PopoverMenuSettings original = PopoverMenuSettings.Defaults;

        try
        {
            // CharMap binds Key.End -> Command.End before binding the context-menu key.
            PopoverMenuSettings.Defaults = new () { DefaultKey = Key.End };

            using CharMap charMap = new ();

            Assert.NotNull (charMap);
        }
        finally
        {
            PopoverMenuSettings.Defaults = original;
        }
    }
}

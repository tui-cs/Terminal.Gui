// Claude - Opus 4.6

using System.Text.Json;

namespace InputTests;

public class KeyBindingSchemaTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new () { TypeInfoResolver = SourceGenerationContext.Default };

    [Fact]
    public void PlatformKeyBinding_RoundTrips_ThroughJson ()
    {
        // Arrange
        PlatformKeyBinding original = new () { All = ["CursorLeft", "Home"], Windows = ["Alt+CursorLeft"], Linux = ["Ctrl+B"], Macos = ["Alt+CursorLeft"] };

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<PlatformKeyBinding> (json, _jsonOptions);

        // Assert
        Assert.NotNull (deserialized);
        Assert.Equal (original.All!.AsEnumerable (), deserialized.All!.AsEnumerable ());
        Assert.Equal (original.Windows!.AsEnumerable (), deserialized.Windows!.AsEnumerable ());
        Assert.Equal (original.Linux!.AsEnumerable (), deserialized.Linux!.AsEnumerable ());
        Assert.Equal (original.Macos!.AsEnumerable (), deserialized.Macos!.AsEnumerable ());
    }

    [Fact]
    public void KeyBindingDict_RoundTrips_ThroughJson ()
    {
        // Arrange
        Dictionary<Command, PlatformKeyBinding> original = new ()
        {
            [Command.Left] = new PlatformKeyBinding { All = ["CursorLeft"] },
            [Command.Right] = new PlatformKeyBinding { All = ["CursorRight"], Linux = ["Ctrl+F"] }
        };

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);
        Dictionary<Command, PlatformKeyBinding>? deserialized = JsonSerializer.Deserialize<Dictionary<Command, PlatformKeyBinding>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (deserialized);
        Assert.Equal (2, deserialized.Count);
        Assert.Equal ((Key [])["CursorLeft"], deserialized [Command.Left].All!.AsEnumerable ());
        Assert.Equal ((Key [])["Ctrl+F"], deserialized [Command.Right].Linux!.AsEnumerable ());
    }

    [Fact]
    public void KeyBindingDict_Deserializes_FromUserConfigFormat ()
    {
        // Arrange
        var json = """{ "Left": { "All": ["CursorLeft"], "Linux": ["Ctrl+B"] } }""";

        // Act
        Dictionary<Command, PlatformKeyBinding>? result = JsonSerializer.Deserialize<Dictionary<Command, PlatformKeyBinding>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (result);
        Assert.Single (result);
        Assert.True (result.ContainsKey (Command.Left));
        Assert.Equal ((Key [])["CursorLeft"], result [Command.Left].All!.AsEnumerable ());
        Assert.Equal ((Key [])["Ctrl+B"], result [Command.Left].Linux!.AsEnumerable ());
        Assert.Null (result [Command.Left].Windows);
        Assert.Null (result [Command.Left].Macos);
    }

    [Fact]
    public void KeyBindingDict_EmptyDict_RoundTrips ()
    {
        // Arrange
        Dictionary<Command, PlatformKeyBinding> original = [];

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);
        Dictionary<Command, PlatformKeyBinding>? deserialized = JsonSerializer.Deserialize<Dictionary<Command, PlatformKeyBinding>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (deserialized);
        Assert.Empty (deserialized);
    }

    [Fact]
    public void ViewKeyBindings_RoundTrips_ThroughJson ()
    {
        // Arrange
        Dictionary<string, Dictionary<Command, PlatformKeyBinding>> original = new ()
        {
            ["TextView"] = new Dictionary<Command, PlatformKeyBinding>
            {
                [Command.Left] = new () { All = ["CursorLeft"], Macos = ["Alt+CursorLeft"] }, [Command.SelectAll] = new () { All = ["Ctrl+A"] }
            },
            ["TextField"] = new Dictionary<Command, PlatformKeyBinding> { [Command.DeleteAll] = new () { All = ["Ctrl+Shift+Delete"] } }
        };

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);

        Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? deserialized =
            JsonSerializer.Deserialize<Dictionary<string, Dictionary<Command, PlatformKeyBinding>>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (deserialized);
        Assert.Equal (2, deserialized.Count);
        Assert.Equal ((Key [])["CursorLeft"], deserialized ["TextView"] [Command.Left].All!.AsEnumerable ());
        Assert.Equal ((Key [])["Alt+CursorLeft"], deserialized ["TextView"] [Command.Left].Macos!.AsEnumerable ());
        Assert.Equal ((Key [])["Ctrl+A"], deserialized ["TextView"] [Command.SelectAll].All!.AsEnumerable ());
        Assert.Equal ((Key [])["Ctrl+Shift+Delete"], deserialized ["TextField"] [Command.DeleteAll].All!.AsEnumerable ());
    }
}

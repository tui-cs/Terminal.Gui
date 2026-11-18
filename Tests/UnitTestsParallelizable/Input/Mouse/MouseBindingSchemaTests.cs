// Copilot

using System.Text.Json;

namespace InputTests;

public class MouseBindingSchemaTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new () { TypeInfoResolver = SourceGenerationContext.Default };

    [Fact]
    public void PlatformMouseBinding_RoundTrips_ThroughJson ()
    {
        // Arrange
        PlatformMouseBinding original = new ()
        {
            All = [MouseFlags.LeftButtonReleased],
            Windows = [MouseFlags.LeftButtonPressed | MouseFlags.Alt],
            Linux = [MouseFlags.LeftButtonPressed | MouseFlags.Shift],
            Macos = [MouseFlags.LeftButtonPressed | MouseFlags.Shift]
        };

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);
        PlatformMouseBinding? deserialized = JsonSerializer.Deserialize<PlatformMouseBinding> (json, _jsonOptions);

        // Assert
        Assert.NotNull (deserialized);
        Assert.Equal (original.All!.AsEnumerable (), deserialized.All!.AsEnumerable ());
        Assert.Equal (original.Windows!.AsEnumerable (), deserialized.Windows!.AsEnumerable ());
        Assert.Equal (original.Linux!.AsEnumerable (), deserialized.Linux!.AsEnumerable ());
        Assert.Equal (original.Macos!.AsEnumerable (), deserialized.Macos!.AsEnumerable ());
    }

    [Fact]
    public void MouseBindingDict_Deserializes_FromUserConfigFormat ()
    {
        // Arrange
        string json =
            """
            {
              "StartSelection": {
                "Windows": ["Alt+LeftButtonPressed"],
                "Linux": ["Shift+LeftButtonPressed"],
                "Macos": ["Shift+LeftButtonPressed"]
              }
            }
            """;

        // Act
        Dictionary<Command, PlatformMouseBinding>? result = JsonSerializer.Deserialize<Dictionary<Command, PlatformMouseBinding>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (result);
        Assert.True (result.ContainsKey (Command.StartSelection));
        Assert.Equal ((MouseFlags [])[MouseFlags.LeftButtonPressed | MouseFlags.Alt], result [Command.StartSelection].Windows!.AsEnumerable ());
        Assert.Equal ((MouseFlags [])[MouseFlags.LeftButtonPressed | MouseFlags.Shift], result [Command.StartSelection].Linux!.AsEnumerable ());
        Assert.Equal ((MouseFlags [])[MouseFlags.LeftButtonPressed | MouseFlags.Shift], result [Command.StartSelection].Macos!.AsEnumerable ());
    }
}
